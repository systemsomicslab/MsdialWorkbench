using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingRoiModel : BindableBase
    {
        private readonly RoiModel _roi;
        private readonly ObservableCollection<RoiIntensitiesModel> _intensities;
        private readonly ObservableCollection<RoiResultModel> _roiResults;
    }
}
