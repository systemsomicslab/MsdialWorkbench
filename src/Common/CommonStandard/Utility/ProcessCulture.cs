using System.Globalization;

namespace CompMs.Common.Utility
{
    /// <summary>
    /// Makes the text form of numbers in this process independent of the regional format of the PC.
    /// </summary>
    /// <remarks>
    /// MS-DIAL's text inputs and outputs use a point as the decimal separator: the embedded IUPAC
    /// table, MSP libraries, method files, mzML, the exported tables and mzTab-M. Much of the code that
    /// reads and writes them formats and parses with the current culture, which on Windows is the
    /// person's regional format. Where that format uses a decimal comma, the result depends on its
    /// digit-grouping symbol. With a space (for example France, Austria, Portugal, Russia, Poland,
    /// Sweden, Norway and Finland) the IUPAC table cannot be read, and the new-project dialog crashes
    /// at "Next". With a point (for example Germany, Denmark, Spain, Italy, Brazil, the Netherlands,
    /// Belgium, Indonesia and Turkey) nothing fails, but every number read from text is scaled by a
    /// power of ten, so library m/z values are wrong and annotation finds nothing, without any
    /// message.
    ///
    /// Calling <see cref="UseInvariantCulture"/> first thing in an executable makes all of that code
    /// agree. It sets the calling thread's culture, which flows to the tasks, timers and threads it
    /// starts, and the process default, which reaches threads that were started earlier or that do not
    /// inherit it. The UI culture is left alone, because it only chooses the language of system
    /// messages.
    /// </remarks>
    public static class ProcessCulture
    {
        /// <summary>
        /// The culture this process had before <see cref="UseInvariantCulture"/> replaced it, which
        /// is the PC's regional format unless something set a culture earlier. Null until then.
        /// </summary>
        public static CultureInfo? RegionalCulture { get; private set; }

        public static void UseInvariantCulture() {
            RegionalCulture ??= CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// One line for crash reports, for example "fr-FR (decimal ',', grouping U+202F); process: invariant".
        /// </summary>
        public static string Describe() {
            return Describe(RegionalCulture ?? CultureInfo.CurrentCulture, CultureInfo.CurrentCulture);
        }

        internal static string Describe(CultureInfo regional, CultureInfo process) {
            var format = regional.NumberFormat;
            return $"{NameOf(regional)} (decimal {Show(format.NumberDecimalSeparator)}, grouping {Show(format.NumberGroupSeparator)}); process: {NameOf(process)}";
        }

        private static string NameOf(CultureInfo culture) {
            return string.IsNullOrEmpty(culture.Name) ? "invariant" : culture.Name;
        }

        // The grouping symbol is often a no-break space, which is invisible in a report.
        private static string Show(string symbol) {
            return symbol.Length == 1 && (symbol[0] <= ' ' || symbol[0] > '~')
                ? $"U+{(int)symbol[0]:X4}"
                : $"'{symbol}'";
        }
    }
}
