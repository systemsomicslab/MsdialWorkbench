using Rfx.Riken.OsakaUniv;
using System.Diagnostics;
using System.Windows;

namespace edu.ucdavis.fiehnlab.MonaExport.ViewModels {
	class LoginErrorVM : ViewModelBase {

		public LoginErrorVM() {	}

		private DelegateCommand close;
		public DelegateCommand Close {
			get {
				return close ?? (close = new DelegateCommand(
					wnd => ((Window)wnd).Close(),
					wnd => true
				));
			}
		}

		private DelegateCommand monaRegistrationCommand;
		public DelegateCommand MonaRegistrationCommand {
			get {
				return monaRegistrationCommand ?? (monaRegistrationCommand = new DelegateCommand(
					it => { Debug.WriteLine("sending to mona"); Process.Start("http://mona.fiehnlab.ucdavis.edu"); },
					it => true
				));
			}
		}
	}
}
