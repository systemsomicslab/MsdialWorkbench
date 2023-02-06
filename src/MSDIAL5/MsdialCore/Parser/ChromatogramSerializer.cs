using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Parser
{
    // at least, you need to override Serialize or SerializeAll, and Deserialize or DeserializeAll.
    // of course, you can override all methods you need.
    public abstract class ChromatogramSerializer<T>
    {
        public virtual void Serialize(Stream stream, T chromatogram) {
            SerializeAll(stream, new T[] { chromatogram });
        }

        public virtual void SerializeAll(Stream stream, IEnumerable<T> chromatograms) {
            foreach (var chromatogram in chromatograms)
                Serialize(stream, chromatogram);
        }

        public virtual void SerializeN(Stream stream, IEnumerable<T> chromatograms, int num) {
            SerializeAll(stream, chromatograms.Take(num));
        }

        public virtual T Deserialize(Stream stream) {
            return DeserializeAt(stream, 0);
        }

        public virtual T DeserializeAt(Stream stream, int index) {
            return DeserializeAll(stream).Skip(index).FirstOrDefault();
        }

        public virtual IEnumerable<T> DeserializeAll(Stream stream) {
            while (stream.Position < stream.Length)
                yield return Deserialize(stream);
        }

        public virtual IEnumerable<T> DeserializeN(Stream stream, int num) {
            return DeserializeAll(stream).Take(num);
        }

        public virtual IEnumerable<T> DeserializeEach(Stream stream, IEnumerable<int> indices) {
            if (!stream.CanSeek)
                throw new ArgumentException("Please use stream which supports seeking for DeserializeEach.");
            var point = stream.Position;
            foreach (var index in indices) {
                yield return DeserializeAt(stream, index);
                stream.Seek(point, SeekOrigin.Begin);
            }
        }

        public void SerializeToFile(string path, T chromatogram) {
            using (var fs = File.OpenWrite(path)) {
                Serialize(fs, chromatogram);
            }
        }

        public void SerializeAllToFile(string path, IEnumerable<T> chromatograms) {
            using (var fs = File.OpenWrite(path)) {
                SerializeAll(fs, chromatograms);
            }
        }

        public void SerializeNToFile(string path, IEnumerable<T> chromatograms, int num) {
            using (var fs = File.OpenWrite(path)) {
                SerializeN(fs, chromatograms, num);
            }
        }

        public T DeserializeFromFile(string path) {
            using (var fs = File.OpenRead(path)) {
                return Deserialize(fs);
            }
        }

        public T DeserializeAtFromFile(string path, int index) {
            using (var fs = File.OpenRead(path)) {
                return DeserializeAt(fs, index);
            }
        }

        public IEnumerable<T> DeserializeAllFromFile(string path) {
            using (var fs = File.OpenRead(path)) {
                foreach (var v in DeserializeAll(fs))
                    yield return v;
            }
        }

        public IEnumerable<T> DeserializeNFromFile(string path, int num) {
            using (var fs = File.OpenRead(path)) {
                foreach (var v in DeserializeN(fs, num))
                    yield return v;
            }
        }

        public IEnumerable<T> DeserializeEachFromFile(string path, IEnumerable<int> indices) {
            using (var fs = File.OpenRead(path)) {
                foreach (var v in DeserializeEach(fs, indices))
                    yield return v;
            }
        }
    }

    public abstract class ChromatogramSerializerDecorator<T> : ChromatogramSerializer<T>
    {
        protected ChromatogramSerializer<T> Serializer;

        public override void Serialize(Stream stream, T chromatogram) {
            Serializer.Serialize(stream, chromatogram);
        }

        public override void SerializeAll(Stream stream, IEnumerable<T> chromatograms) {
            Serializer.SerializeAll(stream, chromatograms);
        }

        public override void SerializeN(Stream stream, IEnumerable<T> chromatograms, int num) {
            Serializer.SerializeN(stream, chromatograms, num);
        }

        public override T Deserialize(Stream stream) {
            return Serializer.Deserialize(stream);
        }

        public override T DeserializeAt(Stream stream, int index) {
            return Serializer.DeserializeAt(stream, index);
        }

        public override IEnumerable<T> DeserializeAll(Stream stream) {
            return Serializer.DeserializeAll(stream);
        }

        public override IEnumerable<T> DeserializeN(Stream stream, int num) {
            return Serializer.DeserializeN(stream, num);
        }

        public override IEnumerable<T> DeserializeEach(Stream stream, IEnumerable<int> indices) {
            return Serializer.DeserializeEach(stream, indices);
        }
    }
}
