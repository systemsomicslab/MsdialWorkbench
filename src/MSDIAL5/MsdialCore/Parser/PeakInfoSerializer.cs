using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialCore.Parser
{
    public class PeakInfoSerializer : ChromatogramSerializer<ChromatogramPeakInfo> {
        /*
         * File format
         * 
         * Peak info chunk
         *   File id: 4 byte
         *   Number of Chromatogram peak: 4 byte
         *   PeakTop horizontal axis value: 4 byte
         *   PeakLeft horizontal axis value: 4 byte
         *   PeakRight horizontal axis value: 4 byte
         *   
         *   Chromatogram chunk
         *     Horizontal value: 4 byte
         *     Intensity: 4 byte
         */

        internal PeakInfoSerializer(ChromXType mainType) {
            ChromXType = mainType;
        }

        public ChromXType ChromXType { get; } = ChromXType.RT;

        public override void Serialize(Stream stream, ChromatogramPeakInfo chromatogram) {
            WritePeakInfo(stream, chromatogram);
        }

        public override ChromatogramPeakInfo Deserialize(Stream stream) {
            return ReadPeakInfo(stream);
        }

        private ChromatogramPeakInfo ReadPeakInfo(Stream stream) {
            byte[] buf = new byte[20];
            stream.Read(buf, 0, 20);
            return new ChromatogramPeakInfo(
                BitConverter.ToInt32(buf, 0),
                ReadPeaks(stream, BitConverter.ToInt32(buf, 4)),
                BitConverter.ToSingle(buf, 8),
                BitConverter.ToSingle(buf, 12),
                BitConverter.ToSingle(buf, 16)
                );
        }

        private List<ChromatogramPeak> ReadPeaks(Stream stream, int length) {
            byte[] buf = new byte[8 * length];
            stream.Read(buf, 0, 8 * length);

            var peaks = new List<ChromatogramPeak>(length);
            for (int i = 0; i < length; i++) {
                var chromXs = new ChromXs(BitConverter.ToSingle(buf, i * 8), ChromXType);
                var intensity = BitConverter.ToSingle(buf, i * 8 + 4);
                peaks.Add(new ChromatogramPeak(i, -1, intensity, chromXs));
            }

            return peaks;
        }

        private void WritePeakInfo(Stream stream, ChromatogramPeakInfo info) {
            stream.Write(BitConverter.GetBytes((int)info.FileID), 0, 4);
            stream.Write(BitConverter.GetBytes((int)info.Chromatogram.Count), 0, 4);

            stream.Write(BitConverter.GetBytes((float)info.ChromXsTop.Value), 0, 4);
            stream.Write(BitConverter.GetBytes((float)info.ChromXsLeft.Value), 0, 4);
            stream.Write(BitConverter.GetBytes((float)info.ChromXsRight.Value), 0, 4);

            foreach (var peak in info.Chromatogram) {
                WritePeak(stream, peak);
            }
        }

        private void WritePeak(Stream stream, IChromatogramPeak peak) {
            stream.Write(BitConverter.GetBytes((float)peak.ChromXs.Value), 0, 4);
            stream.Write(BitConverter.GetBytes((float)peak.Intensity), 0, 4);
        }
    }
}
