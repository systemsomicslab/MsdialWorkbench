using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
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

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsAlignmentModel : AlignmentModelBase
    {
        static DimsAlignmentModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public DimsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotatorCotnainers,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            ParameterBase param, 
            List<AnalysisFileBean> files)
            : base(alignmentFileBean.FilePath) {

            alignmentFile = alignmentFileBean;
            fileName = alignmentFileBean.FileName;
            resultFile = alignmentFileBean.FilePath;
            eicFile = alignmentFileBean.EicFilePath;

            Parameter = param;
            DataBaseMapper = mapper;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            AnnotatorContainers = annotatorCotnainers;

            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));

            MassMin = Ms1Spots.DefaultIfEmpty().Min(v => v?.MassCenter) ?? 0d;
            MassMax = Ms1Spots.DefaultIfEmpty().Max(v => v?.MassCenter) ?? 0d;

            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);
            var labelSource = this.ObserveProperty(m => m.DisplayLabel);
            PlotModel = new Chart.AlignmentPeakPlotModel(Ms1Spots, spot => spot.MassCenter, spot => spot.KMD, Target, labelSource)
            {
                GraphTitle = FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.KMD),
                HorizontalTitle = "m/z",
                VerticalTitle = "Kendrick mass defect"
            };

            var decLoader = new MSDecLoader(alignmentFileBean.SpectraFilePath).AddTo(Disposables);
            var decSpecLoader = new MsDecSpectrumLoader(decLoader, Ms1Spots);
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
                    var parameter = Parameter.ProjectParam;
                    if (parameter.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else if ((comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                        && parameter.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)) {
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

            BarItemsLoader = new HeightBarItemsLoader(Parameter.FileID_ClassName);
            BarChartModel = BarChartModel.Create(Target, BarItemsLoader);
            BarChartModel.Elements.HorizontalTitle = "Class";
            BarChartModel.Elements.VerticalTitle = "Height";
            BarChartModel.Elements.HorizontalProperty = nameof(BarItem.Class);
            BarChartModel.Elements.VerticalProperty = nameof(BarItem.Height);

            var eicLoader = new AlignmentEicLoader(chromatogramSpotSerializer, eicFile, Parameter.FileID_ClassName);
            AlignmentEicModel = AlignmentEicModel.Create(
                Target, eicLoader,
                files, param,
                spot => spot.Time,
                spot => spot.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "m/z";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            AlignmentSpotTableModel = new DimsAlignmentSpotTableModel(Ms1Spots, Target, MassMin, MassMax);

            MsdecResult = Target.Where(t => t != null)
                .Select(t => decLoader.LoadMSDecResult(t.MasterAlignmentID))
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
            switch (Parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    SelectedBrush = Brushes[0].Mapper;
                    break;
                case TargetOmics.Proteomics:
                case TargetOmics.Metabolomics:
                    SelectedBrush = Brushes[1].Mapper;
                    break;
            }
        }

        public AlignmentResultContainer Container {
            get => container;
            set => SetProperty(ref container, value);
        }
        private AlignmentResultContainer container;

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName = string.Empty;

        public AlignmentFileBean AlignmentFile => alignmentFile;
        private readonly AlignmentFileBean alignmentFile;

        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        public ParameterBase Parameter { get; }

        public DataBaseMapper DataBaseMapper { get; }
        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        private readonly string resultFile = string.Empty;
        private readonly string eicFile = string.Empty;

        public IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> AnnotatorContainers { get; }
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

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }

        public double MassMin { get; }
        public double MassMax { get; }

        public Chart.AlignmentPeakPlotModel PlotModel { get; }

        public MsSpectrumModel Ms2SpectrumModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public BarChartModel BarChartModel { get; }

        public DimsAlignmentSpotTableModel AlignmentSpotTableModel { get; }

        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public bool CanSaveSpectra() => Target.Value.innerModel != null && MsdecResult.Value != null;

        public void SaveSpectra(Stream stream, ExportSpectraFileFormat format) {
            SpectraExport.SaveSpectraTable(
                format,
                stream,
                Target.Value.innerModel,
                MsdecResult.Value,
                DataBaseMapper,
                Parameter);
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

        public void SaveProject() {
            MessagePackHandler.SaveToFile(Container, resultFile);
        }
    }
}
