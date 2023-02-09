using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class RawMsSpectrumCheckModel : BindableBase
    {
        public RawMsSpectrumCheckModel(ReadOnlyCollection<RawSpectrum> spectra) {
            Spectra = spectra;

            SelectedSpectrum = Spectra.FirstOrDefault();
        }

        public ReadOnlyCollection<RawSpectrum> Spectra { get; }

        public RawSpectrum SelectedSpectrum {
            get => _selectedSpectrum;
            set => SetProperty(ref _selectedSpectrum, value);
        }
        private RawSpectrum _selectedSpectrum;

        public static Task<RawMsSpectrumCheckModel> CreateAsync(AnalysisDataModel dataModel, CancellationToken token = default) {
            return dataModel.CreateDataProviderByFactory(new StandardDataProviderFactory(isGuiProcess: true), token)
                .ContinueWith(async task =>
                {
                    var provider = await task.ConfigureAwait(false);
                    var spectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
                    return new RawMsSpectrumCheckModel(spectra);
                }).Unwrap();
        }
    }
}
