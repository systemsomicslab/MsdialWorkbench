using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
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
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    public interface IMsSpectrumLoader<in T>
    {
        IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(T target);
    }

    public static class MsSpectrumLoaderExtension {
        public static IMsSpectrumLoader<U> Contramap<T, U>(this IMsSpectrumLoader<T> loader, Func<U, IObservable<T>> map) {
            return new ContramapImplLoader<T, U>(loader, map);
        }

        public static IMsSpectrumLoader<U> Contramap<T, U>(this IMsSpectrumLoader<T> loader, Func<U, T> map) {
            return new ContramapImplLoader<T, U>(loader, u => Observable.Return(map(u)));
        }

        class ContramapImplLoader<T, U> : IMsSpectrumLoader<U> {
            private readonly IMsSpectrumLoader<T> _loader;
            private readonly Func<U, IObservable<T>> _map;

            public ContramapImplLoader(IMsSpectrumLoader<T> loader, Func<U, IObservable<T>> map) {
                _loader = loader;
                _map = map;
            }

            public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(U target) {
                return _map(target).SelectSwitch(_loader.LoadSpectrumAsObservable);
            }
        }
    }

    internal sealed class MsRawSpectrumLoader : IMsSpectrumLoader<ChromatogramPeakFeatureModel>
    {
        public MsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            this.provider = provider;
            this.parameter = parameter;
        }

        private readonly IDataProvider provider;
        private readonly ParameterBase parameter;

        public Task<List<SpectrumPeak>> LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            return target is null
                ? Task.FromResult(new List<SpectrumPeak>())
                : LoadSpectrumCoreAsync(target, token);
        }

        private async Task<List<SpectrumPeak>> LoadSpectrumCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target.MS2RawSpectrumId < 0) {
                return new List<SpectrumPeak>(0);
            }
            var msSpectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var spectra = DataAccess.GetCentroidMassSpectra(
                msSpectra[target.MS2RawSpectrumId],
                parameter.MS2DataType,
                0f, float.MinValue, float.MaxValue);
            if (parameter.RemoveAfterPrecursor) {
                spectra = spectra.Where(peak => peak.Mass <= target.Mass + parameter.KeptIsotopeRange).ToList();
            }
            return spectra;
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(ChromatogramPeakFeatureModel target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }

    internal sealed class MultiMsRawSpectrumLoader : DisposableModelBase, IMsSpectrumLoader<ChromatogramPeakFeatureModel>
    {
        private readonly IDataProvider _provider;
        private readonly ParameterBase _parameter;
        private readonly Task<ReadOnlyCollection<RawSpectrum>> _msSpectra;

        public MultiMsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _msSpectra = _provider.LoadMsSpectrumsAsync(default);
            _ms2List = new Subject<List<MsSelectionItem>>().AddTo(Disposables);
            Ms2IdSelector = new ReactivePropertySlim<MsSelectionItem>().AddTo(Disposables);
        }

        public ReactivePropertySlim<MsSelectionItem> Ms2IdSelector { get; }

        public IObservable<List<MsSelectionItem>> Ms2List => _ms2List;
        private readonly Subject<List<MsSelectionItem>> _ms2List;

        private IObservable<List<SpectrumPeak>> LoadSpectrumAsObservableCore(ChromatogramPeakFeatureModel target) {
            if (target.InnerModel.MS2RawSpectrumID2CE.Count == 0) {
                _ms2List.OnNext(new List<MsSelectionItem>(0));
                Ms2IdSelector.Value = null;
                return Observable.Return(new List<SpectrumPeak>(0));
            }
            var items = target.InnerModel.MS2RawSpectrumID2CE.Select(pair => new MsSelectionItem(pair.Key, pair.Value)).ToList();
            _ms2List.OnNext(items);
            var defaultValue = items.FirstOrDefault(item => item.Id == target.MS2RawSpectrumId) ?? items.First();
            Ms2IdSelector.Value = defaultValue;

            return Observable.FromAsync(() => _msSpectra).CombineLatest(Ms2IdSelector.Where(item => !(item is null)).Select(item => item.Id), (msSpectra, ms2Id) =>
            {
                var spectra = DataAccess.GetCentroidMassSpectra(msSpectra[ms2Id], _parameter.MS2DataType, 0f, float.MinValue, float.MaxValue);
                if (_parameter.RemoveAfterPrecursor) {
                    spectra = spectra.Where(spectrum => spectrum.Mass <= target.Mass + _parameter.KeptIsotopeRange).ToList();
                }
                return spectra;
            });
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(ChromatogramPeakFeatureModel target) {
            return target is null
                ? Observable.Return(new List<SpectrumPeak>(0))
                : LoadSpectrumAsObservableCore(target);
        }
    }

    internal sealed class MsDecSpectrumFromFileLoader : IMsSpectrumLoader<AlignmentSpotPropertyModel>
    {
        private readonly AnalysisFileBeanModel _file;

        public MsDecSpectrumFromFileLoader(AnalysisFileBeanModel file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        IObservable<List<SpectrumPeak>> IMsSpectrumLoader<AlignmentSpotPropertyModel>.LoadSpectrumAsObservable(AlignmentSpotPropertyModel target) {
            if (target is null) {
                return Observable.Return(new List<SpectrumPeak>(0));
            }
            IObservable<ReadOnlyCollection<AlignmentChromPeakFeatureModel>> props = target.AlignedPeakPropertiesModelProperty;
            var task = target.AlignedPeakPropertiesModelProperty.ToTask();
            if (target.AlignedPeakPropertiesModelProperty.Value is null) {
                props = Observable.FromAsync(() => task);
            }
            return props.Select(props_ => {
                var prop = props_.FirstOrDefault(p => p.FileID == _file.AnalysisFileId);
                if (prop is null || prop.MasterPeakID < 0) {
                    return new List<SpectrumPeak>(0);
                }

                return _file.MSDecLoader.LoadMSDecResult(prop.MasterPeakID).Spectrum;
            });
        }
    }

    internal sealed class MsDecSpectrumLoader : IMsSpectrumLoader<object>
    {
        public MsDecSpectrumLoader(
            MSDecLoader loader,
            IReadOnlyList<object> ms1Peaks) {

            this.ms1Peaks = ms1Peaks;
            this.loader = loader;
        }

        public MSDecResult Result { get; private set; }

        private readonly MSDecLoader loader;
        private readonly IReadOnlyList<object> ms1Peaks;

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(object target, CancellationToken token) {
            var ms2DecSpectrum = new List<SpectrumPeak>();
            if (target != null) {
                ms2DecSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            return ms2DecSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(object target) {
            if (target is ChromatogramPeakFeatureModel cpeak) {
                var idx = cpeak.MSDecResultIDUsedForAnnotation;
                var msdecResult = loader.LoadMSDecResult(idx);
                Result = msdecResult;
                return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
            }
            else if (target is AlignmentSpotPropertyModel spot) {
                // var peak = (AlignmentSpotPropertyModel)ms1Peaks[idx];
                //idx = peak.MSDecResultIDUsedForAnnotation;
                var idx = spot.MasterAlignmentID;
                var msdecResult = loader.LoadMSDecResult(idx);
                Result = msdecResult;
                return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
            }
            else {
                var idx = ms1Peaks.IndexOf(target);
                var msdecResult = loader.LoadMSDecResult(idx);
                Result = msdecResult;
                return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
            }
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(object target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }

    internal sealed class MsRefSpectrumLoader : IMsSpectrumLoader<IAnnotatedObject>
    {
        public MsRefSpectrumLoader(DataBaseMapper mapper) {
            this.mapper = mapper;
        }

        //public MsRefSpectrumLoader(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
        //    this.moleculeRefer = refer;
        //}

        //public MsRefSpectrumLoader(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
        //    this.peptideRefer = refer;
        //}

        //private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> moleculeRefer;
        //private readonly IMatchResultRefer<PeptideMsReference, MsScanMatchResult> peptideRefer;
        private readonly DataBaseMapper mapper;

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(IAnnotatedObject target, CancellationToken token) {
            var ms2ReferenceSpectrum = new List<SpectrumPeak>();

            if (target != null) {
                ms2ReferenceSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            return ms2ReferenceSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(IAnnotatedObject target) {
            var representative = target.MatchResults.Representative;
            if (representative.Source == SourceType.FastaDB) {
                //var reference = peptideRefer.Refer(representative);
                var reference = mapper.PeptideMsRefer(representative);
                if (reference != null) {
                    return reference.Spectrum;
                }
            }
            else {
                var reference = mapper.MoleculeMsRefer(representative);
                if (reference != null) {
                    return reference.Spectrum;
                }
            }

            return new List<SpectrumPeak>();
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(IAnnotatedObject target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }
}
