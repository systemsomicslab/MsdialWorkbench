using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.Common.Parser
{
    /// <summary>
    /// One analyzer string of AC$INSTRUMENT_TYPE that a person is likely to need, with the instruments it stands for.
    /// </summary>
    public sealed class MassBankAnalyzerPreset
    {
        public MassBankAnalyzerPreset(string analyzer, string description) {
            Analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>The analyzer part of AC$INSTRUMENT_TYPE, a concatenation of <see cref="MassBankVocabulary.AnalyzerTokens"/>, e.g. "QTOF".</summary>
        public string Analyzer { get; }

        /// <summary>The instruments the analyzer string is used for.</summary>
        public string Description { get; }

        public override string ToString() => $"{Analyzer} ({Description})";
    }

    /// <summary>
    /// The controlled vocabulary of a MassBank record that MS-DIAL lets a person choose from, and the checks
    /// that tell whether a value will pass the MassBank Validator.
    /// </summary>
    /// <remarks>
    /// The vocabularies are taken verbatim from the MassBank record grammar
    /// (MassBank-web, MassBank-lib, massbank/RecordParserDefinition.java: ac_instrument_type_sep,
    /// ac_instrument_type_ionisation, ac_instrument_type_analyzer) and from recordformat/license.ini.
    /// The grammar is case-sensitive and allows nothing else on the line, so neither do these checks.
    ///
    /// AC$INSTRUMENT_TYPE is [separation '-'] ionisation '-' analyzer+, with no separator between analyzer
    /// tokens. The grammar is a PetitParser ordered choice, which can reject a string that a regular
    /// expression accepts when one alternative is a prefix of a later one. That never happens here: no
    /// ionisation token is a prefix of another (EI/ESI, CI/APCI, FI/FD, SI/ESI all differ before either ends),
    /// no separation token begins an ionisation token, and no analyzer token is a prefix of another. The
    /// expression in <see cref="IsValidInstrumentType"/> therefore accepts exactly what the grammar accepts.
    /// </remarks>
    public static class MassBankVocabulary
    {
        private static readonly ReadOnlyCollection<string> SEPARATIONS = Array.AsReadOnly(new[] { "LC", "GC", "CE", });

        // Grammar order is APCI APPI EI ESI FAB MALDI FD CI FI SI; this is the same set with the LC-MS ones first.
        private static readonly ReadOnlyCollection<string> IONISATIONS = Array.AsReadOnly(new[] { "ESI", "APCI", "APPI", "EI", "CI", "FI", "FD", "FAB", "MALDI", "SI", });

        private static readonly ReadOnlyCollection<string> ANALYZER_TOKENS = Array.AsReadOnly(new[] { "Q", "TOF", "IT", "FT", "B", "E", });

        private static readonly ReadOnlyCollection<MassBankAnalyzerPreset> ANALYZER_PRESETS = Array.AsReadOnly(new[]
        {
            new MassBankAnalyzerPreset("QTOF", "quadrupole time-of-flight: SCIEX TripleTOF/ZenoTOF, Agilent Q-TOF, Waters Xevo/Synapt, Bruker timsTOF/impact; ion mobility has no token"),
            new MassBankAnalyzerPreset("QFT", "quadrupole-Orbitrap: Thermo Q Exactive, Orbitrap Exploris"),
            new MassBankAnalyzerPreset("ITFT", "ion trap-Orbitrap: Thermo Orbitrap Fusion/Lumos/Ascend/ID-X, LTQ-Orbitrap"),
            new MassBankAnalyzerPreset("QQ", "triple quadrupole"),
            new MassBankAnalyzerPreset("QIT", "quadrupole ion trap, e.g. SCIEX QTRAP"),
            new MassBankAnalyzerPreset("IT", "ion trap"),
            new MassBankAnalyzerPreset("ITTOF", "ion trap time-of-flight, e.g. Shimadzu LCMS-IT-TOF"),
            new MassBankAnalyzerPreset("TOF", "time-of-flight"),
            new MassBankAnalyzerPreset("TOFTOF", "tandem time-of-flight, e.g. MALDI TOF/TOF"),
            new MassBankAnalyzerPreset("Q", "single quadrupole"),
            new MassBankAnalyzerPreset("FT", "Fourier transform without a quadrupole or ion trap front end, e.g. FT-ICR"),
        });

        // recordformat/license.ini of MassBank-lib, in the order the file lists them.
        private static readonly ReadOnlyCollection<string> LICENSES = Array.AsReadOnly(new[] { "CC BY", "CC BY-SA", "CC BY-NC", "CC BY-NC-SA", "CC BY-NC-ND", "CC0", "dl-de/by-2-0", });

        // \A and \z rather than ^ and $: in .NET '$' also matches before a final "\n", which the grammar rejects.
        private static readonly Regex INSTRUMENT_TYPE = new Regex(
            @"\A(?:(?<sep>CE|GC|LC)-)?(?<ion>APCI|APPI|EI|ESI|FAB|MALDI|FD|CI|FI|SI)-(?<ana>(?:B|E|FT|IT|Q|TOF)+)\z",
            RegexOptions.CultureInvariant);

        private static readonly Regex CONTRIBUTOR_ID = new Regex(@"\A[A-Z0-9_]{1,32}\z", RegexOptions.CultureInvariant);

        /// <summary>Separation prefixes of AC$INSTRUMENT_TYPE. Leaving the prefix out (no separation) is also valid.</summary>
        public static IReadOnlyList<string> Separations => SEPARATIONS;

        /// <summary>Ionisation tokens of AC$INSTRUMENT_TYPE, exactly as the grammar spells them; the LC-MS ones first.</summary>
        public static IReadOnlyList<string> Ionisations => IONISATIONS;

        /// <summary>Analyzer tokens of AC$INSTRUMENT_TYPE; an analyzer is one or more of them concatenated.</summary>
        public static IReadOnlyList<string> AnalyzerTokens => ANALYZER_TOKENS;

        /// <summary>Common analyzer strings and the instruments they are used for.</summary>
        public static IReadOnlyList<MassBankAnalyzerPreset> AnalyzerPresets => ANALYZER_PRESETS;

        /// <summary>The licences MassBank accepts in LICENSE, verbatim from its license.ini.</summary>
        public static IReadOnlyList<string> Licenses => LICENSES;

        /// <summary>True when <paramref name="value"/> is accepted by the AC$INSTRUMENT_TYPE grammar, e.g. "LC-ESI-QTOF".</summary>
        public static bool IsValidInstrumentType(string value) {
            return value != null && INSTRUMENT_TYPE.IsMatch(value);
        }

        /// <summary>
        /// Joins the three parts of AC$INSTRUMENT_TYPE: "LC", "ESI", "QTOF" gives "LC-ESI-QTOF", and an empty
        /// separation gives "ESI-QTOF".
        /// </summary>
        /// <remarks>
        /// It does not validate, so that a dialog can show what a partial choice would give; check the result with
        /// <see cref="IsValidInstrumentType"/>.
        /// </remarks>
        public static string ComposeInstrumentType(string separation, string ionisation, string analyzer) {
            var sep = separation?.Trim() ?? string.Empty;
            var ion = ionisation?.Trim() ?? string.Empty;
            var ana = analyzer?.Trim() ?? string.Empty;
            return sep.Length == 0 ? $"{ion}-{ana}" : $"{sep}-{ion}-{ana}";
        }

        /// <summary>
        /// Splits a valid AC$INSTRUMENT_TYPE into its parts; the separation is empty when the value has none.
        /// Returns false, with every part empty, when the value is not valid.
        /// </summary>
        public static bool TryParseInstrumentType(string value, out string separation, out string ionisation, out string analyzer) {
            var match = value is null ? null : INSTRUMENT_TYPE.Match(value);
            if (match is null || !match.Success) {
                separation = ionisation = analyzer = string.Empty;
                return false;
            }
            separation = match.Groups["sep"].Success ? match.Groups["sep"].Value : string.Empty;
            ionisation = match.Groups["ion"].Value;
            analyzer = match.Groups["ana"].Value;
            return true;
        }

        /// <summary>
        /// True when <paramref name="value"/> is a MassBank contributor ID: 1 to 32 of A-Z, 0-9 and '_'.
        /// </summary>
        /// <remarks>
        /// This is the MassBank specification of the second accession block. The Validator's grammar is more
        /// permissive (it accepts lowercase and non-ASCII letters), but MassBank assigns IDs in this form.
        /// </remarks>
        public static bool IsValidContributorId(string value) {
            return value != null && CONTRIBUTOR_ID.IsMatch(value);
        }

        /// <summary>True when <paramref name="value"/> is one of <see cref="Licenses"/>, compared exactly.</summary>
        public static bool IsValidLicense(string value) {
            return value != null && LICENSES.Contains(value, StringComparer.Ordinal);
        }

        /// <summary>
        /// True when <paramref name="value"/> can be written as AC$MASS_SPECTROMETRY: COLLISION_ENERGY.
        /// </summary>
        /// <remarks>
        /// The grammar gives COLLISION_ENERGY no number or unit format ("35 +/- 15 V", "Ramp 20-40 V" and
        /// "20/40/60 eV" all pass), but an empty value swallows the next line, so the value must be non-empty,
        /// on one line, and without leading or trailing whitespace.
        /// </remarks>
        public static bool IsValidCollisionEnergy(string value) {
            return IsSingleLineText(value) && value.Trim().Length == value.Length;
        }

        /// <summary>True when <paramref name="value"/> can be written as AUTHORS: non-empty after trimming, on one line.</summary>
        public static bool IsValidAuthors(string value) {
            return IsSingleLineText(value);
        }

        /// <summary>True when <paramref name="value"/> can be written as AC$INSTRUMENT: non-empty after trimming, on one line.</summary>
        public static bool IsValidInstrument(string value) {
            return IsSingleLineText(value);
        }

        private static bool IsSingleLineText(string value) {
            return !string.IsNullOrWhiteSpace(value) && value.IndexOfAny(new[] { '\r', '\n', }) < 0;
        }
    }
}
