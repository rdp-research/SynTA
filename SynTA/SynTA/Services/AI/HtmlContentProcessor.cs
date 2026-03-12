using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SynTA.Constants;

namespace SynTA.Services.AI;

/// <summary>
/// Service responsible for processing and simplifying HTML content for AI consumption.
/// Handles HtmlAgilityPack cleaning, list collapsing, truncation logic, and intelligent compression.
/// </summary>
public class HtmlContentProcessor
{
    private readonly ILogger<HtmlContentProcessor> _logger;

    public HtmlContentProcessor(ILogger<HtmlContentProcessor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes raw web content into simplified HTML suitable for AI processing.
    /// </summary>
    public string ProcessHtmlContent(RawWebContent rawContent, HtmlFetchOptions? options = null)
    {
        options ??= new HtmlFetchOptions();
        var sb = new StringBuilder();

        // Priority 0: Add page metadata at the very top
        if (options.IncludePageMetadata && rawContent.PageMetadata != null && !string.IsNullOrEmpty(rawContent.PageMetadata.Title))
        {
            sb.AppendLine("<!-- ========================================== -->");
            sb.AppendLine("<!-- PAGE METADATA - USE THESE EXACT VALUES -->");
            sb.AppendLine("<!-- ========================================== -->");
            sb.AppendLine($"<!-- PAGE TITLE: \"{rawContent.PageMetadata.Title}\" -->");
            if (!string.IsNullOrEmpty(rawContent.PageMetadata.H1Text))
                sb.AppendLine($"<!-- H1 HEADING: \"{rawContent.PageMetadata.H1Text}\" -->");
            if (!string.IsNullOrEmpty(rawContent.PageMetadata.MetaDescription))
                sb.AppendLine($"<!-- META DESCRIPTION: \"{rawContent.PageMetadata.MetaDescription}\" -->");
            if (!string.IsNullOrEmpty(rawContent.PageMetadata.Language))
                sb.AppendLine($"<!-- LANGUAGE: {rawContent.PageMetadata.Language} -->");
            sb.AppendLine("<!-- ");
            sb.AppendLine("IMPORTANT: When asserting page title, use the EXACT value above.");
            sb.AppendLine("Example: cy.title().should('eq', 'Welcome | My Site') // Use actual title, not invented");
            sb.AppendLine("-->");
            sb.AppendLine();
        }

        // Scan for visible semantic container tags
        var visibleSemanticTags = ExtractVisibleSemanticTags(rawContent.Html);
        if (visibleSemanticTags.Count > 0)
        {
            sb.AppendLine($"<!-- VISIBLE SEMANTIC TAGS: [{string.Join(", ", visibleSemanticTags)}] -->");
            sb.AppendLine("<!-- AI: Only use these semantic tags in selectors. Do NOT assume others exist or are visible. -->");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("<!-- VISIBLE SEMANTIC TAGS: [none] -->");
            sb.AppendLine("<!-- AI: No visible semantic containers found. Use cy.get('body') as the safe root selector. -->");
            sb.AppendLine();
        }

        // Priority 1: Add structured UI ELEMENT MAP
        if (options.IncludeUiElementMap && rawContent.InteractiveElements.Count > 0)
        {
            sb.Append(BuildInteractiveElementsMap(rawContent.InteractiveElements));
        }

        // Priority 2: Add accessibility tree if available
        if (options.IncludeAccessibilityTree && !string.IsNullOrEmpty(rawContent.AccessibilityTree))
        {
            sb.AppendLine(rawContent.AccessibilityTree);
            sb.AppendLine();
        }

        if (!options.IncludeSimplifiedHtml)
        {
            return sb.ToString();
        }

        // Priority 3: Process and add the simplified HTML using HtmlAgilityPack
        var simplifiedHtml = CleanHtmlWithParser(rawContent.Html);

        // SMART COMPRESSION STEP 1: Collapse repetitive lists before final assembly
        simplifiedHtml = CollapseRepetitiveLists(simplifiedHtml);

        // SMART COMPRESSION STEP 2: Aggressively truncate header/nav/footer if over limit
        var preliminaryResult = sb.ToString() + simplifiedHtml.Trim();
        if (preliminaryResult.Length > HtmlProcessingConstants.MaxHtmlLength)
        {
            _logger.LogDebug("[{OperationId}] HTML exceeds limit before assembly, truncating semantic regions", rawContent.OperationId);
            simplifiedHtml = TruncateSemanticRegions(simplifiedHtml);
        }

        sb.Append(simplifiedHtml.Trim());

        var result = sb.ToString();

        // SMART COMPRESSION STEP 3: Intelligently preserve critical content regions
        if (result.Length > HtmlProcessingConstants.MaxHtmlLength)
        {
            _logger.LogDebug("[{OperationId}] HTML exceeds max length ({Length} > {MaxLength}), applying intelligent truncation",
                rawContent.OperationId, result.Length, HtmlProcessingConstants.MaxHtmlLength);

            result = ApplyIntelligentTruncation(result, rawContent.InteractiveElements, rawContent.OperationId);
        }

        _logger.LogInformation(
            "[{OperationId}] HTML content processed - OriginalSize: {OriginalSize} chars, FinalSize: {FinalSize} chars, Reduction: {Reduction:P0}",
            rawContent.OperationId, rawContent.Html.Length, result.Length, 1.0 - ((double)result.Length / rawContent.Html.Length));

        return result;
    }

    /// <summary>
    /// Builds the structured UI ELEMENT MAP comment section.
    /// </summary>
    private string BuildInteractiveElementsMap(List<InteractiveElement> interactiveElements)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!-- ========================================== -->");
        sb.AppendLine("<!-- UI ELEMENT MAP - USE THESE EXACT SELECTORS -->");
        sb.AppendLine("<!-- ========================================== -->");
        sb.AppendLine("<!-- ");
        sb.AppendLine("CRITICAL INSTRUCTIONS FOR AI:");
        sb.AppendLine("1. ONLY use selectors from this map. Do NOT invent selectors.");
        sb.AppendLine("2. Each element has a RECOMMENDED SELECTOR - use it directly, NEVER split it.");
        sb.AppendLine("3. NEVER split selectors with .find() or assume parent-child relationships.");
        sb.AppendLine("   Example: If map shows cy.get('h3#hero-title'), the <h3> element HAS that ID.");
        sb.AppendLine("   WRONG: cy.get('#hero-title').find('h3') - Assumes parent-child (incorrect).");
        sb.AppendLine("   CORRECT: cy.get('h3#hero-title') - The selector from the map (correct).");
        sb.AppendLine("4. Stability Score: 100=best (data-testid), 30=worst (fallback).");
        sb.AppendLine("5. Elements are grouped by SEMANTIC REGION (header, nav, main, form, footer).");
        sb.AppendLine("6. Form inputs include their LABEL - use label text for context.");
        sb.AppendLine("7. If Gherkin mentions text NOT in this map, find the closest match.");
        sb.AppendLine("8. [HIDDEN] elements exist but are not visible - they require action to reveal.");
        sb.AppendLine("   EXAMPLE: 'About Us' link may be [HIDDEN] in a collapsed menu.");
        sb.AppendLine("   ACTION: Find and click the menu button FIRST to make it visible.");
        sb.AppendLine("9. [SHADOW-DOM] elements are encapsulated in Shadow DOM - standard selectors WILL FAIL.");
        sb.AppendLine("   EXAMPLE: cy.get('#submit-btn') will fail if #submit-btn is inside Shadow DOM.");
        sb.AppendLine("   ACTION: Use .shadow().find() or see the WARNING comment for each shadow element.");
        sb.AppendLine("-->");

        sb.AppendLine();

        // Group elements by semantic region for better context
        var groupedByRegion = interactiveElements
            .GroupBy(e => e.SemanticRegion ?? "body")
            .OrderBy(g => GetRegionOrder(g.Key));

        foreach (var regionGroup in groupedByRegion)
        {
            sb.AppendLine($"<!-- ?????? REGION: {regionGroup.Key.ToUpper()} ?????? -->");

            // Further group by element type within region
            var byType = regionGroup
                .GroupBy(e => e.Tag)
                .OrderByDescending(g => g.Max(e => e.SelectorStabilityScore));

            foreach (var typeGroup in byType)
            {
                // Sort by stability score (most stable first)
                var sortedElements = typeGroup
                    .OrderByDescending(e => e.SelectorStabilityScore)
                    .Take(15); // Limit per type per region

                foreach (var el in sortedElements)
                {
                    var description = BuildElementDescription(el);
                    var labelInfo = !string.IsNullOrEmpty(el.AssociatedLabel) ? $" [LABEL: \"{el.AssociatedLabel}\"]" : "";
                    var requiredInfo = el.IsRequired ? " [REQUIRED]" : "";
                    var shadowInfo = el.InShadowDom ? " [SHADOW-DOM]" : "";
                    var formInfo = !string.IsNullOrEmpty(el.FormContext) && el.FormContext != el.SemanticRegion ? $" [IN: {el.FormContext}]" : "";
                    var visibilityInfo = !el.IsVisible ? " [HIDDEN]" : "";

                    sb.AppendLine($"<!-- [{el.SelectorStabilityScore:D3}] {el.Tag.ToUpper()}: {description}{labelInfo}{requiredInfo}{shadowInfo}{formInfo}{visibilityInfo} -->");
                    sb.AppendLine($"<!--       SELECTOR: {el.RecommendedSelector} -->");

                    // Add explicit warning for Shadow DOM elements
                    if (el.InShadowDom)
                    {
                        sb.AppendLine($"<!--       WARNING: This element is INSIDE Shadow DOM. Standard cy.get('{el.RecommendedSelector}') WILL FAIL. -->");
                        sb.AppendLine($"<!--       USAGE: First locate the shadow host, then use .shadow().find('{el.RecommendedSelector}') -->");
                    }

                    // Add alternative selectors for high-value elements
                    if (el.SelectorStabilityScore >= 70)
                    {
                        var alternatives = BuildAlternativeSelectors(el);
                        if (alternatives.Count > 0)
                        {
                            sb.AppendLine($"<!--       ALTERNATIVES: {string.Join(" | ", alternatives.Take(3))} -->");
                        }
                    }
                }
            }
            sb.AppendLine();
        }

        // Add a quick-reference text-to-selector lookup table
        sb.AppendLine("<!-- ?????? TEXT-TO-SELECTOR LOOKUP ?????? -->");
        sb.AppendLine("<!-- Use this to find elements by their visible text -->");
        var textLookup = interactiveElements
            .Where(e => !string.IsNullOrWhiteSpace(e.Text) && e.Text.Length > 2 && e.Text.Length < 50)
            .GroupBy(e => e.Text!.Trim().ToLowerInvariant())
            .Where(g => g.Count() == 1) // Only unique text
            .OrderBy(g => g.Key)
            .Take(40);

        foreach (var lookup in textLookup)
        {
            var el = lookup.First();
            var cleanText = el.Text!.Trim();
            if (cleanText.Length > 30) cleanText = cleanText[..30] + "...";
            sb.AppendLine($"<!--   \"{cleanText}\" ? {el.RecommendedSelector} -->");
        }
        sb.AppendLine();

        // Add form fields summary if any forms exist
        var formElements = interactiveElements
            .Where(e => !string.IsNullOrEmpty(e.FormContext) &&
                       (e.Tag == "input" || e.Tag == "select" || e.Tag == "textarea"))
            .ToList();

        if (formElements.Count > 0)
        {
            sb.AppendLine("<!-- ?????? FORM FIELDS REFERENCE ?????? -->");
            sb.AppendLine("<!-- Complete list of form inputs with their labels -->");

            var byForm = formElements.GroupBy(e => e.FormContext);
            foreach (var formGroup in byForm)
            {
                sb.AppendLine($"<!-- FORM: {formGroup.Key} -->");
                foreach (var field in formGroup.Take(20))
                {
                    var inputType = field.Type ?? "text";
                    var label = field.AssociatedLabel ?? field.Placeholder ?? field.AriaLabel ?? field.Name ?? "(no label)";
                    var required = field.IsRequired ? "*" : "";
                    sb.AppendLine($"<!--   [{inputType}] {label}{required} ? {field.RecommendedSelector} -->");
                }
            }
            sb.AppendLine();
        }

        sb.AppendLine("<!-- ========================================== -->");
        sb.AppendLine("<!-- END UI ELEMENT MAP -->");
        sb.AppendLine("<!-- ========================================== -->");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Extracts a list of visible semantic HTML5 container tags from the HTML.
    /// </summary>
    private static List<string> ExtractVisibleSemanticTags(string html)
    {
        var visibleTags = new List<string>();

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (var tag in HtmlSemanticTags.AllSemanticTags)
            {
                var nodes = doc.DocumentNode.SelectNodes($"//{tag}[@data-synta-visible='true']");
                if (nodes != null && nodes.Any())
                {
                    visibleTags.Add(tag);
                }
            }
        }
        catch
        {
            // Fallback to regex if parsing fails
            foreach (var tag in HtmlSemanticTags.AllSemanticTags)
            {
                var pattern = $@"<{tag}[^>]*data-synta-visible\s*=\s*[""]true[""][^>]*>";
                if (Regex.IsMatch(html, pattern, RegexOptions.IgnoreCase))
                {
                    visibleTags.Add(tag);
                }
            }
        }

        return visibleTags;
    }

