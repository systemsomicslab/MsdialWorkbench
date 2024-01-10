using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;


namespace CompMs.MsdialCore.Export
{
    internal sealed class MrmprobsExporter {

    }

    internal sealed class EsiMrmprobsExporter
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            MSDecLoader msdecLoader,
            AlignmentSpotProperty alignedSpot,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }

            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                if (!alignedSpot.MatchResults.IsReferenceMatched(_evaluator)) return;

                MoleculeMsReference reference = _refer.Refer(alignedSpot.MatchResults.Representative);
                var name = StringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + alignedSpot.MasterAlignmentID);
                var precursorMz = Math.Round(reference.PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(alignedSpot.TimesCenter.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                var rtEnd = Math.Round(alignedSpot.TimesCenter.RT.Value + (float)parameter.MpRtTolerance, 2);
                var rt = Math.Round(alignedSpot.TimesCenter.RT.Value, 2);

                writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, reference, parameter);

                if (parameter.MpIsExportOtherCandidates) {
                    var ms2Dec = msdecLoader.LoadMSDecResult(alignedSpot.MSDecResultIdUsed);
                    var spectrum = ms2Dec.Spectrum;
                    if (spectrum != null && spectrum.Count > 0) {
                        spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                    }

                    var query = queryFactory.Create(alignedSpot, ms2Dec, alignedSpot.IsotopicPeaks.Select(p => new RawPeakElement { Mz = p.Mass, Intensity = p.AbsoluteAbundance }).ToArray(), alignedSpot.PeakCharacter, searchParameter);
                    var candidates = query.FindCandidates();

                    foreach (var candidate in candidates) {
                        var r = _refer.Refer(candidate);
                        if (r == reference) {
                            continue;
                        }

                        name = StringReplaceForWindowsAcceptableCharacters(r.Name + "_" + alignedSpot.MasterAlignmentID);
                        precursorMz = Math.Round(r.PrecursorMz, 5);

                        writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, r, parameter);
                    }
                }
            }
        }

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            MSDecLoader msdecLoader,
            ObservableCollection<AlignmentSpotProperty> alignedSpots,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }

            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                foreach (var spot in alignedSpots) {
                    if (!spot.MatchResults.IsReferenceMatched(_evaluator)) continue;
                    if (!string.IsNullOrEmpty(spot.Comment) && spot.Comment.IndexOf("unk", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                    MoleculeMsReference reference = _refer.Refer(spot.MatchResults.Representative);
                    var name = StringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + spot.MasterAlignmentID);
                    var precursorMz = Math.Round(reference.PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(spot.TimesCenter.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.TimesCenter.RT.Value + (float)parameter.MpRtTolerance, 2);
                    var rt = Math.Round(spot.TimesCenter.RT.Value, 2);

                    writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, reference, parameter);

                    if (parameter.MpIsExportOtherCandidates) {
                        var ms2Dec = msdecLoader.LoadMSDecResult(spot.MSDecResultIdUsed);
                        var spectrum = ms2Dec.Spectrum;
                        if (spectrum != null && spectrum.Count > 0) {
                            spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                        }

                        var query = queryFactory.Create(spot, ms2Dec, spot.IsotopicPeaks.Select(p => new RawPeakElement { Mz = p.Mass, Intensity = p.AbsoluteAbundance }).ToArray(), spot.PeakCharacter, searchParameter);
                        var candidates = query.FindCandidates();

                        foreach (var candidate in candidates) {
                            var r = _refer.Refer(candidate);
                            if (r == reference) {
                                continue;
                            }

                            name = StringReplaceForWindowsAcceptableCharacters(r.Name + "_" + spot.MasterAlignmentID);
                            precursorMz = Math.Round(r.PrecursorMz, 5);
                            writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, r, parameter);
                        }
                    }
                }
            }
        }

        public void ExportExperimentalMsmsAsMrmprobsFormat(
            string filepath,
            MSDecResult ms2DecResult,
            AlignmentSpotProperty spotProp,
            MrmprobsExportBaseParameter parameter) {

            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                var name = StringReplaceForWindowsAcceptableCharacters(spotProp.Name + "_" + spotProp.MasterAlignmentID);
                var precursorMz = Math.Round(spotProp.MassCenter, 5);
                var rtBegin = Math.Max(Math.Round(spotProp.TimesCenter.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                var rtEnd = Math.Round(spotProp.TimesCenter.RT.Value + (float)parameter.MpRtTolerance, 2);
                var rt = Math.Round(spotProp.TimesCenter.RT.Value, 2);

                writer.WriteFieldsBasedOnExperiment(name, precursorMz, rt, rtBegin, rtEnd, ms2DecResult, spotProp, parameter);
            }
        }

        public void ExportExperimentalMsmsAsMrmprobsFormat(
            string filepath,
            ObservableCollection<AlignmentSpotProperty> alignmentSpots,
            MSDecLoader msdecLoader,
            MrmprobsExportBaseParameter parameter)
        {
            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                foreach (var spot in alignmentSpots) {
                    var ms2Dec = msdecLoader.LoadMSDecResult(spot.MSDecResultIdUsed);

                    var name = StringReplaceForWindowsAcceptableCharacters(spot.Name + "_" + spot.MasterAlignmentID);
                    var precursorMz = Math.Round(spot.MassCenter, 5);
                    var rtBegin = Math.Max(Math.Round(spot.TimesCenter.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                    var rtEnd = Math.Round(spot.TimesCenter.RT.Value + (float)parameter.MpRtTolerance, 2);
                    var rt = Math.Round(spot.TimesCenter.RT.Value, 2);

                    writer.WriteFieldsBasedOnExperiment(name, precursorMz, rt, rtBegin, rtEnd, ms2Dec, spot, parameter);
                }
            }
        }

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            MSDecLoader msdecLoader,
            ChromatogramPeakFeature peakSpot,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }

            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                if (!peakSpot.MatchResults.IsReferenceMatched(_evaluator)) return;

                MoleculeMsReference reference = _refer.Refer(peakSpot.MatchResults.Representative);
                var name = StringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + peakSpot.MasterPeakID);
                var precursorMz = Math.Round(reference.PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value + (float)parameter.MpRtTolerance, 2);
                var rt = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value, 2);

                writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, reference, parameter);

                if (parameter.MpIsExportOtherCandidates) {
                    var ms2Dec = msdecLoader.LoadMSDecResult(peakSpot.MSDecResultIdUsed);
                    var spectrum = ms2Dec.Spectrum;
                    if (spectrum != null && spectrum.Count > 0) {
                        spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                    }

                    var query = queryFactory.Create(peakSpot, ms2Dec, Array.Empty<RawPeakElement>(), peakSpot.PeakCharacter, searchParameter);
                    var candidates = query.FindCandidates();

                    foreach (var candidate in candidates) {
                        var r = _refer.Refer(candidate);
                        if (r == reference) {
                            continue;
                        }

                        name = StringReplaceForWindowsAcceptableCharacters(r.Name + "_" + peakSpot.MasterPeakID);
                        precursorMz = Math.Round(r.PrecursorMz, 5);

                        writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, r, parameter);
                    }
                }
            }
        }

        public void ExportReferenceMsmsAsMrmprobsFormat(
            string filepath,
            MSDecLoader msdecLoader,
            ObservableCollection<ChromatogramPeakFeature> peakSpots,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }
            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                foreach (var peak in peakSpots) {
                    if (!peak.MatchResults.IsReferenceMatched(_evaluator)) continue;

                    MoleculeMsReference reference = _refer.Refer(peak.MatchResults.Representative);
                    var name = StringReplaceForWindowsAcceptableCharacters(reference.Name + "_" + peak.MasterPeakID);
                    var precursorMz = Math.Round(reference.PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(peak.PeakFeature.ChromXsTop.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                    var rtEnd = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value + (float)parameter.MpRtTolerance, 2);
                    var rt = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value, 2);

                    writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, reference, parameter);

                    if (parameter.MpIsExportOtherCandidates) {
                        var ms2Dec = msdecLoader.LoadMSDecResult(peak.MSDecResultIdUsed);
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

                            name = StringReplaceForWindowsAcceptableCharacters(r.Name + "_" + peak.MasterPeakID);
                            precursorMz = Math.Round(r.PrecursorMz, 5);

                            writer.WriteFieldsBasedOnReference(name, precursorMz, rt, rtBegin, rtEnd, r, parameter);
                        }
                    }
                }
            }

        }

        public void ExportExperimentalMsmsAsMrmprobsFormat(
            string filepath,
            MSDecResult ms2DecResult,
            ChromatogramPeakFeature peakSpot,
            MrmprobsExportBaseParameter parameter)
        {
            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                var name = StringReplaceForWindowsAcceptableCharacters(peakSpot.Name + "_" + peakSpot.MasterPeakID);
                var precursorMz = Math.Round(peakSpot.PrecursorMz, 5);
                var rtBegin = Math.Max(Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                var rtEnd = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value + (float)parameter.MpRtTolerance, 2);
                var rt = Math.Round(peakSpot.PeakFeature.ChromXsTop.RT.Value, 2);

                writer.WriteFieldsBasedOnExperiment(name, precursorMz, rt, rtBegin, rtEnd, ms2DecResult, peakSpot, parameter);
            }
        }

        public static void ExportExperimentalMsmsAsMrmprobsFormat(
            string filepath,
            ObservableCollection<ChromatogramPeakFeature> peakSpots,
            MSDecLoader loader,
            MrmprobsExportBaseParameter parameter)
        {
            using (var writer = new MrmprobsReferenceWriter(filepath)) {
                writer.WriteHeader();

                foreach (var peak in peakSpots) {
                    var ms2Dec = loader.LoadMSDecResult(peak.MSDecResultIdUsed);

                    var name = StringReplaceForWindowsAcceptableCharacters(peak.Name + "_" + peak.MasterPeakID);
                    var precursorMz = Math.Round(peak.PrecursorMz, 5);
                    var rtBegin = Math.Max(Math.Round(peak.PeakFeature.ChromXsTop.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
                    var rtEnd = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value + (float)parameter.MpRtTolerance, 2);
                    var rt = Math.Round(peak.PeakFeature.ChromXsTop.RT.Value, 2);

                    writer.WriteFieldsBasedOnExperiment(name, precursorMz, rt, rtBegin, rtEnd, ms2Dec, peak, parameter);
                }
            }
        }

        private static string StringReplaceForWindowsAcceptableCharacters(string name) {
            var chars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => chars.Contains(c) ? '_' : c).ToArray());
        }
    }

    //TODO: Implement this after the development of GCMS is complete
