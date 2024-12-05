using CompMs.App.Msdial.Utility;
using CompMs.Common.Interfaces;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    public interface IMsSpectrumLoader<in T>
    {
        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is null.</exception>
        /// <returns></returns>
        IObservable<IMSScanProperty?> LoadScanAsObservable(T target);
    }

    public static class MsSpectrumLoaderExtension
    {
        public static IMsSpectrumLoader<U> Contramap<T, U>(this IMsSpectrumLoader<T> loader, Func<U, IObservable<T>> map) {
            return new ContramapImplLoader<T, U>(loader, map);
        }

        public static IMsSpectrumLoader<U> Contramap<T, U>(this IMsSpectrumLoader<T> loader, Func<U, T> map) {
            return new ContramapImplLoader<T, U>(loader, u => Observable.Return(map(u)));
        }

        class ContramapImplLoader<T, U> : IMsSpectrumLoader<U>
        {
            private readonly IMsSpectrumLoader<T> _loader;
            private readonly Func<U, IObservable<T>> _map;

            public ContramapImplLoader(IMsSpectrumLoader<T> loader, Func<U, IObservable<T>> map) {
                _loader = loader;
                _map = map;
            }

            public IObservable<IMSScanProperty?> LoadScanAsObservable(U target) {
                return _map(target).SelectSwitch(_loader.LoadScanAsObservable);
            }
        }
    }
}