using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    class AlignmentFileVM : ViewModelBase
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

        private ParameterBase param;

        public AlignmentFileVM(AlignmentFileBean alignmentFileBean, ParameterBase param) {
            FileName = alignmentFileBean.FileName;
            this.param = param;
            Container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFileBean.FilePath);

            Ms1Spots = CollectionViewSource.GetDefaultView(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyVM(prop)));
        }

        private void OnTargetChanged() {
            var target = Target;

            // TODO: Implement other features (PeakHeight, PeakArea, Normalized PeakHeight, Normalized PeakArea)
            BarItems = target.AlignedPeakProperties
                .GroupBy(peak => param.FileID_ClassName[peak.FileID])
                .Select(pair => new BarItem { Class = pair.Key, Height = pair.Average(peak => peak.PeakHeightTop) })
                .ToList();
        }

    }
    // tempolary
    class BarItem {
        public double Height { get; set; }
        public string Class { get; set; }
    }
}
