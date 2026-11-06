using CompMs.Common.Parser;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsfinderCommon.Export
{
    /// <summary>
    /// Writes the MassBank records of the queries of an MS-FINDER project, one per query, with
    /// <see cref="MsfinderMassBankRecord"/>: the query's compound and spectrum, and the product ion formulas MS-FINDER
    /// assigned as PK$ANNOTATION.
    /// </summary>
    public static class MsfinderMassBankExport
    {
        /// <summary>
        /// Writes the record of each query into <paramref name="destination"/>, named after its accession. A query that
        /// cannot become a valid record leaves no file and is counted with its reason.
        /// </summary>
        /// <param name="files">The queries of the project, in its order; a query's index is its record number.</param>
        /// <param name="msfinderVersion">The MS-FINDER version MS$DATA_PROCESSING: WHOLE names, e.g. "3.73".</param>
        /// <param name="splashCalculator">PK$SPLASH of the printed peaks; MassBankSplash.Compute.</param>
        /// <exception cref="InvalidOperationException">The settings would not make valid records.</exception>
        public static MsfinderMassBankExportSummary Export(IReadOnlyList<MsfinderQueryFile> files, AnalysisParamOfMsfinder param, MsfinderMassBankExportSettings settings, string msfinderVersion, Func<string, string> splashCalculator, string destination) {
            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }
            if (param is null) {
                throw new ArgumentNullException(nameof(param));
            }
            if (settings is null) {
                throw new ArgumentNullException(nameof(settings));
            }
            var handler = settings.CreateHandler(msfinderVersion, splashCalculator);
            var problems = handler.ValidateMetadata();
            if (problems.Count > 0) {
                throw new InvalidOperationException("The MassBank metadata is not valid: " + string.Join(" ", problems));
            }

            var summary = new MsfinderMassBankExportSummary();
            var encoding = new UTF8Encoding(false);
            Directory.CreateDirectory(destination);
            for (int i = 0; i < files.Count; i++) {
                var file = files[i];
                MassBankRecordResult result;
                try {
                    var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);
                    var spectrum = ExportedSpectrum(rawData, param.RelativeAbundanceCutOff);
                    var candidates = FormulaCandidates(file.FormulaFilePath);
                    result = MsfinderMassBankRecord.Build(handler, rawData, spectrum, candidates, i);
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException)) {
                    // One unreadable query (the readers throw what the text gives them) must not stop the export of the
                    // others; the exception type groups such queries in the summary.
                    summary.AddSkipped($"the query file could not be read ({ex.GetType().Name})");
                    continue;
                }
                if (!result.IsSuccess) {
                    summary.AddSkipped($"refused: {result.SkipReason}");
                    continue;
                }
                var path = Path.Combine(destination, result.FileName);
                File.WriteAllText(path, result.Text, encoding);
                summary.AddWritten(result, path);
            }
            summary.CountOtherRecords(destination, handler.Identifier);
            return summary;
        }

        /// <summary>
        /// The MS/MS peaks a record of <paramref name="rawData"/> lists: the centroided spectrum, as MS-FINDER analyses it,
        /// without the peaks below <paramref name="relativeAbundanceCutOff"/> percent of the base peak.
        /// </summary>
        public static List<Peak> ExportedSpectrum(RawData rawData, double relativeAbundanceCutOff) {
            var spectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData) ?? new List<Peak>();
            var maxIntensity = spectrum.Count > 0 ? spectrum.Max(p => p.Intensity) : 0d;
            if (!(maxIntensity > 0d)) {
                return new List<Peak>();
            }
            return spectrum.Where(p => p.Intensity / maxIntensity * 100d >= relativeAbundanceCutOff).ToList();
        }

        /// <summary>MS-FINDER's formula candidates for a query, best total score first; empty when it has none.</summary>
        public static List<MsfinderFormulaCandidate> FormulaCandidates(string formulaFilePath) {
            if (string.IsNullOrEmpty(formulaFilePath) || !File.Exists(formulaFilePath)) {
                return new List<MsfinderFormulaCandidate>();
            }
            var results = FormulaResultParcer.FormulaResultReader(formulaFilePath, out _) ?? new List<FormulaResult>();
            return results
                .Where(r => r?.Formula != null)
                .OrderByDescending(r => r.TotalScore)
                .Select(r => new MsfinderFormulaCandidate(r.Formula.FormulaString, r.ProductIonResult))
                .ToList();
        }
    }
}
