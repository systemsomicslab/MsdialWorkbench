using CompMs.App.Msdial.Model.Chart;
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
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search {
    internal sealed class InternalMsFinder : DisposableModelBase {
        public InternalMsFinder(List<MsfinderQueryFile> msfinderQueryFiles, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> structureQueries) {
            var metabolites = LoadMetabolites(msfinderQueryFiles, parameter, structureQueries);
            _observedMetabolites = new ObservableCollection<MsfinderObservedMetabolite>(metabolites);
            ObservedMetabolites = new ReadOnlyObservableCollection<MsfinderObservedMetabolite>(_observedMetabolites);
            _selectedObservedMetabolite = ObservedMetabolites.FirstOrDefault();

            var ms1 = this.ObserveProperty(m => m.SelectedObservedMetabolite).Select(m => m?._ms1SpectrumSubject.Value);
            var internalMsFinderMs1 = new ObservableMsSpectrum(ms1, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            var ms1HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
            var ms1VerticalAxis = internalMsFinderMs1.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

            var ms2 = this.ObserveProperty(m => m.SelectedObservedMetabolite).Select(m => m?._ms2SpectrumSubject.Value);
            var internalMsFinderMs2 = new ObservableMsSpectrum(ms2, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            var ms2HorizontalAxis = internalMsFinderMs2.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
            var ms2VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

            var structureMs2 = new ObservableMsSpectrum(ms2, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            var ms2RefRange = _selectedObservedMetabolite._spotData.Ms2Spectrum.IsEmptyOrNull()
                ? null
                : new AxisRange(_selectedObservedMetabolite._spotData.Ms2Spectrum.Min(p => p.Mass), _selectedObservedMetabolite._spotData.Ms2Spectrum.Max(p => p.Mass));
            var ms2VerticalAxis2 = structureMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

            var refMs = this.ObserveProperty(m => m.SelectedObservedMetabolite).Select(m => m?._refSpectrum.Value);
            var structureRef = new ObservableMsSpectrum(refMs, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
            var refVerticalAxis = structureRef.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
            var horizontalAxis = _selectedObservedMetabolite._spectrumRange.Select(range => AxisRange.Union(range, ms2RefRange) ?? new AxisRange(0d, 1d)).ToReactiveContinuousAxisManager<double>(new ConstantMargin(40d)).AddTo(Disposables);
            var itemSelector = new AxisItemSelector<double>(new AxisItemModel<double>("m/z", horizontalAxis, "m/z")).AddTo(Disposables);
            var propertySelectors = new AxisPropertySelectors<double>(itemSelector);
            propertySelectors.Register(new PropertySelector<SpectrumPeak, double>(p => p.Mass));
            var refMs2HorizontalAxis = propertySelectors;

            var msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            SpectrumModelMs1 = new SingleSpectrumModel(internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
            SpectrumModelMs2 = new SingleSpectrumModel(internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);

            var ms2SpectrumModel = new SingleSpectrumModel(structureMs2, refMs2HorizontalAxis, ms2VerticalAxis2, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Blue)), msGraphLabels);
            var refSpectrumModel = new SingleSpectrumModel(structureRef, refMs2HorizontalAxis, refVerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Red)), msGraphLabels);
            RefMs2SpectrumModel = new MsSpectrumModel(ms2SpectrumModel, refSpectrumModel, Observable.Return<Ms2ScanMatching?>(null)).AddTo(Disposables);
        }

        public SingleSpectrumModel SpectrumModelMs1 { get; }
        public SingleSpectrumModel SpectrumModelMs2 { get; }
        public MsSpectrumModel RefMs2SpectrumModel { get; }

        private List<MsfinderObservedMetabolite> LoadMetabolites(List<MsfinderQueryFile> msfinderQueryFiles, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> queries) {
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