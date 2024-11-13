using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace ChartDrawingUiTest.Chart;

/// <summary>
/// Interaction logic for AxisValueMappingChangedEventTest.xaml
/// </summary>
public partial class AxisValueMappingChangedEventTest : Page, INotifyPropertyChanged
{
    public AxisValueMappingChangedEventTest() {
        InitializeComponent();
        DataContext = this;

        _horizontalAxis = new DefectAxisManager(DefectX) { ChartMargin = new ConstantMargin(20d) };
        _verticalAxis = new DefectAxisManager(DefectY) { ChartMargin = new ConstantMargin(20d) };
    }

    public List<DataPoint> Series { get; } = Enumerable.Range(0, 100).Select(v => new DataPoint() { X = v, Y = v, }).ToList();

    public double DefectX {
        get => _defectX;
        set {
            if (_defectX != value) {
                _defectX = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(DefectX)));
                _horizontalAxis.Divisor = _horizontalAxis.Factor = value;
            }
        }
    }
    private double _defectX = 100d;

    public double DefectY {
        get => _defectY;
        set {
            if (_defectY != value) {
                _defectY = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(DefectY)));
                _verticalAxis.Divisor = _verticalAxis.Factor = value;
            }
        }
    }
    private double _defectY = 1d;

    public IAxisManager<double> HorizontalAxis => _horizontalAxis;
    private readonly DefectAxisManager _horizontalAxis;

    public IAxisManager<double> VerticalAxis => _verticalAxis;
    private readonly DefectAxisManager _verticalAxis;

    public event PropertyChangedEventHandler PropertyChanged;
}
