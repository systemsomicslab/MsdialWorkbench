using CompMs.MsdialCore.MSDec;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Parser;

public sealed class FileBackedMsdecResults : IReadOnlyList<MSDecResult>, IDisposable {
    private FileStream? _stream;
    private readonly int _version;
    private readonly IReadOnlyList<long> _seekPointers;
    private readonly bool _isAnnotationInfoIncluded;
    private readonly int _cacheCapacity;
    private readonly LinkedList<(int Index, MSDecResult Result)> _cacheOrder = new LinkedList<(int Index, MSDecResult Result)>();
    private readonly Dictionary<int, LinkedListNode<(int Index, MSDecResult Result)>> _cache = new Dictionary<int, LinkedListNode<(int Index, MSDecResult Result)>>();

    public FileBackedMsdecResults(string file, int cacheCapacity = 16) {
        _stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        MsdecResultsReader.GetSeekPointers(_stream, out _version, out var seekPointers, out _isAnnotationInfoIncluded);
        _seekPointers = seekPointers;
        _cacheCapacity = Math.Max(0, cacheCapacity);
    }

    public int Count => _seekPointers.Count;

    public MSDecResult this[int index] {
        get {
            if (_stream is null) {
                throw new ObjectDisposedException(nameof(FileBackedMsdecResults));
            }
            if ((uint)index >= (uint)_seekPointers.Count) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (_cache.TryGetValue(index, out var node)) {
                _cacheOrder.Remove(node);
                _cacheOrder.AddFirst(node);
                return node.Value.Result;
            }

            var result = MsdecResultsReader.ReadMSDecResult(_stream, _seekPointers[index], _version, _isAnnotationInfoIncluded);
            if (_cacheCapacity > 0) {
                var newNode = new LinkedListNode<(int Index, MSDecResult Result)>((index, result));
                _cacheOrder.AddFirst(newNode);
                _cache[index] = newNode;
                if (_cache.Count > _cacheCapacity) {
                    var last = _cacheOrder.Last;
                    if (last != null) {
                        _cache.Remove(last.Value.Index);
                        _cacheOrder.RemoveLast();
                    }
                }
            }
            return result;
        }
    }

    public IEnumerator<MSDecResult> GetEnumerator() {
        for (var i = 0; i < Count; i++) {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public void Dispose() {
        _cache.Clear();
        _cacheOrder.Clear();
        _stream?.Dispose();
        _stream = null;
    }
}
