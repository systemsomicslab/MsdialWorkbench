using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CompMs.App.MsdialConsole.ReleaseTests
{
    /// <summary>
    /// Reads the cases from the JSON file the run parameter <see cref="RunParameters.CasesFile"/>
    /// names. The demo data's file names and settings stay with whoever runs the checks instead of
    /// in the repository; see console-cases.example.json for the format.
    /// </summary>
    internal static class CasesFile
    {
        public static IReadOnlyList<ConsoleCase> Load(string path) {
            List<Entry> entries;
            var settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
            using (var stream = File.OpenRead(path)) {
                entries = (List<Entry>)new DataContractJsonSerializer(typeof(List<Entry>), settings).ReadObject(stream);
            }
            return entries.Select((entry, i) => entry.ToCase(path, i)).ToList();
        }

        [DataContract]
        private sealed class Entry
        {
            [DataMember(Name = "name")] public string? Name { get; set; }
            [DataMember(Name = "mode")] public string? Mode { get; set; }
            [DataMember(Name = "ionMode")] public string? IonMode { get; set; }
            [DataMember(Name = "rawData")] public string[]? RawData { get; set; }
            [DataMember(Name = "method")] public Dictionary<string, string>? Method { get; set; }
            [DataMember(Name = "riCompoundFile")] public string? RiCompoundFile { get; set; }
            [DataMember(Name = "knownIssue")] public string? KnownIssue { get; set; }

            public ConsoleCase ToCase(string path, int index) {
                string Missing(string member) => $"Case {index + 1} in '{path}' has no \"{member}\".";
                if (string.IsNullOrWhiteSpace(Name)) throw new InvalidDataException(Missing("name"));
                if (string.IsNullOrWhiteSpace(Mode)) throw new InvalidDataException(Missing("mode"));
                if (RawData is null || RawData.Length == 0) throw new InvalidDataException(Missing("rawData"));
                if (!Enum.TryParse<IonMode>(IonMode, ignoreCase: true, out var ionMode)) {
                    throw new InvalidDataException($"Case '{Name}' in '{path}' has ionMode '{IonMode}'; use \"Positive\" or \"Negative\".");
                }
                return new ConsoleCase(Name!, Mode!, ionMode, RawData) {
                    Method = Method ?? new Dictionary<string, string>(),
                    RiCompoundFile = RiCompoundFile,
                    KnownIssue = KnownIssue,
                };
            }
        }
    }
}
