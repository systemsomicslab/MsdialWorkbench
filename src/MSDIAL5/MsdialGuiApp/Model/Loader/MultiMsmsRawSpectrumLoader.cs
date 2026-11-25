using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MultiMsmsRawSpectrumLoader : DisposableModelBase, IMsSpectrumLoader<ChromatogramPeakFeatureModel>
    {
        private readonly IDataProvider _provider;
        private readonly ParameterBase _parameter;
        private readonly Task<ReadOnlyCollection<RawSpectrum>> _msSpectra;

        public MultiMsmsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _msSpectra = _provider.LoadMsSpectrumsAsync(default);
            _ms2List = new Subject<List<MsSelectionItem>>().AddTo(Disposables);
            Ms2IdSelector = new ReactivePropertySlim<MsSelectionItem?>().AddTo(Disposables);
        }

        public ReactivePropertySlim<MsSelectionItem?> Ms2IdSelector { get; }

        public IObservable<List<MsSelectionItem>> Ms2List => _ms2List;
        private readonly Subject<List<MsSelectionItem>> _ms2List;

        private IObservable<IMSScanProperty?> LoadScanAsObservableCore(ChromatogramPeakFeatureModel target) {
            if (target.InnerModel.MS2RawSpectrumID2CE.Count == 0) {
                _ms2List.OnNext(new List<MsSelectionItem>(0));
                Ms2IdSelector.Value = null;
                return Observable.Return<IMSScanProperty?>(null);
            }
            var items = target.InnerModel.MS2RawSpectrumID2CE.Select(pair => new MsSelectionItem(pair.Key, pair.Value)).ToList();
            _ms2List.OnNext(items);
            var defaultValue = items.FirstOrDefault(item => item.Id == target.MS2RawSpectrumId) ?? items.First();
            Ms2IdSelector.Value = defaultValue;

            return Observable.FromAsync(() => _msSpectra).CombineLatest(Ms2IdSelector.Where(item => item is not null).Select(item => item!.Id), (msSpectra, ms2Id) =>
            {
                // No such scan, as when the raw data file is missing: the scan list above still comes from the peak list.
                if (ms2Id < 0 || ms2Id >= msSpectra.Count) {
                    return (IMSScanProperty?)null;
                }
                return (IMSScanProperty?)new MSScanProperty(target.MS2RawSpectrumId, target.Mass, target.InnerModel.ChromXs.GetRepresentativeXAxis(), target.InnerModel.IonMode)
                {
                    Spectrum = GetSpectrum(msSpectra[ms2Id], target),
                };
            });
        }

        private List<SpectrumPeak> GetSpectrum(RawSpectrum rawSpectrum, ChromatogramPeakFeatureModel target) {
            var spectra = DataAccess.GetCentroidMassSpectra(rawSpectrum, _parameter.MS2DataType, 0f, float.MinValue, float.MaxValue);
            if (_parameter.RemoveAfterPrecursor) {
                spectra = spectra.Where(spectrum => spectrum.Mass <= target.Mass + _parameter.KeptIsotopeRange).ToList();
            }
            return spectra;
        }

        /// <summary>
        /// Every MS2 scan acquired over the peak, in scan order, as drawn in the MS2 spectrum list.
        /// A scan the raw data does not have, as when the raw data file is missing, is listed with no spectrum.
        /// </summary>
        public async Task<IReadOnlyList<Ms2SpectrumListItem>> LoadMs2SpectrumListAsync(ChromatogramPeakFeatureModel target) {
            if (target is null || target.InnerModel.MS2RawSpectrumID2CE.Count == 0) {
                return [];
            }
            var msSpectra = await _msSpectra.ConfigureAwait(false);
            var scans = target.InnerModel.MS2RawSpectrumID2CE
                .OrderBy(pair => pair.Key)
                .Select(pair => {
                    if (pair.Key < 0 || pair.Key >= msSpectra.Count) {
                        return new PeakMs2Scan(pair.Key, pair.Value, null, null, null);
                    }
                    var raw = msSpectra[pair.Key];
                    return new PeakMs2Scan(pair.Key, pair.Value, raw.ScanStartTime, raw.Precursor?.SelectedIonMz, GetSpectrum(raw, target));
                })
                .ToList();
            return Ms2SpectrumList.FromPeakScans(scans, target.MS2RawSpectrumId);
        }

        public IObservable<IMSScanProperty?> LoadScanAsObservable(ChromatogramPeakFeatureModel target) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            return LoadScanAsObservableCore(target);
        }
    }
}
