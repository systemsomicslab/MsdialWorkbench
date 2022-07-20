using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;


namespace CompMs.App.Msdial.ViewModel.Table
{
    //internal class ProteinGroupTableViewModel : MethodViewModel {
    internal class ProteinGroupTableViewModel {
        private readonly ProteinGroupModel model;
        private readonly IMessageBroker _broker;

        public int GroupId { get; }
        public ReadOnlyObservableCollection<ProteinGroupViewModel> Groups { get; }        

    }
}
