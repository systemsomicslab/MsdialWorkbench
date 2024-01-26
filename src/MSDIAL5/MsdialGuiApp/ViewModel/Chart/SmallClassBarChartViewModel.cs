using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class SmallClassBarChartViewModel : ViewModelBase
    {
        public SmallClassBarChartViewModel(SpotBarItemCollection collection, FileClassPropertiesModel? fileClasses = null) {
            Collection = collection;
            var collectionChanged = Collection
                .CollectionChangedAsObservable()
                .ToUnit()
                .StartWith(Unit.Default)
                .Select(_ => Collection);
            if (fileClasses is null) {
                HorizontalAxis = collectionChanged
                    .Select(c => c.Select(item => item.Class).ToArray())
                    .ToReactiveCategoryAxisManager()
                    .AddTo(Disposables);
            }
            else {
                var sets = collectionChanged.Select(c => c.Select(item => item.Class).ToHashSet());
                HorizontalAxis = fileClasses.OrderedClasses
                    .CombineLatest(sets, (clss, s) => clss.Where(cls => s.Contains(cls)).ToArray())
                    .ToReactiveCategoryAxisManager().AddTo(Disposables);
            }
            VerticalAxis = collectionChanged
                .Where(c => c.Count > 0)
                .Select(c => (0d, c.Max(item => item.Height)))
                .ToReactiveContinuousAxisManager()
                .AddTo(Disposables);
        }

        public SpotBarItemCollection Collection { get; }

        public IAxisManager<string> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }
    }
}
