using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Information;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search
{
    internal class InternalMsFinderMetaboliteList : DisposableModelBase {
        public List<ExistStructureQuery> _structureQueries;

        public InternalMsFinderMetaboliteList(List<MsfinderQueryFile> msfinderQueryFiles, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> structureQueries) {
            _structureQueries = structureQueries;
            var metabolites = LoadMetabolites(msfinderQueryFiles, parameter, structureQueries);
            _observedMetabolites = new ObservableCollection<MsfinderObservedMetabolite>(metabolites);
            ObservedMetabolites = new ReadOnlyObservableCollection<MsfinderObservedMetabolite>(_observedMetabolites);
            _selectedObservedMetabolite = ObservedMetabolites.FirstOrDefault();

            InternalMsFinderMs1 = new ObservableMsSpectrum(_selectedObservedMetabolite._ms1SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            InternalMsFinderMs2 = new ObservableMsSpectrum(_selectedObservedMetabolite._ms2SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);

            StructureMs2 = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(_selectedObservedMetabolite._spotData.Ms2Spectrum)), null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            Ms2RefRange = _selectedObservedMetabolite._spotData.Ms2Spectrum.IsEmptyOrNull()
                ? null
                : new AxisRange(_selectedObservedMetabolite._spotData.Ms2Spectrum.Min(p => p.Mass), _selectedObservedMetabolite._spotData.Ms2Spectrum.Max(p => p.Mass));

            StructureRef = new ObservableMsSpectrum(_selectedObservedMetabolite._refSpectrum, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            StructureMs2RefRange = _selectedObservedMetabolite._spectrumRange;
            MoleculeStructureModel = _selectedObservedMetabolite.MoleculeStructureModel;
        }

        public ObservableMsSpectrum InternalMsFinderMs1 { get; }
        public ObservableMsSpectrum InternalMsFinderMs2 { get; }
        public ObservableMsSpectrum StructureMs2 { get; }
        public ObservableMsSpectrum StructureRef { get; }
        public AxisRange? Ms2RefRange { get; }
        public BehaviorSubject<AxisRange?> StructureMs2RefRange { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }

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
