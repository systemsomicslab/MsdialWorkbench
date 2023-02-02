using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StructureFinderConsoleApp
{
    public class StructureRecognitionQuery
    {
        private string smiles;
        private string inchikey;

        public string Smiles
        {
            get {
                return smiles;
            }

            set {
                smiles = value;
            }
        }

        public string Inchikey
        {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }
    }

    public sealed class StructureRecognitionTest
    {
        private StructureRecognitionTest() { }

        public static void ParcerTest(string inputfile, string outputfile)
        {
            var queries = ReadQueries(inputfile);

            using (var sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                var counter = 0;
                foreach (var query in queries) {
                    var errorString = string.Empty;
                    var smies = query.Smiles;
                    var inchikey = query.Inchikey;
                    var structure = MoleculeConverter.SmilesToStructure(smies, out errorString);

                    if (errorString != string.Empty) {
                        sw.WriteLine(query.Inchikey + "\t" + "False" + "\t" + errorString);
                    }
                    else {
                        var descriptor = structure.MolecularDescriptor;
                        var infoArray = descriptor.GetType().GetProperties();
                        var flg = false;
                        foreach (var info in infoArray) {
                            if ((int)info.GetValue(descriptor, null) == 1) {
                                if (info.Name == inchikey) {
                                    flg = true;
                                    break;
                                }
                            }
                        }

                        if (flg) sw.WriteLine(query.Inchikey + "\t" + "TRUE" + "\t" + "");
                        else sw.WriteLine(query.Inchikey + "\t" + "FALSE" + "\t" + "Recognition faled");
                    }

                    Console.WriteLine(counter);
                    counter++;
                }
            }
        }

        private static List<StructureRecognitionQuery> ReadQueries(string input)
        {
            var queries = new List<StructureRecognitionQuery>();
            using (var sr = new StreamReader(input, Encoding.UTF8)) {

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;

                    var lineArray = line.Split('\t');
                    var query = new StructureRecognitionQuery() {
                        Inchikey = lineArray[0],
                        Smiles = lineArray[1]
                    };
                    queries.Add(query);
                }
            }
            return queries;
        }
        
    }
}
