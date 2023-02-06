using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace edu.ucdavis.fiehnlab.MonaExport.Windows {
	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class PasswordUserControl : UserControl {
		public PasswordUserControl() {
			InitializeComponent();

			PasswordBox.PasswordChanged += (sender, args) => {
				Password = ((PasswordBox)sender).SecurePassword;
			};
		}

		public SecureString Password {
			get { return (SecureString)GetValue(PasswordProperty); }
			set { SetValue(PasswordProperty, value); }
		}

		public static readonly DependencyProperty PasswordProperty =
			DependencyProperty.Register("Password", typeof(SecureString), typeof(PasswordUserControl),
				new PropertyMetadata(default(SecureString)));
	}
}
