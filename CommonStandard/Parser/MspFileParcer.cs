using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompMs.Common.Parser {
    public sealed class MspFileParcer
    {

        private MspFileParcer() { }
        
        /// <summary>
        /// This is the MSP file parcer.
        /// Each MS/MS information will be stored in MspFormatCompoundInformationBean.cs.
        /// </summary>
        /// <param name="databaseFilepath"></param>
        /// <returns></returns>
        public static List<MoleculeMsReference> MspFileReader(string databaseFilepath)
        {
            var mspFields = new List<MoleculeMsReference>();
            var mspField = new MoleculeMsReference();
            var mspPeak = new SpectrumPeak();
            string wkstr;
            int counter = 0, pairCount = 0;

            using (StreamReader sr = new StreamReader(databaseFilepath, Encoding.ASCII))
            {
                float rt = 0, preMz = 0, ri = 0, intensity = 0;

                while (sr.Peek() > -1)
                {
                    wkstr = sr.ReadLine();
                    if (Regex.IsMatch(wkstr, "^NAME:.*", RegexOptions.IgnoreCase))
                    {
                        mspField.ScanID = counter;
                        mspField.Name = getMspFieldValue(wkstr);

                        while (sr.Peek() > -1)
                        {
                            wkstr = sr.ReadLine();
                            if (wkstr == string.Empty || String.IsNullOrWhiteSpace(wkstr)) break;
                            if (Regex.IsMatch(wkstr, "COMMENT.*:.*", RegexOptions.IgnoreCase)) {
                                mspField.Comment = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "FORMULA:.*", RegexOptions.IgnoreCase)) {
                                var formulaString = getMspFieldValue(wkstr);
                                if (formulaString != null && formulaString != string.Empty)
                                    mspField.Formula = FormulaStringParcer.OrganicElementsReader(formulaString);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "IONMODE:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "ion_mode:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "Ionization:.*", RegexOptions.IgnoreCase)) {
                                var ionmodeString = getMspFieldValue(wkstr);
                                if (ionmodeString == "Negative" ||
                                    ionmodeString.Contains("N")) mspField.IonMode = IonMode.Negative;
                                else mspField.IonMode = IonMode.Positive;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "SMILES:.*", RegexOptions.IgnoreCase)) {
                                mspField.SMILES = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "InChIKey:.*", RegexOptions.IgnoreCase)) {
                                mspField.InChIKey = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "COMPOUNDCLASS:.*", RegexOptions.IgnoreCase)) {
                                mspField.CompoundClass = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Ontology:.*", RegexOptions.IgnoreCase)) {
                                mspField.Ontology = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RETENTIONTIME:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "Retention_time:.*", RegexOptions.IgnoreCase)) {
                                var rtString = getMspFieldValue(wkstr);
                                rt = -1;
                                float.TryParse(rtString, out rt);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "COLLISIONENERGY:.*", RegexOptions.IgnoreCase)){ 

                                var collisionenergy = -1.0F;
                                if (float.TryParse(getMspFieldValue(wkstr), out collisionenergy)) mspField.CollisionEnergy = collisionenergy;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RT:.*", RegexOptions.IgnoreCase)) {
                                var rtString = getMspFieldValue(wkstr);
                                rt = -1;
                                float.TryParse(rtString, out rt);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RETENTIONINDEX:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "Retention_index:.*", RegexOptions.IgnoreCase)) {
                                var rtString = getMspFieldValue(wkstr);
                                ri = -1;
                                float.TryParse(rtString, out ri);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RI:.*", RegexOptions.IgnoreCase)) {
                                var rtString = getMspFieldValue(wkstr);
                                ri = -1;
                                float.TryParse(rtString, out ri);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORMZ:.*", RegexOptions.IgnoreCase) || Regex.IsMatch(wkstr, "precursor_m/z:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out preMz)) mspField.PrecursorMz = preMz;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "CollisionCrossSection:.*", RegexOptions.IgnoreCase) || Regex.IsMatch(wkstr, "CCS:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out preMz)) mspField.CollisionCrossSection = preMz;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORTYPE:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "Precursor_type:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                mspField.AdductType = AdductIonParser.GetAdductIonBean(fieldString);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Links:.*", RegexOptions.IgnoreCase)) {
                                mspField.Links = getMspFieldValue(wkstr);
                                continue;
                            }
                            //else if (Regex.IsMatch(wkstr, "Intensity:.*", RegexOptions.IgnoreCase)) {
                            //    var fieldString = getMspFieldValue(wkstr);
                            //    if (float.TryParse(fieldString, out intensity)) mspField.Intensity = intensity; else mspField.Intensity = -1;
                            //    continue;
                            //}
                            else if (Regex.IsMatch(wkstr, "SCANNUMBER:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                mspField.Comment += fieldString;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "INSTRUMENTTYPE:.*", RegexOptions.IgnoreCase)) {
                                mspField.InstrumentType = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "INSTRUMENT:.*", RegexOptions.IgnoreCase)) {
                                mspField.InstrumentModel = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "QuantMass:.*", RegexOptions.IgnoreCase)) {
                                float quantmass = -1;
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out quantmass))
                                    mspField.QuantMass = quantmass;
                                else
                                    mspField.QuantMass = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Num Peaks:.*", RegexOptions.IgnoreCase)) {
                                var peakNum = 0;
                                mspField.Spectrum = ReadSpectrum(sr, wkstr, out peakNum);
                               // mspField.PeakNumber = mspField.MzIntensityCommentBeanList.Count;
                                continue;
                            }
                        }
                        mspField.ChromXs = new ChromXs() {
                            RT = new RetentionTime(rt), RI = new RetentionIndex(ri), Drift = new DriftTime(-1), MainType = ChromXType.RT
                        };
                        mspFields.Add(mspField);
                        mspField = new MoleculeMsReference();
                        counter++;
                    }
                }
            }

            return mspFields;
        }

        private static string getMspFieldValue(string wkstr) {
            return wkstr.Substring(wkstr.Split(':')[0].Length + 1).Trim();
        }

        public static List<MoleculeMsReference> FiehnGcmsMspReader(Stream stream)
        {
            var mspFields = new List<MoleculeMsReference>();
            var mspField = new MoleculeMsReference();
            var mspPeak = new SpectrumPeak();
            string wkstr;
            int counter = 0, pairCount = 0;

            using (var sr = new StreamReader(stream))
            {
                float rt = 0, preMz = 0, ri = 0;
                int num;
                string numChar, letterChar;
                bool mzFill;

                while (sr.Peek() > -1)
                {
                    wkstr = sr.ReadLine();
                    if (Regex.IsMatch(wkstr, "^Name:.*", RegexOptions.IgnoreCase))
                    {
                        mspField.Name = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                        mspField.IonMode = IonMode.Positive;
                        mspField.ScanID = counter;
                        while (sr.Peek() > -1)
                        {
                            wkstr = sr.ReadLine();
                            if (wkstr == string.Empty) break;
                            if (Regex.IsMatch(wkstr, "BinId:.*", RegexOptions.IgnoreCase))
                            {
                                num = counter;
                                if (int.TryParse(wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim(), out num))
                                    mspField.BinId = num;
                                else
                                    mspField.BinId = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "InChIKey:.*", RegexOptions.IgnoreCase))
                            {
                                mspField.InChIKey = wkstr.Split(':')[1].Trim();
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "InChI Key:.*", RegexOptions.IgnoreCase))
                            {
                                mspField.InChIKey = wkstr.Split(':')[1].Trim();
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RI:.*", RegexOptions.IgnoreCase))
                            {
                                ri = -1; rt = -1;
                                if (float.TryParse(wkstr.Split(':')[1].Trim(), out ri)) {
                                    rt = (float)(ri * 0.001 / 60.0);
                                }
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Num Peaks:.*", RegexOptions.IgnoreCase))
                            {
                                if (int.TryParse(wkstr.Split(':')[1].Trim(), out num))
                                {
                                    if (num == 0) continue; 

                                    pairCount = 0;
                                    mspPeak = new SpectrumPeak();

                                    while (pairCount < num)
                                    {
                                        wkstr = sr.ReadLine();
                                        numChar = string.Empty;
                                        mzFill = false;
                                        for (int i = 0; i < wkstr.Length; i++)
                                        {
                                            if (char.IsNumber(wkstr[i]) || wkstr[i] == '.')
                                            {
                                                numChar += wkstr[i];

                                                if (i == wkstr.Length - 1 && wkstr[i] != '"')
                                                {
                                                    if (mzFill == false)
                                                    {
                                                        if (numChar != string.Empty)
                                                        {
                                                            mspPeak.Mass = float.Parse(numChar);
                                                            mzFill = true;
                                                            numChar = string.Empty;
                                                        }
                                                    }
                                                    else if (mzFill == true)
                                                    {
                                                        if (numChar != string.Empty)
                                                        {
                                                            mspPeak.Intensity = float.Parse(numChar);
                                                            mzFill = false;
                                                            numChar = string.Empty;

                                                            if (mspPeak.Comment == null)
                                                                mspPeak.Comment = mspPeak.Mass.ToString();
                                                            mspField.Spectrum.Add(mspPeak);
                                                            mspPeak = new SpectrumPeak();
                                                            pairCount++;
                                                        }
                                                    }
                                                }
                                            }
                                            else if (wkstr[i] == '"')
                                            {
                                                i++;
                                                letterChar = string.Empty;

                                                while (wkstr[i] != '"')
                                                {
                                                    letterChar += wkstr[i];
                                                    i++;
                                                }
                                                if (!letterChar.Contains("_f_"))
                                                    mspField.Spectrum[mspField.Spectrum.Count - 1].Comment = letterChar;
                                                else
                                                {
                                                    mspField.Spectrum[mspField.Spectrum.Count - 1].Comment = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[0];
                                                   // mspField.Spectrum[mspField.Spectrum.Count - 1].Frag = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[1];
                                                }

                                            }
                                            else
                                            {
                                                if (mzFill == false)
                                                {
                                                    if (numChar != string.Empty)
                                                    {
                                                        mspPeak.Mass = float.Parse(numChar);
                                                        mzFill = true;
                                                        numChar = string.Empty;
                                                    }
                                                }
                                                else if (mzFill == true)
                                                {
                                                    if (numChar != string.Empty)
                                                    {
                                                        mspPeak.Intensity = float.Parse(numChar);
                                                        mzFill = false;
                                                        numChar = string.Empty;

                                                        if (mspPeak.Comment == null)
                                                            mspPeak.Comment = mspPeak.Mass.ToString();

                                                        mspField.Spectrum.Add(mspPeak);
                                                        mspPeak = new SpectrumPeak();
                                                        pairCount++;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    mspField.Spectrum = mspField.Spectrum.OrderBy(n => n.Mass).ToList();
                                }
                                continue;
                            }
                        }
                        mspField.ChromXs = new ChromXs() {
                            RT = new RetentionTime(rt), RI = new RetentionIndex(ri), Drift = new DriftTime(-1), MainType = ChromXType.RT
                        };
                        mspFields.Add(mspField);
                        mspField = new MoleculeMsReference();
                        counter++;
                    }
                }
            }
            return mspFields;
        }

        public static List<SpectrumPeak> ReadSpectrum(StreamReader sr, string numPeakField, out int peaknum)
        {
            peaknum = 0;
            var mspPeaks = new List<SpectrumPeak>();

            if (int.TryParse(numPeakField.Split(':')[1].Trim(), out peaknum))
            {
                if (peaknum == 0) { return mspPeaks; }

                var pairCount = 0;
                var mspPeak = new SpectrumPeak();

                while (pairCount < peaknum)
                {
                    var wkstr = sr.ReadLine();
                    if (wkstr == null) break;
                    if (wkstr == string.Empty) break;
                    var numChar = string.Empty;
                    var mzFill = false;
                    
                    for (int i = 0; i < wkstr.Length; i++)
                    {
                        if (char.IsNumber(wkstr[i]) || wkstr[i] == '.')
                        {
                            numChar += wkstr[i];

                            if (i == wkstr.Length - 1 && wkstr[i] != '"')
                            {
                                if (mzFill == false)
                                {
                                    if (numChar != string.Empty)
                                    {
                                        mspPeak.Mass = float.Parse(numChar);
                                        mzFill = true;
                                        numChar = string.Empty;
                                    }
                                }
                                else if (mzFill == true)
                                {
                                    if (numChar != string.Empty)
                                    {
                                        mspPeak.Intensity = float.Parse(numChar);
                                        mzFill = false;
                                        numChar = string.Empty;

                                        if (mspPeak.Comment == null)
                                            mspPeak.Comment = mspPeak.Mass.ToString();
                                        mspPeaks.Add(mspPeak);
                                        mspPeak = new SpectrumPeak();
                                        pairCount++;
                                    }
                                }
                            }
                        }
                        else if (wkstr[i] == '"')
                        {
                            i++;
                            var letterChar = string.Empty;

                            while (wkstr[i] != '"')
                            {
                                letterChar += wkstr[i];
                                i++;
                            }
                            if (!letterChar.Contains("_f_"))
                                mspPeaks[mspPeaks.Count - 1].Comment = letterChar;
                            else
                            {
                                mspPeaks[mspPeaks.Count - 1].Comment = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[0];
                                //mspPeaks[mspPeaks.Count - 1].Frag = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[1];
                            }

                        }
                        else
                        {
                            if (mzFill == false)
                            {
                                if (numChar != string.Empty)
                                {
                                    mspPeak.Mass = float.Parse(numChar);
                                    mzFill = true;
                                    numChar = string.Empty;
                                }
                            }
                            else if (mzFill == true)
                            {
                                if (numChar != string.Empty)
                                {
                                    mspPeak.Intensity = float.Parse(numChar);
                                    mzFill = false;
                                    numChar = string.Empty;

                                    if (mspPeak.Comment == null)
                                        mspPeak.Comment = mspPeak.Mass.ToString();

                                    mspPeaks.Add(mspPeak);
                                    mspPeak = new SpectrumPeak();
                                    pairCount++;
                                }
                            }
                        }
                    }
                }

                mspPeaks = mspPeaks.OrderBy(n => n.Mass).ToList();
            }

            return mspPeaks;
        }

        public static Dictionary<int, float> GetFiehnFamesDictionary()
        {
            return new Dictionary<int, float>()
            {
                { 8, 262320.0F },
                { 9, 323120.0F },
                { 10, 381020.0F },
                { 12, 487220.0F },
                { 14, 582620.0F },
                { 16, 668720.0F },
                { 18, 747420.0F },
                { 20, 819620.0F },
                { 22, 886620.0F },
                { 24, 948820.0F },
                { 26, 1006900.0F },
                { 28, 1061700.0F },
                { 30, 1113100.0F },
            };
        }

        public static void ConvertMspToSeparatedMSPs(string filepath, string folderpath) {
            var mspRecords = MspFileReader(filepath);
            WriteAsSeparatedMSPs(folderpath, mspRecords);
        }

        public static void WriteAsSeparatedMSPs(string folderpath, List<MoleculeMsReference> mspRecords) {
            var invalidChars = Path.GetInvalidFileNameChars();
            var counter = 0;
            foreach (var record in mspRecords) {
                var filename = record.Name;
                if (filename == string.Empty)
                    filename = "Query_" + counter;
                var converted = string.Concat(
                  filename.Select(c => invalidChars.Contains(c) ? '_' : c));

                var filepath = folderpath + "\\" + counter.ToString("0000") + "_" + converted + ".msp";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    writeMspFields(record, sw);
                }
                counter++;
            }
        }

        private static void writeMspFields(MoleculeMsReference record, StreamWriter sw) {

            var adducttype = string.Empty;
            if (record.AdductType == null || record.AdductType.AdductIonName == null || record.AdductType.AdductIonName == string.Empty) {
                if (record.IonMode == IonMode.Positive) {
                    adducttype = "[M+H]+";
                }
                else {
                    adducttype = "[M-H]-";
                }
            }
            else {
                adducttype = record.AdductType.AdductIonName;
            }


            sw.WriteLine("NAME: " + record.Name);
            sw.WriteLine("PRECURSORMZ: " + record.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + adducttype);
            sw.WriteLine("RETENTIONTIME: " + record.ChromXs.RT);
            sw.WriteLine("FORMULA: " + record.Formula);
            sw.WriteLine("SMILES: " + record.SMILES);
            sw.WriteLine("INCHIKEY: " + record.InChIKey);
            sw.WriteLine("COLLISIONENERGY: " + record.CollisionEnergy);
            sw.WriteLine("IONMODE: " + record.IonMode);
            sw.WriteLine("Comment: " + record.Comment);
            sw.WriteLine("Num Peaks: " + record.Spectrum.Count);

            foreach (var peak in record.Spectrum) {
                if (string.IsNullOrEmpty(peak.Comment)) {
                    sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                }
                else {
                    // tantative version; if user want to split exported msp file by MS-FINDER
                    if (peak.Comment.Contains(";")) {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity + "\t" + "\"" + peak.Comment.Split(';')[0] + "\"");
                    }
                    else {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                    }
                }
            }
            sw.WriteLine();
        }
    }
}
