using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingImageModel : BindableBase
    {
        private readonly WholeImageResultModel _imageResult;
        private readonly RoiModel _roi;
        private readonly ObservableCollection<ImagingRoiModel> _imagingRoiModels;

        public ImagingImageModel(AnalysisFileBean file) {
            _imagingRoiModels = new ObservableCollection<ImagingRoiModel>();
            _imageResult = new WholeImageResultModel(file);

            var rawObj = file.LoadRawMeasurement(isImagingMsData: false, isGuiProcess: true, retry: 5, sleepMilliSeconds: 100);
            _roi = new RoiModel(file, rawObj.MaldiFrames);
            _imagingRoiModels.Add(new ImagingRoiModel());
        }
    }
}
