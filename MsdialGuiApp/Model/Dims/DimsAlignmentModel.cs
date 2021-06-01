using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    public class DimsAlignmentModel : BindableBase, IDisposable
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

            ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));

            var mzAxis = new ContinuousAxisManager<double>(MassMin, MassMax)
            {
                ChartMargin = new Graphics.Core.Base.ChartMargin(0.05),
            };

            var kmdAxis = new ContinuousAxisManager<double>(-0.5, 0.5)
            {
                ChartMargin = new Graphics.Core.Base.ChartMargin(0.05),
            };

            PlotModel = new AlignmentPeakPlotModel(Ms1Spots, mzAxis, kmdAxis);

            var ms2MzAxis = new AxisData(new ContinuousAxisManager<double>(0, 1), "Mass", "m/z");
            var repIntensityAxis = new AxisData(new ContinuousAxisManager<double>(0, 1, 0, 0), "Intensity", "Abundance");
            var refIntensityAxis = new AxisData(new ContinuousAxisManager<double>(0, 1, 0, 0), "Intensity", "Abundance");
            var decLoader = new MsDecSpectrumLoader(alignmentFileBean.SpectraFilePath, ms1Spots).AddTo(disposables);
            var refLoader = new MsRefSpectrumLoader(refer);
            Ms2SpectrumModel = new MsSpectrumModel<AlignmentSpotPropertyModel>(
                ms2MzAxis,
                repIntensityAxis, decLoader,
                refIntensityAxis, refLoader,
                "Representation vs. Reference");

            AlignmentEicModel = new AlignmentEicModel(
                chromatogramSpotSerializer,
                eicFile,
                Parameter.FileID_ClassName,
                "TIC, EIC, or BPC chromatograms",
                "m/z");

            BarChartModel = new BarChartModel(
                new AxisData(
                    new CategoryAxisManager<string>(new List<string>()),
                    "Class",
                    "Class"),
                new AxisData(
                    new ContinuousAxisManager<double>(0, 1, 0, 0) {
                        ChartMargin = new ChartMargin(0, 0.025)
                    },
                    "Height",
                    "Height"),
                new HeightBarItemsLoader(Parameter.FileID_ClassName));
        }

        private readonly CompositeDisposable disposables = new CompositeDisposable();

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

        public MSDecResult MsdecResult => msdecResult;
        private MSDecResult msdecResult = null;

        public ParameterBase Parameter { get; }

        public IMatchResultRefer DataBaseRefer { get; }

        private readonly string resultFile = string.Empty;
        private readonly string eicFile = string.Empty;

        public IAnnotator<AlignmentSpotProperty, MSDecResult> MspAnnotator => mspAnnotator;
        public IAnnotator<AlignmentSpotProperty, MSDecResult> TextDBAnnotator => textDBAnnotator;
        private readonly IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator, textDBAnnotator;

        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots => ms1Spots;
        private readonly ObservableCollection<AlignmentSpotPropertyModel> ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>();

        public double MassMin => ms1Spots.Min(spot => spot.MassCenter);
        public double MassMax => ms1Spots.Max(spot => spot.MassCenter);

        public AlignmentPeakPlotModel PlotModel {
            get => plotModel;
            private set {
                var newValue = value;
                var oldValue = plotModel;
                if (SetProperty(ref plotModel, value)) {
                    if (oldValue != null) {
                        oldValue.PropertyChanged -= OnPlotModelTargetChanged;
                    }
                    if (newValue != null) {
                        newValue.PropertyChanged += OnPlotModelTargetChanged;
                    }
                }
            }
        }
        private AlignmentPeakPlotModel plotModel;

        private void OnPlotModelTargetChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(PlotModel.Target)) {
                Target = PlotModel.Target;
            }
        }

        public MsSpectrumModel<AlignmentSpotPropertyModel> Ms2SpectrumModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public BarChartModel BarChartModel { get; }

        public AlignmentSpotPropertyModel Target {
            get => target;
            set {
                if (SetProperty(ref target, value)) {
                    _ = OnTargetChangedAsync(target);   
                }
            }
        }
        private AlignmentSpotPropertyModel target;

        private CancellationTokenSource cts;
        private bool disposedValue;

        public async Task OnTargetChangedAsync(AlignmentSpotPropertyModel target) {
            cts?.Cancel();
            var localCts = cts = new CancellationTokenSource();

            try {
                await OnTargetChangedAsync(target, localCts.Token).ContinueWith(
                    t => {
                        localCts.Dispose();
                        if (cts == localCts) {
                            cts = null;
                        }
                    }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {

            }
        }

        private async Task OnTargetChangedAsync(AlignmentSpotPropertyModel target, CancellationToken token = default) {
            await Task.WhenAll(
                BarChartModel.LoadBarItemsAsync(target, token),
                AlignmentEicModel.LoadEicAsync(target, token),
                Ms2SpectrumModel.LoadSpectrumAsync(target, token)
            ).ConfigureAwait(false);
        }

        public void SaveSpectra(string filename) {
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                filename,
                Target.innerModel,
                msdecResult,
                Parameter);
        }

        public bool CanSaveSpectra() => Target.innerModel != null && msdecResult != null;

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
