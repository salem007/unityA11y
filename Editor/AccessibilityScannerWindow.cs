using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System;
using System.Threading;

public class AccessibilityScannerWindow : EditorWindow
{
    private enum ScanTarget { EntireProject, Dependencies }

    private string _apiKey = "";
    private Vector2 _scrollPosition;
    private Vector2 _filesScrollPosition;
    private List<AccessibilityIssue> _issues = new List<AccessibilityIssue>();
    private ScanTarget _target = ScanTarget.EntireProject;
    private bool _isScanning = false;
    private List<string> _scannedFiles = new List<string>();
    private CancellationTokenSource _scanCancellation;
    private AccessibilityIssue _selectedIssue;

    private readonly string[] _extensionsToScan = new string[] { 
        ".cs", ".unity", ".shader", ".prefab", ".anim", ".mat", ".asset" 
    };

    [MenuItem("Tools/UnityA11y Scanner")]
    public static void ShowWindow()
    {
        GetWindow<AccessibilityScannerWindow>("UnityA11y Scanner");
    }

    private void OnEnable()
    {
        _apiKey = EditorPrefs.GetString("UnityA11y_ApiKey", "");
    }

    private void OnDisable()
    {
        _scanCancellation?.Cancel();
        _scanCancellation?.Dispose();
    }

    private void OnGUI()
    {
        GUILayout.Label("Unity Accessibility Scanner", EditorStyles.boldLabel);
        
        // API key field
        EditorGUI.BeginChangeCheck();
        _apiKey = EditorGUILayout.PasswordField("OpenAI API Key", _apiKey);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString("UnityA11y_ApiKey", _apiKey);
        }

        _target = (ScanTarget)EditorGUILayout.EnumPopup("Scan Target", _target);

        var selection = Selection.activeObject;
        string selectedPath = "";
        
