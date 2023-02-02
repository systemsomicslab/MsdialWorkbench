using CompMs.Common.DataObj.Ion;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace CompMs.Common.Proteomics.Parser {
    public class ModificationsXmlRefParser {
        private XmlTextReader xmlRdr = null;
        public List<Modification> Modifications { get; set; } = new List<Modification>();

        public void Read(string file) {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                using (var xmlRdr = new XmlTextReader(fs)) {
                    this.xmlRdr = xmlRdr;
                    while (xmlRdr.Read()) {
                        if (xmlRdr.NodeType == XmlNodeType.Element) {
                            switch (xmlRdr.Name) {
                                case "modifications": this.parseRun(); break; // mandatory
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
                            case "modifications": this.parseRun(); break; // mandatory
                        }
                    }
                }
            }
        }

        public void Read() {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CompMs.Common.Proteomics.Resources.modifications.xml";
            using (var fs = assembly.GetManifestResourceStream(resourceName)) {
                Read(fs);
            }
            //foreach (var mod in Modifications) {
            //    Console.WriteLine("Title{0}, Description{1}, CreateDate{2}, LastModified{3}, User{4}, position{5}, type{6}, terminustype{7}",
            //        mod.Title, mod.Description, mod.CreateDate, mod.LastModifiedDate, mod.User, mod.Position, mod.Type, mod.TerminusType);
            //    foreach (var site in mod.ModificationSites) {
            //        Console.WriteLine("Site{0}", site.Site);
            //        foreach (var nl in site.DiagnosticNLs) {
            //            Console.WriteLine("Name{0}, ShortName{1}, Formula{2}", nl.Name, nl.ShortName, nl.Formula.FormulaString);
            //        }
            //        foreach (var pl in site.DiagnosticIons) {
            //            Console.WriteLine("Name{0}, ShortName{1}, Formula{2}", pl.Name, pl.ShortName, pl.Formula.FormulaString);
            //        }
            //    }
            //}
        }

        private void parseRun() {
            XmlParserUtility.ParserCommonMethod(
                this.xmlRdr, "modifications", null,
                new Dictionary<string, Action>()
                {
                    { "modification", () => this.parseModification() },
                });
        }

        private void parseModification() {
            var modification = new Modification();
            XmlParserUtility.ParserCommonMethod(
                this.xmlRdr, "modification",
                new Dictionary<string, Action<string>>() {
                    { "title", (v) => {
                        modification.Title = v;
                    }},
                    { "description", (v) => {
                        modification.Description = v;
                    }},
                    { "create_date", (v) => {
                        modification.CreateDate = v;
                    }},
                    { "last_modified_date", (v) => {
                        modification.LastModifiedDate = v;
                    }},
                    { "user", (v) => {
                        modification.User = v;
                    }}
                    ,
                    { "reporterCorrectionM2", (v) => {
                        if (int.TryParse(v, out int vint))
                            modification.ReporterCorrectionM2 = vint;
                    }}
                    ,
                    { "reporterCorrectionM1", (v) => {
                        if (int.TryParse(v, out int vint))
                            modification.ReporterCorrectionM1 = vint;
                    }}
                    ,
                    { "reporterCorrectionP1", (v) => {
                        if (int.TryParse(v, out int vint))
                            modification.ReporterCorrectionP1 = vint;
                    }}
                    ,
                    { "reporterCorrectionP2", (v) => {
                        if (int.TryParse(v, out int vint))
                            modification.ReporterCorrectionP2 = vint;
                    }}
                    ,
                    { "reporterCorrectionType", (v) => {
                        modification.ReporterCorrectionType = v.Contains("f") ? false : true;
                    }}
                    ,
                    { "composition", (v) => {
                        modification.Composition = FormulaStringParcer.MQComposition2FormulaObj(v);
                    }}
                },
                new Dictionary<string, Action>()
                {
                    { "position", () => this.parsePosition(modification) },
                    { "modification_site", () => this.parseModificationSite(modification) },
                    { "type", () => this.parseType(modification) },
                    { "terminus_type", () => this.parseTerminusType(modification) },
                });
            Modifications.Add(modification);
        }

        private void parseModificationSite(Modification modification) {
            var modsite = new ModificationSite();
            XmlParserUtility.ParserCommonMethod(
                this.xmlRdr, "modification_site",
                new Dictionary<string, Action<string>>() {
                    { "site", (v) => {
                        modsite.Site = v;
                    }}
                },
                new Dictionary<string, Action>()
                {
                    { "neutralloss_collection", () => this.parseNeutralLossCollection(modsite) },
                    { "diagnostic_collection", () => this.parseDiagnosticCollection(modsite) }
                });
            modification.ModificationSites.Add(modsite);
        }

        private void parseDiagnosticCollection(ModificationSite modsite) {
            if (this.xmlRdr.IsEmptyElement) return;
            XmlParserUtility.ParserCommonMethod(
                this.xmlRdr, "diagnostic_collection",
                null,
                new Dictionary<string, Action>()
                {
                    { "diagnostic", () => this.parseDiagnostic(modsite) },
                });
        }

        private void parseDiagnostic(ModificationSite modsite) {
            var product = new ProductIon();

            while (this.xmlRdr.MoveToNextAttribute()) {
                if (this.xmlRdr.Name == "name") product.Name = this.xmlRdr.Value;
                else if (this.xmlRdr.Name == "shortname") product.ShortName = this.xmlRdr.Value;
                else if (this.xmlRdr.Name == "composition") product.Formula = FormulaStringParcer.MQComposition2FormulaObj(this.xmlRdr.Value);
            }

            modsite.DiagnosticIons.Add(product);
        }

        private void parseNeutralLossCollection(ModificationSite modsite) {

            if (this.xmlRdr.IsEmptyElement) return;
            XmlParserUtility.ParserCommonMethod(
                this.xmlRdr, "neutralloss_collection",
                null,
                new Dictionary<string, Action>()
                {
                    { "neutralloss", () => this.parseNeutralLoss(modsite) },
                });
        }

        private void parseNeutralLoss(ModificationSite modsite) {
            var neutralloss = new NeutralLoss();

            while (this.xmlRdr.MoveToNextAttribute()) {
                if (this.xmlRdr.Name == "name") neutralloss.Name = this.xmlRdr.Value;
                else if (this.xmlRdr.Name == "shortname") neutralloss.ShortName = this.xmlRdr.Value;
                else if (this.xmlRdr.Name == "composition") neutralloss.Formula = FormulaStringParcer.MQComposition2FormulaObj(this.xmlRdr.Value);
            }

            modsite.DiagnosticNLs.Add(neutralloss);
        }

        private void parseTerminusType(Modification modification) {
            while (this.xmlRdr.Read()) {
                if (xmlRdr.NodeType == XmlNodeType.Text) {
                    modification.TerminusType = this.xmlRdr.Value;
                }
                else if (xmlRdr.NodeType == XmlNodeType.EndElement) {
                    if (xmlRdr.Name == "terminus_type")
                        return;
                }
            }
        }

        private void parseType(Modification modification) {
            while (this.xmlRdr.Read()) {
                if (xmlRdr.NodeType == XmlNodeType.Text) {
                    modification.Type = this.xmlRdr.Value;
                }
                else if (xmlRdr.NodeType == XmlNodeType.EndElement) {
                    if (xmlRdr.Name == "type")
                        return;
                }
            }
        }

        private void parsePosition(Modification modification) {
            while (this.xmlRdr.Read()) {
                if (xmlRdr.NodeType == XmlNodeType.Text) {
                    modification.Position = this.xmlRdr.Value;
                }
                else if (xmlRdr.NodeType == XmlNodeType.EndElement) {
                    if (xmlRdr.Name == "position")
                        return;
                }
            }
        }
    }
}
