using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class InternalMsFinderViewModel : ViewModelBase {
        private InternalMsFinder _model;
        private readonly IMessageBroker _broker;

        public InternalMsFinderViewModel(InternalMsFinder model, IMessageBroker broker) {
            _model = model;
            _broker = broker;

            MsfinderObservedMetabolites = model.MsfinderObservedMetabolites;
            MsfinderSelectedMetabolite = model.ToReactivePropertySlimAsSynchronized(m => m.MsfinderSelectedMetabolite).AddTo(Disposables);
            MetaboliteName = MsfinderSelectedMetabolite.Value.metaboliteName;
            AlignmentID = MsfinderSelectedMetabolite.Value.alignmentID;
            RetentionTime = MsfinderSelectedMetabolite.Value.retentionTime;
            CentralCcs = MsfinderSelectedMetabolite.Value.centralCcs;
            Mass = MsfinderSelectedMetabolite.Value.mass;
            Adduct = MsfinderSelectedMetabolite.Value.adduct;
            Formula = MsfinderSelectedMetabolite.Value.formula;
            Ontology = MsfinderSelectedMetabolite.Value.ontology;
            Smiles = MsfinderSelectedMetabolite.Value.smiles;
            InchiKey = MsfinderSelectedMetabolite.Value.inchikey;
            Comment = MsfinderSelectedMetabolite.Value.comment;
            IonMode = MsfinderSelectedMetabolite.Value.ionMode;

            LoadAsyncCommand = new DelegateCommand(LoadAsync);
        }

        public InternalMsFinderMetaboliteList InternalMsFinderMetaboliteList { get; set; }
        public ReadOnlyObservableCollection<MsfinderObservedMetabolite> MsfinderObservedMetabolites { get; }
        public ReactivePropertySlim<MsfinderObservedMetabolite?> MsfinderSelectedMetabolite { get; }
        public ObservableCollection<string> MetaboliteList { get; }

        public DelegateCommand LoadAsyncCommand { get; }
        public string MetaboliteName { get; set; }
        public int AlignmentID { get; set; }
        public double RetentionTime { get; set; }
        public double CentralCcs { get; set; }
        public double Mass { get; set; }
        public string Adduct {  get; set; }
        public Formula Formula { get; set; }
        public string Ontology { get; set; }
        public string Smiles { get; set; }
        public string InchiKey { get; set; }
        public string Comment { get; set; }
        public IonMode IonMode { get; set; }

        private void LoadAsync()  {
            Mouse.OverrideCursor = Cursors.Wait;

            Mouse.OverrideCursor = null;
        }
    }
}
