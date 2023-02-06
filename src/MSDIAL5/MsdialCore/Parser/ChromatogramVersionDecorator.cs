using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Parser
{
    public class ChromatogramVersionDecorator<T> : ChromatogramSerializer<T> {

        /*
         * File format
         * 
         * Version infomation: 10 byte
         * 
         * Content chunk
         */

        public ChromatogramSerializer<T> Serializer { get; }
        public string Version { get; } = string.Empty;

        internal ChromatogramVersionDecorator(ChromatogramSerializer<T> serializer, string version) {
            Serializer = serializer;
            Version = version;
        }

        public override void SerializeAll(Stream stream, IEnumerable<T> chromatograms) {
            WriteVersion(stream);
            Serializer.SerializeAll(stream, chromatograms);
        }

        public override void SerializeN(Stream stream, IEnumerable<T> chromatograms, int num) {
            WriteVersion(stream);
            Serializer.SerializeN(stream, chromatograms, num);
        }

        public override IEnumerable<T> DeserializeAll(Stream stream) {
            _ = ReadVersion(stream);
            return Serializer.DeserializeAll(stream);
        }

        public override T DeserializeAt(Stream stream, int index) {
            _ = ReadVersion(stream);
            return Serializer.DeserializeAt(stream, index);
        }

        public override IEnumerable<T> DeserializeEach(Stream stream, IEnumerable<int> indices) {
            _ = ReadVersion(stream);
            return Serializer.DeserializeEach(stream, indices);
        }

        public string GetVersion(Stream stream) {
            var pos = stream.Position;
            var result = ReadVersion(stream);
            stream.Seek(pos, SeekOrigin.Begin);
            return result.TrimEnd('\0');
        }

        protected string ReadVersion(Stream stream) {
            var buf = new byte[10];
            stream.Read(buf, 0, 10);
            return System.Text.Encoding.ASCII.GetString(buf);
        }

        protected void WriteVersion(Stream stream) {
            var pos = stream.Position;
            var buf = System.Text.Encoding.ASCII.GetBytes(Version);
            stream.Write(buf, 0, buf.Length);
            stream.Seek(pos + 10, SeekOrigin.Begin);
        }
    }
}
