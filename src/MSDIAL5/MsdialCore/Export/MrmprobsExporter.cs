using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

using AnalysisParametersBean = CompMs.MsdialCore.Parameter.ParameterBase;
using MS1DecResult = CompMs.MsdialCore.MSDec.MSDecResult;
using MS2DecResult = CompMs.MsdialCore.MSDec.MSDecResult;
using PeakAreaBean = CompMs.MsdialCore.DataObj.ChromatogramPeakFeature;
using AlignmentPropertyBean = CompMs.MsdialCore.DataObj.AlignmentSpotProperty;
using MspFormatCompoundInformationBean = CompMs.Common.Components.MoleculeMsReference;
using SpectralDeconvolution = CompMs.MsdialCore.Parser.MsdecResultsReader;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.DataObj;

namespace CompMs.MsdialCore.Export
{
    internal sealed class MrmprobsExporter {

    }

    internal sealed class EsiMrmprobsExporter
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMatchResultRefer<MspFormatCompoundInformationBean, MsScanMatchResult> _refer;

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            Stream fs,
            List<long> seekpoints,
            AlignmentPropertyBean alignedSpot,
            AnalysisParametersBean param,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            if (isExportOtherCanidates) {
                param.MpIdentificationScoreCutOff = searchParameter.TotalScoreCutoff;
            }

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (!alignedSpot.MatchResults.IsReferenceMatched(_evaluator)) return;

