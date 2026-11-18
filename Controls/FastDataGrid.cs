using System.Collections.Generic;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Info_Extreme_System.Controls;

/// <summary>
/// Оптимізована реалізація <see cref="DataGrid"/> для швидкого відображення великих числових матриць.
/// Блокує створення важких екземплярів <see cref="DataGridCellItemAutomationPeer"/> та <see cref="DataGridItemAutomationPeer"/>
/// для кожної клітинки (10 000+ елементів), що усуває застигання UI потоку (UI Freeze)
/// при скролінгу, віртуалізації та активній підсистемі Windows UI Automation (dotTrace, Rider, Accessibility Services).
/// </summary>
public class FastDataGrid : DataGrid
{
    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return new FastDataGridAutomationPeer(this);
    }

    private sealed class FastDataGridAutomationPeer : FrameworkElementAutomationPeer
    {
        public FastDataGridAutomationPeer(FastDataGrid owner) : base(owner)
        {
        }

        protected override string GetClassNameCore() => nameof(FastDataGrid);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

        protected override List<AutomationPeer>? GetChildrenCore() => null;
    }
}
