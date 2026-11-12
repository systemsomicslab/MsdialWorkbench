using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    /// <summary>
    /// The (lipid class, adduct) pairs a lipid seed library is searched with.
    /// </summary>
    public sealed class LipidAdductSelection
    {
        private readonly HashSet<(LbmClass Class, string Adduct)> _allowed;

        public LipidAdductSelection(IEnumerable<(LbmClass Class, string Adduct)> allowed) {
            _allowed = new HashSet<(LbmClass, string)>(allowed ?? throw new ArgumentNullException(nameof(allowed)));
        }

        public IReadOnlyCollection<(LbmClass Class, string Adduct)> Allowed => _allowed;

        public bool IsAllowed(LbmClass lipidClass, AdductIon adduct) {
            return _allowed.Contains((lipidClass, adduct.AdductIonName));
        }

        /// <summary>
        /// Every (class, adduct) the spectrum models have an annotatable rule set for; "_P"/"_O" subtype
        /// rule sets count for their LbmClass.
        /// </summary>
        public static IReadOnlyList<(LbmClass Class, AdductIon Adduct)> Available(IEnumerable<ILipidSpectrumModel> models) {
            return models
                .SelectMany(model => LipidSpectrumModelSelector.TryResolveLbmClass(model.LipidClassName, out var lbm)
                    ? model.RuleSets.Where(r => r.Resolution != LipidSpectrumResolution.None).Select(r => (Class: lbm, Adduct: r.AdductIonName))
                    : Enumerable.Empty<(LbmClass Class, string Adduct)>())
                .Distinct()
                .Select(p => (p.Class, Adduct: AdductIon.GetAdductIon(p.Adduct)))
                .Where(p => p.Adduct.FormatCheck)
                .ToList();
        }
    }

    /// <summary>
    /// A named rule for which adducts to expect per lipid class, read from text:
    /// <code>
    /// # Name: Negative HCOOH
    /// Class	Adduct
    /// *	[M-H]-
    /// *	[M+HCOO]-
    /// CL	[M-2H]2-
    /// </code>
    /// Tab-separated rows of an LbmClass name ("*" for every class) and an adduct; "#" lines are comments,
    /// "# Name:" names the preset, and the "Class Adduct" header line is optional. Pairs the spectrum models
    /// have no rule set for are ignored when the preset is applied.
    /// </summary>
    public sealed class LipidAdductPreset
    {
        private const string NamePrefix = "# Name:";

        private readonly (LbmClass? Class, string Adduct)[] _rules;

        private LipidAdductPreset(string name, IEnumerable<(LbmClass? Class, string Adduct)> rules) {
            Name = name;
            _rules = rules.ToArray();
            var modes = _rules.Select(r => AdductIon.GetAdductIon(r.Adduct).IonMode).Distinct().ToArray();
            IonMode = modes.Length == 1 ? modes[0] : (IonMode?)null;
        }

        public string Name { get; }

        /// <summary>The polarity of every adduct in the preset; null when it mixes both.</summary>
        public IonMode? IonMode { get; }

        public bool Allows(LbmClass lipidClass, AdductIon adduct) {
            return _rules.Any(r => (r.Class is null || r.Class == lipidClass) && r.Adduct == adduct.AdductIonName);
        }

        /// <summary>The concrete pairs this preset picks out of what the models can annotate.</summary>
        public LipidAdductSelection Apply(IEnumerable<ILipidSpectrumModel> models) {
            return new LipidAdductSelection(LipidAdductSelection.Available(models)
                .Where(p => Allows(p.Class, p.Adduct))
                .Select(p => (p.Class, p.Adduct.AdductIonName)));
        }

        public static LipidAdductPreset Parse(TextReader reader, string defaultName, out IReadOnlyList<string> errors) {
            var errorList = new List<string>();
            errors = errorList;
            var name = defaultName;
            var rules = new List<(LbmClass?, string)>();
            var lineNumber = 0;
            string? line;
            while ((line = reader.ReadLine()) != null) {
                lineNumber++;
                var trimmed = line.Trim();
                if (trimmed.Length == 0) {
                    continue;
                }
                if (trimmed.StartsWith(NamePrefix, StringComparison.OrdinalIgnoreCase)) {
                    name = trimmed.Substring(NamePrefix.Length).Trim();
                    continue;
                }
                if (trimmed.StartsWith("#")) {
                    continue;
                }
                var cells = line.Split('\t').Select(c => c.Trim()).ToArray();
                if (cells.Length < 2 || cells[0].Length == 0 || cells[1].Length == 0) {
                    errorList.Add($"line {lineNumber}: expected \"class<TAB>adduct\"");
                    continue;
                }
                if (string.Equals(cells[0], "Class", StringComparison.OrdinalIgnoreCase) && string.Equals(cells[1], "Adduct", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                LbmClass? lipidClass = null;
                if (cells[0] != "*") {
                    if (!System.Enum.TryParse<LbmClass>(cells[0], out var parsed)) {
                        errorList.Add($"line {lineNumber}: '{cells[0]}' is not an LbmClass");
                        continue;
                    }
                    lipidClass = parsed;
                }
                var adduct = AdductIon.GetAdductIon(cells[1]);
                if (!adduct.FormatCheck) {
                    errorList.Add($"line {lineNumber}: adduct '{cells[1]}' is not recognized");
                    continue;
                }
                rules.Add((lipidClass, adduct.AdductIonName));
            }
            return new LipidAdductPreset(name, rules);
        }

        public static LipidAdductPreset Load(string path, out IReadOnlyList<string> errors) {
            using (var reader = new StreamReader(path)) {
                return Parse(reader, Path.GetFileNameWithoutExtension(path), out errors);
            }
        }

        public const string PositiveName = "Positive";
        public const string NegativeHcoohName = "Negative HCOOH";
        public const string NegativeCh3coohName = "Negative CH3COOH";
        public const string NegativeNh4hco3Name = "Negative NH4HCO3";

        // Adducts that don't come from the mobile-phase salt, shared by the negative presets.
        private const string NegativeCommon = "*\t[M-H]-\n*\t[M-2H]2-\n";

        /// <summary>The MS-DIAL presets: positive mode, and negative mode per solvent.</summary>
        public static IReadOnlyList<LipidAdductPreset> BuiltIn { get; } = new[]
        {
            FromText(PositiveName, "*\t[M+H]+\n*\t[M+NH4]+\n*\t[M+Na]+\n*\t[M+H-H2O]+\n*\t[M+2H]2+\n*\t[M+2NH4]2+\n"),
            FromText(NegativeHcoohName, NegativeCommon + "*\t[M+HCOO]-\n"),
            FromText(NegativeCh3coohName, NegativeCommon + "*\t[M+CH3COO]-\n"),
            FromText(NegativeNh4hco3Name, NegativeCommon + "*\t[M+HCO3]-\n"),
        };

        /// <summary>The MS-DIAL preset to start from for a polarity.</summary>
        public static LipidAdductPreset DefaultFor(IonMode ionMode) {
            return BuiltIn.First(p => p.Name == (ionMode == Enum.IonMode.Positive ? PositiveName : NegativeHcoohName));
        }

        private static LipidAdductPreset FromText(string name, string text) {
            using (var reader = new StringReader(text)) {
                return Parse(reader, name, out _);
            }
        }
    }
}
