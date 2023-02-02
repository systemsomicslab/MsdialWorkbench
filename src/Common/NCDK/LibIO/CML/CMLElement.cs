using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NCDK.LibIO.CML
{
    public partial class CMLElement : XElement
    {
        internal static readonly XNamespace Namespace_CML = "http://www.xml-cml.org/schema"; //Convertor.NS_CML;
        internal static readonly XName XName_CML_property = Namespace_CML + "property";
        internal static readonly XName XName_CML_propertyList = Namespace_CML + "propertyList";
        internal static readonly XName XName_CML_element = Namespace_CML + "element";
        internal static readonly XName XName_CML_cml = Namespace_CML + "cml";
        internal static readonly XName XName_CML_list = Namespace_CML + "list";
        internal static readonly XName XName_CML_title = Namespace_CML + "title";
        internal static readonly XName XName_CML_metadata = Namespace_CML + "metadata";
        internal static readonly XName XName_CML_metadataList = Namespace_CML + "metadataList";
        internal static readonly XName XName_CML_reaction = Namespace_CML + "reaction";
        internal static readonly XName XName_CML_reactionScheme = Namespace_CML + "reactionScheme";
        internal static readonly XName XName_CML_reactionStep = Namespace_CML + "reactionStep";
        internal static readonly XName XName_CML_reactionList = Namespace_CML + "reactionList";
        internal static readonly XName XName_CML_reactant = Namespace_CML + "reactant";
        internal static readonly XName XName_CML_reactantList = Namespace_CML + "reactantList";
        internal static readonly XName XName_CML_product = Namespace_CML + "product";
        internal static readonly XName XName_CML_productList = Namespace_CML + "productList";
        internal static readonly XName XName_CML_substance = Namespace_CML + "substance";
        internal static readonly XName XName_CML_substanceList = Namespace_CML + "substanceList";
        internal static readonly XName XName_CML_molecule = Namespace_CML + "molecule";
        internal static readonly XName XName_CML_moleculeList = Namespace_CML + "moleculeList";
        internal static readonly XName XName_CML_crystal = Namespace_CML + "crystal";
        internal static readonly XName XName_CML_formula = Namespace_CML + "formula";
        internal static readonly XName XName_CML_identifier = Namespace_CML + "identifier";
        internal static readonly XName XName_CML_scalar = Namespace_CML + "scalar";
        internal static readonly XName XName_CML_atom = Namespace_CML + "atom";
        internal static readonly XName XName_CML_bond = Namespace_CML + "bond";
        internal static readonly XName XName_CML_bondType = Namespace_CML + "bondType"; 
        internal static readonly XName XName_CML_bondStereo = Namespace_CML + "bondStereo";
        internal static readonly XName XName_CML_label = Namespace_CML + "label";
        internal static readonly XName XName_CML_array = Namespace_CML + "array";
        internal static readonly XName XName_CML_atomArray = Namespace_CML + "atomArray";
        internal static readonly XName XName_CML_bondArray = Namespace_CML + "bondArray";

        internal const string Attribute_objectClass = "objectClass";
        internal const string Attribute_convention = "convention";
        internal const string Attribute_concise = "concise";
        internal const string Attribute_dictRef = "dictRef";
        internal const string Attribute_content = "content";
        internal const string Attribute_count= "count";
        internal const string Attribute_id = "id";
        internal const string Attribute_inline = "inline";
        internal const string Attribute_idgen = "idgen";
        internal const string Attribute_title = "title";
        internal const string Attribute_dataType = "dataType";
        internal const string Attribute_ref = "ref";
        internal const string Attribute_role = "role";
        internal const string Attribute_type = "type";
        internal const string Attribute_state = "state";
        internal const string Attribute_size = "size";
        internal const string Attribute_max = "max";
        internal const string Attribute_min = "min";
        internal const string Attribute_units = "units";
        internal const string Attribute_unitType = "unitType";
        internal const string Attribute_process = "process";
        internal const string Attribute_formula = "formula";
        internal const string Attribute_constantToSI = "constantToSI";
        internal const string Attribute_multiplierToSI = "multiplierToSI";
        internal const string Attribute_chirality = "chirality";
        internal const string Attribute_formalCharge = "formalCharge";
        internal const string Attribute_hydrogenCount = "hydrogenCount";
        internal const string Attribute_isotopeNumber = "isotopeNumber";
        internal const string Attribute_spinMultiplicity = "spinMultiplicity";
        internal const string Attribute_symmetryOriented = "symmetryOriented";
        internal const string Attribute_elementType = "elementType";
        internal const string Attribute_order = "order";
        internal const string Attribute_z = "z";
        internal const string Attribute_x2 = "x2";
        internal const string Attribute_y2 = "y2";
        internal const string Attribute_x3 = "x3";
        internal const string Attribute_y3 = "y3";
        internal const string Attribute_z3 = "z3";
        internal const string Attribute_xFract = "xFract";
        internal const string Attribute_yFract = "yFract";
        internal const string Attribute_zFract = "zFract";
        internal const string Attribute_atomRefs = "atomRefs";
        internal const string Attribute_atomRefs2 = "atomRefs2";
        internal const string Attribute_errorBasis = "errorBasis";
        internal const string Attribute_errorValue = "errorValue";
        internal const string Attribute_value = "value";

        protected static string Concat<T>(IEnumerable<T> values)
        {
            var sb = new StringBuilder();
            bool isFirst = true;
            foreach (var s in values)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(" ");
                }
                sb.Append(s);
            }
            return sb.ToString();
        }

        private static readonly char[] delim = " \t\n\r".ToCharArray();
        protected static string[] ToArray(string s)
        {
            return s.Split(delim);
        }

        protected static int[] ToIntArray(string s) 
        {
            var ss = ToArray(s);
            var ret = new int[ss.Length];
            for (int i = 0; i < ss.Length; i++)
            {
                ret[i] = int.Parse(ss[i], NumberFormatInfo.InvariantInfo);
            }
            return ret;
        }

        public CMLElement()
            : this(XName_CML_element)
        { }

        public CMLElement(XName name)
            : base(name)
        { }

        public CMLElement(CMLElement element)
            : base(element.Name)
        {
            foreach (var att in element.Attributes())
                SetAttributeValue(att.Name, att.Value);

            foreach (var child in element.Nodes())
                Add(child); // dont clone
        }

        protected string GetAttributeValue(string name, string defaultValue)
        {
            var attribute = Attribute(name);
            if (attribute == null)
                return defaultValue;
            return attribute.Value;
        }

        protected string GetAttributeValue(string name)
        {
            var attribute = Attribute(name);
            if (attribute == null)
                throw new ApplicationException($"Cannot get an attribute value of {name}.");
            return attribute.Value;
        }

        protected int GetAttributeValueAsInt(string name, int defaultValue)
        {
            var vs = GetAttributeValue(name, null);
            if (vs == null)
                return defaultValue;
            return int.Parse(vs, NumberFormatInfo.InvariantInfo);
        }

        protected int GetAttributeValueAsInt(string name)
        {
            var vs = GetAttributeValue(name);
            return int.Parse(vs, NumberFormatInfo.InvariantInfo);
        }

        protected double GetAttributeValueAsDouble(string name)
        {
            var vs = GetAttributeValue(name);
            return double.Parse(vs, NumberFormatInfo.InvariantInfo);
        }

        protected int[] GetAttributeValueAsInts(string name)
        {
            var vs = GetAttributeValue(name);
            return ToIntArray(vs);
        }
    }
}
