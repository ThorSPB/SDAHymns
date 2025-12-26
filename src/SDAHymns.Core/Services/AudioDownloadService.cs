using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SDAHymns.Core.Models;

namespace SDAHymns.Core.Services;

/// <summary>
/// Service for downloading and managing audio packages
/// </summary>
public class AudioDownloadService : IAudioDownloadService
{
    private readonly ISettingsService _settingsService;
    private readonly IAudioLibraryService _libraryService;
    private readonly HttpClient _httpClient;

    private readonly string[] _manifestSources = new[]
    {
        "https://github.com/ThorSPB/SDAHymns/releases/download/audio-v{version}/manifest.json",
        "http://sdahymns.duckdns.org:8080/audio/manifest.json"  // Fallback
    };

    public AudioDownloadService(ISettingsService settingsService, IAudioLibraryService libraryService, HttpClient httpClient)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<AudioPackageManifest> FetchManifestAsync(CancellationToken cancellationToken = default)
    {
        // Try each source until one works
        foreach (var sourceTemplate in _manifestSources)
        {
            try
            {
                // For now, use "latest" or "1.0.0" as version
                var source = sourceTemplate.Replace("{version}", "1.0.0");
                var json = await _httpClient.GetStringAsync(source, cancellationToken);
                var manifest = JsonSerializer.Deserialize<AudioPackageManifest>(json);

                if (manifest != null)
                {
                    return manifest;
                }
            }
            catch
            {
                // Try next source
                continue;
            }
        }

        throw new InvalidOperationException("Failed to fetch manifest from all sources");
    }

    public async Task<List<CategoryPackage>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await FetchManifestAsync(cancellationToken);
        return manifest.Categories.Select(kvp =>
        {
            var package = kvp.Value;
            return package;
        }).ToList();
    }

    public async Task<List<InstalledPackage>> GetInstalledPackagesAsync()
    {
        var installedCategories = await _libraryService.GetInstalledCategoriesAsync();
        var installedPackages = new List<InstalledPackage>();

        foreach (var category in installedCategories)
        {
            var files = await _libraryService.GetInstalledFilesAsync(category);
            var size = await _libraryService.GetCategorySizeAsync(category);

            installedPackages.Add(new InstalledPackage
            {
                Category = category,
                DisplayName = GetCategoryDisplayName(category),
                FileCount = files.Count,
                TotalSize = size,
                InstalledDate = files.Any() ? files.Min(f => f.LastModified) : DateTime.MinValue,
                Version = "1.0.0",  // TODO: Track version
                IsVerified = true  // TODO: Implement verification
            });
        }

        return installedPackages;
    }

    public async Task DownloadCategoryAsync(string categorySlug, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var downloadProgress = new DownloadProgress { State = DownloadState.FetchingManifest };
        progress?.Report(downloadProgress);

        // Fetch manifest
        var manifest = await FetchManifestAsync(cancellationToken);

        if (!manifest.Categories.TryGetValue(categorySlug, out var package))
        {
            throw new ArgumentException($"Category '{categorySlug}' not found in manifest", nameof(categorySlug));
        }

        // Download file
        downloadProgress.State = DownloadState.Downloading;
        downloadProgress.CurrentFile = $"{categorySlug}-pack.zip";
        progress?.Report(downloadProgress);

        var tempZipPath = Path.GetTempFileName();

        try
        {
            // Try primary download URL
            await DownloadFileAsync(package.DownloadUrl, tempZipPath, downloadProgress, progress, cancellationToken);
        }
        catch when (!string.IsNullOrEmpty(package.FallbackUrl))
        {
            // Try fallback URL
            await DownloadFileAsync(package.FallbackUrl, tempZipPath, downloadProgress, progress, cancellationToken);
        }

        // Verify checksum
        downloadProgress.State = DownloadState.Verifying;
        progress?.Report(downloadProgress);

        var actualChecksum = await ComputeChecksumAsync(tempZipPath);
        if (!actualChecksum.Equals(package.Checksum, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(tempZipPath);
            throw new InvalidOperationException($"Checksum mismatch for {categorySlug}. Expected: {package.Checksum}, Got: {actualChecksum}");
        }

        // Extract
        downloadProgress.State = DownloadState.Extracting;
        progress?.Report(downloadProgress);

        var libraryPath = await _settingsService.GetAudioLibraryPathAsync();
        var categoryPath = Path.Combine(libraryPath, categorySlug);

        if (!Directory.Exists(categoryPath))
        {
            Directory.CreateDirectory(categoryPath);
        }

        ZipFile.ExtractToDirectory(tempZipPath, categoryPath, overwriteFiles: true);

        // Cleanup
        File.Delete(tempZipPath);

        downloadProgress.State = DownloadState.Complete;
        downloadProgress.BytesDownloaded = downloadProgress.TotalBytes;  // Mark as 100%
        progress?.Report(downloadProgress);
    }

    public async Task DownloadAllAsync(IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var packages = await GetAvailablePackagesAsync(cancellationToken);

        foreach (var package in packages)
        {
            // Find the category slug (key in manifest.Categories)
            var manifest = await FetchManifestAsync(cancellationToken);
            var categorySlug = manifest.Categories.FirstOrDefault(kvp => kvp.Value == package).Key;

            if (!string.IsNullOrEmpty(categorySlug))
            {
                await DownloadCategoryAsync(categorySlug, progress, cancellationToken);
            }
        }
    }

    public async Task UpdateCategoryAsync(string categorySlug, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        // For now, update is the same as download (overwrites existing)
        await DownloadCategoryAsync(categorySlug, progress, cancellationToken);
    }

    public async Task<bool> VerifyCategoryAsync(string categorySlug, IProgress<int>? progress = null)
    {
        var files = await _libraryService.GetInstalledFilesAsync(categorySlug);
        var totalFiles = files.Count;
        var verifiedFiles = 0;

        foreach (var file in files)
        {
            if (File.Exists(file.FilePath))
            {
                verifiedFiles++;
            }

            progress?.Report((verifiedFiles * 100) / totalFiles);
        }

        return verifiedFiles == totalFiles;
    }

    private async Task DownloadFileAsync(string url, string destinationPath, DownloadProgress downloadProgress, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        downloadProgress.TotalBytes = totalBytes;

        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

        var buffer = new byte[8192];
        long totalBytesRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;

            downloadProgress.BytesDownloaded = totalBytesRead;
            progress?.Report(downloadProgress);
        }
    }

    private static async Task<string> ComputeChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string GetCategoryDisplayName(string categorySlug)
    {
        return categorySlug switch
        {
            "crestine" => "Imnuri Crestine",
            "companioni" => "Imnuri Companioni",
            "exploratori" => "Imnuri Exploratori",
            "licurici" => "Imnuri Licurici",
            "tineret" => "Imnuri Tineret",
            _ => categorySlug
        };
    }
}
