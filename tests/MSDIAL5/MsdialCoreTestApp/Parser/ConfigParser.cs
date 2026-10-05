using CompMs.Common.DataObj.Property;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.IO;
using CompMs.Common.Query;
using CompMs.Common.Parameter;
using System.Linq;

namespace CompMs.App.MsdialConsole.Parser
{
    public sealed class ConfigParser
    {
        private ConfigParser() { }

        #region // to get analysisparamOfMsdialGcms

        /// <summary>
        /// Collects the method-file keys a run did not understand, and says so before the run
        /// starts.
        /// </summary>
        /// <remarks>
        /// WHAT THIS ENDS. Every dispatcher below used to read a line, hand it to the parameter
        /// readers, and DISCARD the boolean saying whether anything had matched -- under a comment
        /// reading "// write something if needed". GC-MS, IMMS and LC-IM-MS discarded it twice, once
        /// for the common reader and once for their own.
        ///
        /// So a misspelt key, a key from a newer MS-DIAL, a key copied from another mode's template:
        /// all were read, matched nothing, and vanished. The run then used the built-in default and
        /// said nothing, and the analyst had every reason to believe their value had been applied.
        /// For a reanalysis campaign that is a silently wrong scientific result with a method file
        /// that appears to document it correctly -- the author called it critical on 2026-09-16.
        ///
        /// REPORTED, NOT FATAL, and that is measured rather than assumed. Run against the shipped
        /// lipidomics template on 2026-09-16, this found TWENTY-SEVEN keys with no effect --
        /// including "Only report top hit for LBM-based annotation", "Sigma window value",
        /// "Process option" and "Replace true zero values with 1/2 of minimum peak height over all
        /// samples", every one of which an analyst would reasonably believe they had set. Failing
        /// the run would therefore reject every method file in existence, including MS-DIAL's own
        /// templates. Saying so on every run is what can be done today; making those keys work is a
        /// separate decision, because settings that have been ignored for years would start taking
        /// effect and change results.
        ///
        /// A BLANK VALUE IS NOT AN ERROR and is reported separately. "Msp file path:" with nothing
        /// after it is how a method file says there is no MSP library, and it is how the templates
        /// are written. But it is the same experience from the analyst's side when it was not
        /// deliberate, so it is named rather than passed over in silence.
        /// </remarks>
        /// <summary>
        /// What happened to one line of a method file.
        /// </summary>
        /// <remarks>
        /// The readers used to answer this with a bool, and every numeric arm was written
        ///     case "minimum peak height": if (int.TryParse(v, out int x)) param.MinimumAmplitude = x; return true;
        /// with the `return true` OUTSIDE the `if`. A value the arm could not parse was therefore
        /// reported as applied, the property kept its constructor default, and nothing said so.
        /// That is not hypothetical: MinimumAmplitude is a double, ParameterBase writes it back as
        /// one, and MS-DIAL Interactive writes it as a Python float, so every method file on this
        /// machine carried "Minimum peak height: 500.0", int.TryParse rejected all of them, and
        /// every run silently peak-picked at the built-in 1000 while the audit trail recorded 500.
        ///
        /// Three outcomes are needed because two of them used to be one. A key no reader claims is
        /// a spelling mistake or a parameter this mode does not have. A value a reader claims and
        /// cannot read is a format mismatch between the writer and the reader, and it is the more
        /// dangerous of the two, because the key looks right to anyone reading the method file.
        ///
        /// It converts implicitly from bool so that the ~190 arms that genuinely answer
        /// applied-or-unknown stay exactly as they were.
        /// </remarks>
        public readonly struct MethodKeyOutcome
        {
            private const byte UNKNOWN = 0;
            private const byte APPLIED = 1;
            private const byte UNUSABLE = 2;

            private readonly byte _state;

            private MethodKeyOutcome(byte state) {
                _state = state;
            }

            public static MethodKeyOutcome Applied => new MethodKeyOutcome(APPLIED);
            public static MethodKeyOutcome UnknownKey => new MethodKeyOutcome(UNKNOWN);
            public static MethodKeyOutcome UnusableValue => new MethodKeyOutcome(UNUSABLE);

            public static implicit operator MethodKeyOutcome(bool applied) {
                return applied ? Applied : UnknownKey;
            }

            public bool IsApplied => _state == APPLIED;
            public bool IsUnknownKey => _state == UNKNOWN;
            public bool IsUnusableValue => _state == UNUSABLE;
        }

