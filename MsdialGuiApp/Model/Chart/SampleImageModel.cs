using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class ImagePixel : BindableBase {
        public ImagePixel(double x, double y, double intensity) {
            X = x;
            Y = y;
            Intensity = intensity;
        }

        public double X { get; }
        public double Y { get; }
        public double Intensity { get; }
    }

    internal sealed class SampleImageModel : DisposableModelBase
    {
        public SampleImageModel(IEnumerable<ImagePixel> imagePixels, string title, GradientStopCollection gradientStops, double patchWidth, double patchHeight) {
            if (imagePixels is null) {
                throw new ArgumentNullException(nameof(imagePixels));
            }

            Title = title;
            PatchWidth = patchWidth;
            PatchHeight = patchHeight;
            ImagePixels = (imagePixels as List<ImagePixel>) ?? imagePixels.ToList();
            HorizontalAxis = new ContinuousAxisManager<double>(ImagePixels.Select(pixel => pixel.X).ToArray()).AddTo(Disposables);
            VerticalAxis = new ContinuousAxisManager<double>(ImagePixels.Select(pixel => pixel.Y).ToArray()).AddTo(Disposables);
            GradientBrushMapper = new GradientBrushMapper<double>(ImagePixels.Min(pixel => pixel.Intensity), ImagePixels.Max(pixel => pixel.Intensity), gradientStops);
        }

        public string Title { get; }
        public double PatchWidth { get; }
        public double PatchHeight { get; }
        public List<ImagePixel> ImagePixels { get; }
        public IAxisManager<double> HorizontalAxis { get; }
        public IAxisManager<double> VerticalAxis { get; }
        public GradientBrushMapper<double> GradientBrushMapper { get; }
    }
}
