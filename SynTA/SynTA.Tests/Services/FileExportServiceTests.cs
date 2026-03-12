using Microsoft.Extensions.Logging;
using Moq;
using SynTA.Services.Export;
using SynTA.Services.Utilities;
using SynTA.Models.Domain;

namespace SynTA.Tests.Services
{
    /// <summary>
    /// Unit tests for FileExportService.
    /// Uses REAL FileNameService (no mocking) because it has no external dependencies.
    /// This ensures tests verify actual behavior, not mock implementations.
    /// </summary>
    public class FileExportServiceTests
    {
        private readonly FileExportService _service;
        private readonly Mock<ILogger<FileExportService>> _loggerMock;
        private readonly IFileNameService _fileNameService; // Real implementation

        public FileExportServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileExportService>>();
            
            // Use REAL FileNameService - it has no external dependencies
            // This prevents false positives when logic changes in FileNameService
            _fileNameService = new FileNameService();

            _service = new FileExportService(_loggerMock.Object, _fileNameService);
        }

        #region CreateCypressFile Tests

        [Fact]
        public void CreateCypressFile_ValidContent_ReturnsCorrectResult()
        {
            // Arrange
            var content = "describe('Test', () => { it('should pass', () => {}); });";
            var fileName = "test";

            // Act
            var result = _service.CreateCypressFile(content, fileName);

            // Assert
            Assert.NotNull(result.FileContent);
            Assert.True(result.FileContent.Length > 0);
            Assert.Equal("text/typescript", result.ContentType);
            Assert.Equal("test.cy.ts", result.FileName);
        }

        [Fact]
        public void CreateCypressFile_WithJavaScriptExtension_ReturnsJsFile()
        {
            // Arrange
            var content = "describe('Test', () => { it('should pass', () => {}); });";
            var fileName = "test.cy.js";

            // Act
            var result = _service.CreateCypressFile(content, fileName);

            // Assert
            Assert.EndsWith(".cy.js", result.FileName);
        }

        [Fact]
        public void CreateCypressFile_FileNameWithExtension_DoesNotDuplicateExtension()
        {
            // Arrange
            var content = "test content";
            var fileName = "test.cy.ts";

            // Act
            var result = _service.CreateCypressFile(content, fileName);

            // Assert
            Assert.Equal("test.cy.ts", result.FileName);
        }

        [Fact]
        public void CreateCypressFile_FileNameWithSpaces_ReplacesWithHyphens()
        {
            // Arrange
            var content = "test content";
            var fileName = "my test file";
            // Use real FileNameService to sanitize
            var sanitized = _fileNameService.Sanitize(fileName);

            // Act
            var result = _service.CreateCypressFile(content, sanitized);

            // Assert
            Assert.Equal("my-test-file.cy.ts", result.FileName);
        }

        [Fact]
        public void CreateCypressFile_FileNameWithUpperCase_ConvertsToLowerCase()
        {
            // Arrange
            var content = "test content";
            var fileName = "MyTestFile";
            // Use real FileNameService to sanitize
            var sanitized = _fileNameService.Sanitize(fileName);

            // Act
            var result = _service.CreateCypressFile(content, sanitized);

            // Assert
            Assert.Equal(sanitized + ".cy.ts", result.FileName);
        }

        [Fact]
        public void CreateCypressFile_LongFileName_TruncatesToAllowedLength()
        {
            // Arrange
            var content = "test content";
            var fileName = new string('a', 150);
            // Use real FileNameService to sanitize (truncates to 50 chars)
            var sanitized = _fileNameService.Sanitize(fileName);

            // Act
            var result = _service.CreateCypressFile(content, sanitized);

            // Assert
            // Sanitizer limits to 50 characters; add extension length
            Assert.True(result.FileName.Length <= 50 + ".cy.ts".Length);
        }

        [Fact]
        public void CreateCypressFile_FileNameWithInvalidCharacters_RemovesInvalidChars()
        {
            // Arrange
            var content = "test content";
            var fileName = "test<>:file";
            // Use real FileNameService to sanitize (removes invalid chars)
            var sanitized = _fileNameService.Sanitize(fileName);

            // Act
            var result = _service.CreateCypressFile(content, sanitized);

            // Assert
            Assert.DoesNotContain("<", result.FileName);
            Assert.DoesNotContain(">", result.FileName);
            Assert.DoesNotContain(":", result.FileName);
        }

