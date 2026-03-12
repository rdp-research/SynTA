using Microsoft.Extensions.Logging;
using Moq;
using SynTA.Services.AI;
using Xunit;

namespace SynTA.Tests.Services.AI
{
    /// <summary>
    /// Tests for HtmlContentProcessor, focusing on the intelligent truncation and navigation preservation.
    /// </summary>
    public class HtmlContentProcessorTests
    {
        private readonly Mock<ILogger<HtmlContentProcessor>> _loggerMock;
        private readonly HtmlContentProcessor _processor;

        public HtmlContentProcessorTests()
        {
            _loggerMock = new Mock<ILogger<HtmlContentProcessor>>();
            _processor = new HtmlContentProcessor(_loggerMock.Object);
        }

        private RawWebContent CreateRawWebContent(string html, List<InteractiveElement>? elements = null)
        {
            return new RawWebContent
            {
                Html = html,
                InteractiveElements = elements ?? new List<InteractiveElement>(),
                AccessibilityTree = "",
                PageMetadata = new PageMetadata { Title = "Test Page" },
                OperationId = "test-op-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Url = "https://example.com"
            };
        }

        [Fact]
        public void ProcessHtmlContent_RemovesScriptAndStyleTags()
        {
            // Arrange
            var html = @"
                <html>
                <head>
                    <script>console.log('test');</script>
                    <style>body { color: red; }</style>
                </head>
                <body>
                    <main><p>Content</p></main>
                </body>
                </html>";

            var rawContent = CreateRawWebContent(html);

            // Act
            var result = _processor.ProcessHtmlContent(rawContent);

            // Assert
            Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<style", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Content", result);
        }

        [Fact]
        public void ProcessHtmlContent_PreservesDataTestIdAttributes()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <button data-testid=""submit-button"">Submit</button>
                    <input data-cy=""email-input"" type=""text"" />
                    <a data-test=""home-link"" href=""/"">Home</a>
                </body>
                </html>";

            var rawContent = CreateRawWebContent(html);

            // Act
            var result = _processor.ProcessHtmlContent(rawContent);

            // Assert
            Assert.Contains("data-testid", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("submit-button", result);
            Assert.Contains("data-cy", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-test", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ProcessHtmlContent_PreservesSynTAVisibleAttributes()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <button data-synta-visible=""true"">Visible Button</button>
                    <div data-synta-visible=""false"">Hidden Content</div>
                </body>
                </html>";

            var rawContent = CreateRawWebContent(html);

            // Act
            var result = _processor.ProcessHtmlContent(rawContent);

            // Assert
            Assert.Contains("data-synta-visible", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ProcessHtmlContent_RemovesFrameworkSpecificAttributes()
        {
            // Arrange
            var html = @"
                <html>
                <body>
                    <div data-v-12345678="""" class=""vue-component"">Vue content</div>
                    <span _ngcontent-abc123="""">Angular content</span>
                </body>
                </html>";

            var rawContent = CreateRawWebContent(html);

            // Act
            var result = _processor.ProcessHtmlContent(rawContent);

            // Assert
            Assert.DoesNotContain("data-v-", result);
            Assert.DoesNotContain("_ngcontent-", result);
            Assert.Contains("Vue content", result);
            Assert.Contains("Angular content", result);
        }

        [Fact]
        public void ProcessHtmlContent_CollapsesRepetitiveLists()
        {
            // Arrange
            var items = string.Join("", Enumerable.Range(1, 15).Select(i => $"<li>Item {i}</li>"));
            var html = $"<html><body><ul>{items}</ul></body></html>";

            var rawContent = CreateRawWebContent(html);

            // Act
            var result = _processor.ProcessHtmlContent(rawContent);

            // Assert
            // Should contain collapse indicator
            Assert.Contains("collapsed", result, StringComparison.OrdinalIgnoreCase);
            // Should preserve first few items
            Assert.Contains("Item 1", result);
        }

        [Fact]
        public void ProcessHtmlContent_IncludesInteractiveElementMap()
        {
            // Arrange
            var html = "<html><body><button>Click me</button></body></html>";
            var elements = new List<InteractiveElement>
            {
                new InteractiveElement
                {
                    Tag = "button",
                    Text = "Click me",
                    IsVisible = true,
                    RecommendedSelector = "cy.get(\"button\")",
                    SemanticRegion = "body"
                }
            };

            var rawContent = CreateRawWebContent(html, elements);

            // Act
            var result = _processor.ProcessHtmlContent(rawContent);

            // Assert
            Assert.Contains("UI ELEMENT MAP", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("button", result.ToLower());
        }

        [Fact]
        public void ProcessHtmlContent_IncludesPageMetadata()
        {
            // Arrange
            var html = "<html><body><main>Content</main></body></html>";
            var rawContent = new RawWebContent
            {
                Html = html,
                InteractiveElements = new List<InteractiveElement>(),
                AccessibilityTree = "",
                PageMetadata = new PageMetadata 
                { 
                    Title = "My Test Page",
                    H1Text = "Welcome Heading",
                    MetaDescription = "This is a test page"
                },
                OperationId = "test-metadata",
                Url = "https://example.com"
            };

            // Act
            var result = _processor.ProcessHtmlContent(rawContent);

            // Assert
            Assert.Contains("My Test Page", result);
            Assert.Contains("PAGE METADATA", result);
        }
    }
}
