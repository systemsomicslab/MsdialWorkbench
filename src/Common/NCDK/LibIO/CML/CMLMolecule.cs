using System.Globalization;

namespace NCDK.LibIO.CML
{
    public partial class CMLMolecule
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref).Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }

        public string IdGen
        {
            get { return Attribute(Attribute_idgen).Value; }
            set { SetAttributeValue(Attribute_idgen, value); }
        }

        public string Process
        {
            get { return Attribute(Attribute_process).Value; }
            set { SetAttributeValue(Attribute_process, value); }
        }

        public string Formula
        {
            get { return Attribute(Attribute_formula).Value; }
            set { SetAttributeValue(Attribute_formula, value); }
        }

        public double Count
        {
            get { return GetAttributeValueAsDouble(Attribute_count); }
            set { SetAttributeValue(Attribute_count, value); }
        }

        public string Chirality
        {
            get { return Attribute(Attribute_chirality).Value; }
            set { SetAttributeValue(Attribute_chirality, value); }
        }

        public int FormalCharge
        {
            get { return GetAttributeValueAsInt(Attribute_formalCharge); }
            set { SetAttributeValue(Attribute_formalCharge, value.ToString(NumberFormatInfo.InvariantInfo)); }
        }

        public string SpinMultiplicity
        {
            get { return GetAttributeValue(Attribute_spinMultiplicity); }
            set { SetAttributeValue(Attribute_spinMultiplicity, value); }
        }

        public string SymmetryOriented
        {
            get { return GetAttributeValue(Attribute_symmetryOriented); }
            set { SetAttributeValue(Attribute_symmetryOriented, value); }
        }

        public string Role
        {
            get { return GetAttributeValue(Attribute_role); }
            set { SetAttributeValue(Attribute_role, value); }
        }

        public void Add(CMLAtom atom)
        {
            var aa = this.Element(XName_CML_atomArray);
            if (aa == null)
            {
                aa = new CMLAtomArray();
                this.Add(aa);
            }
            aa.Add(atom);
        }

        public void Add(CMLBond bond)
        {
            var aa = this.Element(XName_CML_bondArray);
            if (aa == null)
            {
                aa = new CMLBondArray();
                this.Add(aa);
            }
            aa.Add(bond);
        }
    }
}
