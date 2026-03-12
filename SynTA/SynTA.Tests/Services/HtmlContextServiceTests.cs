using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SynTA.Tests.Services
{
    // NOTE: These tests are temporarily disabled after refactoring HtmlContextService into WebScraperService and HtmlContentProcessor.
    // The tests need to be rewritten to test the separated services individually.
    // TODO: Create separate test files for WebScraperService and HtmlContentProcessor
    public class HtmlContextServiceTests
    {
        /*
        [Fact]
        public void SimplifyHtml_RemovesScriptsAndCollapsesLists()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<SynTA.Services.AI.HtmlContextService>>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var service = new SynTA.Services.AI.HtmlContextService(loggerMock.Object, serviceProviderMock.Object);

            var items = string.Join("", Enumerable.Range(1, 10).Select(i => $"<li>Item {i}</li>"));
            var html = $"<html><head><script>console.log('x')</script><style>body{{color:red}}</style></head><body><ul>{items}</ul></body></html>";

            // Use reflection to call the non-public SimplifyHtml method
            var serviceType = typeof(SynTA.Services.AI.HtmlContextService);
            var method = serviceType.GetMethod("SimplifyHtml", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            // Build an empty interactive elements list using the internal InteractiveElement type
            var interactiveType = serviceType.Assembly.GetTypes().First(t => t.Name == "InteractiveElement");
            var listType = typeof(List<>).MakeGenericType(interactiveType);
            var interactiveList = (System.Collections.IList)Activator.CreateInstance(listType)!;

            // Act
            var result = (string)method.Invoke(service, new object[] { html, interactiveList, string.Empty, null! })!;

            // Assert
            Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<style", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<!-- ... [5] items collapsed ... -->", result);
        }
        */

        /*
        [Fact]
        public void SimplifyHtml_PreservesDomStructureWithNestedElements()
        {
            // Arrange - Test case for issue #70: nested HTML structures should remain valid
            var loggerMock = new Mock<ILogger<SynTA.Services.AI.HtmlContextService>>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var service = new SynTA.Services.AI.HtmlContextService(loggerMock.Object, serviceProviderMock.Object);

            // Complex nested HTML with various attributes that should be preserved or removed
            var html = @"
                <html>
                <head>
                    <script>console.log('test')</script>
                    <style>body { color: red; }</style>
                </head>
                <body>
                    <header data-synta-visible='true'>
                        <nav>
                            <div class='menu css-abc123 sc-xyz789'>
                                <a href='/home' data-testid='home-link'>Home</a>
                                <a href='/about' class='link makeStyles-root-xyz'>About</a>
                            </div>
                        </nav>
                    </header>
                    <main data-synta-visible='true'>
                        <article>
                            <div class='content' style='color: blue;' data-v-12345678='' _ngcontent-c12=''>
                                <h1>Title</h1>
                                <p>Nested <strong>content</strong> with <em>multiple</em> levels</p>
                                <div class='nested'>
                                    <div class='deeper'>
                                        <span>Deep content</span>
                                    </div>
                                </div>
                            </div>
                        </article>
                    </main>
                    <footer>Footer</footer>
                </body>
                </html>";

            // Use reflection to call the non-public SimplifyHtml method
            var serviceType = typeof(SynTA.Services.AI.HtmlContextService);
            var method = serviceType.GetMethod("SimplifyHtml", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var interactiveType = serviceType.Assembly.GetTypes().First(t => t.Name == "InteractiveElement");
            var listType = typeof(List<>).MakeGenericType(interactiveType);
            var interactiveList = (System.Collections.IList)Activator.CreateInstance(listType)!;

            // Act
            var result = (string)method.Invoke(service, new object[] { html, interactiveList, string.Empty, null! })!;

            // Assert - Verify DOM structure is preserved
            Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<style", result, StringComparison.OrdinalIgnoreCase);
            
            // Verify nested structure is intact
            Assert.Contains("<header", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<main", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<article", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("<h1>Title</h1>", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Deep content", result);
            
            // Verify inline styles are removed
            Assert.DoesNotContain("style=", result, StringComparison.OrdinalIgnoreCase);
            
            // Verify hashed classes are removed
            Assert.DoesNotContain("css-abc123", result);
            Assert.DoesNotContain("sc-xyz789", result);
            Assert.DoesNotContain("makeStyles-root-xyz", result);
            
            // Verify framework-specific attributes are removed
            Assert.DoesNotContain("data-v-", result);
            Assert.DoesNotContain("_ngcontent-", result);
            
            // Verify semantic classes and test IDs are preserved
            Assert.Contains("data-testid", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("home-link", result);
        }
        */

        /*
        [Fact]
        public void SimplifyHtml_HandlesComplexNestedDivsWithoutCorruption()
        {
            // Arrange - Test deeply nested divs that could break with regex
            var loggerMock = new Mock<ILogger<SynTA.Services.AI.HtmlContextService>>();
            var serviceProviderMock = new Mock<IServiceProvider>();
            var service = new SynTA.Services.AI.HtmlContextService(loggerMock.Object, serviceProviderMock.Object);

            var html = @"
                <div class='outer'>
                    <div class='level1' data-v-abc123=''>
                        <div class='level2' style='margin: 10px;'>
                            <div class='level3 css-xyz123'>
                                <div class='level4'>
                                    <span>Content</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>";

            var serviceType = typeof(SynTA.Services.AI.HtmlContextService);
            var method = serviceType.GetMethod("SimplifyHtml", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);

            var interactiveType = serviceType.Assembly.GetTypes().First(t => t.Name == "InteractiveElement");
            var listType = typeof(List<>).MakeGenericType(interactiveType);
            var interactiveList = (System.Collections.IList)Activator.CreateInstance(listType)!;

            // Act
            var result = (string)method.Invoke(service, new object[] { html, interactiveList, string.Empty, null! })!;

            // Assert - Count opening and closing div tags
            var openDivCount = System.Text.RegularExpressions.Regex.Matches(result, @"<div", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
            var closeDivCount = System.Text.RegularExpressions.Regex.Matches(result, @"</div>", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
            
            // All opening divs should have matching closing tags (DOM structure preserved)
            Assert.Equal(openDivCount, closeDivCount);
            
            // Content should still be present
            Assert.Contains("Content", result);
            
            // Verify cleaned attributes
            Assert.DoesNotContain("style=", result, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("data-v-", result);
            Assert.DoesNotContain("css-xyz123", result);
        }
        */
    }
}
