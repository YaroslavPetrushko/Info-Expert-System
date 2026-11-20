namespace Info_Extreme_System.Models;

/// <summary>
/// Числові та екранні характеристики обраної ознаки на графіку СКД (Вкладка 2).
/// </summary>
public class TolerancePointDetails
{
    public int FeatureIndex { get; init; }
    public int FeatureNumber => FeatureIndex + 1;
    public int TotalFeatures { get; init; }

    public double Mean { get; init; }
    public double VD { get; init; }
    public double ND { get; init; }
    public int StepKD { get; init; }

    public string EvSummary { get; init; } = string.Empty;

    // Екранні координати на полотні Canvas (700x200) для малювання оверлеїв
    public double CanvasX { get; init; }
    public double MeanCanvasY { get; init; }
    public double VdCanvasY { get; init; }
    public double NdCanvasY { get; init; }
}
