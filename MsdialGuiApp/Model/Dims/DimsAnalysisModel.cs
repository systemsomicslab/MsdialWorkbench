using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using NSSplash;
using NSSplash.impl;
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
    public class DimsAnalysisModel : ValidatableBase
    {
        public DimsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            ParameterBase parameter,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> textDBAnnotator) {

            this.provider = provider;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            AnalysisFile = analysisFile;
            FileName = analysisFile.AnalysisFileName;
            peakAreaFile = analysisFile.PeakAreaBeanInformationFilePath;
            deconvolutionFile = analysisFile.DeconvolutionFilePath;
            Parameter = parameter;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(peakAreaFile);
            ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>(
                peaks.Select(peak => new ChromatogramPeakFeatureModel(peak, parameter.TargetOmics != TargetOmics.Metabolomics)));
            Peaks = peaks;

            MsdecResultsReader.GetSeekPointers(deconvolutionFile, out _, out seekPointers, out _);

            HorizontalAxis = new AxisData(new ContinuousAxisManager<double>(MassMin, MassMax), "Mass", "m/z");
            VerticalAxis = new AxisData(new ContinuousAxisManager<double>(-0.5, 0.5), "KMD", "Kendrick mass defect");
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, HorizontalAxis.Axis, VerticalAxis.Axis);

            var abundanceAxis = new AxisData(
                    /*
                    new DependencyContinuousAxisManager
                    {
                        TargetAxisMapper = HorizontalAxis.Axis,
                        TargetPropertyName = "ChromXValue",
                        ValuePropertyName = "Intensity",
                        TargetRange = HorizontalAxis.Axis.Range,
                        ChartMargin = new ChartMargin(0, 0.05),
                        Bounds = new Range(0d, 0d),
                    },
                    */
                    new ContinuousAxisManager<double>(0, 1),
                    "Intensity",
                    "Abundance");
            var massAxis = new AxisData(HorizontalAxis.Axis, "ChromXValue", "m/z");

            EicModel = new EicModel(
                massAxis,
                abundanceAxis,
                new DimsEicLoader(
                    provider,
                    Parameter,
                    ChromXType.Mz,
                    ChromXUnit.Mz,
                    Parameter.MassRangeBegin,
                    Parameter.MassRangeEnd)
            );
            EicModel.PropertyChanged += OnEicChanged;
        }

        private readonly string peakAreaFile;
        private readonly string deconvolutionFile;
        private readonly List<long> seekPointers;
        private readonly IDataProvider provider;

        public AnalysisFileBean AnalysisFile { get; }
        public ParameterBase Parameter { get; }

        public IAnnotator<ChromatogramPeakFeature, MSDecResult> MspAnnotator => mspAnnotator;
        public IAnnotator<ChromatogramPeakFeature, MSDecResult> TextDBAnnotator => textDBAnnotator;

        private readonly IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator, textDBAnnotator;

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks => ms1Peaks;
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>();

        public double MassMin => Ms1Peaks.DefaultIfEmpty().Min(peak => peak.Mass);
        public double MassMax => Ms1Peaks.DefaultIfEmpty().Max(peak => peak.Mass);

        public List<ChromatogramPeakFeature> Peaks { get; } = new List<ChromatogramPeakFeature>();

        public ChromatogramPeakFeatureModel Target {
            get => target;
            set {
                if (SetProperty(ref target, value)) {
                    _ = OnTargetChangedAsync(target);
                }
            }
        }
        private ChromatogramPeakFeatureModel target;

        public AxisData HorizontalAxis {
            get => horizontalAxis;
            set => SetProperty(ref horizontalAxis, value);
        }
        private AxisData horizontalAxis;

        public AxisData VerticalAxis {
            get => verticalAxis;
            set => SetProperty(ref verticalAxis, value);
        }
        private AxisData verticalAxis;

        public AnalysisPeakPlotModel PlotModel {
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
        private AnalysisPeakPlotModel plotModel;

        private void OnPlotModelTargetChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(PlotModel.Target)) {
                Target = PlotModel.Target;
            }
        }

        public EicModel EicModel { get; }

        private void OnEicChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(EicModel.Eic)) {
                var axis = EicModel.VerticalData;
                var type = typeof(ChromatogramPeakWrapper);
                var prop = type.GetProperty(axis.Property);
                EicModel.VerticalData = new AxisData(
                    ContinuousAxisManager<double>.Build(EicModel.Eic, peak => (double)prop.GetValue(peak), 0, 0),
                    axis.Property,
                    axis.Title);
            }
        }

        private CancellationTokenSource cts;
        public async Task OnTargetChangedAsync(ChromatogramPeakFeatureModel target) {
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

        async Task OnTargetChangedAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            await Task.WhenAll(
                LoadMs1SpectrumAsync(target, token),
                EicModel.LoadEicAsync(target, token),
                LoadMs2SpectrumAsync(target, token),
                LoadMs2DecSpectrumAsync(target, token),
                LoadMs2ReferenceAsync(target, token)
            ).ConfigureAwait(false);
        }

        public List<SpectrumPeak> Ms1Spectrum {
            get => ms1Spectrum;
            set {
                if (SetProperty(ref ms1Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms1SpectrumMaxIntensity));
                }
            }
        }
        private List<SpectrumPeak> ms1Spectrum = new List<SpectrumPeak>();

        public double Ms1SpectrumMaxIntensity => Ms1Spectrum.Select(peak => peak.Intensity).DefaultIfEmpty().Max();

        public string Ms1SplashKey {
            get => ms1SplashKey;
            set => SetProperty(ref ms1SplashKey, value);
        }
        private string ms1SplashKey = string.Empty;

        async Task LoadMs1SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms1Spectrum = new List<SpectrumPeak>();
            var ms1SplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS1RawSpectrumIdTop < 0) {
                        return;
                    }
                    ms1Spectrum = DataAccess.GetCentroidMassSpectra(provider.LoadMs1Spectrums()[target.MS1RawSpectrumIdTop], Parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                    ms1SplashKey = CalculateSplashKey(ms1Spectrum);
                }, token);
            }

            token.ThrowIfCancellationRequested();
            Ms1Spectrum = ms1Spectrum;
            Ms1SplashKey = ms1SplashKey;
        }

        public List<SpectrumPeak> Ms2Spectrum {
            get => ms2Spectrum;
            set {
                if (SetProperty(ref ms2Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeak> ms2Spectrum;

        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }
        private string rawSplashKey = string.Empty;

        public List<SpectrumPeak> Ms2DecSpectrum {
            get => ms2DecSpectrum;
            set {
                if (SetProperty(ref ms2DecSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeak> ms2DecSpectrum;

        public string DeconvolutionSplashKey {
            get => deconvolutionSplashKey;
            set => SetProperty(ref deconvolutionSplashKey, value);
        }
        private string deconvolutionSplashKey = string.Empty;

        public List<SpectrumPeak> Ms2ReferenceSpectrum {
            get => ms2ReferenceSpectrum;
            set {
                if (SetProperty(ref ms2ReferenceSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeak> ms2ReferenceSpectrum;

        public double Ms2MassMin => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DecSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Min();
        public double Ms2MassMax => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DecSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Max();

        async Task LoadMs2SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms2Spectrum = new List<SpectrumPeak>(); 
            var rawSplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS2RawSpectrumId < 0)
                        return;
                    var spectra = DataAccess.GetCentroidMassSpectra(provider.LoadMsSpectrums()[target.MS2RawSpectrumId], Parameter.MS2DataType, 0, float.MinValue, float.MaxValue);
                    if (Parameter.RemoveAfterPrecursor)
                        spectra = spectra.Where(peak => peak.Mass <= target.Mass + Parameter.KeptIsotopeRange).ToList();
                    token.ThrowIfCancellationRequested();
                    ms2Spectrum = spectra;
                    rawSplashKey = CalculateSplashKey(spectra);
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2Spectrum = ms2Spectrum;
            RawSplashKey = rawSplashKey;
        }

        public MSDecResult MsdecResult => msdecResult;
        private MSDecResult msdecResult = null;

        async Task LoadMs2DecSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms2DecSpectrum = new List<SpectrumPeak>();
            var deconvolutionSplashKey = string.Empty;

            if (target != null) {
                await Task.Run((Action)(() => {
                    var idx = this.Ms1Peaks.IndexOf(target);
                    msdecResult = MsdecResultsReader.ReadMSDecResult(deconvolutionFile, seekPointers[(int)idx]);
                    token.ThrowIfCancellationRequested();
                    ms2DecSpectrum = msdecResult.Spectrum;
                    deconvolutionSplashKey = CalculateSplashKey(msdecResult.Spectrum);
                }), token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2DecSpectrum = ms2DecSpectrum;
            DeconvolutionSplashKey = deconvolutionSplashKey;
        }

        async Task LoadMs2ReferenceAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms2ReferenceSpectrum = new List<SpectrumPeak>();

            if (target != null) {
                await Task.Run(() => {
                    var representative = RetrieveMspMatchResult(target.InnerModel);
                    if (representative == null)
                        return;

                    var reference = mspAnnotator.Refer(representative);
                    if (reference != null) {
                        token.ThrowIfCancellationRequested();
                        ms2ReferenceSpectrum = reference.Spectrum;
                    }
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2ReferenceSpectrum = ms2ReferenceSpectrum;
        }

        MsScanMatchResult RetrieveMspMatchResult(ChromatogramPeakFeature prop) {
            if (prop.MatchResults?.Representative is MsScanMatchResult representative) {
                if ((representative.Source & (SourceType.Unknown | SourceType.Manual)) == (SourceType.Unknown | SourceType.Manual))
                    return null;
                if (prop.MatchResults.TextDbBasedMatchResults.Contains(representative)) {
                    return null;
                }
                if ((representative.Source & SourceType.Unknown) == SourceType.None) {
                    return representative;
                }
            }
            return prop.MspBasedMatchResult;
        }

        static string CalculateSplashKey(IReadOnlyList<SpectrumPeak> spectra) {
            if (spectra.IsEmptyOrNull() || spectra.Count <= 2 && spectra.All(peak => peak.Intensity == 0))
                return "N/A";
            var msspectrum = new MSSpectrum(string.Join(" ", spectra.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
            return new Splash().splashIt(msspectrum);
        }

        private static readonly double MzTol = 20;
        public void FocusByMz(IAxisManager axis, double mz) {
            axis?.Focus(mz - MzTol, mz + MzTol);
        }       

        public void FocusById(IAxisManager mzAxis, int id) {
            var focus = Ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == id);
            Target = focus;
            FocusByMz(mzAxis, focus.Mass);
        }

        public void SaveSpectra(string filename) {
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                filename,
                Target.InnerModel,
                msdecResult,
                Parameter);
        }

        public bool CanSaveSpectra() => Target.InnerModel != null && msdecResult != null;
    }
}
