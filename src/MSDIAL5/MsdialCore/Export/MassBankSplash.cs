using System;
using System.Collections.Generic;
using System.Globalization;

namespace CompMs.MsdialCore.Export
{
    /// <summary>
    /// PK$SPLASH of a MassBank record, computed from the peak list exactly as MassBankRecordHandler prints it.
    /// </summary>
    /// <remarks>
    /// The MassBank Validator recomputes the SPLASH from the printed PK$PEAK lines, so the hash must be taken over
    /// the printed values, not the full-precision spectrum. The handler passes them as invariant-culture
    /// "m/z:intensity" pairs. They are parsed here with the invariant culture, rather than through
    /// <c>new NSSplash.impl.MSSpectrum(string)</c>, because that constructor parses with the culture of the thread
    /// and would read "56.04679" as 5604679 under a culture whose group separator is '.'.
    /// </remarks>
    public static class MassBankSplash
    {
        /// <summary>
        /// The SPLASH of <paramref name="printedPeaks"/>, "m/z:intensity" pairs separated by single spaces.
        /// </summary>
        public static string Compute(string printedPeaks) {
            if (string.IsNullOrWhiteSpace(printedPeaks)) {
                throw new ArgumentException("The peak list is empty.", nameof(printedPeaks));
            }
            var ions = new List<NSSplash.impl.Ion>();
            foreach (var pair in printedPeaks.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                var values = pair.Split(':');
                if (values.Length != 2
                    || !double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var mz)
                    || !double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var intensity)) {
                    throw new FormatException($"\"{pair}\" is not an m/z:intensity pair.");
                }
                ions.Add(new NSSplash.impl.Ion(mz, intensity));
            }
            return new NSSplash.Splash().splashIt(new NSSplash.impl.MSSpectrum(ions));
        }
    }
}
