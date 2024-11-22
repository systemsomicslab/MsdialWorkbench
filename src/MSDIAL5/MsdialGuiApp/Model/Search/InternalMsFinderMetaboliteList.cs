using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Parameter;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsFinderMetaboliteList : DisposableModelBase {
        public List<ExistStructureQuery> _structureQueries;

        public InternalMsFinderMetaboliteList(List<MsfinderQueryFile> msfinderQueryFiles, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> structureQueries) {
            _structureQueries = structureQueries;
            var metabolites = LoadMetabolites(msfinderQueryFiles, parameter, structureQueries);
            _observedMetabolites = new ObservableCollection<MsfinderObservedMetabolite>(metabolites);
            ObservedMetabolites = new ReadOnlyObservableCollection<MsfinderObservedMetabolite>(_observedMetabolites);
            _selectedObservedMetabolite = ObservedMetabolites.FirstOrDefault();
            var ms1 = this.ObserveProperty(m => m.SelectedObservedMetabolite).Select(m => m?.Ms1Spectrum);
            var ms2 = this.ObserveProperty(m => m.SelectedObservedMetabolite).Select(m => m?.Ms2Spectrum);
            InternalMsFinderMs1 = new ObservableMsSpectrum(ms1, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            InternalMsFinderMs2 = new ObservableMsSpectrum(ms2, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
        }

        public ObservableMsSpectrum InternalMsFinderMs1 { get; }
        public ObservableMsSpectrum InternalMsFinderMs2 { get; }

        private List<MsfinderObservedMetabolite> LoadMetabolites(List<MsfinderQueryFile>msfinderQueryFiles, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery>queries) {
            var metaboliteList = new List<MsfinderObservedMetabolite>();
            foreach (var queryFile in msfinderQueryFiles) {
                var metabolite = new MsfinderObservedMetabolite(queryFile, parameter, queries);
                metaboliteList.Add(metabolite);
            }
            return metaboliteList;
        }

        public ReadOnlyObservableCollection<MsfinderObservedMetabolite> ObservedMetabolites { get; }
        private readonly ObservableCollection<MsfinderObservedMetabolite> _observedMetabolites;

        public MsfinderObservedMetabolite? SelectedObservedMetabolite {
            get => _selectedObservedMetabolite; 
            set => SetProperty(ref _selectedObservedMetabolite, value);
        }
        private MsfinderObservedMetabolite? _selectedObservedMetabolite;
    }
}
