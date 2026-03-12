using System.Text;
using SynTA.Services.Utilities;

namespace SynTA.Services.Export;

public class FileExportService : IFileExportService
{
    private readonly ILogger<FileExportService> _logger;
    private readonly IFileNameService _fileNameService;

    public FileExportService(ILogger<FileExportService> logger, IFileNameService fileNameService)
    {
        _logger = logger;
        _fileNameService = fileNameService;
    }

    public (byte[] FileContent, string ContentType, string FileName) CreateCypressFile(string content, string fileName)
    {
        try
        {
            // Don't sanitize the filename again - it's already been sanitized and has the correct extension
            // Sanitizing again would corrupt the extension (e.g., "load-homepage.cy.ts" -> "load-homepagecyts")
            var sanitizedFileName = fileName;

            // Check if file already has a valid Cypress extension
            var hasValidExtension = sanitizedFileName.EndsWith(".cy.ts", StringComparison.OrdinalIgnoreCase) ||
                                    sanitizedFileName.EndsWith(".cy.js", StringComparison.OrdinalIgnoreCase);

            // Only add extension if not already present
            if (!hasValidExtension)
            {
                // Default to .cy.ts for backwards compatibility
                sanitizedFileName = $"{sanitizedFileName}.cy.ts";
            }

            // Determine content type based on extension
            var contentType = sanitizedFileName.EndsWith(".cy.js", StringComparison.OrdinalIgnoreCase)
                ? "text/javascript"
                : "text/typescript";

            var fileContent = Encoding.UTF8.GetBytes(content);
            _logger.LogInformation("Created Cypress file: {FileName}, Size: {Size} bytes", sanitizedFileName, fileContent.Length);

            return (fileContent, contentType, sanitizedFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Cypress file: {FileName}", fileName);
            throw;
        }
    }

    public (byte[] FileContent, string ContentType, string FileName) CreateGherkinFile(string content, string fileName)
    {
        try
        {
            // Ensure .feature extension
            var sanitizedFileName = _fileNameService.Sanitize(fileName);
            if (!sanitizedFileName.EndsWith(".feature", StringComparison.OrdinalIgnoreCase))
            {
                sanitizedFileName = $"{sanitizedFileName}.feature";
            }

            var fileContent = Encoding.UTF8.GetBytes(content);
            _logger.LogInformation("Created Gherkin file: {FileName}, Size: {Size} bytes", sanitizedFileName, fileContent.Length);

            return (fileContent, "text/plain", sanitizedFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Gherkin file: {FileName}", fileName);
            throw;
        }
    }
}
