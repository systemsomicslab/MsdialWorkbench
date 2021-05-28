using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using System;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ReactiveAxisManager<T> : ContinuousAxisManager<T>, IDisposable where T : IConvertible
    {
        public ReactiveAxisManager(IObservable<Range> rangeSource) : base(new Range(0, 1)) {
            rangeUnSubscriber = rangeSource.Subscribe(UpdateRange);
        }

        private void UpdateRange(Range initial) {
            UpdateInitialRange(initial);
            Range = initial;
        }

        private bool disposedValue;
        private IDisposable rangeUnSubscriber;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    rangeUnSubscriber.Dispose();
                    rangeUnSubscriber = null;
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
}