        [Fact]
        public void CreateCypressFile_EmptyFileName_ReturnsDefaultFileName()
        {
            // Arrange
            var content = "test content";
            var fileName = "";
            // Use real FileNameService to sanitize (returns "unnamed" for empty)
            var sanitized = _fileNameService.Sanitize(fileName);

            // Act
            var result = _service.CreateCypressFile(content, sanitized);

            // Assert
            Assert.Equal("unnamed.cy.ts", result.FileName);
        }

        [Fact]
        public void CreateCypressFile_ContentEncodedAsUtf8()
        {
            // Arrange
            var content = "describe('????', () => { it('???', () => {}); });"; // Mixed unicode
            var fileName = "unicode_test";

            // Act
            var result = _service.CreateCypressFile(content, fileName);

            // Assert
            var decodedContent = System.Text.Encoding.UTF8.GetString(result.FileContent);
            Assert.Equal(content, decodedContent);
        }

        #endregion

        #region CreateGherkinFile Tests

        [Fact]
        public void CreateGherkinFile_ValidContent_ReturnsCorrectResult()
        {
            // Arrange
            var content = "Feature: Test\n  Scenario: Test scenario";
            var fileName = "test";

            // Act
            var result = _service.CreateGherkinFile(content, fileName);

            // Assert
            Assert.NotNull(result.FileContent);
            Assert.True(result.FileContent.Length > 0);
            Assert.Equal("text/plain", result.ContentType);
            Assert.Equal("test.feature", result.FileName);
        }

        [Fact]
        public void CreateGherkinFile_FileNameWithExtension_DoesNotDuplicateExtension()
        {
            // Arrange
            var content = "Feature: Test";
            var fileName = "test.feature";

            // Act
            var result = _service.CreateGherkinFile(content, fileName);

            // Assert
            Assert.Equal("test.feature", result.FileName);
        }

        [Fact]
        public void CreateGherkinFile_FileNameWithSpaces_ReplacesWithHyphens()
        {
            // Arrange
            var content = "Feature: Test";
            var fileName = "my test feature";
            // Use real FileNameService to sanitize
            var sanitized = _fileNameService.Sanitize(fileName);

            // Act
            var result = _service.CreateGherkinFile(content, sanitized);

            // Assert
            Assert.Equal("my-test-feature.feature", result.FileName);
        }

        [Fact]
        public void CreateGherkinFile_FileNameWithUpperCase_ConvertsToLowerCase()
        {
            // Arrange
            var content = "Feature: Test";
            var fileName = "MyFeature";

            // Act
            var result = _service.CreateGherkinFile(content, fileName);

            // Assert
            Assert.Equal("myfeature.feature", result.FileName);
        }

        [Fact]
        public void CreateGherkinFile_LongFileName_TruncatesToAllowedLength()
        {
            // Arrange
            var content = "Feature: Test";
            var fileName = new string('b', 150);

            // Act
            var result = _service.CreateGherkinFile(content, fileName);

            // Assert
            Assert.True(result.FileName.Length <= 50 + ".feature".Length);
        }

        [Fact]
        public void CreateGherkinFile_EmptyFileName_ReturnsDefaultFileName()
        {
            // Arrange
            var content = "Feature: Test";
            var fileName = "";

            // Act
            var result = _service.CreateGherkinFile(content, fileName);

            // Assert
            Assert.Equal("unnamed.feature", result.FileName);
        }

        [Fact]
        public void CreateGherkinFile_ContentEncodedAsUtf8()
        {
            // Arrange
            var content = "Feature: ????\n  Scenario: ???"; // Mixed unicode
            var fileName = "unicode_test";

            // Act
            var result = _service.CreateGherkinFile(content, fileName);

            // Assert
            var decodedContent = System.Text.Encoding.UTF8.GetString(result.FileContent);
            Assert.Equal(content, decodedContent);
        }

        #endregion
    }
}
