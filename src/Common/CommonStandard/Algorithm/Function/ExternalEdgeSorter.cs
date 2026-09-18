using CompMs.Common.DataObj.NodeEdge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.Algorithm.Function
{
    // External merge sort: bounded candidate storage and at most 32 open input files.
    internal static class ExternalEdgeSorter
    {
        internal static IEnumerable<EdgeData> Sort(IEnumerable<EdgeData> edges, int chunkSize = 65536, string temporaryDirectory = null) {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));
            var folder = Path.Combine(temporaryDirectory ?? Path.GetTempPath(), "msdial-msn-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                var paths = new List<string>();
                var chunk = new List<Record>(chunkSize);
                long ordinal = 0;
                int fileNumber = 0;
                foreach (var edge in edges) {
                    chunk.Add(new Record(edge, ordinal++));
                    if (chunk.Count == chunkSize) {
                        paths.Add(WriteChunk(chunk, folder, fileNumber++));
                        chunk.Clear();
                    }
                }
                if (paths.Count == 0) {
                    chunk.Sort(Compare);
                    foreach (var record in chunk) yield return record.Edge;
                    yield break;
                }
                if (chunk.Count > 0) paths.Add(WriteChunk(chunk, folder, fileNumber++));
                chunk.Clear();
                while (paths.Count > 32) {
                    var next = new List<string>();
                    for (int i = 0; i < paths.Count; i += 32) {
                        var inputs = paths.Skip(i).Take(32).ToArray();
                        var output = Path.Combine(folder, (fileNumber++).ToString() + ".bin");
                        using (var writer = new BinaryWriter(File.Create(output))) {
                            foreach (var record in Merge(inputs)) Write(writer, record);
                        }
                        foreach (var input in inputs) File.Delete(input);
                        next.Add(output);
                    }
                    paths = next;
                }
                foreach (var record in Merge(paths)) yield return record.Edge;
            }
            finally {
                Directory.Delete(folder, true);
            }
        }

        private static string WriteChunk(List<Record> chunk, string folder, int number) {
            chunk.Sort(Compare);
            var path = Path.Combine(folder, number.ToString() + ".bin");
            using (var writer = new BinaryWriter(File.Create(path))) {
                foreach (var record in chunk) Write(writer, record);
            }
            return path;
        }

        private static IEnumerable<Record> Merge(IEnumerable<string> paths) {
            var readers = new List<BinaryReader>();
            var heads = new SortedSet<(Record Record, int Reader)>(
                Comparer<(Record Record, int Reader)>.Create((a, b) => Compare(a.Record, b.Record)));
            try {
                foreach (var path in paths) {
                    var reader = new BinaryReader(File.OpenRead(path));
                    readers.Add(reader);
                    if (reader.BaseStream.Position < reader.BaseStream.Length)
                        heads.Add((Read(reader), readers.Count - 1));
                }
                while (heads.Count > 0) {
                    var head = heads.Min;
                    heads.Remove(head);
                    yield return head.Record;
                    var reader = readers[head.Reader];
                    if (reader.BaseStream.Position < reader.BaseStream.Length)
                        heads.Add((Read(reader), head.Reader));
                }
            }
            finally {
                foreach (var reader in readers) reader.Dispose();
            }
        }

        private static int Compare(Record a, Record b) {
            var scoreOrder = b.Edge.score.CompareTo(a.Edge.score);
            return scoreOrder != 0 ? scoreOrder : a.Ordinal.CompareTo(b.Ordinal);
        }

        private static void Write(BinaryWriter writer, Record record) {
            writer.Write(record.Ordinal);
            writer.Write(record.Edge.source);
            writer.Write(record.Edge.target);
            writer.Write(record.Edge.score);
            writer.Write(record.Edge.matchpeakcount);
        }

        private static Record Read(BinaryReader reader) {
            var ordinal = reader.ReadInt64();
            return new Record(new EdgeData {
                source = reader.ReadInt32(), target = reader.ReadInt32(),
                score = reader.ReadDouble(), matchpeakcount = reader.ReadDouble(), linecolor = "red",
            }, ordinal);
        }

        private sealed class Record {
            internal Record(EdgeData edge, long ordinal) { Edge = edge; Ordinal = ordinal; }
            internal EdgeData Edge { get; }
            internal long Ordinal { get; }
        }
    }
}