    /// <summary>
    /// Cleans HTML using HtmlAgilityPack for safe DOM manipulation.
    /// </summary>
    private string CleanHtmlWithParser(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script tags and their content
            var scriptNodes = doc.DocumentNode.SelectNodes("//script");
            if (scriptNodes != null)
            {
                foreach (var node in scriptNodes.ToList())
                {
                    node.Remove();
                }
            }

            // Remove style tags and their content
            var styleNodes = doc.DocumentNode.SelectNodes("//style");
            if (styleNodes != null)
            {
                foreach (var node in styleNodes.ToList())
                {
                    node.Remove();
                }
            }

            // Replace SVG content with simplified placeholder
            var svgNodes = doc.DocumentNode.SelectNodes("//svg");
            if (svgNodes != null)
            {
                foreach (var node in svgNodes.ToList())
                {
                    var replacement = HtmlNode.CreateNode("<svg/>");
                    node.ParentNode.ReplaceChild(replacement, node);
                }
            }

            // Remove noscript tags
            var noscriptNodes = doc.DocumentNode.SelectNodes("//noscript");
            if (noscriptNodes != null)
            {
                foreach (var node in noscriptNodes.ToList())
                {
                    node.Remove();
                }
            }

            // Remove HTML comments (except our summary comments)
            var commentNodes = doc.DocumentNode.SelectNodes("//comment()");
            if (commentNodes != null)
            {
                foreach (var node in commentNodes.ToList())
                {
                    var commentText = node.InnerText;
                    // Keep important comments
                    if (!commentText.Contains("INTERACTIVE") &&
                        !commentText.Contains("ACCESSIBILITY") &&
                        !commentText.Contains("VISIBLE SEMANTIC") &&
                        !commentText.Contains("AI:") &&
                        !commentText.Contains("Use these"))
                    {
                        node.Remove();
                    }
                }
            }

            // Process all elements to clean attributes
            var allNodes = doc.DocumentNode.SelectNodes("//*");
            if (allNodes != null)
            {
                foreach (var node in allNodes)
                {
                    CleanElementAttributes(node);
                }
            }

            // Remove empty div and span tags
            RemoveEmptyElements(doc.DocumentNode, new[] { "div", "span" });

            // Extract just the body content if present
            var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode != null)
            {
                return bodyNode.InnerHtml;
            }

