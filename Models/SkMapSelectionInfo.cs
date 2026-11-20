namespace Info_Extreme_System.Models;

public enum SkMapSelectionType
{
    None,
    Center,
    Realization
}

/// <summary>
/// Стан виділення інтерактивного елемента на карті SK_MAP (Вкладка 3).
/// Містить геометричні параметри оверлеїв для малювання на полотні та текстовий опис для інфопанелі.
/// </summary>
public class SkMapSelectionInfo
{
    public SkMapSelectionType SelectionType { get; init; } = SkMapSelectionType.None;
    public bool HasSelection => SelectionType != SkMapSelectionType.None;
    public bool IsCenterSelected => SelectionType == SkMapSelectionType.Center;
    public bool IsRealizationSelected => SelectionType == SkMapSelectionType.Realization;

    public string Title { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string SubDetails { get; init; } = string.Empty;

    // --- Оверлей 1: Пунктирне коло приблизного радіуса навколо центру ---
    public bool IsCircleVisible { get; init; }
    public double CircleLeft { get; init; }
    public double CircleTop { get; init; }
    public double CircleDiameter { get; init; }
    public string CircleStroke { get; init; } = "#2563EB";
    public string CircleFill { get; init; } = "#152563EB";

    // --- Оверлей 2: Пунктирний відрізок-радіус від центру до найдальшої точки ---
    public bool IsRadiusLineVisible { get; init; }
    public double RadiusLineX1 { get; init; }
    public double RadiusLineY1 { get; init; }
    public double RadiusLineX2 { get; init; }
    public double RadiusLineY2 { get; init; }
    public string RadiusLineStroke { get; init; } = "#2563EB";

    // --- Оверлей 3: Пунктирні лінії триангуляції від точки до C1 та C2 ---
    public bool IsTriangulationVisible { get; init; }
    public double TriLine1X1 { get; init; }
    public double TriLine1Y1 { get; init; }
    public double TriLine1X2 { get; init; }
    public double TriLine1Y2 { get; init; }
    public double TriLine2X1 { get; init; }
    public double TriLine2Y1 { get; init; }
    public double TriLine2X2 { get; init; }
    public double TriLine2Y2 { get; init; }

    // --- Оверлей 4: Кільце підсвітки виділеного об'єкта (центру або точки) ---
    public bool IsHighlightVisible { get; init; }
    public double HighlightLeft { get; init; }
    public double HighlightTop { get; init; }
    public double HighlightDiameter { get; init; } = 18.0;
    public string HighlightStroke { get; init; } = "#2563EB";
}
