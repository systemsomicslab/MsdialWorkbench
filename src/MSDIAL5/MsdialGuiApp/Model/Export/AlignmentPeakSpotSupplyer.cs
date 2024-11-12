using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentPeakSpotSupplyer : BindableBase
    {
        private readonly IReadOnlyReactiveProperty<IAlignmentModel?> _currentResult;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _filter;

        public AlignmentPeakSpotSupplyer(IReadOnlyReactiveProperty<IAlignmentModel?> currentResult, PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter filter) {
            _currentResult = currentResult;
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public bool UseFilter {
            get => _useFilter;
            set => SetProperty(ref _useFilter, value);
        }
        private bool _useFilter;

        public IReadOnlyList<AlignmentSpotProperty> Supply(AlignmentFileBeanModel file, CancellationToken token) {
            AlignmentResultContainer container;
            if (_currentResult.Value is null || file != _currentResult.Value.AlignmentFile) {
                container = file.LoadAlignmentResultAsync(token).Result;
            }
            else {
                container = _currentResult.Value.AlignmentResult;
            }
            var spots = container.AlignmentSpotProperties;
            var flatten = spots.SelectMany(s => s.IsMultiLayeredData() ? s.AlignmentDriftSpotFeatures : [s]).ToList();
            if (UseFilter) {
                using var disposable = new CompositeDisposable();
                return _filter.Filter(flatten.Select(spot => new AlignmentSpotPropertyModel(spot).AddTo(disposable))).Select(m => m.innerModel).ToList();
            }
            return flatten;
        }
    }
}
