using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using NCDK;
using NCDK.Graphs.InChI;
using NCDK.Smiles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.Parser
{
    /// <summary>
    /// How far a record's annotation goes, as the caller decided it from MS-DIAL's match result.
    /// </summary>
    public enum MassBankAnnotationLevel
    {
        /// <summary>The caller did not say; the record is written from the molecule as it is.</summary>
        Unspecified,
        /// <summary>Matched to a reference (MSP or LBM) and accepted.</summary>
        ReferenceMatched,
        /// <summary>
        /// A suggestion (low score, no MS2, Unsettled, Putative): the structure is kept and the record says
        /// "COMMENT: MS-DIAL annotation level: suggested".
        /// </summary>
        Suggested,
        /// <summary>Not annotated: CH$NAME is "Unknown" and every structure field is N/A.</summary>
        Unknown,
    }

    /// <summary>
    /// A record built in memory: its text when it could be built, otherwise why it was not.
    /// </summary>
    public sealed class MassBankRecordResult
    {
        internal MassBankRecordResult(string accession, string text, string skipReason, IReadOnlyList<string> notes) {
            Accession = accession;
            Text = text;
            SkipReason = skipReason;
            Notes = notes ?? Array.Empty<string>();
        }

        /// <summary>True when <see cref="Text"/> holds a complete record.</summary>
        public bool IsSuccess => Text != null;

        /// <summary>The ACCESSION of the record, which is also its file name without ".txt".</summary>
        public string Accession { get; }

        /// <summary>The file name the Validator requires: the accession plus ".txt".</summary>
        public string FileName => Accession + ".txt";

        /// <summary>The whole record, with LF line endings, ending with "//"; null when the record was refused.</summary>
        public string Text { get; }

        /// <summary>Why the record was refused; null when it was built.</summary>
        public string SkipReason { get; }

        /// <summary>What the handler changed to make the record valid, e.g. a structure written as N/A.</summary>
        public IReadOnlyList<string> Notes { get; }
    }

    /// <summary>
    /// Writes MassBank records (MassBank Record Format 2.6) that pass the MassBank Validator.
    /// </summary>
    /// <remarks>
    /// Every value is written with <see cref="CultureInfo.InvariantCulture"/> and the Gregorian calendar, whatever
    /// the culture of the process. A record is built in memory and written only when it is complete, so a spot that
    /// cannot become a valid record leaves nothing on disk and is reported with a reason instead.
    ///
    /// The structure fields (CH$SMILES, CH$IUPAC, CH$LINK: INCHIKEY, CH$FORMULA, CH$EXACT_MASS) are derived together
    /// from the SMILES: the InChI and InChIKey come from one NCDK InChI generator, and the formula and mass from the
    /// structure that InChI describes, which is what the Validator compares them against. When there is no usable
    /// structure, SMILES and IUPAC are N/A and no InChIKey is written; the library InChIKey is never copied.
    /// </remarks>
    public sealed class MassBankRecordHandler
    {
        /// <summary>The licences MassBank accepts; the same list as <see cref="MassBankVocabulary.Licenses"/>.</summary>
        public static ReadOnlyCollection<string> AvailableLicenses { get; } = new ReadOnlyCollection<string>(MassBankVocabulary.Licenses.ToList());

        /// <summary>RECORD_TITLE longer than this is refused by the Validator.</summary>
        public const int MaxRecordTitleLength = 600;
        /// <summary>A COMMENT value longer than this is refused by the Validator.</summary>
        public const int MaxCommentLength = 600;
        /// <summary>A CH$SMILES value longer than this is refused by the Validator.</summary>
        public const int MaxSmilesLength = 1200;

        /// <summary>The value of COMMENT that marks a suggestion-level annotation.</summary>
        public const string SuggestedAnnotationComment = "MS-DIAL annotation level: suggested";

        private const string UnknownName = "Unknown";
        private const string NotAvailable = "N/A";
        private const double ElectronMass = 0.00054857990946;
        private const char LineFeed = '\n';

        private static readonly CultureInfo INVARIANT = CultureInfo.InvariantCulture;
        private static readonly char[] LINE_BREAKS = { '\r', '\n', };

        private readonly Func<string, string> _splashCalculator;
        private readonly DateTime _now;

        /// <param name="ionMode">The polarity used when a spot's adduct does not tell its own.</param>
        /// <param name="instrumentType">AC$INSTRUMENT_TYPE, e.g. "LC-ESI-QTOF".</param>
        /// <param name="splashCalculator">
        /// Computes PK$SPLASH from the peak list exactly as it is printed in PK$PEAK, given as "m/z:intensity" pairs
        /// separated by single spaces, in invariant culture (e.g. "56.04679:124.37 84.04584:1638.24").
        /// </param>
        public MassBankRecordHandler(IonMode ionMode, string instrumentType, Func<string, string> splashCalculator) {
            IonMode = ionMode;
            InstrumentType = instrumentType ?? throw new ArgumentNullException(nameof(instrumentType));
            _splashCalculator = splashCalculator ?? throw new ArgumentNullException(nameof(splashCalculator));
            _now = DateTime.UtcNow;
        }

        public string Identifier { get; set; } = "MSBNK";
        public string Software { get; set; } = "MSDIAL";

        /// <summary>The MassBank contributor ID, the second block of ACCESSION; see <see cref="MassBankVocabulary.IsValidContributorId"/>.</summary>
        public string ContributorIdentifier { get; set; } = string.Empty;

        public string Authors { get; set; } = string.Empty;

        /// <summary>LICENSE. Written as given; a value MassBank does not list is a metadata error and is never replaced.</summary>
        public string License { get; set; } = string.Empty;

        public string Instrument { get; set; } = string.Empty;

        public string InstrumentType { get; set; }

        /// <summary>AC$MASS_SPECTROMETRY: COLLISION_ENERGY, written when not empty.</summary>
        public string CollisionEnergy { get; set; } = string.Empty;

        /// <summary>Free text written as one COMMENT line per text line; empty lines are skipped.</summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>The MS-DIAL version, written as "MS$DATA_PROCESSING: WHOLE MS-DIAL &lt;version&gt;" when not empty.</summary>
        public string MsdialVersion { get; set; } = string.Empty;

        public IonMode IonMode { get; set; }

        public int MsLevel {
            get => _msLevel;
            set {
                if (value >= 1) {
                    _msLevel = value;
                }
            }
        }
        private int _msLevel = 2;

        /// <summary>MS_TYPE: MS for MS1, MS2 to MS5, MSn beyond.</summary>
        public string MSType => _msLevel == 1 ? "MS" : _msLevel <= 5 ? "MS" + _msLevel.ToString(INVARIANT) : "MSn";

        public string GetAccession(IChromatogramPeak peak) {
            if (peak is null) {
                throw new ArgumentNullException(nameof(peak));
            }
            // The invariant culture formats dates with the Gregorian calendar; th-TH would otherwise give 2569.
            return $"{Identifier}-{ContributorIdentifier}-{Software}{_now.ToString("yyMMdd", INVARIANT)}{peak.ID.ToString("D8", INVARIANT)}";
        }

        /// <summary>
        /// The problems that keep the project metadata from making valid records; empty when there are none.
        /// Callers refuse to export, and show these, while the list is not empty.
        /// </summary>
        public IReadOnlyList<string> ValidateMetadata() {
            var problems = new List<string>(ValidateProjectMetadata());
            if (!MassBankVocabulary.IsValidContributorId(ContributorIdentifier)) {
                problems.Add(string.IsNullOrEmpty(ContributorIdentifier)
                    ? "Contributor ID is empty. Enter the ID MassBank assigned to you: 1 to 32 of A-Z, 0-9 and _."
                    : $"Contributor ID \"{ContributorIdentifier}\" is not valid. MassBank contributor IDs are 1 to 32 of A-Z, 0-9 and _.");
            }
            return problems;
        }

        /// <summary>
        /// The problems of <see cref="ValidateMetadata"/> that come from the project (instrument type, authors,
        /// instrument, license, collision energy), that is, all but the contributor ID, which the export dialog asks for.
        /// </summary>
        public IReadOnlyList<string> ValidateProjectMetadata() {
            var problems = new List<string>();
            if (!MassBankVocabulary.IsValidInstrumentType(InstrumentType)) {
                problems.Add(string.IsNullOrEmpty(InstrumentType)
                    ? "Instrument type is empty. MassBank needs [separation-]ionisation-analyzer, e.g. LC-ESI-QTOF, LC-ESI-QFT or LC-ESI-ITFT."
                    : $"Instrument type \"{InstrumentType}\" is not a MassBank instrument type. MassBank needs [separation-]ionisation-analyzer, e.g. LC-ESI-QTOF, LC-ESI-QFT or LC-ESI-ITFT.");
            }
            if (!MassBankVocabulary.IsValidAuthors(Authors)) {
                problems.Add("Authors must be given, on one line.");
            }
            if (!MassBankVocabulary.IsValidInstrument(Instrument)) {
                problems.Add("Instrument must be given, on one line.");
            }
            if (!MassBankVocabulary.IsValidLicense(License)) {
                problems.Add(string.IsNullOrEmpty(License)
                    ? $"License is empty. MassBank accepts {string.Join(", ", MassBankVocabulary.Licenses)}."
                    : $"License \"{License}\" is not one MassBank accepts: {string.Join(", ", MassBankVocabulary.Licenses)}.");
            }
            var collisionEnergy = CollisionEnergy?.Trim() ?? string.Empty;
            if (collisionEnergy.Length > 0 && !MassBankVocabulary.IsValidCollisionEnergy(collisionEnergy)) {
                problems.Add("Collision energy must be on one line.");
            }
            if (string.IsNullOrEmpty(Identifier) || Identifier.Length > 10 || !Identifier.All(c => char.IsLetterOrDigit(c) || c == '_')) {
                problems.Add($"Accession prefix \"{Identifier}\" is not valid.");
            }
            if (string.IsNullOrEmpty(Software) || !Software.All(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_')) {
                problems.Add($"Accession software code \"{Software}\" is not valid.");
            }
            if (!string.IsNullOrEmpty(MsdialVersion) && MsdialVersion.IndexOfAny(LINE_BREAKS) >= 0) {
                problems.Add("MS-DIAL version must be on one line.");
            }
            return problems;
        }

        /// <summary>
        /// Writes the record, or throws <see cref="InvalidOperationException"/> with the reason when it cannot be built.
        /// </summary>
        public void WriteRecord(Stream stream, IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty) {
            if (!TryWriteRecord(stream, peak, molecule, scan, ionProperty, MassBankAnnotationLevel.Unspecified, out var reason)) {
                throw new InvalidOperationException($"MassBank record {GetAccession(peak)} was not written: {reason}");
            }
        }

        /// <summary>
        /// Writes the record when it can be built; otherwise writes nothing, returns false and says why.
        /// </summary>
        public bool TryWriteRecord(Stream stream, IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty, out string reason) {
            return TryWriteRecord(stream, peak, molecule, scan, ionProperty, MassBankAnnotationLevel.Unspecified, out reason);
        }

        /// <inheritdoc cref="TryWriteRecord(Stream, IChromatogramPeak, IMoleculeProperty, IMSScanProperty, IIonProperty, out string)"/>
        public bool TryWriteRecord(Stream stream, IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty, MassBankAnnotationLevel annotationLevel, out string reason) {
            if (stream is null) {
                throw new ArgumentNullException(nameof(stream));
            }
            var result = BuildRecord(peak, molecule, scan, ionProperty, annotationLevel);
            if (!result.IsSuccess) {
                reason = result.SkipReason;
                return false;
            }
            var bytes = new UTF8Encoding(false).GetBytes(result.Text);
            stream.Write(bytes, 0, bytes.Length);
            reason = null;
            return true;
        }

        /// <summary>
        /// Builds the record in memory. Nothing is written; a refused record carries its reason.
        /// </summary>
        public MassBankRecordResult BuildRecord(IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty, MassBankAnnotationLevel annotationLevel = MassBankAnnotationLevel.Unspecified) {
            if (peak is null) {
                throw new ArgumentNullException(nameof(peak));
            }
            if (molecule is null) {
                throw new ArgumentNullException(nameof(molecule));
            }
            if (scan is null) {
                throw new ArgumentNullException(nameof(scan));
            }
            if (ionProperty is null) {
                throw new ArgumentNullException(nameof(ionProperty));
            }

            var accession = GetAccession(peak);
            var notes = new List<string>();
            MassBankRecordResult Refuse(string why) => new MassBankRecordResult(accession, null, why, notes);

            var metadataProblems = ValidateMetadata();
            if (metadataProblems.Count > 0) {
                return Refuse("the project metadata is not valid for MassBank: " + string.Join(" ", metadataProblems));
            }
            if (peak.ID < 0) {
                return Refuse($"the peak ID {peak.ID.ToString(INVARIANT)} cannot form an accession");
            }

            // Peaks: sorted, merged where they print the same, and only what PK$PEAK prints feeds SPLASH.
            var peaks = PreparePeaks(scan.Spectrum);
            if (peaks.Count == 0) {
                return Refuse("no MS/MS peaks");
            }

            // Polarity: the spot's own adduct; the handler's ion mode only when the adduct does not say.
            var adduct = ionProperty.AdductType ?? AdductIon.Default;
            var adductKnown = adduct.FormatCheck && adduct.HasAdduct;
            var polarity = adductKnown ? adduct.IonMode : IonMode;
            if (polarity != IonMode.Positive && polarity != IonMode.Negative) {
                return Refuse($"the polarity is {polarity}, but a MassBank record is POSITIVE or NEGATIVE");
            }

            // Name.
            var isUnknown = annotationLevel == MassBankAnnotationLevel.Unknown;
            var originalName = molecule.Name ?? string.Empty;
            var name = isUnknown ? UnknownName : SanitizeName(originalName);
            if (string.IsNullOrEmpty(name)) {
                name = UnknownName;
            }
            var nameChanged = !isUnknown && !string.IsNullOrWhiteSpace(originalName) && !string.Equals(name, originalName, StringComparison.Ordinal);

            // Structure.
            Structure structure = null;
            if (!isUnknown) {
                structure = ResolveStructure(molecule.SMILES, out var structureNote);
                if (structureNote != null) {
                    notes.Add(structureNote);
                }
            }
            string formula;
            double exactMass;
            if (structure != null) {
                formula = structure.Formula;
                exactMass = structure.MonoisotopicMass;
            }
            else {
                var libraryFormula = isUnknown ? null : ParseLibraryFormula(molecule.Formula?.FormulaString);
                if (libraryFormula != null) {
                    formula = libraryFormula.Formula;
                    exactMass = libraryFormula.MonoisotopicMass;
                }
                else {
                    formula = NotAvailable;
                    exactMass = adductKnown ? adduct.ConvertToExactMass(peak.Mass) : double.NaN;
                    if (!IsPositiveNumber(exactMass)) {
                        return Refuse("there is no formula, and the neutral mass cannot be derived from the precursor m/z and adduct");
                    }
                    notes.Add("CH$EXACT_MASS is the neutral mass inferred from the precursor m/z and adduct");
                }
            }

            var record = new StringBuilder();
            void Line(string text) {
                record.Append(text).Append(LineFeed);
            }

            Line($"ACCESSION: {accession}");

            var titleSuffix = $"; {InstrumentType}; {MSType}";
            var titleName = name;
            if (titleName.Length + titleSuffix.Length > MaxRecordTitleLength) {
                titleName = titleName.Substring(0, Math.Max(1, MaxRecordTitleLength - titleSuffix.Length)).TrimEnd();
                notes.Add("RECORD_TITLE was shortened to 600 characters");
            }
            Line($"RECORD_TITLE: {titleName}{titleSuffix}");
            Line($"DATE: {_now.ToString("yyyy.MM.dd", INVARIANT)}");
            Line($"AUTHORS: {Authors.Trim()}");
            Line($"LICENSE: {License}");

            foreach (var comment in SplitComment(Comment)) {
                Line($"COMMENT: {comment}");
            }
            if (nameChanged) {
                var original = "Original name: " + Regex.Replace(originalName, @"[\r\n]+", " ").Trim();
                if (original.Length > MaxCommentLength) {
                    original = original.Substring(0, MaxCommentLength).TrimEnd();
                }
                Line($"COMMENT: {original}");
                notes.Add("the name was changed to characters MassBank accepts");
            }
            if (annotationLevel == MassBankAnnotationLevel.Suggested) {
                Line($"COMMENT: {SuggestedAnnotationComment}");
            }

            Line($"CH$NAME: {name}");
            var compoundClass = isUnknown ? string.Empty : SanitizeCompoundClass(molecule.Ontology);
            if (compoundClass.Length > 0) {
                Line($"CH$COMPOUND_CLASS: {compoundClass}");
            }
            Line($"CH$FORMULA: {formula}");
            Line($"CH$EXACT_MASS: {exactMass.ToString("F5", INVARIANT)}");
            Line($"CH$SMILES: {structure?.Smiles ?? NotAvailable}");
            Line($"CH$IUPAC: {structure?.InChI ?? NotAvailable}");
            if (structure != null) {
                Line($"CH$LINK: INCHIKEY {structure.InChIKey}");
            }

            Line($"AC$INSTRUMENT: {Instrument.Trim()}");
            Line($"AC$INSTRUMENT_TYPE: {InstrumentType}");
            Line($"AC$MASS_SPECTROMETRY: MS_TYPE {MSType}");
            Line($"AC$MASS_SPECTROMETRY: ION_MODE {(polarity == IonMode.Positive ? "POSITIVE" : "NEGATIVE")}");
            var collisionEnergy = CollisionEnergy?.Trim() ?? string.Empty;
            if (collisionEnergy.Length > 0) {
                Line($"AC$MASS_SPECTROMETRY: COLLISION_ENERGY {collisionEnergy}");
            }

            // AC$CHROMATOGRAPHY in alphabetical order. CCS is not a listed subtag; the grammar takes it as a free subtag.
            var ccs = ionProperty.CollisionCrossSection;
            if (IsPositiveNumber(ccs)) {
                Line($"AC$CHROMATOGRAPHY: CCS {ccs.ToString("0.0##", INVARIANT)}");
            }
            var ri = peak.ChromXs?.RI?.Value ?? 0d;
            if (IsPositiveNumber(ri)) {
                Line($"AC$CHROMATOGRAPHY: RETENTION_INDEX {ri.ToString("0.0##", INVARIANT)}");
            }
            var rtMin = RetentionTimeInMinutes(peak.ChromXs?.RT);
            if (IsPositiveNumber(rtMin)) {
                Line($"AC$CHROMATOGRAPHY: RETENTION_TIME {rtMin.ToString("F3", INVARIANT)} min");
            }

            if (IsPositiveNumber(peak.Mass)) {
                Line($"MS$FOCUSED_ION: PRECURSOR_M/Z {peak.Mass.ToString("F5", INVARIANT)}");
            }
            var precursorType = adductKnown ? ToMassBankPrecursorType(adduct.AdductIonName) : null;
            if (precursorType != null && IsValidPrecursorType(precursorType)) {
                Line($"MS$FOCUSED_ION: PRECURSOR_TYPE {precursorType}");
            }
            else {
                notes.Add("MS$FOCUSED_ION: PRECURSOR_TYPE was omitted because the adduct has no MassBank notation");
            }

            if (!string.IsNullOrWhiteSpace(MsdialVersion)) {
                Line($"MS$DATA_PROCESSING: WHOLE MS-DIAL {MsdialVersion.Trim()}");
            }

            var printedPeaks = string.Join(" ", peaks.Select(p => p.MzText + ":" + p.IntensityText));
            string splash;
            try {
                splash = _splashCalculator(printedPeaks);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException)) {
                return Refuse($"SPLASH could not be computed: {ex.Message}");
            }
            if (string.IsNullOrWhiteSpace(splash) || splash.IndexOfAny(LINE_BREAKS) >= 0) {
                return Refuse("SPLASH could not be computed");
            }
            Line($"PK$SPLASH: {splash.Trim()}");

            // PK$ANNOTATION is optional; it is written only when some peak carries an annotation, one token per peak.
            var annotated = peaks.Select(p => (p.MzText, Token: AnnotationToken(p.Comment))).Where(p => p.Token != null).ToList();
            if (annotated.Count > 0) {
                Line("PK$ANNOTATION: m/z type");
                foreach (var (mz, token) in annotated) {
                    Line($"  {mz} {token}");
                }
            }

            Line($"PK$NUM_PEAK: {peaks.Count.ToString(INVARIANT)}");
            Line("PK$PEAK: m/z int. rel.int.");
            var maxIntensity = peaks.Max(p => p.Intensity);
            foreach (var p in peaks) {
                var relative = (int)Math.Round(p.Intensity / maxIntensity * 999d, MidpointRounding.AwayFromZero);
                Line($"  {p.MzText} {p.IntensityText} {relative.ToString(INVARIANT)}");
            }
            Line("//");

            return new MassBankRecordResult(accession, record.ToString(), null, notes);
        }

        /// <summary>
        /// The number of peaks the record of <paramref name="scan"/> would list: peaks with a positive m/z and an
        /// intensity that prints as more than 0.00, counting once those that print with the same m/z.
        /// </summary>
        public static int CountExportablePeaks(IMSScanProperty scan) {
            return scan?.Spectrum is null ? 0 : PreparePeaks(scan.Spectrum).Count;
        }

        /// <summary>
        /// True when <paramref name="name"/> carries one of the prefixes MS-DIAL gives a suggestion-level annotation:
        /// "low score:", "no MS2:", "w/o MS2:", "Unsettled:" or "Putative:".
        /// </summary>
        public static bool IsSuggestionName(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                return false;
            }
            var value = name.TrimStart();
            foreach (var prefix in SUGGESTION_PREFIXES) {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && value.Substring(prefix.Length).TrimStart().StartsWith(":", StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }

        private static readonly string[] SUGGESTION_PREFIXES = {
            AnnotationName.LowScorePrefix, AnnotationName.NoMs2Prefix, AnnotationName.WithoutMs2Prefix, "Unsettled", "Putative",
        };

        // ---------------------------------------------------------------- peaks

        private sealed class PrintedPeak
        {
            public string MzText;
            public string IntensityText;
            public double Intensity;
            public SpectrumComment Comment;
        }

        private static List<PrintedPeak> PreparePeaks(IEnumerable<SpectrumPeak> spectrum) {
            var result = new List<PrintedPeak>();
            if (spectrum is null) {
                return result;
            }
            // OrderBy is stable, so peaks of equal m/z keep their order.
            foreach (var p in spectrum.Where(p => p != null && IsPositiveNumber(p.Mass) && IsPositiveNumber(p.Intensity)).OrderBy(p => p.Mass)) {
                var intensityText = p.Intensity.ToString("F2", INVARIANT);
                if (intensityText == "0.00") {
                    continue;
                }
                var peak = new PrintedPeak { MzText = p.Mass.ToString("F5", INVARIANT), IntensityText = intensityText, Intensity = p.Intensity, Comment = p.SpectrumComment, };
                // Two peaks that print with the same m/z fail the Validator's strictly increasing check; keep the larger.
                if (result.Count > 0 && result[result.Count - 1].MzText == peak.MzText) {
                    if (peak.Intensity > result[result.Count - 1].Intensity) {
                        result[result.Count - 1] = peak;
                    }
                    continue;
                }
                result.Add(peak);
            }
            return result;
        }

        private static string AnnotationToken(SpectrumComment comment) {
            if (comment == SpectrumComment.none) {
                return null;
            }
            var names = new List<string>();
            foreach (SpectrumComment flag in System.Enum.GetValues(typeof(SpectrumComment))) {
                var bits = (long)flag;
                if (bits != 0 && (bits & (bits - 1)) == 0 && (comment & flag) == flag) {
                    names.Add(flag.ToString());
                }
            }
            return names.Count == 0 ? null : string.Join("+", names);
        }

        // ---------------------------------------------------------------- structure

        private sealed class Structure
        {
            public string Smiles;
            public string InChI;
            public string InChIKey;
            public string Formula;
            public double MonoisotopicMass;
        }

        private sealed class FormulaAndMass
        {
            public string Formula;
            public double MonoisotopicMass;
        }

        private static readonly Regex INCHIKEY = new Regex(@"\A[A-Z]{14}-[A-Z]{10}-[A-Z]\z", RegexOptions.CultureInvariant);

        /// <summary>
        /// The structure fields from <paramref name="smiles"/>, or null, with the reason, when it gives no usable structure.
        /// </summary>
        private static Structure ResolveStructure(string smiles, out string note) {
            note = null;
            var text = smiles?.Trim() ?? string.Empty;
            if (text.Length == 0) {
                return null; // nothing to report: the reference simply has no structure
            }
            var space = text.IndexOfAny(new[] { ' ', '\t', '\r', '\n', });
            if (space >= 0) {
                text = text.Substring(0, space);
            }
            if (text.Length > MaxSmilesLength || text.Equals(NotAvailable, StringComparison.OrdinalIgnoreCase)) {
                note = "the structure was written as N/A: the SMILES is too long or not available";
                return null;
            }
            try {
                // A SMILES with several components (a salt) is kept as it is: an InChI describes all of them.
                var container = new SmilesParser(CDK.Builder).ParseSmiles(text);
                if (container is null || container.Atoms.Count == 0) {
                    note = "the structure was written as N/A: the SMILES could not be parsed";
                    return null;
                }
                if (container.Atoms.Any(atom => atom.AtomicNumber == 0)) {
                    note = "the structure was written as N/A: the SMILES has wildcard atoms";
                    return null;
                }
                var generator = InChIGeneratorFactory.Instance.GetInChIGenerator(container);
                if ((generator.ReturnStatus != InChIReturnCode.Ok && generator.ReturnStatus != InChIReturnCode.Warning)
                    || string.IsNullOrEmpty(generator.InChI)
                    || !generator.InChI.StartsWith("InChI=", StringComparison.Ordinal)) {
                    note = $"the structure was written as N/A: no InChI could be generated from the SMILES ({generator.ReturnStatus})";
                    return null;
                }
                var inchi = generator.InChI;
                var key = generator.GetInChIKey();
                if (string.IsNullOrEmpty(key) || !INCHIKEY.IsMatch(key)) {
                    note = "the structure was written as N/A: no InChIKey could be generated";
                    return null;
                }
                // The formula and mass of the structure the InChI describes, which is what the Validator compares
                // CH$FORMULA with. Isotopes are collapsed in the formula, which has no isotope notation, and kept in
                // the mass, so a labelled standard has the mass of the labelled compound.
                var toStructure = InChIGeneratorFactory.Instance.GetInChIToStructure(inchi, CDK.Builder);
                if ((toStructure.ReturnStatus != InChIReturnCode.Ok && toStructure.ReturnStatus != InChIReturnCode.Warning) || toStructure.AtomContainer is null) {
                    note = $"the structure was written as N/A: the InChI could not be read back ({toStructure.ReturnStatus})";
                    return null;
                }
                var formulaAndMass = FormulaOf(toStructure.AtomContainer);
                if (formulaAndMass is null) {
                    note = "the structure was written as N/A: the formula of the structure could not be determined";
                    return null;
                }
                return new Structure { Smiles = text, InChI = inchi, InChIKey = key, Formula = formulaAndMass.Formula, MonoisotopicMass = formulaAndMass.MonoisotopicMass, };
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException || ex is DllNotFoundException || ex is BadImageFormatException || ex is EntryPointNotFoundException)) {
                // One unusual structure must not stop the export of the others.
                note = $"the structure was written as N/A: {ex.GetType().Name}: {ex.Message}";
                return null;
            }
        }

        private static FormulaAndMass FormulaOf(IAtomContainer container) {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var mass = 0d;
            var charge = 0;
            var hydrogenMass = IsotopeMass("H", null);
            if (!hydrogenMass.HasValue) {
                return null;
            }
            foreach (var atom in container.Atoms) {
                var symbol = atom.Symbol;
                if (string.IsNullOrEmpty(symbol) || !ELEMENTS.Contains(symbol)) {
                    return null;
                }
                var atomMass = IsotopeMass(symbol, atom.MassNumber);
                if (!atomMass.HasValue) {
                    return null;
                }
                Add(counts, symbol, 1);
                mass += atomMass.Value;
                var hydrogens = atom.ImplicitHydrogenCount ?? 0;
                if (hydrogens > 0) {
                    Add(counts, "H", hydrogens);
                    mass += hydrogens * hydrogenMass.Value;
                }
                charge += atom.FormalCharge ?? 0;
            }
            if (counts.Count == 0) {
                return null;
            }
            return new FormulaAndMass { Formula = HillFormula(counts, charge), MonoisotopicMass = mass - charge * ElectronMass, };
        }

        private static void Add(Dictionary<string, int> counts, string symbol, int count) {
            counts.TryGetValue(symbol, out var current);
            counts[symbol] = current + count;
        }

        private static double? IsotopeMass(string symbol, int? massNumber) {
            try {
                var isotope = massNumber.HasValue && massNumber.Value > 0
                    ? CDK.IsotopeFactory.GetIsotope(symbol, massNumber.Value)
                    : CDK.IsotopeFactory.GetMajorIsotope(symbol);
                return isotope?.ExactMass;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException)) {
                return null;
            }
        }

        /// <summary>
        /// The formula as CDK prints it, which the Validator requires: C, H, then the other elements alphabetically
        /// (alphabetically throughout when there is no carbon), no count of 1, and "[...]n+" for a charged structure.
        /// </summary>
        private static string HillFormula(IReadOnlyDictionary<string, int> counts, int charge) {
            var hasCarbon = counts.TryGetValue("C", out var carbons) && carbons > 0;
            var order = hasCarbon
                ? new[] { "C", "H", }.Concat(counts.Keys.Where(k => k != "C" && k != "H").OrderBy(k => k, StringComparer.Ordinal))
                : counts.Keys.OrderBy(k => k, StringComparer.Ordinal);
            var builder = new StringBuilder();
            foreach (var element in order) {
                if (!counts.TryGetValue(element, out var count) || count <= 0) {
                    continue;
                }
                builder.Append(element);
                if (count != 1) {
                    builder.Append(count.ToString(INVARIANT));
                }
            }
            if (charge == 0) {
                return builder.ToString();
            }
            var magnitude = Math.Abs(charge);
            return "[" + builder + "]" + (magnitude > 1 ? magnitude.ToString(INVARIANT) : string.Empty) + (charge > 0 ? "+" : "-");
        }

        /// <summary>
        /// A library formula such as "C5H7NO3" or "C8H7[2H]3N4O2" in the form CH$FORMULA requires, with isotopes
        /// collapsed into their element, and its monoisotopic mass with the isotopes kept; null when it cannot be read.
        /// </summary>
        private static FormulaAndMass ParseLibraryFormula(string formula) {
            var text = formula?.Trim();
            if (string.IsNullOrEmpty(text)) {
                return null;
            }
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var mass = 0d;
            var i = 0;
            while (i < text.Length) {
                string symbol;
                int? massNumber = null;
                if (text[i] == '[') {
                    var close = text.IndexOf(']', i);
                    if (close < 0) {
                        return null;
                    }
                    var inner = text.Substring(i + 1, close - i - 1);
                    var digits = new string(inner.TakeWhile(c => c >= '0' && c <= '9').ToArray());
                    symbol = inner.Substring(digits.Length);
                    if (digits.Length == 0 || !int.TryParse(digits, NumberStyles.None, INVARIANT, out var number)) {
                        return null;
                    }
                    massNumber = number;
                    i = close + 1;
                }
                else if (text[i] == 'D' && (i + 1 >= text.Length || !(text[i + 1] >= 'a' && text[i + 1] <= 'z'))) {
                    symbol = "H";
                    massNumber = 2;
                    i++;
                }
                else if (text[i] >= 'A' && text[i] <= 'Z') {
                    symbol = i + 1 < text.Length && text[i + 1] >= 'a' && text[i + 1] <= 'z' ? text.Substring(i, 2) : text.Substring(i, 1);
                    i += symbol.Length;
                }
                else {
                    return null;
                }
                if (!ELEMENTS.Contains(symbol)) {
                    return null;
                }
                var start = i;
                while (i < text.Length && text[i] >= '0' && text[i] <= '9') {
                    i++;
                }
                var count = 1;
                if (i > start && !int.TryParse(text.Substring(start, i - start), NumberStyles.None, INVARIANT, out count)) {
                    return null;
                }
                var atomMass = IsotopeMass(symbol, massNumber);
                if (!atomMass.HasValue) {
                    return null;
                }
                Add(counts, symbol, count);
                mass += count * atomMass.Value;
            }
            if (counts.Values.All(c => c <= 0)) {
                return null;
            }
            return new FormulaAndMass { Formula = HillFormula(counts, 0), MonoisotopicMass = mass, };
        }

        // The 118 element symbols of the CH$FORMULA grammar.
        private static readonly HashSet<string> ELEMENTS = new HashSet<string>(StringComparer.Ordinal)
        {
            "Zr", "Zn", "Yb", "Y", "Xe", "W", "V", "U", "Ts", "Tm", "Tl", "Ti", "Th", "Te", "Tc", "Tb", "Ta", "Sr", "Sn", "Sm", "Si", "Sg", "Se", "Sc", "Sb", "S",
            "Ru", "Rn", "Rh", "Rg", "Rf", "Re", "Rb", "Ra", "Pu", "Pt", "Pr", "Po", "Pm", "Pd", "Pb", "Pa", "P", "Os", "Og", "O", "Np", "No", "Ni", "Nh", "Ne",
            "Nd", "Nb", "Na", "N", "Mt", "Mo", "Mn", "Mg", "Md", "Mc", "Lv", "Lu", "Lr", "Li", "La", "Kr", "K", "Ir", "In", "I", "Hs", "Ho", "Hg", "Hf", "He",
            "H", "Ge", "Gd", "Ga", "Fr", "Fm", "Fl", "Fe", "F", "Eu", "Es", "Er", "Dy", "Ds", "Db", "Cu", "Cs", "Cr", "Co", "Cn", "Cm", "Cl", "Cf", "Ce", "Cd",
            "Ca", "C", "Br", "Bk", "Bi", "Bh", "Be", "Ba", "B", "Au", "At", "As", "Ar", "Am", "Al", "Ag", "Ac",
        };

        // ---------------------------------------------------------------- precursor type

        /// <summary>
        /// MS-DIAL's adduct name in the notation of MS$FOCUSED_ION: PRECURSOR_TYPE. The abbreviations the MassBank
        /// grammar does not know are written as formulas: IsoProp as C3H8O, DMSO as C2H6OS, TFA as C2HF3O2, and
        /// "+Hac-H" as "+CH3COO" (any other Hac as CH3COOH). ACN and FA are MassBank tokens and are kept.
        /// </summary>
        public static string ToMassBankPrecursorType(string adductName) {
            if (string.IsNullOrWhiteSpace(adductName)) {
                return null;
            }
            var value = adductName.Trim();
            value = Regex.Replace(value, @"\+Hac-H(?=[\]+-])", "+CH3COO", RegexOptions.CultureInvariant);
            value = Regex.Replace(value, @"(?<=[+-][0-9]*)Hac(?=[\]+-])", "CH3COOH", RegexOptions.CultureInvariant);
            value = Regex.Replace(value, @"(?<=[+-][0-9]*)IsoProp(?=[\]+-])", "C3H8O", RegexOptions.CultureInvariant);
            value = Regex.Replace(value, @"(?<=[+-][0-9]*)DMSO(?=[\]+-])", "C2H6OS", RegexOptions.CultureInvariant);
            value = Regex.Replace(value, @"(?<=[+-][0-9]*)TFA(?=[\]+-])", "C2HF3O2", RegexOptions.CultureInvariant);
            return value;
        }

        /// <summary>
        /// True when <paramref name="value"/> is accepted by the PRECURSOR_TYPE grammar of the MassBank Validator:
        /// '[' n? 'M' ([+-] n? (ACN | FA | formula))* ']' ([+-]+ | n[+-] with n not 1) '*'?, alternatives joined by '/'.
        /// </summary>
        public static bool IsValidPrecursorType(string value) {
            if (string.IsNullOrEmpty(value)) {
                return false;
            }
            var i = 0;
            if (!ParseAdduct(value, ref i)) {
                return false;
            }
            while (i < value.Length && value[i] == '/') {
                i++;
                if (!ParseAdduct(value, ref i)) {
                    return false;
                }
            }
            return i == value.Length;
        }

        private static bool ParseAdduct(string s, ref int i) {
            if (!Expect(s, ref i, '[')) {
                return false;
            }
            SkipDigits(s, ref i);
            if (!Expect(s, ref i, 'M')) {
                return false;
            }
            while (i < s.Length && (s[i] == '+' || s[i] == '-')) {
                i++;
                SkipDigits(s, ref i);
                if (StartsWithAt(s, i, "ACN")) {
                    i += 3;
                }
                else if (StartsWithAt(s, i, "FA")) {
                    i += 2;
                }
                else if (!ParseMolecularFormula(s, ref i)) {
                    return false;
                }
            }
            if (!Expect(s, ref i, ']')) {
                return false;
            }
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) {
                while (i < s.Length && (s[i] == '+' || s[i] == '-')) {
                    i++;
                }
            }
            else {
                if (i + 1 < s.Length && s[i] == '1' && (s[i + 1] == '+' || s[i + 1] == '-')) {
                    return false;
                }
                var start = i;
                SkipDigits(s, ref i);
                if (i == start || i >= s.Length || (s[i] != '+' && s[i] != '-')) {
                    return false;
                }
                i++;
            }
            if (i < s.Length && s[i] == '*') {
                i++;
            }
            return true;
        }

        // element_count+, where element is an ordered choice that tries two-letter symbols before one-letter ones.
        private static bool ParseMolecularFormula(string s, ref int i) {
            var count = 0;
            while (i < s.Length) {
                if (i + 1 < s.Length && ELEMENTS.Contains(s.Substring(i, 2))) {
                    i += 2;
                }
                else if (ELEMENTS.Contains(s.Substring(i, 1))) {
                    i += 1;
                }
                else {
                    break;
                }
                SkipDigits(s, ref i);
                count++;
            }
            return count > 0;
        }

        private static bool StartsWithAt(string s, int i, string token) {
            return i + token.Length <= s.Length && string.CompareOrdinal(s, i, token, 0, token.Length) == 0;
        }

        private static bool Expect(string s, ref int i, char c) {
            if (i < s.Length && s[i] == c) {
                i++;
                return true;
            }
            return false;
        }

        private static void SkipDigits(string s, ref int i) {
            while (i < s.Length && s[i] >= '0' && s[i] <= '9') {
                i++;
            }
        }

        // ---------------------------------------------------------------- text

        private const string NAME_PUNCTUATION = "-+, ()[]{}/.:$^'`_*?<>#|;";
        private const string CLASS_PUNCTUATION = "-+,()[]{}/.:$^'`_*?<> ";

        private static readonly Dictionary<char, string> REPLACEMENTS = new Dictionary<char, string>
        {
            ['±'] = "+/-", // plus-minus sign
            ['∓'] = "-/+", // minus-or-plus sign
            ['′'] = "'", // prime
            ['″'] = "''", // double prime
            ['‴'] = "'''", // triple prime
            ['‘'] = "'",
            ['’'] = "'",
            ['‚'] = "'",
            ['´'] = "'",
            ['ʹ'] = "'",
            ['ʼ'] = "'",
            ['"'] = "'",
            ['“'] = "'",
            ['”'] = "'",
            ['‐'] = "-",
            ['‑'] = "-",
            ['‒'] = "-",
            ['–'] = "-",
            ['—'] = "-",
            ['―'] = "-",
            ['−'] = "-", // minus sign
            ['~'] = "-",
            ['='] = "-",
            ['&'] = "and",
            ['\\'] = "/",
            ['·'] = ".", // middle dot
            ['•'] = ".",
            ['×'] = "x", // multiplication sign
            ['→'] = "->",
        };

        /// <summary>
        /// The name in the characters CH$NAME and RECORD_TITLE accept: letters and digits of any script, space and
        /// -+,()[]{}/.:$^'`_*?&lt;&gt;#|; but never "; ". Symbols with a usual ASCII spelling are replaced (± as +/-,
        /// primes as ', dashes as -, &amp; as "and", = as -); any other character is removed.
        /// </summary>
        public static string SanitizeName(string name) {
            return SanitizeText(name, NAME_PUNCTUATION);
        }

        /// <summary>
        /// CH$COMPOUND_CLASS in the characters its grammar accepts, values joined by "; "; empty when nothing is left,
        /// in which case the optional line is omitted.
        /// </summary>
        public static string SanitizeCompoundClass(string compoundClass) {
            if (string.IsNullOrWhiteSpace(compoundClass)) {
                return string.Empty;
            }
            var values = compoundClass.Split(new[] { "; " }, StringSplitOptions.None)
                .Select(value => SanitizeText(value.Replace(';', ',').Replace('|', '/'), CLASS_PUNCTUATION))
                .Where(value => value.Length > 0);
            return string.Join("; ", values);
        }

        private static string SanitizeText(string text, string punctuation) {
            if (string.IsNullOrEmpty(text)) {
                return string.Empty;
            }
            var builder = new StringBuilder(text.Length);
            foreach (var c in text) {
                if (char.IsWhiteSpace(c) || char.IsControl(c)) {
                    builder.Append(' ');
                }
                else if (char.IsLetterOrDigit(c) || c == '_' || punctuation.IndexOf(c) >= 0) {
                    builder.Append(c);
                }
                else if (REPLACEMENTS.TryGetValue(c, out var replacement)) {
                    foreach (var r in replacement) {
                        if (char.IsLetterOrDigit(r) || punctuation.IndexOf(r) >= 0) {
                            builder.Append(r);
                        }
                    }
                }
                // Anything else is removed.
            }
            var result = Regex.Replace(builder.ToString(), " {2,}", " ").Trim();
            // "; " separates the parts of RECORD_TITLE and the values of CH$COMPOUND_CLASS.
            while (result.Contains("; ")) {
                result = result.Replace("; ", ", ");
            }
            return result;
        }

        private static IEnumerable<string> SplitComment(string comment) {
            if (string.IsNullOrWhiteSpace(comment)) {
                yield break;
            }
            foreach (var rawLine in comment.Split(new[] { "\r\n", "\r", "\n", }, StringSplitOptions.None)) {
                var line = rawLine.Trim();
                for (var start = 0; start < line.Length; start += MaxCommentLength) {
                    var chunk = line.Substring(start, Math.Min(MaxCommentLength, line.Length - start)).Trim();
                    // The Validator refuses an empty COMMENT and one that is only "CONFIDENCE".
                    if (chunk.Length > 0 && chunk != "CONFIDENCE") {
                        yield return chunk;
                    }
                }
            }
        }

        // ---------------------------------------------------------------- numbers

        private static bool IsPositiveNumber(double value) {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d;
        }

        private static double RetentionTimeInMinutes(RetentionTime rt) {
            if (rt is null) {
                return 0d;
            }
            switch (rt.Unit) {
                case ChromXUnit.Sec:
                    return rt.Value / 60d;
                case ChromXUnit.Msec:
                    return rt.Value / 60000d;
                default:
                    return rt.Value;
            }
        }
    }
}
