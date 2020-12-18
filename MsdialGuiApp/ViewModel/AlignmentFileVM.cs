using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
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

        public AlignmentFileVM(AlignmentFileBean alignmentFileBean) {
            FileName = alignmentFileBean.FileName;
            Container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFileBean.FilePath);

            Ms1Spots = CollectionViewSource.GetDefaultView(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyVM(prop)));
        }
    }
}
