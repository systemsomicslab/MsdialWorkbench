using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public class AlignmentEicModel : ValidatableBase
    {
        public AlignmentEicModel(
            ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer,
            string eicFile,
            Dictionary<int, string> id2cls,
            string graphTitle,
            string horizontalTitle) {

            this.chromatogramSpotSerializer = chromatogramSpotSerializer;
            this.eicFile = eicFile;
            this.id2cls = id2cls;
            this.horizontalTitle = horizontalTitle;

            HorizontalData = new AxisData(new ContinuousAxisManager<double>(0, 1), string.Empty, string.Empty);
            VerticalData = new AxisData(new ContinuousAxisManager<double>(0, 1), string.Empty, string.Empty);
            GraphTitle = graphTitle;
        }

        private readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly string eicFile;
        private readonly Dictionary<int, string> id2cls;
        private readonly string horizontalTitle;

        public List<Chromatogram> EicChromatograms {
            get => eicChromatogram;
            set => SetProperty(ref eicChromatogram, value);
        }
        private List<Chromatogram> eicChromatogram;

        public AxisData HorizontalData {
            get => horizontalData;
            set => SetProperty(ref horizontalData, value);
        }
        private AxisData horizontalData;

        public AxisData VerticalData {
            get => verticalData;
            set => SetProperty(ref verticalData, value);
        }
        private AxisData verticalData;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

        public async Task LoadEicAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            var eicChromatograms = new List<Chromatogram>();
            if (target != null) {
                // maybe using file pointer is better
                eicChromatograms = await Task.Run(() => {
                    var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
                    var chroms = new List<Chromatogram>(spotinfo.PeakInfos.Count);
                    foreach (var peakinfo in spotinfo.PeakInfos) {
                        var items = peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList();
                        var peakitems = items.Where(item => peakinfo.ChromXsLeft.Value <= item.Time && item.Time <= peakinfo.ChromXsRight.Value).ToList();
                        chroms.Add(new Chromatogram
                        {
                            Class = id2cls[peakinfo.FileID],
                            Peaks = items,
                            PeakArea = peakitems,
                        });
                    }
                    return chroms;
                }, token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            EicChromatograms = eicChromatograms;

            HorizontalData = new AxisData(
                ContinuousAxisManager<double>.Build(
                    EicChromatograms?.SelectMany(chrom => chrom.Peaks),
                    peak => peak.Time),
                "Time",
                horizontalTitle);
            VerticalData = new AxisData(
                ContinuousAxisManager<double>.Build(
                    EicChromatograms?.SelectMany(chrom => chrom.Peaks),
                    peak => peak.Intensity),
                "Intensity",
                "Abundance");
        }
    }
}
