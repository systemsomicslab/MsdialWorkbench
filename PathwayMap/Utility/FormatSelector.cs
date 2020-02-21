using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.Pathwaymap.Parser {
    public class FormatSelector {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Edge> Edges { get; set; } = new List<Edge>(); 
        public void ReadPathwayData(string file) {
            var extension = System.IO.Path.GetExtension(file);
            switch (extension) {
                case ".graphml":
                    var vantedReader = new VantedFormatParser();
                    vantedReader.Read(file);
                    this.Nodes = vantedReader.nodes;
                    this.Edges = vantedReader.edges;
                    break;
                case ".gpml":
                    var wikipathwayReader = new WikipathwayFormatParser();
                    wikipathwayReader.Read(file);
                    this.Nodes = wikipathwayReader.nodes;
                    this.Edges = wikipathwayReader.edges;
                    break;
                default:
                    break;
            }
        }

        public void ReadPathwayData(Stream file, string extension) { // should be wikipathway but now, vanted format parser is used for test
            if (extension == ".graphml") {
                var vantedReader = new VantedFormatParser();
                vantedReader.Read(file);
                this.Nodes = vantedReader.nodes;
                this.Edges = vantedReader.edges;
            } else if (extension == ".gpml") {
                var wikipathwayReader = new WikipathwayFormatParser();
                wikipathwayReader.Read(file);
                this.Nodes = wikipathwayReader.nodes;
                this.Edges = wikipathwayReader.edges;
            }
        }
    }
}
