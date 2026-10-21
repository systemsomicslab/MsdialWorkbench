using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.App.SpectrumViewer.Model.LipidSpectrumXml
{
    // Wraps one <LipidMS> block (one LipidClass x Adduct combination) from the spectrum-generator
    // markup XML.
    public class LipidMsEntryModel : BindableBaseXElement
    {
        public LipidMsEntryModel(XElement element) : base(element) {
            Precursors = LoadRows("Precursors", IonRowKind.Precursor);
            ClassIons = LoadRows("ClassIons", IonRowKind.ClassIon);
            ChainIons = LoadRows("ChainIons", IonRowKind.ChainIon);
            InvisibleIons = LoadRows("InvisibleIons", IonRowKind.InvisibleIon);
            ProhibitedIons = LoadRows("ProhibitedIons", IonRowKind.ProhibitedIon);
        }

        public string LipidClass {
            get => GetValue("LipidClass");
            set => SetValue("LipidClass", value);
        }

        public string LSILevel {
            get => GetValue("LSILevel");
            set => SetValue("LSILevel", value);
        }

        public string Adduct {
            get => GetValue("Adduct");
            set => SetValue("Adduct", value);
        }

        public string DisplayName => $"{LipidClass} {Adduct} ({LSILevel})";

        public ObservableCollection<IonRowModel> Precursors { get; }
        public ObservableCollection<IonRowModel> ClassIons { get; }
        public ObservableCollection<IonRowModel> ChainIons { get; }
        public ObservableCollection<IonRowModel> InvisibleIons { get; }
        public ObservableCollection<IonRowModel> ProhibitedIons { get; }

        private ObservableCollection<IonRowModel> LoadRows(string wrapperElementName, IonRowKind kind) {
            var wrapper = Element.Element(wrapperElementName);
            if (wrapper is null) {
                wrapper = new XElement(wrapperElementName);
                Element.Add(wrapper);
            }
            return new ObservableCollection<IonRowModel>(wrapper.Elements().Select(e => new IonRowModel(e, kind)));
        }

        public IonRowModel AddRow(ObservableCollection<IonRowModel> rows, string wrapperElementName, IonRowKind kind) {
            var wrapper = Element.Element(wrapperElementName);
            if (wrapper is null) {
                wrapper = new XElement(wrapperElementName);
                Element.Add(wrapper);
            }
            var newElement = IonRowModel.CreateBlank(kind);
            wrapper.Add(newElement);
            var row = new IonRowModel(newElement, kind);
            rows.Add(row);
            return row;
        }

        public void RemoveRow(ObservableCollection<IonRowModel> rows, IonRowModel row) {
            row.Element.Remove();
            rows.Remove(row);
        }
    }
}
