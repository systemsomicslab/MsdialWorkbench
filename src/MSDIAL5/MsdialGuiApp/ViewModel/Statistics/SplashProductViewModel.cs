using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class SplashProductViewModel : ViewModelBase
    {
        public SplashProductViewModel(SplashProduct product) {
            Model = product;
            Lipids = Model.Lipids.ToReadOnlyReactiveCollection(m => new StandardCompoundViewModel(m)).AddTo(Disposables);
            SelectedLipid = product.ToReactivePropertySlimAsSynchronized(
                m => m.SelectedLipid,
                om => om.Select(m => Lipids.FirstOrDefault(lipid => lipid.Compound == m)),
                ovm => ovm.Select(vm => vm?.Compound))
                .AddTo(Disposables);
        }

        public SplashProduct Model { get; }

        public string Label => Model.Label;

        public ReadOnlyReactiveCollection<StandardCompoundViewModel> Lipids { get; }

        public ReactivePropertySlim<StandardCompoundViewModel> SelectedLipid { get; }

        public void Refresh() {
            foreach (var lipid in Lipids) {
                lipid.Refresh();
            }
        }
    }
}
