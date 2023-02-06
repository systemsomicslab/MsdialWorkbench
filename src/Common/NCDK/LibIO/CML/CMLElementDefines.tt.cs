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
    public partial class CMLArray : CMLElement
    {
        public CMLArray()
            : base(XName_CML_array)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLAtom : CMLElement
    {
        public CMLAtom()
            : base(XName_CML_atom)
        { }
        public CMLAtom(CMLAtom old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLAtomArray : CMLElement
    {
        public CMLAtomArray()
            : base(XName_CML_atomArray)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLBond : CMLElement
    {
        public CMLBond()
            : base(XName_CML_bond)
        { }
        public CMLBond(CMLBond old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLBondArray : CMLElement
    {
        public CMLBondArray()
            : base(XName_CML_bondArray)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLBondStereo : CMLElement
    {
        public CMLBondStereo()
            : base(XName_CML_bondStereo)
        { }
        public CMLBondStereo(CMLBondStereo old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLBondType : CMLElement
    {
        public CMLBondType()
            : base(XName_CML_bondType)
        { }
        public CMLBondType(CMLBondType old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLCml : CMLElement
    {
        public CMLCml()
            : base(XName_CML_cml)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLCrystal : CMLElement
    {
        public CMLCrystal()
            : base(XName_CML_crystal)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLFormula : CMLElement
    {
        public CMLFormula()
            : base(XName_CML_formula)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLIdentifier : CMLElement
    {
        public CMLIdentifier()
            : base(XName_CML_identifier)
        { }
        public CMLIdentifier(CMLIdentifier old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLList : CMLElement
    {
        public CMLList()
            : base(XName_CML_list)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLMetadata : CMLElement
    {
        public CMLMetadata()
            : base(XName_CML_metadata)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLMetadataList : CMLElement
    {
        public CMLMetadataList()
            : base(XName_CML_metadataList)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLMolecule : CMLElement
    {
        public CMLMolecule()
            : base(XName_CML_molecule)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLMoleculeList : CMLElement
    {
        public CMLMoleculeList()
            : base(XName_CML_moleculeList)
        { }
        public CMLMoleculeList(CMLMoleculeList old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLProperty : CMLElement
    {
        public CMLProperty()
            : base(XName_CML_property)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLPropertyList : CMLElement
    {
        public CMLPropertyList()
            : base(XName_CML_propertyList)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLReaction : CMLElement
    {
        public CMLReaction()
            : base(XName_CML_reaction)
        { }
        public CMLReaction(CMLReaction old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLReactionList : CMLElement
    {
        public CMLReactionList()
            : base(XName_CML_reactionList)
        { }
        public CMLReactionList(CMLReactionList old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLReactionScheme : CMLElement
    {
        public CMLReactionScheme()
            : base(XName_CML_reactionScheme)
        { }
        public CMLReactionScheme(CMLReactionScheme old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLReactionStep : CMLElement
    {
        public CMLReactionStep()
            : base(XName_CML_reactionStep)
        { }
        public CMLReactionStep(CMLReactionStep old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLReactant : CMLElement
    {
        public CMLReactant()
            : base(XName_CML_reactant)
        { }
        public CMLReactant(CMLReactant old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLReactantList : CMLElement
    {
        public CMLReactantList()
            : base(XName_CML_reactantList)
        { }
        public CMLReactantList(CMLReactantList old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLProduct : CMLElement
    {
        public CMLProduct()
            : base(XName_CML_product)
        { }
        public CMLProduct(CMLProduct old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLProductList : CMLElement
    {
        public CMLProductList()
            : base(XName_CML_productList)
        { }
        public CMLProductList(CMLProductList old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLSubstance : CMLElement
    {
        public CMLSubstance()
            : base(XName_CML_substance)
        { }
        public CMLSubstance(CMLSubstance old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLSubstanceList : CMLElement
    {
        public CMLSubstanceList()
            : base(XName_CML_substanceList)
        { }
        public CMLSubstanceList(CMLSubstanceList old)
            : base(old)
        { }        
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
    public partial class CMLScalar : CMLElement
    {
        public CMLScalar()
            : base(XName_CML_scalar)
        { }
        public string Title
        {
            get { return Attribute(Attribute_title)?.Value; }
            set { SetAttributeValue(Attribute_title, value); }
        }
        public string Convention
        {
            get { return Attribute(Attribute_convention)?.Value; }
            set { SetAttributeValue(Attribute_convention, value); }
        }
        public string DictRef
        {
            get { return Attribute(Attribute_dictRef)?.Value; }
            set { SetAttributeValue(Attribute_dictRef, value); }
        }
    }
}
