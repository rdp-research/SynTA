using SynTA.Services.AI;

namespace SynTA.Tests.Services
{
    public class HtmlFetchOptionsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var options = new HtmlFetchOptions();

            // Assert
            Assert.Equal(30000, options.TimeoutMs);
            Assert.Null(options.WaitForSelector);
            Assert.Equal(1000, options.AdditionalWaitMs);
        }

        [Fact]
        public void CanSetCustomTimeout()
        {
            // Arrange & Act
            var options = new HtmlFetchOptions
            {
                TimeoutMs = 60000
            };

            // Assert
            Assert.Equal(60000, options.TimeoutMs);
        }

        [Fact]
        public void CanSetWaitForSelector()
        {
            // Arrange & Act
            var options = new HtmlFetchOptions
            {
                WaitForSelector = "#main-content"
            };

            // Assert
            Assert.Equal("#main-content", options.WaitForSelector);
        }

        [Fact]
        public void CanSetAdditionalWaitMs()
        {
            // Arrange & Act
            var options = new HtmlFetchOptions
            {
                AdditionalWaitMs = 2500
            };

            // Assert
            Assert.Equal(2500, options.AdditionalWaitMs);
        }

        [Fact]
        public void CanSetAllOptionsAtOnce()
        {
            // Arrange & Act
            var options = new HtmlFetchOptions
            {
                TimeoutMs = 45000,
                WaitForSelector = ".content-loaded",
                AdditionalWaitMs = 1500
            };

            // Assert
            Assert.Equal(45000, options.TimeoutMs);
            Assert.Equal(".content-loaded", options.WaitForSelector);
            Assert.Equal(1500, options.AdditionalWaitMs);
        }
    }
}
