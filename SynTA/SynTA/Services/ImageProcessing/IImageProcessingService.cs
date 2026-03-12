namespace SynTA.Services.ImageProcessing;

/// <summary>
/// Service for processing images to ensure they meet API provider constraints.
/// Handles validation, compression, and downsampling of images.
/// </summary>
public interface IImageProcessingService
{
    /// <summary>
    /// Validates and processes an image to ensure it meets size constraints.
    /// If the image exceeds the maximum size, it will be compressed or downsampled.
    /// </summary>
    /// <param name="imageBytes">The original image bytes</param>
    /// <param name="maxSizeBytes">Maximum allowed size in bytes</param>
    /// <param name="operationId">Optional operation ID for logging correlation</param>
    /// <returns>Processed image bytes that meet size constraints</returns>
    Task<byte[]> ProcessImageForAIAsync(byte[] imageBytes, int maxSizeBytes, string? operationId = null);

    /// <summary>
    /// Checks if an image exceeds the size limit.
    /// </summary>
    /// <param name="imageBytes">The image bytes to check</param>
    /// <param name="maxSizeBytes">Maximum allowed size in bytes</param>
    /// <returns>True if the image is within limits, false otherwise</returns>
    bool IsWithinSizeLimit(byte[] imageBytes, int maxSizeBytes);
}
