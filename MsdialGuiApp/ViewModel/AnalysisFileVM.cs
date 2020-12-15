using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using NSSplash;
using NSSplash.impl;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.MSDec;

namespace CompMs.App.Msdial.ViewModel
{
    class AnalysisFileVM : ViewModelBase {
        #region Property
        public ICollectionView Ms1Peaks {
            get => ms1Peaks;
            set {
                var old = ms1Peaks;
                if (SetProperty(ref ms1Peaks, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Peaks != null) ms1Peaks.Filter += PeakFilter;
                }
            }
        }

        public List<ChromatogramPeakWrapper> Eic {
            get => eic;
            set {
                if (SetProperty(ref eic, value)) {
                    OnPropertyChanged(nameof(EicMaxIntensity));
                }
            }
        }

        public List<ChromatogramPeakWrapper> PeakEic {
            get => peakEic;
            set => SetProperty(ref peakEic, value);
        }

        public List<ChromatogramPeakWrapper> FocusedEic {
            get => focusedEic;
            set => SetProperty(ref focusedEic, value);
        }

        public List<SpectrumPeakWrapper> Ms1Spectrum {
            get => ms1Spectrum;
            set {
                if (SetProperty(ref ms1Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms1SpectrumMaxIntensity));
                }
            }
        }

