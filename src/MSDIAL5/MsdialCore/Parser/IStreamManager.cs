using System;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public interface IStreamManager : IDisposable
    {
        Task<Stream> Create(string key);

        Task<Stream> Get(string key);

        void Release(Stream stream);

        IStreamManager Join(string relativePath);

        void Complete();
    }
}
