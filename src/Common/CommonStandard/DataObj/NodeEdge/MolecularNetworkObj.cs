using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj.NodeEdge {
    public class NodeData {
        public int id { get; set; }
        public int segment { get; set; }
        public int position { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string Title { get; set; }
        public string Property { get; set; }
        public string Ontology { get; set; }
        public string Method { get; set; }
        public string Rt { get; set; }
        public string Ri { get; set; }
        public string Mz { get; set; }
        public string Adduct { get; set; }
        public string IonMode { get; set; }
        public string Formula { get; set; }
        public string InChiKey { get; set; }
        public string Smiles { get; set; }
        public double MsMin { get; set; }
        public double MsmsMin { get; set; }

        public Chart BarGraph { get; set; }
        public List<List<double>> MS { get; set; }
        public List<List<double>> MSMS { get; set; }
        public List<string> MsLabel { get; set; }
        public List<string> MsMsLabel { get; set; }
        public int Size { get; set; }
        public string backgroundcolor { get; set; }
        public string bordercolor { get; set; }

        public NodeData() {
            MS = new List<List<double>>();
            MSMS = new List<List<double>>();
            MsLabel = new List<string>();
            MsMsLabel = new List<string>();
        }
    }

    public class Node {
        public NodeData data { get; set; }
        public string classes { get; set; }
        public Node() {
            data = new NodeData();
        }
    }

    public class Chart {
        public string type { get; set; }
        public ChartData data { get; set; }
    }

    public class ChartData {
        public List<string> labels { get; set; }
        public List<ChartElement> datasets { get; set; }
    }

    public class ChartElement {
        public string label { get; set; }
        public List<string> backgroundColor { get; set; }
        public List<double> data { get; set; }
    }

    public class EdgeData {
        public int source { get; set; }
        public int target { get; set; }
        public string sourceName { get; set; }
        public string targetName { get; set; }
        public double score { get; set; }
        public double matchpeakcount { get; set; }
        public string linecolor { get; set; }
        public string comment { get; set; }
    }

    public class Edge {
        public EdgeData data { get; set; }
        public string classes { get; set; }
        public Edge() {
            data = new EdgeData();
        }
    }

    public class RootObject {
        public List<Node> nodes { get; set; }
        public List<Edge> edges { get; set; }
    }
}
