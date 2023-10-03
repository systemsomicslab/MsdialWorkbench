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

        public MassBankRecordHandler(IonMode ionMode, string instrumentType, Func<IMSScanProperty, string> splashCalculator)
        {
            IonMode = ionMode;
            InstrumentType = instrumentType ?? throw new ArgumentNullException(nameof(instrumentType));
            _splashCalculator = splashCalculator ?? throw new ArgumentNullException(nameof(splashCalculator));
        }

        public string Identifier { get; set; } = "MSDIAL";
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

        public string GetAccession(IChromatogramPeak peak) => $"{Identifier}-{ContributorIdentifier}-{peak.ID:D8}";

        public void WriteRecord(Stream stream, IChromatogramPeak peak, IMoleculeProperty molecule, IMSScanProperty scan) {
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 4096, leaveOpen: true)) {
                // accession
                writer.WriteLine($"ACCESSION: {Identifier}-{ContributorIdentifier}-{peak.ID:D8}");

                // record title
                writer.WriteLine($"RECORD_TITLE: {molecule.Name}; {InstrumentType}; {MSType}");

                // date
                var now = DateTime.UtcNow;
                writer.WriteLine($"DATE: {now:yyyy.MM.dd}");

                // authors
                writer.WriteLine($"AUTHORS: {Authors}");

                // license
                writer.WriteLine($"LICENSE: {License}");

                // copyright
                // publication
                // project
                // comment
                
                // ch$name
                writer.WriteLine($"CH$NAME: {molecule.Name}");

                // ch$compound class Natural product or Non-Natural product
                writer.WriteLine($"CH$COMPOUND_CLASS: {molecule.Ontology}");

                // ch$formula
                writer.WriteLine($"CH$Formula: {molecule.Formula}");

                // ch$exact_mass
                writer.WriteLine($"CH$EXACT_MASS: {(double)peak.Mass:F5}");

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

                // sp$scientific_name
                // sp$lineage
                // sp$link
                // sp$sample

                // ac$instrument
                writer.WriteLine($"AC$INSTRUMENT: {Instrument}");

                // ac$instrument_type
                writer.WriteLine($"AC$INSTRUMENT_TYPE: {InstrumentType}");

                // ac$mass_spectrometry: ion_mode
                writer.WriteLine($"AC$MASS_SPECTROMETRY: ION_MODE {IonMode}");

                // ac$mass_spectrometry mstype
                writer.WriteLine($"AC$MASS_SPECTROMETRY: MS_TYPE {MSType}");

                // ac$mass_spectrometry: subtag
                // ac$chromatography: subtag
                // ac$general: subtag
                // ac$ion_mobility: subtag

                // ms$focused_ion: base_peak
                // ms$focused_ion: subtag
                // ms$data_processing: subtag

                // pk$splash
                var splash = _splashCalculator.Invoke(scan);
                writer.WriteLine($"PK$SPLASH: {splash}");

                // pk$annotation

                // pk$num_peak
                writer.WriteLine($"PK$NUM_PEAK: {scan.Spectrum.Count}");

                // pk$peak
                writer.WriteLine("PK$PEAK: m/z int. rel.int.");
                var spec = scan.Spectrum.Select(p => new { mz = p.Mass, intensity = p.Intensity }).ToArray();
                var maxIntensity = spec.Max(p => p.intensity);

                foreach (var p in spec) {
                    writer.WriteLine($"  {p.mz} {p.intensity} {p.intensity / maxIntensity * 999:F0}");
                }

                writer.WriteLine("//");
            }
        }
    }
}
