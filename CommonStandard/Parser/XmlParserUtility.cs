using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace CompMs.Common.Parser {
    public sealed class XmlParserUtility {
        private XmlParserUtility() { }
        public static void ParserCommonMethod(XmlTextReader xmlRdr, string returnElementName, 
            Dictionary<string, Action<string>> attributeActions, Dictionary<string, Action> elementActions) {
            if (elementActions == null) return;
            if (attributeActions != null) {
                while (xmlRdr.MoveToNextAttribute()) {
                    if (attributeActions.ContainsKey(xmlRdr.Name)) {
                        attributeActions[xmlRdr.Name](xmlRdr.Value);
                    }
                }
            }
            while (xmlRdr.Read()) {
                if (xmlRdr.NodeType == XmlNodeType.Element) {
                    if (elementActions.ContainsKey(xmlRdr.Name))
                        elementActions[xmlRdr.Name]();
                }
                else if (xmlRdr.NodeType == XmlNodeType.EndElement) {
                    if (xmlRdr.Name == returnElementName)
                        return;
                }
            }
        }
    }
}
