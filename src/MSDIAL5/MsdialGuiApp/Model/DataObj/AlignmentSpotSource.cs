using Accord;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AlignmentSpotSource : IDisposable
    {
        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly AlignmentResultContainer _container;
        private readonly ChromatogramSerializer<ChromatogramSpotInfo> _chromatogramSerializer;
        private AlignmentSpotPropertyModelCollection? _spots;
        private bool _disposedValue;

        public AlignmentSpotSource(AlignmentFileBeanModel alignmentFile, AlignmentResultContainer container, ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSerializer) {
            _alignmentFile = alignmentFile ?? throw new ArgumentNullException(nameof(alignmentFile));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _spots = new AlignmentSpotPropertyModelCollection(container.AlignmentSpotProperties);
            _chromatogramSerializer = chromatogramSerializer ?? throw new ArgumentNullException(nameof(chromatogramSerializer));
        }

        public AlignmentSpotPropertyModelCollection? Spots => _spots;

        public IObservable<(double, double)> ObserveRange(Func<AlignmentSpotPropertyModel, double> map, out IDisposable disposable) {
            if (_spots is null) {
                throw new ObjectDisposedException(nameof(AlignmentSpotSource));
            }
            var items = _spots.Items;
            var subject = new BehaviorSubject<(double, double)>((0d, 1d));
            if (items.Count != 0) {
                subject.OnNext((items.Min(map), items.Max(map)));
            }
            var observable = items.CollectionChangedAsObservable();
            var newRange = observable.WithLatestFrom(subject, (e, p) => {
                double v;
                switch (e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        v = map((AlignmentSpotPropertyModel)e.NewItems[0]);
                        return (Math.Min(v, p.Item1), Math.Max(p.Item2, v));
                    case NotifyCollectionChangedAction.Remove:
                        if (items.Count == 0) {
                            return (0d, 1d);
                        }
                        v = map((AlignmentSpotPropertyModel)e.OldItems[0]);
                        if (p.Item1 == v) {
                            return (items.Min(map), p.Item2);
                        }
                        else if (p.Item2 == v) {
                            return (p.Item1, items.Max(map));
                        }
                        return p;
                    case NotifyCollectionChangedAction.Reset:
                        return (0d, 1d);
                    case NotifyCollectionChangedAction.Replace:
                        v = map((AlignmentSpotPropertyModel)e.NewItems[0]);
                        p = (Math.Min(v, p.Item1), Math.Max(p.Item2, v));
                        v = map((AlignmentSpotPropertyModel)e.OldItems[0]);
                        if (p.Item1 == v) {
                            return (items.Min(map), p.Item2);
                        }
                        else if (p.Item2 == v) {
                            return (p.Item1, items.Max(map));
                        }
                        return p;
                    default:
                        return p;
                }
            });
            newRange.Subscribe(subject);
            disposable = subject;
            return subject;
        }

        public IObservable<(double, double)> ObserveRange(Func<AlignmentSpotPropertyModel, double> map, ICollection<IDisposable> disposables) {
            var result = ObserveRange(map, out var disposable);
            disposables.Add(disposable);
            return result;
        }

        public async Task DuplicateSpotAsync(AlignmentSpotPropertyModel spot) {
            if (spot is null || _spots is null) {
                return;
            }
            var mSDecResult = await _alignmentFile.LoadMSDecResultByIndexAsync(spot.MasterAlignmentID).ConfigureAwait(false);
            if (mSDecResult is null) {
                return;
            }
            _spots.Duplicates(spot);
            var spotInfo = await _alignmentFile.LoadEicInfoByIndexAsync(spot.MasterAlignmentID, _chromatogramSerializer).ConfigureAwait(false);
            await Task.WhenAll(new[]
            {
                _alignmentFile.SaveAlignmentResultAsync(_container),
                _alignmentFile.AppendMSDecResultAsync(mSDecResult),
                _alignmentFile.AppendEicInfoAsync(_chromatogramSerializer, spotInfo),
            }).ConfigureAwait(false);
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    _spots?.Dispose();
                    _spots = null;
                }
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
