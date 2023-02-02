using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

#region vanted schema
//< key attr.name=".directed" attr.type= "boolean" for="graph" id= "ga4" />
//< key attr.name= ".node_halfErrorBar" attr.type= "boolean" for="graph" id= "ga17" />
//< key attr.name= ".node_lineChartShowStdDevRangeLine" attr.type= "boolean" for="graph" id= "ga22" />
//< key attr.name= ".node_lineChartShowLines" attr.type= "boolean" for="graph" id= "ga19" />
//< key attr.name= ".node_lineChartShowStdDev" attr.type= "boolean" for="graph" id= "ga21" />
//< key attr.name= ".node_showRangeAxis" attr.type= "boolean" for="graph" id= "ga32" />
//< key attr.name= ".node_lineChartFillTimeGaps" attr.type= "boolean" for="graph" id= "ga18" />
//< key attr.name= ".node_removeEmptyConditions" attr.type= "boolean" for="graph" id= "ga28" />
//< key attr.name= ".node_showGridCategory" attr.type= "boolean" for="graph" id= "ga30" />
//< key attr.name= ".node_chartStdDevTopWidth" attr.type= "double" for="graph" id= "ga15" />
//< key attr.name= ".node_gridWidth" attr.type= "double" for="graph" id= "ga16" />
//< key attr.name= ".graphbackgroundcolor" attr.type= "string" for="graph" id= "ga5" />
//< key attr.name= ".node_axisWidth" attr.type= "double" for="graph" id= "ga7" />
//< key attr.name= ".node_lineChartShowShapes" attr.type= "boolean" for="graph" id= "ga20" />
//< key attr.name= ".node_outlineBorderWidth" attr.type= "double" for="graph" id= "ga23" />
//< key attr.name= ".node_chartStdDevLineWidth" attr.type= "double" for="graph" id= "ga14" />
//< key attr.name= ".node_categoryBackgroundColorB" attr.type= "string" for="graph" id= "ga9" />
//< key attr.name= ".node_categoryBackgroundColorC" attr.type= "string" for="graph" id= "ga10" />
//< key attr.name= ".node_plotOrientationHor" attr.type= "boolean" for="graph" id= "ga27" />
//< key attr.name= ".node_categoryBackgroundColorA" attr.type= "string" for="graph" id= "ga8" />
//< key attr.name= ".connectPriorItems" attr.type= "boolean" for="graph" id= "ga3" />
//< key attr.name= ".node_showGridRange" attr.type= "boolean" for="graph" id= "ga31" />
//< key attr.name= ".node_plotAxisFontSize" attr.type= "int" for="graph" id= "ga24" />
//< key attr.name= ".node_categoryBackgroundColorIndexA" attr.type= "int" for="graph" id= "ga11" />
//< key attr.name= ".node_plotAxisRotation" attr.type= "double" for="graph" id= "ga25" />
//< key attr.name= ".node_showCategoryAxis" attr.type= "boolean" for="graph" id= "ga29" />
//< key attr.name= ".chart_color_line_names" attr.type= "string" for="graph" id= "ga1" />
//< key attr.name= ".chart_colors" attr.type= "string" for="graph" id= "ga2" />
//< key attr.name= ".grid_color" attr.type= "string" for="graph" id= "ga6" />
//< key attr.name= ".node_plotAxisSteps" attr.type= "double" for="graph" id= "ga26" />
//< key attr.name= ".ttestCircleSize" attr.type= "double" for="graph" id= "ga35" />
//< key attr.name= ".node_usePieScale" attr.type= "boolean" for="graph" id= "ga33" />
//< key attr.name= ".axis_color" attr.type= "string" for="graph" id= "ga0" />
//< key attr.name= ".node_chartShapeSize" attr.type= "double" for="graph" id= "ga13" />
//< key attr.name= ".node_useStdErr" attr.type= "boolean" for="graph" id= "ga34" />
//< key attr.name= ".node_categoryBackgroundColorIndexC" attr.type= "int" for="graph" id= "ga12" />
//< key attr.name= ".graphics.fill.red" attr.type= "int" for="node" id= "na7" />
//< key attr.name= ".graphics.fill.blue" attr.type= "int" for="node" id= "na4" />
//< key attr.name= ".graphics.dimension.height" attr.type= "double" for="node" id= "na2" />
//< key attr.name= ".graphics.opacity" attr.type= "double" for="node" id= "na12" />
//< key attr.name= ".graphics.linemode" attr.type= "string" for="node" id= "na11" />
//< key attr.name= ".graphics.outline.opacity" attr.type= "int" for="node" id= "na15" />
//< key attr.name= ".labelgraphics.fontStyle" attr.type= "string" for="node" id= "na25" />
//< key attr.name= ".labelgraphics.position.relHor" attr.type= "double" for="node" id= "na29" />
//< key attr.name= ".graphics.outline.red" attr.type= "int" for="node" id= "na16" />
//< key attr.name= ".tooltip" attr.type= "string" for="node" id= "na35" />
//< key attr.name= ".graphics.rounding" attr.type= "double" for="node" id= "na18" />
//< key attr.name= ".labelgraphics.position.localAlign" attr.type= "double" for="node" id= "na28" />
//< key attr.name= ".zlevel" attr.type= "int" for="node" id= "na34" />
//< key attr.name= ".labelgraphics.alignment" attr.type= "string" for="node" id= "na20" />
//< key attr.name= ".labelgraphics.anchor" attr.type= "string" for="node" id= "na21" />
//< key attr.name= ".graphics.gradient" attr.type= "double" for="node" id= "na10" />
//< key attr.name= ".graphics.frameThickness" attr.type= "double" for="node" id= "na9" />
//< key attr.name= ".graphics.shape" attr.type= "string" for="node" id= "na19" />
//< key attr.name= ".labelgraphics.labelOffset.x" attr.type= "double" for="node" id= "na26" />
//< key attr.name= ".pastedNode" attr.type= "boolean" for="node" id= "na33" />
//< key attr.name= ".graphics.coordinate.x" attr.type= "double" for="node" id= "na0" />
//< key attr.name= ".labelgraphics.labelOffset.y" attr.type= "double" for="node" id= "na27" />
//< key attr.name= ".labelgraphics.text" attr.type= "string" for="node" id= "na31" />
//< key attr.name= ".labelgraphics.fontName" attr.type= "string" for="node" id= "na23" />
//< key attr.name= ".labelgraphics.color" attr.type= "string" for="node" id= "na22" />
//< key attr.name= ".graphics.fill.green" attr.type= "int" for="node" id= "na5" />
//< key attr.name= ".graphics.outline.transparency" attr.type= "int" for="node" id= "na17" />
//< key attr.name= ".graphics.coordinate.y" attr.type= "double" for="node" id= "na1" />
//< key attr.name= ".graphics.dimension.width" attr.type= "double" for="node" id= "na3" />
//< key attr.name= ".graphics.fill.opacity" attr.type= "int" for="node" id= "na6" />
//< key attr.name= ".graphics.outline.blue" attr.type= "int" for="node" id= "na13" />
//< key attr.name= ".labelgraphics.position.relVert" attr.type= "double" for="node" id= "na30" />
//< key attr.name= ".graphics.outline.green" attr.type= "int" for="node" id= "na14" />
//< key attr.name= ".labelgraphics.fontSize" attr.type= "int" for="node" id= "na24" />
//< key attr.name= ".graphics.fill.transparency" attr.type= "int" for="node" id= "na8" />
//< key attr.name= ".labelgraphics.type" attr.type= "string" for="node" id= "na32" />
//< key attr.name= ".graphics.fill.red" attr.type= "int" for="edge" id= "ea7" />
//< key attr.name= ".graphics.fill.blue" attr.type= "int" for="edge" id= "ea4" />
//< key attr.name= ".graphics.opacity" attr.type= "double" for="edge" id= "ea13" />
//< key attr.name= ".graphics.bends.bend4.y" attr.type= "double" for="edge" id= "ea29" />
//< key attr.name= ".graphics.bends.bend3.y" attr.type= "double" for="edge" id= "ea27" />
//< key attr.name= ".graphics.bends.bend4.x" attr.type= "double" for="edge" id= "ea28" />
//< key attr.name= ".graphics.linemode" attr.type= "string" for="edge" id= "ea11" />
//< key attr.name= ".graphics.outline.opacity" attr.type= "int" for="edge" id= "ea16" />
//< key attr.name= ".graphics.bends.bend1.x" attr.type= "double" for="edge" id= "ea22" />
//< key attr.name= ".graphics.bends.bend2.y" attr.type= "double" for="edge" id= "ea25" />
//< key attr.name= ".graphics.bends.bend3.x" attr.type= "double" for="edge" id= "ea26" />
//< key attr.name= ".graphics.bends.bend1.y" attr.type= "double" for="edge" id= "ea23" />
//< key attr.name= ".graphics.bends.bend2.x" attr.type= "double" for="edge" id= "ea24" />
//< key attr.name= ".graphics.docking.target" attr.type= "string" for="edge" id= "ea3" />
//< key attr.name= ".graphics.outline.red" attr.type= "int" for="edge" id= "ea17" />
//< key attr.name= ".graphics.rounding" attr.type= "double" for="edge" id= "ea19" />
//< key attr.name= ".graphics.gradient" attr.type= "double" for="edge" id= "ea10" />
//< key attr.name= ".graphics.thickness" attr.type= "double" for="edge" id= "ea21" />
//< key attr.name= ".graphics.frameThickness" attr.type= "double" for="edge" id= "ea9" />
//< key attr.name= ".graphics.shape" attr.type= "string" for="edge" id= "ea20" />
//< key attr.name= ".graphics.docking.source" attr.type= "string" for="edge" id= "ea2" />
//< key attr.name= ".graphics.arrowhead" attr.type= "string" for="edge" id= "ea0" />
//< key attr.name= ".graphics.linetype" attr.type= "string" for="edge" id= "ea12" />
//< key attr.name= ".graphics.fill.green" attr.type= "int" for="edge" id= "ea5" />
//< key attr.name= ".graphics.outline.transparency" attr.type= "int" for="edge" id= "ea18" />
//< key attr.name= ".graphics.fill.opacity" attr.type= "int" for="edge" id= "ea6" />
//< key attr.name= ".graphics.outline.blue" attr.type= "int" for="edge" id= "ea14" />
//< key attr.name= ".graphics.outline.green" attr.type= "int" for="edge" id= "ea15" />
//< key attr.name= ".graphics.arrowtail" attr.type= "string" for="edge" id= "ea1" />
//< key attr.name= ".graphics.fill.transparency" attr.type= "int" for="edge" id= "ea8" />
#endregion

