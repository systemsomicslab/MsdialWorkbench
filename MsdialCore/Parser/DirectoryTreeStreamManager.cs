using System;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public class DirectoryTreeStreamManager : IStreamManager
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
                throw new ArgumentException(nameof(key));
            }
            var stream = File.Open(file, FileMode.Open);
            return Task.FromResult<Stream>(stream);
        }

        public void Release(Stream stream) {
            stream?.Close();
        }
    }
}
