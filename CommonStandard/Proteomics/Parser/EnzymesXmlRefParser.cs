using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace CompMs.Common.Proteomics.Parser {
    public class EnzymesXmlRefParser {
        private XmlTextReader xmlRdr = null;
        public List<Enzyme> Enzymes { get; set; } = new List<Enzyme>();
        public void Read(string file) {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                using (var xmlRdr = new XmlTextReader(fs)) {
                    this.xmlRdr = xmlRdr;
                    while (xmlRdr.Read()) {
                        if (xmlRdr.NodeType == XmlNodeType.Element) {
                            switch (xmlRdr.Name) {
                                case "enzymes": this.parseRun(); break; // mandatory
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
                            case "enzymes": this.parseRun(); break; // mandatory
                        }
                    }
                }
            }
        }

        public void Read() {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CompMs.Common.Proteomics.Resources.enzymes.xml";
            using (var fs = assembly.GetManifestResourceStream(resourceName)) {
                Read(fs);
            }
            //foreach (var enzyme in Enzymes) {
            //    Console.WriteLine("Title{0}, Description{1}, CreateDate{2}, LastModified{3}, User{4}, Specificity{5}",
            //        enzyme.Title, enzyme.Description, enzyme.CreateDate, enzyme.LastModifiedDate, enzyme.User,
            //        String.Join(",", enzyme.SpecificityList));
            //}
        }


        private void parseRun() {
            XmlParserUtility.ParserCommonMethod(
                this.xmlRdr, "enzymes", null,
                new Dictionary<string, Action>()
                {
                    { "enzyme", () => this.parseEnzyme() },
                });
        }

        private void parseEnzyme() {
            var enzyme = new Enzyme();
            XmlParserUtility.ParserCommonMethod(
                this.xmlRdr, "enzyme", 
                new Dictionary<string, Action<string>>() {
                    { "title", (v) => {
                        enzyme.Title = v;
                    }},
                    { "description", (v) => {
                        enzyme.Description = v;
                    }},
                    { "create_date", (v) => {
                        enzyme.CreateDate = v;
                    }},
                    { "last_modified_date", (v) => {
                        enzyme.LastModifiedDate = v;
                    }},
                    { "user", (v) => {
                        enzyme.User = v;
                    }}
                },
                new Dictionary<string, Action>()
                {
                    { "specificity", () => this.parseSpecificity(enzyme) },
                });
            Enzymes.Add(enzyme);
        }

        private void parseSpecificity(Enzyme enzyme) {
            XmlParserUtility.ParserCommonMethod(
               this.xmlRdr, "specificity", null,
               new Dictionary<string, Action>()
               {
                    { "string", () => this.parseString(enzyme) },
               });
        }

        private void parseString(Enzyme enzyme) {
            while (this.xmlRdr.Read()) {
                if (xmlRdr.NodeType == XmlNodeType.Text) {
                    enzyme.SpecificityList.Add(this.xmlRdr.Value);
                }
                else if (xmlRdr.NodeType == XmlNodeType.EndElement) {
                    if (xmlRdr.Name == "string")
                        return;
                }
            }
        }
    }
}
