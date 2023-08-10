using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using MessagePack;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AlignmentResultContainer
    {
        [Key(0)]
        public Ionization Ionization { get; set; }
        [Key(1)]
        public int AlignmentResultFileID { get; set; }
        [Key(2)]
        public int TotalAlignmentSpotCount { get; set; }
        [Key(3)]
        public ObservableCollection<AlignmentSpotProperty> AlignmentSpotProperties { get; set; }
        [Key(4)]
        public bool IsNormalized { get; set; }
        [IgnoreMember]
        public Task LoadAlginedPeakPropertiesTask { get; private set; } = Task.CompletedTask;
        //public AnalysisParametersBean AnalysisParamForLC { get; set; }
        //public AnalysisParamOfMsdialGcms AnalysisParamForGC { get; set; }

        public void Save(AlignmentFileBean file) {
            var peakProperty = AlignmentSpotProperties.Select(x => x.AlignedPeakProperties).ToList();
            var driftProperty = AlignmentSpotProperties.Select(prop => prop.AlignmentDriftSpotFeatures).ToList();
            foreach (var b in AlignmentSpotProperties) {
                b.AlignedPeakProperties = null;
                b.AlignmentDriftSpotFeatures = null;
            }

            MessagePackHandler.SaveToFile(this, file.FilePath);
            LoadAlginedPeakPropertiesTask.Wait();
            var chromatogramPeakFile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_PeakProperties" + Path.GetExtension(file.FilePath));
            var driftSpotFile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_DriftSopts" + Path.GetExtension(file.FilePath));
            MessagePackDefaultHandler.SaveLargeListToFile(peakProperty, chromatogramPeakFile);
            MessagePackDefaultHandler.SaveLargeListToFile(driftProperty, driftSpotFile);

            for (var i = 0; i < peakProperty.Count; i++) {
                AlignmentSpotProperties[i].AlignedPeakProperties = peakProperty[i];
                AlignmentSpotProperties[i].AlignmentDriftSpotFeatures = driftProperty[i];
            }

            var tagfile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_tags.xml");
            var defsElement = new XElement("Definitions");
            foreach (var tag in PeakSpotTag.AllTypes()) {
                var tagElement = new XElement("Tag", new XElement("Id", tag.Id), new XElement("Label", tag.Label));
                defsElement.Add(tagElement);
            }
            var peaksElement = new XElement("Peaks");
            foreach (var peak in AlignmentSpotProperties.SelectMany(Flatten)) {
                if (!peak.TagCollection.Any()) {
                    continue;
                }
                var peakElement = new XElement("Peak");
                peakElement.SetAttributeValue("Id", peak.MasterAlignmentID);
                foreach (var tag in peak.TagCollection.Selected) {
                    peakElement.Add(new XElement("Tag", tag.Id));
                }
                peaksElement.Add(peakElement);
            }
            var doc = new XElement("PeakSpotTags", defsElement, peaksElement);
            doc.Save(tagfile);
        }

        public static AlignmentResultContainer Load(AlignmentFileBean file)
        {
            var containerFile = file.FilePath;
            var chromatogramPeakFile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_PeakProperties" + Path.GetExtension(file.FilePath));
            var driftSpotFile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_DriftSopts" + Path.GetExtension(file.FilePath));

            var result = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(containerFile);
            if (result is null) {
                return null;
            }

            var collection = result.AlignmentSpotProperties;

            if (collection != null && collection.Count > 0 && collection[0].AlignedPeakProperties is null)
            {
                if (File.Exists(chromatogramPeakFile)) {
                    var alignmentChromPeakFeatures = MessagePackDefaultHandler.LoadLargerListFromFile<List<AlignmentChromPeakFeature>>(chromatogramPeakFile);
                    for (var i = 0; i < alignmentChromPeakFeatures.Count; i++) {
                        collection[i].AlignedPeakProperties = alignmentChromPeakFeatures[i];
                    }
                }
                if (File.Exists(driftSpotFile)) {
                    var alignmentDriftSpotProperties = MessagePackDefaultHandler.LoadLargerListFromFile<List<AlignmentSpotProperty>>(driftSpotFile);
                    for (var i = 0; i < alignmentDriftSpotProperties.Count; i++) {
                        collection[i].AlignmentDriftSpotFeatures = alignmentDriftSpotProperties[i];
                    }
                }
            }

            var tagfile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_tags.xml");
            if (File.Exists(tagfile)) {
                var doc = XElement.Load(tagfile);
                var dic = new Dictionary<int, PeakSpotTag[]>();
                foreach (var peakElement in doc.Descendants("Peak")) {
                    if (int.TryParse(peakElement.Attribute("Id").Value, out var id)) {
                        var tags = peakElement.Elements("Tag")
                            .Select(elem => int.TryParse(elem.Value, out var v) ? PeakSpotTag.GetById(v) : null)
                            .Where(v => v != null)
                            .ToArray();
                        dic[id] = tags;
                    }
                }
                foreach (var peak in collection.SelectMany(Flatten)) {
                    if (dic.TryGetValue(peak.MasterAlignmentID, out var tags)) {
                        peak.TagCollection.Set(tags);
                    }
                }
            }
            return result;
        }

        public static AlignmentResultContainer LoadLazy(AlignmentFileBean file, CancellationToken token = default) {
            var containerFile = file.FilePath;
            var result = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(containerFile);
            if (result is null) {
                return null;
            }

            var collection = result.AlignmentSpotProperties;
            if (collection != null && collection.Count > 0 && collection[0].AlignedPeakProperties is null) {
                var driftSpotFile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_DriftSopts" + Path.GetExtension(file.FilePath));
                if (File.Exists(driftSpotFile)) {
                    var alignmentDriftSpotProperties = MessagePackDefaultHandler.LoadLargerListFromFile<List<AlignmentSpotProperty>>(driftSpotFile);
                    foreach (var (alignmentChromPeakFeature, i) in alignmentDriftSpotProperties.WithIndex()) {
                        collection[i].AlignmentDriftSpotFeatures = alignmentChromPeakFeature;
                    }
                }

                var chromatogramPeakFile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_PeakProperties" + Path.GetExtension(file.FilePath));
                if (File.Exists(chromatogramPeakFile)) {
                    var task = Task.FromResult<List<AlignmentChromPeakFeature>>(null);
                    var alignmentChromPeakFeatures = MessagePackDefaultHandler.LoadIncrementalLargerListFromFile<List<AlignmentChromPeakFeature>>(chromatogramPeakFile).SelectMany(featuress => featuress);
                    var enumerator = alignmentChromPeakFeatures.GetEnumerator();
                    foreach (var c in collection) {
                        c.AlignedPeakPropertiesTask = task = task.ContinueWith(_ =>
                        {
                            if (enumerator.MoveNext()) {
                                return enumerator.Current;
                            }
                            return new List<AlignmentChromPeakFeature>(0);
                        },
                        token);
                    }
                    var allTaskCompleted = Task.Run(async () =>
                    {
                        try {
                            await task.ConfigureAwait(false);
                        }
                        finally {
                            enumerator.Dispose();
                        }
                    }, token);
                    result.LoadAlginedPeakPropertiesTask = allTaskCompleted;
                }
            }

            var tagfile = Path.Combine(Path.GetDirectoryName(file.FilePath), Path.GetFileNameWithoutExtension(file.FilePath) + "_tags.xml");
            if (File.Exists(tagfile)) {
                var doc = XElement.Load(tagfile);
                var dic = new Dictionary<int, PeakSpotTag[]>();
                foreach (var peakElement in doc.Descendants("Peak")) {
                    if (int.TryParse(peakElement.Attribute("Id").Value, out var id)) {
                        var tags = peakElement.Elements("Tag")
                            .Select(elem => int.TryParse(elem.Value, out var v) ? PeakSpotTag.GetById(v) : null)
                            .Where(v => v != null)
                            .ToArray();
                        dic[id] = tags;
                    }
                }
                foreach (var peak in collection.SelectMany(Flatten)) {
                    if (dic.TryGetValue(peak.MasterAlignmentID, out var tags)) {
                        peak.TagCollection.Set(tags);
                    }
                }
            }
            return result;
        }

        private static IEnumerable<AlignmentSpotProperty> Flatten(AlignmentSpotProperty spot) {
            return (spot.AlignmentDriftSpotFeatures?.SelectMany(Flatten) ?? Enumerable.Empty<AlignmentSpotProperty>()).Prepend(spot);
        }

        public static AlignmentResultContainerSlim GetSlimData(AlignmentFileBean file) {
            return MessagePackHandler.LoadFromFile<AlignmentResultContainerSlim>(file.FilePath);
        }

        ///<summary>
        ///This is dummy class to check serialized AlignmentResultContainer properties.
        ///This class doesn't contain peak spot informations.
        ///</summary>
        [MessagePackObject]
        public sealed class AlignmentResultContainerSlim
        {
            [Key(0)]
            public Ionization Ionization { get; set; }
            [Key(1)]
            public int AlignmentResultFileID { get; set; }
            [Key(2)]
            public int TotalAlignmentSpotCount { get; set; }
            [Key(4)]
            public bool IsNormalized { get; set; }
        }
    }
}
