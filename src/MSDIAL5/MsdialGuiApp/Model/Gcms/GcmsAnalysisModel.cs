using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
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

        public GcmsAnalysisModel(AnalysisFileBeanModel file, IDataProviderFactory<AnalysisFileBeanModel> providerFactory, ProjectBaseParameter projectParameter, ChromDecBaseParameter chromDecParameter, DataBaseMapper dbMapper, DataBaseStorage dbStorage) {
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
            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               projectParameter.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Blue);
            Color mapToColor(SpectrumComment comment) {
                var commentString = comment.ToString();
                return projectParameter.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color) || (comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                    && projectParameter.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)
                    ? Color.FromRgb(color[0], color[1], color[2])
                    : Colors.Red;
            }
            var lowerSpecBrush = new DelegateBrushMapper<SpectrumComment>(mapToColor, true);
            RawDecSpectrumModel = RawDecSpectrumsModel.Create(
                selectedSpectrum,
                rawSpectrumLoader.Contramap((Ms1BasedSpectrumFeature feature) => feature?.QuantifiedChromatogramPeak),
                decSpectrumLoader,
                matchResultCandidatesModel.LoadMsSpectrumObservable(refLoader),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity),
                new GraphLabels("Measure vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush),
                Observable.Return((ISpectraExporter)null),
                Observable.Return((ISpectraExporter)null),
                Observable.Return((ISpectraExporter)null),
                matchResultCandidatesModel.GetCandidatesScorer(compoundSearchers)).AddTo(_disposables);

            // Raw vs Purified spectrum model
            var rawPurifiedSpectrumsModel = RawPurifiedSpectrumsModel.Create(
                selectedSpectrum,
                rawSpectrumLoader.Contramap((Ms1BasedSpectrumFeature feature) => feature?.QuantifiedChromatogramPeak),
                decSpectrumLoader,
                peak => peak.Mass,
                peak => peak.Intensity,
                Observable.Return(upperSpecBrush),
                Observable.Return(upperSpecBrush)).AddTo(_disposables);
        }

        public SpectrumFeaturePlotModel PeakPlotModel { get; }
        public RawDecSpectrumsModel RawDecSpectrumModel { get; }
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
