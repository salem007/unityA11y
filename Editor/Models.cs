public class AccessibilityIssue
{
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public IssueSeverity Severity { get; set; }
    public string Description { get; set; }

    // Version courte pour affichage dans Unity (≤ 15 mots)
    public string Recommendation { get; set; }

    // Version complète pour le CSV (≤ 100 mots)
    public string RecommendationFull { get; set; }

    public string WCAGRule { get; set; }
}

public enum IssueSeverity
{
    Critical,
    Warning,
    Info
}