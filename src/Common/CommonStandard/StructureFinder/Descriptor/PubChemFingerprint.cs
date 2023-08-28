using CompMs.Common.DataObj.Property;
using CompMs.Common.StructureFinder.Property;
using CompMs.Common.StructureFinder.Utility;
using System.Collections.Generic;
using System.Linq;
using AtomProperty = CompMs.Common.StructureFinder.Property.AtomProperty;

namespace CompMs.Common.StructureFinder.Descriptor
{
    public sealed class PubChemFingerprint
    {
        private PubChemFingerprint() { }

        //section 1
        #region
        public static void SetSection1Properties(MolecularFingerprint descriptors, Formula formula)
        {
            //Section 1: Hierarchic Element Counts - These bits test for the 
            //presence or count of individual chemical atoms 
            //represented by their atomic symbol.
            descriptors.Bit0 = formula.Hnum >= 4 ? 1 : 0;
            descriptors.Bit1 = formula.Hnum >= 8 ? 1 : 0;
            descriptors.Bit2 = formula.Hnum >= 16 ? 1 : 0;
            descriptors.Bit3 = formula.Hnum >= 32 ? 1 : 0;

            descriptors.Bit4 = formula.Cnum >= 2 ? 1 : 0;
            descriptors.Bit5 = formula.Cnum >= 4 ? 1 : 0;
            descriptors.Bit6 = formula.Cnum >= 8 ? 1 : 0;
            descriptors.Bit7 = formula.Cnum >= 16 ? 1 : 0;
            descriptors.Bit8 = formula.Cnum >= 32 ? 1 : 0;

            descriptors.Bit9 = formula.Nnum >= 1 ? 1 : 0;
            descriptors.Bit10 = formula.Nnum >= 2 ? 1 : 0;
            descriptors.Bit11 = formula.Nnum >= 4 ? 1 : 0;
            descriptors.Bit12 = formula.Nnum >= 8 ? 1 : 0;

            descriptors.Bit13 = formula.Onum >= 1 ? 1 : 0;
            descriptors.Bit14 = formula.Onum >= 2 ? 1 : 0;
            descriptors.Bit15 = formula.Onum >= 4 ? 1 : 0;
            descriptors.Bit16 = formula.Onum >= 8 ? 1 : 0;
            descriptors.Bit17 = formula.Onum >= 16 ? 1 : 0;

            descriptors.Bit18 = formula.Fnum >= 1 ? 1 : 0;
            descriptors.Bit19 = formula.Fnum >= 2 ? 1 : 0;
            descriptors.Bit20 = formula.Fnum >= 4 ? 1 : 0;

            descriptors.Bit21 = formula.Sinum >= 1 ? 1 : 0;
            descriptors.Bit22 = formula.Sinum >= 2 ? 1 : 0;

            descriptors.Bit23 = formula.Pnum >= 1 ? 1 : 0;
            descriptors.Bit24 = formula.Pnum >= 2 ? 1 : 0;
            descriptors.Bit25 = formula.Pnum >= 4 ? 1 : 0;

            descriptors.Bit26 = formula.Snum >= 1 ? 1 : 0;
            descriptors.Bit27 = formula.Snum >= 2 ? 1 : 0;
            descriptors.Bit28 = formula.Snum >= 4 ? 1 : 0;
            descriptors.Bit29 = formula.Snum >= 8 ? 1 : 0;

            descriptors.Bit30 = formula.Clnum >= 1 ? 1 : 0;
            descriptors.Bit31 = formula.Clnum >= 2 ? 1 : 0;
            descriptors.Bit32 = formula.Clnum >= 4 ? 1 : 0;
            descriptors.Bit33 = formula.Clnum >= 8 ? 1 : 0;

            descriptors.Bit34 = formula.Brnum >= 1 ? 1 : 0;
            descriptors.Bit35 = formula.Brnum >= 2 ? 1 : 0;
            descriptors.Bit36 = formula.Brnum >= 4 ? 1 : 0;

            descriptors.Bit37 = formula.Inum >= 1 ? 1 : 0;
            descriptors.Bit38 = formula.Inum >= 2 ? 1 : 0;
            descriptors.Bit39 = formula.Inum >= 4 ? 1 : 0;
        }
        #endregion

        //section 2
        #region
        public static void SetSection2Properties(Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary,
            MolecularFingerprint descriptor)
        {
            foreach (var ringset in ringsetDictionary.Values)
            {

                var descriptorTemp = new MolecularDescriptorRingBasis();
                foreach (var ringID in ringset.RingIDs)
                {
                    var ringProp = ringDictionary[ringID];
                    setMolecularDescriptorOfSection2(descriptorTemp, ringProp);
                }
                setMolecularDescriptorOfSection2(descriptor, descriptorTemp);
            }
        }


