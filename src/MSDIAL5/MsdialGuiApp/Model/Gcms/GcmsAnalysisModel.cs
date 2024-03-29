﻿using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Algorithm;
using CompMs.MsdialGcMsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
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
        private static readonly double RT_TOL = .5, MZ_TOL = 20d;

        private bool _disposedValue;
        private readonly ProjectBaseParameter _projectParameter;
        private readonly PeakPickBaseParameter _peakPickParameter;
        private CompositeDisposable? _disposables;
        private readonly Ms1BasedSpectrumFeatureCollection _spectrumFeatures;
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _peaks;
        private readonly AnalysisFileBeanModel _file;
        private readonly CalculateMatchScore _calculateMatchScore;
        private readonly IMessageBroker _broker;
        private readonly IWholeChromatogramLoader _ticLoader, _bpcLoader;
        private readonly IWholeChromatogramLoader<(double, double)> _eicLoader;

        public GcmsAnalysisModel(AnalysisFileBeanModel file, IDataProviderFactory<AnalysisFileBeanModel> providerFactory, MsdialGcmsParameter parameter, DataBaseMapper dbMapper, DataBaseStorage dbStorage, FilePropertiesModel projectBaseParameterModel, PeakFilterModel peakFilterModel, CalculateMatchScore calculateMatchScore, IMessageBroker broker) {
            var projectParameter = parameter.ProjectParam;
            var peakPickParameter = parameter.PeakPickBaseParam;
            var chromDecParameter = parameter.ChromDecBaseParam;
            _projectParameter = projectParameter;
            _peakPickParameter = peakPickParameter;
            _disposables = new CompositeDisposable();
            _spectrumFeatures = file.LoadMs1BasedSpectrumFeatureCollection().AddTo(_disposables);
            _peaks =  file.LoadChromatogramPeakFeatureModels();
            _file = file;
            _calculateMatchScore = calculateMatchScore;
            _broker = broker;
            UndoManager = new UndoManager().AddTo(_disposables);

            var selectedSpectrum = _spectrumFeatures.SelectedSpectrum;
            var evaluator = FacadeMatchResultEvaluator.FromDataBases(dbStorage);

            var filterEnabled = FilterEnableStatus.All & ~FilterEnableStatus.Dt & ~FilterEnableStatus.Protein;
            var filterRegistrationManager = new SpectrumFeatureFilterRegistrationManager(_spectrumFeatures.Items, new SpectrumFeatureFiltering()).AddTo(_disposables);
            filterRegistrationManager.AttachFilter(_spectrumFeatures.Items, peakFilterModel, evaluator.Contramap<Ms1BasedSpectrumFeature, MsScanMatchResult>(spectrumFeature => spectrumFeature.MatchResults.Representative), status: filterEnabled);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            var label = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).Select(l => l ?? string.Empty).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(_disposables);

            var brushMapDataSelector = BrushMapDataSelectorFactory.CreatePeakFeatureBrushes(projectParameter.TargetOmics);
            PeakPlotModel = new SpectrumFeaturePlotModel(_spectrumFeatures, _peaks, brushMapDataSelector, label).AddTo(_disposables);

            IDataProvider provider = providerFactory.Create(file);

            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), parameter.IonMode, file.File.AcquisitionType);
            ChromatogramRange chromatogramRange = new ChromatogramRange(parameter.RetentionTimeBegin, parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            _ticLoader = new TicLoader(rawSpectra, chromatogramRange, peakPickParameter);
            _bpcLoader = new BpcLoader(rawSpectra, chromatogramRange, peakPickParameter);
            var eicLoader2 = Loader.EicLoader.BuildForAllRange(file.File, provider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            _eicLoader = eicLoader2;

            // Eic chart
            var eicLoader = new QuantMassEicLoader(file.File, provider, peakPickParameter, projectParameter.IonMode, ChromXType.RT, ChromXUnit.Min, peakPickParameter.RetentionTimeBegin, peakPickParameter.RetentionTimeEnd, isConstantRange: true);
            var tableEicLoader = new QuantMassEicLoader(file.File, provider, peakPickParameter, projectParameter.IonMode, ChromXType.RT, ChromXUnit.Min, peakPickParameter.RetentionTimeBegin, peakPickParameter.RetentionTimeEnd, isConstantRange: false);
            EicLoader = tableEicLoader;
            EicModel = EicModel.CreateBuilder(string.Empty, string.Empty, string.Empty)
                .Append(selectedSpectrum, eicLoader)
                .Append(PeakPlotModel.SelectedChromatogramPeak, eicLoader2)
                .Build().AddTo(_disposables);
            EicModel.VerticalTitle = "Abundance";
            PeakPlotModel.HorizontalLabel.Subscribe(label => EicModel.HorizontalTitle = label).AddTo(_disposables);

            var matchResultCandidatesModel = new MatchResultCandidatesModel(selectedSpectrum.Select(t => t?.MatchResults)).AddTo(_disposables);
            MatchResultCandidatesModel = matchResultCandidatesModel;
            var rawSpectrumLoader = new MsRawSpectrumLoader(provider, projectParameter.MSDataType, chromDecParameter);
            var decLoader = file.MSDecLoader;
            var decSpectrumLoader = new MsDecSpectrumLoader(decLoader, _spectrumFeatures.Items);
            var refLoader = (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference>(dbMapper);
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);

            var rawSpectrumLoader_ = rawSpectrumLoader.Contramap((Ms1BasedSpectrumFeature feature) => feature?.QuantifiedChromatogramPeak);
            var rawGraphLabels = new GraphLabels("Raw EI spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem measuredHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Blue);
            ObservableMsSpectrum rawObservableMsSpectrum = ObservableMsSpectrum.Create(selectedSpectrum, rawSpectrumLoader_, null).AddTo(_disposables);
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(rawObservableMsSpectrum, rawObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), rawObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, rawGraphLabels).AddTo(_disposables);

            var decGraphLabels = new GraphLabels("Deconvoluted EI spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum decObservableMsSpectrum = ObservableMsSpectrum.Create(selectedSpectrum, decSpectrumLoader, null).AddTo(_disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decObservableMsSpectrum, decObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), decObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, decGraphLabels).AddTo(_disposables);

            var refGraphLabels = new GraphLabels("Reference EI spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem referenceSpectrumHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Red);
            var exporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.SelectedCandidate.Select(dbMapper.MoleculeMsRefer)).AddTo(_disposables);
            ObservableMsSpectrum refObservableMsSpectrum = ObservableMsSpectrum.Create(MatchResultCandidatesModel.SelectedCandidate, refLoader, exporter).AddTo(_disposables);
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refObservableMsSpectrum, refObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), refObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), referenceSpectrumHueItem, refGraphLabels).AddTo(_disposables);

            var compoundSearchers = CompoundSearcherCollection.BuildSearchers(dbStorage, dbMapper);
            var ms2ScanMatching = MatchResultCandidatesModel.GetCandidatesScorer(compoundSearchers).Publish();
            RawDecSpectrumModel = new RawDecSpectrumsModel(rawSpectrumModel, decSpectrumModel, referenceSpectrumModel, ms2ScanMatching).AddTo(_disposables);
            _disposables.Add(ms2ScanMatching.Connect());

            // Raw vs Purified spectrum model
            RawPurifiedSpectrumsModel = new RawPurifiedSpectrumsModel(rawSpectrumModel, decSpectrumModel).AddTo(_disposables);

            // EI chromatogram
            var numberOfChromatograms = new ReactivePropertySlim<int>(10).AddTo(_disposables);
            NumberOfEIChromatograms = numberOfChromatograms;
            var spectra = new RawSpectra(provider, projectParameter.IonMode, file.AcquisitionType);
            var rawChromatograms = selectedSpectrum.SkipNull()
                .SelectSwitch(feature => rawSpectrumLoader_.LoadSpectrumAsObservable(feature).CombineLatest(numberOfChromatograms, (System.Collections.Generic.List<SpectrumPeak> spectrum, int number) => (feature, spectrum: spectrum.OrderByDescending(peak_ => peak_.Intensity).Take(number).OrderBy(n => n.Mass))))
                .Select(pair => spectra.GetMs1ExtractedChromatograms_temp2(pair.spectrum.Select(s => s.Mass), peakPickParameter.CentroidMs1Tolerance, new ChromatogramRange(pair.feature.QuantifiedChromatogramPeak.PeakFeature, ChromXType.RT, ChromXUnit.Min)))
                .Select(chromatograms => chromatograms.Select(chromatogram => chromatogram.ChromatogramSmoothing(CompMs.Common.Enum.SmoothingMethod.LinearWeightedMovingAverage, peakPickParameter.SmoothingLevel)))
                .Select(chromatograms => new ChromatogramsModel(
                    "EI chromatograms",
                    chromatograms.Zip(ChartBrushes.GetSolidColorPenList(1d, DashStyles.Dash), (chromatogram, pen) => new DisplayChromatogram(chromatogram.AsPeakArray().Select(peak_ => peak_.ConvertToChromatogramPeak(ChromXType.RT, ChromXUnit.Min)).ToList(), linePen: pen, title: chromatogram.ExtractedMz.ToString("F2"))).ToList(),
                    "EI chromatograms",
                    "Retention time [min]",
                    "Abundance"));
            var rawChromatogram = new SelectableChromatogram(rawChromatograms, new ReactivePropertySlim<bool>(false), Observable.Return(true).ToReadOnlyReactivePropertySlim()).AddTo(_disposables);
            var deconvolutedChromatograms = selectedSpectrum.SkipNull()
                .Select(feature => decLoader.LoadMSDecResult(_spectrumFeatures.Items.IndexOf(feature)))
                .CombineLatest(numberOfChromatograms, (result, number) => result.DecChromPeaks(number))
                .Select(chromatograms => new ChromatogramsModel(
                    "EI chromatograms",
                    chromatograms.Zip(ChartBrushes.GetSolidColorPenList(1d, DashStyles.Solid), (chromatogram, pen) => new DisplayChromatogram(chromatogram, linePen: pen, title: chromatogram.FirstOrDefault()?.Mass.ToString("F2") ?? "NA")).ToList(),
                    "EI chromatograms",
                    "Retention time [min]",
                    "Abundance"));
            var deconvolutedChromatogram = new SelectableChromatogram(deconvolutedChromatograms, new ReactivePropertySlim<bool>(true), Observable.Return(true).ToReadOnlyReactivePropertySlim()).AddTo(_disposables);
            EiChromatogramsModel = new EiChromatogramsModel(rawChromatogram, deconvolutedChromatogram, broker).AddTo(_disposables);

            // SurveyScan
            var surveyScanSpectrum = SurveyScanSpectrum.Create(selectedSpectrum, t =>
            {
                if (t is null) {
                    return Observable.Return(new List<SpectrumPeakWrapper>());
                }
                return Observable.FromAsync(provider.LoadMsSpectrumsAsync)
                    .Select(spectrums =>
                        {
                            var spectrum = DataAccess.GetCentroidMassSpectra(spectrums[t.QuantifiedChromatogramPeak.MS1RawSpectrumIdTop], projectParameter.MSDataType, 0, float.MinValue, float.MaxValue);
                            return spectrum.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                        });
            }).AddTo(_disposables);
            SurveyScanModel = new SurveyScanModel(surveyScanSpectrum, spec => spec.Mass, spec => spec.Intensity).AddTo(_disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            var peakInformationModel = new PeakInformationMs1BasedModel(selectedSpectrum).AddTo(_disposables);
            peakInformationModel.Add(t => new RtPoint(t?.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value ?? 0d, dbMapper.MoleculeMsRefer(t?.MatchResults.Representative)?.ChromXs.RT.Value));
            if (parameter.RetentionType == RetentionType.RI) {
                peakInformationModel.Add(t => new RiPoint(t?.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value ?? 0d, dbMapper.MoleculeMsRefer(t?.MatchResults.Representative)?.ChromXs.RI.Value));
            }
            peakInformationModel.Add(t => new QuantMassPoint(t?.QuantifiedChromatogramPeak.PeakFeature.Mass ?? 0d, dbMapper.MoleculeMsRefer(t?.MatchResults.Representative)?.PrecursorMz));
            peakInformationModel.Add(
                t => new HeightAmount(t?.QuantifiedChromatogramPeak.PeakFeature.PeakHeightTop ?? 0d),
                t => new AreaAmount(t?.QuantifiedChromatogramPeak.PeakFeature.PeakAreaAboveZero ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(selectedSpectrum.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.MatchResults.Representative)).Publish().RefCount(), dbMapper).AddTo(_disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d));
            if (parameter.RetentionType == RetentionType.RI) {
                compoundDetailModel.Add(r_ => new RiSimilarity(r_?.RiSimilarity ?? 0d));
            }
            compoundDetailModel.Add(r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
            var moleculeStructureModel = new MoleculeStructureModel().AddTo(_disposables);
            MoleculeStructureModel = moleculeStructureModel;
            selectedSpectrum.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.GetCurrentSpectrumFeature().AnnotatedMSDecResult.Molecule)).AddTo(_disposables);

            PeakTableModel = new GcmsAnalysisPeakTableModel(_spectrumFeatures.Items, selectedSpectrum, PeakSpotNavigatorModel, UndoManager);

            var rtSpotFocus = new ChromSpotFocus(PeakPlotModel.HorizontalAxis, RT_TOL, selectedSpectrum.Select(s => s?.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(_disposables);
            var mzSpotFocus = new ChromSpotFocus(PeakPlotModel.VerticalAxis, MZ_TOL, selectedSpectrum.Select(s => s?.QuantifiedChromatogramPeak.PeakFeature.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(_disposables);
            var idSpotFocus = new IdSpotFocus<Ms1BasedSpectrumFeature>(
                selectedSpectrum,
                id => _spectrumFeatures.Items.Argmin(s => Math.Abs(s.Scan.ScanID - id)),
                selectedSpectrum.Select(s => s?.Scan.ScanID ?? 0d),
                "ID",
                (rtSpotFocus, s => s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value),
                (mzSpotFocus, s => s.QuantifiedChromatogramPeak.PeakFeature.Mass)).AddTo(_disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus);
        }

        public SpectrumFeaturePlotModel PeakPlotModel { get; }
        public RawDecSpectrumsModel RawDecSpectrumModel { get; }
        public RawPurifiedSpectrumsModel RawPurifiedSpectrumsModel { get; }
        public EiChromatogramsModel EiChromatogramsModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }

        public ReactivePropertySlim<int> NumberOfEIChromatograms { get; }
        public SurveyScanModel SurveyScanModel { get; }
        public IChromatogramLoader<Ms1BasedSpectrumFeature?> EicLoader { get; }
        public EicModel EicModel { get; }
        public PeakInformationMs1BasedModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public GcmsAnalysisPeakTableModel PeakTableModel { get; }
        public UndoManager UndoManager { get; }

        public IObservable<bool> CanSetUnknown => _spectrumFeatures.SelectedSpectrum.Select(t => !(t is null));

        public FocusNavigatorModel FocusNavigatorModel { get; }

        public void SetUnknown() => _spectrumFeatures.SelectedSpectrum.Value?.SetUnknown(UndoManager);

        public LoadChromatogramsUsecase LoadChromatogramsUsecase() {
            return new LoadChromatogramsUsecase(_ticLoader, _bpcLoader, _eicLoader, _peaks, _peakPickParameter);
        }

        public CompoundSearchModel<Ms1BasedSpectrumFeature>? CreateCompoundSearchModel() {
            if (_spectrumFeatures.SelectedSpectrum.Value is not Ms1BasedSpectrumFeature spectrumFeature) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }
            var plotService = new PlotComparedMsSpectrumUsecase(spectrumFeature.Scan);
            var compoundSearch = new CompoundSearchModel<Ms1BasedSpectrumFeature>(
                _file,
                spectrumFeature,
                new GcmsAnalysisCompoundSearchUsecase(_calculateMatchScore),
                plotService,
                new SetAnnotationUsecase(spectrumFeature.Molecule, spectrumFeature.MatchResults, UndoManager));
            compoundSearch.Disposables.Add(plotService);
            return compoundSearch;
        }

        // IAnalysisModel interface
        Task IAnalysisModel.SaveAsync(CancellationToken token) {
            return _spectrumFeatures.SaveAsync(_file);
        }

        // IResultModel interface
        void IResultModel.InvokeMsfinder() {
            if (!(_spectrumFeatures.SelectedSpectrum.Value is Ms1BasedSpectrumFeature spectrumFeature)) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgramForGcms(_file, spectrumFeature.GetCurrentSpectrumFeature(), _projectParameter);
        }

        void IResultModel.SearchFragment() {
            throw new NotImplementedException();
        }

        void IResultModel.ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter) {
            throw new NotImplementedException();
        }

        void IResultModel.InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter) {
            throw new NotImplementedException();
        }

        void IResultModel.InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }

        // IDisposable interface
        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    _disposables?.Dispose();
                }

                _disposables?.Clear();
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
