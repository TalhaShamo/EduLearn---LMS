using EduLearn.Course.API.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EduLearn.Course.API.Infrastructure.Services;

// Local video storage service — File I/O concept implementation
// Saves instructor-uploaded videos to wwwroot/videos/{courseId}/{lessonId}/
public class LocalVideoStorageService : IVideoStorageService
{
    private readonly string _basePath;       // Absolute path to wwwroot/videos
    private readonly ILogger<LocalVideoStorageService> _logger;

    public LocalVideoStorageService(IWebHostEnvironment env, ILogger<LocalVideoStorageService> logger)
    {
        // Resolve the absolute path to wwwroot/videos/
        _basePath = Path.Combine(env.WebRootPath, "videos");
        _logger   = logger;

        // Ensure the root video directory exists
        Directory.CreateDirectory(_basePath);
    }

    // Save video file using async File I/O
    public async Task<string> SaveVideoAsync(Guid courseId, Guid lessonId, Stream fileStream, string fileName)
    {
        // Build directory path: wwwroot/videos/{courseId}/{lessonId}/
        var dir = Path.Combine(_basePath, courseId.ToString(), lessonId.ToString());
        Directory.CreateDirectory(dir);   // File I/O: create directories if needed

        // Sanitise file name and build full path
        var safeFileName = Path.GetFileName(fileName);  // Strip directory traversal
        var ext          = Path.GetExtension(safeFileName).ToLowerInvariant();

        // Only allow video file types
        if (ext is not (".mp4" or ".mov" or ".avi"))
            throw new InvalidOperationException($"File type '{ext}' is not allowed. Use MP4, MOV, or AVI.");

        var filePath = Path.Combine(dir, $"video{ext}");

        _logger.LogInformation("Saving video to {Path}", filePath);

        // Write stream to file — async File I/O
        await using var fileOutput = File.Create(filePath);
        await fileStream.CopyToAsync(fileOutput);

        // Return relative path (stored in DB, not the full absolute path)
        return Path.Combine(courseId.ToString(), lessonId.ToString(), $"video{ext}")
                   .Replace("\\", "/");  // Normalise for URL use
    }

    // Delete a video file when a lesson is deleted
    public void DeleteVideo(string relativePath)
    {
        var fullPath = Path.Combine(_basePath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);   // File I/O: delete
            _logger.LogInformation("Deleted video at {Path}", fullPath);
        }
    }

    public string GetVideoPath(string relativePath) =>
        Path.Combine(_basePath, relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
}
