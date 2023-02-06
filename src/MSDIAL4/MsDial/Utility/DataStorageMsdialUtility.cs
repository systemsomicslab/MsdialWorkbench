using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class DataStorageMsdialUtility
    {
        private DataStorageMsdialUtility() { }

        public static void SaveToXmlFile(object obj, string path, Type type) {
            var serializer = new DataContractSerializer(type);
            var fs = new FileStream(path, FileMode.Create);
            serializer.WriteObject(fs, obj);
            fs.Close();
        }

        public static object LoadFromXmlFile(string path, Type type) {
            var serializer = new DataContractSerializer(type);
            var fs = new FileStream(path, FileMode.Open);
            object obj = serializer.ReadObject(fs);
            fs.Close();
            return obj;
        }
    }
}
