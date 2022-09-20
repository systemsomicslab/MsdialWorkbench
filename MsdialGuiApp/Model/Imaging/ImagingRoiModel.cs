using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingRoiModel : DisposableModelBase
    {
        private readonly ObservableCollection<RoiIntensitiesModel> _intensities;

        public ImagingRoiModel(RoiModel roi, List<(Raw2DElement, ChromatogramPeakFeatureModel)> elements, ReactiveProperty<ChromatogramPeakFeatureModel> peak,  MaldiFrameLaserInfo laserInfo) {
            Roi = roi ?? throw new System.ArgumentNullException(nameof(roi));
            var rawSpectraOnPixels = roi.RetrieveRawSpectraOnPixels(elements.Select(element => element.Item1).ToList());
            _intensities = new ObservableCollection<RoiIntensitiesModel>(
                elements.Zip(rawSpectraOnPixels.PixelPeakFeaturesList,
                    (element, pixelPeaks) => new RoiIntensitiesModel(pixelPeaks, rawSpectraOnPixels.XYFrames, roi, element.Item1, element.Item2, laserInfo)));
            Intensities = new ReadOnlyObservableCollection<RoiIntensitiesModel>(_intensities);
            SelectedPeakIntensities = _intensities.Select(m => peak.Where(p => m.Peak == p).Select(_ => m)).Merge().ToReactiveProperty().AddTo(Disposables);
        }

        public RoiModel Roi { get; }
        public ReadOnlyObservableCollection<RoiIntensitiesModel> Intensities { get; }
        public ReactiveProperty<RoiIntensitiesModel> SelectedPeakIntensities { get; }
    }
}
