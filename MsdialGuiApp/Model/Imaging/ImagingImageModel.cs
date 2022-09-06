using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingImageModel : BindableBase
    {
        private readonly RoiModel _roi;
        private readonly WholeImageResultModel _imageResult;
        private readonly RoiIntensitiesModel[] _roiIntensities;
        private readonly ObservableCollection<ImagingRoiModel> _imagingRoiModels;
    }
}