        private static void setMolecularDescriptorOfSection2(MolecularFingerprint descriptor, MolecularDescriptorRingBasis descriptorTemp)
        {
            #region from Bit 40 to Bit 53
            if (descriptorTemp.Bit40 >= 2)
            {
                descriptor.Bit47 = 1; //>= 2 any ring size 3
                descriptor.Bit40 = 1; //changed to 1
            }

            if (descriptorTemp.Bit41 >= 2)
            {
                descriptor.Bit48 = 1; //>= 2 saturated or aromatic carbon-only ring size 3
                descriptor.Bit41 = 1; //changed to 1
            }

            if (descriptorTemp.Bit42 >= 2)
            {
                descriptor.Bit49 = 1; //>= 2 saturated or aromatic nitrogen-containing ring size 3
                descriptor.Bit42 = 1; //changed to 1
            }

            if (descriptorTemp.Bit43 >= 2)
            {
                descriptor.Bit50 = 1; //>= 2 saturated or aromatic heteroatom-containing ring size 3
                descriptor.Bit43 = 1; //changed to 1
            }

            if (descriptorTemp.Bit44 >= 2)
            {
                descriptor.Bit51 = 1; //>= 2 unsaturated non-aromatic carbon-only ring size 3
                descriptor.Bit44 = 1; //changed to 1
            }

            if (descriptorTemp.Bit45 >= 2)
            {
                descriptor.Bit52 = 1; //>= 2 unsaturated non-aromatic nitrogen-containing ring size 3
                descriptor.Bit45 = 1; //changed to 1
            }

            if (descriptorTemp.Bit46 >= 2)
            {
                descriptor.Bit53 = 1; //2 unsaturated non-aromatic heteroatom-containing ring size 3
                descriptor.Bit46 = 1; //changed to 1
            }

            #endregion

            #region from Bit 54 to Bit 67
            if (descriptorTemp.Bit54 >= 2)
            {
                descriptor.Bit61 = 1; //>= 2 any ring size 4
                descriptor.Bit54 = 1; //changed to 1
            }

            if (descriptorTemp.Bit55 >= 2)
            {
                descriptor.Bit62 = 1; //>= 2 saturated or aromatic carbon-only ring size 4
                descriptor.Bit55 = 1; //changed to 1
            }

            if (descriptorTemp.Bit56 >= 2)
            {
                descriptor.Bit63 = 1; //>= 2 saturated or aromatic nitrogen-containing ring size 4
                descriptor.Bit56 = 1; //changed to 1
            }

            if (descriptorTemp.Bit57 >= 2)
            {
                descriptor.Bit64 = 1; //>= 2 saturated or aromatic heteroatom-containing ring size 4
                descriptor.Bit57 = 1; //changed to 1
            }

            if (descriptorTemp.Bit58 >= 2)
            {
                descriptor.Bit65 = 1; //>= 2 unsaturated non-aromatic carbon-only ring size 4
                descriptor.Bit58 = 1; //changed to 1
            }

            if (descriptorTemp.Bit59 >= 2)
            {
                descriptor.Bit66 = 1; //>= 2 unsaturated non-aromatic nitrogen-containing ring size 4
                descriptor.Bit59 = 1; //changed to 1
            }

            if (descriptorTemp.Bit60 >= 2)
            {
                descriptor.Bit67 = 1; //2 unsaturated non-aromatic heteroatom-containing ring size 4
                descriptor.Bit60 = 1; //changed to 1
            }
            #endregion

            #region from Bit 68 to 102
            //checking ">= 1 any ring size 5"
            if (descriptorTemp.Bit68 >= 5)
            {
                descriptor.Bit75 = 1;
                descriptor.Bit82 = 1;
                descriptor.Bit89 = 1;
                descriptor.Bit96 = 1;
                descriptor.Bit68 = 1;
            }
            else if (descriptorTemp.Bit68 >= 4)
            {
                descriptor.Bit75 = 1;
                descriptor.Bit82 = 1;
                descriptor.Bit89 = 1;
                descriptor.Bit68 = 1;
            }
            else if (descriptorTemp.Bit68 >= 3)
            {
                descriptor.Bit75 = 1;
                descriptor.Bit82 = 1;
                descriptor.Bit68 = 1;
            }
            else if (descriptorTemp.Bit68 >= 2)
            {
                descriptor.Bit75 = 1;
                descriptor.Bit68 = 1;
            }

            //checking ">= 1 saturated or aromatic carbon-only ring size 5"
            if (descriptorTemp.Bit69 >= 5)
            {
                descriptor.Bit76 = 1;
                descriptor.Bit83 = 1;
                descriptor.Bit90 = 1;
                descriptor.Bit97 = 1;
                descriptor.Bit69 = 1;
            }
            else if (descriptorTemp.Bit69 >= 4)
            {
                descriptor.Bit76 = 1;
                descriptor.Bit83 = 1;
                descriptor.Bit90 = 1;
                descriptor.Bit69 = 1;
            }
            else if (descriptorTemp.Bit69 >= 3)
            {
                descriptor.Bit76 = 1;
                descriptor.Bit83 = 1;
                descriptor.Bit69 = 1;
            }
            else if (descriptorTemp.Bit69 >= 2)
            {
                descriptor.Bit76 = 1;
                descriptor.Bit69 = 1;
            }

            //checking ">= 1 saturated or aromatic nitrogen-containing ring size 5"
            if (descriptorTemp.Bit70 >= 5)
            {
                descriptor.Bit77 = 1;
                descriptor.Bit84 = 1;
                descriptor.Bit91 = 1;
                descriptor.Bit98 = 1;
                descriptor.Bit70 = 1;
            }
            else if (descriptorTemp.Bit70 >= 4)
            {
                descriptor.Bit77 = 1;
                descriptor.Bit84 = 1;
                descriptor.Bit91 = 1;
                descriptor.Bit70 = 1;
            }
            else if (descriptorTemp.Bit70 >= 3)
            {
                descriptor.Bit77 = 1;
                descriptor.Bit84 = 1;
                descriptor.Bit70 = 1;
            }
            else if (descriptorTemp.Bit70 >= 2)
            {
                descriptor.Bit77 = 1;
                descriptor.Bit70 = 1;
            }

            //checking ">= 1 saturated or aromatic heteroatom-containing ring size 5"
            if (descriptorTemp.Bit71 >= 5)
            {
                descriptor.Bit78 = 1;
                descriptor.Bit85 = 1;
                descriptor.Bit92 = 1;
                descriptor.Bit99 = 1;
                descriptor.Bit71 = 1;
            }
            else if (descriptorTemp.Bit71 >= 4)
            {
                descriptor.Bit78 = 1;
                descriptor.Bit85 = 1;
                descriptor.Bit92 = 1;
                descriptor.Bit71 = 1;
            }
            else if (descriptorTemp.Bit71 >= 3)
            {
                descriptor.Bit78 = 1;
                descriptor.Bit85 = 1;
                descriptor.Bit71 = 1;
            }
            else if (descriptorTemp.Bit71 >= 2)
            {
                descriptor.Bit78 = 1;
                descriptor.Bit71 = 1;
            }

            //checking ">= 1 unsaturated non-aromatic carbon-only ring size 5"
            if (descriptorTemp.Bit72 >= 5)
            {
                descriptor.Bit79 = 1;
                descriptor.Bit86 = 1;
                descriptor.Bit93 = 1;
                descriptor.Bit100 = 1;
                descriptor.Bit72 = 1;
            }
            else if (descriptorTemp.Bit72 >= 4)
            {
                descriptor.Bit79 = 1;
                descriptor.Bit86 = 1;
                descriptor.Bit93 = 1;
                descriptor.Bit72 = 1;
            }
            else if (descriptorTemp.Bit72 >= 3)
            {
                descriptor.Bit79 = 1;
                descriptor.Bit86 = 1;
                descriptor.Bit72 = 1;
            }
            else if (descriptorTemp.Bit72 >= 2)
            {
                descriptor.Bit79 = 1;
                descriptor.Bit72 = 1;
            }

            //checking ">= 1 unsaturated non-aromatic nitrogen-containing ring size 5"
            if (descriptorTemp.Bit73 >= 5)
            {
                descriptor.Bit80 = 1;
                descriptor.Bit87 = 1;
                descriptor.Bit94 = 1;
                descriptor.Bit101 = 1;
                descriptor.Bit73 = 1;
            }
            else if (descriptorTemp.Bit73 >= 4)
            {
                descriptor.Bit80 = 1;
                descriptor.Bit87 = 1;
                descriptor.Bit94 = 1;
                descriptor.Bit73 = 1;
            }
            else if (descriptorTemp.Bit73 >= 3)
            {
                descriptor.Bit80 = 1;
                descriptor.Bit87 = 1;
                descriptor.Bit73 = 1;
            }
            else if (descriptorTemp.Bit73 >= 2)
            {
                descriptor.Bit80 = 1;
                descriptor.Bit73 = 1;
            }

            //checking ">= 1 unsaturated non-aromatic heteroatom-containing ring size 5"
            if (descriptorTemp.Bit74 >= 5)
            {
                descriptor.Bit81 = 1;
                descriptor.Bit88 = 1;
                descriptor.Bit95 = 1;
                descriptor.Bit102 = 1;
                descriptor.Bit74 = 1;
            }
            else if (descriptorTemp.Bit74 >= 4)
            {
                descriptor.Bit81 = 1;
                descriptor.Bit88 = 1;
                descriptor.Bit95 = 1;
                descriptor.Bit74 = 1;
            }
            else if (descriptorTemp.Bit74 >= 3)
            {
                descriptor.Bit81 = 1;
                descriptor.Bit88 = 1;
                descriptor.Bit74 = 1;
            }
            else if (descriptorTemp.Bit74 >= 2)
            {
                descriptor.Bit81 = 1;
                descriptor.Bit74 = 1;
            }
            #endregion

            #region from Bit 103 to 137
            //checking ">= 1 any ring size 6"
            if (descriptorTemp.Bit103 >= 5)
            {
                descriptor.Bit110 = 1;
                descriptor.Bit117 = 1;
                descriptor.Bit124 = 1;
                descriptor.Bit131 = 1;
                descriptor.Bit103 = 1;
            }
            else if (descriptorTemp.Bit103 >= 4)
            {
                descriptor.Bit110 = 1;
                descriptor.Bit117 = 1;
                descriptor.Bit124 = 1;
                descriptor.Bit103 = 1;
            }
            else if (descriptorTemp.Bit103 >= 3)
            {
                descriptor.Bit110 = 1;
                descriptor.Bit117 = 1;
                descriptor.Bit103 = 1;
            }
            else if (descriptorTemp.Bit103 >= 2)
            {
                descriptor.Bit110 = 1;
                descriptor.Bit103 = 1;
            }

            //checking ">= 1 saturated or aromatic carbon-only ring size 6"
            if (descriptorTemp.Bit104 >= 5)
            {
                descriptor.Bit111 = 1;
                descriptor.Bit118 = 1;
                descriptor.Bit125 = 1;
                descriptor.Bit132 = 1;
                descriptor.Bit104 = 1;
            }
            else if (descriptorTemp.Bit104 >= 4)
            {
                descriptor.Bit111 = 1;
                descriptor.Bit83 = 1;
                descriptor.Bit125 = 1;
                descriptor.Bit104 = 1;
            }
            else if (descriptorTemp.Bit104 >= 3)
            {
                descriptor.Bit111 = 1;
                descriptor.Bit83 = 1;
                descriptor.Bit104 = 1;
            }
            else if (descriptorTemp.Bit104 >= 2)
            {
                descriptor.Bit111 = 1;
                descriptor.Bit104 = 1;
            }

            //checking ">= 1 saturated or aromatic nitrogen-containing ring size 6"
            if (descriptorTemp.Bit105 >= 5)
            {
                descriptor.Bit112 = 1;
                descriptor.Bit119 = 1;
                descriptor.Bit126 = 1;
                descriptor.Bit133 = 1;
                descriptor.Bit105 = 1;
            }
            else if (descriptorTemp.Bit105 >= 4)
            {
                descriptor.Bit112 = 1;
                descriptor.Bit119 = 1;
                descriptor.Bit126 = 1;
                descriptor.Bit105 = 1;
            }
            else if (descriptorTemp.Bit105 >= 3)
            {
                descriptor.Bit112 = 1;
                descriptor.Bit119 = 1;
                descriptor.Bit105 = 1;
            }
            else if (descriptorTemp.Bit105 >= 2)
            {
                descriptor.Bit112 = 1;
                descriptor.Bit105 = 1;
            }

            //checking ">= 1 saturated or aromatic heteroatom-containing ring size 6"
            if (descriptorTemp.Bit106 >= 5)
            {
                descriptor.Bit113 = 1;
                descriptor.Bit120 = 1;
                descriptor.Bit127 = 1;
                descriptor.Bit134 = 1;
                descriptor.Bit106 = 1;
            }
            else if (descriptorTemp.Bit106 >= 4)
            {
                descriptor.Bit113 = 1;
                descriptor.Bit120 = 1;
                descriptor.Bit127 = 1;
                descriptor.Bit106 = 1;
            }
            else if (descriptorTemp.Bit106 >= 3)
            {
                descriptor.Bit113 = 1;
                descriptor.Bit120 = 1;
                descriptor.Bit106 = 1;
            }
            else if (descriptorTemp.Bit106 >= 2)
            {
                descriptor.Bit113 = 1;
                descriptor.Bit106 = 1;
            }

            //checking ">= 1 unsaturated non-aromatic carbon-only ring size 6"
            if (descriptorTemp.Bit107 >= 5)
            {
                descriptor.Bit114 = 1;
                descriptor.Bit121 = 1;
                descriptor.Bit128 = 1;
                descriptor.Bit135 = 1;
                descriptor.Bit72 = 1;
            }
            else if (descriptorTemp.Bit72 >= 4)
            {
                descriptor.Bit114 = 1;
                descriptor.Bit121 = 1;
                descriptor.Bit128 = 1;
                descriptor.Bit72 = 1;
            }
            else if (descriptorTemp.Bit72 >= 3)
            {
                descriptor.Bit114 = 1;
                descriptor.Bit121 = 1;
                descriptor.Bit72 = 1;
            }
            else if (descriptorTemp.Bit72 >= 2)
            {
                descriptor.Bit114 = 1;
                descriptor.Bit72 = 1;
            }

            //checking ">= 1 unsaturated non-aromatic nitrogen-containing ring size 6"
            if (descriptorTemp.Bit108 >= 5)
            {
                descriptor.Bit115 = 1;
                descriptor.Bit122 = 1;
                descriptor.Bit129 = 1;
                descriptor.Bit136 = 1;
                descriptor.Bit108 = 1;
            }
            else if (descriptorTemp.Bit108 >= 4)
            {
                descriptor.Bit115 = 1;
                descriptor.Bit122 = 1;
                descriptor.Bit129 = 1;
                descriptor.Bit108 = 1;
            }
            else if (descriptorTemp.Bit108 >= 3)
            {
                descriptor.Bit115 = 1;
                descriptor.Bit122 = 1;
                descriptor.Bit108 = 1;
            }
            else if (descriptorTemp.Bit108 >= 2)
            {
                descriptor.Bit115 = 1;
                descriptor.Bit108 = 1;
            }

            //checking ">= 1 unsaturated non-aromatic heteroatom-containing ring size 6"
            if (descriptorTemp.Bit109 >= 5)
            {
                descriptor.Bit116 = 1;
                descriptor.Bit123 = 1;
                descriptor.Bit130 = 1;
                descriptor.Bit137 = 1;
                descriptor.Bit109 = 1;
            }
            else if (descriptorTemp.Bit109 >= 4)
            {
                descriptor.Bit116 = 1;
                descriptor.Bit123 = 1;
                descriptor.Bit130 = 1;
                descriptor.Bit109 = 1;
            }
            else if (descriptorTemp.Bit109 >= 3)
            {
                descriptor.Bit116 = 1;
                descriptor.Bit123 = 1;
                descriptor.Bit109 = 1;
            }
            else if (descriptorTemp.Bit109 >= 2)
            {
                descriptor.Bit116 = 1;
                descriptor.Bit109 = 1;
            }
            #endregion

            #region from Bit 138 to Bit 151
            if (descriptorTemp.Bit138 >= 2)
            {
                descriptor.Bit145 = 1; //>= 2 any ring size 7
                descriptor.Bit138 = 1; //changed to 1
            }

            if (descriptorTemp.Bit139 >= 2)
            {
                descriptor.Bit146 = 1; //>= 2 saturated or aromatic carbon-only ring size 7
                descriptor.Bit139 = 1; //changed to 1
            }

            if (descriptorTemp.Bit140 >= 2)
            {
                descriptor.Bit147 = 1; //>= 2 saturated or aromatic nitrogen-containing ring size 7
                descriptor.Bit140 = 1; //changed to 1
            }

            if (descriptorTemp.Bit141 >= 2)
            {
                descriptor.Bit148 = 1; //>= 2 saturated or aromatic heteroatom-containing ring size 7
                descriptor.Bit141 = 1; //changed to 1
            }

            if (descriptorTemp.Bit142 >= 2)
            {
                descriptor.Bit149 = 1; //>= 2 unsaturated non-aromatic carbon-only ring size 7
                descriptor.Bit142 = 1; //changed to 1
            }

            if (descriptorTemp.Bit143 >= 2)
            {
                descriptor.Bit150 = 1; //>= 2 unsaturated non-aromatic nitrogen-containing ring size 7
                descriptor.Bit143 = 1; //changed to 1
            }

            if (descriptorTemp.Bit144 >= 2)
            {
                descriptor.Bit151 = 1; //2 unsaturated non-aromatic heteroatom-containing ring size 7
                descriptor.Bit144 = 1; //changed to 1
            }
            #endregion

            #region from Bit 152 to Bit 165
            if (descriptorTemp.Bit152 >= 2)
            {
                descriptor.Bit159 = 1; //>= 2 any ring size 8
                descriptor.Bit152 = 1; //changed to 1
            }

            if (descriptorTemp.Bit153 >= 2)
            {
                descriptor.Bit160 = 1; //>= 2 saturated or aromatic carbon-only ring size 8
                descriptor.Bit153 = 1; //changed to 1
            }

            if (descriptorTemp.Bit154 >= 2)
            {
                descriptor.Bit161 = 1; //>= 2 saturated or aromatic nitrogen-containing ring size 8
                descriptor.Bit154 = 1; //changed to 1
            }

            if (descriptorTemp.Bit155 >= 2)
            {
                descriptor.Bit162 = 1; //>= 2 saturated or aromatic heteroatom-containing ring size 8
                descriptor.Bit155 = 1; //changed to 1
            }

            if (descriptorTemp.Bit156 >= 2)
            {
                descriptor.Bit163 = 1; //>= 2 unsaturated non-aromatic carbon-only ring size 8
                descriptor.Bit156 = 1; //changed to 1
            }

            if (descriptorTemp.Bit157 >= 2)
            {
                descriptor.Bit164 = 1; //>= 2 unsaturated non-aromatic nitrogen-containing ring size 8
                descriptor.Bit157 = 1; //changed to 1
            }

            if (descriptorTemp.Bit158 >= 2)
            {
                descriptor.Bit165 = 1; //2 unsaturated non-aromatic heteroatom-containing ring size 8
                descriptor.Bit158 = 1; //changed to 1
            }
            #endregion

            #region from Bit 166 to Bit 179
            if (descriptorTemp.Bit166 > 0) descriptor.Bit166 = 1;
            if (descriptorTemp.Bit167 > 0) descriptor.Bit167 = 1;
            if (descriptorTemp.Bit168 > 0) descriptor.Bit168 = 1;
            if (descriptorTemp.Bit169 > 0) descriptor.Bit169 = 1;
            if (descriptorTemp.Bit170 > 0) descriptor.Bit170 = 1;
            if (descriptorTemp.Bit171 > 0) descriptor.Bit171 = 1;
            if (descriptorTemp.Bit171 > 0) descriptor.Bit172 = 1;
            if (descriptorTemp.Bit171 > 0) descriptor.Bit173 = 1;
            if (descriptorTemp.Bit174 > 0) descriptor.Bit174 = 1;
            if (descriptorTemp.Bit175 > 0) descriptor.Bit175 = 1;
            if (descriptorTemp.Bit176 > 0) descriptor.Bit176 = 1;
            if (descriptorTemp.Bit177 > 0) descriptor.Bit177 = 1;
            if (descriptorTemp.Bit178 > 0) descriptor.Bit178 = 1;
            if (descriptorTemp.Bit179 > 0) descriptor.Bit179 = 1;
            #endregion

            #region from Bit180 to Bit 187
            if (descriptorTemp.Bit180 >= 4)
            {
                descriptor.Bit186 = 1;
                descriptor.Bit184 = 1;
                descriptor.Bit182 = 1;
                descriptor.Bit180 = 1;
            }
            else if (descriptorTemp.Bit180 >= 3)
            {
                descriptor.Bit184 = 1;
                descriptor.Bit182 = 1;
                descriptor.Bit180 = 1;
            }
            else if (descriptorTemp.Bit180 >= 2)
            {
                descriptor.Bit182 = 1;
                descriptor.Bit180 = 1;
            }

            if (descriptorTemp.Bit181 >= 4)
            {
                descriptor.Bit187 = 1;
                descriptor.Bit185 = 1;
                descriptor.Bit183 = 1;
                descriptor.Bit181 = 1;
            }
            else if (descriptorTemp.Bit181 >= 3)
            {
                descriptor.Bit185 = 1;
                descriptor.Bit183 = 1;
                descriptor.Bit181 = 1;
            }
            else if (descriptorTemp.Bit181 >= 2)
            {
                descriptor.Bit183 = 1;
                descriptor.Bit181 = 1;
            }
            #endregion
        }

