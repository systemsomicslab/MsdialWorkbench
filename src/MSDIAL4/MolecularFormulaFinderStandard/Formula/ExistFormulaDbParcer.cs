using System.Collections.Generic;
using System.IO;
using System.Text;
//using System.Windows;
using System.Linq;

namespace Rfx.Riken.OsakaUniv {
    public sealed class ExistFormulaDbParcer
    {
        private ExistFormulaDbParcer() { }

        /// <summary>
        /// This is the parcer to retreive the internal formula queries from .EFD file.
        /// MS-FINDER utilizes the internal database queries to rank the formual candidates.
        /// The internal database will be stored in .EFD file of the program folder.
        /// </summary>
        /// <param name="file">Add the EFD format file path.</param>
        /// <returns>The queries will be return as generic list.</returns>
        public static List<ExistFormulaQuery> ReadExistFormulaDB(string file, out string errorString) {
            var queries = new List<ExistFormulaQuery>();

            int formulaRecords;
            int dbRecords;
            string dbNames;

            errorString = string.Empty;
 
            if (ErrorHandler.IsFileLocked(file, out errorString)){
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (StreamReader sr = new StreamReader(file, Encoding.ASCII)) {
                var header = sr.ReadLine();
                var headerArray = header.Split('\t');

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;

                    var lineArray = line.Split('\t');

                    var formula = FormulaStringParcer.OrganicElementsReader(lineArray[1]);
                    var pubCids = getPubCids(lineArray[2]);
                    int.TryParse(lineArray[3], out formulaRecords);
                    setDbRecords(lineArray, headerArray, out dbRecords, out dbNames);

                    queries.Add(new ExistFormulaQuery(formula, pubCids, formulaRecords, dbRecords, dbNames));
                }
            }

            return queries.OrderBy(n => n.Formula.Mass).ToList();
        }

        /// <summary>
        /// This is the parcer to retreive the internal formula queries from .EFD file with labeld element information.
        /// MS-FINDER utilizes the internal database queries to rank the formual candidates.
        /// The internal database will be stored in .EFD file of the program folder.
        /// </summary>
        /// <returns>The queries will be return as generic list.</returns>
        public static List<ExistFormulaQuery> ReadExistFormulaDB(string file, double cLabelMass, double hLabelMass, double nLabelMass,
            double oLabelMass, double pLabelMass, double sLabelMass, double fLabelMass, double clLabelMass, double brLabelMass,
            double iLabelMass, double siLabelMass, out string errorString) {
            var queries = new List<ExistFormulaQuery>();

            int formulaRecords;
            int dbRecords;
            string dbNames;
            errorString = string.Empty;

            if (ErrorHandler.IsFileLocked(file, out errorString))
            {
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (StreamReader sr = new StreamReader(file, Encoding.ASCII)) {
                var header = sr.ReadLine();
                var headerArray = header.Split('\t');

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;

                    var lineArray = line.Split('\t');

                    var formula = FormulaStringParcer.OrganicElementsReader(lineArray[1], cLabelMass, hLabelMass, nLabelMass, oLabelMass, pLabelMass,
                sLabelMass, fLabelMass, clLabelMass, brLabelMass, iLabelMass, siLabelMass);

                    var pubCids = getPubCids(lineArray[2]);
                    int.TryParse(lineArray[3], out formulaRecords);
                    setDbRecords(lineArray, headerArray, out dbRecords, out dbNames);

                    queries.Add(new ExistFormulaQuery(formula, pubCids, formulaRecords, dbRecords, dbNames));
                }
            }

            return queries;
        }

        public static void setDbRecords(string[] lineArray, string[] headerArray, out int dbRecords, out string dbNames)
        {
            dbRecords = 0;
            dbNames = string.Empty;
            
            for (int i = 4; i < lineArray.Length; i++)
            {
                if (lineArray[i] == "TRUE")
                {
                    dbNames += headerArray[i] + ",";
                    dbRecords++;
                }
            }
            dbNames = dbNames.Substring(0, dbNames.Length - 1);
        }

        public static List<int> getPubCids(string pubString)
        {
            int pubID;
            if (pubString == "N/A")
            {
                return new List<int>();
            }
            else
            {
                var array = pubString.Split(';');
                var publist = new List<int>();

                foreach (var id in array)
                {
                    if (int.TryParse(id, out pubID))
                    {
                        publist.Add(pubID);
                    }
                }

                return publist;
            }
        }
    }
}
