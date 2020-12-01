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

namespace CompMs.App.Msdial.ViewModel
{
    class AnalysisFileVM : ViewModelBase
    {
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

        public List<SpectrumPeakWrapper> Ms1Spectrum {
            get => ms1Spectrum;
            set {
                if (SetProperty(ref ms1Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms1SpectrumMaxIntensity));
                }
            }
        }

        public ChromatogramPeakFeatureWrapper Target {
            get => target;
            set => SetProperty(ref target, value);
        }

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }

        public string SplashKey {
            get => splashKey;
            set => SetProperty(ref splashKey, value);
        }

        public double EicMaxIntensity => Eic.Select(peak => peak.Intensity).DefaultIfEmpty().Max();
        public double Ms1SpectrumMaxIntensity => Ms1Spectrum.Select(peak => peak.Intensity).DefaultIfEmpty().Max();
        public double Ms1Tolerance => param.CentroidMs1Tolerance;
        #endregion

        #region Field
        private ICollectionView ms1Peaks;
        private List<RawSpectrum> spectrumList;
        private List<ChromatogramPeakWrapper> eic;
        private List<SpectrumPeakWrapper> ms1Spectrum;
        private ChromatogramPeakFeatureWrapper target;
        private ParameterBase param;
        private ObservableCollection<ChromatogramPeakFeatureWrapper> _ms1Peaks;
        private string fileName, splashKey;
        #endregion

        public AnalysisFileVM(AnalysisFileBean analysisFileBean, ParameterBase param) {
            this.param = param;

            FileName = analysisFileBean.AnalysisFileName;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFileBean.PeakAreaBeanInformationFilePath);
            _ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureWrapper>(
                peaks.Select(peak => new ChromatogramPeakFeatureWrapper(peak))
            );
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

            PropertyChanged += OnTargetChanged;

            Target = _ms1Peaks.FirstOrDefault();
        }

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureWrapper peak) {
                return peak.IsRefMatched || peak.IsSuggested || peak.IsUnknown;
            }
            return false;
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

                var spectra = DataAccess.GetCentroidMassSpectra(spectrumList, param.MSDataType, Target.MS1RawSpectrumIdTop, 0, float.MinValue, float.MaxValue);
                Ms1Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();

                if (spectra.IsEmptyOrNull() || spectra.Count <= 2 && spectra.All(peak => peak.Intensity == 0)) {
                    SplashKey = "N/A";
                }
                else {
                    var msspectrum = new MSSpectrum(string.Join(" ", spectra.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
                    SplashKey = new Splash().splashIt(msspectrum);
                }
            }
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

    class ChromatogramPeakFeatureWrapper
    {
        public double? ChromXValue => innerModel.ChromXs.Value;
        public double Mass => innerModel.Mass;
        public int MS1RawSpectrumIdTop => innerModel.MS1RawSpectrumIdTop;

        public bool IsRefMatched => innerModel.IsReferenceMatched;
        public bool IsSuggested => innerModel.IsAnnotationSuggested;
        public bool IsUnknown => innerModel.IsUnknown;
        public ChromatogramPeakFeature InnerModel => innerModel;

        private ChromatogramPeakFeature innerModel;
        public ChromatogramPeakFeatureWrapper(ChromatogramPeakFeature feature) {
            innerModel = feature;
        }
    }

}
