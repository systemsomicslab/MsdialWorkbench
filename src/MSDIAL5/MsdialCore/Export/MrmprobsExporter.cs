using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using AnalysisParametersBean = CompMs.MsdialCore.Parameter.ParameterBase;
using ProjectPropertyBean = CompMs.MsdialCore.Parameter.ParameterBase;
using MS1DecResult = CompMs.MsdialCore.MSDec.MSDecResult;
using MS2DecResult = CompMs.MsdialCore.MSDec.MSDecResult;
using PeakAreaBean = CompMs.MsdialCore.DataObj.ChromatogramPeakFeature;
using AlignmentPropertyBean = CompMs.MsdialCore.DataObj.AlignmentSpotProperty;
using MspFormatCompoundInformationBean = CompMs.Common.Components.MoleculeMsReference;
using SpectralDeconvolution = CompMs.MsdialCore.Parser.MsdecResultsReader;
using SpectralSimilarity = CompMs.Common.Algorithm.Scoring.MsScanMatching;

namespace CompMs.MsdialCore.Export
{
    internal sealed class MrmprobsExporter {

    }

    internal sealed class EsiMrmprobsExporter
    {
        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, AlignmentPropertyBean alignedSpot,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (alignedSpot.LibraryID < 0 || mspDB.Count == 0 || alignedSpot.MetaboliteName.Contains("w/o")) return;

