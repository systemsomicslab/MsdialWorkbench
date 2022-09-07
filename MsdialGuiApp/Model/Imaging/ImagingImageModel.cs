using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingImageModel : DisposableModelBase
    {
        public ImagingImageModel(AnalysisFileBean file) {
            File = file ?? throw new System.ArgumentNullException(nameof(file));
            ImagingRoiModels = new ObservableCollection<ImagingRoiModel>();
            ImageResult = new WholeImageResultModel(file).AddTo(Disposables);

            var rawObj = file.LoadRawMeasurement(isImagingMsData: false, isGuiProcess: true, retry: 5, sleepMilliSeconds: 100);
            Roi = new RoiModel(file, rawObj.MaldiFrames);
            ImagingRoiModels.Add(new ImagingRoiModel(Roi, ImageResult.GetTargetElements(), rawObj.MaldiFrameLaserInfo));
        }

        public WholeImageResultModel ImageResult { get; }
        public RoiModel Roi { get; }
        public ObservableCollection<ImagingRoiModel> ImagingRoiModels { get; }
        public AnalysisFileBean File { get; }
    }
}
