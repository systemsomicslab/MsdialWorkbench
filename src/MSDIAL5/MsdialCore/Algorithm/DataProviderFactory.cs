using CompMs.Raw.Contract;
using System;

namespace CompMs.MsdialCore.Algorithm;

public static class DataProviderFactory
{
    public static IDataProviderFactory<U> ContraMap<T, U>(this IDataProviderFactory<T> factory, Func<U, T> map) {
        return new MappedFactory<U, T>(factory, map);
    }

    public static IImagingDataProviderFactory<U> ContraMap<T, U>(this IImagingDataProviderFactory<T> factory, Func<U, T> map) {
        return new MappedImagingFactory<U, T>(factory, map);
    }

    sealed class MappedFactory<T, U> : IDataProviderFactory<T>
    {
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

    sealed class MappedImagingFactory<T, U> : IImagingDataProviderFactory<T>
    {
        private readonly IImagingDataProviderFactory<U> _impl;
        private readonly Func<T, U> _map;

        public MappedImagingFactory(IImagingDataProviderFactory<U> impl, Func<T, U> map) {
            _impl = impl ?? throw new ArgumentNullException(nameof(impl));
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        IDataProvider IDataProviderFactory<T>.Create(T source) {
            return ((IImagingDataProviderFactory<T>)this).Create(source);
        }

        IImagingDataProvider IImagingDataProviderFactory<T>.Create(T source) {
            return _impl.Create(_map(source));
        }
    }
}
