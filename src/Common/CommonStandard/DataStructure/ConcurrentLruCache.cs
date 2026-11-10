using System.Collections.Generic;
using System.Threading;

namespace CompMs.Common.DataStructure;

public sealed class ConcurrentLruCache<TKey, TValue>
{
    private readonly LruCache<TKey, TValue> _cache;
    private readonly ReaderWriterLockSlim _locker;

    public ConcurrentLruCache(int capacity, IEqualityComparer<TKey> comparer) {
        _cache = new LruCache<TKey, TValue>(capacity, comparer);
        _locker = new();
    }

    public ConcurrentLruCache(int capacity) {
        _cache = new LruCache<TKey, TValue>(capacity);
        _locker = new();
    }

    public TValue Get(TKey key) {
        _locker.EnterWriteLock();
        try {
            return _cache.Get(key);
        }
        finally {
            _locker.ExitWriteLock();
        }
    }

    public void Put(TKey key, TValue value) {
        _locker.EnterWriteLock();
        try {
            _cache.Put(key, value);
        }
        finally {
            _locker.ExitWriteLock();
        }
    }

    public bool ContainsKey(TKey key) {
        _locker.EnterReadLock();
        try {
            return _cache.ContainsKey(key);
        }
        finally {
            _locker.ExitReadLock();
        }
    }

    public bool TryGet(TKey key, out TValue value) {
        _locker.EnterWriteLock();
        try {
            if (_cache.ContainsKey(key)) {
                value = _cache.Get(key);
                return true;
            }
            value = default!;
            return false;
        }
        finally {
            _locker.ExitWriteLock();
        }
    }
}
