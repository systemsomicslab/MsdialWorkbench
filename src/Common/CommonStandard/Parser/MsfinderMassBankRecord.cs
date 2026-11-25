using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RfxIonMode = Rfx.Riken.OsakaUniv.IonMode;
using RfxPeak = Rfx.Riken.OsakaUniv.Peak;
using RfxProductIon = Rfx.Riken.OsakaUniv.ProductIon;
using RfxRawData = Rfx.Riken.OsakaUniv.RawData;

namespace CompMs.Common.Parser
{
    /// <summary>
    /// One formula candidate MS-FINDER gave a query: the neutral formula and the formulas it assigned to the product ions.
    /// </summary>
    public sealed class MsfinderFormulaCandidate
    {
        /// <param name="formula">The neutral formula of the candidate, e.g. "C5H11NO2".</param>
        /// <param name="productIons">
        /// The product ions MS-FINDER assigned under it (FormulaResult.ProductIonResult): the peak m/z in Mass and the
        /// elemental composition of the ion, without its charge, in Formula.
        /// </param>
        public MsfinderFormulaCandidate(string formula, IReadOnlyList<RfxProductIon> productIons) {
            Formula = formula ?? string.Empty;
            ProductIons = productIons ?? Array.Empty<RfxProductIon>();
        }

        public string Formula { get; }

        public IReadOnlyList<RfxProductIon> ProductIons { get; }
    }

    /// <summary>
    /// The MassBank record of one MS-FINDER query, written by <see cref="MassBankRecordHandler"/>, the writer MS-DIAL uses,
    /// so that both programs share one MassBank grammar.
    /// </summary>
    /// <remarks>
    /// The compound (name, structure, formula) is the query's own, as the file MS-FINDER read states it; MS-FINDER adds
    /// the formulas it assigned to the product ions, written as PK$ANNOTATION under the candidate formula that is
    /// CH$FORMULA (see the formulaAnnotations parameter of <see cref="MassBankRecordHandler.BuildRecord"/>).
    ///
    /// The handler carries what every record of an export shares (contributor, authors, licence, instrument and
    /// instrument type) and issues the accessions, so one handler serves a whole export. What belongs to a query (its ion
    /// mode, collision energy and comment) is set on the handler for the record and put back afterwards.
    /// </remarks>
    public static class MsfinderMassBankRecord
    {
        /// <summary>The software code of the record ID, <see cref="MassBankRecordHandler.Software"/>.</summary>
        public const string SoftwareCode = "MSFINDER";

        /// <summary>The software MS$DATA_PROCESSING: WHOLE names, <see cref="MassBankRecordHandler.ProcessingSoftware"/>.</summary>
        public const string ProcessingSoftware = "MS-FINDER";

        private const string UnknownName = "Unknown";

        /// <summary>
        /// Sets <paramref name="handler"/> up for MS-FINDER: the software code of the accession and the program and version
        /// MS$DATA_PROCESSING names.
        /// </summary>
        public static MassBankRecordHandler ForMsfinder(this MassBankRecordHandler handler, string msfinderVersion) {
            if (handler is null) {
                throw new ArgumentNullException(nameof(handler));
            }
            handler.Software = SoftwareCode;
            handler.ProcessingSoftware = ProcessingSoftware;
            handler.MsdialVersion = msfinderVersion ?? string.Empty;
            return handler;
        }

        /// <summary>The compound name of a query: its METABOLITENAME when it has one, otherwise its NAME.</summary>
        public static string CompoundName(RfxRawData rawData) {
            if (rawData is null) {
                throw new ArgumentNullException(nameof(rawData));
            }
            return string.IsNullOrWhiteSpace(rawData.MetaboliteName) ? rawData.Name ?? string.Empty : rawData.MetaboliteName;
        }

        /// <summary>
        /// How far the annotation of a query named <paramref name="name"/> goes: Unknown for no name, "Unknown", or the
        /// "Unknown|ID=...|MZ=..." MS-DIAL gives an unannotated spectrum it exports; Suggested for a name with one of
        /// MS-DIAL's suggestion prefixes; otherwise Unspecified, since MS-FINDER cannot tell how the name was reached.
        /// </summary>
        public static MassBankAnnotationLevel Classify(string name) {
            var value = name?.Trim() ?? string.Empty;
            if (value.Length == 0
                || string.Equals(value, UnknownName, StringComparison.OrdinalIgnoreCase)
                || value.StartsWith(UnknownName + "|", StringComparison.OrdinalIgnoreCase)) {
                return MassBankAnnotationLevel.Unknown;
            }
            return MassBankRecordHandler.IsSuggestionName(value) ? MassBankAnnotationLevel.Suggested : MassBankAnnotationLevel.Unspecified;
        }

