using CompMs.Common.Components;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;

namespace CompMs.Common.DataObj {
    public enum ScanPolarity
    {
        Undefined, Positive, Negative, Alternating
    }

    public enum SpectrumRepresentation
    {
        Undefined, Centroid, Profile
    }

    public enum Units
    {
        Undefined, Second, Minute, Mz, NumberOfCounts, ElectronVolt, Milliseconds, Oneoverk0
    }

    public enum DissociationMethods
    {
        Undefined, CID, PD, PSD, SID, BIRD, ECD, IRMPD, SORI, HCD, LowEnergyCID, MPD, ETD, PQD, InSourceCID, LIFT,
    }

    public enum MeasurmentMethod {
        GCMS, DDA, SWATH, ALLIONS, IONMOBILITY, IM_DDA, IM_ALLIONS, IM_SWATH 
    }

    public class MaldiFrameLaserInfo {
        public int Id { get; set; }
        public string LaserApplicationName { get; set; }
        public string LaserParameterName { get; set; }
        public double LaserBoost { get; set; }
        public double LaserFocus { get; set; }
        public int BeamScan { get; set; }
        public double BeamScanSizeX { get; set; }
        public double BeamScanSizeY { get; set; }
        public int WalkOnSpotMode { get; set; }
        public int WalkOnSpotShots { get; set; }
        public double SpotSize { get; set; }

    }

    public class MaldiFrameInfo {
        public long Frame { get; set; }
        public int Chip { get; set; }
        public string SpotName { get; set; }
        public int RegionNumber { get; set; }
        public int XIndexPos { get; set; } // index
        public int YIndexPos { get; set; } // index
        public float LaserPower { get; set; }
        public int NumLaserShots { get; set; }
        public float LaserRepRate { get; set; }
        public float MotorPositionX { get; set; } //  um
        public float MotorPositionY { get; set; } // um
        public float MotorPositionZ { get; set; } // um
        public int LaserInfo { get; set; } 
    }

    public class RawMeasurement {
        public RawSourceFileInfo SourceFileInfo { get; set; }
        public RawSample Sample { get; set; }
        public MeasurmentMethod Method { get; set; }
        public RawCalibrationInfo CalibrantInfo { get; set; }
        public List<RawChromatogram> ChromatogramList { get; set; }
        public List<RawSpectrum> SpectrumList { get; set; }
        public List<RawSpectrum> AccumulatedSpectrumList { get; set; }
        public List<double> CollisionEnergyTargets { get; set; }
        public MaldiFrameLaserInfo MaldiFrameLaserInfo { get; set; }
        public List<MaldiFrameInfo> MaldiFrames { get; set; }

        public RawMeasurement()
        {
            SourceFileInfo = new RawSourceFileInfo();
            Sample = new RawSample();
            CalibrantInfo = new RawCalibrationInfo();

            ChromatogramList = new List<RawChromatogram>();
            SpectrumList = new List<RawSpectrum>();
            AccumulatedSpectrumList = new List<RawSpectrum>();
            MaldiFrameLaserInfo = new MaldiFrameLaserInfo();

            MaldiFrames = new List<MaldiFrameInfo>();
        }
    }

