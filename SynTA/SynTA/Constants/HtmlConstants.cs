namespace SynTA.Constants;

/// <summary>
/// Constants for HTML semantic tags and DOM element processing.
/// Used by HtmlContentProcessor and WebScraperService.
/// </summary>
public static class HtmlSemanticTags
{
    /// <summary>
    /// Primary content container tag.
    /// </summary>
    public const string Main = "main";

    /// <summary>
    /// Navigation section tag.
    /// </summary>
    public const string Nav = "nav";

    /// <summary>
    /// Header section tag.
    /// </summary>
    public const string Header = "header";

    /// <summary>
    /// Footer section tag.
    /// </summary>
    public const string Footer = "footer";

    /// <summary>
    /// Article content tag.
    /// </summary>
    public const string Article = "article";

    /// <summary>
    /// Complementary content tag.
    /// </summary>
    public const string Aside = "aside";

    /// <summary>
    /// Generic section tag.
    /// </summary>
    public const string Section = "section";

    /// <summary>
    /// All semantic container tags.
    /// </summary>
    public static readonly string[] AllSemanticTags = 
    {
        Main,
        Nav,
        Header,
        Footer,
        Article,
        Aside,
        Section
    };

    /// <summary>
    /// Semantic tags that should be truncated to prioritize main content.
    /// </summary>
    public static readonly string[] TagsToTruncate = 
    {
        Header,
        Nav,
        Footer
    };

    /// <summary>
    /// Priority regions for intelligent HTML truncation (in order of priority).
    /// </summary>
    public static readonly string[] PriorityRegions = 
    {
        Main,
        Article,
        Section,
        "form"
    };
}

/// <summary>
/// Constants for interactive HTML elements.
/// </summary>
public static class InteractiveElementTags
{
    public const string Button = "button";
    public const string Input = "input";
    public const string Link = "a";
    public const string Select = "select";
    public const string Textarea = "textarea";

    /// <summary>
    /// All interactive element tags.
    /// </summary>
    public static readonly string[] All = 
    {
        Button,
        Input,
        Link,
        Select,
        Textarea
    };
}

/// <summary>
/// Constants for HTML processing and cleaning.
/// </summary>
public static class HtmlProcessingConstants
{
    /// <summary>
    /// Maximum HTML length for AI processing (characters).
    /// </summary>
    public const int MaxHtmlLength = 70000;

    /// <summary>
    /// Maximum truncation lengths for different regions.
    /// </summary>
    public static class TruncationLengths
    {
        public const int SemanticRegion = 2000;
        public const int Head = 20000;
        public const int Middle = 35000;
        public const int Tail = 5000;
    }

    /// <summary>
    /// Thresholds for list collapsing.
    /// </summary>
    public static class ListCollapsing
    {
        public const int MinItemsToCollapse = 5;
        public const int KeepFirstItems = 3;
        public const int KeepLastItems = 2;
    }
}
