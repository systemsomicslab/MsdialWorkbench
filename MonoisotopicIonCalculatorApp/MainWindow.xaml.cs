using Rfx.Riken.OsakaUniv;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MonoisotopicIonCalculatorApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            this.TextBox_Formula.Text = "C6H12O6";
            this.RadioButton_Positive.IsChecked = true;
            this.RadioButton_Negative.IsChecked = false;

            calculateExactMass();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            calculateExactMass();
        }

        private void calculateExactMass() {
            var formulaString = this.TextBox_Formula.Text;
            if (formulaString == null || formulaString == string.Empty) return;

            var ionMode = IonMode.Positive;
            if (this.RadioButton_Negative.IsChecked == true)
                ionMode = IonMode.Negative;

            var adduction = AdductIonParcer.GetAdductIonBean("[M]+");
            if (ionMode == IonMode.Negative)
                adduction = AdductIonParcer.GetAdductIonBean("[M]-");

            var formula = FormulaStringParcer.OrganicElementsReader(formulaString);
            var mass = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adduction, formula.Mass);

            this.TextBox_Result.Text = mass.ToString();
        }
    }
}
