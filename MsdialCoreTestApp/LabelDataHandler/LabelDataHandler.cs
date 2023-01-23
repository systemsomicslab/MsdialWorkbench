using CompMs.Common.Components;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompMs.Common.Extension;

namespace CompMs.App.MsdialConsole.LabelDataHandler {

    public class PeakInfo {
        public string Memo { get; set; }
        public int AlignmentID { get; set; }
        public string AverageRt { get; set; }
        public string AverageMz { get; set; }
        public string MetaboliteName { get; set; }
        public string Adduct { get; set; }
        public string PostCurationResult { get; set; }
        public string Fill { get; set; }
        public string MSMSAsigned { get; set; }
        public string ReferenceRT { get; set; }
        public string ReferenceMz { get; set; }
        public string Formula { get; set; }
        public string Ontology { get; set; }
        public string InChIKey { get; set; }
        public string SMILES { get; set; }
        public string AnnotationTag { get; set; }
        public string RTMatch { get; set; }
        public string MZMatch { get; set; }
        public string MSMSMatch { get; set; }
        public string Comment { get; set; }
        public string ManuallyModified4Quantification { get; set; }
        public string ManuallyModified4Annotation { get; set; }
        public int IsotopeTrackingParentID { get; set; }
        public int IsotopeTrackingWeightNumber { get; set; }
        public string TotalScore { get; set; }
        public string RTSimilarity { get; set; }
        public string DotProduct { get; set; }
        public string RevDotProduct { get; set; }
        public string FragmentPercentage { get; set; }
        public string SN { get; set; }
        public string SpectrumReferenceFile { get; set; }
        public string MS1IsotopicSpectrum { get; set; }
        public string MSMSSpectrum { get; set; }
        public string Abundance12C { get; set; }
        public string Abundance13C { get; set; }

        public string Abundance12CStd { get; set; }
        public string Abundance13CStd { get; set; }

    }

    public sealed class LabelDataHandler {
        private LabelDataHandler() {

        }

