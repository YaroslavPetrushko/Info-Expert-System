namespace Info_Extreme_System.Models;

/// <summary>
/// Статистичні характеристики масивів кодових відстаней SK та SK_PARA для пари взаємодіючих класів (ЛР №14).
/// </summary>
public class DistanceStatsInfo
{
    /// <summary>
    /// Індекс поточного класу m (0..M-1).
    /// </summary>
    public int CurrentClassZeroIndex { get; set; }

    /// <summary>
    /// Номер поточного класу (1..M).
    /// </summary>
    public int CurrentClassNumber => CurrentClassZeroIndex + 1;

    /// <summary>
    /// Назва поточного класу.
    /// </summary>
    public string CurrentClassName { get; set; } = string.Empty;

    /// <summary>
    /// Індекс найближчого сусіднього класу p = PARA[m] (0..M-1).
    /// </summary>
    public int NeighborClassZeroIndex { get; set; }

    /// <summary>
    /// Номер найближчого сусіднього класу (1..M).
    /// </summary>
    public int NeighborClassNumber => NeighborClassZeroIndex + 1;

    /// <summary>
    /// Назва найближчого сусіднього класу.
    /// </summary>
    public string NeighborClassName { get; set; } = string.Empty;

    /// <summary>
    /// Кодова відстань між центрами класів D = d_H(EV[m], EV[p]).
    /// </summary>
    public int CenterDistanceD { get; set; }

    /// <summary>
    /// Кількість реалізацій L для кожного класу.
    /// </summary>
    public int RealizationsCountL { get; set; }

    // --- SK[m][0]: Поточний клас до свого центру C1 ---
    public int SkOwnMin { get; set; }
    public int SkOwnMax { get; set; }
    public double SkOwnAvg { get; set; }

    // --- SK[m][1]: Сусідній клас до центру C1 ---
    public int SkNeighborMin { get; set; }
    public int SkNeighborMax { get; set; }
    public double SkNeighborAvg { get; set; }

    // --- SK_PARA[m][0]: Сусідній клас до свого центру C2 ---
    public int SkParaNeighborMin { get; set; }
    public int SkParaNeighborMax { get; set; }
    public double SkParaNeighborAvg { get; set; }

    // --- SK_PARA[m][1]: Поточний клас до центру C2 ---
    public int SkParaOwnMin { get; set; }
    public int SkParaOwnMax { get; set; }
    public double SkParaOwnAvg { get; set; }
}
