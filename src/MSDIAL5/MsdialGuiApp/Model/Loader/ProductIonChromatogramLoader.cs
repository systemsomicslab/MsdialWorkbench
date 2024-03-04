using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

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
internal sealed class ProductIonChromatogramLoader : IWholeChromatogramLoader<(MzRange Precursor, MzRange Product)>
{
    private readonly IRawSpectra _rawSpectra;
    private readonly ChromatogramRange _range;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductIonChromatogramLoader"/> class with specified raw spectra data and chromatogram range.
    /// </summary>
    /// <param name="rawSpectra">The raw spectra data to be used for chromatogram generation.</param>
    /// <param name="range">The range within which the chromatogram is to be generated.</param>
    public ProductIonChromatogramLoader(IRawSpectra rawSpectra, ChromatogramRange range)
    {
        _rawSpectra = rawSpectra;
        _range = range;
    }

    /// <summary>
    /// Loads a chromatogram based on the specified precursor and product m/z ranges within the previously defined chromatogram range.
    /// </summary>
    /// <param name="state">A tuple containing the precursor and product m/z ranges for which the chromatogram is to be loaded.</param>
    /// <returns>A list of <see cref="PeakItem"/> representing the peaks within the loaded chromatogram.</returns>
    public List<PeakItem> LoadChromatogram((MzRange Precursor, MzRange Product) state) {
        var chromatogram = _rawSpectra.GetProductIonChromatogram(state.Precursor, state.Product, _range);
        return chromatogram.Peaks.Select(p => new PeakItem(p)).ToList();
    }
}