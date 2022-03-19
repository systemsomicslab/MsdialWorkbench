using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public class ZipStreamManager : IStreamManager, IDisposable
    {
        public ZipStreamManager(Stream stream, ZipArchiveMode mode, CompressionLevel compressionLevel = CompressionLevel.NoCompression, bool leaveOpen = true) {
            zipArchive = new ZipArchive(stream, mode, leaveOpen);
            semaphore = new SemaphoreSlim(1);
            this.compressionLevel = compressionLevel;
        }

        private readonly ZipArchive zipArchive;
        private readonly SemaphoreSlim semaphore;
        private readonly CompressionLevel compressionLevel;
        private Stream cacheStream;
        private bool disposedValue;

        public async Task<Stream> Create(string key) {
            await semaphore.WaitAsync();
            var entry = zipArchive.CreateEntry(key, compressionLevel);
            return cacheStream = new StreamWrapper(entry.Open(), semaphore);
        }

        public async Task<Stream> Get(string key) {
            await semaphore.WaitAsync();
            var entry = zipArchive.GetEntry(key);
            return cacheStream = new StreamWrapper(entry.Open(), semaphore);
        }

        public void Release(Stream stream) {
            if (stream == cacheStream) {
                stream.Dispose();
            }
        }

        public static ZipStreamManager OpenCreate(Stream stream, CompressionLevel compressionLevel = CompressionLevel.NoCompression, bool leaveOpen = true) {
            return new ZipStreamManager(stream, ZipArchiveMode.Update, compressionLevel, leaveOpen);
        }

        public static ZipStreamManager OpenGet(Stream stream, bool leaveOpen = true) {
            return new ZipStreamManager(stream, ZipArchiveMode.Read, leaveOpen: leaveOpen);
        }

        class StreamWrapper : Stream
        {
            private Stream stream;
            private SemaphoreSlim semaphore;

            protected override void Dispose(bool disposing) {
                if (stream != null) {
                    stream.Dispose();
                    stream = null;
                    semaphore?.Release();
                    semaphore = null;
                }
            }

            public StreamWrapper(Stream stream, SemaphoreSlim semaphore) {
                this.stream = stream ?? throw new System.ArgumentNullException(nameof(stream));
                this.semaphore = semaphore;
            }

            public override bool CanRead => stream?.CanRead ?? false;

            public override bool CanSeek => stream?.CanSeek ?? false;

            public override bool CanWrite => stream?.CanWrite ?? false;

            public override long Length => stream.Length;

            public override long Position { get => stream.Position; set => stream.Position = value; }

            public override void Flush() => stream.Flush();

            public override int Read(byte[] buffer, int offset, int count) {
                return stream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin) {
                return stream.Seek(offset, origin);
            }

            public override void SetLength(long value) {
                stream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count) {
                stream.Write(buffer, offset, count);
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    cacheStream?.Dispose();
                    zipArchive.Dispose();
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
