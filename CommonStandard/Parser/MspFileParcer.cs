using CompMs.Common.Algorithm.IsotopeCalc;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Lipidomics;
using CompMs.Common.MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompMs.Common.Parser {
    public sealed class MspFileParcer {
        private MspFileParcer() { }
        static IupacDatabase IupacDatabase = IupacResourceParser.GetIUPACDatabase();

        /// <summary>
        /// This is the MSP file parcer.
        /// Each MS/MS information will be stored in MspFormatCompoundInformationBean.cs.
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static List<MoleculeMsReference> MspFileReader(string filepath)
        {
            var mspFields = new List<MoleculeMsReference>();
            var counter = 0;
            
            using (StreamReader sr = new StreamReader(filepath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    var wkstr = sr.ReadLine();
                    var isRecordStarted = wkstr.StartsWith("NAME:");
                    if (isRecordStarted) {
                        
                        var mspField = new MoleculeMsReference() { ScanID = counter };

                        ReadMspField(wkstr, out string titleName, out string titleValue); // read Name field
                        SetMspField(mspField, titleName, titleValue, sr);

                        while (sr.Peek() > -1) {
                            wkstr = sr.ReadLine();
                            ReadMspField(wkstr, out string fieldName, out string fieldValue);
                            var isSpecRetrieved = SetMspField(mspField, fieldName, fieldValue, sr);
                            if (isSpecRetrieved) break;
                        }
                        mspFields.Add(mspField);
                        counter++;
                    }
                }
            }

            return mspFields;
        }

        public static List<MoleculeMsReference> MonaMspReader(string filepath) {
            var mspFields = new List<MoleculeMsReference>();
            var counter = 0;
            using (StreamReader sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var wkstr = sr.ReadLine();
                    var isRecordStarted = wkstr.StartsWith("NAME:");
                    if (isRecordStarted) {

                        var mspField = new MoleculeMsReference() { ScanID = counter };

                        ReadMspField(wkstr, out string titleName, out string titleValue); // read Name field
                        SetMspField(mspField, titleName, titleValue, sr);

                        while (sr.Peek() > -1) {
                            wkstr = sr.ReadLine();
                            ReadMspField(wkstr, out string fieldName, out string fieldValue);
                            var isSpecRetrieved = SetMspField(mspField, fieldName, fieldValue, sr);

                            if (fieldName.ToLower().StartsWith("comment")) {
                                for (int i = 0; i < mspField.Comment.Length; i++) {
                                    var charvalue = mspField.Comment[i];
                                    if (charvalue == 'S' && i + 7 <= mspField.Comment.Length - 1) {
                                        var integWord = charvalue.ToString() + mspField.Comment[i + 1].ToString() + mspField.Comment[i + 2].ToString()
                                            + mspField.Comment[i + 3].ToString() + mspField.Comment[i + 4].ToString()
                                            + mspField.Comment[i + 5].ToString() + mspField.Comment[i + 6].ToString();
                                        if (integWord == "SMILES=") {
                                            var smilesString = string.Empty;
                                            for (int j = i + 7; j < mspField.Comment.Length; j++) {
                                                if (mspField.Comment[j] == '"') break;
                                                smilesString += mspField.Comment[j];
                                            }
                                            mspField.SMILES = smilesString;
                                        }

                                    }
                                }
                            }

                            if (isSpecRetrieved) break;
                        }
                        mspFields.Add(mspField);
                        counter++;
                    }
                }
            }

            return mspFields;
        }

        /// <summary>
        /// This is the parcer of .LBM (Lipid Blast Msp) file including LipidBlast MS/MSs.
        /// Each MS/MS information of LBM file has some fields including 'IONMODE', 'SOLVENTTYPE (now HCOOH or CH3COOH)', 'COMPOUNDCLASS', and 'COLLISIONTYPE (now CID only. in future, I will add HCD tag.)', 
        /// to pick up what the user wants to identify.
        /// The LBM format is basically following the MSP format but the above fields should be required additionally.
        /// The LBM file should be included in the MS-DIAL folder and the list of LBM queries should be stored in LbmQuery.cs.
        /// 1. first the LbmQueryParcer.cs will pick up all queries of LipidBlast from the LbmQueries.txt of Resources floder of MS-DIAL assembry.
        /// 2. The users can select what the user wants to see in LbmDbSetWin.xaml and the isSelected property will be changed to True.
        /// 3. The LipidBlast MS/MS of the true queries will be picked up by LbmFileParcer.cs. 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="queries"></param>
        /// <param name="ionMode"></param>
        /// <param name="solventType"></param>
        /// <param name="collisionType"></param>
        /// <returns></returns>
        public static List<MoleculeMsReference> LbmFileReader(string file, List<LbmQuery> queries,
            IonMode ionMode, SolventType solventType, CollisionType collisionType) {
            var tQueries = getTrueQueries(queries);
            if (tQueries.Count == 0) return null;

            var mspDB = new List<MoleculeMsReference>();
            var counter = 0;
            using (StreamReader sr = new StreamReader(file, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var wkstr = sr.ReadLine();
                    var isRecordStarted = wkstr.StartsWith("NAME:");
                    if (isRecordStarted) {

                        var mspField = new MoleculeMsReference();

                        ReadMspField(wkstr, out string titleName, out string titleValue); // read Name field
                        SetMspField(mspField, titleName, titleValue, sr);

                        while (sr.Peek() > -1) {
                            wkstr = sr.ReadLine();
                            ReadMspField(wkstr, out string fieldName, out string fieldValue);
                            var isSpecRetrieved = SetMspField(mspField, fieldName, fieldValue, sr);
                            if (isSpecRetrieved) break;
                        }

                        if (queryCheck(mspField, tQueries, ionMode, solventType, collisionType)) {
                            mspDB.Add(mspField);
                            counter++;
                        }
                    }
                }
            }

            return mspDB;
        }

        public static List<MoleculeMsReference> ReadSerializedLbmLibrary(string file, List<LbmQuery> queries,
            IonMode ionMode, SolventType solventType, CollisionType collisionType) {
            var tQueries = getTrueQueries(queries);
            if (tQueries.Count == 0) return null;

            var usedMspDB = new List<MoleculeMsReference>();
            var mspDB = ReadSerializedMspObject(file);
        
            var counter = 0;
            foreach (var mspRecord in mspDB) {
                if (queryCheck(mspRecord, tQueries, ionMode, solventType, collisionType)) {
                    mspRecord.ScanID = counter;
                    usedMspDB.Add(mspRecord);
                    counter++;
                }
            }
            return usedMspDB;
        }

        public static void AsciiMspToSerializedMspObj(string input, string output) {
            var queries = MspFileReader(input);
            MoleculeMsRefMethods.SaveMspToFile(queries, output);
        }

        public static List<MoleculeMsReference> ReadSerializedMspObject(string input) {
            var queries = MoleculeMsRefMethods.LoadMspFromFile(input);
            return queries;
        }

        private static bool queryCheck(MoleculeMsReference mspRecord, List<LbmQuery> queries, IonMode ionMode, SolventType solventType, CollisionType collosionType) {
            //if (queries[0].IonMode != mspRecord.IonMode) return false;
            if (mspRecord.IonMode != ionMode) return false;
            if (ionMode == IonMode.Negative) {
                if (solventType == SolventType.CH3COONH4 && mspRecord.AdductType.AdductIonName == "[M+HCOO]-") {
                    return false;
                }
                else if (solventType == SolventType.HCOONH4 && mspRecord.AdductType.AdductIonName == "[M+CH3COO]-") {
                    return false;
                }
            }
            foreach (var query in queries) {
                if (mspRecord.CompoundClass == "Others" || mspRecord.CompoundClass == "Unknown" || mspRecord.CompoundClass == "SPLASH") {
                    return true;
                }
                if (query.LbmClass.ToString() == mspRecord.CompoundClass) {
                    if (query.AdductType.AdductIonName == mspRecord.AdductType.AdductIonName) {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool queryCheck(MoleculeMsReference mspRecord, List<LbmQuery> queries) {
            foreach (var query in queries) {
                if (query.LbmClass.ToString() == mspRecord.CompoundClass) {
                    if (query.AdductType.AdductIonName == mspRecord.AdductType.AdductIonName) {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<LbmQuery> getTrueQueries(List<LbmQuery> queries) {
            var tQueries = new List<LbmQuery>();

            foreach (var query in queries) {
                if (query.IsSelected == true) tQueries.Add(query);
            }

            return tQueries;
        }



        public static void ReadMspField(string wkstr, out string fieldName, out string fieldValue) {
            fieldName = wkstr.Split(':')[0];
            fieldValue = wkstr.Substring(fieldName.Length + 1).Trim();
         }

        public static bool SetMspField(MoleculeMsReference mspObj, string fieldName, string fieldValue, StreamReader sr, bool isFiehnDB = false) {
            // if (fieldValue.IsEmptyOrNull()) return false; // this code has some bug ?
            switch (fieldName.ToLower()) {
                // string
                case "name": mspObj.Name = fieldValue;  return false;
                case "binid": mspObj.BinId = int.TryParse(fieldValue, out int binid) ? binid : -1;  return false;

                case "comment": 
                case "comments": 
                    mspObj.Comment = fieldValue; 
                    return false;

                case "formula": 
                    mspObj.Formula = FormulaStringParcer.OrganicElementsReader(fieldValue); 
                    if (mspObj.Formula != null) {
                        mspObj.Formula.M1IsotopicAbundance = SevenGoldenRulesCheck.GetM1IsotopicAbundance(mspObj.Formula);
                        mspObj.Formula.M2IsotopicAbundance = SevenGoldenRulesCheck.GetM2IsotopicAbundance(mspObj.Formula);

                        var isotopeProp = IsotopeCalculator.GetAccurateIsotopeProperty(mspObj.Formula.FormulaString, 2, IupacDatabase);
                        mspObj.IsotopicPeaks = isotopeProp.IsotopeProfile;
                    }
                    return false;
                case "smiles": mspObj.SMILES = fieldValue;  return false;
                case "ontology": mspObj.Ontology = fieldValue;  return false;
                case "compoundclass": mspObj.CompoundClass = fieldValue;  return false;
               
                case "inchi key":
                case "inchi_key":
                case "inchikey": mspObj.InChIKey = fieldValue;  return false;

                case "retentiontime": 
                case "retention_time": 
                case "rt": 
                    if (float.TryParse(fieldValue, out float rt)) mspObj.ChromXs.RT = new RetentionTime(rt); 
                     return false;

                case "retentionindex":
                case "retention_index":
                case "ri":
                    if (float.TryParse(fieldValue, out float ri)) {
                        mspObj.ChromXs.RI = new RetentionIndex(ri);
                        if (isFiehnDB) {
                            mspObj.ChromXs.RT = new RetentionTime((float)(ri * 0.001 / 60.0));
                        }
                    }
                   
                     return false;

                case "ionmode": 
                case "ion_mode":
                case "ionization": 
                    mspObj.IonMode = fieldValue.ToLower().Contains("n") ? IonMode.Negative : IonMode.Positive; 
                     return false;

                case "collision_energy":
                case "collisionenergy": mspObj.CollisionEnergy = float.TryParse(fieldValue, out float ce) ? ce : -1;  return false;

                case "precursormz":
                case "precursor_mz":
                case "precursor_m/z":
                    mspObj.PrecursorMz = float.TryParse(fieldValue, out float premz) ? premz : -1;  return false;
                case "quantmass": mspObj.QuantMass = float.TryParse(fieldValue, out float quantmass) ? quantmass : -1;  return false;

                case "collisioncrosssection":
                case "ccs":
                    mspObj.CollisionCrossSection = float.TryParse(fieldValue, out float ccs) ? ccs : -1;  return false;

                case "precursortype":
                case "precursor_type":
                    mspObj.AdductType = AdductIonParser.GetAdductIonBean(fieldValue);
                    mspObj.IonMode = mspObj.IonMode;
                     return false;

                case "DB#":
                case "links": mspObj.Links = fieldValue;  return false;
                case "instrument": mspObj.InstrumentModel = fieldValue;  return false;
                case "instrumenttype": mspObj.InstrumentType = fieldValue;  return false;

                case "scannumber":
                    if (mspObj.Comment == string.Empty) mspObj.Comment = "scannumber=" + fieldValue;
                    else mspObj.Comment += "; scannumber=" + fieldValue;
                    return false;
                case "num peaks":
                case "numpeaks":
                case "num_peaks":
                    mspObj.Spectrum = ReadSpectrum(sr, fieldValue);
                    return true;
            }
            return false;
        }

      
        public static List<SpectrumPeak> ReadSpectrum(StreamReader sr, string numPeakField) {
            var mspPeaks = new List<SpectrumPeak>();

            if (int.TryParse(numPeakField.Trim(), out int peaknum)) {
                if (peaknum == 0) { return mspPeaks; }

                var pairCount = 0;
                var mspPeak = new SpectrumPeak();

                while (pairCount < peaknum) {
                    var wkstr = sr.ReadLine();
                    if (wkstr == null) break;
                    if (wkstr == string.Empty) break;
                    var numChar = string.Empty;
                    var mzFill = false;

                    for (int i = 0; i < wkstr.Length; i++) {
                        if (char.IsNumber(wkstr[i]) || wkstr[i] == '.') {
                            numChar += wkstr[i];

                            if (i == wkstr.Length - 1 && wkstr[i] != '"') {
                                if (mzFill == false) {
                                    if (numChar != string.Empty) {
                                        mspPeak.Mass = float.Parse(numChar);
                                        mzFill = true;
                                        numChar = string.Empty;
                                    }
                                }
                                else if (mzFill == true) {
                                    if (numChar != string.Empty) {
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
                        else if (wkstr[i] == '"') {
                            i++;
                            var letterChar = string.Empty;

                            while (wkstr[i] != '"') {
                                letterChar += wkstr[i];
                                i++;
                            }
                            if (!letterChar.Contains("_f_"))
                                mspPeaks[mspPeaks.Count - 1].Comment = letterChar;
                            else {
                                mspPeaks[mspPeaks.Count - 1].Comment = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[0];
                                //mspPeaks[mspPeaks.Count - 1].Frag = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[1];
                            }

                        }
                        else {
                            if (mzFill == false) {
                                if (numChar != string.Empty) {
                                    mspPeak.Mass = float.Parse(numChar);
                                    mzFill = true;
                                    numChar = string.Empty;
                                }
                            }
                            else if (mzFill == true) {
                                if (numChar != string.Empty) {
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
