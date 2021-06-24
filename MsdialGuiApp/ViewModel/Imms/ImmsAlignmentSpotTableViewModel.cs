using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsAlignmentSpotTableViewModel : ViewModelBase
    {
        public ImmsAlignmentSpotTableViewModel(
            ImmsAlignmentSpotTableModel model,
            IReactiveProperty<double> massLower, IReactiveProperty<double> massUpper,
            IReactiveProperty<double> driftLower, IReactiveProperty<double> driftUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword) {
            this.model = model ?? throw new ArgumentNullException(nameof(model));

            Spots = this.model.Spots;
            SpotsView = CollectionViewSource.GetDefaultView(Spots);
            Target = this.model.Target;

            MassMin = this.model.MassMin;
            MassMax = this.model.MassMax;
            DriftMin = this.model.DriftMin;
            DriftMax = this.model.DriftMax;

            MassLower = massLower ?? throw new ArgumentNullException(nameof(massLower));
            MassUpper = massUpper ?? throw new ArgumentNullException(nameof(massUpper));
            DriftLower = driftLower ?? throw new ArgumentNullException(nameof(driftLower));
            DriftUpper = driftUpper ?? throw new ArgumentNullException(nameof(driftUpper));
            MetaboliteFilterKeyword = metaboliteFilterKeyword ?? throw new ArgumentNullException(nameof(metaboliteFilterKeyword));
            CommentFilterKeyword = commentFilterKeyword ?? throw new ArgumentNullException(nameof(commentFilterKeyword));
        }

        private readonly ImmsAlignmentSpotTableModel model;

        public ObservableCollection<AlignmentSpotPropertyModel> Spots { get; }
        public ICollectionView SpotsView {
            get => spotsView;
            set => SetProperty(ref spotsView, value);
        }
        private ICollectionView spotsView;

        public IReactiveProperty<AlignmentSpotPropertyModel> Target { get; }

        public IReactiveProperty<string> MetaboliteFilterKeyword { get; }

        public IReactiveProperty<string> CommentFilterKeyword { get; }

        public double MassMin { get; }

        public double MassMax { get; }

        public IReactiveProperty<double> MassLower { get; }

        public IReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }

        public double DriftMax { get; }

        public IReactiveProperty<double> DriftLower { get; }

        public IReactiveProperty<double> DriftUpper { get; }
    }
}
