using System.Text;
using SynTA.Models.Domain;

namespace SynTA.Services.AI.Prompts;

/// <summary>
/// Implementation of the prompt service for building AI prompts.
/// Uses a Modular Protocol Architecture to prevent AI hallucination and ensure strict syntax compliance.
/// </summary>
public class PromptService : IPromptService
{
    /// <inheritdoc />
    public string BuildGherkinPrompt(
        string title,
        string userStoryText,
        string? description = null,
        string? acceptanceCriteria = null,
        int maxScenarios = 10,
        string language = "en")
    {
        var languageName = GetLanguageName(language);

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"You are an expert test automation engineer specializing in Behavior-Driven Development (BDD) and Gherkin syntax. Generate all content in {languageName}.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Your task is to generate comprehensive, well-structured Gherkin test scenarios based on the provided user story.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Guidelines:");
        promptBuilder.AppendLine("1. Use proper Gherkin syntax with Feature, Scenario, Given, When, Then, And, But");
        promptBuilder.AppendLine("2. Create multiple scenarios covering happy paths, edge cases, and error conditions");
        promptBuilder.AppendLine("3. Make scenarios specific, testable, and independent");
        promptBuilder.AppendLine("4. Use clear, concise language");
        promptBuilder.AppendLine("5. Include scenario outlines with examples where appropriate");
        promptBuilder.AppendLine("6. Consider security, performance, and accessibility where relevant");

        if (maxScenarios == 0)
        {
            promptBuilder.AppendLine("7. Automatically determine the OPTIMAL number of scenarios needed to thoroughly test this user story. Generate as many scenarios as necessary to cover all important test cases, but avoid redundancy.");
        }
        else
        {
            promptBuilder.AppendLine($"7. Generate a MAXIMUM of {maxScenarios} scenarios total");
        }

