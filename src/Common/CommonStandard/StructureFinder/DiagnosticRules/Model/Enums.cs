namespace CompMs.Common.StructureFinder.DiagnosticRules.Model
{
    /// <summary>Provenance of a parameter, a table row or a rule (design 3.5), ordered as_published &lt; tuned_vs20 &lt; tuned_pooled.</summary>
    public enum ParametersOrigin
    {
        AsPublished,
        TunedVs20,
        TunedPooled,
    }

    /// <summary>Provenance of a motif row (design 3.4).</summary>
    public enum MotifOrigin
    {
        SourceDefined,
        TunedVs20,
        TunedPooled,
    }

    public enum RuleIonMode
    {
        Positive,
        Negative,
    }

    public enum PreconditionMode
    {
        AdductAmbiguous,
        MzOnly,
    }

    /// <summary>Intensity convention of a rule (design 4.4).</summary>
    public enum RuleConvention
    {
        P2,
        Msdial,
    }

    /// <summary>Which product tolerance a rule declares (design 3.5).</summary>
    public enum ProductToleranceKind
    {
        /// <summary>max(0.005 Da, 10 ppm).</summary>
        P2,
        /// <summary>MS-FINDER's Ms2Tolerance setting.</summary>
        Msfinder,
    }

    public enum LossReference
    {
        Hypothesis,
        Observed,
    }

    public enum ClaimLevel
    {
        Class,
        Scaffold,
        Substituent,
    }

    public enum UnitClass
    {
        Sugar,
        Acyl,
        Conjugate,
    }

    public enum TermKind
    {
        ProductIon,
        LossFromPrecursor,
        AglyconeIon,
        IonSeries,
        Window,
    }

    public enum TermRole
    {
        Required,
        Counted,
        Ordering,
        VetoPresent,
        VetoStrongerThan,
        Upgrade,
    }

    public enum IonForm
    {
        Even,
        Radical,
    }

    public enum ChargeCarrier
    {
        Proton,
        Adduct,
    }

    public enum WindowReference
    {
        Ion,
        Loss,
    }

    public enum LogicNodeKind
    {
        All,
        Any,
        Not,
        KOf,
        Term,
    }

    /// <summary>The closed set of k variables (design 4.2).</summary>
    public enum KVariable
    {
        NUnits,
        NSugars,
        NHex,
        NDhex,
        NPen,
        NHexa,
        NAcyl,
    }

    /// <summary>The text names of the enums, as rule files write them.</summary>
    public static class RuleNames
    {
        public static ParametersOrigin ParseOrigin(string text) {
            switch (text) {
                case "as_published": return ParametersOrigin.AsPublished;
                case "tuned_vs20": return ParametersOrigin.TunedVs20;
                case "tuned_pooled": return ParametersOrigin.TunedPooled;
                default: throw new System.ArgumentException("unknown origin", nameof(text));
            }
        }

        public static MotifOrigin ParseMotifOrigin(string text) {
            switch (text) {
                case "source_defined": return MotifOrigin.SourceDefined;
                case "tuned_vs20": return MotifOrigin.TunedVs20;
                case "tuned_pooled": return MotifOrigin.TunedPooled;
                default: throw new System.ArgumentException("unknown motif origin", nameof(text));
            }
        }

        public static UnitClass ParseUnitClass(string text) {
            switch (text) {
                case "sugar": return UnitClass.Sugar;
                case "acyl": return UnitClass.Acyl;
                case "conjugate": return UnitClass.Conjugate;
                default: throw new System.ArgumentException("unknown unit class", nameof(text));
            }
        }

        public static string Name(UnitClass value) {
            switch (value) {
                case UnitClass.Sugar: return "sugar";
                case UnitClass.Acyl: return "acyl";
                default: return "conjugate";
            }
        }

        public static TermKind ParseKind(string text) {
            switch (text) {
                case "product_ion": return TermKind.ProductIon;
                case "loss_from_precursor": return TermKind.LossFromPrecursor;
                case "aglycone_ion": return TermKind.AglyconeIon;
                case "ion_series": return TermKind.IonSeries;
                case "window": return TermKind.Window;
                default: throw new System.ArgumentException("unknown term kind", nameof(text));
            }
        }

        public static TermRole ParseRole(string text) {
            switch (text) {
                case "required": return TermRole.Required;
                case "counted": return TermRole.Counted;
                case "ordering": return TermRole.Ordering;
                case "veto_present": return TermRole.VetoPresent;
                case "veto_stronger_than": return TermRole.VetoStrongerThan;
                case "upgrade": return TermRole.Upgrade;
                default: throw new System.ArgumentException("unknown term role", nameof(text));
            }
        }

        public static KVariable ParseVariable(string text) {
            switch (text) {
                case "n_units": return KVariable.NUnits;
                case "n_sugars": return KVariable.NSugars;
                case "n_hex": return KVariable.NHex;
                case "n_dhex": return KVariable.NDhex;
                case "n_pen": return KVariable.NPen;
                case "n_hexa": return KVariable.NHexa;
                case "n_acyl": return KVariable.NAcyl;
                default: throw new System.ArgumentException("unknown k variable", nameof(text));
            }
        }
    }
}
