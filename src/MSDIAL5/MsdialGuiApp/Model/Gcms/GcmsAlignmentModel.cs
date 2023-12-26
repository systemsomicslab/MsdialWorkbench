using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAlignmentModel : AlignmentModelBase
    {
        private readonly AlignmentFileBeanModel _alignmentFileBean;
        private readonly IMessageBroker _broker;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly ReactiveProperty<BarItemsLoaderData> _barItemsLoaderDataProperty;
        private readonly ReactivePropertySlim<AlignmentSpotPropertyModel> _target;

        public GcmsAlignmentModel(
            AlignmentFileBeanModel alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering,
            PeakFilterModel peakFilterModel,
            DataBaseMapper mapper,
            MsdialGcmsParameter parameter,
            FilePropertiesModel projectBaseParameter,
            List<AnalysisFileBean> files,
            AnalysisFileBeanModelCollection fileCollection,
            IMessageBroker broker)
            : base(alignmentFileBean, broker)
        {
            _alignmentFileBean = alignmentFileBean;
            _broker = broker;
            UndoManager = new UndoManager().AddTo(Disposables);
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);

            ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer = null;
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RI);
                    break;
                case AlignmentIndexType.RT:
                default:
                    chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
                    break;
            }
            var target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);
            _target = target;

            var spotsSource = new AlignmentSpotSource(alignmentFileBean, Container, chromatogramSpotSerializer).AddTo(Disposables);
            var ms1Spots = spotsSource.Spots.Items;

            InternalStandardSetModel = new InternalStandardSetModel(spotsSource.Spots.Items, TargetMsMethod.Gcms).AddTo(Disposables);
            NormalizationSetModel = new NormalizationSetModel(Container, files, fileCollection, mapper, evaluator, InternalStandardSetModel, parameter, broker).AddTo(Disposables);

            var filterRegistrationManager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(ms1Spots, peakSpotFiltering).AddTo(Disposables);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(ms1Spots, peakFilterModel, evaluator.Contramap<AlignmentSpotPropertyModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: FilterEnableStatus.All & ~FilterEnableStatus.Dt);

            // Peak scatter plot
            var brushMapDataSelector = BrushMapDataSelectorFactory.CreateAlignmentSpotBrushes(parameter.TargetOmics);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(spotsSource, spot => spot.TimesCenter, spot => spot.MassCenter, target, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes)
            {
                GraphTitle = alignmentFileBean.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                VerticalTitle = "m/z",
            }.AddTo(Disposables);
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    PlotModel.HorizontalTitle = "Retention index";
                    break;
                case AlignmentIndexType.RT:
                default:
                    PlotModel.HorizontalTitle = "Retention time [min]";
                    break;
            }

            MatchResultCandidatesModel = new MatchResultCandidatesModel(target.Select(t => t?.MatchResultsModel)).AddTo(Disposables);

            // MS spectrum
            var refLoader = new ReferenceSpectrumLoader<MoleculeMsReference>(mapper);
            IMsSpectrumLoader<AlignmentSpotPropertyModel> msDecSpectrumLoader = new AlignmentMSDecSpectrumLoader(alignmentFileBean);
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty>(target.Select(t => t?.innerModel), mapper, parameter).AddTo(Disposables);
            GraphLabels msGraphLabels = new GraphLabels("Representative vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem deconvolutedSpectrumHueItem = new ChartHueItem(projectBaseParameter, Colors.Blue);
            ObservableMsSpectrum deconvolutedObservableMsSpectrum = ObservableMsSpectrum.Create(target, msDecSpectrumLoader, spectraExporter).AddTo(Disposables);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.SelectedCandidate.Select(c => mapper.MoleculeMsRefer(c)));
            AlignmentSpotSpectraLoader spectraLoader = new AlignmentSpotSpectraLoader(fileCollection, refLoader, _compoundSearchers, fileCollection);
            MsSpectrumModel = new AlignmentMs2SpectrumModel(
                target, MatchResultCandidatesModel.SelectedCandidate, fileCollection,
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Mass), peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Intensity), peak => peak.Intensity),
                new ChartHueItem(projectBaseParameter, Colors.Blue),
                new ChartHueItem(projectBaseParameter, Colors.Red),
                new GraphLabels("Representative vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                Observable.Return(spectraExporter),
                Observable.Return(referenceExporter),
                null,
                spectraLoader).AddTo(Disposables);

            // Class intensity bar chart
            var classBrush = projectBaseParameter.ClassProperties
                .CollectionChangedAsObservable().ToUnit()
                .StartWith(Unit.Default)
                .SelectSwitch(_ => projectBaseParameter.ClassProperties.Select(prop => prop.ObserveProperty(p => p.Color).Select(_2 => prop)).CombineLatest())
                .Select(lst => new KeyBrushMapper<string>(lst.ToDictionary(item => item.Name, item => item.Color)))
                .ToReactiveProperty().AddTo(Disposables);
            var barBrush = classBrush.Select(bm => bm.Contramap((BarItem item) => item.Class));

            var fileIdToClassNameAsObservable = projectBaseParameter.ObserveProperty(p => p.FileIdToClassName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var peakSpotAxisLabelAsObservable = target.OfType<AlignmentSpotPropertyModel>().SelectSwitch(t => t.ObserveProperty(t_ => t_.IonAbundanceUnit).Select(t_ => t_.ToLabel())).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
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
            _barItemsLoaderDataProperty = barItemsLoaderDataProperty;
            BarChartModel = new BarChartModel(target, barItemsLoaderDataProperty, barItemLoaderDatas, barBrush, projectBaseParameter, fileCollection, projectBaseParameter.ClassProperties).AddTo(Disposables);

            // Class eic
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            AlignmentEicModel = AlignmentEicModel.Create(
                target,
                alignmentFileBean.CreateEicLoader(chromatogramSpotSerializer, fileCollection, projectBaseParameter).AddTo(Disposables),
                files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "EIC";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    AlignmentEicModel.Elements.HorizontalTitle = "Retention index";
                    break;
                case AlignmentIndexType.RT:
                default:
                    AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
                    break;
            }

            var barItemsLoaderProperty = barItemsLoaderDataProperty.SkipNull().SelectSwitch(data => data.ObservableLoader).ToReactiveProperty().AddTo(Disposables);
            var filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
            AlignmentSpotTableModel = new GcmsAlignmentSpotTableModel(ms1Spots, target, barBrush, projectBaseParameter.ClassProperties, barItemsLoaderProperty, filter, spectraLoader).AddTo(Disposables);

            var peakInformationModel = new PeakInformationAlignmentModel(target).AddTo(Disposables);
            peakInformationModel.Add(t => new MzPoint(t?.MassCenter ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    peakInformationModel.Add(t => new RiPoint(t?.innerModel.TimesCenter.RI.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RI.Value));
                    break;
                case AlignmentIndexType.RT:
                    peakInformationModel.Add(t => new RtPoint(t?.innerModel.TimesCenter.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value));
                    break;
            }
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(target.DefaultIfNull(t => t.ObserveProperty(p => p.ScanMatchResult), Observable.Return<MsScanMatchResult>(null)).Switch(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d));
            switch (parameter.RetentionType) {
                case RetentionType.RI:
                    compoundDetailModel.Add(r_ => new RiSimilarity(r_?.RiSimilarity ?? 0d));
                    break;
                case RetentionType.RT:
                    compoundDetailModel.Add(r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d));
                    break;
            }
            compoundDetailModel.Add(r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;

            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.innerModel)).AddTo(Disposables);
        }

        public AlignmentPeakPlotModel PlotModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }
        public BarChartModel BarChartModel { get; }
        public InternalStandardSetModel InternalStandardSetModel { get; }
        public NormalizationSetModel NormalizationSetModel { get; }
        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public AlignmentEicModel AlignmentEicModel { get; }
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public AlignmentMs2SpectrumModel MsSpectrumModel { get; }
        public GcmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }
        public UndoManager UndoManager { get; }
        public IObservable<bool> CanSetUnknown => _target.Select(t => !(t is null));
        public void SetUnknown() => _target.Value?.SetUnknown(UndoManager);

        public override void InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }

        public override void InvokeMsfinder() {
            throw new NotImplementedException();
        }

        public override void SearchFragment() {
            throw new NotImplementedException();
        }
    }
}
