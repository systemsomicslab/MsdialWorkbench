using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class AdductIonSettingViewModel : ViewModelBase
    {
        public AdductIonSettingViewModel(AdductIonSettingModel model) {
            Model = model;

            AdductIons = Model.AdductIons.ToReadOnlyReactiveCollection(m => new AdductIonVM(m)).AddTo(Disposables);
            SelectedAdduct = new ReactivePropertySlim<AdductIonVM>().AddTo(Disposables);
            UserDefinedAdductName = Model.ToReactivePropertySlimAsSynchronized(m => m.UserDefinedAdductName).AddTo(Disposables);
            UserDefinedAdduct = UserDefinedAdductName.Throttle(TimeSpan.FromMilliseconds(100)).Select(_ => Model.UserDefinedAdduct).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            AddCommand = UserDefinedAdduct
                .Select(vm => vm?.FormatCheck ?? false)
                .ToReactiveCommand()
                .WithSubscribe(Model.AddAdductIon)
                .AddTo(Disposables);

            RemoveCommand = new ReactiveCommand().AddTo(Disposables);
            RemoveCommand.WithLatestFrom(SelectedAdduct)
                .Select(p => p.Second?.Model)
                .Where(m => m != null)
                .Subscribe(Model.RemoveAdductIon)
                .AddTo(Disposables);
        }

        public AdductIonSettingModel Model { get; }

        public ReadOnlyReactiveCollection<AdductIonVM> AdductIons { get; }

        public ReactivePropertySlim<AdductIonVM> SelectedAdduct { get; }

        public ReactivePropertySlim<string> UserDefinedAdductName { get; }

        public ReadOnlyReactivePropertySlim<AdductIon> UserDefinedAdduct { get; }

        public ReactiveCommand AddCommand { get; }

        public ReactiveCommand RemoveCommand { get; }
    }
}
