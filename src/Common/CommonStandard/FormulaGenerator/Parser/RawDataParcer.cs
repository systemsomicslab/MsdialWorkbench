using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.FormulaGenerator.Parser {
    public sealed class RawDataParcer
    {
        private RawDataParcer() { }

        /// <summary>
        /// This parcer is used to get the storage of RawData.cs.
        /// The result, i.e. RawData bean, will be used to calculate molecular formula and compound structures in MS-FINDER program.
        /// </summary>
        /// <param name="filePath">Storage file path (ASCII)</param>
        /// <returns></returns>
        public static RawData RawDataFileReader(string filePath, AnalysisParamOfMsfinder param)
        {
            RawData rawData = new RawData() { RawdataFilePath = filePath };

            string wkstr;
            using (StreamReader sr = new StreamReader(filePath))
            {
                while (sr.Peek() > -1)
                {
                    wkstr = sr.ReadLine();
                    #region parcer
                    if (Regex.IsMatch(wkstr, "NAME:", RegexOptions.IgnoreCase) && !Regex.IsMatch(wkstr, "METABOLITENAME:", RegexOptions.IgnoreCase))
                    {
                        rawData.Name = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "SCANNUMBER:", RegexOptions.IgnoreCase))
                    {
                        int scan;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out scan)) rawData.ScanNumber = scan; else rawData.ScanNumber = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "RETENTIONTIME:", RegexOptions.IgnoreCase))
                    {
                        double rt;
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out rt)) rawData.RetentionTime = rt; else rawData.RetentionTime = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "CCS:", RegexOptions.IgnoreCase)) {
                        double ccs;
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out ccs)) rawData.Ccs = ccs; else rawData.Ccs = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "RETENTIONINDEX:", RegexOptions.IgnoreCase)) {
                        double rt;
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out rt)) rawData.RetentionIndex = rt; else rawData.RetentionIndex = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "PRECURSORMZ:", RegexOptions.IgnoreCase))
                    {
                        double mz;
                        if (double.TryParse(wkstr.Split(':')[1].Trim(), out mz)) rawData.PrecursorMz = mz;
                    }
                    else if (Regex.IsMatch(wkstr, "PRECURSORTYPE:", RegexOptions.IgnoreCase))
                    {
                        rawData.PrecursorType = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "INSTRUMENTTYPE:", RegexOptions.IgnoreCase))
                    {
                        rawData.InstrumentType = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "INSTRUMENT:", RegexOptions.IgnoreCase))
                    {
                        rawData.Instrument = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "Authors:", RegexOptions.IgnoreCase))
                    {
                        rawData.Authors = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "License:", RegexOptions.IgnoreCase))
                    {
                        rawData.License = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "SMILES:", RegexOptions.IgnoreCase))
                    {
                        rawData.Smiles = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "INCHI:", RegexOptions.IgnoreCase))
                    {
                        rawData.Inchi = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "INCHIKEY:", RegexOptions.IgnoreCase))
                    {
                        rawData.InchiKey = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "ONTOLOGY:", RegexOptions.IgnoreCase)) {
                        rawData.Ontology = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "IONTYPE:", RegexOptions.IgnoreCase) || Regex.IsMatch(wkstr, "IONMODE:", RegexOptions.IgnoreCase))
                    {
                        string mode = wkstr.Split(':')[1].Trim();
                        if (mode.Contains("Positive") || mode.Contains("positive") || mode.Contains("P") || mode.Contains("p")) rawData.IonMode = IonMode.Positive; else rawData.IonMode = IonMode.Negative;
                    }
                    else if (Regex.IsMatch(wkstr, "COLLISIONENERGY:", RegexOptions.IgnoreCase))
                    {
                        rawData.CollisionEnergy = getCollisionEnergy(wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim()); ;
                    }
                    else if (Regex.IsMatch(wkstr, "SPECTRUMTYPE:", RegexOptions.IgnoreCase))
                    {
                        string spectrum = wkstr.Split(':')[1].Trim();
                        if (spectrum.Contains("Centroid") || spectrum.Contains("C")) rawData.SpectrumType = MSDataType.Centroid; else rawData.SpectrumType = MSDataType.Profile;
                    }
                    else if (Regex.IsMatch(wkstr, "INTENSITY:", RegexOptions.IgnoreCase))
                    {
                        int intensity;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intensity)) rawData.Intensity = intensity; else rawData.Intensity = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "METABOLITENAME:", RegexOptions.IgnoreCase))
                    {
                        rawData.MetaboliteName = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "FORMULA:", RegexOptions.IgnoreCase))
                    {
                        rawData.Formula = wkstr.Split(':')[1].Trim();
                    }
                    else if (Regex.IsMatch(wkstr.ToUpper(), "COMMENT.?:", RegexOptions.IgnoreCase)) {
                        rawData.Comment = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                    }
                    else if (Regex.IsMatch(wkstr, "CarbonCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.CarbonNumberFromLabeledExperiment = intValue;
                        else
                            rawData.CarbonNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "NitrogenCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.NitrogenNumberFromLabeledExperiment = intValue;
                        else
                            rawData.NitrogenNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "SulfurCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.SulfurNumberFromLabeledExperiment = intValue;
                        else
                            rawData.SulfurNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "OxygenCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.OxygenNumberFromLabeledExperiment = intValue;
                        else
                            rawData.OxygenNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "CarbonNitrogenCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.CarbonNitrogenNumberFromLabeledExperiment = intValue;
                        else
                            rawData.CarbonNitrogenNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "CarbonSulfurCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.CarbonSulfurNumberFromLabeledExperiment = intValue;
                        else
                            rawData.CarbonSulfurNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "NitrogenSulfurCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.NitrogenSulfurNumberFromLabeledExperiment = intValue;
                        else
                            rawData.NitrogenSulfurNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "CarbonNitrogenSulfurCount:", RegexOptions.IgnoreCase)) {
                        int intValue;
                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out intValue))
                            rawData.CarbonNitrogenSulfurNumberFromLabeledExperiment = intValue;
                        else
                            rawData.CarbonNitrogenSulfurNumberFromLabeledExperiment = -1;
                    }
                    else if (Regex.IsMatch(wkstr, "IsMarked:", RegexOptions.IgnoreCase)) {
                        var flg = false;
                        if (bool.TryParse(wkstr.Split(':')[1].Trim(), out flg)) rawData.IsMarked = flg; else { rawData.IsMarked = flg; }
                    }
                    else if (Regex.IsMatch(wkstr, "Num Peaks:", RegexOptions.IgnoreCase))
                    {
                        int num;

                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out num))
                        {
                            rawData.Ms1PeakNumber = 0;
                            rawData.Ms2PeakNumber = num;
                            rawData.Ms2Spectrum = spectrumParcer(sr, num);
                            if (rawData.PrecursorMz <= 0) rawData.PrecursorMz = 0;
                            if (rawData.PrecursorType == null) rawData.PrecursorType = "[M]+.";
                        }
                        else
                        {
                            rawData.Ms1PeakNumber = 0;
                            rawData.Ms2PeakNumber = 0;
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "MSTYPE: MS1", RegexOptions.IgnoreCase))
                    {
                        wkstr = sr.ReadLine();
                        int num;

                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out num))
                        {
                            rawData.Ms1PeakNumber = num;
                            rawData.Ms1Spectrum = spectrumParcer(sr, num);
                        }
                        else
                        {
                            rawData.Ms1PeakNumber = 0;
                        }
                    }
                    else if (Regex.IsMatch(wkstr, "MSTYPE: MS2", RegexOptions.IgnoreCase))
                    {
                        wkstr = sr.ReadLine();
                        int num;

                        if (int.TryParse(wkstr.Split(':')[1].Trim(), out num))
                        {
                            rawData.Ms2PeakNumber = num;
                            rawData.Ms2Spectrum = spectrumParcer(sr, num);

                        }
                        else
                        {
                            rawData.Ms2PeakNumber = 0;
                        }

                        continue;
                    }
                    #endregion
                }
            }

            setIsotopicIons(rawData, param.Mass1Tolerance, param.MassTolType);

            return rawData;
        }

        public static RawData RawDataFileRapidReader(string filePath) {
            RawData rawData = new RawData() { RawdataFilePath = filePath };

            string wkstr;
            using (StreamReader sr = new StreamReader(filePath)) {
                while (sr.Peek() > -1) {
                    wkstr = sr.ReadLine();
                    #region parcer
                    if (Regex.IsMatch(wkstr, "IsMarked:", RegexOptions.IgnoreCase)) {
                        var flg = false;
                        if (bool.TryParse(wkstr.Split(':')[1].Trim(), out flg)) rawData.IsMarked = flg; else { rawData.IsMarked = flg; }
                        break;
                    }
                    #endregion
                }
            }
            return rawData;
        }

        /// <summary>
        /// This is the writer to store the RawData storage as ASCII format file.
        /// </summary>
        /// <param name="filePath">Storage file path (ASCII)</param>
        /// <param name="rawData">Raw data</param>
        public static void RawDataFileWriter(string filePath, RawData rawData)
        {
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                sw.WriteLine("NAME: " + rawData.Name);
                sw.WriteLine("PRECURSORMZ: " + rawData.PrecursorMz);
                sw.WriteLine("PRECURSORTYPE: " + rawData.PrecursorType);
                sw.WriteLine("INSTRUMENTTYPE: " + rawData.InstrumentType);
                sw.WriteLine("INSTRUMENT: " + rawData.Instrument);
                sw.WriteLine("Authors: " + rawData.Authors);
                sw.WriteLine("License: " + rawData.License);
                sw.WriteLine("FORMULA: " + rawData.Formula);
                sw.WriteLine("ONTOLOGY: " + rawData.Ontology);
                sw.WriteLine("SMILES: " + rawData.Smiles);
                sw.WriteLine("INCHIKEY: " + rawData.InchiKey);
                sw.WriteLine("INCHI: " + rawData.Inchi);
                sw.WriteLine("IONMODE: " + rawData.IonMode);
                sw.WriteLine("COLLISIONENERGY: " + rawData.CollisionEnergy);
                sw.WriteLine("SPECTRUMTYPE: " + rawData.SpectrumType);
                sw.WriteLine("METABOLITENAME: " + rawData.MetaboliteName);
                sw.WriteLine("SCANNUMBER: " + rawData.ScanNumber);
                sw.WriteLine("RETENTIONTIME: " + rawData.RetentionTime);
                sw.WriteLine("RETENTIONINDEX: " + rawData.RetentionIndex);
                sw.WriteLine("CCS: " + rawData.Ccs);
                sw.WriteLine("INTENSITY: " + rawData.Intensity);
                sw.WriteLine("#Specific field for labeled experiment");
                if (rawData.CarbonNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("CarbonCount: " + rawData.CarbonNumberFromLabeledExperiment);
                if (rawData.NitrogenNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("NitrogenCount: " + rawData.NitrogenNumberFromLabeledExperiment);
                if (rawData.SulfurNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("SulfurCount: " + rawData.SulfurNumberFromLabeledExperiment);
                if (rawData.OxygenNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("OxygenCount: " + rawData.OxygenNumberFromLabeledExperiment);
                if (rawData.CarbonNitrogenNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("CarbonNitrogenCount: " + rawData.CarbonNitrogenNumberFromLabeledExperiment);
                if (rawData.CarbonSulfurNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("CarbonSulfurCount: " + rawData.CarbonSulfurNumberFromLabeledExperiment);
                if (rawData.NitrogenSulfurNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("NitrogenSulfurCount: " + rawData.NitrogenSulfurNumberFromLabeledExperiment);
                if (rawData.CarbonNitrogenSulfurNumberFromLabeledExperiment >= 0)
                    sw.WriteLine("CarbonNitrogenSulfurCount: " + rawData.CarbonNitrogenSulfurNumberFromLabeledExperiment);
                sw.WriteLine("IsMarked: " + rawData.IsMarked);
                sw.WriteLine("Comment: " + rawData.Comment);
                sw.WriteLine("MSTYPE: MS1");
                sw.WriteLine("Num Peaks: " + rawData.Ms1PeakNumber);
                foreach (var peak in rawData.Ms1Spectrum) {
                    if (peak.Comment == "") {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                    }
                    else {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity + "\t\"" + peak.Comment +"\"");
                    }
                }

                sw.WriteLine("MSTYPE: MS2");
                sw.WriteLine("Num Peaks: " + rawData.Ms2PeakNumber);
                foreach (var peak in rawData.Ms2Spectrum) {
                    if (peak.Comment == "") {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
                    }
                    else {
                        sw.WriteLine(peak.Mass + "\t" + peak.Intensity + "\t\"" + peak.Comment + "\"");
                    }
                }
            }        
        }

        private static void setIsotopicIons(RawData rawDataBean, double massTol, MassToleranceType massTolType)
        {
            var peaklist = new List<IsotopicPeak>();
            if (massTolType == MassToleranceType.Ppm) massTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(rawDataBean.PrecursorMz, massTol);
            var tol = massTol * 0.5;

            peaklist.Add(new IsotopicPeak() { Mass = rawDataBean.PrecursorMz, RelativeAbundance = 1, Comment = "M" });
            peaklist.Add(new IsotopicPeak() { Mass = rawDataBean.PrecursorMz + 1, RelativeAbundance = -1, Comment = "M+1" });
            peaklist.Add(new IsotopicPeak() { Mass = rawDataBean.PrecursorMz + 2, RelativeAbundance = -1, Comment = "M+2" });

            if (rawDataBean.Ms1PeakNumber != 0)
            {
                peaklist[0].RelativeAbundance = 0;
                peaklist[1].RelativeAbundance = 0;
                peaklist[2].RelativeAbundance = 0;

                var spectrum = FragmentAssigner.GetCentroidMs1Spectrum(rawDataBean);

                for (int i = 0; i < spectrum.Count; i++)
                {
                    if (spectrum[i].Mass <= rawDataBean.PrecursorMz - 0.00632 - tol) continue;
                    if (spectrum[i].Mass >= rawDataBean.PrecursorMz + 2.00671 + 0.005844 + tol) break;

                    if (spectrum[i].Mass > rawDataBean.PrecursorMz - tol && spectrum[i].Mass < rawDataBean.PrecursorMz + tol)
                        peaklist[0].RelativeAbundance += spectrum[i].Intensity;
                    else if (spectrum[i].Mass > rawDataBean.PrecursorMz + 1.00335 - 0.00632 - tol && spectrum[i].Mass < rawDataBean.PrecursorMz + 1.00335 + 0.00292 + tol)
                        peaklist[1].RelativeAbundance += spectrum[i].Intensity;
                    else if (spectrum[i].Mass > rawDataBean.PrecursorMz + 2.00671 - 0.01264 - tol && spectrum[i].Mass < rawDataBean.PrecursorMz + 2.00671 + 0.00584 + tol)
                        peaklist[2].RelativeAbundance += spectrum[i].Intensity;
                }

                if (peaklist[0].RelativeAbundance > 0)
                {
                    peaklist[1].RelativeAbundance = peaklist[1].RelativeAbundance / peaklist[0].RelativeAbundance;
                    peaklist[2].RelativeAbundance = peaklist[2].RelativeAbundance / peaklist[0].RelativeAbundance;
                    peaklist[0].RelativeAbundance = 1;
                }
                else
                {
                    peaklist[0].RelativeAbundance = 1;
                    peaklist[1].RelativeAbundance = -1;
                    peaklist[2].RelativeAbundance = -1;
                }
            }

            rawDataBean.NominalIsotopicPeakList = peaklist;
        }

        public static List<SpectrumPeak> SpectrumStringParcer(string spectrum)
        {
            var peakBeanList = new List<SpectrumPeak>();
            var peakBean = new SpectrumPeak();
            
            int pairCount = 0;
            string numChar, letterChar;
            bool mzFill;

            var spectrumArray = spectrum.Replace("\r\n", "\n").Split('\n');

            foreach (var spect in spectrumArray)
            {
                if (spect == null || spect == string.Empty) continue;

                numChar = string.Empty;
                mzFill = false;
                for (int i = 0; i < spect.Length; i++)
                {
                    if (char.IsNumber(spect[i]) || spect[i] == '.')
                    {
                        numChar += spect[i];

                        if (i == spect.Length - 1 && spect[i] != '"')
                        {
                            if (mzFill == false)
                            {
                                if (numChar != string.Empty)
                                {
                                    peakBean.Mass = double.Parse(numChar);
                                    mzFill = true;
                                    numChar = string.Empty;
                                }
                            }
                            else if (mzFill == true)
                            {
                                if (numChar != string.Empty)
                                {
                                    peakBean.Intensity = double.Parse(numChar);
                                    mzFill = false;
                                    numChar = string.Empty;
                                    if (peakBean.Comment == null) peakBean.Comment = peakBean.Mass.ToString();

                                    peakBeanList.Add(peakBean);
                                    peakBean = new SpectrumPeak();
                                    pairCount++;
                                }
                            }
                        }
                    }
                    else if (spect[i] == '"')
                    {
                        i++;
                        letterChar = string.Empty;

                        while (spect[i] != '"')
                        {
                            letterChar += spect[i];
                            i++;
                        }
                        peakBean.Comment = letterChar;
                    }
                    else
                    {
                        if (mzFill == false)
                        {
                            if (numChar != string.Empty)
                            {
                                peakBean.Mass = double.Parse(numChar);
                                mzFill = true;
                                numChar = string.Empty;
                            }
                        }
                        else if (mzFill == true)
                        {
                            if (numChar != string.Empty)
                            {
                                peakBean.Intensity = double.Parse(numChar);
                                mzFill = false;
                                numChar = string.Empty;
                                if (peakBean.Comment == null) peakBean.Comment = peakBean.Mass.ToString();

                                peakBeanList.Add(peakBean);
                                peakBean = new SpectrumPeak();
                                pairCount++;
                            }
                        }
                    }
                }
            }

            return peakBeanList;
        }

        private static List<SpectrumPeak> spectrumParcer(StreamReader sr, int num)
        {
            List<SpectrumPeak> peakBeanList = new List<SpectrumPeak>();
            SpectrumPeak peakBean = new SpectrumPeak();
            int pairCount = 0;
            string wkstr, numChar, letterChar;
            bool mzFill;

            while (pairCount < num)
            {
                wkstr = sr.ReadLine();
                numChar = string.Empty;
                mzFill = false;
                for (int i = 0; i < wkstr.Length; i++)
                {
                    if (char.IsNumber(wkstr[i]) || wkstr[i] == '.' ||
                        (i + 1 <= wkstr.Length && wkstr[i].ToString().ToLower() == "e" && wkstr[i + 1] == '+') ||
                        (i - 1 >= 0 && wkstr[i - 1].ToString().ToLower() == "e" && wkstr[i] == '+'))
                    {
                        numChar += wkstr[i];

                        if (i == wkstr.Length - 1 && wkstr[i] != '"')
                        {
                            if (mzFill == false)
                            {
                                if (numChar != string.Empty)
                                {
                                    peakBean.Mass = double.Parse(numChar);
                                    mzFill = true;
                                    numChar = string.Empty;
                                }
                            }
                            else if (mzFill == true)
                            {
                                if (numChar != string.Empty)
                                {
                                    peakBean.Intensity = double.Parse(numChar);
                                    mzFill = false;
                                    numChar = string.Empty;
                                    if (peakBean.Comment == null) {
                                        peakBean.Comment = "";
                                        //   peakBean.Comment = peakBean.Mz.ToString();
                                    }
                                    peakBeanList.Add(peakBean);
                                    peakBean = new SpectrumPeak();
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
                        peakBean.Comment = letterChar;
                        peakBeanList.Add(peakBean);
                        peakBean = new SpectrumPeak();
                        pairCount++;
                    }
                    else
                    {
                        if (mzFill == false)
                        {
                            if (numChar != string.Empty)
                            {
                                peakBean.Mass = double.Parse(numChar);
                                mzFill = true;
                                numChar = string.Empty;
                            }
                        }
                        else if (mzFill == true)
                        {
                            if (numChar != string.Empty) {
                                peakBean.Intensity = double.Parse(numChar);
                                mzFill = false;
                                numChar = string.Empty;
                                if (peakBean.Comment == null) {
                                    peakBean.Comment = "";
                                    //   peakBean.Comment = peakBean.Mz.ToString();
                                }

                                if (i == wkstr.Length - 1 || (wkstr[i] != ' ' && wkstr[i] != '\t')) {
                                    peakBeanList.Add(peakBean);
                                    peakBean = new SpectrumPeak();
                                    pairCount++;
                                }
                            }
                        }
                    }
                }
            }

            if (peakBeanList.Count > 0) peakBeanList = peakBeanList.OrderBy(n => n.Mass).ToList();
            return peakBeanList;
        }

        private static double getCollisionEnergy(string ce)
        {
            string figure = string.Empty;
            double ceValue = 0.0;
            for (int i = 0; i < ce.Length; i++)
            {
                if (Char.IsNumber(ce[i]) || ce[i] == '.')
                {
                    figure += ce[i];
                }
                else
                {
                    double.TryParse(figure, out ceValue);
                    return ceValue;
                }
            }
            double.TryParse(figure, out ceValue);
            return ceValue;
        }
    }
}
