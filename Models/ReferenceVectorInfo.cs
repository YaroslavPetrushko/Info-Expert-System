using System.Windows.Media.Imaging;

namespace Info_Extreme_System.Models;

/// <summary>
/// Інформація про двійковий еталонний вектор EV класу та його візуалізація у вигляді штрих-коду.
/// </summary>
public class ReferenceVectorInfo
{
    public int ClassIndex { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public bool IsBaseClass { get; set; }
    public int OnesCount { get; set; }
    public int TotalFeatures { get; set; }
    public double Percentage { get; set; }
    public BitmapSource? BarcodeImage { get; set; }

    public string SummaryText => $"{ClassName}: {OnesCount} з {TotalFeatures} од. ({Percentage:F1}%)";
}
