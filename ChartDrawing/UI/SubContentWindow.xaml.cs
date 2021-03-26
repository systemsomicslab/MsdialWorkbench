using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CompMs.Graphics.UI
{
    /// <summary>
    /// Interaction logic for SubContentWindow.xaml
    /// </summary>
    public partial class SubContentWindow : System.Windows.Window
    {
        public SubContentWindow() {
            InitializeComponent();
        }

        public static ICommand CreateNewViewCommand = new DelegateCommand<FrameworkElement>(ExecutedCreateNewViewCommand);

        private static void ExecutedCreateNewViewCommand(FrameworkElement fe) {
            var owner = GetWindow(fe);
            var type = fe.GetType();
            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            if (constructorInfo == null) {
                return;
            }
            var instance = constructorInfo.Invoke(null);

            var scw = new SubContentWindow()
            {
                Owner = owner,
            };
            scw.Content = instance;
            scw.DataContext = fe.DataContext;

            scw.Show();
        }
    }
}
