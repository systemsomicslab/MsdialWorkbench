using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Riken.Metabolomics.KeggApi.Parser {

    public class KgmlField {
        public string EntryID { get; set; }
        public string EntryName { get; set; }
        public string EntryType { get; set; }
        public string EntryReaction { get; set; }
        public string EntryLink { get; set; }
        public string GraphicsName { get; set; }
        public string GraphicsFgcolor { get; set; }
        public string GraphicsBgcolor { get; set; }
        public string GraphicsType { get; set; }
        public string GraphicsX { get; set; }
        public string GraphicsY { get; set; }
        public string GraphicsWidth { get; set; }
        public string GraphicsHeight { get; set; }
    }

    public class KgmlParser {

        private List<KgmlField> kgmlFields = null;
        private XmlReader xmlRdr = null;

        public List<KgmlField> KgmlReader(string input) {

            this.kgmlFields = new List<KgmlField>();

            using (var fs = new FileStream(input, FileMode.Open, FileAccess.Read)) {
                using (var xmlRdr = new XmlTextReader(fs)) {
                    this.xmlRdr = xmlRdr;
                    while (xmlRdr.Read()) {
                        if (xmlRdr.NodeType == XmlNodeType.Element) {
                            switch (xmlRdr.Name) {
                                case "entry": entryParser(); continue; // 
                            }
                        }
                    }
                }
            }
            return this.kgmlFields;
        }

        public void MergeKgmlFiles(string inputfolder, string output) {

            var masterfields = new List<KgmlField>();
            var masterqueries = new List<string>();
            var files = System.IO.Directory.GetFiles(inputfolder);
            foreach (var file in files) {
                var fields = new KgmlParser().KgmlReader(file);
                foreach (var field in fields) {
                    if (field.EntryName == "undefined") continue;
                    if (field.EntryType == "gene" || field.EntryType == "compound" || field.EntryType == "ortholog") {
                        if (!masterqueries.Contains(field.GraphicsName)) {
                            masterfields.Add(field);
                            masterqueries.Add(field.GraphicsName);
                        }
                    }
                }
            }

            masterfields = masterfields.OrderBy(n => n.GraphicsName).ToList();
            WriteKgmlAsText(output, masterfields);
        }

        public void WriteKgmlAsText(string output, List<KgmlField> fields) {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //sw.WriteLine("Entry ID\tEntry name\tEntry type\tEntry reaction\tEntry link\tGraphics name\tGraphics fgcolor\tGraphics bgcolor\tGraphics type\tGraphics x\tGraphics y\tGraphics width\tGraphics height");
                //foreach (var field in fields) {
                //    sw.WriteLine(field.EntryID + "\t" + field.EntryName + "\t" + field.EntryType + "\t" + field.EntryReaction + "\t" + field.EntryLink + "\t" +
                //        field.GraphicsName + "\t" + field.GraphicsFgcolor + "\t" + field.GraphicsBgcolor + "\t" + field.GraphicsType + "\t" + field.GraphicsX + "\t" +
                //        field.GraphicsY + "\t" + field.GraphicsWidth + "\t" + field.GraphicsHeight);
                //}
                sw.WriteLine("Entry name\tGraphics name\tType");
                foreach (var field in fields) {
                    sw.WriteLine(field.EntryName + "\t" + field.GraphicsName + "\t" + field.EntryType);
                }
            }
        }

        private void entryParser() {
            var field = new KgmlField();
            while (this.xmlRdr.MoveToNextAttribute()) {
                switch (this.xmlRdr.Name) {
                    case "id": field.EntryID = this.xmlRdr.Value; break;
                    case "name": field.EntryName = this.xmlRdr.Value; break;
                    case "type": field.EntryType = this.xmlRdr.Value; break;
                    case "reaction": field.EntryReaction = this.xmlRdr.Value; break;
                    case "link": field.EntryLink = this.xmlRdr.Value; break;
                }
            }

            while (this.xmlRdr.Read()) {
                if (this.xmlRdr.NodeType == XmlNodeType.Element) {
                    switch (this.xmlRdr.Name) {
                        case "graphics":
                            while (this.xmlRdr.MoveToNextAttribute()) {
                                switch (this.xmlRdr.Name) {
                                    case "name": field.GraphicsName = this.xmlRdr.Value; break;
                                    case "fgcolor": field.GraphicsFgcolor = this.xmlRdr.Value; break;
                                    case "bgcolor": field.GraphicsBgcolor = this.xmlRdr.Value; break;
                                    case "type": field.GraphicsType = this.xmlRdr.Value; break;
                                    case "x": field.GraphicsX = this.xmlRdr.Value; break;
                                    case "y": field.GraphicsY = this.xmlRdr.Value; break;
                                    case "width": field.GraphicsWidth = this.xmlRdr.Value; break;
                                    case "height": field.GraphicsHeight = this.xmlRdr.Value; break;
                                }
                            }
                            break; // 
                    }
                }
                else if (this.xmlRdr.NodeType == XmlNodeType.EndElement) {
                    switch (this.xmlRdr.Name) {
                        case "entry": this.kgmlFields.Add(field); break;
                        case "graphics": return;
                    }
                    break;
                }
            }
        }


        public void MergeKgmlFieldAndOwnTextData(string kgmlfile, string owntext, string output) {
            var keggmapFields = new List<string[]>();
            using (var sr = new StreamReader(kgmlfile, Encoding.ASCII)) {
                sr.ReadLine();
                while(sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    keggmapFields.Add(line.Split('\t'));
                }
            }

            var ownFields = new List<string[]>();
            using (var sr = new StreamReader(owntext, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    ownFields.Add(line.Split('\t'));
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                var total = keggmapFields.Count;
                var counter = 0;
                foreach (var kField in keggmapFields) {
                    var entryname = kField[0];
                    var graphicsname = kField[1];
                    var typeid = kField[2];

                    switch (typeid) {
                        case "gene":
                            foreach (var oField in ownFields) {
                                var geneid = oField[1];
                                if (entryname.Contains(geneid)) {
                                    sw.WriteLine(entryname + "\t" + graphicsname + "\t" + typeid + "\t" + String.Join("\t", oField));
                                    break;
                                }
                            }

                            break;
                        case "compound":

                            foreach (var oField in ownFields) {
                                var compoundId = oField[1];
                                if (graphicsname.Contains(compoundId)) {
                                    sw.WriteLine(entryname + "\t" + graphicsname + "\t" + typeid + "\t" + String.Join("\t", oField));
                                    break;
                                }
                            }

                            break;
                        case "ortholog":

                            foreach (var oField in ownFields) {
                                var orthologId = oField[1];
                                if (graphicsname.Contains(orthologId)) {
                                    sw.WriteLine(entryname + "\t" + graphicsname + "\t" + typeid + "\t" + String.Join("\t", oField));
                                    break;
                                }
                            }

                            break;
                    }


                    counter++;
                    Console.Write("Done {0}", counter + "/" + total);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }
            }

        }
    }
}
