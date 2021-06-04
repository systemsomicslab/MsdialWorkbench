using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Parser
{
    public class ChromatogramAccessDecorator<T> : ChromatogramSerializer<T>
    {
        /*
         * File format
         * 
         * Number of spot: 4 byte
         * Seek pointers to spot: 8 byte x number of spot
         * 
         * Chunks
         */

        public ChromatogramSerializer<T> Serializer { get; }

        internal ChromatogramAccessDecorator(ChromatogramSerializer<T> serializer) {
            Serializer = serializer;
        }

        public override void SerializeAll(Stream stream, IEnumerable<T> chromatograms) {
            var arr = chromatograms.ToArray();
            WriteContent(stream, arr, arr.Length);
        }

        public override void SerializeN(Stream stream, IEnumerable<T> chromatograms, int num) {
            WriteContent(stream, chromatograms, num);
        }

        public override T DeserializeAt(Stream stream, int index) {
            var pointers = ReadHeader(stream);
            if (index < 0 || index >= pointers.Length) return default;

            stream.Seek(pointers[index], SeekOrigin.Begin);
            return Serializer.Deserialize(stream);
        }

        public override IEnumerable<T> DeserializeAll(Stream stream) {
            var pointers = ReadHeader(stream);
            return Serializer.DeserializeN(stream, pointers.Length);
        }

        public override IEnumerable<T> DeserializeEach(Stream stream, IEnumerable<int> indices) {
            var pointers = ReadHeader(stream);
            foreach (var index in indices) {
                if (index < 0 || index >= pointers.Length) {
                    // yield return default;
                    throw new ArgumentException(nameof(indices));
                }
                else {
                    stream.Seek(pointers[index], SeekOrigin.Begin);
                    yield return Serializer.Deserialize(stream);
                }
            }
        }

        protected long[] ReadHeader(Stream stream) {
            byte[] buffer = new byte[4];
            stream.Read(buffer, 0, 4);
            var n_spot = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[n_spot * 8];
            stream.Read(buffer, 0, 8 * n_spot);

            var result = new long[n_spot];
            for (int i = 0; i < n_spot; i++) {
                result[i] = BitConverter.ToInt64(buffer, i * 8);
            }

            return result;
        }

        protected void WriteContent(Stream stream, IEnumerable<T> chromatograms, int n_spot) {
            stream.Write(BitConverter.GetBytes((int)n_spot), 0, 4);

            var seeks = stream.Position;
            stream.Seek(n_spot * 8, SeekOrigin.Current);

            var pointers = new List<long>(n_spot);
            foreach (var chromatogram in chromatograms) {
                pointers.Add((long)stream.Position);
                Serializer.Serialize(stream, chromatogram);
            }

            stream.Seek(seeks, SeekOrigin.Begin);
            foreach (var pointer in pointers) {
                stream.Write(BitConverter.GetBytes(pointer), 0, 8);
            }
        }
    }
}
