using System.IO;
using System.Reflection;
using System.Xml;

namespace CompMs.App.Msdial.Model.Statistics {
    internal class NotameXmlReader {
        public string rScript;
        private XmlTextReader xmlRdr;
        public void Read(string file) {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                using (var xmlRdr = new XmlTextReader(fs)) {
                    this.xmlRdr = xmlRdr;
                    xmlRdr.Read();
                    while (xmlRdr.Read()) {
                        if (xmlRdr.NodeType == XmlNodeType.CDATA) {
                            rScript = xmlRdr.Value; break;
                        }
                    }
                }
            }
        }

        public void Read(Stream fs) {
            using (var xmlRdr = new XmlTextReader(fs)) {
                this.xmlRdr = xmlRdr;
                xmlRdr.Read();
                while (xmlRdr.Read()) {
                    if (xmlRdr.NodeType == XmlNodeType.CDATA) {
                        rScript = xmlRdr.Value; break;
                    }
                }
            }
        }

        public void Read() {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CompMs.App.Msdial.Resources.NotameR.xml";
            using var fs = assembly.GetManifestResourceStream(resourceName);
            Read(fs);
        }
    }
}