namespace Riken.Metabolomics.Pathwaymap.Parser {
    public class VantedFormatParser {
        private XmlTextReader xmlRdr = null;
        public List<Node> nodes { get; set; } = new List<Node>();
        public List<Edge> edges { get; set; } = new List<Edge>();
        public void Read(string file) {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                using (var xmlRdr = new XmlTextReader(fs)) {
                    this.xmlRdr = xmlRdr;
                    while (xmlRdr.Read()) {
                        if (xmlRdr.NodeType == XmlNodeType.Element) {
                            switch (xmlRdr.Name) {
                                case "graph": this.parseRun(); break; // mandatory
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
                            case "graph": this.parseRun(); break; // mandatory
                        }
                    }
                }
            }
        }

        private void parseRun() {
            parserCommonMethod(
                "graph", null,
                new Dictionary<string, Action>()
                {
                    { "node", () => this.parseNode() },
                    { "edge", () => this.parseEdge() },
                });
        }

        private void parseEdge() {
            var edge = new Edge();

            parserCommonMethod(
                "edge",
                new Dictionary<string, Action<string>>() {
                    { "id", (v) => {
                        edge.ID = v;
                    }},
                    { "source", (v) => {
                        edge.SourceNodeID = v;
                    }},
                    { "target", (v) => {
                         edge.TargetNodeID = v;
                    }}
                }
                ,
                new Dictionary<string, Action>()
                {
                    { "data", () => this.parseEdgeData(edge) }
                });
            this.edges.Add(edge);
        }

