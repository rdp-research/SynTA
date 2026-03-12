using Microsoft.Extensions.Logging;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace SynTA.Tests.Services.ImageProcessing;

public class ImageProcessingServiceTests
{
    [Fact]
    public void IsWithinSizeLimit_ReturnsTrueForSmallImage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SynTA.Services.ImageProcessing.ImageProcessingService>>();
        var service = new SynTA.Services.ImageProcessing.ImageProcessingService(loggerMock.Object);
        var imageBytes = new byte[1024 * 1024]; // 1MB

        // Act
        var result = service.IsWithinSizeLimit(imageBytes, 5 * 1024 * 1024); // 5MB limit

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsWithinSizeLimit_ReturnsFalseForLargeImage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SynTA.Services.ImageProcessing.ImageProcessingService>>();
        var service = new SynTA.Services.ImageProcessing.ImageProcessingService(loggerMock.Object);
        var imageBytes = new byte[10 * 1024 * 1024]; // 10MB

        // Act
        var result = service.IsWithinSizeLimit(imageBytes, 5 * 1024 * 1024); // 5MB limit

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinSizeLimit_ReturnsFalseForNullImage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SynTA.Services.ImageProcessing.ImageProcessingService>>();
        var service = new SynTA.Services.ImageProcessing.ImageProcessingService(loggerMock.Object);

        // Act
        var result = service.IsWithinSizeLimit(null!, 5 * 1024 * 1024);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsWithinSizeLimit_ReturnsFalseForEmptyImage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SynTA.Services.ImageProcessing.ImageProcessingService>>();
        var service = new SynTA.Services.ImageProcessing.ImageProcessingService(loggerMock.Object);
        var imageBytes = Array.Empty<byte>();

        // Act
        var result = service.IsWithinSizeLimit(imageBytes, 5 * 1024 * 1024);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessImageForAIAsync_ThrowsForNullImage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SynTA.Services.ImageProcessing.ImageProcessingService>>();
        var service = new SynTA.Services.ImageProcessing.ImageProcessingService(loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ProcessImageForAIAsync(null!, 5 * 1024 * 1024));
    }

    [Fact]
    public async Task ProcessImageForAIAsync_ThrowsForEmptyImage()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SynTA.Services.ImageProcessing.ImageProcessingService>>();
        var service = new SynTA.Services.ImageProcessing.ImageProcessingService(loggerMock.Object);
        var imageBytes = Array.Empty<byte>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.ProcessImageForAIAsync(imageBytes, 5 * 1024 * 1024));
    }

    [Fact]
    public async Task ProcessImageForAIAsync_ReturnsUnmodifiedImageWhenWithinLimit()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SynTA.Services.ImageProcessing.ImageProcessingService>>();
        var service = new SynTA.Services.ImageProcessing.ImageProcessingService(loggerMock.Object);
        
        // Create a small test JPEG image (100x100 white image)
        var imageBytes = CreateTestJpegImage(100, 100);
        var originalLength = imageBytes.Length;

        // Act
        var result = await service.ProcessImageForAIAsync(imageBytes, 20 * 1024 * 1024); // 20MB limit

        // Assert
        Assert.NotNull(result);
        Assert.Equal(originalLength, result.Length);
        Assert.Equal(imageBytes, result);
    }

    private static byte[] CreateTestJpegImage(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height);
        
        // Fill with white color
        image.Mutate(ctx => ctx.BackgroundColor(Color.White));
        
        using var memoryStream = new MemoryStream();
        var encoder = new JpegEncoder
        {
            Quality = 90
        };
        image.SaveAsJpeg(memoryStream, encoder);
        return memoryStream.ToArray();
    }
}
