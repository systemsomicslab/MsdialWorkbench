using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public sealed class ProteinGroupExporter
    {
        private static readonly string[] HEADER = new[]
        {
            "Protein group ID",
            "Protein ID",
            "Protein name",
            "Protein description",
            "Coverage",
            "Score",
            "Peptide count",
            "Unique peptide count"
        };

        public void Export(Stream stream, ProteinResultContainer proteinResultContainer, IEnumerable<AnalysisFileBean> files) {
            var header = new List<string>(HEADER);
            header.AddRange(files.Select(file => file.AnalysisFileName));
            using (var sw = new StreamWriter(stream, Encoding.ASCII)) {
                sw.WriteLine(string.Join("\t", header));
                foreach (var group in proteinResultContainer.ProteinGroups) {
                    foreach (var protein in group.ProteinMsResults) {
                        var values = new List<string>() {
                            group.GroupID.ToString(),
                            protein.FastaProperty.UniqueIdentifier,
                            protein.FastaProperty.ProteinName,
                            protein.FastaProperty.Description,
                            protein.PeptideCoverage.ToString(),
                            protein.Score.ToString(),
                            protein.MatchedPeptideResults.Count.ToString(),
                            protein.UniquePeptides.Count.ToString()
                        };
                        values.AddRange(protein.PeakHeights.Select(h => h.ToString()));
                        sw.WriteLine(string.Join("\t", values));
                    }
                }
            }
        }
    }
}
