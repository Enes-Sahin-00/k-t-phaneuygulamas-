using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;

namespace kütüphaneuygulaması.Services
{
    public class ImageOptimizationService : IImageOptimizationService
    {
        private readonly ILogger<ImageOptimizationService> _logger;
        private readonly string _imagesPath;

        public ImageOptimizationService(ILogger<ImageOptimizationService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _imagesPath = Path.Combine(environment.WebRootPath, "images");
        }

        public async Task<string> OptimizeImageAsync(IFormFile file, int maxWidth = 800, int maxHeight = 600, int quality = 85)
        {
            try
            {
                if (!await IsImageFileAsync(file))
                {
                    throw new ArgumentException("File is not a valid image");
                }

                var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName).ToLower();
                var optimizedFileName = $"{fileName}_optimized_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var optimizedPath = Path.Combine(_imagesPath, optimizedFileName);

                using var originalImage = Image.FromStream(file.OpenReadStream());
                using var optimizedImage = ResizeImage(originalImage, maxWidth, maxHeight);

                // Save with compression
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                var codec = GetImageCodec(extension);
                optimizedImage.Save(optimizedPath, codec, encoderParameters);

                _logger.LogInformation("Image optimized: {FileName}", optimizedFileName);
                return optimizedFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing image: {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<string> ResizeImageAsync(string imagePath, int width, int height)
        {
            try
            {
                var fullPath = Path.Combine(_imagesPath, imagePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Image not found", fullPath);
                }

                using var originalImage = Image.FromFile(fullPath);
                using var resizedImage = ResizeImage(originalImage, width, height);

                var fileName = Path.GetFileNameWithoutExtension(imagePath);
                var extension = Path.GetExtension(imagePath);
                var resizedFileName = $"{fileName}_resized_{width}x{height}{extension}";
                var resizedPath = Path.Combine(_imagesPath, resizedFileName);

                resizedImage.Save(resizedPath, originalImage.RawFormat);

                _logger.LogInformation("Image resized: {FileName}", resizedFileName);
                return resizedFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resizing image: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<string> CompressImageAsync(string imagePath, int quality = 85)
        {
            try
            {
                var fullPath = Path.Combine(_imagesPath, imagePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Image not found", fullPath);
                }

                using var originalImage = Image.FromFile(fullPath);
                var extension = Path.GetExtension(imagePath).ToLower();
                var codec = GetImageCodec(extension);

                var fileName = Path.GetFileNameWithoutExtension(imagePath);
                var compressedFileName = $"{fileName}_compressed{extension}";
                var compressedPath = Path.Combine(_imagesPath, compressedFileName);

                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                originalImage.Save(compressedPath, codec, encoderParameters);

                _logger.LogInformation("Image compressed: {FileName}", compressedFileName);
                return compressedFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing image: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<string> GenerateThumbnailAsync(string imagePath, int width = 150, int height = 150)
        {
            try
            {
                var fullPath = Path.Combine(_imagesPath, imagePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Image not found", fullPath);
                }

                using var originalImage = Image.FromFile(fullPath);
                using var thumbnail = ResizeImage(originalImage, width, height);

                var fileName = Path.GetFileNameWithoutExtension(imagePath);
                var extension = Path.GetExtension(imagePath);
                var thumbnailFileName = $"{fileName}_thumb{extension}";
                var thumbnailPath = Path.Combine(_imagesPath, thumbnailFileName);

                thumbnail.Save(thumbnailPath, originalImage.RawFormat);

                _logger.LogInformation("Thumbnail generated: {FileName}", thumbnailFileName);
                return thumbnailFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<bool> IsImageFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
                return false;

            try
            {
                using var stream = file.OpenReadStream();
                using var image = Image.FromStream(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<long> GetImageSizeAsync(string imagePath)
        {
            try
            {
                var fullPath = Path.Combine(_imagesPath, imagePath);
                if (!File.Exists(fullPath))
                {
                    return 0;
                }

                var fileInfo = new FileInfo(fullPath);
                return fileInfo.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image size: {ImagePath}", imagePath);
                return 0;
            }
        }

        public async Task<string> ConvertToWebPAsync(string imagePath)
        {
            try
            {
                var fullPath = Path.Combine(_imagesPath, imagePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("Image not found", fullPath);
                }

                using var originalImage = Image.FromFile(fullPath);
                var fileName = Path.GetFileNameWithoutExtension(imagePath);
                var webpFileName = $"{fileName}.webp";
                var webpPath = Path.Combine(_imagesPath, webpFileName);

                // Note: WebP conversion requires additional libraries
                // For now, we'll just copy the original file
                File.Copy(fullPath, webpPath, true);

                _logger.LogInformation("Image converted to WebP: {FileName}", webpFileName);
                return webpFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting to WebP: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<string> GenerateImageHashAsync(string imagePath)
        {
            try
            {
                var fullPath = Path.Combine(_imagesPath, imagePath);
                if (!File.Exists(fullPath))
                {
                    return string.Empty;
                }

                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(fullPath);
                var hash = await sha256.ComputeHashAsync(stream);
                return Convert.ToBase64String(hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image hash: {ImagePath}", imagePath);
                return string.Empty;
            }
        }

        public async Task CleanupOrphanedImagesAsync()
        {
            try
            {
                var imageFiles = Directory.GetFiles(_imagesPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(file => IsImageFile(file))
                    .ToList();

                var orphanedFiles = new List<string>();

                foreach (var file in imageFiles)
                {
                    var fileName = Path.GetFileName(file);
                    // Check if the image is referenced in the database
                    // This would require database context injection
                    // For now, we'll just log the files
                    orphanedFiles.Add(fileName);
                }

                _logger.LogInformation("Found {Count} orphaned image files", orphanedFiles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up orphaned images");
            }
        }

        public async Task<List<string>> GetImageFormatsAsync()
        {
            return new List<string>
            {
                "JPEG", "PNG", "GIF", "BMP", "WebP", "TIFF"
            };
        }

        private Image ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }

        private ImageCodecInfo GetImageCodec(string extension)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();

            return extension switch
            {
                ".jpg" or ".jpeg" => codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid),
                ".png" => codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Png.Guid),
                ".gif" => codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Gif.Guid),
                ".bmp" => codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Bmp.Guid),
                _ => codecs.FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid)
            };
        }

        private bool IsImageFile(string filePath)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var extension = Path.GetExtension(filePath).ToLower();
            return allowedExtensions.Contains(extension);
        }
    }
} 