        /// <summary>
        /// Read a real-valued parameter, or report that the value could not be read.
        /// </summary>
        /// <remarks>
        /// Parsed with the invariant culture, because a method file is a machine-written file
        /// format and not a document typed by a person: the same file must mean the same thing on
        /// a machine whose decimal separator is a comma.
        /// </remarks>
        private static MethodKeyOutcome Number(string text, Action<double> assign) {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)) {
                return Assign(parsed, assign);
            }
            return MethodKeyOutcome.UnusableValue;
        }

        /// <summary>
        /// Apply a parsed value, treating a value the parameter refuses as an unusable value.
        /// </summary>
        /// <remarks>
        /// A few parameters accept only part of their type's range - a positive thread count, a
        /// weight between zero and one - and the old arms dropped an out-of-range value as
        /// silently as an unparseable one. Both are the same finding to whoever wrote the file:
        /// the number in the method file is not the number the run used.
        /// </remarks>
        private static MethodKeyOutcome Assign<T>(T parsed, Action<T> assign) {
            try {
                assign(parsed);
            }
            catch (FormatException) {
                return MethodKeyOutcome.UnusableValue;
            }
            catch (ArgumentOutOfRangeException) {
                return MethodKeyOutcome.UnusableValue;
            }
            return MethodKeyOutcome.Applied;
        }

        /// <summary>
        /// Read a whole-numbered parameter, accepting a value written as a real number when it
        /// names a whole number.
        /// </summary>
        /// <remarks>
        /// "5" and "5.0" are the same count and both are accepted, because the writers of these
        /// files disagree about which they emit. "5.7" is not a count and is refused rather than
        /// rounded: a smoothing level of 5.7 means whoever wrote it believed something this
        /// parameter cannot express, and silently choosing 6 for them hides that.
        /// </remarks>
        private static MethodKeyOutcome Count(string text, Action<int> assign) {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) {
                return Assign(parsed, assign);
            }
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double real)
                && Math.Abs(real - Math.Round(real)) < 1e-9
                && real >= int.MinValue && real <= int.MaxValue) {
                return Assign((int)Math.Round(real), assign);
            }
            return MethodKeyOutcome.UnusableValue;
        }

        /// <summary>
        /// Read an on/off parameter, accepting "true" and "false" in any letter case.
        /// </summary>
        /// <remarks>
        /// Several arms used to assign only one of the two values -
        ///     case "keep original precursor isotopes": if (valueLower == "false") param.X = false; return true;
        /// - so the other value was reported as applied and silently ignored. That was harmless
        /// only while the constructor default happened to equal the value the arm could not
        /// assign; KeepOriginalPrecursorIsotopes defaults to false, so "True" never took effect.
        /// Anything other than true or false ("yes", "1", "on") is refused rather than guessed
        /// at, because ParameterBase writes these keys with bool.ToString() and any other spelling
        /// came from somewhere that did not know the format.
        /// </remarks>
        private static MethodKeyOutcome TrueOrFalse(string text, Action<bool> assign) {
            if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)) {
                return Assign(true, assign);
            }
            if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase)) {
                return Assign(false, assign);
            }
            return MethodKeyOutcome.UnusableValue;
        }

        /// <summary>
        /// Try the second reader only when the first did not recognise the key.
        /// </summary>
        /// <remarks>
        /// This replaces `first(...) || second(...)`. The difference matters: a key the first
        /// reader claims and whose value it cannot read must NOT be offered to the second reader,
        /// or an unusable value comes back as an unknown key and is reported as a misspelling.
        /// </remarks>
        private static MethodKeyOutcome Either(MethodKeyOutcome first, Func<MethodKeyOutcome> second) {
            return first.IsUnknownKey ? second() : first;
        }

        /// <summary>
        /// Read a comma-separated list of adduct ions as the adducts the peak character
        /// estimation searches, or report that none of them could be read.
        /// </summary>
        /// <remarks>
        /// THE ADDUCTS MUST BE MARKED INCLUDED. PeakCharacterEstimator.SearchedAdductInitialize
        /// keeps only the adducts whose IsIncluded is true and, when none are, falls back to
        /// [M+H]+ or [M-H]-. Only the GUI's adduct setting ever set that flag. This arm used to
        /// store what AdductIon.GetAdductIon returned, whose IsIncluded is false, so a method file
        /// listing "[M+H]+,[M+Na]+,[M+NH4]+" was searched with the proton adduct alone while the
        /// key report said the key had been applied.
        ///
        /// THE FLAG IS SET ON THE CACHED INSTANCES. GetAdductIon hands out one shared instance per
        /// name from a process-wide cache, so an adduct listed here stays IsIncluded for every
        /// later caller in the process. That is deliberate: AdductIon is not constructed outside
        /// its own factory, and the GUI's AdductIonSettingModel already sets IsIncluded on a
        /// GetAdductIon instance the same way. A console run reads one method file per process.
        ///
        /// An entry that fails AdductIon.FormatCheck is dropped as before, so one mistyped adduct
        /// does not discard the rest. When NO entry can be read, nothing the file asked for would
        /// be searched, so the value is reported as unusable and the built-in list is kept.
        /// Entries are trimmed because "[M+H]+, [M+Na]+" is how a person types a list.
        /// </remarks>
        private static MethodKeyOutcome SearchedAdductIons(string text, Action<List<AdductIon>> assign) {
            var adducts = new List<AdductIon>();
            foreach (var adductString in text.Split(',')) {
                var adductObj = AdductIon.GetAdductIon(adductString.Trim());
                if (adductObj.FormatCheck) adducts.Add(adductObj);
            }
            if (adducts.Count == 0) {
                return MethodKeyOutcome.UnusableValue;
            }
            foreach (var adduct in adducts) {
                adduct.IsIncluded = true;
            }
            assign(adducts);
            return MethodKeyOutcome.Applied;
        }

        private sealed class MethodFileKeys
        {
            private readonly List<string> _applied = new List<string>();
            private readonly List<string> _unrecognised = new List<string>();
            private readonly List<string> _unusable = new List<string>();
            private readonly List<string> _blank = new List<string>();
            private readonly Dictionary<string, Occurrences> _occurrences = new Dictionary<string, Occurrences>(StringComparer.OrdinalIgnoreCase);
            private readonly List<Occurrences> _occurrenceOrder = new List<Occurrences>();

            /// <summary>
            /// Every line one key was written on, and the last value a reader accepted.
            /// </summary>
            private sealed class Occurrences
            {
                public Occurrences(string key) {
                    Key = key;
                }

                public string Key { get; }
                public int Count { get; set; }
                public string? Used { get; set; }
            }

            public void Read(string method, string value, Func<MethodKeyOutcome> apply) {
                var occurrences = Count(method);
                if (value.IsEmptyOrNull()) {
                    _blank.Add(method);
                    return;
                }
                var outcome = apply();
                if (outcome.IsUnknownKey) {
                    _unrecognised.Add(method);
                }
                else if (outcome.IsUnusableValue) {
                    _unusable.Add($"{method}: {value}");
                }
                else {
                    _applied.Add(method);
                    occurrences.Used = value;
                }
            }

            private Occurrences Count(string method) {
                if (!_occurrences.TryGetValue(method, out var occurrences)) {
                    occurrences = new Occurrences(method);
                    _occurrences.Add(method, occurrences);
                    _occurrenceOrder.Add(occurrences);
                }
                occurrences.Count++;
                return occurrences;
            }

            /// <summary>
            /// The keys written on more than one line, and the value the run used for each.
            /// </summary>
            /// <remarks>
            /// Every reader keeps the last line it accepts, so a key written twice is not an error,
            /// but "applied" alone cannot say WHICH of the two lines was applied, and a method file
            /// edited by appending a corrected line looks, to anyone reading it top to bottom, as
            /// if the first value governs. This names the value that did.
            ///
            /// The used value is the last line a reader accepted; a blank or unusable later line
            /// leaves it in place, as it leaves the parameter in place. Keys are matched by
            /// spelling, ignoring letter case. Two aliases of one setting -- "LBM annotator
            /// priority" and "LBM annotation priority" -- are different spellings and are not
            /// matched here, because only the readers know which spellings are one setting.
            /// </remarks>
            public IEnumerable<Dictionary<string, object?>> Repeated =>
                _occurrenceOrder
                    .Where(occurrences => occurrences.Count > 1)
                    .Select(occurrences => new Dictionary<string, object?> {
                        ["key"] = occurrences.Key,
                        ["lines"] = occurrences.Count,
                        ["used"] = occurrences.Used,
                    });

            /// <summary>
            /// True when a reader accepted at least one line written with any of these keys.
            /// </summary>
            public bool WasApplied(params string[] keys) {
                return _applied.Any(applied => keys.Contains(applied.Trim(), StringComparer.OrdinalIgnoreCase));
            }

            /// <summary>
            /// True when the method file contained a key no reader claimed.
            /// </summary>
            public bool HasUnrecognised => _unrecognised.Count > 0;

            public IReadOnlyList<string> Unrecognised => _unrecognised;

            /// <summary>
            /// True when a reader claimed a key and could not read its value. The parameter kept
            /// its built-in default and the method file says otherwise.
            /// </summary>
            public bool HasUnusableValues => _unusable.Count > 0;

            public IReadOnlyList<string> UnusableValues => _unusable;

            /// <summary>
            /// Write what happened to every key beside the method file that was read.
            /// </summary>
            /// <remarks>
            /// The Console said all of this on stdout and nowhere else. Stdout reaches a log the
            /// caller keeps for as long as it keeps the job, and the project contract's retained
            /// artifacts do not include it, so an audit reading a unit's workspace could see the
            /// method file's declared settings and could not see which of them the run had used.
            /// A parameter that had no effect is invisible in exactly the place it matters.
            ///
            /// It lands beside the method file because that is the directory the reader was
            /// pointed at, which for a repository reanalysis is the unit's own output directory.
            /// The method file's hash ties the record to the exact file that was read, so a
            /// check comparing the two cannot be satisfied by a record left over from a different
            /// method file.
            ///
            /// A failure to write it is reported and never stops the run: this describes the run,
            /// it does not perform it.
            /// </remarks>
            private void WriteRecord(string filepath) {
                try {
                    var record = new Dictionary<string, object> {
                        ["schema"] = "msdial-method-file-keys.v1",
                        ["method_file"] = Path.GetFileName(filepath),
                        ["method_file_sha256"] = FileDigest(filepath),
                        ["read_at"] = DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
                        ["applied"] = _applied,
                        ["unrecognised"] = _unrecognised,
                        ["unusable"] = _unusable,
                        ["blank"] = _blank,
                        ["repeated"] = Repeated.ToList(),
                    };
                    var directory = Path.GetDirectoryName(Path.GetFullPath(filepath));
                    if (string.IsNullOrEmpty(directory)) {
                        return;
                    }
                    var target = Path.Combine(directory, Path.GetFileNameWithoutExtension(filepath) + ".keys.json");
                    File.WriteAllText(target, JsonConvert.SerializeObject(record, Formatting.Indented), new UTF8Encoding(false));
                }
                catch (Exception error) {
                    Console.WriteLine($"Method file key record could not be written: {error.Message}");
                }
            }

            private static string FileDigest(string filepath) {
                try {
                    using (var stream = File.OpenRead(filepath))
                    using (var sha = System.Security.Cryptography.SHA256.Create()) {
                        var hash = sha.ComputeHash(stream);
                        var text = new StringBuilder(hash.Length * 2);
                        foreach (var octet in hash) {
                            text.Append(octet.ToString("x2", CultureInfo.InvariantCulture));
                        }
                        return text.ToString();
                    }
                }
                catch (Exception) {
                    return string.Empty;
                }
            }

            public void Report(string filepath) {
                WriteRecord(filepath);
                var name = Path.GetFileName(filepath);
                if (_unrecognised.Count > 5) {
                    Console.WriteLine($"Method file '{name}': {_unrecognised.Count} parameter(s) had NO EFFECT in this mode. See '{Path.GetFileNameWithoutExtension(filepath)}.keys.json' for every key and check whether the Console controls that setting elsewhere.");
                }
                else if (_unrecognised.Count > 0) {
                    foreach (var key in _unrecognised) {
                        Console.WriteLine($"Method file '{name}': the parameter '{key}' was not recognised and had NO EFFECT. The built-in default was used instead.");
                    }
                    Console.WriteLine($"Method file '{name}': {_unrecognised.Count} parameter(s) had no effect. Check the spelling against the template for this mode.");
                }
                foreach (var entry in _unusable) {
                    Console.WriteLine($"Method file '{name}': the value of '{entry}' could not be read and had NO EFFECT. The built-in default was used instead.");
                }
                if (_unusable.Count > 0) {
                    Console.WriteLine($"Method file '{name}': {_unusable.Count} parameter(s) named a value this reader cannot parse. The run did NOT use them.");
                }
                if (_blank.Count > 5) {
                    Console.WriteLine($"Method file '{name}': {_blank.Count} parameter(s) were left blank; their defaults apply. See '{Path.GetFileNameWithoutExtension(filepath)}.keys.json' for the names.");
                }
                else if (_blank.Count > 0) {
                    Console.WriteLine($"Method file '{name}': left blank, so the default applies: {string.Join(", ", _blank)}");
                }
                foreach (var repeated in _occurrenceOrder.Where(occurrences => occurrences.Count > 1)) {
                    var used = repeated.Used is null
                        ? "no line was applied"
                        : $"the last line applied was used: '{repeated.Used}'";
                    Console.WriteLine($"Method file '{name}': the parameter '{repeated.Key}' is written on {repeated.Count} lines; {used}.");
                }
                // Last, so the record and every other finding above are already out when this refuses the run.
                var blankFilteringConflict = BlankFilteringConflict(Used);
                if (blankFilteringConflict != null) {
                    throw new FormatException($"Method file '{name}': {blankFilteringConflict}");
                }
            }

            /// <summary>
            /// The last value a reader accepted for this key, or null when no line of it was applied.
            /// </summary>
            private string? Used(string key) {
                return _occurrences.TryGetValue(key, out var occurrences) ? occurrences.Used : null;
            }
        }

        public static MsdialGcmsParameter ReadForGcms(string filepath)
        {
            var param = new MsdialGcmsParameter();
            var keys = new MethodFileKeys();
            using (var sr = new StreamReader(filepath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                while (sr.Peek() > -1)
                {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        keys.Read(method, value, () => Either(ReadCommonParameter(param, method, value),
                            () => ReadGcmsSpecificParameter(param, method, value)));
                    }
                }
            }
            keys.Report(filepath);
            if (param.AccuracyType == AccuracyType.IsNominal) {
                param.MassSliceWidth = 0.5F;
                param.CentroidMs1Tolerance = 0.5F;
            }
            ResolveGcmsFilePaths(param, filepath);
            
            return param;
        }

     
        public static MsdialLcmsParameter ReadForLcmsParameter(string filepath) {
            var param = new MsdialLcmsParameter();
            var keys = new MethodFileKeys();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        keys.Read(method, value, () => Either(ReadCommonParameter(param, method, value),
                            () => ReadLcmsConsoleParameter(method, value)));
                    }
                }
            }
            ApplyGeneratedLipidLbmDefaults(param, keys);
            keys.Report(filepath);
            return param;
        }

        /// <summary>
        /// Give the LBM annotator the GUI's spectrum cut-offs for EAD, OAD and EID where the method
        /// file does not set them.
        /// </summary>
        /// <remarks>
        /// In these modes the LBM match is the gate to the generated lipid library: only a peak the
        /// LBM library reference-matches is expanded into chain and double-bond positions. The GUI
        /// therefore gives the LBM annotator much lower spectrum cut-offs for these collision types
        /// (LcmsMspAnnotatorSettingModel) than the built-in search parameter has, which was written
        /// for a CID spectrum against an acquired library. Without this the Console would pass far
        /// fewer peaks to the generated library than the GUI does from the same data.
        ///
        /// Only the keys the method file leaves unset change, and it is decided after the whole
        /// file is read, so the order of "Collision type", "Target omics" and the cut-off lines
        /// does not matter. What changed is said on the console.
        /// </remarks>
        private static void ApplyGeneratedLipidLbmDefaults(ParameterBase param, MethodFileKeys keys) {
            if (GeneratedLipidLibrary.SourceFor(param) is null) {
                return;
            }
            var lbm = param.LbmSearchParam;
            var changed = new List<string>();
            void Default(string name, Action apply, params string[] methodKeys) {
                if (!keys.WasApplied(methodKeys)) {
                    apply();
                    changed.Add(name);
                }
            }
            Default("weighted dot product cutoff 0.05", () => lbm.WeightedDotProductCutOff = 0.05F,
                "weighted dot product cutoff for lbm-based annotation", "square root of weighted dot product cutoff for lbm-based annotation");
            Default("simple dot product cutoff 0.05", () => lbm.SimpleDotProductCutOff = 0.05F,
                "simple dot product cutoff for lbm-based annotation", "square root of simple dot product cutoff for lbm-based annotation");
            Default("reverse dot product cutoff 0.05", () => lbm.ReverseDotProductCutOff = 0.05F,
                "reverse dot product cutoff for lbm-based annotation", "square root of reverse dot product cutoff for lbm-based annotation");
            Default("matched peaks percentage cutoff 0", () => lbm.MatchedPeaksPercentageCutOff = 0F,
                "matched peaks percentage cutoff for lbm-based annotation");
            Default("minimum spectrum match 1", () => lbm.MinimumSpectrumMatch = 1F,
                "minimum spectrum match for lbm-based annotation");
            if (changed.Count > 0) {
                Console.WriteLine($"LBM-based annotation for collision type {param.CollistionType}: using the GUI defaults for the keys the method file does not set ({string.Join(", ", changed)}).");
            }
        }

        /// <summary>
        /// Say what the settings LcmsProcess reads for itself would do with one line.
        /// </summary>
        /// <remarks>
        /// LcmsProcess reads several settings with readers of their own, and calls them after
        /// ReadForLcmsParameter has already written the key record. The record knew only
        /// ReadCommonParameter, so it listed those keys as unrecognised with NO EFFECT while they
        /// governed the run. A repository run's record put "MSP annotator settings file path",
        /// "LBM annotator priority" and "Alignment light mode" among its twenty-nine keys with no
        /// effect, and the audits that trust the record were told the opposite of what happened.
        ///
        /// This asks the same line readers the side readers use, so the record cannot disagree
        /// with the run: a value the side reader takes is applied, and a value it passes over --
        /// "yes" for a switch, "2.0" for a priority -- is unusable, because the run used the
        /// default. Nothing is assigned here, so which keys take effect is unchanged. A new
        /// setting LcmsProcess reads for itself belongs in this list.
        ///
        /// Only the LC-MS reader asks. No other mode calls these readers, so in a GC-MS, DIMS,
        /// IMMS or LC-IM-MS method file the same keys really have no effect and are still
        /// reported as unrecognised.
        /// </remarks>
        private static MethodKeyOutcome ReadLcmsConsoleParameter(string method, string value) {
            foreach (var line in LcmsConsoleLines) {
                var outcome = line(method, value);
                if (!outcome.IsUnknownKey) {
                    return outcome;
                }
            }
            return MethodKeyOutcome.UnknownKey;
        }

        private static readonly Func<string, string, MethodKeyOutcome>[] LcmsConsoleLines = {
            (method, value) => MspAnnotatorSettingsFilePathLine(method, value, _ => { }),
            (method, value) => TextAnnotatorSettingsFilePathLine(method, value, _ => { }),
            (method, value) => LbmAnnotatorPriorityLine(method, value, _ => { }),
            (method, value) => AlignmentLightModeLine(method, value, _ => { }),
            (method, value) => DetailedAlignmentProvenanceLine(method, value, _ => { }),
            (method, value) => AnnotationCandidateExportLine(method, value, _ => { }),
            (method, value) => GeneratedLipidAnnotatorLine(method, value, new GeneratedLipidAnnotatorSetting()),
        };

        /// <summary>
        /// Read a setting from the last line its line reader takes.
        /// </summary>
        /// <remarks>
        /// THE LAST USABLE LINE WINS, as it does in the main readers. These readers used to stop
        /// at the first usable line, so a key written twice meant the later value for every
        /// setting ReadCommonParameter reads and the earlier value for these six, while the key
        /// record said both lines had been applied. One rule for the whole file ends that.
        ///
        /// A blank value is skipped, as the main readers skip it: "Msp file path:" does not clear
        /// a path an earlier line set, and neither does a blank settings-file path here. A value
        /// the line reader cannot use is skipped too, so it leaves the earlier value in place,
        /// exactly as an unparseable number leaves a main-reader parameter alone.
        /// </remarks>
        private static T ReadLast<T>(string filepath, T fallback, Func<string, string, Action<T>, MethodKeyOutcome> line) {
            var result = fallback;
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (!isReadable || value.IsEmptyOrNull()) {
                        continue;
                    }
                    var read = fallback;
                    if (line(method, value, v => read = v).IsApplied) {
                        result = read;
                    }
                }
            }
            return result;
        }

        public static List<MspAnnotatorSetting> ReadMspAnnotatorSettings(string filepath, ParameterBase param) {
            var settingsFilePath = ReadMspAnnotatorSettingsFilePath(filepath);
            if (settingsFilePath.IsEmptyOrNull()) {
                return new List<MspAnnotatorSetting>();
            }
            if (!Path.IsPathRooted(settingsFilePath)) {
                var baseDirectory = Path.GetDirectoryName(filepath) ?? string.Empty;
                settingsFilePath = Path.Combine(baseDirectory, settingsFilePath);
            }
            return ReadMspAnnotatorSettingsTable(settingsFilePath, param);
        }

        public static List<TextAnnotatorSetting> ReadTextAnnotatorSettings(string filepath, ParameterBase param) {
            var settingsFilePath = ReadTextAnnotatorSettingsFilePath(filepath);
            if (settingsFilePath.IsEmptyOrNull()) {
                return new List<TextAnnotatorSetting>();
            }
            if (!Path.IsPathRooted(settingsFilePath)) {
                var baseDirectory = Path.GetDirectoryName(filepath) ?? string.Empty;
                settingsFilePath = Path.Combine(baseDirectory, settingsFilePath);
            }
            return ReadTextAnnotatorSettingsTable(settingsFilePath, param);
        }

        public static bool ReadAlignmentLightMode(string filepath) {
            return ReadLast(filepath, false, AlignmentLightModeLine);
        }

        public static int ReadLbmAnnotatorPriority(string filepath) {
            return ReadLast(filepath, 1, LbmAnnotatorPriorityLine);
        }

        public static bool ReadDetailedAlignmentProvenance(string filepath) {
            return ReadLast(filepath, false, DetailedAlignmentProvenanceLine);
        }

        public static bool ReadAnnotationCandidateExport(string filepath) {
            return ReadLast(filepath, false, AnnotationCandidateExportLine);
        }

        /// <summary>
        /// Read how the in silico lipid library for EAD, OAD and EID spectra is built and searched.
        /// </summary>
        /// <remarks>
        /// Several keys share one setting object, so this cannot be ReadLast, but it keeps
        /// ReadLast's rules: the last usable line of each key wins, and a blank or unusable line
        /// leaves the earlier value in place.
        /// </remarks>
        public static GeneratedLipidAnnotatorSetting ReadGeneratedLipidAnnotatorSetting(string filepath) {
            var setting = new GeneratedLipidAnnotatorSetting();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (!isReadable || value.IsEmptyOrNull()) {
                        continue;
                    }
                    GeneratedLipidAnnotatorLine(method, value, setting);
                }
            }
            return setting;
        }

        private static string ReadMspAnnotatorSettingsFilePath(string filepath) {
            return ReadLast(filepath, string.Empty, MspAnnotatorSettingsFilePathLine);
        }

        private static string ReadTextAnnotatorSettingsFilePath(string filepath) {
            return ReadLast(filepath, string.Empty, TextAnnotatorSettingsFilePathLine);
        }

        private static MethodKeyOutcome AlignmentLightModeLine(string method, string value, Action<bool> assign) {
            switch (method.ToLower()) {
                case "alignment light mode":
                case "alignment light":
                case "console alignment light mode":
                    return TrueOrFalse(value, assign);
                default:
                    return MethodKeyOutcome.UnknownKey;
            }
        }

        private static MethodKeyOutcome LbmAnnotatorPriorityLine(string method, string value, Action<int> assign) {
            switch (method.ToLowerInvariant()) {
                case "lbm annotator priority":
                case "lbm annotation priority":
                    return int.TryParse(value, out var priority)
                        ? Assign(priority, assign)
                        : MethodKeyOutcome.UnusableValue;
                default:
                    return MethodKeyOutcome.UnknownKey;
            }
        }

        private static MethodKeyOutcome DetailedAlignmentProvenanceLine(string method, string value, Action<bool> assign) {
            switch (method.ToLower()) {
                case "detailed alignment provenance":
                case "export detailed alignment provenance":
                    return TrueOrFalse(value, assign);
                default:
                    return MethodKeyOutcome.UnknownKey;
            }
        }

        private static MethodKeyOutcome AnnotationCandidateExportLine(string method, string value, Action<bool> assign) {
            switch (method.ToLower()) {
                case "annotation candidates":
                case "export annotation candidates":
                    return TrueOrFalse(value, assign);
                default:
                    return MethodKeyOutcome.UnknownKey;
            }
        }

        private static MethodKeyOutcome GeneratedLipidAnnotatorLine(string method, string value, GeneratedLipidAnnotatorSetting setting) {
            var search = setting.SearchParameter;
            switch (method.ToLowerInvariant()) {
                case "use generated lipid library":
                case "generated lipid annotation":
                    return TrueOrFalse(value, v => setting.Enabled = v);
                case "generated lipid annotator priority":
                case "generated lipid annotation priority":
                    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority)
                        ? Assign(priority, v => setting.Priority = v)
                        : MethodKeyOutcome.UnusableValue;
                case "ms1 tolerance for generated lipid annotation": return Number(value, v => search.Ms1Tolerance = (float)v);
                case "ms2 tolerance for generated lipid annotation": return Number(value, v => search.Ms2Tolerance = (float)v);
                case "mass range begin for generated lipid annotation": return Number(value, v => search.MassRangeBegin = (float)v);
                case "mass range end for generated lipid annotation": return Number(value, v => search.MassRangeEnd = (float)v);
                case "relative amplitude cutoff for generated lipid annotation": return Number(value, v => search.RelativeAmpCutoff = (float)v);
                case "absolute amplitude cutoff for generated lipid annotation": return Number(value, v => search.AbsoluteAmpCutoff = (float)v);
                case "square root of weighted dot product cutoff for generated lipid annotation": return Number(value, v => search.WeightedDotProductCutOff = (float)v);
                case "square root of simple dot product cutoff for generated lipid annotation": return Number(value, v => search.SimpleDotProductCutOff = (float)v);
                case "square root of reverse dot product cutoff for generated lipid annotation": return Number(value, v => search.ReverseDotProductCutOff = (float)v);
                case "matched peaks percentage cutoff for generated lipid annotation": return Number(value, v => search.MatchedPeaksPercentageCutOff = (float)v);
                case "minimum spectrum match for generated lipid annotation": return Number(value, v => search.MinimumSpectrumMatch = (float)v);
                case "total score cutoff for generated lipid annotation": return Number(value, v => search.TotalScoreCutoff = (float)v);
                case "rt tolerance for generated lipid annotation": return Number(value, v => search.RtTolerance = (float)v);
                case "ccs tolerance for generated lipid annotation": return Number(value, v => search.CcsTolerance = (float)v);
                case "use retention information for generated lipid annotation scoring": return TrueOrFalse(value, v => search.IsUseTimeForAnnotationScoring = v);
                case "use retention information for generated lipid annotation filtering": return TrueOrFalse(value, v => search.IsUseTimeForAnnotationFiltering = v);
                case "use ccs for generated lipid annotation scoring": return TrueOrFalse(value, v => search.IsUseCcsForAnnotationScoring = v);
                case "use ccs for generated lipid annotation filtering": return TrueOrFalse(value, v => search.IsUseCcsForAnnotationFiltering = v);
                default:
                    return MethodKeyOutcome.UnknownKey;
            }
        }

        // ReadLast skips a blank line before asking, so a blank path neither clears an earlier one
        // nor stops a later one from applying. The key record never asks about a blank value
        // either, because it lists it as blank first.
        private static MethodKeyOutcome MspAnnotatorSettingsFilePathLine(string method, string value, Action<string> assign) {
            switch (method.ToLower()) {
                case "msp annotator settings file path":
                case "msp annotation settings file path":
                case "msp search settings file path":
                    return Assign(value, assign);
                default:
                    return MethodKeyOutcome.UnknownKey;
            }
        }

        private static MethodKeyOutcome TextAnnotatorSettingsFilePathLine(string method, string value, Action<string> assign) {
            switch (method.ToLower()) {
                case "text annotator settings file path":
                case "text library annotator settings file path":
                case "text db annotator settings file path":
                case "text annotation settings file path":
                    return Assign(value, assign);
                default:
                    return MethodKeyOutcome.UnknownKey;
            }
        }

        private static List<MspAnnotatorSetting> ReadMspAnnotatorSettingsTable(string filepath, ParameterBase param) {
            if (!File.Exists(filepath)) {
                Console.WriteLine($"MSP annotator settings file was not found: {filepath}");
                return new List<MspAnnotatorSetting>();
            }

            var rows = new List<string>();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine()?.TrimEnd('\r', '\n');
                    if (!line.IsEmptyOrNull() && !line.TrimStart().StartsWith("#")) {
                        rows.Add(line);
                    }
                }
            }
            if (rows.Count == 0) {
                return new List<MspAnnotatorSetting>();
            }

            var headers = rows[0].Split('\t').Select(NormalizeHeader).ToArray();
            var pathIndex = FindColumn(headers, "mspfilepath", "mspfile", "filepath", "path");
            if (pathIndex < 0) {
                Console.WriteLine("MSP annotator settings TSV requires a 'msp_file_path' column.");
                return new List<MspAnnotatorSetting>();
            }

            var settings = new List<MspAnnotatorSetting>();
            var usedAnnotatorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var baseDirectory = Path.GetDirectoryName(filepath) ?? string.Empty;
            for (var i = 1; i < rows.Count; i++) {
                var fields = rows[i].Split('\t');
                var mspFilePath = GetField(fields, pathIndex);
                if (mspFilePath.IsEmptyOrNull()) {
                    continue;
                }
                if (!Path.IsPathRooted(mspFilePath)) {
                    mspFilePath = Path.Combine(baseDirectory, mspFilePath);
                }

                var settingIndex = settings.Count + 1;
                var annotatorId = GetField(fields, headers, "annotatorid", "id", "name");
                if (annotatorId.IsEmptyOrNull()) {
                    annotatorId = $"MspDB_{settingIndex}";
                }
                if (!usedAnnotatorIds.Add(annotatorId)) {
                    Console.WriteLine($"Duplicated MSP annotator_id was skipped: {annotatorId}");
                    continue;
                }

                var priority = settingIndex;
                var priorityText = GetField(fields, headers, "priority");
                if (!priorityText.IsEmptyOrNull() && int.TryParse(priorityText, out var parsedPriority)) {
                    priority = parsedPriority;
                }

                var searchParameter = new MsRefSearchParameterBase(param.MspSearchParam);
                ApplyMspSearchParameter(searchParameter, fields, headers);
                TargetOmics? targetOmics = null;
                var targetOmicsText = GetField(fields, headers, "targetomics", "annotationmode", "omics");
                if (!targetOmicsText.IsEmptyOrNull()) {
                    if (Enum.TryParse(targetOmicsText, true, out TargetOmics parsedTargetOmics)) {
                        targetOmics = parsedTargetOmics;
                    }
                    else {
                        Console.WriteLine($"Unknown target_omics '{targetOmicsText}' for MSP annotator '{annotatorId}'. The project Target omics setting will be used.");
                    }
                }
                var dataBaseSource = ReadLibraryKind(GetField(fields, headers, "librarykind", "librarytype", "spectrasource", "mspkind"), annotatorId);
                settings.Add(new MspAnnotatorSetting(annotatorId, mspFilePath, priority, searchParameter, targetOmics, dataBaseSource));
                ReportEffectiveAnnotatorSettings("MSP", annotatorId, mspFilePath, priority, searchParameter);
                Console.WriteLine($"MSP annotator {annotatorId}: library kind {dataBaseSource}");
            }
            return settings;
        }

        private static List<TextAnnotatorSetting> ReadTextAnnotatorSettingsTable(string filepath, ParameterBase param) {
            if (!File.Exists(filepath)) {
                Console.WriteLine($"Text annotator settings file was not found: {filepath}");
                return new List<TextAnnotatorSetting>();
            }

            var rows = new List<string>();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine()?.TrimEnd('\r', '\n');
                    if (!line.IsEmptyOrNull() && !line.TrimStart().StartsWith("#")) {
                        rows.Add(line);
                    }
                }
            }
            if (rows.Count == 0) {
                return new List<TextAnnotatorSetting>();
            }

            var headers = rows[0].Split('\t').Select(NormalizeHeader).ToArray();
            var pathIndex = FindColumn(headers, "textdbfilepath", "textlibraryfilepath", "textfilepath", "filepath", "path");
            if (pathIndex < 0) {
                Console.WriteLine("Text annotator settings TSV requires a 'text_db_file_path' column.");
                return new List<TextAnnotatorSetting>();
            }

            var settings = new List<TextAnnotatorSetting>();
            var usedAnnotatorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var baseDirectory = Path.GetDirectoryName(filepath) ?? string.Empty;
            for (var i = 1; i < rows.Count; i++) {
                var fields = rows[i].Split('\t');
                var textDbFilePath = GetField(fields, pathIndex);
                if (textDbFilePath.IsEmptyOrNull()) {
                    continue;
                }
                if (!Path.IsPathRooted(textDbFilePath)) {
                    textDbFilePath = Path.Combine(baseDirectory, textDbFilePath);
                }

                var settingIndex = settings.Count + 1;
                var annotatorId = GetField(fields, headers, "annotatorid", "id", "name");
                if (annotatorId.IsEmptyOrNull()) {
                    annotatorId = $"TextDB_{settingIndex}";
                }
                if (!usedAnnotatorIds.Add(annotatorId)) {
                    Console.WriteLine($"Duplicated Text annotator_id was skipped: {annotatorId}");
                    continue;
                }

                var priority = settingIndex;
                var priorityText = GetField(fields, headers, "priority");
                if (!priorityText.IsEmptyOrNull() && int.TryParse(priorityText, out var parsedPriority)) {
                    priority = parsedPriority;
                }

                var searchParameter = new MsRefSearchParameterBase(param.TextDbSearchParam);
                ApplyMspSearchParameter(searchParameter, fields, headers);
                settings.Add(new TextAnnotatorSetting(annotatorId, textDbFilePath, priority, searchParameter));
                ReportEffectiveAnnotatorSettings("Text", annotatorId, textDbFilePath, priority, searchParameter);
            }
            return settings;
        }

        /// <summary>
        /// States the settings an annotator will actually use.
        /// </summary>
        /// <remarks>
        /// A settings row starts from the method file's annotation block and overrides,
        /// column by column, whatever the table supplies. So the same setting is written
        /// down in two places with two different values and neither file says which one
        /// governs. Printing the resolved value settles it in the run log, where a reader
        /// of the artifacts can see it.
        /// </remarks>
        private static void ReportEffectiveAnnotatorSettings(
            string kind, string annotatorId, string filePath, int priority, MsRefSearchParameterBase parameter) {
            Console.WriteLine(
                $"{kind} annotator {annotatorId} ({Path.GetFileName(filePath)}), priority {priority}: "
                + $"RT tolerance {parameter.RtTolerance}, MS1 tolerance {parameter.Ms1Tolerance}, "
                + $"MS2 tolerance {parameter.Ms2Tolerance}, total score cutoff {parameter.TotalScoreCutoff}");
        }

        private static void ApplyMspSearchParameter(MsRefSearchParameterBase parameter, string[] fields, string[] headers) {
            SetFloat(fields, headers, value => parameter.MassRangeBegin = value, "massrangebegin", "massbegin");
            SetFloat(fields, headers, value => parameter.MassRangeEnd = value, "massrangeend", "massend");
            SetFloat(fields, headers, value => parameter.RtTolerance = value, "rttolerance", "retentiontimetolerance");
            SetFloat(fields, headers, value => parameter.RiTolerance = value, "ritolerance", "retentionindextolerance");
            SetFloat(fields, headers, value => parameter.CcsTolerance = value, "ccstolerance");
            SetFloat(fields, headers, value => parameter.Ms1Tolerance = value, "ms1tolerance", "accuratems1tolerance");
            SetFloat(fields, headers, value => parameter.Ms2Tolerance = value, "ms2tolerance");
            SetFloat(fields, headers, value => parameter.RelativeAmpCutoff = value, "relativeamplitudecutoff", "relativeampcutoff");
            SetFloat(fields, headers, value => parameter.AbsoluteAmpCutoff = value, "absoluteamplitudecutoff", "absoluteampcutoff");
            SetFloat(fields, headers, value => parameter.WeightedDotProductCutOff = value, "weighteddotproductcutoff");
            SetFloat(fields, headers, value => parameter.SimpleDotProductCutOff = value, "simpledotproductcutoff");
            SetFloat(fields, headers, value => parameter.ReverseDotProductCutOff = value, "reversedotproductcutoff");
            SetFloat(fields, headers, value => parameter.MatchedPeaksPercentageCutOff = value, "matchedpeakspercentagecutoff", "matchedpeakpercentagecutoff");
            SetFloat(fields, headers, value => parameter.MinimumSpectrumMatch = value, "minimumspectrummatch", "minimumpeakmatch");
            SetFloat(fields, headers, value => parameter.TotalScoreCutoff = value, "totalscorecutoff");
            SetBool(fields, headers, value => parameter.IsUseTimeForAnnotationScoring = value, "useretentioninformationforscoring", "useretentiontimeforscoring", "usertscoring", "usertimescoring");
            SetBool(fields, headers, value => parameter.IsUseTimeForAnnotationFiltering = value, "useretentioninformationforfiltering", "useretentiontimeforfiltering", "usertfiltering", "usetimefiltering");
            SetBool(fields, headers, value => parameter.IsUseCcsForAnnotationScoring = value, "useccsforscoring", "useccsscoring");
            SetBool(fields, headers, value => parameter.IsUseCcsForAnnotationFiltering = value, "useccsforfiltering", "useccsfiltering");
        }

        /// <summary>
        /// Whether a library's spectra were acquired or computed, as the settings file states it.
        /// </summary>
        /// <remarks>
        /// The distinction MS-DIAL cannot make by looking: a generated spectrum parses and scores
        /// exactly like an acquired one, and an MSP carries no field that says which. NEIMS for EI,
        /// CFM-ID and ICEBERG for MS/MS all produce libraries that arrive looking experimental.
        /// The answer reaches AnnotationEvidence.ForDatabaseMatch and decides whether a match
        /// against this library is published as a reference-spectrum match or as in silico.
        ///
        /// Silence means acquired, which is what every library was assumed to be before the question
        /// could be asked -- so an existing settings file runs unchanged. An unrecognised value is
        /// reported and treated as silence rather than failing the run: a typo here should not lose
        /// a whole reanalysis, and the run log says what was actually used.
        /// </remarks>
        private static DataBaseSource ReadLibraryKind(string text, string annotatorId) {
            if (text.IsEmptyOrNull()) {
                return DataBaseSource.Msp;
            }
            switch (NormalizeHeader(text)) {
                case "predicted":
                case "insilico":
                case "computed":
                case "generated":
                case "predictedmsp":
                    return DataBaseSource.PredictedMsp;
                case "acquired":
                case "experimental":
                case "measured":
                case "msp":
                    return DataBaseSource.Msp;
                default:
                    Console.WriteLine(
                        $"Unknown library_kind '{text}' for MSP annotator '{annotatorId}'. "
                        + "Expected 'acquired' or 'predicted'; the library will be treated as acquired.");
                    return DataBaseSource.Msp;
            }
        }

        private static string NormalizeHeader(string text) {
            return new string((text ?? string.Empty)
                .Trim()
                .Trim('"')
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private static int FindColumn(string[] headers, params string[] aliases) {
            foreach (var alias in aliases.Select(NormalizeHeader)) {
                for (var i = 0; i < headers.Length; i++) {
                    if (headers[i] == alias) {
                        return i;
                    }
                }
            }
            return -1;
        }

        private static string GetField(string[] fields, int index) {
            return index >= 0 && index < fields.Length
                ? fields[index].Trim().Trim('"')
                : string.Empty;
        }

        private static string GetField(string[] fields, string[] headers, params string[] aliases) {
            return GetField(fields, FindColumn(headers, aliases));
        }

        private static void SetFloat(string[] fields, string[] headers, Action<float> setter, params string[] aliases) {
            var value = GetField(fields, headers, aliases);
            if (value.IsEmptyOrNull()) {
                return;
            }
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                || float.TryParse(value, out parsed)) {
                setter(parsed);
            }
        }

        private static void SetBool(string[] fields, string[] headers, Action<bool> setter, params string[] aliases) {
            var value = GetField(fields, headers, aliases);
            if (value.IsEmptyOrNull()) {
                return;
            }
            if (bool.TryParse(value, out var parsed)) {
                setter(parsed);
            }
            else if (value == "1") {
                setter(true);
            }
            else if (value == "0") {
                setter(false);
            }
        }

        public static MolecularSpectrumNetworkingBaseParameter ReadForMoleculerNetworkingParameter(string filepath) {
            var param = new MolecularSpectrumNetworkingBaseParameter();
            // Not reported: this reader is given the SAME method file as the mode reader above and
            // claims only the networking subset, so every other key in the file would be listed as
            // unrecognised. The mode reader is where a key gets its verdict.
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        ReadMoleculerNetworkingParameter(param, method, value);
                    }
                }
            }
            return param;
        }

       

        public static MsdialDimsParameter ReadForDimsParameter(string filepath) {
            var param = new MsdialDimsParameter();
            var keys = new MethodFileKeys();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        // DimsProcess reads the generated lipid keys itself; asked here only so the
                        // key record says they took effect.
                        keys.Read(method, value, () => Either(ReadCommonParameter(param, method, value),
                            () => GeneratedLipidAnnotatorLine(method, value, new GeneratedLipidAnnotatorSetting())));
                    }
                }
            }
            ApplyGeneratedLipidLbmDefaults(param, keys);
            keys.Report(filepath);
            return param;
        }

        public static MsdialLcImMsParameter ReadForLcImMsParameter(string filepath) {
            var param = new MsdialLcImMsParameter();
            var keys = new MethodFileKeys();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        keys.Read(method, value, () => Either(ReadCommonParameter(param, method, value),
                            () => ReadLcImMsSpecificParameter(param, method, value)));
                    }
                }
            }
            keys.Report(filepath);
            return param;
        }

        public static MsdialImmsParameter ReadForImmsParameter(string filepath) {
            var param = new MsdialImmsParameter();
            var keys = new MethodFileKeys();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        keys.Read(method, value, () => Either(ReadCommonParameter(param, method, value),
                            () => ReadImmsSpecificParameter(param, method, value)));
                    }
                }
            }
            keys.Report(filepath);
            return param;
        }

        private static void readFieldValues(string? line, out string method, out string value, out bool isReadable) {
            method = string.Empty; value = string.Empty; isReadable = false;
            if (string.IsNullOrEmpty(line)) return;
            if (line!.Length < 2) return;
            if (line.TrimStart().StartsWith("#", StringComparison.Ordinal)) return;

            var colonIndex = line.IndexOf(':');
            var equalsIndex = line.IndexOf('=');
            var separatorIndex = colonIndex < 0
                ? equalsIndex
                : equalsIndex < 0
                    ? colonIndex
                    : Math.Min(colonIndex, equalsIndex);
            if (separatorIndex < 0) return;

            method = line.Substring(0, separatorIndex).Trim();
            value = line.Substring(separatorIndex + 1).Trim();
            if (value.Length >= 2
                && ((value[0] == '"' && value[value.Length - 1] == '"')
                    || (value[0] == '\'' && value[value.Length - 1] == '\''))) {
                value = value.Substring(1, value.Length - 2).Trim();
            }
            isReadable = true;
        }

        private static void ResolveGcmsFilePaths(MsdialGcmsParameter param, string methodFilePath) {
            param.MspFilePath = ResolvePathFromMethodFile(param.MspFilePath, methodFilePath);
            param.LbmFilePath = ResolvePathFromMethodFile(param.LbmFilePath, methodFilePath);
            param.TextDBFilePath = ResolvePathFromMethodFile(param.TextDBFilePath, methodFilePath);
            param.IsotopeTextDBFilePath = ResolvePathFromMethodFile(param.IsotopeTextDBFilePath, methodFilePath);
            param.CompoundListInTargetModePath = ResolvePathFromMethodFile(param.CompoundListInTargetModePath, methodFilePath);
            param.CompoundListForRtCorrectionPath = ResolvePathFromMethodFile(param.CompoundListForRtCorrectionPath, methodFilePath);
            param.ReferenceFileParam.RtCorrectionPeakSelectionFilePath = ResolvePathFromMethodFile(param.ReferenceFileParam.RtCorrectionPeakSelectionFilePath, methodFilePath);
            param.RiDictionaryFilePath = ResolvePathFromMethodFile(param.RiDictionaryFilePath, methodFilePath);
        }

        private static string ResolvePathFromMethodFile(string? path, string methodFilePath) {
            if (path.IsEmptyOrNull()) {
                return string.Empty;
            }

            var expanded = Environment.ExpandEnvironmentVariables(path!.Trim());
            if (Path.IsPathRooted(expanded)) {
                return Path.GetFullPath(expanded);
            }

            var methodDirectory = Path.GetDirectoryName(Path.GetFullPath(methodFilePath)) ?? Environment.CurrentDirectory;
            return Path.GetFullPath(Path.Combine(methodDirectory, expanded));
        }

        public static MethodKeyOutcome ReadGcmsSpecificParameter(MsdialGcmsParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            var valueLower = value.ToLower();
            switch (method) {
                case "ri index file pathes":
                case "ri index file paths":
                case "ri dictionary file path":
                case "ri dictionary file paths":
                    param.RiDictionaryFilePath = value;
                    return true;
                case "retention type":
                    if (valueLower == "rt" || valueLower == "ri")
                        param.RetentionType = (RetentionType)Enum.Parse(typeof(RetentionType), valueLower, true);
                    return true;
                case "ri compound":
                case "ri compound type":
                    if (valueLower == "fames" || valueLower == "alkanes")
                        param.RiCompoundType = (RiCompoundType)Enum.Parse(typeof(RiCompoundType), valueLower, true);
                    return true;
                case "alignment index type": if (valueLower == "ri") param.AlignmentIndexType = AlignmentIndexType.RI; else param.AlignmentIndexType = AlignmentIndexType.RT; return true;
                case "retention index tolerance for alignment":
                case "retention index alignment tolerance":
                    return Number(valueLower, v => param.RetentionIndexAlignmentTolerance = (float)v);
                case "replace quant mass by user defined value":
                    return TrueOrFalse(valueLower, v => param.IsReplaceQuantmassByUserDefinedValue = v);
                case "is quant mass based on base peak mz":
                    return TrueOrFalse(valueLower, v => param.IsRepresentativeQuantMassBasedOnBasePeakMz = v);
                default: return false;
            }
        }

        public static MethodKeyOutcome ReadLcImMsSpecificParameter(MsdialLcImMsParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            value = value.ToLower();
            switch (method) {
                case "drift time begin": return Number(value, v => param.DriftTimeBegin = (float)v);
                case "drift time end": return Number(value, v => param.DriftTimeEnd = (float)v);
                case "accumulated rt ragne": return Number(value, v => param.AccumulatedRtRange = (float)v);
                case "accumulate ms2 spectra":
                    return TrueOrFalse(value, v => param.IsAccumulateMS2Spectra = v);
                case "drift time alignment tolerance": return Number(value, v => param.DriftTimeAlignmentTolerance = (float)v);
                case "drift time alignment factor": return Number(value, v => param.DriftTimeAlignmentFactor = (float)v);
                case "ion mobility type":
                    if (value == "tims" || value == "dtims" || value == "twims" || value == "ccs")
                        param.IonMobilityType = (IonMobilityType)Enum.Parse(typeof(IonMobilityType), value, true); return true;
                default: return false;
            }
        }

        public static MethodKeyOutcome ReadImmsSpecificParameter(MsdialImmsParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            value = value.ToLower();
            switch (method) {
                case "drift time begin": return Number(value, v => param.DriftTimeBegin = (float)v);
                case "drift time end": return Number(value, v => param.DriftTimeEnd = (float)v);
                case "drift time alignment tolerance": return Number(value, v => param.DriftTimeAlignmentTolerance = (float)v);
                case "drift time alignment factor": return Number(value, v => param.DriftTimeAlignmentFactor = (float)v);
                case "ion mobility type":
                    if (value == "tims" || value == "dtims" || value == "twims" || value == "ccs")
                        param.IonMobilityType = (IonMobilityType)Enum.Parse(typeof(IonMobilityType), value, true); return true;
                default: return false;
            }
        }

        private static MethodKeyOutcome ReadMoleculerNetworkingParameter(MolecularSpectrumNetworkingBaseParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            var valueLower = value.ToLower();
            switch (method) {
                case "mnrttolerance":
                    return Number(valueLower, v => param.MnRtTolerance = (float)v);
                case "mnioncorrelationsimilaritycutoff":
                    return Number(valueLower, v => param.MnIonCorrelationSimilarityCutOff = (float)v);
                case "mnspectrumsimilaritycutoff":
                    return Number(valueLower, v => param.MnSpectrumSimilarityCutOff = (float)v);
                case "mnrelativeabundancecutoff":
                    return Number(valueLower, v => param.MnRelativeAbundanceCutOff = (float)v);
                case "mnmasstolerance":
                    return Number(valueLower, v => param.MnMassTolerance = (float)v);
                case "minimumpeakmatch":
                    return Number(valueLower, v => param.MinimumPeakMatch = (float)v);
                case "maxedgenumberpernode":
                    return Number(valueLower, v => param.MaxEdgeNumberPerNode = (float)v);
                case "maxprecursordifference":
                    return Number(valueLower, v => param.MaxPrecursorDifference = (float)v);
                case "mnabsoluteabundancecutoff":
                    return Number(valueLower, v => param.MnAbsoluteAbundanceCutOff = (float)v);
                case "msmssimilaritycalc":
                    if (value == "Bonanza" || value == "ModDot" || value == "Cosine" || value == "All")
                        param.MsmsSimilarityCalc = (MsmsSimilarityCalc)Enum.Parse(typeof(MsmsSimilarityCalc), value, true); return true;
                case "mnisexportioncorrelation":
                    return TrueOrFalse(valueLower, v => param.MnIsExportIonCorrelation = v);
                default: return false;
            }
        }

        /// <summary>
        /// Why the blank filtering lines of a method file disagree, or null when they do not.
        /// </summary>
        /// <remarks>
        /// "Sample max / blank average: X" means "Blank filtering: SampleMaxOverBlankAve" with
        /// "Fold change for blank filtering: X", and "Sample average / blank average: X" the same for
        /// SampleAveOverBlankAve. A file that also says something different through the other keys
        /// names two comparisons or two fold changes, and whichever line came last would decide
        /// silently. Earlier text exports wrote "Blank filtering" and both ratio keys, of which only
        /// "Sample max / blank average" took effect, so such files are refused rather than reinterpreted.
        /// </remarks>
        private static string? BlankFilteringConflict(Func<string, string?> used) {
            var mode = used(BlankFilteringKey);
            var foldChange = used(FoldChangeForBlankFilteringKey);
            var sampleMax = used(SampleMaxOverBlankAverageKey);
            var sampleAverage = used(SampleAverageOverBlankAverageKey);
            if (sampleMax != null && sampleAverage != null) {
                return $"'{SampleMaxOverBlankAverageKey}: {sampleMax}' and '{SampleAverageOverBlankAverageKey}: {sampleAverage}' choose different blank filtering comparisons. Write only one of them.";
            }
            var (shorthandKey, shorthandValue, shorthandMode) = sampleMax != null
                ? (SampleMaxOverBlankAverageKey, sampleMax, BlankFiltering.SampleMaxOverBlankAve)
                : (SampleAverageOverBlankAverageKey, sampleAverage, BlankFiltering.SampleAveOverBlankAve);
            if (shorthandValue is null) {
                return null;
            }
            var meaning = $"'{shorthandKey}: {shorthandValue}' is shorthand for '{BlankFilteringKey}: {shorthandMode}' with '{FoldChangeForBlankFilteringKey}: {shorthandValue}'";
            if (mode != null && (BlankFiltering)Enum.Parse(typeof(BlankFiltering), mode, true) != shorthandMode) {
                return $"'{BlankFilteringKey}: {mode}' and '{shorthandKey}: {shorthandValue}' choose different blank filtering comparisons; {meaning}. Remove one of them.";
            }
            if (foldChange != null && ParseFloat(foldChange) != ParseFloat(shorthandValue)) {
                return $"'{FoldChangeForBlankFilteringKey}: {foldChange}' and '{shorthandKey}: {shorthandValue}' set different fold changes; {meaning}. Remove one of them.";
            }
            return null;

            static float ParseFloat(string text) => (float)double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private const string BlankFilteringKey = "Blank filtering";
        private const string FoldChangeForBlankFilteringKey = "Fold change for blank filtering";
        private const string SampleMaxOverBlankAverageKey = "Sample max / blank average";
        private const string SampleAverageOverBlankAverageKey = "Sample average / blank average";

        public static MethodKeyOutcome ReadCommonParameter(ParameterBase param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            var valueLower = value.ToLower();
            switch (method) {
                //Data type
                case "ms1 data type":
                    if (valueLower == "centroid" || valueLower == "profile")
                        param.MSDataType = (MSDataType)Enum.Parse(typeof(MSDataType), valueLower, true);
                    return true;

                case "ms2 data type":
                    if (valueLower == "centroid" || valueLower == "profile")
                        param.MS2DataType = (MSDataType)Enum.Parse(typeof(MSDataType), valueLower, true);
                    return true;

                case "ion mode":
                    if (valueLower == "positive" || valueLower == "negative")
                        param.IonMode = (IonMode)Enum.Parse(typeof(IonMode), valueLower, true);
                    return true;
                
                case "collision type":
                    // EAD is the instrument name for the EIEIO collision type in MS-DIAL.
                    var collisionName = value.Trim();
                    if (string.Equals(collisionName, "EAD", StringComparison.OrdinalIgnoreCase)) {
                        collisionName = nameof(CollisionType.EIEIO);
                    }
                    if (!Enum.TryParse(collisionName, true, out CollisionType collisionType)
                        || !Enum.IsDefined(typeof(CollisionType), collisionType)) {
                        return MethodKeyOutcome.UnusableValue;
                    }
                    param.CollistionType = collisionType;
                    param.LipidQueryContainer.CollisionType = collisionType;
                    return MethodKeyOutcome.Applied;

                case "target omics":
                    if (valueLower == "metabolomics" || valueLower == "lipidomics")
                        param.TargetOmics = (TargetOmics)Enum.Parse(typeof(TargetOmics), valueLower, true);
                    return true;

                case "acquisition type":
                    if (valueLower == "dda" || valueLower == "swath" || valueLower == "aif")
#pragma warning disable CS0618 // Type or member is obsolete
                        // ProjectBaseParameter.AcquisitionType is obsolete, but is used because it is not possible to set the AcquisitionType of individual files in the Console application.
                        param.ProjectParam.AcquisitionType = (AcquisitionType)Enum.Parse(typeof(AcquisitionType), valueLower, true);
#pragma warning restore CS0618 // Type or member is obsolete
                    return true;

                //{ GCMS, LCMS, IMMS, LCIMMS, IFMS, IIMMS, IDIMS, }
                case "machine category":
                    if (value == "GCMS" || value == "LCMS" || value == "IMMS" || value == "LCIMMS" || value == "IFMS" || value == "IIMMS" || value == "IDIMS")
                        param.ProjectParam.MachineCategory = (MachineCategory)Enum.Parse(typeof(MachineCategory), valueLower, true);
                    return true;

                case "solvent type":
                    if (value == "CH3COONH4" || value == "HCOONH4" || value == "NH4HCO3")
                        param.LipidQueryContainer.SolventType = (SolventType)Enum.Parse(typeof(SolventType), valueLower, true);
                    return true;

                case "searched lipid class":
                    if (!value.IsEmptyOrNull()) {
                        param.LipidQueryContainer = new LipidQueryBean() {
                            SolventType = SolventType.CH3COONH4,
                            LbmQueries = LbmQueryParcer.GetLbmQueries(isLabUseOnly: param.IsLabPrivate)
                        };

                        param.LipidQueryContainer.LbmQueries = param.LipidQueryContainer.LbmQueries.Where(n => n.IonMode == param.IonMode).ToList();
                        foreach (var l in param.LipidQueryContainer.LbmQueries) l.IsSelected = false;

                        var aStrings = value.Split(';');
                        foreach (var lipidString in aStrings) {
                            if (lipidString.Split(' ').Length >= 2) {
                                var lipidclass = lipidString.Split(' ')[0];
                                var adducttype = lipidString.Split(' ')[1];
                                if (!Enum.IsDefined(typeof(LbmClass), lipidclass)) continue;
                                var adductObj = AdductIon.GetAdductIon(adducttype);
                                if (!adductObj.FormatCheck) continue;

                                foreach (var l in param.LipidQueryContainer.LbmQueries) {
                                    if (l.LbmClass.ToString() == lipidclass && adductObj.ToString() == l.AdductType.ToString()) {
                                        l.IsSelected = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    return true;

                //File paths
                case "msp file path": param.MspFilePath = value; return true;
                case "lbm file path": param.LbmFilePath = value; return true;
                case "text db file path": param.TextDBFilePath = value; return true;
                case "isotope text db file path": param.IsotopeTextDBFilePath = value; return true;
                case "compounds library file path for target detection": param.CompoundListInTargetModePath = value; return true;
                // Only the path is read here. The library is loaded by RetentionTimeCorrectionProcess,
                // from the path as it stands when the run starts; see LoadStandards there for why.
                case "compounds library file path for rt correction": param.CompoundListForRtCorrectionPath = value; return true;
                case "rt correction peak selection file path": param.ReferenceFileParam.RtCorrectionPeakSelectionFilePath = value; return true;

                // Private version
                case "is private version of tada":
                    return TrueOrFalse(valueLower, v => param.IsLabPrivateVersionTada = v);
                case "is private version":
                    return TrueOrFalse(valueLower, v => param.IsLabPrivate = v);

                //Data correction
                case "retention time begin": return Number(valueLower, v => param.RetentionTimeBegin = (float)v);
                case "retention time end": return Number(valueLower, v => param.RetentionTimeEnd = (float)v);
                case "ms1 mass range begin": return Number(valueLower, v => param.MassRangeBegin = (float)v);
                case "ms1 mass range end": return Number(valueLower, v => param.MassRangeEnd = (float)v);
                case "ms2 mass range begin": return Number(valueLower, v => param.Ms2MassRangeBegin = (float)v);
                case "ms2 mass range end": return Number(valueLower, v => param.Ms2MassRangeEnd = (float)v);
                case "accuracy type":
                    if (valueLower == "isnominal" || valueLower == "isaccurate")
                        param.AccuracyType = (AccuracyType)Enum.Parse(typeof(AccuracyType), valueLower, true);
                    return true;

                //Centroid parameters
                case "ms1 tolerance for centroid": return Number(valueLower, v => param.CentroidMs1Tolerance = (float)v);
                case "ms2 tolerance for centroid": return Number(valueLower, v => param.CentroidMs2Tolerance = (float)v);

                //Peak detection param
                case "smoothing method":
                    if (Enum.TryParse(value, true, out SmoothingMethod smoothingMethod))
                        param.SmoothingMethod = smoothingMethod;
                    return true;
                case "smoothing level": return Count(valueLower, v => param.SmoothingLevel = v);
                case "average peak width": return Count(valueLower, v => param.AveragePeakWidth = v);
                case "minimum peak width": return Count(valueLower, v => param.MinimumDatapoints = v);
                case "minimum peak height": return Count(valueLower, v => param.MinimumAmplitude = v);
                case "mass slice width": return Number(valueLower, v => param.MassSliceWidth = (float)v);
                case "mass accuracy": return Number(valueLower, v => param.CentroidMs1Tolerance = (float)v);
                case "max charge number": return Count(valueLower, v => param.MaxChargeNumber = v);
                case "searched adduct ions": return SearchedAdductIons(value, adducts => param.SearchedAdductIons = adducts);

                


                //Deconvolution
                case "sigma window value": return Number(valueLower, v => param.SigmaWindowValue = (float)v);
                case "amplitude cut off": return Number(valueLower, v => param.ChromDecBaseParam.AmplitudeCutoff = (float)v);
                case "relative amplitude cut off": return Number(valueLower, v => param.ChromDecBaseParam.RelativeAmplitudeCutoff = (float)v);
                case "keep isotope range": return Number(valueLower, v => param.KeptIsotopeRange = (float)v);
                case "exclude after precursor": return TrueOrFalse(valueLower, v => param.RemoveAfterPrecursor = v);
                case "keep original precursor isotopes": return TrueOrFalse(valueLower, v => param.KeepOriginalPrecursorIsotopes = v);
                case "target ce": return Number(valueLower, v => param.TargetCE = v);

                //Identification
                case "rt tolerance for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.RtTolerance = (float)v);
                case "ri tolerance for msp-based annotation":
                case "ri tolerance for identification":
                case "retention index tolerance for identification":
                    return Number(valueLower, v => param.MspSearchParam.RiTolerance = (float)v);
                case "ccs tolerance for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.CcsTolerance = (float)v);
                case "mass range begin for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.MassRangeBegin = (float)v);
                case "mass range end for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.MassRangeEnd = (float)v);
                case "relative amplitude cutoff for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.RelativeAmpCutoff = (float)v);
                case "absolute amplitude cutoff for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.AbsoluteAmpCutoff = (float)v);
                case "weighted dot product cutoff for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.SquaredWeightedDotProductCutOff = (float)v);
                case "simple dot product cutoff for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.SquaredSimpleDotProductCutOff = (float)v);
                case "reverse dot product cutoff for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.SquaredReverseDotProductCutOff = (float)v);
                case "weighted dot product cutoff":
                case "square root of weighted dot product cutoff for msp-based annotation":
                    return Number(valueLower, v => param.MspSearchParam.WeightedDotProductCutOff = (float)v);
                case "simple dot product cutoff":
                case "square root of simple dot product cutoff for msp-based annotation":
                    return Number(valueLower, v => param.MspSearchParam.SimpleDotProductCutOff = (float)v);
                case "reverse dot product cutoff":
                case "square root of reverse dot product cutoff for msp-based annotation":
                    return Number(valueLower, v => param.MspSearchParam.ReverseDotProductCutOff = (float)v);
                case "matched peaks percentage cutoff":
                case "matched peaks percentage cutoff for msp-based annotation":
                    return Number(valueLower, v => param.MspSearchParam.MatchedPeaksPercentageCutOff = (float)v);
                case "minimum spectrum match":
                case "minimum spectrum match for msp-based annotation":
                    return Number(valueLower, v => param.MspSearchParam.MinimumSpectrumMatch = (float)v);
                case "total score cutoff for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.TotalScoreCutoff = (float)v);
                case "ms1 tolerance for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.Ms1Tolerance = (float)v);
                case "ms2 tolerance for msp-based annotation": return Number(valueLower, v => param.MspSearchParam.Ms2Tolerance = (float)v);
                case "use retention information for msp-based annotation scoring": return TrueOrFalse(valueLower, v => param.MspSearchParam.IsUseTimeForAnnotationScoring = v);
                case "use retention information for msp-based annotation filtering": return TrueOrFalse(valueLower, v => param.MspSearchParam.IsUseTimeForAnnotationFiltering = v);
                case "use ccs for msp-based annotation scoring": return TrueOrFalse(valueLower, v => param.MspSearchParam.IsUseCcsForAnnotationScoring = v);
                case "use ccs for msp-based annotation filtering": return TrueOrFalse(valueLower, v => param.MspSearchParam.IsUseCcsForAnnotationFiltering = v);
                case "only report top hit for msp-based annotation": return TrueOrFalse(valueLower, v => param.OnlyReportTopHitInMspSearch = v);
                case "execute annotation process only for alignment file":
                case "execute annotation process only for alignment file for msp-based annotation":
                    return TrueOrFalse(valueLower, v => param.IsIdentificationOnlyPerformedForAlignmentFile = v);

                //Identification
                case "rt tolerance for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.RtTolerance = (float)v);
                case "ri tolerance for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.RiTolerance = (float)v);
                case "ccs tolerance for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.CcsTolerance = (float)v);
                case "mass range begin for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.MassRangeBegin = (float)v);
                case "mass range end for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.MassRangeEnd = (float)v);
                case "relative amplitude cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.RelativeAmpCutoff = (float)v);
                case "absolute amplitude cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.AbsoluteAmpCutoff = (float)v);
                case "weighted dot product cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.SquaredWeightedDotProductCutOff = (float)v);
                case "simple dot product cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.SquaredSimpleDotProductCutOff = (float)v);
                case "reverse dot product cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.SquaredReverseDotProductCutOff = (float)v);
                case "square root of weighted dot product cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.WeightedDotProductCutOff = (float)v);
                case "square root of simple dot product cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.SimpleDotProductCutOff = (float)v);
                case "square root of reverse dot product cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.ReverseDotProductCutOff = (float)v);
                case "matched peaks percentage cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.MatchedPeaksPercentageCutOff = (float)v);
                case "minimum spectrum match for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.MinimumSpectrumMatch = (float)v);
                case "total score cutoff for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.TotalScoreCutoff = (float)v);
                case "ms1 tolerance for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.Ms1Tolerance = (float)v);
                case "ms2 tolerance for lbm-based annotation": return Number(valueLower, v => param.LbmSearchParam.Ms2Tolerance = (float)v);
                case "use retention information for lbm-based annotation scoring": return TrueOrFalse(valueLower, v => param.LbmSearchParam.IsUseTimeForAnnotationScoring = v);
                case "use retention information for lbm-based annotation filtering": return TrueOrFalse(valueLower, v => param.LbmSearchParam.IsUseTimeForAnnotationFiltering = v);
                case "use ccs for lbm-based annotation scoring": return TrueOrFalse(valueLower, v => param.LbmSearchParam.IsUseCcsForAnnotationScoring = v);
                case "use ccs for lbm-based annotation filtering": return TrueOrFalse(valueLower, v => param.LbmSearchParam.IsUseCcsForAnnotationFiltering = v);
                case "execute annotation process only for alignment file for lbm-based annotation": return TrueOrFalse(valueLower, v => param.IsIdentificationOnlyPerformedForAlignmentFile = v);


                //Post identification
                case "rt tolerance for text-based annotation": return Number(valueLower, v => param.TextDbSearchParam.RtTolerance = (float)v);
                case "ri tolerance for text-based annotation": return Number(valueLower, v => param.TextDbSearchParam.RiTolerance = (float)v);
                case "ccs tolerance for text-based annotation": return Number(valueLower, v => param.TextDbSearchParam.CcsTolerance = (float)v);
                case "total score cutoff for text-based annotation": return Number(valueLower, v => param.TextDbSearchParam.TotalScoreCutoff = (float)v);
                case "accurate ms1 tolerance for text-based annotation": return Number(valueLower, v => param.TextDbSearchParam.Ms1Tolerance = (float)v);
                case "use retention information for text-based annotation scoring": return TrueOrFalse(valueLower, v => param.TextDbSearchParam.IsUseTimeForAnnotationScoring = v);
                case "use retention information for text-based annotation filtering": return TrueOrFalse(valueLower, v => param.TextDbSearchParam.IsUseTimeForAnnotationFiltering = v);
                case "use ccs for text-based annotation scoring": return TrueOrFalse(valueLower, v => param.TextDbSearchParam.IsUseCcsForAnnotationScoring = v);
                case "use ccs for text-based annotation filtering": return TrueOrFalse(valueLower, v => param.TextDbSearchParam.IsUseCcsForAnnotationFiltering = v);
                case "only report top hit for text-based annotation": return TrueOrFalse(valueLower, v => param.OnlyReportTopHitInTextDBSearch = v);

                //Alignment parameters setting
                case "alignment reference file id": return Count(valueLower, v => param.AlignmentReferenceFileID = v);
                case "retention time tolerance for alignment": return Number(valueLower, v => param.RetentionTimeAlignmentTolerance = (float)v);
                case "retention time factor for alignment": return Number(valueLower, v => param.RetentionTimeAlignmentFactor = (float)v);
                case "spectrum similarity tolerance for alignment": return Number(valueLower, v => param.SpectrumSimilarityAlignmentTolerance = (float)v);
                case "spectrum similarity factor for alignment": return Number(valueLower, v => param.SpectrumSimilarityAlignmentFactor = (float)v);
                case "ms1 tolerance for alignment": return Number(valueLower, v => param.Ms1AlignmentTolerance = (float)v);
                case "ms1 factor for alignment": return Number(valueLower, v => param.Ms1AlignmentFactor = (float)v);
                case "force insert peaks in gap filling": return TrueOrFalse(valueLower, v => param.IsForceInsertForGapFilling = v);
                case "together with alignment": return TrueOrFalse(valueLower, v => param.TogetherWithAlignment = v);
                case "execute automatic rt correction for alignment": return TrueOrFalse(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.Execute = v);
                case "automatic rt correction reference file id": return Count(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.ReferenceFileId = v);
                case "automatic rt correction rt bin width": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.RtBinWidth = (float)v);
                case "automatic rt correction match rt tolerance": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MatchRtTolerance = (float)v);
                case "automatic rt correction minimum anchors": return Count(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MinimumAnchorCount = v);
                case "automatic rt correction maximum anchors": return Count(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MaximumAnchorCount = v);
                case "automatic rt correction minimum sample coverage": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MinimumSampleCoverage = (float)v);
                case "automatic rt correction intensity quantile": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.IntensityQuantile = (float)v);
                case "automatic rt correction maximum peak width quantile": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MaximumPeakWidthQuantile = (float)v);
                case "automatic rt correction minimum signal to noise": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MinimumSignalToNoise = (float)v);
                case "automatic rt correction minimum gaussian similarity": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MinimumGaussianSimilarity = (float)v);
                case "automatic rt correction minimum ideal slope": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.MinimumIdealSlope = (float)v);
                case "automatic rt correction outlier mad threshold": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.OutlierMadThreshold = (float)v);
                case "automatic rt correction reference centrality weight": return Number(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.ReferenceCentralityWeight = (float)v);
                case "automatic rt correction interpolate blanks by analytical order": return TrueOrFalse(valueLower, v => param.AlignmentBaseParam.AutomaticRtCorrection.InterpolateBlankByAnalyticalOrder = v);

                //Filtering
                case "peak count filter": return Number(valueLower, v => param.PeakCountFilter = (float)v);
                case "n percent detected in one group": return Number(valueLower, v => param.NPercentDetectedInOneGroup = (float)v);
                case "remove feature based on peak height fold-change": return TrueOrFalse(valueLower, v => param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = v);
                // "Blank filtering" and "Fold change for blank filtering" are the settings. The two ratio keys are
                // shorthand for both at once; BlankFilteringConflict refuses a file whose lines disagree.
                case "blank filtering":
                    return Enum.TryParse(value, true, out BlankFiltering blankFiltering) && Enum.IsDefined(typeof(BlankFiltering), blankFiltering)
                        ? Assign(blankFiltering, v => param.BlankFiltering = v)
                        : MethodKeyOutcome.UnusableValue;
                case "fold change for blank filtering": return Number(valueLower, v => param.FoldChangeForBlankFiltering = (float)v);
                case "sample max / blank average":
                    return Number(valueLower, v => {
                        param.BlankFiltering = BlankFiltering.SampleMaxOverBlankAve;
                        param.FoldChangeForBlankFiltering = (float)v;
                    });
                case "sample average / blank average":
                    return Number(valueLower, v => {
                        param.BlankFiltering = BlankFiltering.SampleAveOverBlankAve;
                        param.FoldChangeForBlankFiltering = (float)v;
                    });
                case "keep reference matched metabolites": return TrueOrFalse(valueLower, v => param.IsKeepRefMatchedMetaboliteFeatures = v);
                case "keep suggested metabolites": return TrueOrFalse(valueLower, v => param.IsKeepSuggestedMetaboliteFeatures = v);
                case "keep removable features and assigned tag for checking": return TrueOrFalse(valueLower, v => param.IsKeepRemovableFeaturesAndAssignedTagForChecking = v);
                case "replace true zero values with 1/2 of minimum peak height over all samples": return TrueOrFalse(valueLower, v => param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = v);

                //Retentiontime correction
                case "execute rt correction": return TrueOrFalse(valueLower, v => param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = v);
                case "rt correction with smoothing for rt diff": return TrueOrFalse(valueLower, v => param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.doSmoothing = v);
                case "user setting intercept": return Number(valueLower, v => param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.UserSettingIntercept = (float)v);
                case "rt diff calc method":
                    if (valueLower == "sampleminussampleaverage" || valueLower == "sampleminusreference")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.RtDiffCalcMethod = (RtDiffCalcMethod)Enum.Parse(typeof(RtDiffCalcMethod), valueLower, true);
                    return true;
                case "interpolation method":
                    if (valueLower == "linear")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.InterpolationMethod = InterpolationMethod.Linear;
                    return true;
                case "extrapolation method (begin)":
                    if (valueLower == "usersetting" || valueLower == "firstpoint" || valueLower == "linearextrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodBegin = (ExtrapolationMethodBegin)Enum.Parse(typeof(ExtrapolationMethodBegin), valueLower, true);
                    return true;
                case "extrapolation method (end)":
                    if (valueLower == "lastpoint" || valueLower == "linearextrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodEnd = (ExtrapolationMethodEnd)Enum.Parse(typeof(ExtrapolationMethodEnd), valueLower, true);
                    return true;
                case "rt correction peak selection mode":
                    if (Enum.TryParse(valueLower, true, out RetentionTimeCorrectionPeakSelectionMode peakSelectionMode))
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.PeakSelectionMode = peakSelectionMode;
                    return true;
                case "rt correction peak selection rt weight":
                    return Number(valueLower, v => {
                        if (v < 0d || v > 1d) throw new FormatException($"Peak selection RT weight must be between 0 and 1, not {v}.");
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.PeakSelectionRtWeight = v;
                    });

                //Isotope tracking setting
                case "tracking isotope label": return TrueOrFalse(valueLower, v => param.TrackingIsotopeLabels = v);
                case "set fully labeled reference file": return TrueOrFalse(valueLower, v => param.SetFullyLabeledReferenceFile = v);
                case "non labeled reference id": return Count(valueLower, v => param.NonLabeledReferenceID = v);
                case "fully labeled reference id": return Count(valueLower, v => param.FullyLabeledReferenceID = v);
                // ParameterBase writes "Number of threads" into every exported method file,
                // but nothing read it back, so a method file could describe a thread count
                // it could never request and every Console run stayed on the default of 2.
                case "number of threads":
                    // A thread count of zero or less is refused rather than ignored. ProcessRunner
                    // now throws on a non-positive count, so accepting it here only moves the
                    // failure to a place with less context about where the number came from.
                    return Count(valueLower, v => {
                        if (v <= 0) throw new FormatException($"Number of threads must be positive, not {v}.");
                        param.NumThreads = v;
                    });
                case "isotope tracking dictionary id": return Count(valueLower, v => param.IsotopeTrackingDictionary.SelectedID = v);

                //CorrDec settings
                case "corrdec execute":
                    return TrueOrFalse(valueLower, v => param.CorrDecParam.CanExcute = v);
                case "corrdec ms2 tolerance":
                    return Number(valueLower, v => param.CorrDecParam.MS2Tolerance = (float)v);
                case "corrdec minimum ms2 peak height":
                    return Count(valueLower, v => param.CorrDecParam.MinMS2Intensity = v);
                case "corrdec minimum number of detected samples":
                    return Count(valueLower, v => param.CorrDecParam.MinNumberOfSample = v);
                case "corrdec exclude highly correlated spots":
                    return Number(valueLower, v => param.CorrDecParam.MinCorr_MS1 = (float)v);
                case "corrdec minimum correlation coefficient (ms2)":
                    return Number(valueLower, v => param.CorrDecParam.MinCorr_MS2 = (float)v);
                case "corrdec margin 1 (target precursor)":
                    return Number(valueLower, v => param.CorrDecParam.CorrDiff_MS1 = (float)v);
                case "corrdec margin 2 (coeluted precursor)":
                    return Number(valueLower, v => param.CorrDecParam.CorrDiff_MS2 = (float)v);
                case "corrdec minimum detected rate":
                    return Number(valueLower, v => param.CorrDecParam.MinDetectedPercentToVisualize = (float)v);
                case "corrdec minimum ms2 relative intensity":
                    return Number(valueLower, v => param.CorrDecParam.MinMS2RelativeIntensity = (float)v);
                case "corrdec remove peaks larger than precursor":
                    return TrueOrFalse(valueLower, v => param.CorrDecParam.CorrDecRemoveAfterPrecursor = v);
                default: return false;
            }
        }
      
        #endregion
    }
}
