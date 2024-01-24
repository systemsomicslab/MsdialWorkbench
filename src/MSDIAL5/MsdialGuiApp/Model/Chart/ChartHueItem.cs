using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Graphics.Base;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class ChartHueItem {
        public ChartHueItem(string property, IBrushMapper brush) {
            Property = property;
            Brush = brush;
        }

        public ChartHueItem(FilePropertiesModel projectBaseParameterModel, Color defaultColor)
            : this(nameof(SpectrumPeak.SpectrumComment), projectBaseParameterModel.GetSpectrumBrush(defaultColor)) {
            
        }

        public string Property { get; }
        public IBrushMapper Brush { get; }
    }
}
