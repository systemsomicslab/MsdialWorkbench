using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingRoiModel : BindableBase
    {
        private readonly ObservableCollection<RoiIntensitiesModel> _intensities;

        public ImagingRoiModel(RoiModel roi, List<Raw2DElement> elements, MaldiFrameLaserInfo laserInfo) {
            Roi = roi ?? throw new System.ArgumentNullException(nameof(roi));
            var rawSpectraOnPixels = roi.RetrieveRawSpectraOnPixels(elements.Take(10).ToList());
            _intensities = new ObservableCollection<RoiIntensitiesModel>(
                elements.Zip(rawSpectraOnPixels.PixelPeakFeaturesList,
                    (element, pixelPeaks) => new RoiIntensitiesModel(pixelPeaks, rawSpectraOnPixels.XYFrames, roi, element, laserInfo)));
            Intensities = new ReadOnlyObservableCollection<RoiIntensitiesModel>(_intensities);
        }

        public RoiModel Roi { get; }
        public ReadOnlyObservableCollection<RoiIntensitiesModel> Intensities { get; }
    }
}
