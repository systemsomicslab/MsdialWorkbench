using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;
using CompMs.App.Msdial.Model.Loader;

namespace CompMs.App.Msdial.Model.Lcms
{
    class LcmsAlignmentModel : AlignmentModelBase
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
            IObservable<ParameterBase> parameterAsObservable,
            IObservable<IBarItemsLoader> barItemsLoader,
            List<AnalysisFileBean> files)
            : base(alignmentFileBean.FilePath) {
            if (databases is null) {
                throw new ArgumentNullException(nameof(databases));
            }

            if (barItemsLoader is null) {
                throw new ArgumentNullException(nameof(barItemsLoader));
            }

            AlignmentFile = alignmentFileBean;
            Parameter = parameter;
            ParameterAsObservable = parameterAsObservable ?? throw new ArgumentNullException(nameof(parameterAsObservable));
            DataBaseMapper = mapper;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            CompoundSearchers = ConvertToCompoundSearchers(databases);
            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop, barItemsLoader)));
           
            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);
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
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel);
            PlotModel = new Chart.AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource)
            {
                GraphTitle = AlignmentFile.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
            };

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
                new MsDecSpectrumLoader(this.decLoader, Ms1Spots),
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
            BarChartModel = BarChartModel.Create(Target, barItemsLoader);
            BarChartModel.Elements.HorizontalTitle = "Class";
            BarChartModel.Elements.VerticalTitle = "Height";
            BarChartModel.Elements.HorizontalProperty = nameof(BarItem.Class);
            BarChartModel.Elements.VerticalProperty = nameof(BarItem.Height);

            // Class eic
            var classToColorAsObservable = parameterAsObservable
                .Select(p => p.ClassnameToColorBytes.ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])));
            AlignmentEicModel = AlignmentEicModel.Create(
                Target,
                new AlignmentEicLoader(chromatogramSpotSerializer, alignmentFileBean.EicFilePath, parameterAsObservable.Select(p => p.FileID_ClassName), classToColorAsObservable),
                files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            AlignmentSpotTableModel = new LcmsAlignmentSpotTableModel(Ms1Spots, Target, MassMin, MassMax, RtMin, RtMax).AddTo(Disposables);

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
                    SelectedBrush = Brushes[0].Mapper;
                    break;
                case TargetOmics.Metabolomics:
                case TargetOmics.Proteomics:
                    SelectedBrush = Brushes[1].Mapper;
                    break;
            }

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
        public IObservable<ParameterBase> ParameterAsObservable { get; }
        public DataBaseMapper DataBaseMapper { get; }
        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        private readonly List<CompoundSearcher> CompoundSearchers;

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }
        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        protected readonly MSDecLoader decLoader;
        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public FocusNavigatorModel FocusNavigatorModel { get; }

        public Chart.AlignmentPeakPlotModel PlotModel { get; }
        public MsSpectrumModel Ms2SpectrumModel { get; }
        public BarChartModel BarChartModel { get; }
        public AlignmentEicModel AlignmentEicModel { get; }

        public LcmsAlignmentSpotTableModel AlignmentSpotTableModel { get; private set; }
        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public IBrushMapper<AlignmentSpotPropertyModel> SelectedBrush {
            get => selectedBrush;
            set => SetProperty(ref selectedBrush, value);
        }
        private IBrushMapper<AlignmentSpotPropertyModel> selectedBrush;

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

            //foreach (var feature in features) {
            //    var featureStatus = feature.innerModel.FeatureFilterStatus;
            //    if (featureStatus.IsFragmentExistFiltered) {
            //        Console.WriteLine("A fragment is found in alignment !!!");
            //    }
            //}

        }

        public void GoToMsfinderMethod() {
            MsDialToExternalApps.SendToMsFinderProgram(
                this.AlignmentFile,
                Target.Value.innerModel,
                MsdecResult.Value,
                DataBaseMapper,
                Parameter);
        }

        public void Normalize() {
            
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
