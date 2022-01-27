using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
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
using System.Windows;
using System.Windows.Media;

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
            IReadOnlyList<ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotators,
            DataBaseMapper mapper,
            MsdialLcmsParameter parameter,
            IObservable<IBarItemsLoader> barItemsLoader) {
            if (annotators is null) {
                throw new ArgumentNullException(nameof(annotators));
            }

            if (barItemsLoader is null) {
                throw new ArgumentNullException(nameof(barItemsLoader));
            }

            AlignmentFile = alignmentFileBean;
            Parameter = parameter;
            DataBaseMapper = mapper;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            Annotators = annotators;
            container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(AlignmentFile.FilePath);
            if (container == null) {
                MessageBox.Show("No aligned spot information.");
            }
            Ms1Spots = container == null ? new ObservableCollection<AlignmentSpotPropertyModel>() : 
                new ObservableCollection<AlignmentSpotPropertyModel>(
                container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));
           
            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);
            this.decLoader = new MSDecLoader(AlignmentFile.SpectraFilePath);
            MsdecResult = Target.Where(t => t != null)
                .Select(t => this.decLoader.LoadMSDecResult(t.MasterAlignmentID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            BarItemsLoader = new HeightBarItemsLoader(parameter.FileID_ClassName);

            MassMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.MassCenter) ?? 0d;
            MassMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.MassCenter) ?? 0d;
            RtMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.TimesCenter) ?? 0d;
            RtMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.TimesCenter) ?? 0d;

            // Peak scatter plot
            var labelSource = this.ObserveProperty(m => m.DisplayLabel);
            PlotModel = new Chart.AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource)
            {
                GraphTitle = AlignmentFile.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
            };

            // Ms2 spectrum
            Ms2SpectrumModel = MsSpectrumModel.Create(
                Target,
                new MsDecSpectrumLoader(this.decLoader, Ms1Spots),
                new MsRefSpectrumLoader(mapper),
                peak => peak.Mass,
                peak => peak.Intensity);
            Ms2SpectrumModel.GraphTitle = "Representative vs. Reference";
            Ms2SpectrumModel.HorizontalTitle = "m/z";
            Ms2SpectrumModel.VerticalTitle = "Abundance";
            Ms2SpectrumModel.HorizontalProperty = nameof(SpectrumPeak.Mass);
            Ms2SpectrumModel.VerticalProperty = nameof(SpectrumPeak.Intensity);
            Ms2SpectrumModel.LabelProperty = nameof(SpectrumPeak.Mass);
            Ms2SpectrumModel.OrderingProperty = nameof(SpectrumPeak.Intensity);

            // Class intensity bar chart
            BarChartModel = BarChartModel.Create(Target, barItemsLoader);
            BarChartModel.Elements.HorizontalTitle = "Class";
            BarChartModel.Elements.VerticalTitle = "Height";
            BarChartModel.Elements.HorizontalProperty = nameof(BarItem.Class);
            BarChartModel.Elements.VerticalProperty = nameof(BarItem.Height);

            // Class eic
            AlignmentEicModel = AlignmentEicModel.Create(
                Target,
                new AlignmentEicLoader(chromatogramSpotSerializer, alignmentFileBean.EicFilePath, parameter.PeakPickBaseParam, parameter.FileID_ClassName),
                peak => peak.Time,
                peak => peak.Intensity);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            AlignmentSpotTableModel = new LcmsAlignmentSpotTableModel(Ms1Spots, Target, MassMin, MassMax, RtMin, RtMax);

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
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        private readonly AlignmentResultContainer container;

        public AlignmentFileBean AlignmentFile { get; }
        public ParameterBase Parameter { get; }
        public DataBaseMapper DataBaseMapper { get; }
        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }
        public IReadOnlyList<ISerializableAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> Annotators { get; }
        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }
        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }
        public IBarItemsLoader BarItemsLoader { get; }

        protected readonly MSDecLoader decLoader;
        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }

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

        public void SaveProject() {
            MessagePackHandler.SaveToFile(container, AlignmentFile.FilePath);
        }

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public CompoundSearchModel<AlignmentSpotProperty> CreateCompoundSearchModel() {
            if (Target.Value?.innerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new LcmsCompoundSearchModel<AlignmentSpotProperty>(
                AlignmentFile,
                Target.Value.innerModel,
                MsdecResult.Value,
                null,
                Annotators);
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
        }

        public void GoToMsfinderMethod() {
            MsDialToExternalApps.SendToMsFinderProgram(
                this.AlignmentFile,
                Target.Value.innerModel,
                MsdecResult.Value,
                DataBaseMapper,
                Parameter);
        }
    }
}
