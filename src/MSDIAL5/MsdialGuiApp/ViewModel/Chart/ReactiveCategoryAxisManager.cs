using CompMs.Graphics.AxisManager.Generic;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public sealed class ReactiveCategoryAxisManager<U, T> : CategoryAxisManager<U, T>
    {
        public ReactiveCategoryAxisManager(
            IObservable<IReadOnlyCollection<T>> collectionSource,
            Func<T, U> toKey,
            Func<T, string>? toLabel = null)
            : base(new T[0], toKey, toLabel) {

            _unSubscriber = collectionSource.ObserveOnDispatcher().Subscribe(UpdateCollection);
        }

        private IDisposable? _unSubscriber;
        private bool _disposedValue;

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (!_disposedValue) {
                if (disposing) {
                    _unSubscriber?.Dispose();
                    _unSubscriber = null;
                }

                _disposedValue = true;
            }
        }
    }

    public sealed class ReactiveCategoryAxisManager<T>: CategoryAxisManager<T>
    {
        public ReactiveCategoryAxisManager(
            IObservable<IReadOnlyCollection<T>> collectionSource,
            Func<T, string>? toLabel = null)
            : base(new T[0], toLabel) {

            _unSubscriber = collectionSource.ObserveOnDispatcher().Subscribe(UpdateCollection);
        }

        private IDisposable? _unSubscriber;
        private bool _disposedValue;

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (!_disposedValue) {
                if (disposing) {
                    _unSubscriber?.Dispose();
                    _unSubscriber = null;
                }

                _disposedValue = true;
            }
        }
    }

    public static class ReactiveCategoryAxisManager
    {
        public static ReactiveCategoryAxisManager<U, T> ToReactiveCategoryAxisManager<T, U>(
            this IObservable<IReadOnlyCollection<T>> self,
            Func<T, U> toKey) {

            return new ReactiveCategoryAxisManager<U, T>(self, toKey);
        }

        public static ReactiveCategoryAxisManager<U, T> ToReactiveCategoryAxisManager<T, U>(
            this IObservable<IReadOnlyCollection<T>> self,
            Func<T, U> toKey,
            Func<T, string> toLabel) {

            return new ReactiveCategoryAxisManager<U, T>(self, toKey, toLabel);
        }

        public static ReactiveCategoryAxisManager<T> ToReactiveCategoryAxisManager<T>(
            this IObservable<IReadOnlyCollection<T>> self) {

            return new ReactiveCategoryAxisManager<T>(self);
        }

        public static ReactiveCategoryAxisManager<T> ToReactiveCategoryAxisManager<T>(
            this IObservable<IReadOnlyCollection<T>> self,
            Func<T, string> toLabel) {

            return new ReactiveCategoryAxisManager<T>(self, toLabel);
        }
    }
}
