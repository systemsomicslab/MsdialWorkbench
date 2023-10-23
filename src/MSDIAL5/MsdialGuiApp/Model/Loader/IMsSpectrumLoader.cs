using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
        IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(T target);

        IObservable<IMSScanProperty> LoadScanAsObservable(T target);
    }

    public static class MsSpectrumLoaderExtension
    {
        public static IMsSpectrumLoader<U> Contramap<T, U>(this IMsSpectrumLoader<T> loader, Func<U, IObservable<T>> map) {
            return new ContramapImplLoader<T, U>(loader, map);
        }

        public static IMsSpectrumLoader<U> Contramap<T, U>(this IMsSpectrumLoader<T> loader, Func<U, T> map) {
            return new ContramapImplLoader<T, U>(loader, u => Observable.Return(map(u)));
        }

        public static IObservable<MsSpectrum> LoadMsSpectrumAsObservable<T>(this IMsSpectrumLoader<T> loader, T target) {
            return loader.LoadSpectrumAsObservable(target).Select(s => new MsSpectrum(s));
        }

        class ContramapImplLoader<T, U> : IMsSpectrumLoader<U> {
            private readonly IMsSpectrumLoader<T> _loader;
            private readonly Func<U, IObservable<T>> _map;

            public ContramapImplLoader(IMsSpectrumLoader<T> loader, Func<U, IObservable<T>> map) {
                _loader = loader;
                _map = map;
            }

            public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(U target) {
                return _map(target).SelectSwitch(_loader.LoadSpectrumAsObservable);
            }

            public IObservable<IMSScanProperty> LoadScanAsObservable(U target) {
                return _map(target).SelectSwitch(_loader.LoadScanAsObservable);
            }
        }
    }
}