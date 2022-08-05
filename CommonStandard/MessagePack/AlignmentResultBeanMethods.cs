using Rfx.Riken.OsakaUniv;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;

namespace CompMs.Common.MessagePack
{
    public static class AlignmentResultBeanMethods
    {
        const int version = 2;

        public static void SaveAlignmentResultBeanToFile(AlignmentResultBean bean, string path)
        {
            var collection = ((AlignmentResultBean)(object)bean).AlignmentPropertyBeanCollection;

            var peakProperty = collection.Select(x => x.AlignedPeakPropertyBeanCollection).ToList();
            var filePath = GetNewPeakFileName(path);

            var driftProperty = collection.Select(prop => prop.AlignedDriftSpots).ToList();
            var driftPath = GetNewDriftFileName(path);


            // clear peak AlignedPeakPropertyBeanCollection and AlignedDriftSpotPropertyBean
            foreach (var b in collection)
            {
                b.AlignedPeakPropertyBeanCollection = new ObservableCollection<AlignedPeakPropertyBean>();
                b.AlignedDriftSpots = new ObservableCollection<AlignedDriftSpotPropertyBean>();
            }


            // save AlignmentResultBean without AlignedPeakPropertyBeanCollection
            MessagePackDefaultHandler.SaveToFile<AlignmentResultBean>(bean, path);
            MessagePackDefaultHandler.SaveLargeListToFile<ObservableCollection<AlignedPeakPropertyBean>>(peakProperty, filePath);
            MessagePackDefaultHandler.SaveLargeListToFile<ObservableCollection<AlignedDriftSpotPropertyBean>>(driftProperty, driftPath);

            for (var i = 0; i < peakProperty.Count; i++)
            {
                collection[i].AlignedPeakPropertyBeanCollection = peakProperty[i];
                collection[i].AlignedDriftSpots = driftProperty[i];
            }
            return;
        }


        public static void LoadAlignmentResultBeanFromFile(AlignmentResultBean bean, string path)
        {
            var collection = ((AlignmentResultBean)(object)bean).AlignmentPropertyBeanCollection;

            if (collection != null && collection.Count > 0 && (collection[0].AlignedPeakPropertyBeanCollection == null || collection[0].AlignedPeakPropertyBeanCollection.Count == 0))
            {
                var filePath = GetNewPeakFileName(path);
                var driftPath = GetNewDriftFileName(path);
                //check 190819 updated LargeListMessagePack
                if (File.Exists(filePath) && File.Exists(driftPath)) {
                    var alignedPeakPropertyBeans = MessagePackDefaultHandler.LoadLargerListFromFile<ObservableCollection<AlignedPeakPropertyBean>>(filePath);
                    var alignedDriftSpotPropertyBeans = MessagePackDefaultHandler.LoadLargerListFromFile<ObservableCollection<AlignedDriftSpotPropertyBean>>(driftPath);
                    for (var i = 0; i < alignedPeakPropertyBeans.Count; i++) {
                        collection[i].AlignedPeakPropertyBeanCollection = alignedPeakPropertyBeans[i];
                    }
                    for (var i = 0; i < alignedDriftSpotPropertyBeans.Count; i++) {
                        collection[i].AlignedDriftSpots = alignedDriftSpotPropertyBeans[i];
                    }
                }
                else if (File.Exists(filePath))
                {
                    var alignedPeakPropertyBeans = MessagePackDefaultHandler.LoadLargerListFromFile<ObservableCollection<AlignedPeakPropertyBean>>(filePath);
                    for (var i = 0; i < alignedPeakPropertyBeans.Count; i++) {
                        collection[i].AlignedPeakPropertyBeanCollection = alignedPeakPropertyBeans[i];
                    }
                }
                else // read from many files in the folder
                {
                    var counter = 0;
                    foreach (var b in collection)
                    {
                        var paths = new string[] {
                            Path.GetDirectoryName(path),
                            Path.GetFileNameWithoutExtension(path) + "_AlignedPeakPropertyBeans",
                            "AlignedPeakPropertyBean_" + counter + ".arf" + version
                        };
                        var filePaths = Path.Combine(paths);
                        b.AlignedPeakPropertyBeanCollection = MessagePackHandler.LoadFromFile<ObservableCollection<AlignedPeakPropertyBean>>(filePaths);

                        var driftpaths = new string[] {
                            Path.GetDirectoryName(path),
                            Path.GetFileNameWithoutExtension(path) + "_AlignedDriftSpotPropertyBeans",
                            "AlignedDriftSpotPropertyBean_" + counter + ".arf" + version
                        };
                        var driftPaths = Path.Combine(driftpaths);
                        b.AlignedDriftSpots = MessagePackHandler.LoadFromFile<ObservableCollection<AlignedDriftSpotPropertyBean>>(driftPaths);

                        counter++;
                    }

                }
            }
            return;
        }

        private static string GetNewPeakFileName(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folder = Path.GetDirectoryName(path);
            var ext = Path.GetExtension(path);
            return Path.Combine(folder, fileName + "_PeakProperties" + ext);
        }

        private static string GetNewDriftFileName(string path) {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folder = Path.GetDirectoryName(path);
            var ext = Path.GetExtension(path);
            return Path.Combine(folder, fileName + "_DriftSpots" + ext);
        }
    }
}
