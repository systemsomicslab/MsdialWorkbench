using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for SubstructureViewer.xaml
    /// </summary>
    public partial class SubstructureViewer : Window
    {
        private const int autosizeColumnNumber = 4;

        public SubstructureViewer(Rfx.Riken.OsakaUniv.RawData rawData, FormulaResult formulaResult, List<FragmentOntology> uniqueFragmentDB)
        {
            InitializeComponent();
            this.DataContext = new SubstructureViewerVM(rawData, formulaResult, uniqueFragmentDB);
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e) {
            var view = sender as ListView;
            if (view == null || view.View == null)
                return;
            
            var gridView = view.View as GridView;
            var size = view.ActualWidth / (double)gridView.Columns.Count;
           
            for (int i = 0; i < gridView.Columns.Count; i++) {
                gridView.Columns[i].Width = size;
            }
        }
    }
}
