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

namespace CompMs.Graphics.UI.Message
{
    /// <summary>
    /// Interaction logic for ShortMessageWindow.xaml
    /// </summary>
    public partial class ShortMessageWindow : System.Windows.Window
    {
        public ShortMessageWindow() {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text), 
                typeof(string), 
                typeof(ShortMessageWindow),
                new PropertyMetadata("Loading files..", TextPropertyChanged));

        private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            Console.WriteLine("Name from {0} to {1}", e.OldValue, e.NewValue);
        }

        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public override string ToString() {
            return Text;
        }
    }
}
