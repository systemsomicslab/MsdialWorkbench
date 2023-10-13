using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
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
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }

            var ms2ReferenceSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
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
