using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    /// <summary>
    /// Interaction logic for SaveImageAsDialog.xaml
    /// </summary>
    public partial class SaveImageAsDialog : System.Windows.Window
    {
        public SaveImageAsDialog() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));
        }
    }
}
