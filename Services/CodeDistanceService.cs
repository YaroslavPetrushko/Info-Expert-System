using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using Info_Extreme_System.Models;

namespace Info_Extreme_System.Services;

/// <summary>
/// Сервіс для обчислення кодових відстаней за Хеммінгом, визначення найближчих сусідів (PARA),
/// формування масивів SK та SK_PARA і побудови 2D-карти розподілу реалізацій SK_MAP (ЛР №14).
/// Повністю незалежний від графічного інтерфейсу (UI).
/// </summary>
public class CodeDistanceService
{
    /// <summary>
    /// Розрахунок кодової відстані за Хеммінгом між двома двійковими векторами довжини N.
    /// d_H(a, b) = Σ |a[i] - b[i]| (кількість незбіжних бітів).
    /// </summary>
    public static int ComputeHammingDistance(int[] a, int[] b)
    {
        if (a == null || b == null)
            throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));

        int len = Math.Min(a.Length, b.Length);
        int dist = 0;
        for (int i = 0; i < len; i++)
        {
            if (a[i] != b[i])
            {
                dist++;
            }
        }
        return dist;
    }

    /// <summary>
    /// Пошук індексу найближчого сусіда PARA[m] для кожного класу m за мінімальною відстанню між центрами EV.
    /// PARA[m] = argmin_{k ≠ m} d_H(EV[m], EV[k]). При рівності відстаней обирається найменший індекс.
    /// </summary>
    public static int[] ComputeNearestNeighbors(int[][] ev, int m, int n)
    {
        if (ev == null || m <= 0)
            throw new ArgumentException("Масив еталонних векторів порожній або некоректний.", nameof(ev));

        int[] para = new int[m];

        for (int i = 0; i < m; i++)
        {
            int bestNeighbor = -1;
            int minDistance = int.MaxValue;

            for (int k = 0; k < m; k++)
            {
                if (k == i) continue;

                int dist = ComputeHammingDistance(ev[i], ev[k]);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestNeighbor = k;
                }
            }

            para[i] = bestNeighbor != -1 ? bestNeighbor : 0;
        }

        return para;
    }

    /// <summary>
    /// Розрахунок тривимірних масивів кодових відстаней SK та SK_PARA розміром [M][2][L].
    /// </summary>
    public static (int[][][] sk, int[][][] skPara) ComputeAllCodeDistances(int[][][] x, int[][] ev, int[] para, int m, int l, int n)
    {
        if (x == null || ev == null || para == null || m <= 0 || l <= 0)
            throw new ArgumentException("Некоректні вхідні дані для розрахунку кодових відстаней.");

        int[][][] sk = new int[m][][];
        int[][][] skPara = new int[m][][];

        for (int c = 0; c < m; c++)
        {
            sk[c] = new int[2][];
            sk[c][0] = new int[l];
            sk[c][1] = new int[l];

            skPara[c] = new int[2][];
            skPara[c][0] = new int[l];
            skPara[c][1] = new int[l];

            int p = para[c];

            for (int j = 0; j < l; j++)
            {
                // SK[m][0][j] - відстань від центру поточного класу c до j-ї реалізації СВОГО класу c
                sk[c][0][j] = ComputeHammingDistance(ev[c], x[c][j]);

                // SK[m][1][j] - відстань від центру поточного класу c до j-ї реалізації СУСІДНЬОГО класу p
                sk[c][1][j] = ComputeHammingDistance(ev[c], x[p][j]);

                // SK_PARA[m][0][j] - відстань від центру сусіда p до j-ї реалізації класу p
                skPara[c][0][j] = ComputeHammingDistance(ev[p], x[p][j]);

                // SK_PARA[m][1][j] - відстань від центру сусіда p до j-ї реалізації класу c
                skPara[c][1][j] = ComputeHammingDistance(ev[p], x[c][j]);
            }
        }

        return (sk, skPara);
    }

    /// <summary>
    /// Розрахунок базових описових статистик для одновимірного вектора довжини L.
    /// </summary>
    public static (int Min, int Max, double Average) CalculateVectorStats(int[] vector, int l)
    {
        if (vector == null || vector.Length == 0 || l <= 0)
            return (0, 0, 0.0);

        int min = int.MaxValue;
        int max = int.MinValue;
        long sum = 0;
        int count = Math.Min(vector.Length, l);

        for (int i = 0; i < count; i++)
        {
            int val = vector[i];
            if (val < min) min = val;
            if (val > max) max = val;
            sum += val;
        }

        return (min, max, (double)sum / count);
    }

    /// <summary>
    /// Формування зведеної інформаційної моделі статистичних характеристик для обраної пари класів.
    /// </summary>
    public static DistanceStatsInfo ComputeClassPairStats(
        int[][] skClass,
        int[][] skParaClass,
        int centerDistanceD,
        int currentClassIdx,
        int neighborClassIdx,
        string currentClassName,
        string neighborClassName,
        int l)
    {
        var (s1Min, s1Max, s1Avg) = CalculateVectorStats(skClass[0], l);
        var (s2Min, s2Max, s2Avg) = CalculateVectorStats(skClass[1], l);
        var (sp1Min, sp1Max, sp1Avg) = CalculateVectorStats(skParaClass[0], l);
        var (sp2Min, sp2Max, sp2Avg) = CalculateVectorStats(skParaClass[1], l);

        return new DistanceStatsInfo
        {
            CurrentClassZeroIndex = currentClassIdx,
            CurrentClassName = currentClassName,
            NeighborClassZeroIndex = neighborClassIdx,
            NeighborClassName = neighborClassName,
            CenterDistanceD = centerDistanceD,
            RealizationsCountL = l,

            SkOwnMin = s1Min,
            SkOwnMax = s1Max,
            SkOwnAvg = s1Avg,

            SkNeighborMin = s2Min,
            SkNeighborMax = s2Max,
            SkNeighborAvg = s2Avg,

            SkParaNeighborMin = sp1Min,
            SkParaNeighborMax = sp1Max,
            SkParaNeighborAvg = sp1Avg,

            SkParaOwnMin = sp2Min,
            SkParaOwnMax = sp2Max,
            SkParaOwnAvg = sp2Avg
        };
    }

    /// <summary>
    /// Створення таблиці DataTable для відображення значень SK та SK_PARA в інспекторі FastDataGrid.
    /// Рядки відповідають реалізаціям l_1..l_L.
    /// </summary>
    public static DataTable CreateDataTableForDistances(int[][] skClass, int[][] skParaClass, int l)
    {
        var table = new DataTable("DistancesInspectorTable");

        table.Columns.Add("Реалізація", typeof(string));
        table.Columns.Add("SK[1] (свій → C1)", typeof(int));
        table.Columns.Add("SK[2] (сусід → C1)", typeof(int));
        table.Columns.Add("SK_PARA[1] (сусід → C2)", typeof(int));
        table.Columns.Add("SK_PARA[2] (свій → C2)", typeof(int));

        for (int j = 0; j < l; j++)
        {
            var row = table.NewRow();
            row[0] = $"l_{j + 1}";
            row[1] = skClass[0][j];
            row[2] = skClass[1][j];
            row[3] = skParaClass[0][j];
            row[4] = skParaClass[1][j];
            table.Rows.Add(row);
        }

        return table;
    }

    /// <summary>
    /// Розрахунок світових координат (WorldX, WorldY) точок реалізацій на площині SK_MAP за методом триангуляції кіл.
    /// Якщо центри класів збігаються (D == 0), повертає порожній список (малювання блокується).
    /// </summary>
    public static List<SkMapPoint> ComputeSkMapPoints(
        int[][] skClass,
        int[][] skParaClass,
        int centerDistanceD,
        int currentClassIdx,
        int neighborClassIdx,
        string currentClassName,
        string neighborClassName,
        int l)
    {
        var points = new List<SkMapPoint>();

        if (centerDistanceD <= 0)
        {
            return points;
        }

        double d = centerDistanceD;
        double d2 = d * d;

        // 1. Точки ПОТОЧНОГО класу m
        for (int j = 0; j < l; j++)
        {
            int r1 = skClass[0][j];      // відстань до власного центру C1
            int r2 = skParaClass[1][j];  // відстань до центру сусіда C2

            double r1Sq = (double)r1 * r1;
            double r2Sq = (double)r2 * r2;

            double x = (d2 + r1Sq - r2Sq) / (2.0 * d);
            double yRad = Math.Max(0.0, r1Sq - x * x);
            double yAbs = Math.Sqrt(yRad);
            double sign = (j % 2 == 0) ? 1.0 : -1.0;
            double y = sign * yAbs;

            points.Add(new SkMapPoint
            {
                ClassIndex = currentClassIdx + 1,
                ClassName = currentClassName,
                RealizationIndex = j + 1,
                IsCurrentClass = true,
                R1 = r1,
                R2 = r2,
                WorldX = x,
                WorldY = y
            });
        }

        // 2. Точки СУСІДНЬОГО класу p
        for (int j = 0; j < l; j++)
        {
            int r1 = skClass[1][j];      // відстань до центру C1
            int r2 = skParaClass[0][j];  // відстань до власного центру C2

            double r1Sq = (double)r1 * r1;
            double r2Sq = (double)r2 * r2;

            double x = (d2 + r1Sq - r2Sq) / (2.0 * d);
            double yRad = Math.Max(0.0, r1Sq - x * x);
            double yAbs = Math.Sqrt(yRad);
            double sign = (j % 2 == 0) ? 1.0 : -1.0;
            double y = sign * yAbs;

            points.Add(new SkMapPoint
            {
                ClassIndex = neighborClassIdx + 1,
                ClassName = neighborClassName,
                RealizationIndex = j + 1,
                IsCurrentClass = false,
                R1 = r1,
                R2 = r2,
                WorldX = x,
                WorldY = y
            });
        }

        return points;
    }

    /// <summary>
    /// Ізотропне перетворення (1:1 масштаб по X та Y) координат реалізацій та центрів класів C1(0,0), C2(D,0)
    /// у систему координат Canvas з автоматичним центруванням та відступами.
    /// </summary>
    public static (double c1CanvasX, double c1CanvasY, double c2CanvasX, double c2CanvasY, double scale, Rect worldBounds)
        TransformPointsToCanvas(
            List<SkMapPoint> points,
            int centerDistanceD,
            double canvasWidth,
            double canvasHeight,
            double paddingRatio = 0.12)
    {
        if (points == null || points.Count == 0 || centerDistanceD <= 0)
        {
            return (canvasWidth / 2.0, canvasHeight / 2.0, canvasWidth / 2.0, canvasHeight / 2.0, 1.0, Rect.Empty);
        }

        double d = centerDistanceD;

        // Початкові межі включають обидва центри: C1 = (0, 0) та C2 = (D, 0)
        double minX = Math.Min(0.0, d);
        double maxX = Math.Max(0.0, d);
        double minY = 0.0;
        double maxY = 0.0;

        foreach (var pt in points)
        {
            if (pt.WorldX < minX) minX = pt.WorldX;
            if (pt.WorldX > maxX) maxX = pt.WorldX;
            if (pt.WorldY < minY) minY = pt.WorldY;
            if (pt.WorldY > maxY) maxY = pt.WorldY;
        }

        // Додаємо запас для симетрії осі Y
        double maxAbsY = Math.Max(Math.Abs(minY), Math.Abs(maxY));
        if (maxAbsY < 1.0) maxAbsY = 1.0;
        minY = -maxAbsY;
        maxY = maxAbsY;

        double spanX = Math.Max(1.0, maxX - minX);
        double spanY = Math.Max(1.0, maxY - minY);

        // Розрахунок корисного простору Canvas з урахуванням полів (padding)
        double padX = canvasWidth * paddingRatio;
        double padY = canvasHeight * paddingRatio;
        double drawW = Math.Max(50.0, canvasWidth - 2.0 * padX);
        double drawH = Math.Max(50.0, canvasHeight - 2.0 * padY);

        // Єдиний ізотропний масштаб (1:1) для обох осей
        double scale = Math.Min(drawW / spanX, drawH / spanY);

        double worldCenterX = (minX + maxX) / 2.0;
        double worldCenterY = 0.0; // Центруємо горизонтальну вісь строго по середині Y

        double canvasMidX = canvasWidth / 2.0;
        double canvasMidY = canvasHeight / 2.0;

        // Перетворення координат точок
        foreach (var pt in points)
        {
            pt.CanvasX = canvasMidX + (pt.WorldX - worldCenterX) * scale;
            // У WPF вісь Y спрямована вниз, тому віднімаємо вираз
            pt.CanvasY = canvasMidY - (pt.WorldY - worldCenterY) * scale;
        }

        // Обчислення збігів та формування підказок
        var grouped = points.GroupBy(p => $"{Math.Round(p.WorldX, 2)};{Math.Round(p.WorldY, 2)}").ToList();
        foreach (var group in grouped)
        {
            int overlap = group.Count();
            foreach (var pt in group)
            {
                pt.OverlapCount = overlap;
                string overlapNote = overlap > 1 ? $"\nЗбігів у цій точці: {overlap} реалізацій" : string.Empty;
                pt.TooltipText =
                    $"{pt.ClassName} — Реалізація #{pt.RealizationIndex}\n" +
                    $"• Відстань до центру свого класу C1 (r1): {pt.R1}\n" +
                    $"• Відстань до центру сусіда C2 (r2): {pt.R2}\n" +
                    $"• Координати на площині: ({pt.WorldX:F2}; {pt.WorldY:F2}){overlapNote}";
            }
        }

        // Перетворення координат центрів C1(0, 0) та C2(D, 0)
        double c1CanvasX = canvasMidX + (0.0 - worldCenterX) * scale;
        double c1CanvasY = canvasMidY - (0.0 - worldCenterY) * scale;

        double c2CanvasX = canvasMidX + (d - worldCenterX) * scale;
        double c2CanvasY = canvasMidY - (0.0 - worldCenterY) * scale;

        var worldBounds = new Rect(minX, minY, spanX, spanY);
        return (c1CanvasX, c1CanvasY, c2CanvasX, c2CanvasY, scale, worldBounds);
    }
}
