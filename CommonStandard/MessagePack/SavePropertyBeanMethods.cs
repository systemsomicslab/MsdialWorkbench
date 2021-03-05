using System.Collections.Generic;
using System.Linq;
using System.IO;
using Rfx.Riken.OsakaUniv;

namespace CompMs.Common.MessagePack {

    public static class SavePropertyBeanMethods
    {
        const int version = 2;

        // To save msp file separately
        public static void SaveSavePropertyBeanToFile(SavePropertyBean bean, string path)
        {
            var mspList = bean.MspFormatCompoundInformationBeanList.ToList();
            bean.MspFormatCompoundInformationBeanList = new List<MspFormatCompoundInformationBean>();

            var mspPath = GetNewFileName(path);
            MessagePackDefaultHandler.SaveToFile<SavePropertyBean>(bean, path);
            MspMethods.SaveMspToFile(mspList, mspPath);

            bean.MspFormatCompoundInformationBeanList = mspList;
        }

        public static void LoadSavePropertyBeanFromFile(SavePropertyBean bean, string path)
        {
            var mspPath = GetNewFileName(path);
            if (File.Exists(mspPath))
            {
                bean.MspFormatCompoundInformationBeanList = MspMethods.LoadMspFromFile(mspPath);
            }
            return;
        }

        private static string GetNewFileName(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folder = Path.GetDirectoryName(path);
            return Path.Combine(folder, fileName + "_Loaded.msp2");
        }
    }
}
