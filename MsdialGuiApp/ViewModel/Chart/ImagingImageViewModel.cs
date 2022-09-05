using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class ImagePixel : BindableBase {
        public double X { get; }
        public double Y { get; }
        public double Intensity { get; }
    }

    internal sealed class ImagingImageViewModel : ViewModelBase
    {
        public ReadOnlyCollection<ImagePixel> ImagePixels { get; }
        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
        public GradientStopCollection GradientStops { get; }
    }
}
