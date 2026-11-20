using System.Windows;
using System.Windows.Input;
using Info_Extreme_System.Models;

namespace Info_Extreme_System.Controls;

/// <summary>
/// Приєднана поведінка (Attached Behavior) для передачі координат кліку мишею по Canvas у команду ViewModel.
/// Дозволяє реалізувати інтерактивний вибір точок у канонічному MVVM без логіки в code-behind.
/// </summary>
public static class CanvasClickBehavior
{
    public static readonly DependencyProperty ClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "ClickCommand",
            typeof(ICommand),
            typeof(CanvasClickBehavior),
            new PropertyMetadata(null, OnClickCommandChanged));

    public static ICommand? GetClickCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(ClickCommandProperty);

    public static void SetClickCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(ClickCommandProperty, value);

    private static void OnClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.MouseLeftButtonDown -= ElementOnMouseLeftButtonDown;

        if (e.NewValue is ICommand)
        {
            element.MouseLeftButtonDown += ElementOnMouseLeftButtonDown;
        }
    }

    private static void ElementOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Handled) return;

        // Якщо клік здійснено безпосередньо по інтерактивному маркеру на карті SK_MAP - не перехоплюємо
        if (e.OriginalSource is FrameworkElement fe &&
            (fe.Tag as string == "InteractiveMarker" || fe.DataContext is SkMapPoint))
        {
            return;
        }

        if (sender is not UIElement element) return;

        var command = GetClickCommand(element);
        if (command == null) return;

        var pos = e.GetPosition(element);
        var info = new CanvasClickInfo(pos.X, pos.Y, element.RenderSize.Width, element.RenderSize.Height);

        if (command.CanExecute(info))
        {
            command.Execute(info);
            e.Handled = true;
        }
    }
}
