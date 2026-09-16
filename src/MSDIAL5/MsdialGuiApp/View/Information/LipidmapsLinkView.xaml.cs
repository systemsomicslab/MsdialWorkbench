using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Information
{
    /// <summary>
    /// Interaction logic for LipidmapsLinkView.xaml
    /// </summary>
    public partial class LipidmapsLinkView : UserControl
    {
        public LipidmapsLinkView() {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }
}
