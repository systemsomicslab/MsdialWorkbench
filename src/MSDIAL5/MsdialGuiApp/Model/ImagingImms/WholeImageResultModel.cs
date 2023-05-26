using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class WholeImageResultModel : DisposableModelBase
    {
        private readonly ImmsAnalysisModel _analysisModel;
        private readonly ObservableCollection<IntensityImageModel> _intensities;
        private readonly List<Raw2DElement> _elements;

        public WholeImageResultModel(AnalysisFileBeanModel file, MaldiFrames maldiFrames, RoiModel wholeRoi, IMsdialDataStorage<MsdialImmsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBeanModel> providerFactory, IMessageBroker broker) {
            var peakFilter = new PeakFilterModel(DisplayFilter.All);
            var analysisModel = new ImmsAnalysisModel(file, providerFactory.Create(file), evaluator, storage.DataBases, storage.DataBaseMapper, storage.Parameter, peakFilter, broker).AddTo(Disposables);
            _analysisModel = analysisModel;

            _elements = analysisModel.Ms1Peaks.Select(item => new Raw2DElement(item.Mass, item.Drift.Value)).ToList();
            var rawSpectraOnPixels = wholeRoi.RetrieveRawSpectraOnPixels(_elements);
            ImagingRoiModel = new ImagingRoiModel($"ROI{wholeRoi.Id}", wholeRoi, rawSpectraOnPixels, analysisModel.Ms1Peaks, analysisModel.Target).AddTo(Disposables);
            ImagingRoiModel.Select();
            MaldiFrameLaserInfo laserInfo = file.File.GetMaldiFrameLaserInfo();
            _intensities = new ObservableCollection<IntensityImageModel>(
                analysisModel.Ms1Peaks.Zip(rawSpectraOnPixels.PixelPeakFeaturesList,
                    (peak, pixelPeaks) => new IntensityImageModel(pixelPeaks, maldiFrames, peak, laserInfo)));
            Intensities = new ReadOnlyObservableCollection<IntensityImageModel>(_intensities);
            analysisModel.Target.Select(p => _intensities.FirstOrDefault(intensity => intensity.Peak == p))
                .SkipNull()
                .Subscribe(intensity => SelectedPeakIntensities = intensity)
                .AddTo(Disposables);
        }

        public ImmsAnalysisModel AnalysisModel => _analysisModel;
        public ObservableCollection<ChromatogramPeakFeatureModel> Peaks => _analysisModel.Ms1Peaks;
        public AnalysisPeakPlotModel PeakPlotModel => _analysisModel.PlotModel;
        public ReactivePropertySlim<ChromatogramPeakFeatureModel> Target => _analysisModel.Target;

        public ImagingRoiModel ImagingRoiModel { get; }
        public ReadOnlyObservableCollection<IntensityImageModel> Intensities { get; }
        public IntensityImageModel SelectedPeakIntensities
        {
            get => _selectedPeakIntensities;
            set => SetProperty(ref _selectedPeakIntensities, value);
        }
        private IntensityImageModel _selectedPeakIntensities;

        public async Task<ImagingRoiModel> CreateImagingRoiModelAsync(RoiModel roi)
        {
            var rawSpectraOnPixels = await Task.Run(() => roi.RetrieveRawSpectraOnPixels(_elements)).ConfigureAwait(false);
            var result = new ImagingRoiModel($"ROI{roi.Id}", roi, rawSpectraOnPixels, _analysisModel.Ms1Peaks, _analysisModel.Target);
            result.Select();
            return result;
        }
    }
}
