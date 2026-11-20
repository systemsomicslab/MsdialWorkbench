using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>One row of the timing file (design 5.8). A null count or time is an empty cell.</summary>
    public sealed class DiagnosticTimingRow
    {
        public string FileName { get; set; }
        public int Ms2Peaks { get; set; }
        public int? RulesEvaluated { get; set; }
        public int? HypothesesTested { get; set; }
        public int? Outcomes { get; set; }
        public int? Candidates { get; set; }
        public int? UniqueSmiles { get; set; }
        public long? CacheHits { get; set; }
        public double? EvalMs { get; set; }
        public double? MatchMs { get; set; }
        public double? FormulaStepMs { get; set; }
        public double? StructureStepMs { get; set; }
    }

    /// <summary>
    /// The per-query runtime measurement of a console run with evaluation on (design 5.8). The formula and structure
    /// steps run in parallel, so their times are recorded by query (thread-safe) and looked up when the rows are
    /// written in query order; no output order comes from the lookup.
    /// </summary>
    public sealed class DiagnosticTimingLog
    {
        /// <summary>Milliseconds are written with 3 decimals (design 5.5).</summary>
        public const int MsDecimals = 3;

        public static readonly string[] Columns =
        {
            "File name", "MS2 peaks", "Rules evaluated", "Hypotheses tested", "Outcomes", "Candidates", "Unique SMILES",
            "Cache hits", "Eval ms", "Match ms", "Formula step ms", "Structure step ms",
        };

        public static string Header => string.Join("\t", Columns);

        private readonly object _sync = new object();
        private readonly Dictionary<object, double> _formula = new Dictionary<object, double>();
        private readonly Dictionary<object, double> _structure = new Dictionary<object, double>();

        /// <summary>Adds the formula-step time of a query (keyed by its query object).</summary>
        public void AddFormulaStep(object query, TimeSpan elapsed) => Add(_formula, query, elapsed);

        /// <summary>Adds the structure-step time of a query (keyed by its query object).</summary>
        public void AddStructureStep(object query, TimeSpan elapsed) => Add(_structure, query, elapsed);

        public double? FormulaStepMs(object query) => Get(_formula, query);

        public double? StructureStepMs(object query) => Get(_structure, query);

        private void Add(Dictionary<object, double> map, object query, TimeSpan elapsed) {
            if (query == null) {
                return;
            }
            lock (_sync) {
                map.TryGetValue(query, out var ms);
                map[query] = ms + Milliseconds(elapsed);
            }
        }

        private double? Get(Dictionary<object, double> map, object query) {
            if (query == null) {
                return null;
            }
            lock (_sync) {
                return map.TryGetValue(query, out var ms) ? ms : (double?)null;
            }
        }

        /// <summary>A time span in milliseconds.</summary>
        public static double Milliseconds(TimeSpan elapsed) => elapsed.Ticks / (double)TimeSpan.TicksPerMillisecond;

        /// <summary>The TSV line of a row (no line break).</summary>
        public static string Line(DiagnosticTimingRow row) {
            var cells = new[]
            {
                DiagnosticRuleExport.Cell(row.FileName),
                Int(row.Ms2Peaks),
                row.RulesEvaluated.HasValue ? Int(row.RulesEvaluated.Value) : string.Empty,
                row.HypothesesTested.HasValue ? Int(row.HypothesesTested.Value) : string.Empty,
                row.Outcomes.HasValue ? Int(row.Outcomes.Value) : string.Empty,
                row.Candidates.HasValue ? Int(row.Candidates.Value) : string.Empty,
                row.UniqueSmiles.HasValue ? Int(row.UniqueSmiles.Value) : string.Empty,
                row.CacheHits.HasValue ? row.CacheHits.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                Ms(row.EvalMs),
                Ms(row.MatchMs),
                Ms(row.FormulaStepMs),
                Ms(row.StructureStepMs),
            };
            return string.Join("\t", cells);
        }

        /// <summary>Writes the header and the rows, in the order given.</summary>
        public static void Write(TextWriter writer, IEnumerable<DiagnosticTimingRow> rows) {
            writer.WriteLine(Header);
            foreach (var row in rows) {
                writer.WriteLine(Line(row));
            }
        }

        private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Ms(double? value) => value.HasValue ? ExactDecimal.FormatOrNull(value.Value, MsDecimals) ?? string.Empty : string.Empty;
    }
}
