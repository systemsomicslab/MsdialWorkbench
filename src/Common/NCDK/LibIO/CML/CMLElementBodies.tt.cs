/*
 * Copyright (C) 2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */




namespace NCDK.LibIO.CML
{
    public partial class CMLElement
    {
        public string Id
        {
            get { return Attribute(Attribute_id)?.Value; }
            set { SetAttributeValue(Attribute_id, value); }
        }
    }

    public partial class CMLLabel
    {
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
        public string ObjectClass
        {
            get { return Attribute(Attribute_objectClass)?.Value; }
            set { SetAttributeValue(Attribute_objectClass, value); }
        }
    }

    public partial class CMLAtom
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public double Count
        {
            get
            {
                var v = Attribute(Attribute_count).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_count, value); }
        }
        public string ElementType
        {
            get { return Attribute(Attribute_elementType)?.Value; }
            set { SetAttributeValue(Attribute_elementType, value); }
        }
        public int FormalCharge
        {
            get { return GetAttributeValueAsInt(Attribute_formalCharge, 0); }
            set { SetAttributeValue(Attribute_formalCharge, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)); }
        }
        public int HydrogenCount
        {
            get { return GetAttributeValueAsInt(Attribute_hydrogenCount, 0); }
            set { SetAttributeValue(Attribute_hydrogenCount, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)); }
        }
        public int IsotopeNumber
        {
            get { return GetAttributeValueAsInt(Attribute_isotopeNumber, 0); }
            set { SetAttributeValue(Attribute_isotopeNumber, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)); }
        }
        public int SpinMultiplicity
        {
            get { return GetAttributeValueAsInt(Attribute_spinMultiplicity, 0); }
            set { SetAttributeValue(Attribute_spinMultiplicity, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)); }
        }
        public double X2
        {
            get
            {
                var v = Attribute(Attribute_x2).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_x2, value); }
        }
        public double Y2
        {
            get
            {
                var v = Attribute(Attribute_y2).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_y2, value); }
        }
        public double X3
        {
            get
            {
                var v = Attribute(Attribute_x3).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_x3, value); }
        }
        public double Y3
        {
            get
            {
                var v = Attribute(Attribute_y3).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_y3, value); }
        }
        public double Z3
        {
            get
            {
                var v = Attribute(Attribute_z3).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_z3, value); }
        }
        public double XFract
        {
            get
            {
                var v = Attribute(Attribute_xFract).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_xFract, value); }
        }
        public double YFract
        {
            get
            {
                var v = Attribute(Attribute_yFract).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_yFract, value); }
        }
        public double ZFract
        {
            get
            {
                var v = Attribute(Attribute_zFract).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_zFract, value); }
        }
    }

    public partial class CMLBond
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Order
        {
            get { return Attribute(Attribute_order)?.Value; }
            set { SetAttributeValue(Attribute_order, value); }
        }
        public System.Collections.Generic.IReadOnlyList<string> AtomRefs
        {
            get
            {
                var vs = GetAttributeValue(Attribute_atomRefs);
                return ToArray(vs);
            }
            set { SetAttributeValue(Attribute_atomRefs, Concat(value)); }
        }
        public System.Collections.Generic.IReadOnlyList<string> AtomRefs2
        {
            get
            {
                var vs = GetAttributeValue(Attribute_atomRefs2);
                return ToArray(vs);
            }
            set { SetAttributeValue(Attribute_atomRefs2, Concat(value)); }
        }
    }

    public partial class CMLAtomArray
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
    }
    
    public partial class CMLBondArray
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
    }

    public partial class CMLFormula
    {
        public double Count
        {
            get
            {
                var v = Attribute(Attribute_count).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_count, value); }
        }
        public int FormalCharge
        {
            get { return GetAttributeValueAsInt(Attribute_formalCharge, 0); }
            set { SetAttributeValue(Attribute_formalCharge, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)); }
        }
        public string Inline
        {
            get { return Attribute(Attribute_inline)?.Value; }
            set { SetAttributeValue(Attribute_inline, value); }
        }
    }

    public partial class CMLProperty
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
    }

    public partial class CMLPropertyList
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
    }

    public partial class CMLScalar
    {
        public string DataType
        {
            get { return Attribute(Attribute_dataType)?.Value; }
            set { SetAttributeValue(Attribute_dataType, value); }
        }
        public double ErrorValue
        {
            get
            {
                var v = Attribute(Attribute_errorValue).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_errorValue, value); }
        }
        public string ErrorBasis
        {
            get { return Attribute(Attribute_errorBasis)?.Value; }
            set { SetAttributeValue(Attribute_errorBasis, value); }
        }
        public string Min
        {
            get { return Attribute(Attribute_min)?.Value; }
            set { SetAttributeValue(Attribute_min, value); }
        }
        public string Max
        {
            get { return Attribute(Attribute_max)?.Value; }
            set { SetAttributeValue(Attribute_max, value); }
        }
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Units
        {
            get { return Attribute(Attribute_units)?.Value; }
            set { SetAttributeValue(Attribute_units, value); }
        }
        public string UnitType
        {
            get { return Attribute(Attribute_unitType)?.Value; }
            set { SetAttributeValue(Attribute_unitType, value); }
        }
        public double ConstantToSI
        {
            get
            {
                var v = Attribute(Attribute_constantToSI).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_constantToSI, value); }
        }
        public double MultiplierToSI
        {
            get
            {
                var v = Attribute(Attribute_multiplierToSI).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_multiplierToSI, value); }
        }
    }


    public partial class CMLReactant
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Role
        {
            get { return Attribute(Attribute_role)?.Value; }
            set { SetAttributeValue(Attribute_role, value); }
        }
        public double Count
        {
            get
            {
                var v = Attribute(Attribute_count).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_count, value); }
        }
        public double State
        {
            get
            {
                var v = Attribute(Attribute_state).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_state, value); }
        }
    }
    
    public partial class CMLReactantList
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Role
        {
            get { return Attribute(Attribute_role)?.Value; }
            set { SetAttributeValue(Attribute_role, value); }
        }
        public double Count
        {
            get
            {
                var v = Attribute(Attribute_count).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_count, value); }
        }
        public string State
        {
            get { return Attribute(Attribute_state)?.Value; }
            set { SetAttributeValue(Attribute_state, value); }
        }
    }

    public partial class CMLProduct
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Role
        {
            get { return Attribute(Attribute_role)?.Value; }
            set { SetAttributeValue(Attribute_role, value); }
        }
        public double Count
        {
            get
            {
                var v = Attribute(Attribute_count).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_count, value); }
        }
        public double State
        {
            get
            {
                var v = Attribute(Attribute_state).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_state, value); }
        }
    }
    
    public partial class CMLProductList
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Role
        {
            get { return Attribute(Attribute_role)?.Value; }
            set { SetAttributeValue(Attribute_role, value); }
        }
        public double Count
        {
            get
            {
                var v = Attribute(Attribute_count).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_count, value); }
        }
        public string State
        {
            get { return Attribute(Attribute_state)?.Value; }
            set { SetAttributeValue(Attribute_state, value); }
        }
    }

    public partial class CMLSubstance
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Role
        {
            get { return Attribute(Attribute_role)?.Value; }
            set { SetAttributeValue(Attribute_role, value); }
        }
        public string Type
        {
            get { return Attribute(Attribute_type)?.Value; }
            set { SetAttributeValue(Attribute_type, value); }
        }
        public double Count
        {
            get
            {
                var v = Attribute(Attribute_count).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_count, value); }
        }
        public double State
        {
            get
            {
                var v = Attribute(Attribute_state).Value;
                return v == null ? double.NaN : double.Parse(v, System.Globalization.NumberFormatInfo.InvariantInfo);
            }
            set { SetAttributeValue(Attribute_state, value); }
        }
    }
    
    public partial class CMLSubstanceList
    {
        public string Ref
        {
            get { return Attribute(Attribute_ref)?.Value; }
            set { SetAttributeValue(Attribute_ref, value); }
        }
        public string Role
        {
            get { return Attribute(Attribute_role)?.Value; }
            set { SetAttributeValue(Attribute_role, value); }
        }
        public string Type
        {
            get { return Attribute(Attribute_type)?.Value; }
            set { SetAttributeValue(Attribute_type, value); }
        }
    }

    public partial class CMLCrystal
    {
        public int Z
        {
            get { return GetAttributeValueAsInt(Attribute_z, 0); }
            set { SetAttributeValue(Attribute_z, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)); }
        }
    }
}