        /// <summary>
        /// This method judges 1 cycle ring descriptors.
        /// The trick of this method is to increment the existence of ring descriptor that we can find in a structure.
        /// Finally, this descriptor will be changed to another static (above) method to manage ring counts for each single ring descriptor.
        /// </summary>
        private static void setMolecularDescriptorOfSection2(MolecularDescriptorRingBasis descriptor, RingProperty ringProp)
        {
            #region from Bit 40 to Bit 46
            if (ringProp.ConnectedAtoms.Count >= 3)
            { //ring size >= 3
                descriptor.Bit40 += 1;  //>= 1 any ring size 3

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit41 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 3
                    descriptor.Bit42 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 3
                    descriptor.Bit43 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 3
                }
                else
                { //unsaturated
                    descriptor.Bit44 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 3
                    descriptor.Bit45 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 3
                    descriptor.Bit46 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 3
                }
            }
            #endregion

            #region from Bit 54 to Bit 60
            if (ringProp.ConnectedAtoms.Count >= 4)
            { //ring size >= 4

                descriptor.Bit54 += 1; //>= 1 any ring size 4

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit55 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 4
                    descriptor.Bit56 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 4
                    descriptor.Bit57 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 4
                }
                else
                { //unsaturated
                    descriptor.Bit58 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 4
                    descriptor.Bit59 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 4
                    descriptor.Bit60 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 4
                }
            }
            #endregion

