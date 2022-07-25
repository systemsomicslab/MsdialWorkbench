using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;


namespace CompMs.App.Msdial.ViewModel.Table
{
    //internal class ProteinGroupTableViewModel : MethodViewModel {
    internal class ProteinGroupTableViewModel : ViewModelBase {
        private readonly IObservable<ProteinResultContainerModel> _model;

        public ProteinGroupTableViewModel(IObservable<ProteinResultContainerModel> model)
        {
            _model = model;
        }

        public ReadOnlyObservableCollection<ProteinGroupViewModel> Groups { get; }

    }
}
