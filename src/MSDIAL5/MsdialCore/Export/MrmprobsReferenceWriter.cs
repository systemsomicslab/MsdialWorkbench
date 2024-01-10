using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.IO;
using System.Linq;
using System.Text;


namespace CompMs.MsdialCore.Export
{
    internal sealed class MrmprobsReferenceWriter : IDisposable
    {
        private StreamWriter _writer;

        public MrmprobsReferenceWriter(string file)
        {
            _writer = new StreamWriter(file, false, Encoding.ASCII);
        }

        public void WriteHeader() {
            const string header = "Compound name\tPrecursor mz\tProduct mz\tRT min\tTQ Ratio\tRT begin\tRT end\tMS1 tolerance\tMS2 tolerance\tMS level\tClass";
            _writer.WriteLine(header);
        }

        public void WriteFieldsBasedOnReference(
            string name,
            double precursorMz,
            double rt,
            double rtBegin,
            double rtEnd,
            MoleculeMsReference mspQuery,
            MrmprobsExportBaseParameter parameter) {

            if ((!parameter.MpIsIncludeMsLevel1 || parameter.MpIsUseMs1LevelForQuant) && mspQuery.Spectrum.Count == 0) {
                return;
            }

            var massSpec = mspQuery.Spectrum.OrderByDescending(p => p.Intensity).ToList();
            var compClass = mspQuery.CompoundClass;
            if (string.IsNullOrEmpty(compClass)) {
                compClass = "NA";
            }

            if (parameter.MpIsIncludeMsLevel1) {
                var tqRatio = 100;
                if (!parameter.MpIsUseMs1LevelForQuant) tqRatio = 150;
                // Since we cannot calculate the real QT ratio from the reference DB and the real MS1 value (actually I can calculate them from the raw data with the m/z matching),
                //currently the ad hoc value 150 is used.

                WriteFields(_writer, name, precursorMz, precursorMz, rt, tqRatio, rtBegin, rtEnd, 1, compClass, parameter);
            }

            if (parameter.MpTopN == 1 && parameter.MpIsIncludeMsLevel1) return;
            if (mspQuery.Spectrum.Count == 0) return;
            var basePeak = massSpec[0].Intensity;
            for (int i = 0; i < massSpec.Count; i++)
            {
                if (i > parameter.MpTopN - 1) break;
                var productMz = Math.Round(massSpec[i].Mass, 5);
                var tqRatio = Math.Round(massSpec[i].Intensity / basePeak * 100, 0);
                if (parameter.MpIsUseMs1LevelForQuant && i == 0) tqRatio = 99;
                else if (!parameter.MpIsUseMs1LevelForQuant && i == 0) tqRatio = 100;
                else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                if (tqRatio == 0) tqRatio = 1;

                WriteFields(_writer, name, precursorMz, productMz, rt, tqRatio, rtBegin, rtEnd, 2, compClass, parameter);
            }
        }

        public void WriteFieldsBasedOnExperiment(
            string name,
            double precursorMz,
            double rt,
            double rtBegin,
            double rtEnd,
            MSDecResult ms2DecResult,
            IChromatogramPeak peak,
            MrmprobsExportBaseParameter parameter) {

            if (!parameter.MpIsIncludeMsLevel1 && ms2DecResult.Spectrum.Count == 0) return;
            if (parameter.MpIsIncludeMsLevel1) {
                var tqRatio = 99;
                if (parameter.MpIsUseMs1LevelForQuant) tqRatio = 100;
                if (tqRatio == 100 && !parameter.MpIsUseMs1LevelForQuant) tqRatio = 99; // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.

                WriteFields(_writer, name, precursorMz, precursorMz, rt, tqRatio, rtBegin, rtEnd, 1, "NA", parameter);
            }

            if (parameter.MpTopN == 1 && parameter.MpIsIncludeMsLevel1) return;
            if (ms2DecResult.Spectrum is null || ms2DecResult.Spectrum.Count == 0) return;

            var massSpec = ms2DecResult.Spectrum.OrderByDescending(p => p.Intensity).ToList();
            var baseIntensity = 0.0;

            if (parameter.MpIsUseMs1LevelForQuant) baseIntensity = peak.Intensity;
            else baseIntensity = massSpec[0].Intensity;

            for (int i = 0; i < massSpec.Count; i++) {
                if (i > parameter.MpTopN - 1) break;
                var productMz = Math.Round(massSpec[i].Mass, 5);
                var tqRatio = Math.Round(massSpec[i].Intensity / baseIntensity * 100, 0);
                if (parameter.MpIsUseMs1LevelForQuant && i == 0) tqRatio = 99;
                else if (!parameter.MpIsUseMs1LevelForQuant && i == 0) tqRatio = 100;
                else if (i != 0 && tqRatio == 100) tqRatio = 99;  // 100 is used just once for the target (quantified) m/z trace. Otherwise, non-100 value should be used.
                if (tqRatio == 0) tqRatio = 1;

                WriteFields(_writer, name, precursorMz, productMz, rt, tqRatio, rtBegin, rtEnd, 2, "NA", parameter);
            }
        }

        private static void WriteFields(
            StreamWriter sw,
            string name,
            double precursorMz,
            double productMz,
            double rt,
            double tqRatio,
            double rtBegin,
            double rtEnd,
            int msLevel,
            string compoundClass,
            MrmprobsExportBaseParameter parameter) {
            sw.Write(name + "\t");
            sw.Write(precursorMz + "\t");
            sw.Write(productMz + "\t");
            sw.Write(rt + "\t");
            sw.Write(tqRatio + "\t");
            sw.Write(rtBegin + "\t");
            sw.Write(rtEnd + "\t");
            sw.Write(parameter.MpMs1Tolerance + "\t");
            sw.Write(parameter.MpMs2Tolerance + "\t");
            sw.Write(msLevel + "\t");
            sw.WriteLine(compoundClass);
        }

        public void Dispose() {
            ((IDisposable)_writer)?.Dispose();
            _writer = null;
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