        if (_target == ScanTarget.Dependencies)
        {
            if (selection != null)
            {
                selectedPath = AssetDatabase.GetAssetPath(selection);
                EditorGUILayout.LabelField("Selected:", Path.GetFileName(selectedPath));
                
                string absPath = Path.Combine(Application.dataPath, "..", selectedPath);
                if (!File.Exists(absPath))
                {
                    EditorGUILayout.HelpBox("Selected file doesn't exist!", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Please select a file in Project window for dependency analysis", MessageType.Info);
            }

            EditorGUILayout.HelpBox("Will scan the selected file AND all its dependencies", MessageType.Info);
            
            if (selection != null)
            {
                bool hasDeps = DependencyAnalyzer.HasDependencies(selectedPath);
                
                using (new EditorGUI.DisabledScope(!hasDeps))
                {
                    if (GUILayout.Button("Show Dependencies"))
                    {
                        Debug.Log($"Showing dependencies for: {selectedPath}");
                        DependencyAnalyzer.LogDependencies(selectedPath);
                    }
                }
                
                if (!hasDeps)
                {
                    EditorGUILayout.HelpBox("No dependencies found for this file", MessageType.Warning);
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Will scan all files in the project", MessageType.Info);
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_isScanning || (_target == ScanTarget.Dependencies && selection == null)))
        {
            if (GUILayout.Button("Start Scan")) _ = StartScanAsync();
            
            if (_isScanning && GUILayout.Button("Cancel Scan"))
            {
                _scanCancellation?.Cancel();
            }
            
            if (GUILayout.Button("Clear Results")) ClearResults();
            if (_issues.Count > 0 && GUILayout.Button("Export CSV")) ExportCSV();
        }

        EditorGUILayout.Space();
        
        if (_scannedFiles.Count > 0)
        {
            GUILayout.Label($"Files scanned: {_scannedFiles.Count}", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box", GUILayout.Height(100));
            _filesScrollPosition = EditorGUILayout.BeginScrollView(_filesScrollPosition, 
                GUILayout.Height(100));
            
            foreach (var file in _scannedFiles)
            {
                EditorGUILayout.LabelField(Path.GetFileName(file), EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        GUILayout.Label($"Issues found: {_issues.Count}", EditorStyles.boldLabel);

        // Display selected issue details
        if (_selectedIssue != null)
        {
            EditorGUILayout.HelpBox(
                $"SELECTED ISSUE:\n" +
                $"File: {_selectedIssue.FilePath}\n" +
                $"Line: {(_selectedIssue.LineNumber > 0 ? _selectedIssue.LineNumber.ToString() : "Unknown")}\n" +
                $"Severity: {_selectedIssue.Severity}\n" +
                $"WCAG: {_selectedIssue.WCAGRule}\n\n" +
                $"Double-click to open file at this line\n" +
                $"Click elsewhere to deselect",
                MessageType.Info);
            
            if (GUILayout.Button("Open File at Line"))
            {
                OpenFileAtLine(_selectedIssue.FilePath, _selectedIssue.LineNumber);
            }
        }

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(350));
        
        for (int i = 0; i < _issues.Count; i++)
        {
            var issue = _issues[i];
            MessageType mt = MessageType.Info;
            if (issue.Severity == IssueSeverity.Critical) mt = MessageType.Error;
            else if (issue.Severity == IssueSeverity.Warning) mt = MessageType.Warning;

            // Create a box for each issue with better visual feedback
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            if (_selectedIssue == issue)
            {
                boxStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.3f));
            }

            EditorGUILayout.BeginVertical(boxStyle);
            
            // Main issue display
            EditorGUILayout.HelpBox(
                $"File: {Path.GetFileName(issue.FilePath)}\n" +
                $"Line: {(issue.LineNumber > 0 ? issue.LineNumber.ToString() : "Unknown")}\n" +
                $"Severity: {issue.Severity}\n" +
                $"WCAG: {issue.WCAGRule}\n\n" +
                $"{issue.Description}\n\n" +
                $"Fix: {issue.Recommendation}",
                mt);
            
            EditorGUILayout.EndVertical();

            // Handle mouse events
            Rect issueRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && issueRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount == 1)
                {
                    // Single click: select issue
                    _selectedIssue = issue;
                    Repaint();
                    Event.current.Use();
                }
                else if (Event.current.clickCount == 2)
                {
                    // Double click: open file
                    OpenFileAtLine(issue.FilePath, issue.LineNumber);
                    Event.current.Use();
                }
            }
        }
        
        // Handle background click to deselect
        if (Event.current.type == EventType.MouseDown && _selectedIssue != null)
        {
            _selectedIssue = null;
            Repaint();
        }

        GUILayout.EndScrollView();
    }

    // Helper to create texture for selected background
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OpenFileAtLine(string filePath, int lineNumber)
    {
        if (string.IsNullOrEmpty(filePath)) 
        {
            Debug.LogWarning("No file path provided");
            return;
        }
        
        // Convert relative asset path to absolute path
        string absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", filePath));
        
        if (!File.Exists(absolutePath))
        {
            Debug.LogWarning($"File not found: {absolutePath}");
            EditorUtility.DisplayDialog("File Not Found", 
                $"Could not find file: {filePath}\n\n" +
                "The file may have been moved, renamed, or deleted.", "OK");
            return;
        }

        try
        {
            // Highlight the file in Project window first
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            // Open the file in default editor at the specific line
            if (lineNumber > 0)
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(absolutePath, lineNumber);
                Debug.Log($"Opening {filePath} at line {lineNumber}");
            }
            else
            {
                // If no line number, just open the file
                System.Diagnostics.Process.Start(absolutePath);
                Debug.Log($"Opening {filePath}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to open file: {ex.Message}");
            EditorUtility.DisplayDialog("Open Failed", 
                $"Could not open file: {ex.Message}", "OK");
        }
    }

    private void ClearResults()
    {
        _issues.Clear();
        _scannedFiles.Clear();
        _selectedIssue = null;
    }

    private async Task StartScanAsync()
    {
        ClearResults();

        if (_isScanning) return;

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            EditorUtility.DisplayDialog("API Key missing", "Please enter your OpenAI API key.", "OK");
            return;
        }

        if (_target == ScanTarget.Dependencies && Selection.activeObject == null)
        {
            EditorUtility.DisplayDialog("No file selected", "Please select a file for dependency analysis.", "OK");
            return;
        }

        _isScanning = true;
        _scanCancellation = new CancellationTokenSource();

        try
        {
            // Test API key
            bool keyOk = await GPTAPIIntegration.TestApiKeyAsync(_apiKey);
            if (!keyOk)
            {
                EditorUtility.DisplayDialog("API Key invalid", 
                    "Please check your OpenAI API key:\n\n" +
                    "1. Get a valid API key from https://platform.openai.com/api-keys\n" +
                    "2. Ensure you have sufficient credits\n" +
                    "3. Check that the key starts with 'sk-'\n\n" +
                    "The key will be saved for future use.", "OK");
                return;
            }

            // Get files to scan
            List<string> filesToScan = GetFilesToScan();
            if (filesToScan.Count == 0)
            {
                EditorUtility.DisplayDialog("No files found", "No matching files found to scan.", "OK");
                return;
            }

            Debug.Log($"Starting scan of {filesToScan.Count} files");
            _scannedFiles = filesToScan.Select(f => GetRelativeAssetPath(f)).ToList();

            // Get dependencies for context if needed
            string[] dependencyPaths = null;
            if (_target == ScanTarget.Dependencies && Selection.activeObject != null)
            {
                string mainPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                dependencyPaths = DependencyAnalyzer.GetDependencyPaths(mainPath);
                Debug.Log($"Using {dependencyPaths?.Length} dependencies for context");
            }

            int total = filesToScan.Count;
            int batchSize = 2;
            int delayBetweenBatchesMs = 2000;
            
            for (int i = 0; i < total; i += batchSize)
            {
                _scanCancellation.Token.ThrowIfCancellationRequested();
                
                int currentBatchSize = Math.Min(batchSize, total - i);
                
                EditorUtility.DisplayProgressBar("UnityA11y Scanner", 
                    $"Scanning batch {i/batchSize + 1}/{(total + batchSize - 1)/batchSize}\n" +
                    $"Files: {i + 1}-{Math.Min(i + batchSize, total)} of {total}", 
                    (float)i / total);

                var batchTasks = new List<Task>();
                for (int j = 0; j < currentBatchSize; j++)
                {
                    int fileIndex = i + j;
                    batchTasks.Add(ProcessFileAsync(filesToScan[fileIndex], dependencyPaths, _scanCancellation.Token));
                }
                
                await Task.WhenAll(batchTasks);
                
                if (i + batchSize < total)
                {
                    await Task.Delay(delayBetweenBatchesMs, _scanCancellation.Token);
                }
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Scan completed", 
                $"Files scanned: {total}\nIssues found: {_issues.Count}", "OK");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Scan cancelled by user");
            EditorUtility.ClearProgressBar();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Scan failed: {ex.Message}");
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Scan Failed", $"Error during scan: {ex.Message}", "OK");
        }
        finally
        {
            _isScanning = false;
            Repaint();
        }
    }

