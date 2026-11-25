using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// Reads the datasets <c>LcmsScenarioTests</c> runs from the JSON file the run parameter
    /// <see cref="RunParameters.DatasetsFile"/> names. The file names and settings of the demo data
    /// stay with whoever runs the tests instead of in the repository; see
    /// lcms-datasets.example.json for the format.
    /// </summary>
    internal static class DatasetsFile
    {
        public static IReadOnlyList<RawDataset> Load(string path) {
            List<Entry> entries;
            using (var stream = File.OpenRead(path)) {
                entries = (List<Entry>)new DataContractJsonSerializer(typeof(List<Entry>)).ReadObject(stream);
            }
            return entries.Select((entry, i) => entry.ToDataset(path, i)).ToList();
        }

        [DataContract]
        private sealed class Entry
        {
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "folder")] public string? Folder { get; set; }
            [DataMember(Name = "polarity")] public string? Polarity { get; set; }
            [DataMember(Name = "minimumPeakHeight")] public double? MinimumPeakHeight { get; set; }
            [DataMember(Name = "files")] public string[]? Files { get; set; }

            public RawDataset ToDataset(string path, int index) {
                string Missing(string member) => $"Dataset {index + 1} in '{path}' has no \"{member}\".";
                if (string.IsNullOrWhiteSpace(Name)) throw new InvalidDataException(Missing("name"));
                if (string.IsNullOrWhiteSpace(Folder)) throw new InvalidDataException(Missing("folder"));
                if (MinimumPeakHeight is null) throw new InvalidDataException(Missing("minimumPeakHeight"));
                if (Files is null || Files.Length == 0) throw new InvalidDataException(Missing("files"));
                if (!Enum.TryParse<Polarity>(Polarity, ignoreCase: true, out var polarity)) {
                    throw new InvalidDataException($"Dataset '{Name}' in '{path}' has polarity '{Polarity}'; use \"Positive\" or \"Negative\".");
                }
                return new RawDataset(Name!, Folder!, polarity, MinimumPeakHeight.Value, Files);
            }
        }
    }
}
