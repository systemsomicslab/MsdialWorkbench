using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public sealed class DirectoryTreeStreamManager : IStreamManager
    {
        private readonly Dictionary<string, string> _pathToTempFile;
        private bool _disposedValue;

        public DirectoryTreeStreamManager(string rootDirectory = "") {
            RootDirectory = rootDirectory;
            _pathToTempFile = new Dictionary<string, string>();
        }

        private DirectoryTreeStreamManager(string rootDirectory, Dictionary<string, string> pathToTempFile) {
            RootDirectory = rootDirectory;
            _pathToTempFile = pathToTempFile;
        }

        public string RootDirectory { get; }

        public Task<Stream> Create(string key) {
            var tmpFile = Path.GetTempFileName();
            var file = Path.Combine(RootDirectory, key);
            _pathToTempFile.Add(file, tmpFile);

            var stream = File.Open(tmpFile, FileMode.Create, FileAccess.ReadWrite);
            return Task.FromResult<Stream>(stream);
        }

        public Task<Stream> Get(string key) {
            var file = Path.Combine(RootDirectory, key);
            if (_pathToTempFile.TryGetValue(file, out var tmpFile)) {
                file = tmpFile;
            }
            if (!File.Exists(file)) {
                throw new FileNotFoundException(file);
            }
            var stream = File.Open(file, FileMode.Open, FileAccess.Read);
            return Task.FromResult<Stream>(stream);
        }

        public void Release(Stream stream) {
            stream?.Close();
        }

        IStreamManager IStreamManager.Join(string relativePath) {
            return new DirectoryTreeStreamManager(Path.Combine(RootDirectory, relativePath), _pathToTempFile);
        }

        void IStreamManager.Complete() {
            MoveFiles();
        }

        public void MoveFiles() {
            foreach (var kvp in _pathToTempFile) {
                string file = kvp.Key;
                var directory = Path.GetDirectoryName(file);
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }

                string tmpFile = kvp.Value;
                if (File.Exists(tmpFile)) {
                    if (File.Exists(file)) {
                        File.Delete(file);
                    }
                    File.Move(tmpFile, file);
                }
            }
            _pathToTempFile.Clear();
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {

                }
                _disposedValue = true;
            }
        }

        ~DirectoryTreeStreamManager()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
