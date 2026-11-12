using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    /// <summary>
    /// A line of a seed file that could not be read.
    /// </summary>
    public sealed class LipidSeedReadError
    {
        public LipidSeedReadError(int lineNumber, string message) {
            LineNumber = lineNumber;
            Message = message;
        }

        public int LineNumber { get; }
        public string Message { get; }

        public override string ToString() => $"line {LineNumber}: {Message}";
    }

    /// <summary>
    /// Reads seeds from a tab-separated file. PROVISIONAL: the seed library format is not decided yet;
    /// this reader only exists so the annotation can be exercised, and may be replaced.
    /// <para>
    /// The first line is a header; columns are found by name (case-insensitive), so order and extra
    /// columns don't matter. Name is required (a species-level lipid name such as "PC 34:1"); RT
    /// (minutes), Adduct, PrecursorMz, Formula and Class are optional and may be left empty. Class is an
    /// LbmClass name; it overrides the class read from the name, which a species-level name can leave
    /// ambiguous (e.g. "Cer 34:1;O2" for the ceramide subclasses).
    /// </para>
    /// </summary>
    public static class LipidSeedTsvReader
    {
        public const string NameColumn = "Name";
        public const string RetentionTimeColumn = "RT";
        public const string AdductColumn = "Adduct";
        public const string PrecursorMzColumn = "PrecursorMz";
        public const string FormulaColumn = "Formula";
        public const string ClassColumn = "Class";

        public static IReadOnlyList<LipidSeed> Read(TextReader reader, out IReadOnlyList<LipidSeedReadError> errors) {
            var seeds = new List<LipidSeed>();
            var errorList = new List<LipidSeedReadError>();
            errors = errorList;

            var header = reader.ReadLine();
            if (header is null) {
                errorList.Add(new LipidSeedReadError(1, "the file is empty"));
                return seeds;
            }
            var columns = header.Split('\t').Select(c => c.Trim()).ToArray();
            var name = Array.FindIndex(columns, c => string.Equals(c, NameColumn, StringComparison.OrdinalIgnoreCase));
            if (name < 0) {
                errorList.Add(new LipidSeedReadError(1, $"no '{NameColumn}' column in the header"));
                return seeds;
            }
            var rt = Array.FindIndex(columns, c => string.Equals(c, RetentionTimeColumn, StringComparison.OrdinalIgnoreCase));
            var adduct = Array.FindIndex(columns, c => string.Equals(c, AdductColumn, StringComparison.OrdinalIgnoreCase));
            var precursorMz = Array.FindIndex(columns, c => string.Equals(c, PrecursorMzColumn, StringComparison.OrdinalIgnoreCase));
            var formula = Array.FindIndex(columns, c => string.Equals(c, FormulaColumn, StringComparison.OrdinalIgnoreCase));
            var lipidClass = Array.FindIndex(columns, c => string.Equals(c, ClassColumn, StringComparison.OrdinalIgnoreCase));

            var lineNumber = 1;
            string? line;
            while ((line = reader.ReadLine()) != null) {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                var cells = line.Split('\t');
                string? Cell(int index) => index >= 0 && index < cells.Length && !string.IsNullOrWhiteSpace(cells[index]) ? cells[index].Trim() : null;

                var lipidName = Cell(name);
                if (lipidName is null) {
                    errorList.Add(new LipidSeedReadError(lineNumber, "the name is empty"));
                    continue;
                }
                var lipid = FacadeLipidParser.Default.Parse(lipidName);
                if (lipid is null) {
                    errorList.Add(new LipidSeedReadError(lineNumber, $"'{lipidName}' is not a lipid name the parser knows"));
                    continue;
                }
                if (Cell(lipidClass) is string className) {
                    if (!System.Enum.TryParse<LbmClass>(className, out var lbmClass)) {
                        errorList.Add(new LipidSeedReadError(lineNumber, $"class '{className}' is not an LbmClass"));
                        continue;
                    }
                    lipid = WithClass(lipid, lbmClass);
                }
                if (!TryParseOptionalDouble(Cell(rt), out var rtValue)) {
                    errorList.Add(new LipidSeedReadError(lineNumber, $"RT '{Cell(rt)}' is not a number"));
                    continue;
                }
                if (!TryParseOptionalDouble(Cell(precursorMz), out var mzValue)) {
                    errorList.Add(new LipidSeedReadError(lineNumber, $"PrecursorMz '{Cell(precursorMz)}' is not a number"));
                    continue;
                }
                AdductIon? adductIon = null;
                if (Cell(adduct) is string adductName) {
                    adductIon = AdductIon.GetAdductIon(adductName);
                    if (!adductIon.FormatCheck) {
                        errorList.Add(new LipidSeedReadError(lineNumber, $"adduct '{adductName}' is not recognized"));
                        continue;
                    }
                }
                if (mzValue.HasValue && adductIon is null) {
                    errorList.Add(new LipidSeedReadError(lineNumber, "PrecursorMz is given without an adduct"));
                    continue;
                }
                seeds.Add(new LipidSeed(lipidName, lipid, rtValue, adductIon, mzValue, Cell(formula)));
            }
            return seeds;
        }

        public static IReadOnlyList<LipidSeed> Read(string path, out IReadOnlyList<LipidSeedReadError> errors) {
            using (var reader = new StreamReader(path)) {
                return Read(reader, out errors);
            }
        }

        /// <summary>The lipid with its class replaced (same mass and chains).</summary>
        public static ILipid WithClass(ILipid lipid, LbmClass lipidClass) {
            return lipid.LipidClass == lipidClass ? lipid : new Lipid(lipidClass, lipid.Mass, lipid.Chains);
        }

        private static bool TryParseOptionalDouble(string? text, out double? value) {
            value = null;
            if (text is null) {
                return true;
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) {
                value = parsed;
                return true;
            }
            return false;
        }
    }
}
