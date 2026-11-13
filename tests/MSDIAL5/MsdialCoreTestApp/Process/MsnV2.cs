using CompMs.App.MsdialConsole.Parser;
using CompMs.Common;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process.MoleculerNetworking
{
    /// <summary>Which side of a v2 row has its identity columns blanked.</summary>
    public enum MsnRedaction { None, Target, Query, Both }

    /// <summary>The options of <c>msn --format v2</c>.</summary>
    public sealed class MsnV2Options
    {
        /// <summary>Write every scored pair: no MinimumPeakMatch, no cutoff, no top-k. MaxPrecursorDifference still applies.</summary>
        public bool AllPairs { get; set; }
        /// <summary>The kernels, in column order; null means the method file's MsmsSimilarityCalc. The first is primary.</summary>
        public IReadOnlyList<MsmsSimilarityCalc>? Kernels { get; set; }
        public MsnRedaction Redaction { get; set; } = MsnRedaction.None;
        public int Threads { get; set; } = 1;
    }

    /// <summary>The run cannot start as asked: an option it cannot honour. msn exits with 2.</summary>
    public sealed class MsnUsageException : Exception
    {
        public MsnUsageException(string message) : base(message) { }
    }

    /// <summary>One record as v2 names it.</summary>
    internal sealed class MsnRecord
    {
        public int Ordinal { get; set; }
        public string PeakId { get; set; } = string.Empty;
        public string DbId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string InChIKey { get; set; } = string.Empty;
        public double PrecursorMz { get; set; }
        public string Adduct { get; set; } = string.Empty;
        public IReadOnlyList<SpectrumPeak> Spectrum { get; set; } = Array.Empty<SpectrumPeak>();
    }

    /// <summary>The usable records of one input, with what was excluded and why.</summary>
    internal sealed class MsnRecordSet
    {
        public string FileName { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        /// <summary>Read by the MSP parser, which reads numbers in single precision.</summary>
        public bool IsMsp { get; set; }
        public List<MsnRecord> Records { get; } = new List<MsnRecord>();
        public int NoPeaks { get; set; }
        public int WrongIonMode { get; set; }
        public int PrecursorNotPositive { get; set; }

        public string Excluded => $"no_peaks={NoPeaks},wrong_ionmode={WrongIonMode},precursor_le_0={PrecursorNotPositive}";

        // The order of the reasons is the order they are tested in; a record is counted once.
        public void Add(MsnRecord record, bool hasPeaks, bool ionModeMatches) {
            if (!hasPeaks) NoPeaks++;
            else if (!ionModeMatches) WrongIonMode++;
            else if (!(record.PrecursorMz > 0d)) PrecursorNotPositive++;
            else Records.Add(record);
        }
    }

    /// <summary>
    /// <c>msn --format v2</c>: metadata lines, ID columns for both records, and one score and matched
    /// column per kernel, named by its kernel identity.
    /// </summary>
    /// <remarks>
    /// Rows are ordered by (query ordinal, target ordinal) for any thread count: queries are scored in
    /// blocks in parallel and each block is written in order. No line carries a time or an absolute
    /// path, so two runs over the same files with the same build are byte-identical.
    /// </remarks>
    internal static class MsnV2
    {
        private static readonly Regex PeakIdPattern = new Regex(@"(?:^|\|)PEAKID=(\d+)", RegexOptions.CultureInvariant);

        public static MsnRecordSet LoadMsp(string path, string ionMode) {
            var set = new MsnRecordSet { FileName = Path.GetFileName(path), Sha256 = Sha256(path), IsMsp = true, };
            foreach (var reference in LibraryHandler.ReadMspLibrary(path)) {
                var peakId = PeakIdPattern.Match(reference.Comment ?? string.Empty);
                set.Add(new MsnRecord {
                    Ordinal = reference.ScanID,
                    PeakId = peakId.Success ? peakId.Groups[1].Value : string.Empty,
                    DbId = reference.DatabaseUniqueIdentifier ?? string.Empty,
                    Name = reference.Name ?? string.Empty,
                    InChIKey = reference.InChIKey ?? string.Empty,
                    PrecursorMz = reference.PrecursorMz,
                    Adduct = reference.AdductType?.AdductIonName ?? string.Empty,
                    Spectrum = reference.Spectrum ?? new List<SpectrumPeak>(),
                }, hasPeaks: reference.Spectrum?.Count > 0, ionModeMatches: reference.IonMode.ToString() == ionMode);
            }
            return set;
        }

        public static MsnRecordSet LoadBinary<T>(MolecularNetworkingInput<T> input, string path, IonMode ionMode) where T : IMoleculeProperty, IChromatogramPeak {
            var set = new MsnRecordSet { FileName = Path.GetFileName(path), Sha256 = Sha256(path) };
            for (int i = 0; i < input.Spots.Count; i++) {
                var spot = input.Spots[i];
                var scan = input.Scans[i];
                set.Add(new MsnRecord {
                    Ordinal = i,
                    PeakId = spot.ID.ToString(CultureInfo.InvariantCulture),
                    Name = spot.Name ?? string.Empty,
                    InChIKey = spot.InChIKey ?? string.Empty,
                    PrecursorMz = scan.PrecursorMz,
                    Spectrum = scan.Spectrum ?? new List<SpectrumPeak>(),
                }, hasPeaks: scan.Spectrum?.Count > 0, ionModeMatches: scan.IonMode == ionMode);
            }
            return set;
        }

        public static string Sha256(string path) {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create()) {
                return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        /// <summary>The kernels of a v2 run. MS-DIAL's Cosine mode has no registered identity and is refused.</summary>
        public static List<IMsmsSimilarityKernel> Kernels(MolecularSpectrumNetworkingBaseParameter parameter, MsnV2Options options) {
            var calcs = options.Kernels ?? new[] { parameter.MsmsSimilarityCalc };
            if (calcs.Count == 0) {
                throw new MsnUsageException("--kernels names no kernel.");
            }
            if (calcs.Contains(MsmsSimilarityCalc.Cosine)) {
                throw new MsnUsageException(
                    "MS-DIAL's Cosine mode (a squared simple dot product) has no registered kernel identity, so --format v2 cannot name its column. "
                    + "Use v1 for it, or another kernel; it becomes available in v2 once the spectrum catalog's similarity module registers a base for it.");
            }
            return MsmsSimilarityKernelFactory.CreateMany(calcs, MsnSettings.ToQuery(parameter));
        }

        public static void Write(string output, MsnRecordSet query, MsnRecordSet? target, string ionMode,
            MolecularSpectrumNetworkingBaseParameter parameter, MsnV2Options options) {
            var kernels = Kernels(parameter, options);
            if (options.Threads < 1) {
                throw new MsnUsageException("--threads must be at least 1.");
            }
            var self = target is null;
            var targets = target ?? query;
            // Each kernel scores its own preprocessed copy; the loaded spectra are never changed.
            var queryScans = kernels.Select(kernel => Preprocess(kernel, query.Records)).ToArray();
            var targetScans = self ? queryScans : kernels.Select(kernel => Preprocess(kernel, targets.Records)).ToArray();
            var redactQuery = options.Redaction == MsnRedaction.Both || options.Redaction == MsnRedaction.Query || (self && options.Redaction != MsnRedaction.None);
            var redactTarget = options.Redaction == MsnRedaction.Both || options.Redaction == MsnRedaction.Target || (self && options.Redaction != MsnRedaction.None);

            AtomicOutputFile.Write(output, writer => {
                writer.NewLine = "\n";
                WriteHeader(writer, query, target, ionMode, parameter, options, kernels);

                var blockSize = Math.Max(1, options.Threads);
                var blockRows = new string[blockSize];
                var parallel = new ParallelOptions { MaxDegreeOfParallelism = options.Threads, };
                for (int start = 0; start < query.Records.Count; start += blockSize) {
                    var end = Math.Min(start + blockSize, query.Records.Count);
                    var first = start;
                    Parallel.For(first, end, parallel, i => blockRows[i - first] = Rows(i, self, query, targets, queryScans, targetScans, kernels, parameter, options, redactQuery, redactTarget));
                    for (int i = first; i < end; i++) {
                        writer.Write(blockRows[i - first]);
                        blockRows[i - first] = null!;
                    }
                    Console.Error.WriteLine("{0} / {1}", end, query.Records.Count);
                }
            });
        }

        private static MSScanProperty[] Preprocess(IMsmsSimilarityKernel kernel, List<MsnRecord> records) {
            return records.Select(record => new MSScanProperty {
                ScanID = record.Ordinal,
                PrecursorMz = record.PrecursorMz,
                Spectrum = kernel.Preprocess(record.Spectrum, record.PrecursorMz),
            }).ToArray();
        }

        private static string Rows(int i, bool self, MsnRecordSet query, MsnRecordSet targets,
            MSScanProperty[][] queryScans, MSScanProperty[][] targetScans, List<IMsmsSimilarityKernel> kernels,
            MolecularSpectrumNetworkingBaseParameter parameter, MsnV2Options options, bool redactQuery, bool redactTarget) {
            var builder = new StringBuilder();
            var q = query.Records[i];
            var scores = new SimilarityScore[kernels.Count];
            for (int j = self ? i + 1 : 0; j < targets.Records.Count; j++) {
                var t = targets.Records[j];
                if (Math.Abs(q.PrecursorMz - t.PrecursorMz) > parameter.MaxPrecursorDifference) continue;
                if (!options.AllPairs) {
                    if (queryScans[0][i].Spectrum.Count == 0 || targetScans[0][j].Spectrum.Count == 0) continue;
                    scores[0] = kernels[0].Score(queryScans[0][i], targetScans[0][j]);
                    if (scores[0].Matched < parameter.MinimumPeakMatch) continue;
                    if (scores[0].Score < parameter.MnSpectrumSimilarityCutOff * 0.01) continue;
                }
                else {
                    scores[0] = kernels[0].Score(queryScans[0][i], targetScans[0][j]);
                }
                for (int k = 1; k < kernels.Count; k++) {
                    scores[k] = kernels[k].Score(queryScans[k][i], targetScans[k][j]);
                }
                AppendRecord(builder, q, redactQuery);
                AppendRecord(builder, t, redactTarget);
                builder.Append(Number(q.PrecursorMz - t.PrecursorMz));
                foreach (var score in scores) {
                    builder.Append('\t').Append(Number(score.Score)).Append('\t').Append(score.Matched.ToString(CultureInfo.InvariantCulture));
                }
                builder.Append('\n');
            }
            return builder.ToString();
        }

        private static void AppendRecord(StringBuilder builder, MsnRecord record, bool redact) {
            builder.Append(record.Ordinal.ToString(CultureInfo.InvariantCulture)).Append('\t')
                .Append(Text(record.PeakId)).Append('\t')
                .Append(redact ? string.Empty : Text(record.DbId)).Append('\t')
                .Append(redact ? string.Empty : Text(record.Name)).Append('\t')
                .Append(redact ? string.Empty : Text(record.InChIKey)).Append('\t')
                .Append(Number(record.PrecursorMz)).Append('\t')
                .Append(Text(record.Adduct)).Append('\t');
        }

        private static string Text(string value) {
            return (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static void WriteHeader(TextWriter writer, MsnRecordSet query, MsnRecordSet? target, string ionMode,
            MolecularSpectrumNetworkingBaseParameter parameter, MsnV2Options options, List<IMsmsSimilarityKernel> kernels) {
            writer.WriteLine("#msn_format=2");
            writer.WriteLine($"#console={MsdialBuildIdentity.DisplayVersion} commit={MsdialBuildIdentity.FullCommitId}");
            writer.WriteLine($"#mode={(target is null ? "self" : "target")}");
            writer.WriteLine($"#query_file={query.FileName} sha256={query.Sha256}");
            writer.WriteLine(target is null ? "#target_file=self" : $"#target_file={target.FileName} sha256={target.Sha256}");
            writer.WriteLine($"#ionmode={ionMode}");
            writer.WriteLine("#kernels=" + string.Join("\t", kernels.Select(kernel => kernel.Identity)));
            writer.WriteLine($"#primary={kernels[0].Identity}");
            writer.WriteLine($"#all_pairs={(options.AllPairs ? "true" : "false")}");
            writer.WriteLine($"#max_precursor_difference={Number(parameter.MaxPrecursorDifference)}");
            writer.WriteLine($"#minimum_peak_match={(options.AllPairs ? "none" : Number(parameter.MinimumPeakMatch))}");
            writer.WriteLine($"#cutoff={(options.AllPairs ? "none" : Number(parameter.MnSpectrumSimilarityCutOff) + "%")}");
            writer.WriteLine("#top_k=none");
            writer.WriteLine($"#excluded_query={query.Excluded}");
            writer.WriteLine($"#excluded_target={(target is null ? "self" : target.Excluded)}");
            writer.WriteLine($"#redact_identity={options.Redaction.ToString().ToLowerInvariant()}");
            writer.WriteLine("#precursor_delta=q_precursor_mz-t_precursor_mz");
            if (query.IsMsp || target?.IsMsp == true) {
                writer.WriteLine("#msp_numbers=float32 (the MSP reader parses precursor m/z, peak m/z and intensity in single precision)");
            }
            var columns = new List<string> {
                "q_ordinal", "q_peak_id", "q_db_id", "q_name", "q_inchikey", "q_precursor_mz", "q_adduct",
                "t_ordinal", "t_peak_id", "t_db_id", "t_name", "t_inchikey", "t_precursor_mz", "t_adduct",
                "precursor_delta",
            };
            foreach (var kernel in kernels) {
                columns.Add($"score[{kernel.Identity}]");
                columns.Add($"matched[{kernel.Identity}]");
            }
            writer.WriteLine(string.Join("\t", columns));
        }
    }
}