        public List<SpectrumPeakWrapper> Ms2Spectrum {
            get => ms2Spectrum;
            set {
                if (SetProperty(ref ms2Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }

        public List<SpectrumPeakWrapper> Ms2ReferenceSpectrum {
            get => ms2ReferenceSpectrum;
            set {
                if (SetProperty(ref ms2ReferenceSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }

        public List<SpectrumPeakWrapper> Ms2DeconvolutionSpectrum {
            get => ms2DeconvolutionSpectrum;
            set {
                if (SetProperty(ref ms2DeconvolutionSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }

        public List<ChromatogramPeakFeature> Peaks { get; }

        public ChromatogramPeakFeatureVM Target {
            get => target;
            set => SetProperty(ref target, value);
        }

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        public string Ms1SplashKey {
            get => ms1SplashKey;
            set => SetProperty(ref ms1SplashKey, value);
        }

        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }

        public string DeconvolutionSplashKey {
            get => deconvolutionSplashKey;
            set => SetProperty(ref deconvolutionSplashKey, value);
        }

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }

        public bool RefMatchedChecked {
            get => refMatchedChecked;
            set => SetProperty(ref refMatchedChecked, value);
        }

        public bool SuggestedChecked {
            get => suggestedChecked;
            set => SetProperty(ref suggestedChecked, value);
        }

        public bool UnknownChecked {
            get => unknownChecked;
            set => SetProperty(ref unknownChecked, value);
        }

        public bool CcsChecked {
            get => ccsChecked;
            set => SetProperty(ref ccsChecked, value);
        }

        public bool MsmsAcquiredChecked {
            get => msmsAcquiredChecked;
            set => SetProperty(ref msmsAcquiredChecked, value);
        }

        public bool MolecularIonChecked {
            get => molecularIonChecked;
            set => SetProperty(ref molecularIonChecked, value);
        }

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }

        public double AmplitudeUpperValue {
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }

        public double EicMaxIntensity => Eic.Select(peak => peak.Intensity).DefaultIfEmpty().Max();
        public double Ms1SpectrumMaxIntensity => Ms1Spectrum.Select(peak => peak.Intensity).DefaultIfEmpty().Max();
        public double Ms2MassMin => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DeconvolutionSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Min();
        public double Ms2MassMax => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DeconvolutionSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Max();
        public double ChromMin => _ms1Peaks.Min(peak => peak.ChromXValue) ?? 0;
        public double ChromMax => _ms1Peaks.Max(peak => peak.ChromXValue) ?? 0;
        public double MassMin => _ms1Peaks.Min(peak => peak.Mass);
        public double MassMax => _ms1Peaks.Max(peak => peak.Mass);
        public double Ms1Tolerance => param.CentroidMs1Tolerance;
        #endregion

        #region Field
        private List<RawSpectrum> spectrumList;
        private List<MSDecResult> msdecResults;
        private ICollectionView ms1Peaks;
        private List<ChromatogramPeakWrapper> eic, peakEic, focusedEic;
        private List<SpectrumPeakWrapper> ms1Spectrum, ms2Spectrum, ms2ReferenceSpectrum, ms2DeconvolutionSpectrum;
        private ChromatogramPeakFeatureVM target;
        private ParameterBase param;
        private IReadOnlyList<MoleculeMsReference> msps;
        private ObservableCollection<ChromatogramPeakFeatureVM> _ms1Peaks;
        private string fileName, ms1SplashKey, rawSplashKey, deconvolutionSplashKey;
        private string displayLabel;
        private bool refMatchedChecked = true, suggestedChecked = true, unknownChecked = true, ccsChecked = false, msmsAcquiredChecked = false, molecularIonChecked = false;
        private double amplitudeLowerValue = 0d, amplitudeUpperValue = 1d;
        #endregion

        public AnalysisFileVM(AnalysisFileBean analysisFileBean, ParameterBase param, IReadOnlyList<MoleculeMsReference> msps) {
            this.param = param;
            this.msps = msps;

            FileName = analysisFileBean.AnalysisFileName;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFileBean.PeakAreaBeanInformationFilePath);
            _ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureVM>(
                peaks.Select(peak => new ChromatogramPeakFeatureVM(peak))
            );
            Peaks = peaks;
            Ms1Peaks = CollectionViewSource.GetDefaultView(_ms1Peaks);

            using (var access = new RawDataAccess(analysisFileBean.AnalysisFilePath, 0, true, analysisFileBean.RetentionTimeCorrectionBean.PredictedRt)) {
                RawMeasurement rawObj = null;
                foreach (var i in Enumerable.Range(0, 5)) {
                    rawObj = DataAccess.GetRawDataMeasurement(access);
                    if (rawObj != null) break;
                    Thread.Sleep(2000);
                }
                if (rawObj == null) {
                    throw new FileLoadException($"Loading {analysisFileBean.AnalysisFilePath} failed.");
                }
                spectrumList = rawObj.SpectrumList;
            }

            msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFileBean.DeconvolutionFilePath, out _, out _);

            PropertyChanged += OnTargetChanged;
            PropertyChanged += OnFilterChanged;

            Target = _ms1Peaks.FirstOrDefault();
        }

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureVM peak) {
                return AnnotationFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!MsmsAcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureVM peak) {
            if (!(RefMatchedChecked || SuggestedChecked || UnknownChecked || CcsChecked)) return true;
            return RefMatchedChecked && peak.IsRefMatched
                || SuggestedChecked && peak.IsSuggested
                || UnknownChecked && peak.IsUnknown
                || CcsChecked && peak.IsCcsMatch;
        }

        bool AmplitudeFilter(ChromatogramPeakFeatureVM peak) {
            return AmplitudeLowerValue <= peak.AmplitudeScore && peak.AmplitudeScore <= AmplitudeUpperValue;
        }

        void OnFilterChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(RefMatchedChecked)
                || e.PropertyName == nameof(SuggestedChecked)
                || e.PropertyName == nameof(UnknownChecked)
                || e.PropertyName == nameof(CcsChecked)
                || e.PropertyName == nameof(MsmsAcquiredChecked)
                || e.PropertyName == nameof(MolecularIonChecked)
                || e.PropertyName == nameof(AmplitudeLowerValue)
                || e.PropertyName == nameof(AmplitudeUpperValue))
                Ms1Peaks?.Refresh();
        }

        void OnTargetChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Target)) {
                if (Target == null) {
                    Eic = new List<ChromatogramPeakWrapper>();
                }

                Eic = DataAccess.GetSmoothedPeaklist(
                    DataAccess.GetMs1Peaklist(
                        spectrumList, Target.Mass, param.CentroidMs1Tolerance, param.IonMode,
                        ChromXType.RT, ChromXUnit.Min,  // TODO: hard coded for LC
                        param.RetentionTimeBegin, param.RetentionTimeEnd
                        ),
                    param.SmoothingMethod, param.SmoothingLevel
                ).Select(peak => new ChromatogramPeakWrapper(peak)).DefaultIfEmpty().ToList();

                PeakEic = Eic.Where(peak => Target.ChromXLeftValue <= peak.ChromXValue && peak.ChromXValue <= Target.ChromXRightValue).ToList();
                FocusedEic = Target.ChromXValue.HasValue
                    ?  new List<ChromatogramPeakWrapper> {
                        Eic.Where(peak => peak.ChromXValue.HasValue)
                           .DefaultIfEmpty()
                           .Argmin(peak => Math.Abs(Target.ChromXValue.Value - peak.ChromXValue.Value))
                    }
                    : new List<ChromatogramPeakWrapper>();

                var spectra = DataAccess.GetCentroidMassSpectra(spectrumList, param.MSDataType, Target.MS1RawSpectrumIdTop, 0, float.MinValue, float.MaxValue);
                Ms1Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                Ms1SplashKey = CalculateSplashKey(spectra);

                spectra = DataAccess.GetCentroidMassSpectra(spectrumList, param.MS2DataType, Target.MS2RawSpectrumId, 0, float.MinValue, float.MaxValue);
                Ms2Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                RawSplashKey = CalculateSplashKey(spectra);

                var msdecResult = msdecResults.FirstOrDefault(dec => dec.ScanID == Target.InnerModel.PeakID);
                Ms2DeconvolutionSpectrum = msdecResult?.Spectrum.Select(peak => new SpectrumPeakWrapper(peak)).ToList() ?? new List<SpectrumPeakWrapper>();
                DeconvolutionSplashKey = CalculateSplashKey(msdecResult?.Spectrum);

                Ms2ReferenceSpectrum = new List<SpectrumPeakWrapper>();
                if (Target.TextDbBasedMatchResult == null && Target.MspBasedMatchResult is MsScanMatchResult matched) {
                    var reference = msps[matched.LibraryIDWhenOrdered];
                    if (matched.LibraryID != reference.ScanID) {
                        reference = msps.FirstOrDefault(msp => msp.ScanID == matched.LibraryID);
                    }
                    Ms2ReferenceSpectrum = reference?.Spectrum.Select(peak => new SpectrumPeakWrapper(peak)).ToList() ?? new List<SpectrumPeakWrapper>();
                }
            }
        }

        static string CalculateSplashKey(IReadOnlyCollection<SpectrumPeak> spectra) {
            if (spectra.IsEmptyOrNull() || spectra.Count <= 2 && spectra.All(peak => peak.Intensity == 0))
                return "N/A";
            var msspectrum = new MSSpectrum(string.Join(" ", spectra.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
            return new Splash().splashIt(msspectrum);
        }
    }

    public class SpectrumPeakWrapper
    {
        public double Intensity => innerModel.Intensity;
        public double Mass => innerModel.Mass;

        private SpectrumPeak innerModel;
        public SpectrumPeakWrapper(SpectrumPeak peak) {
            innerModel = peak;
        }
    }

    public class ChromatogramPeakWrapper
    {
        public double Intensity => innerModel.Intensity;
        public double? ChromXValue => innerModel.ChromXs?.Value;

        private ChromatogramPeak innerModel;
        public ChromatogramPeakWrapper(ChromatogramPeak peak) {
            innerModel = peak;
        }
    }

}
