namespace SynTA.Services.Export
{
    public interface IFileExportService
    {
        /// <summary>
        /// Creates a downloadable Cypress test file
        /// </summary>
        /// <param name="content">The TypeScript content</param>
        /// <param name="fileName">The desired file name (without extension)</param>
        /// <returns>Tuple containing file content bytes and MIME type</returns>
        (byte[] FileContent, string ContentType, string FileName) CreateCypressFile(string content, string fileName);

        /// <summary>
        /// Creates a downloadable Gherkin file
        /// </summary>
        /// <param name="content">The Gherkin content</param>
        /// <param name="fileName">The desired file name (without extension)</param>
        /// <returns>Tuple containing file content bytes and MIME type</returns>
        (byte[] FileContent, string ContentType, string FileName) CreateGherkinFile(string content, string fileName);
    }
}
