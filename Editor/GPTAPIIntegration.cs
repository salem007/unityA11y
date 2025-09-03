using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Threading;

public static class GPTAPIIntegration
{
    private const string ChatEndpoint = "https://api.openai.com/v1/chat/completions";

    // Rate limiting variables
    private static DateTime _lastApiCallTime = DateTime.MinValue;
    private static readonly TimeSpan _minTimeBetweenCalls = TimeSpan.FromMilliseconds(1500);
    private static readonly object _apiLock = new object();
    private static int _concurrentRequests = 0;
    private const int _maxConcurrentRequests = 3;
    private static readonly TimeSpan _apiTimeout = TimeSpan.FromSeconds(45); // 45 second timeout

    public static async Task<bool> TestApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) 
        {
            Debug.LogError("API key is empty");
            return false;
        }
        
        // Basic validation
        if (!apiKey.StartsWith("sk-"))
        {
            Debug.LogError("API key format appears invalid. OpenAI keys typically start with 'sk-'");
            return false;
        }
        
        // Check key length
        if (apiKey.Length < 40)
        {
            Debug.LogWarning("API key appears too short. Please verify your key.");
        }
        else if (apiKey.Length > 250)
        {
            Debug.LogWarning("API key appears too long. Please verify your key.");
        }
        
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var resp = await client.GetAsync("https://api.openai.com/v1/models");
            string responseContent = await resp.Content.ReadAsStringAsync();
            
            if (resp.IsSuccessStatusCode)
            {
                Debug.Log("API key test successful");
                return true;
            }
            else
            {
                Debug.LogError($"API key test failed with status: {resp.StatusCode}, Response: {responseContent}");
                
                try
                {
                    var errorJson = JObject.Parse(responseContent);
                    var errorMessage = errorJson["error"]?["message"]?.ToString();
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        Debug.LogError($"Error details: {errorMessage}");
                    }
                }
                catch
                {
                    // If JSON parsing fails, use raw response
                }
                
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"API key test failed: {ex.Message}");
            return false;
        }
    }

    private static async Task WaitForRateLimit(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_apiLock)
            {
                while (_concurrentRequests >= _maxConcurrentRequests)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException();
                        
                    Monitor.Wait(_apiLock, 100);
                }
                _concurrentRequests++;

                var timeSinceLastCall = DateTime.Now - _lastApiCallTime;
                if (timeSinceLastCall < _minTimeBetweenCalls)
                {
                    var delayTime = _minTimeBetweenCalls - timeSinceLastCall;
                    Debug.Log($"Rate limiting: Waiting {delayTime.TotalMilliseconds}ms");
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException();
                        
                    Thread.Sleep(delayTime);
                }
                _lastApiCallTime = DateTime.Now;
            }
        }, cancellationToken);
    }

    private static void ReleaseRateLimit()
    {
        lock (_apiLock)
        {
            _concurrentRequests--;
            Monitor.Pulse(_apiLock);
        }
    }

    public static async Task<string> AnalyzeFileAsync(string fileContent, string apiKey, string fileName, string model = "gpt-4o", CancellationToken cancellationToken = default)
    {
        return await AnalyzeFileWithDependenciesAsync(fileContent, apiKey, fileName, null, model, 3, cancellationToken);
    }

    public static async Task<string> AnalyzeFileWithDependenciesAsync(string fileContent, string apiKey, string fileName, string[] dependencies, string model = "gpt-4o", int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await WaitForRateLimit(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                
                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new ArgumentException("API key is empty");

                // Generate RAG context
                string ragContext = dependencies != null ? 
                    RAGKnowledgeBase.GetDependencyAwareKnowledge(fileContent, fileName, dependencies) :
                    RAGKnowledgeBase.GetRelevantKnowledge(fileContent, fileName);

                string dependencyContext = BuildDependencyContext(dependencies);

                var prompt = $@"You are an accessibility auditor for Unity/C# projects.
{ragContext}
{dependencyContext}

Refer to WCAG 2.2 standards: https://www.w3.org/WAI/WCAG22/quickref/

Check ONLY for these specific accessibility issues and their WCAG rules:

1. Low Contrast Text or Objects — WCAG 1.4.3
2. No Subtitles or Captions for Audio — WCAG 1.2.2
3. No Audio Description / Visual-only Info — WCAG 1.2.5
4. Time-Limited Interactions — WCAG 2.2.1
5. Fast / Jerky Object Motion — WCAG 2.3.3
6. Camera Movement / Disorientation — WCAG 2.3.3
7. Excessive Motion Blur / Flashing Effects — WCAG 2.3.1
8. No Focus Indicators / Keyboard Trap — WCAG 2.4.7
9. Poor UI Structure or Navigation — WCAG 2.4.6
10. Missing Alternative Text for Objects — WCAG 1.1.1
11. No Feedback for Interactions — WCAG 3.2.2
12. Lack of Customization (text size, contrast) — WCAG 1.4.4
13. Lack of Input Alternatives (e.g., no mouse) — WCAG 2.1.1
14. Keyboard Trap (cannot escape with keyboard) — WCAG 2.1.2
15. Complex Pointer Gestures without alternatives — WCAG 2.5.1

Analyze the following file: '{fileName}'

Return ONLY a JSON array. Each element must have:
- line (integer, line number where the issue occurs or 0 if unknown)
- severity (Critical | Warning | Info)
- description (short explanation of the detected issue)
- recommendation (very short fix suggestion, max 15 words)
- wcag_rule (WCAG code as listed above)

CRITICAL severity for: Flashing effects, keyboard traps, no focus indicators, contrast below 3:1
WARNING severity for: Missing captions, no audio description, time limits, complex gestures
INFO severity for: Minor UI issues, missing feedback, customization options

If no issues found, return [].

File content:
{fileContent}";

                var bodyObj = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 2500,
                    temperature = 0.0
                };

                string jsonBody = JsonConvert.SerializeObject(bodyObj);

                using var request = new HttpRequestMessage(HttpMethod.Post, ChatEndpoint);
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                client.Timeout = _apiTimeout;
                
                var resp = await client.SendAsync(request, cancellationToken);
                string respText = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    string errorDetails = await resp.Content.ReadAsStringAsync();
                    Debug.LogError($"OpenAI API error ({resp.StatusCode}): {errorDetails}");
                    
                    try
                    {
                        var errorJson = JObject.Parse(errorDetails);
                        var errorMessage = errorJson["error"]?["message"]?.ToString();
                        if (!string.IsNullOrEmpty(errorMessage))
                        {
                            Debug.LogError($"API Error details: {errorMessage}");
                            
                            if (errorMessage.ToLower().Contains("invalid api key") || 
                                errorMessage.ToLower().Contains("authentication") ||
                                errorMessage.ToLower().Contains("incorrect api key"))
                            {
                                Debug.LogError("Authentication failed. Please check your API key.");
                            }
                            
                            if (errorMessage.ToLower().Contains("rate limit") && attempt < maxRetries - 1)
                            {
                                int delaySeconds = 10 * (attempt + 1);
                                Debug.LogWarning($"Rate limit hit. Retrying in {delaySeconds} seconds (attempt {attempt + 1}/{maxRetries})");
                                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                                continue;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        Debug.LogError("Could not parse API error response as JSON");
                    }
                    catch (Exception parseEx)
                    {
                        Debug.LogError($"Error parsing API response: {parseEx.Message}");
                    }
                    
                    return "[]";
                }

                var j = JObject.Parse(respText);
                var content = j["choices"]?.First?["message"]?["content"]?.ToString();
                
                if (string.IsNullOrWhiteSpace(content))
                {
                    Debug.LogError("Empty response from OpenAI API");
                    return "[]";
                }

                Debug.Log($"Raw API response: {content}");
                
                ReleaseRateLimit();
                return content;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Debug.LogError($"API timeout for {fileName} (attempt {attempt + 1})");
                ReleaseRateLimit();
                
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5 * (attempt + 1)), cancellationToken);
                    continue;
                }
                return "[]";
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Scan cancelled during API call for {fileName}");
                ReleaseRateLimit();
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AnalyzeFileAsync error (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                ReleaseRateLimit();
                
                if (attempt < maxRetries - 1)
                {
                    int delaySeconds = 5 * (attempt + 1);
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                    continue;
                }
                
                return "[]";
            }
        }
        
        return "[]";
    }

    private static string BuildDependencyContext(string[] dependencies)
    {
        if (dependencies == null || dependencies.Length == 0)
            return "";

        var context = new StringBuilder();
        context.AppendLine("🔗 DEPENDENCY CONTEXT:");
        context.AppendLine("This file is part of a larger system with these related files:");
        
        // Group dependencies by type
        var dependencyGroups = dependencies
            .GroupBy(d => System.IO.Path.GetExtension(d).ToLower())
            .OrderBy(g => g.Key);
        
        foreach (var group in dependencyGroups)
        {
            context.AppendLine($"\n📁 {GetFileTypeDescription(group.Key)}:");
            foreach (string dependency in group.Take(5)) // Show max 5 per type
            {
                string fileName = System.IO.Path.GetFileName(dependency);
                context.AppendLine($"   - {fileName}");
            }
            if (group.Count() > 5)
            {
                context.AppendLine($"   - ... and {group.Count() - 5} more");
            }
        }
        
        context.AppendLine("\n🔍 DEPENDENCY-SPECIFIC CHECKS:");
        context.AppendLine("1. Check for accessibility issues that propagate between files");
        context.AppendLine("2. Verify consistent UI patterns across dependent components");
        context.AppendLine("3. Ensure shared color schemes meet contrast requirements");
        context.AppendLine("4. Confirm navigation works across file boundaries");
        context.AppendLine("5. Validate input alternatives are available in all dependent files");
        
        context.AppendLine("\n⚠️ COMMON CROSS-FILE ACCESSIBILITY ISSUES:");
        context.AppendLine("- Inconsistent focus management between components");
        context.AppendLine("- Color scheme inconsistencies causing contrast violations");
        context.AppendLine("- Missing audio feedback in some dependent files");
        context.AppendLine("- Keyboard traps when navigating between components");
        context.AppendLine("- Incomplete alternative text propagation");

        return context.ToString();
    }

    private static string GetFileTypeDescription(string extension)
    {
        return extension switch
        {
            ".cs" => "Scripts",
            ".unity" => "Scenes",
            ".prefab" => "Prefabs",
            ".shader" => "Shaders",
            ".anim" => "Animations",
            ".mat" => "Materials",
            ".asset" => "Assets",
            ".png" => "Textures",
            ".jpg" => "Textures",
            ".fbx" => "3D Models",
            ".wav" => "Audio Files",
            ".mp3" => "Audio Files",
            _ => "Other Files"
        };
    }

    public static List<AccessibilityIssue> ParseResponse(string response, string filePath)
    {
        var issues = new List<AccessibilityIssue>();
        if (string.IsNullOrWhiteSpace(response)) 
        {
            Debug.LogWarning("Empty response received for parsing");
            return issues;
        }

        try
        {
            string cleaned = ExtractFirstJsonArray(response);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                Debug.LogWarning("No JSON array found in response. Response was: " + response);
                return issues;
            }

            var arr = JArray.Parse(cleaned);
            foreach (var item in arr)
            {
                try
                {
                    var fullRec = item["recommendation"]?.Value<string>()?.Trim() ?? "";
                    var description = item["description"]?.Value<string>()?.Trim() ?? "";
                    var wcagRule = item["wcag_rule"]?.Value<string>()?.Trim() ?? "";

                    // Skip if essential fields are missing
                    if (string.IsNullOrEmpty(description))
                    {
                        Debug.LogWarning("Skipping issue with empty description");
                        continue;
                    }

                    var issue = new AccessibilityIssue
                    {
                        FilePath = filePath,
                        LineNumber = item["line"]?.Value<int?>() ?? 0,
                        Severity = ParseSeverity(item["severity"]?.Value<string>()),
                        Description = description,
                        RecommendationFull = fullRec,
                        Recommendation = Truncate(fullRec, 15),
                        WCAGRule = wcagRule
                    };

                    issues.Add(issue);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing issue item: {ex.Message}\nItem: {item}");
                }
            }

            Debug.Log($"Successfully parsed {issues.Count} issues from response");
        }
        catch (JsonException jsonEx)
        {
            Debug.LogError($"JSON parsing failed: {jsonEx.Message}\nResponse: {response}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ParseResponse failed: {ex.Message}\nResponse: {response}");
        }

        return issues;
    }

    private static string ExtractFirstJsonArray(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Look for the first occurrence of [
            int start = text.IndexOf('[');
            if (start < 0)
                return null;

            // Look for the matching ]
            int depth = 1;
            int end = start + 1;

            while (end < text.Length && depth > 0)
            {
                if (text[end] == '[')
                    depth++;
                else if (text[end] == ']')
                    depth--;

                end++;
            }

            if (depth == 0 && end > start)
            {
                string extracted = text.Substring(start, end - start);
                
                // Validate it's proper JSON
                try
                {
                    JArray.Parse(extracted);
                    return extracted;
                }
                catch
                {
                    // If parsing fails, try alternative extraction
                    return ExtractJsonArrayAlternative(text);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error extracting JSON array: {ex.Message}");
        }

        return ExtractJsonArrayAlternative(text);
    }

    private static string ExtractJsonArrayAlternative(string text)
    {
        // Alternative extraction method using regex
        try
        {
            var match = Regex.Match(text, @"\[.*\]", RegexOptions.Singleline);
            if (match.Success)
            {
                string extracted = match.Value;
                // Validate it's proper JSON
                JArray.Parse(extracted);
                return extracted;
            }
        }
        catch
        {
            // If regex extraction fails, return null
        }

        return null;
    }

    // Unity display (short version)
    private static string Truncate(string recommendation, int maxWords = 15)
    {
        if (string.IsNullOrWhiteSpace(recommendation)) 
            return "";
        
        var words = recommendation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= maxWords ? 
            recommendation.Trim() : 
            string.Join(" ", words.Take(maxWords)) + "...";
    }

    // CSV export (longer version)
    public static string TruncateForCSV(string recommendation)
    {
        return Truncate(recommendation, 100);
    }

    private static IssueSeverity ParseSeverity(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return IssueSeverity.Info;

        return input.ToLower() switch
        {
            "critical" => IssueSeverity.Critical,
            "warning" => IssueSeverity.Warning,
            "error" => IssueSeverity.Critical, // Map error to critical
            "high" => IssueSeverity.Critical, // Map high to critical
            "medium" => IssueSeverity.Warning, // Map medium to warning
            "low" => IssueSeverity.Info, // Map low to info
            _ => IssueSeverity.Info
        };
    }

    // Helper method to validate API responses
    public static bool IsValidResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return false;

        try
        {
            string cleaned = ExtractFirstJsonArray(response);
            if (string.IsNullOrWhiteSpace(cleaned))
                return false;

            JArray.Parse(cleaned);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Method to get API usage statistics (for debugging)
    public static void LogApiStatistics(string response)
    {
        if (!string.IsNullOrWhiteSpace(response))
        {
            try
            {
                string cleaned = ExtractFirstJsonArray(response);
                if (!string.IsNullOrEmpty(cleaned))
                {
                    var arr = JArray.Parse(cleaned);
                    Debug.Log($"API returned {arr.Count} accessibility issues");
                }
            }
            catch
            {
                // Ignore errors in statistics logging
            }
        }
    }
}