using CompMs.CommonMVVM;
using System.Windows.Input;

namespace CompMs.Graphics.UI
{
    public class SettingDialogViewModel : ViewModelBase
    {
        public bool? Result {
            get => _result;
            set => SetProperty(ref _result, value);
        }
        private bool? _result;

        public virtual ICommand? ApplyCommand { get; }
        public virtual ICommand? CancelCommand { get; }
        public virtual ICommand? FinishCommand { get; }
    }
}
