using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser
{
    public sealed class MsdialIgnoreSavingSerializer : IMsdialSerializer
    {
        public Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
            return Task.FromException<IMsdialDataStorage<ParameterBase>>(new NotSupportedException($"{nameof(MsdialIgnoreSavingSerializer)} can't save data."));
        }

        public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
            return Task.CompletedTask;
        }
    }
}
