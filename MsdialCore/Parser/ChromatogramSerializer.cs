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
                return DeserializeAll(fs);
            }
        }

        public IEnumerable<T> DeserializeNFromFile(string path, int num) {
            using (var fs = File.OpenRead(path)) {
                return DeserializeN(fs, num);
            }
        }
    }
}
