using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class ImagingImmsImageModel : DisposableModelBase
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private readonly IMessageBroker _broker;
        private int _roiId = 0;

        public ImagingImmsImageModel(AnalysisFileBeanModel file, IMsdialDataStorage<MsdialImmsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBean> providerFactory, FilePropertiesModel projectBaseParameterModel, IMessageBroker broker) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            _broker = broker;
            _semaphoreSlim = new SemaphoreSlim(1, 1).AddTo(Disposables);
            var maldiFrames = new MaldiFrames(file.File.GetMaldiFrames());
            var wholeRoi = new RoiModel(file, _roiId, maldiFrames, ChartBrushes.GetChartBrush(_roiId).Color);
            ++_roiId;
            var imageResult = new WholeImageResultModel(file, maldiFrames, wholeRoi, storage, evaluator, providerFactory, projectBaseParameterModel, broker).AddTo(Disposables);
            ImageResult = imageResult;

            ImagingRoiModels = [ imageResult.ImagingRoiModel ];
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
                var imagingRoi = ImageResult.CreateImagingRoiModel(roi);
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

        public void RemoveRoi(ImagingRoiModel model) {
            ImagingRoiModels.Remove(model);
        }

        public async Task SaveRoisAsync(CancellationToken token = default) {
            var tasks = new List<Task>();
            foreach (var roi in ImagingRoiModels) {
                tasks.Add(roi.SavePositionsAsync(token));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task SaveIntensitiesAsync(CancellationToken token = default) {
            await Task.WhenAll([SaveRoisAsync(token), ImageResult.SaveIntensitiesAsync(token)]).ConfigureAwait(false);
        }

        public void LoadRoi() {
            string? path = null;
            var request = new OpenFileRequest(p => path = p)
            {
                Filter = "csv|*.csv",
                Title = "Open ROI file",
            };
            _broker.Publish(request);
            if (!System.IO.File.Exists(path)) {
                return;
            }
            using var reader = new StreamReader(path);
            reader.ReadLine(); // skip header
            var sets = new HashSet<(int, int)>();
            while (!reader.EndOfStream) {
                var row = reader.ReadLine();
                var fields = row.Split(',');
                if (int.TryParse(fields[0], out var f1) && int.TryParse(fields[1], out var f2)) {
                    sets.Add((f1, f2));
                }
            }
            var maldiFrames = ImageResult.GetFramesFromPositions(sets);
            var roi = new RoiModel(File, _roiId, maldiFrames, ChartBrushes.GetChartBrush(_roiId).Color);
            ++_roiId;
            var imageRoi = ImageResult.CreateImagingRoiModel(roi).AddTo(Disposables);
            ImagingRoiModels.Add(imageRoi);
        }
    }
}
