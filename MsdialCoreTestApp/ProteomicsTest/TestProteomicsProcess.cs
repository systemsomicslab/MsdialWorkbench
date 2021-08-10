using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.Common.Proteomics.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.ProteomicsTest {
    public class TestProteomicsProcess {

        public List<Modification> FixedModifications;
        public List<Modification> VariableModifications;
        public List<Enzyme> Enzymes;

        public TestProteomicsProcess() {
            var fParser = new ModificationsXmlRefParser();
            fParser.Read();
            this.FixedModifications = fParser.Modifications;

            var vParser = new ModificationsXmlRefParser();
            vParser.Read();
            this.VariableModifications = vParser.Modifications;

            var eParser = new EnzymesXmlRefParser();
            eParser.Read();
            this.Enzymes = eParser.Enzymes;
        }

        public void ProcessTest() {
            var fixedMods = new List<string>() { "Carbamidomethyl (C)" };
            var variableMods = new List<string> { "Acetyl (Protein N-term)", "Oxidation (M)" };

            var enzymeList = new List<string>() { "Trypsin/P" };
            var fasta_file = @"E:\6_Projects\PROJECT_Proteomics\jPOST_files_JPST000200.0\human_proteins_ref.fasta";
            var adduct = AdductIonParser.GetAdductIonBean("[M+H]+");

            var cleavageSites = ProteinDigestion.GetCleavageSites(this.Enzymes, enzymeList);
            var modContainer = ModificationUtility.GetModificationContainer(this.FixedModifications, this.VariableModifications, fixedMods, variableMods);
            var fastaQueries = FastaParser.ReadFastaUniProtKB(fasta_file);

            var maxMissedCleavage = 2;
            var maxNumberOfModificationsPerPeptide = 5;
            var queries = new List<Peptide>();
            var refSpecs = new List<MoleculeMsReference>();

            var sw = new Stopwatch();
            sw.Start();
            var counter = 0;


            var syncObj = new object();
            var queryCount = fastaQueries.Count;
            var error = string.Empty;

            //Parallel.ForEach(fastaQueries, fQuery => {
            //    if (fQuery.IsValidated) {
            //        var sequence = fQuery.Sequence;
            //        var digestedPeptides = ProteinDigestion.GetDigestedPeptideSequences(sequence, cleavageSites, maxMissedCleavage, fQuery.UniqueIdentifier, fQuery.Index);
            //        if (!digestedPeptides.IsEmptyOrNull()) {
            //            var mPeptides = ModificationUtility.GetModifiedPeptides(digestedPeptides, modContainer, maxNumberOfModificationsPerPeptide);
            //            lock (syncObj) {
            //                foreach (var peptide in mPeptides.OrderByDescending(n => n.ExactMass)) {
            //                    //var refSpec = SequenceToSpec.Convert2SpecObj(peptide, adduct, Common.Enum.CollisionType.HCD);
            //                    //refSpecs.Add(refSpec);
            //                    queries.Add(peptide);
            //                }
            //                counter++;
            //                Console.WriteLine("Creation finished: {0} / {1} for total {2} peptides", counter, queryCount, queries.Count);
            //            }
            //        }
            //    }
            //});










            foreach (var fQuery in fastaQueries) {
                counter++;
                if (fQuery.IsValidated) {
                    var sequence = fQuery.Sequence;
                    var digestedPeptides = ProteinDigestion.GetDigestedPeptideSequences(sequence, cleavageSites, maxMissedCleavage, fQuery.UniqueIdentifier, fQuery.Index);

                    //Console.WriteLine(fQuery.Header);

                    if (!digestedPeptides.IsEmptyOrNull()) {

                        var mPeptides = ModificationUtility.GetModifiedPeptides(digestedPeptides, modContainer, maxNumberOfModificationsPerPeptide);
                        foreach (var peptide in mPeptides.OrderByDescending(n => n.ExactMass)) {
                            //var refSpec = SequenceToSpec.Convert2SpecObj(peptide, adduct, Common.Enum.CollisionType.HCD);
                            //Console.WriteLine("Mass {0}, Position {1}, Sequence {2}", refSpec.PrecursorMz, (peptide.Position.Start + 1).ToString() + "-" + (peptide.Position.End + 1).ToString(), peptide.ModifiedSequence);
                            queries.Add(peptide);
                            counter++;
                            Console.WriteLine("Creation finished: {0} / {1} for total {2} peptides", counter, queryCount, queries.Count);
                            //foreach (var peak in refSpec.Spectrum) {
                            //    Console.WriteLine("Mass {0}, Intensity {1}, Comment {2}", peak.Mass, peak.Intensity, peak.Comment);
                            //}
                        }
                    }
                }
            }
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadLine();
        }
    }
}
