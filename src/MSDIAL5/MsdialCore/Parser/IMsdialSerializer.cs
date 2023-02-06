using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public interface IMsdialSerializer
    {
        Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix);
        Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix);
    }
}
