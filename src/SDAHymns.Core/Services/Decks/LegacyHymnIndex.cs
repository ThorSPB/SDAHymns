using System.Xml.Linq;

namespace SDAHymns.Core.Services.Decks;

/// <summary>
/// The number-to-title map a category ships in <c>index.xml</c>.
/// </summary>
/// <remarks>
/// Authoritative where it exists, and it usually does - but "Imnuri crestine" lists only
/// 735 of its 920 hymns. Everything from 736 up was added to the folder without the index
/// being updated, so those decks have to name themselves.
/// </remarks>
public sealed class LegacyHymnIndex
{
    private readonly IReadOnlyDictionary<int, string> _titles;

    private LegacyHymnIndex(IReadOnlyDictionary<int, string> titles) => _titles = titles;

    public static LegacyHymnIndex Empty { get; } = new(new Dictionary<int, string>());

    public int Count => _titles.Count;

    /// <summary>Loads a category's index, returning <see cref="Empty"/> when there isn't one.</summary>
    public static LegacyHymnIndex Load(string categoryPath)
    {
        var path = Path.Combine(categoryPath, "index.xml");
        if (!File.Exists(path))
        {
            return Empty;
        }

        try
        {
            var titles = new Dictionary<int, string>();
            foreach (var entry in XDocument.Load(path).Descendants("Imn"))
            {
                if (int.TryParse(entry.Element("Numar")?.Value, out var number))
                {
                    var title = HymnTextNormalizer.Normalize(entry.Element("Titlu")?.Value ?? "");
                    if (title.Length > 0)
                    {
                        titles[number] = title;
                    }
                }
            }

            return new LegacyHymnIndex(titles);
        }
        catch (Exception)
        {
            // A malformed index must not stop the import; the decks can still name themselves.
            return Empty;
        }
    }

    public bool TryGetTitle(int number, out string title) => _titles.TryGetValue(number, out title!);
}
