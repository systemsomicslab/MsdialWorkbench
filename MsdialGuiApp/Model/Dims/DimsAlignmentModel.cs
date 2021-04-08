using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    public class DimsAlignmentModel : ValidatableBase
    {
        static DimsAlignmentModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public DimsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            ParameterBase param,
            IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator,
            IAnnotator<AlignmentSpotProperty, MSDecResult> textDBAnnotator) {

            alignmentFile = alignmentFileBean;
            fileName = alignmentFileBean.FileName;
            resultFile = alignmentFileBean.FilePath;
            eicFile = alignmentFileBean.EicFilePath;
            spectraFile = alignmentFileBean.SpectraFilePath;

            this.Parameter = param;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            Container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(resultFile);

            ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop)));
            MsdecResultsReader.GetSeekPointers(alignmentFileBean.SpectraFilePath, out _, out seekPointers, out _);

            var mzAxis = new ContinuousAxisManager
            {
                MinValue = MassMin,
                MaxValue = MassMax,
                ChartMargin = new Graphics.Core.Base.ChartMargin
                {
                    Left = 0.05, Right = 0.05
                },
            };

            var kmdAxis = new ContinuousAxisManager
            {
                MinValue = -0.5,
                MaxValue = 0.5,
                ChartMargin = new Graphics.Core.Base.ChartMargin
                {
                    Left = 0.05, Right = 0.05
                },
            };

            PlotModel = new AlignmentPeakPlotModel(Ms1Spots, mzAxis, kmdAxis);
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

        public MSDecResult MsdecResult => msdecResult;
        private MSDecResult msdecResult = null;

        public ParameterBase Parameter { get; }

        private readonly List<long> seekPointers = new List<long>();
        private readonly string resultFile = string.Empty;
        private readonly string eicFile = string.Empty;
        private readonly string spectraFile = string.Empty;

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
                LoadBarItemsAsync(target, token),
                LoadEicAsync(target, token),
                LoadMs2SpectrumAsync(target, token),
                LoadMs2ReferenceAsync(target, token)
            ).ConfigureAwait(false);
        }

        public List<BarItem> BarItems {
            get => barItems;
            set => SetProperty(ref barItems, value);
        }
        private List<BarItem> barItems = new List<BarItem>();

        async Task LoadBarItemsAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            var barItems = new List<BarItem>();
            if (target != null) {
                // TODO: Implement other features (PeakHeight, PeakArea, Normalized PeakHeight, Normalized PeakArea)
                barItems = await Task.Run(() =>
                    target.AlignedPeakProperties
                    .GroupBy(peak => Parameter.FileID_ClassName[peak.FileID])
                    .Select(pair => new BarItem { Class = pair.Key, Height = pair.Average(peak => peak.PeakHeightTop) })
                    .ToList(), token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            BarItems = barItems;
        }

        public List<Chromatogram> EicChromatograms {
            get => eicChromatograms;
            set {
                if (SetProperty(ref eicChromatograms, value)) {
                    OnPropertyChanged(nameof(EicMax));
                    OnPropertyChanged(nameof(EicMin));
                    OnPropertyChanged(nameof(IntensityMax));
                    OnPropertyChanged(nameof(IntensityMin));
                }
            }
        }
        private List<Chromatogram> eicChromatograms;

        public double EicMax => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Max(peak => peak?.Time) ?? 0;
        public double EicMin => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Min(peak => peak?.Time) ?? 0;
        public double IntensityMax => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Max(peak => peak?.Intensity) ?? 0;
        public double IntensityMin => EicChromatograms?.SelectMany(chrom => chrom.Peaks).DefaultIfEmpty().Min(peak => peak?.Intensity) ?? 0;

        async Task LoadEicAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            var eicChromatograms = new List<Chromatogram>();
            if (target != null) {
                // maybe using file pointer is better
                eicChromatograms = await Task.Run(() => {
                    var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
                    var chroms = new List<Chromatogram>(spotinfo.PeakInfos.Count);
                    foreach (var peakinfo in spotinfo.PeakInfos) {
                        var items = peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList();
                        var peakitems = items.Where(item => peakinfo.ChromXsLeft.Value <= item.Time && item.Time <= peakinfo.ChromXsRight.Value).ToList();
                        chroms.Add(new Chromatogram
                        {
                            Class = Parameter.FileID_ClassName[peakinfo.FileID],
                            Peaks = items,
                            PeakArea = peakitems,
                        });
                    }
                    return chroms;
                }, token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            EicChromatograms = eicChromatograms;
        }

        public List<SpectrumPeak> Ms2Spectrum {
            get => ms2Spectrum;
            set {
                if (SetProperty(ref ms2Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMin));
                    OnPropertyChanged(nameof(Ms2MassMax));
                }
            }
        }
        private List<SpectrumPeak> ms2Spectrum = new List<SpectrumPeak>();

        public List<SpectrumPeak> Ms2ReferenceSpectrum {
            get => ms2ReferenceSpectrum;
            set {
                if (SetProperty(ref ms2ReferenceSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMin));
                    OnPropertyChanged(nameof(Ms2MassMax));
                }
            }
        }
        private List<SpectrumPeak> ms2ReferenceSpectrum = new List<SpectrumPeak>();

        public double Ms2MassMin => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).DefaultIfEmpty().Min(peak => peak?.Mass) ?? 0;
        public double Ms2MassMax => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).DefaultIfEmpty().Max(peak => peak?.Mass) ?? 0;

        async Task LoadMs2SpectrumAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            var ms2Spectrum = new List<SpectrumPeak>();
            if (target != null) {
                await Task.Run(() => {
                    var idx = this.ms1Spots.IndexOf(target);
                    msdecResult = MsdecResultsReader.ReadMSDecResult(spectraFile, seekPointers[idx]);
                    ms2Spectrum = msdecResult.Spectrum;
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2Spectrum = ms2Spectrum;
        }

        async Task LoadMs2ReferenceAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            var ms2ReferenceSpectrum = new List<SpectrumPeak>();
            if (target != null) {
                await Task.Run(() => {
                    var representative = RetrieveMspMatchResult(target.innerModel);
                    if (representative == null)
                        return;

                    var reference = mspAnnotator.Refer(representative);
                    if (reference != null) {
                        token.ThrowIfCancellationRequested();
                        ms2ReferenceSpectrum = reference.Spectrum;
                    }
                }).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2ReferenceSpectrum = ms2ReferenceSpectrum;
        }

        MsScanMatchResult RetrieveMspMatchResult(AlignmentSpotProperty prop) {
            if (prop.MatchResults?.Representative is MsScanMatchResult representative) {
                if ((representative.Priority & (DataBasePriority.Unknown | DataBasePriority.Manual)) == (DataBasePriority.Unknown | DataBasePriority.Manual))
                    return null;
                if (prop.MatchResults.TextDbBasedMatchResults.Contains(representative)) {
                    return null;
                }
                if ((representative.Priority & DataBasePriority.Unknown) == DataBasePriority.None) {
                    return representative;
                }
            }
            return prop.MspBasedMatchResult;
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
    }
}
