using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parser;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace StructureFinderConsoleApp {

    public class CASMIquery {
        public int ID { get; set;}
        public string Name { get; set;}
        public double RT { get; set; }
        public double MZ { get; set; }
        public AdductIon AdductObj { get; set; }
    }

    public class CfmQuery {
        public string SMILES { get; set; }
        public double Mass { get; set; }
    }

    public class MetFragQuery {
        public string InChI { get; set; }
        public string Identifier { get; set; }
        public string InChIKey2 { get; set; }
        public string InChIKey1 { get; set; }
        public Formula MolecularFormula { get; set; }
        public double MonoisotopicMass { get; set; }
    }
    public sealed class Code4CASMI {
        private Code4CASMI() { }    

        public static List<CASMIquery> GetCASMIQueries(string input) {
            var queries = new List<CASMIquery>();
            using (var sr = new StreamReader(input)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('\t');
                    var query = new CASMIquery() {
                        ID = int.Parse(linearray[0]), Name = linearray[1], RT = double.Parse(linearray[2]), MZ = double.Parse(linearray[3]), AdductObj = AdductIon.GetAdductIon(linearray[5])
                    };
                    queries.Add(query);
                }
            }
            return queries;
        }

        public static void SeparatedLibraryCreator4Cfmid(string queryfile, string reffile, string outputfolder) {
            var queries = GetCASMIQueries(queryfile);
            var cfmqueries = new List<CfmQuery>();
            using (var sr = new StreamReader(reffile)) {
                var counter = 0;
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split(' ');

                    var smiles = linearray[1];
                    var structure = MoleculeConverter.SmilesToStructure(smiles, out string error);
                    if (structure == null) continue;
                    cfmqueries.Add(new CfmQuery() { SMILES = smiles, Mass = structure.ExactMass });

                    if (counter % 1000 == 0) Console.WriteLine(counter); 

                    counter++;
                }
            }
            cfmqueries = cfmqueries.OrderBy(n => n.Mass).ToList();  

            var tolerance = 0.01;
            var adductobj = new AdductIon();
            foreach (var query in queries) {
                var outputfile = Path.Combine(outputfolder, query.ID + ".txt");
                var queryMass = query.AdductObj.ConvertToExactMass(query.MZ);

                var cCfmQuery = new List<CfmQuery>();
                foreach (var cQuery in cfmqueries) {
                    if (cQuery.Mass < queryMass - tolerance) continue;
                    if (cQuery.Mass > queryMass + tolerance) break;

                    cCfmQuery.Add(cQuery);
                }

                using (var sw = new StreamWriter(outputfile)) {
                    var counter = 1;
                    foreach (var mQuery in cCfmQuery) {
                        sw.WriteLine(counter + " " +  mQuery.SMILES);
                        counter++;
                    }
                }
            }
        }

        public static void SeparatedLibraryCreator4Metfrag(string queryfile, string reffile, string outputfolder) {
            var queries = GetCASMIQueries(queryfile);
            var metfragQueries = new List<MetFragQuery>();
            using (var sr = new StreamReader(reffile)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('|');
                    var query = new MetFragQuery() {
                        MonoisotopicMass = double.Parse(linearray[0]), InChI = linearray[1], Identifier = linearray[2], InChIKey2 = linearray[3], InChIKey1 = linearray[4],
                        MolecularFormula = FormulaStringParcer.Convert2FormulaObjV2(linearray[5])
                    };
                    metfragQueries.Add(query);
                }
            }

            var tolerance = 0.01;
            var adductobj = new AdductIon();
            foreach (var query in queries) {
                var outputfile = Path.Combine(outputfolder, query.ID + ".txt");
                var queryMass = query.AdductObj.ConvertToExactMass(query.MZ); 

                var cMetFragQueries = new List<MetFragQuery>();
                foreach (var metfragQuery in metfragQueries) {
                    if (metfragQuery.MonoisotopicMass < queryMass - tolerance) continue;
                    if (metfragQuery.MonoisotopicMass > queryMass + tolerance) break;

                    cMetFragQueries.Add(metfragQuery);
                }

                using (var sw = new StreamWriter(outputfile)) {
                    var header = new List<string>() { "MonoisotopicMass", "InChI", "Identifier", "InChIKey2", "InChIKey1", "MolecularFormula" };
                    sw.WriteLine(String.Join("|", header.ToArray()));
                    foreach (var mQuery in cMetFragQueries) {
                        var fields = new List<string>() { mQuery.MonoisotopicMass.ToString(), mQuery.InChI, mQuery.Identifier, mQuery.InChIKey2, mQuery.InChIKey1, mQuery.MolecularFormula.ToString() };
                        sw.WriteLine(String.Join("|", fields.ToArray()));
                    }
                }
            }
        }
    }
}
