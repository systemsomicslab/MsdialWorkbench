using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

        public static void ExtractCorrectPeakList(string input, string output) {

            var header = GetHeader(input);
            var peaks = GetPeaks(input);
            var logfile = Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "_log.txt");

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
                           
                        }

                        startPeak = dict[newStartPeakID];
                    }
                    else if (okComment.Split(' ')[1].Contains("OK")) { //1213 OK?
                        var newStartPeakID = int.Parse(okComment.Split(' ')[0]);
                        startPeak = dict[newStartPeakID];
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
        }

        private static void writePeakInfo(StreamWriter sw, (PeakInfo, PeakInfo) peak) {
            var beginPeak = peak.Item1;
            var endPeak = peak.Item1;
            writePeakInfo(sw, beginPeak);
        }

        private static void writePeakInfo(StreamWriter sw, PeakInfo peak) {
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
