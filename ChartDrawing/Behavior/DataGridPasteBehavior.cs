using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.Graphics.Behavior
{
    public class DataGridPasteBehavior
    {
        public static bool GetEnable(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableProperty, value);
        }

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(DataGridPasteBehavior), new PropertyMetadata(false, EnablePropertyChanged));

        private static void EnablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is DataGrid datagrid)) return;

            if ((bool)e.NewValue)
            {
                datagrid.KeyDown += Datagrid_KeyDown;
            }
            else
            {
                datagrid.KeyDown -= Datagrid_KeyDown;
            }
        }

        private static void Datagrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is DataGrid datagrid)) return;

            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var clipText = Clipboard.GetText().Replace("\r\n", "\n").TrimEnd().Split('\n').Select(row => row.Split('\t')).ToArray();

                var startRow = datagrid.Items.IndexOf(datagrid.CurrentItem);
                var startCol = datagrid.Columns.IndexOf(datagrid.CurrentCell.Column);

                for (int i = 0; i < clipText.Length; i++)
                {
                    if (i + startRow >= datagrid.Items.Count) break;
                    var datas = clipText[i];
                    var item = datagrid.Items[i + startRow];

                    for (int j = 0; j < datas.Length; j++)
                    {
                        if (j + startCol >= datagrid.Columns.Count) break;
                        var data = datas[j];
                        datagrid.Columns[j + startCol].OnPastingCellClipboardContent(item, data);
                    }
                }
                datagrid.CommitEdit();
            }
        }

    }
}
