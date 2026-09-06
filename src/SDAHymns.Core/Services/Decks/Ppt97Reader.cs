using System.Buffers.Binary;
using System.Text;
using OpenMcdf;

namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// Reads PowerPoint 97-2003 (.ppt) decks directly from their binary records.
/// </summary>
/// <remarks>
/// <para>
/// Replaces the old "shell out to LibreOffice, convert to .pptx, reopen" route. That
/// cost a process per file with a 45s timeout, and silently produced nothing when the
/// conversion stalled.
/// </para>
/// <para>
/// The format stores every save generation in one stream, so the bytes on disk contain
/// records that are no longer part of the presentation. Reading it correctly means
/// following the live chain rather than scanning for records:
/// </para>
/// <list type="number">
///   <item>the <c>Current User</c> stream points at the most recent UserEditAtom;</item>
///   <item>each UserEditAtom points back to the previous one, and to a PersistDirectory;</item>
///   <item>replaying those directories oldest-first yields persist-id to offset, newest winning;</item>
///   <item>the Document container's SlideListWithText gives true presentation order.</item>
/// </list>
/// <para>
/// Scanning for SlideContainers instead - the obvious approach - picks up orphaned
/// slides from earlier saves. On this library that inflates hymn 422 from 7 slides to 12.
/// </para>
/// </remarks>
public sealed class Ppt97Reader : IDeckReader
{
    // Record types (MS-PPT).
    private const ushort RtDocument = 0x03E8;
    private const ushort RtSlide = 0x03EE;
    private const ushort RtUserEditAtom = 0x0FF5;
    private const ushort RtPersistDirectoryAtom = 0x1772;
    private const ushort RtSlideListWithText = 0x0FF0;
    private const ushort RtSlidePersistAtom = 0x03F3;
    private const ushort RtTextCharsAtom = 0x0FA0;   // UTF-16LE
    private const ushort RtTextBytesAtom = 0x0FA8;   // one byte per character, high byte 0x00

    /// <summary>SlideListWithText instance 0 holds slides; 1 is masters, 2 is notes.</summary>
    private const int SlideListInstanceSlides = 0;

    /// <summary>A container record is flagged by the low nibble of the version/instance field.</summary>
    private const int ContainerVersion = 0xF;

    private const int RecordHeaderSize = 8;

    /// <summary>Guards against a corrupt UserEdit chain looping forever.</summary>
    private const int MaxSaveGenerations = 512;

    private static readonly byte[] Ole2Signature =
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public bool CanRead(ReadOnlySpan<byte> header) =>
        header.Length >= Ole2Signature.Length && header[..8].SequenceEqual(Ole2Signature);

    public SlideDeck Read(string path)
    {
        byte[] ppt, currentUser;
        try
        {
            using var root = RootStorage.OpenRead(path);
            ppt = ReadStream(root, "PowerPoint Document");
            currentUser = ReadStream(root, "Current User");
        }
        catch (Exception ex) when (ex is not DeckReadException)
        {
            throw new DeckReadException($"Not a readable PowerPoint 97 file: {path}", ex);
        }

        var persist = BuildPersistMap(ppt, currentUser);
        var slides = ReadSlidesInOrder(ppt, persist);
        return new SlideDeck(path, DeckFormat.Ppt97, slides);
    }

    private static byte[] ReadStream(RootStorage root, string name)
    {
        try
        {
            using var stream = root.OpenStream(name);
            var buffer = new byte[stream.Length];
            stream.ReadExactly(buffer);
            return buffer;
        }
        catch (Exception ex)
        {
            throw new DeckReadException($"Missing '{name}' stream", ex);
        }
    }

    /// <summary>
    /// Replays the UserEdit chain to map persist id -> byte offset of the live record.
    /// </summary>
    private static Dictionary<uint, uint> BuildPersistMap(byte[] ppt, byte[] currentUser)
    {
        // CurrentUserAtom body: size(4) headerToken(4) offsetToCurrentEdit(4)
        if (currentUser.Length < RecordHeaderSize + 12)
        {
            throw new DeckReadException("'Current User' stream is truncated");
        }

        var offset = ReadU32(currentUser, RecordHeaderSize + 8);

        // Walk newest -> oldest, remembering each generation's persist directory.
        var directories = new List<uint>();
        var visited = new HashSet<uint>();
        while (offset != 0 && offset + RecordHeaderSize <= (uint)ppt.Length && visited.Add(offset))
        {
            if (directories.Count >= MaxSaveGenerations)
            {
                throw new DeckReadException("UserEdit chain is implausibly long; file is corrupt");
            }

            var (_, type, _, body) = ReadHeader(ppt, offset);
            if (type != RtUserEditAtom)
            {
                break;
            }

            // UserEditAtom: lastSlideIdRef(4) version(2) minor(1) major(1)
            //               offsetLastEdit(4) offsetPersistDirectory(4) ...
            var offsetLastEdit = ReadU32(ppt, body + 8);
            directories.Add(ReadU32(ppt, body + 12));
            offset = offsetLastEdit;
        }

        // Oldest first, so a newer generation overwrites the entry it replaced.
        var map = new Dictionary<uint, uint>();
        for (var i = directories.Count - 1; i >= 0; i--)
        {
            ApplyPersistDirectory(ppt, directories[i], map);
        }

        if (map.Count == 0)
        {
            throw new DeckReadException("No persist directory found; file is corrupt");
        }

        return map;
    }

