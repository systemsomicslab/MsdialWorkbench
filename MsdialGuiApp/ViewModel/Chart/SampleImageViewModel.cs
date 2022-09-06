using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class ImagePixel : BindableBase {
        public double X { get; }
        public double Y { get; }
        public double Intensity { get; }
    }

    internal sealed class SampleImageViewModel : ViewModelBase
    {
        public string Title { get; }
        public ReadOnlyCollection<ImagePixel> ImagePixels { get; }
        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
        public GradientStopCollection GradientStops { get; }
    }
}
