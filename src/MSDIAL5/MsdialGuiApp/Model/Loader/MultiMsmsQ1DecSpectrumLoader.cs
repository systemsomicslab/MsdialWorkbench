using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Raw.Abstractions;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader;

internal sealed class MultiMsmsQ1DecSpectrumLoader : DisposableModelBase, IMultiMsmsSpectrumLoader<ChromatogramPeakFeatureModel>
{
    private readonly IDataProvider _provider;
    private readonly Subject<List<MsSelectionItem>> _ms2List;

    public MultiMsmsQ1DecSpectrumLoader(IDataProvider provider) {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _ms2List = new Subject<List<MsSelectionItem>>().AddTo(Disposables);
        Ms2IdSelector = new ReactivePropertySlim<MsSelectionItem?>().AddTo(Disposables);
    }

    public ReactivePropertySlim<MsSelectionItem?> Ms2IdSelector { get; }

    public IObservable<List<MsSelectionItem>> Ms2List => _ms2List;

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
                    return Observable.Return<IMSScanProperty?>(null);
                }

                var query = new SpectraLoadingQuery
                {
                    MSLevel = spectrum.MsLevel,
                    ExperimentID = spectrum.ExperimentID,
                    ScanTimeRange = new() { Start = spectrum.ScanStartTime, End = spectrum.ScanStartTime },
                    CollisionEnergy = spectrum.Precursor?.CollisionEnergy,
                    EnableQ1Deconvolution = true,
                };
                if (spectrum.DriftTime > 0) {
                    query.DriftTimeRange = new() { Start = spectrum.DriftTime, End = spectrum.DriftTime };
                }

                var deced = Observable.FromAsync(token => _provider.LoadMSSpectraAsync(query, token));

                return deced.Select(s => s?.FirstOrDefault()).DefaultIfNull(s => new MSScanProperty(target.MS2RawSpectrumId, target.Mass, target.InnerModel.ChromXs.GetRepresentativeXAxis(), target.InnerModel.IonMode)
                {
                    Spectrum = s.Spectrum.Select(p => new SpectrumPeak { Mass = p.Mz, Intensity = p.Intensity }).ToList(),
                });
            }).Switch();
        }).Switch();
    }

    public IObservable<IMSScanProperty?> LoadScanAsObservable(ChromatogramPeakFeatureModel target) {
        if (target is null) {
            throw new ArgumentNullException(nameof(target));
        }
        return LoadScanAsObservableCore(target);
    }
}
