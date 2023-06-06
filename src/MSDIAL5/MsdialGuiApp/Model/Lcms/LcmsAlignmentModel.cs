using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.Model.Table;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
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
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly List<AnalysisFileBean> _files;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly UndoManager _undoManager;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult> _msdecResult;

        public LcmsAlignmentModel(
            AlignmentFileBeanModel alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            PeakFilterModel peakFilterModel,
            DataBaseMapper mapper,
            MsdialLcmsParameter parameter,
            ProjectBaseParameterModel projectBaseParameter,
            List<AnalysisFileBean> files,
            AnalysisFileBeanModelCollection fileCollection,
            IMessageBroker messageBroker)
            : base(alignmentFileBean) {
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
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            _undoManager = new UndoManager().AddTo(Disposables);

            var spotsSource = new AlignmentSpotSource(alignmentFileBean, Container, CHROMATOGRAM_SPOT_SERIALIZER).AddTo(Disposables);
            Ms1Spots = spotsSource.Spots.Items;
            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);

            InternalStandardSetModel = new InternalStandardSetModel(Ms1Spots, TargetMsMethod.Lcms).AddTo(Disposables);
            NormalizationSetModel = new NormalizationSetModel(Container, files, fileCollection, mapper, evaluator, InternalStandardSetModel, parameter, messageBroker).AddTo(Disposables);

            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                var proteinResultContainer = alignmentFileBean.LoadProteinResult();
                var proteinResultContainerModel = new ProteinResultContainerModel(proteinResultContainer, Ms1Spots, Target);
                ProteinResultContainerModel = proteinResultContainerModel;
            }

            _msdecResult = Target.SkipNull()
                .Select(t => _alignmentFile.LoadMSDecResultByIndexAsync(t.MasterAlignmentID))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Spots, peakFilterModel, evaluator, status: FilterEnableStatus.All & ~FilterEnableStatus.Dt).AddTo(Disposables);

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
            PlotModel = new AlignmentPeakPlotModel(spotsSource, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource, selectedBrush, brushes)
            {
                GraphTitle = ((IFileBean)_alignmentFile).FileName,
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
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel)).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference>(mapper);
            IConnectableObservable<List<SpectrumPeak>> refSpectrum = MatchResultCandidatesModel.LoadSpectrumObservable(refLoader).Publish();
            Disposables.Add(refSpectrum.Connect());
            IMsSpectrumLoader<AlignmentSpotPropertyModel> msDecSpectrumLoader = new AlignmentMSDecSpectrumLoader(_alignmentFile);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.SelectedCandidate.Select(c => mapper.MoleculeMsRefer(c)));
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty>(Target.Select(t => t?.innerModel), mapper, Parameter).AddTo(Disposables);
            Ms2SpectrumModel = new MsSpectrumModel(
                Target.SelectSwitch(msDecSpectrumLoader.LoadSpectrumAsObservable),
                refSpectrum,
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Mass), peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Intensity), peak => peak.Intensity),
                new GraphLabels("Representative vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush),
                Observable.Return(spectraExporter),
                Observable.Return(referenceExporter),
                null,
                MatchResultCandidatesModel.GetCandidatesScorer(_compoundSearchers)).AddTo(Disposables);

            // Class intensity bar chart
            var classBrush = projectBaseParameter.ClassProperties
                .CollectionChangedAsObservable().ToUnit()
                .StartWith(Unit.Default)
                .SelectSwitch(_ => projectBaseParameter.ClassProperties.Select(prop => prop.ObserveProperty(p => p.Color).Select(_2 => prop)).CombineLatest())
                .Select(lst => new KeyBrushMapper<string>(lst.ToDictionary(item => item.Name, item => item.Color)))
                .ToReactiveProperty().AddTo(Disposables);
            var barBrush = classBrush.Select(bm => bm.Contramap((BarItem item) => item.Class));

            var fileIdToClassNameAsObservable = projectBaseParameter.ObserveProperty(p => p.FileIdToClassName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var peakSpotAxisLabelAsObservable = Target.OfType<AlignmentSpotPropertyModel>().SelectSwitch(t => t.ObserveProperty(t_ => t_.IonAbundanceUnit).Select(t_ => t_.ToLabel())).Publish();
            var normalizedAreaZeroLoader = new BarItemsLoaderData("Normalized peak area above zero", peakSpotAxisLabelAsObservable, new NormalizedAreaAboveZeroBarItemsLoader(fileIdToClassNameAsObservable, fileCollection), NormalizationSetModel.IsNormalized);
            var normalizedAreaBaselineLoader = new BarItemsLoaderData("Normalized peak area above base line", peakSpotAxisLabelAsObservable, new NormalizedAreaAboveBaseLineBarItemsLoader(fileIdToClassNameAsObservable, fileCollection), NormalizationSetModel.IsNormalized);
            var normalizedHeightLoader = new BarItemsLoaderData("Normalized peak height", peakSpotAxisLabelAsObservable, new NormalizedHeightBarItemsLoader(fileIdToClassNameAsObservable, fileCollection), NormalizationSetModel.IsNormalized);
            var areaZeroLoader = new BarItemsLoaderData("Peak area above zero", "Area", new AreaAboveZeroBarItemsLoader(fileIdToClassNameAsObservable, fileCollection));
            var areaBaselineLoader = new BarItemsLoaderData("Peak area above base line", "Area", new AreaAboveBaseLineBarItemsLoader(fileIdToClassNameAsObservable, fileCollection));
            var heightLoader = new BarItemsLoaderData("Peak height", "Height", new HeightBarItemsLoader(fileIdToClassNameAsObservable, fileCollection));
            var barItemLoaderDatas = new[]
            {
                heightLoader, areaBaselineLoader, areaZeroLoader,
                normalizedHeightLoader, normalizedAreaBaselineLoader, normalizedAreaZeroLoader,
            };
            var barItemsLoaderDataProperty = NormalizationSetModel.Normalized.ToConstant(normalizedHeightLoader).ToReactiveProperty(NormalizationSetModel.IsNormalized.Value ? normalizedHeightLoader : heightLoader).AddTo(Disposables);
            Disposables.Add(peakSpotAxisLabelAsObservable.Connect());
            BarChartModel = new BarChartModel(Target, barItemsLoaderDataProperty, barItemLoaderDatas, barBrush, projectBaseParameter, projectBaseParameter.ClassProperties).AddTo(Disposables);

            // Class eic
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            AlignmentEicModel = AlignmentEicModel.Create(
                Target,
                alignmentFileBean.CreateEicLoader(CHROMATOGRAM_SPOT_SERIALIZER, fileCollection, projectBaseParameter).AddTo(Disposables),
                files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            var barItemsLoaderProperty = barItemsLoaderDataProperty.SkipNull().SelectSwitch(data => data.ObservableLoader).ToReactiveProperty().AddTo(Disposables);
            AlignmentSpotTableModel = new LcmsAlignmentSpotTableModel(Ms1Spots, Target, PeakSpotNavigatorModel, barBrush, projectBaseParameter.ClassProperties, barItemsLoaderProperty).AddTo(Disposables);

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
                ((ISpotFocus, Func<AlignmentSpotPropertyModel, double>))(rtSpotFocus, spot => spot.TimesCenter),
                ((ISpotFocus, Func<AlignmentSpotPropertyModel, double>))(mzSpotFocus, spot => spot.MassCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus);

            var peakInformationModel = new PeakInformationAlignmentModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new RtPoint(t?.innerModel.TimesCenter.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value),
                t => new MzPoint(t?.MassCenter ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(Target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;

            if (parameter.ProjectParam.TargetOmics != TargetOmics.Proteomics) {
                var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
                MoleculeStructureModel = moleculeStructureModel;
                Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.innerModel)).AddTo(Disposables);
            }

            MultivariateAnalysisSettingModel = new MultivariateAnalysisSettingModel(parameter, Ms1Spots, evaluator, files, classBrush);

            FindTargetCompoundSpotModel = new FindTargetCompoundsSpotModel(spotsSource.Spots.Items, Target, messageBroker).AddTo(Disposables);
        }

        public UndoManager UndoManager => _undoManager;

        public ParameterBase Parameter { get; }

        public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }
        public InternalStandardSetModel InternalStandardSetModel { get; }
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public AlignmentPeakPlotModel PlotModel { get; }
        public MsSpectrumModel Ms2SpectrumModel { get; }
        public BarChartModel BarChartModel { get; }
        public AlignmentEicModel AlignmentEicModel { get; }
        public LcmsAlignmentSpotTableModel AlignmentSpotTableModel { get; private set; }
        public NormalizationSetModel NormalizationSetModel { get; }
        public MultivariateAnalysisSettingModel MultivariateAnalysisSettingModel { get; }
        public FindTargetCompoundsSpotModel FindTargetCompoundSpotModel { get; }
        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }
        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }
        public ProteinResultContainerModel ProteinResultContainerModel { get; }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public ICompoundSearchModel CreateCompoundSearchModel() {
            return new LcmsCompoundSearchModel(_files[Target.Value.RepresentativeFileID], Target.Value, _msdecResult.Value, _compoundSearchers.Items, _undoManager);
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

        public override void SearchFragment() {
            using (var decLoader = _alignmentFile.CreateTemporaryMSDecLoader()) {
                MsdialCore.Algorithm.FragmentSearcher.Search(Ms1Spots.Select(n => n.innerModel).ToList(), decLoader, Parameter);
            }
        }

        public override void InvokeMsfinder() {
            if (Target.Value is null || (_msdecResult.Value?.Spectrum).IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                _alignmentFile,
                Target.Value.innerModel,
                _msdecResult.Value,
                _dataBaseMapper,
                Parameter);
        }
    }
}
