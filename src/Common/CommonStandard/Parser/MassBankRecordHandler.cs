using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using NCDK.Graphs.InChI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Parser
{
    public sealed class MassBankRecordHandler
    {
        public static ReadOnlyCollection<string> AvailableLicenses { get; } = new List<string>
        {
            "CC BY", "CC0", "CC BY-NC", "CC BY-NC-SA", "CC BY-SA",
        }.AsReadOnly();


        private readonly Func<IMSScanProperty, string> _splashCalculator;
        private readonly DateTime _now;

        public MassBankRecordHandler(IonMode ionMode, string instrumentType, Func<IMSScanProperty, string> splashCalculator)
        {
            IonMode = ionMode;
            InstrumentType = instrumentType ?? throw new ArgumentNullException(nameof(instrumentType));
            _splashCalculator = splashCalculator ?? throw new ArgumentNullException(nameof(splashCalculator));
            _now = DateTime.UtcNow;
        }

        public string Identifier { get; set; } = "MSBNK";
        public string Software { get; set; } = "MSDIAL";

        public string ContributorIdentifier { get; set; } = "CONTRIBUTOR_IDENTIFIER";

        public string Authors { get; set; } = "Authors";
        public string License {
            get => _license;
            set {
                if (AvailableLicenses.Contains(value)) {
                    _license = value;
                }
            }
        }
        private string _license = "CC BY";

        public string Instrument { get; set; } = "N/A";

        public string InstrumentType { get; set; }

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

        public string MSType => $"MS{_msLevel}";

        public string GetAccession(IChromatogramPeak peak) => $"{Identifier}-{ContributorIdentifier}-{Software}{_now:yyMMdd}{peak.ID:D8}";

        public void WriteRecord(Stream stream, IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan, IIonProperty ionProperty) {
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 4096, leaveOpen: true)) {
                // accession
                writer.WriteLine($"ACCESSION: {GetAccession(peak)}");

                // record title
                writer.WriteLine($"RECORD_TITLE: {molecule.Name}; {InstrumentType}; {MSType}");

                // date
                writer.WriteLine($"DATE: {_now:yyyy.MM.dd}");

                // authors
                writer.WriteLine($"AUTHORS: {Authors}");

                // license
                writer.WriteLine($"LICENSE: {License}");

                // copyright
                // publication
                // project

                // comment
                //WriteLineIfNotEmpty(writer, "COMMENT: {0}", peak.Comment);
                
                // ch$name
                writer.WriteLine($"CH$NAME: {molecule.Name}");

                // ch$compound class Natural product or Non-Natural product
                writer.WriteLine($"CH$COMPOUND_CLASS: {molecule.Ontology}");

                // ch$formula
                writer.WriteLine($"CH$FORMULA: {molecule.Formula}");

                // ch$exact_mass
                writer.WriteLine($"CH$EXACT_MASS: {ionProperty.AdductType.ConvertToExactMass(peak.Mass):F5}");

                // ch$smiles
                writer.WriteLine($"CH$SMILES: {molecule.SMILES}");

                // ch$iupac
                try {
                    var inchi = InChIGeneratorFactory.Instance.GetInChIGenerator(molecule.ToAtomContainer()).InChI;
                    writer.WriteLine($"CH$IUPAC: {inchi}");
                }
                catch (NCDK.InvalidSmilesException) {
                    writer.WriteLine($"CH$IUPAC: N/A");
                }

                // ch$cdkdepict

                // ch$link
                WriteLineIfNotEmpty(writer, "CH$LINK: INCHIKEY: {0}", molecule.InChIKey);

                // sp$scientific_name
                // sp$lineage
                // sp$link
                // sp$sample

                // ac$instrument
                writer.WriteLine($"AC$INSTRUMENT: {Instrument}");

                // ac$instrument_type
                writer.WriteLine($"AC$INSTRUMENT_TYPE: {InstrumentType}");

                // ac$mass_spectrometry mstype
                writer.WriteLine($"AC$MASS_SPECTROMETRY: MS_TYPE {MSType}");

                // ac$mass_spectrometry: ion_mode
                writer.WriteLine($"AC$MASS_SPECTROMETRY: ION_MODE {IonMode.ToString().ToUpperInvariant()}");

                // ac$mass_spectrometry: subtag
                //WriteLineIfPositive(writer, "AC$MASS_SPECTROMETRY: COLLISION_ENERGY {0}", scan.CollisionEnergy);

                // ac$chromatography: subtag
                WriteLineIfPositive(writer, "AC$CHROMATOGRAPHY: CCS {0}", ionProperty.CollisionCrossSection);
                WriteLineIfPositive(writer, "AC$CHROMATOGRAPHY: KOVATS_RTI {0}", peak.ChromXs.RI.Value);
                WriteLineIfPositive(writer, "AC$CHROMATOGRAPHY: RETENTION_TIME {0}", peak.ChromXs.RT.Value);

                // ac$general: subtag
                // ac$ion_mobility: subtag

                // ms$focused_ion: base_peak
                // ms$focused_ion: subtag
                writer.WriteLine($"MS$FOCUSED_ION: PRECURSOR_M/Z {peak.Mass:F5}");
                writer.WriteLine($"MS$FOCUSED_ION: PRECURSOR_TYPE {ionProperty.AdductType.AdductIonName}");

                // ms$data_processing: subtag

                // pk$splash
                var splash = _splashCalculator.Invoke(scan);
                writer.WriteLine($"PK$SPLASH: {splash}");

                // pk$annotation
                writer.WriteLine("PK$ANNOTATION: m/z type");
                foreach (var p in scan.Spectrum) {
                    writer.WriteLine($"  {p.Mass:F5} {p.SpectrumComment}");
                }

                // pk$num_peak
                writer.WriteLine($"PK$NUM_PEAK: {scan.Spectrum.Count}");

                // pk$peak
                writer.WriteLine("PK$PEAK: m/z int. rel.int.");
                var spec = scan.Spectrum.Select(p => new { mz = p.Mass, intensity = p.Intensity }).ToArray();
                var maxIntensity = spec.DefaultIfEmpty().Max(p => p?.intensity ?? 1d);

                foreach (var p in spec) {
                    writer.WriteLine($"  {p.mz:F5} {p.intensity:F2} {p.intensity / maxIntensity * 999:F0}");
                }

                writer.WriteLine("//");
            }
        }

        private static void WriteLineIfNotEmpty(StreamWriter writer, string format, string arg0) {
            if (!string.IsNullOrEmpty(arg0)) {
                writer.WriteLine(format, arg0);
            }
        }

        private static void WriteLineIfPositive(StreamWriter writer, string format, double arg0) {
            if (arg0 > 0) {
                writer.WriteLine(format, arg0);
            }
        }
    }
}
