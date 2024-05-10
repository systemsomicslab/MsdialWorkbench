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
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace CompMs.MsdialCore.Export
{
    internal sealed class MrmprobsExporter {

    }

    public sealed class EsiMrmprobsExporter
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;

        public EsiMrmprobsExporter(IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer)
        {
            _evaluator = evaluator;
            _refer = refer;
        }

        public void ExportReferenceMsms<T>(Stream stream, T peakSpot, MrmprobsExportBaseParameter parameter) where T: IChromatogramPeak, IAnnotatedObject {
            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                MoleculeMsReference? reference = _refer.Refer(peakSpot.MatchResults.Representative);
                if (!peakSpot.MatchResults.IsReferenceMatched(_evaluator) || reference is null || reference.Spectrum.Count == 0) {
                    return;
                }
                writer.WriteFieldsBasedOnReference(peakSpot, reference, parameter);
            }
        }

        public void ExportReferenceMsms(Stream stream, IReadOnlyList<AlignmentSpotProperty> alignedSpots, MrmprobsExportBaseParameter parameter) {
            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                foreach (var spot in alignedSpots) {
                    if (!string.IsNullOrEmpty(spot.Comment) && spot.Comment.IndexOf("unk", StringComparison.OrdinalIgnoreCase) >= 0) {
                        continue;
                    }
                    MoleculeMsReference reference = _refer.Refer(spot.MatchResults.Representative);
                    if (!spot.MatchResults.IsReferenceMatched(_evaluator) || reference is null || reference.Spectrum.Count == 0) {
                        continue;
                    }
                    writer.WriteFieldsBasedOnReference(spot, reference, parameter);
                }
            }
        }

        public void ExportReferenceMsms(Stream stream, IReadOnlyList<ChromatogramPeakFeature> peakSpots, MrmprobsExportBaseParameter parameter) {
            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                foreach (var peak in peakSpots) {
                    MoleculeMsReference reference = _refer.Refer(peak.MatchResults.Representative);
                    if (!peak.MatchResults.IsReferenceMatched(_evaluator) || reference is null || reference.Spectrum.Count == 0) {
                        continue;
                    }
                    writer.WriteFieldsBasedOnReference(peak, reference, parameter);
                }
            }
        }

        public void ExportReferenceMsms(
            Stream stream,
            AlignmentSpotProperty alignedSpot,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MSDecLoader msdecLoader,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }

            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                MoleculeMsReference reference = _refer.Refer(alignedSpot.MatchResults.Representative);
                if (!alignedSpot.MatchResults.IsReferenceMatched(_evaluator) || reference is null || reference.Spectrum.Count == 0) return;

                writer.WriteFieldsBasedOnReference(alignedSpot, reference, parameter);

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
                        writer.WriteFieldsBasedOnReference(alignedSpot, r, parameter);
                    }
                }
            }
        }

        public void ExportReferenceMsms(
            Stream stream,
            ChromatogramPeakFeature peakSpot,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MSDecLoader msdecLoader,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }

            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();

                MoleculeMsReference reference = _refer.Refer(peakSpot.MatchResults.Representative);
                if (!peakSpot.MatchResults.IsReferenceMatched(_evaluator) || reference is null || reference.Spectrum.Count == 0) return;

                writer.WriteFieldsBasedOnReference(peakSpot, reference, parameter);

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

                        writer.WriteFieldsBasedOnReference(peakSpot, r, parameter);
                    }
                }
            }
        }

        public void ExportReferenceMsms(
            Stream stream,
            IReadOnlyList<AlignmentSpotProperty> alignedSpots,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MSDecLoader msdecLoader,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }

            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();

                foreach (var spot in alignedSpots) {
                    if (!string.IsNullOrEmpty(spot.Comment) && spot.Comment.IndexOf("unk", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    MoleculeMsReference reference = _refer.Refer(spot.MatchResults.Representative);
                    if (!spot.MatchResults.IsReferenceMatched(_evaluator) || reference is null || reference.Spectrum.Count == 0) continue;

                    writer.WriteFieldsBasedOnReference(spot, reference, parameter);

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
                            writer.WriteFieldsBasedOnReference(spot, r, parameter);
                        }
                    }
                }
            }
        }

        public void ExportReferenceMsms(
            Stream stream,
            IReadOnlyList<ChromatogramPeakFeature> peakSpots,
            MrmprobsExportBaseParameter parameter,
            IAnnotationQueryFactory<MsScanMatchResult> queryFactory,
            MSDecLoader msdecLoader,
            MsRefSearchParameterBase searchParameter)
        {
            if (parameter.MpIsExportOtherCandidates) {
                searchParameter.TotalScoreCutoff = parameter.MpIdentificationScoreCutOff;
            }
            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();

                foreach (var peak in peakSpots) {
                    MoleculeMsReference reference = _refer.Refer(peak.MatchResults.Representative);
                    if (!peak.MatchResults.IsReferenceMatched(_evaluator) || reference is null || reference.Spectrum.Count == 0) continue;

                    writer.WriteFieldsBasedOnReference(peak, reference, parameter);

                    if (parameter.MpIsExportOtherCandidates) {
                        var ms2Dec = msdecLoader.LoadMSDecResult(peak.MSDecResultIdUsed);
                        var spectrum = ms2Dec.Spectrum;
                        if (spectrum != null && spectrum.Count > 0) {
                            spectrum = spectrum.OrderBy(n => n.Mass).ToList();
                        }

                        var query = queryFactory.Create(peak, ms2Dec, Array.Empty<RawPeakElement>(), peak.PeakCharacter, searchParameter);
                        var candidates = query.FindCandidates();
                        foreach (var candidate in candidates) {
                            var r = _refer.Refer(candidate);
                            if (r == reference) {
                                continue;
                            }
                            writer.WriteFieldsBasedOnReference(peak, r, parameter);
                        }
                    }
                }
            }
        }

        public void ExportExperimentalMsms(
            Stream stream,
            AlignmentSpotProperty spotProp,
            MSDecResult ms2DecResult,
            MrmprobsExportBaseParameter parameter) {

            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                writer.WriteFieldsBasedOnExperiment(spotProp, ms2DecResult, parameter);
            }
        }

        public void ExportExperimentalMsms(
            Stream stream,
            ChromatogramPeakFeature peakSpot,
            MSDecResult ms2DecResult,
            MrmprobsExportBaseParameter parameter)
        {
            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                writer.WriteFieldsBasedOnExperiment(peakSpot, ms2DecResult, parameter);
            }
        }

        public void ExportExperimentalMsms(
            Stream stream,
            IReadOnlyList<AlignmentSpotProperty> alignmentSpots,
            MSDecLoader msdecLoader,
            MrmprobsExportBaseParameter parameter)
        {
            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                foreach (var spot in alignmentSpots) {
                    var ms2Dec = msdecLoader.LoadMSDecResult(spot.MSDecResultIdUsed);
                    writer.WriteFieldsBasedOnExperiment(spot, ms2Dec, parameter);
                }
            }
        }

        public void ExportExperimentalMsms(
            Stream stream,
            IReadOnlyList<ChromatogramPeakFeature> peakSpots,
            MSDecLoader loader,
            MrmprobsExportBaseParameter parameter)
        {
            using (var writer = new MrmprobsReferenceWriter(stream, leaveOpen: true)) {
                writer.WriteHeader();
                foreach (var peak in peakSpots) {
                    var ms2Dec = loader.LoadMSDecResult(peak.MSDecResultIdUsed);
                    writer.WriteFieldsBasedOnExperiment(peak, ms2Dec, parameter);
                }
            }
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
