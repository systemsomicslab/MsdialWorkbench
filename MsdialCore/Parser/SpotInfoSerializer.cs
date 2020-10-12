using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialCore.Parser
{
    public class SpotInfoSerializer : ChromatogramSerializer<ChromatogramSpotInfo>
    {
        /*
         * File format
         * 
         * Spot Chunk
         *   Retension time: 4 byte
         *   Retension index: 4 byte
         *   Mass: 4 byte
         *   Drift time: 4 byte
         *   MainType: 1 byte
         *   Number of sample: 4 byte
         *   
         *   Peak info chunk: depends on PeakInfoSerializer
         */

        public ChromatogramSerializer<ChromatogramPeakInfo> PeakInfoSerializer { get; }

        internal SpotInfoSerializer(ChromatogramSerializer<ChromatogramPeakInfo> peakInfoSerializer) {
            PeakInfoSerializer = peakInfoSerializer;
        }

        public override void Serialize(Stream stream, ChromatogramSpotInfo chromatogram) =>
            WriteSpotInfo(stream, chromatogram);

        public override ChromatogramSpotInfo Deserialize(Stream stream) =>
            ReadSpotInfo(stream);

        private ChromatogramSpotInfo ReadSpotInfo(Stream stream) {
            var buf = new byte[21];
            stream.Read(buf, 0, 21);

            var chromXs = new ChromXs {
                RT = new RetentionTime(BitConverter.ToSingle(buf, 0)),
                RI = new RetentionIndex(BitConverter.ToSingle(buf, 4)),
                Mz = new MzValue(BitConverter.ToSingle(buf, 8)),
                Drift = new DriftTime(BitConverter.ToSingle(buf, 12)),
                MainType = (ChromXType)buf[16],
            };

            var peaks = PeakInfoSerializer.DeserializeN(stream, BitConverter.ToInt32(buf, 17));

            return new ChromatogramSpotInfo(peaks, chromXs);
        }

        private void WriteSpotInfo(Stream stream, ChromatogramSpotInfo spot) {
            stream.Write(BitConverter.GetBytes((float)spot.ChromXs.RT.Value), 0, 4);
            stream.Write(BitConverter.GetBytes((float)spot.ChromXs.RI.Value), 0, 4);
            stream.Write(BitConverter.GetBytes((float)spot.ChromXs.Mz.Value), 0, 4);
            stream.Write(BitConverter.GetBytes((float)spot.ChromXs.Drift.Value), 0, 4);
            stream.WriteByte((byte)spot.ChromXs.MainType);
            stream.Write(BitConverter.GetBytes((int)spot.PeakInfos.Count), 0, 4);

            PeakInfoSerializer.SerializeAll(stream, spot.PeakInfos);
        }
    }
}
