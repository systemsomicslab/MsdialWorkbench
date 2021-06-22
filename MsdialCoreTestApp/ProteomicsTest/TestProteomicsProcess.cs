using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.Common.Proteomics.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.ProteomicsTest {
    public class TestProteomicsProcess {

        public List<Modification> Modifications;
        public List<Enzyme> Enzymes;

        public TestProteomicsProcess() {
            var mParser = new ModificationsXmlRefParser();
            mParser.Read();

            var eParser = new EnzymesXmlRefParser();
            eParser.Read();
            this.Modifications = mParser.Modifications;
            this.Enzymes = eParser.Enzymes;
        }

        public void ProcessTest() {
            var modList = new List<string>() { "Carbamidomethyl (C)" };
            var enzymeList = new List<string>() { "Trypsin" };
            var fasta_file = @"E:\6_Projects\PROJECT_Proteomics\test\fasta_alox15.txt";
            var adduct = AdductIonParser.GetAdductIonBean("[M+H]+");

            var cleavageSites = ProteinDigestion.GetCleavageSites(this.Enzymes, enzymeList);
            var modContainer = ModificationUtility.GetModificationContainer(this.Modifications, modList);
            var fastaQueries = FastaParser.ReadFastaUniProtKB(fasta_file);

            foreach (var fQuery in fastaQueries) {
                if (fQuery.IsValidated) {
                    var sequence = fQuery.Sequence;
                    var digestedPeptides = ProteinDigestion.GetDigestedPeptideSequences(sequence, cleavageSites, fQuery.UniqueIdentifier, fQuery.Index);

                    Console.WriteLine(fQuery.Header);

                    if (!digestedPeptides.IsEmptyOrNull()) {

                        var mPeptides = ModificationUtility.GetModifiedPeptides(digestedPeptides, modContainer);
                        foreach (var peptide in mPeptides.OrderByDescending(n => n.ExactMass())) { 
                            var refSpec = SequenceToSpec.Convert2SpecObj(peptide, adduct, Common.Enum.CollisionType.HCD);
                            Console.WriteLine("Mass {0}, Position {1}, Sequence {2}", refSpec.PrecursorMz, (peptide.Position.Start + 1).ToString() + "-" + (peptide.Position.End + 1).ToString(), peptide.ModifiedSequence);
                            
                            foreach(var peak in refSpec.Spectrum) {
                                Console.WriteLine("Mass {0}, Intensity {1}, Comment {2}", peak.Mass, peak.Intensity, peak.Comment);
                            }
                        }
                    }
                }
            }
        }
    }
}
