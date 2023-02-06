using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.Common.MessagePack {
    public static class MoleculeMsRefMethods {
        const int version = 2;

        public static void SaveMspToFile(List<MoleculeMsReference> bean, string path) {
            MessagePackDefaultHandler.SaveLargeListToFile<MoleculeMsReference>(bean, path);
        }

        public static void SaveMspToStream(List<MoleculeMsReference> bean, Stream stream) {
            MessagePackDefaultHandler.SaveLargeListToStream(bean, stream);
        }

        public static List<MoleculeMsReference> LoadMspFromFile(string path) {
            return MessagePackDefaultHandler.LoadLargerListFromFile<MoleculeMsReference>(path);
        }

        public static List<MoleculeMsReference> LoadMspFromStream(Stream stream) {
            return MessagePackDefaultHandler.LoadLargerListFromStream<MoleculeMsReference>(stream);
        }
    }
}
