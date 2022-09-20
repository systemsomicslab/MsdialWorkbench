using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialIntegrate.Parser
{
    public class MsdialIntegrateSerializer : IMsdialSerializer
    {
        private static Dictionary<MachineCategory?, IMsdialSerializer> Serializers = new Dictionary<MachineCategory?, IMsdialSerializer>
        {
            { MachineCategory.LCIMMS, MsdialLcImMsApi.DataObj.MsdialLcImMsDataStorage.Serializer },
            { MachineCategory.LCMS, MsdialLcMsApi.DataObj.MsdialLcmsDataStorage.Serializer },
            { MachineCategory.IMMS, MsdialImmsCore.DataObj.MsdialImmsDataStorage.Serializer },
            { MachineCategory.IFMS, MsdialDimsCore.DataObj.MsdialDimsDataStorage.Serializer },
            { MachineCategory.GCMS, MsdialGcMsApi.DataObj.MsdialGcmsDataStorage.Serializer },
            { MachineCategory.IIMMS, MsdialImmsCore.DataObj.MsdialImmsDataStorage.Serializer },
            { MachineCategory.IDIMS, MsdialDimsCore.DataObj.MsdialDimsDataStorage.Serializer },
        };

        public async Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
            var serializer = await ResolveSerializer(streamManager, Combine(prefix, projectTitle));
            return await serializer.LoadAsync(streamManager, projectTitle, projectFolderPath, prefix).ConfigureAwait(false);
        }

        private async Task<IMsdialSerializer> ResolveSerializer(IStreamManager streamManager, string path) {
            using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                var storage = MessagePackDefaultHandler.LoadFromStream<MsdialDataStorage>(stream);
                if (Serializers.TryGetValue(storage?.ParameterBase?.MachineCategory, out var serializer)) {
                    return serializer;
                }
                throw new System.Runtime.Serialization.SerializationException($"Unknown method. {storage?.ParameterBase?.MachineCategory}");
            }
        }

        public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
            return dataStorage.SaveAsync(streamManager, projectTitle, prefix);
        }

        private static string Combine(string path1, string path2) {
            if (string.IsNullOrEmpty(path1)) {
                return path2;
            }
            if (string.IsNullOrEmpty(path2)) {
                return path1;
            }
            return Path.Combine(path1, path2);
        }
    }
}
