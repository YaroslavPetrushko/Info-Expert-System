using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Info_Extreme_System.Models;
using Info_Extreme_System.Services;
using Microsoft.Win32;

namespace Info_Extreme_System.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ImageProcessingService _imageService;
    private readonly BinarizationService _binarizationService;

    // --- Поля вкладки 1 (ЛР №12) ---
    private int _targetM = 2;
    private bool _isAutoDimensions = true;
    private int _customL = 100;
    private int _customN = 100;
    private int? _referenceWidth;
    private int? _referenceHeight;
    private bool _isMatrixFormed;
    private int _selectedClassIndexForInspector = 0;
    private DataView? _currentMatrixView;
    private string _selectedClassMatrixInfo = "Оберіть клас та сформуйте матрицю Y для перегляду.";
    private int _selectedMinBrightness;
    private int _selectedMaxBrightness;
    private double _selectedAvgBrightness;

    // --- Поля вкладки 2 (ЛР №13: СКД та бінаризація) ---
    private int _baseClassIndex = 0;
    private int _stepKD = 30;
    private bool _isBinarized;
    private int _selectedClassIndexForBinaryInspector = 0;
    private DataView? _currentBinaryMatrixView;
    private BitmapSource? _currentBinaryVisualMap;
    private string _selectedBinaryClassInfo = "Оберіть клас та виконайте бінаризацію для перегляду.";
    private int _selectedBinaryOnesCount;
    private double _selectedBinaryOnesPercentage;
    private PointCollection? _toleranceMeanPoints;
    private PointCollection? _toleranceNDPoints;
    private PointCollection? _toleranceVDPoints;

    public MainViewModel()
    {
        _imageService = new ImageProcessingService();
        _binarizationService = new BinarizationService();
        RecognitionObj = new RecognitionObject();
        LoadedClasses = new ObservableCollection<ClassImageInfo>();
        LogMessages = new ObservableCollection<string>();
        BinaryClassesStatistics = new ObservableCollection<BinaryClassStatInfo>();
        ReferenceVectors = new ObservableCollection<ReferenceVectorInfo>();

        // Команди вкладки 1 (ЛР №12)
        AddImagesCommand = new RelayCommand(ExecuteAddImages);
        LoadDemoResourcesCommand = new RelayCommand(ExecuteLoadDemoResources);
        RemoveClassCommand = new RelayCommand(ExecuteRemoveClass);
        ClearAllClassesCommand = new RelayCommand(ExecuteClearAllClasses);
        FormTrainingMatrixCommand = new RelayCommand(ExecuteFormTrainingMatrix);

        // Команди вкладки 2 (ЛР №13)
        BinarizeCommand = new RelayCommand(ExecuteBinarize);

        Log("Система ІЕІТ ініціалізована. Готова до роботи з ЛР №12 та ЛР №13.");
    }

    // ==========================================
    // ВЛАСТИВОСТІ МОДЕЛІ ДАНИХ ТА СТАНУ (ЛР 12)
    // ==========================================

    /// <summary>
    /// Ядро структури даних ІЕІТ (trecognition_obj).
    /// </summary>
    public RecognitionObject RecognitionObj { get; }

    /// <summary>
    /// Колекція завантажених зображень класів для відображення в галереї.
    /// </summary>
    public ObservableCollection<ClassImageInfo> LoadedClasses { get; }

    /// <summary>
    /// Журнал подій та інформаційних повідомлень.
    /// </summary>
    public ObservableCollection<string> LogMessages { get; }

    /// <summary>
    /// Задане користувачем число класів розпізнавання (m).
    /// </summary>
    public int TargetM
    {
        get => _targetM;
        set
        {
            if (value < 2) value = 2;
            if (value > 20) value = 20;
            if (_targetM != value)
            {
                _targetM = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusCounterText));
                OnPropertyChanged(nameof(StatusBrush));
                OnPropertyChanged(nameof(TotalElementsCountText));
            }
        }
    }

    /// <summary>
    /// Режим автоматичного визначення розмірності за розмірами зображення.
    /// </summary>
    public bool IsAutoDimensions
    {
        get => _isAutoDimensions;
        set
        {
            if (_isAutoDimensions != value)
            {
                _isAutoDimensions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsManualDimensions));
                OnPropertyChanged(nameof(EffectiveL));
                OnPropertyChanged(nameof(EffectiveN));
                OnPropertyChanged(nameof(MatrixL));
                OnPropertyChanged(nameof(MatrixN));
                OnPropertyChanged(nameof(TotalElementsCountText));
            }
        }
    }

    /// <summary>
    /// Протилежність IsAutoDimensions для двостороннього зв'язування з інтерфейсом.
    /// </summary>
    public bool IsManualDimensions
    {
        get => !IsAutoDimensions;
        set => IsAutoDimensions = !value;
    }

    /// <summary>
    /// Користувацька кількість реалізацій l (висота зрізу).
    /// </summary>
    public int CustomL
    {
        get => _customL;
        set
        {
            if (_customL != value)
            {
                _customL = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveL));
                OnPropertyChanged(nameof(MatrixL));
                OnPropertyChanged(nameof(TotalElementsCountText));
            }
        }
    }

    /// <summary>
    /// Користувацька кількість ознак N (ширина зрізу).
    /// </summary>
    public int CustomN
    {
        get => _customN;
        set
        {
            if (_customN != value)
            {
                _customN = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EffectiveN));
                OnPropertyChanged(nameof(MatrixN));
                OnPropertyChanged(nameof(TotalElementsCountText));
            }
        }
    }

    /// <summary>
    /// Еталонна ширина за першим завантаженим зображенням.
    /// </summary>
    public int? ReferenceWidth
    {
        get => _referenceWidth;
        private set
        {
            if (_referenceWidth != value)
            {
                _referenceWidth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReferenceDimensionsText));
                OnPropertyChanged(nameof(EffectiveN));
                OnPropertyChanged(nameof(MatrixN));
                OnPropertyChanged(nameof(TotalElementsCountText));
            }
        }
    }

    /// <summary>
    /// Еталонна висота за першим завантаженим зображенням.
    /// </summary>
    public int? ReferenceHeight
    {
        get => _referenceHeight;
        private set
        {
            if (_referenceHeight != value)
            {
                _referenceHeight = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReferenceDimensionsText));
                OnPropertyChanged(nameof(EffectiveL));
                OnPropertyChanged(nameof(MatrixL));
                OnPropertyChanged(nameof(TotalElementsCountText));
            }
        }
    }

    /// <summary>
    /// Фактична кількість реалізацій L, яка буде використана у матриці.
    /// </summary>
    public int EffectiveL => IsAutoDimensions ? (ReferenceHeight ?? 0) : CustomL;

    /// <summary>
    /// Фактична кількість ознак N, яка буде використана у матриці.
    /// </summary>
    public int EffectiveN => IsAutoDimensions ? (ReferenceWidth ?? 0) : CustomN;

    /// <summary>
    /// Фактична кількість класів розпізнавання (m) для відображення в системі.
    /// </summary>
    public int MatrixM => RecognitionObj.M > 0 ? RecognitionObj.M : LoadedClasses.Count;

    /// <summary>
    /// Фактична кількість реалізацій образу (l) для відображення в системі.
    /// </summary>
    public int MatrixL => RecognitionObj.L > 0 ? RecognitionObj.L : EffectiveL;

    /// <summary>
    /// Фактична кількість ознак розпізнавання (N) для відображення в системі.
    /// </summary>
    public int MatrixN => RecognitionObj.N > 0 ? RecognitionObj.N : EffectiveN;

    /// <summary>
    /// Рядкове представлення еталонних розмірів.
    /// </summary>
    public string ReferenceDimensionsText =>
        (ReferenceWidth.HasValue && ReferenceHeight.HasValue)
            ? $"{ReferenceWidth.Value} × {ReferenceHeight.Value} px"
            : "Ще не визначено";

    /// <summary>
    /// Статус завантаження класів: "Завантажено зображень: X з M".
    /// </summary>
    public string StatusCounterText => $"Завантажено зображень: {LoadedClasses.Count} з {TargetM}";

    /// <summary>
    /// Колір індикатора статусу завантаження.
    /// </summary>
    public Brush StatusBrush
    {
        get
        {
            if (LoadedClasses.Count == TargetM)
                return new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Зелений (готово)
            if (LoadedClasses.Count < TargetM)
                return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Помаранчевий (не вистачає)
            return new SolidColorBrush(Color.FromRgb(220, 53, 69));   // Червоний (перевищено)
        }
    }

    /// <summary>
    /// Ознака того, чи сформовано навчальну матрицю Y.
    /// </summary>
    public bool IsMatrixFormed
    {
        get => _isMatrixFormed;
        private set
        {
            if (_isMatrixFormed != value)
            {
                _isMatrixFormed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MatrixFormedStatusText));
                OnPropertyChanged(nameof(MatrixStatusBrush));
                OnPropertyChanged(nameof(IsYMissing));
            }
        }
    }

    /// <summary>
    /// Прапорець відсутності сформованої матриці Y (для підказки на вкладці 2).
    /// </summary>
    public bool IsYMissing => !IsMatrixFormed;

    /// <summary>
    /// Текстовий опис стану навчальної матриці Y.
    /// </summary>
    public string MatrixFormedStatusText =>
        IsMatrixFormed
            ? $"Сформовано успішно (Розмір: {RecognitionObj.M} класів × {RecognitionObj.L} рядків × {RecognitionObj.N} стовпчиків)"
            : "Не сформовано (Очікується завантаження класів та запуск формування)";

    /// <summary>
    /// Колір статусу формування матриці.
    /// </summary>
    public Brush MatrixStatusBrush =>
        IsMatrixFormed
            ? new SolidColorBrush(Color.FromRgb(40, 167, 69))
            : new SolidColorBrush(Color.FromRgb(108, 117, 125));

    /// <summary>
    /// Загальний обсяг елементів вибірки (m * l * N).
    /// </summary>
    public string TotalElementsCountText
    {
        get
        {
            int l = EffectiveL;
            int n = EffectiveN;
            long total = (long)TargetM * l * n;
            return total > 0 ? $"{total:N0} ел. ({TargetM} × {l} × {n})" : "0";
        }
    }

    /// <summary>
    /// Індекс обраного для інспектування класу (0..M-1).
    /// </summary>
    public int SelectedClassIndexForInspector
    {
        get => _selectedClassIndexForInspector;
        set
        {
            if (_selectedClassIndexForInspector != value)
            {
                _selectedClassIndexForInspector = value;
                OnPropertyChanged();
                UpdateMatrixInspectorView();
            }
        }
    }

    /// <summary>
    /// DataView для прив'язки до DataGrid з числовими значеннями матриці Y.
    /// </summary>
    public DataView? CurrentMatrixView
    {
        get => _currentMatrixView;
        private set
        {
            if (_currentMatrixView != value)
            {
                _currentMatrixView = value;
                OnPropertyChanged();
            }
        }
    }

    public string SelectedClassMatrixInfo
    {
        get => _selectedClassMatrixInfo;
        private set
        {
            if (_selectedClassMatrixInfo != value)
            {
                _selectedClassMatrixInfo = value;
                OnPropertyChanged();
            }
        }
    }

    public int SelectedMinBrightness
    {
        get => _selectedMinBrightness;
        private set
        {
            if (_selectedMinBrightness != value)
            {
                _selectedMinBrightness = value;
                OnPropertyChanged();
            }
        }
    }

    public int SelectedMaxBrightness
    {
        get => _selectedMaxBrightness;
        private set
        {
            if (_selectedMaxBrightness != value)
            {
                _selectedMaxBrightness = value;
                OnPropertyChanged();
            }
        }
    }

    public double SelectedAvgBrightness
    {
        get => _selectedAvgBrightness;
        private set
        {
            if (Math.Abs(_selectedAvgBrightness - value) > 0.001)
            {
                _selectedAvgBrightness = value;
                OnPropertyChanged();
            }
        }
    }

    // ==========================================
    // ВЛАСТИВОСТІ ВКЛАДКИ 2 (ЛР №13: СКД ТА БІНАРИЗАЦІЯ)
    // ==========================================

    /// <summary>
    /// Індекс обраного базового класу (0..M-1).
    /// </summary>
    public int BaseClassIndex
    {
        get => _baseClassIndex;
        set
        {
            if (_baseClassIndex != value)
            {
                _baseClassIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BaseClassNameText));
            }
        }
    }

    /// <summary>
    /// Назва обраного базового класу.
    /// </summary>
    public string BaseClassNameText
    {
        get
        {
            if (LoadedClasses.Count > 0 && BaseClassIndex >= 0 && BaseClassIndex < LoadedClasses.Count)
            {
                return LoadedClasses[BaseClassIndex].ClassName;
            }
            return "Не обрано";
        }
    }

    /// <summary>
    /// Крок дельта для побудови коридору системи контрольних допусків (0..255).
    /// </summary>
    public int StepKD
    {
        get => _stepKD;
        set
        {
            int clamped = Math.Clamp(value, 0, 255);
            if (_stepKD != clamped)
            {
                _stepKD = clamped;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Прапорець того, чи розраховано СКД та сформовано бінарну матрицю X.
    /// </summary>
    public bool IsBinarized
    {
        get => _isBinarized;
        private set
        {
            if (_isBinarized != value)
            {
                _isBinarized = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBinarizePending));
                OnPropertyChanged(nameof(BinarizationStatusText));
                OnPropertyChanged(nameof(BinarizationStatusBrush));
            }
        }
    }

    /// <summary>
    /// Протилежність IsBinarized (для підказки очікування побудови СКД).
    /// </summary>
    public bool IsBinarizePending => !IsBinarized;

    /// <summary>
    /// Текстовий статус бінаризації.
    /// </summary>
    public string BinarizationStatusText =>
        IsBinarized
            ? $"СКД та бінарні матриці X сформовано успішно! (База: {BaseClassNameText}, δ={StepKD})"
            : "Очікується запуск побудови СКД та бінаризації";

    /// <summary>
    /// Колір індикатора статусу бінаризації.
    /// </summary>
    public Brush BinarizationStatusBrush =>
        IsBinarized
            ? new SolidColorBrush(Color.FromRgb(5, 150, 105)) // Зелений
            : new SolidColorBrush(Color.FromRgb(100, 116, 139)); // Сірий

    /// <summary>
    /// Індекс класу для перегляду бінарної матриці X в інспекторі (0..M-1).
    /// </summary>
    public int SelectedClassIndexForBinaryInspector
    {
        get => _selectedClassIndexForBinaryInspector;
        set
        {
            if (_selectedClassIndexForBinaryInspector != value)
            {
                _selectedClassIndexForBinaryInspector = value;
                OnPropertyChanged();
                UpdateBinaryInspectorView();
            }
        }
    }

    /// <summary>
    /// Табличне представлення DataView для відображення X в DataGrid.
    /// </summary>
    public DataView? CurrentBinaryMatrixView
    {
        get => _currentBinaryMatrixView;
        private set
        {
            if (_currentBinaryMatrixView != value)
            {
                _currentBinaryMatrixView = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Растрова візуальна карта бінарної матриці X (чорне = 1, біле = 0).
    /// </summary>
    public BitmapSource? CurrentBinaryVisualMap
    {
        get => _currentBinaryVisualMap;
        private set
        {
            if (_currentBinaryVisualMap != value)
            {
                _currentBinaryVisualMap = value;
                OnPropertyChanged();
            }
        }
    }

    public string SelectedBinaryClassInfo
    {
        get => _selectedBinaryClassInfo;
        private set
        {
            if (_selectedBinaryClassInfo != value)
            {
                _selectedBinaryClassInfo = value;
                OnPropertyChanged();
            }
        }
    }

    public int SelectedBinaryOnesCount
    {
        get => _selectedBinaryOnesCount;
        private set
        {
            if (_selectedBinaryOnesCount != value)
            {
                _selectedBinaryOnesCount = value;
                OnPropertyChanged();
            }
        }
    }

    public double SelectedBinaryOnesPercentage
    {
        get => _selectedBinaryOnesPercentage;
        private set
        {
            if (Math.Abs(_selectedBinaryOnesPercentage - value) > 0.001)
            {
                _selectedBinaryOnesPercentage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Колекція статистичних показників бінаризації по класах.
    /// </summary>
    public ObservableCollection<BinaryClassStatInfo> BinaryClassesStatistics { get; }

    /// <summary>
    /// Колекція двійкових еталонних векторів EV (зі штрих-кодами) по класах.
    /// </summary>
    public ObservableCollection<ReferenceVectorInfo> ReferenceVectors { get; }

    /// <summary>
    /// Точки лінії математичного сподівання (MeanVector) для графіка СКД.
    /// </summary>
    public PointCollection? ToleranceMeanPoints
    {
        get => _toleranceMeanPoints;
        private set
        {
            if (_toleranceMeanPoints != value)
            {
                _toleranceMeanPoints = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Точки лінії нижніх допусків (ND) для графіка СКД.
    /// </summary>
    public PointCollection? ToleranceNDPoints
    {
        get => _toleranceNDPoints;
        private set
        {
            if (_toleranceNDPoints != value)
            {
                _toleranceNDPoints = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Точки лінії верхніх допусків (VD) для графіка СКД.
    /// </summary>
    public PointCollection? ToleranceVDPoints
    {
        get => _toleranceVDPoints;
        private set
        {
            if (_toleranceVDPoints != value)
            {
                _toleranceVDPoints = value;
                OnPropertyChanged();
            }
        }
    }

    // ==========================================
    // КОМАНДИ (COMMANDS)
    // ==========================================

    public ICommand AddImagesCommand { get; }
    public ICommand LoadDemoResourcesCommand { get; }
    public ICommand RemoveClassCommand { get; }
    public ICommand ClearAllClassesCommand { get; }
    public ICommand FormTrainingMatrixCommand { get; }
    public ICommand BinarizeCommand { get; }

    // ==========================================
    // РЕАЛІЗАЦІЯ ДІЙ ТА ВАЛІДАЦІЇ (ЛР №12)
    // ==========================================

    private void ExecuteAddImages(object? parameter)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Оберіть зображення для класів розпізнавання (BMP, JPG, JPEG)",
            Filter = "Зображення (*.bmp;*.jpg;*.jpeg)|*.bmp;*.jpg;*.jpeg|BMP (*.bmp)|*.bmp|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|Всі файли (*.*)|*.*",
            Multiselect = true
        };

        if (openFileDialog.ShowDialog() != true) return;

        string[] selectedFiles = openFileDialog.FileNames;
        if (selectedFiles == null || selectedFiles.Length == 0) return;

        // Перевірка на перевищення встановленого ліміту M
        int currentCount = LoadedClasses.Count;
        int remainingSlots = TargetM - currentCount;

        if (remainingSlots <= 0)
        {
            var result = MessageBox.Show(
                $"Поточна кількість завантажених зображень ({currentCount}) вже досягла заданого числа класів m = {TargetM}.\n\n" +
                $"Бажаєте збільшити значення m до {currentCount + selectedFiles.Length} та додати обрані файли?",
                "Перевищення кількості класів",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                TargetM = currentCount + selectedFiles.Length;
            }
            else
            {
                return;
            }
        }
        else if (selectedFiles.Length > remainingSlots)
        {
            var result = MessageBox.Show(
                $"Обрано {selectedFiles.Length} файлів, але доступно лише {remainingSlots} слот(ів) до ліміту m = {TargetM}.\n\n" +
                $"Бажаєте збільшити кількість класів m до {currentCount + selectedFiles.Length}?",
                "Збільшення числа класів",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                TargetM = currentCount + selectedFiles.Length;
            }
            else
            {
                // Додаємо тільки скільки дозволено
                selectedFiles = selectedFiles.Take(remainingSlots).ToArray();
            }
        }

        // Завантаження кожного файлу з обов'язковою валідацією геометрії
        foreach (var file in selectedFiles)
        {
            try
            {
                int nextIndex = LoadedClasses.Count + 1;
                var classInfo = _imageService.LoadClassImage(file, nextIndex);

                // Якщо це перше зображення, фіксуємо еталонні розміри
                if (!ReferenceWidth.HasValue || !ReferenceHeight.HasValue)
                {
                    ReferenceWidth = classInfo.Width;
                    ReferenceHeight = classInfo.Height;
                    if (IsAutoDimensions)
                    {
                        CustomL = classInfo.Height;
                        CustomN = classInfo.Width;
                    }
                    Log($"[Еталон] Встановлено еталонну геометричну розмірність: {ReferenceWidth} × {ReferenceHeight} px за файлом {classInfo.FileName}.");
                }
                else
                {
                    // Валідація геометрії проти еталону
                    if (!_imageService.ValidateGeometry(classInfo, ReferenceWidth.Value, ReferenceHeight.Value, out string errorMsg))
                    {
                        Log($"[Помилка валідації] Відхилено файл {classInfo.FileName}: розмірність {classInfo.Width}x{classInfo.Height} != еталонній {ReferenceWidth}x{ReferenceHeight}.");
                        MessageBox.Show(errorMsg, "Помилка валідації геометричних розмірів", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue; // пропускаємо некоректний файл
                    }
                }

                LoadedClasses.Add(classInfo);
                Log($"Додано клас {classInfo.ClassIndex}: \"{classInfo.FileName}\" ({classInfo.DimensionsString}).");
            }
            catch (Exception ex)
            {
                Log($"[Помилка завантаження] Файл {file}: {ex.Message}");
                MessageBox.Show($"Помилка читання файлу \"{file}\":\n{ex.Message}", "Помилка читання", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        ReindexClasses();
        NotifyCountersChanged();
        ResetBinarizationState();
    }

    private void ExecuteLoadDemoResources(object? parameter)
    {
        try
        {
            // Пошук папки Resources у поточному каталозі та у каталозі додатку
            string[] searchPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources")
            };

            string? foundDir = searchPaths.FirstOrDefault(d => Directory.Exists(d));
            if (foundDir == null)
            {
                MessageBox.Show("Папку Resources не знайдено на диску.", "Помилка демо-завантаження", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string file1 = Path.Combine(foundDir, "1.bmp");
            string file2 = Path.Combine(foundDir, "2.bmp");

            if (!File.Exists(file1) || !File.Exists(file2))
            {
                MessageBox.Show($"Файли 1.bmp або 2.bmp не знайдено у папці {foundDir}.", "Помилка демо-завантаження", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Очищення попереднього стану
            LoadedClasses.Clear();
            ReferenceWidth = null;
            ReferenceHeight = null;
            TargetM = 2;
            ResetBinarizationState();

            var class1 = _imageService.LoadClassImage(file1, 1, "Клас 1 (1.bmp - норма)");
            ReferenceWidth = class1.Width;
            ReferenceHeight = class1.Height;
            CustomL = class1.Height;
            CustomN = class1.Width;
            LoadedClasses.Add(class1);

            var class2 = _imageService.LoadClassImage(file2, 2, "Клас 2 (2.bmp - відхилення)");
            LoadedClasses.Add(class2);

            Log($"[Демо] Успішно завантажено тестові зображення варіанту з {foundDir}: 1.bmp ({class1.DimensionsString}), 2.bmp ({class2.DimensionsString}).");

            ReindexClasses();
            NotifyCountersChanged();

            // Автоматичне формування навчальної матриці Y
            ExecuteFormTrainingMatrix(null);
        }
        catch (Exception ex)
        {
            Log($"[Помилка демо] {ex.Message}");
            MessageBox.Show($"Помилка демо-завантаження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteRemoveClass(object? parameter)
    {
        if (parameter is ClassImageInfo info && LoadedClasses.Contains(info))
        {
            LoadedClasses.Remove(info);
            Log($"Видалено клас {info.ClassIndex} (\"{info.FileName}\").");

            ResetBinarizationState();

            if (LoadedClasses.Count == 0)
            {
                ReferenceWidth = null;
                ReferenceHeight = null;
                IsMatrixFormed = false;
                CurrentMatrixView = null;
                SelectedClassMatrixInfo = "Всі класи видалено. Завантажте нові зображення.";
                Log("Усі класи видалено. Еталонні розміри скинуто.");
            }
            else
            {
                IsMatrixFormed = false;
            }

            ReindexClasses();
            NotifyCountersChanged();
        }
    }

    private void ExecuteClearAllClasses(object? parameter)
    {
        if (LoadedClasses.Count == 0) return;

        var result = MessageBox.Show("Ви впевнені, що бажаєте видалити всі завантажені класи?", "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            LoadedClasses.Clear();
            ReferenceWidth = null;
            ReferenceHeight = null;
            IsMatrixFormed = false;
            CurrentMatrixView = null;
            SelectedClassMatrixInfo = "Список очищено. Завантажте зображення класів.";
            SelectedMinBrightness = 0;
            SelectedMaxBrightness = 0;
            SelectedAvgBrightness = 0;
            ResetBinarizationState();
            Log("Список зображень очищено користувачем.");
            NotifyCountersChanged();
        }
    }

    private void ExecuteFormTrainingMatrix(object? parameter)
    {
        // ВАЛІДАЦІЯ 1: Чи завантажені зображення взагалі
        if (LoadedClasses.Count == 0)
        {
            MessageBox.Show(
                "Помилка: не завантажено жодного зображення класу!\n\n" +
                "Будь ласка, скористайтеся кнопкою «Додати зображення класу» або «Завантажити з папки Resources».",
                "Валідація вхідних даних",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // ВАЛІДАЦІЯ 2: Перевірка відповідності кількості завантажених зображень заданій кількості класів M
        if (LoadedClasses.Count != TargetM)
        {
            MessageBox.Show(
                $"Помилка валідації кількості класів розпізнавання!\n\n" +
                $"Задана кількість класів у налаштуваннях: m = {TargetM}.\n" +
                $"Фактично завантажено зображень: {LoadedClasses.Count}.\n\n" +
                (LoadedClasses.Count < TargetM
                    ? $"Будь ласка, завантажте ще {TargetM - LoadedClasses.Count} зображення(ь) або змініть значення m на {LoadedClasses.Count}."
                    : $"Будь ласка, видаліть {LoadedClasses.Count - TargetM} зайве(і) зображення або змініть значення m на {LoadedClasses.Count}."),
                "Невідповідність кількості класів",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // ВАЛІДАЦІЯ 3: Перевірка розмірностей l та N у разі ручного режиму
        int targetL;
        int targetN;

        if (IsAutoDimensions)
        {
            targetL = ReferenceHeight ?? LoadedClasses[0].Height;
            targetN = ReferenceWidth ?? LoadedClasses[0].Width;
        }
        else
        {
            int maxH = ReferenceHeight ?? LoadedClasses[0].Height;
            int maxW = ReferenceWidth ?? LoadedClasses[0].Width;

            if (CustomL < 1 || CustomL > maxH || CustomN < 1 || CustomN > maxW)
            {
                MessageBox.Show(
                    $"Некоректні користувацькі розміри зрізу матриці!\n\n" +
                    $"Кількість реалізацій L повинна бути в діапазоні 1..{maxH} (висота зображення).\n" +
                    $"Кількість ознак N повинна бути в діапазоні 1..{maxW} (ширина зображення).\n\n" +
                    $"Введено: L = {CustomL}, N = {CustomN}.",
                    "Помилка розмірності",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            targetL = CustomL;
            targetN = CustomN;
        }

        try
        {
            // Формування математичного опису trecognition_obj (ЛР №12)
            RecognitionObj.CreateMathematicalDescription(LoadedClasses.ToList(), targetL, targetN);
            IsMatrixFormed = true;
            ResetBinarizationState(); // Скидання попередніх бінарних даних при зміні Y
            NotifyCountersChanged();

            Log($"[Успіх] Навчальну матрицю Y успішно сформовано! Параметри: M = {RecognitionObj.M}, L = {RecognitionObj.L}, N = {RecognitionObj.N}.");
            Log($"[Метрика] Загальна кількість точок вхідного опису: {RecognitionObj.M * RecognitionObj.L * RecognitionObj.N} елементів.");

            // Оновлення перегляду інспектора для поточного класу
            if (SelectedClassIndexForInspector >= RecognitionObj.M)
            {
                SelectedClassIndexForInspector = 0;
            }
            UpdateMatrixInspectorView();

            MessageBox.Show(
                $"Навчальну матрицю Y типу «об'єкт-властивість» успішно сформовано!\n\n" +
                $"• Число класів (m): {RecognitionObj.M}\n" +
                $"• Кількість ознак розпізнавання (N): {RecognitionObj.N}\n" +
                $"• Кількість реалізацій образу (l): {RecognitionObj.L}\n" +
                $"• Загальний обсяг даних вибірки: {RecognitionObj.M * RecognitionObj.L * RecognitionObj.N:N0} значень.\n\n" +
                $"Ви можете переглянути числову матрицю яскравості пікселів (0..255) в інспекторі нижче.\n" +
                $"Тепер ви можете перейти на вкладку «ЛР №13: Бінаризація та СКД» для побудови допусків та бінаризації.",
                "Матрицю Y сформовано",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            IsMatrixFormed = false;
            ResetBinarizationState();
            Log($"[Помилка формування] {ex.Message}");
            MessageBox.Show($"Помилка формування матриці: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateMatrixInspectorView()
    {
        if (!IsMatrixFormed || RecognitionObj.M == 0 || SelectedClassIndexForInspector < 0 || SelectedClassIndexForInspector >= RecognitionObj.M)
        {
            CurrentMatrixView = null;
            SelectedClassMatrixInfo = "Матрицю Y ще не сформовано або клас не обрано.";
            SelectedMinBrightness = 0;
            SelectedMaxBrightness = 0;
            SelectedAvgBrightness = 0;
            return;
        }

        try
        {
            int c = SelectedClassIndexForInspector;
            int l = RecognitionObj.L;
            int n = RecognitionObj.N;

            var slice = RecognitionObj.Y[c];
            var dataTable = _imageService.CreateDataTableForMatrix(slice, l, n);
            CurrentMatrixView = dataTable.DefaultView;

            var stats = _imageService.CalculateMatrixStatistics(slice, l, n);
            SelectedMinBrightness = stats.Min;
            SelectedMaxBrightness = stats.Max;
            SelectedAvgBrightness = stats.Average;

            string className = (RecognitionObj.Name != null && c < RecognitionObj.Name.Length)
                ? RecognitionObj.Name[c]
                : $"Клас {c + 1}";

            SelectedClassMatrixInfo = $"Матриця Y для класу #{c + 1} («{className}»): {l} рядків-реалізацій × {n} стовпчиків-ознак.";
        }
        catch (Exception ex)
        {
            Log($"[Помилка відображення інспектора Y] {ex.Message}");
        }
    }

    // ==========================================
    // РЕАЛІЗАЦІЯ ДІЙ ТА ВАЛІДАЦІЇ (ЛР №13: СКД ТА БІНАРИЗАЦІЯ)
    // ==========================================

    private void ExecuteBinarize(object? parameter)
    {
        if (!IsMatrixFormed || RecognitionObj.M == 0 || RecognitionObj.L == 0 || RecognitionObj.N == 0)
        {
            MessageBox.Show(
                "Помилка: Навчальна матриця Y ще не сформована!\n\n" +
                "Будь ласка, перейдіть на вкладку «ЛР №12: Навчальна матриця (Y)», завантажте зображення класів та натисніть «Сформувати навчальну матрицю Y».",
                "Неможливо виконати бінаризацію",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (BaseClassIndex < 0 || BaseClassIndex >= RecognitionObj.M)
        {
            BaseClassIndex = 0;
        }

        if (StepKD < 0 || StepKD > 255)
        {
            MessageBox.Show(
                "Крок коридору допусків δ (StepKD) повинен знаходитись у межах від 0 до 255.",
                "Некоректний крок дельта",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            int m = RecognitionObj.M;
            int l = RecognitionObj.L;
            int n = RecognitionObj.N;

            // 1. Вектор математичних сподівань базового класу MeanVector[N]
            var mean = _binarizationService.ComputeMeanVector(RecognitionObj.Y[BaseClassIndex], l, n);

            // 2. Система контрольних допусків ND[N] та VD[N]
            var (nd, vd) = _binarizationService.BuildTolerances(mean, StepKD);

            // 3. Бінаризація 3D-матриці Y -> X
            var x = _binarizationService.Binarize(RecognitionObj.Y, nd, vd, m, l, n);

            // 4. Двійкові еталонні вектори EV[M][N] (строго > 0.5)
            var ev = _binarizationService.BuildReferenceVectors(x, m, l, n);

            // Збереження в ядро trecognition_obj
            RecognitionObj.BaseClassIndex = BaseClassIndex;
            RecognitionObj.StepKD = StepKD;
            RecognitionObj.MeanVector = mean;
            RecognitionObj.ND = nd;
            RecognitionObj.VD = vd;
            RecognitionObj.X = x;
            RecognitionObj.EV = ev;

            // 5. Оновлення колекцій статистики для UI
            BinaryClassesStatistics.Clear();
            ReferenceVectors.Clear();

            for (int c = 0; c < m; c++)
            {
                string className = (LoadedClasses.Count > c) ? LoadedClasses[c].ClassName : $"Клас {c + 1}";
                bool isBase = (c == BaseClassIndex);

                var stat = _binarizationService.CalculateOnesStatistics(x[c], l, n);
                BinaryClassesStatistics.Add(new BinaryClassStatInfo
                {
                    ClassIndex = c + 1,
                    ClassName = className,
                    IsBaseClass = isBase,
                    OnesCount = stat.Count,
                    TotalElements = l * n,
                    Percentage = stat.Percentage
                });

                var evStat = _binarizationService.CalculateVectorOnesStatistics(ev[c]);
                var barcode = _binarizationService.RenderBarcodeBitmap(ev[c], n, 28);
                ReferenceVectors.Add(new ReferenceVectorInfo
                {
                    ClassIndex = c + 1,
                    ClassName = className,
                    IsBaseClass = isBase,
                    OnesCount = evStat.Count,
                    TotalFeatures = n,
                    Percentage = evStat.Percentage,
                    BarcodeImage = barcode
                });
            }

            // 6. Оновлення точок для Canvas-графіка коридору допусків
            UpdateToleranceGraphPoints(mean, nd, vd, n);

            // Позначаємо успішне завершення бінаризації перед оновленням інспектора
            IsBinarized = true;

            // 7. Оновлення перегляду інспектора бінарної матриці
            if (SelectedClassIndexForBinaryInspector >= m || SelectedClassIndexForBinaryInspector < 0)
            {
                SelectedClassIndexForBinaryInspector = 0;
            }
            UpdateBinaryInspectorView();
            NotifyCountersChanged();

            string baseName = (LoadedClasses.Count > BaseClassIndex) ? LoadedClasses[BaseClassIndex].ClassName : $"Клас {BaseClassIndex + 1}";
            Log($"[ЛР №13: СКД та Бінаризація] Сформовано СКД: Базовий клас = «{baseName}», крок δ = {StepKD}, m = {m}, l = {l}, N = {n}.");
            foreach (var s in BinaryClassesStatistics)
            {
                var evItem = ReferenceVectors.FirstOrDefault(r => r.ClassIndex == s.ClassIndex);
                Log($"   • {s.ClassName}: X одиниць = {s.OnesCount:N0} ({s.Percentage:F1}%), EV одиниць = {evItem?.OnesCount ?? 0}/{n} ({evItem?.Percentage ?? 0:F1}%).");
            }

            MessageBox.Show(
                $"Систему контрольних допусків (СКД) та бінарні матриці X успішно сформовано!\n\n" +
                $"• Базовий клас: {baseName}\n" +
                $"• Крок коридору допусків (δ): {StepKD}\n" +
                $"• Кількість класів розпізнавання: {m}\n" +
                $"• Розмірність матриць: {l} рядків × {n} стовпчиків\n\n" +
                $"Праворуч оновлено графік коридору СКД, інспектор бінарної матриці X, растрову карту та штрих-коди еталонів EV.",
                "СКД та бінаризація успішні",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            IsBinarized = false;
            Log($"[Помилка бінаризації] {ex.Message}");
            MessageBox.Show($"Помилка бінаризації: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateToleranceGraphPoints(double[] mean, double[] nd, double[] vd, int n)
    {
        if (mean == null || nd == null || vd == null || n <= 1) return;

        double xMin = 45.0;
        double xMax = 665.0;
        double xSpan = xMax - xMin;

        double yTop = 15.0;     // відповідає значенню 255
        double yBottom = 185.0;  // відповідає значенню 0
        double ySpan = yBottom - yTop; // 170.0

        var meanPts = new PointCollection(n);
        var ndPts = new PointCollection(n);
        var vdPts = new PointCollection(n);

        for (int i = 0; i < n; i++)
        {
            double xCoord = xMin + ((double)i / (n - 1)) * xSpan;

            double meanVal = Math.Clamp(mean[i], 0.0, 255.0);
            double yMean = yBottom - (meanVal / 255.0) * ySpan;
            meanPts.Add(new Point(xCoord, yMean));

            double ndVal = Math.Clamp(nd[i], -15.0, 270.0);
            double yNd = yBottom - (ndVal / 255.0) * ySpan;
            ndPts.Add(new Point(xCoord, yNd));

            double vdVal = Math.Clamp(vd[i], -15.0, 270.0);
            double yVd = yBottom - (vdVal / 255.0) * ySpan;
            vdPts.Add(new Point(xCoord, yVd));
        }

        meanPts.Freeze();
        ndPts.Freeze();
        vdPts.Freeze();

        ToleranceMeanPoints = meanPts;
        ToleranceNDPoints = ndPts;
        ToleranceVDPoints = vdPts;
    }

    private void UpdateBinaryInspectorView()
    {
        if (!IsBinarized || RecognitionObj.M == 0 || SelectedClassIndexForBinaryInspector < 0 || SelectedClassIndexForBinaryInspector >= RecognitionObj.M)
        {
            CurrentBinaryMatrixView = null;
            CurrentBinaryVisualMap = null;
            SelectedBinaryClassInfo = "Бінаризацію ще не виконано або клас не обрано.";
            SelectedBinaryOnesCount = 0;
            SelectedBinaryOnesPercentage = 0.0;
            return;
        }

        try
        {
            int c = SelectedClassIndexForBinaryInspector;
            int l = RecognitionObj.L;
            int n = RecognitionObj.N;
            var xSlice = RecognitionObj.X[c];

            var dataTable = _binarizationService.CreateDataTableForBinaryMatrix(xSlice, l, n);
            CurrentBinaryMatrixView = dataTable.DefaultView;

            CurrentBinaryVisualMap = _binarizationService.RenderBinaryMatrixBitmap(xSlice, l, n);

            var stat = _binarizationService.CalculateOnesStatistics(xSlice, l, n);
            SelectedBinaryOnesCount = stat.Count;
            SelectedBinaryOnesPercentage = stat.Percentage;

            string className = (LoadedClasses.Count > c) ? LoadedClasses[c].ClassName : $"Клас {c + 1}";
            SelectedBinaryClassInfo = $"Бінарна матриця X для класу #{c + 1} («{className}»): {l} рядків-реалізацій × {n} стовпчиків-ознак.";
        }
        catch (Exception ex)
        {
            Log($"[Помилка інспектора X] {ex.Message}");
        }
    }

    private void ResetBinarizationState()
    {
        IsBinarized = false;
        CurrentBinaryMatrixView = null;
        CurrentBinaryVisualMap = null;
        BinaryClassesStatistics.Clear();
        ReferenceVectors.Clear();
        ToleranceMeanPoints = null;
        ToleranceNDPoints = null;
        ToleranceVDPoints = null;
        SelectedBinaryClassInfo = "Бінаризацію ще не виконано.";
        SelectedBinaryOnesCount = 0;
        SelectedBinaryOnesPercentage = 0.0;
        NotifyCountersChanged();
    }

    private void ReindexClasses()
    {
        for (int i = 0; i < LoadedClasses.Count; i++)
        {
            LoadedClasses[i].ClassIndex = i + 1;
        }
    }

    private void NotifyCountersChanged()
    {
        OnPropertyChanged(nameof(StatusCounterText));
        OnPropertyChanged(nameof(StatusBrush));
        OnPropertyChanged(nameof(TotalElementsCountText));
        OnPropertyChanged(nameof(BaseClassNameText));
        OnPropertyChanged(nameof(MatrixM));
        OnPropertyChanged(nameof(MatrixL));
        OnPropertyChanged(nameof(MatrixN));
    }

    private void Log(string message)
    {
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogMessages.Insert(0, logEntry);
    }

    // ==========================================
    // INotifyPropertyChanged
    // ==========================================

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
