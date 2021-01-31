using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Parser {
    public sealed class MgfParser
    {
        private MgfParser() { }

        public static List<MgfRecord> ReadMgf(string filepath) {
            var mgfRecords = new List<MgfRecord>();

            using (var sr = new StreamReader(filepath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == "BEGIN IONS")
                    {
                        var record = getMgfRecord(sr);
                        mgfRecords.Add(record);
                    }
                }
            }

            return mgfRecords;
        }

        private static MgfRecord getMgfRecord(StreamReader sr) {
            var mgfRecord = new MgfRecord();
            while (sr.Peek() > -1)
            {
                var line = sr.ReadLine();
                if (line == "END IONS") return mgfRecord;

                var lineEqualSplit = line.Split('=');
                if (lineEqualSplit.Length > 1)
                {
                    var field = lineEqualSplit[0];
                    switch (field)
                    {
                        case "TITLE": mgfRecord.Title = line.Substring(field.Length + 1); break;
                        case "PEPMASS":
                            var m = 0.0F;
                            if (float.TryParse(lineEqualSplit[1], out m)) mgfRecord.Pepmass = m;
                            break;
                        case "RTINSECONDS":
                            var r = 0.0F;
                            if (float.TryParse(lineEqualSplit[1], out r)) mgfRecord.Rt = r / 60.0F;
                            break;
                        case "CHARGE":
                            var chargeState = lineEqualSplit[1].Trim();
                            var chargeValue = 0;
                            if (int.TryParse(chargeState.Substring(0, chargeState.Length - 1), out chargeValue))
                                mgfRecord.Charge = chargeValue;

                            if (chargeState.Contains("+")) mgfRecord.IonMode = IonMode.Positive;
                            else if (chargeState.Contains("-")) mgfRecord.IonMode = IonMode.Negative;

                            break;
                        case "MSLEVEL":
                            var mslevelString = lineEqualSplit[1];
                            var mslevel = 1;
                            if (int.TryParse(mslevelString, out mslevel)) mgfRecord.Mslevel = mslevel;
                            break;
                        case "SOURCE_INSTRUMENT":
                            mgfRecord.Source_instrument = line.Substring(field.Length + 1);
                            break;
                        case "FILENAME":
                            mgfRecord.Filename = line.Substring(field.Length + 1);
                            break;
                        case "SEQ":
                            mgfRecord.Seq = line.Substring(field.Length + 1);
                            break;
                        case "IONMODE":
                            if (lineEqualSplit[1].Contains("P") || lineEqualSplit[1].Contains("p")) mgfRecord.IonMode = IonMode.Positive;
                            else mgfRecord.IonMode = IonMode.Negative;
                            break;
                        case "ORGANISM":
                            mgfRecord.Organism = line.Substring(field.Length + 1);
                            break;
                        case "NAME":
                            mgfRecord.Name = line.Substring(field.Length + 1);
                            //var adduct = line.Split(' ')[line.Split(' ').Length - 1];
                            //if (AdductConverter.GnpsAdductToMF.ContainsKey(adduct))
                            //{
                            //    var adductMF = AdductConverter.GnpsAdductToMF[adduct];
                            //    mgfRecord.Adduct = AdductIonParcer.GetAdductIonBean(adductMF);
                            //}
                            //else mgfRecord.Adduct = null;
                            break;
                        case "PI":
                            mgfRecord.Pi = line.Substring(field.Length + 1);
                            break;
                        case "DATACOLLECTOR":
                            mgfRecord.Datacollector = line.Substring(field.Length + 1); ;
                            break;
                        case "SMILES":
                            if (lineEqualSplit[1].Length > 0)
                                mgfRecord.Smiles = line.Substring(field.Length + 1); ;
                            break;
                        case "LIBRARYQUALITY":
                            mgfRecord.LibraryQuality = line.Substring(field.Length + 1);
                            break;
                        case "SPECTRUMID":
                            mgfRecord.SpectrumID = line.Substring(field.Length + 1);
                            break;
                        case "SCAN":
                            var scan = 0;
                            var scanString = lineEqualSplit[1];
                            if (int.TryParse(scanString, out scan)) mgfRecord.Scan = scan;
                            break;
                    }
                }
                else
                {
                    double mz = 0.0, intensity = 0.0;
                    int charge = 1;
                    //var lineSpaceSplit = line.Split('\t');
                    var lineSpaceSplit = line.Split(' ');
                    var peak = new SpectrumPeak();
                    if (lineSpaceSplit.Length > 0 && double.TryParse(lineSpaceSplit[0], out mz)) peak.Mass = mz;
                    if (lineSpaceSplit.Length > 1 && double.TryParse(lineSpaceSplit[1], out intensity)) peak.Intensity = intensity;
                    if (lineSpaceSplit.Length > 2 && int.TryParse(lineSpaceSplit[2], out charge)) peak.Charge = charge;
                    mgfRecord.Spectrum.Add(peak);
                }
            }
            return mgfRecord;
        }

        public static void ConvertMgfToSeparatedMSPs(string filepath, string folderpath) {
            var mgfRecords = ReadMgf(filepath);
            WriteAsSeparatedMSPs(folderpath, mgfRecords);
        }

        public static void WriteAsSeparatedMSPs(string folderpath, List<MgfRecord> mgfRecords) {

            var invalidChars = Path.GetInvalidFileNameChars();
            var counter = 0;
            foreach (var record in mgfRecords) {
                var filename = record.Title;
                if (filename == string.Empty)
                    filename = "Query_" + counter;

                var converted = string.Concat(
                  filename.Select(c => invalidChars.Contains(c) ? '_' : c));

                var filepath = Path.Combine(folderpath, converted + ".msp");
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    writeMspFields(record, sw);
                }
                counter++;
            }
        }

        public static void WriteAsMsp(string filepath, List<MgfRecord> mgfRecords) {
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                for (int i = 0; i < mgfRecords.Count; i++)
                {
                    writeMspFields(mgfRecords[i], sw);
                }
            }
        }

        public static void WriteFileName(string filepath, List<MgfRecord> mgfRecords) {
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {
                for (int i = 0; i < mgfRecords.Count; i++)
                {
                    if (mgfRecords[i].Adduct == null) continue;
                    if (mgfRecords[i].Smiles == null || mgfRecords[i].Smiles == string.Empty || mgfRecords[i].Smiles == "N/A" || mgfRecords[i].Smiles == "/A" || mgfRecords[i].Smiles.Trim() == string.Empty) continue;
                    //if (mgfRecord.LibraryQuality != "1") return;
                    if (mgfRecords[i].Mslevel != 2) continue;
                    sw.WriteLine(mgfRecords[i].SpectrumID);
                }
            }
        }

        private static void writeMspFields(MgfRecord mgfRecord, StreamWriter sw) {

            var adducttype = string.Empty;
            if (mgfRecord.Adduct == null || !mgfRecord.Adduct.HasAdduct) {
                if (mgfRecord.IonMode== IonMode.Positive) {
                    adducttype = "[M+H]+";
                }
                else {
                    adducttype = "[M-H]-";
                }
            }
            else {
                adducttype = mgfRecord.Adduct.AdductIonName;
            }

            sw.WriteLine("NAME: " + mgfRecord.Title);
            sw.WriteLine("PRECURSORMZ: " + mgfRecord.Pepmass);
            sw.WriteLine("PRECURSORTYPE: " + adducttype);
            sw.WriteLine("RETENTIONTIME: " + mgfRecord.Rt);
            sw.WriteLine("IONMODE: " + mgfRecord.IonMode);
            sw.WriteLine("Num Peaks: " + mgfRecord.Spectrum.Count);

            foreach (var peak in mgfRecord.Spectrum)
            {
                sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
            }
            sw.WriteLine();
        }

    }
}
