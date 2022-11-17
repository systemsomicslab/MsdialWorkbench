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
using CompMs.Common.Extension;
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
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Dims
{
    internal class DimsAlignmentModel : AlignmentModelBase
    {
        static DimsAlignmentModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;
        private static readonly double MZ_TOLERANCE = 20d;

        private readonly AlignmentFileBean _alignmentFile;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _matchResultEvaluator;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult> _msdecResult;
        private readonly ParameterBase _parameter;
        private readonly List<AnalysisFileBean> _files;
        private readonly AnalysisFileBeanModelCollection _fileCollection;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly IMessageBroker _broker;
        private readonly MSDecLoader _decLoader;

        public DimsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            DataBaseStorage databaseStorage,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            ParameterBase parameter,
            List<AnalysisFileBean> files,
            AnalysisFileBeanModelCollection fileCollection,
            PeakFilterModel peakFilterModel,
            IMessageBroker broker)
            : base(alignmentFileBean, alignmentFileBean.FilePath) {

            _alignmentFile = alignmentFileBean;

            _parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _fileCollection = fileCollection ?? throw new ArgumentNullException(nameof(fileCollection));
            _broker = broker;
            _dataBaseMapper = mapper;
            _matchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databaseStorage, mapper, parameter.PeakPickBaseParam);

            InternalStandardSetModel = new InternalStandardSetModel(Ms1Spots, TargetMsMethod.Dims).AddTo(Disposables);

            var barItemsLoader = new HeightBarItemsLoader(parameter.FileID_ClassName);
            var observableBarItemsLoader = Observable.Return(barItemsLoader);
            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));

            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Spots, peakFilterModel, evaluator, status: ~(FilterEnableStatus.Rt | FilterEnableStatus.Dt)).AddTo(Disposables);

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
            var brushes = new List<BrushMapData<AlignmentSpotPropertyModel>> { ontologyBrush, intensityBrush };
            BrushMapData<AlignmentSpotPropertyModel> selectedBrush = null;
            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    selectedBrush = ontologyBrush;
                    break;
                case TargetOmics.Proteomics:
                case TargetOmics.Metabolomics:
                default:
                    selectedBrush = intensityBrush;
                    break;
            }

            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(Ms1Spots, spot => spot.MassCenter, spot => spot.KMD, Target, labelSource, selectedBrush, brushes)
            {
                GraphTitle = _alignmentFile.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.KMD),
                HorizontalTitle = "m/z",
                VerticalTitle = "Kendrick mass defect"
            }.AddTo(Disposables);

            var decLoader = new MSDecLoader(alignmentFileBean.SpectraFilePath).AddTo(Disposables);
            _decLoader = decLoader;
            var decSpecLoader = new MsDecSpectrumLoader(decLoader, Ms1Spots);
            var refLoader = new MsRefSpectrumLoader(mapper);
            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               parameter.ProjectParam.SpectrumCommentToColorBytes
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
                    if (parameter.ProjectParam.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else if ((comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                        && parameter.ProjectParam.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else {
                        return Colors.Red;
                    }
                },
                true);
            Ms2SpectrumModel = MsSpectrumModel.Create(
                Target, decSpecLoader, refLoader,
                spot => spot.Mass,
                spot => spot.Intensity,
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

            var classToColor = parameter.ClassnameToColorBytes
                .ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            var eicLoader = new AlignmentEicLoader(CHROMATOGRAM_SPOT_SERIALIZER, alignmentFileBean.EicFilePath, Observable.Return(parameter.FileID_ClassName), Observable.Return(classToColor), Observable.Return(fileIdToFileName)).AddTo(Disposables);
            AlignmentEicModel = AlignmentEicModel.Create(
                Target, eicLoader,
                files, parameter,
                spot => spot.Time,
                spot => spot.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "m/z";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            AlignmentSpotTableModel = new DimsAlignmentSpotTableModel(Ms1Spots, Target, Observable.Return(classBrush), observableBarItemsLoader).AddTo(Disposables);

            _msdecResult = Target.Where(t => t != null)
                .Select(t => decLoader.LoadMSDecResult(t.MasterAlignmentID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            CanSeachCompound = new[] {
                Target.Select(t => t?.innerModel != null),
                _msdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var mzSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, MZ_TOLERANCE, Target.Select(t => t?.MassCenter ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                Target,
                id => Ms1Spots.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                Target.Select(t => t?.MasterAlignmentID ?? 0d),
                "Region focus by ID",
                (mzSpotFocus, spot => spot.MassCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, mzSpotFocus);

            var peakInformationModel = new PeakInformationAlignmentModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new MzPoint(t?.innerModel.TimesCenter.Mz.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(Target.Select(t => t?.ScanMatchResult), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
        }

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public AlignmentPeakPlotModel PlotModel { get; }

        public MsSpectrumModel Ms2SpectrumModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public BarChartModel BarChartModel { get; }

        public DimsAlignmentSpotTableModel AlignmentSpotTableModel { get; }

        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }
        public ReadOnlyReactivePropertySlim<bool> CanSeachCompound { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }

        public ICompoundSearchModel BuildCompoundSearchModel() {
            return new CompoundSearchModel(_files[Target.Value.RepresentativeFileID], Target.Value, _msdecResult.Value, _compoundSearchers.Items);
        }

        public InternalStandardSetModel InternalStandardSetModel { get; }

        public NormalizationSetModel BuildNormalizeSetModel() {
            return new NormalizationSetModel(Container, _files, _fileCollection, _dataBaseMapper, _matchResultEvaluator, InternalStandardSetModel, _parameter, _broker);
        }

        public bool CanSaveSpectra() => Target.Value.innerModel != null && _msdecResult.Value != null;

        public void SaveSpectra(Stream stream, ExportSpectraFileFormat format) {
            SpectraExport.SaveSpectraTable(
                format,
                stream,
                Target.Value.innerModel,
                _msdecResult.Value,
                _dataBaseMapper,
                _parameter);
        }

        public void SaveSpectra(string filename) {
            var format = (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.'));
            using (var file = File.Open(filename, FileMode.Create)) {
                SaveSpectra(file, format);
            }
        }

        public void CopySpectrum() {
            var memory = new MemoryStream();
            SaveSpectra(memory, ExportSpectraFileFormat.msp);
            Clipboard.SetText(System.Text.Encoding.UTF8.GetString(memory.ToArray()));
        }

        public override void SearchFragment() {
            MsdialCore.Algorithm.FragmentSearcher.Search(Ms1Spots.Select(n => n.innerModel).ToList(), _decLoader, _parameter);
        }
    }
}
