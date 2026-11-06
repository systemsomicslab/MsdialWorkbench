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
    ///
    /// ACCESSION is "{Identifier}-{ContributorIdentifier}-{record ID}", and the record ID has one of two layouts:
    /// <list type="bullet">
    /// <item>an alignment spot: {Software}{yyMMddHHmmss}{spot ID:D8}, e.g. MSBNK-RIKEN_IMS-MSDIAL26110614302500000042;</item>
    /// <item>a peak of an analysis file (pass the file ID): {Software}{yyMMddHHmmss}F{file ID:D4}P{peak ID:D8},
    /// e.g. MSBNK-RIKEN_IMS-MSDIAL261106143025F0003P00000042.</item>
    /// </list>
    /// yyMMddHHmmss is the local time the handler was created, in the Gregorian calendar, the same for every record of
    /// the handler; create one handler per export (one per peak list session of several files). The time keeps two
    /// exports apart (the positive and the negative project of a study, two alignment results), and the file ID keeps
    /// apart the peaks of different files, whose peak IDs all start at 0. F and P mark where the file ID and the peak
    /// ID start, so the record ID still reads unambiguously when an ID is wider than its padding. A handler never
    /// issues an accession twice: a record whose accession it has already given to another is refused.
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

        /// <summary>The reason <see cref="BuildRecord"/> gives when the spectrum has no peak to print.</summary>
        public const string NoPeaksReason = "no MS/MS peaks";

        /// <summary>
        /// The reason <see cref="BuildRecord"/> gives for a record whose accession this handler has already issued; a set
        /// of records with one accession twice fails the Validator as a whole.
        /// </summary>
        public const string DuplicateAccessionReason = "its accession was already given to another record of this export";

        /// <summary>The longest record ID (the third block of ACCESSION) the MassBank record format accepts.</summary>
        public const int MaxRecordIdLength = 64;

        /// <summary>
        /// The longest <see cref="Software"/> code that leaves room in the record ID for the time and the widest IDs:
        /// 12 digits of time, F, P and two IDs of up to 10 digits each.
        /// </summary>
        public const int MaxSoftwareCodeLength = MaxRecordIdLength - 12 - 2 - 10 - 10;

        private const string AccessionTimeFormat = "yyMMddHHmmss";

        /// <summary>
        /// The longest AC$INSTRUMENT_TYPE that still leaves RECORD_TITLE ("name; type; MSn") within
        /// <see cref="MaxRecordTitleLength"/> for a one-character name.
        /// </summary>
        public const int MaxInstrumentTypeLength = MaxRecordTitleLength - 3 - 5; // "X; " and "; MSn"

        private const string UnknownName = "Unknown";
        private const string NotAvailable = "N/A";
        private const double ElectronMass = 0.00054857990946;
        private const char LineFeed = '\n';

        private static readonly CultureInfo INVARIANT = CultureInfo.InvariantCulture;
        private static readonly char[] LINE_BREAKS = { '\r', '\n', };

        private readonly Func<string, string> _splashCalculator;
        private readonly DateTime _now;
        private readonly HashSet<string> _issuedAccessions = new HashSet<string>(StringComparer.Ordinal);

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
            // The local time: DATE is the day the person exported, which in JST before 09:00 is not the UTC date, and the
            // accession carries the same time to the second.
            _now = DateTime.Now;
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

        /// <summary>
        /// The ACCESSION of the record of <paramref name="peak"/>: an alignment spot's when
        /// <paramref name="analysisFileId"/> is null, otherwise that of a peak of the analysis file with this ID. See the
        /// remarks of <see cref="MassBankRecordHandler"/> for the layouts.
        /// </summary>
        public string GetAccession(IChromatogramPeak peak, int? analysisFileId = null) {
            if (peak is null) {
                throw new ArgumentNullException(nameof(peak));
            }
            return $"{Identifier}-{ContributorIdentifier}-{GetRecordId(peak.ID, analysisFileId)}";
        }

        private string GetRecordId(int peakId, int? analysisFileId) {
            // The invariant culture formats dates with the Gregorian calendar; th-TH would otherwise give 2569.
            var time = _now.ToString(AccessionTimeFormat, INVARIANT);
            return analysisFileId.HasValue
                ? $"{Software}{time}F{analysisFileId.Value.ToString("D4", INVARIANT)}P{peakId.ToString("D8", INVARIANT)}"
                : $"{Software}{time}{peakId.ToString("D8", INVARIANT)}";
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
            else if (InstrumentType.Length > MaxInstrumentTypeLength) {
                problems.Add($"Instrument type is {InstrumentType.Length.ToString(INVARIANT)} characters long; RECORD_TITLE leaves room for at most {MaxInstrumentTypeLength.ToString(INVARIANT)}.");
            }
            // The value is written with control characters replaced by spaces, so it must still say something then.
            if (!MassBankVocabulary.IsValidAuthors(Authors) || CleanLine(Authors).Length == 0) {
                problems.Add("Authors must be given, on one line.");
            }
            if (!MassBankVocabulary.IsValidInstrument(Instrument) || CleanLine(Instrument).Length == 0) {
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
            else if (Software.Length > MaxSoftwareCodeLength) {
                problems.Add($"Accession software code \"{Software}\" is longer than {MaxSoftwareCodeLength.ToString(INVARIANT)} characters.");
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
            if (!TryWriteRecord(stream, peak, molecule, scan, ionProperty, MassBankAnnotationLevel.Unspecified, lipidChainsResolved: false, out var reason)) {
                throw new InvalidOperationException($"MassBank record {GetAccession(peak)} was not written: {reason}");
            }
        }

        /// <summary>
        /// Writes the record when it can be built; otherwise writes nothing, returns false and says why.
        /// </summary>
        public bool TryWriteRecord(Stream stream, IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty, out string reason) {
            return TryWriteRecord(stream, peak, molecule, scan, ionProperty, MassBankAnnotationLevel.Unspecified, lipidChainsResolved: false, out reason);
        }

        /// <inheritdoc cref="TryWriteRecord(Stream, IChromatogramPeak, IMoleculeProperty, IMSScanProperty, IIonProperty, out string)"/>
        /// <param name="lipidChainsResolved">See <see cref="BuildRecord"/>.</param>
        public bool TryWriteRecord(Stream stream, IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty, MassBankAnnotationLevel annotationLevel, bool lipidChainsResolved, out string reason) {
            if (stream is null) {
                throw new ArgumentNullException(nameof(stream));
            }
            var result = BuildRecord(peak, molecule, scan, ionProperty, annotationLevel, lipidChainsResolved: lipidChainsResolved);
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
        /// <param name="analysisFileId">
        /// The ID of the analysis file <paramref name="peak"/> belongs to, for a record of a peak list; null for an
        /// alignment spot. It selects the accession layout; see the remarks of <see cref="MassBankRecordHandler"/>.
        /// </param>
        /// <param name="lipidChainsResolved">
        /// MS-DIAL's chain verdict for the annotation (<c>MsScanMatchResult.IsLipidChainsMatch</c> of the representative
        /// match), which decides the half of a dual lipid name ("PC 34:1|PC 16:0_18:1") CH$NAME is: the chain-level one
        /// when true, the class-level one when false. False is the safe reading when the verdict is not known.
        /// </param>
        /// <remarks>
        /// A record that is built takes its accession for good: a later record of this handler with the same accession
        /// is refused with <see cref="DuplicateAccessionReason"/>. A refused record takes nothing.
        ///
        /// CH$NAME and RECORD_TITLE carry the compound name every MS-DIAL export shows,
        /// <see cref="AnnotationName.Canonical(string, bool)"/>: MS-DIAL's suggestion prefix removed and a dual lipid
        /// name reduced to the level the evidence supports. A suggestion also loses the markers "Unsettled:" and
        /// "Putative:", because the record states its level in COMMENT. Whenever the written name differs from the
        /// spot's own, "COMMENT: Original name:" keeps the latter whole.
        /// </remarks>
        public MassBankRecordResult BuildRecord(IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty, MassBankAnnotationLevel annotationLevel = MassBankAnnotationLevel.Unspecified, int? analysisFileId = null, bool lipidChainsResolved = false) {
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

            var accession = GetAccession(peak, analysisFileId);
            var notes = new List<string>();
            MassBankRecordResult Refuse(string why) => new MassBankRecordResult(accession, null, why, notes);

            var metadataProblems = ValidateMetadata();
            if (metadataProblems.Count > 0) {
                return Refuse("the project metadata is not valid for MassBank: " + string.Join(" ", metadataProblems));
            }
            if (peak.ID < 0) {
                return Refuse($"the peak ID {peak.ID.ToString(INVARIANT)} cannot form an accession");
            }
            if (analysisFileId < 0) {
                return Refuse($"the analysis file ID {analysisFileId.Value.ToString(INVARIANT)} cannot form an accession");
            }
            if (IsIssued(accession)) {
                return Refuse(DuplicateAccessionReason);
            }

            // Peaks: sorted, merged where they print the same, and only what PK$PEAK prints feeds SPLASH.
            var peaks = PreparePeaks(scan.Spectrum);
            if (peaks.Count == 0) {
                return Refuse(NoPeaksReason);
            }

            // Polarity: the spot's own adduct; the handler's ion mode only when the adduct does not say.
            var adduct = ionProperty.AdductType ?? AdductIon.Default;
            var adductKnown = adduct.FormatCheck && adduct.HasAdduct;
            var polarity = adductKnown ? adduct.IonMode : IonMode;
            if (polarity != IonMode.Positive && polarity != IonMode.Negative) {
                return Refuse($"the polarity is {polarity}, but a MassBank record is POSITIVE or NEGATIVE");
            }

            // Name: the compound name every MS-DIAL export shows, so that CH$NAME never claims more than the other files
            // of the run (a suggestion's prefix, or the sn-chains of a lipid whose chains nothing resolved).
            var isUnknown = annotationLevel == MassBankAnnotationLevel.Unknown;
            var originalName = molecule.Name ?? string.Empty;
            var prefixRemoved = false;
            var dualNameReduced = false;
            var compoundName = isUnknown ? UnknownName : CompoundName(originalName, annotationLevel, lipidChainsResolved, out prefixRemoved, out dualNameReduced);
            var name = isUnknown ? UnknownName : SanitizeName(compoundName);
            var hasName = name.Length > 0;
            if (!hasName) {
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
            if (structure != null && !hasName) {
                // "Unknown" next to a structure would name the compound unknown while identifying it.
                return Refuse("the compound has a structure but no name in characters MassBank accepts");
            }
            string formula;
            double exactMass;
            var isotopeLabelled = false;
            string inferredFrom = null;
            if (structure != null) {
                formula = structure.Formula;
                exactMass = structure.MonoisotopicMass;
                isotopeLabelled = structure.IsIsotopeLabelled;
            }
            else {
                var libraryFormula = isUnknown ? null : ParseLibraryFormula(molecule.Formula?.FormulaString);
                if (libraryFormula != null) {
                    formula = libraryFormula.Formula;
                    exactMass = libraryFormula.MonoisotopicMass;
                    isotopeLabelled = libraryFormula.IsIsotopeLabelled;
                }
                else {
                    formula = NotAvailable;
                    exactMass = adductKnown ? NeutralMass(adduct, peak.Mass) : double.NaN;
                    if (!IsPositiveNumber(exactMass)) {
                        return Refuse("there is no formula, and the neutral mass cannot be derived from the precursor m/z and adduct");
                    }
                    inferredFrom = adduct.AdductIonName;
                    notes.Add("CH$EXACT_MASS is the neutral mass inferred from the precursor m/z and adduct");
                }
            }

            var record = new StringBuilder();
            void Line(string text) {
                record.Append(text).Append(LineFeed);
            }

            Line($"ACCESSION: {accession}");

            var titleSuffix = $"; {InstrumentType}; {MSType}";
            if (titleSuffix.Length + 1 > MaxRecordTitleLength) {
                return Refuse($"the instrument type is too long for RECORD_TITLE ({MaxRecordTitleLength.ToString(INVARIANT)} characters)");
            }
            var titleName = name;
            if (titleName.Length + titleSuffix.Length > MaxRecordTitleLength) {
                titleName = Cut(titleName, MaxRecordTitleLength - titleSuffix.Length).TrimEnd();
                if (titleName.Length == 0) {
                    titleName = name.Substring(0, 1);
                }
                notes.Add("RECORD_TITLE was shortened to 600 characters");
            }
            Line($"RECORD_TITLE: {titleName}{titleSuffix}");
            Line($"DATE: {_now.ToString("yyyy.MM.dd", INVARIANT)}");
            Line($"AUTHORS: {CleanLine(Authors)}");
            Line($"LICENSE: {License}");

            // The project comment comes first; the handler's own comments follow it.
            foreach (var comment in SplitComment(Comment)) {
                Line($"COMMENT: {comment}");
            }
            if (nameChanged) {
                Line($"COMMENT: {Cut("Original name: " + CleanLine(originalName), MaxCommentLength).TrimEnd()}");
                if (prefixRemoved) {
                    notes.Add("the suggestion prefix was removed from CH$NAME");
                }
                if (dualNameReduced) {
                    notes.Add(DualNameNote);
                }
                if (!string.Equals(name, compoundName.Trim(), StringComparison.Ordinal)) {
                    notes.Add("the name was changed to characters MassBank accepts");
                }
            }
            if (annotationLevel == MassBankAnnotationLevel.Suggested) {
                Line($"COMMENT: {SuggestedAnnotationComment}");
            }
            if (isotopeLabelled) {
                Line($"COMMENT: {IsotopeLabelledComment}");
            }
            if (inferredFrom != null) {
                Line($"COMMENT: {Cut($"CH$EXACT_MASS was calculated from PRECURSOR_M/Z and the adduct {CleanLine(inferredFrom)}; it is not a reference value", MaxCommentLength)}");
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

            Line($"AC$INSTRUMENT: {CleanLine(Instrument)}");
            Line($"AC$INSTRUMENT_TYPE: {InstrumentType}");
            Line($"AC$MASS_SPECTROMETRY: MS_TYPE {MSType}");
            Line($"AC$MASS_SPECTROMETRY: ION_MODE {(polarity == IonMode.Positive ? "POSITIVE" : "NEGATIVE")}");
            var collisionEnergy = CleanLine(CollisionEnergy);
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
            // rel.int. from the intensities as printed, so that the two columns agree for a reader.
            var maxIntensity = peaks.Max(p => p.PrintedIntensity);
            foreach (var p in peaks) {
                var relative = (int)Math.Round(p.PrintedIntensity / maxIntensity * 999d, MidpointRounding.AwayFromZero);
                Line($"  {p.MzText} {p.IntensityText} {relative.ToString(INVARIANT)}");
            }
            Line("//");

            // Taken only now that the record is complete, and checked again: the set a handler writes never holds an
            // accession twice.
            if (!TryIssue(accession)) {
                return Refuse(DuplicateAccessionReason);
            }
            return new MassBankRecordResult(accession, record.ToString(), null, notes);
        }

        private bool IsIssued(string accession) {
            lock (_issuedAccessions) {
                return _issuedAccessions.Contains(accession);
            }
        }

        private bool TryIssue(string accession) {
            lock (_issuedAccessions) {
                return _issuedAccessions.Add(accession);
            }
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

        // MS-DIAL's markers of a suggestion that AnnotationName.Canonical leaves alone, because the other exports keep
        // them in the name: "Unsettled:" (set by hand) and "Putative:" (another spot kept the same reference). A MassBank
        // record states the level in COMMENT instead.
        private static readonly string[] LEVEL_MARKERS = { "Unsettled", "Putative", };

        private static readonly string[] SUGGESTION_PREFIXES = new[] {
            AnnotationName.LowScorePrefix, AnnotationName.NoMs2Prefix, AnnotationName.WithoutMs2Prefix,
        }.Concat(LEVEL_MARKERS).ToArray();

        /// <summary>The note a record carries when CH$NAME is one half of a dual lipid name.</summary>
        private const string DualNameNote = "the dual lipid name was written at the level its evidence supports";

        /// <summary>
        /// The name CH$NAME and RECORD_TITLE are made from, before sanitising: <see cref="AnnotationName.Canonical"/>, the
        /// name every MS-DIAL export shows, and for a suggestion also without the <see cref="LEVEL_MARKERS"/>.
        /// </summary>
        private static string CompoundName(string name, MassBankAnnotationLevel level, bool lipidChainsResolved, out bool prefixRemoved, out bool dualNameReduced) {
            var original = name.Trim();
            var value = level == MassBankAnnotationLevel.Suggested ? WithoutSuggestionMarkers(original) : original;
            // The suggestion prefixes themselves are AnnotationName's, so that the list cannot drift from the writers'.
            var withoutPrefix = AnnotationName.WithoutPrefix(value);
            var canonical = AnnotationName.Canonical(value, lipidChainsResolved);
            prefixRemoved = !string.Equals(withoutPrefix, original, StringComparison.Ordinal);
            dualNameReduced = !string.Equals(canonical, withoutPrefix, StringComparison.Ordinal);
            return canonical;
        }

        // "Unsettled: low score: Glycine" and "Putative: low score: Glycine" -> "Glycine": the level markers and
        // AnnotationName's suggestion prefixes, in whatever order MS-DIAL stacked them.
        private static string WithoutSuggestionMarkers(string name) {
            var value = name.Trim();
            while (true) {
                var next = AnnotationName.WithoutPrefix(WithoutLevelMarker(value)).Trim();
                if (string.Equals(next, value, StringComparison.Ordinal)) {
                    return value;
                }
                value = next;
            }
        }

        private static string WithoutLevelMarker(string name) {
            foreach (var marker in LEVEL_MARKERS) {
                if (!name.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                var rest = name.Substring(marker.Length).TrimStart();
                if (rest.StartsWith(":", StringComparison.Ordinal)) {
                    return rest.Substring(1).Trim();
                }
            }
            return name;
        }

        // ---------------------------------------------------------------- peaks

        private sealed class PrintedPeak
        {
            public string MzText;
            public string IntensityText;
            public double Intensity;
            public double PrintedIntensity;
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
                // Near double.MaxValue the printed text rounds out of range (.NET Framework cannot parse it back); such a
                // spectrum is refused when its SPLASH cannot be computed from the same text.
                var printedIntensity = double.TryParse(intensityText, NumberStyles.Float, INVARIANT, out var parsed) && IsPositiveNumber(parsed) ? parsed : p.Intensity;
                var peak = new PrintedPeak
                {
                    MzText = p.Mass.ToString("F5", INVARIANT),
                    IntensityText = intensityText,
                    Intensity = p.Intensity,
                    PrintedIntensity = printedIntensity,
                    Comment = p.SpectrumComment,
                };
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
            public bool IsIsotopeLabelled;
        }

        private sealed class FormulaAndMass
        {
            /// <summary>Hill order, isotopes collapsed into their element, "[...]n+" when charged.</summary>
            public string Formula;
            /// <summary>The mass with the isotopes as given (the labelled compound), less the electrons of the charge.</summary>
            public double MonoisotopicMass;
            /// <summary>The mass of <see cref="Formula"/> with the most abundant isotope of each element.</summary>
            public double CollapsedMass;

            /// <summary>True when the isotopes given make the mass differ from that of <see cref="Formula"/>.</summary>
            public bool IsIsotopeLabelled => Math.Abs(MonoisotopicMass - CollapsedMass) > 1e-6;
        }

        /// <summary>The COMMENT written when CH$EXACT_MASS is that of an isotope-labelled compound.</summary>
        private const string IsotopeLabelledComment = "CH$EXACT_MASS is the monoisotopic mass of the isotope-labelled compound; CH$FORMULA cannot show isotopes";

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
                // The formula and the mass of the structure as the SMILES gives it, with its isotopes.
                var fromSmiles = FormulaOf(container, useMassNumbers: true);
                if (fromSmiles is null) {
                    note = "the structure was written as N/A: the formula of the structure could not be determined";
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
                // The formula of the structure the InChI describes, which is what the Validator compares CH$FORMULA
                // with. Its element symbols are used, not its mass numbers, which NCDK gets wrong for isotopes lighter
                // than the element's average mass ([79Br], [18F]); the mass, with the isotopes, comes from the SMILES.
                var toStructure = InChIGeneratorFactory.Instance.GetInChIToStructure(inchi, CDK.Builder);
                if ((toStructure.ReturnStatus != InChIReturnCode.Ok && toStructure.ReturnStatus != InChIReturnCode.Warning) || toStructure.AtomContainer is null) {
                    note = $"the structure was written as N/A: the InChI could not be read back ({toStructure.ReturnStatus})";
                    return null;
                }
                var fromInChI = FormulaOf(toStructure.AtomContainer, useMassNumbers: false);
                if (fromInChI is null) {
                    note = "the structure was written as N/A: the formula of the structure could not be determined";
                    return null;
                }
                if (!string.Equals(fromInChI.Formula, fromSmiles.Formula, StringComparison.Ordinal)) {
                    // e.g. an explicit [1H], which the InChI leaves out of its formula.
                    note = "the structure was written as N/A: its InChI does not keep the formula of the SMILES";
                    return null;
                }
                // CH$SMILES is NCDK's own SMILES of the structure the InChI was made from, not the library text: for
                // some double bonds (imines, conjugated rings) NCDK and the Validator's CDK read the stereo of the
                // library text differently, and the Validator requires the InChIKey of CH$SMILES to equal CH$IUPAC's.
                var smilesOut = new SmilesGenerator(SmiFlavors.Isomeric).Create(container);
                if (string.IsNullOrEmpty(smilesOut) || smilesOut.Length > MaxSmilesLength || smilesOut.Any(char.IsWhiteSpace)) {
                    note = "the structure was written as N/A: the SMILES written for it would be too long";
                    return null;
                }
                return new Structure
                {
                    Smiles = smilesOut,
                    InChI = inchi,
                    InChIKey = key,
                    Formula = fromInChI.Formula,
                    MonoisotopicMass = fromSmiles.MonoisotopicMass,
                    IsIsotopeLabelled = fromSmiles.IsIsotopeLabelled,
                };
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException || ex is DllNotFoundException || ex is BadImageFormatException || ex is EntryPointNotFoundException)) {
                // One unusual structure must not stop the export of the others. The note names the exception type
                // only, so that the export summary counts such structures together.
                note = $"the structure was written as N/A: NCDK could not process the SMILES ({ex.GetType().Name})";
                return null;
            }
        }

        /// <param name="useMassNumbers">
        /// True to take each atom's mass number for the mass; false to take the most abundant isotope throughout.
        /// </param>
        private static FormulaAndMass FormulaOf(IAtomContainer container, bool useMassNumbers) {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var mass = 0d;
            var collapsedMass = 0d;
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
                var majorMass = IsotopeMass(symbol, null);
                var atomMass = useMassNumbers ? IsotopeMass(symbol, atom.MassNumber) : majorMass;
                if (!atomMass.HasValue || !majorMass.HasValue) {
                    return null;
                }
                Add(counts, symbol, 1);
                mass += atomMass.Value;
                collapsedMass += majorMass.Value;
                var hydrogens = atom.ImplicitHydrogenCount ?? 0;
                if (hydrogens > 0) {
                    Add(counts, "H", hydrogens);
                    mass += hydrogens * hydrogenMass.Value;
                    collapsedMass += hydrogens * hydrogenMass.Value;
                }
                charge += atom.FormalCharge ?? 0;
            }
            if (counts.Count == 0) {
                return null;
            }
            return new FormulaAndMass
            {
                Formula = HillFormula(counts, charge),
                MonoisotopicMass = mass - charge * ElectronMass,
                CollapsedMass = collapsedMass - charge * ElectronMass,
            };
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
            var collapsedMass = 0d;
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
                var majorMass = IsotopeMass(symbol, null);
                if (!atomMass.HasValue || !majorMass.HasValue) {
                    return null;
                }
                Add(counts, symbol, count);
                mass += count * atomMass.Value;
                collapsedMass += count * majorMass.Value;
            }
            if (counts.Values.All(c => c <= 0)) {
                return null;
            }
            return new FormulaAndMass { Formula = HillFormula(counts, 0), MonoisotopicMass = mass, CollapsedMass = collapsedMass, };
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
                else if (IsLetterOrDigitForValidator(c) || c == '_' || punctuation.IndexOf(c) >= 0) {
                    builder.Append(c);
                }
                else if (REPLACEMENTS.TryGetValue(c, out var replacement)) {
                    foreach (var r in replacement) {
                        if (IsLetterOrDigitForValidator(r) || punctuation.IndexOf(r) >= 0) {
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

        /// <summary>
        /// A letter or digit for the Validator's word(), which is Java's Character.isLetterOrDigit(char). .NET follows
        /// another Unicode version: .NET Framework calls U+1885 and U+1886 letters, which Unicode 9 made marks, and
        /// .NET 8 and later know letters that Java 17 (Unicode 13) does not. Those are refused here; surrogates, which
        /// neither calls letters, already are.
        /// </summary>
        private static bool IsLetterOrDigitForValidator(char c) {
            return char.IsLetterOrDigit(c) && !IsNewerUnicodeLetter(c);
        }

        // BMP characters that .NET Framework 4.8, .NET 8 or .NET 10 counts as letters or digits and Java 17 does not,
        // found by comparing char.IsLetterOrDigit with Character.isLetterOrDigit over U+0000 to U+FFFF.
        private static bool IsNewerUnicodeLetter(char c) {
            return (c >= 'ࡰ' && c <= 'ࢇ') || (c >= 'ࢉ' && c <= 'ࢎ') || c == 'ࢵ' || c == 'ࣈ' || c == 'ࣉ'
                || c == 'ౝ' || c == 'ೝ' || c == 'ᜍ' || c == 'ᜟ' || c == 'ᢅ' || c == 'ᢆ' || c == 'ᭌ'
                || c == 'Ᲊ' || c == 'ᲊ' || c == 'Ⱟ' || c == 'ⱟ' || (c >= '鿽' && c <= '鿿')
                || c == 'Ꟁ' || c == 'ꟁ' || (c >= 'Ɤ' && c <= 'ꟍ') || c == 'Ꟑ' || c == 'ꟑ' || c == 'ꟓ'
                || (c >= 'ꟕ' && c <= 'Ƛ') || (c >= 'ꟲ' && c <= 'ꟴ');
        }

        /// <summary>
        /// A one-line value as the record writes it: line breaks and other control characters become spaces, and the
        /// ends are trimmed as Java's String.trim() and .NET's Trim() both would.
        /// </summary>
        private static string CleanLine(string value) {
            if (string.IsNullOrEmpty(value)) {
                return string.Empty;
            }
            var builder = new StringBuilder(value.Length);
            foreach (var c in value) {
                builder.Append(c < ' ' || c == '\u007F' ? ' ' : c);
            }
            return JavaTrim(builder.ToString().Trim());
        }

        // Java's String.trim(): every character up to U+0020 at either end.
        private static string JavaTrim(string value) {
            var start = 0;
            var end = value.Length;
            while (start < end && value[start] <= ' ') {
                start++;
            }
            while (end > start && value[end - 1] <= ' ') {
                end--;
            }
            return value.Substring(start, end - start);
        }

        /// <summary>At most <paramref name="length"/> UTF-16 units of <paramref name="value"/>, never half a surrogate pair.</summary>
        private static string Cut(string value, int length) {
            if (value.Length <= length) {
                return value;
            }
            if (length <= 0) {
                return string.Empty;
            }
            return value.Substring(0, char.IsHighSurrogate(value[length - 1]) ? length - 1 : length);
        }

        private static IEnumerable<string> SplitComment(string comment) {
            if (string.IsNullOrWhiteSpace(comment)) {
                yield break;
            }
            foreach (var rawLine in comment.Split(new[] { "\r\n", "\r", "\n", }, StringSplitOptions.None)) {
                var rest = CleanLine(rawLine);
                while (rest.Length > 0) {
                    var chunk = Cut(rest, MaxCommentLength);
                    if (chunk.Length == 0) {
                        chunk = rest.Substring(0, Math.Min(2, rest.Length));
                    }
                    rest = rest.Substring(chunk.Length);
                    chunk = CleanLine(chunk);
                    // The Validator refuses an empty COMMENT and one that is only "CONFIDENCE" after Java's trim().
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

        /// <summary>
        /// The neutral monoisotopic mass M of the molecule behind a precursor of <paramref name="mz"/> with
        /// <paramref name="adduct"/>: (m/z z - A + e z) / n for a cation and (m/z z - A - e z) / n for an anion, where z
        /// is the charge, A the mass the adduct adds, e the electron mass and n the number of molecules (2 for [2M+H]+).
        /// NaN when the adduct gives no positive charge or count, or no polarity.
        /// </summary>
        /// <remarks>
        /// Not <see cref="AdductIon.ConvertToExactMass"/>, which adds the electrons after dividing by n, so that the
        /// mass of a multimer is off by e (n - 1) z / n: +0.27 mDa for [2M+H]+.
        /// </remarks>
        private static double NeutralMass(AdductIon adduct, double mz) {
            var charge = adduct.ChargeNumber;
            var count = adduct.AdductIonXmer;
            if (charge <= 0 || count <= 0) {
                return double.NaN;
            }
            var electrons = ElectronMass * charge;
            var ionMass = mz * charge - adduct.AdductIonAccurateMass;
            switch (adduct.IonMode) {
                case IonMode.Positive:
                    return (ionMass + electrons) / count;
                case IonMode.Negative:
                    return (ionMass - electrons) / count;
                default:
                    return double.NaN;
            }
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
