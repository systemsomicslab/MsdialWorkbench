using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class WholeImageResultModel : BindableBase
    {
        public AnalysisFileBean File { get; }
        public ChromatogramPeakFeatureCollection Peaks { get; }

        public WholeImageResultModel(AnalysisFileBean file) {
            File = file ?? throw new System.ArgumentNullException(nameof(file));
            Peaks = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath, default).Result;
        }
    }
}
