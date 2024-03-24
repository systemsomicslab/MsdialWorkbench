using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Loader;

/// <summary>
/// Represents a loader for generating MS2 Total Ion Chromatograms (TIC) with applied peak picking parameters.
/// Initializes a new instance of the MS2TicLoader class for loading MS2 total ion chromatograms.
/// </summary>
/// <param name="rawSpectra">The raw spectral data from which to generate the chromatogram.</param>
/// <param name="chromatogramRange">The range within which to generate the chromatogram.</param>
/// <param name="peakPickParameter">The parameters to use for peak picking and smoothing within the chromatogram.</param>
/// <remarks>
/// This loader is designed to process raw spectra data to produce display-ready chromatograms for MS2 scans,
/// applying specified smoothing methods and levels as part of the peak picking process.
/// </remarks>
internal sealed class MS2TicLoader(IRawSpectra rawSpectra, ChromatogramRange chromatogramRange, PeakPickBaseParameter peakPickParameter) : IWholeChromatogramLoader, IWholeChromatogramLoader<int> {
    /// <summary>
    /// Loads and processes an MS2 total ion chromatogram from the provided raw spectra, applying smoothing based on the specified peak picking parameters.
    /// </summary>
    /// <returns>A <see cref="DisplayChromatogram"/> that represents the processed chromatogram, ready for display or further analysis.</returns>
    /// <remarks>
    /// This method applies the smoothing method and level defined in the peak picking parameters to the MS2 TIC,
    /// enhancing the clarity and interpretability of the chromatographic output.
    /// </remarks>
    public DisplayChromatogram LoadChromatogram() {
        var chromatogram = rawSpectra.GetMS2TotalIonChromatogram(chromatogramRange)
            .ChromatogramSmoothing(peakPickParameter.SmoothingMethod, peakPickParameter.SmoothingLevel);
        return new DisplayChromatogram(chromatogram);
    }


    /// <summary>
    /// Loads and processes an MS2 total ion chromatogram for a specific experiment from the provided raw spectra, applying smoothing based on the specified peak picking parameters.
    /// </summary>
    /// <param name="experimentID">The ID of the experiment to filter the MS2 spectra by. Only spectra associated with this experiment ID will be included in the chromatogram.</param>
    /// <returns>A <see cref="DisplayChromatogram"/> that represents the processed chromatogram for the specified experiment, ready for display or further analysis.</returns>
    /// <remarks>
    /// This method extends the functionality of <see cref="LoadChromatogram"/> by adding the capability to filter the raw spectra based on an experiment ID before generating the chromatogram. 
    /// It then applies the same smoothing method and level defined in the peak picking parameters to enhance the clarity and interpretability of the chromatographic output, 
    /// allowing for focused analysis on data from a specific experiment.
    /// </remarks>
    public DisplayChromatogram LoadChromatogram(int experimentID) {
        var chromatogram = rawSpectra.GetMS2TotalIonChromatogram(experimentID, chromatogramRange)
            .ChromatogramSmoothing(peakPickParameter.SmoothingMethod, peakPickParameter.SmoothingLevel);
        return new DisplaySpecificExperimentChromatogram(chromatogram);
    }
}
