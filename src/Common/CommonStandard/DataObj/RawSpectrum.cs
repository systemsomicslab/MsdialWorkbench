using CompMs.Common.Components;
using System;
using System.Collections.Generic;

namespace CompMs.Common.DataObj;

public class RawSpectrum
{
    /// <summary>
    /// Gets or sets the identifier for the RawSpectrum, ensuring its uniqueness.
    /// </summary>
    /// <remarks>
    /// This property serves as a unique identifier to ensure the uniqueness of a RawSpectrum. 
    /// When the same raw data is loaded, the same RawSpectrum must always have the same RawSpectrumID.
    /// This ID may be duplicated across different files.
    /// It is not guaranteed that IDs are sequential, and using this property as an index is prohibited.
    /// Typically, the setter for this property is not used for any purpose other than initialization.
    /// </remarks>
    public ISpectrumIdentifier? RawSpectrumID { get; set; }
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="accumulatedMassBin">key: m/z * 100000, value [0] base peak m/z [1] summed intensity [2] base peak intensity</param>
    /// <param name="needsSortMz"></param>
    public void SetSpectrumProperties(Dictionary<int, double[]> accumulatedMassBin) {
        var basepeakIntensity = 0.0;
        var basepeakMz = 0.0;
        var totalIonCurrnt = 0.0;
        var lowestMz = double.MaxValue;
        var highestMz = double.MinValue;
        var minIntensity = double.MaxValue;

        var spectrum = new RawPeakElement[accumulatedMassBin.Count];
        var idx = 0;

        foreach (var pair in accumulatedMassBin) {
            var pBasepeakMz = pair.Value[0];
            var pSummedIntensity = pair.Value[1];
            var pBasepeakIntensity = pair.Value[2];

            totalIonCurrnt += pSummedIntensity;

            if (pSummedIntensity > basepeakIntensity) {
                basepeakIntensity = pSummedIntensity;
                basepeakMz = pBasepeakMz;
            }
            if (lowestMz > pBasepeakMz) lowestMz = pBasepeakMz;
            if (highestMz < pBasepeakMz) highestMz = pBasepeakMz;
            if (minIntensity > pSummedIntensity) minIntensity = pSummedIntensity;

            var spec = new RawPeakElement() {
                Mz = Math.Round(pBasepeakMz, 5),
                Intensity = Math.Round(pSummedIntensity, 0)
            };
            spectrum[idx++] = spec;
        }
        Array.Sort(spectrum, MzComparer.Instance);

        Spectrum = spectrum;
        DefaultArrayLength = spectrum.Length;
        BasePeakIntensity = basepeakIntensity;
        BasePeakMz = basepeakMz;
        TotalIonCurrent = totalIonCurrnt;
        LowestObservedMz = lowestMz;
        HighestObservedMz = highestMz;
        MinIntensity = minIntensity;
    }

    class MzComparer : IComparer<RawPeakElement>
    {
        public static readonly MzComparer Instance = new();
        public int Compare(RawPeakElement x, RawPeakElement y) => x.Mz.CompareTo(y.Mz);
    }

}
