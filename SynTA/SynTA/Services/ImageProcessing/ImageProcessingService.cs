using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace SynTA.Services.ImageProcessing;

/// <summary>
/// Implementation of image processing service using ImageSharp for compression and resizing.
/// Ensures images meet API provider size constraints (e.g., OpenAI 20MB limit, Gemini limits).
/// </summary>
public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    // Default limits for AI providers
    private const int DefaultMaxSizeBytes = 20 * 1024 * 1024; // 20MB (OpenAI limit)
    private const int DefaultQuality = 70;
    private const int MinQuality = 30;
    private const double ScaleFactor = 0.8; // Scale down by 20% each iteration

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    public bool IsWithinSizeLimit(byte[] imageBytes, int maxSizeBytes)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            return false;

        return imageBytes.Length <= maxSizeBytes;
    }

    public async Task<byte[]> ProcessImageForAIAsync(byte[] imageBytes, int maxSizeBytes, string? operationId = null)
    {
        operationId ??= Guid.NewGuid().ToString("N")[..8];

        if (imageBytes == null || imageBytes.Length == 0)
        {
            _logger.LogWarning("[{OperationId}] Cannot process null or empty image", operationId);
            throw new ArgumentException("Image bytes cannot be null or empty", nameof(imageBytes));
        }

        var originalSize = imageBytes.Length;
        _logger.LogInformation(
            "[{OperationId}] Processing image for AI - OriginalSize: {OriginalSize} bytes ({OriginalSizeMB:F2} MB), MaxSize: {MaxSize} bytes ({MaxSizeMB:F2} MB)",
            operationId, originalSize, originalSize / (1024.0 * 1024.0), maxSizeBytes, maxSizeBytes / (1024.0 * 1024.0));

        // If already within limits, return as-is
        if (IsWithinSizeLimit(imageBytes, maxSizeBytes))
        {
            _logger.LogInformation("[{OperationId}] Image is already within size limit, no processing needed", operationId);
            return imageBytes;
        }

        try
        {
            using var image = Image.Load(imageBytes);
            var originalWidth = image.Width;
            var originalHeight = image.Height;

            _logger.LogDebug(
                "[{OperationId}] Original image dimensions: {Width}x{Height}",
                operationId, originalWidth, originalHeight);

            byte[] processedBytes = imageBytes;
            int currentQuality = DefaultQuality;
            var currentWidth = originalWidth;
            var currentHeight = originalHeight;
            int iteration = 0;

            // Strategy: Try reducing quality first, then resize if needed
            while (processedBytes.Length > maxSizeBytes && iteration < 10)
            {
                iteration++;
                
                // Try reducing quality first
                if (currentQuality > MinQuality)
                {
                    currentQuality = Math.Max(MinQuality, currentQuality - 10);
                    _logger.LogDebug(
                        "[{OperationId}] Iteration {Iteration}: Reducing JPEG quality to {Quality}",
                        operationId, iteration, currentQuality);
                }
                else
                {
                    // Quality is at minimum, now resize
                    currentWidth = (int)(currentWidth * ScaleFactor);
                    currentHeight = (int)(currentHeight * ScaleFactor);

                    // Don't scale below reasonable minimum
                    if (currentWidth < 320 || currentHeight < 240)
                    {
                        _logger.LogWarning(
                            "[{OperationId}] Image dimensions would be too small ({Width}x{Height}), stopping compression",
                            operationId, currentWidth, currentHeight);
                        break;
                    }

                    _logger.LogDebug(
                        "[{OperationId}] Iteration {Iteration}: Resizing to {Width}x{Height}",
                        operationId, iteration, currentWidth, currentHeight);

                    image.Mutate(ctx => ctx.Resize(currentWidth, currentHeight));
                }

                // Re-encode with current settings
                using var memoryStream = new MemoryStream();
                var encoder = new JpegEncoder
                {
                    Quality = currentQuality
                };
                await image.SaveAsync(memoryStream, encoder);
                processedBytes = memoryStream.ToArray();

                _logger.LogDebug(
                    "[{OperationId}] Iteration {Iteration} result: Size {Size} bytes ({SizeMB:F2} MB), Quality: {Quality}, Dimensions: {Width}x{Height}",
                    operationId, iteration, processedBytes.Length, processedBytes.Length / (1024.0 * 1024.0),
                    currentQuality, currentWidth, currentHeight);
            }

            if (processedBytes.Length > maxSizeBytes)
            {
                _logger.LogWarning(
                    "[{OperationId}] Unable to compress image below size limit after {Iterations} iterations - FinalSize: {FinalSize} bytes ({FinalSizeMB:F2} MB), MaxSize: {MaxSize} bytes ({MaxSizeMB:F2} MB)",
                    operationId, iteration, processedBytes.Length, processedBytes.Length / (1024.0 * 1024.0),
                    maxSizeBytes, maxSizeBytes / (1024.0 * 1024.0));
                throw new InvalidOperationException(
                    $"Unable to compress image to required size. Final size: {processedBytes.Length / (1024.0 * 1024.0):F2} MB, Required: {maxSizeBytes / (1024.0 * 1024.0):F2} MB");
            }

            var reductionPercent = (1.0 - ((double)processedBytes.Length / originalSize)) * 100;
            _logger.LogInformation(
                "[{OperationId}] Successfully processed image - OriginalSize: {OriginalSize} bytes ({OriginalSizeMB:F2} MB), FinalSize: {FinalSize} bytes ({FinalSizeMB:F2} MB), Reduction: {Reduction:F1}%, FinalQuality: {Quality}, FinalDimensions: {Width}x{Height}",
                operationId, originalSize, originalSize / (1024.0 * 1024.0), 
                processedBytes.Length, processedBytes.Length / (1024.0 * 1024.0),
                reductionPercent, currentQuality, currentWidth, currentHeight);

            return processedBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{OperationId}] Failed to process image - Error: {ErrorMessage}",
                operationId, ex.Message);
            throw;
        }
    }
}
