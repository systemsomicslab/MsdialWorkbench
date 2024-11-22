using CompMs.Common.Components;

namespace CompMs.Common.DataObj
{
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
            this.Spectrum = [];
        }

        public bool IsInDriftTimeRange(DriftTime start, DriftTime end) {
            return start.Value <= DriftTime && DriftTime <= end.Value;
        }

        public RawSpectrum ShallowCopy() {
            return (RawSpectrum)MemberwiseClone();
        }
    }
}
