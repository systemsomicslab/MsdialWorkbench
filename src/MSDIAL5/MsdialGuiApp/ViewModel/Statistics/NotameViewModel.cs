using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    public sealed class NotameViewModel
    {
        private readonly Notame _notame;

        public NotameViewModel(Notame notame) {
            RunNotameCommand = new DelegateCommand(RunNotame);
            _notame = notame;
        }

        public DelegateCommand RunNotameCommand { get; }

        private void RunNotame() {
            _notame.Run();
        }
    }
}