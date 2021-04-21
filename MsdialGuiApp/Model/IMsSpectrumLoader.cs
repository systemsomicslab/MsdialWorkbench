using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public interface IMsSpectrumLoader
    {
        Task<List<SpectrumPeak>> LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token);
        List<SpectrumPeak> LoadSpectrum(ChromatogramPeakFeatureModel target);
    }

    class MsRawSpectrumLoader : IMsSpectrumLoader
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

    class MsDecSpectrumLoader : IMsSpectrumLoader
    {
        public MsDecSpectrumLoader(
            string deconvolutionFile,
            IList<ChromatogramPeakFeatureModel> ms1Peaks) {

            this.ms1Peaks = ms1Peaks;
            deconvolutionStream = File.OpenRead(deconvolutionFile);
            MsdecResultsReader.GetSeekPointers(deconvolutionStream, out version, out seekPointers, out isAnnotationInfoIncluded);
        }

        private readonly Stream deconvolutionStream;
        private readonly IList<ChromatogramPeakFeatureModel> ms1Peaks;
        private readonly int version;
        private readonly List<long> seekPointers;
        private readonly bool isAnnotationInfoIncluded;

        public List<SpectrumPeak> LoadSpectrum(ChromatogramPeakFeatureModel target) {
            if (target == null) {
                return new List<SpectrumPeak>(0);
            }
            return LoadSpectrumCore(target);
        }

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms2DecSpectrum = new List<SpectrumPeak>();
            if (target != null) {
                ms2DecSpectrum = await Task.Run(() => LoadSpectrumCore(target) , token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            return ms2DecSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(ChromatogramPeakFeatureModel target) {
            var idx = ms1Peaks.IndexOf(target);
            var msdecResult = MsdecResultsReader.ReadMSDecResult(deconvolutionStream, seekPointers[idx], version, isAnnotationInfoIncluded);
            return msdecResult.Spectrum;
        }
    }

    class MsRefSpectrumLoader : IMsSpectrumLoader
    {
        public MsRefSpectrumLoader(IMatchResultRefer refer) {
            this.refer = refer;
        }

        private readonly IMatchResultRefer refer;

        public List<SpectrumPeak> LoadSpectrum(ChromatogramPeakFeatureModel target) {
            if (target == null) {
                return new List<SpectrumPeak>();
            }
            return LoadSpectrumCore(target);
        }

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var ms2ReferenceSpectrum = new List<SpectrumPeak>();

            if (target != null) {
                ms2ReferenceSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            return ms2ReferenceSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(ChromatogramPeakFeatureModel target) {
            var representative = target.InnerModel.MatchResults.Representative;
            var reference = refer.Refer(representative);
            if (reference != null) {
                return reference.Spectrum;
            }
            return new List<SpectrumPeak>();
        }
    }
}
