using Msdial.Lcms.Dataprocess.Algorithm.Clustering;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsdialPrivateConsoleApp {

    public class CircosPeak {
        public string Title { get; set; }
        public string MetaboliteClass { get; set; }
        public double Mz { get; set; }
        public double Rt { get; set; }
        public int Segment { get; set; }
        public int StartPosition { get; set; }
        public int TargetPosition { get; set; }
        public int EndPosition { get; set; }
        public double MaxValue { get; set; }
        public List<Peak> Peaks { get; set; } = new List<Peak>();
        public List<double> Values { get; set; } = new List<double>();
        public Dictionary<string, double> SampleToValue { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> SpeciesToValue { get; set; } = new Dictionary<string, double>();

        // metadata
        public string Formula { get; set; }
        public string SMILES { get; set; }
        public string InChIKey { get; set; }
        public string IsLA { get; set; }
        public string IsAA { get; set; }
        public string IsDTA { get; set; }
        public string IsDHA { get; set; }
        public string IsEPA { get; set; }
        public string IsDPA { get; set; }
        public string IsPUFA { get; set; }
    }

    public sealed class CircosUtility {
        private CircosUtility() { }

        public static void GenerateCircosFilesVs2(string input, int sampleSize, string outputfolder) {
            var lipids = new List<string>() {
                "FA",
                "ACar",
                "CE",
                "MAG",
                "DAG",
                "TAG",
                "LPC",
                "LPE",
                "LPG",
                "LPI",
                "LPS",
                "PA",
                "PC",
                "PE",
                "PG",
                "PI",
                "PS",
                "BMP",
                "MGDG",
                "EtherPC",
                "EtherPE",
                "OxPC",
                "OxPE",
                "OxPI",
                "OxPS",
                "SM",
                "Cer-NS",
                "Cer-NDS",
                "Cer-AP",
                "Cer-NP",
                "Cer-AS",
                "Cer-ADS",
                "HexCer-NS",
                "HexCer-NDS",
                "HexCer-AP",
                "SHexCer"
            };
            var lipidToSegment = new Dictionary<string, int>();
            var lipidToPosition = new Dictionary<string, int> {
                { "FA",1 },
                { "ACar",1 },
                { "CE",1 },
                { "MAG",1 },
                { "DAG",1 },
                { "TAG",1 },
                { "LPC",1 },
                { "LPE",1 },
                { "LPG",1 },
                { "LPI",1 },
                { "LPS",1 },
                { "PA",1 },
                { "PC",1 },
                { "PE",1 },
                { "PG",1 },
                { "PI",1 },
                { "PS",1 },
                { "BMP",1 },
                { "MGDG",1 },
                { "EtherPC",1 },
                { "EtherPE",1 },
                { "OxPC",1 },
                { "OxPE",1 },
                { "OxPI",1 },
                { "OxPS",1 },
                { "SM",1 },
                { "Cer-NS",1 },
                { "Cer-NDS",1 },
                { "Cer-AP",1 },
                { "Cer-NP",1 },
                { "Cer-AS",1 },
                { "Cer-ADS",1 },
                { "HexCer-NS",1 },
                { "HexCer-NDS",1 },
                { "HexCer-AP",1 },
                { "SHexCer",1 }
            };
            var circosPeaks = new List<CircosPeak>();
            var filenames = new List<string>();

            var includedLipidClass = new List<string>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                var classes = sr.ReadLine().Split('\t');
                var header = sr.ReadLine(); // must from [21] Ctr, AA, EPA, DHA (n = 4 in liver, n = 5 in others)
                var headerArray = header.Split('\t');
                for (int i = 21; i < 21 + sampleSize; i++) filenames.Add(headerArray[i]);
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var lineArray = line.Split('\t');
                    var superClass = lineArray[0];
                    var lipidclass = lineArray[1];
                    var totalchain = lineArray[2];
                    var sn1Chain = lineArray[3];
                    var sn2Chain = lineArray[4];
                    var sn3Chain = lineArray[5];
                    var sn4Chain = lineArray[6];
                    var title = string.Empty;
                    if (sn2Chain == string.Empty) title = lipidclass + " " + totalchain;
                    else if (sn3Chain == string.Empty) title = lipidclass + " " + sn1Chain + "-" + sn2Chain;
                    else title = lipidclass + " " + sn1Chain + "-" + sn2Chain + "-" + sn3Chain;


                    var formula = lineArray[7];
                    var inchikey = lineArray[8];
                    var smiles = lineArray[9];
                    var rt = double.Parse(lineArray[10]);
                    var mz = double.Parse(lineArray[11]);
                    var average = Math.Log(double.Parse(lineArray[13]), 2);
                    var isAA = lineArray[17];
                    var isEPA = lineArray[18];
                    var isDHA = lineArray[19];
                    var isPUFA = lineArray[20];
                    var isDTA = isFattyAcidProp(sn1Chain, sn2Chain, sn3Chain, sn4Chain, "22:4");
                    var isDPA = isFattyAcidProp(sn1Chain, sn2Chain, sn3Chain, sn4Chain, "22:5");
                    var isLA = isFattyAcidProp(sn1Chain, sn2Chain, sn3Chain, sn4Chain, "18:2");

                    //Console.WriteLine(sn1Chain + "-" + sn2Chain + " IsDTA=" + isDTA + " IsDPA=" + isDPA);

                    var values = new List<double>();
                    for (int i = 21; i < 21 + sampleSize; i++) {
                        values.Add(double.Parse(lineArray[i]));
                    }

                    if (!includedLipidClass.Contains(lipidclass))
                        includedLipidClass.Add(lipidclass);

                    var circusPeak = new CircosPeak() {
                        Title = title,
                        Formula = formula,
                        InChIKey = inchikey,
                        IsAA = isAA,
                        IsDHA = isDHA,
                        IsEPA = isEPA,
                        IsPUFA = isPUFA,
                        IsDPA = isDPA.ToString(),
                        IsDTA = isDTA.ToString(),
                        IsLA = isLA.ToString(),
                        MetaboliteClass = lipidclass,
                        Mz = mz,
                        Rt = rt,
                        SMILES = smiles,
                        Values = values,
                        MaxValue = values.Max()
                       
                    };
                    circosPeaks.Add(circusPeak);
                }
            }

            circosPeaks = circosPeaks.OrderByDescending(n => n.MaxValue).ToList();
            var cCircosPeaks = new List<CircosPeak>();
            foreach (var peak in circosPeaks) {
                if (cCircosPeaks.Count > 220 && peak.MetaboliteClass != "BMP") {
                    continue;
                }
                cCircosPeaks.Add(peak);
            }

            //var counter = 1;
            //for (int i = 0; i < lipids.Count; i++) {
            //    if (includedLipidClass.Contains(lipids[i])) {
            //        lipidToSegment[lipids[i]] = counter;
            //        counter++;
            //    }
            //}


            // ordering lipid peaks
            var orderedCircosPeaks = new List<CircosPeak>();
            var segment = 1;
            var position = 1;
            foreach (var lipid in lipids) {
                var lCircosPeaks = new List<CircosPeak>();
                foreach (var peak in cCircosPeaks) {
                    if (peak.MetaboliteClass == lipid) {
                        lCircosPeaks.Add(peak);
                    }
                }
               // if (lCircosPeaks.Count < 2) continue;
                if (lipid != "BMP" && lCircosPeaks.Count < 2) continue;
                if (lipid == "BMP" && lCircosPeaks.Count == 0) continue;
                lCircosPeaks = lCircosPeaks.OrderByDescending(n => n.IsPUFA).ThenByDescending(n => n.IsLA).ThenByDescending(n => n.IsAA).ThenByDescending(n => n.IsDTA).
                    ThenByDescending(n => n.IsEPA).ThenByDescending(n => n.IsDPA).ThenByDescending(n => n.IsDHA).ToList();

                position = 1;
                foreach (var lPeak in lCircosPeaks) {
                    lPeak.Segment = segment;
                    lPeak.StartPosition = position - 1;
                    lPeak.EndPosition = position + 1;
                    lPeak.TargetPosition = position;
                    orderedCircosPeaks.Add(lPeak);
                    position += 2;
                }

                lipidToPosition[lipid] = (position + 1) / 2;

                segment++;
            }

            //var directory = System.IO.Path.GetDirectoryName(input);
            var directory = outputfolder;
            var filename = System.IO.Path.GetFileNameWithoutExtension(input);

            var segfile = directory + "\\" + "seg.txt";
            var mapfile_intensity = directory + "\\" + "map_intensity.txt";
            var mapfile_ispufa = directory + "\\" + "map_ispufa.txt";
            var mapfile_isla = directory + "\\" + "map_isla.txt";
            var mapfile_isaa = directory + "\\" + "map_isaa.txt";
            var mapfile_isdta = directory + "\\" + "map_isdta.txt";
            var mapfile_isepa = directory + "\\" + "map_isepa.txt";
            var mapfile_isdpa = directory + "\\" + "map_isdpa.txt";
            var mapfile_isdha = directory + "\\" + "map_isdha.txt";
            var mapnamefile = directory + "\\" + "mapname.txt";

            var other_link = directory + "\\" + "other-link.txt";
            var dha_link = directory + "\\" + "dha-link.txt";
            var epa_link = directory + "\\" + "epa-link.txt";
            var aa_link = directory + "\\" + "aa-link.txt";
            var la_link = directory + "\\" + "la-link.txt";

            var expfile = directory + "\\" + "exp.txt";
            var matrix = directory + "\\" + "matrix.csv";

            using (var sw = new StreamWriter(matrix, false, Encoding.ASCII)) {
                sw.WriteLine("Title," + String.Join(",", filenames));
                var counter = 1;
                foreach (var peak in orderedCircosPeaks) {
                    var textString = "X" + counter;
                    foreach (var intensity in peak.Values) {
                        textString += "," + intensity;
                    }
                    sw.WriteLine(textString);
                    counter++;
                }
            }

            using (var sw = new StreamWriter(segfile, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tSpos\tEpos\tTitle\tName");
                foreach (var peak in orderedCircosPeaks) {
                    sw.WriteLine(peak.Segment + "\t" + peak.StartPosition + "\t" + peak.EndPosition + "\t" + peak.Title + "\t" + peak.Title);
                }
            }

            using (var sw = new StreamWriter(mapfile_intensity, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + Math.Log(peak.Values.Max(), 2));
                }
            }

            using (var sw = new StreamWriter(mapfile_ispufa, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    if (peak.IsPUFA == "TRUE")
                        sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "1");
                    //else
                    //    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "0");
                }
            }

            using (var sw = new StreamWriter(mapfile_isaa, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    if (peak.IsAA == "TRUE")
                        sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "1");
                    //else
                    //    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "0");
                }
            }

            using (var sw = new StreamWriter(mapfile_isdta, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    if (peak.IsDTA.ToUpper() == "TRUE")
                        sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "1");
                    //else
                    //    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "0");
                }
            }

            using (var sw = new StreamWriter(mapfile_isepa, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    if (peak.IsEPA == "TRUE")
                        sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "1");
                    //else
                    //    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "0");
                }
            }

            using (var sw = new StreamWriter(mapfile_isdpa, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    if (peak.IsDPA.ToUpper() == "TRUE")
                        sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "1");
                    //else
                    //    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "0");
                }
            }

            using (var sw = new StreamWriter(mapfile_isla, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    if (peak.IsLA.ToUpper() == "TRUE")
                        sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "1");
                    //else
                    //    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "0");
                }
            }


            using (var sw = new StreamWriter(mapfile_isdha, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in orderedCircosPeaks) {
                    if (peak.IsDHA == "TRUE")
                        sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "1");
                    //else
                    //    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + "0");
                }
            }

            using (var sw = new StreamWriter(mapnamefile, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                var counter = 1;
                var lastlipid = orderedCircosPeaks[0].MetaboliteClass;
                sw.WriteLine(counter + "\t" + lipidToPosition[lastlipid] + "\t" + lastlipid);
                counter++;

                foreach (var peak in orderedCircosPeaks) {
                    if (peak.MetaboliteClass != lastlipid) {
                        lastlipid = peak.MetaboliteClass;
                        sw.WriteLine(counter + "\t" + lipidToPosition[lastlipid] + "\t" + lastlipid);

                        counter++;
                    }
                }
                //for (int i = 0; i < lipids.Count; i++) {
                //    if (includedLipidClass.Contains(lipids[i])) {
                //        sw.WriteLine(counter + "\t" + "1" + "\t" + lipids[i]);
                //        counter++;
                //    }
                //}
            }

            using (var sw = new StreamWriter(expfile, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\t" + String.Join("\t", filenames));
                foreach (var peak in orderedCircosPeaks) {
                    var textString = peak.Segment + "\t" + peak.TargetPosition;
                    var minValue = peak.Values.Min();
                    var maxValue = peak.Values.Max();
                    foreach (var intensity in peak.Values) {
                        var sIntensity = (intensity - minValue) / (maxValue - minValue) * 2.0 - 1.0;
                        textString += "\t" + Math.Round(sIntensity, 5);
                    }
                    sw.WriteLine(textString);
                }
            }

            using (var sw = new StreamWriter(other_link, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tTpos1\tname1\tseg2\tTpos2\tname2");

                for (int i = 0; i < orderedCircosPeaks.Count; i++) {
                    var peak1 = orderedCircosPeaks[i];
                    for (int j = i + 1; j < orderedCircosPeaks.Count; j++) {
                        var peak2 = orderedCircosPeaks[j];
                        if (peak1.Segment == peak2.Segment) continue;
                        var correl = BasicMathematics.Coefficient(peak1.Values.ToArray(), peak2.Values.ToArray());
                        if (Math.Abs(correl) >= 0.9) {
                            sw.WriteLine(peak1.Segment + "\t" + peak1.TargetPosition + "\t" + "Other1" + "\t" + peak2.Segment + "\t" + peak2.TargetPosition + "\t" + "Other2");
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(la_link, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tTpos1\tname1\tseg2\tTpos2\tname2");
                for (int i = 0; i < orderedCircosPeaks.Count; i++) {
                    var peak1 = orderedCircosPeaks[i];
                    if (peak1.IsLA.ToUpper() == "TRUE") {
                        for (int j = i + 1; j < orderedCircosPeaks.Count; j++) {
                            var peak2 = orderedCircosPeaks[j];
                            if (peak2.IsLA.ToUpper() == "TRUE") {
                                if (peak1.Segment == peak2.Segment) continue;

                                var correl = BasicMathematics.Coefficient(peak1.Values.ToArray(), peak2.Values.ToArray());
                                if (Math.Abs(correl) >= 0.9) {
                                    sw.WriteLine(peak1.Segment + "\t" + peak1.TargetPosition + "\t" + "LA1" + "\t" + peak2.Segment + "\t" + peak2.TargetPosition + "\t" + "LA2");
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(dha_link, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tTpos1\tname1\tseg2\tTpos2\tname2");

                for (int i = 0; i < orderedCircosPeaks.Count; i++) {
                    var peak1 = orderedCircosPeaks[i];
                    if (peak1.IsDHA.ToUpper() == "TRUE") {
                        for (int j = i + 1; j < orderedCircosPeaks.Count; j++) {
                            var peak2 = orderedCircosPeaks[j];
                            if (peak2.IsDHA.ToUpper() == "TRUE") {
                                if (peak1.Segment == peak2.Segment) continue;

                                var correl = BasicMathematics.Coefficient(peak1.Values.ToArray(), peak2.Values.ToArray());
                                if (Math.Abs(correl) >= 0.9) {
                                    sw.WriteLine(peak1.Segment + "\t" + peak1.TargetPosition + "\t" + "DHA1" + "\t" + peak2.Segment + "\t" + peak2.TargetPosition + "\t" + "DHA2");
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(aa_link, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tTpos1\tname1\tseg2\tTpos2\tname2");

                for (int i = 0; i < orderedCircosPeaks.Count; i++) {
                    var peak1 = orderedCircosPeaks[i];
                    if (peak1.IsAA.ToUpper() == "TRUE" || peak1.IsDTA.ToUpper() == "TRUE") {
                        for (int j = i + 1; j < orderedCircosPeaks.Count; j++) {
                            var peak2 = orderedCircosPeaks[j];
                            if (peak1.Segment == peak2.Segment) continue;

                            if (peak2.IsAA.ToUpper() == "TRUE" || peak2.IsDTA.ToUpper() == "TRUE") {
                                var correl = BasicMathematics.Coefficient(peak1.Values.ToArray(), peak2.Values.ToArray());
                                if (Math.Abs(correl) >= 0.9) {
                                    sw.WriteLine(peak1.Segment + "\t" + peak1.TargetPosition + "\t" + "AA1" + "\t" + peak2.Segment + "\t" + peak2.TargetPosition + "\t" + "AA2");
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(epa_link, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tTpos1\tname1\tseg2\tTpos2\tname2");

                for (int i = 0; i < orderedCircosPeaks.Count; i++) {
                    var peak1 = orderedCircosPeaks[i];
                    if (peak1.IsEPA.ToUpper() == "TRUE" || peak1.IsDPA.ToUpper() == "TRUE") {
                        for (int j = i + 1; j < orderedCircosPeaks.Count; j++) {
                            var peak2 = orderedCircosPeaks[j];
                            if (peak1.Segment == peak2.Segment) continue;

                            if (peak2.IsEPA.ToUpper() == "TRUE" || peak2.IsDPA.ToUpper() == "TRUE") {
                                var correl = BasicMathematics.Coefficient(peak1.Values.ToArray(), peak2.Values.ToArray());
                                if (Math.Abs(correl) >= 0.9) {
                                    sw.WriteLine(peak1.Segment + "\t" + peak1.TargetPosition + "\t" + "EPA1" + "\t" + peak2.Segment + "\t" + peak2.TargetPosition + "\t" + "EPA2");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool isFattyAcidProp(string sn1Chain, string sn2Chain, string sn3Chain, string sn4Chain, string prop) {
            if (!sn1Chain.Contains("e") && !sn1Chain.Contains("d") && !sn1Chain.Contains("t") && !sn1Chain.Contains("O")) {
                if (sn1Chain == prop) return true;
            }
            if (!sn2Chain.Contains("e") && !sn2Chain.Contains("d") && !sn2Chain.Contains("t") && !sn2Chain.Contains("O")) {
                if (sn2Chain == prop) return true;
            }
            if (!sn3Chain.Contains("e") && !sn3Chain.Contains("d") && !sn3Chain.Contains("t") && !sn3Chain.Contains("O")) {
                if (sn3Chain == prop) return true;
            }
            if (!sn4Chain.Contains("e") && !sn4Chain.Contains("d") && !sn4Chain.Contains("t") && !sn4Chain.Contains("O")) {
                if (sn4Chain == prop) return true;
            }
            return false;
        }

        public static void GenerateCircosFiles(string input) {

            var plants = new List<string>() {
                //"Aizoaceae",
                //"Apiaceae",
                //"Apocynaceae",
                //"Asteraceae",
                //"Calycanthaceae",
                //"Campanulaceae",
                //"Celastraceae",
                //"Daphniphyllaceae",
                //"Fabaceae",
                //"Icacinaceae",
                //"Lauraceae",
                //"Linaceae",
                //"Loganiaceae",
                //"Lycopodiaceae",
                //"Lythraceae",
                //"Menispermaceae",
                //"Nyssaceae",
                //"Rubiaceae",
                //"Rutaceae",
                //"Stemonaceae"

                //"Aizoaceae",
                //"Apiaceae",
                //"Apocynaceae",
                //"Asteraceae",
                //"Calycanthaceae",
                //"Celastraceae",
                //"Daphniphyllaceae",
                //"Fabaceae",
                //"Lauraceae",
                //"Linaceae",
                //"Loganiaceae",
                //"Lycopodiaceae",
                //"Lythraceae",
                //"Nyssaceae",
                //"Rubiaceae",
                //"Rutaceae",
                //"Stemonaceae",

                "Apiaceae",
                "Apocynaceae",
                "Asteraceae",
                "Calycanthaceae",
                "Celastraceae",
                "Fabaceae",
                "Linaceae",
                "Lycopodiaceae",
                "Nyssaceae",
                "Rubiaceae"
            };
            var species = new List<string>() {
                //"Sceletium joubertii_leaf",
                //"Sceletium tortuosum_leaf",
                //"Sceletium sp._leaf",
                //"Angelica acutiloba KITAGAWA._leaf",
                //"Angelica acutiloba KITAGAWA var. sugiyamae HIKINO_leaf",
                //"Angelica dahurica BENTH. et HOOK. FIL._leaf",
                //"Angelica keiskei KOIDZ._leaf",
                //"Bupleurum scorzoneraefolium WILLD. var. stenophyllum NAKAI_leaf",
                //"Cnidium officinale MAKINO_leaf",
                //"Conium maculatum LINN._leaf",
                //"Foeniculum vulgare MILL._leaf",
                //"Allamanda cathartica Hendersonii_leaf",
                //"Catharanthus roseus_leaf",
                //"Carissa carandas L._leaf",
                //"Rauwolfia perakensis KING et GAMBLE_leaf",
                //"Rauwolfia serpentina BENTH._leaf",
                //"Rauwolfia verticillata BAILL._leaf",
                //"Tabernaemontana divaricata (L.) R.Br. ex Roem. & Schult._leaf",
                //"Vinca major LINN._leaf",
                //"Artemisia absinthium LINN._leaf",
                //"Artemisia annua L._leaf",
                //"Artemisia maritima LINN._leaf",
                //"Atractylodes lancea DC._leaf",
                //"Atractylodes lancea DC. var. chinensis KOIDZ._leaf",
                //"Atractylodes ovata DC._leaf",
                //"Carthamus tinctorius LINN._leaf",
                //"Chimonanthus praecox_leaf",
                //"Chimonanthus praecox_cortex",
                //"Lobelia inflata_leaf",
                //"Catha edulis_leaf",
                //"Daphniphyllum macropodum_leaf",
                //"Daphniphyllum teijsmannii_leaf",
                //"Abrus precatorius LINN._seed",
                //"Acacia senegal WILLDENOW_leaf",
                //"Caesalpinia sappan L._leaf",
                //"Cassia alata LlNN._leaf",
                //"Cassia angustifolia VAHL_leaf",
                //"Cassia fistula LINN._leaf",
                //"Cassia nomame HONDA_leaf",
                //"Cassia obtusifolia LINN._leaf",
                //"Cassia torosa CAV._leaf",
                //"Ceratonia siliqua LINN._leaf",
                //"Clitoria ternata L._leaf",
                //"Dalbergia latifolia ROXB._leaf",
                //"Delonix regia Raf._leaf",
                //"Glycyrrhiza echinata_leaf",
                //"Glycyrrhiza glabra LINN._leaf",
                //"Glycyrrhiza pallidinora MAXIM._leaf",
                //"Glycyrrhiza uralensis FISCH._leaf",
                //"Leucaena glauca BENTH._leaf",
                //"Medicago sativa L._leaf",
                //"Melilotus officinalis (L.) PALLAS_leaf",
                //"Pueraria lobata OHWI_leaf",
                //"Saraca asoca (ROXB.) DE WIDLE_leaf",
                //"Sophora flavescens SOLANDER. ex AIT. var. angustifolia KITAGAWA._leaf",
                //"Tamarindus indica L._leaf",
                //"Thermopsis lupinoides LINK_leaf",
                //"Uraria crinita DESV._leaf",
                //"Wisteria floribunda DC._leaf",
                //"Nothapodytes nimmoniana_leaf",
                //"Cinnamomum burmanni BLUME_leaf",
                //"Cinnamomum camphora SIEB._leaf",
                //"Cinnamomum camphora SIEB._cortex",
                //"Cinnamomum camphora SIEB. Forma linaloolifera SUGIMOTO_leaf",
                //"Cinnamomum cassia BLUME_leaf",
                //"Cinnamomum daphnoides SIEB. et ZUCC._leaf",
                //"Cinnamomum iners BL._leaf",
                //"Cinnamomum japonicum SIEB. ex NAKAI_leaf",
                //"Cinnamomum sieboldii MEISN._leaf",
                //"Cinnamomum zeylanicum NEES_leaf",
                //"Linum usitatissimum_seed",
                //"Linum usitatissimum_leaf",
                //"Spigelia marilandica_leaf",
                //"Strychos nux-vomica_leaf",
                //"Huperzia verticillata (L. f.) Rothm._leaf",
                //"Huperzia verticillata (L. f.) Rothm._stem",
                //"Huperzia squarrosa (G. Forst.) Trevis._leaf",
                //"Huperzia squarrosa (G. Forst.) Trevis._stem",
                //"Heimia salicifolia_leaf",
                //"Lythrum anceps/salicaria_leaf",
                //"Lythrum anceps/salicaria_stem",
                //"Sinomenium acutum_leaf",
                //"Camptotheca  acuminata_leaf",
                //"Coptis japonica_leaf",
                //"Ophiorrhiza sp._leaf",
                //"Rubia akane NAKAI_leaf",
                //"Rubia akane NAKAI_stem",
                //"Rubia akane NAKAI_root",
                //"Rubia tinctorum LINN._leaf",
                //"Rubia tinctorum LINN._stem",
                //"Rubia tinctorum LINN._root",
                //"Uncaria rhynchophylla MIQ._leaf",
                //"Uncaria rhynchophylla MIQ._stem",
                //"Uncaria rhynchophylla MIQ._root",
                //"Cinchona succirubra_leaf",
                //"Cephaelis ipecacuanha_leaf",
                //"Evodia rutaecarpa _leaf",
                //"Pilocarpus jaborandi_leaf",
                //"Croomia heterosepala_root",
                //"Stemona japonica MIQ._leaf",
                //"Stemona japonica MIQ._stem",
                //"Stemona japonica MIQ._root",
                //"Stemona sessilifolia MIQ._leaf",
                //"Stemona sessilifolia MIQ._stem",
                //"Stemona sessilifolia MIQ._root",
                //"Taxus baccata L._leaf",
                //"Taxus cuspidata SIEB. et ZUCC._leaf"

                "Angelica acutiloba KITAGAWA var. sugiyamae HIKINO_leaf",
                "Angelica acutiloba KITAGAWA._leaf",
                "Angelica dahurica BENTH. et HOOK. FIL._leaf",
                "Conium maculatum LINN._leaf",
                "Carissa carandas L._leaf",
                "Rauwolfia perakensis KING et GAMBLE_leaf",
                "Rauwolfia serpentina BENTH._leaf",
                "Rauwolfia verticillata BAILL._leaf",
                "Allamanda cathartica Hendersonii_leaf",
                "Vinca major LINN._leaf",
                "Tabernaemontana divaricata (L.) R.Br. ex Roem. & Schult._leaf",
                "Catharanthus roseus_leaf",
                "Artemisia absinthium LINN._leaf",
                "Artemisia maritima LINN._leaf",
                "Chimonanthus praecox_leaf",
                "Chimonanthus praecox_cortex",
                "Catha edulis_leaf",
                "Abrus precatorius LINN._seed",
                "Caesalpinia sappan L._leaf",
                "Cassia fistula LINN._leaf",
                "Clitoria ternata L._leaf",
                "Delonix regia Raf._leaf",
                "Glycyrrhiza glabra LINN._leaf",
                "Glycyrrhiza echinata_leaf",
                "Leucaena glauca BENTH._leaf",
                "Melilotus officinalis (L.) PALLAS_leaf",
                "Pueraria lobata OHWI_leaf",
                "Wisteria floribunda DC._leaf",
                "Medicago sativa L._leaf",
                "Linum usitatissimum_seed",
                "Huperzia verticillata (L. f.) Rothm._leaf",
                "Huperzia verticillata (L. f.) Rothm._stem",
                "Huperzia squarrosa (G. Forst.) Trevis._leaf",
                "Huperzia squarrosa (G. Forst.) Trevis._stem",
                "Camptotheca  acuminata_leaf",
                "Rubia akane NAKAI_stem",
                "Rubia akane NAKAI_root",
                "Rubia tinctorum LINN._leaf",
                "Rubia tinctorum LINN._stem",
                "Rubia tinctorum LINN._root",
                "Uncaria rhynchophylla MIQ._leaf",
                "Uncaria rhynchophylla MIQ._stem",
                "Uncaria rhynchophylla MIQ._root",
                "Cinchona succirubra_leaf",
                "Cephaelis ipecacuanha_leaf",
                "Ophiorrhiza sp._leaf"


            };
            var plantToSegment = new Dictionary<string, int> {

                //{ "Aizoaceae" , 1 },
                //{ "Apiaceae" , 2 },
                //{ "Apocynaceae" , 3 },
                //{ "Asteraceae" , 4 },
                //{ "Calycanthaceae" , 5 },
                //{ "Campanulaceae" , 6 },
                //{ "Celastraceae" , 7 },
                //{ "Daphniphyllaceae" , 8 },
                //{ "Fabaceae" , 9 },
                //{ "Icacinaceae" , 10 },
                //{ "Lauraceae" , 11 },
                //{ "Linaceae" , 12 },
                //{ "Loganiaceae" , 13 },
                //{ "Lycopodiaceae" , 14 },
                //{ "Lythraceae" , 15 },
                //{ "Menispermaceae" , 16 },
                //{ "Nyssaceae" , 17 },
                //{ "Rubiaceae" , 18 },
                //{ "Rutaceae" , 19 },
                //{ "Stemonaceae" , 20 }

                //{ "Aizoaceae" , 1 },
                //{ "Apiaceae" , 2 },
                //{ "Apocynaceae" , 3 },
                //{ "Asteraceae" , 4 },
                //{ "Calycanthaceae" , 5 },
                //{ "Celastraceae" , 6 },
                //{ "Daphniphyllaceae" , 7 },
                //{ "Fabaceae" , 8 },
                //{ "Lauraceae" , 9 },
                //{ "Linaceae" , 10 },
                //{ "Loganiaceae" , 11 },
                //{ "Lycopodiaceae" , 12 },
                //{ "Lythraceae" , 13 },
                //{ "Nyssaceae" , 14 },
                //{ "Rubiaceae" , 15 },
                //{ "Rutaceae" , 16 },
                //{ "Stemonaceae" , 17 }

                { "Apiaceae" , 1 },
                { "Apocynaceae" , 2 },
                { "Asteraceae" , 3 },
                { "Calycanthaceae" , 4 },
                { "Celastraceae" , 5 },
                { "Fabaceae" , 6 },
                { "Linaceae" , 7 },
                { "Lycopodiaceae" , 8 },
                { "Nyssaceae" , 9 },
                { "Rubiaceae" , 10 }


            };
            var plantToPosition = new Dictionary<string, int> {
                //{ "Aizoaceae" , 1 },
                //{ "Apiaceae" , 1 },
                //{ "Apocynaceae" , 1 },
                //{ "Asteraceae" , 1 },
                //{ "Calycanthaceae" , 1 },
                //{ "Campanulaceae" , 1 },
                //{ "Celastraceae" , 1 },
                //{ "Daphniphyllaceae" , 1 },
                //{ "Fabaceae" , 1 },
                //{ "Icacinaceae" , 1 },
                //{ "Lauraceae" , 1 },
                //{ "Linaceae" , 1 },
                //{ "Loganiaceae" , 1 },
                //{ "Lycopodiaceae" , 1 },
                //{ "Lythraceae" , 1 },
                //{ "Menispermaceae" , 1 },
                //{ "Nyssaceae" , 1 },
                //{ "Rubiaceae" , 1 },
                //{ "Rutaceae" , 1 },
                //{ "Stemonaceae" , 1 }

                //{ "Aizoaceae" , 1 },
                //{ "Apiaceae" , 1 },
                //{ "Apocynaceae" , 1 },
                //{ "Asteraceae" , 1 },
                //{ "Calycanthaceae" , 1 },
                //{ "Celastraceae" , 1 },
                //{ "Daphniphyllaceae" , 1 },
                //{ "Fabaceae" , 1 },
                //{ "Lauraceae" , 1 },
                //{ "Linaceae" , 1 },
                //{ "Loganiaceae" , 1 },
                //{ "Lycopodiaceae" , 1 },
                //{ "Lythraceae" , 1 },
                //{ "Nyssaceae" , 1 },
                //{ "Rubiaceae" , 1 },
                //{ "Rutaceae" , 1 },
                //{ "Stemonaceae" , 1 }

                { "Apiaceae" , 1 },
                { "Apocynaceae" , 1 },
                { "Asteraceae" , 1 },
                { "Calycanthaceae" , 1 },
                { "Celastraceae" , 1 },
                { "Fabaceae" , 1 },
                { "Linaceae" , 1 },
                { "Lycopodiaceae" , 1 },
                { "Nyssaceae" , 1 },
                { "Rubiaceae" , 1 },

            };
            var circosPeaks = new List<CircosPeak>();

            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                var header = sr.ReadLine();
                var headerArray = header.Split('\t');

                var idToPlant = new Dictionary<int, string>();
                for (int i = 2; i < headerArray.Length - 1; i++) {
                    idToPlant[i] = headerArray[i];
                }

                var secondHeader = sr.ReadLine();
                var secondHeaderArray = secondHeader.Split('\t');
                var idToSpecies = new Dictionary<int, string>();
                for (int i = 2; i < secondHeaderArray.Length - 1; i++) {
                    idToSpecies[i] = secondHeaderArray[i];
                }

                sr.ReadLine();

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var lineArray = line.Split('\t');
                    var title = lineArray[0];
                    var rt = double.Parse(lineArray[1].Split('_')[0]);
                    var mz = double.Parse(lineArray[1].Split('_')[1]);

                    var plantToValue = new Dictionary<string, double>();
                    var speciesToValue = new Dictionary<string, double>();
                    foreach (var plant in plants) {
                        plantToValue[plant] = 0.0;
                    }

                    foreach (var spe in species) {
                        speciesToValue[spe] = 0.0;
                    }

                    for (int i = 2; i < lineArray.Length - 1; i++) {
                        var plantname = idToPlant[i];
                        plantToValue[plantname] += double.Parse(lineArray[i]);

                        var speciesname = idToSpecies[i];
                        speciesToValue[speciesname] = double.Parse(lineArray[i]);
                    }

                    var plantorigin = title.Split('_')[2];
                    var segment = plantToSegment[plantorigin];
                    var position = plantToPosition[plantorigin];

                    var peaks = TextLibraryParcer.TextToSpectrumList(lineArray[lineArray.Length - 1], ':', ';');
                    var circosPeak = new CircosPeak() {
                        Title = title,
                        Rt = rt,
                        Mz = mz,
                        Peaks = peaks,
                        SampleToValue = plantToValue,
                        SpeciesToValue = speciesToValue,
                        Segment = segment,
                        TargetPosition = position, 
                        StartPosition = position - 1,
                        EndPosition = position + 1
                    };
                    circosPeaks.Add(circosPeak);

                    plantToPosition[plantorigin] += 2;
                }
            }

            var directory = System.IO.Path.GetDirectoryName(input);
            var filename = System.IO.Path.GetFileNameWithoutExtension(input);

            var linkfile = directory + "\\" + filename + "-link.txt";
            var segfile = directory + "\\" + filename + "-seg.txt";
            var mapfile = directory + "\\" + filename + "-map.txt";
            var mapnamefile = directory + "\\" + filename + "-mapname.txt";
            var expfile = directory + "\\" + filename + "-exp.txt";
            var expfileall = directory + "\\" + filename + "-expall.txt";
            var mspfile = directory + "\\" + filename + ".msp";

            using (var sw = new StreamWriter(mspfile, false, Encoding.ASCII)) {
                foreach (var circosPeak in circosPeaks) {
                    sw.WriteLine("NAME: " + circosPeak.Title);
                    sw.WriteLine("PRECURSORMZ: " + circosPeak.Mz);
                    sw.WriteLine("RETENTIONTIME: " + circosPeak.Rt);
                    sw.WriteLine("Comment: " + circosPeak.Segment + "_" + circosPeak.TargetPosition);
                    sw.WriteLine("Num Peaks: " + circosPeak.Peaks.Count);

                    foreach (var peak in circosPeak.Peaks) {
                        sw.WriteLine(Math.Round(peak.Mz, 5) + "\t" + Math.Round(peak.Intensity, 0));
                    }
                    sw.WriteLine();
                }
            }

            var edges = EgdeGenerator.GetEdges(mspfile, 0.025);
            //using (var sw = new StreamWriter(linkfile, false, Encoding.ASCII)) {
            //    sw.WriteLine("source seg\tsource pos\tlinkA\ttarget seg\ttarget pos\tlinkB");
            //    foreach (var edge in edges) {
            //        sw.WriteLine(edge.SourceComment.Split('_')[0] + "\t" + edge.SourceComment.Split('_')[1] + "\t" + "a" + "\t" +
            //            edge.TargetComment.Split('_')[0] + "\t" + edge.TargetComment.Split('_')[1] + "\t" + "b");
            //    }
            //}

            exportLinkfiles(1, linkfile, edges);
            exportLinkfiles(2, linkfile, edges);
            exportLinkfiles(3, linkfile, edges);
            exportLinkfiles(4, linkfile, edges);
            exportLinkfiles(5, linkfile, edges);
            exportLinkfiles(6, linkfile, edges);
            exportLinkfiles(7, linkfile, edges);
            exportLinkfiles(8, linkfile, edges);
            exportLinkfiles(9, linkfile, edges);
            exportLinkfiles(10, linkfile, edges);
            //exportLinkfiles(11, linkfile, edges);
            //exportLinkfiles(12, linkfile, edges);
            //exportLinkfiles(13, linkfile, edges);
            //exportLinkfiles(14, linkfile, edges);
            //exportLinkfiles(15, linkfile, edges);
            //exportLinkfiles(16, linkfile, edges);
            //exportLinkfiles(17, linkfile, edges);
            //exportLinkfiles(18, linkfile, edges);
            //exportLinkfiles(19, linkfile, edges);
            //exportLinkfiles(20, linkfile, edges);


            using (var sw = new StreamWriter(segfile, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tSpos\tEpos\tTitle\tName");
                foreach (var peak in circosPeaks) {
                    sw.WriteLine(peak.Segment + "\t" + peak.StartPosition + "\t" + peak.EndPosition + "\t" + peak.Title + "\t" + peak.Title);
                }
            }

            using (var sw = new StreamWriter(mapfile, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                foreach (var peak in circosPeaks) {
                    sw.WriteLine(peak.Segment + "\t" + peak.TargetPosition + "\t" + Math.Log(peak.SampleToValue.Values.Max(), 2));
                }
            }

            using (var sw = new StreamWriter(mapnamefile, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\tTitle");
                for (int i = 0; i < plants.Count; i++) {
                    sw.WriteLine(i + 1 + "\t" + (int)(plantToPosition[plants[i]] * 0.5) + "\t" + plants[i]);
                }
            }

            using (var sw = new StreamWriter(expfile, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\t" + String.Join("\t", plants));
                foreach (var peak in circosPeaks) {
                    var textString = peak.Segment + "\t" + peak.TargetPosition;
                    foreach (var plant in plants) {
                        if (peak.SampleToValue[plant] > 0)
                            textString += "\t" + 1;
                        else
                            textString += "\t" + -1;
                    }
                    sw.WriteLine(textString);
                }
            }

            using (var sw = new StreamWriter(expfileall, false, Encoding.ASCII)) {
                sw.WriteLine("Seg\tTpos\t" + String.Join("\t", species));
                foreach (var peak in circosPeaks) {
                    var textString = peak.Segment + "\t" + peak.TargetPosition;
                    foreach (var spe in species) {
                        if (peak.SpeciesToValue[spe] > 0)
                            textString += "\t" + 1;
                        else
                            textString += "\t" + -1;
                    }
                    sw.WriteLine(textString);
                }
            }
        }

        private static void exportLinkfiles(int segment, string output, List<EdgeInformation> edges) {

            var filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_" + segment + ".txt";

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                var donelist = new List<string>();
                foreach (var edge in edges.Where(n => n.SourceComment.Split('_')[0] == segment.ToString())) {
                    sw.WriteLine(edge.SourceComment.Split('_')[0] + "\t" + edge.SourceComment.Split('_')[1] + "\t" + "a" + "\t" +
                       edge.TargetComment.Split('_')[0] + "\t" + edge.TargetComment.Split('_')[1] + "\t" + "b");
                }
            }
        }
    }
}
