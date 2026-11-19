using System;
using System.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Info_Extreme_System.Services;

/// <summary>
/// Сервіс бінаризації навчальних матриць та побудови системи контрольних допусків (СКД).
/// Реалізує чисті математичні алгоритми Лабораторної роботи №13.
/// </summary>
public class BinarizationService
{
    /// <summary>
    /// Розрахунок вектора математичних сподівань (еталонного вектора) базового класу для кожної ознаки.
    /// MeanVector[i] = (1 / l) * Sum_j(Y[base][j][i])
    /// </summary>
    /// <param name="yBase">Матриця яскравості базового класу [l][n].</param>
    /// <param name="l">Кількість реалізацій (рядків).</param>
    /// <param name="n">Кількість ознак (стовпчиків).</param>
    /// <returns>Вектор математичних сподівань розмірності n.</returns>
    public double[] ComputeMeanVector(int[][] yBase, int l, int n)
    {
        if (yBase == null || l <= 0 || n <= 0)
        {
            throw new ArgumentException("Некоректні вхідні параметри для розрахунку математичного сподівання.");
        }

        double[] mean = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sum = 0.0;
            for (int j = 0; j < l; j++)
            {
                sum += yBase[j][i];
            }
            mean[i] = sum / l;
        }

        return mean;
    }

    /// <summary>
    /// Побудова системи контрольних допусків (СКД) з кроком дельта:
    /// ND[i] = MeanVector[i] - delta,  VD[i] = MeanVector[i] + delta.
    /// Зберігається точне значення типу double без заокруглення та обрізання.
    /// </summary>
    /// <param name="mean">Вектор математичних сподівань базового класу.</param>
    /// <param name="delta">Крок дельта (ціле число 0..255).</param>
    /// <returns>Кортеж з векторами нижніх (ND) та верхніх (VD) допусків.</returns>
    public (double[] nd, double[] vd) BuildTolerances(double[] mean, int delta)
    {
        if (mean == null)
        {
            throw new ArgumentNullException(nameof(mean));
        }

        int n = mean.Length;
        double[] nd = new double[n];
        double[] vd = new double[n];

        for (int i = 0; i < n; i++)
        {
            nd[i] = mean[i] - delta;
            vd[i] = mean[i] + delta;
        }

        return (nd, vd);
    }

    /// <summary>
    /// Бінаризація 3D-матриці яскравості Y для всіх класів:
    /// X[m][j][i] = 1, якщо ND[i] <= Y[m][j][i] <= VD[i] (включно), інакше 0.
    /// </summary>
    /// <param name="y">3D-навчальна матриця яскравості [m][l][n].</param>
    /// <param name="nd">Вектор нижніх допусків [n].</param>
    /// <param name="vd">Вектор верхніх допусків [n].</param>
    /// <param name="m">Кількість класів.</param>
    /// <param name="l">Кількість реалізацій.</param>
    /// <param name="n">Кількість ознак.</param>
    /// <returns>3D-бінарна матриця X [m][l][n] зі значеннями 0 або 1.</returns>
    public int[][][] Binarize(int[][][] y, double[] nd, double[] vd, int m, int l, int n)
    {
        if (y == null || nd == null || vd == null)
        {
            throw new ArgumentNullException("Матриця Y або вектори допусків не можуть бути null.");
        }

        int[][][] x = new int[m][][];
        for (int c = 0; c < m; c++)
        {
            x[c] = new int[l][];
            for (int j = 0; j < l; j++)
            {
                x[c][j] = new int[n];
                for (int i = 0; i < n; i++)
                {
                    double val = y[c][j][i];
                    x[c][j][i] = (val >= nd[i] && val <= vd[i]) ? 1 : 0;
                }
            }
        }

        return x;
    }

    /// <summary>
    /// Синтез двійкових еталонних векторів EV (геометричних центрів класів) за правилом більшості:
    /// EV[m][i] = 1, якщо (1 / l) * Sum_j(X[m][j][i]) > 0.5 (СТРОГО більше), інакше 0.
    /// </summary>
    /// <param name="x">3D-бінарна матриця [m][l][n].</param>
    /// <param name="m">Кількість класів.</param>
    /// <param name="l">Кількість реалізацій.</param>
    /// <param name="n">Кількість ознак.</param>
    /// <returns>Масив еталонних двійкових векторів EV [m][n].</returns>
    public int[][] BuildReferenceVectors(int[][][] x, int m, int l, int n)
    {
        if (x == null)
        {
            throw new ArgumentNullException(nameof(x));
        }

        int[][] ev = new int[m][];
        for (int c = 0; c < m; c++)
        {
            ev[c] = new int[n];
            for (int i = 0; i < n; i++)
            {
                int sum = 0;
                for (int j = 0; j < l; j++)
                {
                    sum += x[c][j][i];
                }
                double fraction = (double)sum / l;
                ev[c][i] = (fraction > 0.5) ? 1 : 0;
            }
        }

        return ev;
    }

    /// <summary>
    /// Розрахунок кількості та частки одиниць у бінарній матриці одного класу.
    /// </summary>
    public (int Count, double Percentage) CalculateOnesStatistics(int[][] xClass, int l, int n)
    {
        if (xClass == null || l <= 0 || n <= 0) return (0, 0.0);

        int count = 0;
        int total = l * n;
        for (int j = 0; j < l; j++)
        {
            for (int i = 0; i < n; i++)
            {
                if (xClass[j][i] == 1) count++;
            }
        }

        double percentage = (total > 0) ? ((double)count / total) * 100.0 : 0.0;
        return (count, percentage);
    }

    /// <summary>
    /// Розрахунок кількості та частки одиниць у векторі (наприклад, EV).
    /// </summary>
    public (int Count, double Percentage) CalculateVectorOnesStatistics(int[] vector)
    {
        if (vector == null || vector.Length == 0) return (0, 0.0);

        int count = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            if (vector[i] == 1) count++;
        }

        double percentage = ((double)count / vector.Length) * 100.0;
        return (count, percentage);
    }

    /// <summary>
    /// Побудова DataTable для швидкого табличного відображення бінарної матриці X у DataGrid.
    /// </summary>
    public DataTable CreateDataTableForBinaryMatrix(int[][] xClass, int l, int n)
    {
        var dataTable = new DataTable();

        dataTable.Columns.Add("Реалізація", typeof(string));

        for (int i = 0; i < n; i++)
        {
            dataTable.Columns.Add($"x_{i + 1}", typeof(int));
        }

        for (int j = 0; j < l; j++)
        {
            var row = dataTable.NewRow();
            row[0] = $"l_{j + 1}";
            for (int i = 0; i < n; i++)
            {
                row[i + 1] = xClass[j][i];
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Генерація візуальної растрової карти бінарної матриці X (чорне = 1, біле = 0).
    /// Розмірність зображення: ширина n, висота l.
    /// </summary>
    public WriteableBitmap RenderBinaryMatrixBitmap(int[][] xClass, int l, int n)
    {
        int width = n;
        int height = l;
        int stride = width; // 1 байт на піксель у Gray8
        byte[] pixels = new byte[height * stride];

        for (int j = 0; j < height; j++)
        {
            int rowOffset = j * stride;
            for (int i = 0; i < width; i++)
            {
                // У графічному редакторі методички:
                // 1 (потрапляння в допуск) відображається чорним кольором (значення 0)
                // 0 (не потрапляння) відображається білим кольором (значення 255)
                pixels[rowOffset + i] = (xClass[j][i] == 1) ? (byte)0 : (byte)255;
            }
        }

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
        bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, stride, 0);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// Генерація штрих-коду двійкового еталонного вектора EV (чорна лінія = 1, біла лінія = 0).
    /// </summary>
    public WriteableBitmap RenderBarcodeBitmap(int[] vector, int width, int height = 24)
    {
        int stride = width;
        byte[] pixels = new byte[height * stride];

        for (int i = 0; i < width; i++)
        {
            byte color = (vector[i] == 1) ? (byte)0 : (byte)255;
            for (int row = 0; row < height; row++)
            {
                pixels[row * stride + i] = color;
            }
        }

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
        bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, stride, 0);
        bitmap.Freeze();
        return bitmap;
    }
}
