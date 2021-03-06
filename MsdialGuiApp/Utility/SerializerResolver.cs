using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Utility
{
    static class SerializerResolver
    {
        static readonly Dictionary<MachineCategory, MsdialSerializer> serializers;

        static SerializerResolver() {
            serializers = new Dictionary<MachineCategory, MsdialSerializer>
            {
                { MachineCategory.GCMS, new MsdialGcMsApi.Parser.MsdialGcmsSerializer() },
                { MachineCategory.LCMS, new MsdialLcMsApi.Parser.MsdialLcmsSerializer() },
                { MachineCategory.IFMS, new MsdialDimsCore.Parser.MsdialDimsSerializer() },
                { MachineCategory.LCIMMS, new MsdialLcImMsApi.Parser.MsdialLcImMsSerializer() },
                { MachineCategory.IMMS, new MsdialImmsCore.Parser.MsdialImmsSerializer() },
            };
        }

        public static MsdialSerializer ResolveMsdialSerializer(string path) {
            if (serializers.TryGetValue(ResolveProjectType(path), out var serializer))
                return serializer;
            throw new System.Runtime.Serialization.SerializationException("Unknown method.");
        }

        // UNDONE: temporary method. (should not deserialize twice)
        private static MachineCategory ResolveProjectType(string path) {
            try {
                var storage = CompMs.Common.MessagePack.MessagePackHandler.LoadFromFile<MsdialDataStorage>(path);
                return storage.ParameterBase.MachineCategory;
            }
            catch (System.InvalidOperationException) {
            }
            throw new System.Runtime.Serialization.SerializationException("Invalid file format.");
        }
    }
}
