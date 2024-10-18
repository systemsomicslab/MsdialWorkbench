using System;
using System.Collections.Generic;
using System.Threading;

namespace CompMs.Common.DataStructure
{
    public sealed class CacheProxy<T, U>
    {
        private readonly LruCache<T, U> _cache;
        private readonly Func<T, U> _factory;
        private readonly ReaderWriterLockSlim _locker;

        public CacheProxy(int capacity, Func<T, U> factory) {
            _cache = new LruCache<T, U>(capacity);
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _locker = new ReaderWriterLockSlim();
        }

        public CacheProxy(int capacity, Func<T, U> factory, IEqualityComparer<T> comparer) {
            _cache = new LruCache<T, U>(capacity, comparer);
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _locker = new ReaderWriterLockSlim();
        }

        public U GetOrAdd(T key) {
            _locker.EnterReadLock();
            try {
                if (_cache.ContainsKey(key)) {
                    return _cache.Get(key);
                }
            }
            finally {
                _locker.ExitReadLock();
            }

            var item = _factory.Invoke(key);
            _locker.EnterWriteLock();
            try {
                _cache.Put(key, item);
            }
            finally {
                _locker.ExitWriteLock();
            }
            return item;
        }
    }
}
