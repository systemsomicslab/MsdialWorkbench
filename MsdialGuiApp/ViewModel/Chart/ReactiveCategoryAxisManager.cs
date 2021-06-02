using CompMs.Graphics.AxisManager.Generic;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ReactiveCategoryAxisManager<U, T> : CategoryAxisManager<U, T>, IDisposable
    {
        public ReactiveCategoryAxisManager(
            IObservable<IReadOnlyCollection<T>> collectionSource,
            Func<T, U> toKey,
            Func<T, string> toLabel = null)
            : base(new T[0], toKey, toLabel) {

            unSubscriber = collectionSource.ObserveOnDispatcher().Subscribe(UpdateCollection);
        }

        private IDisposable unSubscriber;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    unSubscriber?.Dispose();
                    unSubscriber = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class ReactiveCategoryAxisManager<T>: CategoryAxisManager<T>, IDisposable
    {
        public ReactiveCategoryAxisManager(
            IObservable<IReadOnlyCollection<T>> collectionSource,
            Func<T, string> toLabel = null)
            : base(new T[0], toLabel) {

            unSubscriber = collectionSource.ObserveOnDispatcher().Subscribe(UpdateCollection);
        }

        private IDisposable unSubscriber;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    unSubscriber?.Dispose();
                    unSubscriber = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
