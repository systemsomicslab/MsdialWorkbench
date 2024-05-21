using CompMs.App.Msdial.Model.MsResult;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart
{
    internal class ExperimentSpectrumModel : BindableBase {
        private readonly IDataProvider provider;

        public ExperimentSpectrumModel(
            RangeSelectableChromatogramModel model,
            IFileBean analysisFile,
            IDataProvider provider,
            ChromatogramPeakFeature peak,
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
            ParameterBase parameter) {

            RangeSelectableChromatogramModel = model;
            AnalysisFile = analysisFile;
            this.provider = provider;
            Ms2Spectrums = new ObservableCollection<SummarizedSpectrumModel>();
            Peak = peak;
            Refer = refer;
            Parameter = parameter;
        }

        public RangeSelectableChromatogramModel RangeSelectableChromatogramModel { get; }
        public IFileBean AnalysisFile { get; }
        public SummarizedSpectrumModel? Ms1Spectrum {
            get => ms1Spectrum;
            set => SetProperty(ref ms1Spectrum, value);
        }
        private SummarizedSpectrumModel? ms1Spectrum;

        public ObservableCollection<SummarizedSpectrumModel> Ms2Spectrums { get; }

        public ChromatogramPeakFeature Peak { get; }

        public IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> Refer { get; }

        public ParameterBase Parameter { get; }

        public bool CanSetExperimentSpectrum() {
            return RangeSelectableChromatogramModel is { MainRange: not null, SubtractRanges: { Count: > 0 } };
        }

        public async Task SetExperimentSpectrumAsync(CancellationToken token) {
            if (RangeSelectableChromatogramModel is null or { MainRange: null } or { SubtractRanges: { Count: 0 } }) {
                return;
            }
            var rangeModel = RangeSelectableChromatogramModel;
            (var mainStart, var mainEnd) = rangeModel.ConvertToRt(rangeModel.MainRange);
            (var subStart, var subEnd) = rangeModel.ConvertToRt(rangeModel.SubtractRanges[0]);

            var spectrum = await provider.LoadMsNSpectrumsAsync(level: 2, token).ConfigureAwait(false);
            var experiments = spectrum.Select(spec => spec.ExperimentID).Distinct().OrderBy(v => v).ToArray();
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Ms2Spectrums.Clear();
                foreach (var exp in experiments) {
                    Ms2Spectrums.Add(new SummarizedSpectrumModel(DataAccess.GetSubtractSpectrum(spectrum, mainStart, mainEnd, subStart, subEnd, 1e-3, exp).OrderBy(s => s.Mass).ToList(), exp));
                }
            });

            var ms1Spectrum = await provider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
            Ms1Spectrum = new SummarizedSpectrumModel(DataAccess.GetSubtractSpectrum(ms1Spectrum, mainStart, mainEnd, subStart, subEnd, 1e-3, -1).Where(s => s.Intensity > 5).OrderBy(s => s.Mass).ToList(), -1);
        }

        public void SaveSpectrumAsNist(string mspFileName) {
            var comment = Peak.Comment ?? string.Empty;
            using (var fs = File.Open(mspFileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                foreach (var spectra in Ms2Spectrums) {
                    Peak.Comment = $"{comment}|ExperimentId={spectra.ExperimentId}";
                    SpectraExport.SaveSpectraTableAsNistFormat(fs, Peak, spectra.Spectrums, Refer, Parameter);
                }
            }
            Peak.Comment = comment;
        }
    }
}
