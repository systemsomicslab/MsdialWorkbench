using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisModel : BindableBase, IAnalysisModel, IDisposable
    {
        private bool _disposedValue;
        private CompositeDisposable _disposables;
        private readonly Ms1BasedSpectrumFeatureCollection _spectrumFeatures;
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _peaks;

        public GcmsAnalysisModel(AnalysisFileBeanModel file, IDataProviderFactory<AnalysisFileBeanModel> providerFactory, ProjectBaseParameter projectParameter, ChromDecBaseParameter chromDecParameter, DataBaseMapper dbMapper, DataBaseStorage dbStorage, ProjectBaseParameterModel projectBaseParameterModel) {
            _disposables = new CompositeDisposable();
            _spectrumFeatures = file.LoadMs1BasedSpectrumFeatureCollection().AddTo(_disposables);
            _peaks =  file.LoadChromatogramPeakFeatureModels();

            var compoundSearchers = CompoundSearcherCollection.BuildSearchers(dbStorage, dbMapper);
            var brushMapDataSelector = BrushMapDataSelectorFactory.CreatePeakFeatureBrushes(projectParameter.TargetOmics);
            PeakPlotModel = new SpectrumFeaturePlotModel(_spectrumFeatures, _peaks, brushMapDataSelector).AddTo(_disposables);

            var selectedSpectrum = PeakPlotModel.SelectedSpectrum;
            var matchResultCandidatesModel = new MatchResultCandidatesModel(selectedSpectrum.Select(t => t?.MatchResults)).AddTo(_disposables);
            MatchResultCandidatesModel = matchResultCandidatesModel;
            var rawSpectrumLoader = new MsRawSpectrumLoader(providerFactory.Create(file),projectParameter.MSDataType, chromDecParameter);
            var decLoader = new MSDecLoader(file.DeconvolutionFilePath).AddTo(_disposables);
            var decSpectrumLoader = new MsDecSpectrumLoader(decLoader, _spectrumFeatures.Items);
            var refLoader = (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference>(dbMapper);
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);

            var rawSpectrumLoader_ = rawSpectrumLoader.Contramap((Ms1BasedSpectrumFeature feature) => feature?.QuantifiedChromatogramPeak);
            var rawGraphLabels = new GraphLabels("Raw EI spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem measuredHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Blue);
            ObservableMsSpectrum rawObservableMsSpectrum = ObservableMsSpectrum.Create(selectedSpectrum, rawSpectrumLoader_, null).AddTo(_disposables);
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(rawObservableMsSpectrum, rawObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), rawObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, rawGraphLabels).AddTo(_disposables);

            var decLoader_ = decSpectrumLoader;
            var decGraphLabels = new GraphLabels("Deconvoluted EI spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum decObservableMsSpectrum = ObservableMsSpectrum.Create(selectedSpectrum, decLoader_, null).AddTo(_disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decObservableMsSpectrum, decObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), decObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, decGraphLabels).AddTo(_disposables);

            var refGraphLabels = new GraphLabels("Reference EI spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem referenceSpectrumHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Red);
            ObservableMsSpectrum refObservableMsSpectrum = ObservableMsSpectrum.Create(MatchResultCandidatesModel.SelectedCandidate, refLoader, null).AddTo(_disposables);
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refObservableMsSpectrum, refObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), refObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), referenceSpectrumHueItem, refGraphLabels).AddTo(_disposables);

            var ms2ScanMatching = MatchResultCandidatesModel.GetCandidatesScorer(compoundSearchers).Publish();
            _disposables.Add(ms2ScanMatching.Connect());

            RawDecSpectrumModel = new RawDecSpectrumsModel(rawSpectrumModel, decSpectrumModel, referenceSpectrumModel, ms2ScanMatching).AddTo(_disposables);

            // Raw vs Purified spectrum model
            RawPurifiedSpectrumsModel = new RawPurifiedSpectrumsModel(rawSpectrumModel, decSpectrumModel).AddTo(_disposables);
        }

        public SpectrumFeaturePlotModel PeakPlotModel { get; }
        public RawDecSpectrumsModel RawDecSpectrumModel { get; }
        public RawPurifiedSpectrumsModel RawPurifiedSpectrumsModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }

        // IAnalysisModel interface
        Task IAnalysisModel.SaveAsync(CancellationToken token) {
            throw new NotImplementedException();
        }

        // IResultModel interface
        void IResultModel.InvokeMsfinder() {
            throw new NotImplementedException();
        }

        void IResultModel.SearchFragment() {
            throw new NotImplementedException();
        }

        // IDisposable interface
        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    _disposables.Dispose();
                }

                _disposables.Clear();
                _disposables = null;
                _disposedValue = true;
            }
        }

        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
