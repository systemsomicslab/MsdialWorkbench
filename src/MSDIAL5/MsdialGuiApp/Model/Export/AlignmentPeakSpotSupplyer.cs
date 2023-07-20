using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
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
        private readonly PeakFilterModel _peakFilterModel;
        private readonly IMatchResultEvaluator<AlignmentSpotPropertyModel> _evaluator;
        private readonly IReadOnlyReactiveProperty<IAlignmentModel> _currentResult;

        public AlignmentPeakSpotSupplyer(PeakFilterModel peakFilterModel, IMatchResultEvaluator<AlignmentSpotPropertyModel> evaluator, IReadOnlyReactiveProperty<IAlignmentModel> currentResult) {
            _peakFilterModel = peakFilterModel ?? throw new ArgumentNullException(nameof(peakFilterModel));
            _currentResult = currentResult;
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));;
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
            if (UseFilter) {
                using (var disposable = new CompositeDisposable()) {
                    return container.AlignmentSpotProperties.Where(spot => _peakFilterModel.PeakFilter(new AlignmentSpotPropertyModel(spot).AddTo(disposable), _evaluator)).ToList();
                } 
            }
            return container.AlignmentSpotProperties;
        }
    }
}
