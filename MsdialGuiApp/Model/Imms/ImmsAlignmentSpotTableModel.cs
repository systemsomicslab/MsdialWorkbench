using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsAlignmentSpotTableModel : BindableBase
    {
        public ImmsAlignmentSpotTableModel(
            ObservableCollection<AlignmentSpotPropertyModel> spots,
            IReactiveProperty<AlignmentSpotPropertyModel> target) {
            Spots = spots ?? throw new ArgumentNullException(nameof(spots));
            Target = target ?? throw new ArgumentNullException(nameof(target));

            MassMin = Spots.DefaultIfEmpty().Min(v => v?.MassCenter) ?? 0d;
            MassMax = Spots.DefaultIfEmpty().Max(v => v?.MassCenter) ?? 0d;

            DriftMin = Spots.DefaultIfEmpty().Min(v => v?.TimesCenter) ?? 0d;
            DriftMax = Spots.DefaultIfEmpty().Max(v => v?.TimesCenter) ?? 0d;
        }

        public ObservableCollection<AlignmentSpotPropertyModel> Spots { get; }

        public IReactiveProperty<AlignmentSpotPropertyModel> Target { get; }

        public double MassMin { get; }

        public double MassMax { get; }

        public double DriftMin { get; }

        public double DriftMax { get; }
    }
}