            return doc.DocumentNode.InnerHtml;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean HTML with parser, falling back to original HTML");
            return html;
        }
    }

    /// <summary>
    /// Cleans attributes from an HTML element.
    /// </summary>
    private void CleanElementAttributes(HtmlNode node)
    {
        // Remove inline style attributes
        node.Attributes.Remove("style");

        // Filter class attributes
        var classAttr = node.GetAttributeValue("class", string.Empty);
        if (!string.IsNullOrWhiteSpace(classAttr))
        {
            var classes = classAttr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var filteredClasses = classes.Where(cls =>
            {
                // Remove CSS-in-JS hashed classes
                if (Regex.IsMatch(cls, @"^(css-[a-z0-9]+|sc-[a-zA-Z0-9]+|makeStyles-[a-zA-Z0-9-]+)$"))
                    return false;

                // Remove CSS module hashes
                if (Regex.IsMatch(cls, @"^[a-zA-Z0-9_-]*__[a-zA-Z0-9_-]+___[a-zA-Z0-9]+$"))
                    return false;

                return true;
            }).ToArray();

            if (filteredClasses.Length > 0)
            {
                node.SetAttributeValue("class", string.Join(" ", filteredClasses));
            }
            else
            {
                node.Attributes.Remove("class");
            }
        }

        // Remove Vue scoped style data attributes
        var attributesToRemove = node.Attributes
            .Where(attr => Regex.IsMatch(attr.Name, @"^data-v-[a-f0-9]{6,8}$", RegexOptions.IgnoreCase))
            .ToList();

        foreach (var attr in attributesToRemove)
        {
            node.Attributes.Remove(attr);
        }

        // Remove Angular internal attributes
        attributesToRemove = node.Attributes
            .Where(attr => Regex.IsMatch(attr.Name, @"^_ng(?:content|host)-[a-z]+-?\d+$", RegexOptions.IgnoreCase) ||
                          attr.Name.Equals("ng-version", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var attr in attributesToRemove)
        {
            node.Attributes.Remove(attr);
        }

        // Replace large base64 image sources with placeholder
        var srcAttr = node.GetAttributeValue("src", string.Empty);
        if (!string.IsNullOrEmpty(srcAttr) && srcAttr.StartsWith("data:image/") && srcAttr.Length > 100)
        {
            node.SetAttributeValue("src", "[base64-image-data]");
        }

        // Remove data attributes that aren't useful for testing
        var dataAttrsToRemove = node.Attributes
            .Where(attr => attr.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase) &&
                          !attr.Name.Equals("data-testid", StringComparison.OrdinalIgnoreCase) &&
                          !attr.Name.Equals("data-cy", StringComparison.OrdinalIgnoreCase) &&
                          !attr.Name.Equals("data-test", StringComparison.OrdinalIgnoreCase) &&
                          !attr.Name.Equals("data-id", StringComparison.OrdinalIgnoreCase) &&
                          !attr.Name.StartsWith("data-synta-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var attr in dataAttrsToRemove)
        {
            node.Attributes.Remove(attr);
        }
    }

    /// <summary>
    /// Collapses repetitive lists by keeping first 3 and last 2 items.
    /// </summary>
    private string CollapseRepetitiveLists(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Process ul and ol lists
            var listNodes = doc.DocumentNode.SelectNodes("//ul | //ol");
            if (listNodes != null)
            {
                foreach (var listNode in listNodes)
                {
                    var items = listNode.SelectNodes("./li")?.ToList();
                    if (items != null && items.Count > HtmlProcessingConstants.ListCollapsing.MinItemsToCollapse)
                    {
                        var collapsedCount = items.Count - HtmlProcessingConstants.ListCollapsing.MinItemsToCollapse;

                        // Keep first 3 and last 2
                        var itemsToRemove = items
                            .Skip(HtmlProcessingConstants.ListCollapsing.KeepFirstItems)
                            .Take(collapsedCount)
                            .ToList();

                        // Insert comment after 3rd item
                        var commentNode = doc.CreateComment($" ... [{collapsedCount}] items collapsed ... ");
                        items[HtmlProcessingConstants.ListCollapsing.KeepFirstItems - 1].ParentNode
                            .InsertAfter(commentNode, items[HtmlProcessingConstants.ListCollapsing.KeepFirstItems - 1]);

                        // Remove middle items
                        foreach (var item in itemsToRemove)
                        {
                            item.Remove();
                        }
                    }
                }
            }

            // Process repetitive div structures within containers
            var divContainers = doc.DocumentNode.SelectNodes($"//div[count(./div) > {HtmlProcessingConstants.ListCollapsing.MinItemsToCollapse}]");
            if (divContainers != null)
            {
                foreach (var container in divContainers)
                {
                    var childDivs = container.SelectNodes("./div")?.ToList();
                    if (childDivs != null && childDivs.Count > HtmlProcessingConstants.ListCollapsing.MinItemsToCollapse)
                    {
                        // Check if divs look similar
                        if (AreDivsSimilar(childDivs.Select(d => d.OuterHtml).ToList()))
                        {
                            var collapsedCount = childDivs.Count - HtmlProcessingConstants.ListCollapsing.MinItemsToCollapse;

                            // Keep first 3 and last 2
                            var divsToRemove = childDivs
                                .Skip(HtmlProcessingConstants.ListCollapsing.KeepFirstItems)
                                .Take(collapsedCount)
                                .ToList();

                            // Insert comment after 3rd div
                            var commentNode = doc.CreateComment($" ... [{collapsedCount}] items collapsed ... ");
                            childDivs[HtmlProcessingConstants.ListCollapsing.KeepFirstItems - 1].ParentNode
                                .InsertAfter(commentNode, childDivs[HtmlProcessingConstants.ListCollapsing.KeepFirstItems - 1]);

                            // Remove middle divs
                            foreach (var div in divsToRemove)
                            {
                                div.Remove();
                            }
                        }
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collapse repetitive lists, returning original HTML");
            return html;
        }
    }

    /// <summary>
    /// Checks if a list of div elements have similar structure.
    /// </summary>
    private bool AreDivsSimilar(List<string> divs)
    {
        if (divs.Count < 2) return false;

        // Extract opening tag structure from first div
        var firstDivOpenTag = Regex.Match(divs[0], @"<div[^>]*>", RegexOptions.IgnoreCase).Value;

        // Extract class attribute from first div if exists
        var classMatch = Regex.Match(firstDivOpenTag, @"class=[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        var firstClass = classMatch.Success ? classMatch.Groups[1].Value : "";

        // Check if at least 70% of divs have the same class or similar tag structure
        int similarCount = divs.Count(div =>
        {
            var openTag = Regex.Match(div, @"<div[^>]*>", RegexOptions.IgnoreCase).Value;
            if (!string.IsNullOrEmpty(firstClass))
            {
                return openTag.Contains(firstClass);
            }
            // Check if tags have similar length (crude similarity check)
            return Math.Abs(openTag.Length - firstDivOpenTag.Length) < 50;
        });

        return (double)similarCount / divs.Count >= 0.7;
    }

    /// <summary>
    /// Aggressively truncates content inside header, nav, and footer tags.
    /// </summary>
    private string TruncateSemanticRegions(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            foreach (var tag in HtmlSemanticTags.TagsToTruncate)
            {
                var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        var content = node.InnerHtml;
                        if (content.Length > HtmlProcessingConstants.TruncationLengths.SemanticRegion)
                        {
                            var truncated = content.Substring(0, HtmlProcessingConstants.TruncationLengths.SemanticRegion);
                            var comment = doc.CreateComment($" ... {tag} content truncated ... ");
                            node.InnerHtml = truncated;
                            node.AppendChild(comment);
                        }
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to truncate semantic regions, returning original HTML");
            return html;
        }
    }

    /// <summary>
    /// Recursively removes empty elements of specified tag names from the DOM.
    /// </summary>
    private void RemoveEmptyElements(HtmlNode node, string[] tagNames)
    {
        if (node == null) return;

        foreach (var tagName in tagNames)
        {
            var emptyNodes = node.SelectNodes($"//{tagName}[not(node()) or normalize-space(.)='']");
            if (emptyNodes != null)
            {
                foreach (var emptyNode in emptyNodes.ToList())
                {
                    emptyNode.Remove();
                }
            }
        }
    }

    /// <summary>
    /// Applies intelligent truncation to preserve critical content.
    /// </summary>
    private string ApplyIntelligentTruncation(string result, List<InteractiveElement> interactiveElements, string operationId)
    {
        // Extract the summary section (always keep this)
        var summaryEnd = result.IndexOf("<!-- END UI ELEMENT MAP -->");
        if (summaryEnd == -1)
            summaryEnd = result.IndexOf("<!-- END INTERACTIVE ELEMENTS -->");
        if (summaryEnd == -1)
            summaryEnd = result.IndexOf("<!-- END ACCESSIBILITY TREE -->");
        if (summaryEnd == -1)
            summaryEnd = 0;
        else
            summaryEnd += 35;

        var summary = summaryEnd > 0 ? result[..summaryEnd] : "";
        var htmlContent = summaryEnd > 0 ? result[summaryEnd..] : result;

        return IntelligentTruncation(summary, htmlContent, interactiveElements, HtmlProcessingConstants.MaxHtmlLength, operationId);
    }

    /// <summary>
    /// Intelligently truncates HTML content by prioritizing critical regions.
    /// </summary>
    private string IntelligentTruncation(string summary, string htmlContent, List<InteractiveElement> interactiveElements, int maxLength, string operationId)
    {
        var availableSpace = maxLength - summary.Length - 500;

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // PRIORITY 0: Extract and preserve navigation context (header + nav)
            // Navigation is critical for test generation - Gherkin often references nav links
            var navContext = ExtractNavigationContext(doc, availableSpace / 4); // Use up to 1/4 of budget
            if (!string.IsNullOrEmpty(navContext))
            {
                _logger.LogInformation("[{OperationId}] Intelligent truncation: Preserving navigation context ({Length} chars)",
                    operationId, navContext.Length);
                availableSpace -= navContext.Length;
                summary = summary + "\n<!-- NAVIGATION CONTEXT (preserved for selector accuracy) -->\n" + navContext + "\n";
            }

            // PRIORITY 1: Try to extract and preserve <main> tag content
            var mainNode = doc.DocumentNode.SelectSingleNode($"//{HtmlSemanticTags.Main}");
            if (mainNode != null && mainNode.OuterHtml.Length <= availableSpace)
            {
                _logger.LogInformation("[{OperationId}] Intelligent truncation: Preserved <main> tag content ({Length} chars)", operationId, mainNode.OuterHtml.Length);
                return summary + $"\n<!-- HTML truncated: <{HtmlSemanticTags.Main}> content preserved below -->\n" +
                       mainNode.OuterHtml +
                       "\n<!-- HTML truncated: header/footer/other regions omitted -->";
            }

            // PRIORITY 2: Try <article> tag
            var articleNode = doc.DocumentNode.SelectSingleNode($"//{HtmlSemanticTags.Article}");
            if (articleNode != null && articleNode.OuterHtml.Length <= availableSpace)
            {
                _logger.LogInformation("[{OperationId}] Intelligent truncation: Preserved <article> tag content ({Length} chars)", operationId, articleNode.OuterHtml.Length);
                return summary + $"\n<!-- HTML truncated: <{HtmlSemanticTags.Article}> content preserved below -->\n" +
                       articleNode.OuterHtml +
                       "\n<!-- HTML truncated: header/footer/other regions omitted -->";
            }

            // PRIORITY 3: Find regions with highest density of interactive elements
            if (interactiveElements.Count > 0)
            {
                var mainRegionElements = interactiveElements
                    .Where(e => e.SemanticRegion?.Equals(HtmlSemanticTags.Main, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();

                if (mainRegionElements.Count > 0 && mainNode != null && mainNode.OuterHtml.Length <= availableSpace)
                {
                    _logger.LogInformation("[{OperationId}] Intelligent truncation: Preserved main region with {Count} interactive elements ({Length} chars)",
                        operationId, mainRegionElements.Count, mainNode.OuterHtml.Length);
                    return summary + $"\n<!-- HTML truncated: <{HtmlSemanticTags.Main}> region with interactive elements preserved -->\n" +
                           mainNode.OuterHtml +
                           "\n<!-- HTML truncated: non-essential regions omitted -->";
                }
            }

            // PRIORITY 4: Try <form> tags
            var formNodes = doc.DocumentNode.SelectNodes("//form");
            if (formNodes != null && formNodes.Count > 0)
            {
                var formsContent = new StringBuilder();
                var totalFormLength = 0;

                foreach (var formNode in formNodes)
                {
                    if (totalFormLength + formNode.OuterHtml.Length > availableSpace)
                        break;

                    formsContent.AppendLine(formNode.OuterHtml);
                    totalFormLength += formNode.OuterHtml.Length + 1;
                }

                if (totalFormLength > 0)
                {
                    _logger.LogInformation("[{OperationId}] Intelligent truncation: Preserved {Count} form(s) ({Length} chars)", operationId, formNodes.Count, totalFormLength);
                    return summary + "\n<!-- HTML truncated: <form> content preserved below -->\n" +
                           formsContent.ToString() +
                           "\n<!-- HTML truncated: non-form content omitted -->";
                }
            }

            // PRIORITY 5: Extract sections with highest interactive element density
            var denseRegions = FindDenseInteractiveRegions(htmlContent, interactiveElements, availableSpace, operationId);
            if (!string.IsNullOrEmpty(denseRegions))
            {
                _logger.LogInformation("[{OperationId}] Intelligent truncation: Preserved dense interactive regions", operationId);
                return summary + "\n<!-- HTML truncated: high-density interactive regions preserved -->\n" +
                       denseRegions +
                       "\n<!-- HTML truncated: low-density regions omitted -->";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{OperationId}] Failed to perform intelligent truncation with parser, falling back to simple strategy", operationId);
        }

        // FALLBACK: Use head + middle + tail strategy
        _logger.LogWarning("[{OperationId}] Intelligent truncation: Falling back to head+middle+tail strategy", operationId);

        if (htmlContent.Length > HtmlProcessingConstants.TruncationLengths.Head + 
                                HtmlProcessingConstants.TruncationLengths.Middle + 
                                HtmlProcessingConstants.TruncationLengths.Tail + 500)
        {
            var headEndIndex = htmlContent.LastIndexOf('>', HtmlProcessingConstants.TruncationLengths.Head);
            if (headEndIndex == -1 || headEndIndex < HtmlProcessingConstants.TruncationLengths.Head / 2)
                headEndIndex = HtmlProcessingConstants.TruncationLengths.Head;
            var head = htmlContent[..(headEndIndex + 1)];

            var middleStartPos = (htmlContent.Length / 2) - (HtmlProcessingConstants.TruncationLengths.Middle / 2);
            var middleStartIndex = htmlContent.IndexOf('<', middleStartPos);
            if (middleStartIndex == -1) middleStartIndex = middleStartPos;

            var middleEndPos = middleStartIndex + HtmlProcessingConstants.TruncationLengths.Middle;
            var middleEndIndex = htmlContent.LastIndexOf('>', Math.Min(middleEndPos, htmlContent.Length - 1));
            if (middleEndIndex <= middleStartIndex) middleEndIndex = Math.Min(middleEndPos, htmlContent.Length - 1);

            var middle = htmlContent[middleStartIndex..(middleEndIndex + 1)];

            var tailStartIndex = htmlContent.Length - HtmlProcessingConstants.TruncationLengths.Tail;
            var tailStartTag = htmlContent.IndexOf('<', tailStartIndex);
            if (tailStartTag == -1 || tailStartTag > htmlContent.Length - HtmlProcessingConstants.TruncationLengths.Tail / 2)
                tailStartTag = tailStartIndex;
            var tail = htmlContent[tailStartTag..];

            var omittedChars = htmlContent.Length - head.Length - middle.Length - tail.Length;

            _logger.LogInformation("[{OperationId}] Fallback truncation: kept {HeadLength} head + {MiddleLength} middle + {TailLength} tail, omitted {Omitted} chars",
                operationId, head.Length, middle.Length, tail.Length, omittedChars);

            return summary + head +
                $"\n<!-- ... [{(middleStartIndex - headEndIndex - 1)} chars omitted] ... -->\n" +
                middle +
                $"\n<!-- ... [{(tailStartTag - middleEndIndex - 1)} chars omitted] ... -->\n" +
                tail;
        }

        // Ultimate fallback
        var truncateAt = htmlContent.LastIndexOf('>', availableSpace);
        if (truncateAt > availableSpace / 2)
        {
            return summary + "\n" + htmlContent[..(truncateAt + 1)] + "\n<!-- HTML truncated for length -->";
        }

        return summary + "\n" + htmlContent[..availableSpace] + "\n<!-- HTML truncated for length -->";
    }

    /// <summary>
    /// Finds regions in the HTML with high density of interactive elements.
    /// </summary>
    private string FindDenseInteractiveRegions(string htmlContent, List<InteractiveElement> interactiveElements, int maxLength, string operationId)
    {
        if (interactiveElements.Count == 0)
            return string.Empty;

        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var regionsToExtract = new List<string>();
            var totalLength = 0;

            foreach (var region in HtmlSemanticTags.PriorityRegions)
            {
                var nodes = doc.DocumentNode.SelectNodes($"//{region}");
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        // Check if this region contains interactive elements
                        var elementsInRegion = interactiveElements
                            .Count(e => e.SemanticRegion?.Equals(region, StringComparison.OrdinalIgnoreCase) == true);

                        if (elementsInRegion > 0 && totalLength + node.OuterHtml.Length <= maxLength)
                        {
                            regionsToExtract.Add(node.OuterHtml);
                            totalLength += node.OuterHtml.Length;

                            if (totalLength >= maxLength * 0.9)
                                break;
                        }
                    }
                }

                if (totalLength >= maxLength * 0.9)
                    break;
            }

            if (regionsToExtract.Count > 0)
            {
                _logger.LogInformation("[{OperationId}] Found {Count} dense regions with interactive elements, total {Length} chars",
                    operationId, regionsToExtract.Count, totalLength);
                return string.Join("\n", regionsToExtract);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{OperationId}] Failed to find dense interactive regions", operationId);
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts header and nav content for preservation during truncation.
    /// Navigation is critical for test generation as Gherkin scenarios often reference nav links.
    /// </summary>
    private static string ExtractNavigationContext(HtmlDocument doc, int maxLength)
    {
        var sb = new StringBuilder();

        // First try to get nav element (most important for selectors)
        var navNode = doc.DocumentNode.SelectSingleNode($"//{HtmlSemanticTags.Nav}");
        if (navNode != null && navNode.OuterHtml.Length <= maxLength)
        {
            sb.Append(navNode.OuterHtml);
        }

        // Then try to add header if space permits
        var headerNode = doc.DocumentNode.SelectSingleNode($"//{HtmlSemanticTags.Header}");
        if (headerNode != null)
        {
            // If nav was inside header, don't add header (would duplicate)
            var navInHeader = headerNode.SelectSingleNode($".//{HtmlSemanticTags.Nav}") != null;
            if (!navInHeader && sb.Length + headerNode.OuterHtml.Length <= maxLength)
            {
                sb.Insert(0, headerNode.OuterHtml);
            }
            else if (navInHeader && headerNode.OuterHtml.Length <= maxLength)
            {
                // If nav is in header, prefer the full header
                return headerNode.OuterHtml;
            }
        }

        return sb.ToString();
    }

    // Helper methods for building element descriptions
    private static int GetRegionOrder(string region)
    {
        return region.ToLowerInvariant() switch
        {
            "header" or "banner" => 0,
            "nav" or "navigation" => 1,
            "main" => 2,
            "article" => 3,
            "section" => 4,
            "aside" or "complementary" => 5,
            "form" => 6,
            "footer" or "contentinfo" => 7,
            "body" => 8,
            _ => region.StartsWith("form") ? 6 : 5
        };
    }

    private static string BuildElementDescription(InteractiveElement el)
    {
        var parts = new List<string>();

        // FIX: Check for iframe content and add clear warning
        var isInIframe = el.Text?.Contains("[IFRAME-CONTENT]") == true;
        
        if (!string.IsNullOrEmpty(el.Text) && el.Text.Length <= 50)
        {
            parts.Add($"\"{el.Text.Trim()}\"");
        }
        else if (!string.IsNullOrEmpty(el.AriaLabel))
        {
            parts.Add($"aria:\"{el.AriaLabel}\"");
        }
        else if (!string.IsNullOrEmpty(el.Placeholder))
        {
            parts.Add($"placeholder:\"{el.Placeholder}\"");
        }
        else if (!string.IsNullOrEmpty(el.Name))
        {
            parts.Add($"name:\"{el.Name}\"");
        }
        else if (!string.IsNullOrEmpty(el.Id))
        {
            parts.Add($"id:\"{el.Id}\"");
        }

        if (el.Tag == "input" && !string.IsNullOrEmpty(el.Type))
        {
            parts.Add($"type={el.Type}");
        }

        if (el.Tag == "a" && !string.IsNullOrEmpty(el.Href))
        {
            var href = el.Href;
            if (href.Length > 40) href = href[..40] + "...";
            parts.Add($"href:\"{href}\"");
        }

        if (!string.IsNullOrEmpty(el.Role) && el.Role != el.Tag)
        {
            parts.Add($"role={el.Role}");
        }

        // Add iframe warning at the end so it's visible to the AI
        if (isInIframe)
        {
            parts.Add("[WARNING: INSIDE IFRAME - REQUIRES SPECIAL HANDLING]");
        }

        return parts.Count > 0 ? string.Join(" ", parts) : $"({el.CssPath ?? el.Tag})";
    }

    private static List<string> BuildAlternativeSelectors(InteractiveElement el)
    {
        var alternatives = new List<string>();

        if (!string.IsNullOrEmpty(el.DataTestId))
            alternatives.Add($"[data-testid=\"{el.DataTestId}\"]");
        if (!string.IsNullOrEmpty(el.Id))
            alternatives.Add($"#{el.Id}");
        if (!string.IsNullOrEmpty(el.AriaLabel))
            alternatives.Add($"[aria-label=\"{el.AriaLabel}\"]");
        if (!string.IsNullOrEmpty(el.Name))
            alternatives.Add($"[name=\"{el.Name}\"]");
        if (!string.IsNullOrEmpty(el.Placeholder))
            alternatives.Add($"[placeholder=\"{el.Placeholder}\"]");

        if (el.Tag == "a" && !string.IsNullOrEmpty(el.Href))
        {
            var socialDomains = new[] { "linkedin.com", "facebook.com", "twitter.com", "instagram.com", "youtube.com", "github.com" };
            foreach (var domain in socialDomains)
            {
                if (el.Href.Contains(domain, StringComparison.OrdinalIgnoreCase))
                {
                    alternatives.Add($"a[href*=\"{domain}\"]");
                    break;
                }
            }
        }

        var recommended = el.RecommendedSelector ?? "";
        alternatives.RemoveAll(a => recommended.Contains(a.Replace("\"", "\"\"")));

        return alternatives;
    }
}
