namespace Info_Extreme_System.Models;

/// <summary>
/// Представлення точки реалізації на двовимірній площині карти SK_MAP (ЛР №14).
/// </summary>
public class SkMapPoint
{
    /// <summary>
    /// Номер класу розпізнавання (1..M).
    /// </summary>
    public int ClassIndex { get; set; }

    /// <summary>
    /// Назва класу розпізнавання.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// Номер реалізації образу (1..L).
    /// </summary>
    public int RealizationIndex { get; set; }

    /// <summary>
    /// Прапорець: true - якщо реалізація належить поточному класу m, false - якщо сусідньому класу p.
    /// </summary>
    public bool IsCurrentClass { get; set; }

    /// <summary>
    /// Хеммінгова відстань до геометричного центру C1 поточного класу (r1).
    /// </summary>
    public int R1 { get; set; }

    /// <summary>
    /// Хеммінгова відстань до геометричного центру C2 сусіднього класу (r2).
    /// </summary>
    public int R2 { get; set; }

    /// <summary>
    /// Розрахункова координата X на 2D-площині (світові координати).
    /// </summary>
    public double WorldX { get; set; }

    /// <summary>
    /// Розрахункова координата Y на 2D-площині (світові координати, зі знаковим чергуванням).
    /// </summary>
    public double WorldY { get; set; }

    /// <summary>
    /// Екранна координата X на Canvas (в пікселях).
    /// </summary>
    public double CanvasX { get; set; }

    /// <summary>
    /// Екранна координата Y на Canvas (в пікселях).
    /// </summary>
    public double CanvasY { get; set; }

    /// <summary>
    /// Текст підказки для відображення при наведенні курсору.
    /// </summary>
    public string TooltipText { get; set; } = string.Empty;

    /// <summary>
    /// Кількість точок реалізацій з ідентичними координатами.
    /// </summary>
    public int OverlapCount { get; set; } = 1;

    /// <summary>
    /// Колір заливки маркера точки на карті SK_MAP (#2563EB для свого класу, #EA580C для сусіда).
    /// </summary>
    public string PointColor => IsCurrentClass ? "#2563EB" : "#EA580C";

    /// <summary>
    /// Колір контуру маркера точки на карті SK_MAP (#1D4ED8 для свого класу, #C2410C для сусіда).
    /// </summary>
    public string PointStroke => IsCurrentClass ? "#1D4ED8" : "#C2410C";
}
