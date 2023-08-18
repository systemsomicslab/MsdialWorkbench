using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.Parser {
    public sealed class MsfPepFileParser {
        private MsfPepFileParser() { }

        private static int MSRefStorageFileVersionNumber = 1;
        public static List<PeptideMsReference> GeneratePeptideMsObjcts(
            string msfile, string pepfile, List<Peptide> peptides, 
            Dictionary<string, int> Code2ID, double minMz, double maxMz, CollisionType type, out Stream fs) {
            var pepMsQueries = new List<PeptideMsReference>();
            var adduct = AdductIon.GetAdductIon("[M+H]+");

            fs = File.Open(msfile, FileMode.Create, FileAccess.ReadWrite); // stream of ms file is retained by the main project
            fs.Write(BitConverter.GetBytes(MSRefStorageFileVersionNumber), 0, 4);

            using (var ps = File.Open(pepfile, FileMode.Create, FileAccess.ReadWrite)) {
                var counter = 0;
                foreach (var pep in peptides.OrderBy(n => n.ExactMass)) {
                    var sp = fs.Position;
                    var psp = ps.Position;
                    //var spec = SequenceToSpec.Convert2SpecPeaks(pep, adduct, type, minMz, maxMz);
                    var msObj = new PeptideMsReference(pep, fs, sp, adduct, counter, (float)minMz, (float)maxMz, type);
                    pepMsQueries.Add(msObj);

                    //WriteMsfData(fs, spec);
                    WriteMsfData(fs, null);

                    var aaIDs = GetIDs(pep, Code2ID);
                    WritePepData(ps, aaIDs);

                    counter++;
                }
            }
            
            return pepMsQueries;
        }

        public static List<PeptideMsReference> GenerateFastPeptideMsObjcts(
            string msfile, string pepfile, 
            List<Peptide> peptides, Dictionary<string, int> Code2ID, 
            double minMz, double maxMz, CollisionType type, out Stream fs) {
            var pepMsQueries = new List<PeptideMsReference>();
            var adduct = AdductIon.GetAdductIon("[M+H]+");

            fs = File.Open(msfile, FileMode.Create, FileAccess.ReadWrite); // stream of ms file is retained by the main project
            fs.Write(BitConverter.GetBytes(MSRefStorageFileVersionNumber), 0, 4);

            using (var ps = File.Open(pepfile, FileMode.Create, FileAccess.ReadWrite)) {
                var counter = 0;
                foreach (var pep in peptides.OrderBy(n => n.ExactMass)) {
                    var sp = fs.Position;
                    var psp = ps.Position;
                    var msObj = new PeptideMsReference(pep, fs, sp, adduct, counter, (float)minMz, (float)maxMz, type);
                    pepMsQueries.Add(msObj);

                    WriteMsfData(fs, null);

                    var aaIDs = GetIDs(pep, Code2ID);
                    WritePepData(ps, aaIDs);

                    counter++;
                }
            }

            return pepMsQueries;
        }

        public static void LoadPeptideInformation(string pepfile, List<PeptideMsReference> pepMsRefObjs, Dictionary<int, string> ID2Code,  Dictionary<string, AminoAcid> Code2AminoAcidObj) {
            // Prepare array to map ID to AminoAcid.
            var id2aa = new AminoAcid[ID2Code.Keys.DefaultIfEmpty().Max() + 1];
            foreach (var pair in ID2Code) {
                id2aa[pair.Key] = Code2AminoAcidObj[pair.Value];
            }
            using (var fs = File.Open(pepfile, FileMode.Open, FileAccess.ReadWrite)) {
                // Allocate buffer once.
                var numPeptide = new byte[4];
                var aaBuffer = new byte[100 * 4];
                foreach (var pepms in pepMsRefObjs) {
                    fs.Read(numPeptide, 0, 4);
                    var pepCount = BitConverter.ToInt32(numPeptide, 0);

                    if (pepCount * 4 > aaBuffer.Length) {
                        aaBuffer = new byte[pepCount * 4];
                    }
                    fs.Read(aaBuffer, 0, pepCount * 4);
                    var aaObjs = new List<AminoAcid>(pepCount);
                    for (int i = 0; i < pepCount; i++) {
                        var id = BitConverter.ToInt32(aaBuffer, 4 * i);
                        aaObjs.Add(id2aa[id]);
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
            if (spec.IsEmptyOrNull()) {
                fs.Write(BitConverter.GetBytes(0), 0, 4);
                return;
            }
            fs.Write(BitConverter.GetBytes((int)spec.Count), 0, 4);
            if (spec == null) return;
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
