using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Info_Extreme_System.Models;

/// <summary>
/// Допоміжна модель класу розпізнавання для відображення в інтерфейсі користувача.
/// Зберігає метадані зображення, попередній перегляд та 2D-матрицю яскравості пікселів.
/// </summary>
public class ClassImageInfo : INotifyPropertyChanged
{
    private int _classIndex;
    private string _className = string.Empty;
    private string _filePath = string.Empty;
    private int _width;
    private int _height;
    private BitmapSource? _previewImage;
    private int[,]? _pixelMatrix;

    /// <summary>
    /// Порядковий номер класу (1..M).
    /// </summary>
    public int ClassIndex
    {
        get => _classIndex;
        set
        {
            if (_classIndex != value)
            {
                _classIndex = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Назва класу, яку може редагувати користувач.
    /// </summary>
    public string ClassName
    {
        get => _className;
        set
        {
            if (_className != value)
            {
                _className = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Повний шлях до файлу зображення.
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName));
            }
        }
    }

    /// <summary>
    /// Ім'я файлу зображення.
    /// </summary>
    public string FileName => string.IsNullOrEmpty(FilePath) ? string.Empty : Path.GetFileName(FilePath);

    /// <summary>
    /// Ширина зображення в пікселях (кількість ознак N за замовчуванням).
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DimensionsString));
            }
        }
    }

    /// <summary>
    /// Висота зображення в пікселях (кількість реалізацій L за замовчуванням).
    /// </summary>
    public int Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DimensionsString));
            }
        }
    }

    /// <summary>
    /// Рядок з геометричними розмірами зображення.
    /// </summary>
    public string DimensionsString => $"{Width} × {Height} px";

    /// <summary>
    /// Зображення для попереднього перегляду в інтерфейсі (градації сірого).
    /// </summary>
    public BitmapSource? PreviewImage
    {
        get => _previewImage;
        set
        {
            if (_previewImage != value)
            {
                _previewImage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 2D-матриця яскравості пікселів [Height, Width] у діапазоні 0..255.
    /// </summary>
    public int[,]? PixelMatrix
    {
        get => _pixelMatrix;
        set
        {
            if (_pixelMatrix != value)
            {
                _pixelMatrix = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
