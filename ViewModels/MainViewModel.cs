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
using Info_Extreme_System.Models;
using Info_Extreme_System.Services;
using Microsoft.Win32;

namespace Info_Extreme_System.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ImageProcessingService _imageService;

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

    public MainViewModel()
    {
        _imageService = new ImageProcessingService();
        RecognitionObj = new RecognitionObject();
        LoadedClasses = new ObservableCollection<ClassImageInfo>();
        LogMessages = new ObservableCollection<string>();

        // Commands
        AddImagesCommand = new RelayCommand(ExecuteAddImages);
        LoadDemoResourcesCommand = new RelayCommand(ExecuteLoadDemoResources);
        RemoveClassCommand = new RelayCommand(ExecuteRemoveClass);
        ClearAllClassesCommand = new RelayCommand(ExecuteClearAllClasses);
        FormTrainingMatrixCommand = new RelayCommand(ExecuteFormTrainingMatrix);

        Log("Система ІЕІТ ініціалізована. Готова до завантаження вхідних зображень для ЛР №12.");
    }

    // ==========================================
    // ВЛАСТИВОСТІ МОДЕЛІ ДАНИХ ТА СТАНУ
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
                OnPropertyChanged(nameof(TotalElementsCountText));
            }
        }
    }

    /// <summary>
    /// Протилежність IsAutoDimensions для зв'язування з інтерфейсом.
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
    /// Рядкове представлення еталонних розмірів.
    /// </summary>
    public string ReferenceDimensionsText =>
        (ReferenceWidth.HasValue && ReferenceHeight.HasValue)
            ? $"{ReferenceWidth.Value} × {ReferenceHeight.Value} px"
            : "Ще не визначено (завантажте перше зображення)";

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
            }
        }
    }

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
    // КОМАНДИ (COMMANDS)
    // ==========================================

    public ICommand AddImagesCommand { get; }
    public ICommand LoadDemoResourcesCommand { get; }
    public ICommand RemoveClassCommand { get; }
    public ICommand ClearAllClassesCommand { get; }
    public ICommand FormTrainingMatrixCommand { get; }

    // ==========================================
    // РЕАЛІЗАЦІЯ ДІЙ ТА ВАЛІДАЦІЇ
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

            // Автоматичне формування навчальної матриці для миттєвої готовності
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
                $"Ви можете переглянути числову матрицю яскравості пікселів (0..255) в інспекторі нижче.",
                "Матрицю Y сформовано",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            IsMatrixFormed = false;
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
            Log($"[Помилка відображення інспектора] {ex.Message}");
        }
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
    }

    private void Log(string message)
    {
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogMessages.Insert(0, logEntry); // Нові записи зверху
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
