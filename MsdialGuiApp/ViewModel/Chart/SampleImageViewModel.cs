using CompMs.App.Msdial.Model.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class SampleImageViewModel : ViewModelBase
    {
        private readonly SampleImageModel _model;

        public SampleImageViewModel(SampleImageModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            ImagePixels = _model.ImagePixels.AsReadOnly();
        }

        public string Title => _model.Title;
        public ReadOnlyCollection<ImagePixel> ImagePixels { get; }
        public IAxisManager<double> HorizontalAxis => _model.HorizontalAxis;
        public IAxisManager<double> VerticalAxis => _model.VerticalAxis;
        public GradientBrushMapper<double> GradientBrushMapper => _model.GradientBrushMapper;
        public GradientStopCollection GradientStops => GradientBrushMapper.GradientStops;
        public double PatchWidth => _model.PatchWidth;
        public double PatchHeight => _model.PatchHeight;
    }
}