    private async Task ProcessFileAsync(string filePath, string[] dependencyPaths, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            string content = File.ReadAllText(filePath);
            cancellationToken.ThrowIfCancellationRequested();
            
            string relativePath = GetRelativeAssetPath(filePath);
            string rawResponse;

            if (_target == ScanTarget.Dependencies && dependencyPaths != null && dependencyPaths.Length > 0)
            {
                rawResponse = await GPTAPIIntegration.AnalyzeFileWithDependenciesAsync(
                    content, _apiKey, Path.GetFileName(filePath), dependencyPaths, 
                    cancellationToken: cancellationToken);
            }
            else
            {
                rawResponse = await GPTAPIIntegration.AnalyzeFileAsync(
                    content, _apiKey, Path.GetFileName(filePath), cancellationToken: cancellationToken);
            }
            
            var issuesFromFile = GPTAPIIntegration.ParseResponse(rawResponse, relativePath);
            if (issuesFromFile.Count > 0) 
            {
                _issues.AddRange(issuesFromFile);
                Debug.Log($"Found {issuesFromFile.Count} issues in {Path.GetFileName(filePath)}");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"Cancelled processing: {filePath}");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error scanning {filePath}: {ex.Message}");
        }
    }

    private List<string> GetFilesToScan()
    {
        var files = new List<string>();
        
        if (_target == ScanTarget.Dependencies)
        {
            var sel = Selection.activeObject;
            if (sel == null) 
            {
                Debug.Log("No file selected for dependency scan");
                return files;
            }
            
            string path = AssetDatabase.GetAssetPath(sel);
            if (string.IsNullOrWhiteSpace(path)) 
                return files;

            string absMainPath = Path.Combine(Application.dataPath, "..", path);
            if (File.Exists(absMainPath))
            {
                files.Add(absMainPath);
            }

            var dependencyFiles = DependencyAnalyzer.GetDependencyFiles(path, _extensionsToScan);
            files.AddRange(dependencyFiles);
        }
        else
        {
            string dataPath = Application.dataPath;
            var allFiles = Directory.GetFiles(dataPath, "*.*", SearchOption.AllDirectories)
                .Where(f => _extensionsToScan.Contains(Path.GetExtension(f).ToLower()))
                .ToList();
                
            files.AddRange(allFiles);
        }
        
        return files.Distinct().ToList();
    }

    private string GetRelativeAssetPath(string absolutePath)
    {
        var projectPath = Application.dataPath.Replace("\\", "/");
        var abs = absolutePath.Replace("\\", "/");
        return abs.StartsWith(projectPath) ? abs.Substring(projectPath.Length).TrimStart('/') : absolutePath;
    }

    private void ExportCSV()
    {
        string path = EditorUtility.SaveFilePanel("Export CSV", "", "AccessibilityReport.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;

        var sb = new StringBuilder();
        sb.AppendLine("File Path,Line,Severity,Description,Recommendation,WCAG Rule");

        foreach (var issue in _issues)
        {
            string recCSV = issue.RecommendationFull.Replace("\"", "\"\"");
            sb.AppendLine($"\"{issue.FilePath}\",{issue.LineNumber},{issue.Severity},\"{issue.Description}\",\"{recCSV}\",{issue.WCAGRule}");
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        EditorUtility.DisplayDialog("Export completed", $"CSV file saved at:\n{path}", "OK");
    }
}