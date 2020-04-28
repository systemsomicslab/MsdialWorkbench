using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

    public class RawMeasurement {
        public RawSourceFileInfo SourceFileInfo { get; set; }
        public RawSample Sample { get; set; }
        public MeasurmentMethod Method { get; set; }
        public RawCalibrationInfo CalibrantInfo { get; set; }
        public List<RawChromatogram> ChromatogramList { get; set; }
        public List<RawSpectrum> SpectrumList { get; set; }
        public List<RawSpectrum> AccumulatedSpectrumList { get; set; }

        public RawMeasurement()
        {
            SourceFileInfo = new RawSourceFileInfo();
            Sample = new RawSample();
            CalibrantInfo = new RawCalibrationInfo();

            ChromatogramList = new List<RawChromatogram>();
            SpectrumList = new List<RawSpectrum>();
            AccumulatedSpectrumList = new List<RawSpectrum>();
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
        public int Index { get; set; }
        public string Id { get; set; }
        public int ScanNumber { get; set; }
        public int DriftScanNumber { get; set; }
        public int DefaultArrayLength { get; set; }
        public int OriginalIndex { get; set; } // for ionmobility

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

        public double CollisionEnergy { get; set; } // for MSE, all ions

        public RawPrecursorIon Precursor { get; set; }
        public RawProductIon Product { get; set; }

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
            this.Spectrum = new RawPeakElement[] { };
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
}
