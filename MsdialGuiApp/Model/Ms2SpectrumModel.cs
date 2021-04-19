using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public class Ms2SpectrumModel : ValidatableBase
    {
        public Ms2SpectrumModel(
            AxisData horizontalData,
            IReadOnlyList<AxisData> verticalDatas,
            IReadOnlyList<IMsSpectrumLoader> loaders,
            string graphTitle) {

            HorizontalData = horizontalData;
            VerticalDatas = verticalDatas;
            this.loaders = loaders;
            GraphTitle = graphTitle;
        }

        private readonly IReadOnlyList<IMsSpectrumLoader> loaders;

        public IReadOnlyList<IList<SpectrumPeak>> Spectrums {
            get => spectrums;
            set => SetProperty(ref spectrums, value);
        }
        private IReadOnlyList<IList<SpectrumPeak>> spectrums = new List<IList<SpectrumPeak>>(0);

        public async Task LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            Spectrums = await Task.WhenAll(loaders.Select(loader => loader.LoadSpectrumAsync(target, token)));
        }

        public AxisData HorizontalData {
            get => horizontalData;
            set => SetProperty(ref horizontalData, value);
        }
        private AxisData horizontalData;

        public IReadOnlyList<AxisData> VerticalDatas {
            get => verticalDatas;
            set => SetProperty(ref verticalDatas, value);
        }
        private IReadOnlyList<AxisData> verticalDatas;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;
    }
}
