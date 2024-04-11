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
                var spectra = DataAccess.GetCentroidMassSpectra(msSpectra[ms2Id], _parameter.MS2DataType, 0f, float.MinValue, float.MaxValue);
                if (_parameter.RemoveAfterPrecursor) {
                    spectra = spectra.Where(spectrum => spectrum.Mass <= target.Mass + _parameter.KeptIsotopeRange).ToList();
                }
                return new MSScanProperty(target.MS2RawSpectrumId, target.Mass, target.InnerModel.ChromXs.GetRepresentativeXAxis(), target.InnerModel.IonMode)
                {
                    Spectrum = spectra,
                };
            });
        }

        public IObservable<IMSScanProperty?> LoadScanAsObservable(ChromatogramPeakFeatureModel target) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            return LoadScanAsObservableCore(target);
        }
    }
}
