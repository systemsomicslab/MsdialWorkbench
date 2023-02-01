using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NCDK.MolViewer
{
    /// <summary>
    /// Interaction logic for AppearanceDialog.xaml
    /// </summary>
    public partial class AppearanceDialog : Window
    {
        public AppearanceDialog(object context)
        {
            InitializeComponent();

            DataContext = context;
        }
    }
}