            #region from Bit 68 to Bit 74
            if (ringProp.ConnectedAtoms.Count >= 5)
            { //ring size >= 5

                descriptor.Bit68 += 1; //>= 1 any ring size 5

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit69 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 5
                    descriptor.Bit70 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 5
                    descriptor.Bit71 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 5
                }
                else
                { //unsaturated
                    descriptor.Bit72 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 5
                    descriptor.Bit73 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 5
                    descriptor.Bit74 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 5
                }
            }
            #endregion

            #region from Bit 103 to Bit 109
            if (ringProp.ConnectedAtoms.Count >= 6)
            { //ring size >= 6

                descriptor.Bit103 += 1; //>= 1 any ring size 6

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit104 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 6
                    descriptor.Bit105 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 6
                    descriptor.Bit106 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 6
                }
                else
                { //unsaturated
                    descriptor.Bit107 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 6
                    descriptor.Bit108 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 6
                    descriptor.Bit109 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 6
                }
            }
            #endregion

            #region from Bit 138 to Bit 144
            if (ringProp.ConnectedAtoms.Count >= 7)
            { //ring size >= 7

                descriptor.Bit138 += 1; //>= 1 any ring size 7

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit139 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 7
                    descriptor.Bit140 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 7
                    descriptor.Bit141 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 7
                }
                else
                { //unsaturated
                    descriptor.Bit142 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 7
                    descriptor.Bit143 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 7
                    descriptor.Bit144 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 7
                }
            }
            #endregion

            #region from Bit 152 to Bit 158
            if (ringProp.ConnectedAtoms.Count >= 8)
            { //ring size >= 8

                descriptor.Bit152 += 1; //>= 1 any ring size 8

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit153 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 8
                    descriptor.Bit154 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 8
                    descriptor.Bit155 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 8
                }
                else
                { //unsaturated
                    descriptor.Bit156 += ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 8
                    descriptor.Bit157 += ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 8
                    descriptor.Bit158 += ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 8
                }
            }
            #endregion

            #region from Bit 166 to Bit 172
            if (ringProp.ConnectedAtoms.Count >= 9)
            { //ring size >= 9

                descriptor.Bit166 = 1; //>= 1 any ring size 9

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit167 = ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 9
                    descriptor.Bit168 = ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 9
                    descriptor.Bit169 = ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 9
                }
                else
                { //unsaturated
                    descriptor.Bit170 = ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 9
                    descriptor.Bit171 = ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 9
                    descriptor.Bit172 = ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 9
                }
            }
            #endregion

            #region from Bit 173 to Bit 179
            if (ringProp.ConnectedAtoms.Count >= 10)
            { //ring size >= 10

                descriptor.Bit173 = 1; //>= 1 any ring size 10

                if (ringProp.RingEnv.DoublebondInRing == 0 || ringProp.IsAromaticRing)
                { //saturated or aromatic
                    descriptor.Bit174 = ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 saturated or aromatic carbon-only ring size 10
                    descriptor.Bit175 = ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 saturated or aromatic nitrogen-containing ring size 10
                    descriptor.Bit176 = ringProp.IsHeteroRing ? 1 : 0; //>= 1 saturated or aromatic heteroatom-containing ring size 10
                }
                else
                { //unsaturated
                    descriptor.Bit177 = ringProp.ConnectedAtoms.Count() == ringProp.RingEnv.CarbonInRing ? 1 : 0; //>= 1 unsaturated non-aromatic carbon-only ring size 10
                    descriptor.Bit178 = ringProp.RingEnv.NitorogenInRing > 0 ? 1 : 0; //>= 1 unsaturated non-aromatic nitrogen-containing ring size 10
                    descriptor.Bit179 = ringProp.IsHeteroRing ? 1 : 0; //>= 1 unsaturated non-aromatic heteroatom-containing ring size 10
                }
            }
            #endregion

            #region from Bit 180 to Bit 181
            if (ringProp.IsAromaticRing)
            {
                descriptor.Bit180 += 1;
            }
            if (ringProp.IsAromaticRing && ringProp.IsHeteroRing)
            {
                descriptor.Bit181 += 1;
            }
            #endregion
        }
        #endregion

        //section 3
        #region
        public static void SetfSection3Properties(MolecularFingerprint descriptor, BondProperty bondProp)
        {
            var bondString = MoleculeConverter.BondPropertyToString(bondProp);
            if (bondString == "C-H") descriptor.Bit188 = 1;
            else if (bondString == "C-C") descriptor.Bit189 = 1;
            else if (bondString == "C-N") descriptor.Bit190 = 1;
            else if (bondString == "C-O") descriptor.Bit191 = 1;
            else if (bondString == "C-F") descriptor.Bit192 = 1;
            else if (bondString == "C-Si") descriptor.Bit193 = 1;
            else if (bondString == "C-P") descriptor.Bit194 = 1;
            else if (bondString == "C-S") descriptor.Bit195 = 1;
            else if (bondString == "C-Cl") descriptor.Bit196 = 1;
            else if (bondString == "C-I") descriptor.Bit197 = 1;
            else if (bondString == "N-H") descriptor.Bit198 = 1;
            else if (bondString == "N-N") descriptor.Bit199 = 1;
            else if (bondString == "N-O") descriptor.Bit200 = 1;
            else if (bondString == "N-F") descriptor.Bit201 = 1;
            else if (bondString == "N-Si") descriptor.Bit202 = 1;
            else if (bondString == "N-P") descriptor.Bit203 = 1;
            else if (bondString == "N-S") descriptor.Bit204 = 1;
            else if (bondString == "N-Cl") descriptor.Bit205 = 1;
            else if (bondString == "N-Br") descriptor.Bit206 = 1;
            else if (bondString == "O-H") descriptor.Bit207 = 1;
            else if (bondString == "O-O") descriptor.Bit208 = 1;
            else if (bondString == "O-Si") descriptor.Bit209 = 1;
            else if (bondString == "O-P") descriptor.Bit210 = 1;
            else if (bondString == "P-F") descriptor.Bit211 = 1;
            else if (bondString == "S-F") descriptor.Bit212 = 1;
            else if (bondString == "Si-H") descriptor.Bit213 = 1;
            else if (bondString == "Si-Si") descriptor.Bit214 = 1;
            else if (bondString == "Si-Cl") descriptor.Bit215 = 1;
            else if (bondString == "P-H") descriptor.Bit216 = 1;
            else if (bondString == "P-P") descriptor.Bit217 = 1;
            else if (bondString == "C=C") descriptor.Bit307 = 1;
            else if (bondString == "C#C") descriptor.Bit308 = 1;
            else if (bondString == "C=N") descriptor.Bit309 = 1;
            else if (bondString == "C#C") descriptor.Bit310 = 1;
            else if (bondString == "C=O") descriptor.Bit311 = 1;
            else if (bondString == "C=S") descriptor.Bit312 = 1;
            else if (bondString == "N=N") descriptor.Bit313 = 1;
            else if (bondString == "N=O") descriptor.Bit314 = 1;
            else if (bondString == "N=P") descriptor.Bit315 = 1;
            else if (bondString == "O=P") descriptor.Bit316 = 1;
            else if (bondString == "P=P") descriptor.Bit317 = 1;
        }

        #endregion

        //section 4 and 5
        #region
        public static void SetSection4and5Properties(MolecularFingerprint descriptors, Dictionary<int, AtomProperty> atomDictionary)
        {
            foreach (var atom in atomDictionary)
            {
                var atomProp = atom.Value;
                var atomEnv = atomProp.AtomEnv;
                switch (atomProp.AtomString)
                {
                    case "C":
                        #region Section 4
                        if (atomEnv.BrCount > 0)
                        {
                            if (atomEnv.CarbonCount != 0) descriptors.Bit218 = 1;
                            if (atomEnv.CarbonCount >= 2) descriptors.Bit219 = 1;
                            if (atomEnv.HydrogenCount != 0) descriptors.Bit220 = 1;
                            if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit221 = 1;
                            if (atomEnv.AromaticNitrogenCount != 0) descriptors.Bit222 = 1;
                        }
                        else if (atomEnv.ClCount > 0)
                        {
                            if (atomEnv.CarbonCount != 0) descriptors.Bit233 = 1;
                            if (atomEnv.CarbonCount != 0 && atomEnv.HydrogenCount != 0) descriptors.Bit234 = 1;
                            if (atomEnv.ClCount >= 2) descriptors.Bit251 = 1;
                            if (atomEnv.HydrogenCount != 0) descriptors.Bit252 = 1;
                            if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit253 = 1;
                        }
                        else if (atomEnv.FCount > 0)
                        {
                            if (atomEnv.FCount >= 2) descriptors.Bit254 = 1;
                            if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit255 = 1;
                        }
                        else if (atomEnv.SiCount > 0)
                        {
                            if (atomEnv.CarbonCount != 0) descriptors.Bit245 = 1;
                            if (atomEnv.HydrogenCount != 0) descriptors.Bit260 = 1;
                        }
                        else if (atomEnv.ICount > 0)
                        {
                            if (atomEnv.CarbonCount != 0) descriptors.Bit241 = 1;
                        }
                        else
                        {
                            if (atomEnv.CarbonCount >= 2)
                            {
                                descriptors.Bit223 = 1;
                                if (atomEnv.HydrogenCount != 0 && atomEnv.NitrogenCount != 0) descriptors.Bit229 = 1;
                                if (atomEnv.HydrogenCount != 0 && atomEnv.OxygenCount != 0) descriptors.Bit230 = 1;
                                if (atomEnv.NitrogenCount != 0) descriptors.Bit231 = 1;
                                if (atomEnv.OxygenCount != 0) descriptors.Bit232 = 1;
                                if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit246 = 1;
                                if (atomEnv.AromaticCarbonCount != 0 && atomEnv.AromaticNitrogenCount != 0) descriptors.Bit248 = 1;
                            }

                            if (atomEnv.CarbonCount >= 4) descriptors.Bit225 = 1;

                            if (atomEnv.CarbonCount >= 3)
                            {
                                descriptors.Bit224 = 1;
                                if (atomEnv.HydrogenCount != 0) descriptors.Bit226 = 1;
                                if (atomEnv.NitrogenCount != 0) descriptors.Bit227 = 1;
                                if (atomEnv.OxygenCount != 0) descriptors.Bit228 = 1;
                                if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit247 = 1;
                            }

                            if (atomEnv.CarbonCount != 0)
                            {
                                if (atomEnv.HydrogenCount != 0) descriptors.Bit235 = 1;
                                if (atomEnv.HydrogenCount != 0 && atomEnv.NitrogenCount != 0) descriptors.Bit236 = 1;
                                if (atomEnv.HydrogenCount != 0 && atomEnv.OxygenCount != 0) descriptors.Bit237 = 1;
                                if (atomEnv.HydrogenCount != 0 && atomEnv.OxygenCount >= 2) descriptors.Bit238 = 1;
                                if (atomEnv.HydrogenCount != 0 && atomEnv.PhosphorusCount != 0) descriptors.Bit239 = 1;
                                if (atomEnv.HydrogenCount != 0 && atomEnv.SulfurCount != 0) descriptors.Bit240 = 1;
                                if (atomEnv.NitrogenCount != 0) descriptors.Bit242 = 1;
                                if (atomEnv.OxygenCount != 0) descriptors.Bit243 = 1;
                                if (atomEnv.SulfurCount != 0) descriptors.Bit244 = 1;
                                if (atomEnv.AromaticNitrogenCount != 0) descriptors.Bit249 = 1;
                                if (atomEnv.AromaticNitrogenCount >= 2) descriptors.Bit250 = 1;
                            }

                            if (atomEnv.HydrogenCount != 0)
                            {
                                if (atomEnv.NitrogenCount != 0) descriptors.Bit256 = 1;
                                if (atomEnv.OxygenCount != 0) descriptors.Bit257 = 1;
                                if (atomEnv.OxygenCount >= 2) descriptors.Bit258 = 1;
                                if (atomEnv.SulfurCount != 0) descriptors.Bit259 = 1;
                                if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit261 = 1;
                                if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit262 = 1;
                                if (atomEnv.AromaticCarbonCount != 0 && atomEnv.AromaticNitrogenCount != 0) descriptors.Bit263 = 1;
                                if (atomEnv.AromaticNitrogenCount != 0) descriptors.Bit264 = 1;
                            }

                            if (atomEnv.HydrogenCount >= 3) descriptors.Bit265 = 1;
                            if (atomEnv.NitrogenCount >= 2)
                            {
                                descriptors.Bit266 = 1;
                                if (atomEnv.AromaticCarbonCount != 0 && atomEnv.AromaticNitrogenCount != 0) descriptors.Bit269 = 1;
                                if (atomEnv.AromaticNitrogenCount != 0) descriptors.Bit270 = 1;
                            }
                            if (atomEnv.NitrogenCount != 0)
                            {
                                if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit267 = 1;
                                if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit268 = 1;
                            }

                            if (atomEnv.OxygenCount >= 2) descriptors.Bit271 = 1;
                            if (atomEnv.OxygenCount != 0 && atomEnv.AromaticCarbonCount != 0) descriptors.Bit272 = 1;
                            if (atomEnv.OxygenCount != 0 && atomEnv.AromaticCarbonCount >= 2) descriptors.Bit273 = 1;
                            if (atomEnv.SulfurCount != 0 && atomEnv.AromaticCarbonCount != 0) descriptors.Bit274 = 1;
                            if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit275 = 1;
                            if (atomEnv.AromaticCarbonCount >= 3) descriptors.Bit276 = 1;
                            if (atomEnv.AromaticCarbonCount >= 2 && atomEnv.AromaticNitrogenCount != 0) descriptors.Bit277 = 1;
                            if (atomEnv.AromaticCarbonCount != 0 && atomEnv.AromaticNitrogenCount != 0) descriptors.Bit278 = 1;
                            if (atomEnv.AromaticCarbonCount != 0 && atomEnv.AromaticNitrogenCount >= 2) descriptors.Bit279 = 1;
                            if (atomEnv.AromaticNitrogenCount >= 2) descriptors.Bit280 = 1;
                        }
                        #endregion
                        #region Section 5
                        if (atomEnv.DoubleBond_CC_Count >= 1)
                        {
                            if (atomEnv.SingleBond_CarbonCount >= 2) descriptors.Bit321 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit332 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_HydrogenCount >= 1) descriptors.Bit325 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit328 = 1;
                            if (atomEnv.SingleBond_HydrogenCount >= 1 && atomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit336 = 1;
                            if (atomEnv.SingleBond_HydrogenCount >= 1) descriptors.Bit337 = 1;
                            if (atomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit340 = 1;
                        }

                        if (atomEnv.DoubleBond_CN_Count >= 1)
                        {
                            if (atomEnv.SingleBond_CarbonCount >= 2) descriptors.Bit322 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_HydrogenCount >= 1) descriptors.Bit326 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit329 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit333 = 1;
                            if (atomEnv.SingleBond_HydrogenCount >= 1) descriptors.Bit338 = 1;
                            if (atomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit341 = 1;
                        }


                        if (atomEnv.DoubleBond_CO_Count >= 1)
                        {
                            if (atomEnv.SingleBond_CarbonCount >= 2) descriptors.Bit323 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_ClCount >= 1) descriptors.Bit324 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_HydrogenCount >= 1) descriptors.Bit327 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit330 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1 && atomEnv.SingleBond_OxygenCount >= 1) descriptors.Bit331 = 1;
                            if (atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit334 = 1;
                            if (atomEnv.SingleBond_ClCount >= 1) descriptors.Bit335 = 1;
                            if (atomEnv.SingleBond_HydrogenCount >= 1) descriptors.Bit339 = 1;
                            if (atomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit342 = 1;
                            if (atomEnv.SingleBond_OxygenCount >= 1) descriptors.Bit343 = 1;
                        }

                        if (atomEnv.TripleBond_CC_Count >= 1)
                        {
                            if (atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit318 = 1;
                            if (atomEnv.SingleBond_HydrogenCount >= 1) descriptors.Bit319 = 1;
                        }

                        if (atomEnv.TripleBond_CN_Count >= 1 && atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit320 = 1;
                        #endregion
                        break;
                    case "N":
                        #region Section 4
                        if (atomEnv.CarbonCount >= 2)
                        {
                            descriptors.Bit281 = 1;
                            if (atomEnv.HydrogenCount != 0) descriptors.Bit283 = 1;
                            if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit287 = 1;
                            if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit294 = 1;
                        }

                        if (atomEnv.CarbonCount >= 3)
                        {
                            descriptors.Bit282 = 1;
                            if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit288 = 1;
                            if (atomEnv.AromaticCarbonCount >= 3) descriptors.Bit295 = 1;
                        }

                        if (atomEnv.CarbonCount != 0)
                        {
                            if (atomEnv.HydrogenCount != 0) descriptors.Bit284 = 1;
                            if (atomEnv.HydrogenCount != 0 && atomEnv.NitrogenCount != 0) descriptors.Bit285 = 1;
                            if (atomEnv.OxygenCount != 0) descriptors.Bit286 = 1;
                        }

                        if (atomEnv.HydrogenCount != 0)
                        {
                            if (atomEnv.NitrogenCount != 0) descriptors.Bit289 = 1;
                            if (atomEnv.AromaticCarbonCount != 0) descriptors.Bit290 = 1;
                            if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit291 = 1;
                        }

                        if (atomEnv.OxygenCount >= 2)
                        {
                            descriptors.Bit292 = 1;
                            if (atomEnv.AromaticOxygenCount != 0) descriptors.Bit293 = 1;
                        }
                        #endregion
                        #region Section 5
                        if (atomEnv.DoubleBond_CN_Count >= 1 && atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit344 = 1;
                        if (atomEnv.DoubleBond_NO_Count >= 1 && atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit345 = 1;
                        if (atomEnv.DoubleBond_NO_Count >= 1 && atomEnv.SingleBond_OxygenCount >= 1) descriptors.Bit346 = 1;
                        #endregion
                        break;
                    case "O":
                        #region
                        if (atomEnv.CarbonCount >= 2) descriptors.Bit296 = 1;
                        if (atomEnv.CarbonCount != 0 && atomEnv.HydrogenCount != 0) descriptors.Bit297 = 1;
                        if (atomEnv.CarbonCount != 0 && atomEnv.PhosphorusCount != 0) descriptors.Bit298 = 1;
                        if (atomEnv.SulfurCount != 0 && atomEnv.HydrogenCount != 0) descriptors.Bit299 = 1;
                        if (atomEnv.AromaticCarbonCount >= 2) descriptors.Bit300 = 1;
                        break;
                    #endregion
                    case "P":
                        #region
                        if (atomEnv.CarbonCount >= 2) descriptors.Bit301 = 1;
                        if (atomEnv.OxygenCount >= 2) descriptors.Bit302 = 1;
                        if (atomEnv.DoubleBond_PO_Count >= 1 && atomEnv.SingleBond_OxygenCount >= 1) descriptors.Bit347 = 1;
                        break;
                    #endregion
                    case "S":
                        #region
                        if (atomEnv.CarbonCount >= 2) descriptors.Bit303 = 1;
                        if (atomEnv.CarbonCount != 0 && atomEnv.HydrogenCount != 0) descriptors.Bit304 = 1;
                        if (atomEnv.CarbonCount != 0 && atomEnv.OxygenCount != 0) descriptors.Bit305 = 1;
                        if (atomEnv.DoubleBond_SO_Count >= 1 && atomEnv.SingleBond_CarbonCount >= 1) descriptors.Bit348 = 1;
                        if (atomEnv.DoubleBond_SO_Count >= 1 && atomEnv.SingleBond_OxygenCount >= 1) descriptors.Bit349 = 1;
                        if (atomEnv.DoubleBond_SO_Count >= 2) descriptors.Bit350 = 1;
                        break;
                    #endregion
                    case "Si":
                        #region
                        if (atomEnv.CarbonCount >= 2) descriptors.Bit306 = 1;
                        break;
                        #endregion
                }
            }
        }

        #endregion

        //section 6
        #region
        public static void SetSection6Properties(MolecularFingerprint descriptors, string connectString, string initialString, int connectionCount)
        {
            switch (connectionCount)
            {
                case 4:
                    #region
                    switch (initialString)
                    {
                        case "H-":
                            if (connectString == "H-N-N-H") descriptors.Bit401 = 1;
                            else if (connectString == "H-C=C-H") descriptors.Bit406 = 1;
                            else if (connectString == "H-N-C-H") descriptors.Bit417 = 1;
                            else if (connectString == "H-C-O-H") descriptors.Bit460 = 1;
                            break;

                        case "C-":
                            if (connectString == "C-C-C#C") descriptors.Bit351 = 1;
                            else if (connectString == "C-O-C=C") descriptors.Bit367 = 1;
                            else if (connectString == "C-C-C=C") descriptors.Bit381 = 1;
                            else if (connectString == "C-N:C-H") descriptors.Bit382 = 1;
                            else if (connectString == "C-N-C:C") descriptors.Bit386 = 1;
                            else if (connectString == "C-S-C:C") descriptors.Bit391 = 1;
                            else if (connectString == "C-C:N:C") descriptors.Bit397 = 1;
                            else if (connectString == "C-C-S-C") descriptors.Bit398 = 1;
                            else if (connectString == "C-N-N-H") descriptors.Bit412 = 1;
                            else if (connectString == "C-C=C-C") descriptors.Bit414 = 1;
                            else if (connectString == "C-N-C-H") descriptors.Bit429 = 1;
                            break;

                        case "C=":
                            if (connectString == "C=N-N-C") descriptors.Bit358 = 1;
                            else if (connectString == "C=C-C:C") descriptors.Bit410 = 1;
                            else if (connectString == "C=C-C-C") descriptors.Bit445 = 1;
                            else if (connectString == "C=C-C=C") descriptors.Bit453 = 1;
                            break;

                        case "C:":
                            if (connectString == "C:C-C=C") descriptors.Bit361 = 1;
                            else if (connectString == "C:N:C-C") descriptors.Bit363 = 1;
                            else if (connectString == "C:S:C-C") descriptors.Bit371 = 1;
                            else if (connectString == "C:N-C:C") descriptors.Bit373 = 1;
                            else if (connectString == "C:N-C-H") descriptors.Bit411 = 1;
                            else if (connectString == "C:C:N-H") descriptors.Bit416 = 1;
                            else if (connectString == "C:C-C:C") descriptors.Bit441 = 1;
                            else if (connectString == "C:C-O-C") descriptors.Bit454 = 1;
                            else if (connectString == "C:C-C-C") descriptors.Bit459 = 1;
                            break;

                        case "O-":
                            if (connectString == "O-C-C=N") descriptors.Bit352 = 1;
                            else if (connectString == "O-C-C=O") descriptors.Bit353 = 1;
                            else if (connectString == "O-S-C:C") descriptors.Bit372 = 1;
                            else if (connectString == "O-C:C:N") descriptors.Bit388 = 1;
                            else if (connectString == "O-C=C-C") descriptors.Bit389 = 1;
                            else if (connectString == "O-N-C-C") descriptors.Bit404 = 1;
                            else if (connectString == "O-C:C-C") descriptors.Bit430 = 1;
                            else if (connectString == "O-C:C-H") descriptors.Bit431 = 1;
                            else if (connectString == "O-C:C-N") descriptors.Bit432 = 1;
                            else if (connectString == "O-C:C-O") descriptors.Bit433 = 1;
                            else if (connectString == "O-C-C:C") descriptors.Bit437 = 1;
                            else if (connectString == "O-C-N-H") descriptors.Bit448 = 1;
                            else if (connectString == "O-C-O-C") descriptors.Bit452 = 1;
                            else if (connectString == "O-C-C-N") descriptors.Bit455 = 1;
                            else if (connectString == "O-C-C-O") descriptors.Bit456 = 1;
                            else if (connectString == "O-C-C=C") descriptors.Bit462 = 1;

                            break;

                        case "O=":
                            if (connectString == "O=S-C-C") descriptors.Bit356 = 1;
                            else if (connectString == "O=S-C-N") descriptors.Bit359 = 1;
                            else if (connectString == "O=C-C:C") descriptors.Bit384 = 1;
                            else if (connectString == "O=C-C:N") descriptors.Bit385 = 1;
                            else if (connectString == "O=C-N-N") descriptors.Bit408 = 1;
                            else if (connectString == "O=C-C-C") descriptors.Bit424 = 1;
                            else if (connectString == "O=C-C-N") descriptors.Bit425 = 1;
                            else if (connectString == "O=C-C-O") descriptors.Bit426 = 1;
                            else if (connectString == "O=N-C:C") descriptors.Bit447 = 1;
                            break;

                        case "N:":
                            if (connectString == "N:C-S-H") descriptors.Bit354 = 1;
                            else if (connectString == "N:C:C:N") descriptors.Bit376 = 1;
                            else if (connectString == "N:N-C-H") descriptors.Bit387 = 1;
                            else if (connectString == "N:C:N-C") descriptors.Bit395 = 1;
                            else if (connectString == "N:C:C-C") descriptors.Bit413 = 1;
                            else if (connectString == "N:C-C:C") descriptors.Bit419 = 1;
                            else if (connectString == "N:C-O-H") descriptors.Bit446 = 1;
                            else if (connectString == "N:C:N:C") descriptors.Bit461 = 1;
                            break;

                        case "N-":
                            if (connectString == "N-C-C=C") descriptors.Bit355 = 1;
                            else if (connectString == "N-N-C:C") descriptors.Bit368 = 1;
                            else if (connectString == "N-S-C:C") descriptors.Bit374 = 1;
                            else if (connectString == "N-C:N:C") descriptors.Bit375 = 1;
                            else if (connectString == "N-C:N:N") descriptors.Bit377 = 1;
                            else if (connectString == "N-C=N-C") descriptors.Bit378 = 1;
                            else if (connectString == "N-C=N-H") descriptors.Bit379 = 1;
                            else if (connectString == "N-C-S-C") descriptors.Bit380 = 1;
                            else if (connectString == "N-C:O:C") descriptors.Bit383 = 1;
                            else if (connectString == "N-C:C:N") descriptors.Bit390 = 1;
                            else if (connectString == "N-C=C-H") descriptors.Bit393 = 1;
                            else if (connectString == "N-N-C-C") descriptors.Bit405 = 1;
                            else if (connectString == "N-N-C-N") descriptors.Bit407 = 1;
                            else if (connectString == "N-C:C-C") descriptors.Bit434 = 1;
                            else if (connectString == "N-C:C-H") descriptors.Bit435 = 1;
                            else if (connectString == "N-C:C-N") descriptors.Bit436 = 1;
                            else if (connectString == "N-C-C:C") descriptors.Bit438 = 1;
                            else if (connectString == "N-C-N-C") descriptors.Bit449 = 1;
                            else if (connectString == "N-C-C-N") descriptors.Bit458 = 1;


                            break;

                        case "N=":
                            if (connectString == "N=C-N-C") descriptors.Bit409 = 1;
                            else if (connectString == "N=C-C-C") descriptors.Bit427 = 1;
                            else if (connectString == "N=C-C-H") descriptors.Bit428 = 1;
                            else if (connectString == "N=C-C=C") descriptors.Bit444 = 1;
                            break;

                        case "N#":
                            if (connectString == "N#C-C=C") descriptors.Bit357 = 1;
                            else if (connectString == "N#C-C-C") descriptors.Bit457 = 1;
                            break;

                        case "S-":
                            if (connectString == "S-S-C:C") descriptors.Bit360 = 1;
                            else if (connectString == "S-C:N:C") descriptors.Bit364 = 1;
                            else if (connectString == "S-C=N-C") descriptors.Bit366 = 1;
                            else if (connectString == "S-C=N-H") descriptors.Bit369 = 1;
                            else if (connectString == "S-C-S-C") descriptors.Bit370 = 1;
                            else if (connectString == "S-C:C-C") descriptors.Bit420 = 1;
                            else if (connectString == "S-C:C-H") descriptors.Bit421 = 1;
                            else if (connectString == "S-C:C-N") descriptors.Bit422 = 1;
                            else if (connectString == "S-C:C-O") descriptors.Bit423 = 1;
                            break;

                        case "S:":
                            if (connectString == "S:C:C:C") descriptors.Bit362 = 1;
                            else if (connectString == "S:C:C:N") descriptors.Bit365 = 1;
                            else if (connectString == "S:C:C-H") descriptors.Bit403 = 1;
                            break;

                        case "S=":
                            if (connectString == "S=C-N-C") descriptors.Bit399 = 1;
                            else if (connectString == "S=C-N-H") descriptors.Bit402 = 1;
                            break;

                        case "Cl-":
                            if (connectString == "Cl-C:C-C") descriptors.Bit392 = 1;
                            else if (connectString == "Cl-C:C-H") descriptors.Bit394 = 1;
                            else if (connectString == "Cl-C:C-O") descriptors.Bit396 = 1;
                            else if (connectString == "Cl-C:C-Cl") descriptors.Bit415 = 1;
                            else if (connectString == "Cl-C-C-Cl") descriptors.Bit418 = 1;
                            else if (connectString == "Cl-C-C-C") descriptors.Bit439 = 1;
                            else if (connectString == "Cl-C-C-O") descriptors.Bit440 = 1;
                            else if (connectString == "Cl-C-C=O") descriptors.Bit450 = 1;

                            break;

                        case "Br-":
                            if (connectString == "Br-C:C-C") descriptors.Bit400 = 1;
                            if (connectString == "Br-C-C-C") descriptors.Bit443 = 1;
                            if (connectString == "Br-C-C=O") descriptors.Bit451 = 1;
                            break;

                    }
                    break;
                #endregion
                case 5:
                    #region
                    switch (initialString)
                    {
                        case "H-":
                            if (connectString == "H-C-C=C-H") descriptors.Bit488 = 1;
                            else if (connectString == "H-C-C-N-H") descriptors.Bit531 = 1;
                            break;
                        case "C-":

                            if (connectString == "C-C:C:C-C") descriptors.Bit492 = 1;
                            else if (connectString == "C-C:C-C:C") descriptors.Bit467 = 1;
                            else if (connectString == "C-C:C-C-C") descriptors.Bit556 = 1;
                            else if (connectString == "C-C:C-N-C") descriptors.Bit474 = 1;
                            else if (connectString == "C-C:C-O-H") descriptors.Bit479 = 1;
                            else if (connectString == "C-C:C-O-C") descriptors.Bit478 = 1;
                            else if (connectString == "C-C=C-C:C") descriptors.Bit565 = 1;
                            else if (connectString == "C-C=C-C=C") descriptors.Bit552 = 1;
                            else if (connectString == "C-C=C-C-C") descriptors.Bit548 = 1;
                            else if (connectString == "C-C=N-N-C") descriptors.Bit532 = 1;
                            else if (connectString == "C-C-C:C-C") descriptors.Bit522 = 1;
                            else if (connectString == "C-C-C=C-C") descriptors.Bit566 = 1;
                            else if (connectString == "C-C-C-C:C") descriptors.Bit497 = 1;
                            else if (connectString == "C-C-C-C-C") descriptors.Bit471 = 1;
                            else if (connectString == "C-C-C-O-H") descriptors.Bit506 = 1;
                            else if (connectString == "C-C-N-C-C") descriptors.Bit544 = 1;
                            else if (connectString == "C-C-O-C:C") descriptors.Bit520 = 1;
                            else if (connectString == "C-C-O-C-C") descriptors.Bit503 = 1;
                            else if (connectString == "C-C-S-C-C") descriptors.Bit546 = 1;
                            else if (connectString == "C-N-C:C-C") descriptors.Bit545 = 1;
                            else if (connectString == "C-N-C-C:C") descriptors.Bit516 = 1;
                            else if (connectString == "C-N-C-C-C") descriptors.Bit502 = 1;
                            else if (connectString == "C-N-C-N-C") descriptors.Bit524 = 1;
                            else if (connectString == "C-O-C:C-C") descriptors.Bit515 = 1;
                            else if (connectString == "C-O-C-C:C") descriptors.Bit493 = 1;
                            else if (connectString == "C-O-C-C=C") descriptors.Bit483 = 1;
                            else if (connectString == "C-O-C-O-C") descriptors.Bit499 = 1;
                            else if (connectString == "C-S-C-C-C") descriptors.Bit475 = 1;

                            break;
                        case "C=":

                            if (connectString == "C=C-C-C-C") descriptors.Bit528 = 1;
                            else if (connectString == "C=C-C-O-H") descriptors.Bit555 = 1;
                            else if (connectString == "C=C-C-O-C") descriptors.Bit554 = 1;

                            break;
                        case "C#":
                            break;
                        case "C:":

                            if (connectString == "C:C:N:N:C") descriptors.Bit505 = 1;
                            else if (connectString == "C:C-C=C-C") descriptors.Bit473 = 1;
                            else if (connectString == "C:C-C-C:C") descriptors.Bit507 = 1;
                            else if (connectString == "C:C-C-C-C") descriptors.Bit484 = 1;
                            else if (connectString == "C:C-N-C:C") descriptors.Bit466 = 1;
                            else if (connectString == "C:C-O-C-C") descriptors.Bit509 = 1;

                            break;
                        case "N-":

                            if (connectString == "N-C:C:C:N") descriptors.Bit510 = 1;
                            else if (connectString == "N-C:C:C-C") descriptors.Bit489 = 1;
                            else if (connectString == "N-C:C:C-N") descriptors.Bit490 = 1;
                            else if (connectString == "N-C:C-C-C") descriptors.Bit553 = 1;
                            else if (connectString == "N-C:C-O-H") descriptors.Bit476 = 1;
                            else if (connectString == "N-C-C:C-C") descriptors.Bit521 = 1;
                            else if (connectString == "N-C-C-C:C") descriptors.Bit496 = 1;
                            else if (connectString == "N-C-C-C-C") descriptors.Bit481 = 1;
                            else if (connectString == "N-C-C-C-N") descriptors.Bit482 = 1;
                            else if (connectString == "N-C-C-N-C") descriptors.Bit500 = 1;
                            else if (connectString == "N-C-C-O-C") descriptors.Bit504 = 1;
                            else if (connectString == "N-C-N-C:C") descriptors.Bit562 = 1;
                            else if (connectString == "N-C-N-C-C") descriptors.Bit542 = 1;
                            else if (connectString == "N-C-O-C-C") descriptors.Bit501 = 1;
                            else if (connectString == "N-N-C-N-H") descriptors.Bit523 = 1;

                            break;
                        case "N=":
                            if (connectString == "N=C-C:C-H") descriptors.Bit465 = 1;
                            else if (connectString == "N=C-N-C-C") descriptors.Bit485 = 1;

                            break;
                        case "N#":
                            if (connectString == "N#C-C-C-C") descriptors.Bit564 = 1;

                            break;
                        case "N:":
                            break;
                        case "O-":
                            if (connectString == "O-C:C:C-C") descriptors.Bit539 = 1;
                            else if (connectString == "O-C:C:C-N") descriptors.Bit540 = 1;
                            else if (connectString == "O-C:C:C-O") descriptors.Bit541 = 1;
                            else if (connectString == "O-C:C-C-C") descriptors.Bit495 = 1;
                            else if (connectString == "O-C:C-O-H") descriptors.Bit519 = 1;
                            else if (connectString == "O-C:C-O-C") descriptors.Bit518 = 1;
                            else if (connectString == "O-C-C:C-C") descriptors.Bit463 = 1;
                            else if (connectString == "O-C-C:C-O") descriptors.Bit464 = 1;
                            else if (connectString == "O-C-C=C-C") descriptors.Bit508 = 1;
                            else if (connectString == "O-C-C-C:C") descriptors.Bit543 = 1;
                            else if (connectString == "O-C-C-C=C") descriptors.Bit529 = 1;
                            else if (connectString == "O-C-C-C=O") descriptors.Bit530 = 1;
                            else if (connectString == "O-C-C-C-C") descriptors.Bit525 = 1;
                            else if (connectString == "O-C-C-C-N") descriptors.Bit526 = 1;
                            else if (connectString == "O-C-C-C-O") descriptors.Bit527 = 1;
                            else if (connectString == "O-C-C-N-C") descriptors.Bit547 = 1;
                            else if (connectString == "O-C-C-O-H") descriptors.Bit551 = 1;
                            else if (connectString == "O-C-C-O-C") descriptors.Bit550 = 1;
                            else if (connectString == "O-C-O-C-C") descriptors.Bit549 = 1;


                            break;
                        case "O=":

                            if (connectString == "O=C-C:C-C") descriptors.Bit512 = 1;
                            else if (connectString == "O=C-C:C-N") descriptors.Bit513 = 1;
                            else if (connectString == "O=C-C:C-O") descriptors.Bit514 = 1;
                            else if (connectString == "O=C-C=C-H") descriptors.Bit560 = 1;
                            else if (connectString == "O=C-C=C-C") descriptors.Bit559 = 1;
                            else if (connectString == "O=C-C=C-N") descriptors.Bit561 = 1;
                            else if (connectString == "O=C-C-C:C") descriptors.Bit486 = 1;
                            else if (connectString == "O=C-C-C=O") descriptors.Bit477 = 1;
                            else if (connectString == "O=C-C-C-C") descriptors.Bit468 = 1;
                            else if (connectString == "O=C-C-C-N") descriptors.Bit469 = 1;
                            else if (connectString == "O=C-C-C-O") descriptors.Bit470 = 1;
                            else if (connectString == "O=C-C-N-C") descriptors.Bit491 = 1;
                            else if (connectString == "O=C-C-O-C") descriptors.Bit494 = 1;
                            else if (connectString == "O=C-N-C-H") descriptors.Bit534 = 1;
                            else if (connectString == "O=C-N-C=O") descriptors.Bit538 = 1;
                            else if (connectString == "O=C-N-C-C") descriptors.Bit533 = 1;
                            else if (connectString == "O=C-N-C-N") descriptors.Bit535 = 1;
                            else if (connectString == "O=C-O-C:C") descriptors.Bit511 = 1;
                            else if (connectString == "O=N-C:C-N") descriptors.Bit536 = 1;
                            else if (connectString == "O=N-C:C-O") descriptors.Bit537 = 1;

                            break;
                        case "S-":
                            if (connectString == "S-C:C:C-N") descriptors.Bit517 = 1;
                            break;
                        case "Cl-":

                            if (connectString == "Cl-C:C:C-C") descriptors.Bit487 = 1;
                            else if (connectString == "Cl-C:C-C=O") descriptors.Bit557 = 1;
                            else if (connectString == "Cl-C:C-O-C") descriptors.Bit472 = 1;
                            else if (connectString == "Cl-C-C-C-C") descriptors.Bit480 = 1;
                            else if (connectString == "Cl-C-C-N-C") descriptors.Bit498 = 1;


                            break;
                        case "Br-":
                            if (connectString == "Br-C:C:C-C") descriptors.Bit558 = 1;
                            else if (connectString == "Br-C-C-C:C") descriptors.Bit563 = 1;
                            break;
                    }
                    break;
                #endregion
                case 6:
                    #region
                    switch (initialString)
                    {
                        case "C-":
                            if (connectString == "C-C-C-C-C-C") descriptors.Bit567 = 1;
                            break;
                        case "O-":
                            if (connectString == "O-C-C-C-C-C") descriptors.Bit568 = 1;
                            else if (connectString == "O-C-C-C-C-O") descriptors.Bit569 = 1;
                            else if (connectString == "O-C-C-C-C-N") descriptors.Bit570 = 1;
                            break;
                        case "O=":
                            if (connectString == "O=C-C-C-C-C") descriptors.Bit572 = 1;
                            else if (connectString == "O=C-C-C-C-N") descriptors.Bit573 = 1;
                            else if (connectString == "O=C-C-C-C-O") descriptors.Bit574 = 1;
                            else if (connectString == "O=C-C-C-C=O") descriptors.Bit575 = 1;
                            break;
                        case "N-":
                            if (connectString == "N-C-C-C-C-C") descriptors.Bit571 = 1;
                            break;
                    }
                    break;
                #endregion
                case 7:
                    #region
                    switch (initialString)
                    {
                        case "C-":
                            if (connectString == "C-C-C-C-C-C-C") descriptors.Bit576 = 1;
                            break;
                        case "O-":
                            if (connectString == "O-C-C-C-C-C-C") descriptors.Bit577 = 1;
                            else if (connectString == "O-C-C-C-C-C-O") descriptors.Bit578 = 1;
                            else if (connectString == "O-C-C-C-C-C-N") descriptors.Bit579 = 1;
                            break;
                        case "O=":
                            if (connectString == "O=C-C-C-C-C-C") descriptors.Bit580 = 1;
                            else if (connectString == "O=C-C-C-C-C-O") descriptors.Bit581 = 1;
                            else if (connectString == "O=C-C-C-C-C=O") descriptors.Bit582 = 1;
                            else if (connectString == "O=C-C-C-C-C-N") descriptors.Bit583 = 1;
                            break;
                    }
                    break;
                    #endregion
            }
        }

        public static void SetSection6Properties(MolecularFingerprint descriptors, string connectString,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4,
            AtomProperty atom5, AtomProperty atom6, AtomProperty atom7,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4,
            BondProperty bond5, BondProperty bond6)
        {
            if (atom7.AtomEnv.SingleBond_CarbonCount >= 2)
            {

                if (connectString == "C-C-C-C-C-C-C") descriptors.Bit584 = 1; //C-C-C-C-C-C-C-C
                else if (connectString == "O-C-C-C-C-C-C") descriptors.Bit586 = 1; //O-C-C-C-C-C-C-C
                else if (connectString == "O-C-C-C-C-C-O") descriptors.Bit588 = 1; //O-C-C-C-C-C-O-C
                else if (connectString == "O-C-C-C-C-C-N") descriptors.Bit590 = 1; //O-C-C-C-C-C-N-C
                else if (connectString == "O=C-C-C-C-C-C") descriptors.Bit592 = 1; //O=C-C-C-C-C-C-C
            }

            if (connectString == "C-C-C-C-C-C-C" && atom6.AtomEnv.SingleBond_CarbonCount >= 3) descriptors.Bit585 = 1; //C-C-C-C-C-C(C)-C
            else if (connectString == "O-C-C-C-C-C-C" && atom6.AtomEnv.SingleBond_CarbonCount >= 3) descriptors.Bit587 = 1; //O-C-C-C-C-C(C)-C
            else if (connectString == "O-C-C-C-C-C-C" && atom6.AtomEnv.SingleBond_OxygenCount >= 1) descriptors.Bit589 = 1; //O-C-C-C-C-C(O)-C
            else if (connectString == "O-C-C-C-C-C-C" && atom6.AtomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit591 = 1; //O-C-C-C-C-C(N)-C
            else if (connectString == "O=C-C-C-C-C-C" && atom6.AtomEnv.SingleBond_OxygenCount >= 1) descriptors.Bit593 = 1; //O=C-C-C-C-C(O)-C
            else if (connectString == "O=C-C-C-C-C-C" && atom6.AtomEnv.DoubleBond_CO_Count >= 1) descriptors.Bit594 = 1; //O=C-C-C-C-C(=O)-C
            else if (connectString == "O=C-C-C-C-C-C" && atom6.AtomEnv.SingleBond_NitrogenCount >= 1) descriptors.Bit595 = 1; //O=C-C-C-C-C(N)-C
        }

        public static void SetSection6Properties(MolecularFingerprint descriptors, string connectString,
            AtomProperty firstAtom, AtomProperty secondAtom, AtomProperty thirdAtom, AtomProperty fourthAtom,
            BondProperty firstBond, BondProperty secondBond, BondProperty thirdBond)
        {
            if (connectString == "C-C-C-C")
            {
                if (secondAtom.AtomEnv.SingleBond_CarbonCount >= 3) descriptors.Bit596 = 1;
                if (secondAtom.AtomEnv.SingleBond_CarbonCount >= 4) descriptors.Bit599 = 1;
                if (secondAtom.AtomEnv.SingleBond_CarbonCount >= 3 && thirdAtom.AtomEnv.SingleBond_CarbonCount >= 3) descriptors.Bit596 = 1;
            }
            //else if (connectString == "N-C-C-O") {
            //    if (secondAtom.AtomEnv.CarbonCount != 2) return;
            //    var secondAtomConnectedBonds = secondAtom.ConnectedBonds.Where(n => n.BondID != firstBond.BondID && n.BondID != secondBond.BondID).ToList();
            //    if (secondAtomConnectedBonds.Count != 2) return;

            //    var secondAtomConnectedAtom1 = secondAtomConnectedBonds[0].ConnectedAtoms.Where(n => n.AtomID != secondAtom.AtomID).ToList()[0];
            //    var secondAtomConnectedAtom2 = secondAtomConnectedBonds[1].ConnectedAtoms.Where(n => n.AtomID != secondAtom.AtomID).ToList()[0];

            //    var carbonAtom = secondAtomConnectedAtom1.AtomString == "C" ? secondAtomConnectedAtom1 : secondAtomConnectedAtom2;
            //    if (carbonAtom.AtomEnv.HydroxyCount > 0) {
            //        descriptors.Bit604 = 1;
            //        if (fourthAtom.AtomEnv.PhosphorusCount > 0)
            //            descriptors.Bit605 = 1;
            //    }
            //}
        }

        public static void SetSection6Properties(MolecularFingerprint descriptors, string connectString,
            AtomProperty firstAtom, AtomProperty secondAtom, AtomProperty thirdAtom, AtomProperty fourthAtom, AtomProperty fifthAtom,
            BondProperty firstBond, BondProperty secondBond, BondProperty thirdBond, BondProperty fourthBond)
        {
            if (connectString == "C-C-C-C-C")
            {
                if (secondAtom.AtomEnv.SingleBond_CarbonCount >= 3) descriptors.Bit597 = 1;
                if (thirdAtom.AtomEnv.SingleBond_CarbonCount >= 3) descriptors.Bit598 = 1;
            }
            //else if (connectString == "C-N-C-C-O") {
            //    if (firstAtom.AtomEnv.DoubleBond_CO_Count == 0) return;
            //    if (thirdAtom.AtomEnv.CarbonCount != 2) return;
            //    var thirdAtomConnectedBonds = thirdAtom.ConnectedBonds.Where(n => n.BondID != secondBond.BondID && n.BondID != thirdBond.BondID).ToList();
            //    if (thirdAtomConnectedBonds.Count != 2) return;

            //    var thirdAtomConnectedAtom1 = thirdAtomConnectedBonds[0].ConnectedAtoms.Where(n => n.AtomID != thirdAtom.AtomID).ToList()[0];
            //    var thirdAtomConnectedAtom2 = thirdAtomConnectedBonds[1].ConnectedAtoms.Where(n => n.AtomID != thirdAtom.AtomID).ToList()[0];

            //    var carbonAtom = thirdAtomConnectedAtom1.AtomString == "C" ? thirdAtomConnectedAtom1 : thirdAtomConnectedAtom2;
            //    if (carbonAtom.AtomEnv.HydroxyCount > 0) {
            //        descriptors.Bit606 = 1;
            //        if (fifthAtom.AtomEnv.PhosphorusCount > 0)
            //            descriptors.Bit607 = 1;
            //    }
            //}
            //else if (connectString == "O-C-C-C-O") {
            //    if (thirdAtom.AtomEnv.OxygenCount > 0)
            //        descriptors.Bit608 = 1;
            //}
            //else if (connectString == "C=C-C=C-C") {
            //    if (firstAtom.AtomFunctionType == AtomFunctionType.C_Alkene && thirdAtom.AtomFunctionType == AtomFunctionType.C_Alkene && fifthAtom.AtomFunctionType == AtomFunctionType.C_Alkene)
            //        descriptors.Bit609 = 1;
            //}
            //else if (connectString == "C-C=C-C=C") {
            //    if (firstBond.IsRingConnected && secondAtom.AtomFunctionType == AtomFunctionType.C_Alkene && fourthAtom.AtomFunctionType == AtomFunctionType.C_Alkene)
            //        descriptors.Bit610 = 1;
            //}
        }

        //public static void SetMolecularDescriptorOfSection6OfSpecificConnections(MolecularDescriptor descriptors, string connectString,
        //    AtomProperty firstAtom, AtomProperty secondAtom, AtomProperty thirdAtom, AtomProperty fourthAtom, AtomProperty fifthAtom, AtomProperty sixthAtom,
        //    BondProperty firstBond, BondProperty secondBond, BondProperty thirdBond, BondProperty fourthBond, BondProperty fifthBond)
        //{
        //    if (connectString == "C-S-C=N-O-S") {
        //        if (firstBond.IsSugarRingConnected && sixthAtom.AtomFunctionType == AtomFunctionType.S_Sulfonate)
        //            descriptors.Bit601 = 1;
        //    }
        //    else if (connectString == "C-O-C-C-C-O") {

        //        if (fourthAtom.AtomEnv.OxygenCount != 1 || fourthAtom.AtomEnv.HydrogenCount != 1) return;
        //        var fourthAtomConnectedBonds = fourthAtom.ConnectedBonds.Where(n => n.BondID != thirdBond.BondID && n.BondID != fourthBond.BondID).ToList();
        //        if (fourthAtomConnectedBonds.Count != 2) return;

        //        var fourthAtomConnectedAtom1 = fourthAtomConnectedBonds[0].ConnectedAtoms.Where(n => n.AtomID != fourthAtom.AtomID).ToList()[0];
        //        var fourthAtomConnectedAtom2 = fourthAtomConnectedBonds[1].ConnectedAtoms.Where(n => n.AtomID != fourthAtom.AtomID).ToList()[0];

        //        var oxygenAtom = fourthAtomConnectedAtom1.AtomString == "O" ? fourthAtomConnectedAtom1 : fourthAtomConnectedAtom2;
        //        if (oxygenAtom.ConnectedBonds.Count != 2) return;

        //        var oxygenConnectedBond = oxygenAtom.ConnectedBonds.Where(n => n.BondID != fourthAtomConnectedBonds[0].BondID && n.BondID != fourthAtomConnectedBonds[1].BondID).ToList()[0];
        //        var oxygenConnectedAtom = oxygenConnectedBond.ConnectedAtoms.Where(n => n.AtomID != oxygenAtom.AtomID).ToList()[0];

        //        if (firstAtom.AtomFunctionType == AtomFunctionType.C_Ester
        //            || oxygenConnectedAtom.AtomFunctionType == AtomFunctionType.C_Ester) {
        //            descriptors.Bit602 = 1;
        //            if (sixthAtom.AtomEnv.PhosphorusCount > 0) descriptors.Bit603 = 1;
        //        }
        //    }
        //}
        #endregion

        //section 7
        #region
        public static void SetSection7Properties(MolecularFingerprint descriptors, string connectString,
           AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4,
           BondProperty firstBond, BondProperty secondBond, BondProperty thirdBond, Dictionary<int, RingProperty> ringDictionary)
        {
            if (!atom1.IsInRing || !atom2.IsInRing || !atom3.IsInRing || !atom4.IsInRing) return;
            var ringIdCounts = new Dictionary<int, int>();
            var sharedRingID = -1;

            #region prerequest
            foreach (var ringID in atom1.SharedRingIDs)
            {
                if (ringIdCounts.ContainsKey(ringID)) ringIdCounts[ringID]++;
                else ringIdCounts[ringID] = 1;
            }

            foreach (var ringID in atom2.SharedRingIDs)
            {
                if (ringIdCounts.ContainsKey(ringID)) ringIdCounts[ringID]++;
                else ringIdCounts[ringID] = 1;
            }

            foreach (var ringID in atom3.SharedRingIDs)
            {
                if (ringIdCounts.ContainsKey(ringID)) ringIdCounts[ringID]++;
                else ringIdCounts[ringID] = 1;
            }

            foreach (var ringID in atom4.SharedRingIDs)
            {
                if (ringIdCounts.ContainsKey(ringID)) ringIdCounts[ringID]++;
                else ringIdCounts[ringID] = 1;
            }

            var isSameRing = false;
            foreach (var element in ringIdCounts)
            {
                if (element.Value == 4)
                {
                    isSameRing = true;
                    sharedRingID = element.Key;
                    break;
                }
            }
            if (!isSameRing) return;
            #endregion

            var ring = ringDictionary[sharedRingID];
            var outsideAtoms = ring.RingEnv.OutsideAtomDictionary;
            if (outsideAtoms.ContainsKey(atom1.AtomID) == false) return;

            var secondAtomOutside = outsideAtoms[atom2.AtomID];
            var thirdAtomOutside = outsideAtoms[atom3.AtomID];
            var fourthAtomOutside = outsideAtoms[atom4.AtomID];

            if (connectString == "C:C:C:C" && ring.IsBenzeneRing)
            {
                #region
                if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "C") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit611 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit612 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit613 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit614 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit615 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit616 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit632 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit633 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit634 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit635 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit636 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit637 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit653 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit654 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit655 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit656 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit657 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit658 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "O") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit617 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit618 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit619 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit620 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit621 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit638 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit639 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit640 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit641 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit642 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit659 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit660 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit661 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit662 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit663 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "S") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit622 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit623 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit624 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit625 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit643 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit644 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit645 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit646 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit664 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit665 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit666 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit667 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "N") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit626 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit627 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit628 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit647 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit648 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit649 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit668 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit669 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit670 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "Cl") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit629 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit630 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit650 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit651 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit671 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit672 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "Br") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit631 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit652 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit673 = 1;
                    }
                }
                #endregion
            }
            else if (connectString == "C-C-C-C" && ring.RingFunctionType == RingFunctionType.Cyclohexane)
            {
                #region
                if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "C") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit674 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit675 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit676 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit677 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit678 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit679 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit695 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit696 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit697 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit698 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit699 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit700 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit716 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit717 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit718 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit719 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit720 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit721 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "O") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit680 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit681 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit682 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit683 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit684 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit701 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit702 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit703 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit704 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit705 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit722 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit723 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit724 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit725 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit726 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "S") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit685 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit686 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit687 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit688 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit706 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit707 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit708 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit709 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit727 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit728 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit729 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit730 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "N") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit689 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit690 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit691 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit710 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit711 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit712 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit731 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit732 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit733 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "Cl") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit692 = 1;
                        else if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit693 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit713 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit714 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit734 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit735 = 1;
                    }
                }
                else if (outsideAtoms[atom1.AtomID].Count(n => n.AtomString == "Br") > 0)
                {
                    if (fourthAtomOutside.Count > 0)
                    {
                        if (fourthAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit694 = 1;
                    }

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit715 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit736 = 1;
                    }
                }
                #endregion
            }
        }

        public static void SetSection7Properties(MolecularFingerprint descriptors, string connectString,
          AtomProperty firstAtom, AtomProperty secondAtom, AtomProperty thirdAtom,
          BondProperty firstBond, BondProperty secondBond, Dictionary<int, RingProperty> ringDictionary)
        {
            if (!firstAtom.IsInRing || !secondAtom.IsInRing || !thirdAtom.IsInRing) return;
            var ringIdCounts = new Dictionary<int, int>();
            var sharedRingID = -1;

            #region prerequest
            foreach (var ringID in firstAtom.SharedRingIDs)
            {
                if (ringIdCounts.ContainsKey(ringID)) ringIdCounts[ringID]++;
                else ringIdCounts[ringID] = 1;
            }

            foreach (var ringID in secondAtom.SharedRingIDs)
            {
                if (ringIdCounts.ContainsKey(ringID)) ringIdCounts[ringID]++;
                else ringIdCounts[ringID] = 1;
            }

            foreach (var ringID in thirdAtom.SharedRingIDs)
            {
                if (ringIdCounts.ContainsKey(ringID)) ringIdCounts[ringID]++;
                else ringIdCounts[ringID] = 1;
            }

            var isSameRing = false;
            foreach (var element in ringIdCounts)
            {
                if (element.Value == 3)
                {
                    isSameRing = true;
                    sharedRingID = element.Key;
                    break;
                }
            }
            if (!isSameRing) return;
            #endregion

            var ring = ringDictionary[sharedRingID];
            var outsideAtoms = ring.RingEnv.OutsideAtomDictionary;
            if (outsideAtoms.ContainsKey(firstAtom.AtomID) == false) return;

            var secondAtomOutside = outsideAtoms[secondAtom.AtomID];
            var thirdAtomOutside = outsideAtoms[thirdAtom.AtomID];

            if (connectString == "C-C-C" && ring.RingFunctionType == RingFunctionType.Cyclopentane)
            {
                #region
                if (outsideAtoms[firstAtom.AtomID].Count(n => n.AtomString == "C") > 0)
                {

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit737 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit738 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit739 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit740 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit741 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit742 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "C") > 0) descriptors.Bit758 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit759 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit760 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit761 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit762 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit763 = 1;
                    }
                }
                else if (outsideAtoms[firstAtom.AtomID].Count(n => n.AtomString == "O") > 0)
                {

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit743 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit744 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit745 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit746 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit747 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "O") > 0) descriptors.Bit764 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit765 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit766 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit767 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit768 = 1;
                    }
                }
                else if (outsideAtoms[firstAtom.AtomID].Count(n => n.AtomString == "S") > 0)
                {

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit748 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit749 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit750 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit751 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "S") > 0) descriptors.Bit769 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit770 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit771 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit772 = 1;
                    }
                }
                else if (outsideAtoms[firstAtom.AtomID].Count(n => n.AtomString == "N") > 0)
                {

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit752 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit753 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit754 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "N") > 0) descriptors.Bit773 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit774 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit775 = 1;
                    }
                }
                else if (outsideAtoms[firstAtom.AtomID].Count(n => n.AtomString == "Cl") > 0)
                {

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit755 = 1;
                        else if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit756 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "Cl") > 0) descriptors.Bit776 = 1;
                        else if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit777 = 1;
                    }
                }
                else if (outsideAtoms[firstAtom.AtomID].Count(n => n.AtomString == "Br") > 0)
                {

                    if (thirdAtomOutside.Count > 0)
                    {
                        if (thirdAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit757 = 1;
                    }

                    if (secondAtomOutside.Count > 0)
                    {
                        if (secondAtomOutside.Count(n => n.AtomString == "Br") > 0) descriptors.Bit778 = 1;
                    }
                }
                #endregion
            }
        }

        #endregion

    }
}
