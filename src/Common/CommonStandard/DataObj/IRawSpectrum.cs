namespace CompMs.Common.DataObj;

public interface IRawSpectrum
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
    int RawSpectrumID { get; set; }
    int Index { get; set; } // for raw data parser
    string Id { get; set; } // for raw data parser
    int ScanNumber { get; set; } // for index accessing rawspectrum object
    int DriftScanNumber { get; set; }
    int DefaultArrayLength { get; set; }
    int OriginalIndex { get; set; } // for ionmobility. The accumulated MS1 object should have this to access original scan nmuber

    int MsLevel { get; set; }
    ScanPolarity ScanPolarity { get; set; }
    SpectrumRepresentation SpectrumRepresentation { get; set; }
    double BasePeakIntensity { get; set; }
    double BasePeakMz { get; set; }
    double MinIntensity { get; set; }
    double TotalIonCurrent { get; set; }
    double LowestObservedMz { get; set; }
    double HighestObservedMz { get; set; }

    double ScanStartTime { get; set; }
    double DriftTime { get; set; }
    Units ScanStartTimeUnit { get; set; }
    Units DriftTimeUnit { get; set; }
    double ScanWindowLowerLimit { get; set; }
    double ScanWindowUpperLimit { get; set; }
    double SourceFragmentationInfoCount { get; set; }

    double CollisionEnergy { get; set; } // for MSE, all ions
    int ExperimentID { get; set; } 

    RawPrecursorIon Precursor { get; set; }
    RawProductIon Product { get; set; }

    MaldiFrameInfo MaldiFrameInfo { get; set; }

    RawPeakElement[] Spectrum { get; set; }
}
