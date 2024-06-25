using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsFinderMetaboliteList : BindableBase {
        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly AlignmentSpotPropertyModelCollection _spots;
        private readonly InternalMsfinderSettingModel _settingModel;

        public InternalMsFinderMetaboliteList(AlignmentFileBeanModel alignmentFile, AlignmentSpotPropertyModelCollection spots, InternalMsfinderSettingModel settingModel) {
            _alignmentFile = alignmentFile ?? throw new ArgumentNullException(nameof(alignmentFile));
            _spots = spots;
            _settingModel = settingModel;
            var metabolites = LoadMetabolites(alignmentFile, spots);
            _observedMetabolites = new ObservableCollection<MsfinderObservedMetabolite>(metabolites);
            ObservedMetabolites = new ReadOnlyObservableCollection<MsfinderObservedMetabolite>(_observedMetabolites);
            _selectedObservedMetabolite = ObservedMetabolites.FirstOrDefault();
        }

        private List<MsfinderObservedMetabolite> LoadMetabolites(AlignmentFileBeanModel alignmentFile, AlignmentSpotPropertyModelCollection spots) {
            var metaboliteList = new List<MsfinderObservedMetabolite>();
            foreach (var spot in spots.Items) {
                var metabolite = new MsfinderObservedMetabolite(spot, null);
                metaboliteList.Add(metabolite);
            }
            return metaboliteList;
        }

        public ReadOnlyObservableCollection<MsfinderObservedMetabolite> ObservedMetabolites { get; }
        private ObservableCollection<MsfinderObservedMetabolite> _observedMetabolites;

        public MsfinderObservedMetabolite? SelectedObservedMetabolite {
            get => _selectedObservedMetabolite; 
            set => _selectedObservedMetabolite = value;
        }
        private MsfinderObservedMetabolite? _selectedObservedMetabolite;
    }

    internal sealed class MsfinderObservedMetabolite {
        private readonly string _fgtpath;
        public string metaboliteName { get; set; }
        public int alignmentID { get; set; }
        public double retentionTime {  get; set; }
        public double centralCcs {  get; set; }
        public double mass {  get; set; }
        public string adduct {  get; set; }
        public Formula formula {  get; set; }
        public string ontology {  get; set; }
        public string smiles {  get; set; } 
        public string inchikey { get; set; }
        public string comment { get; set; }
        public IonMode ionMode { get; }

        public MsfinderObservedMetabolite(AlignmentSpotPropertyModel spot, string fgtpath) {
            _fgtpath = fgtpath;
            Spot = spot;
            metaboliteName = spot.Name;
            alignmentID = spot.AlignmentID;
            retentionTime = spot.RT;
            centralCcs = spot.CollisionCrossSection;
            mass = spot.Mass;
            adduct = spot.AdductIonName;
            formula = spot.Formula;
            ontology = spot.Ontology;
            smiles = spot.SMILES;
            inchikey = spot.InChIKey;
            comment = spot.Comment;
        }

        public AlignmentSpotPropertyModel Spot { get; private set; }
        public InternalMsfinderSettingModel SettingModel { get; }

        //public object formula { get; }

        public Task LoadAsync(CancellationToken token = default) {
            throw new NotImplementedException();
            //formula = read_fgt(fgtpath);
        }

        public Task ClearAsync(CancellationToken token = default) {
            throw new NotImplementedException();
            // formula = null;
        }

        public Task ReflectToMsdialAsync(CancellationToken token = default) {
            throw new NotImplementedException();
            //Spot.Formula = formula;
        }
    }
}
