using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Rfx.Riken.OsakaUniv;


namespace Rfx.Riken.OsakaUniv.MessagePack
{

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