/*
    internal sealed class EiMrmprobsExporter {
        public void ExportSpectraAsMrmprobsFormat(
            string filepath,
            List<MSDecResult> ms1DecResults,
            int focusedMs1DecID,
            double rtTolerance,
            double ms1Tolerance,
            List<MoleculeMsReference> mspDB,
            int topN,
            bool isReferenceBase) {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                WriteHeaderAsMrmprobsReferenceFormat(sw);

                if (isReferenceBase) {
                    if (mspDB == null || mspDB.Count == 0) return;
                    if (focusedMs1DecID == -1) { // it means all of identified spots will be exported.
                        foreach (var result in ms1DecResults) {
                            if (result.MspDbID < 0) continue;

                            var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                            var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                            var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                            var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                            var rt = Math.Round(result.RetentionTime, 2);

                            WriteFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, mspDB[result.MspDbID]);
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

                        WriteFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, mspDB[result.MspDbID]);
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

                            WriteFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, result);
                        }
                    }
                    else {
                        var result = ms1DecResults[focusedMs1DecID];
                        var compName = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB);
                        var probsName = stringReplaceForWindowsAcceptableCharacters(compName + "_" + result.Ms1DecID);
                        var rtBegin = Math.Max(Math.Round(result.RetentionTime - rtTolerance, 2), 0);
                        var rtEnd = Math.Round(result.RetentionTime + rtTolerance, 2);
                        var rt = Math.Round(result.RetentionTime, 2);

                        WriteFieldsAsMrmprobsReferenceFormat(sw, probsName, rt, rtBegin, rtEnd, ms1Tolerance, topN, result);
                    }
                }
            }
        }

        private static void WriteHeaderAsMrmprobsReferenceFormat(StreamWriter sw) {
            sw.WriteLine("Compound name\tPrecursor mz\tProduct mz\tRT min\tTQ Ratio\tRT begin\tRT end\tMS1 tolerance\tMS2 tolerance\tMS level\tClass");
        }

        private static string StringReplaceForWindowsAcceptableCharacters(string name) {
            var chars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => chars.Contains(c) ? '_' : c).ToArray());
        }

        private static void WriteFieldsAsMrmprobsReferenceFormat(
            StreamWriter sw,
            string name,
            double rt,
            double rtBegin,
            double rtEnd,
            double ms1Tolerance,
            int topN,
            MoleculeMsReference mspLib)
        {
            if (mspLib.Spectrum.Count == 0) return;
            var massSpec = mspLib.Spectrum.OrderByDescending(n => n.Intensity).ToList();

            var quantMass = Math.Round(massSpec[0].Mass, 4);
            var quantIntensity = massSpec[0].Intensity;

            WriteAsMrmprobsReferenceFormat(sw, name, quantMass, quantMass, rt, 100, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");

            for (int i = 1; i < massSpec.Count; i++) {

                if (i > topN - 1) break;

                var mass = Math.Round(massSpec[i].Mass, 4);
                var intensity = massSpec[i].Intensity;

                if (Math.Abs(mass - quantMass) < ms1Tolerance) continue;

                var tqRatio = Math.Round(intensity / quantIntensity * 100, 0);
                if (tqRatio < 0.5) tqRatio = 1;
                if (tqRatio == 100) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.
                WriteAsMrmprobsReferenceFormat(sw, name, mass, mass, rt, tqRatio, rtBegin, rtEnd, ms1Tolerance, ms1Tolerance, 1, "NA");
            }
        }

        private static void WriteAsMrmprobsReferenceFormat(
            StreamWriter sw,
            string name,
            double precursorMz,
            double productMz,
            double rt,
            double tqRatio,
            double rtBegin,
            double rtEnd,
            double ms1Tolerance,
            double ms2Tolerance,
            int msLevel,
            string compoundClass)
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
*/
}
