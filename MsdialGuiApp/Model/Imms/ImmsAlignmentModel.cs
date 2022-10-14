using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsAlignmentModel : AlignmentModelBase
    {
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        private readonly AlignmentFileBean _alignmentFile;
        private readonly List<AnalysisFileBean> _files;
        private readonly ParameterBase _parameter;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly IReadOnlyList<CompoundSearcher> _compoundSearchers;

        public ImmsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            DataBaseMapper mapper,
            PeakFilterModel peakFilterModel,
            ParameterBase parameter,
            List<AnalysisFileBean> files)
            : base(alignmentFileBean, alignmentFileBean.FilePath) {

            _alignmentFile = alignmentFileBean;
            _parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _dataBaseMapper = mapper;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper, parameter.PeakPickBaseParam).Items;

            var BarItemsLoader = new HeightBarItemsLoader(parameter.FileID_ClassName);
            var observableBarItemsLoader = Observable.Return(BarItemsLoader);
            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));

            InternalStandardSetModel = new InternalStandardSetModel(Ms1Spots, TargetMsMethod.Imms);

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

            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);

            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Spots, peakFilterModel, evaluator, status: ~FilterEnableStatus.Rt).AddTo(Disposables);

            var fileName = alignmentFileBean.FileName;
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel);
            PlotModel = new AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource, SelectedBrush, Brushes)
            {
                GraphTitle = fileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Mobility [1/k0]",
                VerticalTitle = "m/z",
            }.AddTo(Disposables);

            var loader = new MSDecLoader(alignmentFileBean.SpectraFilePath);
            var decLoader = new MsDecSpectrumLoader(loader, Ms1Spots);
            var refLoader = new MsRefSpectrumLoader(mapper);
            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               _parameter.ProjectParam.SpectrumCommentToColorBytes
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
                    var projectParameter = _parameter.ProjectParam;
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
                Target, decLoader, refLoader,
                peak => peak.Mass,
                peak => peak.Intensity,
                "Representation vs. Reference",
                "m/z",
                "Abundance",
                nameof(SpectrumPeak.Mass),
                nameof(SpectrumPeak.Intensity),
                nameof(SpectrumPeak.Mass),
                nameof(SpectrumPeak.Intensity),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush)).AddTo(Disposables);

            var classBrush = new KeyBrushMapper<BarItem, string>(
                _parameter.ProjectParam.ClassnameToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.Class,
                Colors.Blue);
            var barItemsLoaderData = new BarItemsLoaderData("Loader", "Intensity", observableBarItemsLoader, Observable.Return(true));
            var barItemsLoaderDataProperty = new ReactiveProperty<BarItemsLoaderData>(barItemsLoaderData).AddTo(Disposables);
            BarChartModel = new BarChartModel(Target, barItemsLoaderDataProperty, new[] { barItemsLoaderData, }, Observable.Return(classBrush)).AddTo(Disposables);

            var eicFile = alignmentFileBean.EicFilePath;
            var classToColor = parameter.ClassnameToColorBytes
                .ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
            var eicLoader = new AlignmentEicLoader(CHROMATOGRAM_SPOT_SERIALIZER, eicFile, Observable.Return(parameter.FileID_ClassName), Observable.Return(classToColor)).AddTo(Disposables);
            AlignmentEicModel = AlignmentEicModel.Create(
                Target, eicLoader, files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Mobility [1/k0]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            AlignmentSpotTableModel = new ImmsAlignmentSpotTableModel(Ms1Spots, Target, Observable.Return(classBrush), observableBarItemsLoader).AddTo(Disposables);

            MsdecResult = Target.Where(t => t != null)
                .Select(t => loader.LoadMSDecResult(t.MasterAlignmentID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            CanSearchCompound = new[]
            {
                Target.Select(t => t?.innerModel is null),
                MsdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var peakInformationModel = new PeakInformationAlignmentModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new MzPoint(t?.MassCenter ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz),
                t => new DriftPoint(t?.innerModel.TimesCenter.Drift.Value ?? 0d),
                t => new CcsPoint(t?.CollisionCrossSection ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.CollisionCrossSection));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(Target.Select(t => t?.ScanMatchResult), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new CcsSimilarity(r_?.CcsSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
        }

        static ImmsAlignmentModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public InternalStandardSetModel InternalStandardSetModel { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        public AlignmentPeakPlotModel PlotModel { get; }

        public MsSpectrumModel Ms2SpectrumModel { get; }

        public BarChartModel BarChartModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public ImmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }

        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public ImmsCompoundSearchModel<AlignmentSpotProperty> CreateCompoundSearchModel() {
            if (Target.Value?.innerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new ImmsCompoundSearchModel<AlignmentSpotProperty>(
                _files[Target.Value.RepresentativeFileID],
                Target.Value.innerModel,
                MsdecResult.Value,
                _compoundSearchers);
        }

        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public BrushMapData<AlignmentSpotPropertyModel> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<AlignmentSpotPropertyModel> _selectedBrush;

        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        public void SaveSpectra(string filename) {
            using (var file = File.Open(filename, FileMode.Create)) {
                SpectraExport.SaveSpectraTable(
                    (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                    file,
                    Target.Value.innerModel,
                    MsdecResult.Value,
                    _dataBaseMapper,
                    _parameter);
            }
        }

        public bool CanSaveSpectra() => Target.Value.innerModel != null && MsdecResult.Value != null;

        public void SaveProject() {
            MessagePackHandler.SaveToFile(Container, _alignmentFile.FilePath);
        }
    }
}
