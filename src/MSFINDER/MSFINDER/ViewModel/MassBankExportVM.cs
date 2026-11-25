using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// The MassBank export dialog: what every record states, chosen from MassBank's vocabulary where it has one, and the
    /// folder the records go to. Export is possible only once nothing would keep the records from the MassBank Validator.
    /// </summary>
    public sealed class MassBankExportVM : INotifyPropertyChanged
    {
        /// <summary>The separation choice that writes AC$INSTRUMENT_TYPE without a separation prefix.</summary>
        public const string NoSeparation = "(none)";

        private static readonly Regex ANALYZER_ONLY = new Regex(@"\A(?:B|E|FT|IT|Q|TOF)+\z", RegexOptions.CultureInvariant);

        // What the last export of this session used, for the fields the queries do not agree on.
        private static MsfinderMassBankExportSettings lastSettings;
        private static string lastSeparation;

        private readonly string projectFolder;
        private readonly string msfinderVersion;

        public MassBankExportVM(MsfinderMassBankExportSettings suggested, string projectFolder, string msfinderVersion, string exportFolder) {
            this.projectFolder = projectFolder;
            this.msfinderVersion = msfinderVersion;
            suggested = suggested ?? new MsfinderMassBankExportSettings();
            var last = lastSettings ?? new MsfinderMassBankExportSettings();

            contributorId = last.ContributorId;
            authors = FirstGiven(suggested.Authors, last.Authors);
            var license = FirstGiven(suggested.License, last.License);
            this.license = MassBankVocabulary.IsValidLicense(license) ? license : null;
            instrument = FirstGiven(suggested.Instrument, last.Instrument);
            SetInstrumentType(FirstGiven(suggested.InstrumentType, last.InstrumentType));
            if (separation is null && lastSeparation != null && !string.IsNullOrEmpty(ionisation)) {
                separation = lastSeparation;
            }
            this.exportFolder = exportFolder ?? string.Empty;
            Update();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public IReadOnlyList<string> Licenses => MassBankVocabulary.Licenses;

        public IReadOnlyList<string> Separations => new[] { NoSeparation, }.Concat(MassBankVocabulary.Separations).ToList();

        public IReadOnlyList<string> Ionisations => MassBankVocabulary.Ionisations;

        public IReadOnlyList<MassBankAnalyzerPreset> AnalyzerPresets => MassBankVocabulary.AnalyzerPresets;

        public string ContributorId {
            get { return contributorId; }
            set { Set(ref contributorId, value); }
        }
        private string contributorId;

        public string Authors {
            get { return authors; }
            set { Set(ref authors, value); }
        }
        private string authors;

        public string License {
            get { return license; }
            set { Set(ref license, value); }
        }
        private string license;

        public string Instrument {
            get { return instrument; }
            set { Set(ref instrument, value); }
        }
        private string instrument;

        /// <summary>LC, GC, CE or <see cref="NoSeparation"/>; null until chosen.</summary>
        public string Separation {
            get { return separation; }
            set { Set(ref separation, value); }
        }
        private string separation;

        public string Ionisation {
            get { return ionisation; }
            set { Set(ref ionisation, value); }
        }
        private string ionisation;

        /// <summary>The analyzer part, a preset or any concatenation of MassBank's analyzer tokens, e.g. "QTOF".</summary>
        public string Analyzer {
            get { return analyzer; }
            set { Set(ref analyzer, value); }
        }
        private string analyzer;

        public string ExportFolder {
            get { return exportFolder; }
            set { Set(ref exportFolder, value); }
        }
        private string exportFolder;

        /// <summary>AC$INSTRUMENT_TYPE as the parts compose it, e.g. "LC-ESI-QTOF".</summary>
        public string InstrumentType {
            get {
                var sep = separation == NoSeparation ? string.Empty : separation;
                return MassBankVocabulary.ComposeInstrumentType(sep, ionisation, analyzer);
            }
        }

        /// <summary>"= LC-ESI-QTOF" once the parts make a valid instrument type; empty before.</summary>
        public string InstrumentTypePreview {
            get {
                var value = InstrumentType;
                return separation != null && MassBankVocabulary.IsValidInstrumentType(value) ? "= " + value : string.Empty;
            }
        }

        /// <summary>What keeps the export from starting, one problem per line; empty when nothing does.</summary>
        public string Problems { get; private set; } = string.Empty;

        public bool CanExport { get; private set; }

        /// <summary>The settings as chosen, remembered for the next export of this session.</summary>
        public MsfinderMassBankExportSettings ToSettings() {
            var settings = ToSettingsWithoutRemembering();
            lastSettings = settings;
            lastSeparation = separation;
            return settings;
        }

        private void SetInstrumentType(string value) {
            string sep, ion, ana;
            if (MassBankVocabulary.TryParseInstrumentType(value, out sep, out ion, out ana)) {
                separation = sep.Length == 0 ? NoSeparation : sep;
                ionisation = ion;
                analyzer = ana;
            }
            else if (!string.IsNullOrEmpty(value) && ANALYZER_ONLY.IsMatch(value)) {
                // A bare analyzer, as MS-DIAL wrote before PR #872: keep it, and leave separation and ionisation to the person.
                analyzer = value;
            }
        }

        private void Update() {
            var problems = new List<string>();
            if (separation is null) {
                problems.Add($"Choose the separation of the instrument type: LC, GC, CE or {NoSeparation}.");
            }
            if (string.IsNullOrEmpty(ionisation)) {
                problems.Add("Choose the ionisation of the instrument type, e.g. ESI.");
            }
            if (string.IsNullOrWhiteSpace(analyzer)) {
                problems.Add("Choose the analyzer of the instrument type, e.g. QTOF.");
            }
            if (problems.Count == 0) {
                problems.AddRange(ToSettingsWithoutRemembering().Validate(msfinderVersion));
            }
            else {
                // The instrument type is reported above, part by part; the other fields are checked all the same, with a
                // valid instrument type standing in so that the writer does not report it a second time.
                var settings = ToSettingsWithoutRemembering();
                settings.InstrumentType = "LC-ESI-QTOF";
                problems.AddRange(settings.Validate(msfinderVersion));
            }
            var destination = MsfinderMassBankExportSettings.ValidateDestination(exportFolder, projectFolder);
            if (destination != null) {
                problems.Add(destination);
            }
            Problems = string.Join(Environment.NewLine, problems);
            CanExport = problems.Count == 0;
            OnPropertyChanged(nameof(Problems));
            OnPropertyChanged(nameof(CanExport));
            OnPropertyChanged(nameof(InstrumentType));
            OnPropertyChanged(nameof(InstrumentTypePreview));
        }

        private MsfinderMassBankExportSettings ToSettingsWithoutRemembering() {
            return new MsfinderMassBankExportSettings
            {
                ContributorId = (contributorId ?? string.Empty).Trim(),
                Authors = (authors ?? string.Empty).Trim(),
                License = license ?? string.Empty,
                Instrument = (instrument ?? string.Empty).Trim(),
                InstrumentType = InstrumentType,
            };
        }

        private void Set(ref string field, string value, [CallerMemberName] string propertyName = null) {
            if (string.Equals(field, value, StringComparison.Ordinal)) {
                return;
            }
            field = value;
            OnPropertyChanged(propertyName);
            Update();
        }

        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string FirstGiven(string first, string second) {
            return string.IsNullOrWhiteSpace(first) ? second ?? string.Empty : first;
        }
    }
}
