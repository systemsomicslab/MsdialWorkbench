using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsAlignmentSpotTableViewModel : ViewModelBase
    {
        public ImmsAlignmentSpotTableViewModel(ImmsAlignmentSpotTableModel model) {
            this.model = model ?? throw new ArgumentNullException(nameof(model));

            Spots = this.model.Spots;
            SpotsView = CollectionViewSource.GetDefaultView(Spots);
            Target = this.model.Target;
            MassMin = this.model.MassMin;
            MassMax = this.model.MassMax;
            DriftMin = this.model.DriftMin;
            DriftMax = this.model.DriftMax;

            MassLower = new ReactiveProperty<double>(MassMin).AddTo(Disposables);
            MassUpper = new ReactiveProperty<double>(MassMax).AddTo(Disposables);

            DriftLower = new ReactiveProperty<double>(DriftMin).AddTo(Disposables);
            DriftUpper = new ReactiveProperty<double>(DriftMax).AddTo(Disposables);

            MassLower.SetValidateNotifyError(v => v < MassMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassUpper.Value ? "Too large" : null);
            MassUpper.SetValidateNotifyError(v => v < MassLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassMax ? "Too large" : null);
            DriftLower.SetValidateNotifyError(v => v < DriftMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftUpper.Value ? "Too large" : null);
            DriftUpper.SetValidateNotifyError(v => v < DriftLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftMax ? "Too large" : null);
        }

        private readonly ImmsAlignmentSpotTableModel model;

        public ObservableCollection<AlignmentSpotPropertyModel> Spots { get; }
        public ICollectionView SpotsView {
            get => spotsView;
            set => SetProperty(ref spotsView, value);
        }
        private ICollectionView spotsView;

        public IReactiveProperty<AlignmentSpotPropertyModel> Target { get; }

        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }

        public ReactivePropertySlim<string> CommentFilterKeyword { get; }

        public double MassMin { get; }

        public double MassMax { get; }

        public ReactiveProperty<double> MassLower { get; }

        public ReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }

        public double DriftMax { get; }

        public ReactiveProperty<double> DriftLower { get; }

        public ReactiveProperty<double> DriftUpper { get; }
    }
}
