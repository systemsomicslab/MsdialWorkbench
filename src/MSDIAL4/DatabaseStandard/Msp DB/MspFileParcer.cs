using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MspFileParcer
    {

        private MspFileParcer() { }
        
        /// <summary>
        /// This is the MSP file parcer.
        /// Each MS/MS information will be stored in MspFormatCompoundInformationBean.cs.
        /// </summary>
        /// <param name="databaseFilepath"></param>
        /// <returns></returns>
        public static List<MspFormatCompoundInformationBean> MspFileReader(string databaseFilepath)
        {
            var mspFields = new List<MspFormatCompoundInformationBean>();
            var mspField = new MspFormatCompoundInformationBean();
            var mspPeak = new MzIntensityCommentBean();
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
                        mspField.Id = counter;
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
                                mspField.Formula = getMspFieldValue(wkstr);
                                if (mspField.Formula != null && mspField.Formula != string.Empty)
                                    mspField.FormulaBean = FormulaStringParcer.OrganicElementsReader(mspField.Formula);
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
                                mspField.Smiles = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "InChIKey:.*", RegexOptions.IgnoreCase)) {
                                mspField.InchiKey = getMspFieldValue(wkstr);
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
                                if (float.TryParse(rtString, out rt)) mspField.RetentionTime = rt; else mspField.RetentionTime = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "COLLISIONENERGY:.*", RegexOptions.IgnoreCase)){ 
                                var collisionenergy = getMspFieldValue(wkstr);
                                if (float.TryParse(collisionenergy, out rt)) mspField.CollisionEnergy = collisionenergy;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RT:.*", RegexOptions.IgnoreCase)) {
                                var rtString = getMspFieldValue(wkstr);
                                if (float.TryParse(rtString, out rt)) mspField.RetentionTime = rt; else mspField.RetentionTime = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RETENTIONINDEX:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "Retention_index:.*", RegexOptions.IgnoreCase)) {
                                var rtString = getMspFieldValue(wkstr);
                                if (float.TryParse(rtString, out ri)) mspField.RetentionIndex = ri; else mspField.RetentionIndex = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RI:.*", RegexOptions.IgnoreCase)) {
                                var rtString = getMspFieldValue(wkstr);
                                if (float.TryParse(rtString, out ri)) mspField.RetentionIndex = ri; else mspField.RetentionIndex = -1;
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
                                mspField.AdductIonBean = AdductIonStringParser.GetAdductIonBean(fieldString);
                                if (mspField.AdductIonBean != null)
                                    mspField.IonMode = mspField.AdductIonBean.IonType == IonType.Positive ? IonMode.Positive : IonMode.Negative;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Links:.*", RegexOptions.IgnoreCase)) {
                                mspField.Links = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Intensity:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out intensity)) mspField.Intensity = intensity; else mspField.Intensity = -1;
                                continue;
                            }
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
                                mspField.Instrument = getMspFieldValue(wkstr);
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
                                mspField.MzIntensityCommentBeanList = ReadSpectrum(sr, wkstr, out peakNum);
                                mspField.PeakNumber = mspField.MzIntensityCommentBeanList.Count;
                                continue;
                            }
                        }
                        mspFields.Add(mspField);
                        mspField = new MspFormatCompoundInformationBean();
                        counter++;
                    }
                }
            }

            return mspFields;
        }

        private static string getMspFieldValue(string wkstr) {
            return wkstr.Substring(wkstr.Split(':')[0].Length + 1).Trim();
        }

        public static List<MspFormatCompoundInformationBean> FiehnGcmsMspReader(Stream stream)
        {
            var mspFields = new List<MspFormatCompoundInformationBean>();
            var mspField = new MspFormatCompoundInformationBean();
            var mspPeak = new MzIntensityCommentBean();
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
                        mspField.Id = counter;
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
                                mspField.InchiKey = wkstr.Split(':')[1].Trim();
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "InChI Key:.*", RegexOptions.IgnoreCase))
                            {
                                mspField.InchiKey = wkstr.Split(':')[1].Trim();
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RI:.*", RegexOptions.IgnoreCase))
                            {
                                if (float.TryParse(wkstr.Split(':')[1].Trim(), out ri)) mspField.RetentionIndex = ri; else mspField.RetentionIndex = -1;
                                mspField.RetentionTime = (float)(ri * 0.001 / 60.0);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Num Peaks:.*", RegexOptions.IgnoreCase))
                            {
                                if (int.TryParse(wkstr.Split(':')[1].Trim(), out num))
                                {
                                    mspField.PeakNumber = num;
                                    if (mspField.PeakNumber == 0) { mspField.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean()); continue; }

                                    pairCount = 0;
                                    mspPeak = new MzIntensityCommentBean();

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
                                                            mspPeak.Mz = float.Parse(numChar);
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
                                                                mspPeak.Comment = mspPeak.Mz.ToString();
                                                            mspField.MzIntensityCommentBeanList.Add(mspPeak);
                                                            mspPeak = new MzIntensityCommentBean();
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
                                                    mspField.MzIntensityCommentBeanList[mspField.MzIntensityCommentBeanList.Count - 1].Comment = letterChar;
                                                else
                                                {
                                                    mspField.MzIntensityCommentBeanList[mspField.MzIntensityCommentBeanList.Count - 1].Comment = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[0];
                                                    mspField.MzIntensityCommentBeanList[mspField.MzIntensityCommentBeanList.Count - 1].Frag = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[1];
                                                }

                                            }
                                            else
                                            {
                                                if (mzFill == false)
                                                {
                                                    if (numChar != string.Empty)
                                                    {
                                                        mspPeak.Mz = float.Parse(numChar);
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
                                                            mspPeak.Comment = mspPeak.Mz.ToString();

                                                        mspField.MzIntensityCommentBeanList.Add(mspPeak);
                                                        mspPeak = new MzIntensityCommentBean();
                                                        pairCount++;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    mspField.MzIntensityCommentBeanList = mspField.MzIntensityCommentBeanList.OrderBy(n => n.Mz).ToList();
                                }
                                continue;
                            }
                        }
                        mspFields.Add(mspField);
                        mspField = new MspFormatCompoundInformationBean();
                        counter++;
                    }
                }
            }
            return mspFields;
        }

        public static List<MzIntensityCommentBean> ReadSpectrum(StreamReader sr, string numPeakField, out int peaknum)
        {
            peaknum = 0;
            var mspPeaks = new List<MzIntensityCommentBean>();

            if (int.TryParse(numPeakField.Split(':')[1].Trim(), out peaknum))
            {
                if (peaknum == 0) { return mspPeaks; }

                var pairCount = 0;
                var mspPeak = new MzIntensityCommentBean();

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
                                        mspPeak.Mz = float.Parse(numChar);
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
                                            mspPeak.Comment = mspPeak.Mz.ToString();
                                        mspPeaks.Add(mspPeak);
                                        mspPeak = new MzIntensityCommentBean();
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
                                mspPeaks[mspPeaks.Count - 1].Frag = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[1];
                            }

                        }
                        else
                        {
                            if (mzFill == false)
                            {
                                if (numChar != string.Empty)
                                {
                                    mspPeak.Mz = float.Parse(numChar);
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
                                        mspPeak.Comment = mspPeak.Mz.ToString();

                                    mspPeaks.Add(mspPeak);
                                    mspPeak = new MzIntensityCommentBean();
                                    pairCount++;
                                }
                            }
                        }
                    }
                }

                mspPeaks = mspPeaks.OrderBy(n => n.Mz).ToList();
            }

            return mspPeaks;
        }

        public static List<Peak> ReadPeaks(StreamReader sr, string numPeakField, out int peaknum)
        {
            peaknum = 0;
            var mspPeaks = new List<Peak>();

            if (int.TryParse(numPeakField.Split(':')[1].Trim(), out peaknum))
            {
                if (peaknum == 0) { return mspPeaks; }

                var pairCount = 0;
                var mspPeak = new Peak();

                while (pairCount < peaknum)
                {
                    var wkstr = sr.ReadLine();
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
                                        mspPeak.Mz = float.Parse(numChar);
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
                                            mspPeak.Comment = mspPeak.Mz.ToString();
                                        mspPeaks.Add(mspPeak);
                                        mspPeak = new Peak();
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
                            mspPeaks[mspPeaks.Count - 1].Comment = letterChar;
                        }
                        else
                        {
                            if (mzFill == false)
                            {
                                if (numChar != string.Empty)
                                {
                                    mspPeak.Mz = float.Parse(numChar);
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
                                        mspPeak.Comment = mspPeak.Mz.ToString();

                                    mspPeaks.Add(mspPeak);
                                    mspPeak = new Peak();
                                    pairCount++;
                                }
                            }
                        }
                    }
                }

                mspPeaks = mspPeaks.OrderBy(n => n.Mz).ToList();
            }

            return mspPeaks;
        }

        public static List<Peak> ConvertToPeakObject(List<MzIntensityCommentBean> peakPairs)
        {
            if (peakPairs == null || peakPairs.Count == 0) return new List<Peak>();
            var peaks = new List<Peak>();

            foreach (var peak in peakPairs) {
                if (peak.Intensity < 0) continue;
                var nPeak = new Peak() {
                    Mz = peak.Mz,
                    Intensity = Math.Sqrt(peak.Intensity),
                    Comment = peak.Comment
                };
                peaks.Add(nPeak);
            }
            return peaks;
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

        public static void WriteAsSeparatedMSPs(string folderpath, List<MspFormatCompoundInformationBean> mspRecords) {
            var invalidChars = Path.GetInvalidFileNameChars();
            var counter = 0;
            foreach (var record in mspRecords) {

                var filename = record.Name;
                if (filename == string.Empty)
                    filename = "Query_" + counter;
                var converted = string.Concat(
                  filename.Select(c => invalidChars.Contains(c) ? '_' : c));

                var filepath = Path.Combine(folderpath, counter.ToString("00000") + "_" + converted + ".msp");
                if (CompMs.Common.Utility.ErrorHandler.IsExceedFilePathMax(filepath, folderpath, counter.ToString("00000") + "_" + converted, ".msp", out string recFilePath)) {
                    filepath = recFilePath;
                }
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    writeMspFields(record, sw);
                }
                counter++;
            }
        }

        private static void writeMspFields(MspFormatCompoundInformationBean record, StreamWriter sw) {

            var adducttype = string.Empty;
            if (record.AdductIonBean == null || record.AdductIonBean.AdductIonName == null || record.AdductIonBean.AdductIonName == string.Empty) {
                if (record.IonMode == IonMode.Positive) {
                    adducttype = "[M+H]+";
                }
                else {
                    adducttype = "[M-H]-";
                }
            }
            else {
                adducttype = record.AdductIonBean.AdductIonName;
            }


            sw.WriteLine("NAME: " + record.Name);
            sw.WriteLine("PRECURSORMZ: " + record.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + adducttype);
            sw.WriteLine("RETENTIONTIME: " + record.RetentionTime);
            sw.WriteLine("FORMULA: " + record.Formula);
            sw.WriteLine("SMILES: " + record.Smiles);
            sw.WriteLine("INCHIKEY: " + record.InchiKey);
            sw.WriteLine("COLLISIONENERGY: " + record.CollisionEnergy);
            sw.WriteLine("IONMODE: " + record.IonMode);
            sw.WriteLine("Comment: " + record.Comment);
            sw.WriteLine("Num Peaks: " + record.MzIntensityCommentBeanList.Count);

            foreach (var peak in record.MzIntensityCommentBeanList) {
                if (string.IsNullOrEmpty(peak.Comment)) {
                    sw.WriteLine(peak.Mz + "\t" + peak.Intensity);
                }
                else {
                    // tantative version; if user want to split exported msp file by MS-FINDER
                    if (peak.Comment.Contains(";")) {
                        sw.WriteLine(peak.Mz + "\t" + peak.Intensity + "\t" + "\"" + peak.Comment.Split(';')[0] + "\"");
                    }
                    else {
                        sw.WriteLine(peak.Mz + "\t" + peak.Intensity);
                    }
                }
            }
            sw.WriteLine();
        }
    }
}