        public static void ExtractCorrectPeakList(string input, string outputdir) {

            var header = GetHeader(input);
            var peaks = GetPeaks(input);
            
            var outputfilename = Path.GetFileNameWithoutExtension(input) + "_extracted";
            var output = Path.Combine(outputdir, outputfilename + ".txt");
            var logfile = Path.Combine(outputdir, outputfilename + "_log.txt");
            var logerror = string.Empty;

            var msp12cfile = Path.Combine(outputdir, outputfilename + "_12C.msp");
            var msp13cfile = Path.Combine(outputdir, outputfilename + "_13C.msp");

            var output_ms2 = Path.Combine(outputdir, outputfilename + "_ms2contained.txt");
            var msp12cfile_ms2 = Path.Combine(outputdir, outputfilename + "_12C_ms2contained.msp");
            var msp13cfile_ms2 = Path.Combine(outputdir, outputfilename + "_13C_ms2contained.msp");

            var separatedmatfilesexportfolder = Path.Combine(outputdir, "msfinder_cid");
            if (!Directory.Exists(separatedmatfilesexportfolder)) {
                Directory.CreateDirectory(separatedmatfilesexportfolder);
            }

            var dict = new Dictionary<int, PeakInfo>();
            foreach (var peak in peaks) { dict[peak.AlignmentID] = peak; }

            var peakgroups = peaks.OrderBy(n => n.IsotopeTrackingWeightNumber).GroupBy(n => n.IsotopeTrackingParentID).ToList();
            var okPeaks = new List<(PeakInfo, PeakInfo)>();
            
            foreach (var group in peakgroups) {
                var gPeaks = group.ToList();
                var lastOKID = 0;
                for (int i = 0; i < gPeaks.Count; i++) {
                    if (gPeaks[i].Comment.Contains("OK")) {
                        lastOKID = i;
                    }
                }
                if (lastOKID == 0) continue;

                var startPeak = gPeaks[0];
                var okPeak = gPeaks[lastOKID]; // last peak

                var okComment = okPeak.Comment;
                if (okComment.Split(' ').Length >= 2) {
                    if (okComment.Split(' ')[0].Contains("OK")) { //OK? 1213 
                        var newStartPeakID = int.Parse(okComment.Split(' ')[1]);

                        if (!dict.ContainsKey(newStartPeakID)) {
                            logerror += "no id\t" + newStartPeakID + "\r\n";
                            continue;
                        }
                        startPeak = dict[newStartPeakID];
                        startPeak.IsotopeTrackingParentID = startPeak.AlignmentID;
                        okPeak.IsotopeTrackingParentID = startPeak.IsotopeTrackingParentID;

                        okPeak.IsotopeTrackingWeightNumber -= startPeak.IsotopeTrackingWeightNumber;
                        startPeak.IsotopeTrackingWeightNumber -= startPeak.IsotopeTrackingWeightNumber;
                    }
                    else if (okComment.Split(' ')[1].Contains("OK")) { //1213 OK?
                        var newStartPeakID = int.Parse(okComment.Split(' ')[0]);
                        if (!dict.ContainsKey(newStartPeakID)) {
                            logerror += "no id\t" + newStartPeakID + "\r\n";
                            continue;
                        }

                        startPeak = dict[newStartPeakID];
                        startPeak.IsotopeTrackingParentID = startPeak.AlignmentID;
                        okPeak.IsotopeTrackingParentID = startPeak.IsotopeTrackingParentID;

                        okPeak.IsotopeTrackingWeightNumber -= startPeak.IsotopeTrackingWeightNumber;
                        startPeak.IsotopeTrackingWeightNumber -= startPeak.IsotopeTrackingWeightNumber;
                    }
                }

                okPeaks.Add((startPeak, okPeak));
            }

            using (var sw = new StreamWriter(output)) {
                sw.WriteLine(String.Join("\t", header));
                foreach (var peak in okPeaks) {
                    writePeakInfo(sw, peak);
                }
            }

            using (var sw = new StreamWriter(msp12cfile)) {
                foreach (var peak in okPeaks) {
                    writeMspField(sw, peak);
                }
            }

            using (var sw = new StreamWriter(msp13cfile)) {
                foreach (var peak in okPeaks) {
                    writeMspFor13CField(sw, peak);
                }
            }

            
            using (var sw = new StreamWriter(output_ms2)) {
                sw.WriteLine(String.Join("\t", header));
                foreach (var peak in okPeaks) {
                    if (int.TryParse(peak.Item2.Memo, out int convertedNum)) continue;
                    if (peak.Item1.MSMSAsigned == "FALSE") continue;
                    writePeakInfo(sw, peak);
                }
            }

            using (var sw = new StreamWriter(msp12cfile_ms2)) {
                foreach (var peak in okPeaks) {
                    if (int.TryParse(peak.Item2.Memo, out int convertedNum)) continue;
                    if (peak.Item1.MSMSAsigned == "FALSE") continue;
                    writeMspField(sw, peak);
                }
            }

            var counter = 0;
            foreach (var peak in okPeaks) {
                if (int.TryParse(peak.Item2.Memo, out int convertedNum)) continue;
                if (peak.Item1.MSMSAsigned == "FALSE") continue;

                var name = peak.Item1.MetaboliteName.Contains("w/o") ||
                    peak.Item1.MetaboliteName.Contains("Unknown") ? "Unknown" : peak.Item1.MetaboliteName;
                var propertysummary = counter + "_" + peak.Item1.AlignmentID + "_" + Math.Round(double.Parse(peak.Item1.AverageMz), 4).ToString() + "_" + Math.Round(double.Parse(peak.Item1.AverageRt), 2).ToString();
                var summaryname = propertysummary + "_" + name;
                var validname = MakeValidFileName(summaryname);

                var exportfile = Path.Combine(separatedmatfilesexportfolder, validname + ".mat");
                using (var sw = new StreamWriter(exportfile)) {
                    writeMspField(sw, peak);
                }
                counter++;
            }

            using (var sw = new StreamWriter(msp13cfile_ms2)) {
                foreach (var peak in okPeaks) {
                    if (int.TryParse(peak.Item2.Memo, out int convertedNum)) continue;
                    if (peak.Item1.MSMSAsigned == "FALSE") continue;
                    writeMspFor13CField(sw, peak);
                }
            }

            using (var sw = new StreamWriter(logfile)) {
                sw.WriteLine(logerror);
            }
        }