    private static void ApplyPersistDirectory(byte[] ppt, uint offset, Dictionary<uint, uint> map)
    {
        if (offset == 0 || offset + RecordHeaderSize > (uint)ppt.Length)
        {
            return;
        }

        var (_, type, length, body) = ReadHeader(ppt, offset);
        if (type != RtPersistDirectoryAtom)
        {
            return;
        }

        var end = Math.Min(body + length, (uint)ppt.Length);
        var p = body;
        while (p + 4 <= end)
        {
            // Each run: 20-bit starting persist id, 12-bit count, then that many offsets.
            var info = ReadU32(ppt, p);
            p += 4;
            var startId = info & 0xFFFFF;
            var count = info >> 20;

            for (var i = 0u; i < count && p + 4 <= end; i++, p += 4)
            {
                map[startId + i] = ReadU32(ppt, p);
            }
        }
    }

    /// <summary>
    /// Returns slide text in presentation order, taken from the Document container's
    /// slide list. Text lives on the slide itself in newer saves and in the document's
    /// SlideListWithText in older ones, so both are collected and the slide wins.
    /// </summary>
    private static List<string> ReadSlidesInOrder(byte[] ppt, Dictionary<uint, uint> persist)
    {
        var order = new List<uint>();
        var centralText = new Dictionary<uint, List<string>>();

        foreach (var offset in persist.Values)
        {
            if (offset + RecordHeaderSize > (uint)ppt.Length)
            {
                continue;
            }

            var (_, type, length, body) = ReadHeader(ppt, offset);
            if (type != RtDocument)
            {
                continue;
            }

            CollectSlideList(ppt, body, Math.Min(body + length, (uint)ppt.Length), order, centralText);
        }

        var slides = new List<string>(order.Count);
        foreach (var reference in order)
        {
            var parts = new List<string>();

            if (persist.TryGetValue(reference, out var slideOffset) &&
                slideOffset + RecordHeaderSize <= (uint)ppt.Length)
            {
                var (_, type, length, body) = ReadHeader(ppt, slideOffset);
                if (type == RtSlide)
                {
                    CollectText(ppt, body, Math.Min(body + length, (uint)ppt.Length), parts);
                }
            }

            if (parts.Count == 0 && centralText.TryGetValue(reference, out var fallback))
            {
                parts = fallback;
            }

            slides.Add(string.Join("\n", parts).Trim());
        }

        return slides;
    }

    private static void CollectSlideList(
        byte[] ppt,
        uint body,
        uint end,
        List<uint> order,
        Dictionary<uint, List<string>> centralText)
    {
        foreach (var (_, instance, type, childBody, childEnd) in Children(ppt, body, end))
        {
            if (type != RtSlideListWithText || instance != SlideListInstanceSlides)
            {
                continue;
            }

            uint? current = null;
            foreach (var (_, _, innerType, innerBody, innerEnd) in Children(ppt, childBody, childEnd))
            {
                switch (innerType)
                {
                    case RtSlidePersistAtom:
                        // First field is the persist reference of the slide.
                        current = ReadU32(ppt, innerBody);
                        order.Add(current.Value);
                        centralText[current.Value] = [];
                        break;

                    case RtTextCharsAtom when current is not null:
                        centralText[current.Value].Add(DecodeUtf16(ppt, innerBody, innerEnd));
                        break;

                    case RtTextBytesAtom when current is not null:
                        centralText[current.Value].Add(DecodeAnsi(ppt, innerBody, innerEnd));
                        break;
                }
            }
        }
    }

    private static void CollectText(byte[] ppt, uint body, uint end, List<string> into)
    {
        foreach (var (version, _, type, childBody, childEnd) in Children(ppt, body, end))
        {
            switch (type)
            {
                case RtTextCharsAtom:
                    into.Add(DecodeUtf16(ppt, childBody, childEnd));
                    break;

                case RtTextBytesAtom:
                    into.Add(DecodeAnsi(ppt, childBody, childEnd));
                    break;

                default:
                    if (version == ContainerVersion)
                    {
                        CollectText(ppt, childBody, childEnd, into);
                    }
                    break;
            }
        }
    }

    /// <summary>Enumerates the immediate child records between <paramref name="body"/> and <paramref name="end"/>.</summary>
    private static IEnumerable<(int Version, int Instance, ushort Type, uint Body, uint End)> Children(
        byte[] data, uint body, uint end)
    {
        var offset = body;
        while (offset + RecordHeaderSize <= end)
        {
            var (versionInstance, type, length, childBody) = ReadHeader(data, offset);
            var childEnd = childBody + length;
            if (childEnd > end || childEnd < childBody)
            {
                yield break;   // truncated record - stop rather than read past the parent
            }

            yield return (versionInstance & 0xF, versionInstance >> 4, type, childBody, childEnd);
            offset = childEnd;
        }
    }

    private static (int VersionInstance, ushort Type, uint Length, uint Body) ReadHeader(byte[] data, uint offset)
    {
        var versionInstance = ReadU16(data, offset);
        var type = ReadU16(data, offset + 2);
        var length = ReadU32(data, offset + 4);
        return (versionInstance, type, length, offset + RecordHeaderSize);
    }

    private static string DecodeUtf16(byte[] data, uint body, uint end) =>
        Encoding.Unicode.GetString(data, (int)body, (int)(end - body));

    /// <summary>
    /// TextBytesAtom stores one byte per character, each being the low byte of a UTF-16
    /// code unit whose high byte is zero - which is exactly Latin-1, not CP1252. Romanian
    /// diacritics never fit in that range, so they arrive via TextCharsAtom instead.
    /// </summary>
    private static string DecodeAnsi(byte[] data, uint body, uint end) =>
        Encoding.Latin1.GetString(data, (int)body, (int)(end - body));

    private static ushort ReadU16(byte[] data, uint offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan((int)offset, 2));

    private static uint ReadU32(byte[] data, uint offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)offset, 4));
}
