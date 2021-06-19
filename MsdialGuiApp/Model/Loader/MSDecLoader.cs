using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.App.Msdial.Model.Loader
{
    class MSDecLoader : IDisposable
    {
        public MSDecLoader(string deconvolutionFile) {

            if (!File.Exists(deconvolutionFile)) {
                throw new ArgumentException(nameof(deconvolutionFile));
            }

            deconvolutionStream = File.Open(deconvolutionFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            MsdecResultsReader.GetSeekPointers(deconvolutionStream, out version, out seekPointers, out isAnnotationInfoIncluded);
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
