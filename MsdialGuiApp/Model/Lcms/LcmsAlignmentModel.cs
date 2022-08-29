using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
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
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;
        private static readonly double RT_TOL = 0.5;
        private static readonly double MZ_TOL = 20;

        static LcmsAlignmentModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
        }

        private readonly MSDecLoader _decLoader;
        private readonly AlignmentFileBean _alignmentFile;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly List<AnalysisFileBean> _files;
        private readonly IReadOnlyList<CompoundSearcher> _compoundSearchers;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult> _msdecResult;

        public LcmsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            PeakFilterModel peakFilterModel,
            DataBaseMapper mapper,
            MsdialLcmsParameter parameter,
            ProjectBaseParameterModel projectBaseParameter,
            List<AnalysisFileBean> files,
            IObserver<ProteinResultContainerModel> proteinResultContainerModelObserver,
            IMessageBroker messageBroker)
            : base(alignmentFileBean, alignmentFileBean.FilePath) {
            if (databases is null) {
                throw new ArgumentNullException(nameof(databases));
            }

            if (projectBaseParameter is null) {
                throw new ArgumentNullException(nameof(projectBaseParameter));
            }


            _alignmentFile = alignmentFileBean;
            Parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _dataBaseMapper = mapper;
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper, parameter.PeakPickBaseParam).Items;

            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);

            NormalizationSetModel = new NormalizationSetModel(Container, files, mapper, evaluator, parameter, messageBroker).AddTo(Disposables);

            var fileIdToClassNameAsObservable = projectBaseParameter.ObserveProperty(p => p.FileIdToClassName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var heightLoader = new BarItemsLoaderData("Peak height", Observable.Return("Height"), fileIdToClassNameAsObservable.Select(id2class => new HeightBarItemsLoader(id2class)), Observable.Return(true));
            var areaBaselineLoader = new BarItemsLoaderData("Peak area above base line", Observable.Return("Area"), fileIdToClassNameAsObservable.Select(id2class => new AreaAboveBaseLineBarItemsLoader(id2class)), Observable.Return(true));
            var areaZeroLoader = new BarItemsLoaderData("Peak area above zero", Observable.Return("Area"), fileIdToClassNameAsObservable.Select(id2class => new AreaAboveZeroBarItemsLoader(id2class)), Observable.Return(true));
            var normalizedHeightLoader = new BarItemsLoaderData("Normalized peak height", Target.Select(t => t.IonAbundanceUnit.ToLabel()), fileIdToClassNameAsObservable.Select(id2class => new NormalizedHeightBarItemsLoader(id2class)), NormalizationSetModel.IsNormalized);
            var normalizedAreaBaselineLoader = new BarItemsLoaderData("Normalized peak area above base line", Target.Select(t => t.IonAbundanceUnit.ToLabel()), fileIdToClassNameAsObservable.Select(id2class => new NormalizedAreaAboveBaseLineBarItemsLoader(id2class)), NormalizationSetModel.IsNormalized);
            var normalizedAreaZeroLoader = new BarItemsLoaderData("Normalized peak area above zero", Target.Select(t => t.IonAbundanceUnit.ToLabel()), fileIdToClassNameAsObservable.Select(id2class => new NormalizedAreaAboveZeroBarItemsLoader(id2class)), NormalizationSetModel.IsNormalized);
            var barItemLoaderDatas = new[]
            {
                heightLoader, areaBaselineLoader, areaZeroLoader,
                normalizedHeightLoader, normalizedAreaBaselineLoader, normalizedAreaZeroLoader,
            };
            var barItemsLoaderDataProperty = NormalizationSetModel.Normalized.Select(_ => normalizedHeightLoader).ToReactiveProperty(barItemLoaderDatas.First()).AddTo(Disposables);
            var barItemsLoaderProperty = barItemsLoaderDataProperty.Where(data => !(data is null)).Select(data => data.ObservableLoader).Switch().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop, barItemsLoaderProperty)));

            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                var proteinResultContainer = MsdialProteomicsSerializer.LoadProteinResultContainer(alignmentFileBean.ProteinAssembledResultFilePath);
                var proteinResultContainerModel = new ProteinResultContainerModel(proteinResultContainer, Ms1Spots, Target);
                proteinResultContainerModelObserver.OnNext(proteinResultContainerModel);
            }

            _decLoader = new MSDecLoader(_alignmentFile.SpectraFilePath);
            _msdecResult = Target.Where(t => t != null)
                .Select(t => _decLoader.LoadMSDecResult(t.MasterAlignmentID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Spots, peakFilterModel, evaluator, status: ~FilterEnableStatus.Dt).AddTo(Disposables);

            // Peak scatter plot
            var ontologyBrush = new BrushMapData<AlignmentSpotPropertyModel>(
                    new KeyBrushMapper<AlignmentSpotPropertyModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        spot => spot?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181)),
                    "Ontology");
            var intensityBrush = new BrushMapData<AlignmentSpotPropertyModel>(
                    new DelegateBrushMapper<AlignmentSpotPropertyModel>(
                        spot => Color.FromArgb(
                            180,
                            (byte)(255 * spot.innerModel.RelativeAmplitudeValue),
                            (byte)(255 * (1 - Math.Abs(spot.innerModel.RelativeAmplitudeValue - 0.5))),
                            (byte)(255 - 255 * spot.innerModel.RelativeAmplitudeValue)),
                        enableCache: true),
                    "Amplitude");
            var brushes = new List<BrushMapData<AlignmentSpotPropertyModel>>
            {
                ontologyBrush, intensityBrush,
            };
            BrushMapData<AlignmentSpotPropertyModel> selectedBrush = null;
            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    selectedBrush = ontologyBrush;
                    break;
                case TargetOmics.Metabolomics:
                case TargetOmics.Proteomics:
                default:
                    selectedBrush = intensityBrush;
                    break;
            }
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource, selectedBrush, brushes)
            {
                GraphTitle = _alignmentFile.FileName,
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
                new MsDecSpectrumLoader(_decLoader, Ms1Spots),
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
                new AlignmentEicLoader(CHROMATOGRAM_SPOT_SERIALIZER, alignmentFileBean.EicFilePath, projectBaseParameter.ObserveProperty(p => p.FileIdToClassName), classToColorAsObservable).AddTo(Disposables),
                files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            var massMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.MassCenter) ?? 0d;
            var massMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.MassCenter) ?? 0d;
            var rtMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.TimesCenter) ?? 0d;
            var rtMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.TimesCenter) ?? 0d;
            AlignmentSpotTableModel = new LcmsAlignmentSpotTableModel(Ms1Spots, Target, massMin, massMax, rtMin, rtMax, classBrush).AddTo(Disposables);

            CanSearchCompound = new[]
            {
                Target.Select(t => t?.innerModel is null),
                _msdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var rtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, RT_TOL, Target.Select(t => t?.TimesCenter ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MZ_TOL, Target.Select(t => t?.MassCenter ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                Target,
                id => Ms1Spots.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                Target.Select(t => t?.MasterAlignmentID ?? 0d),
                "Region focus by ID",
                (rtSpotFocus, spot => spot.TimesCenter),
                (mzSpotFocus, spot => spot.MassCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus);

            var peakInformationModel = new PeakInformationAlignmentModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new RtPoint(t?.innerModel.TimesCenter.RT.Value ?? 0d),
                t => new MzPoint(t?.MassCenter ?? 0d));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(Target.Select(t => t?.ScanMatchResult), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
        }

        public ParameterBase Parameter { get; }

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public AlignmentPeakPlotModel PlotModel { get; }
        public MsSpectrumModel Ms2SpectrumModel { get; }
        public BarChartModel BarChartModel { get; }
        public AlignmentEicModel AlignmentEicModel { get; }
        public LcmsAlignmentSpotTableModel AlignmentSpotTableModel { get; private set; }
        public NormalizationSetModel NormalizationSetModel { get; }

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }
        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public ProteinResultContainerModel ProteinResultContainerModel { get; }

        public CompoundSearchModel<AlignmentSpotProperty> CreateCompoundSearchModel() {
            return new LcmsCompoundSearchModel<AlignmentSpotProperty>(
                _files[Target.Value.RepresentativeFileID],
                Target.Value.innerModel,
                _msdecResult.Value,
                _compoundSearchers);
        }

        public void SaveSpectra(string filename) {
            using (var file = File.Open(filename, FileMode.Create)) {
                SpectraExport.SaveSpectraTable(
                    (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                    file,
                    Target.Value.innerModel,
                    _msdecResult.Value,
                    _dataBaseMapper,
                    Parameter);
            }
        }

        public bool CanSaveSpectra() => Target.Value.innerModel != null && _msdecResult.Value != null;

        public void FragmentSearcher() {
            var features = Ms1Spots;
            MsdialCore.Algorithm.FragmentSearcher.Search(features.Select(n => n.innerModel).ToList(), this._decLoader, Parameter);

            foreach (var feature in features) {
                var featureStatus = feature.innerModel.FeatureFilterStatus;
                if (featureStatus.IsFragmentExistFiltered) {
                    Console.WriteLine("A fragment is found in alignment !!!");
                }
            }

        }

        public void GoToMsfinderMethod() {
            MsDialToExternalApps.SendToMsFinderProgram(
                _alignmentFile,
                Target.Value.innerModel,
                _msdecResult.Value,
                _dataBaseMapper,
                Parameter);
        }
    }
}
