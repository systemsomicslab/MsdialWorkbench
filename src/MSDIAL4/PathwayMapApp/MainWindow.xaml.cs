using Microsoft.Win32;
using Riken.Metabolomics.Pathwaymap;
using Riken.Metabolomics.Pathwaymap.Parser;
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
using System.Windows.Resources;
using System.Windows.Shapes;

namespace Riken.Metabolomics.PathwayMapApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
            //this.PathwayView.Content = readPathwayFile(@"C:\Users\ADMIN\Dropbox\Vanted mapper\pathwaytest.graphml");
            //this.PathwayView.Content = readPathwayFile();
            this.PathwayView.Content = readGpmlPathwayFile();
        }

        private PathwayMapUI readPathwayFile() {
            var fileUri = new Uri("/TEST/GlobalMapForPlant.graphml", UriKind.Relative);
            var info = Application.GetResourceStream(fileUri);
            var vantedReader = new VantedFormatParser();
            vantedReader.Read(info.Stream);
            var pathwayObj = new PathwayMapObj(vantedReader.nodes, vantedReader.edges);
            return new PathwayMapUI(pathwayObj);
        }

        private PathwayMapUI readGpmlPathwayFile() {
            var fileUri = new Uri("/TEST/LipidMapForAnimal.gpml", UriKind.Relative);
            var info = Application.GetResourceStream(fileUri);
            var wikipathwayReader = new WikipathwayFormatParser();
            wikipathwayReader.Read(info.Stream);

            var nodes = wikipathwayReader.nodes;
            foreach (var node in nodes) {
                if (node.Key != string.Empty)
                    Console.WriteLine(node.Label + "\t" + node.Key);
            }


            var pathwayObj = new PathwayMapObj(wikipathwayReader.nodes, wikipathwayReader.edges);
            return new PathwayMapUI(pathwayObj);
        }

        private PathwayMapUI readPathwayFile(string file) {
            var vantedReader = new VantedFormatParser();
            vantedReader.Read(file);
            var pathwayObj = new PathwayMapObj(vantedReader.nodes, vantedReader.edges);
            return new PathwayMapUI(pathwayObj);
        }

        private void contextMenu_SavePathwayAs_Click(object sender, RoutedEventArgs e) {
            var pathobj = (PathwayMapUI)this.PathwayView.Content;
            var nodes = pathobj.pathwayObj.Nodes;
            var edges = pathobj.pathwayObj.Edges;
            if (nodes == null || edges == null) return;
            var sfd = new SaveFileDialog();
            sfd.FileName = "*.gpml";
            sfd.Filter = "Wikipathway format(*.gpml)|*.gpml";
            sfd.Title = "Save file dialog";

            if (sfd.ShowDialog() == true) {
                new WikipathwayFormatParser().Write(sfd.FileName, nodes, edges);
            }
        }
    }
}