        /// <summary>
        /// Builds the record of <paramref name="rawData"/> in memory; nothing is written.
        /// </summary>
        /// <param name="handler">The handler of the export, set up with <see cref="ForMsfinder"/>.</param>
        /// <param name="rawData">The query as MS-FINDER read it.</param>
        /// <param name="spectrum">The MS/MS peaks to write: centroided, with MS-FINDER's relative abundance cut-off applied.</param>
        /// <param name="formulaCandidates">MS-FINDER's formula candidates for the query, best first; empty when it has none.</param>
        /// <param name="recordId">The number that keeps the records of one export apart, e.g. the query's index in the project.</param>
        public static MassBankRecordResult Build(MassBankRecordHandler handler, RfxRawData rawData, IReadOnlyList<RfxPeak> spectrum, IReadOnlyList<MsfinderFormulaCandidate> formulaCandidates, int recordId) {
            if (handler is null) {
                throw new ArgumentNullException(nameof(handler));
            }
            if (rawData is null) {
                throw new ArgumentNullException(nameof(rawData));
            }

            var name = CompoundName(rawData);
            var level = Classify(name);
            var ionMode = ToIonMode(rawData.IonMode);
            var adduct = AdductIon.GetAdductIon(rawData.PrecursorType);
            var polarity = adduct.FormatCheck && adduct.HasAdduct ? adduct.IonMode : ionMode;
            var charge = polarity == IonMode.Negative ? -1 : 1;

            var chromXs = new ChromXs(
                new RetentionTime(rawData.RetentionTime, ChromXUnit.Min),
                new RetentionIndex(rawData.RetentionIndex),
                new DriftTime(-1d),
                new MzValue(rawData.PrecursorMz),
                ChromXType.RT);
            var molecule = new MoleculeMsReference
            {
                Name = name,
                SMILES = rawData.Smiles ?? string.Empty,
                InChIKey = rawData.InchiKey ?? string.Empty,
                Formula = new Formula { FormulaString = rawData.Formula ?? string.Empty, },
                Ontology = rawData.Ontology ?? string.Empty,
                AdductType = adduct,
                IonMode = ionMode,
                PrecursorMz = rawData.PrecursorMz,
                CollisionCrossSection = rawData.Ccs,
                ChromXs = chromXs,
                Spectrum = (spectrum ?? Array.Empty<RfxPeak>()).Where(p => p != null).Select(p => new SpectrumPeak(p.Mz, p.Intensity)).ToList(),
            };
            var peak = new ChromatogramPeak(recordId, rawData.PrecursorMz, rawData.Intensity, chromXs);

            var annotations = (formulaCandidates ?? Array.Empty<MsfinderFormulaCandidate>())
                .Where(c => c != null)
                .Select(c => new MassBankFormulaAnnotation(
                    c.Formula,
                    c.ProductIons
                        .Where(ion => ion?.Formula != null && !string.IsNullOrWhiteSpace(ion.Formula.FormulaString))
                        .Select(ion => new MassBankPeakAnnotation(ion.Mass, ion.Formula.FormulaString, charge))
                        .ToList()))
                .ToList();

            var savedIonMode = handler.IonMode;
            var savedCollisionEnergy = handler.CollisionEnergy;
            var savedComment = handler.Comment;
            try {
                handler.IonMode = ionMode;
                handler.CollisionEnergy = CollisionEnergyText(rawData.CollisionEnergy);
                handler.Comment = rawData.Comment ?? string.Empty;
                // MS-FINDER does not know MS-DIAL's chain verdict, so a dual lipid name is read at its class level.
                return handler.BuildRecord(peak, molecule, molecule, molecule, level, analysisFileId: null, lipidChainsResolved: false, formulaAnnotations: annotations);
            }
            finally {
                handler.IonMode = savedIonMode;
                handler.CollisionEnergy = savedCollisionEnergy;
                handler.Comment = savedComment;
            }
        }

        /// <summary>
        /// The collision energy as AC$MASS_SPECTROMETRY: COLLISION_ENERGY writes it: the number MS-FINDER read, without a
        /// unit, since the file it came from does not say one; empty when there is none.
        /// </summary>
        public static string CollisionEnergyText(double collisionEnergy) {
            if (double.IsNaN(collisionEnergy) || double.IsInfinity(collisionEnergy) || collisionEnergy <= 0d) {
                return string.Empty;
            }
            return collisionEnergy.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static IonMode ToIonMode(RfxIonMode ionMode) {
            switch (ionMode) {
                case RfxIonMode.Positive:
                    return IonMode.Positive;
                case RfxIonMode.Negative:
                    return IonMode.Negative;
                default:
                    return IonMode.Both;
            }
        }
    }
}
