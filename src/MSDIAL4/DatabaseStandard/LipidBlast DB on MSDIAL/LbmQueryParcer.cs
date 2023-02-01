using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
//using System.Windows;
//using System.Windows.Resources;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class LbmQueryParcer
    {
        private LbmQueryParcer() { }

        /// <summary>
        /// This is the parcer of LbmQueries.txt included in Resources folder of MS-DIAL assembry.
        /// 1. first the LbmQueryParcer.cs will pick up all queries of LipidBlast from the LbmQueries.txt of Resources floder of MS-DIAL assembry.
        /// 2. The users can select what the user wants to see in LbmDbSetWin.xaml and the isSelected property will be changed to True.
        /// 3. The LipidBlast MS/MS of the true queries will be picked up by LbmFileParcer.cs.
        /// </summary>
        /// <returns></returns>
        public static List<LbmQuery> GetLbmQueries(bool isLabUseOnly)
        {
            var queries = new List<LbmQuery>();
            //var lbmQueryString = Properties.Resources.LbmQueries;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DatabaseStandard.Resources.LbmQueries.txt";
            var lbmQueryString = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    lbmQueryString = reader.ReadToEnd();
                }
            }
            //var fileUri = new Uri("/Resources/LbmQueries.txt", UriKind.Relative);
            //var info = Application.GetResourceStream(fileUri);
            var lbmQueries = lbmQueryString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });
            for (int i = 1; i < lbmQueries.Length; i++) {
                var line = lbmQueries[i];
                if (line == string.Empty) break;
                if (line[0] == '#') {
                    if (isLabUseOnly == false)
                        continue;
                    else {
                        if (line[1] == '#') continue;
                        line = line.Substring(1);
                    }
                }
                var lineArray = line.Split('\t');
                var query = getQuery(lineArray[0], lineArray[1], lineArray[2], lineArray[3]);

                queries.Add(query);
            }

            //using (StreamReader sr = new StreamReader(info.Stream))
            //{
            //    sr.ReadLine();
            //    while (sr.Peek() > -1)
            //    {
            //        var line = sr.ReadLine();
            //        if (line == string.Empty) break;
            //        if (line[0] == '#') {
            //            if (isLabUseOnly == false)
            //                continue;
            //            else {
            //                if (line[1] == '#') continue;
            //                line = line.Substring(1);
            //            }
            //        }
            //        var lineArray = line.Split('\t');
            //        var query = getQuery(lineArray[0], lineArray[1], lineArray[2], lineArray[3]);

            //        queries.Add(query);
            //    }
            //}

            return queries;
        }

        public static List<LbmQuery> GetLbmQueries(string input, bool isLabUseOnly) {
            var queries = new List<LbmQuery>();
            using (StreamReader sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    if (line[0] == '#') {
                        if (isLabUseOnly == false)
                            continue;
                        else {
                            if (line[1] == '#') continue;
                            line = line.Substring(1);
                        }
                    }
                    var lineArray = line.Split('\t');
                    var query = getQuery(lineArray[0], lineArray[1], lineArray[2], lineArray[3]);

                    queries.Add(query);
                }
            }

            return queries;
        }

        private static LbmQuery getQuery(string lbmClass, string adduct, string ionmode, string isSelected)
        {
            var query = new LbmQuery();

            foreach (var lipid in Enum.GetValues(typeof(LbmClass)).Cast<LbmClass>()) 
            {
                if (lipid.ToString() == lbmClass) { query.LbmClass = lipid; break; }
            }

            query.AdductIon = AdductIonParcer.GetAdductIonBean(adduct);

            if (ionmode.ToLower() == "positive") query.IonMode = IonMode.Positive;
            else query.IonMode = IonMode.Negative;

            if (isSelected.ToLower() == "true") query.IsSelected = true;
            else query.IsSelected = false;

            return query;
        }
    }
}