        private static string MakeValidFileName(string name) {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        private static void writeMspFor13CField(StreamWriter sw, (PeakInfo, PeakInfo) peak) {
            writeMspFor13CField(sw, peak.Item1, peak.Item2);
        }

       
        private static void writeMspField(StreamWriter sw, (PeakInfo, PeakInfo) peak) {
            writeMspField(sw, peak.Item1, peak.Item2);
        }

        private static void writeMspField(StreamWriter sw, PeakInfo item1, PeakInfo item2) {

            var name = item1.MetaboliteName.Contains("w/o") ||
                 item1.MetaboliteName.Contains("Unknown") ? "Unknown" : item1.MetaboliteName;
            var propertysummary = item1.AlignmentID + "_" + Math.Round(double.Parse(item1.AverageMz), 4).ToString() + "_" + Math.Round(double.Parse(item1.AverageRt), 2).ToString();
            var summaryname = propertysummary + "_" + name;

            sw.WriteLine("NAME: " + summaryname);
            sw.WriteLine("SCANNUMBER: " + item1.AlignmentID);
            sw.WriteLine("RETENTIONTIME: " + item1.AverageRt);
            sw.WriteLine("PRECURSORMZ: " + item1.AverageMz);
            var adduct = item1.Adduct;
            if (adduct == "[Cat]+") adduct = "[M]+";


            sw.WriteLine("PRECURSORTYPE: " + adduct);

            var adductObj = AdductIonParser.GetAdductIonBean(adduct);

            sw.WriteLine("IONMODE: " + adductObj.IonMode.ToString());
            sw.WriteLine("SPECTRUMTYPE: Centroid");
            sw.WriteLine("INTENSITY: " + item1.Abundance12C);
            if (name == "Unknown") {
                sw.WriteLine("INCHIKEY: null");
                sw.WriteLine("SMILES: null");
                sw.WriteLine("ONTOLOGY: null");
                sw.WriteLine("FORMULA: null");
            }
            else {
                sw.WriteLine("INCHIKEY: " + item1.InChIKey);
                sw.WriteLine("SMILES: " + item1.SMILES);
                sw.WriteLine("ONTOLOGY: " + item1.Ontology);
                sw.WriteLine("FORMULA: " + item1.Formula);
            }
            
            sw.WriteLine("#Specific field for labeled experiment");
            sw.WriteLine("CarbonCount: " + item2.IsotopeTrackingWeightNumber);
            sw.Write("MSTYPE: ");
            sw.WriteLine("MS1");

            sw.WriteLine("Num Peaks: 3");

            // 61.07436:520 62.07771:107 63.08107:1156
            var ms1spec = item1.MS1IsotopicSpectrum.Split(' ').ToList().Select(s => new SpectrumPeak() { Mass = double.Parse(s.Split(':')[0]), Intensity = double.Parse(s.Split(':')[1]) });
            foreach (var peak in ms1spec) {
                sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
            }

            if (item1.MSMSSpectrum.IsEmptyOrNull()) {
                sw.WriteLine("MSTYPE: MS2");
                sw.WriteLine("Num Peaks: 0");
            }
            else {
                var ms2spec = item1.MSMSSpectrum.Split(' ').ToList().Select(s => new SpectrumPeak() { Mass = double.Parse(s.Split(':')[0]), Intensity = double.Parse(s.Split(':')[1]) }).ToList();
                sw.WriteLine("MSTYPE: MS2");
                sw.WriteLine("Num Peaks: " + ms2spec.Count);

                foreach (var peak in ms2spec) {
                    sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                }
            }
            sw.WriteLine();
        }

        private static void writeMspFor13CField(StreamWriter sw, PeakInfo item1, PeakInfo item2) {
            var name = item1.MetaboliteName.Contains("w/o") ||
                 item1.MetaboliteName.Contains("Unknown") ? "Unknown_13C" : item1.MetaboliteName + "_13C";
            var propertysummary = item2.AlignmentID + "_" + Math.Round(double.Parse(item2.AverageMz), 4).ToString() + "_" + Math.Round(double.Parse(item2.AverageRt), 2).ToString();
            var summaryname = propertysummary + "_" + name;

            sw.WriteLine("NAME: " + summaryname);
            sw.WriteLine("SCANNUMBER: " + item2.AlignmentID);
            sw.WriteLine("RETENTIONTIME: " + item2.AverageRt);
            sw.WriteLine("PRECURSORMZ: " + item2.AverageMz);

            var adduct = item1.Adduct;
            if (adduct == "[Cat]+") adduct = "[M]+";

            sw.WriteLine("PRECURSORTYPE: " + adduct);


            var adductObj = AdductIonParser.GetAdductIonBean(adduct);

            sw.WriteLine("IONMODE: " + adductObj.IonMode.ToString());
            sw.WriteLine("SPECTRUMTYPE: Centroid");
            sw.WriteLine("INTENSITY: " + item2.Abundance13C);
            if (name == "Unknown_13C") {
                sw.WriteLine("INCHIKEY: null");
                sw.WriteLine("SMILES: null");
                sw.WriteLine("ONTOLOGY: null");
                sw.WriteLine("FORMULA: null");
            }
            else {
                sw.WriteLine("INCHIKEY: " + item1.InChIKey);
                sw.WriteLine("SMILES: " + item1.SMILES);
                sw.WriteLine("ONTOLOGY: " + item1.Ontology);
                sw.WriteLine("FORMULA: " + item1.Formula);
            }

            sw.WriteLine("#Specific field for labeled experiment");
            sw.WriteLine("CarbonCount: " + item2.IsotopeTrackingWeightNumber);
            sw.WriteLine("COMMENT: IsotopeParent_{0}", item1.AlignmentID);
            sw.Write("MSTYPE: ");
            sw.WriteLine("MS1");

            sw.WriteLine("Num Peaks: 3");

            // 61.07436:520 62.07771:107 63.08107:1156
            var ms1spec = item2.MS1IsotopicSpectrum.Split(' ').ToList().Select(s => new SpectrumPeak() { Mass = double.Parse(s.Split(':')[0]), Intensity = double.Parse(s.Split(':')[1]) });
            foreach (var peak in ms1spec) {
                sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
            }

            if (item2.MSMSSpectrum.IsEmptyOrNull()) {
                sw.WriteLine("MSTYPE: MS2");
                sw.WriteLine("Num Peaks: 0");
            }
            else {
                var ms2spec = item2.MSMSSpectrum.Split(' ').ToList().Select(s => new SpectrumPeak() { Mass = double.Parse(s.Split(':')[0]), Intensity = double.Parse(s.Split(':')[1]) }).ToList();
                sw.WriteLine("MSTYPE: MS2");
                sw.WriteLine("Num Peaks: " + ms2spec.Count);

                foreach (var peak in ms2spec) {
                    sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                }
            }
            sw.WriteLine();
        }


        private static void writePeakInfo(StreamWriter sw, (PeakInfo, PeakInfo) peak) {
            var beginPeak = peak.Item1;
            var endPeak = peak.Item2;
            if (int.TryParse(endPeak.Memo,out int convertedNum)) {
                beginPeak.Memo = "Should be in source fragment ion";
            }
            writePeakInfo(sw, beginPeak);
            writePeakInfo(sw, endPeak);
        }

        private static void writePeakInfo(StreamWriter sw, PeakInfo peak) {
            if (peak.Adduct == "[Cat]+") peak.Adduct = "[M]+";
            var info = new List<string>() {
                peak.Memo, peak.AlignmentID.ToString(), peak.AverageRt, peak.AverageMz, peak.MetaboliteName,
                peak.Adduct, peak.PostCurationResult, peak.Fill, peak.MSMSAsigned, peak.ReferenceRT,
                peak.ReferenceMz, peak.Formula, peak.Ontology, peak.InChIKey, peak.SMILES,
                peak.AnnotationTag, peak.RTMatch, peak.MZMatch, peak.MSMSMatch, peak.Comment,
                peak.ManuallyModified4Quantification, peak.ManuallyModified4Annotation,
                peak.IsotopeTrackingParentID.ToString(), peak.IsotopeTrackingWeightNumber.ToString(),
                peak.TotalScore, peak.RTSimilarity, peak.DotProduct, peak.RevDotProduct, peak.FragmentPercentage,
                peak.SN,peak.SpectrumReferenceFile, peak.MS1IsotopicSpectrum,
                peak.MSMSSpectrum, peak.Abundance12C, peak.Abundance13C, peak.Abundance12CStd, peak.Abundance13CStd

            };
            sw.WriteLine(String.Join("\t", info));
        }

        public static List<string> GetHeader(string input) {
            var header = new List<string>();
            using (var sr = new StreamReader(input)) {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                header = sr.ReadLine().Split('\t').ToList();
            }
            return header;
        }

        public static List<PeakInfo> GetPeaks(string input) {
            var peaks = new List<PeakInfo>();
            using (var sr = new StreamReader(input)) {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');
                    var peak = new PeakInfo() {
                        Memo = lineArray[0], AlignmentID = int.Parse(lineArray[1]), AverageRt = lineArray[2], AverageMz = lineArray[3], MetaboliteName = lineArray[4],
                        Adduct = lineArray[5], PostCurationResult = lineArray[6], Fill = lineArray[7], MSMSAsigned = lineArray[8],
                        ReferenceRT = lineArray[9], ReferenceMz = lineArray[10], Formula = lineArray[11], Ontology = lineArray[12], InChIKey = lineArray[13],
                        SMILES = lineArray[14], AnnotationTag = lineArray[15], RTMatch = lineArray[16], MZMatch = lineArray[17], MSMSMatch = lineArray[18],
                        Comment = lineArray[19], ManuallyModified4Quantification = lineArray[20], ManuallyModified4Annotation = lineArray[21],
                        IsotopeTrackingParentID = int.Parse(lineArray[22]), IsotopeTrackingWeightNumber = int.Parse(lineArray[23]), TotalScore = lineArray[24],
                        RTSimilarity = lineArray[25], DotProduct = lineArray[26], RevDotProduct = lineArray[27], FragmentPercentage = lineArray[28],
                        SN = lineArray[29], SpectrumReferenceFile = lineArray[30], MS1IsotopicSpectrum = lineArray[31], MSMSSpectrum = lineArray[32],
                        Abundance12C = lineArray[33], Abundance13C = lineArray[34], Abundance12CStd = lineArray[35], Abundance13CStd = lineArray[36]
                    };
                    peaks.Add(peak);
                }
            }
            return peaks;
        }
    }
}
