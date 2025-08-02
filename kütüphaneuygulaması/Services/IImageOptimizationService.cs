namespace kütüphaneuygulaması.Services
{
    public interface IImageOptimizationService
    {
        Task<string> OptimizeImageAsync(IFormFile file, int maxWidth = 800, int maxHeight = 600, int quality = 85);
        Task<string> ResizeImageAsync(string imagePath, int width, int height);
        Task<string> CompressImageAsync(string imagePath, int quality = 85);
        Task<string> GenerateThumbnailAsync(string imagePath, int width = 150, int height = 150);
        Task<bool> IsImageFileAsync(IFormFile file);
        Task<long> GetImageSizeAsync(string imagePath);
        Task<string> ConvertToWebPAsync(string imagePath);
        Task<string> GenerateImageHashAsync(string imagePath);
        Task CleanupOrphanedImagesAsync();
        Task<List<string>> GetImageFormatsAsync();
    }
} 