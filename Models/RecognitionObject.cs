using System;
using System.Collections.Generic;

namespace Info_Extreme_System.Models;

/// <summary>
/// Базова структура даних "trecognition_obj" інформаційно-екстремальної інтелектуальної технології (ІЕІТ).
/// Містить повний вхідний математичний опис, структуру даних для навчання (ЛР №12-15),
/// бінаризації (ЛР №13), кодових відстаней (ЛР №14), оптимізації КФЕ та екзамену (ЛР №15).
/// </summary>
public class RecognitionObject
{
    // ==========================================
    // 1. ОСНОВНИЙ БЛОК (MAIN BLOCK)
    // ==========================================

    /// <summary>
    /// Кількість класів розпізнавання (m).
    /// </summary>
    public int M { get; set; }

    /// <summary>
    /// Кількість ознак розпізнавання (N, ширина рецепторного поля / стовпчики).
    /// </summary>
    public int N { get; set; }

    /// <summary>
    /// Кількість реалізацій одного образу (L, висота рецепторного поля / рядки).
    /// </summary>
    public int L { get; set; }

    /// <summary>
    /// Навчальна матриця типу "об'єкт-властивість" Y [M][L][N].
    /// Значення яскравості пікселів у градаціях сірого (0..255).
    /// </summary>
    public int[][][] Y { get; set; } = Array.Empty<int[][]>();

    /// <summary>
    /// Бінарне представлення матриці Y: X [M][L][N].
    /// Координати приймають значення 0 або 1 (для ЛР №13).
    /// </summary>
    public int[][][] X { get; set; } = Array.Empty<int[][]>();

    /// <summary>
    /// Крок дельта для побудови коридору системи контрольних допусків (СКД) (0..255).
    /// </summary>
    public int StepKD { get; set; } = 1;

