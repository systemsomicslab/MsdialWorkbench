using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Loader;

/// <summary>
/// Provides functionality to load a chromatogram of product ions for specific precursor and product m/z ranges
/// within a defined chromatogram range.
/// </summary>
/// <remarks>
/// This class implements the <see cref="IWholeChromatogramLoader{T}"/> interface to load chromatograms
/// based on a tuple of precursor and product m/z ranges. It uses a provided <see cref="RawSpectra"/>
/// to retrieve the actual chromatogram data, converting them into a list of <see cref="PeakItem"/> objects
/// for further processing or visualization.
/// </remarks>
internal sealed class ProductIonChromatogramLoader : IWholeChromatogramLoader<(MzRange Precursor, MzRange Product)>, IWholeChromatogramLoader<(int ExperimentID, MzRange Product)>
{
    private readonly IRawSpectra _rawSpectra;
    private readonly IonMode _ionMode;
    private readonly ChromatogramRange _range;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductIonChromatogramLoader"/> class with specified raw spectra data and chromatogram range.
    /// </summary>
    /// <param name="rawSpectra">The raw spectra data to be used for chromatogram generation.</param>
    /// <param name="ionMode"></param>
    /// <param name="range">The range within which the chromatogram is to be generated.</param>
    public ProductIonChromatogramLoader(IRawSpectra rawSpectra, IonMode ionMode, ChromatogramRange range) {
        _rawSpectra = rawSpectra;
        _ionMode = ionMode;
        _range = range;
    }

    /// <summary>
    /// Loads a chromatogram based on the specified precursor and product m/z ranges within the previously defined chromatogram range.
    /// </summary>
    /// <param name="state">A tuple containing the precursor and product m/z ranges for which the chromatogram is to be loaded.</param>
    /// <returns>A <see cref="DisplayChromatogram"/> representing the peaks within the loaded chromatogram.</returns>
    DisplayChromatogram IWholeChromatogramLoader<(MzRange Precursor, MzRange Product)>.LoadChromatogram((MzRange Precursor, MzRange Product) state) {
        var chromatogram = _rawSpectra.GetMS2ExtractedIonChromatogram(state.Precursor, state.Product, _range);
        return new DisplayExtractedIonChromatogram(chromatogram, state.Product.Tolerance, _ionMode);
    }

    /// <summary>
    /// Loads a chromatogram of product ions for a specific experiment ID and product m/z range within the defined chromatogram range.
    /// </summary>
    /// <param name="state">A tuple containing the experiment ID and the product m/z range for which the chromatogram is to be loaded.</param>
    /// <returns>A <see cref="DisplayChromatogram"/> representing the chromatogram of extracted ions filtered by the specified experiment ID and product m/z range. This chromatogram provides a detailed view of the product ions' presence and behavior within the specified conditions.</returns>
    /// <remarks>
    /// This method fetches the MS2 spectra associated with a specified experiment ID and filters those spectra based on the provided product m/z range. It then constructs a chromatogram from these filtered spectra, illustrating the intensity and distribution of product ions across the chromatogram range. This functionality is particularly useful for analyzing the behavior of specific ions in complex mixtures or experimental conditions.
    /// </remarks>
    DisplayChromatogram IWholeChromatogramLoader<(int ExperimentID, MzRange Product)>.LoadChromatogram((int ExperimentID, MzRange Product) state) {
        var chromatogram = _rawSpectra.GetMS2ExtractedIonChromatogram(state.ExperimentID, state.Product, _range);
        return new DisplayExtractedIonChromatogram(chromatogram, state.Product.Tolerance, _ionMode);
    }
}