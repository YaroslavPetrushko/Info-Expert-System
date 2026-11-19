namespace Info_Extreme_System.Models;

/// <summary>
/// Статистичні дані бінаризації матриці X для окремого класу розпізнавання.
/// </summary>
public class BinaryClassStatInfo
{
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public bool IsBaseClass { get; set; }
    public int OnesCount { get; set; }
    public int TotalElements { get; set; }
    public double Percentage { get; set; }

    public string SummaryText => $"{ClassName}: {OnesCount:N0} з {TotalElements:N0} од. ({Percentage:F1}%)";
}
