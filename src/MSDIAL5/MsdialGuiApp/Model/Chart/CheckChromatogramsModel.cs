using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class CheckChromatogramsModel : BindableBase
    {
        private readonly AccumulateSpectraUsecase _accumulateSpectra;
        private readonly ICompoundSearchUsecase<ICompoundResult, PeakSpotModel>? _compoundSearch;
        private readonly AdvancedProcessOptionBaseParameter _advancedProcessParameter;
        private readonly List<PeakFeatureSearchValue> _displaySettingValueCandidates;
        private readonly ObservableCollection<PeakFeatureSearchValueModel> _displaySettingValues;

        public CheckChromatogramsModel(LoadChromatogramsUsecase loadingChromatograms, AccumulateSpectraUsecase accumulateSpectra, ICompoundSearchUsecase<ICompoundResult, PeakSpotModel>? compoundSearch, AdvancedProcessOptionBaseParameter advancedProcessParameter) {
            LoadChromatogramsUsecase = loadingChromatograms ?? throw new ArgumentNullException(nameof(loadingChromatograms));
            _accumulateSpectra = accumulateSpectra;
            _compoundSearch = compoundSearch;
            _advancedProcessParameter = advancedProcessParameter;
            advancedProcessParameter.DiplayEicSettingValues ??= new List<PeakFeatureSearchValue>();
            var values = advancedProcessParameter.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
            values.AddRange(Enumerable.Repeat(0, 100).Select(_ => new PeakFeatureSearchValue()));
            _displaySettingValueCandidates = values;
            _displaySettingValues = new ObservableCollection<PeakFeatureSearchValueModel>(values.Select(v => new PeakFeatureSearchValueModel(v)));
            DisplayEicSettingValues = new ReadOnlyObservableCollection<PeakFeatureSearchValueModel>(_displaySettingValues);
        }

        public ChromatogramsModel? Chromatograms {
            get => _chromatograms;
            private set => SetProperty(ref _chromatograms, value);
        }
        private ChromatogramsModel? _chromatograms;

        public RangeSelectableChromatogramModel? RangeSelectableChromatogramModel {
            get => _rangeSelectableChromatogramModel;
            private set => SetProperty(ref _rangeSelectableChromatogramModel, value);
        }
        private RangeSelectableChromatogramModel? _rangeSelectableChromatogramModel;

        public AccumulatedMs2SpectrumModel[] AccumulatedMs2SpectrumModels {
            get => _accumulatedMs2SpectrumModels;
            private set {
                var prevs = _accumulatedMs2SpectrumModels;
                if (SetProperty(ref _accumulatedMs2SpectrumModels, value)) {
                    foreach (var prev in prevs) {
                        prev.Dispose();
                    }
                }
            }
        }
        private AccumulatedMs2SpectrumModel[] _accumulatedMs2SpectrumModels = Array.Empty<AccumulatedMs2SpectrumModel>();

        public ReadOnlyObservableCollection<PeakFeatureSearchValueModel> DisplayEicSettingValues { get; }

        public LoadChromatogramsUsecase LoadChromatogramsUsecase { get; }

        public Task ExportAsync(Stream stream, string separator) {
            return Chromatograms?.ExportAsync(stream, separator) ?? Task.CompletedTask;
        }

        public void Update() {
            foreach (var m in DisplayEicSettingValues) {
                m.Commit();
            }
            _advancedProcessParameter.DiplayEicSettingValues.Clear();
            _advancedProcessParameter.DiplayEicSettingValues.AddRange(_displaySettingValueCandidates.Where(n => n.Mass > 0 && n.MassTolerance > 0));
            var displayEICs = _advancedProcessParameter.DiplayEicSettingValues;
            Chromatograms = LoadChromatogramsUsecase.Load(displayEICs);

            if (_compoundSearch is not null) {
                RangeSelectableChromatogramModel = new RangeSelectableChromatogramModel(Chromatograms);
                AccumulatedMs2SpectrumModels = Chromatograms.DisplayChromatograms
                    .OfType<DisplayExtractedIonChromatogram>()
                    .Select(c => new AccumulatedMs2SpectrumModel(c, _accumulateSpectra, _compoundSearch))
                    .ToArray();
            }
        }

        public async Task AccumulateAsync(AccumulatedMs2SpectrumModel model, CancellationToken token) {
            if (RangeSelectableChromatogramModel is { MainRange: not null } ) {
                var range = RangeSelectableChromatogramModel.ConvertToRt(RangeSelectableChromatogramModel.MainRange);
                var subs = RangeSelectableChromatogramModel.SubtractRanges.Select(r => RangeSelectableChromatogramModel.ConvertToRt(r)).ToArray();
                await model.CalculateMs2Async(range, subs, token).ConfigureAwait(false);
            }
        }

        public void Clear() {
            foreach (var m in DisplayEicSettingValues) {
                m.ClearChromatogramSearchQuery();
            }
        }
    }
}