                MspFormatCompoundInformationBean reference = _refer.Refer(alignedSpot.MatchResults.Representative);
                var name = stringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + alignedSpot.MasterAlignmentID);
                var precursorMz = Math.Round(reference.PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(alignedSpot.TimesCenter.RT.Value - rtTolerance, 2), 0);
                var rtEnd = Math.Round(alignedSpot.TimesCenter.RT.Value + rtTolerance, 2);
                var rt = Math.Round(alignedSpot.TimesCenter.RT.Value, 2);
                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, reference);

                if (isExportOtherCanidates) {
                    var ms2Dec = SpectralDeconvolution.ReadMSDecResult(fs, seekpoints[alignedSpot.MSDecResultIdUsed], 1, false);
                    var spectrum = ms2Dec.Spectrum;
                    if (spectrum != null && spectrum.Count > 0) {
                        spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                    }

                    var query = queryFactory.Create(alignedSpot, ms2Dec, alignedSpot.IsotopicPeaks.Select(p => new RawPeakElement { Mz = p.Mass, Intensity = p.AbsoluteAbundance }).ToArray(), alignedSpot.PeakCharacter, searchParameter);
                    var candidates = query.FindCandidates().ToArray();

                    foreach (var candidate in candidates) {
                        var r = _refer.Refer(candidate);
                        if (r == reference) {
                            continue;
                        }

                        name = stringReplaceForWindowsAcceptableCharacters(r.Name + "_" + alignedSpot.MasterAlignmentID);
                        precursorMz = Math.Round(r.PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(alignedSpot.TimesCenter.RT.Value - rtTolerance, 2), 0);
                        rtEnd = Math.Round(alignedSpot.TimesCenter.RT.Value + rtTolerance, 2);
                        rt = Math.Round(alignedSpot.TimesCenter.RT.Value, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, r);
                    }
                }
            }
        }

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            Stream fs,
            List<long> seekpoints,
            ObservableCollection<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB,
            AnalysisParametersBean param,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;
            if (isExportOtherCanidates) {
                param.MpIdentificationScoreCutOff = searchParameter.TotalScoreCutoff;
            }

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var spot in alignedSpots) {
                    if (!spot.MatchResults.IsReferenceMatched(_evaluator)) continue;
                    if (!string.IsNullOrEmpty(spot.Comment) && spot.Comment.IndexOf("unk", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                    MspFormatCompoundInformationBean reference = _refer.Refer(spot.MatchResults.Representative);
                    var name = stringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + spot.MasterAlignmentID);
                    var precursorMz = Math.Round(reference.PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(spot.TimesCenter.RT.Value - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.TimesCenter.RT.Value + rtTolerance, 2);
                    var rt = Math.Round(spot.TimesCenter.RT.Value, 2);
                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, reference);

                    if (isExportOtherCanidates) {
                        mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                        var ms2Dec = SpectralDeconvolution.ReadMSDecResult(fs, seekpoints[spot.MSDecResultIdUsed], 1, false);
                        var spectrum = ms2Dec.Spectrum;
                        if (spectrum != null && spectrum.Count > 0) {
                            spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                        }

                        var query = queryFactory.Create(spot, ms2Dec, spot.IsotopicPeaks.Select(p => new RawPeakElement { Mz = p.Mass, Intensity = p.AbsoluteAbundance }).ToArray(), spot.PeakCharacter, searchParameter);
                        var candidates = query.FindCandidates().ToArray();

                        foreach (var candidate in candidates) {
                            var r = _refer.Refer(candidate);
                            if (r == reference) {
                                continue;
                            }

                            name = stringReplaceForWindowsAcceptableCharacters(r.Name + "_" + spot.MasterAlignmentID);
                            precursorMz = Math.Round(r.PrecursorMz, 5);
                            rtBegin = Math.Max(Math.Round(spot.TimesCenter.RT.Value - rtTolerance, 2), 0);
                            rtEnd = Math.Round(spot.TimesCenter.RT.Value + rtTolerance, 2);
                            rt = Math.Round(spot.TimesCenter.RT.Value, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, r);
                        }
                    }
                }
            }
        }

        public void ExportExperimentalMsmsAsMrmprobsFormat(
            string filepath,
            MS2DecResult ms2DecResult,
            AlignmentPropertyBean spotProp,
            double rtTolerance,
            double ms1Tolerance,
            double ms2Tolerance,
            int topN,
            bool isIncludeMslevel1,
            bool isUseMs1LevelForQuant) {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeaderAsMrmprobsReferenceFormat(sw);

                var name = stringReplaceForWindowsAcceptableCharacters(spotProp.Name + "_" + spotProp.MasterAlignmentID);
                var precursorMz = Math.Round(spotProp.MassCenter, 5);
                var rtBegin = Math.Max(Math.Round(spotProp.TimesCenter.RT.Value - rtTolerance, 2), 0);
                var rtEnd = Math.Round(spotProp.TimesCenter.RT.Value + rtTolerance, 2);
                var rt = Math.Round(spotProp.TimesCenter.RT.Value, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
            }
        }

        public void ExportExperimentalMsmsAsMrmprobsFormat(
            string filepath,
            ObservableCollection<AlignmentPropertyBean> alignmentSpots,
            Stream fs,
            List<long> seekpoints,
            double rtTolerance,
            double ms1Tolerance,
            double ms2Tolerance,
            int topN,
            bool isIncludeMslevel1,
            bool isUseMs1LevelForQuant)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var spot in alignmentSpots) {
                    var ms2Dec = SpectralDeconvolution.ReadMSDecResult(fs, seekpoints[spot.MSDecResultIdUsed], 1, false);

                    var name = stringReplaceForWindowsAcceptableCharacters(spot.Name + "_" + spot.MasterAlignmentID);
                    var precursorMz = Math.Round(spot.MassCenter, 5);
                    var rtBegin = Math.Max(Math.Round(spot.TimesCenter.RT.Value - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.TimesCenter.RT.Value + rtTolerance, 2);
                    var rt = Math.Round(spot.TimesCenter.RT.Value, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2Dec);
                }
            }
        }

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            Stream fs,
            List<long> seekpoints,
            PeakAreaBean peakSpot,
            AnalysisParametersBean param,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;
            if (isExportOtherCanidates) {
                param.MpIdentificationScoreCutOff = searchParameter.TotalScoreCutoff;
            }

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeaderAsMrmprobsReferenceFormat(sw);

                if (!peakSpot.MatchResults.IsReferenceMatched(_evaluator)) return;

                MspFormatCompoundInformationBean reference = _refer.Refer(peakSpot.MatchResults.Representative);
                var name = stringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + peakSpot.MasterPeakID);
                var precursorMz = Math.Round(reference.PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value - rtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value + rtTolerance, 2);
                var rt = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, reference);

                if (isExportOtherCanidates) {
                    var ms2Dec = SpectralDeconvolution.ReadMSDecResult(fs, seekpoints[peakSpot.MSDecResultIdUsed], 1, false);
                    var spectrum = ms2Dec.Spectrum;
                    if (spectrum != null && spectrum.Count > 0) {
                        spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                    }

                    var query = queryFactory.Create(peakSpot, ms2Dec, Array.Empty<RawPeakElement>(), peakSpot.PeakCharacter, searchParameter);
                    var candidates = query.FindCandidates().ToArray();

                    foreach (var candidate in candidates) {
                        var r = _refer.Refer(candidate);
                        if (r == reference) {
                            continue;
                        }

                        name = stringReplaceForWindowsAcceptableCharacters(r.Name + "_" + peakSpot.MasterPeakID);
                        precursorMz = Math.Round(r.PrecursorMz, 5);
                        rtBegin = Math.Max(Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value - rtTolerance, 2), 0);
                        rtEnd = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value + rtTolerance, 2);
                        rt = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value, 2);

                        writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, r);
                    }
                }
            }
        }

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            Stream fs,
            List<long> seekpoints,
            ObservableCollection<PeakAreaBean> peakSpots,
            AnalysisParametersBean param,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            var rtTolerance = param.MpRtTolerance;
            var ms1Tolerance = param.MpMs1Tolerance;
            var ms2Tolerance = param.MpMs2Tolerance;
            var topN = param.MpTopN;
            var isIncludeMslevel1 = param.MpIsIncludeMsLevel1;
            var isUseMs1LevelForQuant = param.MpIsUseMs1LevelForQuant;
            var isExportOtherCanidates = param.MpIsExportOtherCandidates;
            var identificationScoreCutoff = param.MpIdentificationScoreCutOff;
            if (isExportOtherCanidates) {
                param.MpIdentificationScoreCutOff = searchParameter.TotalScoreCutoff;
            }

            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeaderAsMrmprobsReferenceFormat(sw);

                foreach (var peak in peakSpots) {
                    if (!peak.MatchResults.IsReferenceMatched(_evaluator)) continue;

                    MspFormatCompoundInformationBean reference = _refer.Refer(peak.MatchResults.Representative);
                    var name = stringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + peak.MasterPeakID);
                    var precursorMz = Math.Round(reference.PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(peak.PeakFeature.ChromXsTop.RT.Value - rtTolerance, 2), 0);
                    var rtEnd = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value + rtTolerance, 2);
                    var rt = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value, 2);

                    writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, reference);

                    if (isExportOtherCanidates) {
                        var ms2Dec = SpectralDeconvolution.ReadMSDecResult(fs, seekpoints[peak.MSDecResultIdUsed], 1, false);
                        var spectrum = ms2Dec.Spectrum;
                        if (spectrum != null && spectrum.Count > 0) {
                            spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                        }

                        var query = queryFactory.Create(peak, ms2Dec, Array.Empty<RawPeakElement>(), peak.PeakCharacter, searchParameter);
                        var candidates = query.FindCandidates().ToArray();

                        foreach (var candidate in candidates) {
                            var r = _refer.Refer(candidate);
                            if (r == reference) {
                                continue;
                            }

                            name = stringReplaceForWindowsAcceptableCharacters(r.Name + "_" + peak.MasterPeakID);
                            precursorMz = Math.Round(r.PrecursorMz, 5);
                            rtBegin = Math.Max(Math.Round(peak.PeakFeature.ChromXsTop.RT.Value - rtTolerance, 2), 0);
                            rtEnd = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value + rtTolerance, 2);
                            rt = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value, 2);

                            writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, r);
                        }
                    }
                }
            }

        }

        public static void ExportExperimentalMsmsAsMrmprobsFormat(
            string filepath,
            MS2DecResult ms2DecResult,
            PeakAreaBean peakSpot,
            double rtTolerance,
            double ms1Tolerance,
            double ms2Tolerance,
            int topN,
            bool isIncludeMslevel1,
            bool isUseMs1LevelForQuant)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                writeHeaderAsMrmprobsReferenceFormat(sw);

                var name = stringReplaceForWindowsAcceptableCharacters(peakSpot.Name + "_" + peakSpot.MasterPeakID);
                var precursorMz = Math.Round(peakSpot.PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value - rtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value + rtTolerance, 2);
                var rt = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value, 2);

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, rt, rtBegin, rtEnd, ms1Tolerance, ms2Tolerance, topN, isIncludeMslevel1, isUseMs1LevelForQuant, ms2DecResult);
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

        private static void writeFieldsAsMrmprobsReferenceFormat(StreamWriter sw, string name, double precursorMz, double rt, double rtBegin, double rtEnd, double ms1Tolerrance, double ms2Tolerance, int topN, bool isIncludeMslevel1, bool isUseMs1LevelForQuant, MS2DecResult ms2DecResult)
        {
            if (isIncludeMslevel1 == false && ms2DecResult.MassSpectra.Count == 0) return;
            if (isIncludeMslevel1)
            {
                var tqRatio = 99;
                if (isUseMs1LevelForQuant) tqRatio = 100;
                if (tqRatio == 100 && !isUseMs1LevelForQuant) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.
                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, precursorMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 1, "NA");
            }

            if (topN == 1 && isIncludeMslevel1) return;
            if (ms2DecResult.MassSpectra == null || ms2DecResult.MassSpectra.Count == 0) return;

            var massSpec = ms2DecResult.MassSpectra.OrderByDescending(n => n[1]).ToList();
            var baseIntensity = 0.0;

            if (isUseMs1LevelForQuant) baseIntensity = ms2DecResult.Ms1PeakHeight;
            else baseIntensity = massSpec[0][1];

            for (int i = 0; i < massSpec.Count; i++)
            {
                if (i > topN - 1) break;
                var productMz = Math.Round(massSpec[i][0], 5);
                var tqRatio = Math.Round(massSpec[i][1] / baseIntensity * 100, 0);
                if (isUseMs1LevelForQuant && i == 0) tqRatio = 99;
                else if (!isUseMs1LevelForQuant && i == 0) tqRatio = 100;
                else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                if (tqRatio == 0) tqRatio = 1;

                writeFieldsAsMrmprobsReferenceFormat(sw, name, precursorMz, productMz, rt, tqRatio, rtBegin, rtEnd, ms1Tolerrance, ms2Tolerance, 2, "NA");
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
