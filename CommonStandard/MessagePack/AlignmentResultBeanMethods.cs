using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using Rfx.Riken.OsakaUniv;

namespace CompMs.Common.MessagePack
{
    public static class AlignmentResultBeanMethods
    {
        const int version = 2;

        public static void SaveAlignmentResultBeanToFile(AlignmentResultBean bean, string path)
        {
            var collection = ((AlignmentResultBean)(object)bean).AlignmentPropertyBeanCollection;
            var peakProperty = collection.Select(x => x.AlignedPeakPropertyBeanCollection).ToList();

            var filePath = GetNewFileName(path);

            // clear peak AlignedPeakPropertyBeanCollection
            foreach (var b in collection)
            {
                b.AlignedPeakPropertyBeanCollection = new ObservableCollection<AlignedPeakPropertyBean>();
            }

            // save AlignmentResultBean without AlignedPeakPropertyBeanCollection
            MessagePackDefaultHandler.SaveToFile<AlignmentResultBean>(bean, path);
            MessagePackDefaultHandler.SaveLargeListToFile<ObservableCollection<AlignedPeakPropertyBean>>(peakProperty, filePath);

            for (var i = 0; i < peakProperty.Count; i++)
            {
                collection[i].AlignedPeakPropertyBeanCollection = peakProperty[i];
            }
            return;
        }


        public static void LoadAlignmentResultBeanFromFile(AlignmentResultBean bean, string path)
        {
            var collection = ((AlignmentResultBean)(object)bean).AlignmentPropertyBeanCollection;

            if (collection != null && collection.Count > 0 && (collection[0].AlignedPeakPropertyBeanCollection == null || collection[0].AlignedPeakPropertyBeanCollection.Count == 0))
            {
                var filePath = GetNewFileName(path);
                //check 190819 updated LargeListMessagePack
                if (File.Exists(filePath))
                {
                    var alignedPeakPropertyBeans = MessagePackDefaultHandler.LoadLargerListFromFile<ObservableCollection<AlignedPeakPropertyBean>>(filePath);
                    for (var i = 0; i < alignedPeakPropertyBeans.Count; i++)
                    {
                        collection[i].AlignedPeakPropertyBeanCollection = alignedPeakPropertyBeans[i];
                    }
                }
                else // read from many files in the folder
                {
                    var counter = 0;
                    foreach (var b in collection)
                    {
                        var filePaths = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + "_AlignedPeakPropertyBeans\\AlignedPeakPropertyBean_" + counter + ".arf" + version;
                        b.AlignedPeakPropertyBeanCollection = MessagePackHandler.LoadFromFile<ObservableCollection<AlignedPeakPropertyBean>>(filePaths);
                        counter++;
                    }

                }
            }
            return;
        }

        private static string GetNewFileName(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folder = Path.GetDirectoryName(path);
            var ext = Path.GetExtension(path);
            return folder + "\\" + fileName + "_PeakProperties" + ext;
        }
    }
}
