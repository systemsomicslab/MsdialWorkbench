using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Normalize;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsAlignmentModel : AlignmentModelBase
    {
        static LcmsAlignmentModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
        }

        public LcmsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            PeakFilterModel peakFilterModel,
            DataBaseMapper mapper,
            MsdialLcmsParameter parameter,
            ProjectBaseParameterModel projectBaseParameter,
            List<AnalysisFileBean> files,
            IMessageBroker messageBroker)
            : base(alignmentFileBean.FilePath) {
            if (databases is null) {
                throw new ArgumentNullException(nameof(databases));
            }

            if (projectBaseParameter is null) {
                throw new ArgumentNullException(nameof(projectBaseParameter));
            }

            AlignmentFile = alignmentFileBean;
            Parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            DataBaseMapper = mapper;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            CompoundSearchers = ConvertToCompoundSearchers(databases);

            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);

            var fileIdToClassNameAsObservable = projectBaseParameter.ObserveProperty(p => p.FileIdToClassName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var barItemLoaderDatas = new[]
            {
                new BarItemsLoaderData("Peak height", Observable.Return("Height"), fileIdToClassNameAsObservable.Select(id2class => new HeightBarItemsLoader(id2class)), Observable.Return(true)),
                new BarItemsLoaderData("Peak area above base line", Observable.Return("Area"), fileIdToClassNameAsObservable.Select(id2class => new AreaAboveBaseLineBarItemsLoader(id2class)), Observable.Return(true)),
                new BarItemsLoaderData("Peak area above zero", Observable.Return("Area"), fileIdToClassNameAsObservable.Select(id2class => new AreaAboveZeroBarItemsLoader(id2class)), Observable.Return(true)),
                new BarItemsLoaderData("Normalized peak height", Target.Select(t => t.IonAbundanceUnit.ToLabel()), fileIdToClassNameAsObservable.Select(id2class => new NormalizedHeightBarItemsLoader(id2class)), Observable.Return(true)),
                new BarItemsLoaderData("Normalized peak area above base line", Target.Select(t => t.IonAbundanceUnit.ToLabel()), fileIdToClassNameAsObservable.Select(id2class => new NormalizedAreaAboveBaseLineBarItemsLoader(id2class)), Observable.Return(true)),
                new BarItemsLoaderData("Normalized peak area above zero", Target.Select(t => t.IonAbundanceUnit.ToLabel()), fileIdToClassNameAsObservable.Select(id2class => new NormalizedAreaAboveZeroBarItemsLoader(id2class)), Observable.Return(true)),
            };
            var barItemsLoaderDataProperty = new ReactivePropertySlim<BarItemsLoaderData>(barItemLoaderDatas.First()).AddTo(Disposables);
            var barItemsLoaderProperty = barItemsLoaderDataProperty.Where(data => !(data is null)).Select(data => data.ObservableLoader).Switch().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop, barItemsLoaderProperty)));
           
            this.decLoader = new MSDecLoader(AlignmentFile.SpectraFilePath);
            MsdecResult = Target.Where(t => t != null)
                .Select(t => this.decLoader.LoadMSDecResult(t.MasterAlignmentID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            MassMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.MassCenter) ?? 0d;
            MassMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.MassCenter) ?? 0d;
            RtMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.TimesCenter) ?? 0d;
            RtMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.TimesCenter) ?? 0d;
            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Spots, peakFilterModel, evaluator, useRtFilter: true);

            // Peak scatter plot
            Brushes = new List<BrushMapData<AlignmentSpotPropertyModel>>
            {
                new BrushMapData<AlignmentSpotPropertyModel>(
                    new KeyBrushMapper<AlignmentSpotPropertyModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        spot => spot?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181)),
                    "Ontology"),
                new BrushMapData<AlignmentSpotPropertyModel>(
                    new DelegateBrushMapper<AlignmentSpotPropertyModel>(
                        spot => Color.FromArgb(
                            180,
                            (byte)(255 * spot.innerModel.RelativeAmplitudeValue),
                            (byte)(255 * (1 - Math.Abs(spot.innerModel.RelativeAmplitudeValue - 0.5))),
                            (byte)(255 - 255 * spot.innerModel.RelativeAmplitudeValue)),
                        enableCache: true),
                    "Amplitude"),
            };
            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    SelectedBrush = Brushes[0];
                    break;
                case TargetOmics.Metabolomics:
                case TargetOmics.Proteomics:
                    SelectedBrush = Brushes[1];
                    break;
            }
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource, SelectedBrush, Brushes)
            {
                GraphTitle = AlignmentFile.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
            }.AddTo(Disposables);

            // Ms2 spectrum
            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               Parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Blue);
            var lowerSpecBrush = new DelegateBrushMapper<SpectrumComment>(
                comment =>
                {
                    var commentString = comment.ToString();
                    var projectParameter = Parameter.ProjectParam;
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
                },
                true);
            Ms2SpectrumModel = MsSpectrumModel.Create(
                Target,
                new MsDecSpectrumLoader(decLoader, Ms1Spots),
                new MsRefSpectrumLoader(mapper),
                peak => peak.Mass,
                peak => peak.Intensity,
                "Representative vs. Reference",
                "m/z",
                "Abundance",
                nameof(SpectrumPeak.Mass),
                nameof(SpectrumPeak.Intensity),
                nameof(SpectrumPeak.Mass),
                nameof(SpectrumPeak.Intensity),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush)).AddTo(Disposables);

            // Class intensity bar chart
            var classBrush = projectBaseParameter
                .ObserveProperty(p => p.ClassnameToColorBytes)
                .Select(classToColor => new KeyBrushMapper<BarItem, string>(
                    classToColor.ToDictionary(
                        kvp => kvp.Key,
                        kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                    ),
                    item => item.Class,
                    Colors.Blue));
            BarChartModel = new BarChartModel(Target, barItemsLoaderDataProperty, barItemLoaderDatas, classBrush).AddTo(Disposables);

            // Class eic
            var classToColorAsObservable = projectBaseParameter
                .ObserveProperty(p => p.ClassnameToColorBytes)
                .Select(classToColor => classToColor.ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])));
            AlignmentEicModel = AlignmentEicModel.Create(
                Target,
                new AlignmentEicLoader(chromatogramSpotSerializer, alignmentFileBean.EicFilePath, projectBaseParameter.ObserveProperty(p => p.FileIdToClassName), classToColorAsObservable),
                files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            AlignmentSpotTableModel = new LcmsAlignmentSpotTableModel(Ms1Spots, Target, MassMin, MassMax, RtMin, RtMax, classBrush).AddTo(Disposables);

            CanSearchCompound = new[]
            {
                Target.Select(t => t?.innerModel is null),
                MsdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var rtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, RtTol, Target.Select(t => t?.TimesCenter ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MzTol, Target.Select(t => t?.MassCenter ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                Target,
                id => Ms1Spots.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                Target.Select(t => t?.MasterAlignmentID ?? 0d),
                "Region focus by ID",
                (rtSpotFocus, spot => spot.TimesCenter),
                (mzSpotFocus, spot => spot.MassCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private static readonly double RtTol = 0.5;
        private static readonly double MzTol = 20;

        public AlignmentFileBean AlignmentFile { get; }
        public ParameterBase Parameter { get; }
        public DataBaseMapper DataBaseMapper { get; }
        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        private readonly List<CompoundSearcher> CompoundSearchers;

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }
        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        private readonly MSDecLoader decLoader;
        private readonly List<AnalysisFileBean> _files;
        private readonly IMessageBroker _messageBroker;

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public FocusNavigatorModel FocusNavigatorModel { get; }

        public AlignmentPeakPlotModel PlotModel { get; }
        public MsSpectrumModel Ms2SpectrumModel { get; }
        public BarChartModel BarChartModel { get; }
        public AlignmentEicModel AlignmentEicModel { get; }

        public LcmsAlignmentSpotTableModel AlignmentSpotTableModel { get; private set; }
        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public BrushMapData<AlignmentSpotPropertyModel> SelectedBrush {
            get => selectedBrush;
            set => SetProperty(ref selectedBrush, value);
        }
        private BrushMapData<AlignmentSpotPropertyModel> selectedBrush;

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public CompoundSearchModel<AlignmentSpotProperty> CreateCompoundSearchModel() {
            if (Target.Value?.innerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new LcmsCompoundSearchModel<AlignmentSpotProperty>(
                AlignmentFile,
                Target.Value.innerModel,
                MsdecResult.Value,
                CompoundSearchers);
        }

        public void SaveSpectra(string filename) {
            using (var file = File.Open(filename, FileMode.Create)) {
                SpectraExport.SaveSpectraTable(
                    (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                    file,
                    Target.Value.innerModel,
                    MsdecResult.Value,
                    DataBaseMapper,
                    Parameter);
            }
        }

        public bool CanSaveSpectra() => Target.Value.innerModel != null && MsdecResult.Value != null;

        public void FragmentSearcher() {
            var features = this.Ms1Spots;
            MsdialCore.Algorithm.FragmentSearcher.Search(features.Select(n => n.innerModel).ToList(), this.decLoader, Parameter);

            foreach (var feature in features) {
                var featureStatus = feature.innerModel.FeatureFilterStatus;
                if (featureStatus.IsFragmentExistFiltered) {
                    Console.WriteLine("A fragment is found in alignment !!!");
                }
            }

        }

        public void GoToMsfinderMethod() {
            MsDialToExternalApps.SendToMsFinderProgram(
                this.AlignmentFile,
                Target.Value.innerModel,
                MsdecResult.Value,
                DataBaseMapper,
                Parameter);
        }

        public NormalizationSetModel Normalize() {
            return new NormalizationSetModel(Container, _files, DataBaseMapper, MatchResultEvaluator, Parameter, _messageBroker);
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
