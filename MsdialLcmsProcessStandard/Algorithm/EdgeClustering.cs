using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm.Clustering
{
    public class EdgeInformation {
        public string SourceComment { get; set; }
        public string TargetComment { get; set; }
        public string SourceName { get; set; }
        public string TargetName { get; set; }
        public int SourceID { get; set; }
        public int TargetID { get; set; }
        public double Score { get; set; }
        public string Comment { get; set; }
    }

    public sealed class EgdeGenerator {
        private EgdeGenerator() { }

        public static void GenerateEdgesFromMsp(string iuputMsp, string biotransformfile, float mzTol, string output) {
            var mspQueries = MspFileParcer.MspFileReader(iuputMsp);
            changeCommentField(mspQueries);
            var formulaEdges = FormulaClustering.GetEdgeInformations(mspQueries, biotransformfile);
            var msmsEdges = MsmsClustering.GetEdgeInformations(mspQueries, 1, mzTol);
            var ontologyEdges = OntologyClustering.GetEdgeInformations(mspQueries);

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                sw.WriteLine("source\ttarget\tscore\tsource name\ttarget name\tComment");
                foreach (var edge in formulaEdges) {
                    sw.WriteLine(edge.SourceComment + "\t" + edge.TargetComment + "\t" + edge.Score + "\t" + edge.Comment);
                }

                foreach (var edge in msmsEdges) {
                    sw.WriteLine(edge.SourceComment + "\t" + edge.TargetComment + "\t" + edge.Score + "\t" + edge.Comment);
                }

                foreach (var edge in ontologyEdges) {
                    sw.WriteLine(edge.SourceComment + "\t" + edge.TargetComment + "\t" + edge.Score + "\t" + edge.Comment);
                }
            }
        }

        private static void changeCommentField(List<MspFormatCompoundInformationBean> mspQueries) {
           foreach (var query in mspQueries) {
                var comment = query.Comment;
                var commentArray = comment.Split(';');
                var plasmaIdField = commentArray[1];
                var plasmaID = plasmaIdField.Split('-')[1];
                query.Comment = plasmaID;
            }
        }

        public static void GenerateEdgesFromMsp(string inputMsp, float mzTol, string output) {
            var mspQueries = MspFileParcer.MspFileReader(inputMsp);
            var msmsEdges = MsmsClustering.GetEdgeInformations(mspQueries, 1, mzTol);

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //sw.WriteLine("source\ttarget\tscore\tsource name\ttarget name\tComment");
                sw.WriteLine("source\ttarget\tscore");
                foreach (var edge in msmsEdges) {
                    //sw.WriteLine(edge.Source + "\t" + edge.Target + "\t" + edge.Score
                    //                + "\t" + edge.SourceName + "\t" + edge.TargetName + "\t" + edge.Comment);
                    sw.WriteLine(edge.SourceName + "\t" + edge.TargetName + "\t" + edge.Score);
                }
            }
        }

        public static List<EdgeInformation> GetEdges(string input, double mzTol) {
            var mspQueries = MspFileParcer.MspFileReader(input);
            var msmsEdges = MsmsClustering.GetEdgeInformations(mspQueries, 1, mzTol);

            return msmsEdges;
        }

        public static void GenerateEdgesForCircusPlotFromText(string input, string output) {
            var nodes = new List<NodeData>();
            var counter = 0;
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var lineArray = line.Split('\t');
                    var node = new NodeData() {
                        id = counter,
                        segment = int.Parse(lineArray[0]),
                        position = int.Parse(lineArray[1]),
                        Name = lineArray[2],
                        Property = lineArray[3],
                        Comment = lineArray[4],
                        Ontology = lineArray[5]
                    };
                    nodes.Add(node);
                    counter++;
                }
            }

            exportLinkfiles(1, output, nodes);
            exportLinkfiles(2, output, nodes);
            exportLinkfiles(3, output, nodes);
            exportLinkfiles(4, output, nodes);
            exportLinkfiles(5, output, nodes);
            exportLinkfiles(6, output, nodes);
            exportLinkfiles(7, output, nodes);
            exportLinkfiles(8, output, nodes);
            exportLinkfiles(9, output, nodes);
            exportLinkfiles(10, output, nodes);
            exportLinkfiles(11, output, nodes);
            exportLinkfiles(12, output, nodes);



            var filepath = Path.Combine(System.IO.Path.GetDirectoryName(output), System.IO.Path.GetFileNameWithoutExtension(output) + "_indole.txt");
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                var donelist = new List<string>();
                foreach (var node1 in nodes) {
                    if (node1.Comment != "Yes") continue;
                    foreach (var node2 in nodes) {
                        if (node2.Comment != "Yes") continue;
                        var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                        if (!donelist.Contains(key)) {
                            donelist.Add(key);
                            if (node1.segment == node2.segment && node1.position == node2.position) continue;
                            if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                            sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.position + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.position);
                        }
                    }
                }
            }
        }

        private static void exportLinkfiles(int segment, string output, List<NodeData> nodes) {

            var filepath = Path.Combine(System.IO.Path.GetDirectoryName(output), System.IO.Path.GetFileNameWithoutExtension(output) + "_" + segment + ".txt");

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                var donelist = new List<string>();
                foreach (var node1 in nodes) {
                    foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                        var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                        if (!donelist.Contains(key)) {
                            //donelist.Add(key);
                            if (node1.segment != segment) continue;
                            if (node1.segment == node2.segment && node1.position == node2.position) continue;
                            if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                            sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                        }
                    }
                }
            }

            if (segment == 1) {
                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_aminoacids.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Amino acids")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_anthraquinones.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Anthraquinones")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_carbolines.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Carbolines")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_flavoneglycosides.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Flavone O-glycosides")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_flavonolglycosides.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Flavonol O-glycosides")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_iridoidglycosides.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Iridoid glycosides")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_isoflavoneglycosides.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Isoflavone O-glycosides")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_others.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Others")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_lipids.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Lipids")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_organicacids.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Organic acids")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_quinicacids.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Quinic acid and derivatives")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

                filepath = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "_terpeneglycosides.txt";
                using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                    sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                    var donelist = new List<string>();
                    foreach (var node1 in nodes.Where(n => n.Ontology == "Terpene glycosides")) {
                        foreach (var node2 in nodes.Where(n => n.Ontology == node1.Ontology)) {
                            var key = Math.Min(node1.id, node2.id) + "_" + Math.Max(node1.id, node2.id);
                            if (!donelist.Contains(key)) {
                                //donelist.Add(key);
                                if (node1.segment != segment) continue;
                                if (node1.segment == node2.segment && node1.position == node2.position) continue;
                                if (double.Parse(node1.Property) < 4.0 || double.Parse(node2.Property) < 4.0) continue;
                                sw.WriteLine(node1.segment + "\t" + node1.position + "\t" + node1.Ontology + "\t" + node2.segment + "\t" + node2.position + "\t" + node2.Ontology);
                            }
                        }
                    }
                }

            }
        }

        public static void GenerateOntologyOrientedEdgesFromText(string inputTxt, string outputNodes, string outputEdges) {

            var mspQueries = new List<MspFormatCompoundInformationBean>();
            using (var sr = new StreamReader(inputTxt, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var lineArray = line.Split('\t');

                    var query = new MspFormatCompoundInformationBean();
                    query.Name = lineArray[3].Split('_')[0]; //super class
                    query.Comment = lineArray[0];
                    query.Ontology = lineArray[3].Split('_')[1]; //direct parent
                    query.Links = lineArray[2];
                    mspQueries.Add(query);
                }
            }

            var ontoloDict = new Dictionary<string, string>();
            var dictFalse = new Dictionary<string, int>();
            var dictTrue = new Dictionary<string, int>();
            foreach (var query in mspQueries) {
                if (!ontoloDict.ContainsKey(query.Ontology))
                    ontoloDict[query.Ontology] = query.Name;

                if (query.Links == "FALSE") {
                    if (dictFalse.ContainsKey(query.Ontology))
                        dictFalse[query.Ontology]++;
                    else
                        dictFalse[query.Ontology] = 1;
                } else if (query.Links == "TRUE") {
                    if (dictTrue.ContainsKey(query.Ontology))
                        dictTrue[query.Ontology]++;
                    else
                        dictTrue[query.Ontology] = 1;
                }
            }

            var newMspQueries = new List<MspFormatCompoundInformationBean>();
            var counter = 94;
            foreach (var ontology in ontoloDict) {

                var directParent = ontology.Key;
                var superClass = ontology.Value;

                var query = new MspFormatCompoundInformationBean();
                query.Name = directParent;
                query.Comment = counter.ToString();
                query.Ontology = superClass;
                query.Id = 0;
                query.BinId = 0;

                if (dictTrue.ContainsKey(directParent))
                    query.BinId = dictTrue[directParent];
                if (dictFalse.ContainsKey(directParent))
                    query.Id = dictFalse[directParent];

                if (query.Id + query.BinId >= 100) {
                    newMspQueries.Add(query);
                    counter++;
                }
            }

            var ontologyEdges = OntologyClustering.GetEdgeInformations(newMspQueries);
            using (var sw = new StreamWriter(outputEdges, false, Encoding.ASCII)) {

                sw.WriteLine("source\ttarget\tscore\tsource name\ttarget name\tComment");
                foreach (var edge in ontologyEdges) {
                    sw.WriteLine(edge.SourceComment + "\t" + edge.TargetComment + "\t" + edge.Score
                                    + "\t" + edge.SourceName + "\t" + edge.TargetName + "\t" + edge.Comment);
                }
            }

            using (var sw = new StreamWriter(outputNodes, false, Encoding.ASCII)) {
                sw.WriteLine("ID\tSuper class\tDirect parent\tMSMS true\tMSMS false");
                foreach (var query in newMspQueries) {
                    sw.WriteLine(query.Comment + "\t" + query.Ontology + "\t" + query.Name + "\t" + query.BinId + "\t" + query.Id);
                }
            }
        }
    }

    public class MetaboliteInfoTemp {
        public string SegID { get; set; }
        public string PosID { get; set; }
        public string Name { get; set; }
        public List<double> Ions { get; set; }

        public MetaboliteInfoTemp() { Ions = new List<double>(); }
    }

    public sealed class CorrelationClustering {
        private CorrelationClustering() { }

        public static List<EdgeInformation> GetEdgeInformations(List<AlignmentPropertyBean> alignedSpots,
            double similarityCutoff, double rtTolerance, double targetRt,
            BackgroundWorker bgWorker, double maxCoeff = 100) {

            var initialValue = 0;
            if (maxCoeff == 50) initialValue = 50;

            var totalCount = (alignedSpots.Count - 1) * (alignedSpots.Count - 1) * 0.5;
            var counter = 0;
            var edges = new List<EdgeInformation>();

            var OnePercentVal = (int)(totalCount * maxCoeff / 10000);

            for (int i = 0; i < alignedSpots.Count; i++) {
                if (Math.Abs(alignedSpots[i].CentralRetentionTime - targetRt) > rtTolerance && targetRt > 0) continue;

                for (int j = i + 1; j < alignedSpots.Count; j++) {

                    if (Math.Abs(alignedSpots[j].CentralRetentionTime - targetRt) > rtTolerance && targetRt > 0) continue;
                    var score = IonAbundancCorrelation(alignedSpots[i], alignedSpots[j]);
                    if (score >= similarityCutoff * 0.01) {

                        var pairKey = Math.Min(i, j) + "-" + Math.Max(i, j);

                        var sourceID = Math.Min(i, j);
                        var targetID = Math.Max(i, j);

                        var edge = new EdgeInformation() {
                            SourceComment = alignedSpots[sourceID].AlignmentID.ToString(),
                            TargetComment = alignedSpots[targetID].AlignmentID.ToString(),
                            Score = Math.Round(score, 3),
                            Comment = "Ion correlation similarity",
                            SourceName = alignedSpots[sourceID].AlignmentID.ToString(),
                            TargetName = alignedSpots[targetID].AlignmentID.ToString()
                        };
                        edges.Add(edge);
                    }
                    counter++;

                    if (counter % OnePercentVal == 0) {
                        var progress = (double)counter / (double)totalCount * maxCoeff;
                        bgWorker.ReportProgress((int)(initialValue + progress));
                    }
                }
            }

            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(AlignmentPropertyBean targetSpot, List<AlignmentPropertyBean> alignedSpots,
            double similarityCutoff, double rtTolerance, double targetRt,
            BackgroundWorker bgWorker, double maxCoeff = 1.0) {

            var initialValue = 0;
            if (maxCoeff == 0.5) initialValue = 50;

            var totalCount = alignedSpots.Count - 1;
            var counter = 0;
            var edges = new List<EdgeInformation>();

            var OnePercentVal = (int)(totalCount * maxCoeff / 100);
           
            for (int i = 0; i < alignedSpots.Count; i++) {
                if (Math.Abs(alignedSpots[i].CentralRetentionTime - targetRt) > rtTolerance && targetRt > 0) continue;
                if (alignedSpots[i].AlignmentID == targetSpot.AlignmentID) continue;
                var score = IonAbundancCorrelation(alignedSpots[i], targetSpot);
                if (score >= similarityCutoff * 0.01) {

                    var edge = new EdgeInformation() {
                        SourceComment = targetSpot.AlignmentID.ToString(),
                        TargetComment = alignedSpots[i].AlignmentID.ToString(),
                        Score = Math.Round(score, 3),
                        Comment = "Ion correlation similarity",
                        SourceName = targetSpot.AlignmentID.ToString(),
                        TargetName = alignedSpots[i].AlignmentID.ToString()
                    };
                    edges.Add(edge);
                }
                counter++;

                if (counter % OnePercentVal == 0) {
                    var progress = (double)counter / (double)totalCount * 100.0 * maxCoeff;
                    bgWorker.ReportProgress((int)(initialValue + progress));
                }
            }

            return edges;
        }

        /// <summary>
        /// text file: [0] seg ID, [1] position, [2] name, [3] - [n] intensities
        /// </summary>
        public static void TextFormatFileToEdgeList(string txtfilepath, string output, double cutoff) {

            var metTemps = new List<MetaboliteInfoTemp>();
            using (StreamReader sr = new StreamReader(txtfilepath, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;

                    var lineArray = line.Split('\t');
                    var seg = lineArray[0];
                    var pos = lineArray[1];
                    var name = lineArray[2];
                    var ions = new List<double>();
                    for (int i = 3; i < lineArray.Length; i++) {
                        double ion;
                        if (double.TryParse(lineArray[i], out ion)) {
                            ions.Add(ion);
                        }
                        else {
                            ions.Add(0);
                        }
                    }

                    metTemps.Add(new MetaboliteInfoTemp() {
                        SegID = seg,
                        PosID = pos,
                        Name = name,
                        Ions = ions
                    });
                }
            }

            var totalCount = (metTemps.Count - 1) * (metTemps.Count - 1) * 0.5;
            var counter = 0;
            var edges = new List<EdgeInformation>();

            for (int i = 0; i < metTemps.Count; i++) {

                for (int j = i + 1; j < metTemps.Count; j++) {

                    var score = IonAbundancCorrelation(metTemps[i], metTemps[j]);
                    if (score >= cutoff * 0.01) {

                        var sourceID = Math.Min(i, j);
                        var targetID = Math.Max(i, j);

                        var edge = new EdgeInformation() {
                            SourceComment = sourceID.ToString(),
                            TargetComment = targetID.ToString(),
                            Score = Math.Round(score, 3),
                            Comment = "Ion correlation similarity",
                        };
                        edges.Add(edge);
                    }
                    counter++;

                    Console.WriteLine("Finished {0} / {1}", counter, totalCount);
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                sw.WriteLine("seg1\tpos1\tname1\tseg2\tpos2\tname2");
                foreach (var edge in edges) {

                    var sourceID = int.Parse(edge.SourceComment);
                    var targetID = int.Parse(edge.TargetComment);

                    sw.WriteLine(metTemps[sourceID].SegID + "\t" + metTemps[sourceID].PosID + "\t" + metTemps[sourceID].Name
                                    + "\t" + metTemps[targetID].SegID + "\t" + metTemps[targetID].PosID + "\t" + metTemps[targetID].Name);
                }
            }
        }

        private static double IonAbundancCorrelation(AlignmentPropertyBean spot1, AlignmentPropertyBean spot2) {

            var variables1 = spot1.AlignedPeakPropertyBeanCollection.Select(n => n.Variable).ToArray();
            var variables2 = spot2.AlignedPeakPropertyBeanCollection.Select(n => n.Variable).ToArray();

            return BasicMathematics.Coefficient(variables1, variables2);
        }

        private static double IonAbundancCorrelation(MetaboliteInfoTemp spot1, MetaboliteInfoTemp spot2) {

            var variables1 = spot1.Ions.ToArray();
            var variables2 = spot2.Ions.ToArray();

            return BasicMathematics.Coefficient(variables1, variables2);
        }
    }

    public sealed class MsmsClustering
    {
        private MsmsClustering() { }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries, 
            double relativeCutoff, double masstol, double similarityCutoff, BackgroundWorker bgWorker, double maxProgress = 100, double initialValue = 0.0) {

            var totalCount = (mspQueries.Count - 1) * (mspQueries.Count - 1) * 0.5;
            var counter = 0;
            var edges = new List<EdgeInformation>();

            var reportedProgress = 0.0;

            for (int i = 0; i < mspQueries.Count; i++) {
                if (mspQueries[i].PeakNumber < 2) {
                    counter += mspQueries.Count - 1 - i;
                    continue;
                }
                for (int j = i + 1; j < mspQueries.Count; j++) {
                    if (mspQueries[j].PeakNumber < 2) {
                        counter++;
                        continue;
                    }

                    var msmsClusterScore = MsmsSimilarityScore(mspQueries[i], mspQueries[j], relativeCutoff, masstol);
                    if (msmsClusterScore >= similarityCutoff * 0.01) {

                        var pairKey = Math.Min(i, j) + "-" + Math.Max(i, j);

                        var sourceID = Math.Min(i, j);
                        var targetID = Math.Max(i, j);

                        var edge = new EdgeInformation() {
                            SourceComment = mspQueries[sourceID].Comment,
                            SourceName = mspQueries[sourceID].Name,
                            SourceID = mspQueries[sourceID].Id,
                            TargetComment = mspQueries[targetID].Comment,
                            TargetName = mspQueries[targetID].Name,
                            TargetID = mspQueries[targetID].Id,
                            Comment = "MS/MS similarity",
                            Score = Math.Round(msmsClusterScore, 3)
                        };
                        edges.Add(edge);
                    }
                    counter++;

                    var progress = initialValue + (double)counter / (double)totalCount * maxProgress;
                    if (progress - reportedProgress > 1) {
                        bgWorker.ReportProgress((int)progress);
                    }
                }
            }
            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(MspFormatCompoundInformationBean targetQuery, List<MspFormatCompoundInformationBean> mspQueries,
            double relativeCutoff, double masstol, double similarityCutoff, BackgroundWorker bgWorker, double maxProgress = 100, double initialValue = 0.0) {

            var totalCount = mspQueries.Count - 1;
            var counter = 0;
            var edges = new List<EdgeInformation>();

            var reportedProgress = 0.0;

            for (int i = 0; i < mspQueries.Count; i++) {
                if (mspQueries[i].Comment == targetQuery.Comment.ToString()) continue;
                if (mspQueries[i].PeakNumber < 2) {
                    counter++;
                    continue;
                }

                var msmsClusterScore = MsmsSimilarityScore(mspQueries[i], targetQuery, relativeCutoff, masstol);
                if (msmsClusterScore >= similarityCutoff * 0.01) {

                    var edge = new EdgeInformation() {
                        SourceComment = targetQuery.Comment,
                        SourceName = targetQuery.Name,
                        SourceID = targetQuery.Id,
                        TargetComment = mspQueries[i].Comment,
                        TargetName = mspQueries[i].Name,
                        TargetID = mspQueries[i].Id,
                        Score = Math.Round(msmsClusterScore, 3),
                        Comment = "MS/MS similarity"
                    };
                    edges.Add(edge);
                }
                counter++;

                var progress = initialValue + (double)counter / (double)totalCount * maxProgress;
                if (progress - reportedProgress > 1) {
                    bgWorker.ReportProgress((int)progress);
                }
            }
            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries, double cutoff, double masstol) {

            var totalCount = (mspQueries.Count - 1) * (mspQueries.Count - 1) * 0.5;
            var counter = 0;
            var edges = new List<EdgeInformation>();

            for (int i = 0; i < mspQueries.Count; i++) {
                if (mspQueries[i].PeakNumber < 2) {
                    counter += mspQueries.Count - 1 - i;
                    continue;
                }
                for (int j = i + 1; j < mspQueries.Count; j++) {
                    if (mspQueries[j].PeakNumber < 2) {
                        counter++;
                        continue;
                    }

                    var msmsClusterScore = MsmsSimilarityScore(mspQueries[i], mspQueries[j], cutoff, masstol);
                    if (msmsClusterScore >= 0.75) {

                        var pairKey = Math.Min(i, j) + "-" + Math.Max(i, j);

                        var sourceID = Math.Min(i, j);
                        var targetID = Math.Max(i, j);

                        var edge = new EdgeInformation() {
                            SourceComment = mspQueries[sourceID].Comment,
                            TargetComment = mspQueries[targetID].Comment,
                            Score = Math.Round(msmsClusterScore, 3),
                            Comment = "MS/MS similarity",
                            SourceName = mspQueries[sourceID].Name,
                            TargetName = mspQueries[targetID].Name
                        };
                        edges.Add(edge);
                    }
                    counter++;
                    Console.WriteLine("Finished {0} / {1}", counter, totalCount);
                }
            }

            return edges;
        }

        public static double calculationTest(string inputA, string inputB) {
            var mspQueryA = MspFileParcer.MspFileReader(inputA);
            var mspQueryB = MspFileParcer.MspFileReader(inputB);

            var msmsClusterScore = MsmsSimilarityScore(mspQueryA[0], mspQueryB[0], 1, 0.025);

            return msmsClusterScore;
        }

        public static double MsmsSimilarityScore(MspFormatCompoundInformationBean spectrumA, MspFormatCompoundInformationBean spectrumB, 
            double cutoff, double masstol) {
            var iPrecursor = spectrumA.PrecursorMz;
            var iPeaks = MspFileParcer.ConvertToPeakObject(spectrumA.MzIntensityCommentBeanList);

            var jPrecursor = spectrumB.PrecursorMz;
            var jPeaks = MspFileParcer.ConvertToPeakObject(spectrumB.MzIntensityCommentBeanList);

            var msmsClusterScore = 0.0;
            if (iPrecursor < jPrecursor)
                msmsClusterScore = SpectralSimilarity.GetMsmsClusterSimilarityScore(iPeaks, iPrecursor, jPeaks, jPrecursor, cutoff, masstol, MassToleranceType.Da);
            else
                msmsClusterScore = SpectralSimilarity.GetMsmsClusterSimilarityScore(jPeaks, jPrecursor, iPeaks, iPrecursor, cutoff, masstol, MassToleranceType.Da);

            return msmsClusterScore;
        }

        public static void Run(string inputMsp, string output)
        {
            var mspQueries = MspFileParcer.MspFileReader(inputMsp);

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                //write header
                sw.WriteLine("MSMS clustering score matrix");
                sw.WriteLine("source\ttarget\tscore\tsource name\ttarget name");
               
                var totalCount = mspQueries.Count * mspQueries.Count;
                var counter = 0;
                var pairDone = new List<string>();
                for (int i = 0; i < mspQueries.Count; i++) {
                    if (mspQueries[i].PeakNumber == 0) continue;
                    for (int j = i + 1; j < mspQueries.Count; j++) {
                        if (i == j) continue;
                        if (mspQueries[j].PeakNumber == 0) continue;

                        var iPrecursor = mspQueries[i].PrecursorMz;
                        var iPeaks = MspFileParcer.ConvertToPeakObject(mspQueries[i].MzIntensityCommentBeanList);

                        var jPrecursor = mspQueries[j].PrecursorMz;
                        var jPeaks = MspFileParcer.ConvertToPeakObject(mspQueries[j].MzIntensityCommentBeanList);

                        var msmsClusterScore = 0.0;
                        var precursorDiff = Math.Abs(iPrecursor - jPrecursor);
                        if (Math.Round(precursorDiff, 0) % 2 == 1) continue; //if precursor diff is odd value, it means that the origins of MS/MS spectra should be different.

                        if (iPrecursor < jPrecursor)
                            msmsClusterScore = SpectralSimilarity.GetMsmsClusterSimilarityScore(iPeaks, iPrecursor, jPeaks, jPrecursor, 2.0, 1, 0.01, MassToleranceType.Da);
                        else
                            msmsClusterScore = SpectralSimilarity.GetMsmsClusterSimilarityScore(jPeaks, jPrecursor, iPeaks, iPrecursor, 2.0, 1, 0.01, MassToleranceType.Da);

                        if (msmsClusterScore >= 0.9) {

                            var pairKey = Math.Min(i, j) + "-" + Math.Max(i, j);
                            if (!pairDone.Contains(pairKey)) {

                                var sourceID = Math.Min(i, j);
                                var targetID = Math.Max(i, j);

                                sw.WriteLine(mspQueries[sourceID].Comment + "\t" + mspQueries[targetID].Comment + "\t" + Math.Round(msmsClusterScore, 3)
                                    + "\t" + mspQueries[sourceID].Name + "\t" + mspQueries[targetID].Name);
                                pairDone.Add(pairKey);
                            }

                        }

                        counter++;
                    }
                }
            }
        }
    }

    public class BioTransformQuery {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FormulaDiff { get; set; }
        public double MassDiff { get; set; }
        public string ElutionBehavior { get; set; }
    }

    public sealed class FormulaClustering {

        private FormulaClustering() { }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries, string biotransformFile) {
            var biotranforms = readBiotransformQueries(biotransformFile);

            var totalCount = (mspQueries.Count - 1) * (mspQueries.Count - 1) * 0.5;
            var counter = 0;
            var pairDone = new List<string>();
            var edges = new List<EdgeInformation>();

            for (int i = 0; i < mspQueries.Count; i++) {

                var sourceQuery = mspQueries[i];
                if (sourceQuery.Formula == null || sourceQuery.Formula == string.Empty || 
                    sourceQuery.Formula.Contains("||") || sourceQuery.Formula == "Unknown") continue;
                for (int j = i + 1; j < mspQueries.Count; j++) {

                    //if (i == j) continue;
                    var targetQuery = mspQueries[j];
                    if (targetQuery.Formula == null || targetQuery.Formula == string.Empty ||
                        targetQuery.Formula.Contains("||") || targetQuery.Formula == "Unknown") continue;

                    foreach (var query in biotranforms) {
                        if (isBioTransformed(sourceQuery, targetQuery, query)) {
                            //var pairKey = Math.Min(i, j) + "-" + Math.Max(i, j);
                            //if (!pairDone.Contains(pairKey)) {

                                var sourceID = Math.Min(i, j);
                                var targetID = Math.Max(i, j);

                                var edge = new EdgeInformation() {
                                    SourceComment = mspQueries[sourceID].Comment,
                                    TargetComment = mspQueries[targetID].Comment,
                                    Score = 1,
                                    Comment = query.Name,
                                    SourceName = mspQueries[sourceID].Name,
                                    TargetName = mspQueries[targetID].Name
                                };
                                edges.Add(edge);
                            //    pairDone.Add(pairKey);
                            //}
                            break;
                        }
                    }
                    counter++;
                }
            }

            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries, 
            string biotransformFile, double maxProgress, double initialValue, BackgroundWorker bgWorker) {
            var biotranforms = readBiotransformQueries(biotransformFile);

            var totalCount = (mspQueries.Count - 1) * (mspQueries.Count - 1) * 0.5;
            var counter = 0;
            var reportedProgress = 0.0;
            var edges = new List<EdgeInformation>();

            for (int i = 0; i < mspQueries.Count; i++) {

                var sourceQuery = mspQueries[i];
                if (sourceQuery.Formula == null || sourceQuery.Formula == string.Empty ||
                    sourceQuery.Formula.Contains("||") || sourceQuery.Formula == "Unknown") continue;
                for (int j = i + 1; j < mspQueries.Count; j++) {

                    var targetQuery = mspQueries[j];
                    if (targetQuery.Formula == null || targetQuery.Formula == string.Empty ||
                        targetQuery.Formula.Contains("||") || targetQuery.Formula == "Unknown") continue;

                    foreach (var query in biotranforms) {
                        if (isBioTransformed(sourceQuery, targetQuery, query)) {

                            var sourceID = Math.Min(i, j);
                            var targetID = Math.Max(i, j);

                            var edge = new EdgeInformation() {
                                SourceName = mspQueries[sourceID].Name,
                                SourceComment = mspQueries[sourceID].Comment,
                                SourceID = mspQueries[sourceID].Id,
                                TargetName = mspQueries[targetID].Name,
                                TargetComment = mspQueries[targetID].Comment,
                                TargetID = mspQueries[targetID].Id,
                                Comment = query.Name,
                                Score = 1,
                            };
                            edges.Add(edge);
                            break;
                        }
                    }
                    counter++;
                    var progress = initialValue + (double)counter / (double)totalCount * maxProgress;
                    if (progress - reportedProgress > 1) {
                        bgWorker.ReportProgress((int)progress);
                    }
                }
            }

            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(MspFormatCompoundInformationBean targetQuery, List<MspFormatCompoundInformationBean> mspQueries,
          string biotransformFile, double maxProgress, double initialValue, BackgroundWorker bgWorker) {

            if (targetQuery.Formula == null || targetQuery.Formula == string.Empty ||
                        targetQuery.Formula.Contains("||") || targetQuery.Formula == "Unknown") return new List<EdgeInformation>();

            var biotranforms = readBiotransformQueries(biotransformFile);

            var totalCount = mspQueries.Count - 1;
            var counter = 0;
            var reportedProgress = 0.0;
            var edges = new List<EdgeInformation>();

            for (int i = 0; i < mspQueries.Count; i++) {

                var sourceQuery = mspQueries[i];
                if (sourceQuery.Formula == null || sourceQuery.Formula == string.Empty ||
                    sourceQuery.Formula.Contains("||") || sourceQuery.Formula == "Unknown") continue;

                foreach (var query in biotranforms) {
                    if (isBioTransformed(sourceQuery, targetQuery, query)) {

                        var edge = new EdgeInformation() {
                            SourceComment = targetQuery.Comment,
                            SourceName = targetQuery.Name,
                            SourceID = targetQuery.Id,
                            TargetComment = mspQueries[i].Comment,
                            TargetName = mspQueries[i].Name,
                            TargetID = mspQueries[i].Id,
                            Score = 1,
                            Comment = query.Name
                        };
                        edges.Add(edge);
                        break;
                    }
                }
                counter++;
                var progress = initialValue + (double)counter / (double)totalCount * maxProgress;
                if (progress - reportedProgress > 1) {
                    bgWorker.ReportProgress((int)progress);
                }
            }

            return edges;
        }

        private static bool isBioTransformed(MspFormatCompoundInformationBean sourceQuery, 
            MspFormatCompoundInformationBean targetQuery, 
            BioTransformQuery transformQuery) {

            if (sourceQuery.Formula == null || sourceQuery.Formula == string.Empty || 
                sourceQuery.Formula.Contains("||") || sourceQuery.Formula == "Unknown") return false;
            if (targetQuery.Formula == null || targetQuery.Formula == string.Empty || 
                targetQuery.Formula.Contains("||") || targetQuery.Formula == "Unknown") return false;

            var rtSource = sourceQuery.RetentionTime;
            var rtTarget = targetQuery.RetentionTime;

            if (Math.Abs(rtSource - rtTarget) > 3.0) return false;
            if (transformQuery.ElutionBehavior == "+" && rtTarget - rtSource < 0) return false;
            if (transformQuery.ElutionBehavior == "-" && rtTarget - rtSource > 0) return false;
            if (transformQuery.ElutionBehavior == "n" && Math.Abs(rtTarget - rtSource) > 0.5) return false;
            if (transformQuery.ElutionBehavior == "n+" && rtTarget - rtSource < 0 && Math.Abs(rtTarget - rtSource) > 0.5) return false;
            if (transformQuery.ElutionBehavior == "n-" && rtTarget - rtSource > 0 && Math.Abs(rtTarget - rtSource) > 0.5) return false;

            Formula formulaSource = sourceQuery.FormulaBean;
            if (formulaSource == null)
                formulaSource = FormulaStringParcer.OrganicElementsReader(sourceQuery.Formula);

            Formula formulaTarget = targetQuery.FormulaBean; 
            if (formulaTarget == null)
                formulaTarget = FormulaStringParcer.OrganicElementsReader(targetQuery.Formula);

            var formulaDiff = getFormulaDifference(formulaSource, formulaTarget);
            if (formulaDiff == transformQuery.FormulaDiff) {
                return true;
            }
            else {
                return false;
            }
        }

        private static string getFormulaDifference(Formula formulaSource, Formula formulaTarget) {
            if (formulaSource == null || formulaTarget == null) return "-1";

            var cDiff = formulaTarget.Cnum - formulaSource.Cnum;
            var hDiff = formulaTarget.Hnum - formulaSource.Hnum;
            var nDiff = formulaTarget.Nnum - formulaSource.Nnum;
            var oDiff = formulaTarget.Onum - formulaSource.Onum;
            var pDiff = formulaTarget.Pnum - formulaSource.Pnum;
            var sDiff = formulaTarget.Snum - formulaSource.Snum;

            var posString = "+";
            var negString = "-";

            if (cDiff == 1) posString += "C";
            else if (cDiff > 1) posString += "C" + cDiff.ToString();
            else if (cDiff == 0) { }
            else if (cDiff == -1) negString += "C";
            else if (cDiff < -1) negString += "C" + Math.Abs(cDiff).ToString();

            if (hDiff == 1) posString += "H";
            else if (hDiff > 1) posString += "H" + hDiff.ToString();
            else if (hDiff == 0) { }
            else if (hDiff == -1) negString += "H";
            else if (hDiff < -1) negString += "H" + Math.Abs(hDiff).ToString();

            if (nDiff == 1) posString += "N";
            else if (nDiff > 1) posString += "N" + nDiff.ToString();
            else if (nDiff == 0) { }
            else if (nDiff == -1) negString += "N";
            else if (nDiff < -1) negString += "N" + Math.Abs(nDiff).ToString();

            if (oDiff == 1) posString += "O";
            else if (oDiff > 1) posString += "O" + oDiff.ToString();
            else if (oDiff == 0) { }
            else if (oDiff == -1) negString += "O";
            else if (oDiff < -1) negString += "O" + Math.Abs(oDiff).ToString();

            if (pDiff == 1) posString += "P";
            else if (pDiff > 1) posString += "P" + pDiff.ToString();
            else if (pDiff == 0) { }
            else if (pDiff == -1) negString += "P";
            else if (pDiff < -1) negString += "P" + Math.Abs(pDiff).ToString();

            if (sDiff == 1) posString += "S";
            else if (sDiff > 1) posString += "S" + sDiff.ToString();
            else if (sDiff == 0) { }
            else if (sDiff == -1) negString += "S";
            else if (sDiff < -1) negString += "S" + Math.Abs(sDiff).ToString();

            if (posString == "+") posString = string.Empty;
            if (negString == "-") negString = string.Empty;

            return posString + negString;
        }


        /// <summary>
        /// [0]ID [1]Name [2]Formula diff [3]Mass difference [4]Elution behavior
        /// </summary>
        private static List<BioTransformQuery> readBiotransformQueries(string input) {

            var queries = new List<BioTransformQuery>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');

                    var query = new BioTransformQuery() {
                        Id = int.Parse(lineArray[0]),
                        Name = lineArray[1],
                        FormulaDiff = lineArray[2],
                        MassDiff = double.Parse(lineArray[3]),
                        ElutionBehavior = lineArray[4]
                    };
                    queries.Add(query);
                }
            }
            return queries;
        }
    }

    public sealed class OntologyClustering {
        private OntologyClustering() { }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries) {
            var totalCount = mspQueries.Count * mspQueries.Count;
            var counter = 0;
            var pairDone = new List<string>();
            var edges = new List<EdgeInformation>();

            for (int i = 0; i < mspQueries.Count; i++) {
                if (mspQueries[i].Ontology == string.Empty || mspQueries[i].Ontology == "Unknown") continue;
                for (int j = i + 1; j < mspQueries.Count; j++) {
                    //if (i == j) continue;
                    //if (i < j) continue;
                    if (mspQueries[j].Ontology == string.Empty || mspQueries[i].Ontology == "Unknown") continue;

                    var similarity = ontologyStringSimilarity(mspQueries[i].Ontology, mspQueries[j].Ontology);

                    if (similarity >= 0.85) {

                        //var pairKey = Math.Min(i, j) + "-" + Math.Max(i, j);
                        //if (!pairDone.Contains(pairKey)) {

                            var sourceID = Math.Min(i, j);
                            var targetID = Math.Max(i, j);

                            var edge = new EdgeInformation() {
                                SourceComment = mspQueries[sourceID].Comment,
                                TargetComment = mspQueries[targetID].Comment,
                                Score = Math.Round(similarity, 3),
                                Comment = "Ontology similarity",
                                SourceName = mspQueries[sourceID].Name,
                                TargetName = mspQueries[targetID].Name
                            };
                            edges.Add(edge);
                            //pairDone.Add(pairKey);
                        //}
                    }
                    counter++;
                }
            }

            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(List<MspFormatCompoundInformationBean> mspQueries, double cutOff, double progressMax, double initalValue, BackgroundWorker bgWorker) {
            var totalCount = (mspQueries.Count - 1) * (mspQueries.Count - 1) * 0.5;
            var counter = 0;
            var pairDone = new List<string>();
            var edges = new List<EdgeInformation>();
            var reportedProgress = 0.0;

            for (int i = 0; i < mspQueries.Count; i++) {
                if (mspQueries[i].Ontology == string.Empty || mspQueries[i].Ontology == "Unknown") continue;
                for (int j = i + 1; j < mspQueries.Count; j++) {
                    if (mspQueries[j].Ontology == string.Empty || mspQueries[i].Ontology == "Unknown") continue;
                    var similarity = ontologyStringSimilarity(mspQueries[i].Ontology, mspQueries[j].Ontology);
                    if (similarity >= cutOff * 0.01) {

                        var sourceID = Math.Min(i, j);
                        var targetID = Math.Max(i, j);

                        var edge = new EdgeInformation() {
                            SourceName = mspQueries[sourceID].Name,
                            SourceComment = mspQueries[sourceID].Comment,
                            SourceID = mspQueries[sourceID].Id,
                            TargetName = mspQueries[targetID].Name,
                            TargetComment = mspQueries[targetID].Comment,
                            TargetID = mspQueries[targetID].Id,
                            Score = Math.Round(similarity, 3),
                            Comment = "Ontology similarity",
                        };
                        edges.Add(edge);
                    }
                    counter++;

                    var progress = initalValue + (double)counter / (double)totalCount * progressMax;
                    if (progress - reportedProgress > 1) {
                        bgWorker.ReportProgress((int)progress);
                        reportedProgress = (int)progress;
                    }
                }
            }

            return edges;
        }

        public static List<EdgeInformation> GetEdgeInformations(MspFormatCompoundInformationBean targetQuery,
            List<MspFormatCompoundInformationBean> mspQueries, 
            double cutOff, double progressMax, double initalValue, BackgroundWorker bgWorker) {

            if (targetQuery.Ontology == string.Empty || targetQuery.Ontology == "Unknown") return new List<EdgeInformation>();

            var totalCount = mspQueries.Count - 1;
            var counter = 0;
            var edges = new List<EdgeInformation>();
            var reportedProgress = 0.0;

            for (int i = 0; i < mspQueries.Count; i++) {
                if (mspQueries[i].Ontology == string.Empty || mspQueries[i].Ontology == "Unknown") continue;
                var similarity = ontologyStringSimilarity(mspQueries[i].Ontology, targetQuery.Ontology);

                if (similarity >= cutOff * 0.01) {

                    var edge = new EdgeInformation() {
                        SourceComment = targetQuery.Comment,
                        SourceName = targetQuery.Name,
                        SourceID = targetQuery.Id,
                        TargetComment = mspQueries[i].Comment,
                        TargetName = mspQueries[i].Name,
                        TargetID = mspQueries[i].Id,
                        Score = Math.Round(similarity, 3),
                        Comment = "Ontology similarity",
                    };
                    edges.Add(edge);
                }
                counter++;
                var progress = initalValue + (double)counter / (double)totalCount * progressMax;
                if (progress - reportedProgress > 1) {
                    bgWorker.ReportProgress((int)progress);
                }
            }

            return edges;
        }

        public static void GenerateOntologyStringSimilarityDistMatrix(string input, string output) {

            var names = new List<string>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    names.Add(line.Trim());
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                for (int i = 0; i < names.Count; i++) {
                    sw.Write(names[i] + "\t");
                }
                sw.WriteLine();

                for (int i = 0; i < names.Count; i++) {
                    for (int j = 0; j < names.Count; j++) {
                        if (i < j) {
                            sw.Write("\t");
                        }
                        else if (i == j) {
                            sw.Write("1\t");
                        }
                        else {
                            var similarity = ontologyStringSimilarity(names[i], names[j]);
                            sw.Write(Math.Round(similarity, 5) + "\t");
                        }

                        if (j == names.Count - 1)
                            sw.WriteLine();
                    }
                }
            }
        }

        public static double ontologyStringSimilarity(string a, string b) {

            if (String.IsNullOrEmpty(a) || String.IsNullOrEmpty(b)) return 0;
            if (a.Contains("||")) a = a.Split('|')[0];
            if (b.Contains("||")) b = b.Split('|')[0];
            if (a == "NA") return 0;
            if (b == "NA") return 0;
            int intValue = 0;
            if (int.TryParse(a, out intValue)) return 0;
            if (int.TryParse(b, out intValue)) return 0;

            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (int i = 1; i <= lengthA; i++)
                for (int j = 1; j <= lengthB; j++) {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min
                        (
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost
                        );
                }
            return ((double)Math.Max(lengthA, lengthB) - (double)distances[lengthA, lengthB]) / (double)Math.Max(lengthA, lengthB);
        }

    }
}
