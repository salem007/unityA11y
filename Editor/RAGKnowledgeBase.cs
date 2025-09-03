// RAGKnowledgeBase.cs
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class RAGKnowledgeBase
{
    private static readonly Dictionary<string, string> AccessibilityGuidelines = new Dictionary<string, string>
    {
        {
            "WCAG 1.4.3", 
            "MINIMUM CONTRAST: Text must have a contrast ratio of at least 4.5:1. " +
            "For large text (18pt+ or 14pt+ bold): at least 3:1.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Button with #888888 text on #FFFFFF background (contrast 2.8:1)\n" +
            "- Gray text (#CCCCCC) on white background\n" +
            "- Light blue links on light gray background\n\n" +
            "FIX EXAMPLES:\n" +
            "- Change text color to #666666 (contrast 5.7:1)\n" +
            "- Use darker background or lighter text\n" +
            "- Implement contrast ratio validation in UI system\n" +
            "- Add high contrast mode in game settings"
        },
        {
            "WCAG 1.2.2", 
            "CAPTIONS: Provide synchronized captions for all pre-recorded audio content. " +
            "Captions must include dialogue, sound effects, and meaningful music.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Cutscenes without subtitle options\n" +
            "- Voice instructions without text alternatives\n" +
            "- Important audio cues without visual indicators\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add .srt subtitle support for videos\n" +
            "- Implement in-game captioning system\n" +
            "- Provide toggle for subtitles in settings\n" +
            "- Add visual indicators for important sounds"
        },
        {
            "WCAG 1.2.5", 
            "AUDIO DESCRIPTION: Provide audio description for all pre-recorded video content. " +
            "Describe important visual information not contained in the main audio track.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Visual storytelling without audio description\n" +
            "- Cutscenes showing visual clues only\n" +
            "- Text-based information shown on screen without narration\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add optional audio description track\n" +
            "- Provide text alternatives for visual information\n" +
            "- Implement descriptive audio mode in settings\n" +
            "- Use narrator for important visual events"
        },
        {
            "WCAG 2.2.1", 
            "TIMING: If a time limit is imposed, give users option to turn off, adjust, or extend it. " +
            "Minimum 20 seconds for alerts and notifications.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- 5-second timeout for puzzle solutions\n" +
            "- Quick-time events without pause option\n" +
            "- Auto-dismissing notifications without user control\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add pause button for timed sections\n" +
            "- Provide settings to adjust/disable time limits\n" +
            "- Implement grace periods for time-sensitive actions\n" +
            "- Allow users to request time extensions"
        },
        {
            "WCAG 2.3.3", 
            "ANIMATION: Avoid excessive animations and movements that could cause dizziness or seizures. " +
            "Provide way to disable animations. Limit automatic animations to 3 seconds maximum.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Constant camera shaking effects\n" +
            "- Rapid flashing lights (more than 3 flashes per second)\n" +
            "- Spinning objects that could cause vertigo\n" +
            "- Excessive particle effects with rapid movement\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add reduced motion option in settings\n" +
            "- Limit flash frequency to safe levels\n" +
            "- Provide toggle for screen shake effects\n" +
            "- Use subtle animations instead of intense ones"
        },
        {
            "WCAG 2.3.1", 
            "FLASHING: Content must not flash more than 3 times per second. " +
            "Avoid flashing that could trigger photosensitive epilepsy.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Strobe light effects in horror games\n" +
            "- Rapid flashing during explosion sequences\n" +
            "- UI elements that blink rapidly for attention\n\n" +
            "FIX EXAMPLES:\n" +
            "- Limit flash frequency to safe levels\n" +
            "- Add warning for flashing content\n" +
            "- Provide option to disable flashing effects\n" +
            "- Use alternative visual cues instead of flashing"
        },
        {
            "WCAG 2.4.7", 
            "FOCUS INDICATOR: All interactive elements must have visible focus indicator during keyboard navigation. " +
            "Focus indicator contrast must be at least 3:1 with background.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Buttons with no visual focus state\n" +
            "- Hidden focus indicators that disappear\n" +
            "- Low contrast focus outlines\n" +
            "- Keyboard traps where focus gets stuck\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add outline: 2px solid #007ACC with good contrast\n" +
            "- Implement clear focus highlighting\n" +
            "- Ensure proper focus management in UI\n" +
            "- Test complete keyboard navigation flow"
        },
        {
            "WCAG 2.4.6", 
            "NAVIGATION: Navigation structure must be logical and predictable. " +
            "Use hierarchical headings, descriptive labels, and consistent organization.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Inconsistent menu structures between screens\n" +
            "- Hidden navigation options\n" +
            "- No clear way to navigate back\n" +
            "- Complex nested menus without clear hierarchy\n\n" +
            "FIX EXAMPLES:\n" +
            "- Standardize menu layout across all screens\n" +
            "- Use clear headings and section labels\n" +
            "- Provide breadcrumb navigation\n" +
            "- Implement consistent back button behavior"
        },
        {
            "WCAG 1.1.1", 
            "ALT TEXT: All non-text content must have equivalent text alternative. " +
            "For decorative images, use alt=\"\" (empty).\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Icons without tooltips or labels\n" +
            "- Images without alt text for screen readers\n" +
            "- Decorative images that aren't marked as such\n" +
            "- UI elements without proper ARIA labels\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add alt=\"Warning: Low health\" for important images\n" +
            "- Use ARIA labels for interactive elements\n" +
            "- Mark decorative images with empty alt text\n" +
            "- Provide text alternatives for all visual information"
        },
        {
            "WCAG 3.2.2", 
            "FEEDBACK: Provide immediate feedback for all user actions. " +
            "Confirmation messages, progress indicators, audio/visual feedback.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Button clicks with no visual/audio confirmation\n" +
            "- Actions that don't provide success/failure feedback\n" +
            "- Loading states without progress indication\n" +
            "- Form submissions without confirmation\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add hover effects and click animations\n" +
            "- Implement audio feedback for actions\n" +
            "- Provide loading spinners for async operations\n" +
            "- Show confirmation messages for important actions"
        },
        {
            "WCAG 1.4.4", 
            "CUSTOMIZATION: Allow text resizing up to 200% without loss of functionality. " +
            "Support system preferences for text size and contrast.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Fixed font sizes that don't scale\n" +
            "- Text that gets cut off when enlarged\n" +
            "- UI elements that break with larger text\n" +
            "- Ignoring system accessibility settings\n\n" +
            "FIX EXAMPLES:\n" +
            "- Use relative units (em, rem, %) instead of fixed pixels\n" +
            "- Test UI with 200% text enlargement\n" +
            "- Respect system accessibility settings\n" +
            "- Implement text size slider in options"
        },
        {
            "WCAG 2.1.1", 
            "INPUT ALTERNATIVES: All functionality must be keyboard accessible. " +
            "Avoid mouse-only interactions. Support assistive technologies.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Drag-and-drop puzzles requiring mouse\n" +
            "- Hover-based menus without keyboard alternative\n" +
            "- Touchscreen-only interactions\n" +
            "- Game mechanics requiring precise mouse control\n\n" +
            "FIX EXAMPLES:\n" +
            "- Add keyboard alternative (arrow keys + spacebar)\n" +
            "- Provide toggle/button alternatives for hover actions\n" +
            "- Support game controllers and keyboard navigation\n" +
            "- Ensure all functions work without mouse"
        },
        {
            "WCAG 2.1.2", 
            "NO KEYBOARD TRAP: Keyboard focus should never get trapped in a component. " +
            "Users must be able to navigate away using keyboard only.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Modal dialogs that can't be closed with keyboard\n" +
            "- Focus loops that can't be escaped\n" +
            "- Components that don't respond to escape key\n" +
            "- Custom controls that break tab navigation\n\n" +
            "FIX EXAMPLES:\n" +
            "- Ensure escape key closes all dialogs\n" +
            "- Implement proper focus trapping and release\n" +
            "- Test tab navigation through all components\n" +
            "- Provide clear keyboard exit strategies"
        },
        {
            "WCAG 2.5.1", 
            "POINTER GESTURES: Provide simple alternatives for complex gestures. " +
            "Not all users can perform precise pointer movements.\n\n" +
            "DEFECT EXAMPLES:\n" +
            "- Multi-finger touch gestures required\n" +
            "- Precise swiping motions without alternatives\n" +
            "- Gesture-based controls without button options\n\n" +
            "FIX EXAMPLES:\n" +
            "- Provide button alternatives for complex gestures\n" +
            "- Allow customization of gesture sensitivity\n" +
            "- Support multiple input methods for same action\n" +
            "- Add tutorial with alternative control methods"
        }
    };

    public static string GetRelevantKnowledge(string fileContent, string fileName)
    {
        // Advanced content analysis to determine relevant guidelines
        var relevantRules = new List<string>();
        
        var contentLower = fileContent.ToLower();
        
        // Detect contrast issues
        if (contentLower.Contains("color") || contentLower.Contains("contrast") || 
            contentLower.Contains("hex") || contentLower.Contains("rgb") ||
            contentLower.Contains("gui.color") || contentLower.Contains("guistyle"))
            relevantRules.Add("WCAG 1.4.3");
        
        // Detect audio/video issues
        if (contentLower.Contains("audio") || contentLower.Contains("sound") || 
            contentLower.Contains("video") || contentLower.Contains("subtitle") ||
            contentLower.Contains("audioclip") || contentLower.Contains("videoplayer"))
            relevantRules.AddRange(new[] { "WCAG 1.2.2", "WCAG 1.2.5" });
        
        // Detect timing issues
        if (contentLower.Contains("time") || contentLower.Contains("timer") || 
            contentLower.Contains("delay") || contentLower.Contains("timeout") ||
            contentLower.Contains("invoke") || contentLower.Contains("coroutine"))
            relevantRules.Add("WCAG 2.2.1");
        
        // Detect animation/motion issues
        if (contentLower.Contains("animation") || contentLower.Contains("move") || 
            contentLower.Contains("transform") || contentLower.Contains("shake") ||
            contentLower.Contains("particle") || contentLower.Contains("flash"))
            relevantRules.AddRange(new[] { "WCAG 2.3.3", "WCAG 2.3.1" });
        
        // Detect UI/focus issues
        if (contentLower.Contains("button") || contentLower.Contains("input") || 
            contentLower.Contains("selectable") || contentLower.Contains("focus") ||
            contentLower.Contains("ui") || contentLower.Contains("interface"))
            relevantRules.AddRange(new[] { "WCAG 2.4.7", "WCAG 2.4.6" });
        
        // Detect image/alt text issues
        if (contentLower.Contains("image") || contentLower.Contains("sprite") || 
            contentLower.Contains("texture") || contentLower.Contains("alt") ||
            contentLower.Contains("texture2d") || contentLower.Contains("rawimage"))
            relevantRules.Add("WCAG 1.1.1");
        
        // Detect feedback issues
        if (contentLower.Contains("feedback") || contentLower.Contains("message") || 
            contentLower.Contains("alert") || contentLower.Contains("debug.log") ||
            contentLower.Contains("console") || contentLower.Contains("toast"))
            relevantRules.Add("WCAG 3.2.2");
        
        // Detect text/customization issues
        if (contentLower.Contains("font") || contentLower.Contains("textsize") || 
            contentLower.Contains("scale") || contentLower.Contains("textmesh") ||
            contentLower.Contains("guiskin") || contentLower.Contains("dynamicfont"))
            relevantRules.Add("WCAG 1.4.4");
        
        // Detect input/control issues
        if (contentLower.Contains("keyboard") || contentLower.Contains("input") || 
            contentLower.Contains("mouse") || contentLower.Contains("controller") ||
            contentLower.Contains("input.getkey") || contentLower.Contains("input.getaxis"))
            relevantRules.AddRange(new[] { "WCAG 2.1.1", "WCAG 2.1.2" });
        
        // Detect gesture/touch issues
        if (contentLower.Contains("touch") || contentLower.Contains("gesture") || 
            contentLower.Contains("swipe") || contentLower.Contains("pinch") ||
            contentLower.Contains("input.touch") || contentLower.Contains("touchphase"))
            relevantRules.Add("WCAG 2.5.1");

        // Remove duplicates
        relevantRules = relevantRules.Distinct().ToList();
        
        // Build comprehensive RAG context
        var ragContext = new StringBuilder();
        ragContext.AppendLine("ACCESSIBILITY CONTEXT (RAG):");
        ragContext.AppendLine("Detailed WCAG guidelines with specific defect examples and fixes:");
        ragContext.AppendLine();
        
        foreach (var rule in relevantRules)
        {
            if (AccessibilityGuidelines.TryGetValue(rule, out var knowledge))
            {
                ragContext.AppendLine($"=== {rule} ===");
                ragContext.AppendLine(knowledge);
                ragContext.AppendLine();
            }
        }
        
        if (relevantRules.Count == 0)
        {
            ragContext.AppendLine("No specific accessibility rules detected in file content.");
            ragContext.AppendLine("Apply general accessibility principles and check for common issues.");
        }

        // Add general accessibility best practices
        ragContext.AppendLine("GENERAL ACCESSIBILITY BEST PRACTICES:");
        ragContext.AppendLine("- Test with screen readers and keyboard navigation");
        ragContext.AppendLine("- Provide multiple ways to complete actions");
        ragContext.AppendLine("- Use semantic HTML and proper ARIA attributes");
        ragContext.AppendLine("- Ensure color is not the only means of conveying information");
        ragContext.AppendLine("- Provide clear error messages and recovery options");

        return ragContext.ToString();
    }

    public static string GetDependencyAwareKnowledge(string fileContent, string fileName, string[] dependencies)
    {
        var knowledge = GetRelevantKnowledge(fileContent, fileName);
        
        if (dependencies != null && dependencies.Length > 0)
        {
            knowledge += "\nDEPENDENCY-RELATED ACCESSIBILITY CONCERNS:\n";
            knowledge += "- Accessibility issues can propagate through dependencies\n";
            knowledge += "- Changes in one file may affect accessibility in dependent files\n";
            knowledge += "- Consider cross-file accessibility impact analysis\n";
            knowledge += "- Shared components should maintain consistent accessibility standards\n";
            knowledge += "- Test accessibility across the entire dependency chain\n";
        }

        return knowledge;
    }

    public static string GetAllGuidelines()
    {
        var allGuidelines = new StringBuilder();
        allGuidelines.AppendLine("COMPLETE ACCESSIBILITY GUIDELINES DATABASE:");
        allGuidelines.AppendLine();
        
        foreach (var guideline in AccessibilityGuidelines)
        {
            allGuidelines.AppendLine($"=== {guideline.Key} ===");
            allGuidelines.AppendLine(guideline.Value);
            allGuidelines.AppendLine();
        }
        
        return allGuidelines.ToString();
    }
}