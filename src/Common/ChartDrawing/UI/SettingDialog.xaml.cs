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
    /// Interaction logic for SettingDialog.xaml
    /// </summary>
    public partial class SettingDialog : System.Windows.Window
    {
        public SettingDialog() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ApplyCommandProperty =
            DependencyProperty.Register(
                nameof(ApplyCommand),
                typeof(ICommand),
                typeof(SettingDialog),
                new PropertyMetadata(NeverCommand.Instance));

        public ICommand ApplyCommand {
            get => (ICommand)GetValue(ApplyCommandProperty);
            set => SetValue(ApplyCommandProperty, value);
        }

        public static readonly DependencyProperty FinishCommandProperty =
            DependencyProperty.Register(
                nameof(FinishCommand),
                typeof(ICommand),
                typeof(SettingDialog),
                new PropertyMetadata(NeverCommand.Instance));

        public ICommand FinishCommand {
            get => (ICommand)GetValue(FinishCommandProperty);
            set => SetValue(FinishCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(
                nameof(CancelCommand),
                typeof(ICommand),
                typeof(SettingDialog),
                new PropertyMetadata(IdentityCommand.Instance));

        public ICommand CancelCommand {
            get => (ICommand)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        private void FinishClose(object sender, RoutedEventArgs e) {
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) {
                DialogResult = true;
            }
            Close();
        }

        private void CancelClose(object sender, RoutedEventArgs e) {
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) {
                DialogResult = false;
            }
            Close();
        }
    }
}
