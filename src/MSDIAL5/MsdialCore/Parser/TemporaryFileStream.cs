using System.IO;

namespace CompMs.MsdialCore.Parser
{
    public sealed class TemporaryFileStream : Stream
    {
        private readonly string _filePath;
        private readonly bool _moveBeforeDispose;
        private readonly string _tempFile;
        private Stream _tempStream;

        public TemporaryFileStream(string filePath, bool moveBeforeDispose = false) {
            _tempFile = Path.GetTempFileName();
            _tempStream = File.Open(_tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            _filePath = filePath;
            _moveBeforeDispose = moveBeforeDispose;
        }

        public override bool CanRead => _tempStream.CanRead;

        public override bool CanSeek => _tempStream.CanSeek;

        public override bool CanWrite => _tempStream.CanWrite;

        public override long Length => _tempStream.Length;

        public override long Position { get => _tempStream.Position; set => _tempStream.Position = value; }

        public override void Flush() {
            _tempStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return _tempStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return _tempStream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            _tempStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            _tempStream.Write(buffer, offset, count);
        }

        public void Move() {
            CloseTempStream();
           if (File.Exists(_tempFile)) {
                if (File.Exists(_filePath)) {
                    File.Delete(_filePath);
                }
                File.Move(_tempFile, _filePath);
            }
        }

        public void Discard() {
            CloseTempStream();
           if (File.Exists(_tempFile)) {
                File.Delete(_tempFile);
            }
        }

        private void CloseTempStream() {
            if (_tempStream != Null) {
                _tempStream.Dispose();
                _tempStream = Null;
            }
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (_moveBeforeDispose) {
                Move();
            }
            else {
                Discard();
            }
        }
    }
}
