namespace SynTA.Services.AI.Scripts;

/// <summary>
/// Static class containing JavaScript scripts used for HTML extraction and DOM processing.
/// These scripts are executed in the browser context via Playwright.
/// </summary>
public static class HtmlExtractionScripts
{
    /// <summary>
    /// Script for smart scrolling through the page to trigger lazy loading and hydration.
    /// This helps dynamic websites (React, Vue, Angular) load all content before extraction.
    /// Scrolls from top to bottom in viewport increments, then returns to top.
    /// </summary>
    public const string SmartScrollScript = @"async () => {
        // Get the total scrollable height
        const totalHeight = document.body.scrollHeight;
        const viewportHeight = window.innerHeight;
        
        // Scroll down in viewport increments
        for (let currentPosition = 0; currentPosition < totalHeight; currentPosition += viewportHeight) {
            window.scrollTo(0, currentPosition);
            
            // Increased delay to allow network requests and lazy-loaded content to render and hydrate
            // Many sites take 200-1000ms+ for network requests to complete
            await new Promise(resolve => setTimeout(resolve, 500));
        }
        
        // Scroll back to top
        window.scrollTo(0, 0);
    }";
    /// <summary>
    /// Script for identifying and annotating trigger-content relationships (menu buttons ? menus, accordions, modals, etc.).
    /// This helps the AI understand which visible elements control hidden content, solving the "visibility trap".
    /// Adds data-synta-controls and data-synta-controlled-by attributes to link triggers and their content.
    /// IMPORTANT: This should be called AFTER DomEnrichmentScript so visibility data is available.
    /// </summary>
    public const string TriggerRelationshipScript = @"() => {
        // Find all elements that control other elements via ARIA or common patterns
        function identifyTriggerRelationships() {
            const allElements = document.querySelectorAll('*');
            const relationships = [];
            
            allElements.forEach(trigger => {
                try {
                    // ARIA-based relationships
                    const controls = trigger.getAttribute('aria-controls');
                    const expanded = trigger.getAttribute('aria-expanded');
                    const haspopup = trigger.getAttribute('aria-haspopup');
                    
                    if (controls) {
                        // Element explicitly controls another element
                        const controlled = document.getElementById(controls);
                        if (controlled) {
                            trigger.setAttribute('data-synta-controls', controls);
                            controlled.setAttribute('data-synta-controlled-by', trigger.id || generateId(trigger));
                            relationships.push({
                                triggerId: trigger.id || generateId(trigger),
                                controlledId: controls,
                                type: haspopup || 'generic',
                                currentState: expanded
                            });
                        }
                    }
                    
                    // Common patterns: buttons/links that toggle visibility
                    const role = trigger.getAttribute('role');
                    const tag = trigger.tagName?.toLowerCase();
                    
                    if ((tag === 'button' || tag === 'a' || role === 'button') && (haspopup || expanded !== null)) {
                        // This is likely a menu/dropdown trigger
                        // Find nearby hidden menus/dropdowns
                        const parent = trigger.parentElement;
                        if (parent) {
                            // Look for sibling or child menus
                            const menuCandidates = parent.querySelectorAll('[role=""menu""], [role=""listbox""], [role=""dialog""], .menu, .dropdown, .popover');
                            menuCandidates.forEach(menu => {
                                const menuVisible = menu.getAttribute('data-synta-visible') === 'true';
                                if (!menuVisible || expanded === 'false') {
                                    const triggerId = trigger.id || generateId(trigger);
                                    const menuId = menu.id || generateId(menu);
                                    trigger.setAttribute('data-synta-controls', menuId);
                                    menu.setAttribute('data-synta-controlled-by', triggerId);
                                    relationships.push({
                                        triggerId: triggerId,
                                        controlledId: menuId,
                                        type: haspopup || 'menu',
                                        currentState: expanded || 'collapsed'
                                    });
                                }
                            });
                        }
                    }
                    
                    // Pattern: Mobile menu buttons (hamburger menus)
                    const text = (trigger.innerText || '').toLowerCase();
                    const ariaLabel = (trigger.getAttribute('aria-label') || '').toLowerCase();
                    if ((text.includes('menu') || ariaLabel.includes('menu') || ariaLabel.includes('navigation')) && 
                        (tag === 'button' || role === 'button')) {
                        // Find associated nav/menu
                        const nav = document.querySelector('nav[data-synta-visible=""false""], [role=""navigation""][data-synta-visible=""false""]');
                        if (nav) {
                            const triggerId = trigger.id || generateId(trigger);
                            const navId = nav.id || generateId(nav);
                            trigger.setAttribute('data-synta-controls', navId);
                            trigger.setAttribute('data-synta-trigger-type', 'mobile-menu');
                            nav.setAttribute('data-synta-controlled-by', triggerId);
                            relationships.push({
                                triggerId: triggerId,
                                controlledId: navId,
                                type: 'mobile-menu',
                                currentState: 'collapsed'
                            });
                        }
                    }
                    
                    // Pattern: Profile/User menus
                    if ((text.includes('profile') || text.includes('account') || text.includes('user') || 
                         ariaLabel.includes('profile') || ariaLabel.includes('account') || ariaLabel.includes('user')) &&
                        (tag === 'button' || tag === 'a' || role === 'button')) {
                        // Find nearby dropdown menus
                        let searchRoot = trigger.parentElement;
                        for (let i = 0; i < 3 && searchRoot; i++) {
                            const dropdowns = searchRoot.querySelectorAll('[data-synta-visible=""false""][role=""menu""], [data-synta-visible=""false""].dropdown, [data-synta-visible=""false""].menu');
                            if (dropdowns.length > 0) {
                                const dropdown = dropdowns[0];
                                const triggerId = trigger.id || generateId(trigger);
                                const dropdownId = dropdown.id || generateId(dropdown);
                                trigger.setAttribute('data-synta-controls', dropdownId);
                                trigger.setAttribute('data-synta-trigger-type', 'profile-menu');
                                dropdown.setAttribute('data-synta-controlled-by', triggerId);
                                relationships.push({
                                    triggerId: triggerId,
                                    controlledId: dropdownId,
                                    type: 'profile-menu',
                                    currentState: 'collapsed'
                                });
                                break;
                            }
                            searchRoot = searchRoot.parentElement;
                        }
                    }
                    
                    // Pattern: Modal/Dialog triggers
                    if ((text.includes('open') || text.includes('show') || ariaLabel.includes('open') || ariaLabel.includes('show')) &&
                        (tag === 'button' || role === 'button')) {
                        // Look for hidden dialogs/modals
                        const modals = document.querySelectorAll('[role=""dialog""][data-synta-visible=""false""], .modal[data-synta-visible=""false""], [data-synta-visible=""false""][role=""alertdialog""]');
                        if (modals.length > 0) {
                            const modal = modals[0];
                            const triggerId = trigger.id || generateId(trigger);
                            const modalId = modal.id || generateId(modal);
                            trigger.setAttribute('data-synta-controls', modalId);
                            trigger.setAttribute('data-synta-trigger-type', 'modal');
                            modal.setAttribute('data-synta-controlled-by', triggerId);
                            relationships.push({
                                triggerId: triggerId,
                                controlledId: modalId,
                                type: 'modal',
                                currentState: 'closed'
                            });
                        }
                    }
                    
                } catch (e) {
                    // Skip elements that cause errors
                }
            });
            
            return relationships;
        }
        
        function generateId(element) {
            // CRITICAL FIX: Return existing ID if element has one
            // DO NOT set element.id - this causes selector mismatches at Cypress runtime
            // because the injected ID only exists in scraper memory, not on the live page
            if (element.id && element.id.trim() !== '') {
                return element.id;
            }
            
            // For internal tracking only - NOT assigned to element
            // Use a descriptive prefix for debugging
            const tag = element.tagName?.toLowerCase() || 'el';
            const timestamp = Date.now();
            const random = Math.floor(Math.random() * 1000);
            return 'internal-synta-ref-' + tag + '-' + timestamp + '-' + random;
        }
        
        return identifyTriggerRelationships();
    }";

    /// <summary>
    /// Script for enriching the DOM with visibility data by recursively traversing all elements including shadow DOM.
    /// This method handles Web Components and provides accurate visibility detection for modern web applications.
    /// Sets data-synta-visible='true' for visible elements, 'false' for hidden elements.
    /// </summary>
    public const string DomEnrichmentScript = @"() => {
        // Recursive function to traverse DOM including shadow roots and iframes
        function traverseAndMarkVisibility(root) {
            // Get all elements in the current root
            const elements = root.querySelectorAll('*');
            
            elements.forEach(element => {
                try {
                    // Skip hidden input elements (they're meant to be hidden)
                    if (element.tagName === 'INPUT' && element.getAttribute('type') === 'hidden') {
                        return;
                    }
                    
                    // Get computed style and bounding rect
                    const style = window.getComputedStyle(element);
                    const rect = element.getBoundingClientRect();
                    
                    // CYPRESS-ALIGNED visibility detection
                    // Matches Cypress :visible pseudo-selector behavior
                    const notDisplayNone = style.display !== 'none';
                    const notVisibilityHidden = style.visibility !== 'hidden';
                    const hasVisibleOpacity = parseFloat(style.opacity) !== 0; // Cypress treats opacity:0 as hidden
                    
                    // Cypress requires BOTH width AND height > 0 (not just one)
                    const hasPositiveDimensions = rect.width > 0 && rect.height > 0;
                    
                    // Check if element is positioned off-screen (common hide technique)
                    const isOffScreen = rect.right < 0 || rect.bottom < 0 || 
                                       rect.left > window.innerWidth || rect.top > window.innerHeight;
                    
                    // Special handling: SVG elements and icon fonts with 0x0 dimensions but visible parent
                    const isSvgOrIconFont = element.tagName === 'SVG' || element.tagName === 'I' || 
                                           element.classList.contains('icon') || element.classList.contains('fa');
                    const hasVisibleText = element.innerText && element.innerText.trim().length > 0;
                    
                    // Element is visible if:
                    // 1. CSS doesn't hide it (display, visibility, opacity) AND
                    // 2. Has positive dimensions OR is special element (SVG/icon/text) AND
                    // 3. Is NOT positioned off-screen
                    const isVisible = notDisplayNone && notVisibilityHidden && hasVisibleOpacity &&
                                     (hasPositiveDimensions || isSvgOrIconFont || hasVisibleText) &&
                                     !isOffScreen;

                    // Set visibility attribute
                    element.setAttribute('data-synta-visible', isVisible ? 'true' : 'false');
                    
                    // If element has shadow root, recurse into it
                    if (element.shadowRoot) {
                        traverseAndMarkVisibility(element.shadowRoot);
                    }
                    
                    // If element is an iframe, try to access its content document (same-origin only)
                    if (element.tagName === 'IFRAME') {
                        try {
                            const iframeDoc = element.contentDocument || element.contentWindow?.document;
                            if (iframeDoc && iframeDoc.body) {
                                traverseAndMarkVisibility(iframeDoc.body);
                            }
                        } catch (e) {
                            // Cross-origin iframe, skip
                        }
                    }
                } catch (e) {
                    // Skip elements that cause errors
                }
            });
        }
        
        // Start traversal from document body
        if (document.body) {
            traverseAndMarkVisibility(document.body);
        }
    }";

    /// <summary>
    /// Script for extracting page metadata including title, meta description, canonical URL, and other SEO/identity info.
    /// This helps the AI understand the page context and avoid hallucinating page titles.
    /// </summary>
    public const string PageMetadataScript = @"() => {
        return {
            title: document.title || '',
            metaDescription: document.querySelector('meta[name=""description""]')?.getAttribute('content') || '',
            canonicalUrl: document.querySelector('link[rel=""canonical""]')?.getAttribute('href') || '',
            ogTitle: document.querySelector('meta[property=""og:title""]')?.getAttribute('content') || '',
            ogDescription: document.querySelector('meta[property=""og:description""]')?.getAttribute('content') || '',
            h1Text: document.querySelector('h1')?.innerText?.trim()?.substring(0, 100) || '',
            language: document.documentElement?.lang || '',
            charset: document.characterSet || 'UTF-8'
        };
    }";

    /// <summary>
    /// Script for annotating elements with visibility markers without removing them from the DOM.
    /// This preserves the full page structure while informing the AI which elements are currently hidden.
    /// Hidden elements include mobile menus, collapsed accordions, and modals that aren't visible on page load.
    /// </summary>
    public const string VisibilityAnnotationScript = @"() => {
        // Iterate through all elements in the body
        const allElements = document.body.querySelectorAll('*');
        
        allElements.forEach(element => {
            try {
                // Skip hidden input elements (they're meant to be hidden)
                if (element.tagName === 'INPUT' && element.getAttribute('type') === 'hidden') {
                    return;
                }
                
                const style = window.getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                
                // Improved visibility detection - handles edge cases from resource blocking
                const hasNonzeroDimensions = rect.width > 0 || rect.height > 0;
                const hasVisibleOpacity = parseFloat(style.opacity) > 0.01;
                const notDisplayNone = style.display !== 'none';
                const notVisibilityHidden = style.visibility !== 'hidden';
                
                // Special handling: SVG elements, icon fonts, and elements with visible children
                const isSvgOrIconFont = element.tagName === 'SVG' || element.tagName === 'I' || 
                                       element.classList.contains('icon') || element.classList.contains('fa');
                const hasVisibleText = element.innerText && element.innerText.trim().length > 0;
                const hasPositioning = style.position === 'absolute' || style.position === 'fixed';
                
                // Check if element is hidden via CSS
                const isHidden = 
                    !notDisplayNone || 
                    !notVisibilityHidden || 
                    !hasVisibleOpacity || 
                    element.offsetParent === null || // Classic hidden check
                    (!hasNonzeroDimensions && !isSvgOrIconFont && !hasVisibleText && !hasPositioning);
                
                // Annotate hidden elements with visibility marker
                if (isHidden) {
                    element.setAttribute('data-synta-visibility', 'hidden');
                }
                // Explicitly mark visible interactive elements for clarity
                else if (
                    element.tagName === 'BUTTON' || 
                    element.tagName === 'A' || 
                    element.tagName === 'INPUT' || 
                    element.tagName === 'SELECT' || 
                    element.tagName === 'TEXTAREA' ||
                    element.hasAttribute('onclick') ||
                    element.hasAttribute('role') && element.getAttribute('role') === 'button'
                ) {
                    element.setAttribute('data-synta-visibility', 'visible');
                }
            } catch (e) {
                // Skip elements that cause errors
            }
        });
    }";

    /// <summary>
    /// Script for building a simplified accessibility tree from the DOM.
    /// This provides semantic information about the page structure for AI context.
    /// </summary>
    public const string AccessibilityTreeScript = @"async () => {
        // Build a simplified accessibility tree from the DOM
        function buildAxTree(element, maxDepth = 5, currentDepth = 0) {
            if (currentDepth > maxDepth || !element) return null;
            
            const role = element.getAttribute('role') || 
                         (element.tagName === 'BUTTON' ? 'button' : 
                          element.tagName === 'A' ? 'link' :
                          element.tagName === 'INPUT' ? 'textbox' :
                          element.tagName === 'IMG' ? 'image' :
                          element.tagName === 'NAV' ? 'navigation' :
                          element.tagName === 'MAIN' ? 'main' :
                          element.tagName === 'HEADER' ? 'banner' :
                          element.tagName === 'FOOTER' ? 'contentinfo' :
                          element.tagName === 'H1' || element.tagName === 'H2' || 
                          element.tagName === 'H3' || element.tagName === 'H4' ? 'heading' :
                          null);
            
            if (!role && currentDepth > 0) {
                // Skip non-landmark elements but process children
                const childResults = [];
                for (const child of element.children || []) {
                    const childTree = buildAxTree(child, maxDepth, currentDepth + 1);
                    if (childTree) childResults.push(childTree);
                }
                return childResults.length === 1 ? childResults[0] : 
                       childResults.length > 0 ? { role: 'group', children: childResults } : null;
            }
            
            const name = element.getAttribute('aria-label') || 
                         element.getAttribute('alt') ||
                         element.getAttribute('title') ||
                         (element.innerText || '').substring(0, 50).trim() || '';
            
            const node = { role: role || 'generic', name };
            
            const children = [];
            for (const child of element.children || []) {
                const childTree = buildAxTree(child, maxDepth, currentDepth + 1);
                if (childTree) children.push(childTree);
            }
            if (children.length > 0) node.children = children;
            
            return node;
        }
        
        return buildAxTree(document.body);
    }";

    /// <summary>
    /// Script for extracting interactive elements (buttons, inputs, links, selects, textareas) with their attributes
    /// for better AI context when generating Cypress tests.
    /// CRITICAL: Captures visibility state (isVisible property) instead of filtering hidden elements.
    /// This fixes the "visibility trap" where hidden menus, dropdowns, and modals were excluded from the dataset,
    /// causing the AI to skip tests or hallucinate selectors for elements it didn't know existed.
    /// Now the AI knows about ALL interactive elements and can write code to reveal hidden ones.
    /// Enhanced to capture semantic regions, CSS paths, form associations, and selector recommendations.
    /// </summary>
    public const string InteractiveElementsScript = @"() => {
        const elements = [];
        const interactiveSelectors = 'button, a, input, select, textarea, img, [role=""button""], [role=""link""], [role=""textbox""], [role=""checkbox""], [role=""radio""], [role=""combobox""], [role=""menuitem""], [role=""tab""]';
        
        // Helper: Find the semantic region an element belongs to
        function getSemanticRegion(el) {
            let current = el;
            const semanticTags = ['header', 'nav', 'main', 'footer', 'aside', 'article', 'section', 'form', 'dialog', 'menu'];
            while (current && current !== document.body) {
                if (semanticTags.includes(current.tagName?.toLowerCase())) {
                    if (current.tagName?.toLowerCase() === 'form') {
                        const formId = current.id || current.getAttribute('name') || '';
                        return formId ? 'form#' + formId : 'form';
                    }
                    return current.tagName.toLowerCase();
                }
                const role = current.getAttribute?.('role');
                if (role && ['navigation', 'banner', 'main', 'contentinfo', 'complementary', 'form', 'dialog', 'menu'].includes(role)) {
                    return role;
                }
                current = current.parentElement;
            }
            return 'body';
        }
        
        // Helper: Build a unique CSS path for an element
        // IMPROVED: Prioritizes stable attributes/classes over brittle nth-of-type selectors
        function getCssPath(el) {
            // Try to find the shortest unique selector, starting from the element itself
            function tryUniqueSelector(element, withParent = false) {
                const tag = element.tagName.toLowerCase();
                const candidates = [];
                
                // Priority 1: ID (if valid and stable-looking)
                if (element.id && !element.id.match(/^[0-9]/) && !element.id.match(/\s/) && !element.id.match(/_[a-z0-9]{5,}$/i)) {
                    const escapedId = element.id.replace(/([:.\[\]()])/g, '\\$1');
                    candidates.push('#' + escapedId);
                }
                
                // Priority 2: Test IDs (data-testid, data-cy, data-test)
                const testId = element.getAttribute('data-testid') || element.getAttribute('data-cy') || element.getAttribute('data-test');
                if (testId) {
                    candidates.push('[data-testid=' + String.fromCharCode(34) + testId + String.fromCharCode(34) + ']');
                }
                
                // Priority 3: Stable classes (not CSS-in-JS generated)
                const classList = Array.from(element.classList || []).filter(cls => 
                    !cls.match(/^(css-|sc-|makeStyles|jss|___)[a-zA-Z0-9]+$/) && // Exclude CSS-in-JS
                    !cls.match(/^[a-z0-9]{5,}$/) && // Exclude short random hashes
                    cls.length > 2 // Exclude single/double letter classes
                );
                if (classList.length > 0) {
                    // Try each stable class
                    classList.forEach(cls => {
                        candidates.push(tag + '.' + cls);
                    });
                    // Try combination of multiple stable classes (more specific)
                    if (classList.length > 1) {
                        candidates.push(tag + '.' + classList.slice(0, 2).join('.'));
                    }
                }
                
                // Priority 4: Semantic attributes (role, name, type for inputs)
                const role = element.getAttribute('role');
                if (role) {
                    candidates.push(tag + '[role=' + String.fromCharCode(34) + role + String.fromCharCode(34) + ']');
                }
                
                const name = element.getAttribute('name');
                if (name && !name.match(/^[0-9a-f]{8,}$/i)) { // Exclude hash-like names
                    candidates.push(tag + '[name=' + String.fromCharCode(34) + name + String.fromCharCode(34) + ']');
                }
                
                const type = element.getAttribute('type');
                if (type && tag === 'input') {
                    candidates.push('input[type=' + String.fromCharCode(34) + type + String.fromCharCode(34) + ']');
                }
                
                // Priority 5: For links, use href if it's internal/stable
                if (tag === 'a') {
                    const href = element.getAttribute('href');
                    if (href && (href.startsWith('/') || href.startsWith('#')) && href.length < 100) {
                        candidates.push('a[href=' + String.fromCharCode(34) + href + String.fromCharCode(34) + ']');
                    }
                }
                
                // Priority 6: For buttons/links with stable text content (short and no dynamic numbers)
                const text = (element.innerText || '').trim();
                if ((tag === 'button' || tag === 'a') && text && text.length < 30 && !text.match(/\d{3,}/)) {
                    // Don't add this as a candidate for getCssPath, but note it exists
                    // (text-based selectors are better handled by contains() in Cypress)
                }
                
                // Test each candidate for uniqueness
                for (const candidate of candidates) {
                    try {
                        const matches = withParent && element.parentElement ? 
                            element.parentElement.querySelectorAll(candidate) :
                            document.querySelectorAll(candidate);
                        if (matches.length === 1 && matches[0] === element) {
                            return candidate;
                        }
                    } catch (e) {
                        // Invalid selector, skip
                    }
                }
                
                return null;
            }
            
            // Step 1: Try to find a unique selector for just this element
            const uniqueSelector = tryUniqueSelector(el);
            if (uniqueSelector) return uniqueSelector;
            
            // Step 2: Build path with stable selectors, avoiding nth-of-type when possible
            const parts = [];
            let current = el;
            let depth = 0;
            const maxDepth = 5;
            
            while (current && current !== document.body && depth < maxDepth) {
                const tag = current.tagName.toLowerCase();
                let selector = tag;
                let foundStableSelector = false;
                
                // Try to build a stable selector for this element in the chain
                if (current.id && !current.id.match(/^[0-9]/) && !current.id.match(/\s/) && !current.id.match(/_[a-z0-9]{5,}$/i)) {
                    const escapedId = current.id.replace(/([:.\[\]()])/g, '\\$1');
                    selector = '#' + escapedId;
                    parts.unshift(selector);
                    break; // ID is unique, stop here
                }
                
                const testId = current.getAttribute('data-testid') || current.getAttribute('data-cy') || current.getAttribute('data-test');
                if (testId) {
                    selector = '[data-testid=' + String.fromCharCode(34) + testId + String.fromCharCode(34) + ']';
                    parts.unshift(selector);
                    break; // Test ID should be unique, stop here
                }
                
                // Add first stable class if available
                const classList = Array.from(current.classList || []).filter(cls => 
                    !cls.match(/^(css-|sc-|makeStyles|jss|___)[a-zA-Z0-9]+$/) && 
                    !cls.match(/^[a-z0-9]{5,}$/) && 
                    cls.length > 2
                );
                if (classList.length > 0) {
                    selector = tag + '.' + classList[0];
                    foundStableSelector = true;
                } else {
                    // Try semantic attributes
                    const role = current.getAttribute('role');
                    const name = current.getAttribute('name');
                    if (role) {
                        selector = tag + '[role=' + String.fromCharCode(34) + role + String.fromCharCode(34) + ']';
                        foundStableSelector = true;
                    } else if (name && !name.match(/^[0-9a-f]{8,}$/i)) {
                        selector = tag + '[name=' + String.fromCharCode(34) + name + String.fromCharCode(34) + ']';
                        foundStableSelector = true;
                    }
                }
                
                // FALLBACK: Use nth-of-type - imperfect for dynamic pages but element EXISTS at runtime
                // (data-synta-id was worse because it only exists in scraper memory, never on live page)
                if (!foundStableSelector) {
                    const siblings = Array.from(parent.children).filter(c => c.tagName === current.tagName);
                    if (siblings.length > 1) {
                        const index = siblings.indexOf(current) + 1;
                        selector = tag + ':nth-of-type(' + index + ')';
                    }
                }
                
                parts.unshift(selector);
                current = current.parentElement;
                depth++;
            }
            
            // Use single space instead of ' > ' for more flexible matching
            return parts.join(' ');
        }
        
        // Helper: Compute selector stability score
        function getSelectorStability(el) {
            if (el.getAttribute('data-testid') || el.getAttribute('data-cy') || el.getAttribute('data-test')) return 100;
            if (el.id && !el.id.match(/^[0-9]/) && !el.id.match(/\s/) && !el.id.match(/_[a-z0-9]{5,}$/i)) return 90;
            if (el.getAttribute('aria-label')) return 85;
            const name = el.getAttribute('name');
            if (name && !name.match(/^[0-9a-f]{8,}$/i)) return 80; // Exclude hash-like names
            // Stable classes (semantic, BEM-style)
            const classList = Array.from(el.classList || []).filter(cls => 
                !cls.match(/^(css-|sc-|makeStyles|jss|___)[a-zA-Z0-9]+$/) && 
                !cls.match(/^[a-z0-9]{5,}$/) && 
                cls.length > 2
            );
            if (classList.length > 0) return 78;
            // Semantic attributes
            if (el.getAttribute('role')) return 75;
            // Links with recognizable href domains are fairly stable
            const href = el.getAttribute('href');
            if (el.tagName.toLowerCase() === 'a' && href) {
                const socialDomains = ['linkedin.com', 'facebook.com', 'twitter.com', 'instagram.com', 'youtube.com', 'github.com'];
                if (socialDomains.some(d => href.includes(d))) return 73;
                if (href.startsWith('/') && href.length < 100) return 70;
            }
            if (el.getAttribute('placeholder')) return 68;
            const text = (el.innerText || '').trim();
            if (text && text.length < 30 && !text.match(/\d{3,}/)) return 60;
            // Fallback: Elements with ANY class (even CSS-in-JS) are slightly better than nothing
            const className = el.className?.toString() || '';
            if (className) return 40;
            // Worst case: positional selector with nth-of-type
            return 20;
        }
        
        function getImplicitRole(el) {
            const tag = el.tagName?.toLowerCase();
            const type = el.getAttribute('type');
            const roleMap = {
                'button': 'button', 'a': 'link',
                'input': type === 'submit' || type === 'button' ? 'button' : type === 'checkbox' ? 'checkbox' : type === 'radio' ? 'radio' : 'textbox',
                'select': 'combobox', 'textarea': 'textbox'
            };
            return roleMap[tag] || null;
        }
        
        function escapeRegex(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\\\$&');
        }
        
        
        // Helper: Find shadow host chain for an element inside shadow DOM
        function getShadowHostChain(el) {
            const chain = [];
            let current = el;
            
            // Walk up until we find the shadow root boundary
            while (current) {
                const root = current.getRootNode();
                if (root && root.host) {
                    // We're in a shadow DOM, store the host
                    chain.unshift(root.host);
                    current = root.host;
                } else {
                    // We've reached the document root
                    break;
                }
            }
            
            return chain;
        }
        
        // Helper: Build selector for an element (without shadow DOM chain)
        function buildElementSelector(el) {
            const q = String.fromCharCode(34); // double quote
            const testId = el.getAttribute('data-testid') || el.getAttribute('data-cy') || el.getAttribute('data-test');
            if (testId) return '[data-testid=' + q + testId + q + ']';
            
            // Check if ID is safe to use (not dynamically generated)
            // Dynamic IDs change between page loads and cause Cypress selector failures
            if (el.id && el.id.trim() !== '') {
                const id = el.id;
                
                // Skip IDs that look dynamically generated
                const isDynamicId = (
                    /^[0-9]/.test(id) ||                          // Starts with number
                    /\s/.test(id) ||                              // Contains whitespace
                    /_[a-z0-9]{5,}$/i.test(id) ||                 // Framework suffix (e.g., _abc123)
                    /-\d{10,}-/.test(id) ||                       // Timestamp pattern (e.g., button-1234567890-123)
                    /^comp-[a-z0-9]+$/i.test(id) ||               // Wix component IDs (comp-abc123)
                    /^[0-9a-f]{8,}$/i.test(id) ||                 // Pure hash IDs
                    /internal-ref-/.test(id) ||                   // Our internal references
                    /-synta-/.test(id) ||                         // Our SynTA IDs (shouldn't exist anymore)
                    /_r_comp-/.test(id) ||                        // Wix responsive component IDs
                    /^ng-/.test(id) ||                            // Angular generated IDs
                    /^react-/.test(id) ||                         // React generated IDs
                    /^v-/.test(id) ||                             // Vue generated IDs  
                    /^ember\d+$/.test(id) ||                      // Ember generated IDs
                    /__/.test(id) && /\d{4,}/.test(id)            // BEM with timestamp (e.g., Button__123456)
                );
                
                if (!isDynamicId) {
                    const escapedId = id.replace(/([:.\\[\\]()])/g, '\\\\\\\\$1');
                    const tag = el.tagName ? el.tagName.toLowerCase() : '';
                    return tag + '#' + escapedId;
                }
            }
            
            // FIX: Don't strip hyphens/underscores/dots blindly. Preserve valid attribute values.
            // Only use aria-label if it doesn't contain characters that would break the selector
            const ariaLabel = el.getAttribute('aria-label');
            if (ariaLabel && /^[a-zA-Z0-9 _.:-]+$/.test(ariaLabel)) {
                return '[aria-label=' + q + ariaLabel + q + ']';
            }
            
            // FIX: Preserve hyphens, underscores, and dots in name attributes (common in modern forms)
            const name = el.getAttribute('name');
            if (name && /^[a-zA-Z0-9 _.:-]+$/.test(name)) {
                return '[name=' + q + name + q + ']';
            }
            
            // FIX: Preserve hyphens, underscores, and dots in placeholder text
            const placeholder = el.getAttribute('placeholder');
            if (placeholder && /^[a-zA-Z0-9 _.:-]+$/.test(placeholder)) {
                return '[placeholder=' + q + placeholder + q + ']';
            }
            
            // For links, check href for recognizable domains (social media, common patterns)
            const href = el.getAttribute('href');
            if (el.tagName.toLowerCase() === 'a' && href) {
                const socialDomains = ['linkedin.com', 'facebook.com', 'twitter.com', 'instagram.com', 'youtube.com', 'github.com', 'tiktok.com'];
                for (const domain of socialDomains) {
                    if (href.includes(domain)) {
                        return 'a[href*=' + q + domain + q + ']';
                    }
                }
                if (href.startsWith('/') && href.length > 1 && href.length < 50) {
                    return 'a[href=' + q + href + q + ']';
                }
            }
            
            const text = (el.innerText || '').trim();
            if (text && text.length < 50) {
                const escapedText = escapeRegex(text.substring(0, 30));
                const flexibleRegex = escapedText.split(/\s+/).filter(word => word.length > 0).join('\\\\s+');
                return 'contains(' + q + el.tagName.toLowerCase() + q + ', /' + flexibleRegex + '/i)';
            }
            
            return getCssPath(el);
        }
        
        // Helper: Build the recommended Cypress selector (uses single quotes for JS, double quotes inside)
        function getRecommendedSelector(el) {
            const q = String.fromCharCode(34); // double quote
            
            // Check if element is in shadow DOM
            const shadowHostChain = getShadowHostChain(el);
            
            if (shadowHostChain.length > 0) {
                // Element is inside shadow DOM, build chain
                let selector = '';
                
                // Start with the outermost shadow host
                selector += 'cy.get(' + q + buildElementSelector(shadowHostChain[0]) + q + ')';
                
                // Add .shadow() for each level in the chain
                for (let i = 1; i < shadowHostChain.length; i++) {
                    selector += '.shadow().find(' + q + buildElementSelector(shadowHostChain[i]) + q + ')';
                }
                
                // Finally, add the element itself
                const elementSelector = buildElementSelector(el);
                if (elementSelector.startsWith('contains(')) {
                    // Special handling for contains() - it's not a regular selector
                    const text = (el.innerText || '').trim();
                    if (text && text.length < 50) {
                        const escapedText = escapeRegex(text.substring(0, 30));
                        const flexibleRegex = escapedText.split(/\s+/).filter(word => word.length > 0).join('\\\\s+');
                        selector += '.shadow().contains(' + q + el.tagName.toLowerCase() + q + ', /' + flexibleRegex + '/i)';
                    }
                } else {
                    selector += '.shadow().find(' + q + elementSelector + q + ')';
                }
                
                return selector;
            }
            
            // Element is not in shadow DOM, use standard selector
            const elementSelector = buildElementSelector(el);
            if (elementSelector.startsWith('contains(')) {
                // contains() is a Cypress command, not a selector
                const text = (el.innerText || '').trim();
                if (text && text.length < 50) {
                    const escapedText = escapeRegex(text.substring(0, 30));
                    const flexibleRegex = escapedText.split(/\s+/).filter(word => word.length > 0).join('\\\\s+');
                    return 'cy.contains(' + q + el.tagName.toLowerCase() + q + ', /' + flexibleRegex + '/i)';
                }
            }
            
            return 'cy.get(' + q + elementSelector + q + ')';
        }
        
        
        // Helper: Find associated label for form elements
        function getAssociatedLabel(el) {
            if (el.id) {
                const label = document.querySelector('label[for=' + String.fromCharCode(34) + el.id + String.fromCharCode(34) + ']');
                if (label) return (label.innerText || '').trim().substring(0, 50);
            }
            let current = el.parentElement;
            while (current && current !== document.body) {
                if (current.tagName === 'LABEL') {
                    const clone = current.cloneNode(true);
                    clone.querySelectorAll('input, select, textarea').forEach(e => e.remove());
                    return (clone.innerText || '').trim().substring(0, 50);
                }
                current = current.parentElement;
            }
            const labelledBy = el.getAttribute('aria-labelledby');
            if (labelledBy) {
                const labelEl = document.getElementById(labelledBy);
                if (labelEl) return (labelEl.innerText || '').trim().substring(0, 50);
            }
            return null;
        }
        
        // Helper: Get form context
        function getFormContext(el) {
            const form = el.closest('form');
            if (!form) return null;
            const formId = form.id || form.getAttribute('name');
            if (formId) return 'form#' + formId;
            const formAction = form.getAttribute('action');
            if (formAction) return 'form[action=' + String.fromCharCode(34) + formAction + String.fromCharCode(34) + ']';
            const heading = form.querySelector('h1, h2, h3, h4, legend');
            if (heading) return 'form: ' + (heading.innerText || '').trim().substring(0, 30);
            return 'form';
        }
        
        // Helper: Get nearby context
        function getNearbyContext(el) {
            const contexts = [];
            let sibling = el.previousElementSibling;
            let depth = 0;
            while (sibling && depth < 3) {
                if (/^h[1-6]$/i.test(sibling.tagName)) {
                    contexts.push('heading: ' + (sibling.innerText || '').trim().substring(0, 30));
                    break;
                }
                sibling = sibling.previousElementSibling;
                depth++;
            }
            const fieldset = el.closest('fieldset');
            if (fieldset) {
                const legend = fieldset.querySelector('legend');
                if (legend) contexts.push('fieldset: ' + (legend.innerText || '').trim().substring(0, 30));
            }
            return contexts.length > 0 ? contexts.join('; ') : null;
        }
        
        
        // Recursive function to extract elements from a root, including shadow DOM and iframes
        function extractFromRoot(root, inShadowDom = false, inIframe = false) {
            root.querySelectorAll(interactiveSelectors).forEach(el => {
                try {
                    // CRITICAL FIX: Capture visibility state instead of filtering
                    // Hidden elements (menus, dropdowns, modals) must be included so AI knows they exist
                    const isVisible = el.getAttribute('data-synta-visible') === 'true';
                    
                    // Handle className - SVG elements return SVGAnimatedString
                    let classValue = '';
                    if (typeof el.className === 'string') {
                        classValue = el.className;
                    } else if (el.className && el.className.baseVal) {
                        classValue = el.className.baseVal;
                    } else if (el.getAttribute) {
                        classValue = el.getAttribute('class') || '';
                    }
                    
                    const style = window.getComputedStyle(el);
                    const opacity = parseFloat(style.opacity);
                    let visibilityNote = '';
                    if (opacity < 0.1) {
                        visibilityNote = '(opacity-hidden)';
                    }
                    // FIX: Make iframe content MUCH more visible to the AI by using a clear flag
                    if (inIframe) {
                        visibilityNote += ' [IFRAME-CONTENT]';
                    }
                    
                    const text = ((el.innerText || '').substring(0, 100) + visibilityNote).trim();
                    
                    // Capture all other attributes to prevent AI hallucination
                    const otherAttributes = {};
                    const explicitlyMappedAttrs = ['id', 'class', 'name', 'type', 'href', 'placeholder', 'data-testid', 'data-cy', 'data-test', 'aria-label', 'role', 'required', 'aria-required', 'value', 'pattern'];
                    Array.from(el.attributes).forEach(attr => {
                        if (!explicitlyMappedAttrs.includes(attr.name)) {
                            otherAttributes[attr.name] = attr.value;
                        }
                    });
                    
                    elements.push({
                        tag: el.tagName ? el.tagName.toLowerCase() : '',
                        id: el.id || '',
                        name: el.getAttribute('name') || '',
                        type: el.getAttribute('type') || '',
                        className: classValue,
                        placeholder: el.getAttribute('placeholder') || '',
                        text: text,
                        href: el.getAttribute('href') || '',
                        dataTestId: el.getAttribute('data-testid') || el.getAttribute('data-cy') || el.getAttribute('data-test') || '',
                        ariaLabel: el.getAttribute('aria-label') || '',
                        role: el.getAttribute('role') || getImplicitRole(el) || '',
                        opacity: opacity,
                        inShadowDom: inShadowDom,
                        isVisible: isVisible,
                        // Enhanced properties
                        semanticRegion: getSemanticRegion(el),
                        cssPath: getCssPath(el),
                        recommendedSelector: getRecommendedSelector(el),
                        selectorStabilityScore: getSelectorStability(el),
                        associatedLabel: getAssociatedLabel(el),
                        formContext: getFormContext(el),
                        nearbyContext: getNearbyContext(el),
                        isRequired: el.required || el.getAttribute('aria-required') === 'true',
                        value: el.value || el.getAttribute('value') || '',
                        validationPattern: el.getAttribute('pattern') || '',
                        otherAttributes: JSON.stringify(otherAttributes)
                    });
                } catch (e) {
                    // Skip elements that cause errors
                }
            });
            
            // Traverse into shadow roots
            root.querySelectorAll('*').forEach(el => {
                if (el.shadowRoot) {
                    extractFromRoot(el.shadowRoot, true, inIframe);
                }
                
                // Traverse into iframes (same-origin only)
                if (el.tagName === 'IFRAME') {
                    try {
                        const iframeDoc = el.contentDocument || el.contentWindow?.document;
                        if (iframeDoc && iframeDoc.body) {
                            extractFromRoot(iframeDoc.body, inShadowDom, true);
                        }
                    } catch (e) {
                        // Cross-origin iframe, skip
                    }
                }
            });
        }
        
        // Start extraction from document
        extractFromRoot(document);
        return elements;
    }";

    /// <summary>
    /// ATOMIC DOM CAPTURE: Combines visibility annotation, interactive elements extraction, and HTML capture
    /// into a single JavaScript execution to eliminate race conditions.
    /// 
    /// PROBLEM SOLVED: Previously, visibility was annotated in one call, then elements were extracted in another,
    /// then HTML was captured in a third. Between these calls, React/Vue/Angular could re-render elements,
    /// causing selectors to become stale or visibility states to change.
    /// 
    /// SOLUTION: Execute all operations atomically in one page.evaluate() call, ensuring the DOM state
    /// is consistent across all operations.
    /// 
    /// Returns an object with: html (string), elements (array), and success (boolean).
    /// </summary>
    public const string AtomicDomCaptureScript = @"() => {
        const result = {
            html: '',
            elements: [],
            success: false
        };
        
        try {
            // ============================================================
            // PHASE 1: Visibility Annotation (inline DomEnrichmentScript)
            // ============================================================
            function traverseAndMarkVisibility(root) {
                const elements = root.querySelectorAll('*');
                elements.forEach(element => {
                    try {
                        if (element.tagName === 'INPUT' && element.getAttribute('type') === 'hidden') return;
                        
                        const style = window.getComputedStyle(element);
                        const rect = element.getBoundingClientRect();
                        
                        // CYPRESS-ALIGNED visibility detection
                        const notDisplayNone = style.display !== 'none';
                        const notVisibilityHidden = style.visibility !== 'hidden';
                        const hasVisibleOpacity = parseFloat(style.opacity) !== 0;
                        const hasPositiveDimensions = rect.width > 0 && rect.height > 0;
                        const isOffScreen = rect.right < 0 || rect.bottom < 0 || 
                                           rect.left > window.innerWidth || rect.top > window.innerHeight;
                        const isSvgOrIconFont = element.tagName === 'SVG' || element.tagName === 'I' || 
                                               element.classList.contains('icon') || element.classList.contains('fa');
                        const hasVisibleText = element.innerText && element.innerText.trim().length > 0;
                        
                        const isVisible = notDisplayNone && notVisibilityHidden && hasVisibleOpacity &&
                                         (hasPositiveDimensions || isSvgOrIconFont || hasVisibleText) &&
                                         !isOffScreen;
                        
                        element.setAttribute('data-synta-visible', isVisible ? 'true' : 'false');
                        
                        if (element.shadowRoot) traverseAndMarkVisibility(element.shadowRoot);
                        if (element.tagName === 'IFRAME') {
                            try {
                                const iframeDoc = element.contentDocument || element.contentWindow?.document;
                                if (iframeDoc && iframeDoc.body) traverseAndMarkVisibility(iframeDoc.body);
                            } catch (e) {}
                        }
                    } catch (e) {}
                });
            }
            
            if (document.body) {
                traverseAndMarkVisibility(document.body);
            }
            
            // ============================================================
            // PHASE 2: Interactive Elements Extraction (inline script)
            // ============================================================
            const interactiveSelectors = 'a, button, input, select, textarea, [role=""button""], [role=""link""], [role=""tab""], [role=""menuitem""], [tabindex], [onclick], [contenteditable=""true""]';
            const elements = [];
            
            function tryUniqueSelector(element) {
                const tag = element.tagName.toLowerCase();
                const candidates = [];
                
                const testId = element.getAttribute('data-testid') || element.getAttribute('data-cy') || element.getAttribute('data-test');
                if (testId) candidates.push('[data-testid=""' + testId + '""]');
                
                if (element.id && !element.id.match(/^[0-9]/) && !element.id.match(/_[a-z0-9]{5,}$/i)) {
                    candidates.push('#' + element.id.replace(/([:.[\]()])/g, '\\$1'));
                }
                
                const ariaLabel = element.getAttribute('aria-label');
                if (ariaLabel && ariaLabel.length < 50) {
                    candidates.push('[aria-label=""' + ariaLabel.replace(/[""/]/g, '') + '""]');
                }
                
                const name = element.getAttribute('name');
                if (name && !name.match(/^[0-9a-f]{8,}$/i)) {
                    candidates.push(tag + '[name=""' + name + '""]');
                }
                
                for (const candidate of candidates) {
                    try {
                        const matches = document.querySelectorAll(candidate);
                        if (matches.length === 1 && matches[0] === element) return candidate;
                    } catch (e) {}
                }
                return null;
            }
            
            function getRecommendedSelector(el) {
                const unique = tryUniqueSelector(el);
                if (unique) return 'cy.get(""' + unique + '"")';
                
                const text = (el.innerText || '').trim();
                if (text && text.length < 30 && !text.match(/\d{3,}/)) {
                    const tag = el.tagName.toLowerCase();
                    // Escape quotes in text for Cypress selector
                    const safeText = text.replace(/['""]]/g, '');
                    if (safeText.length > 0) {
                        return 'cy.contains(""' + tag + '"", ""' + safeText + '"")';
                    }
                }
                
                // FALLBACK: Use aria-label if available (exists on live page)
                const ariaLabel = el.getAttribute('aria-label');
                if (ariaLabel && ariaLabel.length < 50) {
                    const safeLabel = ariaLabel.replace(/[""\']/g, '');
                    return 'cy.get(""[aria-label=' + String.fromCharCode(39) + safeLabel + String.fromCharCode(39) + ']"")';
                }
                
                // LAST RESORT: Use CSS path (may include nth-of-type but at least it EXISTS on the page)
                const cssPath = getCssPath(el);
                return 'cy.get(""' + cssPath + '"")';
            }
            
            function getSemanticRegion(el) {
                const semanticTags = ['main', 'header', 'footer', 'nav', 'aside', 'article', 'section', 'form'];
                let current = el;
                while (current && current !== document.body) {
                    const tag = current.tagName.toLowerCase();
                    if (semanticTags.includes(tag)) return tag;
                    const role = current.getAttribute('role');
                    if (role) {
                        const roleToRegion = {'main':'main','banner':'header','contentinfo':'footer','navigation':'nav','complementary':'aside'};
                        if (roleToRegion[role]) return roleToRegion[role];
                    }
                    if (tag === 'form') return 'form#' + (current.id || current.name || 'unnamed');
                    current = current.parentElement;
                }
                return 'body';
            }
            
            function extractFromRoot(root, inShadowDom = false, inIframe = false) {
                root.querySelectorAll(interactiveSelectors).forEach(el => {
                    try {
                        const isVisible = el.getAttribute('data-synta-visible') === 'true';
                        let classValue = '';
                        if (typeof el.className === 'string') classValue = el.className;
                        else if (el.className && el.className.baseVal) classValue = el.className.baseVal;
                        else if (el.getAttribute) classValue = el.getAttribute('class') || '';
                        
                        const style = window.getComputedStyle(el);
                        const text = ((el.innerText || '').substring(0, 100) + (inIframe ? ' [IFRAME-CONTENT]' : '')).trim();
                        
                        elements.push({
                            tag: el.tagName ? el.tagName.toLowerCase() : '',
                            id: el.id || '',
                            name: el.getAttribute('name') || '',
                            type: el.getAttribute('type') || '',
                            className: classValue,
                            placeholder: el.getAttribute('placeholder') || '',
                            text: text,
                            href: el.getAttribute('href') || '',
                            dataTestId: el.getAttribute('data-testid') || el.getAttribute('data-cy') || el.getAttribute('data-test') || '',
                            ariaLabel: el.getAttribute('aria-label') || '',
                            role: el.getAttribute('role') || '',
                            opacity: parseFloat(style.opacity),
                            inShadowDom: inShadowDom,
                            isVisible: isVisible,
                            semanticRegion: getSemanticRegion(el),
                            recommendedSelector: getRecommendedSelector(el),
                            selectorStabilityScore: el.getAttribute('data-testid') ? 100 : (el.id ? 90 : 50),
                            associatedLabel: '',
                            formContext: el.closest('form')?.id || el.closest('form')?.name || '',
                            nearbyContext: '',
                            isRequired: el.required || el.getAttribute('aria-required') === 'true',
                            value: '',
                            validationPattern: el.getAttribute('pattern') || ''
                        });
                    } catch (e) {}
                });
                
                root.querySelectorAll('*').forEach(el => {
                    if (el.shadowRoot) extractFromRoot(el.shadowRoot, true, inIframe);
                    if (el.tagName === 'IFRAME') {
                        try {
                            const iframeDoc = el.contentDocument || el.contentWindow?.document;
                            if (iframeDoc && iframeDoc.body) extractFromRoot(iframeDoc.body, inShadowDom, true);
                        } catch (e) {}
                    }
                });
            }
            
            extractFromRoot(document);
            
            // ============================================================
            // PHASE 3: Capture HTML (AFTER all modifications are complete)
            // ============================================================
            result.html = document.documentElement.outerHTML;
            result.elements = elements;
            result.success = true;
            
        } catch (error) {
            result.success = false;
        }
        
        return result;
    }";
}

