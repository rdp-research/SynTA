using SynTA.Models.DTOs;

namespace SynTA.Services.AI
{
    /// <summary>
    /// Service interface for fetching and processing HTML content from websites
    /// </summary>
    public interface IHtmlContextService
    {
        /// <summary>
        /// Fetches HTML content from a URL using a headless browser and simplifies it for AI processing.
        /// Supports dynamic websites including WordPress, Wix, React, Angular, and Vue applications.
        /// Also captures a screenshot for multimodal AI processing.
        /// </summary>
        /// <param name="url">The URL to fetch</param>
        /// <returns>HtmlContextResult containing simplified HTML context and optional screenshot</returns>
        Task<HtmlContextResult> FetchAndSimplifyHtmlAsync(string url);

        /// <summary>
        /// Fetches HTML content from a URL with custom options using a headless browser.
        /// Supports dynamic websites including WordPress, Wix, React, Angular, and Vue applications.
        /// Also captures a screenshot for multimodal AI processing.
        /// </summary>
        /// <param name="url">The URL to fetch</param>
        /// <param name="options">Options controlling fetch behavior (timeout, wait strategies, etc.)</param>
        /// <returns>HtmlContextResult containing simplified HTML context and optional screenshot</returns>
        Task<HtmlContextResult> FetchAndSimplifyHtmlAsync(string url, HtmlFetchOptions options);
    }
}
