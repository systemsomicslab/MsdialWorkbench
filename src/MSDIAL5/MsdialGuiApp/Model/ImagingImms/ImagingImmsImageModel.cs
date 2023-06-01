using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.Model.Information;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class ImagingImmsImageModel : DisposableModelBase
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private int _roiId = 0;

        public ImagingImmsImageModel(AnalysisFileBeanModel file, IMsdialDataStorage<MsdialImmsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBeanModel> providerFactory, IMessageBroker broker) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            _semaphoreSlim = new SemaphoreSlim(1, 1).AddTo(Disposables);
            var maldiFrames = new MaldiFrames(file.File.GetMaldiFrames());
            var wholeRoi = new RoiModel(file, _roiId, maldiFrames, ChartBrushes.GetChartBrush(_roiId).Color);
            ++_roiId;
            var imageResult = new WholeImageResultModel(file, maldiFrames, wholeRoi, storage, evaluator, providerFactory, broker).AddTo(Disposables);
            ImageResult = imageResult;

            ImagingRoiModels = new ObservableCollection<ImagingRoiModel>
            {
                imageResult.ImagingRoiModel
            };
            RoiEditModel = new RoiEditModel(file, maldiFrames);
            SaveImagesModel = new SaveImagesModel(imageResult, ImagingRoiModels);
        }

        public WholeImageResultModel ImageResult { get; }
        public ObservableCollection<ImagingRoiModel> ImagingRoiModels { get; }
        public RoiEditModel RoiEditModel { get; }
        public SaveImagesModel SaveImagesModel { get; }
        public AnalysisFileBeanModel File { get; }
        public PeakInformationAnalysisModel PeakInformationModel => ImageResult.AnalysisModel.PeakInformationModel;
        public MoleculeStructureModel MoleculeStructureModel => ImageResult.AnalysisModel.MoleculeStructureModel;

        public async Task AddRoiAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var roi = RoiEditModel.CreateRoi(_roiId, ChartBrushes.GetChartBrush(_roiId).Color);
                ++_roiId;
                if (roi is null)
                {
                    return;
                }
                var imagingRoi = await ImageResult.CreateImagingRoiModelAsync(roi);
                if (imagingRoi is null)
                {
                    return;
                }
                ImagingRoiModels.Add(imagingRoi.AddTo(Disposables));
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