                var mspID = alignedSpot.LibraryID;
                var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + alignedSpot.AlignmentID);
                var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(alignedSpot.CentralRetentionTime - rtTolerance, 2), 0);
                var rtEnd = Math.Round(alignedSpot.CentralRetentionTime + rtTolerance, 2);
                var rt = Math.Round(alignedSpot.CentralRetentionTime, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                if (isExportOtherCanidates)
                {

                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                    var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, alignedSpot.AlignmentID);
                    var spectrum = ms2Dec.MassSpectra;
                    if (spectrum != null && spectrum.Count > 0)
                        spectrum = spectrum.OrderBy(n => n[0]).ToList();

                    var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(alignedSpot.CentralAccurateMass, param.Ms1LibrarySearchTolerance,
                        alignedSpot.CentralRetentionTime, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                        spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                    mspDB = mspDB.OrderBy(n => n.Id).ToList();

                    foreach (var id in otherCandidateMspIDs)
                    {
                        if (id == alignedSpot.LibraryID) continue;

                        mspID = id;

                        name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + alignedSpot.AlignmentID);
                        precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(alignedSpot.CentralRetentionTime - rtTolerance, 2), 0);
                        rtEnd = Math.Round(alignedSpot.CentralRetentionTime + rtTolerance, 2);
                        rt = Math.Round(alignedSpot.CentralRetentionTime, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                            ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                    }
                }
            }
        }

        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, ObservableCollection<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var spot in alignedSpots)
                {
                    if (spot.LibraryID < 0 || mspDB.Count == 0 || spot.MetaboliteName.Contains("w/o")) continue;
                    if (spot.Comment != null && spot.Comment != string.Empty && spot.Comment.ToLower().Contains("unk")) continue;

                    //internal
                    // if (spot.Comment == null || spot.Comment == string.Empty) continue;

                    var mspID = spot.LibraryID;

                    var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + spot.AlignmentID);
                    var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(spot.CentralRetentionTime - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.CentralRetentionTime + rtTolerance, 2);
                    var rt = Math.Round(spot.CentralRetentionTime, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                        ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                    if (isExportOtherCanidates)
                    {

                        mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                        var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, spot.AlignmentID);
                        var spectrum = ms2Dec.MassSpectra;
                        if (spectrum != null && spectrum.Count > 0)
                            spectrum = spectrum.OrderBy(n => n[0]).ToList();

                        var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(spot.CentralAccurateMass, param.Ms1LibrarySearchTolerance,
                            spot.CentralRetentionTime, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                            spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                        mspDB = mspDB.OrderBy(n => n.Id).ToList();

                        foreach (var id in otherCandidateMspIDs)
                        {
                            if (id == spot.LibraryID) continue;

                            mspID = id;

                            name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + spot.AlignmentID);
                            precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                            rtBegin = Math.Max(Math.Round(spot.CentralRetentionTime - rtTolerance, 2), 0);
                            rtEnd = Math.Round(spot.CentralRetentionTime + rtTolerance, 2);
                            rt = Math.Round(spot.CentralRetentionTime, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                                ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                        }
                    }
                }
            }
        }

        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, MS2DecResult ms2DecResult, AlignmentPropertyBean spotProp,
            double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                var name = stringReplaceForWindowsAcceptableCharacters(spotProp.MetaboliteName + "_" + spotProp.AlignmentID);
                var precursorMz = Math.Round(spotProp.CentralAccurateMass, 5);
                var rtBegin = Math.Max(Math.Round(spotProp.CentralRetentionTime - rtTolerance, 2), 0);
                var rtEnd = Math.Round(spotProp.CentralRetentionTime + rtTolerance, 2);
                var rt = Math.Round(spotProp.CentralRetentionTime, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                    ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
            }
        }

        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, ObservableCollection<AlignmentPropertyBean> alignmentSpots,
            FileStream fs, List<long> seekpoints, double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var spot in alignmentSpots)
                {
                    //if (peak.LibraryID < 0 || peak.PostIdentificationLibraryID < 0) continue;

                    var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, spot.AlignmentID);

                    var name = stringReplaceForWindowsAcceptableCharacters(spot.MetaboliteName + "_" + spot.AlignmentID);
                    var precursorMz = Math.Round(spot.CentralAccurateMass, 5);
                    var rtBegin = Math.Max(Math.Round(spot.CentralRetentionTime - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.CentralRetentionTime + rtTolerance, 2);
                    var rt = Math.Round(spot.CentralRetentionTime, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                        ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
                }
            }
        }

        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, PeakAreaBean peakSpot, List<MspFormatCompoundInformationBean> mspDB,
            AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (peakSpot.LibraryID < 0 || mspDB.Count == 0 || peakSpot.MetaboliteName.Contains("w/o")) return;

                var mspID = peakSpot.LibraryID;
                var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peakSpot.PeakID);
                var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - rtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.RtAtPeakTop + rtTolerance, 2);
                var rt = Math.Round(peakSpot.RtAtPeakTop, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                if (isExportOtherCanidates)
                {

                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                    var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, peakSpot.PeakID);
                    var spectrum = ms2Dec.MassSpectra;
                    if (spectrum != null && spectrum.Count > 0)
                        spectrum = spectrum.OrderBy(n => n[0]).ToList();

                    var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(peakSpot.AccurateMass, param.Ms1LibrarySearchTolerance,
                        peakSpot.RtAtPeakTop, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                        spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                    mspDB = mspDB.OrderBy(n => n.Id).ToList();

                    foreach (var id in otherCandidateMspIDs)
                    {
                        if (id == peakSpot.LibraryID) continue;

                        mspID = id;

                        name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peakSpot.PeakID);
                        precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - rtTolerance, 2), 0);
                        rtEnd = Math.Round(peakSpot.RtAtPeakTop + rtTolerance, 2);
                        rt = Math.Round(peakSpot.RtAtPeakTop, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance,
                            ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);
                    }
                }
            }
        }

        public static void ExportReferenceMsmsAsMrmprobsFormat(string filepath, FileStream fs, List<long> seekpoints, ObservableCollection<PeakAreaBean> peakSpots, List<MspFormatCompoundInformationBean> mspDB,
           AnalysisParametersBean param, ProjectPropertyBean projectProp)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var peak in peakSpots)
                {
                    if (peak.LibraryID < 0 || mspDB.Count == 0 || peak.MetaboliteName.Contains("w/o")) continue;

                    var mspID = peak.LibraryID;

                    var name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peak.PeakID);
                    var precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(peak.RtAtPeakTop - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(peak.RtAtPeakTop + rtTolerance, 2);
                    var rt = Math.Round(peak.RtAtPeakTop, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);

                    if (isExportOtherCanidates)
                    {

                        mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                        var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, peak.PeakID);
                        var spectrum = ms2Dec.MassSpectra;
                        if (spectrum != null && spectrum.Count > 0)
                            spectrum = spectrum.OrderBy(n => n[0]).ToList();

                        var otherCandidateMspIDs = SpectralSimilarity.GetHighSimilarityMspIDs(peak.AccurateMass, param.Ms1LibrarySearchTolerance,
                            peak.RtAtPeakTop, param.RetentionTimeLibrarySearchTolerance, param.Ms2LibrarySearchTolerance,
                            spectrum, mspDB, identificationScoreCutoff, projectProp.TargetOmics, param.IsUseRetentionInfoForIdentificationScoring);

                        mspDB = mspDB.OrderBy(n => n.Id).ToList();

                        foreach (var id in otherCandidateMspIDs)
                        {
                            if (id == peak.LibraryID) continue;

                            mspID = id;

                            name = stringReplaceForWindowsAcceptableCharacters(mspDB[mspID].Name + "_" + peak.PeakID);
                            precursorMz = Math.Round(mspDB[mspID].PrecursorMz, 5);
                            rtBegin = Math.Max(Math.Round(peak.RtAtPeakTop - rtTolerance, 2), 0);
                            rtEnd = Math.Round(peak.RtAtPeakTop + rtTolerance, 2);
                            rt = Math.Round(peak.RtAtPeakTop, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance,
                                ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, mspDB[mspID]);
                        }
                    }
                }
            }

        }

        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, MS2DecResult ms2DecResult, PeakAreaBean peakSpot,
            double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                var name = stringReplaceForWindowsAcceptableCharacters(peakSpot.MetaboliteName + "_" + peakSpot.PeakID);
                var precursorMz = Math.Round(peakSpot.AccurateMass, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.RtAtPeakTop - rtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.RtAtPeakTop + rtTolerance, 2);
                var rt = Math.Round(peakSpot.RtAtPeakTop, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                    ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
            }
        }

        public static void ExportExperimentalMsmsAsMrmprobsFormat(string filepath, ObservableCollection<PeakAreaBean> peakSpots, FileStream fs, List<long> seekpoints,
            double rtTolerance, double ms1Tolerance, double ms2Tolerance, int topN = 5, bool isIncludeMslevel1 = true, bool isUseMs1LevelForQuant = true)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII))
            {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var peak in peakSpots)
                {
                    //if (peak.LibraryID < 0 && peak.PostIdentificationLibraryId < 0) continue;

                    var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoints, peak.PeakID);

                    var name = stringReplaceForWindowsAcceptableCharacters(peak.MetaboliteName + "_" + peak.PeakID);
                    var precursorMz = Math.Round(peak.AccurateMass, 5);
                    var rtBegin = Math.Max(Math.Round(peak.RtAtPeakTop - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(peak.RtAtPeakTop + rtTolerance, 2);
                    var rt = Math.Round(peak.RtAtPeakTop, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd,
                        ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
                }
            }
        }

        private static void writeHeaderAsMrmprobsReferenceFormat(StreamWriter sw)
        {
            sw.WriteLine("Compound name\tPrecursor mz\tProduct mz\tRT min\tTQ Ratio\tRT begin\tRT end\tMS1 tolerance\tMS2 tolerance\tMS level\tClass");
        }

        private static string stringReplaceForWindowsAcceptableCharacters(string name)
        {
            var chars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => chars.Contains(c) ? '_' : c).ToArray());
        }

        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double precursorMz, double rt, double rtBegin, double rtEnd, double ms1Tolerrance,
            double ms2Tolerance, int topN, bool isIncludeMslevel1, bool isUseMs1LevelForQuant, MspFormatCompoundInformationBean mspQuery)
        {
            if ((isIncludeMslevel1 == false || isUseMs1LevelForQuant == true) && mspQuery.MzIntensityCommentBeanList.Count == 0) return;

            var massSpec = mspQuery.MzIntensityCommentBeanList.OrderByDescending(n => n.Intensity).ToList();
            var compClass = mspQuery.CompoundClass;
            if (compClass == null || compClass == string.Empty) compClass = "NA";

            if (isIncludeMslevel1)
            {
                var tqRatio = 100;
                if (!isUseMs1LevelForQuant) tqRatio = 150;
                // Since we cannot calculate the real QT ratio from the reference DB and the real MS1 value (actually I can calculate them from the raw data with the m/z matching),
                //currently the ad hoc value 150 is used.

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, precursorMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 1, compClass);
            }

            if (topN == 1 && isIncludeMslevel1) return;
            if (mspQuery.MzIntensityCommentBeanList.Count == 0) return;
            var basePeak = massSpec[0].Intensity;
            for (int i = 0; i < massSpec.Count; i++)
            {
                if (i > topN - 1) break;
                var productMz = Math.Round(massSpec[i].Mz, 5);
                var tqRatio = Math.Round(massSpec[i].Intensity / basePeak * 100, 0);
                if (isUseMs1LevelForQuant && i == 0) tqRatio = 99;
                else if (!isUseMs1LevelForQuant && i == 0) tqRatio = 100;
                else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                if (tqRatio == 0) tqRatio = 1;

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, productMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 2, compClass);
            }
        }
    }

    internal sealed class EiMrmprobsExporter
    {
        public static void ExportSpectraAsMrmprobsFormat(string filepath, List<MS1DecResult> ms1DecResults, int focusedMs1DecID,
            double rtTolerance, double ms1Tolerance, List<MspFormatCompoundInformationBean> mspDB, int topN = 5, bool isReferenceBase = true) {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {

                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (isReferenceBase == true) {
                    if (mspDB == null || mspDB.Count == 0) return;
                    if (focusedMs1DecID == -1) { // it means all of identified spots will be exported.
                        foreach (var result in ms1DecResults) {
                            if (result.MspDbID < 0) continue;

                            var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                            var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                            var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                            var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                            var rt = Math.Round(result.RetentionTime, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, mspDB[result.MspDbID]);
                        }
                    }
                    else {
                        var result = ms1DecResults[focusedMs1DecID];
                        if (result.MspDbID < 0) return;
                        var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                        var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                        var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                        var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                        var rt = Math.Round(result.RetentionTime, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, mspDB[result.MspDbID]);
                    }
                }
                else {
                    if (focusedMs1DecID == -1) { // it means all of identified spots will be exported.
                        foreach (var result in ms1DecResults) {
                            //if (result.MspDbID < 0) continue;

                            var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                            var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                            var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                            var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                            var rt = Math.Round(result.RetentionTime, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, result);
                        }
                    }
                    else {
                        var result = ms1DecResults[focusedMs1DecID];
                        var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                        var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                        var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                        var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                        var rt = Math.Round(result.RetentionTime, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, result);
                    }
                }
            }
        }

        private static void writeHeaderAsMrmprobsReferenceFormat(StreamWriter sw)
        {
            sw.WriteLine("Compound name\tPrecursor mz\tProduct mz\tRT min\tTQ Ratio\tRT begin\tRT end\tMS1 tolerance\tMS2 tolerance\tMS level\tClass");
        }

        private static string stringReplaceForWindowsAcceptableCharacters(string name)
        {
            var chars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => chars.Contains(c) ? '_' : c).ToArray());
        }

        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double rt, double rtBegin, double rtEnd,
            double ms1Tolerance, int topN, MspFormatCompoundInformationBean mspLib)
        {
            if (mspLib.MzIntensityCommentBeanList.Count == 0) return;
            var massSpec = mspLib.MzIntensityCommentBeanList.OrderByDescending(n => n.Intensity).ToList();

            var quantMass = Math.Round(massSpec[0].Mz, 4);
            var quantIntensity = massSpec[0].Intensity;

            writeAsMrmprobsReferenceFormat(sw, name, quantMass, quantMass, rt, 100, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");

            for (int i = 1; i < massSpec.Count; i++) {

                if (i > topN - 1) break;

                var mass = Math.Round(massSpec[i].Mz, 4);
                var intensity = massSpec[i].Intensity;

                if (Math.Abs(mass - quantMass) < ms1Tolerance) continue;

                var tqRatio = Math.Round(intensity / quantIntensity * 100, 0);
                if (tqRatio < 0.5) tqRatio = 1;
                if (tqRatio == 100) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.
                writeAsMrmprobsReferenceFormat(sw, name, mass, mass, rt, tqRatio, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");
            }
        }

        private static void writeAsMrmprobsReferenceFormat(StreamWriter sw, string name, double precursorMz, double productMz, double rt, double tqRatio, double rtBegin, double rtEnd, double ms1Tolerance, double ms2Tolerance, int msLevel, string compoundClass)
        {
            sw.Write(name + "\t");
            sw.Write(precursorMz + "\t");
            sw.Write(productMz + "\t");
            sw.Write(rt + "\t");
            sw.Write(tqRatio + "\t");
            sw.Write(rtBegin + "\t");
            sw.Write(rtEnd + "\t");
            sw.Write(ms1Tolerance + "\t");
            sw.Write(ms2Tolerance + "\t");
            sw.Write(msLevel + "\t");
            sw.WriteLine(compoundClass);
        }

    }
}
