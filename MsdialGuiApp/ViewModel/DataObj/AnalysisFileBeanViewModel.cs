using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    class FileBeanViewModel<T> : ViewModelBase where T: IFileBean
    {
        public FileBeanViewModel(T file) {
            File = file;
        }
        public T File { get; }

        public string FileName => File.FileName;

        public bool IsSelected {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }
        private bool isSelected;
    }

    class AnalysisFileBeanViewModel : FileBeanViewModel<AnalysisFileBean>
    {
        public AnalysisFileBeanViewModel(AnalysisFileBean file) : base(file) {
        }
    }

    class AlignmentFileBeanViewModel : FileBeanViewModel<AlignmentFileBean>
    {
        public AlignmentFileBeanViewModel(AlignmentFileBean file) : base(file) {
        }
    }
}
