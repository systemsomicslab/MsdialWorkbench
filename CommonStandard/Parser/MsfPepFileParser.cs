using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.Common.Parser {
    public sealed class MsfPepFileParser {
        private MsfPepFileParser() { }

        private static int MSRefStorageFileVersionNumber = 1;
        public static List<PeptideMsReference> GeneratePeptideMsObjcts(string msfile, string pepfile, List<Peptide> peptides, Dictionary<string, int> Code2ID, double minMz, double maxMz, out Stream fs) {
            var pepMsQueries = new List<PeptideMsReference>();
            var adduct = AdductIonParser.GetAdductIonBean("[M+H]+");

            fs = File.Open(msfile, FileMode.Create, FileAccess.ReadWrite); // stream of ms file is retained by the main project
            fs.Write(BitConverter.GetBytes(MSRefStorageFileVersionNumber), 0, 4);

            using (var ps = File.Open(pepfile, FileMode.Create, FileAccess.ReadWrite)) {
                foreach (var pep in peptides) {
                    var sp = fs.Position;
                    var psp = ps.Position;
                    var spec = SequenceToSpec.Convert2SpecPeaks(pep, adduct, CollisionType.HCD, minMz, maxMz);
                    var msObj = new PeptideMsReference(pep, fs, sp, adduct);
                    pepMsQueries.Add(msObj);

                    WriteMsfData(fs, spec);

                    var aaIDs = GetIDs(pep, Code2ID);
                    WritePepData(ps, aaIDs);
                }
            }
            
            return pepMsQueries;
        }

        public static void LoadPeptideInformation(string pepfile, List<PeptideMsReference> pepMsRefObjs, Dictionary<int, string> ID2Code,  Dictionary<string, AminoAcid> Code2AminoAcidObj) {
            using (var fs = File.Open(pepfile, FileMode.Open, FileAccess.ReadWrite)) {
                foreach (var pepms in pepMsRefObjs) {
                    var buffer = new byte[4];
                    fs.Read(buffer, 0, 4);
                    var pepCount = BitConverter.ToInt32(buffer, 0);

                    buffer = new byte[pepCount * 4];
                    fs.Read(buffer, 0, buffer.Length);

                    var aaObjs = new List<AminoAcid>();
                    for (int i = 0; i < pepCount; i++) {
                        var id = BitConverter.ToInt32(buffer, 4 * i);
                        aaObjs.Add(Code2AminoAcidObj[ID2Code[id]]);
                    }
                    pepms.Peptide.SequenceObj = aaObjs;
                }
            }
        }

        private static int[] GetIDs(Peptide pep, Dictionary<string, int> code2ID) {
            var aaIDs = new int[pep.SequenceObj.Count];
            for (int i = 0; i < aaIDs.Length; i++) {
                aaIDs[i] = code2ID[pep.SequenceObj[i].Code()];
            }
            return aaIDs;
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

        private static void WritePepData(FileStream ps, int[] aaIDs) {
            ps.Write(BitConverter.GetBytes((int)aaIDs.Length), 0, 4);
            foreach (var id in aaIDs) {
                ps.Write(BitConverter.GetBytes(id), 0, 4);
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
