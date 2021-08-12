using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.Common.Parser {
    public sealed class MsfFileParser {
        private MsfFileParser() { }

        private static int MSRefStorageFileVersionNumber = 1;
        public static List<PeptideMsReference> GeneratePeptideMsObjcts(string file, List<Peptide> peptides, double minMz, double maxMz, out Stream fs) {
            var pepMsQueries = new List<PeptideMsReference>();
            var adduct = AdductIonParser.GetAdductIonBean("[M+H]+");

            fs = File.Open(file, FileMode.Create, FileAccess.ReadWrite);
            fs.Write(BitConverter.GetBytes(MSRefStorageFileVersionNumber), 0, 4);

            foreach (var pep in peptides) {
                var sp = fs.Position;
                var spec = SequenceToSpec.Convert2SpecPeaks(pep, adduct, CollisionType.HCD, minMz, maxMz);
                var msObj = new PeptideMsReference(pep, fs, sp, adduct);
                pepMsQueries.Add(msObj);

                WriteMsfData(fs, spec);
            }
            return pepMsQueries;
        }

        private static void WriteMsfData(Stream fs, List<SpectrumPeak> spec) {
            fs.Write(BitConverter.GetBytes((int)spec.Count), 0, 4);
            foreach (var peak in spec) {
                fs.Write(BitConverter.GetBytes((float)peak.Mass), 0, 4);
                fs.Write(BitConverter.GetBytes((float)peak.Intensity), 0, 4);
                fs.Write(BitConverter.GetBytes((int)peak.SpectrumComment), 0, 4);
                fs.Write(BitConverter.GetBytes((int)peak.PeakID), 0, 4);
            }
        }

        public static List<SpectrumPeak> ReadSpectrumPeaks(Stream fs, long seekpoint) {
            fs.Seek(0, SeekOrigin.Begin);

            var buffer = new byte[4];
            fs.Read(buffer, 0, 4);

            var vnum = BitConverter.ToInt32(buffer, 0);

            switch (vnum) {
                case 1: return ReadSpectrumPeaksVS1(fs, seekpoint);
                default: return ReadSpectrumPeaksVS1(fs, seekpoint);
            }
        }

        public static List<SpectrumPeak> ReadSpectrumPeaksVS1(Stream fs, long seekpoint) {
            fs.Seek(seekpoint, SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[4];
            fs.Read(buffer, 0, 4);
            var specCount = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[specCount * 4 * 4];
            fs.Read(buffer, 0, buffer.Length);

            var peaks = new List<SpectrumPeak>();
            for (int i = 0; i < specCount; i++) {

                var mz = BitConverter.ToSingle(buffer, 16 * i);
                var intensity = BitConverter.ToSingle(buffer, 16 * i + 4);
                var comment = BitConverter.ToInt32(buffer, 16 * i + 8);
                var id = BitConverter.ToInt32(buffer, 16 * i + 12);
                var peak = new SpectrumPeak(mz, intensity) { SpectrumComment = (SpectrumComment)System.Enum.ToObject(typeof(SpectrumComment), comment), PeakID = id };
                peaks.Add(peak);
            }

            return peaks;
        }
    }
}
