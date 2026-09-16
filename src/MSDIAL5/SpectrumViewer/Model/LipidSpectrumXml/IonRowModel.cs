using System.Xml.Linq;

namespace CompMs.App.SpectrumViewer.Model.LipidSpectrumXml
{
    public enum IonRowKind
    {
        Precursor,
        ClassIon,
        ChainIon,
        InvisibleIon,
        ProhibitedIon,
    }

    // Wraps one <Precursor>/<ClassIon>/<ChainIon>/<InvisibleIon>/<ProhibitedIon> element directly.
    // Each kind uses a different subset of child element names; unsupported fields are simply
    // no-ops so a single row shape can drive all five editor grids.
    public class IonRowModel : BindableBaseXElement
    {
        public IonRowModel(XElement element, IonRowKind kind) : base(element) {
            Kind = kind;
        }

        public IonRowKind Kind { get; }

        private string MzElementName => Kind switch {
            IonRowKind.InvisibleIon => "InvisibleIonMZ",
            IonRowKind.ProhibitedIon => "ProhibitedIonMZ",
            _ => "MZ",
        };

        private string IntensityElementName => Kind switch {
            IonRowKind.InvisibleIon => "InvisibleIonIntensity",
            IonRowKind.ProhibitedIon => "ProhibitedIonIntensity",
            _ => "Intensity",
        };

        public string Mz {
            get => GetValue(MzElementName);
            set => SetValue(MzElementName, value);
        }

        public string Intensity {
            get => GetValue(IntensityElementName);
            set => SetValue(IntensityElementName, value);
        }

        public string Nl {
            get => Kind is IonRowKind.ClassIon or IonRowKind.ChainIon ? GetValue("NL") : null;
            set { if (Kind is IonRowKind.ClassIon or IonRowKind.ChainIon) SetValue("NL", value); }
        }

        public string Pi {
            get => Kind == IonRowKind.ClassIon ? GetValue("PI") : null;
            set { if (Kind == IonRowKind.ClassIon) SetValue("PI", value); }
        }

        public string Comment {
            get => Kind is IonRowKind.Precursor or IonRowKind.ClassIon or IonRowKind.ChainIon ? GetValue("Comment") : null;
            set { if (Kind is IonRowKind.Precursor or IonRowKind.ClassIon or IonRowKind.ChainIon) SetValue("Comment", value); }
        }

        public bool IsDiagnostic {
            get => Kind is IonRowKind.Precursor or IonRowKind.ClassIon or IonRowKind.ChainIon && GetValue("IsDiagnostic") == "1";
            set { if (Kind is IonRowKind.Precursor or IonRowKind.ClassIon or IonRowKind.ChainIon) SetValue("IsDiagnostic", value ? "1" : "0"); }
        }

        public bool IsPositionDiagnostic {
            get => Kind == IonRowKind.ChainIon && GetValue("IsPositionDiagnostic") == "1";
            set { if (Kind == IonRowKind.ChainIon) SetValue("IsPositionDiagnostic", value ? "1" : "0"); }
        }

        public string DiagnosticIonGroup {
            get => Kind is IonRowKind.ClassIon or IonRowKind.ChainIon or IonRowKind.InvisibleIon ? GetValue("DiagnosticIonGroup") : null;
            set { if (Kind is IonRowKind.ClassIon or IonRowKind.ChainIon or IonRowKind.InvisibleIon) SetValue("DiagnosticIonGroup", value); }
        }

        public string DiagnosticIonCount {
            get => Kind is IonRowKind.ClassIon or IonRowKind.ChainIon ? GetValue("DiagnosticIonCount") : null;
            set { if (Kind is IonRowKind.ClassIon or IonRowKind.ChainIon) SetValue("DiagnosticIonCount", value); }
        }

        public string DiagnosticIonIntensity {
            get => Kind is IonRowKind.ClassIon or IonRowKind.ChainIon ? GetValue("DiagnosticIonIntensity") : null;
            set { if (Kind is IonRowKind.ClassIon or IonRowKind.ChainIon) SetValue("DiagnosticIonIntensity", value); }
        }

        // Creates a bare element with just the mz/intensity fields populated, ready to be appended
        // to a <Precursors>/<ClassIons>/<ChainIons>/<InvisibleIons>/<ProhibitedIons> parent.
        public static XElement CreateBlank(IonRowKind kind) {
            var rowElementName = kind switch {
                IonRowKind.Precursor => "Precursor",
                IonRowKind.ClassIon => "ClassIon",
                IonRowKind.ChainIon => "ChainIon",
                IonRowKind.InvisibleIon => "InvisibleIon",
                IonRowKind.ProhibitedIon => "ProhibitedIon",
                _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
            };
            var element = new XElement(rowElementName);
            var row = new IonRowModel(element, kind);
            row.Mz = string.Empty;
            row.Intensity = "0";
            return element;
        }
    }
}
