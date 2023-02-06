using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Riken.Metabolomics.Pathwaymap.Parser {
    public class WikipathwayFormatParser {
        private XmlTextReader xmlRdr = null;
        public List<Node> nodes { get; set; } = new List<Node>();
        public List<Edge> edges { get; set; } = new List<Edge>();

        #region reader
        public void Read(string file) {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                using (var xmlRdr = new XmlTextReader(fs)) {
                    this.xmlRdr = xmlRdr;
                    while (xmlRdr.Read()) {
                        if (xmlRdr.NodeType == XmlNodeType.Element) {
                            switch (xmlRdr.Name) {
                                case "Pathway": this.parseRun(); break; // mandatory
                            }
                        }
                    }
                }
            }
        }

        public void Read(Stream fs) {
            using (var xmlRdr = new XmlTextReader(fs)) {
                this.xmlRdr = xmlRdr;
                while (xmlRdr.Read()) {
                    if (xmlRdr.NodeType == XmlNodeType.Element) {
                        switch (xmlRdr.Name) {
                            case "Pathway": this.parseRun(); break; // mandatory
                        }
                    }
                }
            }
        }

        private void parseRun() {
            parserCommonMethod(
                "Pathway", null,
                new Dictionary<string, Action>()
                {
                    { "DataNode", () => this.parseNode() },
                    { "Interaction", () => this.parseEdge() },
                });
        }

        private void parseEdge() {
            var edge = new Edge();
            parserCommonMethod(
               "Interaction",
               new Dictionary<string, Action<string>>() {
                    { "GraphId", (v) => {
                        edge.ID = v;
                    }}
                },
               new Dictionary<string, Action>()
               {
                    { "Graphics", () => this.parseEdgeGraphics(edge) }
               });
            edge.ID = this.edges.Count.ToString();
            this.edges.Add(edge);
        }

        private void parseEdgeGraphics(Edge edge) {
            parserCommonMethod(
               "Graphics", 
               new Dictionary<string, Action<string>>() {
                    { "LineThickness", (v) => {
                        var tickness = 1.0F; 
                        if (float.TryParse(v, out tickness)) {
                            edge.LineTickness = tickness;
                        }
                    }},
                    { "Color", (v) => {
                        edge.ColorCode = v;
                    }}
               },
               new Dictionary<string, Action>()
               {
                    { "Point", () => this.parseEdgePoint(edge) }
               });
        }

        private void parseEdgePoint(Edge edge) {
            var isSource = false;
            while (this.xmlRdr.MoveToNextAttribute()) {
                switch (this.xmlRdr.Name) {
                    case "GraphRef":
                        if (edge.SourceNodeID == null || edge.SourceNodeID == string.Empty) {
                            edge.SourceNodeID = this.xmlRdr.Value;
                            isSource = true;
                        }
                        else {
                            edge.TargetNodeID = this.xmlRdr.Value;
                        }
                        break;
                    case "ArrowHead":
                       if (isSource) {
                            edge.SourceArrow = this.xmlRdr.Value;
                       }
                       else {
                            edge.TargetArrow = this.xmlRdr.Value;
                        }
                        break;
                }
            }
        }

        private void parseNode() {
            var node = new Node();
            parserCommonMethod(
               "DataNode",
               new Dictionary<string, Action<string>>() {
                    { "TextLabel", (v) => {
                        node.Label = v;
                    }},
                    { "GraphId", (v) => {
                        node.ID = v;
                    }}
                },
               new Dictionary<string, Action>()
               {
                    { "Attribute", () => this.parseNodeAttribute(node) },
                    { "Graphics", () => this.parseNodeGraphics(node) },
                    { "Xref", () => this.parseNodeXref(node) },
               });
            this.nodes.Add(node);
        }

        private void parseNodeXref(Node node) {
            while (this.xmlRdr.MoveToNextAttribute()) {
                switch (this.xmlRdr.Name) {
                    case "Database":
                        node.Database = this.xmlRdr.Value;
                        break;
                    case "ID":
                        node.Key = this.xmlRdr.Value;
                        break;
                }
            }
        }

        private void parseNodeGraphics(Node node) {
            while (this.xmlRdr.MoveToNextAttribute()) {
                float v = 0;
                switch (this.xmlRdr.Name) {
                    case "CenterX":
                        float.TryParse(this.xmlRdr.Value, out v);
                        node.X = v;
                        break;
                    case "CenterY":
                        float.TryParse(this.xmlRdr.Value, out v);
                        node.Y = v;
                        break;
                    case "Width":
                        float.TryParse(this.xmlRdr.Value, out v);
                        node.Width = v;
                        break;
                    case "Height":
                        float.TryParse(this.xmlRdr.Value, out v);
                        node.Height = v;
                        break;
                }
            }
        }

        private void parseNodeAttribute(Node node) {
            return;
        }

        private void parserCommonMethod(string returnElementName, Dictionary<string, Action<string>> attributeActions, Dictionary<string, Action> elementActions) {
            if (elementActions == null) return;
            if (attributeActions != null) {
                while (this.xmlRdr.MoveToNextAttribute()) {
                    if (attributeActions.ContainsKey(this.xmlRdr.Name)) {
                        attributeActions[this.xmlRdr.Name](this.xmlRdr.Value);
                    }
                }
            }
            while (this.xmlRdr.Read()) {
                if (this.xmlRdr.NodeType == XmlNodeType.Element) {
                    if (elementActions.ContainsKey(this.xmlRdr.Name))
                        elementActions[this.xmlRdr.Name]();
                }
                else if (this.xmlRdr.NodeType == XmlNodeType.EndElement) {
                    if (this.xmlRdr.Name == returnElementName)
                        return;
                }
            }
        }
        #endregion


        public void Write(string output, List<Node> nodes, List<Edge> edges) {

            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.Encoding = Encoding.UTF8;

            using (var xr = XmlWriter.Create(output, settings)) {

                #region pathway element
                xr.WriteStartElement("Pathway");

                //xr.WriteStartAttribute("xmlns");
                //var html = "http://pathvisio.org/GPML/2013a";
                //xr.WriteString(html.ToString());
                //xr.WriteEndAttribute();

                xr.WriteStartAttribute("Name");
                xr.WriteString("Metabolomic Pathway");
                xr.WriteEndAttribute();

                xr.WriteStartAttribute("Version");
                xr.WriteString("20190108");
                xr.WriteEndAttribute();

                xr.WriteStartAttribute("Organism");
                xr.WriteString("Homo sapiens");
                xr.WriteEndAttribute();
                #endregion

                #region Graphics element
                xr.WriteStartElement("Graphics");
                xr.WriteStartAttribute("BoardWidth");
                xr.WriteValue("3000");
                xr.WriteEndAttribute();
                xr.WriteStartAttribute("BoardHeight");
                xr.WriteValue("3000");
                xr.WriteEndAttribute();
                xr.WriteEndElement(); // close graphics element
                #endregion

                foreach (var node in nodes) {

                    xr.WriteStartElement("DataNode");
                    xr.WriteStartAttribute("TextLabel");
                    xr.WriteValue(node.Label);
                    xr.WriteEndAttribute();
                    xr.WriteStartAttribute("GraphId");
                    xr.WriteValue(node.ID);
                    xr.WriteEndAttribute();
                    xr.WriteStartAttribute("Type");
                    xr.WriteValue("Metabolite");
                    xr.WriteEndAttribute();

                    #region Graphics
                    xr.WriteStartElement("Graphics");

                    xr.WriteStartAttribute("CenterX");
                    xr.WriteValue(node.X);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("CenterY");
                    xr.WriteValue(node.Y);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("Width");
                    xr.WriteValue(node.Width);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("Height");
                    xr.WriteValue(node.Height);
                    xr.WriteEndAttribute();

                    xr.WriteEndElement(); // close Graphics element
                    #endregion

                    #region Xref
                    xr.WriteStartElement("Xref");

                    xr.WriteStartAttribute("Database");
                    xr.WriteValue(node.Database);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("ID");
                    xr.WriteValue(node.Key);
                    xr.WriteEndAttribute();

                    xr.WriteEndElement(); // close Xref element
                    #endregion


                    xr.WriteEndElement(); // close DataNode element
                }

                foreach (var edge in edges) {
                    xr.WriteStartElement("Interaction");
                    xr.WriteStartAttribute("GraphId");
                    xr.WriteValue(edge.ID);
                    xr.WriteEndAttribute();

                    #region Graphics
                    xr.WriteStartElement("Graphics");

                    xr.WriteStartAttribute("ZOrder");
                    xr.WriteValue("12288");
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("LineThickness");
                    xr.WriteValue("1.0");
                    xr.WriteEndAttribute();

                    #region Point source
                    xr.WriteStartElement("Point");

                    xr.WriteStartAttribute("X");
                    xr.WriteValue(edge.SourceX);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("Y");
                    xr.WriteValue(edge.SourceY);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("GraphRef");
                    xr.WriteValue(edge.SourceNodeID);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("ArrowHead");
                    xr.WriteValue("Arrow");
                    xr.WriteEndAttribute();

                    xr.WriteEndElement(); // close Point element
                    #endregion

                    #region Point target
                    xr.WriteStartElement("Point");

                    xr.WriteStartAttribute("X");
                    xr.WriteValue(edge.TargetX);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("Y");
                    xr.WriteValue(edge.TargetY);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("GraphRef");
                    xr.WriteValue(edge.TargetNodeID);
                    xr.WriteEndAttribute();

                    xr.WriteStartAttribute("ArrowHead");
                    xr.WriteValue("Arrow");
                    xr.WriteEndAttribute();

                    xr.WriteEndElement(); // close Point element
                    #endregion

                    xr.WriteEndElement(); // close Graphics element
                    #endregion

                    xr.WriteEndElement(); // close Interaction element
                }

                xr.WriteEndElement(); // close pathway element
            }
        }
    }
}