        promptBuilder.AppendLine($"8. All scenario descriptions and step text must be in {languageName}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("User Story:");
        promptBuilder.AppendLine($"Title: {title}");
        promptBuilder.AppendLine($"User Story: {userStoryText}");

        if (!string.IsNullOrEmpty(description))
        {
            promptBuilder.AppendLine($"Description: {description}");
        }

        if (!string.IsNullOrEmpty(acceptanceCriteria))
        {
            promptBuilder.AppendLine($"Acceptance Criteria: {acceptanceCriteria}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Generate comprehensive Gherkin scenarios. Return ONLY the Gherkin syntax without any explanations or markdown code blocks:");

        return promptBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildCypressPrompt(
        string gherkinScenarios,
        string targetUrl,
        string userStoryTitle,
        string userStoryText,
        string? description = null,
        string? acceptanceCriteria = null,
        string? htmlContext = null,
        CypressScriptLanguage scriptLanguage = CypressScriptLanguage.TypeScript,
        bool hasScreenshot = false,
        bool hasPageMetadata = true,
        bool hasUiElementMap = true,
        bool hasAccessibilityTree = true)
    {
        var isTypeScript = scriptLanguage == CypressScriptLanguage.TypeScript;
        var fileExtension = isTypeScript ? ".cy.ts" : ".cy.js";
        var languageName = isTypeScript ? "TypeScript" : "JavaScript";

        var promptBuilder = new StringBuilder();

        // 1. Core Identity & Context
        AppendCoreIdentity(promptBuilder, languageName, userStoryTitle, userStoryText, description, acceptanceCriteria);

        // 2. Visual Reality Protocol (Viewport, Visibility, Responsive)
        AppendVisualRealityProtocol(promptBuilder);

        // 3. Selector Protocol (UI Map, Text Matching, DOM Interaction)
        AppendSelectorProtocol(promptBuilder, hasUiElementMap);

        // 4. Negative Testing Protocol (CRITICAL FIX for 404/500 Scenarios)
        AppendNegativeTestingProtocol(promptBuilder);

        // 5. Network Protocol (Intercepts, API Mocking, cy.request)
        AppendNetworkProtocol(promptBuilder);

        // 6. Simulation & State Protocol (NEW - Bridging Gherkin to Reality)
        AppendSimulationProtocol(promptBuilder);

        // 7. Runtime Safety Protocol (Exceptions, Anti-Hallucination, Error Injection)
        AppendRuntimeSafetyProtocol(promptBuilder);

        // 8. HTML Context (if provided)
        promptBuilder.AppendLine($"Target URL: {targetUrl}");
        promptBuilder.AppendLine();

        AppendExtractionInputProfile(promptBuilder, hasPageMetadata, hasUiElementMap, hasAccessibilityTree, !string.IsNullOrEmpty(htmlContext), hasScreenshot);

        if (!string.IsNullOrEmpty(htmlContext))
        {
            AppendHtmlContext(promptBuilder, htmlContext);
        }

        // 7. Visual Context Instructions (if screenshot provided)
        if (hasScreenshot)
        {
            AppendVisualContextInstructions(promptBuilder);
        }

        // 8. Gherkin Scenarios
        promptBuilder.AppendLine("Gherkin Scenarios:");
        promptBuilder.AppendLine(gherkinScenarios);
        promptBuilder.AppendLine();

        // 9. Output Format Requirements
        AppendOutputFormatRequirements(promptBuilder, languageName, fileExtension, isTypeScript);

        return promptBuilder.ToString();
    }

    /// <inheritdoc />
    public string GetLanguageName(string languageCode)
    {
        return languageCode.ToLowerInvariant() switch
        {
            "en" => "English",
            "uk" => "Ukrainian",
            "fr" => "French",
            "de" => "German",
            "es" => "Spanish",
            "it" => "Italian",
            "pt" => "Portuguese",
            "pl" => "Polish",
            "nl" => "Dutch",
            "ja" => "Japanese",
            "zh" => "Chinese",
            "ko" => "Korean",
            _ => "English"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // MODULAR PROTOCOL ARCHITECTURE
    // Each method below represents a distinct, non-overlapping domain of Cypress testing
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Protocol 1: Core Identity - Sets persona, language, and user story context
    /// </summary>
    private static void AppendCoreIdentity(
        StringBuilder promptBuilder,
        string languageName,
        string userStoryTitle,
        string userStoryText,
        string? description,
        string? acceptanceCriteria)
    {
        promptBuilder.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        promptBuilder.AppendLine("║                         CYPRESS TEST GENERATION PROTOCOL                     ║");
        promptBuilder.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"You are an expert Cypress test automation engineer generating {languageName} code.");
        promptBuilder.AppendLine("Your task is to convert Gherkin scenarios into executable, syntactically correct Cypress tests.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("USER STORY CONTEXT:");
        promptBuilder.AppendLine($"Title: {userStoryTitle}");
        promptBuilder.AppendLine($"User Story: {userStoryText}");

        if (!string.IsNullOrEmpty(description))
        {
            promptBuilder.AppendLine($"Description: {description}");
        }

        if (!string.IsNullOrEmpty(acceptanceCriteria))
        {
            promptBuilder.AppendLine($"Acceptance Criteria: {acceptanceCriteria}");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("This context explains the INTENT behind the test scenarios.");
        promptBuilder.AppendLine("Use it to understand what features are being tested and make intelligent decisions when mapping Gherkin steps to actual page elements.");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Protocol 1: Visual Reality - Handles ALL Viewport, Visibility, and Responsive Logic
    /// </summary>
    private static void AppendVisualRealityProtocol(StringBuilder promptBuilder)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("### 1. VISUAL REALITY PROTOCOL (VIEWPORT 1920x1080)");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("THE ENVIRONMENT:");
        promptBuilder.AppendLine("  • Tests run STRICTLY at 1920x1080 (Desktop).");
        promptBuilder.AppendLine("  • The beforeEach block MUST include: cy.viewport(1920, 1080); IMMEDIATELY BEFORE cy.visit()");
        promptBuilder.AppendLine("  • This is MANDATORY to match the HTML context scraping environment.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("THE HAMBURGER TRAP (MOST COMMON FAILURE):");
        promptBuilder.AppendLine("  • Elements like wixui-hamburger-open-button, .nav-toggle, .navbar-toggler, or [aria-label='Menu']");
        promptBuilder.AppendLine("    EXIST in the DOM but are CSS-HIDDEN (display: none) on Desktop at 1920px.");
        promptBuilder.AppendLine("  • Desktop Rule: NEVER assert visibility on mobile menu buttons.");
        promptBuilder.AppendLine("  • Desktop Rule: Assert visibility of the EXPANDED nav links (e.g., 'Home', 'About', 'Contact') instead.");
        promptBuilder.AppendLine("  • Mobile Rule: If Gherkin asks to test mobile/responsive views, YOU MUST SKIP THE TEST:");
        promptBuilder.AppendLine("    Example: it.skip('Test name - Responsive testing not supported in desktop pipeline', () => { ... });");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("DYNAMIC HYDRATION (Wix, React, Vue, Next.js):");
        promptBuilder.AppendLine("  • Modern sites often re-render DOM elements during page load (hydration).");
        promptBuilder.AppendLine("  • Chained commands can fail with 'subject detached from DOM' if elements re-render between commands.");
        promptBuilder.AppendLine("  • BAD (Chained): cy.get('nav').filter(':visible').find('a') - If nav re-renders, this fails.");
        promptBuilder.AppendLine("  • GOOD (Merged): cy.get('nav a').filter(':visible') or cy.get('nav:visible a:visible')");
        promptBuilder.AppendLine("  • Merged selectors retry the full query automatically, avoiding detached DOM errors.");
        promptBuilder.AppendLine("  • RULE: Avoid .find() immediately after obtaining a generic container.");
        promptBuilder.AppendLine("  • RULE: Combine CSS selectors into a single cy.get() string where possible.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("VISIBILITY ANNOTATIONS:");
        promptBuilder.AppendLine("  • HTML Context includes data-synta-visible='true' or data-synta-visible='false' attributes.");
        promptBuilder.AppendLine("  • data-synta-visible='true' means element is VISIBLE (width/height > 0, opacity > 0, display != none).");
        promptBuilder.AppendLine("  • data-synta-visible='false' means element is HIDDEN via CSS at 1920x1080 resolution.");
        promptBuilder.AppendLine("  • DO NOT interact with elements marked data-synta-visible='false'.");
        promptBuilder.AppendLine("  • If testing requires interacting with hidden elements:");
        promptBuilder.AppendLine("    1. First trigger the action that reveals them (click menu button, open accordion, etc.).");
        promptBuilder.AppendLine("    2. Then interact with the now-visible elements.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("SITE CONTAINER CHECK:");
        promptBuilder.AppendLine("  • Do NOT just assert cy.get('body') for page load verification.");
        promptBuilder.AppendLine("  • Assert the main site container from the UI Map (e.g., #SITE_CONTAINER, main, [data-testid=\"app\"]).");
        promptBuilder.AppendLine("  • This ensures the framework (React/Vue/Wix) has fully hydrated before running tests.");
        promptBuilder.AppendLine("  • Example: cy.get('#SITE_CONTAINER').should('be.visible'); // From UI Map");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Protocol 2: Selector Law - Handles ALL DOM interaction, UI Map usage, and text matching
    /// </summary>
    private static void AppendSelectorProtocol(StringBuilder promptBuilder, bool hasUiElementMap)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("### 2. SELECTOR PROTOCOL (SOURCE OF TRUTH)");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();

        if (!hasUiElementMap)
        {
            promptBuilder.AppendLine("UI MAP STATUS: DISABLED");
            promptBuilder.AppendLine("  • The UI ELEMENT MAP is not provided for this generation.");
            promptBuilder.AppendLine("  • Build selectors using stable attributes in this order of preference:");
            promptBuilder.AppendLine("    1. data-testid / data-cy / data-test / data-qa");
            promptBuilder.AppendLine("    2. id");
            promptBuilder.AppendLine("    3. name / aria-label / role + accessible name");
            promptBuilder.AppendLine("    4. text-based fallback only when text appears in provided context");
            promptBuilder.AppendLine("  • Avoid brittle selectors based on deep DOM hierarchy or style classes.");
            promptBuilder.AppendLine("  • Always assert visibility before click/type interactions.");
            promptBuilder.AppendLine();
            return;
        }
        promptBuilder.AppendLine("UI MAP LAW (ABSOLUTE REQUIREMENT):");
        promptBuilder.AppendLine("  • Use ONLY selectors from the provided UI ELEMENT MAP.");
        promptBuilder.AppendLine("  • The HTML Context below includes a 'UI ELEMENT MAP' section.");
        promptBuilder.AppendLine("  • This map contains ONLY visible, interactable elements at 1920x1080 resolution.");
        promptBuilder.AppendLine("  • Each element has a RECOMMENDED SELECTOR - USE IT DIRECTLY, do not invent your own.");
        promptBuilder.AppendLine("  • Elements are grouped by SEMANTIC REGION (header, nav, main, form, footer).");
        promptBuilder.AppendLine("  • Stability Score indicates selector reliability: 100=best (data-testid), 30=worst (fallback).");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("NO HALLUCINATIONS (CRITICAL RULE):");
        promptBuilder.AppendLine("  • If Gherkin says 'Click Login' but the Map has no 'Login' button, look for 'Sign In'.");
        promptBuilder.AppendLine("  • If nothing matches, SKIP the test with:");
        promptBuilder.AppendLine("    it.skip('Test name - Element \"Login\" not found in UI Map', () => { ... });");
        promptBuilder.AppendLine("  • DO NOT invent selectors like .login-btn, #login-button, or cy.contains('Login').");
        promptBuilder.AppendLine("  • If you invent a selector, the test WILL FAIL at runtime.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("SCOPE RULE - NO ASSUMED HTML STRUCTURE:");
        promptBuilder.AppendLine("  • Do NOT assume HTML structure (e.g., ul > li, nav > a, form > input).");
        promptBuilder.AppendLine("  • Use the flat attributes from the Map (data-testid, id, aria-label, class).");
        promptBuilder.AppendLine("  • WRONG: cy.get('header nav a') - You assumed header contains nav which contains a.");
        promptBuilder.AppendLine("  • CORRECT: cy.get('[data-testid=\"nav-link\"]').filter(':visible') - From UI Map.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("NO SELECTOR SPLITTING / COMBINING ATTRIBUTES (CRITICAL):");
        promptBuilder.AppendLine("  • DO NOT split a selector into parent-child relationships unless explicitly shown in the HTML structure.");
        promptBuilder.AppendLine("  • NEVER split a complete selector from the UI Map into parent-child relationships.");
        promptBuilder.AppendLine("  • Example: If Map shows cy.get('button#submit-btn'), use it EXACTLY as shown.");
        promptBuilder.AppendLine("  • WRONG: cy.get('#my-id').find('h3') - Assumes h3 is a child of my-id (parent-child hallucination).");
        promptBuilder.AppendLine("  • CORRECT: cy.get('h3#my-id') - Target the element directly (the h3 HAS that ID).");
        promptBuilder.AppendLine("  • When you see both a tag and an ID (e.g., 'h3#page-title'), they refer to the SAME element.");
        promptBuilder.AppendLine("  • The ID belongs TO that specific tag, not to a parent container.");
        promptBuilder.AppendLine("  • Use the Recommended Selector from the UI Map verbatim.");
        promptBuilder.AppendLine("  • DO NOT use .find(), .children(), or .parent() to navigate from Map selectors.");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("TEXT MATCHING RULES:");
        promptBuilder.AppendLine("  • When Gherkin steps reference text (e.g., 'Click on \"Programs and Services\"'):");
        promptBuilder.AppendLine("    1. Check the 'TEXT-TO-SELECTOR LOOKUP' section in the HTML Context.");
        promptBuilder.AppendLine("    2. If exact text is NOT found, look for synonyms or related text.");
        promptBuilder.AppendLine("    3. Example: If Gherkin says 'Programs and Services' but context has 'Our Programs', use 'Our Programs'.");
        promptBuilder.AppendLine("    4. If NO matching or similar text exists, do NOT use cy.contains().");
        promptBuilder.AppendLine("    5. CRITICAL: NEVER generate cy.contains('Text') if 'Text' does not appear anywhere in the HTML Context.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("VISIBILITY FILTER (USE WITH CAUTION):");
        promptBuilder.AppendLine("  • ONLY use .filter(':visible') when you intend to CLICK, TYPE, or interact with the element.");
        promptBuilder.AppendLine("  • For existence assertions (should('exist')), DO NOT filter by visibility.");
        promptBuilder.AppendLine("  • Responsive sites (Wix, WordPress) may have duplicate DOM nodes (one hidden mobile, one visible desktop).");
        promptBuilder.AppendLine("  • When clicking: cy.get('[data-testid=\"nav\"]').filter(':visible').click()");
        promptBuilder.AppendLine("  • When asserting existence: cy.get('[data-testid=\"nav\"]').should('exist') - NO .filter()");
        promptBuilder.AppendLine("  • IMPORTANT: For NEGATIVE assertions (should('not.exist')), DO NOT use .filter() - see Negative Testing Protocol.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("FORBIDDEN SELECTORS:");
        promptBuilder.AppendLine("  • NO bare tag selectors: cy.get('h5'), cy.get('header'), cy.get('main')");
        promptBuilder.AppendLine("  • NO constructed selectors: cy.get('header[data-testid=\"container\"] img')");
        promptBuilder.AppendLine("  • NO parent-child assumptions: cy.get('nav a'), cy.get('ul li')");
        promptBuilder.AppendLine("  • NO invented CSS classes: cy.get('.site-navigation'), cy.get('.nav-menu')");
        promptBuilder.AppendLine("  • NO splitting Map selectors with .find(): cy.get('#id').find('tag') - Use cy.get('tag#id') instead");
        promptBuilder.AppendLine("  • Use ONLY selectors from the UI ELEMENT MAP.");
        promptBuilder.AppendLine();

        promptBuilder.AppendLine("INTERACTION SAFETY:");
        promptBuilder.AppendLine("  • Always use .should('be.visible') before every .click() or .type().");
        promptBuilder.AppendLine("  • Always use .first() before .click() when selector might match multiple elements.");
        promptBuilder.AppendLine("  • Example: cy.get('[data-testid=\"linkElement\"]').filter(':visible').first().click()");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Adds a concise input profile so the model knows exactly which extraction inputs are present.
    /// </summary>
    private static void AppendExtractionInputProfile(
        StringBuilder promptBuilder,
        bool hasPageMetadata,
        bool hasUiElementMap,
        bool hasAccessibilityTree,
        bool hasHtmlContext,
        bool hasScreenshot)
    {
        promptBuilder.AppendLine("EXTRACTION INPUT PROFILE:");
        promptBuilder.AppendLine($"  • HTML Context: {(hasHtmlContext ? "ENABLED" : "DISABLED")}");
        promptBuilder.AppendLine($"  • Page Metadata: {(hasPageMetadata ? "ENABLED" : "DISABLED")}");
        promptBuilder.AppendLine($"  • UI Element Map: {(hasUiElementMap ? "ENABLED" : "DISABLED")}");
        promptBuilder.AppendLine($"  • Accessibility Tree: {(hasAccessibilityTree ? "ENABLED" : "DISABLED")}");
        promptBuilder.AppendLine($"  • Screenshot Context: {(hasScreenshot ? "ENABLED" : "DISABLED")}");
        promptBuilder.AppendLine("  • Use only the inputs marked ENABLED above.");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Protocol 3: Negative Testing - CRITICAL FIX for 404/500 and Element Absence Scenarios
    /// </summary>
    private static void AppendNegativeTestingProtocol(StringBuilder promptBuilder)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("### 3. NEGATIVE TESTING PROTOCOL (LIVE VS. MOCKED STATES)");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("THE PARADOX:");
        promptBuilder.AppendLine("  • You CANNOT filter an element that doesn't exist.");
        promptBuilder.AppendLine("  • Cypress .get() retries finding the element for 4 seconds by default.");
        promptBuilder.AppendLine("  • If you add .filter(':visible') before .should('not.exist'), Cypress will:");
        promptBuilder.AppendLine("    1. Try to find the element (retry for 4 seconds).");
        promptBuilder.AppendLine("    2. Try to filter it by visibility (requires element to exist first).");
        promptBuilder.AppendLine("    3. Then check if it doesn't exist (contradiction).");
        promptBuilder.AppendLine("  • This causes TIMEOUT failures.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("ASSERTION SAFETY (NEVER USE .filter() WITH not.exist):");
        promptBuilder.AppendLine("  • NEVER use .filter(':visible') or .find() when checking should('not.exist').");
        promptBuilder.AppendLine("  • WRONG: cy.get(selector).filter(':visible').should('not.exist')");
        promptBuilder.AppendLine("    Reason: Cypress retries finding the element to filter it, but it doesn't exist.");
        promptBuilder.AppendLine("  • WRONG: cy.contains(text).filter(':visible').should('not.exist')");
        promptBuilder.AppendLine("    Reason: Same issue - cannot filter something that doesn't exist.");
        promptBuilder.AppendLine("  • WRONG: cy.get('nav').find('a').should('not.exist')");
        promptBuilder.AppendLine("    Reason: .find() requires the parent to exist and searches within it.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("CORRECT PATTERN (USE BASE SELECTOR ONLY):");
        promptBuilder.AppendLine("  • CORRECT: cy.get(selector).should('not.exist')");
        promptBuilder.AppendLine("    Reason: Passes immediately if element is missing from the DOM.");
        promptBuilder.AppendLine("  • CORRECT: cy.contains(text).should('not.exist')");
        promptBuilder.AppendLine("    Reason: No filtering, direct assertion.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("FALSE POSITIVES (THE LIVE SITE PROBLEM):");
        promptBuilder.AppendLine("  • If you visit a LIVE site and check should('not.exist') on an element that is clearly in the UI Map,");
        promptBuilder.AppendLine("    the test WILL FAIL because the element IS there.");
        promptBuilder.AppendLine("  • THE LOGIC RULE: Only use should('not.exist') if:");
        promptBuilder.AppendLine("    1. You have ACTIVELY MOCKED the response to remove that element (via cy.intercept).");
        promptBuilder.AppendLine("    2. You are on an ERROR PAGE (404/500) where the element is NATURALLY ABSENT.");
        promptBuilder.AppendLine("  • If Gherkin says 'the menu is missing' but the UI Map shows the menu exists,");
        promptBuilder.AppendLine("    you MUST use the Simulation Protocol to HIDE it via cy.intercept before asserting not.exist.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("THE RULE:");
        promptBuilder.AppendLine("  • When asserting should('not.exist'), DO NOT use:");
        promptBuilder.AppendLine("    - .filter()");
        promptBuilder.AppendLine("    - .find()");
        promptBuilder.AppendLine("    - .contains() chained after .get()");
        promptBuilder.AppendLine("    - Any other intermediate query commands");
        promptBuilder.AppendLine("  • Target the base selector ONLY.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("ERROR PAGE TESTING (404/500 SCENARIOS):");
        promptBuilder.AppendLine("  • When testing 404 or 500 error pages:");
        promptBuilder.AppendLine("    1. Use cy.intercept to mock the error response with { statusCode: 404 } or { statusCode: 500 }.");
        promptBuilder.AppendLine("    2. Use cy.visit(url, { failOnStatusCode: false }) to prevent Cypress from failing.");
        promptBuilder.AppendLine("    3. CRITICAL: Add cy.on('uncaught:exception', () => false); inside the test to prevent app crashes.");
        promptBuilder.AppendLine("    4. Assert that key HomePage elements (like Hero section, Main nav) should('not.exist') using BASE selectors.");
        promptBuilder.AppendLine("  • When testing error pages via cy.intercept, the app may crash and throw JavaScript errors.");
        promptBuilder.AppendLine("  • You MUST include: cy.on('uncaught:exception', () => false); at the start of the test to suppress these.");
        promptBuilder.AppendLine("  • Example:");
        promptBuilder.AppendLine("    cy.on('uncaught:exception', () => false); // Prevent app crash from killing test");
        promptBuilder.AppendLine("    cy.intercept('GET', targetUrl, { statusCode: 404, body: '<html><h1>Not Found</h1></html>' }).as('error');");
        promptBuilder.AppendLine("    cy.visit(targetUrl, { failOnStatusCode: false });");
        promptBuilder.AppendLine("    cy.get('[data-testid=\"hero-section\"]').should('not.exist'); // NO .filter(':visible')!");
        promptBuilder.AppendLine("    cy.contains('Welcome Home').should('not.exist'); // NO .filter(':visible')!");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("VISIBILITY VS. EXISTENCE:");
        promptBuilder.AppendLine("  • should('not.be.visible') - Element exists in DOM but is hidden. DO NOT use .filter(':visible') - it's redundant.");
        promptBuilder.AppendLine("  • should('not.exist') - Element is NOT in the DOM. DO NOT use .filter(':visible').");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Protocol 4: Network Law - Handles ALL Intercepts, API mocking, and cy.request logic
    /// </summary>
    private static void AppendNetworkProtocol(StringBuilder promptBuilder)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("### 4. NETWORK PROTOCOL (TRAFFIC CONTROL)");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("ORDERING (CRITICAL - WILL TIMEOUT IF WRONG):");
        promptBuilder.AppendLine("  • cy.intercept MUST be defined BEFORE the action (click/visit) that triggers it.");
        promptBuilder.AppendLine("  • If you define the intercept AFTER the action, the request happens before the listener is active.");
        promptBuilder.AppendLine("  • This causes cy.wait('@alias') to timeout indefinitely.");
        promptBuilder.AppendLine("  • CORRECT Pattern:");
        promptBuilder.AppendLine("    cy.intercept('POST', '/api/login').as('loginRequest');");
        promptBuilder.AppendLine("    cy.get('#submit-btn').click(); // Action triggers request");
        promptBuilder.AppendLine("    cy.wait('@loginRequest'); // Wait for request that was intercepted");
        promptBuilder.AppendLine("  • WRONG Pattern:");
        promptBuilder.AppendLine("    cy.get('#submit-btn').click(); // Action triggers request BEFORE intercept is set up");
        promptBuilder.AppendLine("    cy.intercept('POST', '/api/login').as('loginRequest'); // TOO LATE!");
        promptBuilder.AppendLine("    cy.wait('@loginRequest'); // WILL TIMEOUT - request already happened!");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("ANTI-HALLUCINATION (NETWORK ERROR SYNTAX):");
        promptBuilder.AppendLine("  • NEVER use: cy.intercept({ networkError: true }) - THIS COMMAND DOES NOT EXIST.");
        promptBuilder.AppendLine("  • NEVER use: cy.intercept({ forceNetworkError: true }) - INVALID ARGUMENT STRUCTURE.");
        promptBuilder.AppendLine("  • ALWAYS use: cy.intercept('**', { forceNetworkError: true }).as('netError')");
        promptBuilder.AppendLine("  • The error config object MUST be the SECOND argument.");
        promptBuilder.AppendLine("  • You MUST provide a URL matcher (like '**') as the FIRST argument.");
        promptBuilder.AppendLine("  • Example:");
        promptBuilder.AppendLine("    cy.intercept('**', { forceNetworkError: true }).as('networkFailure');");
        promptBuilder.AppendLine("    cy.visit(url, { failOnStatusCode: false });");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("STATUS CODES (404/500 TESTING):");
        promptBuilder.AppendLine("  • Default Cypress behavior: cy.visit() and cy.request() FAIL immediately if status is not 2xx/3xx.");
        promptBuilder.AppendLine("  • ALWAYS use { failOnStatusCode: false } when testing error responses.");
        promptBuilder.AppendLine("  • Example with cy.request:");
        promptBuilder.AppendLine("    cy.request({ url: '/bad-url', failOnStatusCode: false }).its('status').should('eq', 404)");
        promptBuilder.AppendLine("  • Example with cy.visit:");
        promptBuilder.AppendLine("    cy.intercept('GET', targetUrl, {");
        promptBuilder.AppendLine("      statusCode: 500,");
        promptBuilder.AppendLine("      body: '<html><body><h1>Server Error</h1></body></html>',");
        promptBuilder.AppendLine("      headers: { 'content-type': 'text/html' }");
        promptBuilder.AppendLine("    }).as('serverError');");
        promptBuilder.AppendLine("    cy.visit(targetUrl, { failOnStatusCode: false });");
        promptBuilder.AppendLine("    cy.wait('@serverError').its('response.statusCode').should('eq', 500);");
        promptBuilder.AppendLine("  • CRITICAL: Always include headers: { 'content-type': 'text/html' } in intercept response.");
        promptBuilder.AppendLine("  • Without content-type header, Cypress fails with 'content-type undefined' error.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("CY.VISIT() RETURN VALUE:");
        promptBuilder.AppendLine("  • cy.visit() does NOT return a response object.");
        promptBuilder.AppendLine("  • WRONG: cy.visit(url).then((response) => { expect(response.status)... }) - response is NOT available.");
        promptBuilder.AppendLine("  • CORRECT: Use cy.wait('@alias') after intercept to access the response.");
        promptBuilder.AppendLine("  • Example: cy.wait('@serverError').its('response.statusCode').should('eq', 500);");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("NO INVENTED API ENDPOINTS:");
        promptBuilder.AppendLine("  • Do NOT invent API URLs to intercept.");
        promptBuilder.AppendLine("  • WRONG: cy.intercept('**/api/legal/v1/user/widgets/**') - You invented this URL.");
        promptBuilder.AppendLine("  • Only intercept URLs that are EXPLICITLY mentioned in Gherkin or visible in HTML context.");
        promptBuilder.AppendLine("  • If no API endpoints are provided, DO NOT use cy.intercept() for API calls.");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Protocol 5: Simulation & State - Bridges the gap between Gherkin requirements and Live URL reality
    /// </summary>
    private static void AppendSimulationProtocol(StringBuilder promptBuilder)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("### 6. SIMULATION & STATE PROTOCOL (BRIDGING GHERKIN TO REALITY)");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("THE REALITY GAP:");
        promptBuilder.AppendLine("  • Gherkin scenarios often describe 'broken' states (e.g., 'Given the menu is missing', 'Given a JS error occurs').");
        promptBuilder.AppendLine("  • Since you are testing a LIVE URL, these elements ARE present and the site works correctly.");
        promptBuilder.AppendLine("  • Your job is to MAKE the site match the Gherkin requirement by SIMULATING the broken state.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("ACTIVE SIMULATION RULE:");
        promptBuilder.AppendLine("  • If Gherkin defines a state where an element is MISSING or HIDDEN,");
        promptBuilder.AppendLine("    but the UI Map shows it is PRESENT,");
        promptBuilder.AppendLine("    you MUST use cy.intercept to 'break' the page to match the Gherkin.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("HOW TO HIDE ELEMENTS VIA INTERCEPT:");
        promptBuilder.AppendLine("  • Use cy.intercept to modify the HTML response and hide the element with CSS:");
        promptBuilder.AppendLine("    cy.intercept('GET', targetUrl, (req) => {");
        promptBuilder.AppendLine("      req.continue((res) => {");
        promptBuilder.AppendLine("        // Replace the element's ID or class with inline style to hide it");
        promptBuilder.AppendLine("        res.body = res.body.replace('id=\"main-nav\"', 'id=\"main-nav\" style=\"display:none\"');");
        promptBuilder.AppendLine("        // Or replace a specific selector pattern");
        promptBuilder.AppendLine("        res.body = res.body.replace('<nav', '<nav style=\"display:none\"');");
        promptBuilder.AppendLine("      });");
        promptBuilder.AppendLine("    }).as('modifiedPage');");
        promptBuilder.AppendLine("    cy.visit(targetUrl);");
        promptBuilder.AppendLine("    cy.wait('@modifiedPage');");
        promptBuilder.AppendLine("    cy.get('[data-testid=\"main-nav\"]').should('not.exist'); // Now it's truly hidden");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("HOW TO BREAK ELEMENTS (MISSING TEXT, BROKEN IMAGES):");
        promptBuilder.AppendLine("  • If Gherkin says 'Given the logo image is broken':");
        promptBuilder.AppendLine("    cy.intercept('GET', '**/logo.png', { statusCode: 404 }).as('brokenLogo');");
        promptBuilder.AppendLine("  • If Gherkin says 'Given the page title is empty':");
        promptBuilder.AppendLine("    cy.intercept('GET', targetUrl, (req) => {");
        promptBuilder.AppendLine("      req.continue((res) => {");
        promptBuilder.AppendLine("        res.body = res.body.replace(/<title>.*?<\\/title>/, '<title></title>');");
        promptBuilder.AppendLine("      });");
        promptBuilder.AppendLine("    }).as('emptyTitle');");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("SKIP IF IMPOSSIBLE:");
        promptBuilder.AppendLine("  • If you cannot logically simulate the 'missing' state via intercept or CSS injection,");
        promptBuilder.AppendLine("    YOU MUST use it.skip() and explain:");
        promptBuilder.AppendLine("    it.skip('Test name - Cannot simulate missing static element on live site without DOM mutation', () => { ... });");
        promptBuilder.AppendLine("  • Example scenarios where skipping is valid:");
        promptBuilder.AppendLine("    - 'Given the entire HTML is corrupt' (too complex to mock)");
        promptBuilder.AppendLine("    - 'Given the CSS file is missing' (would break the entire test)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("REMEMBER:");
        promptBuilder.AppendLine("  • Gherkin is the REQUIREMENT.");
        promptBuilder.AppendLine("  • The Website is the TARGET.");
        promptBuilder.AppendLine("  • It is YOUR JOB to make the TARGET behave like the REQUIREMENT.");
        promptBuilder.AppendLine("  • Use cy.intercept creatively to simulate broken states, missing elements, and error conditions.");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Protocol 6: Runtime Safety - Handles ALL Exception handling, Window access, and Error Injection
    /// </summary>
    private static void AppendRuntimeSafetyProtocol(StringBuilder promptBuilder)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("### 7. RUNTIME SAFETY PROTOCOL (DEFENSIVE CODING & ERROR INJECTION)");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("JS EXCEPTIONS (UNHANDLED ERRORS):");
        promptBuilder.AppendLine("  • If testing for 'Unhandled JS Errors', 'Console Errors', or 'Application Crashes':");
        promptBuilder.AppendLine("  • You MUST disable the global fail-safe.");
        promptBuilder.AppendLine("  • Add this listener at the start of the test (inside beforeEach or at the top of it() block):");
        promptBuilder.AppendLine("    cy.on('uncaught:exception', (err, runnable) => { return false; });");
        promptBuilder.AppendLine("  • This prevents Cypress from failing the test when the application throws an error.");
        promptBuilder.AppendLine("  • If Gherkin mentions 'Unhandled JS Errors', 'Console Errors', or 'Application Crash', use this pattern.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("JS ERROR INJECTION (CRITICAL - SIMULATING CRASHES):");
        promptBuilder.AppendLine("  • If Gherkin says 'Given a JS error occurs' and you are on a LIVE site,");
        promptBuilder.AppendLine("    you MUST manually trigger the error.");
        promptBuilder.AppendLine("  • DO NOT wait for the site to crash on its own - it won't.");
        promptBuilder.AppendLine("  • Use cy.window() to inject the error:");
        promptBuilder.AppendLine("    cy.window().then((win) => {");
        promptBuilder.AppendLine("      setTimeout(() => {");
        promptBuilder.AppendLine("        win.eval('throw new Error(\"Simulated Crash\");');");
        promptBuilder.AppendLine("      }, 500);");
        promptBuilder.AppendLine("    });");
        promptBuilder.AppendLine("  • Alternative - trigger via ErrorEvent:");
        promptBuilder.AppendLine("    cy.window().then((win) => {");
        promptBuilder.AppendLine("      win.dispatchEvent(new ErrorEvent('error', {");
        promptBuilder.AppendLine("        message: 'Simulated Error',");
        promptBuilder.AppendLine("        error: new Error('Test Error')");
        promptBuilder.AppendLine("      }));");
        promptBuilder.AppendLine("    });");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("STUBBING STRATEGY (CONSOLE ERROR CHECKING):");
        promptBuilder.AppendLine("  • When checking for console.error, always use cy.spy() or cy.stub():");
        promptBuilder.AppendLine("    cy.window().then((win) => {");
        promptBuilder.AppendLine("      cy.spy(win.console, 'error').as('consoleError');");
        promptBuilder.AppendLine("    });");
        promptBuilder.AppendLine("    // Later in the test:");
        promptBuilder.AppendLine("    cy.get('@consoleError').should('be.called');");
        promptBuilder.AppendLine("  • DO NOT use win.console.getEntries() - it DOES NOT EXIST.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("WINDOW OBJECT ACCESS:");
        promptBuilder.AppendLine("  • DO NOT use win.eval() in production code - it may be blocked by CSP (Content Security Policy).");
        promptBuilder.AppendLine("  • WRONG: cy.window().then(win => { win.eval('malicious code') }) <-- BLOCKED BY CSP");
        promptBuilder.AppendLine("  • WRONG: cy.window().then(win => { win.console.getEntries() }) <-- DOES NOT EXIST");
        promptBuilder.AppendLine("  • CORRECT: Use cy.spy(console, 'error') or cy.stub(console, 'error').");
        promptBuilder.AppendLine("  • CORRECT: For error injection, use win.dispatchEvent(new ErrorEvent(...))");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("WAIT STRATEGY:");
        promptBuilder.AppendLine("  • DO NOT use hard waits (cy.wait(5000)).");
        promptBuilder.AppendLine("  • WRONG: cy.wait(2000) - arbitrary delays slow tests and are unreliable.");
        promptBuilder.AppendLine("  • CORRECT: cy.wait('@aliasName') - wait for specific network request.");
        promptBuilder.AppendLine("  • CORRECT: cy.get(selector).should('be.visible') - Cypress auto-retries until visible.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("FORBIDDEN COMMANDS (THESE DO NOT EXIST - HALLUCINATIONS):");
        promptBuilder.AppendLine("  • cy.setNetwork() - DOES NOT EXIST");
        promptBuilder.AppendLine("  • win.console.getEntries() - DOES NOT EXIST");
        promptBuilder.AppendLine("  • cy.throttle() - DOES NOT EXIST");
        promptBuilder.AppendLine("  • cy.offline() - DOES NOT EXIST");
        promptBuilder.AppendLine("  • cy.win() - DOES NOT EXIST (use cy.window() instead)");
        promptBuilder.AppendLine("  • .tab() - DOES NOT EXIST (there is no .tab() method in Cypress)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("MISSING FEATURES (WHEN TO SKIP TESTS):");
        promptBuilder.AppendLine("  • If a scenario requires Tab key navigation:");
        promptBuilder.AppendLine("    it.skip('Test name - Tab key navigation not supported natively', () => { ... });");
        promptBuilder.AppendLine("  • If a scenario requires specific performance timing:");
        promptBuilder.AppendLine("    it.skip('Test name - Performance timing not supported in Cypress', () => { ... });");
        promptBuilder.AppendLine("  • If a scenario requires offline mode via cy.visit():");
        promptBuilder.AppendLine("    it.skip('Test name - Offline testing not supported with cy.visit()', () => { ... });");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("NO EXTERNAL PLUGINS:");
        promptBuilder.AppendLine("  • Do NOT use cypress-real-events, realPress, realClick, or xpath plugins.");
        promptBuilder.AppendLine("  • Use ONLY standard Cypress commands:");
        promptBuilder.AppendLine("    cy.get, cy.contains, cy.click, cy.type, cy.should, cy.intercept, cy.visit, cy.wait, cy.window, cy.wrap");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("NO EXTERNAL IMPORTS:");
        promptBuilder.AppendLine("  • Do NOT add 'import { expect } from \"chai\"' or any other imports.");
        promptBuilder.AppendLine("  • Cypress bundles Chai globally - expect() is available without importing.");
        promptBuilder.AppendLine("  • Adding import statements causes webpack compilation errors.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("NO EMPTY LOGS (PROPER TEST SKIPPING):");
        promptBuilder.AppendLine("  • Do NOT just cy.log a skipped test - it will show as PASSED (false positive).");
        promptBuilder.AppendLine("  • WRONG: it('test name', () => { cy.log('skipping...'); }) - shows as PASS.");
        promptBuilder.AppendLine("  • CORRECT: it.skip('test name - reason for skip', () => { ... }) - shows as SKIPPED.");
        promptBuilder.AppendLine("  • Every test MUST have at least one meaningful assertion.");
        promptBuilder.AppendLine("  • WRONG: it('should click button', () => { cy.get('btn').click(); }) - no assertion.");
        promptBuilder.AppendLine("  • Be HONEST: A test that can't verify its scenario should be skipped, not faked.");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Appends HTML Context with UI Element Map
    /// </summary>
    private static void AppendHtmlContext(StringBuilder promptBuilder, string htmlContext)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("HTML CONTEXT & UI ELEMENT MAP");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("IMPORTANT: Read the UI ELEMENT MAP section FIRST. It contains pre-validated selectors with stability scores.");
        promptBuilder.AppendLine("The UI ELEMENT MAP provides:");
        promptBuilder.AppendLine("  - RECOMMENDED SELECTORS: Copy and use these EXACTLY - they are verified to exist");
        promptBuilder.AppendLine("  - STABILITY SCORES: Higher = more reliable (100=data-testid, 90=id, 85=aria-label, etc.)");
        promptBuilder.AppendLine("  - SEMANTIC REGIONS: Elements grouped by page region (header, nav, main, form, footer)");
        promptBuilder.AppendLine("  - FORM LABELS: Input fields with their associated label text");
        promptBuilder.AppendLine("  - TEXT-TO-SELECTOR LOOKUP: Quick reference to find elements by visible text");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("WARNING: Elements marked data-synta-visible='false' are HIDDEN. Do NOT generate selectors for them.");
        promptBuilder.AppendLine("CRITICAL: Do NOT construct selectors like 'header nav a' - use ONLY selectors from the map.");
        promptBuilder.AppendLine("CRITICAL: If an element is not in the UI ELEMENT MAP, it does NOT exist on the page.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(htmlContext);
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Appends visual context instructions for screenshot-based prompt enhancement
    /// </summary>
    private static void AppendVisualContextInstructions(StringBuilder promptBuilder)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("VISUAL CONTEXT INSTRUCTIONS (SCREENSHOT PROVIDED)");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("A screenshot of the target page has been provided. You MUST use this image to:");
        promptBuilder.AppendLine("1. **Disambiguate Selectors:** If the HTML map shows multiple similar elements (e.g., multiple \"Login\" buttons),");
        promptBuilder.AppendLine("   use the screenshot to identify the one that is primary and visible.");
        promptBuilder.AppendLine("2. **Visibility Check:** Do not generate code for elements that are present in the DOM but are clearly obscured,");
        promptBuilder.AppendLine("   hidden behind overlays, or rendered outside the main viewport in the screenshot.");
        promptBuilder.AppendLine("3. **Mapping Gherkin to Reality:** If Gherkin mentions \"the sidebar\" or \"the header,\" verify their visual presence");
        promptBuilder.AppendLine("   and location on the screen before selecting the code.");
        promptBuilder.AppendLine();
    }

    /// <summary>
    /// Appends output format requirements (TypeScript vs JavaScript)
    /// </summary>
    private static void AppendOutputFormatRequirements(
        StringBuilder promptBuilder,
        string languageName,
        string fileExtension,
        bool isTypeScript)
    {
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine("OUTPUT FORMAT REQUIREMENTS");
        promptBuilder.AppendLine("═══════════════════════════════════════════════════════════════════════════════");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"  • Return ONLY valid {languageName} code that can be saved directly as a {fileExtension} file.");
        promptBuilder.AppendLine("  • Do NOT include any explanations, markdown code blocks, or prose.");
        promptBuilder.AppendLine("  • Do NOT include raw Gherkin syntax (Scenario:, Given, When, Then, And, But) as code statements.");
        promptBuilder.AppendLine("  • The Gherkin steps should ONLY appear as comments inside the it() blocks, NOT as standalone code lines.");
        promptBuilder.AppendLine("  • WRONG: Scenario: User logs in (this is invalid JavaScript and causes 'Unexpected keyword or identifier' errors)");
        promptBuilder.AppendLine("  • CORRECT: it('User logs in', () => { // Given... When... Then... })");
        promptBuilder.AppendLine("  • Each Scenario becomes an it() block with the scenario name as the test description.");
        promptBuilder.AppendLine("  • Group related tests with describe blocks.");
        promptBuilder.AppendLine("  • Add descriptive test names from the scenario titles.");

        if (isTypeScript)
        {
            promptBuilder.AppendLine("  • Use TypeScript syntax with proper type annotations where beneficial.");
            promptBuilder.AppendLine("  • Do NOT add explicit type imports - Cypress provides types globally.");
        }
        else
        {
            promptBuilder.AppendLine("  • Use plain JavaScript syntax (ES6+) - no TypeScript-specific features.");
            promptBuilder.AppendLine("  • Do NOT use type annotations, interfaces, or other TypeScript-only syntax.");
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"Generate a complete Cypress test file ({fileExtension}):");
        promptBuilder.AppendLine();
    }
}
