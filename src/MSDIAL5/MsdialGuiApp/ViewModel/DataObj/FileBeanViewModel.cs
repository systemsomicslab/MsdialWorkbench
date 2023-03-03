using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public abstract class FileBeanViewModel<T> : ViewModelBase where T : IFileBean
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
}
