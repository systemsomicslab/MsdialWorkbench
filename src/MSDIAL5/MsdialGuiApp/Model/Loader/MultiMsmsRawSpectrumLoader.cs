using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.Raw.Abstractions;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
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

        public MultiMsmsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _ms2List = new Subject<List<MsSelectionItem>>().AddTo(Disposables);
            Ms2IdSelector = new ReactivePropertySlim<MsSelectionItem?>().AddTo(Disposables);
        }

        public ReactivePropertySlim<MsSelectionItem?> Ms2IdSelector { get; }

        public IObservable<List<MsSelectionItem>> Ms2List => _ms2List;
        private readonly Subject<List<MsSelectionItem>> _ms2List;

        private IObservable<IMSScanProperty?> LoadScanAsObservableCore(ChromatogramPeakFeatureModel target) {
            if (target.InnerModel.MS2RawSpectrumID2CE.Count == 0) {
                _ms2List.OnNext([]);
                Ms2IdSelector.Value = null;
                return Observable.Return<IMSScanProperty?>(null);
            }
            var items = target.InnerModel.MS2RawSpectrumID2CE.Select(pair => new MsSelectionItem(pair.Key, target.InnerModel.RawDataIDType, pair.Value)).ToList();
            _ms2List.OnNext(items);
            var defaultValue = items.FirstOrDefault(item => item.Id == target.MS2RawSpectrumId) ?? items.First();
            Ms2IdSelector.Value = defaultValue;

            return Ms2IdSelector.Where(item => item is not null).Select(item =>
            {
                var ms2Id = item!.Id;
                var spectrumOx = Observable.FromAsync(() => Task.Run(() => _provider.LoadSpectrumAsync((ulong)ms2Id, item.IDType)));

                return spectrumOx.Select(spectrum => {
                    if (spectrum is null) {
                        return null;
                    }

                    var spectra = DataAccess.GetCentroidMassSpectra(spectrum!, _parameter.MS2DataType, 0f, float.MinValue, float.MaxValue);
                    if (_parameter.RemoveAfterPrecursor) {
                        spectra = spectra.Where(spectrum => spectrum.Mass <= target.Mass + _parameter.KeptIsotopeRange).ToList();
                    }
                    return new MSScanProperty(target.MS2RawSpectrumId, target.Mass, target.InnerModel.ChromXs.GetRepresentativeXAxis(), target.InnerModel.IonMode)
                    {
                        Spectrum = spectra,
                    };
                });
            }).Switch();
        }

        public IObservable<IMSScanProperty?> LoadScanAsObservable(ChromatogramPeakFeatureModel target) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            return LoadScanAsObservableCore(target);
        }
    }
}
