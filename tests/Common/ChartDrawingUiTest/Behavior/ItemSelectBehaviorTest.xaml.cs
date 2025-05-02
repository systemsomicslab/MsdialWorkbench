using System.ComponentModel;
using System.Windows.Controls;

namespace ChartDrawingUiTest.Behavior;

/// <summary>
/// Interaction logic for ItemSelectBehaviorTest.xaml
/// </summary>
public partial class ItemSelectBehaviorTest : Page, INotifyPropertyChanged
{
    public ItemSelectBehaviorTest() {
        InitializeComponent();

        DataContext = this;
    }

    public string SelectedItem {
        get => _selectedItem;
        set {
            _selectedItem = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
        }
    }

    private string? _selectedItem = "A";

    public event PropertyChangedEventHandler PropertyChanged;

    public string[] Items { get; } = ["A", "B", "C", "D", "E"];
}
