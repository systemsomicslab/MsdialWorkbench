using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Proteomics.Function {
    
    public sealed class ProteinDigestion {
        private ProteinDigestion() { }

        public static List<Peptide> GetDigestedPeptideSequences(string proteinSequence, List<string> cleavagesites, Dictionary<char, AminoAcid> char2AA, string database = "", int databaseID = -1) {
            var proteinSeqObj = PeptideCalc.Sequence2AminoAcids(proteinSequence, char2AA);
            return GetDigestedPeptideSequences(proteinSeqObj, cleavagesites, database, databaseID);
        }


        public static List<Peptide> GetDigestedPeptideSequences(List<AminoAcid> proteinSeqObj, List<string> cleavagesites, string database = "", int databaseID = -1) {
            var peptides = new List<Peptide>();
            var twoletterAAs = string.Empty;
            var start = 0;
            var end = 0;

            for (int i = 0; i < proteinSeqObj.Count - 1; i++) {
                twoletterAAs = proteinSeqObj[i].OneLetter.ToString() + proteinSeqObj[i + 1].OneLetter.ToString();

                if (cleavagesites.Contains(twoletterAAs)) {
                    end = i;
                    var peptide = new Peptide() { DatabaseOrigin = database, DatabaseOriginID = databaseID, SequenceObj = proteinSeqObj.GetRange(start, end - start + 1), Position = new CompMs.Common.DataObj.Range(start, end) };
                    peptide.IsProteinNterminal = start == 0 ? true : false;
                    peptide.IsProteinCterminal = end == proteinSeqObj.Count - 1 ? true : false;
                    peptides.Add(peptide);

                    start = i + 1;
                    end = i + 1;
                }
            }

            return peptides;
        }

        public static List<Peptide> GetDigestedPeptideSequences(string proteinSequence, List<string> cleavagesites, Dictionary<char, AminoAcid> char2AA,
            int maxMissedCleavage = 2, string database = "", int databaseID = -1, int minimumPeptideLength = 7) {
            var peptideSequence = string.Empty;
            var twoletterAAs = string.Empty;
            var start = 0;
            var end = 0;
            var proteinSeqObj = PeptideCalc.Sequence2AminoAcids(proteinSequence, char2AA);
            var peptides = GetDigestedPeptideSequences(proteinSeqObj, cleavagesites, database, databaseID);
            var currentTotalCount = peptides.Count;

            for (int i = 1; i <= maxMissedCleavage; i++) {
                for (int j = 0; j < currentTotalCount - i; j++) {
                    start = peptides[j].Position.Start;
                    end = peptides[j + i].Position.End;

                    var peptide = new Peptide() { DatabaseOrigin = database, DatabaseOriginID = databaseID, SequenceObj = proteinSeqObj.GetRange(start, end - start + 1), Position = new CompMs.Common.DataObj.Range(start, end) };
                    peptide.IsProteinNterminal = start == 0 ? true : false;
                    peptide.IsProteinCterminal = end == proteinSequence.Length - 1 ? true : false;
                    peptide.MissedCleavages = i;

                    if (peptide.SequenceObj.Count >= minimumPeptideLength)
                        peptides.Add(peptide);
                }
            }
            if (peptides.IsEmptyOrNull()) return null;
            return peptides.Where(n => n.SequenceObj.Count >= minimumPeptideLength).ToList();
        }

        public static List<string> GetCleavageSites(List<string> selectedEnzymes) {
            var eParser = new EnzymesXmlRefParser();
            eParser.Read();

            var enzymes = eParser.Enzymes;
            return GetCleavageSites(enzymes, selectedEnzymes);
        }

        public static List<string> GetCleavageSites(List<Enzyme> enzymes, List<string> selectedEnzymes) {
            var sites = new List<string>();
            var sEnzymes = new List<Enzyme>();
            foreach (var enzymeString in selectedEnzymes) {
                foreach (var enzymeObj in enzymes) {
                    if (enzymeString == enzymeObj.Title) {
                        sEnzymes.Add(enzymeObj);
                        break;
                    }
                }
            }
                 
            foreach (var enzyme in sEnzymes) {
                foreach (var site in enzyme.SpecificityList) {
                    sites.Add(site);
                }
            }

            return sites;
        }

        public static List<string> GetCleavageSites(List<Enzyme> enzymes) {
            var sites = new List<string>();
            var sEnzymes = enzymes.Where(n => n.IsSelected).ToList();

            foreach (var enzyme in sEnzymes) {
                foreach (var site in enzyme.SpecificityList) {
                    sites.Add(site);
                }
            }

            return sites;
        }
    }
}
