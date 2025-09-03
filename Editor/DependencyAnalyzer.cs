using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class DependencyAnalyzer
{
    public static List<string> GetDependencyFiles(string assetPath, string[] extensionsToInclude)
    {
        var files = new List<string>();
        
        if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
        {
            Debug.LogWarning($"Asset path is invalid or file doesn't exist: {assetPath}");
            return files;
        }

        // Obtenir toutes les dépendances récursives
        string[] allDependencies = AssetDatabase.GetDependencies(assetPath, true);
        Debug.Log($"Found {allDependencies.Length} dependencies for {assetPath}");
        
        foreach (string dependencyPath in allDependencies)
        {
            // Skip self
            if (dependencyPath == assetPath) 
                continue;

            string extension = Path.GetExtension(dependencyPath).ToLower();
            if (extensionsToInclude.Contains(extension))
            {
                string absPath = Path.Combine(Directory.GetCurrentDirectory(), dependencyPath);
                if (File.Exists(absPath))
                {
                    files.Add(absPath);
                    Debug.Log($"Added dependency: {Path.GetFileName(dependencyPath)}");
                }
                else
                {
                    Debug.LogWarning($"Dependency file not found: {absPath}");
                }
            }
        }

        Debug.Log($"Total dependency files to scan: {files.Count}");
        return files.Distinct().ToList();
    }

    public static string[] GetDependencyPaths(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("Asset path is empty");
            return new string[0];
        }

        if (!File.Exists(assetPath))
        {
            Debug.LogWarning($"Asset file doesn't exist: {assetPath}");
            return new string[0];
        }

        string[] allDependencies = AssetDatabase.GetDependencies(assetPath, true);
        return allDependencies.Where(d => d != assetPath).ToArray();
    }

    public static void LogDependencies(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogError("Cannot show dependencies: no asset selected");
            return;
        }

        if (!File.Exists(assetPath))
        {
            Debug.LogError($"Asset file doesn't exist: {assetPath}");
            return;
        }

        string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);
        
        Debug.Log($"=== DEPENDENCIES FOR {Path.GetFileName(assetPath)} ===");
        Debug.Log($"Asset path: {assetPath}");
        Debug.Log($"Total dependencies (including self): {dependencies.Length}");
        
        if (dependencies.Length == 1) // Only self
        {
            Debug.Log("No dependencies found (only the file itself)");
            return;
        }

        int dependencyCount = 0;
        foreach (string dep in dependencies)
        {
            if (dep != assetPath)
            {
                dependencyCount++;
                string absPath = Path.Combine(Directory.GetCurrentDirectory(), dep);
                string exists = File.Exists(absPath) ? "✓" : "✗";
                Debug.Log($"{exists} {dep}");
            }
        }
        
        Debug.Log($"=== TOTAL DEPENDENCIES: {dependencyCount} ===");

        // Show by file type
        var byType = dependencies
            .Where(d => d != assetPath)
            .GroupBy(d => Path.GetExtension(d).ToLower())
            .OrderByDescending(g => g.Count());

        Debug.Log("=== DEPENDENCIES BY TYPE ===");
        foreach (var group in byType)
        {
            Debug.Log($"{group.Key}: {group.Count()} files");
            foreach (var file in group.Take(5))
            {
                Debug.Log($"   - {Path.GetFileName(file)}");
            }
            if (group.Count() > 5)
            {
                Debug.Log($"   - ... and {group.Count() - 5} more");
            }
        }
    }

    public static bool HasDependencies(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return false;
        string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);
        return dependencies.Length > 1; // More than just itself
    }
}