        private void parseEdgeData(Edge edge) {

            if (this.xmlRdr.HasAttributes) {
                while (this.xmlRdr.MoveToNextAttribute()) {
                    if (this.xmlRdr.Name == "key") {
                        var dataKey = this.xmlRdr.Value;
                        var dataValue = this.xmlRdr.ReadString();

                        switch (dataKey) {
                            case "ea0": // graphics.arrowhead property
                                edge.SourceArrow = dataValue;
                                break;
                            case "ea1": // graphics.arrowtail
                                edge.TargetArrow = dataValue;
                                break;
                        }
                        break;
                    }
                }
            }
        }

        private void parseNode() {
            var node = new Node();
            parserCommonMethod(
                "node",
                new Dictionary<string, Action<string>>() {
                    { "id", (v) => {
                        node.ID = v;
                    }}
                }
                ,
                new Dictionary<string, Action>()
                {
                    { "data", () => this.parseNodeData(node) }
                });
            this.nodes.Add(node);
        }

        private void parseNodeData(Node node) {
            if (this.xmlRdr.HasAttributes) {
                while (this.xmlRdr.MoveToNextAttribute()) {
                    if (this.xmlRdr.Name == "key") {
                        var dataKey = this.xmlRdr.Value;
                        var dataValue = this.xmlRdr.ReadString();

                        float fv;
                        switch (dataKey) {
                            case "na0": // graphics.coordinate.x 
                                if (float.TryParse(dataValue, out fv)) node.X = fv;
                                break;
                            case "na1": // graphics.coordinate.y 
                                if (float.TryParse(dataValue, out fv)) node.Y = fv;
                                break;
                            case "na2": // graphics.dimension.height
                                if (float.TryParse(dataValue, out fv)) node.Height = fv;
                                break;
                            case "na3": // graphics.dimension.width
                                if (float.TryParse(dataValue, out fv)) node.Width = fv;
                                break;
                            case "na31": // labelgraphics.text
                                node.Label = dataValue;
                                break;
                            case "na35": // tooltip (used as key in this project)
                                node.Key = dataValue;
                                break;
                        }
                        break;
                    }
                }
            }

            if (this.xmlRdr.NodeType == XmlNodeType.EndElement && this.xmlRdr.Name == "data") {
                return;
            }
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
    }
}
