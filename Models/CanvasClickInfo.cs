namespace Info_Extreme_System.Models;

/// <summary>
/// Дані про подію кліку мишею по Canvas (координати та розміри елемента).
/// </summary>
public record CanvasClickInfo(double X, double Y, double ActualWidth, double ActualHeight);
