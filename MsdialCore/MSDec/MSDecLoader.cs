using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.MSDec {
    public class MSDecLoader : IDisposable {
        public MSDecLoader(Stream fs) {
            if (fs is null || !fs.CanSeek) {
                throw new ArgumentException(nameof(fs));
            }

            deconvolutionStream = fs;
            MsdecResultsReader.GetSeekPointers(deconvolutionStream, out version, out seekPointers, out isAnnotationInfoIncluded);
        }

        public MSDecLoader(string deconvolutionFile) : this(FileOpen(deconvolutionFile)) {

        }

        private static FileStream FileOpen(string deconvolutionFile) {
            if (!File.Exists(deconvolutionFile)) {
                throw new ArgumentException(nameof(deconvolutionFile));
            }

            return File.Open(deconvolutionFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private readonly Stream deconvolutionStream;
        private readonly int version;
        private readonly List<long> seekPointers;
        private readonly bool isAnnotationInfoIncluded;

        public MSDecResult LoadMSDecResult(int idx) {
            if (disposedValue) {
                return null;
            }
            return LoadMSDecResultCore(idx);
        }

        private MSDecResult LoadMSDecResultCore(int idx) {
            return MsdecResultsReader.ReadMSDecResult(deconvolutionStream, seekPointers[idx], version, isAnnotationInfoIncluded);
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    deconvolutionStream.Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
