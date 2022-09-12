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

            Roi = new RoiModel(file, file.GetMaldiFrames());
            ImagingRoiModels.Add(new ImagingRoiModel(Roi, ImageResult.GetTargetElements(), ImageResult.Target, file.GetMaldiFrameLaserInfo()).AddTo(Disposables));
        }

        public WholeImageResultModel ImageResult { get; }
        public RoiModel Roi { get; }
        public ObservableCollection<ImagingRoiModel> ImagingRoiModels { get; }
        public AnalysisFileBean File { get; }
    }
}
