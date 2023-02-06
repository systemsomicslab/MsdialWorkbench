using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.BarChart;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.ComponentModel;

namespace Riken.Metabolomics.Pathwaymap
{
    public enum NodeType { Rectangle, Circle }

    public class Node {
        public string ID { get; set; } // node id assigned in a graph format
        public string Key { get; set; }
        public string Label { get; set; }
        public string Database { get; set; } // for wikipathway
        public string Ontology { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float CopyX { get; set; }
        public float CopyY { get; set; }
        public BarChartBean BarChart { get; set; } = null; // now barchart only
        public BitmapSource BarImageSource { get; set; } = null;
        public NodeType NodeType { get; set; } = NodeType.Rectangle; // now rectangle only
        public bool IsSelected { get; set; } = false;
        public bool IsMapped { get; set; } = false;
        public bool IsStereoValidated { get; set; } = false;
        public bool IsMappedByInChIKey { get; set; } = false;
        public string ColorCode { get; set; } = "000000";
    }

    public class Edge {
        public string ID { get; set; } = string.Empty; // node id assigned in a graph format
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string SourceNodeID { get; set; } = string.Empty;
        public string TargetNodeID { get; set; } = string.Empty;
        public string SourceArrow { get; set; } = string.Empty;
        public string TargetArrow { get; set; } = string.Empty;
        public float SourceX { get; set; } = -1;
        public float SourceY { get; set; } = -1;
        public float TargetX { get; set; } = -1;
        public float TargetY { get; set; } = -1;
        public string ColorCode { get; set; } = "000000";
        public float LineTickness { get; set; } = 1.0F;
    }

    public class PathwayMapObj : INotifyPropertyChanged {

        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Edge> Edges { get; set; } = new List<Edge>();

        public Dictionary<string, Node> Id2Node { get; set; } = new Dictionary<string, Node>();
        public Dictionary<string, Edge> Id2Edge { get; set; } = new Dictionary<string, Edge>();
        public Dictionary<string, List<string>> Node2Edges { get; set; } = new Dictionary<string, List<string>>();

        public float MinX { get; set; }
        public float MaxX { get; set; }
        public float MinY { get; set; }
        public float MaxY { get; set; }

        public float MaxNodeWidth { get; set; }
        public float MinNodeWidth { get; set; }

        public float MaxNodeHeight { get; set; }
        public float MinNodeHeight { get; set; }

        public float DisplayRangeMinX { get; set; }
        public float DisplayRangeMaxX { get; set; }
        public float DisplayRangeMinY { get; set; }
        public float DisplayRangeMaxY { get; set; }

        public string GraphTitle { get; set; }
        public string XAxisTitle { get; set; }
        public string YAxisTitle { get; set; }

        private string selectedPlotID;
        public string SelectedPlotID {
            get { return selectedPlotID;  }

            set {
                selectedPlotID = value;
                OnPropertyChanged("SelectedPlotID");
            }
        }

        public float RectangleRangeYmax { get; set; }
        public float RectangleRangeYmin { get; set; }
        public float RectangleRangeXmax { get; set; }
        public float RectangleRangeXmin { get; set; }

       

        public PathwayMapObj(List<Node> nodes, List<Edge> edges) {
            this.Nodes = nodes;
            this.Edges = edges;

            for (int i = 0; i < nodes.Count; i++) {
                var id = nodes[i].ID;
                Id2Node[id] = nodes[i];
                Node2Edges[id] = new List<string>(); // initialize
                nodes[i].CopyX = nodes[i].X;
                nodes[i].CopyY = nodes[i].Y;

                if (nodes[i].BarChart != null) {
                    var imagesource = new PlainBarChartForTable((int)nodes[i].Height * 3, 
                        (int)nodes[i].Width * 3, 300, 300).DrawBarChart2BitmapSource(nodes[i].BarChart, true);
                    //var image = (System.Windows.Media.Imaging.BitmapImage)imagesource;

                    nodes[i].BarImageSource = imagesource;
                    //nodes[i].BarImage = image;
                }
            }
            for (int i = 0; i < edges.Count; i++) {
                var id = edges[i].ID;
                Id2Edge[id] = edges[i];
            }
            
            // mapper
            foreach (var edge in edges) {
                var sourceID = edge.SourceNodeID;
                var targetID = edge.TargetNodeID;
                //Console.WriteLine(edge.ID + "\t" + edge.SourceNodeID + "\t" + edge.TargetNodeID);
                if (Node2Edges.ContainsKey(sourceID) && !Node2Edges[sourceID].Contains(targetID))
                    Node2Edges[sourceID].Add(targetID);
                if (Node2Edges.ContainsKey(targetID) && !Node2Edges[targetID].Contains(sourceID))
                    Node2Edges[targetID].Add(sourceID);
            }

            initializePathwayViewProperties();
        }

        public PathwayMapObj(List<Node> nodes) {
            this.Nodes = nodes;
            this.Edges = new List<Edge>();

            for (int i = 0; i < nodes.Count; i++) {
                var id = nodes[i].ID;
                Id2Node[id] = nodes[i];
                Node2Edges[id] = new List<string>(); // initialize
                nodes[i].CopyX = nodes[i].X;
                nodes[i].CopyY = nodes[i].Y;

                if (nodes[i].BarChart != null) {
                    var imagesource = new PlainBarChartForTable((int)nodes[i].Height, (int)nodes[i].Width, 300, 300).DrawBarChart2BitmapSource(nodes[i].BarChart, true);
                    nodes[i].BarImageSource = imagesource;
                }
            }
            initializePathwayViewProperties();
        }

        private void initializePathwayViewProperties() {
            if (this.Nodes == null || this.Nodes.Count == 0) return;

            var margin = 100;

            this.MaxNodeWidth = this.Nodes.Max(n => n.Width);
            this.MinNodeWidth = this.Nodes.Min(n => n.Width);
            this.MaxNodeHeight = this.Nodes.Max(n => n.Height);
            this.MinNodeHeight = this.Nodes.Min(n => n.Height);

            this.MinX = this.Nodes.Min(n => n.X) - margin;
            this.MaxX = this.Nodes.Max(n => n.X) + margin;
            this.MinY = this.Nodes.Min(n => n.Y) - margin;
            this.MaxY = this.Nodes.Max(n => n.Y) + margin;

            this.DisplayRangeMinX = this.MinX;
            this.DisplayRangeMaxX = this.MaxX;
            this.DisplayRangeMinY = this.MinY;
            this.DisplayRangeMaxY = this.MaxY;
        }

        #region // Required Methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName) {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged

    }
}
