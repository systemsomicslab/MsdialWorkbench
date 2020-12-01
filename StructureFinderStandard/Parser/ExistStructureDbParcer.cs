using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Parser
{
    public sealed class ExistStructureDbParcer
    {
        private ExistStructureDbParcer() { }

        /// <summary>
        /// see .ESD file in resource folder to check the format
        /// </summary>
        public static List<ExistStructureQuery> ReadExistStructureDB(string file)
        {
            var queries = new List<ExistStructureQuery>();

            DatabaseQuery databaseQuery;
            int dbRecords;
            string dbNames, errorString;

            if (ErrorHandler.IsFileLocked(file, out errorString)) {
                Console.WriteLine(errorString);
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8)) {
                var header = sr.ReadLine();
                var headerArray = header.Split('\t');

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;

                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 8) return null;

                    var title = lineArray[0];
                    var inchikey = lineArray[1];
                    var shortInchi = lineArray[2];
                    var pubCids = getPubCids(lineArray[3]);
                    var formula = FormulaStringParcer.OrganicElementsReader(lineArray[5]);
                    var smiles = lineArray[6];
                    setDbRecords(lineArray, headerArray, out dbRecords, out dbNames, out databaseQuery);

                    queries.Add(new ExistStructureQuery(title, inchikey, shortInchi, pubCids, formula, smiles, dbNames, dbRecords, databaseQuery));
                }
            }
            queries = queries.OrderBy(n => n.Formula.Mass).ToList();
            return queries;
        }

        public static void SetClassyfireOntologies(List<ExistStructureQuery> existStructures, string classyfireFile) {
            var dictionary = new Dictionary<string, string>();
            using (StreamReader sr = new StreamReader(classyfireFile, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;

                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 3) continue; 

                    var inchikey = lineArray[0].Split('-')[0];
                    var ontology = lineArray[1];
                    var id = lineArray[2];

                    if (!dictionary.ContainsKey(inchikey))
                        dictionary[inchikey] = ontology + "_" + id;
                }
            }

            foreach (var query in existStructures) {
                var inchikey = query.ShortInchiKey;
                if (dictionary.ContainsKey(inchikey)) {
                    var dicValues = dictionary[inchikey].Split('_');
                    var ontology = dicValues[0];
                    var id = dicValues[1];

                    query.ClassyfireOntology = ontology;
                    query.ClassyfireID = id;
                }
            }
        }

        public static List<ExistStructureQuery> ReadExistStructureDB(string file, double mass, double tolerance)
        {
            var queries = new List<ExistStructureQuery>();

            DatabaseQuery databaseQuery;
            int dbRecords;
            string dbNames, errorString;
            double dbMass;

            if (ErrorHandler.IsFileLocked(file, out errorString)) {
                Console.WriteLine(errorString);
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (StreamReader sr = new StreamReader(file, Encoding.UTF8)) {
                var header = sr.ReadLine();
                var headerArray = header.Split('\t');

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;

                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 8) return null;

                    var title = lineArray[0];
                    var inchikey = lineArray[1];
                    var shortInchi = lineArray[2];
                    var pubCids = getPubCids(lineArray[3]);

                    if (!double.TryParse(lineArray[4], out dbMass)) continue;
                    if (dbMass < mass - tolerance) continue;
                    if (dbMass > mass + tolerance) break;

                    var formula = FormulaStringParcer.OrganicElementsReader(lineArray[5]);
                    var smiles = lineArray[6];
                    setDbRecords(lineArray, headerArray, out dbRecords, out dbNames, out databaseQuery);

                    queries.Add(new ExistStructureQuery(title, inchikey, shortInchi, pubCids, formula, smiles, dbNames, dbRecords, databaseQuery));
                }
            }
            if (queries.Count == 0) return null;

            queries = queries.OrderBy(n => n.Formula.Mass).ToList();
            return queries;
        }

        public static List<ExistStructureQuery> ReadExistStructureDB(string file, Formula queryFormula)
        {
            var queries = new List<ExistStructureQuery>();

            DatabaseQuery databaseQuery;
            int dbRecords;
            string dbNames, errorString;
            double dbMass;

            if (ErrorHandler.IsFileLocked(file, out errorString)) {
                Console.WriteLine(errorString);
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {

                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8)) {

                    var header = sr.ReadLine();
                    var headerArray = header.Split('\t');

                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) break;

                        var lineArray = line.Split('\t');
                        if (lineArray.Length < 8) return null;

                        var title = lineArray[0];
                        var inchikey = lineArray[1];
                        var shortInchi = lineArray[2];
                        var pubCids = getPubCids(lineArray[3]);
                        if (!double.TryParse(lineArray[4], out dbMass)) continue;
                        if (dbMass < queryFormula.Mass - 0.0001) continue;
                        if (dbMass > queryFormula.Mass + 0.0001) break;

                        var formula = FormulaStringParcer.OrganicElementsReader(lineArray[5]);
                        if (!MolecularFormulaUtility.isFormulaMatch(formula, queryFormula)) continue;

                        var smiles = lineArray[6];
                        setDbRecords(lineArray, headerArray, out dbRecords, out dbNames, out databaseQuery);

                        queries.Add(new ExistStructureQuery(title, inchikey, shortInchi, pubCids, formula, smiles, dbNames, dbRecords, databaseQuery));
                    }
                }
            }

            if (queries.Count == 0) return null;

            queries = queries.OrderBy(n => n.Formula.Mass).ToList();
            return queries;
        }

        /// <summary>
        /// use this method when user-defined DB or MINE DB are imported to retrieve the existing DB info
        /// </summary>
        public static void SetExistStructureDbInfoToUserDefinedDB(List<ExistStructureQuery> existStructureDB, 
            List<ExistStructureQuery> userDefinedDB, bool isMines = false)
        {
            foreach (var userQuery in userDefinedDB) {
                if (isMines == false) {
                    userQuery.ResourceNumber = 1;
                    userQuery.ResourceNames = userQuery.ResourceNames;
                }
                var shortInChiKey = userQuery.ShortInchiKey;
                if (shortInChiKey == null || shortInChiKey == string.Empty) continue;

                var formula = userQuery.Formula;
                var mass = formula.Mass;
                var tol = 0.00005;
                var startID = getQueryStartIndex(mass, tol, existStructureDB);

                for (int i = startID; i < existStructureDB.Count; i++) {
                    if (existStructureDB[i].Formula.Mass < mass - tol) continue;
                    if (existStructureDB[i].Formula.Mass > mass + tol) break;

                    if (MolecularFormulaUtility.isFormulaMatch(formula, existStructureDB[i].Formula))
                        if (shortInChiKey == existStructureDB[i].ShortInchiKey) {
                            userQuery.PubchemCIDs = existStructureDB[i].PubchemCIDs;
                            userQuery.ClassyfireOntology = existStructureDB[i].ClassyfireOntology;
                            userQuery.ClassyfireID = existStructureDB[i].ClassyfireID;
                            if (isMines == false) {
                                userQuery.ResourceNames += "," + existStructureDB[i].ResourceNames;
                                userQuery.ResourceNumber += existStructureDB[i].ResourceNumber;
                            }
                            else {
                                if (userQuery.ResourceNames.Split(',').Length > 1) {
                                    var array = userQuery.ResourceNames.Split(',');
                                    var mineID = array[array.Length - 1].Trim();
                                    userQuery.ResourceNames = mineID;
                                }
                                userQuery.ResourceNames = existStructureDB[i].ResourceNames + "," + userQuery.ResourceNames;
                                userQuery.ResourceNumber = existStructureDB[i].ResourceNumber + 1;
                            }
                            break;
                        }
                }
            }
        }

        private static int getQueryStartIndex(double mass, double tol, List<ExistStructureQuery> queryDB)
        {
            if (queryDB == null || queryDB.Count == 0) return 0;
            double targetMass = mass - tol;
            int startIndex = 0, endIndex = queryDB.Count - 1;
            int counter = 0;

            while (counter < 10) {
                if (queryDB[startIndex].Formula.Mass <= targetMass && targetMass < queryDB[(startIndex + endIndex) / 2].Formula.Mass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (queryDB[(startIndex + endIndex) / 2].Formula.Mass <= targetMass && targetMass < queryDB[endIndex].Formula.Mass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private static void setDbRecords(string[] lineArray, string[] headerArray, out int dbRecords, out string dbNames, out DatabaseQuery databaseQuery)
        {
            dbRecords = 0;
            dbNames = string.Empty;
            databaseQuery = new DatabaseQuery();

            for (int i = 7; i < lineArray.Length; i++) {
                if (lineArray[i] != "N/A") {
                    dbNames += headerArray[i] + "=" + lineArray[i] + ",";
                    dbRecords++;
                    setDatabaseQuery(databaseQuery, headerArray[i]);
                }
            }
            if (dbNames.Length != 0) dbNames = dbNames.Substring(0, dbNames.Length - 1);
        }

        private static void setDatabaseQuery(DatabaseQuery databaseQuery, string databaseName)
        {
            switch (databaseName) {
                case "ChEBI":
                    databaseQuery.Chebi = true;
                    break;
                case "HMDB":
                    databaseQuery.Hmdb = true;
                    break;
                case "PubChem":
                    databaseQuery.Pubchem = true;
                    break;
                case "SMPDB":
                    databaseQuery.Smpdb = true;
                    break;
                case "UNPD":
                    databaseQuery.Unpd = true;
                    break;
                case "YMDB":
                    databaseQuery.Ymdb = true;
                    break;
                case "PlantCyc":
                    databaseQuery.Plantcyc = true;
                    break;
                case "KNApSAcK":
                    databaseQuery.Knapsack = true;
                    break;
                case "BMDB":
                    databaseQuery.Bmdb = true;
                    break;
                case "DrugBank":
                    databaseQuery.Drugbank = true;
                    break;
                case "ECMDB":
                    databaseQuery.Ecmdb = true;
                    break;
                case "FooDB":
                    databaseQuery.Foodb = true;
                    break;
                case "T3DB":
                    databaseQuery.T3db = true;
                    break;
                case "STOFF":
                    databaseQuery.Stoff = true;
                    break;
                case "NANPDB":
                    databaseQuery.Nanpdb = true;
                    break;
                case "LipidMAPS":
                    databaseQuery.Lipidmaps = true;
                    break;
                case "Urine":
                    databaseQuery.Urine = true;
                    break;
                case "Saliva":
                    databaseQuery.Saliva = true;
                    break;
                case "Feces":
                    databaseQuery.Feces = true;
                    break;
                case "Serum":
                    databaseQuery.Serum = true;
                    break;
                case "Csf":
                    databaseQuery.Csf = true;
                    break;
                case "BLEXP":
                    databaseQuery.Blexp = true;
                    break;
                case "NPA":
                    databaseQuery.Npa = true;
                    break;
                case "COCONUT":
                    databaseQuery.Coconut = true;
                    break;
                default:
                    break;
            }
        }

        private static List<int> getPubCids(string pubString)
        {
            int pubID;
            if (pubString == string.Empty) {
                return new List<int>();
            }
            else {
                var array = pubString.Split(';');
                var publist = new List<int>();

                foreach (var id in array) {
                    if (int.TryParse(id, out pubID)) {
                        publist.Add(pubID);
                    }
                }

                return publist;
            }
        }
    }
}
