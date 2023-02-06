using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsdialPrivateConsoleApp {

    public class CtracePeakProperty {
        public string PlantID { get; set; }
        public string PlantMaxIntensity { get; set; }
        public double MZ { get; set; }
        public double RT { get; set; }
        public AdductIon Adduct { get; set; }
        public int CarbonNumber { get; set; }
        public int MsiLevel { get; set; }
        public bool? IsCorrect { get; set; }
    }

    public sealed class CtraceCorrectCheck {
        private CtraceCorrectCheck() { }

        public static void CheckingCtraceCount(string correctFile, string datafolder, string output) {

            var peakPropeties = new List<CtracePeakProperty>();

            using (var sr = new StreamReader(correctFile, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');

                    var property = new CtracePeakProperty() {
                        PlantID = lineArray[0],
                        PlantMaxIntensity = lineArray[1],
                        MZ = double.Parse(lineArray[2]),
                        RT = double.Parse(lineArray[3]),
                        Adduct = AdductIonParcer.GetAdductIonBean(lineArray[4]),
                        CarbonNumber = int.Parse(lineArray[5]),
                        MsiLevel = int.Parse(lineArray[6])
                    };
                    peakPropeties.Add(property);
                }
            }

            var analyzedFiles = System.IO.Directory.GetFiles(datafolder);
            foreach (var peakProp in peakPropeties) {
                //var fileNameWithID = peakProp.PlantID.Split('-');
                //var fileName = fileNameWithID[0].Split('_');
                //var id = fileNameWithID[1];
                var fileName = peakProp.PlantMaxIntensity.Split('_');
                var filenamePlant = fileName[0];
                var filenameTissue = fileName[1];
                var filenameIonmode = fileName[2];

                foreach (var analyzedFile in analyzedFiles) {
                    var analysisFileName = System.IO.Path.GetFileNameWithoutExtension(analyzedFile).Split('_');
                    var analysisFilenamePlant = analysisFileName[0];
                    var analysisFilenameTissue = analysisFileName[1];
                    var analysisFilenameIonmode = analysisFileName[2];

                    if (filenamePlant == analysisFilenamePlant &&
                        filenameTissue == analysisFilenameTissue &&
                        filenameIonmode == analysisFilenameIonmode) {

                        using (var sr = new StreamReader(analyzedFile, Encoding.ASCII)) {
                            sr.ReadLine();
                            while (sr.Peek() > -1) {

                                var line = sr.ReadLine();
                                var lineArray = line.Split('\t');

                                var msdialID = lineArray[0];
                                var mdMz = double.Parse(lineArray[1]);
                                var mdRt = double.Parse(lineArray[2]);
                                var msdialCarbonNumber = int.Parse(lineArray[3]);

                                if (Math.Abs(peakProp.RT - mdRt) < 0.3 &&
                                    Math.Abs(peakProp.MZ - mdMz) < 0.015) {
                                    if (peakProp.CarbonNumber == msdialCarbonNumber) {
                                        peakProp.IsCorrect = true;
                                    }
                                    else {
                                        if (peakProp.IsCorrect == null)
                                            peakProp.IsCorrect = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Plant ID\tPlant having max intensity\tm/z\tRT\tAdduct\tCarbon count\tMSI level\tIsCorrect");
                foreach (var peakProp in peakPropeties) {
                    sw.Write(peakProp.PlantID + "\t");
                    sw.Write(peakProp.PlantMaxIntensity + "\t");
                    sw.Write(peakProp.MZ + "\t");
                    sw.Write(peakProp.RT + "\t");
                    sw.Write(peakProp.Adduct.AdductIonName + "\t");
                    sw.Write(peakProp.CarbonNumber + "\t");
                    sw.Write(peakProp.MsiLevel + "\t");
                    sw.WriteLine(peakProp.IsCorrect);
                }
            }
        }

        public static void CheckingAdductContent(string correctFile, string datafolder, string output) {
            var peakPropeties = new List<CtracePeakProperty>();

            using (var sr = new StreamReader(correctFile, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');

                    var property = new CtracePeakProperty() {
                        PlantID = lineArray[0],
                        PlantMaxIntensity = lineArray[1],
                        MZ = double.Parse(lineArray[2]),
                        RT = double.Parse(lineArray[3]),
                        Adduct = AdductIonParcer.GetAdductIonBean(lineArray[4]),
                        CarbonNumber = int.Parse(lineArray[5]),
                        MsiLevel = int.Parse(lineArray[6])
                    };
                    peakPropeties.Add(property);
                }
            }

            var analyzedFiles = System.IO.Directory.GetFiles(datafolder);

            foreach (var peakProp in peakPropeties) {
                //var fileNameWithID = peakProp.PlantID.Split('-');
                //var fileName = fileNameWithID[0].Split('_');
                //var id = fileNameWithID[1];

                var fileName = peakProp.PlantMaxIntensity.Split('_');
                var filenamePlant = fileName[0];
                var filenameTissue = fileName[1];
                var filenameIonmode = fileName[2];
                foreach (var analyzedFile in analyzedFiles) {
                    var analysisFileName = System.IO.Path.GetFileNameWithoutExtension(analyzedFile).Split('_');
                    var analysisFilenamePlant = analysisFileName[0];
                    var analysisFilenameTissue = analysisFileName[1];
                    var analysisFilenameIonmode = analysisFileName[2];

                    if (filenamePlant == analysisFilenamePlant &&
                        filenameTissue == analysisFilenameTissue &&
                        filenameIonmode == analysisFilenameIonmode) {

                        using (var sr = new StreamReader(analyzedFile, Encoding.ASCII)) {
                            sr.ReadLine();
                            while (sr.Peek() > -1) {
                                var line = sr.ReadLine();
                                var lineArray = line.Split('\t');

                                var mdRt = double.Parse(lineArray[3]);
                                var mdMz = double.Parse(lineArray[4]);
                                var mdAdduct = AdductIonParcer.GetAdductIonBean(lineArray[9]);

                                if (Math.Abs(peakProp.RT - mdRt) < 0.15 &&
                                    Math.Abs(peakProp.MZ - mdMz) < 0.015) {
                                    if (Math.Abs(peakProp.Adduct.AdductIonAccurateMass - mdAdduct.AdductIonAccurateMass) < 0.01) {
                                        peakProp.IsCorrect = true;
                                    }
                                    else {
                                        if (peakProp.IsCorrect == null)
                                            peakProp.IsCorrect = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Plant ID\tPlant having max intensity\tm/z\tRT\tAdduct\tCarbon count\tMSI level\tIsCorrect");
                foreach (var peakProp in peakPropeties) {
                    sw.Write(peakProp.PlantID + "\t");
                    sw.Write(peakProp.PlantMaxIntensity + "\t");
                    sw.Write(peakProp.MZ + "\t");
                    sw.Write(peakProp.RT + "\t");
                    sw.Write(peakProp.Adduct.AdductIonName + "\t");
                    sw.Write(peakProp.CarbonNumber + "\t");
                    sw.Write(peakProp.MsiLevel + "\t");
                    sw.WriteLine(peakProp.IsCorrect);
                }
            }
        }

        public static void CheckingAdductContentOfXcmsCamera(string correctFile, string datafolder, string output) {
            var peakPropeties = new List<CtracePeakProperty>();

            using (var sr = new StreamReader(correctFile, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');

                    var property = new CtracePeakProperty() {
                        PlantID = lineArray[0],
                        PlantMaxIntensity = lineArray[1],
                        MZ = double.Parse(lineArray[2]),
                        RT = double.Parse(lineArray[3]),
                        Adduct = AdductIonParcer.GetAdductIonBean(lineArray[4]),
                        CarbonNumber = int.Parse(lineArray[5]),
                        MsiLevel = int.Parse(lineArray[6])
                    };
                    peakPropeties.Add(property);
                }
            }

            var analyzedFiles = System.IO.Directory.GetFiles(datafolder);

            foreach (var peakProp in peakPropeties) {
                var fileName = peakProp.PlantMaxIntensity.Split('_');
                var filenamePlant = fileName[0];
                var filenameTissue = fileName[1];
                var filenameIonmode = fileName[2];
                foreach (var analyzedFile in analyzedFiles) {
                    var analysisFileName = System.IO.Path.GetFileNameWithoutExtension(analyzedFile).Split('-');
                    var analysisFilenamePlant = analysisFileName[1];
                    var analysisFilenameTissue = analysisFileName[2];
                    var analysisFilenameIonmode = analysisFileName[3];

                    if (filenamePlant == analysisFilenamePlant &&
                        filenameTissue == analysisFilenameTissue &&
                        filenameIonmode == analysisFilenameIonmode) {

                        using (var sr = new StreamReader(analyzedFile, Encoding.ASCII)) {
                            sr.ReadLine();
                            while (sr.Peek() > -1) {
                                var line = sr.ReadLine();
                                var lineArray = line.Split(' ');

                                var mdRt = double.Parse(lineArray[4]) / 60;
                                var mdMz = double.Parse(lineArray[1]);

                                var adductString = string.Empty;
                             
                                var doublequatCounter = 0;
                                for (int i = 0; i < line.Length; i++) {
                                    if (line[i] == '"')
                                        doublequatCounter++;
                                    if (doublequatCounter == 5) {
                                        if (line[i + 1] == '"') break;
                                        var laddctString = string.Empty;
                                        for (int j = i + 1; j < line.Length; j++) {
                                            if (line[j] == ' ') break;
                                            laddctString += line[j];
                                        }
                                        adductString = laddctString;
                                    }

                                    if (doublequatCounter == 9) {
                                        if (line[i + 1] == '"') break;
                                        var laddctString = string.Empty;
                                        for (int j = i + 7; j < line.Length; j++) {
                                            if (line[j] == ' ' || line[j] == '"') break;
                                            laddctString += line[j];
                                        }

                                        if (filenameIonmode == "Pos")
                                            adductString = laddctString.Split('/')[0];
                                        else
                                            adductString = laddctString.Split('/')[1];
                                    }
                                }
                                if (adductString == string.Empty) {
                                    if (filenameIonmode == "Pos")
                                        adductString = "[M+H]+";
                                    else
                                        adductString = "[M-H]-";
                                }



                                var mdAdduct = AdductIonParcer.GetAdductIonBean(adductString);

                                if (Math.Abs(peakProp.RT - mdRt) < 0.3 &&
                                    Math.Abs(peakProp.MZ - mdMz) < 0.015) {
                                    if (Math.Abs(peakProp.Adduct.AdductIonAccurateMass - mdAdduct.AdductIonAccurateMass) < 0.01) {
                                        peakProp.IsCorrect = true;
                                    }
                                    else {
                                        if (peakProp.IsCorrect == null)
                                            peakProp.IsCorrect = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Plant ID\tPlant having max intensity\tm/z\tRT\tAdduct\tCarbon count\tMSI level\tIsCorrect");
                foreach (var peakProp in peakPropeties) {
                    sw.Write(peakProp.PlantID + "\t");
                    sw.Write(peakProp.PlantMaxIntensity + "\t");
                    sw.Write(peakProp.MZ + "\t");
                    sw.Write(peakProp.RT + "\t");
                    sw.Write(peakProp.Adduct.AdductIonName + "\t");
                    sw.Write(peakProp.CarbonNumber + "\t");
                    sw.Write(peakProp.MsiLevel + "\t");
                    sw.WriteLine(peakProp.IsCorrect);
                }
            }
        }

        public static void CheckingInsourceFragment(string correctFile, string datafolder, string output) {
            var peakPropeties = new List<CtracePeakProperty>();

            using (var sr = new StreamReader(correctFile, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');

                    var property = new CtracePeakProperty() {
                        PlantID = lineArray[0],
                        PlantMaxIntensity = lineArray[1],
                        MZ = double.Parse(lineArray[2]),
                        RT = double.Parse(lineArray[3]),
                    };
                    peakPropeties.Add(property);
                }
            }

            var analyzedFiles = System.IO.Directory.GetFiles(datafolder);

            foreach (var peakProp in peakPropeties) {
                //var fileNameWithID = peakProp.PlantID.Split('-');
                //var fileName = fileNameWithID[0].Split('_');
                //var id = fileNameWithID[1];

                var fileName = peakProp.PlantMaxIntensity.Split('_');

                var filenamePlant = fileName[0];
                var filenameTissue = fileName[1];
                var filenameIonmode = fileName[2];
                foreach (var analyzedFile in analyzedFiles) {
                    var analysisFileName = System.IO.Path.GetFileNameWithoutExtension(analyzedFile).Split('_');
                    var analysisFilenamePlant = analysisFileName[0];
                    var analysisFilenameTissue = analysisFileName[1];
                    var analysisFilenameIonmode = analysisFileName[2];

                    if (filenamePlant == analysisFilenamePlant &&
                        filenameTissue == analysisFilenameTissue &&
                        filenameIonmode == analysisFilenameIonmode) {

                        using (var sr = new StreamReader(analyzedFile, Encoding.ASCII)) {
                            sr.ReadLine();
                            while (sr.Peek() > -1) {
                                var line = sr.ReadLine();
                                var lineArray = line.Split('\t');

                                var mdRt = double.Parse(lineArray[3]);
                                var mdMz = double.Parse(lineArray[4]);
                                var mdComment = lineArray[11];

                                if (Math.Abs(peakProp.RT - mdRt) < 0.2 &&
                                    Math.Abs(peakProp.MZ - mdMz) < 0.015) {
                                    if (mdComment != string.Empty) {
                                        peakProp.IsCorrect = true;
                                    }
                                    else {
                                        if (peakProp.IsCorrect == null)
                                            peakProp.IsCorrect = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Plant ID\tPlant having max intensity\tm/z\tRT\tIsCorrect");
                foreach (var peakProp in peakPropeties) {
                    sw.Write(peakProp.PlantID + "\t");
                    sw.Write(peakProp.PlantMaxIntensity + "\t");
                    sw.Write(peakProp.MZ + "\t");
                    sw.Write(peakProp.RT + "\t");
                    sw.WriteLine(peakProp.IsCorrect);
                }
            }
        }


    }
}
