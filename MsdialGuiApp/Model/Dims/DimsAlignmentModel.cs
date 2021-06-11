using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.Base;
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
using System.Reactive.Disposables;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsAlignmentModel : BindableBase, IDisposable
    {
        static DimsAlignmentModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public DimsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            IMatchResultRefer refer,
            ParameterBase param,
            IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator,
            IAnnotator<AlignmentSpotProperty, MSDecResult> textDBAnnotator) {

            alignmentFile = alignmentFileBean;
            fileName = alignmentFileBean.FileName;
            resultFile = alignmentFileBean.FilePath;
            eicFile = alignmentFileBean.EicFilePath;

            this.Parameter = param;
            this.DataBaseRefer = refer;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            Container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(resultFile);

            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));

            PlotModel = new Chart.AlignmentPeakPlotModel(Ms1Spots, spot => spot.MassCenter, spot => spot.KMD)
            {
                GraphTitle = FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.KMD),
                HorizontalTitle = "m/z",
                VerticalTitle = "Kendrick mass defect"
            };

            Target = PlotModel
                .ToReactivePropertySlimAsSynchronized(m => m.Target)
                .AddTo(disposables);

            var decLoader = new MsDecSpectrumLoader(alignmentFileBean.SpectraFilePath, Ms1Spots).AddTo(disposables);
            msdecLoader = decLoader;
            var refLoader = new MsRefSpectrumLoader(refer);
            Ms2SpectrumModel = MsSpectrumModel.Create(
                Target, decLoader, refLoader,
                spot => spot.Mass,
                spot => spot.Intensity);
            Ms2SpectrumModel.GraphTitle = "Representation vs. Reference";
            Ms2SpectrumModel.HorizontalTitle = "m/z";
            Ms2SpectrumModel.VerticalTitle = "Abundance";
            Ms2SpectrumModel.HorizontalProperty = nameof(SpectrumPeak.Mass);
            Ms2SpectrumModel.VerticalProperty = nameof(SpectrumPeak.Intensity);
            Ms2SpectrumModel.LabelProperty = nameof(SpectrumPeak.Mass);
            Ms2SpectrumModel.OrderingProperty = nameof(SpectrumPeak.Intensity);

            var barLoader = new HeightBarItemsLoader(Parameter.FileID_ClassName);
            BarChartModel = Chart.BarChartModel.Create(
                Target, barLoader,
                item => item.Class,
                item => item.Height);
            BarChartModel.Elements.HorizontalTitle = "Class";
            BarChartModel.Elements.VerticalTitle = "Height";
            BarChartModel.Elements.HorizontalProperty = nameof(BarItem.Class);
            BarChartModel.Elements.VerticalProperty = nameof(BarItem.Height);

            var eicLoader = new AlignmentEicLoader(chromatogramSpotSerializer, eicFile, Parameter.FileID_ClassName);
            AlignmentEicModel = Chart.AlignmentEicModel.Create(
                Target, eicLoader,
                spot => spot.Time,
                spot => spot.Intensity);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Drift time [1/k0]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            Brushes = new List<BrushMapData<AlignmentSpotPropertyModel>>
            {
                new BrushMapData<AlignmentSpotPropertyModel>(
                    new KeyBrushMapper<AlignmentSpotPropertyModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        spot => spot.Ontology,
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
                case TargetOmics.Metabolomics:
                    SelectedBrush = Brushes[1].Mapper;
                    break;
            }
        }

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private readonly MsDecSpectrumLoader msdecLoader;

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

        public MSDecResult MsdecResult => msdecLoader.Result;

        public ParameterBase Parameter { get; }

        public IMatchResultRefer DataBaseRefer { get; }

        private readonly string resultFile = string.Empty;
        private readonly string eicFile = string.Empty;

        public IAnnotator<AlignmentSpotProperty, MSDecResult> MspAnnotator => mspAnnotator;
        public IAnnotator<AlignmentSpotProperty, MSDecResult> TextDBAnnotator => textDBAnnotator;
        private readonly IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator, textDBAnnotator;

        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public IBrushMapper<AlignmentSpotPropertyModel> SelectedBrush {
            get => selectedBrush;
            set => SetProperty(ref selectedBrush, value);
        }
        private IBrushMapper<AlignmentSpotPropertyModel> selectedBrush;

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; } = new ObservableCollection<AlignmentSpotPropertyModel>();

        public Chart.AlignmentPeakPlotModel PlotModel { get; }

        public MsSpectrumModel Ms2SpectrumModel { get; }

        public Chart.AlignmentEicModel AlignmentEicModel { get; }

        public Chart.BarChartModel BarChartModel { get; }

        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        private bool disposedValue;

        public void SaveSpectra(string filename) {
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                filename,
                Target.Value.innerModel,
                MsdecResult,
                Parameter);
        }

        public bool CanSaveSpectra() => Target.Value.innerModel != null && MsdecResult != null;

        public void SaveProject() {
            MessagePackHandler.SaveToFile(Container, resultFile);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    disposables.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
