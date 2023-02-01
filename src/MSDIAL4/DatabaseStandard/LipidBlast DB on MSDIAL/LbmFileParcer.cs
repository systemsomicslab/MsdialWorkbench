using Riken.Metabolomics.Lipidomics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class LbmFileParcer
    {
        private LbmFileParcer() { }

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
        public static List<MspFormatCompoundInformationBean> Read(string file, List<LbmQuery> queries, 
            IonMode ionMode, SolventType solventType, CollisionType collisionType)
        {
            var mspDB = new List<MspFormatCompoundInformationBean>();
            var mspRecord = new MspFormatCompoundInformationBean();
            var mzIntCommentBean = new MzIntensityCommentBean();
            
            var tQueries = getTrueQueryStrings(queries);
            if (tQueries.Count == 0) return null;
            
            string wkstr;
            int counter = 0, pairCount = 0;

            using (StreamReader sr = new StreamReader(file, Encoding.ASCII))
            {
                float rt = 0, preMz = 0, ri = 0;
                int num;
                string numChar, letterChar;
                bool mzFill;

                while (sr.Peek() > -1)
                {
                    wkstr = sr.ReadLine();
                    if (Regex.IsMatch(wkstr, "NAME:.*", RegexOptions.IgnoreCase))
                    {
                        mspRecord.Id = counter;
                        mspRecord.Name = getMspFieldValue(wkstr);

                        while (sr.Peek() > -1)
                        {
                            wkstr = sr.ReadLine();
                            if (wkstr == string.Empty) break;
                            if (Regex.IsMatch(wkstr, "COMMENT:.*", RegexOptions.IgnoreCase))
                            {
                                mspRecord.Comment = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "FORMULA:.*", RegexOptions.IgnoreCase))
                            {
                                mspRecord.Formula = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "IONMODE:.*", RegexOptions.IgnoreCase))
                            {
                                var fieldString = getMspFieldValue(wkstr);
                                if (fieldString == "Negative") mspRecord.IonMode = IonMode.Negative;
                                else mspRecord.IonMode = IonMode.Positive;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "SMILES:.*", RegexOptions.IgnoreCase))
                            {
                                mspRecord.Smiles = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "InChIKey:.*", RegexOptions.IgnoreCase))
                            {
                                mspRecord.InchiKey = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "COMPOUNDCLASS:.*", RegexOptions.IgnoreCase))
                            {
                                mspRecord.CompoundClass = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RETENTIONTIME:.*", RegexOptions.IgnoreCase))
                            {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out rt)) mspRecord.RetentionTime = rt; else mspRecord.RetentionTime = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RT:.*", RegexOptions.IgnoreCase))
                            {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out rt)) mspRecord.RetentionTime = rt; else mspRecord.RetentionTime = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RETENTIONINDEX:.*", RegexOptions.IgnoreCase))
                            {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out ri)) mspRecord.RetentionIndex = ri; else mspRecord.RetentionIndex = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RI:.*", RegexOptions.IgnoreCase))
                            {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out ri)) mspRecord.RetentionIndex = ri; else mspRecord.RetentionIndex = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORMZ:.*", RegexOptions.IgnoreCase))
                            {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out preMz)) mspRecord.PrecursorMz = preMz;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Links:.*", RegexOptions.IgnoreCase))
                            {
                                mspRecord.Links = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORTYPE:.*", RegexOptions.IgnoreCase))
                            {
                                var fieldString = getMspFieldValue(wkstr);
                                mspRecord.AdductIonBean = AdductIonStringParser.GetAdductIonBean(fieldString);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "CollisionCrossSection:.*", RegexOptions.IgnoreCase) || Regex.IsMatch(wkstr, "CCS:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out preMz)) mspRecord.CollisionCrossSection = preMz;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Num Peaks:.*", RegexOptions.IgnoreCase))
                            {
                                if (int.TryParse(wkstr.Split(':')[1].Trim(), out num))
                                {
                                    mspRecord.PeakNumber = num;
                                    if (mspRecord.PeakNumber == 0) { mspRecord.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean()); continue; }

                                    pairCount = 0;
                                    mzIntCommentBean = new MzIntensityCommentBean();

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
                                                            mzIntCommentBean.Mz = float.Parse(numChar);
                                                            mzFill = true;
                                                            numChar = string.Empty;
                                                        }
                                                    }
                                                    else if (mzFill == true)
                                                    {
                                                        if (numChar != string.Empty)
                                                        {
                                                            mzIntCommentBean.Intensity = (int)float.Parse(numChar);
                                                            mzFill = false;
                                                            numChar = string.Empty;

                                                            if (mzIntCommentBean.Comment == null)
                                                                mzIntCommentBean.Comment = mzIntCommentBean.Mz.ToString();
                                                            mspRecord.MzIntensityCommentBeanList.Add(mzIntCommentBean);
                                                            mzIntCommentBean = new MzIntensityCommentBean();
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
                                                    mspRecord.MzIntensityCommentBeanList[mspRecord.MzIntensityCommentBeanList.Count - 1].Comment = letterChar;
                                                else
                                                {
                                                    mspRecord.MzIntensityCommentBeanList[mspRecord.MzIntensityCommentBeanList.Count - 1].Comment = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[0];
                                                    mspRecord.MzIntensityCommentBeanList[mspRecord.MzIntensityCommentBeanList.Count - 1].Frag = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[1];
                                                }

                                            }
                                            else
                                            {
                                                if (mzFill == false)
                                                {
                                                    if (numChar != string.Empty)
                                                    {
                                                        mzIntCommentBean.Mz = float.Parse(numChar);
                                                        mzFill = true;
                                                        numChar = string.Empty;
                                                    }
                                                }
                                                else if (mzFill == true)
                                                {
                                                    if (numChar != string.Empty)
                                                    {
                                                        mzIntCommentBean.Intensity = (int)float.Parse(numChar);
                                                        mzFill = false;
                                                        numChar = string.Empty;

                                                        if (mzIntCommentBean.Comment == null)
                                                            mzIntCommentBean.Comment = mzIntCommentBean.Mz.ToString();

                                                        mspRecord.MzIntensityCommentBeanList.Add(mzIntCommentBean);
                                                        mzIntCommentBean = new MzIntensityCommentBean();
                                                        pairCount++;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    mspRecord.MzIntensityCommentBeanList = mspRecord.MzIntensityCommentBeanList.OrderBy(n => n.Mz).ToList();
                                }
                                continue;
                            }
                        }

                        //if (mspRecord.CompoundClass == "SM" && ionMode == IonMode.Negative && solventType == SolventType.CH3COONH4 && mspRecord.AdductIonBean.AdductIonName == "[M+Hac-H]-") {
                        //    Console.WriteLine();
                        //}
                        if (queryCheck(mspRecord, tQueries, ionMode, solventType, collisionType))
                        {
                            mspDB.Add(mspRecord);
                            counter++;
                        }
                        
                        mspRecord = new MspFormatCompoundInformationBean();
                    }
                }
            }

            return mspDB;
        }

        public static List<MspFormatCompoundInformationBean> ReadSerializedObjectLibrary(string file, List<LbmQuery> queries,
            IonMode ionMode, SolventType solventType, CollisionType collisionType) {
            var usedMspDB = new List<MspFormatCompoundInformationBean>();
            var mspDB = LipidomicsConverter.SerializedObjectToMspQeries(file);
            var tQueries = getTrueQueryStrings(queries);
            if (tQueries.Count == 0) return null;
            var counter = 0;
            // var strings = new List<string>();
            var sw = new Stopwatch();
            sw.Start();
            foreach (var mspRecord in mspDB) {
                //if (mspRecord.CompoundClass == "Cer_EBDS") {
                //    Console.WriteLine("OK");
                //}
               // if (!strings.Contains(mspRecord.CompoundClass)) strings.Add(mspRecord.CompoundClass);
                if (queryCheck(mspRecord, tQueries, ionMode, solventType, collisionType)) {
                    mspRecord.Id = counter;
                    usedMspDB.Add(mspRecord);
                    counter++;
                }
            }
            Console.WriteLine("read end {0}", sw.ElapsedMilliseconds);
            sw.Stop();
            
            //usedMspDB = usedMspDB.OrderBy(n => n.PrecursorMz).ToList();
            //for (int i = 0; i < usedMspDB.Count; i++) {
            //    usedMspDB[i].Id = i;
            //}
            return usedMspDB;
        }

        public static List<MspFormatCompoundInformationBean> GetSelectedLipidMspQueries(List<MspFormatCompoundInformationBean> mspDB, List<LbmQuery> queries) {
            var usedMspDB = new List<MspFormatCompoundInformationBean>();
            var tQueries = getTrueQueries(queries);
            if (tQueries.Count == 0) return null;
            var counter = 0;
            foreach (var mspRecord in mspDB) {
                if (queryCheck(mspRecord, tQueries)) {
                    mspRecord.Id = counter;
                    usedMspDB.Add(mspRecord);
                    counter++;
                }
            }
            return usedMspDB;
        }



        public static List<MspFormatCompoundInformationBean> Read(string file) {
            var mspDB = new List<MspFormatCompoundInformationBean>();
            var mspRecord = new MspFormatCompoundInformationBean();
            var mzIntCommentBean = new MzIntensityCommentBean();

            string wkstr;
            int counter = 0, pairCount = 0;

            using (StreamReader sr = new StreamReader(file, Encoding.ASCII)) {
                float rt = 0, preMz = 0, ri = 0;
                int num;
                string numChar, letterChar;
                bool mzFill;

                while (sr.Peek() > -1) {
                    wkstr = sr.ReadLine();
                    if (Regex.IsMatch(wkstr, "NAME:.*", RegexOptions.IgnoreCase)) {
                        mspRecord.Id = counter;
                        mspRecord.Name = getMspFieldValue(wkstr);

                        while (sr.Peek() > -1) {
                            wkstr = sr.ReadLine();
                            if (wkstr == string.Empty) break;
                            if (Regex.IsMatch(wkstr, "COMMENT:.*", RegexOptions.IgnoreCase)) {
                                mspRecord.Comment = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "FORMULA:.*", RegexOptions.IgnoreCase)) {
                                mspRecord.Formula = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "IONMODE:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (fieldString == "Negative") mspRecord.IonMode = IonMode.Negative;
                                else mspRecord.IonMode = IonMode.Positive;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "SMILES:.*", RegexOptions.IgnoreCase)) {
                                mspRecord.Smiles = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "InChIKey:.*", RegexOptions.IgnoreCase)) {
                                mspRecord.InchiKey = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "COMPOUNDCLASS:.*", RegexOptions.IgnoreCase)) {
                                mspRecord.CompoundClass = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RETENTIONTIME:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out rt)) mspRecord.RetentionTime = rt; else mspRecord.RetentionTime = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RT:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out rt)) mspRecord.RetentionTime = rt; else mspRecord.RetentionTime = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RETENTIONINDEX:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out ri)) mspRecord.RetentionIndex = ri; else mspRecord.RetentionIndex = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "RI:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out ri)) mspRecord.RetentionIndex = ri; else mspRecord.RetentionIndex = -1;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORMZ:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out preMz)) mspRecord.PrecursorMz = preMz;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Links:.*", RegexOptions.IgnoreCase)) {
                                mspRecord.Links = getMspFieldValue(wkstr);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORTYPE:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                mspRecord.AdductIonBean = AdductIonStringParser.GetAdductIonBean(fieldString);
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "CollisionCrossSection:.*", RegexOptions.IgnoreCase) || Regex.IsMatch(wkstr, "CCS:.*", RegexOptions.IgnoreCase)) {
                                var fieldString = getMspFieldValue(wkstr);
                                if (float.TryParse(fieldString, out preMz)) mspRecord.CollisionCrossSection = preMz;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Num Peaks:.*", RegexOptions.IgnoreCase)) {
                                if (int.TryParse(wkstr.Split(':')[1].Trim(), out num)) {
                                    mspRecord.PeakNumber = num;
                                    if (mspRecord.PeakNumber == 0) { mspRecord.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean()); continue; }

                                    pairCount = 0;
                                    mzIntCommentBean = new MzIntensityCommentBean();

                                    while (pairCount < num) {
                                        wkstr = sr.ReadLine();
                                        numChar = string.Empty;
                                        mzFill = false;
                                        for (int i = 0; i < wkstr.Length; i++) {
                                            if (char.IsNumber(wkstr[i]) || wkstr[i] == '.') {
                                                numChar += wkstr[i];

                                                if (i == wkstr.Length - 1 && wkstr[i] != '"') {
                                                    if (mzFill == false) {
                                                        if (numChar != string.Empty) {
                                                            mzIntCommentBean.Mz = float.Parse(numChar);
                                                            mzFill = true;
                                                            numChar = string.Empty;
                                                        }
                                                    }
                                                    else if (mzFill == true) {
                                                        if (numChar != string.Empty) {
                                                            mzIntCommentBean.Intensity = (int)float.Parse(numChar);
                                                            mzFill = false;
                                                            numChar = string.Empty;

                                                            if (mzIntCommentBean.Comment == null)
                                                                mzIntCommentBean.Comment = mzIntCommentBean.Mz.ToString();
                                                            mspRecord.MzIntensityCommentBeanList.Add(mzIntCommentBean);
                                                            mzIntCommentBean = new MzIntensityCommentBean();
                                                            pairCount++;
                                                        }
                                                    }
                                                }
                                            }
                                            else if (wkstr[i] == '"') {
                                                i++;
                                                letterChar = string.Empty;

                                                while (wkstr[i] != '"') {
                                                    letterChar += wkstr[i];
                                                    i++;
                                                }
                                                if (!letterChar.Contains("_f_"))
                                                    mspRecord.MzIntensityCommentBeanList[mspRecord.MzIntensityCommentBeanList.Count - 1].Comment = letterChar;
                                                else {
                                                    mspRecord.MzIntensityCommentBeanList[mspRecord.MzIntensityCommentBeanList.Count - 1].Comment = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[0];
                                                    mspRecord.MzIntensityCommentBeanList[mspRecord.MzIntensityCommentBeanList.Count - 1].Frag = letterChar.Split(new string[] { "_f_" }, StringSplitOptions.None)[1];
                                                }

                                            }
                                            else {
                                                if (mzFill == false) {
                                                    if (numChar != string.Empty) {
                                                        mzIntCommentBean.Mz = float.Parse(numChar);
                                                        mzFill = true;
                                                        numChar = string.Empty;
                                                    }
                                                }
                                                else if (mzFill == true) {
                                                    if (numChar != string.Empty) {
                                                        mzIntCommentBean.Intensity = (int)float.Parse(numChar);
                                                        mzFill = false;
                                                        numChar = string.Empty;

                                                        if (mzIntCommentBean.Comment == null)
                                                            mzIntCommentBean.Comment = mzIntCommentBean.Mz.ToString();

                                                        mspRecord.MzIntensityCommentBeanList.Add(mzIntCommentBean);
                                                        mzIntCommentBean = new MzIntensityCommentBean();
                                                        pairCount++;
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    mspRecord.MzIntensityCommentBeanList = mspRecord.MzIntensityCommentBeanList.OrderBy(n => n.Mz).ToList();
                                }
                                continue;
                            }
                        }

                        mspDB.Add(mspRecord);
                        counter++;

                        mspRecord = new MspFormatCompoundInformationBean();
                    }
                }
            }

            return mspDB;
        }

        private static string getMspFieldValue(string wkstr) {
            return wkstr.Substring(wkstr.Split(':')[0].Length + 1).Trim();
        }


        private static bool queryCheck(MspFormatCompoundInformationBean mspRecord, List<string> queries, IonMode ionMode, SolventType solventType, CollisionType collosionType)
        {
            if (ionMode != mspRecord.IonMode) return false;
            if (mspRecord.CompoundClass == "Others" || mspRecord.CompoundClass == "Unknown" || mspRecord.CompoundClass == "SPLASH") {
                return true;
            }
            if (queries.Contains(mspRecord.CompoundClass + "_" + mspRecord.AdductIonBean.AdductIonName)) return true;
            return false;
          
        }

        private static bool queryCheck(MspFormatCompoundInformationBean mspRecord, List<LbmQuery> queries, IonMode ionMode, SolventType solventType, CollisionType collosionType) {
            if (ionMode != mspRecord.IonMode) return false;
            if (mspRecord.CompoundClass == "Others" || mspRecord.CompoundClass == "Unknown" || mspRecord.CompoundClass == "SPLASH") {
                return true;
            }
            foreach (var query in queries) {
                if (query.LbmClass.ToString() == mspRecord.CompoundClass) {
                    if (query.AdductIon.AdductIonName == mspRecord.AdductIonBean.AdductIonName) {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool queryCheck(MspFormatCompoundInformationBean mspRecord, List<LbmQuery> queries) {
            foreach (var query in queries) {
                if (query.LbmClass.ToString() == mspRecord.CompoundClass) {
                    if (query.AdductIon.AdductIonName == mspRecord.AdductIonBean.AdductIonName) {
                        return true;
                    }
                }
            }

            return false;
        }

        private static List<LbmQuery> getTrueQueries(List<LbmQuery> queries)
        {
            var tQueries = new List<LbmQuery>();

            foreach (var query in queries)
            {
                if (query.IsSelected == true) tQueries.Add(query);
            }

            return tQueries;
        }

        /// <summary>
        /// return lipidclass + "_" + adduct list
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        private static List<string> getTrueQueryStrings(List<LbmQuery> queries) {
            var tQueries = new List<string>();
            foreach (var query in queries) {
                if (query.IsSelected == true) tQueries.Add(query.LbmClass + "_" + query.AdductIon.AdductIonName);
            }

            return tQueries;
        }
    }
}
