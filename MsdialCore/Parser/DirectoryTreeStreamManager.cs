using System;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public sealed class DirectoryTreeStreamManager : IStreamManager
    {
        public DirectoryTreeStreamManager(string rootDirectory = "") {
            RootDirectory = rootDirectory;
        }

        public string RootDirectory { get; }

        public Task<Stream> Create(string key) {
            var file = Path.Combine(RootDirectory, key);
            var directory = Path.GetDirectoryName(file);
            Directory.CreateDirectory(directory);
            var stream = File.Open(file, FileMode.Create);
            return Task.FromResult<Stream>(stream);
        }

        public Task<Stream> Get(string key) {
            var file = Path.Combine(RootDirectory, key);
            if (!File.Exists(file)) {
                throw new FileNotFoundException(file);
            }
            var stream = File.Open(file, FileMode.Open);
            return Task.FromResult<Stream>(stream);
        }

        public void Release(Stream stream) {
            stream?.Close();
        }

        IStreamManager IStreamManager.Join(string relativePath) {
            return new DirectoryTreeStreamManager(Path.Combine(RootDirectory, relativePath));
        }

        void IDisposable.Dispose() {

        }
    }
}
