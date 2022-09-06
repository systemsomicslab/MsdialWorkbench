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
        private readonly RoiModel _roi;
        private readonly ObservableCollection<RoiIntensitiesModel> _intensities;

        public ImagingRoiModel(RoiModel roi, List<Raw2DElement> elements) {
            _roi = roi ?? throw new System.ArgumentNullException(nameof(roi));
            var rawSpectraOnPixels = roi.RetrieveRawSpectraOnPixels(elements);
            _intensities = new ObservableCollection<RoiIntensitiesModel>(
                elements.Zip(rawSpectraOnPixels.PixelPeakFeaturesList,
                    (element, pixelPeaks) => new RoiIntensitiesModel(pixelPeaks, roi, element)));
        }
    }
}
