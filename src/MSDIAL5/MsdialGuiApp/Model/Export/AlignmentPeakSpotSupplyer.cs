using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentPeakSpotSupplyer : BindableBase
    {
        private readonly PeakFilterModel _peakFilterModel;
        private readonly IMatchResultEvaluator<IFilterable> _evaluator;

        public AlignmentPeakSpotSupplyer(PeakFilterModel peakFilterModel, IMatchResultEvaluator<IFilterable> evaluator) {
            _peakFilterModel = peakFilterModel ?? throw new ArgumentNullException(nameof(peakFilterModel));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        public bool UseFilter {
            get => _useFilter;
            set => SetProperty(ref _useFilter, value);
        }
        private bool _useFilter;

        public IReadOnlyList<AlignmentSpotProperty> Supply(AlignmentFileBean file, CancellationToken token) {
            var container = AlignmentResultContainer.LoadLazy(file);
            if (UseFilter) {
                return container.AlignmentSpotProperties.Where(spot => _peakFilterModel.PeakFilter(new AlignmentSpotPropertyModel(spot), _evaluator)).ToList();
            }
            return container.AlignmentSpotProperties;
        }
    }
}
