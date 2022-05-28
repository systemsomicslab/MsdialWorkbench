using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    class LcmsAnalysisModel : AnalysisModelBase {
        private readonly IDataProvider provider;

        public LcmsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            DataBaseStorage databases,
            DataBaseMapper mapper,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ParameterBase parameter,
            PeakFilterModel peakFilterModel)
            : base(analysisFile) {
            if (analysisFile is null) {
                throw new ArgumentNullException(nameof(analysisFile));
            }

            if (provider is null) {
                throw new ArgumentNullException(nameof(provider));
            }

            if (mapper is null) {
                throw new ArgumentNullException(nameof(mapper));
            }

            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            this.provider = provider;
            DataBaseMapper = mapper;
            Parameter = parameter;
            CompoundSearchers = ConvertToCompoundSearchers(databases);

            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Peaks, peakFilterModel, evaluator, useRtFilter: true);

            // Peak scatter plot
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelSource)
            {
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            Target.Select(
                t =>  $"File: {analysisFile.AnalysisFileName}" +
                    (t is null
                        ? string.Empty
                        : $" Spot ID: {t.MasterPeakID} Scan: {t.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5}"))
                .Subscribe(title => PlotModel.GraphTitle = title)
                .AddTo(Disposables);

            // Eic chart
            EicLoader = new EicLoader(this.provider, Parameter, ChromXType.RT, ChromXUnit.Min, Parameter.RetentionTimeBegin, Parameter.RetentionTimeEnd);
            EicModel = new EicModel(Target, EicLoader) {
                HorizontalTitle = PlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            ExperimentSpectrumModel = EicModel.EicSource
                .Select(source => new DisplayChromatogram(source))
                .Select(chromatogram => new ChromatogramsModel("Experiment chromatogram", chromatogram))
                .Select(chromatogram => new RangeSelectableChromatogramModel(chromatogram))
                .CombineLatest(
                    Target.Where(t => t != null),
                    (model, t) => new ExperimentSpectrumModel(model, AnalysisFile, provider, t.InnerModel, DataBaseMapper, Parameter))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            rawSpectrumLoader = new MsRawSpectrumLoader(this.provider, Parameter);
            var decSpectrumLoader = new MsDecSpectrumLoader(decLoader, Ms1Peaks);

            // Ms2 spectrum
            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               Parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Blue);
            Func<SpectrumComment, Color> zzz(ProjectBaseParameter projectParameter)
            {
                Color f(SpectrumComment comment) {
                    var commentString = comment.ToString();
                    if (projectParameter.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else if ((comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                        && projectParameter.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else {
                        return Colors.Red;
                    }
                }
                return f;
            };
            var lowerSpecBrush = new DelegateBrushMapper<SpectrumComment>(zzz(Parameter.ProjectParam), true);
            var spectraExporter = new NistSpectraExporter(Target.Select(t => t?.InnerModel), mapper, Parameter).AddTo(Disposables);
            Ms2SpectrumModel = new RawDecSpectrumsModel(
                Target,
                rawSpectrumLoader,
                decSpectrumLoader,
                new MsRefSpectrumLoader(mapper),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity),
                new GraphLabels("Measure vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush),
                Observable.Return(spectraExporter),
                Observable.Return(spectraExporter),
                Observable.Return((ISpectraExporter)null)).AddTo(Disposables);
            

            // Raw vs Purified spectrum model
            RawPurifiedSpectrumsModel = new RawPurifiedSpectrumsModel(
                Target,
                rawSpectrumLoader,
                decSpectrumLoader,
                peak => peak.Mass,
                peak => peak.Intensity) {
                GraphTitle = "Raw vs. Purified spectrum",
                HorizontalTitle = "m/z",
                VerticalTitle = "Absolute abundance",
                HorizontalProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            }.AddTo(Disposables);


            // SurveyScan
            var msdataType = Parameter.MSDataType;
            var surveyScanSpectrum = new SurveyScanSpectrum(Target, t =>
            {
                if (t is null) {
                    return Observable.Return(new List<SpectrumPeakWrapper>());
                }
                return Observable.FromAsync(provider.LoadMs1SpectrumsAsync)
                    .Select(spectrums =>
                        {
                            var spectra = DataAccess.GetCentroidMassSpectra(
                                spectrums[t.MS1RawSpectrumIdTop],
                                msdataType, 0, float.MinValue, float.MaxValue);
                            return spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                        });
            }).AddTo(Disposables);
            SurveyScanModel = new SurveyScanModel(
                surveyScanSpectrum,
                spec => spec.Mass,
                spec => spec.Intensity).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            // Peak table
            PeakTableModel = new LcmsAnalysisPeakTableModel(Ms1Peaks, Target, MassMin, MassMax, ChromMin, ChromMax).AddTo(Disposables);

            switch (Parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    Brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181));
                    break;
                case TargetOmics.Metabolomics:
                case TargetOmics.Proteomics:
                    Brush = new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true);
                    break;
            }

            CanSearchCompound = new[]
            {
                Target.Select(t => t is null || t.InnerModel is null),
                MsdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var rtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, RtTol, Target.Select(t => t?.ChromXValue ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MzTol, Target.Select(t => t?.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            Func<double, ChromatogramPeakFeatureModel> yyy(IReadOnlyList<ChromatogramPeakFeatureModel> ms1Peaks) {
                return id => ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id));
            }
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                Target,
                yyy(Ms1Peaks),
                Target.Select(t => t?.MasterPeakID ?? 0d),
                "Region focus by ID",
                (rtSpotFocus, peak => peak.ChromXValue ?? 0d),
                (mzSpotFocus, peak => peak.Mass)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus);

            CanSaveRawSpectra = Target.Select(t => t?.InnerModel != null)
                .ToReadOnlyReactivePropertySlim(initialValue: false)
                .AddTo(Disposables);
        }

        private static readonly double RtTol = 0.5;
        private static readonly double MzTol = 20;

        public DataBaseMapper DataBaseMapper { get; }
        public ParameterBase Parameter { get; }

        public IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        public EicLoader EicLoader { get; }

        public AnalysisPeakPlotModel PlotModel { get; }

        public EicModel EicModel { get; }
        public ReadOnlyReactivePropertySlim<ExperimentSpectrumModel> ExperimentSpectrumModel { get; }

        private readonly MsRawSpectrumLoader rawSpectrumLoader;

        public RawDecSpectrumsModel Ms2SpectrumModel { get; }
        public RawPurifiedSpectrumsModel RawPurifiedSpectrumsModel { get; }

        public SurveyScanModel SurveyScanModel { get; }

        public LcmsAnalysisPeakTableModel PeakTableModel { get; }

        public IBrushMapper<ChromatogramPeakFeatureModel> Brush { get; }

        public FocusNavigatorModel FocusNavigatorModel { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public double ChromMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.ChromXValue) ?? 0d;
        public double ChromMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.ChromXValue) ?? 0d;
        public double MassMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.Mass) ?? 0d;
        public double MassMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.Mass) ?? 0d;

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public CompoundSearchModel<ChromatogramPeakFeature> CreateCompoundSearchModel() {
            if (Target.Value?.InnerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new LcmsCompoundSearchModel<ChromatogramPeakFeature>(
                AnalysisFile,
                Target.Value.InnerModel,
                MsdecResult.Value,
                CompoundSearchers);
        }

        public void FragmentSearcher() {
            var features = this.Ms1Peaks;
            MsdialCore.Algorithm.FragmentSearcher.Search(features.Select(n => n.InnerModel).ToList(), this.decLoader, Parameter);

            foreach (var feature in features) {
                var featureStatus = feature.InnerModel.FeatureFilterStatus;
                if (featureStatus.IsFragmentExistFiltered) {
                    Console.WriteLine("A fragment is found by MassQL not in alignment !!!");
                }
            }

        }

        public void SaveSpectra(string filename) {
            using (var file = File.Open(filename, FileMode.Create)) {
                SpectraExport.SaveSpectraTable(
                    (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                    file,
                    Target.Value.InnerModel,
                    MsdecResult.Value,
                    provider.LoadMs1Spectrums(),
                    DataBaseMapper,
                    Parameter);
            }
        }

        public bool CanSaveSpectra() => Target.Value.InnerModel != null && MsdecResult.Value != null;

        public async Task SaveRawSpectra(string filename) {
            using (var file = File.Open(filename, FileMode.Create)) {
                var target = Target.Value;
                var spectrum = await rawSpectrumLoader.LoadSpectrumAsync(target, default).ConfigureAwait(false);
                SpectraExport.SaveSpectraTable(
                    (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                    file,
                    target.InnerModel,
                    new MSScanProperty() { Spectrum = spectrum },
                    provider.LoadMs1Spectrums(),
                    DataBaseMapper,
                    Parameter);
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanSaveRawSpectra { get; }

        public void GoToMsfinderMethod() {
            MsDialToExternalApps.SendToMsFinderProgram(
                this.AnalysisFile,
                Target.Value.InnerModel,
                MsdecResult.Value,
                this.provider.LoadMs1Spectrums(),
                DataBaseMapper,
                Parameter
                );
        }

        private List<CompoundSearcher> ConvertToCompoundSearchers(DataBaseStorage databases) {
            var metabolomicsSearchers = databases
                .MetabolomicsDataBases
                .SelectMany(db => db.Pairs)
                .Select(pair => new CompoundSearcher(
                    new AnnotationQueryWithoutIsotopeFactory(pair.SerializableAnnotator),
                    pair.SearchParameter,
                    pair.SerializableAnnotator));
            var lipidomicsSearchers = databases
                .EadLipidomicsDatabases
                .SelectMany(db => db.Pairs)
                .Select(pair => new CompoundSearcher(
                    new AnnotationQueryWithReferenceFactory(DataBaseMapper, pair.SerializableAnnotator, Parameter.PeakPickBaseParam),
                    pair.SearchParameter,
                    pair.SerializableAnnotator));
            return metabolomicsSearchers.Concat(lipidomicsSearchers).ToList();
        }
    }
}
