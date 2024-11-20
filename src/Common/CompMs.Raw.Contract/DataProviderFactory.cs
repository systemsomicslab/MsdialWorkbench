using System;

namespace CompMs.Raw.Contract;

public static class DataProviderFactory
{
    public static IDataProviderFactory<U> ContraMap<T, U>(this IDataProviderFactory<T> factory, Func<U, T> map) {
        return new MappedFactory<U, T>(factory, map);
    }

    sealed class MappedFactory<T, U> : IDataProviderFactory<T> {
        private readonly IDataProviderFactory<U> _impl;
        private readonly Func<T, U> _map;

        public MappedFactory(IDataProviderFactory<U> impl, Func<T, U> map) {
            _impl = impl ?? throw new ArgumentNullException(nameof(impl));
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        IDataProvider IDataProviderFactory<T>.Create(T source) {
            return _impl.Create(_map(source));
        }
    }
}
