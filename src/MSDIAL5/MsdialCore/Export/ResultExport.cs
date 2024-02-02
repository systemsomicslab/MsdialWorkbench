using Accord;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Statistics;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.MsdialCore.Export {
    public sealed class ResultExport {
        private ResultExport() { }

        #region chromatogram peak features and msdec result exporter
        public static void WriteChromPeakFeatureExportHeader(StreamWriter sw, MachineCategory category) {
            var header = new List<string>();
            switch (category) {
                case MachineCategory.GCMS:
                    header = new List<string>() {
                        "Peak ID", "Name", "Scan", "RT (min)", "RI", 
                        "Quant mass", "Height", "Area", "Model masses", "Comment",
                        "Reference RT", "Reference RI", "Formula", "Ontology", "InChIKey", "SMILES",
                        "Annotation tag (VS1.0)", "RT matched", "RI matched", "Spectrum matched",
                        "RT similarity", "RI similarity", "Simple dot product", "Weighted dot product", "Reverse dot product", 
                        "Matched peaks count", "Matched peaks percentage", "Total score",
                        "S/N", "Spectrum" };
                    break;
                case MachineCategory.IFMS:
                    header = new List<string>() { "Peak ID", "Name", "Scan", "m/z left", "m/z", "m/z right",
                        "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
                        "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
                        "Annotation tag (VS1.0)", "m/z matched", "MS/MS matched",
                        "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score",
                        "S/N", "MS1 isotopes", "MSMS spectrum" };
                    break;
                case MachineCategory.LCMS:
                    header = new List<string>() { "Peak ID", "Name", "Scan", "RT left(min)", "RT (min)", "RT right (min)",
                        "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
                        "Reference RT", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
                        "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
                        "RT similarity", "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score",
                        "S/N", "MS1 isotopes", "MSMS spectrum" };
                    break;
                case MachineCategory.IMMS:
                    header = new List<string>() { "Peak ID", "Name", "Scan", "Mobility left", "Mobility", "Mobility right",
                        "CCS", "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
                        "Reference CCS", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
                        "Annotation tag (VS1.0)", "CCS matched", "m/z matched", "MS/MS matched",
                        "CCS similarity", "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score",
                        "S/N", "MS1 isotopes", "MSMS spectrum" };
                    break;
                case MachineCategory.LCIMMS:
                    header = new List<string>() { "Peak ID", "Name", "Scan", "RT left(min)", "RT (min)", "RT right (min)", 
                        "Mobility left", "Mobility", "Mobility right",
                        "CCS", "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
                        "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
                        "Annotation tag (VS1.0)", "RT matched", "CCS matched", "m/z matched", "MS/MS matched",
                        "RT similarity", "CCS similarity", "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score",
                        "S/N", "MS1 isotopes", "MSMS spectrum" };
                    break;
                default: header = new List<string>();
                    break;
            }
            sw.WriteLine(String.Join("\t", header));
        }

        public static void WriteChromPeakFeatureMetadata(StreamWriter sw, AnalysisFileBean file, ChromatogramPeakFeature feature,
            MSDecResult msdec, List<RawSpectrum> spectrumList, ParameterBase param, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB) {
            var category = param.MachineCategory;
            var type = param.ExportSpectraType;
            var ms1tol = param.CentroidMs1Tolerance;
            var isotopes = category != MachineCategory.GCMS ? DataAccess.GetIsotopicPeaks(spectrumList, feature.MS1RawSpectrumIdTop, (float)feature.PrecursorMz, ms1tol, param.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum) : new List<IsotopicPeak>();
            var isotopeString = isotopes.IsEmptyOrNull() ? "null" : String.Join(";", isotopes.Select(n => String.Join(" ", new string[] { String.Format("{0:0.00000}", n.Mass), String.Format("{0:0}", n.AbsoluteAbundance) })));
            var spectrum = DataAccess.GetMassSpectrum(spectrumList, msdec, type, msdec.RawSpectrumID, param, file.AcquisitionType);
            var specString = spectrum.IsEmptyOrNull() ? "null" : String.Join(";", spectrum.Select(n => String.Join(" ", new string[] { String.Format("{0:0.00000}", n.Mass), String.Format("{0:0}", n.Intensity) })));
            var peakID = category == MachineCategory.GCMS ? msdec.ScanID.ToString() : feature.MasterPeakID.ToString();
            var name = category == MachineCategory.GCMS ? MoleculeMsRefDataRetrieve.GetCompoundName(msdec.MspID, mspDB) : feature.Name;
            var scanID = category == MachineCategory.GCMS ? msdec.RawSpectrumID.ToString() : feature.MS1RawSpectrumIdTop.ToString();

            var rtLeft = category == MachineCategory.GCMS ? "null" : String.Format("{0:0.000}", feature.ChromXsLeft.RT.Value);
            var rt = category == MachineCategory.GCMS ? String.Format("{0:0.000}", msdec.ChromXs.RT.Value) : String.Format("{0:0.000}", feature.ChromXsTop.RT.Value);
            var rtRight = category == MachineCategory.GCMS ? "null" : String.Format("{0:0.000}", feature.ChromXsRight.RT.Value);

            var ri = category == MachineCategory.GCMS ? msdec.ChromXs.RI.Value <= 0 ? "null" : String.Format("{0:0.000}", msdec.ChromXs.RI.Value) : "null";

            var dtLeft = category == MachineCategory.IMMS || category == MachineCategory.LCIMMS ? String.Format("{0:0.000}", feature.ChromXsLeft.Drift.Value) : "null";
            var dt = category == MachineCategory.IMMS || category == MachineCategory.LCIMMS ? String.Format("{0:0.000}", msdec.ChromXs.Drift.Value) : "null";
            var dtRight = category == MachineCategory.IMMS || category == MachineCategory.LCIMMS ? String.Format("{0:0.000}", feature.ChromXsRight.Drift.Value) : "null";

            var adduct = category == MachineCategory.GCMS ? "null" : feature.AdductType != null ? feature.AdductType.AdductIonName : "null";

            var mzLeft = category == MachineCategory.IFMS ? String.Format("{0:0.00000}", feature.ChromXsLeft.Mz.Value) : "null";
            var mz = category == MachineCategory.GCMS ? String.Format("{0:0.00000}", msdec.ModelPeakMz) : String.Format("{0:0.00000}", feature.PrecursorMz);
            var mzRight = category == MachineCategory.IFMS ? String.Format("{0:0.00000}", feature.ChromXsRight.Mz.Value) : "null";

            var ccs = category == MachineCategory.GCMS ? "null" : feature.CollisionCrossSection <= 0 ? "null" : String.Format("{0:0.000}", feature.CollisionCrossSection);
            var height =  category == MachineCategory.GCMS ? String.Format("{0:0}", msdec.ModelPeakHeight) : String.Format("{0:0}", feature.PeakHeightTop);
            var area = category == MachineCategory.GCMS ? String.Format("{0:0}", msdec.ModelPeakArea) : String.Format("{0:0}", feature.PeakAreaAboveZero);
            var modelmasses = String.Join(" ", msdec.ModelMasses);
            var comment = category == MachineCategory.GCMS ? string.Empty : feature.Comment;
            var isotope = category == MachineCategory.GCMS ? "null" : feature.PeakCharacter.IsotopeWeightNumber.ToString();
            var sn = category == MachineCategory.GCMS ? String.Format("{0:0.00}", msdec.SignalNoiseRatio) : String.Format("{0:0.00}", feature.PeakShape.SignalToNoise);

            var matchedResult = category == MachineCategory.GCMS ? msdec.MspBasedMatchResult :
                feature.TextDbBasedMatchResult != null ? feature.TextDbBasedMatchResult : feature.MspBasedMatchResult;
            var refID = matchedResult != null ? matchedResult.LibraryID : -1;
            var matchedDB = category == MachineCategory.GCMS ? mspDB :
                feature.TextDbBasedMatchResult != null ? textDB : mspDB;

            var refMz = MoleculeMsRefDataRetrieve.GetMzOrNull(refID, matchedDB);
            var refRT = MoleculeMsRefDataRetrieve.GetRTOrNull(refID, matchedDB);
            var refRI = MoleculeMsRefDataRetrieve.GetRIOrNull(refID, matchedDB);
            var refCCS = MoleculeMsRefDataRetrieve.GetCCSOrNull(refID, matchedDB);
            var formula = MoleculeMsRefDataRetrieve.GetFormulaOrNull(refID, matchedDB);
            var ontology = MoleculeMsRefDataRetrieve.GetOntologyOrNull(refID, matchedDB);
            var inchikey = MoleculeMsRefDataRetrieve.GetInChIKeyOrNull(refID, matchedDB);
            var smiles = MoleculeMsRefDataRetrieve.GetSMILESOrNull(refID, matchedDB);

            var annotationCode = DataAccess.GetAnnotationCode(matchedResult, param);
            var rtMatched = matchedResult != null ? matchedResult.IsRtMatch : false;
            var riMatched = matchedResult != null ? matchedResult.IsRiMatch : false;
            var ccsMatched = matchedResult != null ? matchedResult.IsCcsMatch : false;
            var mzMatched = matchedResult != null ? matchedResult.IsPrecursorMzMatch : false;
            var specMatched = matchedResult != null ? matchedResult.IsSpectrumMatch : false;
            var rtSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.RtSimilarity) : "null";
            var riSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.RiSimilarity) : "null";
            var ccsSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.CcsSimilarity) : "null";
            var mzSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.AcurateMassSimilarity) : "null";
            var weightedDotProduct = matchedResult != null ? String.Format("{0:0.00}", matchedResult.WeightedDotProduct) : "null";
            var simpleDotProduct = matchedResult != null ? String.Format("{0:0.00}", matchedResult.SimpleDotProduct) : "null";
            var revDotProduct = matchedResult != null ? String.Format("{0:0.00}", matchedResult.ReverseDotProduct) : "null";
            var matchedPeaksCount = matchedResult != null ? String.Format("{0:0.00}", matchedResult.MatchedPeaksCount) : "null";
            var matchedPeaksPercentage = matchedResult != null ? String.Format("{0:0.00}", matchedResult.MatchedPeaksPercentage) : "null";
            var totalScore = matchedResult != null ? String.Format("{0:0.00}", matchedResult.TotalScore) : "null";
            var metadata = new List<string>();
            switch (category) {
                case MachineCategory.GCMS:
                    metadata = new List<string>() {
                        peakID, name, scanID, rt, ri, mz, height, area,
                        modelmasses, comment, refRT, refRI, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), rtMatched.ToString(), riMatched.ToString(), specMatched.ToString(),
                        rtSimilarity, riSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct, matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, specString
                    };
                    sw.WriteLine(String.Join("\t", metadata));
                    return;
                case MachineCategory.IFMS:
                    metadata = new List<string>() {
                        peakID, name, scanID, mzLeft, mz, mzRight, height, area,
                        modelmasses, adduct, isotope, comment, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, isotopeString, specString
                    };
                    sw.WriteLine(String.Join("\t", metadata));
                    return;
                case MachineCategory.LCMS:
                    metadata = new List<string>() {
                        peakID, name, scanID, rtLeft, rt, rtRight, mz, height, area,
                        modelmasses, adduct, isotope, comment, refRT, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), rtMatched.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        rtSimilarity, mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, isotopeString, specString
                    };
                    sw.WriteLine(String.Join("\t", metadata));
                    return;
                case MachineCategory.IMMS:
                    metadata = new List<string>() {
                        peakID, name, scanID, dtLeft, dt, dtRight, ccs, mz, height, area,
                        modelmasses, adduct, isotope, comment, refCCS, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), ccsMatched.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        ccsSimilarity, mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, isotopeString, specString
                    };
                    sw.WriteLine(String.Join("\t", metadata));
                    return;
                case MachineCategory.LCIMMS:
                    metadata = new List<string>() {
                        peakID, name, scanID, rtLeft, rt, rtRight, dtLeft, dt, dtRight, ccs, mz, height, area,
                        modelmasses, adduct, isotope, comment, refRT, refCCS, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), rtMatched.ToString(), ccsMatched.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        rtSimilarity, ccsSimilarity, mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, isotopeString, specString
                    };
                    sw.WriteLine(String.Join("\t", metadata));
                    return;
                default:
                    return;
            }
        }
        #endregion

        #region alignment result export
        public static void WriteAlignmentResultHeader(StreamWriter sw, MachineCategory category,
            List<AnalysisFileBean> files, List<BasicStats> StatsList = null) {
            var header = new List<string>();
            switch (category) {
                case MachineCategory.GCMS:
                    header = new List<string>() {
                        "Alignment ID", "Average Rt(min)", "Average RI", "Quant mass", "Metabolite name", "Fill %",
                        "Reference RT", "Reference RI", "Formula", "Ontology", "INCHIKEY", "SMILES",
                        "Annotation tag (VS1.0)", "RT matched", "RI matched", "EI-MS matched",
                        "Comment", "Manually modified for quantification", "Manually modified for annotation",
                        "RT similarity", "RI similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
                        "Spectrum reference file name", "EI spectrum" };
                    break;
                case MachineCategory.IFMS:
                    header = new List<string>() { 
                        "Alignment ID", "Average Mz", "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                        "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES",
                        "Annotation tag (VS1.0)", "m/z matched", "MS/MS matched",
                        "Comment", "Manually modified for quantification", "Manually modified for annotation",
                        "Isotope tracking parent ID",  "Isotope tracking weight number", "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
                        "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"  };
                    break;
                case MachineCategory.LCMS:
                    header = new List<string>() {
                        "Alignment ID", "Average Rt(min)", "Average Mz", "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                        "Reference RT", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES",
                        "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
                        "Comment", "Manually modified for quantification", "Manually modified for annotation",
                        "Isotope tracking parent ID",  "Isotope tracking weight number", "RT similarity", "m/z similarity", 
                        "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
                        "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum" };
                    break;
                case MachineCategory.IMMS:
                    header = new List<string>() { 
                        "Alignment ID", "Average Mz","Average mobility", "Average CCS",
                        "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                        "Reference CCS", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", 
                        "Annotation tag (VS1.0)", "CCS matched", "m/z matched", "MS/MS matched",
                        "Comment", "Manually modified for quantification", "Manually modified for annotation",
                        "Isotope tracking parent ID",  "Isotope tracking weight number", "CCS similarity",
                        "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
                        "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum" };
                    break;
                case MachineCategory.LCIMMS:
                    header = new List<string>() {  
                        "Alignment ID", "Average Rt(min)", "Average Mz","Average mobility", "Average CCS",
                        "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                        "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", 
                        "Annotation tag (VS1.0)", "RT matched", "CCS matched", "m/z matched", "MS/MS matched",
                        "Comment", "Manually modified for quantification", "Manually modified for annotation",
                        "Isotope tracking parent ID",  "Isotope tracking weight number", "RT similarity", "CCS similarity",
                        "m/z similarity", "Simple dot product", "Weighted dot product", "Reverse dot product",
                        "Matched peaks count", "Matched peaks percentage", "Total score", "S/N average",
                        "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum" };
                    break;
                default:
                    header = new List<string>();
                    break;
            }

            var naString = string.Empty;
            var avestdString = string.Empty;
            var legendString = string.Empty;
            if (!StatsList.IsEmptyOrNull()) {
                var naList = new List<string>();
                var aveName = new List<string>();
                var stdName = new List<string>();
                foreach (var stats in StatsList.OrEmptyIfNull()) {
                    naList.Add("NA");
                    aveName.Add("Average");
                    stdName.Add("Stdev");
                }
                naString = "\t" + String.Join("\t", naList) + "\t" + String.Join("\t", naList);
                avestdString = "\t" + String.Join("\t", aveName) + "\t" + String.Join("\t", stdName);
                legendString = "\t" + String.Join("\t", StatsList.Select(n => n.Legend)) + "\t" + String.Join("\t", StatsList.Select(n => n.Legend));
            }

            var marginString = MarginSpace(header.Count - 1);
            sw.WriteLine(marginString + "\tClass\t" + String.Join("\t", files.Select(n => n.AnalysisFileClass)) + naString);
            sw.WriteLine(marginString + "\tFile type\t" + String.Join("\t", files.Select(n => n.AnalysisFileType)) + naString);
            sw.WriteLine(marginString + "\tInjection order\t" + String.Join("\t", files.Select(n => n.AnalysisFileAnalyticalOrder)) + naString);
            sw.WriteLine(marginString + "\tBatch ID\t" + String.Join("\t", files.Select(n => n.AnalysisBatch)) + avestdString);
            sw.WriteLine(String.Join("\t", header) + "\t" + String.Join("\t", files.Select(n => n.AnalysisFileName)) + "\t" + legendString);
        }

        private static string MarginSpace(int numColumn) {
            var margin = new string('\t', numColumn - 1);
            return margin;
        }

        public static void WriteAlignmentSpotFeature(StreamWriter sw, AlignmentSpotProperty feature, MSDecResult msdec,
            ParameterBase param, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, string exporttype = "Height", List<BasicStats> StatsList = null) {
            var category = param.MachineCategory;
            var ms1tol = param.CentroidMs1Tolerance;
            var isotopes = feature.IsotopicPeaks;
            var isotopeString = isotopes.IsEmptyOrNull() ? "null" : String.Join(";", isotopes.Select(n => String.Join(" ", new string[] { String.Format("{0:0.00000}", n.Mass), String.Format("{0:0}", n.AbsoluteAbundance) })));
            var spectrum = msdec.Spectrum;
            var specString = spectrum.IsEmptyOrNull() ? "null" : String.Join(";", spectrum.Select(n => String.Join(" ", new string[] { String.Format("{0:0.00000}", n.Mass), String.Format("{0:0}", n.Intensity) })));
            var peakID = feature.MasterAlignmentID.ToString();
            var name = feature.Name;
            var rt = String.Format("{0:0.000}", feature.TimesCenter.RT.Value);
            var ri = String.Format("{0:0.000}", feature.TimesCenter.RI.Value);
            var dt = String.Format("{0:0.000}", feature.TimesCenter.Drift.Value);
            var ccs = feature.CollisionCrossSection <= 0 ? "null" : String.Format("{0:0.000}", feature.CollisionCrossSection);

            var adduct = feature.AdductType != null ? feature.AdductType.AdductIonName : "null";
            var postcuration = GetPostCurationResult(feature);

            var mz = String.Format("{0:0.00000}", feature.MassCenter);
            var quantmass = String.Format("{0:0.00000}", feature.QuantMass);
            var fill = String.Format("{0:0.00}", feature.FillParcentage);
            var msmsAssigned = feature.IsMsmsAssigned.ToString();
            var comment = feature.Comment;
            var isotope = feature.PeakCharacter.IsotopeWeightNumber.ToString();
            var isotopeParent = feature.PeakCharacter.IsotopeParentPeakID.ToString();
            var sn = String.Format("{0:0.00}", feature.SignalToNoiseAve);

            var isManuallyModForAnnotation = feature.IsManuallyModifiedForAnnotation.ToString();
            var isManuallyModForQuant = feature.IsManuallyModifiedForQuant.ToString();
            var repFileName = feature.AlignedPeakProperties[feature.RepresentativeFileID].FileName;

            var matchedResult = feature.TextDbBasedMatchResult != null ? feature.TextDbBasedMatchResult : feature.MspBasedMatchResult;
            var refID = matchedResult != null ? matchedResult.LibraryID : -1;
            var matchedDB = feature.TextDbBasedMatchResult != null ? textDB : mspDB;

            var refMz = MoleculeMsRefDataRetrieve.GetMzOrNull(refID, matchedDB);
            var refRT = MoleculeMsRefDataRetrieve.GetRTOrNull(refID, matchedDB);
            var refRI = MoleculeMsRefDataRetrieve.GetRIOrNull(refID, matchedDB);
            var refCCS = MoleculeMsRefDataRetrieve.GetCCSOrNull(refID, matchedDB);
            var formula = MoleculeMsRefDataRetrieve.GetFormulaOrNull(refID, matchedDB);
            var ontology = MoleculeMsRefDataRetrieve.GetOntologyOrNull(refID, matchedDB);
            var inchikey = MoleculeMsRefDataRetrieve.GetInChIKeyOrNull(refID, matchedDB);
            var smiles = MoleculeMsRefDataRetrieve.GetSMILESOrNull(refID, matchedDB);

            var annotationCode = DataAccess.GetAnnotationCode(matchedResult, param);
            var rtMatched = matchedResult != null ? matchedResult.IsRtMatch : false;
            var riMatched = matchedResult != null ? matchedResult.IsRiMatch : false;
            var ccsMatched = matchedResult != null ? matchedResult.IsCcsMatch : false;
            var mzMatched = matchedResult != null ? matchedResult.IsPrecursorMzMatch : false;
            var specMatched = matchedResult != null ? matchedResult.IsSpectrumMatch : false;
            var rtSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.RtSimilarity) : "null";
            var riSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.RiSimilarity) : "null";
            var ccsSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.CcsSimilarity) : "null";
            var mzSimilarity = matchedResult != null ? String.Format("{0:0.00}", matchedResult.AcurateMassSimilarity) : "null";
            var weightedDotProduct = matchedResult != null ? String.Format("{0:0.00}", matchedResult.WeightedDotProduct) : "null";
            var simpleDotProduct = matchedResult != null ? String.Format("{0:0.00}", matchedResult.SimpleDotProduct) : "null";
            var revDotProduct = matchedResult != null ? String.Format("{0:0.00}", matchedResult.ReverseDotProduct) : "null";
            var matchedPeaksCount = matchedResult != null ? String.Format("{0:0.00}", matchedResult.MatchedPeaksCount) : "null";
            var matchedPeaksPercentage = matchedResult != null ? String.Format("{0:0.00}", matchedResult.MatchedPeaksPercentage) : "null";
            var totalScore = matchedResult != null ? String.Format("{0:0.00}", matchedResult.TotalScore) : "null";
            var metadata = new List<string>();
            switch (category) {
                case MachineCategory.GCMS:
                    metadata = new List<string>() {
                        peakID, rt, ri, quantmass, name, fill,
                        refRT, refRI, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), rtMatched.ToString(), riMatched.ToString(), specMatched.ToString(),
                        comment, isManuallyModForQuant, isManuallyModForAnnotation,
                        rtSimilarity, riSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct, matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, repFileName, specString
                    };
                    break;
                case MachineCategory.IFMS:
                    metadata = new List<string>() {
                        peakID, mz, name, adduct, postcuration,
                        fill, msmsAssigned, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        comment, isManuallyModForQuant, isManuallyModForAnnotation, isotopeParent, isotope, 
                        mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, repFileName, isotopeString, specString
                    };
                    break;
                case MachineCategory.LCMS:
                    metadata = new List<string>() {
                        peakID, rt, mz, name, adduct, postcuration,
                        fill, msmsAssigned, refRT, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), rtMatched.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        comment, isManuallyModForQuant, isManuallyModForAnnotation, isotopeParent, isotope, rtSimilarity,
                        mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, repFileName, isotopeString, specString
                    };
                    break;
                case MachineCategory.IMMS:
                    metadata = new List<string>() {
                        peakID, mz, dt, ccs, name, adduct, postcuration,
                        fill, msmsAssigned, refCCS, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), ccsMatched.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        comment, isManuallyModForQuant, isManuallyModForAnnotation, isotopeParent, isotope, ccsSimilarity,
                        mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, repFileName, isotopeString, specString
                    };
                    break;
                case MachineCategory.LCIMMS:
                    metadata = new List<string>() {
                        peakID, rt, mz, dt, ccs, name, adduct, postcuration,
                        fill, msmsAssigned, refRT, refCCS, refMz, formula, ontology, inchikey, smiles,
                        annotationCode.ToString(), rtMatched.ToString(), ccsMatched.ToString(), mzMatched.ToString(), specMatched.ToString(),
                        comment, isManuallyModForQuant, isManuallyModForAnnotation, isotopeParent, isotope, rtSimilarity, ccsSimilarity, 
                        mzSimilarity, simpleDotProduct, weightedDotProduct, revDotProduct,
                        matchedPeaksCount, matchedPeaksPercentage, totalScore,
                        sn, repFileName, isotopeString, specString
                    };
                    break;
                default:
                    return;
            }

            var quantValues = GetAlignedQuantValues(feature, exporttype, param);
            var stats = StatsList.IsEmptyOrNull() ? string.Empty : String.Join("\t", StatsList.Select(n => n.Average)) + "\t" + String.Join("\t", StatsList.Select(n => n.Stdev));
            sw.WriteLine(String.Join("\t", metadata) + "\t" + String.Join("\t", quantValues) + "\t" + stats);
        }

        public static List<string> GetAlignedQuantValues(AlignmentSpotProperty feature, string exporttype, ParameterBase param) {
            var quantValues = new List<string>();
            var peaks = feature.AlignedPeakProperties;
            var nonZeroMin = DataAccess.GetInterpolatedValueForMissingValue(peaks, param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples, exporttype);
            foreach (var peak in peaks) {
                var spotValue = DataAccess.GetSpotValueAsString(peak, exporttype);
                if (nonZeroMin >= 0) {
                    double doublevalue = 0.0;
                    double.TryParse(spotValue, out doublevalue);
                    if (doublevalue == 0)
                        doublevalue = nonZeroMin * 0.1;
                    spotValue = doublevalue.ToString();
                }
                quantValues.Add(spotValue);
            }
            return quantValues;
        }

        private static string GetPostCurationResult(AlignmentSpotProperty feature) {
            return "null"; // write something
        }

        #endregion
    }
}
