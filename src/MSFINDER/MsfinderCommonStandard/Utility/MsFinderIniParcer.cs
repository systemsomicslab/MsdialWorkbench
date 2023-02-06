using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Riken.Metabolomics.MsfinderCommon.Utility {
    public sealed class MsFinderIniParcer {
        private MsFinderIniParcer() { }

        public static AnalysisParamOfMsfinder Read(string filepath = null) {
            var param = new AnalysisParamOfMsfinder();
            param.LipidQueryBean = new LipidQueryBean() { LbmQueries = FileStorageUtility.GetLbmQueries() };

            //var iniPath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\MSFINDER.INI";
            var iniPath = System.AppDomain.CurrentDomain.BaseDirectory + "MSFINDER.INI";
            if (filepath != null)
                iniPath = filepath;

            if (!System.IO.File.Exists(iniPath)) { Write(param); }

            //var iniLipidPath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\LipidQueries.INI";
            var iniLipidPath = System.AppDomain.CurrentDomain.BaseDirectory + "LipidQueries.INI";
            if (!System.IO.File.Exists(iniLipidPath)) { WriteLipidINI(param); }

            //using (var fs = new FileStream(iniPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
            //    using (var sr = new StreamReader(fs, Encoding.ASCII)) {

            //    }
            //}

            using (var sr = new StreamReader(iniPath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    if (line.Length != 0 && line[0] == '#') continue;

                    var ans = string.Empty;
                    int intValue;
                    double doubleValue;

                    #region
                    if (Regex.IsMatch(line, "LewisAndSeniorCheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.IsLewisAndSeniorCheck = false;
                        else param.IsLewisAndSeniorCheck = true;
                    }
                    else if (Regex.IsMatch(line, "MassToleranceType=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("Da")) param.MassTolType = MassToleranceType.Da;
                        else param.MassTolType = MassToleranceType.Ppm;
                    }
                    else if (Regex.IsMatch(line, "Ms1Tolerance=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.Mass1Tolerance = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "IsotopicAbundanceTolerance=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.IsotopicAbundanceTolerance = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "CommonRange=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.CoverRange = CoverRange.CommonRange;
                    }
                    else if (Regex.IsMatch(line, "ExtendedRange=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.CoverRange = CoverRange.ExtendedRange;
                    }
                    else if (Regex.IsMatch(line, "ExtremeRange=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.CoverRange = CoverRange.ExtremeRange;
                    }
                    else if (Regex.IsMatch(line, "ElementProbabilityCheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.IsElementProbabilityCheck = false;
                        else param.IsElementProbabilityCheck = true;
                    }
                    else if (Regex.IsMatch(line, "Ocheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsOcheck = true;
                        else param.IsOcheck = false;
                    }
                    else if (Regex.IsMatch(line, "Ncheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsNcheck = true;
                        else param.IsNcheck = false;
                    }
                    else if (Regex.IsMatch(line, "Pcheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsPcheck = true;
                        else param.IsPcheck = false;
                    }
                    else if (Regex.IsMatch(line, "Scheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsScheck = true;
                        else param.IsScheck = false;
                    }
                    else if (Regex.IsMatch(line, "Fcheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsFcheck = true;
                        else param.IsFcheck = false;
                    }
                    else if (Regex.IsMatch(line, "^Icheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsIcheck = true;
                        else param.IsIcheck = false;
                    }
                    else if (Regex.IsMatch(line, "ClCheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsClCheck = true;
                        else param.IsClCheck = false;
                    }
                    else if (Regex.IsMatch(line, "BrCheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsBrCheck = true;
                        else param.IsBrCheck = false;
                    }
                    else if (Regex.IsMatch(line, "SiCheck=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsSiCheck = true;
                        else param.IsSiCheck = false;
                    }
                    else if (Regex.IsMatch(line, "IsTmsMeoxDerivative=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsTmsMeoxDerivative = true;
                        else param.IsTmsMeoxDerivative = false;
                    }
                    else if (Regex.IsMatch(line, "MinimumTmsCount=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (int.TryParse(ans, out intValue)) param.MinimumTmsCount = intValue;
                    }
                    else if (Regex.IsMatch(line, "MinimumMeoxCount=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (int.TryParse(ans, out intValue)) param.MinimumMeoxCount = intValue;
                    }
                    else if (Regex.IsMatch(line, "CanExcuteMS2AdductSearch=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        //Console.WriteLine(ans);
                        if (ans.Length > 0 && ans.ToUpper() == "TRUE") param.CanExcuteMS2AdductSearch = true;
                        else param.CanExcuteMS2AdductSearch = false;
                    }
                    else if (Regex.IsMatch(line, "FormulaMaximumReportNumber=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (int.TryParse(ans, out intValue)) param.FormulaMaximumReportNumber = intValue;
                    }
                    else if (Regex.IsMatch(line, "TreeDepth=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (int.TryParse(ans, out intValue)) param.TreeDepth = intValue;
                    }
                    else if (Regex.IsMatch(line, "Ms2Tolerance=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.Mass2Tolerance = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "RelativeAbundanceCutOff=", RegexOptions.IgnoreCase) && !line.Contains("Fsea")) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.RelativeAbundanceCutOff = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "StructureMaximumReportNumber=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (int.TryParse(ans, out intValue)) param.StructureMaximumReportNumber = intValue;
                    }
                    else if (Regex.IsMatch(line, "IsUseEiFragmentDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseEiFragmentDB = true;
                        else param.IsUseEiFragmentDB = false;
                    }
                    else if (Regex.IsMatch(line, "MinesNeverUse=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMinesNeverUse = true;
                        else param.IsMinesNeverUse = false;
                    }
                    else if (Regex.IsMatch(line, "MinesOnlyUseForNecessary=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMinesOnlyUseForNecessary = true;
                        else param.IsMinesOnlyUseForNecessary = false;
                    }
                    else if (Regex.IsMatch(line, "MinesAllways=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMinesAllTime = true;
                        else param.IsMinesAllTime = false;
                    }
                    else if (Regex.IsMatch(line, "PubChemNeverUse=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsPubChemNeverUse = true;
                        else param.IsPubChemNeverUse = false;
                    }
                    else if (Regex.IsMatch(line, "PubChemOnlyUseForNecessary=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsPubChemOnlyUseForNecessary = true;
                        else param.IsPubChemOnlyUseForNecessary = false;
                    }
                    else if (Regex.IsMatch(line, "PubChemAllways=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsPubChemAllTime = true;
                        else param.IsPubChemAllTime = false;
                    }
                    else if (Regex.IsMatch(line, "HMDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Hmdb = false;
                        else param.DatabaseQuery.Hmdb = true;
                    }
                    else if (Regex.IsMatch(line, "YMDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Ymdb = false;
                        else param.DatabaseQuery.Ymdb = true;
                    }
                    else if (Regex.IsMatch(line, "PubChem=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Pubchem = false;
                        else param.DatabaseQuery.Pubchem = true;
                    }
                    else if (Regex.IsMatch(line, "SMPDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Smpdb = false;
                        else param.DatabaseQuery.Smpdb = true;
                    }
                    else if (Regex.IsMatch(line, "UNPD=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Unpd = false;
                        else param.DatabaseQuery.Unpd = true;
                    }
                    else if (Regex.IsMatch(line, "ChEBI=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Chebi = false;
                        else param.DatabaseQuery.Chebi = true;
                    }
                    else if (Regex.IsMatch(line, "PlantCyc=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Plantcyc = false;
                        else param.DatabaseQuery.Plantcyc = true;
                    }
                    else if (Regex.IsMatch(line, "KNApSAcK=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Knapsack = false;
                        else param.DatabaseQuery.Knapsack = true;
                    }
                    else if (Regex.IsMatch(line, "BMDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Bmdb = false;
                        else param.DatabaseQuery.Bmdb = true;
                    }
                    else if (Regex.IsMatch(line, "FooDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Foodb = false;
                        else param.DatabaseQuery.Foodb = true;
                    }
                    else if (Regex.IsMatch(line, "ECMDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Ecmdb = false;
                        else param.DatabaseQuery.Ecmdb = true;
                    }
                    else if (Regex.IsMatch(line, "DrugBank=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Drugbank = false;
                        else param.DatabaseQuery.Drugbank = true;
                    }
                    else if (Regex.IsMatch(line, "T3DB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.T3db = false;
                        else param.DatabaseQuery.T3db = true;
                    }
                    else if (Regex.IsMatch(line, "STOFF=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Stoff = false;
                        else param.DatabaseQuery.Stoff = true;
                    }
                    else if (Regex.IsMatch(line, "NANPDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Nanpdb = false;
                        else param.DatabaseQuery.Nanpdb = true;
                    }
                    else if (Regex.IsMatch(line, "LipidMAPS=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Lipidmaps = false;
                        else param.DatabaseQuery.Lipidmaps = true;
                    }
                    else if (Regex.IsMatch(line, "Urine=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Urine = false;
                        else param.DatabaseQuery.Urine = true;
                    }
                    else if (Regex.IsMatch(line, "Saliva=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Saliva = false;
                        else param.DatabaseQuery.Saliva = true;
                    }
                    else if (Regex.IsMatch(line, "Feces=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Feces = false;
                        else param.DatabaseQuery.Feces = true;
                    }
                    else if (Regex.IsMatch(line, "Serum=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Serum = false;
                        else param.DatabaseQuery.Serum = true;
                    }
                    else if (Regex.IsMatch(line, "Csf=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Csf = false;
                        else param.DatabaseQuery.Csf = true;
                    }
                    else if (Regex.IsMatch(line, "Blexp=", RegexOptions.IgnoreCase))
                    {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Blexp = false;
                        else param.DatabaseQuery.Blexp = true;
                    }
                    else if (Regex.IsMatch(line, "Npa=", RegexOptions.IgnoreCase))
                    {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Npa = false;
                        else param.DatabaseQuery.Npa = true;
                    }
                    else if (Regex.IsMatch(line, "Coconut=", RegexOptions.IgnoreCase))
                    {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.DatabaseQuery.Coconut = false;
                        else param.DatabaseQuery.Coconut = true;
                    }

                    else if (Regex.IsMatch(line, "IsUserDefinedDB=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.IsUserDefinedDB = false;
                        else param.IsUserDefinedDB = true;
                    }
                    else if (Regex.IsMatch(line, "UserDefinedDbFilePath=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.UserDefinedDbFilePath = ans;
                    }
                    else if (Regex.IsMatch(line, "AllProcess=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsAllProcess = true;
                        else param.IsAllProcess = false;
                    }
                    else if (Regex.IsMatch(line, "FormulaFinder=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.IsFormulaFinder = false;
                        else param.IsFormulaFinder = true;
                    }
                    else if (Regex.IsMatch(line, "StructureFinder=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsStructureFinder = true;
                        else param.IsStructureFinder = false;
                    }
                    else if (Regex.IsMatch(line, "TryTopNMolecularFormulaSearch=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (int.TryParse(ans, out intValue)) param.TryTopNmolecularFormulaSearch = intValue;
                    }
                    else if (Regex.IsMatch(line, "IsRunSpectralDbSearch=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.IsRunSpectralDbSearch = false;
                        else param.IsRunSpectralDbSearch = true;
                    }
                    else if (Regex.IsMatch(line, "IsRunInSilicoFragmenterSearch=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsRunInSilicoFragmenterSearch = true;
                        else param.IsRunInSilicoFragmenterSearch = false;
                    }
                    else if (Regex.IsMatch(line, "IsPrecursorOrientedSearch=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsPrecursorOrientedSearch = true;
                        else param.IsPrecursorOrientedSearch = false;
                    }
                    else if (Regex.IsMatch(line, "IsUseInternalExperimentalSpectralDb=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("F")) param.IsUseInternalExperimentalSpectralDb = false;
                        else param.IsUseInternalExperimentalSpectralDb = true;
                    }
                    else if (Regex.IsMatch(line, "IsUseInSilicoSpectralDbForLipids=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseInSilicoSpectralDbForLipids = true;
                        else param.IsUseInSilicoSpectralDbForLipids = false;
                    }
                    else if (Regex.IsMatch(line, "IsUseUserDefinedSpectralDb=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseUserDefinedSpectralDb = true;
                        else param.IsUseUserDefinedSpectralDb = false;
                    }
                    else if (Regex.IsMatch(line, "UserDefinedSpectralDbFilePath=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.UserDefinedSpectralDbFilePath = ans;
                    }
                    else if (Regex.IsMatch(line, "SolventType=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Contains("CH3COONH4")) param.SolventType = SolventType.CH3COONH4;
                        else param.SolventType = SolventType.HCOONH4;

                    }
                    else if (Regex.IsMatch(line, "MassRangeMin=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.MassRangeMin = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "MassRangeMax=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.MassRangeMax = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "IsUsePredictedRtForStructureElucidation=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUsePredictedRtForStructureElucidation = true;
                        else param.IsUsePredictedRtForStructureElucidation = false;
                    }
                    else if (Regex.IsMatch(line, "IsUseRtInchikeyLibrary=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseRtInchikeyLibrary = true;
                        else param.IsUseRtInchikeyLibrary = false;
                    }
                    else if (Regex.IsMatch(line, "IsUseXlogpPrediction=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseXlogpPrediction = true;
                        else param.IsUseXlogpPrediction = false;
                    }
                    else if (Regex.IsMatch(line, "RtSmilesDictionaryFilepath=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.RtSmilesDictionaryFilepath = ans;
                    }
                    else if (Regex.IsMatch(line, "RtInChIKeyDictionaryFilepath=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.RtInChIKeyDictionaryFilepath = ans;
                    }
                    else if (Regex.IsMatch(line, "RtPredictionSummaryReport=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.RtPredictionSummaryReport = ans;
                    }
                    else if (Regex.IsMatch(line, "Coeff_RtPrediction=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.Coeff_RtPrediction = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "Intercept_RtPrediction=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.Intercept_RtPrediction = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "RtToleranceForStructureElucidation=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.RtToleranceForStructureElucidation = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "IsUseExperimentalRtForSpectralSearching=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseExperimentalRtForSpectralSearching = true;
                        else param.IsUseExperimentalRtForSpectralSearching = false;
                    }
                    else if (Regex.IsMatch(line, "RetentionType=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Contains("RT")) param.RetentionType = RetentionType.RT;
                        else param.RetentionType = RetentionType.RI;
                    }
                    else if (Regex.IsMatch(line, "RtToleranceForSpectralSearching=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.RtToleranceForSpectralSearching = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "FseaRelativeAbundanceCutOff=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.FseaRelativeAbundanceCutOff = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "FseanonsignificantDef=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("ReverseSpectrum"))
                            param.FseanonsignificantDef = FseaNonsignificantDef.ReverseSpectrum;
                        else if (ans.Length > 0 && ans.Contains("LowAbundantIons"))
                            param.FseanonsignificantDef = FseaNonsignificantDef.LowAbundantIons;
                        else
                            param.FseanonsignificantDef = FseaNonsignificantDef.OntologySpace;
                    }
                    else if (Regex.IsMatch(line, "FseaPvalueCutOff=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.FseaPvalueCutOff = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "IsMmnLocalCytoscape=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMmnLocalCytoscape = true;
                        else param.IsMmnLocalCytoscape = false;
                    }
                    else if (Regex.IsMatch(line, "IsMmnMsdialOutput=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMmnMsdialOutput = true;
                        else param.IsMmnMsdialOutput = false;
                    }
                    else if (Regex.IsMatch(line, "IsMmnFormulaBioreaction=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMmnFormulaBioreaction = true;
                        else param.IsMmnFormulaBioreaction = false;
                    }
                    else if (Regex.IsMatch(line, "IsMmnRetentionRestrictionUsed=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMmnRetentionRestrictionUsed = true;
                        else param.IsMmnRetentionRestrictionUsed = false;
                    }
                    else if (Regex.IsMatch(line, "IsMmnOntologySimilarityUsed=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsMmnOntologySimilarityUsed = true;
                        else param.IsMmnOntologySimilarityUsed = false;
                    }
                    else if (Regex.IsMatch(line, "MmnMassTolerance=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.MmnMassTolerance = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "MmnRelativeCutoff=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.MmnRelativeCutoff = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "MmnMassSimilarityCutOff=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.MmnMassSimilarityCutOff = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "MmnRtTolerance=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.MmnRtTolerance = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "MmnOntologySimilarityCutOff=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.MmnOntologySimilarityCutOff = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "MmnOutputFolderPath=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.MmnOutputFolderPath = ans;
                    } else if (Regex.IsMatch(line, "MS2PositiveAdductIonList=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.MS2PositiveAdductIonList = ReadAdductLists(ans);
                    }
                    else if (Regex.IsMatch(line, "MS1PositiveAdductIonList=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.MS1PositiveAdductIonList = ReadAdductLists(ans);
                    }
                    else if (Regex.IsMatch(line, "MS2NegativeAdductIonList=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.MS2NegativeAdductIonList = ReadAdductLists(ans);
                    }
                    else if (Regex.IsMatch(line, "MS1NegativeAdductIonList=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.MS1NegativeAdductIonList = ReadAdductLists(ans);
                    }
                    else if (Regex.IsMatch(line, "FormulaPredictionTimeOut=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.FormulaPredictionTimeOut = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "StructurePredictionTimeOut=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.StructurePredictionTimeOut = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "IsUseRtForFilteringCandidates=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseRtForFilteringCandidates = true;
                        else param.IsUseRtForFilteringCandidates = false;
                    }
                    else if (Regex.IsMatch(line, "IsUsePredictedCcsForStructureElucidation=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUsePredictedCcsForStructureElucidation = true;
                        else param.IsUsePredictedCcsForStructureElucidation = false;
                    }
                    else if (Regex.IsMatch(line, "IsUseCcsInchikeyAdductLibrary=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseCcsInchikeyAdductLibrary = true;
                        else param.IsUseCcsInchikeyAdductLibrary = false;
                    }
                    else if (Regex.IsMatch(line, "IsUseExperimentalCcsForSpectralSearching=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseExperimentalCcsForSpectralSearching = true;
                        else param.IsUseExperimentalCcsForSpectralSearching = false;
                    }
                    else if (Regex.IsMatch(line, "IsUseCcsForFilteringCandidates=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (ans.Length > 0 && ans.Contains("T")) param.IsUseCcsForFilteringCandidates = true;
                        else param.IsUseCcsForFilteringCandidates = false;
                    }
                    else if (Regex.IsMatch(line, "CcsToleranceForStructureElucidation=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.CcsToleranceForStructureElucidation = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "CcsToleranceForSpectralSearching=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.CcsToleranceForSpectralSearching = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "CcsAdductInChIKeyDictionaryFilepath=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        param.CcsAdductInChIKeyDictionaryFilepath = ans;
                    }
                    else if (Regex.IsMatch(line, "StructureScoreCutOff=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.StructureScoreCutOff = doubleValue;
                    }
                    else if (Regex.IsMatch(line, "ScoreCutOffForSpectralMatch=", RegexOptions.IgnoreCase)) {
                        ans = line.Substring(line.Split('=')[0].Length + 1).Trim();
                        if (double.TryParse(ans, out doubleValue)) param.ScoreCutOffForSpectralMatch = doubleValue;
                    }
                    #endregion
                }
            }

            param.LipidQueryBean = new LipidQueryBean() { LbmQueries = LbmQueryParcer.GetLbmQueries(iniLipidPath, false) };

            finalParametersCheck(param);
            return param;
        }

        private static List<AdductIon> ReadAdductLists(string value){
            var AdductList = new List<AdductIon>();
            foreach (var adductName in value.Split(',').ToList()) {
                var adduct = AdductIonParcer.GetAdductIonBean(adductName);
                if(adduct.FormatCheck)
                    AdductList.Add(adduct);
            }
            return AdductList;
        }

        private static void finalParametersCheck(AnalysisParamOfMsfinder param)
        {
            var counter = 0;
            
            if (param.IsPubChemAllTime) counter++;
            if (param.IsPubChemNeverUse) counter++;
            if (param.IsPubChemOnlyUseForNecessary) counter++;
            
            if (counter != 1)
            {
                param.IsPubChemNeverUse = true;
                param.IsPubChemOnlyUseForNecessary = false;
                param.IsPubChemAllTime = false;
            }

            counter = 0;

            if (param.IsMinesAllTime) counter++;
            if (param.IsMinesNeverUse) counter++;
            if (param.IsMinesOnlyUseForNecessary) counter++;

            if (counter != 1)
            {
                param.IsMinesNeverUse = true;
                param.IsMinesOnlyUseForNecessary = false;
                param.IsMinesAllTime = false;
            }

            if (param.IsAllProcess)
            {
                param.IsFormulaFinder = true;
                param.IsStructureFinder = true;
            }

            if (param.IsTmsMeoxDerivative) param.IsSiCheck = true;
        }

        public static void Write(AnalysisParamOfMsfinder param, string output = "")
        {
            if (output == "") {
                //output = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\MSFINDER.INI";
                output = System.AppDomain.CurrentDomain.BaseDirectory + "MSFINDER.INI";
            }

            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII))
            {
                sw.WriteLine("#Formula finder parameters");
                
                sw.WriteLine("LewisAndSeniorCheck=" + param.IsLewisAndSeniorCheck);
                sw.WriteLine("Ms1Tolerance=" + param.Mass1Tolerance);
                sw.WriteLine("IsotopicAbundanceTolerance=" + param.IsotopicAbundanceTolerance);
                sw.WriteLine("MassToleranceType=" + param.MassTolType);

                if (param.CoverRange == CoverRange.CommonRange) sw.WriteLine("CommonRange=TRUE"); else sw.WriteLine("CommonRange=FALSE");
                if (param.CoverRange == CoverRange.ExtendedRange) sw.WriteLine("ExtendedRange=TRUE"); else sw.WriteLine("ExtendedRange=FALSE");
                if (param.CoverRange == CoverRange.ExtremeRange) sw.WriteLine("ExtremeRange=TRUE"); else sw.WriteLine("ExtremeRange=FALSE");

                sw.WriteLine("ElementProbabilityCheck=" + param.IsElementProbabilityCheck);
                //sw.WriteLine("HalgenCheck=" + param.IsHalogenCheck);

                sw.WriteLine("Ocheck=" + param.IsOcheck);
                sw.WriteLine("Ncheck=" + param.IsNcheck);
                sw.WriteLine("Pcheck=" + param.IsPcheck);
                sw.WriteLine("Scheck=" + param.IsScheck);
                sw.WriteLine("Fcheck=" + param.IsFcheck);
                sw.WriteLine("ClCheck=" + param.IsClCheck);
                sw.WriteLine("BrCheck=" + param.IsBrCheck);
                sw.WriteLine("Icheck=" + param.IsIcheck);
                sw.WriteLine("SiCheck=" + param.IsSiCheck);

                sw.WriteLine("IsTmsMeoxDerivative=" + param.IsTmsMeoxDerivative);
                sw.WriteLine("MinimumTmsCount=" + param.MinimumTmsCount);
                sw.WriteLine("MinimumMeoxCount=" + param.MinimumMeoxCount);
                sw.WriteLine("CanExcuteMS1AdductSearch=" + param.CanExcuteMS1AdductSearch);
                sw.WriteLine("CanExcuteMS2AdductSearch=" + param.CanExcuteMS2AdductSearch);
                if (param.MS2PositiveAdductIonList != null && param.MS2PositiveAdductIonList.Count > 0) {
                    sw.WriteLine("MS2PositiveAdductIonList=" + string.Join(",", param.MS2PositiveAdductIonList));
                }
                if (param.MS2NegativeAdductIonList != null && param.MS2NegativeAdductIonList.Count > 0) {
                    sw.WriteLine("MS2NegativeAdductIonList=" + string.Join(",", param.MS2NegativeAdductIonList));
                }
                if (param.MS1PositiveAdductIonList != null && param.MS1PositiveAdductIonList.Count > 0) {
                    sw.WriteLine("MS1PositiveAdductIonList=" + string.Join(",", param.MS1PositiveAdductIonList));
                }
                if (param.MS1NegativeAdductIonList != null && param.MS1NegativeAdductIonList.Count > 0) {
                    sw.WriteLine("MS1NegativeAdductIonList=" + string.Join(",", param.MS1NegativeAdductIonList));
                }

                sw.WriteLine("FormulaMaximumReportNumber=" + param.FormulaMaximumReportNumber);

                sw.WriteLine();

                sw.WriteLine("#Structure finder parameters");

                sw.WriteLine("TreeDepth=" + param.TreeDepth);
                sw.WriteLine("Ms2Tolerance=" + param.Mass2Tolerance);
                sw.WriteLine("RelativeAbundanceCutOff=" + param.RelativeAbundanceCutOff);
                sw.WriteLine("StructureMaximumReportNumber=" + param.StructureMaximumReportNumber);
                sw.WriteLine("IsUseEiFragmentDB=" + param.IsUseEiFragmentDB);
                sw.WriteLine("StructureScoreCutOff=" + param.StructureScoreCutOff);

                sw.WriteLine();

                sw.WriteLine("#Data source");

                sw.WriteLine("MinesNeverUse=" + param.IsMinesNeverUse);
                sw.WriteLine("MinesOnlyUseForNecessary=" + param.IsMinesOnlyUseForNecessary);
                sw.WriteLine("MinesAllways=" + param.IsMinesAllTime);
                sw.WriteLine("PubChemNeverUse=" + param.IsPubChemNeverUse);
                sw.WriteLine("PubChemOnlyUseForNecessary=" + param.IsPubChemOnlyUseForNecessary);
                sw.WriteLine("PubChemAllways=" + param.IsPubChemAllTime);
                sw.WriteLine("HMDB=" + param.DatabaseQuery.Hmdb);
                sw.WriteLine("YMDB=" + param.DatabaseQuery.Ymdb);
                sw.WriteLine("PubChem=" + param.DatabaseQuery.Pubchem);
                sw.WriteLine("SMPDB=" + param.DatabaseQuery.Smpdb);
                sw.WriteLine("UNPD=" + param.DatabaseQuery.Unpd);
                sw.WriteLine("ChEBI=" + param.DatabaseQuery.Chebi);
                sw.WriteLine("PlantCyc=" + param.DatabaseQuery.Plantcyc);
                sw.WriteLine("KNApSAcK=" + param.DatabaseQuery.Knapsack);
                sw.WriteLine("BMDB=" + param.DatabaseQuery.Bmdb);
                sw.WriteLine("FooDB=" + param.DatabaseQuery.Foodb);
                sw.WriteLine("ECMDB=" + param.DatabaseQuery.Ecmdb);
                sw.WriteLine("DrugBank=" + param.DatabaseQuery.Drugbank);
                sw.WriteLine("T3DB=" + param.DatabaseQuery.T3db);
                sw.WriteLine("STOFF=" + param.DatabaseQuery.Stoff);
                sw.WriteLine("NANPDB=" + param.DatabaseQuery.Nanpdb);
                sw.WriteLine("LipidMAPS=" + param.DatabaseQuery.Lipidmaps);
                sw.WriteLine("Urine=" + param.DatabaseQuery.Urine);
                sw.WriteLine("Saliva=" + param.DatabaseQuery.Saliva);
                sw.WriteLine("Feces=" + param.DatabaseQuery.Feces);
                sw.WriteLine("Serum=" + param.DatabaseQuery.Serum);
                sw.WriteLine("Csf=" + param.DatabaseQuery.Csf);
                sw.WriteLine("BLEXP=" + param.DatabaseQuery.Blexp);
                sw.WriteLine("NPA=" + param.DatabaseQuery.Npa);
                sw.WriteLine("COCONUT=" + param.DatabaseQuery.Coconut);
                sw.WriteLine("IsUserDefinedDB=" + param.IsUserDefinedDB);
                sw.WriteLine("UserDefinedDbFilePath=" + param.UserDefinedDbFilePath);

                sw.WriteLine();

                sw.WriteLine("#Spectral database search");

                sw.WriteLine("IsRunSpectralDbSearch=" + param.IsRunSpectralDbSearch);
                sw.WriteLine("IsRunInSilicoFragmenterSearch=" + param.IsRunInSilicoFragmenterSearch);
                sw.WriteLine("IsPrecursorOrientedSearch=" + param.IsPrecursorOrientedSearch);
                sw.WriteLine("IsUseInternalExperimentalSpectralDb=" + param.IsUseInternalExperimentalSpectralDb);
                sw.WriteLine("IsUseInSilicoSpectralDbForLipids=" + param.IsUseInSilicoSpectralDbForLipids);
                sw.WriteLine("IsUseUserDefinedSpectralDb=" + param.IsUseUserDefinedSpectralDb);
                sw.WriteLine("UserDefinedSpectralDbFilePath=" + param.UserDefinedSpectralDbFilePath);
                sw.WriteLine("SolventType=" + param.SolventType);
                sw.WriteLine("MassRangeMin=" + param.MassRangeMin);
                sw.WriteLine("MassRangeMax=" + param.MassRangeMax);
                sw.WriteLine("ScoreCutOffForSpectralMatch=" + param.ScoreCutOffForSpectralMatch);

                sw.WriteLine();

                sw.WriteLine("#Retention time setting for structure elucidation");

                sw.WriteLine("IsUsePredictedRtForStructureElucidation=" + param.IsUsePredictedRtForStructureElucidation);
                sw.WriteLine("IsUseRtInchikeyLibrary=" + param.IsUseRtInchikeyLibrary);
                sw.WriteLine("IsUseXlogpPrediction=" + param.IsUseXlogpPrediction);
                sw.WriteLine("RtInChIKeyDictionaryFilepath=" + param.RtInChIKeyDictionaryFilepath);
                sw.WriteLine("RtSmilesDictionaryFilepath=" + param.RtSmilesDictionaryFilepath);
                sw.WriteLine("Coeff_RtPrediction=" + param.Coeff_RtPrediction);
                sw.WriteLine("Intercept_RtPrediction=" + param.Intercept_RtPrediction);
                sw.WriteLine("RtToleranceForStructureElucidation=" + param.RtToleranceForStructureElucidation);
                sw.WriteLine("RtPredictionSummaryReport=" + param.RtPredictionSummaryReport);
                sw.WriteLine("IsUseRtForFilteringCandidates=" + param.IsUseRtForFilteringCandidates);

                sw.WriteLine();

                sw.WriteLine("#Retention time setting for spectral searching");

                sw.WriteLine("IsUseExperimentalRtForSpectralSearching=" + param.IsUseExperimentalRtForSpectralSearching);
                sw.WriteLine("RetentionType=" + param.RetentionType.ToString());
                sw.WriteLine("RtToleranceForSpectralSearching=" + param.RtToleranceForSpectralSearching);

                sw.WriteLine();

                sw.WriteLine("#CCS setting for structure elucidation");

                sw.WriteLine("CcsToleranceForStructureElucidation=" + param.CcsToleranceForStructureElucidation);
                sw.WriteLine("IsUsePredictedCcsForStructureElucidation=" + param.IsUsePredictedCcsForStructureElucidation);
                sw.WriteLine("IsUseCcsInchikeyAdductLibrary=" + param.IsUseCcsInchikeyAdductLibrary);
                sw.WriteLine("CcsAdductInChIKeyDictionaryFilepath=" + param.CcsAdductInChIKeyDictionaryFilepath);
                sw.WriteLine("IsUseExperimentalCcsForSpectralSearching=" + param.IsUseExperimentalCcsForSpectralSearching);
                sw.WriteLine("CcsToleranceForSpectralSearching=" + param.CcsToleranceForSpectralSearching);
                sw.WriteLine("IsUseCcsForFilteringCandidates=" + param.IsUseCcsForFilteringCandidates);

                sw.WriteLine();

                sw.WriteLine("#Batch job");

                sw.WriteLine("AllProcess=" + param.IsAllProcess);
                sw.WriteLine("FormulaFinder=" + param.IsFormulaFinder);
                sw.WriteLine("StructureFinder=" + param.IsStructureFinder);
                sw.WriteLine("TryTopNMolecularFormulaSearch=" + param.TryTopNmolecularFormulaSearch);

                sw.WriteLine();

                sw.WriteLine("#FSEA parameter");
                sw.WriteLine("FseaRelativeAbundanceCutOff=" + param.FseaRelativeAbundanceCutOff);
                sw.WriteLine("FseanonsignificantDef=" + param.FseanonsignificantDef.ToString());
                sw.WriteLine("FseaPvalueCutOff=" + param.FseaPvalueCutOff);

                sw.WriteLine();

                sw.WriteLine("#Msfinder molecular networking (mmn)");
                sw.WriteLine("IsMmnLocalCytoscape=" + param.IsMmnLocalCytoscape);
                sw.WriteLine("IsMmnMsdialOutput=" + param.IsMmnMsdialOutput);
                sw.WriteLine("IsMmnFormulaBioreaction=" + param.IsMmnFormulaBioreaction);
                sw.WriteLine("IsMmnRetentionRestrictionUsed=" + param.IsMmnRetentionRestrictionUsed);
                sw.WriteLine("IsMmnOntologySimilarityUsed=" + param.IsMmnOntologySimilarityUsed);
                sw.WriteLine("MmnMassTolerance=" + param.MmnMassTolerance);
                sw.WriteLine("MmnRelativeCutoff=" + param.MmnRelativeCutoff);
                sw.WriteLine("MmnMassSimilarityCutOff=" + param.MmnMassSimilarityCutOff);
                sw.WriteLine("MmnRtTolerance=" + param.MmnRtTolerance);
                sw.WriteLine("MmnOntologySimilarityCutOff=" + param.MmnOntologySimilarityCutOff);
                sw.WriteLine("MmnOutputFolderPath=" + param.MmnOutputFolderPath);

                sw.WriteLine();

                sw.WriteLine("#Time out parameter");
                sw.WriteLine("FormulaPredictionTimeOut=" + param.FormulaPredictionTimeOut);
                sw.WriteLine("StructurePredictionTimeOut=" + param.StructurePredictionTimeOut);

                sw.WriteLine();
            }
        }

        public static void WriteLipidINI(AnalysisParamOfMsfinder param, string output = "") {
            if (output == "") {
                //output = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\LipidQueries.INI";
                output = System.AppDomain.CurrentDomain.BaseDirectory + "LipidQueries.INI";
            }

            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Class\tAdduct\tIon mode\tIsSelected");
                var queries = param.LipidQueryBean.LbmQueries;
                foreach (var query in queries) {
                    sw.WriteLine(query.LbmClass + "\t" + query.AdductIon.AdductIonName + "\t" + query.IonMode.ToString() + "\t" + query.IsSelected.ToString());
                }
            }
        }
    }
}
