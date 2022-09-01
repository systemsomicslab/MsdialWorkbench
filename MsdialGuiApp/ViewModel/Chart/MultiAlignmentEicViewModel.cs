using CompMs.CommonMVVM;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class MultiAlignmentEicViewModel : ViewModelBase
    {
        public MultiAlignmentEicViewModel(ObservableCollection<AlignmentEicViewModel> alignmentEics) {
            AlignmentEics = alignmentEics ?? throw new ArgumentNullException(nameof(alignmentEics));
        }

        public MultiAlignmentEicViewModel(params AlignmentEicViewModel[] alignmentEics) {
            AlignmentEics = new ObservableCollection<AlignmentEicViewModel>(alignmentEics);
        }

        public ObservableCollection<AlignmentEicViewModel> AlignmentEics { get; }
    }
}
