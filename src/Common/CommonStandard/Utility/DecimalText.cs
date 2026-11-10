using System.Globalization;

namespace CompMs.Common.Utility
{
    /// <summary>
    /// Reads a number from a text file that may have been written on a PC whose regional format uses
    /// a decimal comma.
    /// </summary>
    /// <remarks>
    /// Until it fixed the culture of its process, MS-FINDER wrote its settings file with the PC's
    /// regional format, so a PC with a decimal comma has "Ms1Tolerance=0,001" where every other PC has
    /// "0.001". Tables that people save from a spreadsheet on such a PC look the same. These values are
    /// never written with digit grouping, so a number with one comma and no point can only mean a
    /// decimal comma, and it is read as one. Every other text is read with the invariant culture, with
    /// the same number styles as before, so "1,234.5" is still 1234.5.
    ///
    /// The one text this reads differently from the invariant culture is a single comma and no point:
    /// "1,234" is 1.234, not 1234. Use it only for files where a thousands separator cannot occur.
    /// </remarks>
    public static class DecimalText
    {
        private const NumberStyles Styles = NumberStyles.Float | NumberStyles.AllowThousands;

        public static bool TryParse(string? text, out double value) {
            if (text is null) {
                value = 0;
                return false;
            }
            return double.TryParse(WithDecimalPoint(text), Styles, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParse(string? text, out float value) {
            if (text is null) {
                value = 0;
                return false;
            }
            return float.TryParse(WithDecimalPoint(text), Styles, CultureInfo.InvariantCulture, out value);
        }

        private static string WithDecimalPoint(string text) {
            var comma = text.IndexOf(',');
            if (comma < 0 || text.IndexOf(',', comma + 1) >= 0 || text.IndexOf('.') >= 0) {
                return text;
            }
            return text.Substring(0, comma) + "." + text.Substring(comma + 1);
        }
    }
}
