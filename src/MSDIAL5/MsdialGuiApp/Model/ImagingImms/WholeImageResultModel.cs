using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.RawDataHandler.Core;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class WholeImageResultModel : DisposableModelBase
    {
        private readonly ImmsAnalysisModel _analysisModel;
        private readonly ObservableCollection<IntensityImageModel> _intensities;
        private readonly List<Raw2DElement> _elements;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
        private readonly AnalysisFileBeanModel _file;
        private readonly MaldiFrames _maldiFrames;
        private readonly RoiModel _wholeRoi;

        public WholeImageResultModel(AnalysisFileBeanModel file, MaldiFrames maldiFrames, RoiModel wholeRoi, IMsdialDataStorage<MsdialImmsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBean> providerFactory, FilePropertiesModel projectBaseParameterModel, IMessageBroker broker) {
            var peakFilter = new PeakFilterModel(DisplayFilter.All);
            var filterEnabled = FilterEnableStatus.All & ~FilterEnableStatus.Rt & ~FilterEnableStatus.Protein;
            var peakFiltering = new PeakSpotFiltering<ChromatogramPeakFeatureModel>(filterEnabled).AddTo(Disposables);
            _msfinderSearcherFactory = new MsfinderSearcherFactory(storage.DataBases, storage.DataBaseMapper, storage.Parameter, "MS-FINDER").AddTo(Disposables);
            var analysisModel = new ImmsAnalysisModel(file, providerFactory.Create(file.File), evaluator, storage.DataBases, storage.DataBaseMapper, storage.Parameter, peakFilter, peakFiltering, projectBaseParameterModel, _msfinderSearcherFactory, broker).AddTo(Disposables);
            _analysisModel = analysisModel;

            _elements = analysisModel.Ms1Peaks.Select(item => new Raw2DElement(item.Mass, item.Drift.Value)).ToList();
            var rawIntensityLoader = wholeRoi.GetIntensityOnPixelsLoader(_elements);
            ImagingRoiModel = new ImagingRoiModel($"ROI{wholeRoi.Id}", wholeRoi, null, analysisModel.Ms1Peaks, analysisModel.Target, rawIntensityLoader).AddTo(Disposables);
            ImagingRoiModel.Select();
            MaldiFrameLaserInfo laserInfo = file.File.GetMaldiFrameLaserInfo();
            _intensities = new ObservableCollection<IntensityImageModel>(analysisModel.Ms1Peaks.Select((peak, index) => new IntensityImageModel(maldiFrames, peak, laserInfo, rawIntensityLoader, index)));
            Intensities = new ReadOnlyObservableCollection<IntensityImageModel>(_intensities);
            analysisModel.Target.Select(p => _intensities.FirstOrDefault(intensity => intensity.Peak == p))
                .Subscribe(intensity => SelectedPeakIntensities = intensity)
                .AddTo(Disposables);
            _file = file;
            _maldiFrames = maldiFrames;
            _wholeRoi = wholeRoi;
        }

        public ImmsAnalysisModel AnalysisModel => _analysisModel;
        public ObservableCollection<ChromatogramPeakFeatureModel> Peaks => _analysisModel.Ms1Peaks;
        public AnalysisPeakPlotModel PeakPlotModel => _analysisModel.PlotModel;
        public ReactivePropertySlim<ChromatogramPeakFeatureModel?> Target => _analysisModel.Target;

        public ImagingRoiModel ImagingRoiModel { get; }
        public ReadOnlyObservableCollection<IntensityImageModel> Intensities { get; }
        public IntensityImageModel? SelectedPeakIntensities
        {
            get => _selectedPeakIntensities;
            set => SetProperty(ref _selectedPeakIntensities, value);
        }
        private IntensityImageModel? _selectedPeakIntensities;

        public ImagingRoiModel CreateImagingRoiModel(RoiModel roi)
        {
            var loader = roi.GetIntensityOnPixelsLoader(_elements);
            var result = new ImagingRoiModel($"ROI{roi.Id}", roi, _wholeRoi, _analysisModel.Ms1Peaks, _analysisModel.Target, loader);
            result.Select();
            return result;
        }

        public async Task SaveIntensitiesAsync(CancellationToken token = default) {
            using var writer = File.Open("pixel_intensities.csv", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var header = string.Join(",", new[] { "ID", "Name", "m/z", "Drift", }.Concat(_maldiFrames.Infos.Select(info => $"{info.XIndexPos}_{info.YIndexPos}")));
            var encoded = UTF8Encoding.Default.GetBytes(header + "\n");
            writer.Write(encoded, 0, encoded.Length);
            using var sem = new SemaphoreSlim(8, 8);
            var tasks = new List<Task>(Intensities.Count);
            foreach (var ints in Intensities) {
                tasks.Add(Task.Run(async () => {
                    await sem.WaitAsync().ConfigureAwait(false);
                    try {
                        await ints.SaveAsync(writer);
                    }
                    finally {
                        sem.Release();
                    }
                }, token));
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public void ResetRawSpectraOnPixels() {
            using RawDataAccess rawDataAccess = new RawDataAccess(_file.AnalysisFilePath, 0, getProfileData: true, isImagingMsData: true, isGuiProcess: true);
            rawDataAccess.SaveRawPixelFeatures(_elements, _maldiFrames.Infos.ToList());
        }

        public MaldiFrames GetFramesFromPositions(HashSet<(int, int)> sets) {
            return new MaldiFrames(_maldiFrames.Infos.Where(info => sets.Contains((info.XIndexPos, info.YIndexPos))), _maldiFrames);
        }

        public Task SaveAsync()
        {
            return _analysisModel.SaveAsync(default);
        }
    }
}