    public class RawSourceFileInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
    }

    public class RawSample
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IonAnalyzerType { get; set; } // TOF, IT, Orbitrap
        public string InstrumentType { get; set; } // QTOF, QE, FT-ICR
    }

    public class RawCalibrationInfo {
        public bool IsAgilentIM { get; set; } = false;
        public bool IsBrukerIM { get; set; } = false;
        public bool IsWatersIM { get; set; } = false;

        public double AgilentBeta { get; set; } = -1.0;
        public double AgilentTFix { get; set; } = -1.0;
        public double WatersCoefficient { get; set; } = -1.0;
        public double WatersT0 { get; set; } = -1.0;
        public double WatersExponent { get; set; } = -1.0;
    }

    public struct RawChromatogramElement
    {
        public double RtInMin;
        public double Intensity;
    }

    public class RawChromatogram
    {
        public int Index { get; set; }
        public string Id { get; set; }
        public int DefaultArrayLength { get; set; }

        public bool IsSRM { get; set; }
        public RawPrecursorIon Precursor { get; set; }
        public RawProductIon Product { get; set; }

        public RawChromatogramElement[] Chromatogram { get; set; }

        public RawChromatogram()
        {
            this.Index = 0;
            this.Id = null;
            this.DefaultArrayLength = 0;
            this.IsSRM = false;
            this.Precursor = null;
            this.Product = null;
            this.Chromatogram = null;
        }
    }

    public class RawSpectrum
    {
        public int Index { get; set; } // for raw data parser
        public string Id { get; set; } // for raw data parser
        public int ScanNumber { get; set; } // for index accessing rawspectrum object
        public int DriftScanNumber { get; set; }
        public int DefaultArrayLength { get; set; }
        public int OriginalIndex { get; set; } // for ionmobility. The accumulated MS1 object should have this to access original scan nmuber

        public int MsLevel { get; set; }
        public ScanPolarity ScanPolarity { get; set; }
        public SpectrumRepresentation SpectrumRepresentation { get; set; }
        public double BasePeakIntensity { get; set; }
        public double BasePeakMz { get; set; }
        public double MinIntensity { get; set; }
        public double TotalIonCurrent { get; set; }
        public double LowestObservedMz { get; set; }
        public double HighestObservedMz { get; set; }

        public double ScanStartTime { get; set; }
        public double DriftTime { get; set; }
        public Units ScanStartTimeUnit { get; set; }
        public Units DriftTimeUnit { get; set; }
        public double ScanWindowLowerLimit { get; set; }
        public double ScanWindowUpperLimit { get; set; }
        public double SourceFragmentationInfoCount { get; set; }

        public double CollisionEnergy { get; set; } // for MSE, all ions
        public int ExperimentID { get; set; } 

        public RawPrecursorIon Precursor { get; set; }
        public RawProductIon Product { get; set; }

        public MaldiFrameInfo MaldiFrameInfo { get; set; }

        public RawPeakElement[] Spectrum { get; set; }

        public RawSpectrum()
        {
            this.Index = 0;
            this.Id = null;
            this.ScanNumber = 0;
            this.DriftScanNumber = 0;
            this.DefaultArrayLength = 0;
            this.OriginalIndex = 0;
            this.MsLevel = 0;
            this.ScanPolarity = ScanPolarity.Undefined;
            this.SpectrumRepresentation = SpectrumRepresentation.Undefined;
            this.BasePeakIntensity = 0;
            this.BasePeakMz = 0;
            this.TotalIonCurrent = 0;
            this.LowestObservedMz = 0;
            this.HighestObservedMz = 0;
            this.MinIntensity = 0;
            this.ScanStartTime = 0;
            this.DriftTime = 0;
            this.ScanStartTimeUnit = Units.Undefined;
            this.ScanWindowLowerLimit = 0;
            this.ScanWindowUpperLimit = 0;
            this.Precursor = null;
            this.Product = null;
            this.MaldiFrameInfo = null;
            this.Spectrum = new RawPeakElement[] { };
        }

        public bool IsInDriftTimeRange(DriftTime start, DriftTime end) {
            return start.Value <= DriftTime && DriftTime <= end.Value;
        }

        public RawSpectrum ShallowCopy() {
            return (RawSpectrum)MemberwiseClone();
        }
    }

    public class RawPrecursorIon
    {
        public double SelectedIonMz { get; set; }
        public double IsolationTargetMz { get; set; }
        public double IsolationWindowLowerOffset { get; set; }
        public double IsolationWindowUpperOffset { get; set; }
        public DissociationMethods Dissociationmethod { get; set; }
        public double CollisionEnergy { get; set; }
        public Units CollisionEnergyUnit { get; set; }
        public double TimeBegin { get; set; } = 0; // use for diapasef
        public double TimeEnd { get; set; } = 0; // use for diapasef

        public RawPrecursorIon()
        {
            this.SelectedIonMz = 0;
            this.IsolationTargetMz = 0;
            this.IsolationWindowLowerOffset = 0;
            this.IsolationWindowUpperOffset = 0;
            this.Dissociationmethod = DissociationMethods.Undefined;
            this.CollisionEnergy = 0;
            this.CollisionEnergyUnit = Units.Undefined;
        }

        public bool ContainsDriftTime(DriftTime drift) {
            return TimeBegin <= drift.Value && drift.Value < TimeEnd;
        }

        public bool ContainsMz(double mz, double tolerance, AcquisitionType acquisitionType) {
            switch (acquisitionType) {
                case AcquisitionType.AIF:
                case AcquisitionType.SWATH:
                    var lowerOffset = IsolationWindowLowerOffset;
                    var upperOffset = IsolationWindowUpperOffset;
                    return (double)SelectedIonMz - lowerOffset - tolerance < mz && (double)mz < (double)SelectedIonMz + upperOffset + (double)tolerance;
                case AcquisitionType.DDA:
                    return Math.Abs((double)SelectedIonMz - (double)mz) < (double)tolerance;
                default:
                    throw new NotSupportedException(nameof(acquisitionType));
            }
        }

        public bool IsNotDiapasefData => TimeBegin == TimeEnd;

        void DUMP()
        {
            Console.WriteLine("Precursor: SelectMz={0}; IsolationTargetMz={1}; WindowOffset={2} / {3}; Dissociation={4}; CE={5} {6};",
                this.SelectedIonMz, this.IsolationTargetMz, this.IsolationWindowLowerOffset, this.IsolationWindowUpperOffset, this.Dissociationmethod, this.CollisionEnergy, this.CollisionEnergyUnit);
        }
    }

    public class RawProductIon
    {
        public double IsolationTargetMz { get; set; }
        public double IsolationWindowLowerOffset { get; set; }
        public double IsolationWindowUpperOffset { get; set; }

        public RawProductIon()
        {
            this.IsolationTargetMz = 0;
            this.IsolationWindowLowerOffset = 0;
            this.IsolationWindowUpperOffset = 0;
        }

        void DUMP()
        {
            Console.WriteLine("Product: TargetMz={0}; WindowOffset={1} / {2};", this.IsolationTargetMz, this.IsolationWindowLowerOffset, this.IsolationWindowUpperOffset);
        }
    }

    public struct RawPeakElement
    {
        public double Mz;
        public double Intensity;
    }

    public struct Raw2DElement {
        public Raw2DElement(double mz, double drift) {
            Mz = mz;
            Drift = drift;
        }

        public Raw2DElement(ChromXs chrom) {
            Mz = chrom.Mz.Value;
            Drift = chrom.Drift.Value;
        }

        public double Mz;
        public double Drift;
    }

    public sealed class RawSpectraOnPixels {
        // array lengths of xyframes and pixelpeakfeatureslist should be equal.
        public List<MaldiFrameInfo> XYFrames;
        public List<RawPixelFeatures> PixelPeakFeaturesList;
    }

    public sealed class RawPixelFeatures {
        public double Mz;
        public double Drift;
        public double[] IntensityArray;
    }
}
