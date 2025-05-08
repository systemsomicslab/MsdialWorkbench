using System.Windows.Controls;

namespace ChartDrawingUiTest.Chart;

/// <summary>
/// Interaction logic for NoProperty.xaml
/// </summary>
public partial class NoProperty : Page
{
    public NoProperty() {
        InitializeComponent();

        DataContext = this;
    }

    public int[] Values { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9];
}
