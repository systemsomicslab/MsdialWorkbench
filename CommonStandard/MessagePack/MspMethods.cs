using Rfx.Riken.OsakaUniv;
using System.Collections.Generic;

namespace CompMs.Common.MessagePack {

    public static class MspMethods
    {
        const int version = 2;

        public static void SaveMspToFile(List<MspFormatCompoundInformationBean> bean, string path)
        {
            MessagePackDefaultHandler.SaveLargeListToFile<MspFormatCompoundInformationBean>(bean, path);
        }

        public static List<MspFormatCompoundInformationBean> LoadMspFromFile(string path)
        {
            return MessagePackDefaultHandler.LoadLargerListFromFile<MspFormatCompoundInformationBean>(path);
        }


    }
}
