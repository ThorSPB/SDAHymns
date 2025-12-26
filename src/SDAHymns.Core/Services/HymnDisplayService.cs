using Microsoft.EntityFrameworkCore;
using SDAHymns.Core.Data;
using SDAHymns.Core.Data.Models;

namespace SDAHymns.Core.Services;

public interface IHymnDisplayService
{
    Task<Hymn?> GetHymnByNumberAsync(int hymnNumber, string categorySlug);
    Task<List<Verse>> GetVersesForHymnAsync(int hymnId);
    Task UpdateAudioRecordingAsync(AudioRecording audioRecording);
}

public class HymnDisplayService : IHymnDisplayService
{
    private readonly HymnsContext _context;

    public HymnDisplayService(HymnsContext context)
    {
        _context = context;
    }

    public async Task<Hymn?> GetHymnByNumberAsync(int hymnNumber, string categorySlug)
    {
        return await _context.Hymns
            .Include(h => h.Category)
            .Include(h => h.Verses.OrderBy(v => v.DisplayOrder))
            .FirstOrDefaultAsync(h =>
                h.Number == hymnNumber &&
                h.Category.Slug == categorySlug);
    }

    public async Task<List<Verse>> GetVersesForHymnAsync(int hymnId)
    {
        return await _context.Verses
            .Where(v => v.HymnId == hymnId)
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync();
    }

    public async Task UpdateAudioRecordingAsync(AudioRecording audioRecording)
    {
        _context.AudioRecordings.Update(audioRecording);
        await _context.SaveChangesAsync();
    }
}