    /// <summary>
    /// Вектор математичного сподівання ознак базового класу [N] (для ЛР №13).
    /// </summary>
    public double[] MeanVector { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Вектор верхніх контрольних допусків СКД [N] (для ЛР №13).
    /// </summary>
    public double[] VD { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Вектор нижніх контрольних допусків СКД [N] (для ЛР №13).
    /// </summary>
    public double[] ND { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Вектор оптимальних радіусів контейнерів класів [M] (для ЛР №15).
    /// </summary>
    public int[] D { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Індекс базового класу (0..M-1) для побудови СКД (для ЛР №13).
    /// </summary>
    public int BaseClassIndex { get; set; } = 0;

    /// <summary>
    /// Еталонні бінарні вектори (геометричні центри класів) EV [M][N] (для ЛР №13).
    /// </summary>
    public int[][] EV { get; set; } = Array.Empty<int[]>();

    /// <summary>
    /// Вектор сусідніх (найближчих) класів PARA [M] (для ЛР №14).
    /// </summary>
    public int[] PARA { get; set; } = Array.Empty<int>();

    // ==========================================
    // 2. БЛОК ІНТЕРФЕЙСНИХ ДАНИХ (INTERFACE DATA)
    // ==========================================

    /// <summary>
    /// Назви класів, задані користувачем [M].
    /// </summary>
    public string[] Name { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Шляхи до файлів зображень, що утворюють класи [M].
    /// </summary>
    public string[] Path { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Ознака того, чи навчена інформаційна система за вхідними даними.
    /// </summary>
    public bool IsTeached { get; set; } = false;

    // ==========================================
    // 3. ОПТИМІЗАЦІЙНИЙ БЛОК (OPTIMIZATION BLOCK)
    // ==========================================

    /// <summary>
    /// Кодові відстані (за Хеммінгом) від центрів класів до власних реалізацій та реалізацій сусіда:
    /// SK [M][2][L], де [c][0][j] - до власних, [c][1][j] - до сусіда (для ЛР №14).
    /// </summary>
    public int[][][] SK { get; set; } = Array.Empty<int[][]>();

    /// <summary>
    /// Значення критерію функціональної ефективності (КФЕ / критерій Кульбака)
    /// залежно від радіуса контейнера EM [M][N + 1] (для ЛР №15).
    /// </summary>
    public double[][] EM { get; set; } = Array.Empty<double[]>();

    /// <summary>
    /// Помилки першого роду (альфа) залежно від радіуса A [M][N + 1] (для ЛР №15).
    /// </summary>
    public double[][] A { get; set; } = Array.Empty<double[]>();

    /// <summary>
    /// Помилки другого роду (бета) залежно від радіуса B [M][N + 1] (для ЛР №15).
    /// </summary>
    public double[][] B { get; set; } = Array.Empty<double[]>();

    /// <summary>
    /// Значення першої достовірності D1 [M][N + 1] (для ЛР №15).
    /// </summary>
    public double[][] D1 { get; set; } = Array.Empty<double[]>();

    /// <summary>
    /// Значення другої достовірності D2 [M][N + 1] (для ЛР №15).
    /// </summary>
    public double[][] D2 { get; set; } = Array.Empty<double[]>();

    /// <summary>
    /// Середні значення функції належності для кожного класу FP [M] (для ЛР №15).
    /// </summary>
    public double[] FP { get; set; } = Array.Empty<double>();

    // ==========================================
    // 4. МЕТОДИ ІНІЦІАЛІЗАЦІЇ ТА ОБРОБКИ
    // ==========================================

    /// <summary>
    /// Конструктор без параметрів.
    /// </summary>
    public RecognitionObject()
    {
    }

    /// <summary>
    /// Конструктор з ініціалізацією розмірностей (відповідає constructor create в Паскалі).
    /// </summary>
    /// <param name="m">Кількість класів.</param>
    /// <param name="l">Кількість реалізацій.</param>
    /// <param name="n">Кількість ознак.</param>
    public RecognitionObject(int m, int l, int n)
    {
        InitializeArrays(m, l, n);
    }

    /// <summary>
    /// Виділення пам'яті для всіх масивів відповідно до заданих розмірностей m, l, N.
    /// </summary>
    /// <param name="m">Кількість класів.</param>
    /// <param name="l">Кількість реалізацій.</param>
    /// <param name="n">Кількість ознак.</param>
    public void InitializeArrays(int m, int l, int n)
    {
        M = m;
        L = l;
        N = n;

        // 3D навчальна матриця Y [M][L][N]
        Y = new int[M][][];
        for (int c = 0; c < M; c++)
        {
            Y[c] = new int[L][];
            for (int r = 0; r < L; r++)
            {
                Y[c][r] = new int[N];
            }
        }

        // 3D бінарна матриця X [M][L][N]
        X = new int[M][][];
        for (int c = 0; c < M; c++)
        {
            X[c] = new int[L][];
            for (int r = 0; r < L; r++)
            {
                X[c][r] = new int[N];
            }
        }

        MeanVector = new double[N];
        VD = new double[N];
        ND = new double[N];
        D = new int[M];
        BaseClassIndex = 0;

        // Еталонні вектори центрів класів EV [M][N]
        EV = new int[M][];
        for (int c = 0; c < M; c++)
        {
            EV[c] = new int[N];
        }

        PARA = new int[M];
        Name = new string[M];
        Path = new string[M];
        IsTeached = false;

        // Кодові відстані SK [M][2][L]
        SK = new int[M][][];
        for (int c = 0; c < M; c++)
        {
            SK[c] = new int[2][];
            SK[c][0] = new int[L];
            SK[c][1] = new int[L];
        }

        // Оптимізаційні масиви розміром [M][N + 1] (для радіусів d = 0..N)
        EM = new double[M][];
        A = new double[M][];
        B = new double[M][];
        D1 = new double[M][];
        D2 = new double[M][];
        for (int c = 0; c < M; c++)
        {
            EM[c] = new double[N + 1];
            A[c] = new double[N + 1];
            B[c] = new double[N + 1];
            D1[c] = new double[N + 1];
            D2[c] = new double[N + 1];
        }

        FP = new double[M];
    }

    /// <summary>
    /// Формування вхідного математичного опису на основі завантажених зображень класів (ЛР №12).
    /// Заповнює матрицю Y[c][r][col] значеннями яскравості в діапазоні 0..255.
    /// </summary>
    /// <param name="loadedClasses">Список завантажених класів.</param>
    /// <param name="targetL">Цільова кількість реалізацій (висота).</param>
    /// <param name="targetN">Цільова кількість ознак (ширина).</param>
    public void CreateMathematicalDescription(List<ClassImageInfo> loadedClasses, int targetL, int targetN)
    {
        if (loadedClasses == null || loadedClasses.Count == 0)
        {
            throw new ArgumentException("Список зображень не може бути порожнім.", nameof(loadedClasses));
        }

        int m = loadedClasses.Count;
        InitializeArrays(m, targetL, targetN);

        for (int c = 0; c < m; c++)
        {
            var info = loadedClasses[c];
            Name[c] = string.IsNullOrWhiteSpace(info.ClassName) ? $"Клас {c + 1}" : info.ClassName;
            Path[c] = info.FilePath;

            if (info.PixelMatrix == null)
            {
                throw new InvalidOperationException($"Матриця пікселів для класу {c + 1} ({info.FileName}) не ініціалізована.");
            }

            // Заповнення навчальної матриці Y[c][r][col]
            for (int r = 0; r < targetL; r++)
            {
                for (int col = 0; col < targetN; col++)
                {
                    Y[c][r][col] = info.PixelMatrix[r, col];
                }
            }
        }
    }

    /// <summary>
    /// Алгоритм навчання системи розпізнавання (заплановано для реалізації в ЛР №13–15).
    /// </summary>
    public virtual void Teach()
    {
        // Заглушка для наступних лабораторних робіт
        IsTeached = true;
    }

    /// <summary>
    /// Алгоритм екзамену за вирішальним правилом (заплановано для реалізації в ЛР №15).
    /// </summary>
    public virtual void Exam()
    {
        // Заглушка для наступних лабораторних робіт
    }
}
