using CompMs.CommonMVVM;
using System.Xml.Linq;

namespace CompMs.App.SpectrumViewer.Model.LipidSpectrumXml
{
    // A bindable wrapper whose backing store is an XElement rather than private fields, so edits
    // made through the UI are written straight into the loaded XDocument (and therefore round-trip
    // untouched sibling <!-- comments --> and any part of the schema this editor doesn't know about).
    public abstract class BindableBaseXElement : BindableBase
    {
        protected BindableBaseXElement(XElement element) {
            Element = element;
        }

        public XElement Element { get; }

        protected string GetValue(string childName) => Element.Element(childName)?.Value;

        protected void SetValue(string childName, string value) {
            var child = Element.Element(childName);
            if (child is null) {
                if (string.IsNullOrEmpty(value)) {
                    return;
                }
                child = new XElement(childName);
                Element.Add(child);
            }
            child.Value = value ?? string.Empty;
            OnPropertyChanged(string.Empty);
        }
    }
}
