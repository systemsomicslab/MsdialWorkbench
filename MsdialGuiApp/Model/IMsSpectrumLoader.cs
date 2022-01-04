using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model {
    public interface IMsSpectrumLoader<in T>
    {
        Task<List<SpectrumPeak>> LoadSpectrumAsync(T target, CancellationToken token);
        List<SpectrumPeak> LoadSpectrum(T target);
    }

    class MsRawSpectrumLoader : IMsSpectrumLoader<ChromatogramPeakFeatureModel>
    {
        public MsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            this.provider = provider;
            this.parameter = parameter;
        }

        private readonly IDataProvider provider;
        private readonly ParameterBase parameter;

        public List<SpectrumPeak> LoadSpectrum(ChromatogramPeakFeatureModel target) {
            return target == null ? new List<SpectrumPeak>() : LoadSpectrumCore(target);
        }

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms2Spectrum = new List<SpectrumPeak>(); 

            if (target != null) {
                await Task.Run(() =>
                ms2Spectrum = LoadSpectrumCore(target),
                token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            return ms2Spectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(ChromatogramPeakFeatureModel target) {
            if (target.MS2RawSpectrumId < 0) {
                return new List<SpectrumPeak>(0);
            }
            var spectra = DataAccess.GetCentroidMassSpectra(
                provider.LoadMsSpectrums()[target.MS2RawSpectrumId],
                parameter.MS2DataType,
                0f, float.MinValue, float.MaxValue);
            if (parameter.RemoveAfterPrecursor) {
                spectra = spectra.Where(peak => peak.Mass <= target.Mass + parameter.KeptIsotopeRange).ToList();
            }
            return spectra;
        }
    }

    class MsDecSpectrumLoader : IMsSpectrumLoader<object>
    {
        public MsDecSpectrumLoader(
            MSDecLoader loader,
            IReadOnlyList<object> ms1Peaks) {

            this.ms1Peaks = ms1Peaks;
            this.loader = loader;
        }

        public MsdialCore.MSDec.MSDecResult Result { get; private set; }

        private readonly MSDecLoader loader;
        private readonly IReadOnlyList<object> ms1Peaks;

        public List<SpectrumPeak> LoadSpectrum(object target) {
            if (target == null) {
                return new List<SpectrumPeak>(0);
            }
            return LoadSpectrumCore(target);
        }

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(object target, CancellationToken token) {
            var ms2DecSpectrum = new List<SpectrumPeak>();
            if (target != null) {
                ms2DecSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            return ms2DecSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(object target) {
            var idx = ms1Peaks.IndexOf(target);
            var msdecResult = loader.LoadMSDecResult(idx);
            Result = msdecResult;
            return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
        }
    }

    class MsRefSpectrumLoader : IMsSpectrumLoader<IAnnotatedObject>
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

        

        public List<SpectrumPeak> LoadSpectrum(IAnnotatedObject target) {
            if (target == null) {
                return new List<SpectrumPeak>();
            }
            return LoadSpectrumCore(target);
        }

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
    }
}
