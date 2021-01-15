using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;


namespace CompMs.App.Msdial.ViewModel.Dims
{
    public class AlignmentDimsVM : AlignmentFileVM
    {
        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set {
                var old = ms1Spots;
                if (SetProperty(ref ms1Spots, value)) {
                    // if (old != null) old.Filter -= PeakFilter;
                    // if (ms1Peaks != null) ms1Peaks.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Spots;

        public List<BarItem> BarItems {
            get => barItems;
            set => SetProperty(ref barItems, value);
        }
        private List<BarItem> barItems = new List<BarItem>();

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public string EicFile {
            get => eicFile;
            set => SetProperty(ref eicFile, value);
        }
        private string eicFile;

        public AlignmentResultContainer Container {
            get => container;
            set => SetProperty(ref container, value);
        }
        private AlignmentResultContainer container;

        public AlignmentSpotPropertyVM Target {
            get => target;
            set {
                if (SetProperty(ref target, value)) {
                    OnTargetChanged();
                }
            }
        }
        private AlignmentSpotPropertyVM target;

        public List<Chromatogram> EicChromatograms {
            get => eicChromatograms;
            set => SetProperty(ref eicChromatograms, value);
        }
        private List<Chromatogram> eicChromatograms;

        public double EicMax => 5d;
        public double EicMin => 0d;
        public double IntensityMax => 1000000d;
        public double IntensityMin => 0d;

        private ParameterBase param;
        private static ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        static AlignmentDimsVM() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", CompMs.Common.Components.ChromXType.Mz);
        }

        public AlignmentDimsVM(AlignmentFileBean alignmentFileBean, ParameterBase param) {
            FileName = alignmentFileBean.FileName;
            EicFile = alignmentFileBean.EicFilePath;
            this.param = param;
            Container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFileBean.FilePath);

            Ms1Spots = CollectionViewSource.GetDefaultView(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyVM(prop)));
        }

        private void OnTargetChanged() {
            var target = Target;
            if (target == null) {
                BarItems = new List<BarItem>();
                EicChromatograms = new List<Chromatogram>();
                return;
            }

            // TODO: Implement other features (PeakHeight, PeakArea, Normalized PeakHeight, Normalized PeakArea)
            BarItems = target.AlignedPeakProperties
                .GroupBy(peak => param.FileID_ClassName[peak.FileID])
                .Select(pair => new BarItem { Class = pair.Key, Height = pair.Average(peak => peak.PeakHeightTop) })
                .ToList();

            // maybe using file pointer is better
            var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(EicFile, target.MasterAlignmentID);
            EicChromatograms = spotinfo.PeakInfos.Select(peakinfo => new Chromatogram { Peaks = peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList() }).ToList();
        }
    }
}
