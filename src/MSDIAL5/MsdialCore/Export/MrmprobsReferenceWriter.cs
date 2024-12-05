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

        public MrmprobsReferenceWriter(Stream stream, bool leaveOpen)
        {
            _writer = new StreamWriter(stream, Encoding.ASCII, 4096, leaveOpen);
        }

        public void WriteHeader() {
            const string header = "Compound name\tPrecursor mz\tProduct mz\tRT min\tTQ Ratio\tRT begin\tRT end\tMS1 tolerance\tMS2 tolerance\tMS level\tClass";
            _writer.WriteLine(header);
        }

        public void WriteFieldsBasedOnReference(IChromatogramPeak peak, MoleculeMsReference mspQuery, MrmprobsExportBaseParameter parameter) {
            var name = StringReplaceForWindowsAcceptableCharacters(mspQuery.Name + "_" + peak.ID);
            var precursorMz = Math.Round(mspQuery.PrecursorMz, 5);
            var rtBegin = Math.Max(Math.Round(peak.ChromXs.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
            var rtEnd = Math.Round(peak.ChromXs.RT.Value + (float)parameter.MpRtTolerance, 2);
            var rt = Math.Round(peak.ChromXs.RT.Value, 2);

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

        public void WriteFieldsBasedOnExperiment<T>(T peak, MSDecResult ms2DecResult, MrmprobsExportBaseParameter parameter) where T: IChromatogramPeak, IMoleculeProperty {
            var name = StringReplaceForWindowsAcceptableCharacters(peak.Name + "_" + peak.ID);
            var precursorMz = Math.Round(peak.Mass, 5);
            var rtBegin = Math.Max(Math.Round(peak.ChromXs.RT.Value - (float)parameter.MpRtTolerance, 2), 0);
            var rtEnd = Math.Round(peak.ChromXs.RT.Value + (float)parameter.MpRtTolerance, 2);
            var rt = Math.Round(peak.ChromXs.RT.Value, 2);

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

        private static string StringReplaceForWindowsAcceptableCharacters(string name) {
            var chars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => chars.Contains(c) ? '_' : c).ToArray());
        }

        public void Dispose() {
            ((IDisposable)_writer)?.Dispose();
            _writer = null;
        }
    }
}
