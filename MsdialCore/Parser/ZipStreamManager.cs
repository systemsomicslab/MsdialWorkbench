using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public sealed class ZipStreamManager : IStreamManager, IDisposable
    {
        private ZipArchive zipArchive;
        private SemaphoreSlim semaphore;
        private readonly CompressionLevel compressionLevel;
        private readonly string _rootPath;
        private Stream cacheStream;
        private readonly bool _hasArchive;

        public ZipStreamManager(Stream stream, ZipArchiveMode mode, CompressionLevel compressionLevel = CompressionLevel.NoCompression, bool leaveOpen = true, string rootPath = "") {
            zipArchive = new ZipArchive(stream, mode, leaveOpen);
            semaphore = new SemaphoreSlim(1);
            this.compressionLevel = compressionLevel;
            _rootPath = rootPath;
            _hasArchive = true;
        }

        private ZipStreamManager(ZipArchive arcvhive, SemaphoreSlim semaphore, CompressionLevel compressionLevel, string rootPath) {
            zipArchive = arcvhive;
            this.semaphore = semaphore;
            this.compressionLevel = compressionLevel;
            _rootPath = rootPath;
            _hasArchive = false;
        }

        public async Task<Stream> Create(string key) {
            await semaphore.WaitAsync();
            var entry = zipArchive.CreateEntry(Path.Combine(_rootPath, key), compressionLevel);
            return cacheStream = new StreamWrapper(entry.Open(), semaphore);
        }

        public async Task<Stream> Get(string key) {
            await semaphore.WaitAsync();
            var entry = zipArchive.GetEntry(Path.Combine(_rootPath, key));
            return cacheStream = new StreamWrapper(entry.Open(), semaphore);
        }

        public void Release(Stream stream) {
            if (stream == cacheStream) {
                stream.Dispose();
            }
        }

        IStreamManager IStreamManager.Join(string relativePath) {
            return new ZipStreamManager(zipArchive, semaphore, compressionLevel, Path.Combine(_rootPath, relativePath));
        }

        void IStreamManager.Complete() {
            Dispose();
        }

        public static ZipStreamManager OpenCreate(Stream stream, CompressionLevel compressionLevel = CompressionLevel.NoCompression, bool leaveOpen = true) {
            return new ZipStreamManager(stream, ZipArchiveMode.Update, compressionLevel, leaveOpen);
        }

        public static ZipStreamManager OpenGet(Stream stream, bool leaveOpen = true) {
            return new ZipStreamManager(stream, ZipArchiveMode.Read, leaveOpen: leaveOpen);
        }

        private bool disposedValue;
        private void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    cacheStream?.Dispose();
                    cacheStream = Stream.Null;
                    if (_hasArchive) {
                        zipArchive.Dispose();
                        zipArchive = null;
                        semaphore.Dispose();
                        semaphore = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
    }
}
