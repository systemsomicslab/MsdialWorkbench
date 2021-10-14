using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Utility
{
    static class SerializerResolver
    {
        private static readonly Dictionary<MachineCategory, IMsdialSerializer> serializers;

        static SerializerResolver() {
            serializers = new Dictionary<MachineCategory, IMsdialSerializer>
            {
                { MachineCategory.GCMS, MsdialGcMsApi.DataObj.MsdialGcmsDataStorage.Serializer },
                { MachineCategory.LCMS, MsdialLcMsApi.DataObj.MsdialLcmsDataStorage.Serializer },
                { MachineCategory.IFMS, MsdialDimsCore.DataObj.MsdialDimsDataStorage.Serializer },
                { MachineCategory.LCIMMS, MsdialLcImMsApi.DataObj.MsdialLcImMsDataStorage.Serializer },
                { MachineCategory.IMMS, MsdialImmsCore.DataObj.MsdialImmsDataStorage.Serializer },
            };
        }

        public static IMsdialSerializer ResolveMsdialSerializer(string path) {
            if (serializers.TryGetValue(ResolveProjectType(path), out var serializer))
                return serializer;
            throw new System.Runtime.Serialization.SerializationException("Unknown method.");
        }

        // UNDONE: temporary method. (should not deserialize twice)
        private static MachineCategory ResolveProjectType(string path) {
            try {
                var storage = CompMs.Common.MessagePack.MessagePackDefaultHandler.LoadFromFile<MsdialDataStorage>(path);
                return storage.ParameterBase.MachineCategory;
            }
            catch (System.InvalidOperationException) {
            }
            throw new System.Runtime.Serialization.SerializationException("Invalid file format.");
        }
    }
}
