using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
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

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsAlignmentModel : AlignmentModelBase
    {
        public ImmsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotatorContainers,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            ParameterBase parameter,
            List<AnalysisFileBean> files)
            : base(alignmentFileBean.FilePath) {

            AlignmentFile = alignmentFileBean;
            ResultFile = alignmentFileBean.FilePath;
            Parameter = parameter;
            DataBaseMapper = mapper;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            AnnotatorContainers = annotatorContainers;

            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));

            MassMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.MassCenter) ?? 0d;
            MassMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.MassCenter) ?? 0d;
            DriftMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.TimesCenter) ?? 0d;
            DriftMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.TimesCenter) ?? 0d;

            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);
            var fileName = alignmentFileBean.FileName;
            var labelSource = this.ObserveProperty(m => m.DisplayLabel);
            PlotModel = new Chart.AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource)
            {
                GraphTitle = fileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "m/z",
            };

            var loader = new MSDecLoader(alignmentFileBean.SpectraFilePath);
            var decLoader = new MsDecSpectrumLoader(loader, Ms1Spots);
            var refLoader = new MsRefSpectrumLoader(mapper);
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

            BarItemsLoader = new HeightBarItemsLoader(parameter.FileID_ClassName);
            BarChartModel = BarChartModel.Create(Target, BarItemsLoader);
            BarChartModel.Elements.HorizontalTitle = "Class";
            BarChartModel.Elements.VerticalTitle = "Height";
            BarChartModel.Elements.HorizontalProperty = nameof(BarItem.Class);
            BarChartModel.Elements.VerticalProperty = nameof(BarItem.Height);

            var eicFile = alignmentFileBean.EicFilePath;
            var eicLoader = new AlignmentEicLoader(chromatogramSpotSerializer, eicFile, parameter.PeakPickBaseParam, parameter.FileID_ClassName);
            AlignmentEicModel = AlignmentEicModel.Create(
                Target, eicLoader, files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Drift time [1/k0]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            AlignmentSpotTableModel = new ImmsAlignmentSpotTableModel(Ms1Spots, Target, MassMin, MassMax, DriftMin, DriftMax);

            MsdecResult = Target.Where(t => t != null)
                .Select(t => loader.LoadMSDecResult(t.MasterAlignmentID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

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

        static ImmsAlignmentModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }

        public double MassMin { get; }
        public double MassMax { get; }
        public double DriftMin { get; }
        public double DriftMax { get; }

        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        public Chart.AlignmentPeakPlotModel PlotModel { get; }

        public MsSpectrumModel Ms2SpectrumModel { get; }

        public BarChartModel BarChartModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public ImmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public void PrepareCompoundSearch() {
        }

        public ImmsCompoundSearchModel<AlignmentSpotProperty> CreateCompoundSearchModel() {
            if (Target.Value?.innerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new ImmsCompoundSearchModel<AlignmentSpotProperty>(
                AlignmentFile,
                Target.Value.innerModel,
                MsdecResult.Value,
                null,
                AnnotatorContainers);
        }

        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public IBrushMapper<AlignmentSpotPropertyModel> SelectedBrush {
            get => selectedBrush;
            set => SetProperty(ref selectedBrush, value);
        }
        private IBrushMapper<AlignmentSpotPropertyModel> selectedBrush;

        public IBarItemsLoader BarItemsLoader {
            get => barItemsLoader;
            set => SetProperty(ref barItemsLoader, value);
        }
        private IBarItemsLoader barItemsLoader;

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public AlignmentFileBean AlignmentFile { get; }

        public string ResultFile { get; }

        public ParameterBase Parameter { get; }
        public DataBaseMapper DataBaseMapper { get; }
        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }
        public IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> AnnotatorContainers { get; }
        
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

        public void SaveProject() {
            MessagePackHandler.SaveToFile(Container, ResultFile);
        }
    }
}
