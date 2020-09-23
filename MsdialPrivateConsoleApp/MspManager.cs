using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.Common.Extension;

namespace MsdialPrivateConsoleApp {
    public sealed class MspManager {
        private MspManager() {

        }

        public static void CurateOntologyField(string mspfile, string inchikeytable, string new_mspfile) {
            var queries = MspFileParser.MspFileReader(mspfile);
            var inchikey2ontology = new Dictionary<string, string>();
            using (var sr = new StreamReader(inchikeytable, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var linearray = line.Split('\t');
                    inchikey2ontology[linearray[0]] = linearray[2];
                }
            }

            using (var sw = new StreamWriter(new_mspfile, false, Encoding.ASCII)) {
                foreach (var key in queries) {

                    var inchikey = key.InChIKey;
                    if (inchikey.IsEmptyOrNull()) continue;
                    var shortinchikey = inchikey.Split('-')[0];
                    if (!inchikey2ontology.ContainsKey(shortinchikey)) continue;
                    if (key.AdductType.AdductIonName != "[M+H]+" && key.AdductType.AdductIonName != "[M-H]-") continue; // temp

                    sw.WriteLine(String.Join(": ", new string[] { "NAME", key.Name }));
                    sw.WriteLine(String.Join(": ", new string[] { "PRECURSORMZ", key.PrecursorMz.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "PRECURSORTYPE", key.AdductType.AdductIonName }));
                    sw.WriteLine(String.Join(": ", new string[] { "IONMODE", key.IonMode.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "FORMULA", key.Formula.FormulaString }));
                    sw.WriteLine(String.Join(": ", new string[] { "ONTOLOGY", inchikey2ontology[shortinchikey] }));
                    sw.WriteLine(String.Join(": ", new string[] { "INCHIKEY", key.InChIKey }));
                    sw.WriteLine(String.Join(": ", new string[] { "SMILES", key.SMILES }));
                    sw.WriteLine(String.Join(": ", new string[] { "RETENTIONTIME", key.ChromXs.RT.Value.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "CCS", key.CollisionCrossSection.ToString() }));
                    sw.WriteLine(String.Join(": ", new string[] { "COMMENT", key.Comment }));

                    var peaks = key.Spectrum;
                    //spectrum num
                    var numPeaks = peaks.Count.ToString();
                    sw.WriteLine(String.Join(": ", new string[] { "Num Peaks", numPeaks }));
                    //spectrum list
                    foreach (var peak in peaks) {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                    };
                    sw.WriteLine();
                }
            }
        }
    }
}
