using CompMs.Common.Components;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Utility
{
    public sealed class FileStorageUtility
    {
        public static string GetResourcesPath(string file)
        {
            var currentDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var pather = Path.Combine(currentDir, "Resources\\msfinderLibrary");

            var filename = string.Empty;
            switch (file)
            {
                case "AdductNegativeLib": filename = "AdductNegatives.anf"; break;
                case "AdductPositiveLib": filename = "AdductPositives.apf"; break;
                case "ChemOntologyLib": filename = "ChemOntologyDB_vs2.cho"; break;
                case "EiFragmentLib": filename = "EiFragmentDB_vs1.eif"; break;
                case "EimsSpectralLib": filename = "EIMS-DBs-vs1.egm"; break;
                case "ExistFormulaLib": filename = "MsfinderFormulaDB-VS13.efd"; break;
                case "ExistStructureLib": filename = "MsfinderStructureDB-VS15.esd"; break;
                case "InchikeyClassyfireLib": filename = "InchikeyClassyfireDB-VS5.icd"; break;
                case "InsilicoLipidSpectralLib": filename = "Msp20201228141756_converted.lbm2"; break;
                case "LipidQueryMaster": filename = "LipidQueryMaster.txt"; break;
                case "MinesStructureLib": filename = "MINEs-StructureDB-vs1.msd"; break;
                case "MsmsSpectralLib": filename = "MSMS-DBs-vs1.etm"; break;
                case "NeutralLossLib": filename = "NeutralLossDB_vs2.ndb"; break;
                case "ProductIonLib": filename = "ProductIonLib_vs1.pid"; break;
                case "UniqueFragmentLib": filename = "UniqueFragmentLib_vs1.ufd"; break;
                default: filename = string.Empty; break;
            }
            pather = Path.Combine(pather, filename);
            return pather;
        }

        public static List<ExistFormulaQuery> GetExistFormulaDB()
        {

            var path = GetResourcesPath("ExistFormulaLib");
            var existFormulaDB = new List<ExistFormulaQuery>();
            //Console.WriteLine(path);
            try
            {
                existFormulaDB = MessagePackMsFinderHandler.LoadFromFile<List<ExistFormulaQuery>>(path);
            }
            catch (Exception)
            {
                Console.WriteLine("Error in GetExistFormulaDB to read messagepack file");
            }
            if (existFormulaDB == null || existFormulaDB.Count == 0)
            {
                var error = string.Empty;
                existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(path, out error);
                if (error != string.Empty)
                {
                    Console.WriteLine(error);
                }
                if (existFormulaDB == null || existFormulaDB.Count == 0)
                {
                    return null;
                }
                MessagePackMsFinderHandler.SaveToFile<List<ExistFormulaQuery>>(existFormulaDB, path);
            }
            return existFormulaDB;
        }

        public static List<ExistStructureQuery> GetExistStructureDB()
        {
            var existStructureDB = new List<ExistStructureQuery>();
            var path = GetResourcesPath("ExistStructureLib");
            try
            {
                existStructureDB = MessagePackMsFinderHandler.LoadFromFile<List<ExistStructureQuery>>(path);
            }
            catch (Exception)
            {
                Console.WriteLine("Error in GetExistStructureDB to read messagepack file");
            }

            if (existStructureDB == null || existStructureDB.Count == 0)
            {
                existStructureDB = ExistStructureDbParser.ReadExistStructureDB(path);
                if (existStructureDB == null || existStructureDB.Count == 0)
                {
                    return null;
                }
                MessagePackMsFinderHandler.SaveToFile<List<ExistStructureQuery>>(existStructureDB, path);
            }
            ExistStructureDbParser.SetClassyfireOntologies(existStructureDB, GetResourcesPath("InchikeyClassyfireLib"));
            return existStructureDB;

        }

        public static List<FragmentLibrary> GetEiFragmentDB()
        {
            var eiFragmentDB = StructureFinder.Parser.FragmentDbParser.ReadEiFragmentDB(GetResourcesPath("EiFragmentLib"));

            if (eiFragmentDB == null || eiFragmentDB.Count == 0)
            {
                return null;
            }
            else
            {
                return eiFragmentDB;
            }

        }

        public static List<NeutralLoss> GetNeutralLossDB()
        {
            var error = string.Empty;
            var neutralLossDB = FormulaGenerator.Parser.FragmentDbParser.GetNeutralLossDB(GetResourcesPath("NeutralLossLib"), out error);

            if (neutralLossDB == null)
            {
                return null;
            }
            else
            {
                return neutralLossDB;
            }
        }

        public static bool IsLibrariesImported(AnalysisParamOfMsfinder param,
            List<ExistStructureQuery> eQueries, List<ExistStructureQuery> mQueries, List<ExistStructureQuery> uQueries, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (param.IsUsePredictedRtForStructureElucidation && param.IsUseRtInchikeyLibrary)
            {
                var filepath = param.RtInChIKeyDictionaryFilepath;

                if (filepath == null || filepath == string.Empty)
                {
                    errorMessage = "A library containing the list of retention time and InChIKey should be selected if you select the RT option for structure elucidation.";
                    //  MessageBox.Show("A library containing the list of retention time and InChIKey should be selected if you select the RT option for structure elucidation."
                    //, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                if (!System.IO.File.Exists(filepath))
                {
                    errorMessage = System.IO.Path.GetFileName(filepath) + "is not found.";
                    //  MessageBox.Show(System.IO.Path.GetFileName(filepath) + "is not found."
                    //, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                FileStorageUtility.SetRetentiontimeDataFromLibrary(eQueries, filepath);
                FileStorageUtility.SetRetentiontimeDataFromLibrary(mQueries, filepath);
                FileStorageUtility.SetRetentiontimeDataFromLibrary(uQueries, filepath);
            }

            if (param.IsUsePredictedCcsForStructureElucidation)
            {
                var filepath = param.CcsAdductInChIKeyDictionaryFilepath;

                if (filepath == null || filepath == string.Empty)
                {
                    errorMessage = "A library containing the list of CCS, adduct type and InChIKey should be selected if you select the CCS option for structure elucidation.";
                    return false;
                }

                if (!System.IO.File.Exists(filepath))
                {
                    errorMessage = System.IO.Path.GetFileName(filepath) + "is not found.";
                    return false;
                }

                FileStorageUtility.SetCcsDataFromLibrary(eQueries, filepath);
                FileStorageUtility.SetCcsDataFromLibrary(mQueries, filepath);
                FileStorageUtility.SetCcsDataFromLibrary(uQueries, filepath);
            }

            return true;
        }

        public static void SetRetentiontimeDataFromLibrary(List<ExistStructureQuery> queries, string input)
        {

            if (queries == null || queries.Count == 0) return;
            var inchikey2Rt = new Dictionary<string, float>();
            using (var sr = new StreamReader(input, Encoding.ASCII))
            { // [0] InChIKey [1] RT (min)
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 2) continue;

                    var inchikey = lineArray[0];
                    var shortinchikey = inchikey.Split('-')[0].Trim();
                    var rtString = lineArray[1];
                    float rt = 0.0F;
                    if (float.TryParse(rtString, out rt) && shortinchikey.Length == 14)
                    {
                        if (!inchikey2Rt.ContainsKey(shortinchikey))
                        {
                            inchikey2Rt[shortinchikey] = rt;
                        }
                    }
                }
            }

            foreach (var query in queries)
            {
                var shortInChIkey = query.ShortInchiKey;
                if (inchikey2Rt.ContainsKey(shortInChIkey))
                {
                    query.Retentiontime = inchikey2Rt[shortInChIkey];
                }
            }
        }

        public static void SetCcsDataFromLibrary(List<ExistStructureQuery> queries, string input)
        {

            if (queries == null || queries.Count == 0) return;
            var inchikey2AdductCcsPair = new Dictionary<string, List<string>>();
            using (var sr = new StreamReader(input, Encoding.ASCII))
            { // [0] InChIKey [1] Adduct [1] CCS
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 3) continue;

                    var inchikey = lineArray[0];
                    var shortinchikey = inchikey.Split('-')[0].Trim();
                    var adductString = lineArray[1];
                    var adductObj = AdductIonParser.GetAdductIonBean(adductString);
                    if (!adductObj.FormatCheck) continue;
                    var ccsString = lineArray[2];
                    float ccs = 0.0F;
                    if (float.TryParse(ccsString, out ccs) && shortinchikey.Length == 14)
                    {

                        var pairKey = String.Join("_", new string[] { adductObj.AdductIonName, ccsString });
                        if (!inchikey2AdductCcsPair.ContainsKey(shortinchikey))
                        {
                            inchikey2AdductCcsPair[shortinchikey] = new List<string>() { pairKey };
                        }
                        else
                        {
                            if (!inchikey2AdductCcsPair[shortinchikey].Contains(pairKey))
                                inchikey2AdductCcsPair[shortinchikey].Add(pairKey);
                        }
                    }
                }
            }

            foreach (var query in queries)
            {
                var shortInChIkey = query.ShortInchiKey;
                if (inchikey2AdductCcsPair.ContainsKey(shortInChIkey))
                {
                    var adductCcsPairs = inchikey2AdductCcsPair[shortInChIkey];
                    foreach (var pair in adductCcsPairs)
                    {
                        var adduct = pair.Split('_')[0];
                        var ccs = pair.Split('_')[1];
                        if (query.AdductToCCS == null) query.AdductToCCS = new Dictionary<string, float>();
                        query.AdductToCCS[adduct] = float.Parse(ccs);
                    }
                }
            }
        }


        public static List<ChemicalOntology> GetChemicalOntologyDB()
        {
            var errorMessage = string.Empty;
            var chemicalOntologies = ChemOntologyDbParser.Read(GetResourcesPath("ChemOntologyLib"), out errorMessage);

            if (chemicalOntologies == null)
            {
                return null;
            }
            else
            {
                return chemicalOntologies;
            }
        }


        public static List<FragmentOntology> GetUniqueFragmentDB()
        {
            var error = string.Empty;
            var uniqueFragmentDB = FormulaGenerator.Parser.FragmentDbParser.GetFragmentOntologyDB(GetResourcesPath("UniqueFragmentLib"), out error);

            if (uniqueFragmentDB == null)
            {
                return null;
            }
            else
            {
                return uniqueFragmentDB;
            }
        }

        public static List<ProductIon> GetProductIonDB()
        {
            var error = string.Empty;
            var productIonDB = FormulaGenerator.Parser.FragmentDbParser.GetProductIonDB(GetResourcesPath("ProductIonLib"), out error);

            if (productIonDB == null)
            {
                return null;
            }
            else
            {
                return productIonDB;
            }
        }

        public static List<ExistStructureQuery> GetMinesStructureDB(Formula formula)
        {
            var minesDB = ExistStructureDbParser.ReadExistStructureDB(GetResourcesPath("MinesStructureLib"), formula);

            if (minesDB == null)
            {
                return null;
            }
            else
                return minesDB;
        }

        public static List<ExistStructureQuery> GetMinesStructureDB()
        {

            var path = GetResourcesPath("MinesStructureLib");
            var minesDB = new List<ExistStructureQuery>();
            try
            {
                minesDB = MessagePackMsFinderHandler.LoadFromFile<List<ExistStructureQuery>>(path);
            }
            catch (Exception)
            {
                Console.WriteLine("Error in GetMinesStructureDB to read messagepack file");
            }

            if (minesDB == null || minesDB.Count == 0)
            {
                minesDB = ExistStructureDbParser.ReadExistStructureDB(path);
                if (minesDB == null || minesDB.Count == 0)
                {
                    return null;
                }
                MessagePackMsFinderHandler.SaveToFile<List<ExistStructureQuery>>(minesDB, path);
            }
            ExistStructureDbParser.SetClassyfireOntologies(minesDB, GetResourcesPath("InchikeyClassyfireLib"));
            return minesDB;
        }

        public static List<MoleculeMsReference> GetInternalEiMsMsp()
        {
            var mspDB = MspFileParser.MspFileReader(GetResourcesPath("EimsSpectralLib"));

            if (mspDB == null)
            {
                return null;
            }
            else
                return mspDB;
        }

        public static List<LbmQuery> GetLbmQueries()
        {
            var queries = LbmQueryParcer.GetLbmQueries(GetResourcesPath("LipidQueryMaster"), false);
            if (queries == null)
            {
                return null;
            }
            else
                return queries;
        }

        public static List<MoleculeMsReference> GetInternalMsmsMsp()
        {
            var mspDB = MspFileParser.MspFileReader(GetResourcesPath("MsmsSpectralLib"));

            if (mspDB == null)
            {
                return null;
            }
            else
                return mspDB;
        }

        public static List<MoleculeMsReference> GetInsilicoLipidMsp(IonMode ionMode, SolventType solventType, CollisionType collisionType)
        {
            var queries = new List<LbmQuery>();
            var mspDB = MspFileParser.ReadSerializedLbmLibrary(GetResourcesPath("InsilicoLipidSpectralLib"), queries, ionMode, solventType, collisionType);

            if (mspDB == null)
            {
                return null;
            }
            else
                return mspDB;
        }

        public static List<MoleculeMsReference> GetMspDB(AnalysisParamOfMsfinder param, IonMode ionMode, out string errorMessage)
        {
            var mspDB = new List<MoleculeMsReference>();
            errorMessage = string.Empty;
            if (param.IsUseUserDefinedSpectralDb)
            {
                var userDefinedDbFilePath = param.UserDefinedSpectralDbFilePath;
                if (userDefinedDbFilePath == null || userDefinedDbFilePath == string.Empty)
                {
                    errorMessage = "Select your own MSP database, or uncheck the user-defined spectral DB option.";
                    //MessageBox.Show("Select your own MSP database, or uncheck the user-defined spectral DB option.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                if (!File.Exists(userDefinedDbFilePath))
                {
                    errorMessage = userDefinedDbFilePath + " file is not existed.";
                    //MessageBox.Show(userDefinedDbFilePath + " file is not existed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }

                var userDefinedMspDB = MspFileParser.MspFileReader(userDefinedDbFilePath);
                foreach (var mspRecord in userDefinedMspDB) mspDB.Add(mspRecord);
            }

            if (param.IsUseInternalExperimentalSpectralDb)
            {
                var internalMsp = new List<MoleculeMsReference>();
                if (param.IsTmsMeoxDerivative)
                    internalMsp = GetInternalEiMsMsp();
                else
                    internalMsp = GetInternalMsmsMsp();
                foreach (var mspRecord in internalMsp) mspDB.Add(mspRecord);
            }

            if (param.IsUseInSilicoSpectralDbForLipids && param.IsTmsMeoxDerivative == false)
            {
                var insilicoLipidMsp = GetSelectedLipidMspQueries(
                    GetInsilicoLipidMsp(ionMode, param.LipidQueryBean.SolventType, param.LipidQueryBean.CollisionType),
                    param.LipidQueryBean.LbmQueries);
                foreach (var mspRecord in insilicoLipidMsp)
                {
                    mspDB.Add(mspRecord);
                }
            }

            if (mspDB.Count == 0)
            {
                errorMessage = "No spectral record.";
                //MessageBox.Show("No spectral record.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            if (param.IsPrecursorOrientedSearch)
                mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();
            else if (param.IsTmsMeoxDerivative && !param.IsPrecursorOrientedSearch)
                mspDB = mspDB.OrderBy(n => n.ChromXs.RI.Value).ToList();

            return mspDB;
        }

        public static string GetStructureDataFilePath(string folderPath, string formula)
        {
            return Path.Combine(folderPath, formula + ".sfd");
        }

        public static void DeleteSfdFiles(string[] structureFiles)
        {
            foreach (var file in structureFiles)
            {
                File.Delete(file);
            }
        }

        private static List<MoleculeMsReference> GetSelectedLipidMspQueries(List<MoleculeMsReference> mspDB, List<LbmQuery> queries)
        {
            var usedMspDB = new List<MoleculeMsReference>();
            var tQueries = getTrueQueries(queries);
            if (tQueries.Count == 0) return null;
            var counter = 0;
            foreach (var mspRecord in mspDB)
            {
                if (queryCheck(mspRecord, tQueries))
                {
                    mspRecord.ScanID = counter;
                    usedMspDB.Add(mspRecord);
                    counter++;
                }
            }
            return usedMspDB;
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

        private static bool queryCheck(MoleculeMsReference mspRecord, List<LbmQuery> queries)
        {
            foreach (var query in queries)
            {
                if (query.LbmClass.ToString() == mspRecord.CompoundClass)
                {
                    if (query.AdductType.AdductIonName == mspRecord.AdductType.AdductIonName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
