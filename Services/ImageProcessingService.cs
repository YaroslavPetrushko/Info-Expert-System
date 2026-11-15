using System;
using System.Data;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Info_Extreme_System.Models;

namespace Info_Extreme_System.Services;

/// <summary>
/// Сервіс обробки зображень та підготовки числових масивів яскравості пікселів.
/// </summary>
public class ImageProcessingService
{
    /// <summary>
    /// Завантажує графічний файл (BMP, JPG, JPEG), конвертує його у 8-бітні градації сірого (0..255)
    /// та вилучає 2D-матрицю значень яскравості пікселів.
    /// </summary>
    /// <param name="filePath">Повний шлях до файлу зображення.</param>
    /// <param name="classIndex">Порядковий номер класу.</param>
    /// <param name="customName">Користувацька назва класу (якщо не вказано, генерується "Клас {classIndex}").</param>
    /// <returns>Об'єкт ClassImageInfo з завантаженим зображенням та матрицею пікселів.</returns>
    public ClassImageInfo LoadClassImage(string filePath, int classIndex, string? customName = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл не знайдено: {filePath}", filePath);
        }

        // Читання файлу через пам'ять для уникнення блокування файлу на диску
        byte[] fileBytes = File.ReadAllBytes(filePath);
        using var memoryStream = new MemoryStream(fileBytes);

        var originalBitmap = new BitmapImage();
        originalBitmap.BeginInit();
        originalBitmap.CacheOption = BitmapCacheOption.OnLoad;
        originalBitmap.StreamSource = memoryStream;
        originalBitmap.EndInit();
        originalBitmap.Freeze();

        // Конвертація в 8-бітні градації сірого (Gray8)
        // Для кожного пікселя яскравість I in [0..255]
        var grayBitmap = new FormatConvertedBitmap(originalBitmap, PixelFormats.Gray8, null, 0);
        grayBitmap.Freeze();

        int width = grayBitmap.PixelWidth;
        int height = grayBitmap.PixelHeight;

        // Копіювання байтів яскравості
        int stride = width; // для Gray8 1 байт на піксель
        byte[] rawPixels = new byte[height * stride];
        grayBitmap.CopyPixels(rawPixels, stride, 0);

        // Формування 2D-матриці яскравості [Height, Width]
        // Рядок (0..Height-1) - реалізація образу
        // Стовпчик (0..Width-1) - ознака розпізнавання
        int[,] pixelMatrix = new int[height, width];
        for (int r = 0; r < height; r++)
        {
            int rowOffset = r * stride;
            for (int c = 0; c < width; c++)
            {
                pixelMatrix[r, c] = rawPixels[rowOffset + c];
            }
        }

        string className = string.IsNullOrWhiteSpace(customName) ? $"Клас {classIndex}" : customName;

        return new ClassImageInfo
        {
            ClassIndex = classIndex,
            ClassName = className,
            FilePath = filePath,
            Width = width,
            Height = height,
            PreviewImage = grayBitmap,
            PixelMatrix = pixelMatrix
        };
    }

    /// <summary>
    /// Перевірка відповідності геометричних розмірів нового зображення еталонній розмірності першого класу.
    /// </summary>
    /// <param name="candidate">Кандидат на додавання.</param>
    /// <param name="referenceWidth">Еталонна ширина.</param>
    /// <param name="referenceHeight">Еталонна висота.</param>
    /// <param name="errorMessage">Повідомлення про помилку у разі невідповідності.</param>
    /// <returns>True, якщо розмірність співпадає; інакше False.</returns>
    public bool ValidateGeometry(ClassImageInfo candidate, int referenceWidth, int referenceHeight, out string errorMessage)
    {
        if (candidate.Width != referenceWidth || candidate.Height != referenceHeight)
        {
            errorMessage = $"Помилка валідації геометричних розмірів зображення!\n\n" +
                           $"Файл: \"{candidate.FileName}\"\n" +
                           $"Фактична розмірність: {candidate.Width} × {candidate.Height} px.\n" +
                           $"Очікувана (еталонна) розмірність: {referenceWidth} × {referenceHeight} px.\n\n" +
                           $"Усі зображення класів розпізнавання повинні мати однакові розміри (ширину та висоту). " +
                           $"Завантаження відхилено.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    /// <summary>
    /// Створення DataTable для швидкого і наочного відображення навчальної матриці Y[classIndex] у DataGrid.
    /// </summary>
    /// <param name="matrixSlice">2D масив [L, N] значень яскравості.</param>
    /// <param name="l">Кількість реалізацій (рядків).</param>
    /// <param name="n">Кількість ознак (стовпчиків).</param>
    /// <returns>Заповнений DataTable.</returns>
    public DataTable CreateDataTableForMatrix(int[][] matrixSlice, int l, int n)
    {
        var dataTable = new DataTable();

        // Перший стовпчик - заголовок реалізації
        dataTable.Columns.Add("Реалізація", typeof(string));

        // Стовпчики ознак: y_1, y_2, ..., y_N
        for (int j = 0; j < n; j++)
        {
            dataTable.Columns.Add($"y_{j + 1}", typeof(int));
        }

        // Заповнення рядків
        for (int r = 0; r < l; r++)
        {
            var row = dataTable.NewRow();
            row[0] = $"l_{r + 1}";
            for (int c = 0; c < n; c++)
            {
                row[c + 1] = matrixSlice[r][c];
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Розрахунок базових статистичних характеристик яскравості матриці класу.
    /// </summary>
    public (int Min, int Max, double Average) CalculateMatrixStatistics(int[][] matrixSlice, int l, int n)
    {
        if (l == 0 || n == 0) return (0, 0, 0.0);

        int min = int.MaxValue;
        int max = int.MinValue;
        long sum = 0;
        long total = (long)l * n;

        for (int r = 0; r < l; r++)
        {
            for (int c = 0; c < n; c++)
            {
                int val = matrixSlice[r][c];
                if (val < min) min = val;
                if (val > max) max = val;
                sum += val;
            }
        }

        return (min, max, (double)sum / total);
    }
}
