using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
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
            var variableMods = new List<string> { "Acetyl (Protein N-term)", "Oxidation (M)", "4HNE (CHK)" };

            var enzymeList = new List<string>() { "Trypsin/P" };
            var fasta_file = @"E:\6_Projects\PROJECT_Proteomics\test\human_liver.fasta";
            var adduct = AdductIon.GetAdductIon("[M+H]+");

            var cleavageSites = ProteinDigestion.GetCleavageSites(this.Enzymes, enzymeList);
            var modContainer = ModificationUtility.GetModificationContainer(this.FixedModifications, this.VariableModifications, fixedMods, variableMods);
            var fastaQueries = FastaParser.ReadFastaUniProtKB(fasta_file);
            var char2AA = PeptideCalc.GetSimpleChar2AminoAcidDictionary();
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
            Parallel.ForEach(fastaQueries, fQuery => {
                if (fQuery.IsValidated) {
                    var sequence = fQuery.Sequence;
                    var digestedPeptides = ProteinDigestion.GetDigestedPeptideSequences(sequence, cleavageSites, char2AA, maxMissedCleavage, fQuery.UniqueIdentifier, fQuery.Index);
                    if (!digestedPeptides.IsEmptyOrNull()) {
                        var mPeptides = ModificationUtility.GetFastModifiedPeptides(digestedPeptides, modContainer, maxNumberOfModificationsPerPeptide);
                        lock (syncObj) {
                            foreach (var peptide in mPeptides.OrderByDescending(n => n.ExactMass)) {
                                var mass = peptide.ExactMass;
                                var precursorMz = adduct.ConvertToMz(mass);
                                //Console.WriteLine(mass + "\t" + peptide.ModifiedSequence);
                                //var refSpec = SequenceToSpec.Convert2SpecObj(peptide, adduct, Common.Enum.CollisionType.HCD);
                                //refSpecs.Add(refSpec);
                                queries.Add(peptide);
                            }
                            counter++;
                            //Console.WriteLine("Creation finished: {0} / {1} for total {2} peptides", counter, queryCount, queries.Count);
                        }
                    }
                }
            });










            //foreach (var fQuery in fastaQueries) {
            //    counter++;
            //    if (fQuery.IsValidated) {
            //        var sequence = fQuery.Sequence;
            //        var digestedPeptides = ProteinDigestion.GetDigestedPeptideSequences(sequence, cleavageSites, maxMissedCleavage, fQuery.UniqueIdentifier, fQuery.Index);

            //        //Console.WriteLine(fQuery.Header);

            //        if (!digestedPeptides.IsEmptyOrNull()) {

            //            var mPeptides = ModificationUtility.GetModifiedPeptides(digestedPeptides, modContainer, maxNumberOfModificationsPerPeptide);
            //            foreach (var peptide in mPeptides.OrderByDescending(n => n.ExactMass)) {
            //                //var refSpec = SequenceToSpec.Convert2SpecObj(peptide, adduct, Common.Enum.CollisionType.HCD);
            //                //Console.WriteLine("Mass {0}, Position {1}, Sequence {2}", refSpec.PrecursorMz, (peptide.Position.Start + 1).ToString() + "-" + (peptide.Position.End + 1).ToString(), peptide.ModifiedSequence);
            //                queries.Add(peptide);
            //                counter++;
            //                Console.WriteLine("Creation finished: {0} / {1} for total {2} peptides", counter, queryCount, queries.Count);
            //                //foreach (var peak in refSpec.Spectrum) {
            //                //    Console.WriteLine("Mass {0}, Intensity {1}, Comment {2}", peak.Mass, peak.Intensity, peak.Comment);
            //                //}
            //            }
            //        }
            //    }
            //}
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);
            Console.ReadLine();
        }



        public void PDFTest() {
            var data = new double[][] {
                new double[] { 0 },
                new double[] { 1 },
                new double[] { 1 },
                new double[] { 2 },
                new double[] { 2 },
                new double[] { 2 },
                new double[] { 3 },
                new double[] { 3 },
                new double[] { 3 },
                new double[] { 3 },
                new double[] { 4 },
                new double[] { 4 },
                new double[] { 4 },
                new double[] { 4 },
                new double[] { 4 },
                new double[] { 5 },
                new double[] { 5 },
                new double[] { 5 },
                new double[] { 5 },
                new double[] { 5 },
                new double[] { 5 },
                new double[] { 6 },
                new double[] { 6 },
                new double[] { 6 },
                new double[] { 6 },
                new double[] { 6 },
                new double[] { 7 },
                new double[] { 7 },
                new double[] { 7 },
                new double[] { 7 },
                new double[] { 8 },
                new double[] { 8 },
                new double[] { 8 },
                new double[] { 9 },
                new double[] { 9 },
                new double[] { 10 }
            };

            var dist = PeptideAnnotation.GetGussianKernelDistribution(data);
            Console.WriteLine(String.Join(" ", dist.Mean));
            Console.WriteLine(String.Join(" ", dist.Median));
            Console.WriteLine(String.Join(" ", dist.Covariance));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 0 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 1 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 2 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 3 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 4 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 6 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 7 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 8 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 9 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 10 }));

            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 0.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 1.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 2.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 3.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 4.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 5.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 6.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 7.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 8.5 }));
            Console.WriteLine(dist.ProbabilityDensityFunction(new double[] { 9.5 }));

        }
    }
}
