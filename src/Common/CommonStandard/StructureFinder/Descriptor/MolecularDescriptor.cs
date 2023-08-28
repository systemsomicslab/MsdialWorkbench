namespace CompMs.Common.StructureFinder.Descriptor
{
    public class MolecularFingerprint
    {
        /// <summary>
        /// Section 1: Hierarchic Element Counts - These bits test for the 
        /// presence or count of individual chemical atoms 
        /// represented by their atomic symbol.
        /// </summary>
        public int Bit0 { get; set; } //>= 4 H
        public int Bit1 { get; set; } //>= 8 H
        public int Bit2 { get; set; } //>= 16 H
        public int Bit3 { get; set; } //>= 32 H
        public int Bit4 { get; set; } //>= 2 C
        public int Bit5 { get; set; } //>= 4 C
        public int Bit6 { get; set; } //>= 8 C
        public int Bit7 { get; set; } //>= 16 C
        public int Bit8 { get; set; } //>= 32 C
        public int Bit9 { get; set; } //>= 1 N
        public int Bit10 { get; set; } //>= 2 N
        public int Bit11 { get; set; } //>= 4 N
        public int Bit12 { get; set; } //>= 8 N
        public int Bit13 { get; set; } //>= 1 O
        public int Bit14 { get; set; } //>= 2 O
        public int Bit15 { get; set; } //>= 4 O
        public int Bit16 { get; set; } //>= 8 O
        public int Bit17 { get; set; } //>= 16 O
        public int Bit18 { get; set; } //>= 1 F
        public int Bit19 { get; set; } //>= 2 F
        public int Bit20 { get; set; } //>= 4 F
        public int Bit21 { get; set; } //>= 1 Si
        public int Bit22 { get; set; } //>= 2 Si
        public int Bit23 { get; set; } //>= 1 P
        public int Bit24 { get; set; } //>= 2 P
        public int Bit25 { get; set; } //>= 4 P
        public int Bit26 { get; set; } //>= 1 S
        public int Bit27 { get; set; } //>= 2 S
        public int Bit28 { get; set; } //>= 4 S
        public int Bit29 { get; set; } //>= 8 S
        public int Bit30 { get; set; } //>= 1 Cl
        public int Bit31 { get; set; } //>= 2 Cl
        public int Bit32 { get; set; } //>= 4 Cl
        public int Bit33 { get; set; } //>= 8 Cl
        public int Bit34 { get; set; } //>= 1 Br
        public int Bit35 { get; set; } //>= 2 Br
        public int Bit36 { get; set; } //>= 4 Br
        public int Bit37 { get; set; } //>= 1 I
        public int Bit38 { get; set; } //>= 2 I
        public int Bit39 { get; set; } //>= 4 I

        /// <summary>
        /// Section 2: This is a modified version of original PubChem section2.
        /// Instead of ESSSR (Extended Smallest Set of Smallest Rings), 
        /// SSSR (Smallest Set of Smallest Ring) is used for the detection of bit substructure
        /// </summary>
        public int Bit40 { get; set; } //>= 1 any ring size 3
        public int Bit41 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 3
        public int Bit42 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 3
        public int Bit43 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 3
        public int Bit44 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 3
        public int Bit45 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 3
        public int Bit46 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 3
        public int Bit47 { get; set; } //>= 2 any ring size 3
        public int Bit48 { get; set; } //>= 2 saturated or aromatic carbon-only ring size 3
        public int Bit49 { get; set; } //>= 2 saturated or aromatic nitrogen-containing ring size 3
        public int Bit50 { get; set; } //>= 2 saturated or aromatic heteroatom-containing ring size 3
        public int Bit51 { get; set; } //>= 2 unsaturated non-aromatic carbon-only ring size 3
        public int Bit52 { get; set; } //>= 2 unsaturated non-aromatic nitrogen-containing ring size 3
        public int Bit53 { get; set; } //>= 2 unsaturated non-aromatic heteroatom-containing ring size 3
        public int Bit54 { get; set; } //>= 1 any ring size 4
        public int Bit55 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 4
        public int Bit56 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 4
        public int Bit57 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 4
        public int Bit58 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 4
        public int Bit59 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 4
        public int Bit60 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 4
        public int Bit61 { get; set; } //>= 2 any ring size 4
        public int Bit62 { get; set; } //>= 2 saturated or aromatic carbon-only ring size 4
        public int Bit63 { get; set; } //>= 2 saturated or aromatic nitrogen-containing ring size 4
        public int Bit64 { get; set; } //>= 2 saturated or aromatic heteroatom-containing ring size 4
        public int Bit65 { get; set; } //>= 2 unsaturated non-aromatic carbon-only ring size 4
        public int Bit66 { get; set; } //>= 2 unsaturated non-aromatic nitrogen-containing ring size 4
        public int Bit67 { get; set; } //>= 2 unsaturated non-aromatic heteroatom-containing ring size 4
        public int Bit68 { get; set; } //>= 1 any ring size 5
        public int Bit69 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 5
        public int Bit70 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 5
        public int Bit71 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 5
        public int Bit72 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 5
        public int Bit73 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 5
        public int Bit74 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 5
        public int Bit75 { get; set; } //>= 2 any ring size 5
        public int Bit76 { get; set; } //>= 2 saturated or aromatic carbon-only ring size 5
        public int Bit77 { get; set; } //>= 2 saturated or aromatic nitrogen-containing ring size 5
        public int Bit78 { get; set; } //>= 2 saturated or aromatic heteroatom-containing ring size 5
        public int Bit79 { get; set; } //>= 2 unsaturated non-aromatic carbon-only ring size 5
        public int Bit80 { get; set; } //>= 2 unsaturated non-aromatic nitrogen-containing ring size 5
        public int Bit81 { get; set; } //>= 2 unsaturated non-aromatic heteroatom-containing ring size 5
        public int Bit82 { get; set; } //>= 3 any ring size 5
        public int Bit83 { get; set; } //>= 3 saturated or aromatic carbon-only ring size 5
        public int Bit84 { get; set; } //>= 3 saturated or aromatic nitrogen-containing ring size 5
        public int Bit85 { get; set; } //>= 3 saturated or aromatic heteroatom-containing ring size 5
        public int Bit86 { get; set; } //>= 3 unsaturated non-aromatic carbon-only ring size 5
        public int Bit87 { get; set; } //>= 3 unsaturated non-aromatic nitrogen-containing ring size 5
        public int Bit88 { get; set; } //>= 3 unsaturated non-aromatic heteroatom-containing ring size 5
        public int Bit89 { get; set; } //>= 4 any ring size 5
        public int Bit90 { get; set; } //>= 4 saturated or aromatic carbon-only ring size 5
        public int Bit91 { get; set; } //>= 4 saturated or aromatic nitrogen-containing ring size 5
        public int Bit92 { get; set; } //>= 4 saturated or aromatic heteroatom-containing ring size 5
        public int Bit93 { get; set; } //>= 4 unsaturated non-aromatic carbon-only ring size 5
        public int Bit94 { get; set; } //>= 4 unsaturated non-aromatic nitrogen-containing ring size 5
        public int Bit95 { get; set; } //>= 4 unsaturated non-aromatic heteroatom-containing ring size 5
        public int Bit96 { get; set; } //>= 5 any ring size 5
        public int Bit97 { get; set; } //>= 5 saturated or aromatic carbon-only ring size 5
        public int Bit98 { get; set; } //>= 5 saturated or aromatic nitrogen-containing ring size 5
        public int Bit99 { get; set; } //>= 5 saturated or aromatic heteroatom-containing ring size 5
        public int Bit100 { get; set; } //>= 5 unsaturated non-aromatic carbon-only ring size 5
        public int Bit101 { get; set; } //>= 5 unsaturated non-aromatic nitrogen-containing ring size 5
        public int Bit102 { get; set; } //>= 5 unsaturated non-aromatic heteroatom-containing ring size 5
        public int Bit103 { get; set; } //>= 1 any ring size 6
        public int Bit104 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 6
        public int Bit105 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 6
        public int Bit106 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 6
        public int Bit107 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 6
        public int Bit108 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 6
        public int Bit109 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 6
        public int Bit110 { get; set; } //>= 2 any ring size 6
        public int Bit111 { get; set; } //>= 2 saturated or aromatic carbon-only ring size 6
        public int Bit112 { get; set; } //>= 2 saturated or aromatic nitrogen-containing ring size 6
        public int Bit113 { get; set; } //>= 2 saturated or aromatic heteroatom-containing ring size 6
        public int Bit114 { get; set; } //>= 2 unsaturated non-aromatic carbon-only ring size 6
        public int Bit115 { get; set; } //>= 2 unsaturated non-aromatic nitrogen-containing ring size 6
        public int Bit116 { get; set; } //>= 2 unsaturated non-aromatic heteroatom-containing ring size 6
        public int Bit117 { get; set; } //>= 3 any ring size 6
        public int Bit118 { get; set; } //>= 3 saturated or aromatic carbon-only ring size 6
        public int Bit119 { get; set; } //>= 3 saturated or aromatic nitrogen-containing ring size 6
        public int Bit120 { get; set; } //>= 3 saturated or aromatic heteroatom-containing ring size 6
        public int Bit121 { get; set; } //>= 3 unsaturated non-aromatic carbon-only ring size 6
        public int Bit122 { get; set; } //>= 3 unsaturated non-aromatic nitrogen-containing ring size 6
        public int Bit123 { get; set; } //>= 3 unsaturated non-aromatic heteroatom-containing ring size 6
        public int Bit124 { get; set; } //>= 4 any ring size 6
        public int Bit125 { get; set; } //>= 4 saturated or aromatic carbon-only ring size 6
        public int Bit126 { get; set; } //>= 4 saturated or aromatic nitrogen-containing ring size 6
        public int Bit127 { get; set; } //>= 4 saturated or aromatic heteroatom-containing ring size 6
        public int Bit128 { get; set; } //>= 4 unsaturated non-aromatic carbon-only ring size 6
        public int Bit129 { get; set; } //>= 4 unsaturated non-aromatic nitrogen-containing ring size 6
        public int Bit130 { get; set; } //>= 4 unsaturated non-aromatic heteroatom-containing ring size 6
        public int Bit131 { get; set; } //>= 5 any ring size 6
        public int Bit132 { get; set; } //>= 5 saturated or aromatic carbon-only ring size 6
        public int Bit133 { get; set; } //>= 5 saturated or aromatic nitrogen-containing ring size 6
        public int Bit134 { get; set; } //>= 5 saturated or aromatic heteroatom-containing ring size 6
        public int Bit135 { get; set; } //>= 5 unsaturated non-aromatic carbon-only ring size 6
        public int Bit136 { get; set; } //>= 5 unsaturated non-aromatic nitrogen-containing ring size 6
        public int Bit137 { get; set; } //>= 5 unsaturated non-aromatic heteroatom-containing ring size 6
        public int Bit138 { get; set; } //>= 1 any ring size 7
        public int Bit139 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 7
        public int Bit140 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 7
        public int Bit141 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 7
        public int Bit142 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 7
        public int Bit143 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 7
        public int Bit144 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 7
        public int Bit145 { get; set; } //>= 2 any ring size 7
        public int Bit146 { get; set; } //>= 2 saturated or aromatic carbon-only ring size 7
        public int Bit147 { get; set; } //>= 2 saturated or aromatic nitrogen-containing ring size 7
        public int Bit148 { get; set; } //>= 2 saturated or aromatic heteroatom-containing ring size 7
        public int Bit149 { get; set; } //>= 2 unsaturated non-aromatic carbon-only ring size 7
        public int Bit150 { get; set; } //>= 2 unsaturated non-aromatic nitrogen-containing ring size 7
        public int Bit151 { get; set; } //>= 2 unsaturated non-aromatic heteroatom-containing ring size 7
        public int Bit152 { get; set; } //>= 1 any ring size 8
        public int Bit153 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 8
        public int Bit154 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 8
        public int Bit155 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 8
        public int Bit156 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 8
        public int Bit157 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 8
        public int Bit158 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 8
        public int Bit159 { get; set; } //>= 2 any ring size 8
        public int Bit160 { get; set; } //>= 2 saturated or aromatic carbon-only ring size 8
        public int Bit161 { get; set; } //>= 2 saturated or aromatic nitrogen-containing ring size 8
        public int Bit162 { get; set; } //>= 2 saturated or aromatic heteroatom-containing ring size 8
        public int Bit163 { get; set; } //>= 2 unsaturated non-aromatic carbon-only ring size 8
        public int Bit164 { get; set; } //>= 2 unsaturated non-aromatic nitrogen-containing ring size 8
        public int Bit165 { get; set; } //>= 2 unsaturated non-aromatic heteroatom-containing ring size 8
        public int Bit166 { get; set; } //>= 1 any ring size 9
        public int Bit167 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 9
        public int Bit168 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 9
        public int Bit169 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 9
        public int Bit170 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 9
        public int Bit171 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 9
        public int Bit172 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 9
        public int Bit173 { get; set; } //>= 1 any ring size 10
        public int Bit174 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 10
        public int Bit175 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 10
        public int Bit176 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 10
        public int Bit177 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 10
        public int Bit178 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 10
        public int Bit179 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 10
        public int Bit180 { get; set; } //>= 1 aromatic ring
        public int Bit181 { get; set; } //>= 1 hetero-aromatic ring
        public int Bit182 { get; set; } //>= 2 aromatic rings
        public int Bit183 { get; set; } //>= 2 hetero-aromatic rings
        public int Bit184 { get; set; } //>= 3 aromatic rings
        public int Bit185 { get; set; } //>= 3 hetero-aromatic rings
        public int Bit186 { get; set; } //>= 4 aromatic rings
        public int Bit187 { get; set; } //>= 4 hetero-aromatic rings

        //section 3: Simple atom pairs - These bits test for the presence of 
        //patterns of bonded atom pairs, regardless of bond order 
        //or count.
        public int Bit188 { get; set; } //C-H
        public int Bit189 { get; set; } //C-C
        public int Bit190 { get; set; } //C-N
        public int Bit191 { get; set; } //C-O
        public int Bit192 { get; set; } //C-F
        public int Bit193 { get; set; } //C-Si
        public int Bit194 { get; set; } //C-P
        public int Bit195 { get; set; } //C-S
        public int Bit196 { get; set; } //C-Cl
        public int Bit197 { get; set; } //C-I
        public int Bit198 { get; set; } //N-H
        public int Bit199 { get; set; } //N-N
        public int Bit200 { get; set; } //N-O
        public int Bit201 { get; set; } //N-F
        public int Bit202 { get; set; } //N-Si
        public int Bit203 { get; set; } //N-P
        public int Bit204 { get; set; } //N-S
        public int Bit205 { get; set; } //N-Cl
        public int Bit206 { get; set; } //N-Br
        public int Bit207 { get; set; } //O-H
        public int Bit208 { get; set; } //O-O
        public int Bit209 { get; set; } //O-Si
        public int Bit210 { get; set; } //O-P
        public int Bit211 { get; set; } //P-F
        public int Bit212 { get; set; } //S-F
        public int Bit213 { get; set; } //Si-H
        public int Bit214 { get; set; } //Si-Si
        public int Bit215 { get; set; } //Si-Cl
        public int Bit216 { get; set; } //P-H
        public int Bit217 { get; set; } //P-P

        /// <summary>
        /// Section 4: Simple atom nearest neighbors - These bits test for the 
        /// presence of atom nearest neighbor patterns, regardless of 
        /// bond order (denoted by "~") or count, but where bond
        /// aromaticity (denoted by ":") is significant.
        /// </summary>
        public int Bit218 { get; set; } //C(~Br)(~C)
        public int Bit219 { get; set; } //C(~Br)(~C)(~C)
        public int Bit220 { get; set; } //C(~Br)(~H)
        public int Bit221 { get; set; } //C(~Br)(:C)
        public int Bit222 { get; set; } //C(~Br)(:N)
        public int Bit223 { get; set; } //C(~C)(~C)
        public int Bit224 { get; set; } //C(~C)(~C)(~C)
        public int Bit225 { get; set; } //C(~C)(~C)(~C)(~C)
        public int Bit226 { get; set; } //C(~C)(~C)(~C)(~H)
        public int Bit227 { get; set; } //C(~C)(~C)(~C)(~N)
        public int Bit228 { get; set; } //C(~C)(~C)(~C)(~O)
        public int Bit229 { get; set; } //C(~C)(~C)(~H)(~N)
        public int Bit230 { get; set; } //C(~C)(~C)(~H)(~O)
        public int Bit231 { get; set; } //C(~C)(~C)(~N)
        public int Bit232 { get; set; } //C(~C)(~C)(~O)
        public int Bit233 { get; set; } //C(~C)(~Cl)
        public int Bit234 { get; set; } //C(~C)(~Cl)(~H)
        public int Bit235 { get; set; } //C(~C)(~H)
        public int Bit236 { get; set; } //C(~C)(~H)(~N)
        public int Bit237 { get; set; } //C(~C)(~H)(~O)
        public int Bit238 { get; set; } //C(~C)(~H)(~O)(~O)
        public int Bit239 { get; set; } //C(~C)(~H)(~P)
        public int Bit240 { get; set; } //C(~C)(~H)(~S)
        public int Bit241 { get; set; } //C(~C)(~I)
        public int Bit242 { get; set; } //C(~C)(~N)
        public int Bit243 { get; set; } //C(~C)(~O)
        public int Bit244 { get; set; } //C(~C)(~S)
        public int Bit245 { get; set; } //C(~C)(~Si)
        public int Bit246 { get; set; } //C(~C)(:C)
        public int Bit247 { get; set; } //C(~C)(:C)(:C)
        public int Bit248 { get; set; } //C(~C)(:C)(:N)
        public int Bit249 { get; set; } //C(~C)(:N)
        public int Bit250 { get; set; } //C(~C)(:N)(:N)
        public int Bit251 { get; set; } //C(~Cl)(~Cl)
        public int Bit252 { get; set; } //C(~Cl)(~H)
        public int Bit253 { get; set; } //C(~Cl)(:C)
        public int Bit254 { get; set; } //C(~F)(~F)
        public int Bit255 { get; set; } //C(~F)(:C)
        public int Bit256 { get; set; } //C(~H)(~N)
        public int Bit257 { get; set; } //C(~H)(~O)
        public int Bit258 { get; set; } //C(~H)(~O)(~O)
        public int Bit259 { get; set; } //C(~H)(~S)
        public int Bit260 { get; set; } //C(~H)(~Si)
        public int Bit261 { get; set; } //C(~H)(:C)
        public int Bit262 { get; set; } //C(~H)(:C)(:C)
        public int Bit263 { get; set; } //C(~H)(:C)(:N)
        public int Bit264 { get; set; } //C(~H)(:N)
        public int Bit265 { get; set; } //C(~H)(~H)(~H)
        public int Bit266 { get; set; } //C(~N)(~N)
        public int Bit267 { get; set; } //C(~N)(:C)
        public int Bit268 { get; set; } //C(~N)(:C)(:C)
        public int Bit269 { get; set; } //C(~N)(:C)(:N)
        public int Bit270 { get; set; } //C(~N)(:N)
        public int Bit271 { get; set; } //C(~O)(~O)
        public int Bit272 { get; set; } //C(~O)(:C)
        public int Bit273 { get; set; } //C(~O)(:C)(:C)
        public int Bit274 { get; set; } //C(~S)(:C)
        public int Bit275 { get; set; } //C(:C)(:C)
        public int Bit276 { get; set; } //C(:C)(:C)(:C)
        public int Bit277 { get; set; } //C(:C)(:C)(:N)
        public int Bit278 { get; set; } //C(:C)(:N)
        public int Bit279 { get; set; } //C(:C)(:N)(:N)
        public int Bit280 { get; set; } //C(:N)(:N)
        public int Bit281 { get; set; } //N(~C)(~C)
        public int Bit282 { get; set; } //N(~C)(~C)(~C)
        public int Bit283 { get; set; } //N(~C)(~C)(~H)
        public int Bit284 { get; set; } //N(~C)(~H)
        public int Bit285 { get; set; } //N(~C)(~H)(~N)
        public int Bit286 { get; set; } //N(~C)(~O)
        public int Bit287 { get; set; } //N(~C)(:C)
        public int Bit288 { get; set; } //N(~C)(:C)(:C)
        public int Bit289 { get; set; } //N(~H)(~N)
        public int Bit290 { get; set; } //N(~H)(:C)
        public int Bit291 { get; set; } //N(~H)(:C)(:C)
        public int Bit292 { get; set; } //N(~O)(~O)
        public int Bit293 { get; set; } //N(~O)(:O)
        public int Bit294 { get; set; } //N(:C)(:C)
        public int Bit295 { get; set; } //N(:C)(:C)(:C)
        public int Bit296 { get; set; } //O(~C)(~C)
        public int Bit297 { get; set; } //O(~C)(~H)
        public int Bit298 { get; set; } //O(~C)(~P)
        public int Bit299 { get; set; } //O(~H)(~S)
        public int Bit300 { get; set; } //O(:C)(:C)
        public int Bit301 { get; set; } //P(~C)(~C)
        public int Bit302 { get; set; } //P(~O)(~O)
        public int Bit303 { get; set; } //S(~C)(~C)
        public int Bit304 { get; set; } //S(~C)(~H)
        public int Bit305 { get; set; } //S(~C)(~O)
        public int Bit306 { get; set; } //Si(~C)(~C)

        /// <summary>
        /// Section 5: Detailed atom neighborhoods - These bits test for the 
        //presence of detailed atom neighborhood patterns, 
        //regardless of count, but where bond orders are specific, 
        //bond aromaticity matches both single and double bonds, 
        //and where "-", "=", and "#" matches a single bond, double 
        //bond, and triple bond order, respectively.
        /// </summary>
        public int Bit307 { get; set; } //C=C
        public int Bit308 { get; set; } //C#C
        public int Bit309 { get; set; } //C=N
        public int Bit310 { get; set; } //C#N
        public int Bit311 { get; set; } //C=O
        public int Bit312 { get; set; } //C=S
        public int Bit313 { get; set; } //N=N
        public int Bit314 { get; set; } //N=O
        public int Bit315 { get; set; } //N=P
        public int Bit316 { get; set; } //O=P
        public int Bit317 { get; set; } //P=P
        public int Bit318 { get; set; } //C(#C)(-C)
        public int Bit319 { get; set; } //C(#C)(-H)
        public int Bit320 { get; set; } //C(#N)(-C)
        public int Bit321 { get; set; } //C(-C)(-C)(=C)
        public int Bit322 { get; set; } //C(-C)(-C)(=N)
        public int Bit323 { get; set; } //C(-C)(-C)(=O)
        public int Bit324 { get; set; } //C(-C)(-Cl)(=O)
        public int Bit325 { get; set; } //C(-C)(-H)(=C)
        public int Bit326 { get; set; } //C(-C)(-H)(=N)
        public int Bit327 { get; set; } //C(-C)(-H)(=O)
        public int Bit328 { get; set; } //C(-C)(-N)(=C)
        public int Bit329 { get; set; } //C(-C)(-N)(=N)
        public int Bit330 { get; set; } //C(-C)(-N)(=O)
        public int Bit331 { get; set; } //C(-C)(-O)(=O)
        public int Bit332 { get; set; } //C(-C)(=C)
        public int Bit333 { get; set; } //C(-C)(=N)
        public int Bit334 { get; set; } //C(-C)(=O)
        public int Bit335 { get; set; } //C(-Cl)(=O)
        public int Bit336 { get; set; } //C(-H)(-N)(=C)
        public int Bit337 { get; set; } //C(-H)(=C)
        public int Bit338 { get; set; } //C(-H)(=N)
        public int Bit339 { get; set; } //C(-H)(=O)
        public int Bit340 { get; set; } //C(-N)(=C)
        public int Bit341 { get; set; } //C(-N)(=N)
        public int Bit342 { get; set; } //C(-N)(=O)
        public int Bit343 { get; set; } //C(-O)(=O)
        public int Bit344 { get; set; } //N(-C)(=C)
        public int Bit345 { get; set; } //N(-C)(=O)
        public int Bit346 { get; set; } //N(-O)(=O)
        public int Bit347 { get; set; } //P(-O)(=O)
        public int Bit348 { get; set; } //S(-C)(=O)
        public int Bit349 { get; set; } //S(-O)(=O)
        public int Bit350 { get; set; } //S(=O)(=O)

        /// <summary>
        /// Section 6: Simple SMARTS patterns - These bits test for the 
        /// presence of simple SMARTS patterns, regardless of count, 
        /// but where bond orders are specific and bond aromaticity 
        /// matches both single and double bonds.
        /// </summary>
        /// 4 atom connection
        public int Bit351 { get; set; } //C-C-C#C
        public int Bit352 { get; set; } //O-C-C=N
        public int Bit353 { get; set; } //O-C-C=O
        public int Bit354 { get; set; } //N:C-S-[#1]
        public int Bit355 { get; set; } //N-C-C=C
        public int Bit356 { get; set; } //O=S-C-C
        public int Bit357 { get; set; } //N#C-C=C
        public int Bit358 { get; set; } //C=N-N-C
        public int Bit359 { get; set; } //O=S-C-N
        public int Bit360 { get; set; } //S-S-C:C
        public int Bit361 { get; set; } //C:C-C=C
        public int Bit362 { get; set; } //S:C:C:C
        public int Bit363 { get; set; } //C:N:C-C
        public int Bit364 { get; set; } //S-C:N:C
        public int Bit365 { get; set; } //S:C:C:N
        public int Bit366 { get; set; } //S-C=N-C
        public int Bit367 { get; set; } //C-O-C=C
        public int Bit368 { get; set; } //N-N-C:C
        public int Bit369 { get; set; } //S-C=N-[#1]
        public int Bit370 { get; set; } //S-C-S-C
        public int Bit371 { get; set; } //C:S:C-C
        public int Bit372 { get; set; } //O-S-C:C
        public int Bit373 { get; set; } //C:N-C:C
        public int Bit374 { get; set; } //N-S-C:C
        public int Bit375 { get; set; } //N-C:N:C
        public int Bit376 { get; set; } //N:C:C:N
        public int Bit377 { get; set; } //N-C:N:N
        public int Bit378 { get; set; } //N-C=N-C
        public int Bit379 { get; set; } //N-C=N-[#1]
        public int Bit380 { get; set; } //N-C-S-C
        public int Bit381 { get; set; } //C-C-C=C
        public int Bit382 { get; set; } //C-N:C-[#1]
        public int Bit383 { get; set; } //N-C:O:C
        public int Bit384 { get; set; } //O=C-C:C
        public int Bit385 { get; set; } //O=C-C:N
        public int Bit386 { get; set; } //C-N-C:C
        public int Bit387 { get; set; } //N:N-C-[#1]
        public int Bit388 { get; set; } //O-C:C:N
        public int Bit389 { get; set; } //O-C=C-C
        public int Bit390 { get; set; } //N-C:C:N
        public int Bit391 { get; set; } //C-S-C:C
        public int Bit392 { get; set; } //Cl-C:C-C
        public int Bit393 { get; set; } //N-C=C-[#1]
        public int Bit394 { get; set; } //Cl-C:C-[#1]
        public int Bit395 { get; set; } //N:C:N-C
        public int Bit396 { get; set; } //Cl-C:C-O
        public int Bit397 { get; set; } //C-C:N:C
        public int Bit398 { get; set; } //C-C-S-C
        public int Bit399 { get; set; } //S=C-N-C
        public int Bit400 { get; set; } //Br-C:C-C
        public int Bit401 { get; set; } //[#1]-N-N-[#1]
        public int Bit402 { get; set; } //S=C-N-[#1]
        public int Bit403 { get; set; } //S:C:C-[#1]
        public int Bit404 { get; set; } //O-N-C-C
        public int Bit405 { get; set; } //N-N-C-C
        public int Bit406 { get; set; } //[#1]-C=C-[#1]
        public int Bit407 { get; set; } //N-N-C-N
        public int Bit408 { get; set; } //O=C-N-N
        public int Bit409 { get; set; } //N=C-N-C
        public int Bit410 { get; set; } //C=C-C:C
        public int Bit411 { get; set; } //C:N-C-[#1]
        public int Bit412 { get; set; } //C-N-N-[#1]
        public int Bit413 { get; set; } //N:C:C-C
        public int Bit414 { get; set; } //C-C=C-C
        public int Bit415 { get; set; } //Cl-C:C-Cl
        public int Bit416 { get; set; } //C:C:N-[#1]
        public int Bit417 { get; set; } //[#1]-N-C-[#1]
        public int Bit418 { get; set; } //Cl-C-C-Cl
        public int Bit419 { get; set; } //N:C-C:C
        public int Bit420 { get; set; } //S-C:C-C
        public int Bit421 { get; set; } //S-C:C-[#1]
        public int Bit422 { get; set; } //S-C:C-N
        public int Bit423 { get; set; } //S-C:C-O
        public int Bit424 { get; set; } //O=C-C-C
        public int Bit425 { get; set; } //O=C-C-N
        public int Bit426 { get; set; } //O=C-C-O
        public int Bit427 { get; set; } //N=C-C-C
        public int Bit428 { get; set; } //N=C-C-[#1]
        public int Bit429 { get; set; } //C-N-C-[#1]
        public int Bit430 { get; set; } //O-C:C-C
        public int Bit431 { get; set; } //O-C:C-[#1]
        public int Bit432 { get; set; } //O-C:C-N
        public int Bit433 { get; set; } //O-C:C-O
        public int Bit434 { get; set; } //N-C:C-C
        public int Bit435 { get; set; } //N-C:C-[#1]
        public int Bit436 { get; set; } //N-C:C-N
        public int Bit437 { get; set; } //O-C-C:C
        public int Bit438 { get; set; } //N-C-C:C
        public int Bit439 { get; set; } //Cl-C-C-C
        public int Bit440 { get; set; } //Cl-C-C-O
        public int Bit441 { get; set; } //C:C-C:C
        public int Bit442 { get; set; } //O=C-C=C
        public int Bit443 { get; set; } //Br-C-C-C
        public int Bit444 { get; set; } //N=C-C=C
        public int Bit445 { get; set; } //C=C-C-C
        public int Bit446 { get; set; } //N:C-O-[#1]
        public int Bit447 { get; set; } //O=N-C:C
        public int Bit448 { get; set; } //O-C-N-[#1]
        public int Bit449 { get; set; } //N-C-N-C
        public int Bit450 { get; set; } //Cl-C-C=O
        public int Bit451 { get; set; } //Br-C-C=O
        public int Bit452 { get; set; } //O-C-O-C
        public int Bit453 { get; set; } //C=C-C=C
        public int Bit454 { get; set; } //C:C-O-C
        public int Bit455 { get; set; } //O-C-C-N
        public int Bit456 { get; set; } //O-C-C-O
        public int Bit457 { get; set; } //N#C-C-C
        public int Bit458 { get; set; } //N-C-C-N
        public int Bit459 { get; set; } //C:C-C-C
        public int Bit460 { get; set; } //[#1]-C-O-[#1]
        public int Bit461 { get; set; } //N:C:N:C
        public int Bit462 { get; set; } //O-C-C=C

        //5 atom connections
        public int Bit463 { get; set; } //O-C-C:C-C
        public int Bit464 { get; set; } //O-C-C:C-O
        public int Bit465 { get; set; } //N=C-C:C-[#1]
        public int Bit466 { get; set; } //C:C-N-C:C
        public int Bit467 { get; set; } //C-C:C-C:C
        public int Bit468 { get; set; } //O=C-C-C-C
        public int Bit469 { get; set; } //O=C-C-C-N
        public int Bit470 { get; set; } //O=C-C-C-O
        public int Bit471 { get; set; } //C-C-C-C-C
        public int Bit472 { get; set; } //Cl-C:C-O-C
        public int Bit473 { get; set; } //C:C-C=C-C
        public int Bit474 { get; set; } //C-C:C-N-C
        public int Bit475 { get; set; } //C-S-C-C-C
        public int Bit476 { get; set; } //N-C:C-O-[#1]
        public int Bit477 { get; set; } //O=C-C-C=O
        public int Bit478 { get; set; } //C-C:C-O-C
        public int Bit479 { get; set; } //C-C:C-O-[#1]
        public int Bit480 { get; set; } //Cl-C-C-C-C
        public int Bit481 { get; set; } //N-C-C-C-C
        public int Bit482 { get; set; } //N-C-C-C-N
        public int Bit483 { get; set; } //C-O-C-C=C
        public int Bit484 { get; set; } //C:C-C-C-C
        public int Bit485 { get; set; } //N=C-N-C-C
        public int Bit486 { get; set; } //O=C-C-C:C
        public int Bit487 { get; set; } //Cl-C:C:C-C
        public int Bit488 { get; set; } //[#1]-C-C=C-[#1]
        public int Bit489 { get; set; } //N-C:C:C-C
        public int Bit490 { get; set; } //N-C:C:C-N
        public int Bit491 { get; set; } //O=C-C-N-C
        public int Bit492 { get; set; } //C-C:C:C-C
        public int Bit493 { get; set; } //C-O-C-C:C
        public int Bit494 { get; set; } //O=C-C-O-C
        public int Bit495 { get; set; } //O-C:C-C-C
        public int Bit496 { get; set; } //N-C-C-C:C
        public int Bit497 { get; set; } //C-C-C-C:C
        public int Bit498 { get; set; } //Cl-C-C-N-C
        public int Bit499 { get; set; } //C-O-C-O-C
        public int Bit500 { get; set; } //N-C-C-N-C
        public int Bit501 { get; set; } //N-C-O-C-C
        public int Bit502 { get; set; } //C-N-C-C-C
        public int Bit503 { get; set; } //C-C-O-C-C
        public int Bit504 { get; set; } //N-C-C-O-C
        public int Bit505 { get; set; } //C:C:N:N:C
        public int Bit506 { get; set; } //C-C-C-O-[#1]
        public int Bit507 { get; set; } //C:C-C-C:C
        public int Bit508 { get; set; } //O-C-C=C-C
        public int Bit509 { get; set; } //C:C-O-C-C
        public int Bit510 { get; set; } //N-C:C:C:N
        public int Bit511 { get; set; } //O=C-O-C:C
        public int Bit512 { get; set; } //O=C-C:C-C
        public int Bit513 { get; set; } //O=C-C:C-N
        public int Bit514 { get; set; } //O=C-C:C-O
        public int Bit515 { get; set; } //C-O-C:C-C
        public int Bit516 { get; set; } //C-N-C-C:C
        public int Bit517 { get; set; } //S-C:C:C-N
        public int Bit518 { get; set; } //O-C:C-O-C
        public int Bit519 { get; set; } //O-C:C-O-[#1]
        public int Bit520 { get; set; } //C-C-O-C:C
        public int Bit521 { get; set; } //N-C-C:C-C
        public int Bit522 { get; set; } //C-C-C:C-C
        public int Bit523 { get; set; } //N-N-C-N-[#1]
        public int Bit524 { get; set; } //C-N-C-N-C
        public int Bit525 { get; set; } //O-C-C-C-C
        public int Bit526 { get; set; } //O-C-C-C-N
        public int Bit527 { get; set; } //O-C-C-C-O
        public int Bit528 { get; set; } //C=C-C-C-C
        public int Bit529 { get; set; } //O-C-C-C=C
        public int Bit530 { get; set; } //O-C-C-C=O
        public int Bit531 { get; set; } //[#1]-C-C-N-[#1]
        public int Bit532 { get; set; } //C-C=N-N-C
        public int Bit533 { get; set; } //O=C-N-C-C
        public int Bit534 { get; set; } //O=C-N-C-[#1]
        public int Bit535 { get; set; } //O=C-N-C-N
        public int Bit536 { get; set; } //O=N-C:C-N
        public int Bit537 { get; set; } //O=N-C:C-O
        public int Bit538 { get; set; } //O=C-N-C=O
        public int Bit539 { get; set; } //O-C:C:C-C
        public int Bit540 { get; set; } //O-C:C:C-N
        public int Bit541 { get; set; } //O-C:C:C-O
        public int Bit542 { get; set; } //N-C-N-C-C
        public int Bit543 { get; set; } //O-C-C-C:C
        public int Bit544 { get; set; } //C-C-N-C-C
        public int Bit545 { get; set; } //C-N-C:C-C
        public int Bit546 { get; set; } //C-C-S-C-C
        public int Bit547 { get; set; } //O-C-C-N-C
        public int Bit548 { get; set; } //C-C=C-C-C
        public int Bit549 { get; set; } //O-C-O-C-C
        public int Bit550 { get; set; } //O-C-C-O-C
        public int Bit551 { get; set; } //O-C-C-O-[#1]
        public int Bit552 { get; set; } //C-C=C-C=C
        public int Bit553 { get; set; } //N-C:C-C-C
        public int Bit554 { get; set; } //C=C-C-O-C
        public int Bit555 { get; set; } //C=C-C-O-[#1]
        public int Bit556 { get; set; } //C-C:C-C-C
        public int Bit557 { get; set; } //Cl-C:C-C=O
        public int Bit558 { get; set; } //Br-C:C:C-C
        public int Bit559 { get; set; } //O=C-C=C-C
        public int Bit560 { get; set; } //O=C-C=C-[#1]
        public int Bit561 { get; set; } //O=C-C=C-N
        public int Bit562 { get; set; } //N-C-N-C:C
        public int Bit563 { get; set; } //Br-C-C-C:C
        public int Bit564 { get; set; } //N#C-C-C-C
        public int Bit565 { get; set; } //C-C=C-C:C
        public int Bit566 { get; set; } //C-C-C=C-C

        //six atom connections
        public int Bit567 { get; set; } //C-C-C-C-C-C
        public int Bit568 { get; set; } //O-C-C-C-C-C
        public int Bit569 { get; set; } //O-C-C-C-C-O
        public int Bit570 { get; set; } //O-C-C-C-C-N
        public int Bit571 { get; set; } //N-C-C-C-C-C
        public int Bit572 { get; set; } //O=C-C-C-C-C
        public int Bit573 { get; set; } //O=C-C-C-C-N
        public int Bit574 { get; set; } //O=C-C-C-C-O
        public int Bit575 { get; set; } //O=C-C-C-C=O

        //seven atom connections
        public int Bit576 { get; set; } //C-C-C-C-C-C-C
        public int Bit577 { get; set; } //O-C-C-C-C-C-C
        public int Bit578 { get; set; } //O-C-C-C-C-C-O
        public int Bit579 { get; set; } //O-C-C-C-C-C-N
        public int Bit580 { get; set; } //O=C-C-C-C-C-C
        public int Bit581 { get; set; } //O=C-C-C-C-C-O
        public int Bit582 { get; set; } //O=C-C-C-C-C=O
        public int Bit583 { get; set; } //O=C-C-C-C-C-N

        //eight atom connections
        public int Bit584 { get; set; } //C-C-C-C-C-C-C-C
        public int Bit585 { get; set; } //C-C-C-C-C-C(C)-C
        public int Bit586 { get; set; } //O-C-C-C-C-C-C-C
        public int Bit587 { get; set; } //O-C-C-C-C-C(C)-C
        public int Bit588 { get; set; } //O-C-C-C-C-C-O-C
        public int Bit589 { get; set; } //O-C-C-C-C-C(O)-C
        public int Bit590 { get; set; } //O-C-C-C-C-C-N-C
        public int Bit591 { get; set; } //O-C-C-C-C-C(N)-C
        public int Bit592 { get; set; } //O=C-C-C-C-C-C-C
        public int Bit593 { get; set; } //O=C-C-C-C-C(O)-C
        public int Bit594 { get; set; } //O=C-C-C-C-C(=O)-C
        public int Bit595 { get; set; } //O=C-C-C-C-C(N)-C
        
        //branched chains and others
        public int Bit596 { get; set; } //C-C(C)-C-C
        public int Bit597 { get; set; } //C-C(C)-C-C-C
        public int Bit598 { get; set; } //C-C-C(C)-C-C
        public int Bit599 { get; set; } //C-C(C)(C)-C-C
        public int Bit600 { get; set; } //C-C(C)-C(C)-C
        //public int Bit601 { get; set; } //C(sugar)-S-C=N-O-S(sulfone), Glucosinolate
        //public int Bit602 { get; set; } //(Ester C)-O-C-C(-O-Ester C)-C-O, glycerolipid
        //public int Bit603 { get; set; } //(Ester C)-O-C-C(-O-Ester C)-C-O-P, glycerophospholipid
        //public int Bit604 { get; set; } //N-C(C-OH)-C-O, shingo base
        //public int Bit605 { get; set; } //N-C(C-OH)-C-O-P, shingophosphate
        //public int Bit606 { get; set; } //C-(amide bond)-N-C(C-OH)-C-O, shingolipid
        //public int Bit607 { get; set; } //C-(amide bond)-N-C(C-OH)-C-O-P, shingophospholipid
        //public int Bit608 { get; set; } //O-C-C(O)-C-O, glycerol
        //public int Bit609 { get; set; } //C(-C)=C-C=C-C(-C)
        //public int Bit610 { get; set; } //C-C=C-C(-C)=C

        /// <summary>
        /// Section 7: Complex SMARTS patterns - These bits test for the 
        //presence of complex SMARTS patterns, regardless of count, 
        //but where bond orders and bond aromaticity are specific.
        /// </summary>
        public int Bit611 { get; set; } //Cc1ccc(C)cc1
        public int Bit612 { get; set; } //Cc1ccc(O)cc1
        public int Bit613 { get; set; } //Cc1ccc(S)cc1
        public int Bit614 { get; set; } //Cc1ccc(N)cc1
        public int Bit615 { get; set; } //Cc1ccc(Cl)cc1
        public int Bit616 { get; set; } //Cc1ccc(Br)cc1
        public int Bit617 { get; set; } //Oc1ccc(O)cc1
        public int Bit618 { get; set; } //Oc1ccc(S)cc1
        public int Bit619 { get; set; } //Oc1ccc(N)cc1
        public int Bit620 { get; set; } //Oc1ccc(Cl)cc1
        public int Bit621 { get; set; } //Oc1ccc(Br)cc1
        public int Bit622 { get; set; } //Sc1ccc(S)cc1
        public int Bit623 { get; set; } //Sc1ccc(N)cc1
        public int Bit624 { get; set; } //Sc1ccc(Cl)cc1
        public int Bit625 { get; set; } //Sc1ccc(Br)cc1
        public int Bit626 { get; set; } //Nc1ccc(N)cc1
        public int Bit627 { get; set; } //Nc1ccc(Cl)cc1
        public int Bit628 { get; set; } //Nc1ccc(Br)cc1
        public int Bit629 { get; set; } //Clc1ccc(Cl)cc1
        public int Bit630 { get; set; } //Clc1ccc(Br)cc1
        public int Bit631 { get; set; } //Brc1ccc(Br)cc1
        public int Bit632 { get; set; } //Cc1cc(C)ccc1
        public int Bit633 { get; set; } //Cc1cc(O)ccc1
        public int Bit634 { get; set; } //Cc1cc(S)ccc1
        public int Bit635 { get; set; } //Cc1cc(N)ccc1
        public int Bit636 { get; set; } //Cc1cc(Cl)ccc1
        public int Bit637 { get; set; } //Cc1cc(Br)ccc1
        public int Bit638 { get; set; } //Oc1cc(O)ccc1
        public int Bit639 { get; set; } //Oc1cc(S)ccc1
        public int Bit640 { get; set; } //Oc1cc(N)ccc1
        public int Bit641 { get; set; } //Oc1cc(Cl)ccc1
        public int Bit642 { get; set; } //Oc1cc(Br)ccc1
        public int Bit643 { get; set; } //Sc1cc(S)ccc1
        public int Bit644 { get; set; } //Sc1cc(N)ccc1
        public int Bit645 { get; set; } //Sc1cc(Cl)ccc1
        public int Bit646 { get; set; } //Sc1cc(Br)ccc1
        public int Bit647 { get; set; } //Nc1cc(N)ccc1
        public int Bit648 { get; set; } //Nc1cc(Cl)ccc1
        public int Bit649 { get; set; } //Nc1cc(Br)ccc1
        public int Bit650 { get; set; } //Clc1cc(Cl)ccc1
        public int Bit651 { get; set; } //Clc1cc(Br)ccc1
        public int Bit652 { get; set; } //Brc1cc(Br)ccc1
        public int Bit653 { get; set; } //Cc1c(C)cccc1
        public int Bit654 { get; set; } //Cc1c(O)cccc1
        public int Bit655 { get; set; } //Cc1c(S)cccc1
        public int Bit656 { get; set; } //Cc1c(N)cccc1
        public int Bit657 { get; set; } //Cc1c(Cl)cccc1
        public int Bit658 { get; set; } //Cc1c(Br)cccc1
        public int Bit659 { get; set; } //Oc1c(O)cccc1
        public int Bit660 { get; set; } //Oc1c(S)cccc1
        public int Bit661 { get; set; } //Oc1c(N)cccc1
        public int Bit662 { get; set; } //Oc1c(Cl)cccc1
        public int Bit663 { get; set; } //Oc1c(Br)cccc1
        public int Bit664 { get; set; } //Sc1c(S)cccc1
        public int Bit665 { get; set; } //Sc1c(N)cccc1
        public int Bit666 { get; set; } //Sc1c(Cl)cccc1
        public int Bit667 { get; set; } //Sc1c(Br)cccc1
        public int Bit668 { get; set; } //Nc1c(N)cccc1
        public int Bit669 { get; set; } //Nc1c(Cl)cccc1
        public int Bit670 { get; set; } //Nc1c(Br)cccc1
        public int Bit671 { get; set; } //Clc1c(Cl)cccc1
        public int Bit672 { get; set; } //Clc1c(Br)cccc1
        public int Bit673 { get; set; } //Brc1c(Br)cccc1
        public int Bit674 { get; set; } //CC1CCC(C)CC1
        public int Bit675 { get; set; } //CC1CCC(O)CC1
        public int Bit676 { get; set; } //CC1CCC(S)CC1
        public int Bit677 { get; set; } //CC1CCC(N)CC1
        public int Bit678 { get; set; } //CC1CCC(Cl)CC1
        public int Bit679 { get; set; } //CC1CCC(Br)CC1
        public int Bit680 { get; set; } //OC1CCC(O)CC1
        public int Bit681 { get; set; } //OC1CCC(S)CC1
        public int Bit682 { get; set; } //OC1CCC(N)CC1
        public int Bit683 { get; set; } //OC1CCC(Cl)CC1
        public int Bit684 { get; set; } //OC1CCC(Br)CC1
        public int Bit685 { get; set; } //SC1CCC(S)CC1
        public int Bit686 { get; set; } //SC1CCC(N)CC1
        public int Bit687 { get; set; } //SC1CCC(Cl)CC1
        public int Bit688 { get; set; } //SC1CCC(Br)CC1
        public int Bit689 { get; set; } //NC1CCC(N)CC1
        public int Bit690 { get; set; } //NC1CCC(Cl)CC1
        public int Bit691 { get; set; } //NC1CCC(Br)CC1
        public int Bit692 { get; set; } //ClC1CCC(Cl)CC1
        public int Bit693 { get; set; } //ClC1CCC(Br)CC1
        public int Bit694 { get; set; } //BrC1CCC(Br)CC1
        public int Bit695 { get; set; } //CC1CC(C)CCC1
        public int Bit696 { get; set; } //CC1CC(O)CCC1
        public int Bit697 { get; set; } //CC1CC(S)CCC1
        public int Bit698 { get; set; } //CC1CC(N)CCC1
        public int Bit699 { get; set; } //CC1CC(Cl)CCC1
        public int Bit700 { get; set; } //CC1CC(Br)CCC1
        public int Bit701 { get; set; } //OC1CC(O)CCC1
        public int Bit702 { get; set; } //OC1CC(S)CCC1
        public int Bit703 { get; set; } //OC1CC(N)CCC1
        public int Bit704 { get; set; } //OC1CC(Cl)CCC1
        public int Bit705 { get; set; } //OC1CC(Br)CCC1
        public int Bit706 { get; set; } //SC1CC(S)CCC1
        public int Bit707 { get; set; } //SC1CC(N)CCC1
        public int Bit708 { get; set; } //SC1CC(Cl)CCC1
        public int Bit709 { get; set; } //SC1CC(Br)CCC1
        public int Bit710 { get; set; } //NC1CC(N)CCC1
        public int Bit711 { get; set; } //NC1CC(Cl)CCC1
        public int Bit712 { get; set; } //NC1CC(Br)CCC1
        public int Bit713 { get; set; } //ClC1CC(Cl)CCC1
        public int Bit714 { get; set; } //ClC1CC(Br)CCC1
        public int Bit715 { get; set; } //BrC1CC(Br)CCC1
        public int Bit716 { get; set; } //CC1C(C)CCCC1
        public int Bit717 { get; set; } //CC1C(O)CCCC1
        public int Bit718 { get; set; } //CC1C(S)CCCC1
        public int Bit719 { get; set; } //CC1C(N)CCCC1
        public int Bit720 { get; set; } //CC1C(Cl)CCCC1
        public int Bit721 { get; set; } //CC1C(Br)CCCC1
        public int Bit722 { get; set; } //OC1C(O)CCCC1
        public int Bit723 { get; set; } //OC1C(S)CCCC1
        public int Bit724 { get; set; } //OC1C(N)CCCC1
        public int Bit725 { get; set; } //OC1C(Cl)CCCC1
        public int Bit726 { get; set; } //OC1C(Br)CCCC1
        public int Bit727 { get; set; } //SC1C(S)CCCC1
        public int Bit728 { get; set; } //SC1C(N)CCCC1
        public int Bit729 { get; set; } //SC1C(Cl)CCCC1
        public int Bit730 { get; set; } //SC1C(Br)CCCC1
        public int Bit731 { get; set; } //NC1C(N)CCCC1
        public int Bit732 { get; set; } //NC1C(Cl)CCCC1
        public int Bit733 { get; set; } //NC1C(Br)CCCC1
        public int Bit734 { get; set; } //ClC1C(Cl)CCCC1
        public int Bit735 { get; set; } //ClC1C(Br)CCCC1
        public int Bit736 { get; set; } //BrC1C(Br)CCCC1
        public int Bit737 { get; set; } //CC1CC(C)CC1
        public int Bit738 { get; set; } //CC1CC(O)CC1
        public int Bit739 { get; set; } //CC1CC(S)CC1
        public int Bit740 { get; set; } //CC1CC(N)CC1
        public int Bit741 { get; set; } //CC1CC(Cl)CC1
        public int Bit742 { get; set; } //CC1CC(Br)CC1
        public int Bit743 { get; set; } //OC1CC(O)CC1
        public int Bit744 { get; set; } //OC1CC(S)CC1
        public int Bit745 { get; set; } //OC1CC(N)CC1
        public int Bit746 { get; set; } //OC1CC(Cl)CC1
        public int Bit747 { get; set; } //OC1CC(Br)CC1
        public int Bit748 { get; set; } //SC1CC(S)CC1
        public int Bit749 { get; set; } //SC1CC(N)CC1
        public int Bit750 { get; set; } //SC1CC(Cl)CC1
        public int Bit751 { get; set; } //SC1CC(Br)CC1
        public int Bit752 { get; set; } //NC1CC(N)CC1
        public int Bit753 { get; set; } //NC1CC(Cl)CC1
        public int Bit754 { get; set; } //NC1CC(Br)CC1
        public int Bit755 { get; set; } //ClC1CC(Cl)CC1
        public int Bit756 { get; set; } //ClC1CC(Br)CC1
        public int Bit757 { get; set; } //BrC1CC(Br)CC1
        public int Bit758 { get; set; } //CC1C(C)CCC1
        public int Bit759 { get; set; } //CC1C(O)CCC1
        public int Bit760 { get; set; } //CC1C(S)CCC1
        public int Bit761 { get; set; } //CC1C(N)CCC1
        public int Bit762 { get; set; } //CC1C(Cl)CCC1
        public int Bit763 { get; set; } //CC1C(Br)CCC1
        public int Bit764 { get; set; } //OC1C(O)CCC1
        public int Bit765 { get; set; } //OC1C(S)CCC1
        public int Bit766 { get; set; } //OC1C(N)CCC1
        public int Bit767 { get; set; } //OC1C(Cl)CCC1
        public int Bit768 { get; set; } //OC1C(Br)CCC1
        public int Bit769 { get; set; } //SC1C(S)CCC1
        public int Bit770 { get; set; } //SC1C(N)CCC1
        public int Bit771 { get; set; } //SC1C(Cl)CCC1
        public int Bit772 { get; set; } //SC1C(Br)CCC1
        public int Bit773 { get; set; } //NC1C(N)CCC1
        public int Bit774 { get; set; } //NC1C(Cl)CC1
        public int Bit775 { get; set; } //NC1C(Br)CCC1
        public int Bit776 { get; set; } //ClC1C(Cl)CCC1
        public int Bit777 { get; set; } //ClC1C(Br)CCC1
        public int Bit778 { get; set; } //BrC1C(Br)CCC1

        //Fragment fingerprinter
        public int VNWKTOKETHGBQD { get; set; } // Formula:CH4, Mass:16.031, SMILES:C
        public int QGZKDVFQNNGYKY { get; set; } // Formula:H3N, Mass:17.027, SMILES:N
        public int XLYOFNOQVPJJNP { get; set; } // Formula:H2O, Mass:18.011, SMILES:O
        public int LELOWRISYMNNSU { get; set; } // Formula:CHN, Mass:27.011, SMILES:N#C
        public int VGGSQFUCUMXWEO { get; set; } // Formula:C2H4, Mass:28.031, SMILES:C=C
        public int WDWDWGRYHDPSDS { get; set; } // Formula:CH3N, Mass:29.027, SMILES:N=C
        public int WSFSSNUMVMOOMR { get; set; } // Formula:CH2O, Mass:30.011, SMILES:O=C
        public int OTMSDBZUPAUEDD { get; set; } // Formula:C2H6, Mass:30.047, SMILES:CC
        public int BAVYZALUXZFZLV { get; set; } // Formula:CH5N, Mass:31.042, SMILES:NC
        public int OKKJLVBELUTLKV { get; set; } // Formula:CH4O, Mass:32.026, SMILES:OC
        public int RWSOTUBLDIXVET { get; set; } // Formula:H2S, Mass:33.988, SMILES:S
        public int LVZWSLJZHVFIQJ { get; set; } // Formula:C3H6, Mass:42.047, SMILES:C1CC1
        public int QQONPFPTGQHPMA { get; set; } // Formula:C3H6, Mass:42.047, SMILES:C=CC
        public int IKHGUXGNUITLKF { get; set; } // Formula:C2H4O, Mass:44.026, SMILES:O=CC
        public int IMROMDMJAWUWLK { get; set; } // Formula:C2H4O, Mass:44.026, SMILES:OC=C
        public int PNKUSGQVOMIXLU { get; set; } // Formula:CH4N2, Mass:44.037, SMILES:N=CN
        public int ATUOYWHBWRKTHZ { get; set; } // Formula:C3H8, Mass:44.063, SMILES:CCC
        public int ZHNUHDYFZUAESO { get; set; } // Formula:CH3NO, Mass:45.021, SMILES:N=CO
        public int QUSNBJAOOMFDIB { get; set; } // Formula:C2H7N, Mass:45.058, SMILES:NCC
        public int ROSDSFDQCJNGOL { get; set; } // Formula:C2H7N, Mass:45.058, SMILES:N(C)C
        public int JCXJVPUVTGWSNB { get; set; } // Formula:NO2, Mass:45.993, SMILES:O=[N+][O-]
        public int BDAGIHXWWSANSR { get; set; } // Formula:CH2O2, Mass:46.005, SMILES:O=CO
        public int LCGLNKUTAGEVQW { get; set; } // Formula:C2H6O, Mass:46.042, SMILES:O(C)C
        public int LFQSCWFLJHTTHZ { get; set; } // Formula:C2H6O, Mass:46.042, SMILES:OCC
        public int LSDPWZHWYPCBBB { get; set; } // Formula:CH4S, Mass:48.003, SMILES:CS
        public int CKFGINPQOCXMAZ { get; set; } // Formula:CH4O2, Mass:48.021, SMILES:OCO
        public int HGINCPLSRVDWNT { get; set; } // Formula:C3H4O, Mass:56.026, SMILES:O=CC=C
        public int IAQRGUVFOMOMEM { get; set; } // Formula:C4H8, Mass:56.063, SMILES:C(=CC)C
        public int VQTUBCCKSQIDNK { get; set; } // Formula:C4H8, Mass:56.063, SMILES:C=C(C)C
        public int VXNZUUAINFGPBY { get; set; } // Formula:C4H8, Mass:56.063, SMILES:C=CCC
        public int CSCPPACGZOOCGX { get; set; } // Formula:C3H6O, Mass:58.042, SMILES:O=C(C)C
        public int GOOHAUXETOMSMM { get; set; } // Formula:C3H6O, Mass:58.042, SMILES:O1CC1C
        public int NBBJYMSMWIIQGU { get; set; } // Formula:C3H6O, Mass:58.042, SMILES:O=CCC
        public int XJRBAMWJDBPFIM { get; set; } // Formula:C3H6O, Mass:58.042, SMILES:O(C=C)C
        public int XXROGKLTLUQVRX { get; set; } // Formula:C3H6O, Mass:58.042, SMILES:OCC=C
        public int IJDNQMDRQITEOD { get; set; } // Formula:C4H10, Mass:58.078, SMILES:CCCC
        public int NNPPMTNAJDCUHE { get; set; } // Formula:C4H10, Mass:58.078, SMILES:CC(C)C
        public int GRHBQAYDJPGGLF { get; set; } // Formula:CHNS, Mass:58.983, SMILES:N=C=S
        public int ATHHXGZTWNVVOU { get; set; } // Formula:C2H5NO, Mass:59.037, SMILES:O=CNC
        public int DLFVBJFMPXGRIB { get; set; } // Formula:C2H5NO, Mass:59.037, SMILES:N=C(O)C
        public int LYIIBVSRGJSHAV { get; set; } // Formula:C2H5NO, Mass:59.037, SMILES:O=CCN
        public int ZRALSGWEFCBTJO { get; set; } // Formula:CH5N3, Mass:59.048, SMILES:N=C(N)N
        public int INXJOSXEHHMQQF { get; set; } // Formula:C3H9N, Mass:59.073, SMILES:C[N+](C)C
        public int GETQZCLCWQTVFV { get; set; } // Formula:C3H9N, Mass:59.073, SMILES:N(C)(C)C
        public int JJWLVOIRVHMVIS { get; set; } // Formula:C3H9N, Mass:59.073, SMILES:NC(C)C
        public int LIWAQLJGPBVORC { get; set; } // Formula:C3H9N, Mass:59.073, SMILES:N(C)CC
        public int WGYKZJWCGVVSQN { get; set; } // Formula:C3H9N, Mass:59.073, SMILES:NCCC
        public int QTBSBXVTEAMEQO { get; set; } // Formula:C2H4O2, Mass:60.021, SMILES:O=C(O)C
        public int TZIHFWKZFHZASV { get; set; } // Formula:C2H4O2, Mass:60.021, SMILES:O=COC
        public int WGCNASOHLSPBMP { get; set; } // Formula:C2H4O2, Mass:60.021, SMILES:O=CCO
        public int XSQUKJJJFZCRTK { get; set; } // Formula:CH4N2O, Mass:60.032, SMILES:N=C(O)N
        public int BDERNNFJNOPAEC { get; set; } // Formula:C3H8O, Mass:60.058, SMILES:OCCC
        public int KFZMGEQAYNKOFK { get; set; } // Formula:C3H8O, Mass:60.058, SMILES:OC(C)C
        public int XOBKSJJDNFUZPF { get; set; } // Formula:C3H8O, Mass:60.058, SMILES:O(C)CC
        public int XOTDURGROKNGTI { get; set; } // Formula:C2H8N2, Mass:60.069, SMILES:NCNC
        public int HZAXFHJVJLSVMW { get; set; } // Formula:C2H7NO, Mass:61.053, SMILES:OCCN
        public int DNJIEGIFACGWOD { get; set; } // Formula:C2H6S, Mass:62.019, SMILES:CCS
        public int QMMFVYPAHWMCMS { get; set; } // Formula:C2H6S, Mass:62.019, SMILES:CSC
        public int LYCAIKOWRPUZTN { get; set; } // Formula:C2H6O2, Mass:62.037, SMILES:OCCO
        public int RAHZWNYVWXNFOC { get; set; } // Formula:O2S, Mass:63.962, SMILES:O=S=O
        public int BOTYRLOGQKSOAE { get; set; } // Formula:CH4OS, Mass:63.998, SMILES:O=SC
        public int RAXXELZNTBOGNW { get; set; } // Formula:C3H4N2, Mass:68.037, SMILES:N=1C=CNC=1
        public int NSPMIYGKQJPBQR { get; set; } // Formula:C2H3N3, Mass:69.033, SMILES:N=1C=NNC=1
        public int MLUCVPSAIODCQM { get; set; } // Formula:C4H6O, Mass:70.042, SMILES:O=CC=CC
        public int BKOOMYPCSUNDGP { get; set; } // Formula:C5H10, Mass:70.078, SMILES:C(=C(C)C)C
        public int QMMOXUPEWRXHJS { get; set; } // Formula:C5H10, Mass:70.078, SMILES:C(=CCC)C
        public int RGSFGYAAUTVSQA { get; set; } // Formula:C5H10, Mass:70.078, SMILES:C1CCCC1
        public int YWAKXRMUMFPDSH { get; set; } // Formula:C5H10, Mass:70.078, SMILES:C=CCCC
        public int RWRDLPDLKQPQOW { get; set; } // Formula:C4H9N, Mass:71.073, SMILES:N1CCCC1
        public int GMSHJLUJOABYOM { get; set; } // Formula:C3H4O2, Mass:72.021, SMILES:O=CC=CO
        public int NIXOWILDQLNWCW { get; set; } // Formula:C3H4O2, Mass:72.021, SMILES:O=C(O)C=C
        public int QRFGDGZPNFSNCE { get; set; } // Formula:C3H4O2, Mass:72.021, SMILES:O=CC(O)=C
        public int MKUWVMRNQOOSAT { get; set; } // Formula:C4H8O, Mass:72.058, SMILES:OC(C=C)C
        public int ZSPTYLOMNJNZNG { get; set; } // Formula:C4H8O, Mass:72.058, SMILES:OCCC=C
        public int ZTQSAGDEMFDKMZ { get; set; } // Formula:C4H8O, Mass:72.058, SMILES:O=CCCC
        public int MCGDEFQPHREUNF { get; set; } // Formula:C3H8N2, Mass:72.069, SMILES:N(=CN)CC
        public int OFBQJSOFQDEBGM { get; set; } // Formula:C5H12, Mass:72.094, SMILES:CCCCC
        public int QWTDNUCVQCZILF { get; set; } // Formula:C5H12, Mass:72.094, SMILES:CCC(C)C
        public int LGDSHSYDSCRFAB { get; set; } // Formula:C2H3NS, Mass:72.999, SMILES:C(=NC)=S
        public int QLNJFJADRCOGBJ { get; set; } // Formula:C3H7NO, Mass:73.053, SMILES:N=C(O)CC
        public int QPMCUNAXNMSGTK { get; set; } // Formula:C3H7NO, Mass:73.053, SMILES:O=CC(N)C
        public int DAZXVJBJRMWXJP { get; set; } // Formula:C4H11N, Mass:73.089, SMILES:N(C)(C)CC
        public int GVWISOJSERXQBM { get; set; } // Formula:C4H11N, Mass:73.089, SMILES:N(C)CCC
        public int HPNMFZURTQLUMO { get; set; } // Formula:C4H11N, Mass:73.089, SMILES:N(CC)CC
        public int HQABUPZFAYXKJW { get; set; } // Formula:C4H11N, Mass:73.089, SMILES:NCCCC
        public int KDSNLYIMUZNERS { get; set; } // Formula:C4H11N, Mass:73.089, SMILES:NCC(C)C
        public int XHFGWHUWQXTGAT { get; set; } // Formula:C4H11N, Mass:73.089, SMILES:N(C)C(C)C
        public int HHLFWLYXYJOTON { get; set; } // Formula:C2H2O3, Mass:74, SMILES:O=CC(=O)O
        public int AKXKFZDCRYJKTF { get; set; } // Formula:C3H6O2, Mass:74.037, SMILES:O=CCCO
        public int BSABBBMNWQWLLU { get; set; } // Formula:C3H6O2, Mass:74.037, SMILES:O=CC(O)C
        public int KXKVLQRXCPHEJC { get; set; } // Formula:C3H6O2, Mass:74.037, SMILES:O=C(OC)C
        public int WBJINCZRORDGAQ { get; set; } // Formula:C3H6O2, Mass:74.037, SMILES:O=COCC
        public int XBDQKXXYIPTUBI { get; set; } // Formula:C3H6O2, Mass:74.037, SMILES:O=C(O)CC
        public int YSEFYOVWKJXNCH { get; set; } // Formula:C3H6O2, Mass:74.037, SMILES:O=CCOC
        public int BEBCJVAWIBVWNZ { get; set; } // Formula:C2H6N2O, Mass:74.048, SMILES:N=C(O)CN
        public int BTANRVKWQNVYAZ { get; set; } // Formula:C4H10O, Mass:74.073, SMILES:OC(C)CC
        public int LRHPLDYGYMQRHN { get; set; } // Formula:C4H10O, Mass:74.073, SMILES:OCCCC
        public int VNKYTQGIUYNRMY { get; set; } // Formula:C4H10O, Mass:74.073, SMILES:O(C)CCC
        public int ZXEKIIBDNHEJCQ { get; set; } // Formula:C4H10O, Mass:74.073, SMILES:OCC(C)C
        public int BYYUJTPNKCFZST { get; set; } // Formula:C2H5NO2, Mass:75.032, SMILES:O=CNOC
        public int DHMQDGOQFOQNFH { get; set; } // Formula:C2H5NO2, Mass:75.032, SMILES:O=C(O)CN
        public int HXKKHQJGJAFBHI { get; set; } // Formula:C3H9NO, Mass:75.068, SMILES:OC(C)CN
        public int WUGQZFFCHPXWKQ { get; set; } // Formula:C3H9NO, Mass:75.068, SMILES:OCCCN
        public int AEMRFAOFKBGASW { get; set; } // Formula:C2H4O3, Mass:76.016, SMILES:O=C(O)CO
        public int DNIAPMSPPWPWGF { get; set; } // Formula:C3H8O2, Mass:76.052, SMILES:OCC(O)C
        public int RRLWYLINGKISHN { get; set; } // Formula:C3H8O2, Mass:76.052, SMILES:OCOCC
        public int XNWFRZJHXBZDAG { get; set; } // Formula:C3H8O2, Mass:76.052, SMILES:OCCOC
        public int YPFDHNVEDLHUCE { get; set; } // Formula:C3H8O2, Mass:76.052, SMILES:OCCCO
        public int UFULAYFCSOUIOV { get; set; } // Formula:C2H7NS, Mass:77.03, SMILES:NCCS
        public int DGVVWUTYPXICAM { get; set; } // Formula:C2H6OS, Mass:78.014, SMILES:OCCS
        public int PAWYOSHGFARYKR { get; set; } // Formula:C2H6OS, Mass:78.014, SMILES:O=SCC
        public int VIBDJEWPNNCFQO { get; set; } // Formula:C2H6O3, Mass:78.032, SMILES:OCC(O)O
        public int UHOVQNZJYSORNB { get; set; } // Formula:C6H6, Mass:78.047, SMILES:C1=CC=CC=C1
        public int JUJWROOIHBZHMG { get; set; } // Formula:C5H5N, Mass:79.042, SMILES:N=1C=CC=CC=1
        public int KEIVLHIFSZBKGU { get; set; } // Formula:CH4O2S, Mass:79.993, SMILES:O=S(=O)C
        public int AHZVGADFMXTGBU { get; set; } // Formula:H3NO2S, Mass:80.988, SMILES:O=S(=O)N
        public int BDHFUVZGWQCTTF { get; set; } // Formula:H2O3S, Mass:81.972, SMILES:O=S(=O)O
        public int ABLZXFCXXLZCGV { get; set; } // Formula:H3O3P, Mass:81.982, SMILES:O=P(O)O
        public int XLSZMDLNRCVEIJ { get; set; } // Formula:C4H6N2, Mass:82.053, SMILES:N1=CNC(=C1)C
        public int PRBHEGAFLDMLAL { get; set; } // Formula:C6H10, Mass:82.078, SMILES:C=CCC=CC
        public int ACZNIBVNGPLHAC { get; set; } // Formula:C5H8O, Mass:84.058, SMILES:OCC=CC=C
        public int BEQGRRJLJLVQAQ { get; set; } // Formula:C6H12, Mass:84.094, SMILES:C(=C(C)CC)C
        public int GDOPTJXRTPNYNR { get; set; } // Formula:C6H12, Mass:84.094, SMILES:CC1CCCC1
        public int LDTAOIUHUHHCMU { get; set; } // Formula:C6H12, Mass:84.094, SMILES:C=CC(C)CC
        public int LIKMAJRDDDTEIG { get; set; } // Formula:C6H12, Mass:84.094, SMILES:C=CCCCC
        public int RYPKRALMXUUNKS { get; set; } // Formula:C6H12, Mass:84.094, SMILES:C(=CCCC)C
        public int XDTMQSROBMDMFD { get; set; } // Formula:C6H12, Mass:84.094, SMILES:C1CCCCC1
        public int ZQDPJFUHLCOCRG { get; set; } // Formula:C6H12, Mass:84.094, SMILES:C(=CCC)CC
        public int AVFZOVWCLRSYKC { get; set; } // Formula:C5H11N, Mass:85.089, SMILES:N1(C)CCCC1
        public int NQRYJNQNLNOLGT { get; set; } // Formula:C5H11N, Mass:85.089, SMILES:N1CCCCC1
        public int BAPJBEWLBFYGME { get; set; } // Formula:C4H6O2, Mass:86.037, SMILES:O=C(OC)C=C
        public int CERQOIWHTDAKMF { get; set; } // Formula:C4H6O2, Mass:86.037, SMILES:O=C(O)C(=C)C
        public int XJQSLJZYZFSHAI { get; set; } // Formula:C4H6O2, Mass:86.037, SMILES:O=CC=COC
        public int YEJRWHAVMIAJKC { get; set; } // Formula:C4H6O2, Mass:86.037, SMILES:O=C1OCCC1
        public int FSUXYWPILZJGCC { get; set; } // Formula:C5H10O, Mass:86.073, SMILES:OCCC=CC
        public int GJYMQFMQRRNLCY { get; set; } // Formula:C5H10O, Mass:86.073, SMILES:OC(C=CC)C
        public int HGBOYTHUEUWSSQ { get; set; } // Formula:C5H10O, Mass:86.073, SMILES:O=CCCCC
        public int JWUJQDFVADABEY { get; set; } // Formula:C5H10O, Mass:86.073, SMILES:O1CCCC1C
        public int NEJDKFPXHQRVMV { get; set; } // Formula:C5H10O, Mass:86.073, SMILES:OCC(=CC)C
        public int AFABGHUZZDYHJO { get; set; } // Formula:C6H14, Mass:86.11, SMILES:CCCC(C)C
        public int HNRMPXKDFBEGFZ { get; set; } // Formula:C6H14, Mass:86.11, SMILES:CCC(C)(C)C
        public int PFEOZHBOMNWTJB { get; set; } // Formula:C6H14, Mass:86.11, SMILES:CCC(C)CC
        public int VLKZOEOYAKHREP { get; set; } // Formula:C6H14, Mass:86.11, SMILES:CCCCCC
        public int TWSCVZUCNWUBGX { get; set; } // Formula:C3H5NO2, Mass:87.032, SMILES:N=C(O)C=CO
        public int DNSISZSEWVHGLH { get; set; } // Formula:C4H9NO, Mass:87.068, SMILES:N=C(O)CCC
        public int DZQLQEYLEYWJIB { get; set; } // Formula:C4H9NO, Mass:87.068, SMILES:O=CCCCN
        public int FGEPRNXUNITOCW { get; set; } // Formula:C4H9NO, Mass:87.068, SMILES:O=CC(N)CC
        public int SUUDTPGCUKBECW { get; set; } // Formula:C4H9NO, Mass:87.068, SMILES:O=CNCCC
        public int YNAVUWVOSKDBBP { get; set; } // Formula:C4H9NO, Mass:87.068, SMILES:O1CCNCC1
        public int BMFVGAAISNGQNM { get; set; } // Formula:C5H13N, Mass:87.105, SMILES:NCCC(C)C
        public int DPBLXKKOBLCELK { get; set; } // Formula:C5H13N, Mass:87.105, SMILES:NCCCCC
        public int GNVRJGIVDSQCOP { get; set; } // Formula:C5H13N, Mass:87.105, SMILES:N(C)(CC)CC
        public int QCOGKXLOEWLIDC { get; set; } // Formula:C5H13N, Mass:87.105, SMILES:CNCCCC
        public int VJROPLWGFCORRM { get; set; } // Formula:C5H13N, Mass:87.105, SMILES:NCC(C)CC
        public int ZUHZZVMEUAUWHY { get; set; } // Formula:C5H13N, Mass:87.105, SMILES:CN(C)CCC
        public int FERIUCNNQQJTOY { get; set; } // Formula:C4H8O2, Mass:88.052, SMILES:O=C(O)CCC
        public int KQNPFQTWMSNSAP { get; set; } // Formula:C4H8O2, Mass:88.052, SMILES:O=C(O)C(C)C
        public int PIAOXUVIBAKVSP { get; set; } // Formula:C4H8O2, Mass:88.052, SMILES:OCCCC=O
        public int XEKOWRVHYACXOJ { get; set; } // Formula:C4H8O2, Mass:88.052, SMILES:O=C(OCC)C
        public int HQMLIDZJXVVKCW { get; set; } // Formula:C3H8N2O, Mass:88.064, SMILES:N=C(O)C(N)C
        public int RSDOASZYYCOXIB { get; set; } // Formula:C3H8N2O, Mass:88.064, SMILES:N=C(O)CCN
        public int AMQJEAYHLZJPGS { get; set; } // Formula:C5H12O, Mass:88.089, SMILES:OCCCCC
        public int AQIXEPGDORPWBJ { get; set; } // Formula:C5H12O, Mass:88.089, SMILES:OC(CC)CC
        public int FVNIMHIOIXPIQT { get; set; } // Formula:C5H12O, Mass:88.089, SMILES:O(C)C(C)CC
        public int JYVLIDXNZAXMDK { get; set; } // Formula:C5H12O, Mass:88.089, SMILES:OC(C)CCC
        public int MXLMTQWGSQIYOW { get; set; } // Formula:C5H12O, Mass:88.089, SMILES:OC(C)C(C)C
        public int PHTQWCKDNZKARW { get; set; } // Formula:C5H12O, Mass:88.089, SMILES:OCCC(C)C
        public int YOMFVLRTMZWACQ { get; set; } // Formula:C5H14N, Mass:88.112, SMILES:CC[N+](C)(C)C
        public int QNAYBMKLOCPYGJ { get; set; } // Formula:C3H7NO2, Mass:89.048, SMILES:O=C(O)C(N)C
        public int UCMIRNVEIXFBKS { get; set; } // Formula:C3H7NO2, Mass:89.048, SMILES:O=C(O)CCN
        public int UXPVGJOKPUABJV { get; set; } // Formula:C3H7NO2, Mass:89.048, SMILES:O=CC(N)CO
        public int UEEJHVSXFDXPFK { get; set; } // Formula:C4H11NO, Mass:89.084, SMILES:OCCN(C)C
        public int ALRHLSYJTWAHJZ { get; set; } // Formula:C3H6O3, Mass:90.032, SMILES:O=C(O)CCO
        public int JVTAAEKCZFNVCJ { get; set; } // Formula:C3H6O3, Mass:90.032, SMILES:O=C(O)C(O)C
        public int MNQZXJOMYWMBOU { get; set; } // Formula:C3H6O3, Mass:90.032, SMILES:O=CC(O)CO
        public int BMRWNKZVCUKKSR { get; set; } // Formula:C4H10O2, Mass:90.068, SMILES:OCC(O)CC
        public int CHCLGECDSSWNCP { get; set; } // Formula:C4H10O2, Mass:90.068, SMILES:O(C)COCC
        public int JDFDHBSESGTDAL { get; set; } // Formula:C4H10O2, Mass:90.068, SMILES:OCCCOC
        public int PUPZLCDOIYMWBV { get; set; } // Formula:C4H10O2, Mass:90.068, SMILES:OCCC(O)C
        public int QWGRWMMWNDWRQN { get; set; } // Formula:C4H10O2, Mass:90.068, SMILES:OCC(C)CO
        public int YTTFFPATQICAQN { get; set; } // Formula:C4H10O2, Mass:90.068, SMILES:OCC(OC)C
        public int ZNQVEEAIQZEUHB { get; set; } // Formula:C4H10O2, Mass:90.068, SMILES:OCCOCC
        public int PEDCQBHIVMGVHV { get; set; } // Formula:C3H8O3, Mass:92.047, SMILES:OCC(O)CO
        public int YXFVVABEGXRONW { get; set; } // Formula:C7H8, Mass:92.063, SMILES:C=1C=CC(=CC=1)C
        public int ITQTTZVARXURQS { get; set; } // Formula:C6H7N, Mass:93.058, SMILES:N=1C=CC=C(C=1)C
        public int PAYRUJLWNCNPSJ { get; set; } // Formula:C6H7N, Mass:93.058, SMILES:NC=1C=CC=CC=1
        public int ISWSIDIOOBJBQZ { get; set; } // Formula:C6H6O, Mass:94.042, SMILES:OC=1C=CC=CC=1
        public int UBQKCCHYAOITMY { get; set; } // Formula:C5H5NO, Mass:95.037, SMILES:O=C1C=CC=CN1
        public int GDIPMASOJOFGKK { get; set; } // Formula:C4H5N3, Mass:95.048, SMILES:N1=CNC(N=C)=C1
        public int FWFSEYBSWVRWGL { get; set; } // Formula:C6H8O, Mass:96.058, SMILES:O=C1C=CCCC1
        public int HQNBJNDMPLEUDS { get; set; } // Formula:C5H8N2, Mass:96.069, SMILES:N=1C=C(N(C=1)C)C
        public int NJQHZENQKNIRSY { get; set; } // Formula:C5H8N2, Mass:96.069, SMILES:N1=CNC(=C1)CC
        public int FMAMSYPJXSEYSW { get; set; } // Formula:C7H12, Mass:96.094, SMILES:C=CCC=CCC
        public int QAOWNCQODCNURD { get; set; } // Formula:H2O4S, Mass:97.967, SMILES:O=S(=O)(O)O
        public int NBIIXXVUZAFLBC { get; set; } // Formula:H3O4P, Mass:97.977, SMILES:O=P(O)(O)O
        public int USLRUYZDOLMIRJ { get; set; } // Formula:C6H10O, Mass:98.073, SMILES:O=CCCCC=C
        public int VAOJQRMYAXTUQI { get; set; } // Formula:C6H10O, Mass:98.073, SMILES:OC(C=CC=C)C
        public int ADHCYQWFCLQBFG { get; set; } // Formula:C7H14, Mass:98.11, SMILES:C=C(CC)C(C)C
        public int QWHNJUXXYKPLQM { get; set; } // Formula:C7H14, Mass:98.11, SMILES:CC1(C)(CCCC1)
        public int UAEPNZWRGJTJPN { get; set; } // Formula:C7H14, Mass:98.11, SMILES:CC1CCCCC1
        public int WZHKDGJSXCTSCK { get; set; } // Formula:C7H14, Mass:98.11, SMILES:C(=CCCC)CC
        public int ZGEGCLOFRBLKSE { get; set; } // Formula:C7H14, Mass:98.11, SMILES:C=CCCCCC
        public int JIDDDPVQQUHACU { get; set; } // Formula:C5H9NO, Mass:99.068, SMILES:O=CC1NCCC1
        public int YLHJZQNYUYVALI { get; set; } // Formula:C4H9N3, Mass:99.08, SMILES:N(C=NCC)=CN
        public int PAMIQIKDUOTOBW { get; set; } // Formula:C6H13N, Mass:99.105, SMILES:N1(C)CCCCC1
        public int PXHHIBMOFPCBJQ { get; set; } // Formula:C6H13N, Mass:99.105, SMILES:N1(C)CCCC1C
        public int BVIJQMCYYASIFP { get; set; } // Formula:C6H12O, Mass:100.089, SMILES:OC1CCCC1(C)
        public int BVOSSZSHBZQJOI { get; set; } // Formula:C6H12O, Mass:100.089, SMILES:OC(C=C)CCC
        public int HPXRVTGHNJAIIH { get; set; } // Formula:C6H12O, Mass:100.089, SMILES:OC1CCCCC1
        public int JARKCYVAAOWBJS { get; set; } // Formula:C6H12O, Mass:100.089, SMILES:O=CCCCCC
        public int UFLHIIWVXFIJGU { get; set; } // Formula:C6H12O, Mass:100.089, SMILES:OCCC=CCC
        public int PVOAHINGSUIXLS { get; set; } // Formula:C5H12N2, Mass:100.1, SMILES:N1CCN(C)CC1
        public int BZHMBWZPUJHVEE { get; set; } // Formula:C7H16, Mass:100.125, SMILES:CC(C)CC(C)C
        public int IMNFDUFMRHMDMM { get; set; } // Formula:C7H16, Mass:100.125, SMILES:C(C)CCCCC
        public int VLJXXKKOSFGPHI { get; set; } // Formula:C7H16, Mass:100.125, SMILES:CCCC(C)CC
        public int DVOFEOSDXAVUJD { get; set; } // Formula:C5H11NO, Mass:101.084, SMILES:O=CC(N)C(C)C
        public int DJEQZVQFEPKLOY { get; set; } // Formula:C6H15N, Mass:101.12, SMILES:CN(C)CCCC
        public int SMBYUOXUISCLCF { get; set; } // Formula:C6H15N, Mass:101.12, SMILES:CN(CC)CCC
        public int VLSTXUUYLIALPB { get; set; } // Formula:C6H15N, Mass:101.12, SMILES:N(CCC)C(C)C
        public int ZMANZCXQSJIPKH { get; set; } // Formula:C6H15N, Mass:101.12, SMILES:N(CC)(CC)CC
        public int FVYNPFNRUNVROH { get; set; } // Formula:C4H6O3, Mass:102.032, SMILES:O=C(OC)C=CO
        public int RMQJECWPWQIIPW { get; set; } // Formula:C4H6O3, Mass:102.032, SMILES:O=C(O)C=CCO
        public int UIUJIQZEACWQSV { get; set; } // Formula:C4H6O3, Mass:102.032, SMILES:O=CCCC(=O)O
        public int CNRGMQRNYAIBTN { get; set; } // Formula:C5H10O2, Mass:102.068, SMILES:OCCCCC=O
        public int KSTOUTVBGBEWOS { get; set; } // Formula:C5H10O2, Mass:102.068, SMILES:OC1COC(C)C1
        public int WLAMNBDJUVNPJU { get; set; } // Formula:C5H10O2, Mass:102.068, SMILES:O=C(O)C(C)CC
        public int NQPDZGIKBAWPEJ { get; set; } // Formula:C5H10O2, Mass:102.068, SMILES:O=C(O)CCCC
        public int WCVPFJVXEXJFLB { get; set; } // Formula:C4H10N2O, Mass:102.079, SMILES:N=C(O)CCCN
        public int ZSIAUFGUXNUGDI { get; set; } // Formula:C6H14O, Mass:102.104, SMILES:OCCCCCC
        public int VHRGRCVQAFMJIZ { get; set; } // Formula:C5H14N2, Mass:102.116, SMILES:NCCCCCN
        public int UGJBHEZMOKVTIM { get; set; } // Formula:C3H5NO3, Mass:103.027, SMILES:O=C(O)CN=CO
        public int WHBMMWSBFZVSSR { get; set; } // Formula:C4H7O3, Mass:103.04, SMILES:O=C([O-])CC(O)C
        public int BTCSSZJGUNDROE { get; set; } // Formula:C4H9NO2, Mass:103.063, SMILES:O=C(O)CCCN
        public int OQEBBZSWEGYTPG { get; set; } // Formula:C4H9NO2, Mass:103.063, SMILES:O=C(O)CC(N)C
        public int ORDNBIQZKXYGTM { get; set; } // Formula:C4H9NO2, Mass:103.063, SMILES:O=CC(N)C(O)C
        public int QWCKQJZIFLGMSD { get; set; } // Formula:C4H9NO2, Mass:103.063, SMILES:O=C(O)C(N)CC
        public int RILLZYSZSDGYGV { get; set; } // Formula:C5H13NO, Mass:103.1, SMILES:OCCNC(C)C
        public int AFENDNXGAFYKQO { get; set; } // Formula:C4H8O3, Mass:104.047, SMILES:O=C(O)C(O)CC
        public int DFFAMJFPVBTTBX { get; set; } // Formula:C4H8O3, Mass:104.047, SMILES:O=CC(O)C(O)C
        public int SJZRECIVHVDYJC { get; set; } // Formula:C4H8O3, Mass:104.047, SMILES:OCCCC(O)=O
        public int CLAHOZSYMRNIPY { get; set; } // Formula:C3H8N2O2, Mass:104.059, SMILES:N=C(O)NCCO
        public int PPBRXRYQALVLMV { get; set; } // Formula:C8H8, Mass:104.063, SMILES:C=CC=1C=CC=CC=1
        public int KLKFAASOGCDTDT { get; set; } // Formula:C5H12O2, Mass:104.084, SMILES:O(COCC)CC
        public int QONBKISCDWCHKF { get; set; } // Formula:C5H12O2, Mass:104.084, SMILES:OCOCC(C)C
        public int SLCVBVWXLSEKPL { get; set; } // Formula:C5H12O2, Mass:104.084, SMILES:OCC(C)(C)CO
        public int WCVRQHFDJLLWFE { get; set; } // Formula:C5H12O2, Mass:104.084, SMILES:OCC(O)CCC
        public int XLMFDCKSFJWJTP { get; set; } // Formula:C5H12O2, Mass:104.084, SMILES:OC(C)C(O)CC
        public int OEYIOHPDSNJKLS { get; set; } // Formula:C5H14NO, Mass:104.107, SMILES:OCC[N+](C)(C)C
        public int KKYSBGWCYXYOHA { get; set; } // Formula:C4H11NS, Mass:105.061, SMILES:NCCCSC
        public int RBNPOMFGQQGHHO { get; set; } // Formula:C3H6O4, Mass:106.027, SMILES:O=C(O)C(O)CO
        public int HUMNYLRZRPPJDN { get; set; } // Formula:C7H6O, Mass:106.042, SMILES:O=CC=1C=CC=CC=1
        public int ARXKVVRQIIOZGF { get; set; } // Formula:C4H10O3, Mass:106.063, SMILES:OCCC(O)CO
        public int MTHSVFCYNBDYFN { get; set; } // Formula:C4H10O3, Mass:106.063, SMILES:OCCOCCO
        public int RKOGJKGQMPZCGG { get; set; } // Formula:C4H10O3, Mass:106.063, SMILES:OCC(OC)CO
        public int YAXKTBLXMTYWDQ { get; set; } // Formula:C4H10O3, Mass:106.063, SMILES:OCC(O)C(O)C
        public int YNQLUTRBYVCPMQ { get; set; } // Formula:C8H10, Mass:106.078, SMILES:C=1C=CC(=CC=1)CC
        public int AFBPFSWMIHJQDM { get; set; } // Formula:C7H9N, Mass:107.073, SMILES:C=1C=CC(=CC=1)NC
        public int RNVCVTLRINQCPJ { get; set; } // Formula:C7H9N, Mass:107.073, SMILES:CC1=CC=CC=C1N
        public int MQVRLONNONYDJP { get; set; } // Formula:C3H8O4, Mass:108.042, SMILES:OCC(O)C(O)O
        public int IWDCLRJOBJJRNH { get; set; } // Formula:C7H8O, Mass:108.058, SMILES:OC=1C=CC(=CC=1)C
        public int RDOXTESZEPMUJZ { get; set; } // Formula:C7H8O, Mass:108.058, SMILES:O(C=1C=CC=CC=1)C
        public int WVDDGKGOMKODPV { get; set; } // Formula:C7H8O, Mass:108.058, SMILES:OCC=1C=CC=CC=1
        public int BUAQSUJYSBQTHA { get; set; } // Formula:C8H12, Mass:108.094, SMILES:C=CCC=CCC=C
        public int PKHBEGZTQNOZLP { get; set; } // Formula:C8H12, Mass:108.094, SMILES:C=CC=CCC=CC
        public int AQSRRZGQRFFFGS { get; set; } // Formula:C6H7NO, Mass:109.053, SMILES:OC=1C=CC=NC=1C
        public int CSNWPSHAZNPDBA { get; set; } // Formula:C7H11N, Mass:109.089, SMILES:C1=CC2N(C1)CCC2
        public int GHMLBKRAJCXXBS { get; set; } // Formula:C6H6O2, Mass:110.037, SMILES:OC=1C=CC=C(O)C=1
        public int MISVBCMQSJUHMH { get; set; } // Formula:C4H6N4, Mass:110.059, SMILES:N=1C=NC(N)=CC=1N
        public int JORKABJLEXAPFV { get; set; } // Formula:C7H10O, Mass:110.073, SMILES:O=C1CCC(C=C)C1
        public int RKSNPTXBQXBXDJ { get; set; } // Formula:C7H10O, Mass:110.073, SMILES:O=C1C=CC(C)CC1
        public int YGDXNKVVWAQSEN { get; set; } // Formula:C7H10O, Mass:110.073, SMILES:OCC=CC=CC=C
        public int NNUKGGJBMPLKIW { get; set; } // Formula:C6H10N2, Mass:110.084, SMILES:N=1C=C(N(C=1)C)CC
        public int GDDAJHJRAKOILH { get; set; } // Formula:C8H14, Mass:110.11, SMILES:C(=CCC=CCC)C
        public int QJXCDRBHYIAFTH { get; set; } // Formula:C8H14, Mass:110.11, SMILES:C=C1CCCCC1C
        public int OPTASPLRGRRNAP { get; set; } // Formula:C4H5N3O, Mass:111.043, SMILES:N=C1N=C(O)NC=C1
        public int XQCZBXHVTFVIFE { get; set; } // Formula:C4H5N3O, Mass:111.043, SMILES:N=C1N=C(O)C=CN1
        public int ZBNZAJFNDPPMDT { get; set; } // Formula:C4H5N3O, Mass:111.043, SMILES:N=C(O)C=1N=CNC=1
        public int NTYJJOPFIAHURM { get; set; } // Formula:C5H9N3, Mass:111.08, SMILES:N1=CNC(=C1)CCN
        public int ISAKRJDGNUQOIC { get; set; } // Formula:C4H4N2O2, Mass:112.027, SMILES:O=C1C=CNC(=O)N1
        public int UDOAKURRCZMWOJ { get; set; } // Formula:C7H12O, Mass:112.089, SMILES:O=CCCCC=CC
        public int KVZJLSYJROEPSQ { get; set; } // Formula:C8H16, Mass:112.125, SMILES:CC1CCCCC1(C)
        public int KWKAKUADMBZCLK { get; set; } // Formula:C8H16, Mass:112.125, SMILES:C=CCCCCCC
        public int LETYIFNDQBJGPJ { get; set; } // Formula:C8H16, Mass:112.125, SMILES:CCC1(C)(CCCC1)
        public int QEGNUYASOUJEHD { get; set; } // Formula:C8H16, Mass:112.125, SMILES:CC1(C)(CCCCC1)
        public int SGVUHPSBDNVHKL { get; set; } // Formula:C8H16, Mass:112.125, SMILES:CC1CCCC(C)C1
        public int YXLCVBVDFKWWRW { get; set; } // Formula:C8H16, Mass:112.125, SMILES:C=C(CC)C(C)CC
        public int HONVBDHDLJUKLX { get; set; } // Formula:C5H7NO2, Mass:113.048, SMILES:O=C1CCC(=O)C1(N)
        public int FEWLNYSYJNLUOO { get; set; } // Formula:C6H11NO, Mass:113.084, SMILES:O=CN1CCCCC1
        public int HRVXPXCISZSDCC { get; set; } // Formula:C6H11NO, Mass:113.084, SMILES:O=CC1CCNCC1
        public int ONQLCPDIXPYJSS { get; set; } // Formula:C7H15N, Mass:113.12, SMILES:N1(C)CCCC(C)C1
        public int YLUDSYGJHAQGOD { get; set; } // Formula:C7H15N, Mass:113.12, SMILES:N1CCCC(C1)CC
        public int SUPCQIBBMFXVTL { get; set; } // Formula:C6H10O2, Mass:114.068, SMILES:O=C(OCC)C(=C)C
        public int VKMRVNVYNKTCBS { get; set; } // Formula:C6H10O2, Mass:114.068, SMILES:O=CC(=COC)CC
        public int VLJNHYLEOZPXFW { get; set; } // Formula:C5H10N2O, Mass:114.079, SMILES:N=C(O)C1NCCC1
        public int CATSNJVOTSVZJV { get; set; } // Formula:C7H14O, Mass:114.104, SMILES:O=C(C)CCCCC
        public int FXHGMKSSBGDXIY { get; set; } // Formula:C7H14O, Mass:114.104, SMILES:O=CCCCCCC
        public int MQWCXKGKQLNYQG { get; set; } // Formula:C7H14O, Mass:114.104, SMILES:OC1CCC(C)CC1
        public int PZKFYTOLVRCMOA { get; set; } // Formula:C7H14O, Mass:114.104, SMILES:OC(C=C)CCCC
        public int JVSWJIKNEAIKJW { get; set; } // Formula:C8H18, Mass:114.141, SMILES:C(C)(C)CCCCC
        public int TVMXDCGIABBOFY { get; set; } // Formula:C8H18, Mass:114.141, SMILES:C(C)CCCCCC
        public int ONIBWKKTOPOVIA { get; set; } // Formula:C5H9NO2, Mass:115.063, SMILES:O=C(O)C1NCCC1
        public int PMQMMDVXYSSGCD { get; set; } // Formula:C5H9NO2, Mass:115.063, SMILES:O=CNCC(=O)CC
        public int QCAAXUQRSHUEHU { get; set; } // Formula:C6H13NO, Mass:115.1, SMILES:O=CC(N)C(C)CC
        public int ZOFRRNUENOHELM { get; set; } // Formula:C6H13NO, Mass:115.1, SMILES:O=CC(N)CC(C)C
        public int CGWBIHLHAGNJCX { get; set; } // Formula:C5H13N3, Mass:115.111, SMILES:N(=C(N)N)CCCC
        public int AUTCCPQKLPMHDN { get; set; } // Formula:C5H8O3, Mass:116.047, SMILES:O=C(OC)C=COC
        public int FVRHIJPPPSYMGI { get; set; } // Formula:C4H8N2O2, Mass:116.059, SMILES:O=CC(N)CC(=O)N
        public int OQAGVSWESNCJJT { get; set; } // Formula:C6H12O2, Mass:116.084, SMILES:O=C(OC)CC(C)C
        public int WDAXFOBOLVPGLV { get; set; } // Formula:C6H12O2, Mass:116.084, SMILES:O=C(OCC)C(C)C
        public int YRMUTQRWVGDUBW { get; set; } // Formula:C6H12O2, Mass:116.084, SMILES:CC(O)CCCC=O
        public int FUZZWVXGSFPDMH { get; set; } // Formula:C6H12O2, Mass:116.084, SMILES:O=C(O)CCCCC
        public int JAEJQOUXXMFTJU { get; set; } // Formula:C5H12N2O, Mass:116.095, SMILES:O=CNCCCCN
        public int SKPCTMWFLGENTL { get; set; } // Formula:C5H12N2O, Mass:116.095, SMILES:O=C(NCC)C(N)C
        public int UZRUKLJLGXLGDM { get; set; } // Formula:C5H12N2O, Mass:116.095, SMILES:N=C(O)C(N)CCC
        public int CETWDUZRCINIHU { get; set; } // Formula:C7H16O, Mass:116.12, SMILES:OC(C)CCCCC
        public int OVOVDHYEOQJKMD { get; set; } // Formula:C7H16O, Mass:116.12, SMILES:OCC(C)CC(C)C
        public int CGJJPOYORMGCGE { get; set; } // Formula:C4H7NO3, Mass:117.043, SMILES:O=CC(N)CC(=O)O
        public int SRBATDDRZARFDZ { get; set; } // Formula:C4H7NO3, Mass:117.043, SMILES:O=C(O)C(N=CO)C
        public int SIKJAQJRHWYJAI { get; set; } // Formula:C8H7N, Mass:117.058, SMILES:C=1C=CC=2NC=CC=2(C=1)
        public int SNDPXSYFESPGGJ { get; set; } // Formula:C5H11NO2, Mass:117.079, SMILES:O=C(O)C(N)CCC
        public int BFSVOASYOCHEOV { get; set; } // Formula:C6H15NO, Mass:117.115, SMILES:OCCN(CC)CC
        public int RNFDZDMIFOFNMC { get; set; } // Formula:C6H15NO, Mass:117.115, SMILES:OC(C)CNC(C)C
        public int KDYFGRWQOYBRFD { get; set; } // Formula:C4H6O4, Mass:118.027, SMILES:O=C(O)CCC(=O)O
        public int KTRCDOAFABEGON { get; set; } // Formula:C5H10O3, Mass:118.063, SMILES:O=CCOCOCC
        public int MMQYUDBFDDEJBP { get; set; } // Formula:C5H10O3, Mass:118.063, SMILES:OC1COCC(O)C1
        public int NSMOSDAEGJTOIQ { get; set; } // Formula:C5H10O3, Mass:118.063, SMILES:OCC1OCCC1(O)
        public int VVDIZWFSGJERMA { get; set; } // Formula:C5H10O3, Mass:118.063, SMILES:OC1COC(C)C1(O)
        public int XIRBXKMCQNAYAT { get; set; } // Formula:C5H10O3, Mass:118.063, SMILES:OC1OC(C)C(O)C1
        public int PHOJOSOUIAQEDH { get; set; } // Formula:C5H10O3, Mass:118.063, SMILES:O=C(O)CCCCO
        public int PZUOEYPTQJILHP { get; set; } // Formula:C4H10N2O2, Mass:118.074, SMILES:N=C(O)C(N)C(O)C
        public int QROGIFZRVHSFLM { get; set; } // Formula:C9H10, Mass:118.078, SMILES:C=1C=CC(=CC=1)C=CC
        public int GTLACDSXYULKMZ { get; set; } // Formula:C2HF5, Mass:120, SMILES:FC(F)C(F)(F)F
        public int BVDRUCCQKHGCRX { get; set; } // Formula:C4H8O4, Mass:120.042, SMILES:O=COCC(O)CO
        public int DZAIOXUZHHTJKN { get; set; } // Formula:C4H8O4, Mass:120.042, SMILES:O=C(O)CC(O)CO
        public int KDCGOANMDULRCW { get; set; } // Formula:C5H4N4, Mass:120.044, SMILES:N=1C=NC=2N=CNC=2(C=1)
        public int FUGYGGDSWSUORM { get; set; } // Formula:C8H8O, Mass:120.058, SMILES:OC=1C=CC(C=C)=CC=1
        public int JESXATFQYMPTNL { get; set; } // Formula:C8H8O, Mass:120.058, SMILES:OC=1C=CC=CC=1(C=C)
        public int KWOLFJPFCHCOCG { get; set; } // Formula:C8H8O, Mass:120.058, SMILES:O=C(C=1C=CC=CC=1)C
        public int VEIIEWOTAHXGKS { get; set; } // Formula:C8H8O, Mass:120.058, SMILES:OC(=C)C1=CC=CC=C1
        public int XLLXMBCBJGATSP { get; set; } // Formula:C8H8O, Mass:120.058, SMILES:OC=CC=1C=CC=CC=1
        public int AALKGALVYCZETF { get; set; } // Formula:C5H12O3, Mass:120.079, SMILES:OCC(O)C(O)CC
        public int BXXXHBWYZKLUGM { get; set; } // Formula:C5H12O3, Mass:120.079, SMILES:OCCOCOCC
        public int GZXFYIDYTCWDDN { get; set; } // Formula:C5H12O3, Mass:120.079, SMILES:OCC(O)C(OC)C
        public int LOSWWGJGSSQDKH { get; set; } // Formula:C5H12O3, Mass:120.079, SMILES:OCC(O)COCC
        public int ODLMAHJVESYWTB { get; set; } // Formula:C9H12, Mass:120.094, SMILES:C=1C=CC(=CC=1)CCC
        public int XUJNEKJLAYXESH { get; set; } // Formula:C3H7NO2S, Mass:121.02, SMILES:O=C(O)C(N)CS
        public int DYDNPESBYVVLBO { get; set; } // Formula:C7H7NO, Mass:121.053, SMILES:O=CNC1=CC=CC=C1
        public int FXWFZIRWWNPPOV { get; set; } // Formula:C7H7NO, Mass:121.053, SMILES:O=CC=1C=CC=CC=1(N)
        public int BHHGXPLMPWCGHP { get; set; } // Formula:C8H11N, Mass:121.089, SMILES:NCCC=1C=CC=CC=1
        public int MLAXEZHEGARMPE { get; set; } // Formula:C8H11N, Mass:121.089, SMILES:N=1C=CC=C(C=1)CCC
        public int RIWRFSMVIUAEBX { get; set; } // Formula:C8H11N, Mass:121.089, SMILES:C=1C=CC(=CC=1)CNC
        public int SMQUZDBALVYZAC { get; set; } // Formula:C7H6O2, Mass:122.037, SMILES:O=CC1=CC=CC=C1(O)
        public int WPYMKLBDIGXBTP { get; set; } // Formula:C7H6O2, Mass:122.037, SMILES:O=C(O)C=1C=CC=CC=1
        public int UNXHWFMMPAWVPI { get; set; } // Formula:C4H10O4, Mass:122.058, SMILES:OCC(O)C(O)CO
        public int CHLICZRVGGXEOD { get; set; } // Formula:C8H10O, Mass:122.073, SMILES:O(C=1C=CC(=CC=1)C)C
        public int DLRJIFUOBPOJNS { get; set; } // Formula:C8H10O, Mass:122.073, SMILES:O(C=1C=CC=CC=1)CC
        public int HMNKTRSOROOSPP { get; set; } // Formula:C8H10O, Mass:122.073, SMILES:OC1=CC=CC(=C1)CC
        public int HXDOZKJGKXYMEW { get; set; } // Formula:C8H10O, Mass:122.073, SMILES:OC1=CC=C(C=C1)CC
        public int WRMNZCZEMHIOCP { get; set; } // Formula:C8H10O, Mass:122.073, SMILES:OCCC=1C=CC=CC=1
        public int NABXNTPHRFMODH { get; set; } // Formula:C9H14, Mass:122.11, SMILES:C=CCC=CCC=CC
        public int DJCJOWDAAZEMCI { get; set; } // Formula:C7H9NO, Mass:123.068, SMILES:OCC=1C=NC(=CC=1)C
        public int NSBIQPJIWUJBBX { get; set; } // Formula:C7H9NO, Mass:123.068, SMILES:O(NC=1C=CC=CC=1)C
        public int ADGSVAHCPADPBZ { get; set; } // Formula:C8H13N, Mass:123.105, SMILES:C1=C(C)C2N(C1)CCC2
        public int JTJIXCMSHWPJJE { get; set; } // Formula:C2H4O4S, Mass:123.983, SMILES:O=CCS(=O)(=O)O
        public int BVJSUAQZOZWCKN { get; set; } // Formula:C7H8O2, Mass:124.052, SMILES:OC1=CC=C(C=C1)CO
        public int LHGVFZTZFXWLCP { get; set; } // Formula:C7H8O2, Mass:124.052, SMILES:OC=1C=CC=CC=1(OC)
        public int OIPPWFOQEKKFEE { get; set; } // Formula:C7H8O2, Mass:124.052, SMILES:OC=1C=C(O)C=C(C=1)C
        public int ZBCATMYQYDCTIZ { get; set; } // Formula:C7H8O2, Mass:124.052, SMILES:OC=1C=CC(=CC=1(O))C
        public int NDFYWJZFESRLBQ { get; set; } // Formula:C8H12O, Mass:124.089, SMILES:O=C1C=C(C)C(C)CC1
        public int ILIUIABCBOXIRX { get; set; } // Formula:C9H16, Mass:124.125, SMILES:C=C1CCCCC1(C)C
        public int NMFOAHIHPAPYJJ { get; set; } // Formula:C9H16, Mass:124.125, SMILES:C(=CCC=CCCC)C
        public int NUHCEMOQRZEGAQ { get; set; } // Formula:C9H16, Mass:124.125, SMILES:C(=C1CCCCC1C)C
        public int ZJXZSIYSNXKHEA { get; set; } // Formula:C2H6O4P, Mass:125.001, SMILES:O=P([O-])(O)OCC
        public int XLRPYZSEQKXZAA { get; set; } // Formula:C8H15N, Mass:125.12, SMILES:N1(C)C2CCCC1CC2
        public int ACXJKDUFAXMPOM { get; set; } // Formula:C2H7O4P, Mass:126.008, SMILES:O=P(O)OCCO
        public int NPDACUSDTOMAMK { get; set; } // Formula:C7H7Cl, Mass:126.024, SMILES:C=1C=C(C=CC=1C)Cl
        public int QCDYQQDYXPDABM { get; set; } // Formula:C6H6O3, Mass:126.032, SMILES:OC=1C=C(O)C=C(O)C=1
        public int RWQNBRDOKXIBIV { get; set; } // Formula:C5H6N2O2, Mass:126.043, SMILES:O=C1N=C(O)C(=CN1)C
        public int NBFKNCBRFJKDDR { get; set; } // Formula:C8H14O, Mass:126.104, SMILES:O=C(C)CCC=CCC
        public int ZUSUVEKHEZURSD { get; set; } // Formula:C8H14O, Mass:126.104, SMILES:O=CCCCC=CCC
        public int IICQZTQZQSBHBY { get; set; } // Formula:C9H18, Mass:126.141, SMILES:C(=CCCCCCC)C
        public int KPADFPAILITQBG { get; set; } // Formula:C9H18, Mass:126.141, SMILES:C(=CCCCC)CCC
        public int PYOLJOJPIPCRDP { get; set; } // Formula:C9H18, Mass:126.141, SMILES:CC1CCCC(C)(C)C1
        public int XARGIVYWQPXRTC { get; set; } // Formula:C9H18, Mass:126.141, SMILES:CCC1CCCCC1(C)
        public int KDISMIMTGUMORD { get; set; } // Formula:C7H13NO, Mass:127.1, SMILES:O=C(N1CCCCC1)C
        public int FMETWOPSCIOJSL { get; set; } // Formula:C8H17N, Mass:127.136, SMILES:N1(C)CCCC(C1)CC
        public int GNXGHNXLUPRSCR { get; set; } // Formula:C8H17N, Mass:127.136, SMILES:N1CC(C)CCC1CC
        public int KPSZWAJWFMFMFF { get; set; } // Formula:C7H12O2, Mass:128.084, SMILES:O=C(O)CCCC=CC
        public int DPBWFNDFMCCGGJ { get; set; } // Formula:C6H12N2O, Mass:128.095, SMILES:O=C(N)C1CCNCC1
        public int LEBVLOLOVGHEFE { get; set; } // Formula:C8H16O, Mass:128.12, SMILES:O=CCC(C)C(C)(C)C
        public int LZHHQGKEJNRBAZ { get; set; } // Formula:C8H16O, Mass:128.12, SMILES:OC(C)CCC=CCC
        public int NUJGJRNETVAIRJ { get; set; } // Formula:C8H16O, Mass:128.12, SMILES:O=CCCCCCCC
        public int OHEFFKYYKJVVOX { get; set; } // Formula:C8H16O, Mass:128.12, SMILES:OC(C)CCC=C(C)C
        public int VSMOENVRRABVKN { get; set; } // Formula:C8H16O, Mass:128.12, SMILES:OC(C=C)CCCCC
        public int VWLITJGXNSANSN { get; set; } // Formula:C8H16O, Mass:128.12, SMILES:O=CCC(C)CC(C)C
        public int ZBAXJUPCYVIBSP { get; set; } // Formula:C8H16O, Mass:128.12, SMILES:OC1CCC(C)C(C)C1
        public int SMWDFEZZVXVKRB { get; set; } // Formula:C9H7N, Mass:129.058, SMILES:N=1C=CC=C2C=CC=CC=12
        public int SRJOCJYGOFTFLH { get; set; } // Formula:C6H11NO2, Mass:129.079, SMILES:O=C(O)C1CCNCC1
        public int JQASYHORSFXWGI { get; set; } // Formula:C7H15NO, Mass:129.115, SMILES:O=C(N)CCCC(C)C
        public int PFBUXLQVTTXUQM { get; set; } // Formula:C7H15NO, Mass:129.115, SMILES:O=C(N)CC(C)CCC
        public int BSTCHFGEIXFVRN { get; set; } // Formula:C6H10O3, Mass:130.063, SMILES:O=C1OC(C)C(O)C1C
        public int XEQYPQHAQCSASZ { get; set; } // Formula:C6H10O3, Mass:130.063, SMILES:O=C(OC)C(=COC)C
        public int YAHJGDKGXVOYKU { get; set; } // Formula:C6H10O3, Mass:130.063, SMILES:O=C(OC)C=COCC
        public int MHPUGCYGQWGLJL { get; set; } // Formula:C7H14O2, Mass:130.099, SMILES:O=C(O)CCCC(C)C
        public int MNWFXJYAOYHMED { get; set; } // Formula:C7H14O2, Mass:130.099, SMILES:CCCCCCC(O)=O
        public int NZQMQVJXSRMTCJ { get; set; } // Formula:C7H14O2, Mass:130.099, SMILES:O=C(O)CC(C)CCC
        public int UKFNNBXGHVJQMJ { get; set; } // Formula:C7H14O2, Mass:130.099, SMILES:CCC(O)CCCC=O
        public int DCDFYDJCNWNFFZ { get; set; } // Formula:C6H14N2O, Mass:130.111, SMILES:O=CNCCCCCN
        public int FORGMRSGVSYZQR { get; set; } // Formula:C6H14N2O, Mass:130.111, SMILES:N=C(O)C(N)CC(C)C
        public int JDAMFKGXSUOWBV { get; set; } // Formula:C6H14N2O, Mass:130.111, SMILES:O=C(N)C(N)C(C)CC
        public int RWIVICVCHVMHMU { get; set; } // Formula:C6H14N2O, Mass:130.111, SMILES:O1CCN(CC1)CCN
        public int YUZOKOFJOOANSW { get; set; } // Formula:C6H14N2O, Mass:130.111, SMILES:O=CC(N)CCCCN
        public int GTFMAONWNTUZEW { get; set; } // Formula:C5H9NO3, Mass:131.058, SMILES:O=C(O)CCCC(=N)O
        public int KABXUUFDPUOJMW { get; set; } // Formula:C5H9NO3, Mass:131.058, SMILES:O=CCCC(N)C(=O)O
        public int KTHDTJVBEPMMGL { get; set; } // Formula:C5H9NO3, Mass:131.058, SMILES:O=C(O)C(N=C(O)C)C
        public int MPUUQNGXJSEWTF { get; set; } // Formula:C5H9NO3, Mass:131.058, SMILES:O=CC(N)CCC(=O)O
        public int QAISXTKWTBOYAA { get; set; } // Formula:C5H9NO3, Mass:131.058, SMILES:O=C(O)C(N=CO)CC
        public int ZFRKQXVRDFCRJG { get; set; } // Formula:C9H9N, Mass:131.073, SMILES:C=1C=CC2=C(C=1)NC=C2C
        public int SLXKOJJOQWFEFD { get; set; } // Formula:C6H13NO2, Mass:131.095, SMILES:O=C(O)CCCCCN
        public int CGDXUTMWWHKMOE { get; set; } // Formula:CH2F2O3S, Mass:131.969, SMILES:O=S(=O)(O)C(F)F
        public int JFCQEDHGNNZCLN { get; set; } // Formula:C5H8O4, Mass:132.042, SMILES:O=C(O)CCCC(=O)O
        public int WXUAQHNMJWJLTG { get; set; } // Formula:C5H8O4, Mass:132.042, SMILES:O=C(O)CC(C(=O)O)C
        public int KJPRLNWUNMBNBZ { get; set; } // Formula:C9H8O, Mass:132.058, SMILES:O=CC=CC=1C=CC=CC=1
        public int ZFBRJUBOJXNIQM { get; set; } // Formula:C9H8O, Mass:132.058, SMILES:O=CC(=C)C=1C=CC=CC=1
        public int UXUFMKFUJJLWKM { get; set; } // Formula:C6H12O3, Mass:132.079, SMILES:OC1CCOC(C)C1(O)
        public int YDCRNMJQROAWFT { get; set; } // Formula:C6H12O3, Mass:132.079, SMILES:CC(O)CCCC(O)=O
        public int AHLPHDHHMVZTML { get; set; } // Formula:C5H12N2O2, Mass:132.09, SMILES:O=C(O)C(N)CCCN
        public int JYGFTBXVXVMTGB { get; set; } // Formula:C8H7NO, Mass:133.053, SMILES:O=C2NC=1C=CC=CC=1C2
        public int PCKPVGOLPKLUHR { get; set; } // Formula:C8H7NO, Mass:133.053, SMILES:OC1=CNC=2C=CC=CC1=2
        public int XAWPKHNOFIWWNZ { get; set; } // Formula:C8H7NO, Mass:133.053, SMILES:OC=1C=CC=2C=CNC=2(C=1)
        public int AMRLMYBWZLIJPE { get; set; } // Formula:C5H11NOS, Mass:133.056, SMILES:O=CC(N)CCSC
        public int CZWARROQQFCFJB { get; set; } // Formula:C5H11NO3, Mass:133.074, SMILES:O=C(O)C(N)CCCO
        public int CQRNPUSDYPNGDN { get; set; } // Formula:C6H15NO2, Mass:133.11, SMILES:OCCOCCNCC
        public int CJXCLBPFKGZXJP { get; set; } // Formula:C5H10O4, Mass:134.058, SMILES:O=C(O)C(O)C(O)CC
        public int HRARQIAGSKMHMV { get; set; } // Formula:C5H10O4, Mass:134.058, SMILES:O=C(O)COCOCC
        public int KZVAAIRBJJYZOW { get; set; } // Formula:C5H10O4, Mass:134.058, SMILES:OCC1OCC(O)C1(O)
        public int MKMRBXQLEMYZOY { get; set; } // Formula:C5H10O4, Mass:134.058, SMILES:OC1OC(C)C(O)C1(O)
        public int MRKRITSFTIHTAQ { get; set; } // Formula:C5H10O4, Mass:134.058, SMILES:O=C(CO)C(O)C(O)C
        public int QXAMTEJJAZOINB { get; set; } // Formula:C5H10O4, Mass:134.058, SMILES:OC1COCC(O)C1(O)
        public int WDRISBUVHBMJEF { get; set; } // Formula:C5H10O4, Mass:134.058, SMILES:O=CC(O)C(O)C(O)C
        public int JAGRUUPXPPLSRX { get; set; } // Formula:C9H10O, Mass:134.073, SMILES:OC=1C=CC(=CC=1)C(=C)C
        public int UAJRSHJHFRVGMG { get; set; } // Formula:C9H10O, Mass:134.073, SMILES:O(C=1C=CC(C=C)=CC=1)C
        public int YGCZTXZTJXYWCO { get; set; } // Formula:C9H10O, Mass:134.073, SMILES:O=CCCC=1C=CC=CC=1
        public int FFFHZYDWPBMWHY { get; set; } // Formula:C4H9NO2S, Mass:135.035, SMILES:O=C(O)C(N)CCS
        public int CGVSVWWXNAFRRH { get; set; } // Formula:C8H7O2, Mass:135.045, SMILES:[O-]C=1C=C(O)C=CC=1(C=C)
        public int GFFGJBXGBJISGV { get; set; } // Formula:C5H5N5, Mass:135.054, SMILES:N=1C=NC(N)=C2NC=NC=12
        public int FZERHIULMFGESH { get; set; } // Formula:C8H9NO, Mass:135.068, SMILES:O=C(NC1=CC=CC=C1)C
        public int SASNBVQSOZSTPD { get; set; } // Formula:C9H13N, Mass:135.105, SMILES:C=1C=CC(=CC=1)CCNC
        public int JPIJQSOTBSSVTP { get; set; } // Formula:C4H8O5, Mass:136.037, SMILES:O=C(O)C(O)C(O)CO
        public int FDGQSTZJBFJUBT { get; set; } // Formula:C5H4N4O, Mass:136.039, SMILES:O=C1N=CNC=2NC=NC1=2
        public int AVHKOIHDSGYXMF { get; set; } // Formula:C8H8O2, Mass:136.052, SMILES:OC=CC1=CC=C(O)C=C1
        public int FBTSUTGMWBDAAC { get; set; } // Formula:C8H8O2, Mass:136.052, SMILES:OC=1C=CC(C=C)=CC=1(O)
        public int GHPODDMCSOYWNE { get; set; } // Formula:C8H8O2, Mass:136.052, SMILES:O1C=2C=CC(=CC=2(OC1))C
        public int JECYUBVRTQDVAT { get; set; } // Formula:C8H8O2, Mass:136.052, SMILES:O=C(C=1C=CC=CC=1(O))C
        public int QPJVMBTYPHYUOC { get; set; } // Formula:C8H8O2, Mass:136.052, SMILES:O=C(OC)C=1C=CC=CC=1
        public int XCQCERPCWZQGIX { get; set; } // Formula:C8H8O2, Mass:136.052, SMILES:OC(=C)C1=CC=C(O)C=C1
        public int FJGNTEKSQVNVTJ { get; set; } // Formula:C5H12O4, Mass:136.074, SMILES:OCC(O)C(O)C(O)C
        public int ZDAWZDFBPUUDAY { get; set; } // Formula:C5H12O4, Mass:136.074, SMILES:OCCC(O)C(O)CO
        public int CTIZSKHLELUAOT { get; set; } // Formula:C9H12O, Mass:136.089, SMILES:O=C1C=CC(C(=C1)CC)C
        public int DWLZULQNIPIABE { get; set; } // Formula:C9H12O, Mass:136.089, SMILES:O(C=1C=CC=C(C=1)CC)C
        public int HDNRAPAFJLXKBV { get; set; } // Formula:C9H12O, Mass:136.089, SMILES:O(C=1C=CC(=CC=1)CC)C
        public int MPWGZBWDLMDIHO { get; set; } // Formula:C9H12O, Mass:136.089, SMILES:OC1=CC=CC(=C1)CCC
        public int ALYNCZNDIQEVRV { get; set; } // Formula:C7H7NO2, Mass:137.048, SMILES:O=C(O)C=1C=CC(N)=CC=1
        public int CDEBRYPYJRWMDC { get; set; } // Formula:C8H11NO, Mass:137.084, SMILES:O=C1C=CC=C(N1)CCC
        public int DZGWFCGJZKJUFP { get; set; } // Formula:C8H11NO, Mass:137.084, SMILES:OC=1C=CC(=CC=1)CCN
        public int IUNJCFABHJZSKB { get; set; } // Formula:C7H6O3, Mass:138.032, SMILES:O=CC=1C=CC(O)=CC=1(O)
        public int JJMZVBNCCUWEDF { get; set; } // Formula:C4H10O5, Mass:138.053, SMILES:OCC(O)C(O)C(O)O
        public int CGTXYEVJPWEPGW { get; set; } // Formula:C5H6N4O, Mass:138.054, SMILES:OC1=NCNC=2NC=NC1=2
        public int ABDKAPXRBAPSQN { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:O(C=1C=CC=CC=1(OC))C
        public int DPZNOMCNRMUKPS { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:O(C=1C=CC=C(OC)C=1)C
        public int HFLGBNBLMBSXEM { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:OC=1C=CC(=CC=1(O))CC
        public int IIGNZLVHOZEOPV { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:OCC=1C=CC=C(OC)C=1
        public int NOTCZLKDULMKBR { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:OC=1C=C(OC)C=C(C=1)C
        public int PETRWTHZSKVLRE { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:OC=1C=CC(=CC=1(OC))C
        public int PMRFBLQVGJNGLU { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:OC1=CC=C(C=C1)C(O)C
        public int VGMJYYDKPUPTID { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:OC=1C=CC(=C(O)C=1)CC
        public int YCCILVSKPBXVIP { get; set; } // Formula:C8H10O2, Mass:138.068, SMILES:OC1=CC=C(C=C1)CCO
        public int KABCBIJVOXYNHG { get; set; } // Formula:C9H14O, Mass:138.104, SMILES:O=C1C=C(CC)C(C)CC1
        public int GGWBGCSGJCRVIQ { get; set; } // Formula:C10H18, Mass:138.141, SMILES:CC12(CCCCC2(CCC1))
        public int NYFHUVYMLLAQHH { get; set; } // Formula:C10H18, Mass:138.141, SMILES:C=C1CCCCC1(C)CC
        public int ZJARKNXLSAIKJE { get; set; } // Formula:C2H5NO4S, Mass:138.994, SMILES:O=S(=O)(O)ON=CC
        public int GMUFGAIZRAMTEN { get; set; } // Formula:C7H9NO2, Mass:139.063, SMILES:OC=1C=C(C=NC=1C)CO
        public int VYOIELONWKIZJS { get; set; } // Formula:C6H9N3O, Mass:139.075, SMILES:O=CC(N)CC=1N=CNC=1
        public int LJPZHJUSICYOIX { get; set; } // Formula:C9H17N, Mass:139.136, SMILES:N12(CCCCC1CCCC2)
        public int AVPYQKSLYISFPO { get; set; } // Formula:C7H5ClO, Mass:140.003, SMILES:O=CC=1C=CC(=CC=1)Cl
        public int BPHYZRNTQNPLFI { get; set; } // Formula:C7H8O3, Mass:140.047, SMILES:OC=1C=C(O)C(=C(O)C=1)C
        public int HDVRLUFGYQYLFJ { get; set; } // Formula:C7H8O3, Mass:140.047, SMILES:OC=1C=C(O)C=C(OC)C=1
        public int PCYGLFXKCBFGPC { get; set; } // Formula:C7H8O3, Mass:140.047, SMILES:OC=1C=CC(=CC=1(O))CO
        public int RXFZCBZCGBDPDT { get; set; } // Formula:C9H16O, Mass:140.12, SMILES:O=C(C)CCC=CCCC
        public int BUOAKQCLRBVIOX { get; set; } // Formula:C10H20, Mass:140.157, SMILES:CC1CCCC(C)(C)C1(C)
        public int HIVFIUIKABOGIO { get; set; } // Formula:C10H20, Mass:140.157, SMILES:CCC1CCCCC1(C)(C)
        public int YLZKVXHQMPWDBJ { get; set; } // Formula:C10H20, Mass:140.157, SMILES:CCC1(C)(CCCC(C)C1)
        public int SUHOOTKUPISOBE { get; set; } // Formula:C2H8NO4P, Mass:141.019, SMILES:O=P(O)(O)OCCN
        public int HWAHQASPJMRWPP { get; set; } // Formula:C6H7NO3, Mass:141.043, SMILES:O=CNC1C(=O)CCC1(=O)
        public int RZMDMVOHBMAFDF { get; set; } // Formula:C7H11NO2, Mass:141.079, SMILES:O=CN1CCC(C=O)CC1
        public int CYHOMWAPJJPNMW { get; set; } // Formula:C8H15NO, Mass:141.115, SMILES:OC2CC1N(C)C(CC1)C2
        public int MZMHDMCGCUWTDS { get; set; } // Formula:C9H19N, Mass:141.152, SMILES:N1(CC)CCCC(C1)CC
        public int PCIBVZXUNDZWRL { get; set; } // Formula:C2H7O5P, Mass:142.003, SMILES:O=P(O)(O)OCCO
        public int RDJUHLUBPADHNP { get; set; } // Formula:C6H6O4, Mass:142.027, SMILES:OC=1C=C(O)C(O)=C(O)C=1
        public int BQOAKIRTXKZTKD { get; set; } // Formula:C9H18O, Mass:142.136, SMILES:OCC(C)CC=CCCC
        public int SOUITUXPBGREAE { get; set; } // Formula:C9H18O, Mass:142.136, SMILES:OC1CCC(C)C(CC)C1
        public int ZIWXVGHUDKNNSH { get; set; } // Formula:C9H18O, Mass:142.136, SMILES:OC(C=CC)CCCCC
        public int GYHFUZHODSMOHU { get; set; } // Formula:C9H18O, Mass:142.136, SMILES:O=CCCCCCCCC
        public int DHDRGOURKDLAOT { get; set; } // Formula:C6H9NO3, Mass:143.058, SMILES:O=CN1CCCC1(C(=O)O)
        public int GCQWVYLQXJQXQL { get; set; } // Formula:C6H13N3O, Mass:143.106, SMILES:O=CCCCCN=C(N)N
        public int IVAHELAXKRVMQR { get; set; } // Formula:C8H17NO, Mass:143.131, SMILES:O=C(N)CC(C)CC(C)C
        public int JLNIGUCDBFYXDC { get; set; } // Formula:C7H12O3, Mass:144.079, SMILES:O=C(OC)C(=COC)CC
        public int XHFLOEDHTDPKPD { get; set; } // Formula:C7H12O3, Mass:144.079, SMILES:O=C1OCC(C)C1(C)CO
        public int QVGWQEOFNGJAOO { get; set; } // Formula:C8H16O2, Mass:144.115, SMILES:CCCC(O)CCCC=O
        public int WLBGHZBIFQNGCH { get; set; } // Formula:C8H16O2, Mass:144.115, SMILES:O=C(O)CC(C)C(C)(C)C
        public int YFBDMUJOFKYAGE { get; set; } // Formula:C8H16O2, Mass:144.115, SMILES:O=C(CC)CCCC(O)C
        public int WWZKQHOCKIZLMA { get; set; } // Formula:C8H16O2, Mass:144.115, SMILES:O=C(O)CCCCCCC
        public int UIKUBYKUYUSRSM { get; set; } // Formula:C7H16N2O, Mass:144.126, SMILES:O1CCN(CC1)CCCN
        public int QBYYLBWFBPAOKU { get; set; } // Formula:C6H11NO3, Mass:145.074, SMILES:O=C(O)C(N=CO)C(C)C
        public int GOVXKUCVZUROAN { get; set; } // Formula:C10H11N, Mass:145.089, SMILES:C=1C=CC2=C(C=1)NC=C2CC
        public int JWNWCEAWZGLYTE { get; set; } // Formula:C7H15NO2, Mass:145.11, SMILES:O=C([O-])C(CC)[N+](C)(C)C
        public int KBUULMSISDOTQC { get; set; } // Formula:C7H15NO2, Mass:145.11, SMILES:O=COCCN(CC)CC
        public int NJRNRSGIUZYMQH { get; set; } // Formula:C9H6O2, Mass:146.037, SMILES:[O-]C1=CC=CC2=[O+]C=CC=C12
        public int OTAFHZMPRISVEM { get; set; } // Formula:C9H6O2, Mass:146.037, SMILES:O=C1C=COC=2C=CC=CC1=2
        public int ZYGHJZDHTFUPRJ { get; set; } // Formula:C9H6O2, Mass:146.037, SMILES:O=C1OC=2C=CC=CC=2(C=C1)
        public int GPDLGPXIXSDAIU { get; set; } // Formula:C6H10O4, Mass:146.058, SMILES:O=C1OC(CO)C(O)C1C
        public int QDHYJVJAGQLHBA { get; set; } // Formula:C5H10N2O3, Mass:146.069, SMILES:O=C(O)CN=C(O)CCN
        public int ZDXPYRJPNDTMRX { get; set; } // Formula:C5H10N2O3, Mass:146.069, SMILES:O=C(O)C(N)CCC(=N)O
        public int RNAODKZCUVVPEN { get; set; } // Formula:C9H10N2, Mass:146.084, SMILES:NCC1=CC=2C=CC=CC=2(N1)
        public int ICKVZOWXZYBERI { get; set; } // Formula:C7H14O3, Mass:146.094, SMILES:CCC(O)CCCC(O)=O
        public int KDXKERNSBIXSRK { get; set; } // Formula:C6H14N2O2, Mass:146.106, SMILES:O=C(O)C(N)CCCCN
        public int VJBCBLLQDMITLJ { get; set; } // Formula:C9H7O2, Mass:147.044, SMILES:OC=1C=CC2=CC=C[O+]=C2(C=1)
        public int WHUUTDBJXJRKMK { get; set; } // Formula:C5H9NO4, Mass:147.053, SMILES:O=C(O)CCC(N)C(=O)O
        public int QJRWYBIKLXNYLF { get; set; } // Formula:C9H9NO, Mass:147.068, SMILES:O(C=1C=CC=2C=CNC=2(C=1))C
        public int CJEQHXGMZPFAQE { get; set; } // Formula:C9H8O2, Mass:148.052, SMILES:O=CC(=C)C1=CC=C(O)C=C1
        public int CJXMVKYNVIGQBS { get; set; } // Formula:C9H8O2, Mass:148.052, SMILES:O=CC=CC=1C=CC(O)=CC=1
        public int MSTDXOZUKAQDRL { get; set; } // Formula:C9H8O2, Mass:148.052, SMILES:O=C1C2=CC=CC=C2(OCC1)
        public int ONLCITAWNHDTRE { get; set; } // Formula:C9H8O2, Mass:148.052, SMILES:O=CC=C(O)C1=CC=CC=C1
        public int BJBURJZEESAQPG { get; set; } // Formula:C6H12O4, Mass:148.074, SMILES:OC1OC(C)CC(O)C1(O)
        public int QFHKFGOUFKUPNX { get; set; } // Formula:C6H12O4, Mass:148.074, SMILES:OCC1OCCC(O)C1(O)
        public int QVNPTEPOWBEITR { get; set; } // Formula:C6H12O4, Mass:148.074, SMILES:OC1CCOC(OC)C1(O)
        public int CKOMXBHMKXXTNW { get; set; } // Formula:C6H7N5, Mass:149.07, SMILES:N=1C=NC(NC)=C2N=CNC=12
        public int COPYQTHBVCHZHI { get; set; } // Formula:C9H11NO, Mass:149.084, SMILES:O=C1C=CC=C2N1CCCC2
        public int CQIUZHAQYHXKRY { get; set; } // Formula:C9H11NO, Mass:149.084, SMILES:O=CC(N)CC=1C=CC=CC=1
        public int SRBFZHDQGSBBOR { get; set; } // Formula:C5H10O5, Mass:150.053, SMILES:OC1OCC(O)C(O)C1(O)
        public int AMQRNDIFPLIQBP { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:O=CCCC=1C=CC=C(O)C=1
        public int ARGITQZGLVBTTI { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:O1C=2C=CC(=CC=2(OC1))CC
        public int DGNQXQJVAILJBW { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:O=CC(C=1C=CC=CC=1)CO
        public int HLOZUROOXAWGSD { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:OC=CC1=CC=C(OC)C=C1
        public int MDXYNRAMDATLBT { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:OC=1C=CC(C=CC)=CC=1(O)
        public int PYWRGHUOBJZVGT { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:OC=1C=CC(=C(O)C=1)C(=C)C
        public int REEQXZCFSBLNDH { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:O=CCCC=1C=CC(O)=CC=1
        public int XMIIGOLPHOKFCH { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:O=C(O)CCC1=CC=CC=C1
        public int YOMSJEATGXXYPX { get; set; } // Formula:C9H10O2, Mass:150.068, SMILES:OC=1C=CC(C=C)=CC=1(OC)
        public int QYZAWZFNVOMDEF { get; set; } // Formula:C6H14O4, Mass:150.089, SMILES:OCC(OCOCC)CO
        public int FOCKPZOFYCTNIA { get; set; } // Formula:C10H14O, Mass:150.104, SMILES:O=C2C=C1CCCCC1CC2
        public int MQSXUKPGWMJYBT { get; set; } // Formula:C10H14O, Mass:150.104, SMILES:OC1=CC=CC(=C1)CCCC
        public int GCLZXEJWFDSXJZ { get; set; } // Formula:C11H18, Mass:150.141, SMILES:C2=C1CCCCC1(C)CCC2
        public int UYTPUPDQBNUYGX { get; set; } // Formula:C5H5N5O, Mass:151.049, SMILES:N=C1N=C(O)C=2N=CNC=2(N1)
        public int SOYIPVHKLPDELX { get; set; } // Formula:C8H9NO2, Mass:151.063, SMILES:O=CC=1C(O)=C(N=CC=1C)C
        public int LRFVTYWOQMYALW { get; set; } // Formula:C5H4N4O2, Mass:152.033, SMILES:O=C1NC(=O)C=2N=CNC=2(N1)
        public int ASROQXGLZMIYNN { get; set; } // Formula:C8H8O3, Mass:152.047, SMILES:OC=C(O)C1=CC=C(O)C=C1
        public int DZJPDDVDKXHRLF { get; set; } // Formula:C8H8O3, Mass:152.047, SMILES:O=CC=1C(O)=CC=CC=1(OC)
        public int OUWKTIRCGDAGJB { get; set; } // Formula:C8H8O3, Mass:152.047, SMILES:OC=CC=1C=CC(O)=C(O)C=1
        public int PCPXULICWBYAAW { get; set; } // Formula:C8H8O3, Mass:152.047, SMILES:OC=1C=C(O)C=C(OC=C)C=1
        public int RGFKONOOOXBVQC { get; set; } // Formula:C8H8O3, Mass:152.047, SMILES:OC(=C)C=1C=CC(O)=C(O)C=1
        public int SULYEHHGGXARJS { get; set; } // Formula:C8H8O3, Mass:152.047, SMILES:O=C(C=1C=CC(O)=CC=1(O))C
        public int ZZMFDMIBZYLXQE { get; set; } // Formula:C8H8O3, Mass:152.047, SMILES:OC=1C=C(C=C)C=C(O)C=1(O)
        public int HEBKCHPVOIAQTA { get; set; } // Formula:C5H12O5, Mass:152.068, SMILES:OCC(O)C(O)C(O)CO
        public int CAWZFQGJUGEKFU { get; set; } // Formula:C9H12O2, Mass:152.084, SMILES:OC=1C=C(C=CC=1(OC))CC
        public int GYPMBQZAVBFUIZ { get; set; } // Formula:C9H12O2, Mass:152.084, SMILES:O(C=1C=CC(=CC=1(OC))C)C
        public int RIZBLVRXRWHLFA { get; set; } // Formula:C9H12O2, Mass:152.084, SMILES:O(C=1C=C(OC)C=C(C=1)C)C
        public int KGBMCONWCCBLAE { get; set; } // Formula:C11H20, Mass:152.157, SMILES:CC1CCCC2(C)(CCCC12)
        public int ZHGAPOLZDJPAJB { get; set; } // Formula:C7H11N3O, Mass:153.09, SMILES:O=CNCCCN1C=NC=C1
        public int YTNVMHWNIJXNGO { get; set; } // Formula:C3H7O5P, Mass:154.003, SMILES:O=C(O)CCOP(=O)O
        public int BTQAJGSMXCDDAJ { get; set; } // Formula:C7H6O4, Mass:154.027, SMILES:O=CC=1C(O)=CC(O)=CC=1(O)
        public int FALWUVSXNUUXQA { get; set; } // Formula:C8H10O3, Mass:154.063, SMILES:OC1=CC(=CC(OC)=C1(O))C
        public int JUUBCHWRXWPFFH { get; set; } // Formula:C8H10O3, Mass:154.063, SMILES:OC=1C=CC(=CC=1(O))CCO
        public int XQDNFAMOIPNVES { get; set; } // Formula:C8H10O3, Mass:154.063, SMILES:OC=1C=C(OC)C=C(OC)C=1
        public int ZENOXNGFMSCLLL { get; set; } // Formula:C8H10O3, Mass:154.063, SMILES:OC=1C=CC(=CC=1(OC))CO
        public int VTXRDCQMLXABHJ { get; set; } // Formula:C11H22, Mass:154.172, SMILES:CCC1(C)(CCCC(C)C1(C))
        public int ATQNWDREMOIFSG { get; set; } // Formula:C6H9N3O2, Mass:155.069, SMILES:N=C1N=C(O)N(C=C1)CCO
        public int HNDVDQJCIGZPNO { get; set; } // Formula:C6H9N3O2, Mass:155.069, SMILES:O=C(O)C(N)CC1=CN=CN1
        public int HJSJELVDQOXCHO { get; set; } // Formula:C8H13NO2, Mass:155.095, SMILES:OCC1=CCN2CCC(O)C12
        public int HYCSHFLKPSMPGO { get; set; } // Formula:C3H9O5P, Mass:156.019, SMILES:O=P(O)(O)OCCCO
        public int VSFDKTSQZYFUOP { get; set; } // Formula:C3H9O5P, Mass:156.019, SMILES:O=P(O)OCC(O)CO
        public int WHRQRNQSZWMIMU { get; set; } // Formula:C3H9O5P, Mass:156.019, SMILES:O=P(O)(OC)OCCO
        public int IIVIBYBQXSHRGE { get; set; } // Formula:C8H12O3, Mass:156.079, SMILES:O=C(OC)C1=COC(C)CC1
        public int ISWZMNIHDVCPSK { get; set; } // Formula:C7H12N2O2, Mass:156.09, SMILES:O=CN1CCC(C(=O)N)CC1
        public int KSMVZQYAVGTKIV { get; set; } // Formula:C10H20O, Mass:156.151, SMILES:CCCCCCCCCC=O
        public int DYHJCQLXZIELGG { get; set; } // Formula:C6H7NO2S, Mass:157.02, SMILES:O=S(=O)C=1C=CC(N)=CC=1
        public int GNMSLDIYJOSUSW { get; set; } // Formula:C7H11NO3, Mass:157.074, SMILES:O=C(O)C1N(C(=O)C)CCC1
        public int IZNTWTMLHCGAJU { get; set; } // Formula:C7H11NO3, Mass:157.074, SMILES:O=CN1CCC(C(=O)O)CC1
        public int VOUIHMBRJVKANW { get; set; } // Formula:C8H15NO2, Mass:157.11, SMILES:O=C(OC)CC1CCNCC1
        public int LZQFCQFMVUXLTE { get; set; } // Formula:C8H14O3, Mass:158.094, SMILES:O=C(OC)C(=COC)C(C)C
        public int NMWHGTRTJQHXGY { get; set; } // Formula:C8H14O3, Mass:158.094, SMILES:O=C(O)CCCC=CCCO
        public int MMVGWKHDLPGACU { get; set; } // Formula:C7H14N2O2, Mass:158.106, SMILES:O=CNCCN1CCOCC1
        public int QJYRUYURLPTHLR { get; set; } // Formula:C6H14N4O, Mass:158.117, SMILES:O=CC(N)CCCN=C(N)N
        public int FBUKVWPVBMHYJY { get; set; } // Formula:C9H18O2, Mass:158.131, SMILES:CCCCCCCCC(O)=O
        public int PBZLKHXTZLJSSO { get; set; } // Formula:C9H18O2, Mass:158.131, SMILES:CCCCC(O)CCCC=O
        public int HFBHOAHFRNLZGN { get; set; } // Formula:C7H13NO3, Mass:159.09, SMILES:O=C(O)C(N=CO)CC(C)C
        public int IONXXIKCTQHZNC { get; set; } // Formula:C7H13NO3, Mass:159.09, SMILES:O=C(O)C(N=CO)C(C)CC
        public int UKUBCVAQGIZRHL { get; set; } // Formula:C6H13N3O2, Mass:159.101, SMILES:O=C(O)CCCCN=C(N)N
        public int FDEOGFDSMRBYGC { get; set; } // Formula:C6H12N2O3, Mass:160.085, SMILES:O=C(O)CN=C(O)CCCN
        public int APJYDQYYACXCRM { get; set; } // Formula:C10H12N2, Mass:160.1, SMILES:NCCC2=CNC=1C=CC=CC=12
        public int RDIZNKNSMGEKKE { get; set; } // Formula:C10H12N2, Mass:160.1, SMILES:C1=CC=C2NC(=CC2(=C1))CNC
        public int LAWNVEFFZXVKAN { get; set; } // Formula:C8H16O3, Mass:160.11, SMILES:CCCC(O)CCCC(O)=O
        public int RMRBBZNNXRPEQH { get; set; } // Formula:C8H16O3, Mass:160.11, SMILES:OC1C(OCCC1(OC)(C))C
        public int JYVGHERHVBFYJB { get; set; } // Formula:C10H11NO, Mass:161.084, SMILES:OC=1C=CC=2NC=C(C=2(C=1))CC
        public int LSMGVUHULSBVAW { get; set; } // Formula:C10H11NO, Mass:161.084, SMILES:O(C=1C=CC2=C(C=1)NC=C2C)C
        public int MBBOMCVGYCRMEA { get; set; } // Formula:C10H11NO, Mass:161.084, SMILES:OCCC2=CNC1=CC=CC=C12
        public int YDNBZEZHOYJIKQ { get; set; } // Formula:C11H15N, Mass:161.12, SMILES:C1=CC=C(C=C1)C2CN(C)CC2
        public int PAXPHUUREDAUGV { get; set; } // Formula:H4O6P2, Mass:161.948, SMILES:O=P(O)OP(=O)(O)O
        public int ORHBXUUXSCNDEV { get; set; } // Formula:C9H6O3, Mass:162.032, SMILES:O=C1OC=2C=C(O)C=CC=2(C=C1)
        public int WVJCRTSTRGRJJT { get; set; } // Formula:C9H6O3, Mass:162.032, SMILES:O=C1C=COC=2C=C(O)C=CC1=2
        public int AXCXHFKZHDEKTP { get; set; } // Formula:C10H10O2, Mass:162.068, SMILES:O=CC=CC1=CC=C(OC)C=C1
        public int JKTXGBOLZMGAIF { get; set; } // Formula:C10H10O2, Mass:162.068, SMILES:O=CC(=C)C=1C=CC(OC)=CC=1
        public int KPNHONAEPLEAJL { get; set; } // Formula:C10H10O2, Mass:162.068, SMILES:O=CC(OC)=CC1=CC=CC=C1
        public int XHYAQFCRAQUBTD { get; set; } // Formula:C10H10O2, Mass:162.068, SMILES:O=CC=CC=1C=CC=C(OC)C=1
        public int QNKOVWCOVLYPKR { get; set; } // Formula:C7H14O4, Mass:162.089, SMILES:OC1CC(OC)OC(C)C1(O)
        public int FSCGLKWYHHSLST { get; set; } // Formula:C5H9NO3S, Mass:163.03, SMILES:O=C(O)CN=C(O)CCS
        public int UKDDNHZSXJUZPY { get; set; } // Formula:C9H7O3, Mass:163.039, SMILES:OC=1C=C(O)C2=CC=C[O+]=C2(C=1)
        public int TVTGZVYLUHVBAJ { get; set; } // Formula:C6H13NO4, Mass:163.084, SMILES:OC1OC(C)C(O)C(O)C1(N)
        public int LWGGUSPGCQNJBN { get; set; } // Formula:C10H13NO, Mass:163.1, SMILES:O=C1C=CC=C2N1CC(C)CC2
        public int AXMVYSVVTMKQSL { get; set; } // Formula:C9H8O3, Mass:164.047, SMILES:O=CC=CC=1C=CC(O)=C(O)C=1
        public int NGSWKAQJJWESNS { get; set; } // Formula:C9H8O3, Mass:164.047, SMILES:O=C(O)C=CC=1C=CC(O)=CC=1
        public int OFLNADCNQAQFEO { get; set; } // Formula:C9H8O3, Mass:164.047, SMILES:O=C1C=2C(O)=CC=CC=2(OCC1)
        public int YBSHFNPTZIZIGY { get; set; } // Formula:C9H8O3, Mass:164.047, SMILES:O=CC=C(O)C1=CC=C(O)C=C1
        public int VBDHNUHBSKNOMC { get; set; } // Formula:C6H12O3S, Mass:164.051, SMILES:OC1COC(CSC)C1(O)
        public int CJJCPDZKQKUXSS { get; set; } // Formula:C6H12O5, Mass:164.068, SMILES:OCC1(O)(OC(C)C(O)C1(O))
        public int MCHWWJLLPNDHGL { get; set; } // Formula:C6H12O5, Mass:164.068, SMILES:OCC1OC(CO)C(O)C1(O)
        public int MPCAJMNYNOGXPB { get; set; } // Formula:C6H12O5, Mass:164.068, SMILES:OCC1OCC(O)C(O)C1(O)
        public int PMMURAAUARKVCB { get; set; } // Formula:C6H12O5, Mass:164.068, SMILES:OCC1OC(O)CC(O)C1(O)
        public int SHZGCJCMOBCMKK { get; set; } // Formula:C6H12O5, Mass:164.068, SMILES:OC1OC(C)C(O)C(O)C1(O)
        public int ZBDGHWFPLXXWRD { get; set; } // Formula:C6H12O5, Mass:164.068, SMILES:OC1COC(OC)C(O)C1(O)
        public int GJPLRURZZCQESP { get; set; } // Formula:C10H12O2, Mass:164.084, SMILES:O1C=2C=C(C(=CC=2(OC1))CC)C
        public int NJXYTXADXSRFTJ { get; set; } // Formula:C10H12O2, Mass:164.084, SMILES:O(C=1C=CC(C=C)=CC=1(OC))C
        public int QANLIVZYWPBKPX { get; set; } // Formula:C10H12O2, Mass:164.084, SMILES:OCCOC=CC1=CC=CC=C1
        public int ZOXCMZXXNOSBHU { get; set; } // Formula:C10H12O2, Mass:164.084, SMILES:O=CCCC1=CC=C(OC)C=C1
        public int OBSIQMZKFXFYLV { get; set; } // Formula:C9H12N2O, Mass:164.095, SMILES:N=C(O)C(N)CC1=CC=CC=C1
        public int OHERZLWVBJCXOF { get; set; } // Formula:C11H16O, Mass:164.12, SMILES:O=C2C=C1CCCCC1(C)CC2
        public int XWASVAMCARPLHN { get; set; } // Formula:C12H20, Mass:164.157, SMILES:C2=C1CCCCC1(C)C(C)CC2
        public int COLNVLDHVKWLRT { get; set; } // Formula:C9H11NO2, Mass:165.079, SMILES:O=C(O)C(N)CC=1C=CC=CC=1
        public int DXGAIOIQACHYRK { get; set; } // Formula:C9H11NO2, Mass:165.079, SMILES:O=CC(N)CC=1C=CC(O)=CC=1
        public int QYFCERPAFOOLBB { get; set; } // Formula:C10H15NO, Mass:165.115, SMILES:O=C1C=CC=C(N1)C(C)CCC
        public int XKCAXTAJNCEDGN { get; set; } // Formula:C2H2F4O2S, Mass:165.971, SMILES:O=S(=O)C(F)(F)C(F)F
        public int GEEXFDOMGJGVHC { get; set; } // Formula:C9H10O3, Mass:166.063, SMILES:OC1=CC(C=C)=CC(OC)=C1(O)
        public int JACRWUWPXAESPB { get; set; } // Formula:C9H10O3, Mass:166.063, SMILES:O=C(O)C(C=1C=CC=CC=1)CO
        public int KSPZRGLDPYUSSZ { get; set; } // Formula:C9H10O3, Mass:166.063, SMILES:O=CCC(O)C1=CC=C(O)C=C1
        public int MGRQLSWDIFMJGV { get; set; } // Formula:C9H10O3, Mass:166.063, SMILES:OC=CC=1C=CC(O)=C(OC)C=1
        public int NMHMNPHRMNGLLB { get; set; } // Formula:C9H10O3, Mass:166.063, SMILES:O=C(O)CCC1=CC=C(O)C=C1
        public int LWWIYCLYWKAKRR { get; set; } // Formula:C6H14O5, Mass:166.084, SMILES:OCCC(O)C(O)C(O)CO
        public int RUIACMUVCHMOMF { get; set; } // Formula:C6H14O5, Mass:166.084, SMILES:OCC(O)CC(O)C(O)CO
        public int SKCKOFZKJLZSFA { get; set; } // Formula:C6H14O5, Mass:166.084, SMILES:OCC(O)C(O)C(O)C(O)C
        public int NEBQMYHKOREVAL { get; set; } // Formula:C10H14O2, Mass:166.099, SMILES:O(C=1C=CC(=CC=1(OC))CC)C
        public int NXCPHYJTOJDSLE { get; set; } // Formula:C12H22, Mass:166.172, SMILES:C=C1CCCC(CC)C1(C)CC
        public int ABNNGIOYMQTQQO { get; set; } // Formula:C8H8O4, Mass:168.042, SMILES:O=CC=1C(O)=CC(O)=CC=1(OC)
        public int DYKBONVDLJHZLM { get; set; } // Formula:C8H8O4, Mass:168.042, SMILES:O=C(C=1C=CC(O)=CC=1(O))CO
        public int JSAAGGWDCXOBNL { get; set; } // Formula:C8H8O4, Mass:168.042, SMILES:OC=COC=1C=C(O)C=C(O)C=1
        public int LTJNCZSXVJCHGE { get; set; } // Formula:C8H8O4, Mass:168.042, SMILES:OC=C(O)C=1C=CC(O)=C(O)C=1
        public int XLEYFDVVXLMULC { get; set; } // Formula:C8H8O4, Mass:168.042, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))C
        public int XNORYOIOVQAIQB { get; set; } // Formula:C4H11NO4P, Mass:168.043, SMILES:O=P([O-])(O)OCC[N+](C)C
        public int AIFRHYZBTHREPW { get; set; } // Formula:C11H8N2, Mass:168.069, SMILES:N=1C=CC3=C(C=1)NC2=CC=CC=C23
        public int AGIQIOSHSMJYJP { get; set; } // Formula:C9H12O3, Mass:168.079, SMILES:O(C=1C=CC(OC)=C(OC)C=1)C
        public int CRUILBNAQILVHZ { get; set; } // Formula:C9H12O3, Mass:168.079, SMILES:O(C=1C=CC=C(OC)C=1(OC))C
        public int DWGPKVSAWGQTPQ { get; set; } // Formula:C9H12O3, Mass:168.079, SMILES:OC=1C=CC(=CC=1(O))CC(O)C
        public int CZZYITDELCSZES { get; set; } // Formula:C13H12, Mass:168.094, SMILES:C=1C=CC(=CC=1)CC=2C=CC=CC=2
        public int IRXJZDLOCNJAHE { get; set; } // Formula:C12H24, Mass:168.188, SMILES:CCC1CCCC(C)C1(C)(CC)
        public int QPEHZWXCQFKJQO { get; set; } // Formula:C12H24, Mass:168.188, SMILES:CC1CCC(C(C)C)C(C)(C)C1
        public int KQUUQYKFKUSHHJ { get; set; } // Formula:C3H7NO3S2, Mass:168.987, SMILES:O=S(=O)ON=C(C)SC
        public int CHCUDBFRYVCNDF { get; set; } // Formula:C9H15NO2, Mass:169.11, SMILES:O=COC2CC1N(C)C(CC1)C2
        public int UKACHOXRXFQJFN { get; set; } // Formula:C3HF7, Mass:169.997, SMILES:FC(F)C(F)(F)C(F)(F)F
        public int KUSIZNPTWCYZMX { get; set; } // Formula:C7H6O5, Mass:170.022, SMILES:O=CC=1C(O)=CC(O)=C(O)C=1(O)
        public int DFOATKRXQLALOL { get; set; } // Formula:C8H10O4, Mass:170.058, SMILES:OC=1C=C(O)C(=C(O)C=1)CCO
        public int QHHFGWJKHBSQCD { get; set; } // Formula:C8H10O4, Mass:170.058, SMILES:OC=1C=C(O)C=C(OCCO)C=1
        public int VKHCWIJJBRBDLT { get; set; } // Formula:C9H14O3, Mass:170.094, SMILES:O=C(OC)C1=COC(C)CC1C
        public int NWZYRRPOEASODK { get; set; } // Formula:C8H14N2O2, Mass:170.106, SMILES:O=C(N)C1CCN(C(=O)C)CC1
        public int KMPQYAYAQWNLME { get; set; } // Formula:C11H22O, Mass:170.167, SMILES:CCCCCCCCCCC=O
        public int IHJYCNWBPOTSGA { get; set; } // Formula:C6H9N3O3, Mass:171.064, SMILES:N=C1N=C(O)N(C=C1)C(O)CO
        public int WFCLWJHOKCQYOQ { get; set; } // Formula:C8H13NO3, Mass:171.09, SMILES:O=C(O)C1CCN(C(=O)C)CC1
        public int JLSURYXXEOGGEU { get; set; } // Formula:C7H12N2O3, Mass:172.085, SMILES:O=C(C(=CO)C(=N)O)CN(C)C
        public int CFTOTSJVQRFXOF { get; set; } // Formula:C11H12N2, Mass:172.1, SMILES:C1=CC=C2C(=C1)NC3=C2CCNC3
        public int CVFFFSPGTZUPTF { get; set; } // Formula:C9H16O3, Mass:172.11, SMILES:O=C(OC)C(=COC)CCCC
        public int HYHMOIFJBNVOTK { get; set; } // Formula:C9H16O3, Mass:172.11, SMILES:O=COC(C)CCCC(=O)CC
        public int PMLVVKMWRUMPQM { get; set; } // Formula:C8H16N2O2, Mass:172.121, SMILES:O=CNCCCN1CCOCC1
        public int AASQBYVPOFXFGU { get; set; } // Formula:C10H20O2, Mass:172.146, SMILES:CCCCCC(O)CCCC=O
        public int GHVNFZFCNZKVNT { get; set; } // Formula:C10H20O2, Mass:172.146, SMILES:CCCCCCCCCC(O)=O
        public int OBNFCFYSFICKKX { get; set; } // Formula:C11H11NO, Mass:173.084, SMILES:O=CCCC2=CNC=1C=CC=CC=12
        public int ZOAMBXDOGPRZLP { get; set; } // Formula:C10H10N2O, Mass:174.079, SMILES:N=C(O)CC2=CNC1=CC=CC=C12
        public int LIEIRDDOGFQXEH { get; set; } // Formula:C7H14N2O3, Mass:174.1, SMILES:O=C(O)C(N=CO)CCCCN
        public int ODKSFYDXXFIFQN { get; set; } // Formula:C6H14N4O2, Mass:174.112, SMILES:O=C(O)C(N)CCCN=C(N)N
        public int HBBCKARIXVLLRZ { get; set; } // Formula:C11H14N2, Mass:174.116, SMILES:C1=CC=C2NC(=CC2(=C1))CN(C)C
        public int NCIKQJBVUNUXLW { get; set; } // Formula:C11H14N2, Mass:174.116, SMILES:C=1C=CC2=C(C=1)NC=C2CCNC
        public int XYXANQCOUCDVRX { get; set; } // Formula:C9H18O3, Mass:174.126, SMILES:CCCCC(O)CCCC(O)=O
        public int ADZLWSMFHHHOBV { get; set; } // Formula:C6H9NO5, Mass:175.048, SMILES:O=C(O)CCC(N=CO)C(=O)O
        public int CAAUHCJBMYDKBU { get; set; } // Formula:C11H13NO, Mass:175.1, SMILES:O(C=1C=CC2=C(C=1)NC=C2CC)C
        public int DKUVJAGWIDKYRA { get; set; } // Formula:C5H9N2O3P, Mass:176.035, SMILES:O=P(O)C(O)CN1C=NC=C1
        public int XSFUUZAFOVXTLP { get; set; } // Formula:C10H9O3, Mass:177.055, SMILES:OC=1C=CC2=CC(OC)=C[O+]=C2(C=1)
        public int DGSAGELMAWVXIF { get; set; } // Formula:C11H15NO, Mass:177.115, SMILES:O=C1C=CC=C2N1CC(C)CC2C
        public int FXCIEQJPMSJEII { get; set; } // Formula:C11H15NO, Mass:177.115, SMILES:O(C=1C=CC2=C(C=1)CCN(C)C2)C
        public int XPPKVPWEQAFLFU { get; set; } // Formula:H4O7P2, Mass:177.943, SMILES:O=P(O)(O)OP(=O)(O)O
        public int ILEDWLMCKZNDJK { get; set; } // Formula:C9H6O4, Mass:178.027, SMILES:O=C1OC2=CC(O)=C(O)C=C2(C=C1)
        public int NYCXYKOXLNBYID { get; set; } // Formula:C9H6O4, Mass:178.027, SMILES:O=C1C=COC2=CC(O)=CC(O)=C12
        public int JMZFHBIADDYOTC { get; set; } // Formula:C6H10O6, Mass:178.048, SMILES:O=C(O)C1OC(O)CC(O)C1(O)
        public int DISYDHABSCTQFK { get; set; } // Formula:C10H10O3, Mass:178.063, SMILES:O=C1C=2C=CC(OC)=CC=2(OCC1)
        public int YODAPCPITHANRU { get; set; } // Formula:C10H10O3, Mass:178.063, SMILES:O=CC(OC)=CC1=CC=C(O)C=C1
        public int OHWCAVRRXKJCRB { get; set; } // Formula:C7H14O5, Mass:178.084, SMILES:OC1C(OC)OC(C)C(O)C1(O)
        public int UBUITGRMYNAZAX { get; set; } // Formula:C7H14O5, Mass:178.084, SMILES:OC1COC(OCC)C(O)C1(O)
        public int NUQYJMDNHVQOME { get; set; } // Formula:C13H22, Mass:178.172, SMILES:C2=C1CCCCC1(C)C(CC)CC2
        public int UNWNSYMIUBZKCZ { get; set; } // Formula:C13H22, Mass:178.172, SMILES:C=C1CCC2CC(C)CCC2(C1C)
        public int TYYUTXYNKKBNFV { get; set; } // Formula:C9H7O4, Mass:179.034, SMILES:OC1=C[O+]=C2C=C(O)C=C(O)C2(=C1)
        public int NDDUBDUPYJZIAK { get; set; } // Formula:C9H8O4, Mass:180.042, SMILES:O=C1C=2C(O)=CC(O)=CC=2(OCC1)
        public int QAIPRVGONGVQAS { get; set; } // Formula:C9H8O4, Mass:180.042, SMILES:O=C(O)C=CC=1C=CC(O)=C(O)C=1
        public int PATWQGKLOAFEDC { get; set; } // Formula:C6H12O6, Mass:180.063, SMILES:O=CCOC(O)C(O)C(O)CO
        public int RFSUNEUAIZKAJO { get; set; } // Formula:C6H12O6, Mass:180.063, SMILES:OCC1OC(O)(CO)C(O)C1(O)
        public int WQZGKKKJIJFFOK { get; set; } // Formula:C6H12O6, Mass:180.063, SMILES:OCC1OC(O)C(O)C(O)C1(O)
        public int JUOCEVARIIPUNO { get; set; } // Formula:C10H12O3, Mass:180.079, SMILES:OC1=CC=C(C=COCCO)C=C1
        public int PZPQXEDPDKLPRN { get; set; } // Formula:C10H12O3, Mass:180.079, SMILES:OC=1C=CC(=CC=1(O))C=C(OC)C
        public int QYOYBHCGTRXATB { get; set; } // Formula:C10H12O3, Mass:180.079, SMILES:O=CCCC=1C=CC(OC)=C(O)C=1
        public int PQFMNVGMJJMLAE { get; set; } // Formula:C9H12N2O2, Mass:180.09, SMILES:N=C(O)C(N)CC1=CC=C(O)C=C1
        public int PJANXHGTPQOBST { get; set; } // Formula:C14H12, Mass:180.094, SMILES:C=1C=CC(=CC=1)C=CC=2C=CC=CC=2
        public int XBENKLBOLWZLNQ { get; set; } // Formula:C13H24, Mass:180.188, SMILES:CC2CCC1C(CCCC1(C)(C))C2
        public int KZWJWYFPLXRYIL { get; set; } // Formula:C2H2F4O3S, Mass:181.966, SMILES:O=S(=O)(O)C(F)(F)C(F)F
        public int IZKQBJLVIODBSO { get; set; } // Formula:C5H11O5P, Mass:182.034, SMILES:O=P(O)OCC1OCCC1(O)
        public int CNPKCQMGPCOELU { get; set; } // Formula:C9H10O4, Mass:182.058, SMILES:O=CCC(O)C=1C=CC(O)=C(O)C=1
        public int DZAUWHJDUNRCTF { get; set; } // Formula:C9H10O4, Mass:182.058, SMILES:O=C(O)CCC=1C=CC(O)=C(O)C=1
        public int FQRQWPNYJOFDLO { get; set; } // Formula:C9H10O4, Mass:182.058, SMILES:O=CC=1C(O)=CC(OC)=CC=1(OC)
        public int GUBPZPIZJGZPPT { get; set; } // Formula:C9H10O4, Mass:182.058, SMILES:OC2=CC(O)=C1C(OCC(O)C1)=C2
        public int NAKWZRBQGKLMCP { get; set; } // Formula:C9H10O4, Mass:182.058, SMILES:O=CC=1C=CC(OCCO)=CC=1(O)
        public int KCIZTNZGSBSSRM { get; set; } // Formula:C10H14O3, Mass:182.094, SMILES:O(C=1C=C(C=C(OC)C=1(OC))C)C
        public int MWOMNLDJNQWJMK { get; set; } // Formula:C10H14O3, Mass:182.094, SMILES:OC=1C=CC(=CC=1(OC))CCCO
        public int OTBOAOSTJYUVHP { get; set; } // Formula:C13H26, Mass:182.203, SMILES:CC(C)C1CCC(C)C(C)C1(C)(C)
        public int YRZSSOHWUWGVCZ { get; set; } // Formula:C7H9N3O3, Mass:183.064, SMILES:OC=NC=1N=C(OC)C=C(N=1)OC
        public int YHHSONZFOIEMCP { get; set; } // Formula:C5H14NO4P, Mass:183.066, SMILES:O=P([O-])(O)OCC[N+](C)(C)C
        public int NPZQKSBUUPRTOB { get; set; } // Formula:C9H13NO3, Mass:183.09, SMILES:O=COCC1=CCN2CCC(O)C12
        public int HPPCGHJRDOZHQY { get; set; } // Formula:C8H8O5, Mass:184.037, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))CO
        public int OCINWLFNVFQQLR { get; set; } // Formula:C8H8O5, Mass:184.037, SMILES:OC=C(O)C1=CC(O)=C(O)C(O)=C1
        public int POUBACQTFWJJGJ { get; set; } // Formula:C8H8O5, Mass:184.037, SMILES:O=CC=1C(O)=CC(OCO)=CC=1(O)
        public int BOTNYLSAWDQNEX { get; set; } // Formula:C13H12O, Mass:184.089, SMILES:O(C=1C=CC=CC=1)CC2=CC=CC=C2
        public int HFJRKMMYBMWEAD { get; set; } // Formula:C12H24O, Mass:184.183, SMILES:O=CCCCCCCCCCCC
        public int BZQFBWGGLXLEPQ { get; set; } // Formula:C3H8NO6P, Mass:185.009, SMILES:O=C(O)C(N)COP(=O)(O)O
        public int LTTDCEYZEHQDGW { get; set; } // Formula:C7H11N3O3, Mass:185.08, SMILES:N=C1N=C(O)N(C=C1)CC(O)CO
        public int XMPWCYNJUJLWKF { get; set; } // Formula:C8H14N2O3, Mass:186.1, SMILES:O=C(N)C1CCN(C(=O)CO)CC1
        public int JOFKCNJIUXPJAC { get; set; } // Formula:C12H14N2, Mass:186.116, SMILES:C1=CC=C2C(=C1)NC3=C2CCN(C)C3
        public int TYXOEOVOUIVPQE { get; set; } // Formula:C10H18O3, Mass:186.126, SMILES:O=C(OC)C(=COC)C(C)CCC
        public int IOKPZOGAWQXXLF { get; set; } // Formula:C11H22O2, Mass:186.162, SMILES:CCCCCCC(O)CCCC=O
        public int ZDPHROOEEOARMN { get; set; } // Formula:C11H22O2, Mass:186.162, SMILES:CCCCCCCCCCC(O)=O
        public int PQMCFIPTFAVVSI { get; set; } // Formula:C8H13NO4, Mass:187.084, SMILES:O=C(O)C1CCN(C(=O)CO)CC1
        public int JPBDMIWPTFDFEU { get; set; } // Formula:C6H5BrO2, Mass:187.947, SMILES:OC=1C=CC=C(C=1(O))Br
        public int LULCPJWUGUVEFU { get; set; } // Formula:C11H8O3, Mass:188.047, SMILES:O=C1C(O)=C(C(=O)C2=CC=CC=C12)C
        public int SOPHXQXKBAXPAL { get; set; } // Formula:C11H12N2O, Mass:188.095, SMILES:O=CC(N)CC2=CNC=1C=CC=CC=12
        public int LMHJFKYQYDSOQO { get; set; } // Formula:C10H20O3, Mass:188.141, SMILES:CCCCCC(O)CCCC(O)=O
        public int GOLXRNDWAUTYKT { get; set; } // Formula:C11H11NO2, Mass:189.079, SMILES:O=C(O)CCC2=CNC=1C=CC=CC=12
        public int QUWFMGIQXDVKNA { get; set; } // Formula:C8H14O5, Mass:190.084, SMILES:OC1C(OC=C)OC(C)C(O)C1(O)
        public int ANJTVLIZGCUXLD { get; set; } // Formula:C11H14N2O, Mass:190.111, SMILES:O=C1C=CC=C2N1CC3CNCC2C3
        public int XNZFFRJWTVYMBF { get; set; } // Formula:C11H13NO2, Mass:191.095, SMILES:O1C=2C=C3C(=CC=2(OC1))CCN(C)C3
        public int BBTITIVJFGSHNE { get; set; } // Formula:C5H9N2O4P, Mass:192.03, SMILES:O=P(O)(O)C(O)CN1C=NC=C1
        public int LRCHDYPFGNFWLO { get; set; } // Formula:C10H8O4, Mass:192.042, SMILES:O=C1C(OC)=COC2=CC=CC(O)=C12
        public int MZWSHEJWANSXEH { get; set; } // Formula:C7H12O6, Mass:192.063, SMILES:O=CCOC1OCC(O)C(O)C1(O)
        public int FUTPFHGTGHIWFV { get; set; } // Formula:C11H12O3, Mass:192.079, SMILES:O=CC(OCCO)=CC1=CC=CC=C1
        public int KNUFNLWDGZQKKJ { get; set; } // Formula:C11H12O3, Mass:192.079, SMILES:O=CC=CC=1C=CC(OC)=C(OC)C=1
        public int HRWQYCOJZMKBGI { get; set; } // Formula:C8H16O5, Mass:192.1, SMILES:OC1C(OCC)OC(C)C(O)C1(O)
        public int SCBVMAGDJBWWEM { get; set; } // Formula:C10H9O4, Mass:193.05, SMILES:OC=1C=C(O)C2=CC(OC)=C[O+]=C2(C=1)
        public int NSTPXGARCQOSAU { get; set; } // Formula:C10H11NO3, Mass:193.074, SMILES:O=C(O)C(N=CO)CC1=CC=CC=C1
        public int OZNMEZAXFKUCPN { get; set; } // Formula:C9H6O5, Mass:194.022, SMILES:O=C1C(O)=COC2=CC(O)=CC(O)=C12
        public int AEMOLEFTQBMNLQ { get; set; } // Formula:C6H10O7, Mass:194.043, SMILES:O=C(O)C1OC(O)C(O)C(O)C1(O)
        public int ORFFGRQMMWVHIB { get; set; } // Formula:C10H10O4, Mass:194.058, SMILES:O=C1OCC=2C=CC(OC)=C(OC)C1=2
        public int WDIFRGCUOJVZLO { get; set; } // Formula:C10H10O4, Mass:194.058, SMILES:O=C1C2=C(O)C=C(OC)C=C2(OCC1)
        public int DCZHIIAMPOQNQY { get; set; } // Formula:C7H14O6, Mass:194.079, SMILES:OCCOC1OCC(O)C(O)C1(O)
        public int HOVAGTYPODGVJG { get; set; } // Formula:C7H14O6, Mass:194.079, SMILES:OCC1OC(OC)C(O)C(O)C1(O)
        public int AYKRZZCBPHNROJ { get; set; } // Formula:C11H14O3, Mass:194.094, SMILES:OC=1C=CC(=CC=1(OC))C=C(OC)C
        public int FMQIYCZYTNRLES { get; set; } // Formula:C11H14O3, Mass:194.094, SMILES:OC1=CC=C(C=COCOCC)C=C1
        public int YXNZWUPLVHBMCC { get; set; } // Formula:C11H14O3, Mass:194.094, SMILES:O(C=1C=C2OCCCC2(=CC=1(OC)))C
        public int NWYBPFPJJYBYSR { get; set; } // Formula:C11H17NO2, Mass:195.126, SMILES:O(C=1C=CC(=CC=1(OC))CN(C)C)C
        public int AZZVQQAZMSIAKQ { get; set; } // Formula:C5H9O6P, Mass:196.014, SMILES:O=P1(O)(OCC2OCC(O)C2(O1))
        public int CVYMCVWHEXKUCO { get; set; } // Formula:C9H8O5, Mass:196.037, SMILES:O=CC(O)=C(O)C=1C=CC(O)=C(O)C=1
        public int JUSMHIGDXPKSID { get; set; } // Formula:C6H12O5S, Mass:196.041, SMILES:OCC1OC(C(O)C(O)C1(O))S
        public int VNGRIQMEDUZTCJ { get; set; } // Formula:C13H24O, Mass:196.183, SMILES:CCC\C=C/CCCCCCCC=O
        public int MCMFWKPSXYARAC { get; set; } // Formula:C9H5F2NO2, Mass:197.029, SMILES:O=C1C=CNC2=C(O)C(F)=C(F)C=C12
        public int BVOBPNSQIRMLCA { get; set; } // Formula:C5H11O6P, Mass:198.029, SMILES:O=P(O)(O)OCC1OCCC1(O)
        public int FYRXXSCCOWRCLX { get; set; } // Formula:C9H10O5, Mass:198.053, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))COC
        public int CJMBZJYOZMJNEQ { get; set; } // Formula:C13H10O2, Mass:198.068, SMILES:O=C1C=C(C(=O)C2=CC=CC=C12)CC=C
        public int BGEHHAVMRVXCGR { get; set; } // Formula:C13H26O, Mass:198.198, SMILES:CCCCCCCCCCCCC=O
        public int ACOJOKPGGCIXCN { get; set; } // Formula:C5H14NO5P, Mass:199.061, SMILES:O=P(O)(OCCN)OCC(O)C
        public int OHLUOZVYBDODSP { get; set; } // Formula:C5H14NO5P, Mass:199.061, SMILES:O=P(O)(OCCN)OCCCO
        public int BUWCCEMQRIMVKX { get; set; } // Formula:C9H12O5, Mass:200.068, SMILES:OC1=CC=CC(OC(O)C(O)CO)=C1
        public int DSOFUIOMIHBCLC { get; set; } // Formula:C9H12O5, Mass:200.068, SMILES:OC1=CC(O)=C(C(O)=C1)COCCO
        public int VMUAZGMTADMRDK { get; set; } // Formula:C9H12O5, Mass:200.068, SMILES:OC=1C=C(O)C=C(OCOCCO)C=1
        public int FOTVZLOJAIEAOY { get; set; } // Formula:C13H12O2, Mass:200.084, SMILES:OC=2C=CC=C(OCC1=CC=CC=C1)C=2
        public int PSBWSIWOVNCTKX { get; set; } // Formula:C13H12O2, Mass:200.084, SMILES:OC1=CC=C(C=C1)COC2=CC=CC=C2
        public int VYQNWZOUAUKGHI { get; set; } // Formula:C13H12O2, Mass:200.084, SMILES:OC=2C=CC(OCC1=CC=CC=C1)=CC=2
        public int VENAANZHUCMOGD { get; set; } // Formula:C13H16N2, Mass:200.131, SMILES:C1=CC=C2C(=C1)NC3=C2CCN(C)C3C
        public int NGBPDDMTRGHYFM { get; set; } // Formula:C12H24O2, Mass:200.178, SMILES:CCCCCCCC(O)CCCC=O
        public int POULHZVOKOAJMA { get; set; } // Formula:C12H24O2, Mass:200.178, SMILES:CCCCCCCCCCCC(O)=O
        public int QRDCEYBRRFPBMZ { get; set; } // Formula:C4H11O7P, Mass:202.024, SMILES:O=P(O)(O)OCC(O)C(O)CO
        public int MBDCBOQSTZRKJP { get; set; } // Formula:C7H14N4O3, Mass:202.107, SMILES:O=C(O)C(N=CO)CCCNC(=N)N
        public int KTPMCFLPGSCUKH { get; set; } // Formula:C11H22O3, Mass:202.157, SMILES:CCCCCCC(O)CCCC(O)=O
        public int GZKWULQSPPFVMV { get; set; } // Formula:C6H5BrO3, Mass:203.942, SMILES:OC=1C=C(C=C(O)C=1(O))Br
        public int VCYYRDKGHLOTQU { get; set; } // Formula:C8H15NO5, Mass:205.095, SMILES:OC(=NC1COC(CO)C(O)C1(O))C
        public int QPDOOAOPIHRGIC { get; set; } // Formula:C8H14O6, Mass:206.079, SMILES:O=CCOC1OC(C)C(O)C(O)C1(O)
        public int RSDMPVNLUKICCT { get; set; } // Formula:C8H14O6, Mass:206.079, SMILES:OCC1OC(OC=C)C(O)C(O)C1(O)
        public int FSMINEQXZDUUTP { get; set; } // Formula:C13H18O2, Mass:206.131, SMILES:O=C1C=C(OC1(C)C)C(=CCCC=C)C
        public int DKCPKDPYUFEZCP { get; set; } // Formula:C14H22O, Mass:206.167, SMILES:OC1=C(C=CC=C1C(C)(C)C)C(C)(C)C
        public int FSSKXIOSLFOPBN { get; set; } // Formula:C11H11O4, Mass:207.065, SMILES:OC=1C=CC2=CC(OCCO)=C[O+]=C2(C=1)
        public int TXPPKWZEHFNZOE { get; set; } // Formula:C12H17NO2, Mass:207.126, SMILES:O(C=1C=C2C(=CC=1(OC))CCN(C)C2)C
        public int HXDPHXHZEUZQRO { get; set; } // Formula:C10H8O5, Mass:208.037, SMILES:O=C1C(OC)=COC2=CC(O)=CC(O)=C12
        public int CDICDSOGTRCHMG { get; set; } // Formula:C11H12O4, Mass:208.074, SMILES:O=CC=CC=1C=C(OC)C(O)=C(OC)C=1
        public int KGKCIJKXFXINOM { get; set; } // Formula:C11H12O4, Mass:208.074, SMILES:O=CC(OCCO)=CC1=CC=C(O)C=C1
        public int SXGTVXYSXBZEBN { get; set; } // Formula:C11H12O4, Mass:208.074, SMILES:O=C1C2=CC=C(OCCO)C=C2(OCC1)
        public int QTIFSOVGBSROEQ { get; set; } // Formula:C8H16O6, Mass:208.095, SMILES:OCCCOC1OCC(O)C(O)C1(O)
        public int WEKZOSZDHREWIU { get; set; } // Formula:C8H16O6, Mass:208.095, SMILES:OCCOC1OC(C)C(O)C(O)C1(O)
        public int WYUFTYLVLQZQNH { get; set; } // Formula:C8H16O6, Mass:208.095, SMILES:OCC1OC(OCC)C(O)C(O)C1(O)
        public int DUKLXSCPUYTGMX { get; set; } // Formula:C12H16O3, Mass:208.11, SMILES:OCCOC(=CC=1C=CC=C(OC)C=1)C
        public int ROUWPHMRHBMAFE { get; set; } // Formula:C10H11NO4, Mass:209.069, SMILES:O=C(O)C(N=CO)CC1=CC=C(O)C=C1
        public int PURVHZPPXMOOSB { get; set; } // Formula:C10H10O5, Mass:210.053, SMILES:O=C1C2=C(O)C=C(OCO)C=C2(OCC1)
        public int FRMCCLBYAFUKSG { get; set; } // Formula:C7H14O7, Mass:210.074, SMILES:OCOC1OC(CO)C(O)C(O)C1(O)
        public int IHOWYPOFBITTRU { get; set; } // Formula:C11H14O4, Mass:210.089, SMILES:OC1=CC=C(C=COCC(O)CO)C=C1
        public int VQJVACMKXDMZFT { get; set; } // Formula:C11H14O4, Mass:210.089, SMILES:O=C(C=1C=CC(OCOCC)=CC=1(O))C
        public int ANJAOCICJSRZSR { get; set; } // Formula:C14H26O, Mass:210.198, SMILES:CCCC\C=C/CCCCCCCC=O
        public int GZURMVKYRIAPGE { get; set; } // Formula:C14H26O, Mass:210.198, SMILES:OC1CCC2(C)(CC(C)CCC2(C1(C)(C)))
        public int HZNDJPAXRXLIOU { get; set; } // Formula:C14H26O, Mass:210.198, SMILES:OCC1(C)(CCCC2(C)(CC(C)CCC12))
        public int PNTFIUBSDLCBKC { get; set; } // Formula:C10H13NO4, Mass:211.084, SMILES:O=COCC1=CCN2CCC(OC=O)C12
        public int XCRWLLXZYREZPO { get; set; } // Formula:C9H13N3O3, Mass:211.096, SMILES:N=C1N=C(O)N(C=C1)C2OC(C)CC2(O)
        public int UFKDXENLDAZGOS { get; set; } // Formula:C11H17NO3, Mass:211.121, SMILES:O=C(OC)C1=COC(C)C2CNCCC12
        public int ROTFEGAKYXTHPP { get; set; } // Formula:C10H12O5, Mass:212.068, SMILES:OC1=CC=C(C=C1)C(O)=COC(O)CO
        public int VZHRTYHDYQRFPX { get; set; } // Formula:C10H12O5, Mass:212.068, SMILES:O=COCC1C(=C)C(=O)OC1(C=CCO)
        public int FDCFKLBIAIKUKB { get; set; } // Formula:C9H12N2O4, Mass:212.08, SMILES:O=C1C=CN(C(=O)N1)C2OC(C)C(O)C2
        public int YMJTVJQVXGBEJC { get; set; } // Formula:C14H12O2, Mass:212.084, SMILES:OC=2C=CC=C(OC=CC=1C=CC=CC=1)C=2
        public int ZCFAMXDOQTZMTD { get; set; } // Formula:C14H12O2, Mass:212.084, SMILES:OC=2C=CC=C(OC(=C)C1=CC=CC=C1)C=2
        public int CALZPZVTQZEUQP { get; set; } // Formula:C12H20O3, Mass:212.141, SMILES:O=CCCCC=CCC1CC(O)CC1(O)
        public int KIWIPIAOWPKUNM { get; set; } // Formula:C13H24O2, Mass:212.178, SMILES:CCC\C=C/CCCCCCCC(O)=O
        public int UYTDVOAIGROZFX { get; set; } // Formula:C13H24O2, Mass:212.178, SMILES:CCC\C=C/CCCC(O)CCCC=O
        public int UHUFTBALEZWWIH { get; set; } // Formula:C14H28O, Mass:212.214, SMILES:CCCCCCCCCCCCCC=O
        public int DDDFNRDAPKJTGF { get; set; } // Formula:C11H19NO3, Mass:213.136, SMILES:O=C(OC)C(=COC)C1CCN(C)CC1
        public int BJRKSJFOKUMMRO { get; set; } // Formula:C5H11O7P, Mass:214.024, SMILES:O=P(O)OC1OCC(O)C(O)C1(O)
        public int CYZZKTRFOOKUMT { get; set; } // Formula:C5H11O7P, Mass:214.024, SMILES:O=P(O)(O)OCC1OCC(O)C1(O)
        public int JPBCCNVSNVZSQL { get; set; } // Formula:C5H11O7P, Mass:214.024, SMILES:O=P(O)(O)OC1C(O)COC1(CO)
        public int QCLDEGYWPRUFHE { get; set; } // Formula:C9H10O6, Mass:214.048, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))COCO
        public int NAMFFBYYOKLDOM { get; set; } // Formula:C13H26O2, Mass:214.193, SMILES:CCCCCCCCC(O)CCCC=O
        public int SZHOJFHSIKHZHA { get; set; } // Formula:C13H26O2, Mass:214.193, SMILES:CCCCCCCCCCCCC(O)=O
        public int JZNWSCPGTDBMEW { get; set; } // Formula:C5H14NO6P, Mass:215.056, SMILES:O=P(O)(OCCN)OCC(O)CO
        public int NVOJWBBMMVWYGJ { get; set; } // Formula:C13H12O3, Mass:216.079, SMILES:OC=2C=C(O)C=C(OCC1=CC=CC=C1)C=2
        public int TXJAUSJCVVFWCG { get; set; } // Formula:C13H12O3, Mass:216.079, SMILES:OC1=CC=C(C=C1)COC=2C=CC=C(O)C=2
        public int LXNOENXQFNYMGT { get; set; } // Formula:C12H24O3, Mass:216.173, SMILES:CCCCCCCC(O)CCCC(O)=O
        public int WHJNSSCPQRQPTI { get; set; } // Formula:C13H13O3, Mass:217.086, SMILES:OC=2C=C(O)CC(=[O+]CC1=CC=CC=C1)C=2
        public int BIVCHTRQZFQIBX { get; set; } // Formula:C9H14O6, Mass:218.079, SMILES:O=CC(OC1OC(C)C(O)C(O)C1(O))=C
        public int PCYQRXYBKKZUSR { get; set; } // Formula:C12H14N2O2, Mass:218.106, SMILES:O=CN2CC3C1=CC=CC(=O)N1CC(C2)C3
        public int UZKQTCBAMSWPJD { get; set; } // Formula:C10H13N5O, Mass:219.112, SMILES:OCC(=CCNC=2N=CN=C1NC=NC1=2)C
        public int QUXPINSZKIRTKH { get; set; } // Formula:C10H21NO4, Mass:219.147, SMILES:O=C([O-])C(CCOCC(O)C)[N+](C)(C)C
        public int ZQTIKDIHRRLSRV { get; set; } // Formula:C4HF9, Mass:219.993, SMILES:FC(F)C(F)(F)C(F)(F)C(F)(F)F
        public int CZECIKXGUYQZJL { get; set; } // Formula:C14H20O2, Mass:220.146, SMILES:O=CCCCCCCCC1=CC=CC(O)=C1
        public int KVAPIDLGYVSWRD { get; set; } // Formula:C16H28, Mass:220.219, SMILES:C1=CC(C)(CC)C2(C)(CCCC(C)(C)C2(C1))
        public int CXZNQDNVILLBLN { get; set; } // Formula:C11H15N3O2, Mass:221.116, SMILES:O=C(N)NCC2C1=CC=CC(=O)N1CCC2
        public int HRSIPKSSEVRSPG { get; set; } // Formula:C13H19NO2, Mass:221.142, SMILES:O(C=1C=C2C(=CC=1(OC))C(N(C)CC2)C)C
        public int VHBFFQKBGNRLFZ { get; set; } // Formula:C15H10O2, Mass:222.068, SMILES:O=C1C=C(OC2=CC=CC=C12)C1=CC=CC=C1
        public int QBYFEBYWGWYNTO { get; set; } // Formula:C8H14O7, Mass:222.074, SMILES:O=CCOC1OC(CO)C(O)C(O)C1(O)
        public int XTESTHXDVBPIGF { get; set; } // Formula:C8H14O7, Mass:222.074, SMILES:OC=COC1OC(CO)C(O)C(O)C1(O)
        public int NBBFAZFBNQZIFY { get; set; } // Formula:C12H14O4, Mass:222.089, SMILES:O=C1OCCC1CC=2C=CC(O)=C(OC)C=2
        public int SEKORMOFYKJAJU { get; set; } // Formula:C12H14O4, Mass:222.089, SMILES:O=CC(OCC(O)CO)=CC1=CC=CC=C1
        public int UTUBAYVKULAGMT { get; set; } // Formula:C12H14O4, Mass:222.089, SMILES:O=CC(OCOCC)=CC1=CC=C(O)C=C1
        public int SLVFVSVYCYGPJP { get; set; } // Formula:C14H22O2, Mass:222.162, SMILES:OC=1C=CC=C(C=1)CCCCCCCCO
        public int DWAICOVNOFPYLS { get; set; } // Formula:C8H17NO6, Mass:223.106, SMILES:O=C(NC(CO)C(O)C(O)C(O)CO)C
        public int MLLROTAVTINRFA { get; set; } // Formula:C11H12O5, Mass:224.068, SMILES:O=C1C2=C(O)C=C(OCCO)C=C2(OCC1)
        public int PCMORTLOPMLEFB { get; set; } // Formula:C11H12O5, Mass:224.068, SMILES:O=C(O)C=CC=1C=C(OC)C(O)=C(OC)C=1
        public int ZONYXWQDUYMKFB { get; set; } // Formula:C15H12O2, Mass:224.084, SMILES:O=C1CC(OC2=CC=CC=C12)C1=CC=CC=C1
        public int BZQBQGKZJCGZCL { get; set; } // Formula:C8H16O7, Mass:224.09, SMILES:OCC(O)COC1OCC(O)C(O)C1(O)
        public int QUTQPHMWNPVJTB { get; set; } // Formula:C8H16O7, Mass:224.09, SMILES:OCCOC1C(O)C(O)C(O)C(O)C1(O)
        public int SMORULFPMBCEPX { get; set; } // Formula:C8H16O7, Mass:224.09, SMILES:OCCOC1OC(CO)C(O)C(O)C1(O)
        public int YSDLONOYOKMMAS { get; set; } // Formula:C12H16O4, Mass:224.105, SMILES:OC=1C=CC(=CC=1(O))C=C(OCOCC)C
        public int WCXGWJNKCOBVDB { get; set; } // Formula:C13H20O3, Mass:224.141, SMILES:O=C3OC1C(CC2(C)(CC(O)CCC12))C3C
        public int ILDMCKZJSLWXTH { get; set; } // Formula:C15H28O, Mass:224.214, SMILES:CCCCC\C=C/CCCCCCCC=O
        public int HIKHPXKYXLQZFQ { get; set; } // Formula:C12H19NO3, Mass:225.136, SMILES:O=C(OC)C1=COC(C)C2CN(C)CCC12
        public int YILOWADLNZNSNC { get; set; } // Formula:C10H10O6, Mass:226.048, SMILES:O=COC(C(=O)O)CC=1C=CC(O)=C(O)C=1
        public int UGUILUGCFSCUKR { get; set; } // Formula:C10H14N2O4, Mass:226.095, SMILES:O=C1N=C(O)C(=CN1C2OC(C)C(O)C2)C
        public int OURDZMSSMGUMKR { get; set; } // Formula:C15H18N2, Mass:226.147, SMILES:C=1C=CC2=C(C=1)NC3=C2CCN4CCCCC34
        public int OELPASIFBUUFIQ { get; set; } // Formula:C14H26O2, Mass:226.193, SMILES:CCCC\C=C/CCCC(O)CCCC=O
        public int YWWVWXASSLXJHU { get; set; } // Formula:C14H26O2, Mass:226.193, SMILES:CCCC\C=C/CCCCCCCC(O)=O
        public int XGQJZNCFDLXSIJ { get; set; } // Formula:C15H30O, Mass:226.23, SMILES:CCCCCCCCCCCCCCC=O
        public int JSKBTAJNHHGHSV { get; set; } // Formula:C10H10ClNO3, Mass:227.035, SMILES:O=CNC(C(=O)O)CC=1C=CC(=CC=1)Cl
        public int HLPAJQITBMEOML { get; set; } // Formula:C9H13N3O4, Mass:227.091, SMILES:N=C1N=C(O)N(C=C1)C2OC(C)C(O)C2(O)
        public int ZHHOTKZTEUZTHX { get; set; } // Formula:C9H13N3O4, Mass:227.091, SMILES:N=C1N=C(O)N(C=C1)C2OC(CO)CC2(O)
        public int KLVLAMGYGAOJBE { get; set; } // Formula:C7H18NO5P, Mass:227.092, SMILES:O=P([O-])(OCCO)OCC[N+](C)(C)C
        public int MFXCOLDTJDSQIV { get; set; } // Formula:C11H17NO4, Mass:227.116, SMILES:O=C(OCC1=CCN2CCC(O)C12)C(O)C
        public int OKTTXPFUNSZDIO { get; set; } // Formula:C12H21NO3, Mass:227.152, SMILES:O=C(OC)C(=COC)C1CCNCC1(CC)
        public int OKUIXBZDLGNNIT { get; set; } // Formula:C12H21NO3, Mass:227.152, SMILES:O=C(OC)C(=COC)C1CCN(CC)CC1
        public int NSBSWKMMSOEBLZ { get; set; } // Formula:C6H12O7S, Mass:228.03, SMILES:O=S(=O)(O)CC1OCC(O)C(O)C1(O)
        public int XTZHIZMKJUVKDN { get; set; } // Formula:C6H13O7P, Mass:228.04, SMILES:O=P(O)OC1OC(C)C(O)C(O)C1(O)
        public int FDNTWSSVAAXJJX { get; set; } // Formula:C10H12O6, Mass:228.063, SMILES:OC=1C=CC(=CC=1(O))C(O)=COC(O)CO
        public int FMNTWTDBPCQTDP { get; set; } // Formula:C10H12O6, Mass:228.063, SMILES:O=CC=1C(O)=CC(OCOCCO)=CC=1(O)
        public int WUBAOANSQGKRHF { get; set; } // Formula:C9H12N2O5, Mass:228.075, SMILES:O=C1C=CN(C(=O)N1)C2OC(C)C(O)C2(O)
        public int MSCGMSSVACOHTH { get; set; } // Formula:C14H12O3, Mass:228.079, SMILES:OC=2C=C(O)C=C(OC(=C)C1=CC=CC=C1)C=2
        public int VKFLFQDQIQVZMK { get; set; } // Formula:C14H12O3, Mass:228.079, SMILES:O=C1C(O)=C(C(=O)C2=CC=CC=C12)CC=CC
        public int YGTYGSKGKAVGLY { get; set; } // Formula:C14H12O3, Mass:228.079, SMILES:OC1=CC=C(C=C1)C(OC=2C=CC=C(O)C=2)=C
        public int ZRFRNFXJUHDIAP { get; set; } // Formula:C14H12O3, Mass:228.079, SMILES:OC2=CC=C(C=COC1=CC=CC(O)=C1)C=C2
        public int WPCPWFUUUOIOPR { get; set; } // Formula:C13H24O3, Mass:228.173, SMILES:CCC\C=C/CCCC(O)CCCC(O)=O
        public int TUNFSRHWOTWDNC { get; set; } // Formula:C14H28O2, Mass:228.209, SMILES:CCCCCCCCCCCCCC(O)=O
        public int ZTKXZDLAHNDYCJ { get; set; } // Formula:C14H28O2, Mass:228.209, SMILES:CCCCCCCCCC(O)CCCC=O
        public int GLDWNOHZSHZNOT { get; set; } // Formula:C6H15O7P, Mass:230.056, SMILES:O=P(O)(OCC(O)C)OCC(O)CO
        public int LBTWTOJXQNSHOG { get; set; } // Formula:C10H14O6, Mass:230.079, SMILES:OC=1C=C(O)C=C(OCOC(CO)CO)C=1
        public int SJTNJMYYSPVHOR { get; set; } // Formula:C10H14O6, Mass:230.079, SMILES:OC=1C=CC=C(OC(O)C(O)C(O)CO)C=1
        public int LHBXEONLEYIMGE { get; set; } // Formula:C14H14O3, Mass:230.094, SMILES:OC=2C=CC=C(OCC1=CC=C(OC)C=C1)C=2
        public int ZVANOEGUOMGWAU { get; set; } // Formula:C14H14O3, Mass:230.094, SMILES:OC1=CC=C(C=C1)C(OC2=CC=CC(O)=C2)C
        public int DEXVEASLUGBXDF { get; set; } // Formula:C13H26O3, Mass:230.188, SMILES:CCCCCCCCC(O)CCCC(O)=O
        public int LTOQTEOVRRXGBX { get; set; } // Formula:C3H2F6O3S, Mass:231.963, SMILES:O=S(=O)(O)C(F)(F)C(F)(F)C(F)F
        public int GHFOFTCKGUMTML { get; set; } // Formula:C13H12O4, Mass:232.074, SMILES:OC=2C=CC=C(OCC=1C=CC(O)=C(O)C=1)C=2
        public int KZSHYRKQUJWKDY { get; set; } // Formula:C13H12O4, Mass:232.074, SMILES:OC=1C=CC=C(C=1)COC2=CC(O)=CC(O)=C2
        public int RNEMLJPSSOJRHX { get; set; } // Formula:C12H12N2O3, Mass:232.085, SMILES:O=C(O)C(N=CO)CC2=CNC1=CC=CC=C12
        public int MCJZAFLSUPAMEV { get; set; } // Formula:C16H24O, Mass:232.183, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCC=O
        public int BOCRSCLNQLUNJO { get; set; } // Formula:C13H13O4, Mass:233.081, SMILES:OC=1C=CC=C(C=1)C[O+]=C2C=C(O)C=C(O)C2
        public int AMLAKRUHAQODRI { get; set; } // Formula:C9H14O7, Mass:234.074, SMILES:O=CC(OC1OC(CO)C(O)C(O)C1(O))=C
        public int FLDSHIUBZABFCL { get; set; } // Formula:C9H14O7, Mass:234.074, SMILES:O=CC(OC1OC(C)C(O)C(O)C1(O))=CO
        public int VDNOKVNTQJWRAC { get; set; } // Formula:C10H18O6, Mass:234.11, SMILES:OCC1OC(OCCC=C)C(O)C(O)C1(O)
        public int AZOMRYVPGFQMGZ { get; set; } // Formula:C14H18O3, Mass:234.126, SMILES:O=C1C=C2OC1(C)CC(O)C(C=C)CC=C2C
        public int GHRJJPYBLOLTEO { get; set; } // Formula:C14H18O3, Mass:234.126, SMILES:O=C1C=C2OC1(C)CCC(C=C)CC=C2CO
        public int HGFZBZIWLICHFG { get; set; } // Formula:C15H22O2, Mass:234.162, SMILES:OC3C=CC2CC1(CC(=C)C(O)C1)CC2C3(C)
        public int NXFLGXNCIBUWJR { get; set; } // Formula:C15H22O2, Mass:234.162, SMILES:O=CC(C)C1CC3CCC2(O)(C(=C)CC3(C1)(C2))
        public int ZBTPMRFIHVRWEM { get; set; } // Formula:C15H22O2, Mass:234.162, SMILES:O=CC1=C(OC)C=CC=C1CCCCCCC
        public int HRMIBYZWBMSUHC { get; set; } // Formula:C16H26O, Mass:234.198, SMILES:OC1=CC=CC(=C1)CCCCCCCCCC
        public int USMAGXMGZXKLHT { get; set; } // Formula:C16H26O, Mass:234.198, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCC=O
        public int FFHPXOJTVQDVMO { get; set; } // Formula:C10H13N5O2, Mass:235.107, SMILES:OC1CC(OC1(C))N3C=NC=2C(=NC=NC=23)N
        public int NOXWNIQIEPDBLD { get; set; } // Formula:C10H21NO5, Mass:235.142, SMILES:O=C([O-])C(CCOCC(O)CO)[N+](C)(C)C
        public int BGYISLAFYPDORJ { get; set; } // Formula:C10H12N4O3, Mass:236.091, SMILES:OC3=NC=NC1=C3(N=CN1C2OC(C)C(O)C2)
        public int AFEBBKTVFDABFK { get; set; } // Formula:C14H20O3, Mass:236.141, SMILES:O=CCCCC=CCC1C(=O)CC(O)C1(C=C)
        public int DYKBZKYHZBBMFK { get; set; } // Formula:C14H20O3, Mass:236.141, SMILES:O=C(O)CCCCCCCC1=CC=CC(O)=C1
        public int IRIFVIYTQGKTLY { get; set; } // Formula:C15H24O2, Mass:236.178, SMILES:OCCCCCCCCC1=CC=CC(OC)=C1
        public int KMGXOQZDOIYYBO { get; set; } // Formula:C16H28O, Mass:236.214, SMILES:CCC\C=C/C\C=C/CCCCCCCC=O
        public int NEWLRZGZKYZHLJ { get; set; } // Formula:C5H8N2O5P2, Mass:237.991, SMILES:O=PC(O)(CN1C=NC=C1)P(=O)(O)O
        public int HGZGMSVCFBWKLH { get; set; } // Formula:C11H10O6, Mass:238.048, SMILES:O=C(OCC(=O)O)C=CC=1C=CC(O)=C(O)C=1
        public int WMKOZARWBMFKAS { get; set; } // Formula:C15H10O3, Mass:238.063, SMILES:O=C1C(=COC=2C=C(O)C=CC1=2)C=3C=CC=CC=3
        public int YHUSUSKGDGXWGS { get; set; } // Formula:C9H18O7, Mass:238.105, SMILES:OCC(O)COC1OC(C)C(O)C(O)C1(O)
        public int QFPVVMKZTVQDTL { get; set; } // Formula:C16H30O, Mass:238.23, SMILES:CCCCCC\C=C/CCCCCCCC=O
        public int SJJYEHXVJQAFPK { get; set; } // Formula:C13H21NO3, Mass:239.152, SMILES:O=C(OC)C1=COC(C)C2CN(CC)CCC12
        public int YFDGNWYGKKBZDJ { get; set; } // Formula:C8H16O8, Mass:240.085, SMILES:OCC(O)OC1OC(CO)C(O)C(O)C1(O)
        public int AWOGPWWJNGIDKU { get; set; } // Formula:C11H16N2O4, Mass:240.111, SMILES:N=C(O)C1=CN(C=CC1)C2OC(C)C(O)C2(O)
        public int DJCQJZKZUCHHAL { get; set; } // Formula:C15H28O2, Mass:240.209, SMILES:CCCCC\C=C/CCCCCCCC(O)=O
        public int JQYGCQRQGRVLDN { get; set; } // Formula:C15H28O2, Mass:240.209, SMILES:CCCCC\C=C/CCCC(O)CCCC=O
        public int NIOYUNMRJMEDGI { get; set; } // Formula:C16H32O, Mass:240.245, SMILES:O=CCCCCCCCCCCCCCCC
        public int HLORMWAKCPDIPU { get; set; } // Formula:C11H12ClNO3, Mass:241.051, SMILES:O=C(O)C(NC(=O)C)CC=1C=CC(=CC=1)Cl
        public int DFJZCSQJBNQERR { get; set; } // Formula:C8H20NO5P, Mass:241.108, SMILES:O=P([O-])(OCC[N+](C)(C)C)OCC(O)C
        public int FYMHCWBPOCZPKA { get; set; } // Formula:C13H23NO3, Mass:241.168, SMILES:O=C(OC)C(=COC)C1CCN(C)CC1(CC)
        public int NPBGQWVNBJQFCA { get; set; } // Formula:C9H14N4O4, Mass:242.102, SMILES:N=C(O)C=1N=CN(C=1(N))C2OC(C)C(O)C2(O)
        public int YTOUXRDHJPZMFE { get; set; } // Formula:C14H26O3, Mass:242.188, SMILES:CCCC\C=C/CCCC(O)CCCC(O)=O
        public int PXBOJWNZUUEBDU { get; set; } // Formula:C15H30O2, Mass:242.225, SMILES:CCCCCCCCCCC(O)CCCC=O
        public int WQEPLUUGTLDZJY { get; set; } // Formula:C15H30O2, Mass:242.225, SMILES:CCCCCCCCCCCCCCC(O)=O
        public int LWZOUGMRXADXQM { get; set; } // Formula:C6H14NO7P, Mass:243.051, SMILES:O=C(O)C(N)COP(=O)(O)OCC(O)C
        public int UHDGCWIWMRVCDJ { get; set; } // Formula:C9H13N3O5, Mass:243.086, SMILES:N=C1N=C(O)N(C=C1)C2OC(CO)C(O)C2(O)
        public int QFBWOLBPVQLZEH { get; set; } // Formula:C6H12O8S, Mass:244.025, SMILES:O=S(=O)(O)CC1OC(O)C(O)C(O)C1(O)
        public int PTVXQARCLQPGIR { get; set; } // Formula:C6H13O8P, Mass:244.035, SMILES:O=P(O)(O)OC1OC(C)C(O)C(O)C1(O)
        public int RZLISDJKQIPSBD { get; set; } // Formula:C6H13O8P, Mass:244.035, SMILES:O=P(O)OC1C(O)C(O)C(O)C(O)C1(O)
        public int VSABSRHRDFYAJK { get; set; } // Formula:C6H13O8P, Mass:244.035, SMILES:O=P(O)OC1OC(CO)C(O)C(O)C1(O)
        public int MLAMTXQKDKVEQN { get; set; } // Formula:C10H12O7, Mass:244.058, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))COC(O)CO
        public int DRTQHJPVMGBUCF { get; set; } // Formula:C9H12N2O6, Mass:244.07, SMILES:O=C1C=CN(C(=O)N1)C2OC(CO)C(O)C2(O)
        public int DHUXSOZCLDKUJQ { get; set; } // Formula:C14H12O4, Mass:244.074, SMILES:OC1=CC=C(C=C1)C(OC=2C=C(O)C=C(O)C=2)=C
        public int RAHHWXLNOLAZBK { get; set; } // Formula:C14H12O4, Mass:244.074, SMILES:O=CC2=C(O)C=CC=C2(OCC1=CC=C(O)C=C1)
        public int YBAQMCGPGRBLJG { get; set; } // Formula:C14H12O4, Mass:244.074, SMILES:[O-]C2=CC(O)=CC(=[O+]CC1=CC=C(O)C=C1)C2(=C)
        public int PDJBHHCCMUJBLL { get; set; } // Formula:C15H16O3, Mass:244.11, SMILES:OC=2C=CC=C(OC(C1=CC=C(OC)C=C1)C)C=2
        public int IOSBERPLDLSAIE { get; set; } // Formula:C17H24O, Mass:244.183, SMILES:OC4C=CC1C(CC23(CC(=C)C(CCC12)C3))C4(C)
        public int RIOQEURNPLKGHG { get; set; } // Formula:C14H28O3, Mass:244.204, SMILES:CCCCCCCCCC(O)CCCC(O)=O
        public int HWWPQFPZWDGFFV { get; set; } // Formula:C14H13O4, Mass:245.081, SMILES:OC2=CC=C(C=[O+]C=1C=C(O)C=C(O)C=1C)C=C2
        public int LLCSXHMJULHSJN { get; set; } // Formula:C6H15O8P, Mass:246.05, SMILES:O=P(O)(OCC(O)CO)OCC(O)CO
        public int GWFJKRUNUNZHEM { get; set; } // Formula:C14H14O4, Mass:246.089, SMILES:OC1=CC=C(C=C1)C(OC2=CC(O)=CC(O)=C2)C
        public int OVMIWIRPAZMPMW { get; set; } // Formula:C14H14O4, Mass:246.089, SMILES:O=C2OC3C=CC=1OC(C(=O)C=1)(C)CCC3(C2(=C))
        public int QLUHYVGUOXRLQR { get; set; } // Formula:C14H14O4, Mass:246.089, SMILES:OC1=CC=C(C=C1)COC=2C=C(O)C=C(O)C=2C
        public int RBYSZEUQPYQATQ { get; set; } // Formula:C14H14O4, Mass:246.089, SMILES:OC=2C=CC=C(OCC=1C=CC(OC)=C(O)C=1)C=2
        public int XVOVEZNPYPHHRI { get; set; } // Formula:C14H14O4, Mass:246.089, SMILES:OC=2C=C(O)C=C(OCC1=CC=C(OC)C=C1)C=2
        public int DTGLJMVWAASSAF { get; set; } // Formula:C14H15O4, Mass:247.096, SMILES:OC=2C=CCC(=[O+]CC=1C=CC(O)=C(OC)C=1)C=2
        public int BXYSBICUNBDVDH { get; set; } // Formula:C13H12O5, Mass:248.068, SMILES:OC=2C=C(O)C=C(OCC=1C=CC(O)=C(O)C=1)C=2
        public int XRNUSRYCHMSNHQ { get; set; } // Formula:C13H12O5, Mass:248.068, SMILES:[O-]C2=CC(O)=CC(=[O+]CC=1C=CC(O)=C(O)C=1)C2
        public int IVTCJQZAGWTMBZ { get; set; } // Formula:C16H24O2, Mass:248.178, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCC(O)=O
        public int YMDRHCPLTCCRLX { get; set; } // Formula:C16H24O2, Mass:248.178, SMILES:CC\C=C/C\C=C/C\C=C/C(O)\C=C/CCC=O
        public int FAXWFCTVSHEODL { get; set; } // Formula:C6H4Br2O, Mass:249.863, SMILES:OC=1C=CC(=CC=1Br)Br
        public int IQGAPHDCDWBTLD { get; set; } // Formula:C9H14O8, Mass:250.069, SMILES:O=CC(OC1OC(CO)C(O)C(O)C1(O))=CO
        public int PQRSLRAWPZCACH { get; set; } // Formula:C13H14O5, Mass:250.084, SMILES:O=C2OC(C=C(C=1OC(C(=O)C=1)C)CO)CC2(=C)
        public int JFPANMGCHQEUEM { get; set; } // Formula:C15H22O3, Mass:250.157, SMILES:O=C(O)CCCCCCCC1=CC=CC(OC)=C1
        public int KBGYPXOSNDMZRV { get; set; } // Formula:C16H26O2, Mass:250.193, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCC(O)=O
        public int XIOAXVAJCXMAIV { get; set; } // Formula:C16H26O2, Mass:250.193, SMILES:CC\C=C/C\C=C/C\C=C/CC(O)CCCC=O
        public int SORBVCZQFFPNBZ { get; set; } // Formula:C17H30O, Mass:250.23, SMILES:CCCC\C=C/C\C=C/CCCCCCCC=O
        public int FFMZJRIUKRMUJN { get; set; } // Formula:C9H17NO5S, Mass:251.083, SMILES:O=C(O)C(N)CCSCC1OCC(O)C1(O)
        public int CPSONQHMTCQJJX { get; set; } // Formula:C10H13N5O3, Mass:251.102, SMILES:N=C1N=C(O)C=2N=CN(C=2(N1))C3OC(C)CC3(O)
        public int OFEZSBMBBKLLBJ { get; set; } // Formula:C10H13N5O3, Mass:251.102, SMILES:OCC3OC(N2C=NC1=C(N=CN=C12)N)C(O)C3
        public int RWRBHYBHSXNVPD { get; set; } // Formula:C10H13N5O3, Mass:251.102, SMILES:N=C1N=C(O)C=2N=CN(C=2(N1))C3OC(C)C(O)C3
        public int XGYIMTFOTBMPFP { get; set; } // Formula:C10H13N5O3, Mass:251.102, SMILES:OC3C(O)C(OC3(N2C=NC=1C(=NC=NC=12)N))C
        public int RIKPNWPEMPODJD { get; set; } // Formula:C16H12O3, Mass:252.079, SMILES:O=C1C(=COC=2C=CC=CC1=2)C3=CC=C(OC)C=C3
        public int IPJDTNIZLKTLEU { get; set; } // Formula:C10H12N4O4, Mass:252.086, SMILES:O=C3N=CNC1=C3(N=CN1C2OC(C)C(O)C2(O))
        public int ADKODQNXEFIOIC { get; set; } // Formula:C10H20O7, Mass:252.121, SMILES:OCCC(OC1OC(C)C(O)C(O)C1(O))CO
        public int IKDNWWNNSBFRPD { get; set; } // Formula:C16H28O2, Mass:252.209, SMILES:CCC\C=C/C\C=C/CCCC(O)CCCC=O
        public int RVEKLXYYCHAMDF { get; set; } // Formula:C16H28O2, Mass:252.209, SMILES:CCC\C=C/C\C=C/CCCCCCCC(O)=O
        public int HJEWKIMQTQIIIN { get; set; } // Formula:C17H32O, Mass:252.245, SMILES:CCCCCCC\C=C/CCCCCCCC=O
        public int COCYGNDCWFKTMF { get; set; } // Formula:C15H10O4, Mass:254.058, SMILES:OC1=CC=C2C(=O)C=C(OC2=C1O)C1=CC=CC=C1
        public int KEZLDSPIRVZOKZ { get; set; } // Formula:C15H10O4, Mass:254.058, SMILES:OC1=CC=C(C=C2OC3=CC(O)=CC=C3C2=O)C=C1
        public int OKRNDQLCMXUCGG { get; set; } // Formula:C15H10O4, Mass:254.058, SMILES:O=C1C=C(OC=2C=CC=C(O)C1=2)C3=CC=C(O)C=C3
        public int RTIXKCRFFJGDFG { get; set; } // Formula:C15H10O4, Mass:254.058, SMILES:O=C1C=C(OC=2C=C(O)C=C(O)C1=2)C3=CC=CC=C3
        public int ZQSIJRDFPHDXIC { get; set; } // Formula:C15H10O4, Mass:254.058, SMILES:O=C1C(=COC=2C=C(O)C=CC1=2)C3=CC=C(O)C=C3
        public int LQGKZLBYKWGNGP { get; set; } // Formula:C15H14N2S, Mass:254.088, SMILES:N=2C1=CC=CC=C1SC=3C=CC=CC=3(C=2NCC)
        public int AQTKXCPRNZDOJU { get; set; } // Formula:C9H18O8, Mass:254.1, SMILES:OCC(OC1OC(CO)C(O)C(O)C1(O))CO
        public int BLQTVZFBJSHRHL { get; set; } // Formula:C9H18O8, Mass:254.1, SMILES:OCC(O)COC1C(O)C(O)C(O)C(O)C1(O)
        public int NHJUPBDCSOGIKX { get; set; } // Formula:C9H18O8, Mass:254.1, SMILES:OCC(O)COC1OC(CO)C(O)C(O)C1(O)
        public int BXDTULNYBZHGMC { get; set; } // Formula:C16H30O2, Mass:254.225, SMILES:CCCCCC\C=C/CCCC(O)CCCC=O
        public int SECPZKHBENQXJG { get; set; } // Formula:C16H30O2, Mass:254.225, SMILES:CCCCCC\C=C/CCCCCCCC(O)=O
        public int PIYDVAYKYBWPPY { get; set; } // Formula:C17H34O, Mass:254.261, SMILES:CCCCCCCCCCCCCCCCC=O
        public int ZKMZBAABQFUXFE { get; set; } // Formula:C15H11O4, Mass:255.065, SMILES:OC1=CC=C(C=C1)C=2[O+]=C3C=C(O)C=C(O)C3(=CC=2)
        public int UBAHQWXPQYBZBI { get; set; } // Formula:C14H25NO3, Mass:255.183, SMILES:O=C(OC)C(=COC)C1CCN(CC)CC1(CC)
        public int DXDRHHKMWQZJHT { get; set; } // Formula:C15H12O4, Mass:256.074, SMILES:OC1=CC=C(C=CC(=O)C2=CC=C(O)C=C2O)C=C1
        public int FURUXTVZLHCCNA { get; set; } // Formula:C15H12O4, Mass:256.074, SMILES:OC1=CC=C(C=C1)C1CC(=O)C2=CC=C(O)C=C2O1
        public int HPQDBWACKYGCNM { get; set; } // Formula:C15H12O4, Mass:256.074, SMILES:O=C2C3=C(O)C=CC=C3(OC(C1=CC=CC=C1)C2(O))
        public int JSRZRMCBOQDRGK { get; set; } // Formula:C15H12O4, Mass:256.074, SMILES:OC1=CC=C(C=C1)C=3OC=2C=C(O)C=C(O)C=2CC=3
        public int URFCJEUYXNAHFI { get; set; } // Formula:C15H12O4, Mass:256.074, SMILES:O=C2C3=C(O)C=C(O)C=C3(OC(C1=CC=CC=C1)C2)
        public int OBVMPKMPQYQXKI { get; set; } // Formula:C16H20N2O, Mass:256.158, SMILES:O(C=1C=CC2=C(C=1)NC3=C2CCN4CCCCC34)C
        public int IKEWYFJJKSVXOH { get; set; } // Formula:C15H28O3, Mass:256.204, SMILES:CCCCC\C=C/CCCC(O)CCCC(O)=O
        public int GBOCFSKPHBCOCX { get; set; } // Formula:C16H32O2, Mass:256.24, SMILES:CCCCCCCCCCCC(O)CCCC=O
        public int IPCSVZSSVZVIGE { get; set; } // Formula:C16H32O2, Mass:256.24, SMILES:O=C(O)CCCCCCCCCCCCCCC
        public int UUVDEUSHGUNZAH { get; set; } // Formula:C11H12ClNO4, Mass:257.045, SMILES:O=C(O)C(NC(=O)CO)CC=1C=CC(=CC=1)Cl
        public int SUHOQUVVVLNYQR { get; set; } // Formula:C8H20NO6P, Mass:257.103, SMILES:O=P([O-])(OCC[N+](C)(C)C)OCC(O)CO
        public int XQSXMCDHWGUAAV { get; set; } // Formula:C6H11O9P, Mass:258.014, SMILES:O=C(O)C1OC(OP(=O)O)C(O)C(O)C1(O)
        public int AKOMBGQKYZCJOF { get; set; } // Formula:C11H14O7, Mass:258.074, SMILES:OC=1C=CC(=CC=1(O))C(O)=COC(O)C(O)CO
        public int QHNOMYQJGFFYSV { get; set; } // Formula:C11H14O7, Mass:258.074, SMILES:OC=2C=C(O)C=C(OC1OCC(O)C(O)C1(O))C=2
        public int IITKKCIKFTVFKR { get; set; } // Formula:C18H26O, Mass:258.198, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CC=O
        public int ZNZLXMVSALEVHW { get; set; } // Formula:C15H30O3, Mass:258.219, SMILES:CCCCCCCCCCC(O)CCCC(O)=O
        public int BCZCEDXQUVQFNI { get; set; } // Formula:C6H13NO6S2, Mass:259.018, SMILES:O=S(=O)ON=C(C)SCC(O)C(O)CO
        public int BIDLAQGLNJPMQU { get; set; } // Formula:C6H13NO6S2, Mass:259.018, SMILES:O=S(=O)ON=C(C)SCOC(CO)CO
        public int WCEGKXRIUJFLCT { get; set; } // Formula:C6H13NO6S2, Mass:259.018, SMILES:O=S(=O)ON=C(C)SC(OCCO)CO
        public int KQQGBGQNHPNHQH { get; set; } // Formula:C14H12O5, Mass:260.068, SMILES:OC=2C=C(O)C=C(OC(=C)C=1C=CC(O)=C(O)C=1)C=2
        public int RGJPTNRDAZLRPY { get; set; } // Formula:C14H12O5, Mass:260.068, SMILES:O=CC2=C(O)C=CC=C2(OCC=1C=CC(O)=C(O)C=1)
        public int UXJPJHCVFOJNSV { get; set; } // Formula:C14H12O5, Mass:260.068, SMILES:O=CC=2C(O)=CC(O)=CC=2(OCC1=CC=C(O)C=C1)
        public int WPFYNWPCNMKUBY { get; set; } // Formula:C14H12O5, Mass:260.068, SMILES:[O-]C2=CC(O)=CC(=[O+]CC=1C=CC(O)=C(O)C=1)C2(=C)
        public int YOUSVGRZGTZUBG { get; set; } // Formula:C14H12O5, Mass:260.068, SMILES:OC=C(OC=1C=C(O)C=C(O)C=1)C2=CC=C(O)C=C2
        public int KPINMAZYHUYPHM { get; set; } // Formula:C15H16O4, Mass:260.105, SMILES:OC=2C=C(O)C=C(OC(C1=CC=C(OC)C=C1)C)C=2
        public int VEKXYPCVZLIIMU { get; set; } // Formula:C15H16O4, Mass:260.105, SMILES:O=C3OC=2C(=C(O)C=C1OC(C)(C)CCC1=2)C(=C3)C
        public int GZNTYUKHRRFIEK { get; set; } // Formula:C17H24O2, Mass:260.178, SMILES:OC4C=CC1C(CC23(CC(=C)C(O)(CCC12)C3))C4(C)
        public int VWXPMSHGZMFPMK { get; set; } // Formula:C18H28O, Mass:260.214, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCC=O
        public int VZRIGAQJSXYJDP { get; set; } // Formula:C5H11NO7S2, Mass:260.998, SMILES:O=S(=O)ON=CSC(O)C(O)C(O)CO
        public int HSSQTNGDCXYNCA { get; set; } // Formula:C14H14O5, Mass:262.084, SMILES:OC=2C=C(O)C=C(OCC=1C=CC(OC)=C(O)C=1)C=2
        public int LZVDWXGWBSMYEF { get; set; } // Formula:C14H14O5, Mass:262.084, SMILES:OC=2C=C(OC)C=C(OCC=1C=CC(O)=C(O)C=1)C=2
        public int YDIQGFWMZGWLGI { get; set; } // Formula:C14H14O5, Mass:262.084, SMILES:[O-]C2=CC(O)=CC(=[O+]CC=1C=CC(O)=C(OC)C=1)C2
        public int TUCMDDWTBVMRTP { get; set; } // Formula:C18H30O, Mass:262.23, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCCCC=O
        public int MXIYPGNETXDPDR { get; set; } // Formula:C10H17NO5S, Mass:263.083, SMILES:N=C(CC=C)SC1OC(CO)C(O)C(O)C1(O)
        public int XGXUGXPKRBQINS { get; set; } // Formula:C7H6Br2O, Mass:263.879, SMILES:O(C=1C=CC(=CC=1Br)Br)C
        public int HXKIQVWRGNCIEH { get; set; } // Formula:C13H12O6, Mass:264.063, SMILES:OC2=CC(O)=CC(OCC1=CC(O)=C(O)C(O)=C1)=C2
        public int DBFVUHASLXHZCE { get; set; } // Formula:C16H24O3, Mass:264.173, SMILES:O=C(OC)C1=C(OC)C=CC=C1CCCCCCC
        public int MOMWXVGXQLHTJS { get; set; } // Formula:C16H24O3, Mass:264.173, SMILES:CC\C=C/C\C=C/C\C=C/C(O)\C=C/CCC(O)=O
        public int HXLZULGRVFOIDK { get; set; } // Formula:C18H32O, Mass:264.245, SMILES:CCCCC\C=C/C\C=C/CCCCCCCC=O
        public int PSHYJLFHLSZNLM { get; set; } // Formula:C6H4Br2O2, Mass:265.858, SMILES:OC=1C=C(C=C(C=1(O))Br)Br
        public int SDACWHIIYVTWNU { get; set; } // Formula:C16H26O3, Mass:266.188, SMILES:CC\C=C/C\C=C/C\C=C/CC(O)CCCC(O)=O
        public int FXLBBNFRKMTPNR { get; set; } // Formula:C17H30O2, Mass:266.225, SMILES:CCCC\C=C/C\C=C/CCCC(O)CCCC=O
        public int LEIXEEFBKOMCEQ { get; set; } // Formula:C17H30O2, Mass:266.225, SMILES:CCCC\C=C/C\C=C/CCCCCCCC(O)=O
        public int ZENZJGDPWWLORF { get; set; } // Formula:C18H34O, Mass:266.261, SMILES:CCCCCCCC\C=C/CCCCCCCC=O
        public int FBLYADUDJIDSCH { get; set; } // Formula:C10H13N5O4, Mass:267.097, SMILES:N=C1N=C(O)C=2N=CN(C=2(N1))C3OC(C)C(O)C3(O)
        public int OIRDTQYFTABQOQ { get; set; } // Formula:C10H13N5O4, Mass:267.097, SMILES:OCC3OC(N2C=NC1=C(N=CN=C12)N)C(O)C3(O)
        public int YKBGVTZYEHREMT { get; set; } // Formula:C10H13N5O4, Mass:267.097, SMILES:N=C1N=C(O)C=2N=CN(C=2(N1))C3OC(CO)C(O)C3
        public int HKQYGTCOTHHOMP { get; set; } // Formula:C16H12O4, Mass:268.074, SMILES:COC1=CC=C(C=C1)C1=COC2=CC(O)=CC=C2C1=O
        public int UGQMRVRMYYASKQ { get; set; } // Formula:C10H12N4O5, Mass:268.081, SMILES:O=C3N=CNC1=C3(N=CN1C2OC(CO)C(O)C2(O))
        public int VKYHXDQOZGKJIN { get; set; } // Formula:C10H12N4O5, Mass:268.081, SMILES:O=C1NC(=O)C=2N=CN(C=2(N1))C3OC(C)C(O)C3(O)
        public int DEAZPSKQWDTLRW { get; set; } // Formula:C13H16O6, Mass:268.095, SMILES:O=C1C2=CC=C(OCC(O)C(O)CO)C=C2(OCC1)
        public int LBLNXZIGDJPBJM { get; set; } // Formula:C13H12N6O, Mass:268.107, SMILES:N=C1N=C(O)C=2N=C(C=NC=2(N1))CNC3=CC=CC=C3
        public int OOLQGZAAQGIVPH { get; set; } // Formula:C10H20O8, Mass:268.116, SMILES:OCC(O)C(O)COC1OC(C)C(O)C(O)C1(O)
        public int ZRAPKHPTJKSDDV { get; set; } // Formula:C16H28O3, Mass:268.204, SMILES:CCC\C=C/C\C=C/CCCC(O)CCCC(O)=O
        public int FEJABJBBRPQHRG { get; set; } // Formula:C17H32O2, Mass:268.24, SMILES:CCCCCCC\C=C/CCCC(O)CCCC=O
        public int QSBYPNXLFMSGKH { get; set; } // Formula:C17H32O2, Mass:268.24, SMILES:CCCCCCC\C=C/CCCCCCCC(O)=O
        public int FWWQKRXKHIRPJY { get; set; } // Formula:C18H36O, Mass:268.277, SMILES:CCCCCCCCCCCCCCCCCC=O
        public int WXFBZGUXZMEPIR { get; set; } // Formula:C5HF11, Mass:269.99, SMILES:FC(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F
        public int KZNIFHPLKGYRTM { get; set; } // Formula:C15H10O5, Mass:270.053, SMILES:O=C1C=C(OC=2C=C(O)C=C(O)C1=2)C3=CC=C(O)C=C3
        public int TZBJGXHYKVUXJN { get; set; } // Formula:C15H10O5, Mass:270.053, SMILES:OC1=CC=C(C=C1)C1=COC2=CC(O)=CC(O)=C2C1=O
        public int VCCRNZQBSJXYJD { get; set; } // Formula:C15H10O5, Mass:270.053, SMILES:O=C1C(O)=C(OC2=CC(O)=CC(O)=C12)C3=CC=CC=C3
        public int ZLNYYIOAMRCRGD { get; set; } // Formula:C15H10O5, Mass:270.053, SMILES:O=C1C(O)=C(OC2=CC=CC(O)=C12)C3=CC=C(O)C=C3
        public int NSRJSISNDPOJOP { get; set; } // Formula:C16H14O4, Mass:270.089, SMILES:COC1=CC=C2C3COC4=CC(O)=CC=C4C3OC2=C1
        public int LWQLZUYMERWSMQ { get; set; } // Formula:C9H18O9, Mass:270.095, SMILES:OCC(O)C(O)OCC1OC(O)(CO)C(O)C1(O)
        public int WADMMPOYJUJWAB { get; set; } // Formula:C16H30O3, Mass:270.219, SMILES:CCCCCC\C=C/CCCC(O)CCCC(O)=O
        public int KEMQGTRYUADPNZ { get; set; } // Formula:C17H34O2, Mass:270.256, SMILES:CCCCCCCCCCCCCCCCC(O)=O
        public int XZNLDDNERIACTN { get; set; } // Formula:C17H34O2, Mass:270.256, SMILES:CCCCCCCCCCCCC(O)CCCC=O
        public int GDNIGMNXEKGFIP { get; set; } // Formula:C15H11O5, Mass:271.06, SMILES:OC=1C=C(O)C2=CC=C([O+]=C2(C=1))C=3C=CC(O)=C(O)C=3
        public int XVFMGWDSJLBXDZ { get; set; } // Formula:C15H11O5, Mass:271.06, SMILES:OC=1C=CC(=CC=1)C2=[O+]C3=CC(O)=CC(O)=C3(C=C2(O))
        public int ARVXNSRONWYRIU { get; set; } // Formula:C15H12O5, Mass:272.068, SMILES:OC1=CC=C(C=C1)C=3OC2=CC(O)=CC(O)=C2CC=3(O)
        public int FTVWIRXFELQLPI { get; set; } // Formula:C15H12O5, Mass:272.068, SMILES:O=C1CC(OC2=CC(O)=CC(O)=C12)C=3C=CC(O)=CC=3
        public int SUYJZKRQHBQNCA { get; set; } // Formula:C15H12O5, Mass:272.068, SMILES:O=C2C3=C(O)C=C(O)C=C3(OC(C1=CC=CC=C1)C2(O))
        public int UYUZDOBYKBYLLE { get; set; } // Formula:C15H12O5, Mass:272.068, SMILES:OC=3C=C(O)C1=C(OC(=CC1)C=2C=CC(O)=C(O)C=2)C=3
        public int LAWZEAWGPUTPRO { get; set; } // Formula:C12H16O7, Mass:272.09, SMILES:OC2=CC=CC(OC1OC(CO)C(O)C(O)C1(O))=C2
        public int XRVFNNUXNVWYTI { get; set; } // Formula:C16H16O4, Mass:272.105, SMILES:COC1=CC=C(C2COC3=CC(O)=CC=C3C2)C(O)=C1
        public int GIEJMPHHFWMLHG { get; set; } // Formula:C16H32O3, Mass:272.235, SMILES:CCCCCCCCCCCC(O)CCCC(O)=O
        public int JMKLCSDHBFYFKV { get; set; } // Formula:C11H14O8, Mass:274.069, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))COC(O)C(O)CO
        public int POMIUIQILZMMDR { get; set; } // Formula:C11H14O8, Mass:274.069, SMILES:O=CC1=C(O)C=C(OC(O)C(O)C(O)CO)C=C1(O)
        public int UEQAICZERAGAMU { get; set; } // Formula:C15H14O5, Mass:274.084, SMILES:O=CC2=C(O)C=C(O)C=C2(OCC1=CC=C(OC)C=C1)
        public int LYJOUWBWJDKKEF { get; set; } // Formula:C18H26O2, Mass:274.193, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CC(O)=O
        public int SXRXBWFMAYCJSP { get; set; } // Formula:C18H26O2, Mass:274.193, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C(O)\C=C/CC=O
        public int NJVCBRZYGVLGQI { get; set; } // Formula:C17H25NO2, Mass:275.189, SMILES:O(C=1C=C2C(=CC=1(OC))C3N(CC2)CC(CC)CC3)C
        public int HEXOPSWGNBGINC { get; set; } // Formula:C14H12O6, Mass:276.063, SMILES:O=CC2=C(O)C=C(O)C=C2(OCC=1C=CC(O)=C(O)C=1)
        public int JVAHPDRCBKDCGK { get; set; } // Formula:C14H12O6, Mass:276.063, SMILES:OC2=CC(O)=CC(OC(=C)C1=CC(O)=C(O)C(O)=C1)=C2
        public int MOZSFSXUQDZNKW { get; set; } // Formula:C14H12O6, Mass:276.063, SMILES:OC=C(OC=1C=C(O)C=C(O)C=1)C=2C=CC(O)=C(O)C=2
        public int MESCNDMXSKOWHE { get; set; } // Formula:C15H16O5, Mass:276.1, SMILES:O=C2OC3C=C(C=1OC(C(=O)C=1)(C)CCC3(C2(=C)))CO
        public int OUGXDFQHDCEVIE { get; set; } // Formula:C16H20O4, Mass:276.136, SMILES:O=C1OC2(C=CC(O)C1C2)C4CCC3(O)(C(=C)CC4(C3))
        public int DEEQOBNVYSHJLG { get; set; } // Formula:C18H28O2, Mass:276.209, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C(O)CCCC=O
        public int JIWBIWFOSCKQMA { get; set; } // Formula:C18H28O2, Mass:276.209, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCC(O)=O
        public int JDVGMXCLBRXCCE { get; set; } // Formula:C5H11NO8S2, Mass:276.993, SMILES:O=S(=O)(O)ON=CSC(O)C(O)C(O)CO
        public int MIWPEPMEDVNBCB { get; set; } // Formula:C14H13O6, Mass:277.071, SMILES:OC=2C=C(O)C(=C)C(=[O+]CC=1C=C(O)C(O)=C(O)C=1)C=2
        public int CVOOKAVYLTYIQE { get; set; } // Formula:C5H12O9P2, Mass:277.996, SMILES:O=P(OCC1OCC(O)C1(O))OP(=O)(O)O
        public int JLAOMXITMPJYDG { get; set; } // Formula:C5H12O9P2, Mass:277.996, SMILES:O=P(O)OP(=O)OC1OCC(O)C(O)C1(O)
        public int JOAKUIIGLMLEGB { get; set; } // Formula:C5H12O9P2, Mass:277.996, SMILES:O=P(O)(O)OP(=O)(O)OCC1OCCC1(O)
        public int WMISJDZYUVCWEJ { get; set; } // Formula:C5H12O9P2, Mass:277.996, SMILES:O=P(O)OCC1OCC(O)C1(OP(=O)(O)O)
        public int ASNCGFWZNUIUHX { get; set; } // Formula:C16H22O4, Mass:278.152, SMILES:O=CCCCCCCCC1=CC=CC(OC)=C1(C(=O)O)
        public int DRJSFRIXGBWQQP { get; set; } // Formula:C16H22O4, Mass:278.152, SMILES:O=CC1=C(OC)C=CC=C1CCCCCCCC(=O)O
        public int AYEXUOCADOPDOM { get; set; } // Formula:C18H30O2, Mass:278.225, SMILES:CC\C=C/C\C=C/C\C=C/CCCC(O)CCCC=O
        public int DTOSIQBPPRVQHS { get; set; } // Formula:C18H30O2, Mass:278.225, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCCCC(O)=O
        public int GPXKCXWIFRWJHH { get; set; } // Formula:C19H34O, Mass:278.261, SMILES:CCCCCC\C=C/C\C=C/CCCCCCCC=O
        public int JTBLHXSTTJUWET { get; set; } // Formula:C15H21NO4, Mass:279.147, SMILES:O=C(O)C(N=C(O)CCC(C)C)CC1=CC=C(O)C=C1
        public int OYHQOLUKZRVURQ { get; set; } // Formula:C18H32O2, Mass:280.24, SMILES:CCCCC\C=C/C\C=C/CCCCCCCC(O)=O
        public int XDTCKKYGZMYRFG { get; set; } // Formula:C18H32O2, Mass:280.24, SMILES:CCCCC\C=C/C\C=C/CCCC(O)CCCC=O
        public int AKQYTSLMKKEXOK { get; set; } // Formula:C19H36O, Mass:280.277, SMILES:CCCCCCCCC\C=C/CCCCCCCC=O
        public int UTWCWKUZICRKOA { get; set; } // Formula:C4H2F8O3S, Mass:281.96, SMILES:O=S(=O)(O)C(F)(F)C(F)(F)C(F)(F)C(F)F
        public int MPALGUFFCYBZBP { get; set; } // Formula:C11H22O8, Mass:282.131, SMILES:OCC(O)C(O)CCOC1OC(C)C(O)C(O)C1(O)
        public int RLQSUKOOLWMMSY { get; set; } // Formula:C11H22O8, Mass:282.131, SMILES:OCCC(O)C(O)COC1OC(C)C(O)C(O)C1(O)
        public int JBITZWPSOWZJRD { get; set; } // Formula:C17H30O3, Mass:282.219, SMILES:CCCC\C=C/C\C=C/CCCC(O)CCCC(O)=O
        public int WXMZNRDJAZOZCO { get; set; } // Formula:C18H34O2, Mass:282.256, SMILES:CCCCCCCC\C=C/CCCC(O)CCCC=O
        public int ZQPPMHVWECSIRJ { get; set; } // Formula:C18H34O2, Mass:282.256, SMILES:O=C(O)CCCCCCCC=CCCCCCCCC
        public int SXIYYZWCMUFWBW { get; set; } // Formula:C19H38O, Mass:282.292, SMILES:CCCCCCCCCCCCCCCCCCC=O
        public int NYHBQMYGNKIUIF { get; set; } // Formula:C10H13N5O5, Mass:283.092, SMILES:N=C1N=C(O)C=2N=CN(C=2(N1))C3OC(CO)C(O)C3(O)
        public int BWUSUWNUKKXAEV { get; set; } // Formula:C12H12O8, Mass:284.053, SMILES:O=C1C(OC(O)C(O)CO)=COC2=CC(O)=CC(O)=C12
        public int DANYIYRPLHHOCZ { get; set; } // Formula:C16H12O5, Mass:284.068, SMILES:O=C1C=C(OC=2C=C(O)C=C(O)C1=2)C3=CC=C(OC)C=C3
        public int JPMYFOBNRRGFNO { get; set; } // Formula:C16H12O5, Mass:284.068, SMILES:COC1=CC(O)=C2C(=O)C=C(OC2=C1)C1=CC=C(O)C=C1
        public int WUADCCWRTIWANL { get; set; } // Formula:C16H12O5, Mass:284.068, SMILES:COC1=CC=C(C=C1)C1=COC2=CC(O)=CC(O)=C2C1=O
        public int XKHHKXCBFHUOHM { get; set; } // Formula:C16H12O5, Mass:284.068, SMILES:COC1=CC=C(C(O)=C1)C1=COC2=CC(O)=CC=C2C1=O
        public int YXIWGOWCOPLSJZ { get; set; } // Formula:C16H12O5, Mass:284.068, SMILES:O=C1C=C(OC=2C1=C(O)C=C(O)C=2C)C3=CC=C(O)C=C3
        public int ZLGRXDWWYMFIGI { get; set; } // Formula:C16H12O5, Mass:284.068, SMILES:O=C1C=C(OC=2C=C(O)C(=C(O)C1=2)C)C3=CC=C(O)C=C3
        public int BJHFCSYKUJICOO { get; set; } // Formula:C13H16O7, Mass:284.09, SMILES:OC=2C=CC(C=COC1OCC(O)C(O)C1(O))=CC=2(O)
        public int CPODZCVBMHISBV { get; set; } // Formula:C10H20O9, Mass:284.111, SMILES:OCC(O)C(O)COC1(OC(CO)C(O)C1(O))(CO)
        public int DUUKYOAVWFMSKJ { get; set; } // Formula:C10H20O9, Mass:284.111, SMILES:OCC(O)C(OC1OC(CO)C(O)C(O)C1(O))CO
        public int KLLPNSTZQPSZHU { get; set; } // Formula:C10H20O9, Mass:284.111, SMILES:OCC(O)C(O)C(O)COC1OCC(O)C(O)C1(O)
        public int NHYPIRXECJPNTG { get; set; } // Formula:C10H20O9, Mass:284.111, SMILES:OCC(O)C(O)COC1C(O)C(O)C(O)C(O)C1(O)
        public int UGJALIVEHMFBSP { get; set; } // Formula:C10H20O9, Mass:284.111, SMILES:OCC(O)C(O)COC1OC(CO)C(O)C(O)C1(O)
        public int ISKJMLYXFUULLY { get; set; } // Formula:C17H32O3, Mass:284.235, SMILES:CCCCCCC\C=C/CCCC(O)CCCC(O)=O
        public int PZLYXEVGRAQCLK { get; set; } // Formula:C18H36O2, Mass:284.272, SMILES:CCCCCCCCCCCCCC(O)CCCC=O
        public int QIQXTHQIDYTFRH { get; set; } // Formula:C18H36O2, Mass:284.272, SMILES:O=C(O)CCCCCCCCCCCCCCCCC
        public int FWGOJLIMNNWOLX { get; set; } // Formula:C8H16NO8P, Mass:285.061, SMILES:O=P(O)OC1OC(CO)C(O)C(O)C1(N=C(O)C)
        public int ABDQTIKKAYGHNV { get; set; } // Formula:C15H10O6, Mass:286.048, SMILES:O=C1C(O)=C(OC2=CC(O)=CC(O)=C12)C=3C=CC=C(O)C=3
        public int IQPNAANSBPBGFQ { get; set; } // Formula:C15H10O6, Mass:286.048, SMILES:O=C1C=C(OC=2C=C(O)C=C(O)C1=2)C=3C=CC(O)=C(O)C=3
        public int VEVZSMAEJFVWIL { get; set; } // Formula:C15H10O6, Mass:286.048, SMILES:[O-]C1=CC(O)=CC2=[O+]C(=C(O)C=C12)C=3C=CC(O)=C(O)C=3
        public int XHEFDIBZLJXQHF { get; set; } // Formula:C15H10O6, Mass:286.048, SMILES:OC1=CC=C2C(OC(=C(O)C2=O)C2=CC(O)=C(O)C=C2)=C1
        public int HAKNARODOLMANY { get; set; } // Formula:C12H14O8, Mass:286.069, SMILES:O=CC=2C(O)=CC(OC1OCC(O)C(O)C1(O))=CC=2(O)
        public int FVMMGLAHGMDCLW { get; set; } // Formula:C16H14O5, Mass:286.084, SMILES:OC1=CC=C(C=C1)C=3OC2=CC(O)=CC(O)=C2CC=3(OC)
        public int HMUJXQRRKBLVOO { get; set; } // Formula:C16H14O5, Mass:286.084, SMILES:O=C2C3=C(O)C=C(O)C=C3(OC(C1=CC=C(OC)C=C1)C2)
        public int KZSKFOJHFUBFSD { get; set; } // Formula:C20H30O, Mass:286.23, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CCCC=O
        public int SUESMKJJGAOOGC { get; set; } // Formula:C17H34O3, Mass:286.251, SMILES:CCCCCCCCCCCCC(O)CCCC(O)=O
        public int ZPVNWCMRCGXRJD { get; set; } // Formula:C15H12O6, Mass:288.063, SMILES:O=C2C=3C=CC(O)=C(O)C=3(OC(C=1C=CC(O)=C(O)C=1)C2)
        public int BUIKUQZPNWEHMR { get; set; } // Formula:C12H16O8, Mass:288.085, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))COCC(O)C(O)CO
        public int WHDIYCULVGAWGN { get; set; } // Formula:C12H16O8, Mass:288.085, SMILES:OC=1C=C(O)C(=C(O)C=1)C2OC(CO)C(O)C(O)C2(O)
        public int WWVGRACROSBOHO { get; set; } // Formula:C12H16O8, Mass:288.085, SMILES:OC2=CC=CC(OC1OC(CO)C(O)C(O)C1(O))=C2(O)
        public int CEHNAQNSWRFJSE { get; set; } // Formula:C16H16O5, Mass:288.1, SMILES:OC=2C=C(O)C(=CC(OC)=CC=1C=CC(O)=C(O)C=1)CC=2
        public int LRVALLDMDUHEPD { get; set; } // Formula:C18H24O3, Mass:288.173, SMILES:O=CC2(C)(C(O)C=CC1C4CCC3(O)(C(=C)CC4(CC12)(C3)))
        public int XOZDREDGDDNHBM { get; set; } // Formula:C20H32O, Mass:288.245, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCCCC=O
        public int WWRALUWFOYYMKZ { get; set; } // Formula:C18H27NO2, Mass:289.204, SMILES:O(C=1C=C2C(=CC=1(OC))C3N(CC2)CC(CC)C(C)C3)C
        public int CQWRVJZHJPBJEC { get; set; } // Formula:C15H14O6, Mass:290.079, SMILES:O=CC2=C(O)C=C(O)C=C2(OCC=1C=CC(OC)=C(O)C=1)
        public int PFTAWBLQPZVEMU { get; set; } // Formula:C15H14O6, Mass:290.079, SMILES:OC=3C=C(O)C2=C(OC(C=1C=CC(O)=C(O)C=1)C(O)C2)C=3
        public int UFWSPVHXCKOZKP { get; set; } // Formula:C15H14O6, Mass:290.079, SMILES:O=CC2=C(O)C(OC)=C(O)C=C2(OCC1=CC=C(O)C=C1)
        public int ZGELUCMZDPAADK { get; set; } // Formula:C15H14O6, Mass:290.079, SMILES:OC1=CC=C(C=C1)C(OC2=CC(O)=CC(O)=C2)=COCO
        public int JSRBPYKOLODWGC { get; set; } // Formula:C18H26O3, Mass:290.188, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C(O)\C=C/CC(O)=O
        public int GVYIFDMCYYJQNW { get; set; } // Formula:C20H34O, Mass:290.261, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCCCCCC=O
        public int KNLJEJQKLJVQQJ { get; set; } // Formula:C9H14N3O6P, Mass:291.062, SMILES:O=C1N=C(N)C=CN1C2OC(COP(=O)O)C(O)C2
        public int FABRUUBOLWFYIR { get; set; } // Formula:C14H12O7, Mass:292.058, SMILES:O=CC2=C(O)C=C(O)C=C2(OCC1=CC(O)=C(O)C(O)=C1)
        public int JZPSRJYCHQSSRG { get; set; } // Formula:C18H28O3, Mass:292.204, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C(O)CCCC(O)=O
        public int SYNQCAFPBFGQPP { get; set; } // Formula:C20H36O, Mass:292.277, SMILES:CCCCC\C=C/C\C=C/CCCCCCCCCC=O
        public int HNPYBPXXPMVWIY { get; set; } // Formula:C5H12O10P2, Mass:293.991, SMILES:O=P(O)(O)OP(=O)(O)OCC1OCC(O)C1(O)
        public int WKFIWTREJDPNAT { get; set; } // Formula:C18H30O3, Mass:294.219, SMILES:CC\C=C/C\C=C/C\C=C/CCCC(O)CCCC(O)=O
        public int BFXHARLBAGKNRH { get; set; } // Formula:C19H34O2, Mass:294.256, SMILES:CCCCCC\C=C/C\C=C/CCCCCCCC(O)=O
        public int DGXSKDFLQFJHGI { get; set; } // Formula:C19H34O2, Mass:294.256, SMILES:CCCCCC\C=C/C\C=C/CCCC(O)CCCC=O
        public int WJLJKNYXWACGHC { get; set; } // Formula:C20H38O, Mass:294.292, SMILES:CCCCCCCC\C=C/CCCCCCCCCC=O
        public int XBNXEPBRQKLHLS { get; set; } // Formula:C14H16O7, Mass:296.09, SMILES:O=CC(OC1OCC(O)C(O)C1(O))=C(O)C2=CC=CC=C2
        public int AQXYGNDEVDTNGB { get; set; } // Formula:C11H20O9, Mass:296.111, SMILES:O=C(O)C1OC(OCC)C(OCC(O)CO)C(O)C1(O)
        public int HQZSWVRAXHHXOP { get; set; } // Formula:C11H20O9, Mass:296.111, SMILES:O=C(O)C1OC(OC(CO)COCC)C(O)C(O)C1(O)
        public int KEGVSAQKMDDMFR { get; set; } // Formula:C11H20O9, Mass:296.111, SMILES:OC2COC(OCC1OCC(O)C(O)C1(O))C(O)C2(O)
        public int SXEHDCDGTPHFBR { get; set; } // Formula:C11H20O9, Mass:296.111, SMILES:OCC2OCC(OC1OCC(O)C(O)C1(O))C(O)C2(O)
        public int VQCRNRSTXBCRKF { get; set; } // Formula:C18H32O3, Mass:296.235, SMILES:CCCCC\C=C/C\C=C/CCCC(O)CCCC(O)=O
        public int OBIITBOBZMXSFO { get; set; } // Formula:C19H36O2, Mass:296.272, SMILES:CCCCCCCCC\C=C/CCCC(O)CCCC=O
        public int YOKHLRHWEXTWJR { get; set; } // Formula:C19H36O2, Mass:296.272, SMILES:CCCCCCCCC\C=C/CCCCCCCC(O)=O
        public int FWBUWJHWAKTPHI { get; set; } // Formula:C20H40O, Mass:296.308, SMILES:CCCCCCCCCCCCCCCCCCCC=O
        public int NUGRQNBDTZWXTP { get; set; } // Formula:C16H10O6, Mass:298.048, SMILES:OC1=CC=C(C=C1)C1=COC2=CC3=C(OCO3)C(O)=C2C1=O
        public int HLAMYEBSNZBRER { get; set; } // Formula:C17H14O5, Mass:298.084, SMILES:O=C1C=C(OC2=CC(OC)=C(C=C12)CO)C3=CC=C(O)C=C3
        public int KJGPBYUQZLUKLL { get; set; } // Formula:C17H14O5, Mass:298.084, SMILES:COC1=CC=C(C=C1)C1=COC2=CC(O)=C(OC)C=C2C1=O
        public int SXTAZFUZGSUXKO { get; set; } // Formula:C17H14O5, Mass:298.084, SMILES:O=C1C=C(OC=2C=C(O)C(=C(O)C1=2)CC)C3=CC=C(O)C=C3
        public int VQCXCCMCKDSXMQ { get; set; } // Formula:C17H14O5, Mass:298.084, SMILES:O=C1C=C(OC2=CC(OC)=C(C(O)=C12)C)C3=CC=C(O)C=C3
        public int XBOSFRCNQGTSLQ { get; set; } // Formula:C17H14O5, Mass:298.084, SMILES:O=C1C=C(OC2=C1C(O)=CC(O)=C2CC)C3=CC=C(O)C=C3
        public int OQRACADPMGWIGG { get; set; } // Formula:C11H22O9, Mass:298.126, SMILES:OCC(O)C(O)C(O)COC1OC(C)C(O)C(O)C1(O)
        public int DQSUJQBHODXQRH { get; set; } // Formula:C18H34O3, Mass:298.251, SMILES:CCCCCCCC\C=C/CCCC(O)CCCC(O)=O
        public int ISYWECDDZWTKFF { get; set; } // Formula:C19H38O2, Mass:298.287, SMILES:CCCCCCCCCCCCCCCCCCC(O)=O
        public int RQRCANBXSSHEIY { get; set; } // Formula:C19H38O2, Mass:298.287, SMILES:CCCCCCCCCCCCCCC(O)CCCC=O
        public int AQDQLNMPFWYWEN { get; set; } // Formula:C6H3Br2ClO2, Mass:299.819, SMILES:OC=1C=C(C(=C(C=1(O))Br)Cl)Br
        public int MBNGWHIJMBWFHU { get; set; } // Formula:C16H12O6, Mass:300.063, SMILES:O=C1C=C(OC2=CC(O)=CC(O)=C12)C=3C=CC(OC)=C(O)C=3
        public int NFSDHORLNPYHIE { get; set; } // Formula:C16H12O6, Mass:300.063, SMILES:O=C1C=C(OC=2C1=C(O)C=C(O)C=2C)C=3C=CC(O)=C(O)C=3
        public int SCZVLDHREVKTSH { get; set; } // Formula:C16H12O6, Mass:300.063, SMILES:COC1=CC(=CC=C1O)C1=CC(=O)C2=C(O)C=C(O)C=C2O1
        public int SQFSKOYWJBQGKQ { get; set; } // Formula:C16H12O6, Mass:300.063, SMILES:COC1=CC=C(C=C1)C1=C(O)C(=O)C2=C(O)C=C(O)C=C2O1
        public int VJJZJBUCDWKPLC { get; set; } // Formula:C16H12O6, Mass:300.063, SMILES:O=C1C(OC)=C(OC2=CC(O)=CC(O)=C12)C3=CC=C(O)C=C3
        public int YTITYUDOZJUZBE { get; set; } // Formula:C18H36O3, Mass:300.266, SMILES:CCCCCCCCCCCCCC(O)CCCC(O)=O
        public int AQRDRMRSQVOKBY { get; set; } // Formula:C16H13O6, Mass:301.071, SMILES:OC=1C=CC2=CC(OCO)=C([O+]=C2(C=1))C=3C=CC(O)=C(O)C=3
        public int QLWSWRWMXXKOBO { get; set; } // Formula:C16H13O6, Mass:301.071, SMILES:OC=1C=C(O)C2=CC(OC)=C([O+]=C2(C=1))C=3C=CC(O)=C(O)C=3
        public int XFDQJKDGGOEYPI { get; set; } // Formula:C16H13O6, Mass:301.071, SMILES:OC=1C=C(O)C2=CC(O)=C([O+]=C2(C=1))C=3C=CC(O)=C(OC)C=3
        public int LFPHMXIOQBBTSS { get; set; } // Formula:C15H10O7, Mass:302.043, SMILES:O=C1C(O)=C(OC2=CC(O)=C(O)C(O)=C12)C3=CC=C(O)C=C3
        public int REFJWTPEDVJJIY { get; set; } // Formula:C15H10O7, Mass:302.043, SMILES:O=C1C(O)=C(OC2=CC(O)=CC(O)=C12)C=3C=CC(O)=C(O)C=3
        public int ZDBWHGOODQDHIY { get; set; } // Formula:C9H19O9P, Mass:302.077, SMILES:O=P(O)(OCCC)OC1C(O)C(O)C(O)C(O)C1(O)
        public int AIONOLUJZLIMTK { get; set; } // Formula:C16H14O6, Mass:302.079, SMILES:O=C2C3=C(O)C=C(O)C=C3(OC(C=1C=CC(OC)=C(O)C=1)C2)
        public int NHBXJBAYARAWKJ { get; set; } // Formula:C16H14O6, Mass:302.079, SMILES:OC3=CC(O)=C2C(OC(C=1C=CC(O)=C(O)C=1)=C(OC)C2)=C3
        public int UAQXHIYSKKLMPW { get; set; } // Formula:C18H22O4, Mass:302.152, SMILES:O=C1OC25(C=CC(O)C1(C)C5(CC34(CC(=C)C(O)(CCC23)C4)))
        public int JAZBEHYOTPTENJ { get; set; } // Formula:C20H30O2, Mass:302.225, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CCCC(O)=O
        public int KQCIODJBABTUAA { get; set; } // Formula:C20H30O2, Mass:302.225, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C(O)\C=C/CCCC=O
        public int JKHRCGUTYDNCLE { get; set; } // Formula:C15H11O7, Mass:303.05, SMILES:OC=1C=C(O)C2=CC(O)=C([O+]=C2(C=1))C3=CC(O)=C(O)C(O)=C3
        public int CXQWRCVTCMQVQX { get; set; } // Formula:C15H12O7, Mass:304.058, SMILES:O=C2C3=C(O)C=C(O)C=C3(OC(C=1C=CC(O)=C(O)C=1)C2(O))
        public int DDNOLNFAGDJTJZ { get; set; } // Formula:C12H16O9, Mass:304.079, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))COC(O)C(O)C(O)CO
        public int OJBWFYJOLJOTPP { get; set; } // Formula:C16H16O6, Mass:304.095, SMILES:[O-]C2=CC(O)=CC(=[O+]C(=COC)C=1C=CC(O)=C(OC)C=1)C2
        public int HQPCSDADVLFHHO { get; set; } // Formula:C20H32O2, Mass:304.24, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCCCC(O)=O
        public int WRAABDGBHRJLHR { get; set; } // Formula:C20H32O2, Mass:304.24, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCC(O)CCCC=O
        public int PTFIPECGHSYQNR { get; set; } // Formula:C21H36O, Mass:304.277, SMILES:OC=1C=CC=C(C=1)CCCCCCCCCCCCCCC
        public int HYZIWFJILYVTQR { get; set; } // Formula:C10H15N2O7P, Mass:306.062, SMILES:O=C1N=C(O)C(=CN1C2OC(COP(=O)O)C(O)C2)C
        public int LLZWBQZRXRWZDE { get; set; } // Formula:C15H14O7, Mass:306.074, SMILES:OC=2C=C(O)C=C(OC(=COCO)C=1C=CC(O)=C(O)C=1)C=2
        public int AHANXAKGNAKFSK { get; set; } // Formula:C20H34O2, Mass:306.256, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCCCCCC(O)=O
        public int KHZUFLNDJSDGNR { get; set; } // Formula:C20H34O2, Mass:306.256, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCC(O)CCCC=O
        public int RQBMSYDMYCXPOK { get; set; } // Formula:C21H38O, Mass:306.292, SMILES:CCCCCC\C=C/C\C=C/CCCCCCCCCC=O
        public int LPOBTJSHGYZVBV { get; set; } // Formula:C9H14N3O7P, Mass:307.057, SMILES:O=C1N=C(N)C=CN1C2OC(COP(=O)O)C(O)C2(O)
        public int MWQLKVPHFZFVLS { get; set; } // Formula:C9H13N2O8P, Mass:308.041, SMILES:O=C1C=CN(C(=O)N1)C2OC(COP(=O)O)C(O)C2(O)
        public int SEOHEWZQAGEAGZ { get; set; } // Formula:C9H13N2O8P, Mass:308.041, SMILES:O=C1C=CN(C(=O)N1)C2OC(COP(=O)(O)O)CC2(O)
        public int GIPXVCZDXUYTPQ { get; set; } // Formula:C15H16O7, Mass:308.09, SMILES:[O-]C2=CC(O)=CC3=[O+]C=C(OC1OC(C)CC(O)C1(O))C=C23
        public int JNCNZITXCFDRQT { get; set; } // Formula:C20H36O2, Mass:308.272, SMILES:CCCCC\C=C/C\C=C/CCCCCC(O)CCCC=O
        public int XSXIVVZCUAHUJO { get; set; } // Formula:C20H36O2, Mass:308.272, SMILES:CCCCC\C=C/C\C=C/CCCCCCCCCC(O)=O
        public int ZPLPTXJQMQAZPW { get; set; } // Formula:C21H40O, Mass:308.308, SMILES:CCCCCCCCC\C=C/CCCCCCCCCC=O
        public int WZQIRMNZNKYTMX { get; set; } // Formula:C15H18O7, Mass:310.105, SMILES:O=C(OCC1OCC(O)C(O)C1(O))C=CC2=CC=C(O)C=C2
        public int UZIKLNYKVUKZQZ { get; set; } // Formula:C12H22O9, Mass:310.126, SMILES:OC2COC(COC1OC(C)C(O)C(O)C1(O))C(O)C2(O)
        public int WXEKRRWEMCHREJ { get; set; } // Formula:C12H22O9, Mass:310.126, SMILES:OCC2OCC(OC1OC(C)C(O)C(O)C1(O))C(O)C2(O)
        public int JGCKLVYWXXUJPX { get; set; } // Formula:C16H22O6, Mass:310.142, SMILES:OC2C(OC(=CC=1C=CC=C(OC)C=1)C)OC(C)C(O)C2(O)
        public int IIRPAOCBGJCCAW { get; set; } // Formula:C19H34O3, Mass:310.251, SMILES:CCCCCC\C=C/C\C=C/CCCC(O)CCCC(O)=O
        public int BITHHVVYSMSWAG { get; set; } // Formula:C20H38O2, Mass:310.287, SMILES:CCCCCCCC\C=C/CCCCCCCCCC(O)=O
        public int MHIXDXZRIVKKHP { get; set; } // Formula:C20H38O2, Mass:310.287, SMILES:CCCCCCCC\C=C/CCCCCC(O)CCCC=O
        public int FJZCFGKQFDPNHS { get; set; } // Formula:C21H42O, Mass:310.324, SMILES:CCCCCCCCCCCCCCCCCCCCC=O
        public int VYYGXLYTVAMSDT { get; set; } // Formula:C18H16O5, Mass:312.1, SMILES:O=C1C=C(OC2=CC(OC)=C(C(O)=C12)CC)C3=CC=C(O)C=C3
        public int BUEBVQCTEJTADB { get; set; } // Formula:C11H20O10, Mass:312.106, SMILES:OCC2OC(O)C(OC1OCC(O)C(O)C1(O))C(O)C2(O)
        public int QYNRIDLOTGRNML { get; set; } // Formula:C11H20O10, Mass:312.106, SMILES:OC2OC(COC1OCC(O)C(O)C1(O))C(O)C(O)C2(O)
        public int OKFGGODLENCAOQ { get; set; } // Formula:C22H32O, Mass:312.245, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CCC=O
        public int NYGRZFWFEWFNMH { get; set; } // Formula:C19H36O3, Mass:312.266, SMILES:CCCCCCCCC\C=C/CCCC(O)CCCC(O)=O
        public int NJWDYOOPZZRUMD { get; set; } // Formula:C20H40O2, Mass:312.303, SMILES:CCCCCCCCCCCCCCCC(O)CCCC=O
        public int VKOBVWXKNCXXDE { get; set; } // Formula:C20H40O2, Mass:312.303, SMILES:CCCCCCCCCCCCCCCCCCCC(O)=O
        public int BJBUTJQYZDYRMJ { get; set; } // Formula:C17H14O6, Mass:314.079, SMILES:O=C1C(OC)=C(OC2=CC(OC)=CC(O)=C12)C3=CC=C(O)C=C3
        public int JDMXMMBASFOTIF { get; set; } // Formula:C17H14O6, Mass:314.079, SMILES:COC1=CC(=CC=C1O)C1=CC(=O)C2=C(OC)C=C(O)C=C2O1
        public int VOOFPOMXNLNEOF { get; set; } // Formula:C17H14O6, Mass:314.079, SMILES:COC1=CC=C(C=C1)C1=COC2=CC(O)=C(OC)C(O)=C2C1=O
        public int FVEOHTQVMGVKNQ { get; set; } // Formula:C14H18O8, Mass:314.1, SMILES:O=CC=1C(O)=CC(OC)=C(C=1(O))C2OC(CO)C(O)C(O)C2
        public int XCDIKQSRUWRPHH { get; set; } // Formula:C22H34O, Mass:314.261, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CCCCCC=O
        public int UHBRCMNTMJCASZ { get; set; } // Formula:C19H38O3, Mass:314.282, SMILES:CCCCCCCCCCCCCCC(O)CCCC(O)=O
        public int DAEBTLQZOWXOBW { get; set; } // Formula:C16H12O7, Mass:316.058, SMILES:O=C1C(OC)=C(OC2=CC(O)=C(O)C(O)=C12)C3=CC=C(O)C=C3
        public int IZQSVPBOUDKVDZ { get; set; } // Formula:C16H12O7, Mass:316.058, SMILES:O=C1C(O)=C(OC2=CC(O)=CC(O)=C12)C=3C=CC(O)=C(OC)C=3
        public int OGQSUSFDBWGFFJ { get; set; } // Formula:C16H12O7, Mass:316.058, SMILES:O=C1C(O)=C(OC2=CC(O)=C(OC)C(O)=C12)C3=CC=C(O)C=C3
        public int WEPBGSIAWZTEJR { get; set; } // Formula:C16H12O7, Mass:316.058, SMILES:O=C1C(OC)=C(OC2=CC(O)=CC(O)=C12)C=3C=CC(O)=C(O)C=3
        public int AIVADCLCYQKORG { get; set; } // Formula:C13H16O9, Mass:316.079, SMILES:O=CC=1C(O)=CC(O)=C(C=1(O))C2OC(CO)C(O)C(O)C2(O)
        public int YVORLBJZJIGULG { get; set; } // Formula:C13H16O9, Mass:316.079, SMILES:O=C(C1=C(O)C=C(O)C=C1(O))COC2OCC(O)C(O)C2(O)
        public int ZTCZPSUNIDBMPJ { get; set; } // Formula:C13H16O9, Mass:316.079, SMILES:O=CC=2C(O)=CC(OC1OC(CO)C(O)C(O)C1(O))=CC=2(O)
        public int BLEIOPIOOBZCEQ { get; set; } // Formula:C22H36O, Mass:316.277, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCCCCCC=O
        public int AFOLOMGWVXKIQL { get; set; } // Formula:C16H13O7, Mass:317.066, SMILES:OC=1C=C(O)C2=CC(O)=C([O+]=C2(C=1))C3=CC(O)=C(O)C(OC)=C3
        public int IKMDFBPHZNJCSN { get; set; } // Formula:C15H10O8, Mass:318.038, SMILES:O=C1C(O)=C(OC2=CC(O)=CC(O)=C12)C3=CC(O)=C(O)C(O)=C3
        public int RWZRMLYARYCZJA { get; set; } // Formula:C9H19O10P, Mass:318.072, SMILES:O=P(O)(OCC(O)C)OC1C(O)C(O)C(O)C(O)C1(O)
        public int KONMRYXTSAJFFW { get; set; } // Formula:C20H30O3, Mass:318.219, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C(O)\C=C/CCCC(O)=O
        public int KFSQTHOUPXNGQR { get; set; } // Formula:C22H38O, Mass:318.292, SMILES:O(C=1C=CC=C(C=1)CCCCCCCCCCCCCCC)C
        public int OHNHBFLLYAQIOG { get; set; } // Formula:C22H38O, Mass:318.292, SMILES:CCCCC\C=C/C\C=C/C\C=C/CCCCCCCCC=O
        public int TZXLXHWAZVVUPY { get; set; } // Formula:C8H19NO8P2, Mass:319.059, SMILES:O=C(NCC)C(O)C(C)(C)COP(=O)(O)OP(=O)O
        public int XJSRKJAHJGCPGC { get; set; } // Formula:C6HF13, Mass:319.987, SMILES:FC(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F
        public int UXFYKGHOTJIZHF { get; set; } // Formula:C11H17N2O7P, Mass:320.077, SMILES:O=C(N)C1=CN(C=CC1)C2OC(COP(=O)O)C(O)C2(O)
        public int PLUCAVLERAWBGO { get; set; } // Formula:C20H32O3, Mass:320.235, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCC(O)CCCC(O)=O
        public int GDFMOYUXMKMYJF { get; set; } // Formula:C22H40O, Mass:320.308, SMILES:CCCCC\C=C/C\C=C/CCCCCCCCCCCC=O
        public int AEJYSWYTTROYBT { get; set; } // Formula:C9H15N4O7P, Mass:322.068, SMILES:O=P(O)(O)OC1C(O)C(OC1(CO))NC=2N=CN=C(N)C=2
        public int MFGADEKBMXNEKF { get; set; } // Formula:C20H34O3, Mass:322.251, SMILES:CC\C=C/C\C=C/C\C=C/CCCCCC(O)CCCC(O)=O
        public int IXGRSGKLNDEHCM { get; set; } // Formula:C21H38O2, Mass:322.287, SMILES:CCCCCC\C=C/C\C=C/CCCCCC(O)CCCC=O
        public int SGRSBJJDICHPHB { get; set; } // Formula:C21H38O2, Mass:322.287, SMILES:CCCCCC\C=C/C\C=C/CCCCCCCCCC(O)=O
        public int YBXUDVOPLYWFSH { get; set; } // Formula:C22H42O, Mass:322.324, SMILES:CCCCCCCC\C=C/CCCCCCCCCCCC=O
        public int KUKQJNQKJWEVCE { get; set; } // Formula:C19H21N3S, Mass:323.146, SMILES:N=2C1=CC=CC=C1SC=4C=CC=CC=4(C=2N3CCN(CC)CC3)
        public int DJJCXFVJDGTHFX { get; set; } // Formula:C9H13N2O9P, Mass:324.036, SMILES:O=C1C=CN(C(=O)N1)C2OC(COP(=O)(O)O)C(O)C2(O)
        public int DEAPPYHSCUWMEW { get; set; } // Formula:C20H36O3, Mass:324.266, SMILES:CCCCC\C=C/C\C=C/CCCCCC(O)CCCC(O)=O
        public int AQAQOOGENKQRTR { get; set; } // Formula:C21H40O2, Mass:324.303, SMILES:CCCCCCCCC\C=C/CCCCCC(O)CCCC=O
        public int HMQNHJJIYIZSOU { get; set; } // Formula:C21H40O2, Mass:324.303, SMILES:CCCCCCCCC\C=C/CCCCCCCCCC(O)=O
        public int ULCXRAFXRZTNRO { get; set; } // Formula:C22H44O, Mass:324.339, SMILES:CCCCCCCCCCCCCCCCCCCCCC=O
        public int KJPUBQHCMQSSNA { get; set; } // Formula:C12H23NO5S2, Mass:325.102, SMILES:N=C(CCCCSC)SC1OC(CO)C(O)C(O)C1(O)
        public int CLCFCUQOMMGQKP { get; set; } // Formula:C12H22O10, Mass:326.121, SMILES:OCC2OCC(OC1OC(CO)C(O)C(O)C1(O))C(O)C2(O)
        public int FHWWVCXCPVVWII { get; set; } // Formula:C12H22O10, Mass:326.121, SMILES:OCC2OC(OC)C(OC1OCC(O)C(O)C1(O))C(O)C2(O)
        public int GJWZUOOUQNJKQC { get; set; } // Formula:C12H22O10, Mass:326.121, SMILES:OCC2OC(OC1C(O)C(OC1(CO))CO)C(O)C(O)C2(O)
        public int LTHOGZQBHZQCGR { get; set; } // Formula:C12H22O10, Mass:326.121, SMILES:OCC2OC(O)C(O)C(O)C2(OC1OC(C)C(O)C(O)C1(O))
        public int OUAYUOJVAXWVFB { get; set; } // Formula:C12H22O10, Mass:326.121, SMILES:OCC2OC(OCC1OCC(O)C(O)C1(O))C(O)C(O)C2(O)
        public int OVVGHDNPYGTYIT { get; set; } // Formula:C12H22O10, Mass:326.121, SMILES:OC2OC(COC1OC(C)C(O)C(O)C1(O))C(O)C(O)C2(O)
        public int DIVCIRCXJNMPGK { get; set; } // Formula:C20H38O3, Mass:326.282, SMILES:CCCCCCCC\C=C/CCCCCC(O)CCCC(O)=O
        public int AHMWOMXSNXJDGM { get; set; } // Formula:C21H42O2, Mass:326.318, SMILES:CCCCCCCCCCCCCCCCC(O)CCCC=O
        public int CKDDRHZIAZRDBW { get; set; } // Formula:C21H42O2, Mass:326.318, SMILES:CCCCCCCCCCCCCCCCCCCCC(O)=O
        public int LHUOUYOGBORMMH { get; set; } // Formula:C21H28O3, Mass:328.204, SMILES:O=CC3(O)(C(C)CC2C4CCC1=CC(=O)C=CC1(C)C4(CCC23(C)))
        public int MBMBGCFOFBJSGT { get; set; } // Formula:C22H32O2, Mass:328.24, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CCC(O)=O
        public int PHIOZXXTYNODED { get; set; } // Formula:C22H32O2, Mass:328.24, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/C(O)\C=C/CCC=O
        public int BGEGHPOGIYNAEZ { get; set; } // Formula:C20H40O3, Mass:328.298, SMILES:CCCCCCCCCCCCCCCC(O)CCCC(O)=O
        public int DDNPCXHBFYJXBJ { get; set; } // Formula:C17H14O7, Mass:330.074, SMILES:O=C1C(OC)=C(OC2=CC(O)=C(OC)C(O)=C12)C3=CC=C(O)C=C3
        public int HRGUSFBJBOKSML { get; set; } // Formula:C17H14O7, Mass:330.074, SMILES:COC1=CC(=CC(OC)=C1O)C1=CC(=O)C2=C(O)C=C(O)C=C2O1
        public int KWMAWXWUGIEVDG { get; set; } // Formula:C17H14O7, Mass:330.074, SMILES:O=C1C(O)=C(OC2=CC(OC)=C(OC)C(O)=C12)C3=CC=C(O)C=C3
        public int ARYSAKCPIBLSDO { get; set; } // Formula:C14H18O9, Mass:330.095, SMILES:O=C(C=2C(O)=CC(OC1OC(CO)C(O)C(O)C1(O))=CC=2(O))C
        public int IWMUXTZLTOTAQO { get; set; } // Formula:C14H18O9, Mass:330.095, SMILES:O=C(C=1C(O)=CC(O)=C(C=1(O))C2OC(CO)C(O)C(O)C2(O))C
        public int HRQBVNFDMJPOBV { get; set; } // Formula:C22H34O2, Mass:330.256, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CC(O)CCCC=O
        public int YUFFSWGQGVEMMI { get; set; } // Formula:C22H34O2, Mass:330.256, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CCCCCC(O)=O
        public int PLLPSKJIVDRMFI { get; set; } // Formula:C10H14N5O6P, Mass:331.068, SMILES:O=P(O)OCC3OC(N2C=NC=1C(=N)NC=NC=12)C(O)C3(O)
        public int XKFCKNDXVMFENB { get; set; } // Formula:C10H14N5O6P, Mass:331.068, SMILES:O=P(O)(O)OCC3OC(N2C=NC=1C(=NC=NC=12)N)C(O)C3
        public int KZMACGJDUUWFCH { get; set; } // Formula:C17H15O7, Mass:331.081, SMILES:OC=1C=C(O)C2=CC(O)=C([O+]=C2(C=1))C=3C=C(OC)C(O)=C(OC)C=3
        public int JEGVBRYEXSGSKC { get; set; } // Formula:C5H2F10O3S, Mass:331.956, SMILES:O=S(=O)(O)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)F
        public int DFAUVYAUUHAORK { get; set; } // Formula:C10H13N4O7P, Mass:332.052, SMILES:O=C3N=CNC1=C3(N=CN1C2OC(COP(=O)O)C(O)C2(O))
        public int IGFLWIHPPADNDL { get; set; } // Formula:C22H36O2, Mass:332.272, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCCCCCC(O)=O
        public int SCOXRLWYDFLQPP { get; set; } // Formula:C22H36O2, Mass:332.272, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCC(O)CCCC=O
        public int RBTVCRWROCDIOQ { get; set; } // Formula:C23H40O, Mass:332.308, SMILES:OC3CC2C(CCC1CCCCC12(C))C4CCC(C(C)CC)C34(C)
        public int RILVNGKQIULBOQ { get; set; } // Formula:C22H38O2, Mass:334.287, SMILES:CCCCC\C=C/C\C=C/C\C=C/CCCCCCCCC(O)=O
        public int YDJRNYWFRANPOL { get; set; } // Formula:C22H38O2, Mass:334.287, SMILES:CCCCC\C=C/C\C=C/C\C=C/CCCCC(O)CCCC=O
        public int LVDUPRJMLLNNGS { get; set; } // Formula:C16H16O8, Mass:336.085, SMILES:O=CC2=C(O)C(OC)=C(O)C=C2(OCC=1C=C(O)C(OC)=C(O)C=1)
        public int HVGRZDASOHMCSK { get; set; } // Formula:C22H40O2, Mass:336.303, SMILES:CCCCC\C=C/C\C=C/CCCCCCCCCCCC(O)=O
        public int UPMVPXJUQWHITN { get; set; } // Formula:C22H40O2, Mass:336.303, SMILES:CCCCC\C=C/C\C=C/CCCCCCCC(O)CCCC=O
        public int IFIWPTBMSNMDAR { get; set; } // Formula:C21H38O3, Mass:338.282, SMILES:CCCCCC\C=C/C\C=C/CCCCCC(O)CCCC(O)=O
        public int DPUOLQHDNGRHBS { get; set; } // Formula:C22H42O2, Mass:338.318, SMILES:CCCCCCCC\C=C/CCCCCCCCCCCC(O)=O
        public int JKCLGQXEZFEMHP { get; set; } // Formula:C22H42O2, Mass:338.318, SMILES:CCCCCCCC\C=C/CCCCCCCC(O)CCCC=O
        public int IALIDHPAWNTXOK { get; set; } // Formula:C23H46O, Mass:338.355, SMILES:CCCCCCCCCCCCCCCCCCCCCCC=O
        public int IGXZXBRGFRTKRI { get; set; } // Formula:C15H16O9, Mass:340.079, SMILES:O=C1C=COC2=C1C(O)=CC(O)=C2C3OC(CO)C(O)C(O)C3(O)
        public int XCVOCJFOQNDTNC { get; set; } // Formula:C15H16O9, Mass:340.079, SMILES:O=C2C(OC1OC(C)C(O)C(O)C1(O))=COC3=CC(O)=CC(O)=C23
        public int ODYJCLZVHOXGIZ { get; set; } // Formula:C13H24O10, Mass:340.137, SMILES:OC2C(OC)OC(COC1OC(C)C(O)C(O)C1(O))C(O)C2(O)
        public int XAWIBMFYJXZFOA { get; set; } // Formula:C21H40O3, Mass:340.298, SMILES:CCCCCCCCC\C=C/CCCCCC(O)CCCC(O)=O
        public int MKQYREVLMZYGEL { get; set; } // Formula:C22H44O2, Mass:340.334, SMILES:CCCCCCCCCCCCCCCCCC(O)CCCC=O
        public int UKMSUNONTOPOIO { get; set; } // Formula:C22H44O2, Mass:340.334, SMILES:CCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int CQZLVEWBMWVXBQ { get; set; } // Formula:C15H17O9, Mass:341.087, SMILES:OC=2C=C(O)C3=CC(OC1OC(CO)C(O)C(O)C1(O))=C[O+]=C3(C=2)
        public int HENJHBDLEUWCGF { get; set; } // Formula:C12H23NO6S2, Mass:341.097, SMILES:O=S(C)CCCCC(=N)SC1OC(CO)C(O)C(O)C1(O)
        public int PIIYZZRUYDYLOR { get; set; } // Formula:C15H18O9, Mass:342.095, SMILES:O=C2C3=C(O)C=C(OC1OC(CO)C(O)C(O)C1(O))C=C3(OCC2)
        public int DLRVVLDZNNYCBX { get; set; } // Formula:C12H22O11, Mass:342.116, SMILES:OCC2OC(OCC1OC(O)C(O)C(O)C1(O))C(O)C(O)C2(O)
        public int HIWPGCMGAMJNRG { get; set; } // Formula:C12H22O11, Mass:342.116, SMILES:OCC2OC(O)C(OC1OC(CO)C(O)C(O)C1(O))C(O)C2(O)
        public int BAIUBEVPLHSQCG { get; set; } // Formula:C21H42O3, Mass:342.313, SMILES:CCCCCCCCCCCCCCCCC(O)CCCC(O)=O
        public int HHRZYEXLGLQGKQ { get; set; } // Formula:C6H3Br3O2, Mass:343.768, SMILES:OC=1C=C(C(=C(C=1(O))Br)Br)Br
        public int YSXFFLGRZJWNFM { get; set; } // Formula:C18H16O7, Mass:344.09, SMILES:O=C1C(OC)=C(OC2=CC(OC)=C(OC)C(O)=C12)C3=CC=C(O)C=C3
        public int MDOJKSAAJAOYSQ { get; set; } // Formula:C21H28O4, Mass:344.199, SMILES:O=CC3(O)(C(C)CC2C4CCC1=CC(=O)C=CC1(C)C4(C(O)CC23(C)))
        public int FRUPIWUHZRKAOK { get; set; } // Formula:C22H32O3, Mass:344.235, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/C(O)\C=C/CCC(O)=O
        public int KSKDCCGYHFTZTC { get; set; } // Formula:C18H17O7, Mass:345.097, SMILES:OC=1C=C(O)C2=CC(OCOCC)=C([O+]=C2(C=1))C=3C=CC(O)=C(O)C=3
        public int KIGVXRGRNLQNNI { get; set; } // Formula:C17H14O8, Mass:346.069, SMILES:O=C1C(OC)=C(OC2=CC(O)=C(OC)C(O)=C12)C=3C=CC(O)=C(O)C=3
        public int YJDSHSSMZYNEPP { get; set; } // Formula:C17H14O8, Mass:346.069, SMILES:O=C1C(OCCO)=C(OC2=CC(O)=CC(O)=C12)C=3C=CC(O)=C(O)C=3
        public int KIBWUBXJNWEJNV { get; set; } // Formula:C14H18O10, Mass:346.09, SMILES:O=C(C=1C(O)=CC(O)=CC=1(O))COC2OC(CO)C(O)C(O)C2(O)
        public int ICCPODUZIIZNAR { get; set; } // Formula:C22H34O3, Mass:346.251, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/C\C=C/CC(O)CCCC(O)=O
        public int UDMBCSSLTHHNCD { get; set; } // Formula:C10H14N5O7P, Mass:347.063, SMILES:O=P(O)(O)OCC3OC(N2C=NC=1C(=NC=NC=12)N)C(O)C3(O)
        public int ADFWQBGTDJIESE { get; set; } // Formula:C22H36O3, Mass:348.266, SMILES:O=C(O)C=1C(O)=CC=CC=1CCCCCCCCCCCCCCC
        public int HOBYCVCGJUBHHN { get; set; } // Formula:C22H36O3, Mass:348.266, SMILES:CC\C=C/C\C=C/C\C=C/C\C=C/CCCCC(O)CCCC(O)=O
        public int WJXVLZCOWIIKOS { get; set; } // Formula:C23H40O2, Mass:348.303, SMILES:OC4CCC2(C)(C(CCC1C3CCC(C(C)CC)C3(C)(C(O)CC12))C4)
        public int QULYEZPJHHROQJ { get; set; } // Formula:C17H18O8, Mass:350.1, SMILES:O=CC2=C(O)C(OC)=C(O)C=C2(OCC=1C=C(O)C(OC)=C(OC)C=1)
        public int DMYHHDOBDAMXAT { get; set; } // Formula:C22H38O3, Mass:350.282, SMILES:CCCCC\C=C/C\C=C/C\C=C/CCCCC(O)CCCC(O)=O
        public int IIAGTUAVIBVHMZ { get; set; } // Formula:C22H40O3, Mass:352.298, SMILES:CCCCC\C=C/C\C=C/CCCCCCCC(O)CCCC(O)=O
        public int HGINZVDZNQJVLQ { get; set; } // Formula:C24H48O, Mass:352.371, SMILES:CCCCCCCCCCCCCCCCCCCCCCCC=O
        public int DNYRKEWXPBSYOS { get; set; } // Formula:C12H18O12, Mass:354.08, SMILES:O=C(O)C2OC(O)C(OC1OC(C(=O)O)C(O)C(O)C1(O))C(O)C2
        public int SFUQBVYPRJLLKE { get; set; } // Formula:C12H18O12, Mass:354.08, SMILES:O=C(O)C2OCC(OC1OC(C(=O)O)C(O)C(O)C1(O))C(O)C2(O)
        public int AYCQQDJXMCSMAV { get; set; } // Formula:C22H42O3, Mass:354.313, SMILES:CCCCCCCC\C=C/CCCCCCCC(O)CCCC(O)=O
        public int XEZVDURJDFGERA { get; set; } // Formula:C23H46O2, Mass:354.35, SMILES:CCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int AMRLFIVZLCQZPG { get; set; } // Formula:C22H44O3, Mass:356.329, SMILES:CCCCCCCCCCCCCCCCCC(O)CCCC(O)=O
        public int XUWTZJRCCPNNJR { get; set; } // Formula:C18H16O8, Mass:360.085, SMILES:O=C1C(OC)=C(OC2=CC(O)=C(OC)C(O)=C12)C3=CC=C(O)C(OC)=C3
        public int ZUBFWGOFMRYWNS { get; set; } // Formula:C17H14O9, Mass:362.064, SMILES:O=C1C(OC)=C(OC2=CC(O)=C(OC)C(O)=C12)C=3C=C(O)C(O)=C(O)C=3
        public int RQFCJASXJCIDSX { get; set; } // Formula:C10H14N5O8P, Mass:363.058, SMILES:O=C3N=C(N)NC1=C3(N=CN1C2OC(COP(=O)(O)O)C(O)C2(O))
        public int HAGKFWXVDSAFHB { get; set; } // Formula:C25H50O, Mass:366.386, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int TYTGMCXGFOWKTG { get; set; } // Formula:C14H24O11, Mass:368.132, SMILES:O=CCOC2OC(COC1OC(C)C(O)C(O)C1(O))C(O)C(O)C2(O)
        public int QZZGJDVWLFXDLK { get; set; } // Formula:C24H48O2, Mass:368.365, SMILES:CCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int HBZVXKDQRIQMCW { get; set; } // Formula:C7HF15, Mass:369.984, SMILES:FC(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)F
        public int OGEXYUOJQNKSAL { get; set; } // Formula:C15H14O11, Mass:370.054, SMILES:O=C(O)C3OC(OC1=COC2=CC(O)=CC(O)=C2(C1(=O)))C(O)C(O)C3(O)
        public int IMNADAQGVSDVMI { get; set; } // Formula:C12H18O13, Mass:370.075, SMILES:O=C(O)C2OC(O)C(OC1OC(C(=O)O)C(O)C(O)C1(O))C(O)C2(O)
        public int AWHCMAHYNRUGIC { get; set; } // Formula:C14H26O11, Mass:370.148, SMILES:OCC2OC(OCC1OC(OCC)C(O)C(O)C1(O))C(O)C(O)C2(O)
        public int DTGDZMYNKLTSKC { get; set; } // Formula:C27H46, Mass:370.36, SMILES:C2=C1CCCCC1(C)C3CCC4(C)(C(CCC4(C3(C2)))C(C)CCCC(C)C)
        public int FPFLMCPZDZURSF { get; set; } // Formula:C18H16O9, Mass:376.079, SMILES:O=C1C(OC)=C(OC2=CC(O)=C(OC)C(O)=C12)C=3C=C(O)C(OC)=C(O)C=3
        public int QAXXQMIHMLTJQI { get; set; } // Formula:C26H52O, Mass:380.402, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int SKZFCJKAAJTRQC { get; set; } // Formula:C6H2F12O3S, Mass:381.953, SMILES:O=S(=O)(O)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)(F)C(F)F
        public int MWMPEAHGUXCSMY { get; set; } // Formula:C25H50O2, Mass:382.381, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int WVPWRXOKOWTKIB { get; set; } // Formula:C15H28O11, Mass:384.163, SMILES:OCC2OC(OCC1OC(OCCC)C(O)C(O)C1(O))C(O)C(O)C2(O)
        public int HKKMGGKMNHKQMC { get; set; } // Formula:C27H44O, Mass:384.339, SMILES:CC12CCC=C1C1CCC3C4(C)CCC(O)C(C)(C)C4CCC3(C)C1(C)CC2
        public int HVYWMOMLDIMFJA { get; set; } // Formula:C27H46O, Mass:386.355, SMILES:CC(C)CCCC(C)C1CCC2C3CC=C4CC(O)CCC4(C)C3CCC12C
        public int WTTGIVJDHDPLCL { get; set; } // Formula:C19H18O9, Mass:390.095, SMILES:O=C1C(OC)=C(OC2=CC(O)=C(OC)C(O)=C12)C=3C=C(O)C(OC)=C(OC)C=3
        public int JYJAXNKEZJHGMJ { get; set; } // Formula:C6H3Br2IO2, Mass:391.754, SMILES:OC1=C(O)C(=C(C=C1Br)Br)I
        public int RKGHLTUWZNNHCL { get; set; } // Formula:C24H42O4, Mass:394.308, SMILES:OC(C)C4CCC2(C)(C4(C(O)CC1C3(C)(CCC(O)C(C)(C)C3(C(O)CC12(C)))))
        public int UEAAOADMOOTTQM { get; set; } // Formula:C27H54O, Mass:394.417, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int XMHIUKTWLZUKEX { get; set; } // Formula:C26H52O2, Mass:396.397, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int JVKYZPBMZPJNAJ { get; set; } // Formula:C27H43NO, Mass:397.334, SMILES:CC1C2CCC(C)CN2C2CC3C4CC=C5CC(O)CCC5(C)C4CCC3(C)C12
        public int BHYGFFPNUSKIOG { get; set; } // Formula:C23H30N2O4, Mass:398.221, SMILES:O=C(OC)C3C(OC)CCC4CN2CCC=1C=5C=CC(OC)=CC=5(NC=1C2CC34)
        public int JALVTHFTYRPDMB { get; set; } // Formula:C27H45NO, Mass:399.35, SMILES:CC1C2CCC(C)CN2C2CC3C4CCC5CC(O)CCC5(C)C4CCC3(C)C12
        public int ZIIVVHSRXUVJGR { get; set; } // Formula:C27H44O2, Mass:400.334, SMILES:CC(CO)CCC1=C(C)C2C(CC3C4CCC5CCCCC5(C)C4CCC23C)O1
        public int SGNBVLSWZMBQTH { get; set; } // Formula:C28H48O, Mass:400.371, SMILES:CC(C)C(C)CCC(C)C1CCC2C3CC=C4CC(O)CCC4(C)C3CCC12C
        public int LVXORIXZNUNHGQ { get; set; } // Formula:C28H56O, Mass:408.433, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int VXZBFBRLRNDJCS { get; set; } // Formula:C27H54O2, Mass:410.412, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int XLIAWOGMZGYPCO { get; set; } // Formula:C10H15N5O9P2, Mass:411.035, SMILES:O=P(O)OCC3OC(N2C=NC=1C(=NC=NC=12)N)C(O)C3(OP(=O)(O)O)
        public int HCXVJBMSMIARIN { get; set; } // Formula:C29H48O, Mass:412.371, SMILES:CCC(C=CC(C)C1CCC2C3CC=C4CC(O)CCC4(C)C3CCC12C)C(C)C
        public int KWVISVAMQJWJSZ { get; set; } // Formula:C27H43NO2, Mass:413.329, SMILES:CC1C2C(CC3C4CC=C5CC(O)CCC5(C)C4CCC23C)OC11CCC(C)CN1
        public int RFIYLZGMGGONQR { get; set; } // Formula:C27H43NO2, Mass:413.329, SMILES:CC1C2C(O)CC(C)CN2C2CC3C4CC=C5CC(O)CCC5(C)C4CCC3(C)C12
        public int MDJQWFFIUHUJSB { get; set; } // Formula:C23H30N2O5, Mass:414.215, SMILES:O=C(OC)C3C(OC)C(O)CC4CN2CCC=1C=5C=CC(OC)=CC=5(NC=1C2CC34)
        public int GFGFAMNBRXAQGB { get; set; } // Formula:C27H42O3, Mass:414.313, SMILES:CC1C2C(CC3C4CCC5CC(O)CCC5(C)C4CCC23C)OC11CCC(=C)CO1
        public int WQLVFSAGQJTQCK { get; set; } // Formula:C27H42O3, Mass:414.313, SMILES:CC1C2C(CC3C4CC=C5CC(O)CCC5(C)C4CCC23C)OC11CCC(C)CO1
        public int KZJWDPNRJALLNS { get; set; } // Formula:C29H50O, Mass:414.386, SMILES:CCC(CCC(C)C1CCC2C3CC=C4CC(O)CCC4(C)C3CCC12C)C(C)C
        public int ATEWGTOGFJMCPH { get; set; } // Formula:C27H45NO2, Mass:415.345, SMILES:CC1C2C(O)CC(C)CN2C2CC3C4CCC5CC(O)CCC5(C)C4CCC3(C)C12
        public int XYNPYHXGMWJBLV { get; set; } // Formula:C27H45NO2, Mass:415.345, SMILES:CC1C2C(CC3C4CCC5CC(O)CCC5(C)C4CCC23C)OC11CCC(C)CN1
        public int GMBQZIIUCVWOCD { get; set; } // Formula:C27H44O3, Mass:416.329, SMILES:CC1C2C(CC3C4CCC5CC(O)CCC5(C)C4CCC23C)OC11CCC(C)CO1
        public int IQDKIMJGXXRZGR { get; set; } // Formula:C27H44O3, Mass:416.329, SMILES:CC(CO)CCC1=C(C)C2C(CC3C4CCC5CC(O)CCC5(C)C4CCC23C)O1
        public int BXQPHIBPVVBVAB { get; set; } // Formula:C17H24O12, Mass:420.127, SMILES:OC1=CC(O)=C(C(O)=C1C2OCC(O)C(O)C2(O))C3OC(CO)C(O)C(O)C3(O)
        public int AUSHGUYKHVWAKG { get; set; } // Formula:C29H58O, Mass:422.449, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int UTOPWMOLSKOLTQ { get; set; } // Formula:C28H56O2, Mass:424.428, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int SIGYXFJSXPUADL { get; set; } // Formula:C10H20O14P2, Mass:426.033, SMILES:O=P(O)(OCC1OCC(O)C1(O))OP(=O)(O)OC2OCC(O)C(O)C2(O)
        public int CAHGCLMLTWQZNJ { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC(CCC=C(C)C)C1CCC2(C)C3=C(CCC12C)C1(C)CCC(O)C(C)(C)C1CC3
        public int DICCPNLDOZNSML { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC(CCC=C(C)C)C1CCC2(C)C3=CCC4C(C)(C)C(O)CCC4(C)C3CCC12C
        public int JFSHUTJDVKUMTJ { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC1(C)CCC2(C)CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(C)C5CCC34C)C2C1
        public int MQYXUWHLBZFQQO { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC(=C)C1CCC2(C)CCC3(C)C(CCC4C5(C)CCC(O)C(C)(C)C5CCC34C)C12
        public int NGFFRJBGMSPDMS { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC1C2C3CCC4C5(C)CCC(O)C(C)(C)C5CCC4(C)C3(C)CCC2(C)CC=C1C
        public int ONQRKEUAIJMULO { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC(CCC=C(C)C)C1CCC2(C)C3CCC4C5(CC35CCC12C)CCC(O)C4(C)C
        public int QMUXVPRGNJLGRT { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC1(C)CCC2(C)CCC3(C)C(CCC4C5(C)CCC(O)C(C)(C)C5CCC34C)C2=C1
        public int TZVDWGXUGGUMCE { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC1CCC2(C)CCC3(C)C4=CCC5C(C)(C)C(O)CCC5(C)C4CCC3(C)C2C1C
        public int XWMMEBCFHUKHEX { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC1C2C3CCC4C5(C)CCC(O)C(C)(C)C5CCC4(C)C3(C)CCC2(C)CCC1=C
        public int ZDFUASMRJUVZJP { get; set; } // Formula:C30H50O, Mass:426.386, SMILES:CC1(C)CCC2(C)CCC3(C)C4=CCC5C(C)(C)C(O)CCC5(C)C4CCC3(C)C2C1
        public int QMQIQBOGXYYATH { get; set; } // Formula:C27H42O4, Mass:430.308, SMILES:CC1C2C(CC3C4CC=C5CC(O)CC(O)C5(C)C4CCC23C)OC11CCC(C)CO1
        public int QOLRLLFJMZLYQJ { get; set; } // Formula:C27H42O4, Mass:430.308, SMILES:CC1C2C(CC3C4CCC5CC(O)CCC5(C)C4CC(=O)C23C)OC11CCC(C)CO1
        public int SGEWCQFRYRRZDC { get; set; } // Formula:C21H20O10, Mass:432.106, SMILES:O=C1C=C(OC=2C1=C(O)C=C(O)C=2C3OC(CO)C(O)C(O)C3(O))C4=CC=C(O)C=C4
        public int HHNBHWPLXYYKCZ { get; set; } // Formula:C27H44O4, Mass:432.324, SMILES:CC(CO)CCC1(O)OC2CC3C4CC=C5CC(O)CCC5(C)C4CCC3(C)C2C1C
        public int IIASKQTXRIMIGU { get; set; } // Formula:C27H44O4, Mass:432.324, SMILES:CC1C2(CCC(C)CO2)OC2CC3C4CCC5CC(O)CCC5(C)C4CCC3(C)C12O
        public int PZNPHSFXILSZTM { get; set; } // Formula:C27H44O4, Mass:432.324, SMILES:CC1C2C(CC3C4CC(O)C5CC(O)CCC5(C)C4CCC23C)OC11CCC(C)CO1
        public int RCWQRFAOSIMYAG { get; set; } // Formula:C18H26O12, Mass:434.142, SMILES:OC=3C=C(O)C=C(OC2OC(CO)C(O)C(O)C2(OC1OC(C)C(O)C(O)C1(O)))C=3
        public int YCNDWQCUDMBHPI { get; set; } // Formula:C18H26O12, Mass:434.142, SMILES:OC=3C=C(O)C=C(OC2OC(COC1OC(C)C(O)C(O)C1(O))C(O)C(O)C2(O))C=3
        public int BLAWZIKKZHLPBD { get; set; } // Formula:C27H46O4, Mass:434.34, SMILES:CC(CO)CCC1(O)OC2CC3C4CCC5CC(O)CCC5(C)C4CCC3(C)C2C1C
        public int CGNVIRPGBUXJES { get; set; } // Formula:C30H60O, Mass:436.464, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int IHEJEKZAKSNRLY { get; set; } // Formula:C29H58O2, Mass:438.444, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int VOTYPVGLIYAPMP { get; set; } // Formula:C11H22O14P2, Mass:440.048, SMILES:O=P(O)(OCC1OCC(O)C1(O))OP(=O)(O)OC2OC(C)C(O)C(O)C2(O)
        public int NTWLPZMPTFQYQI { get; set; } // Formula:C30H50O2, Mass:442.381, SMILES:CC1(C)CCC2(C)CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(CO)C5CCC34C)C2C1
        public int PSZDOEIIIJFCFE { get; set; } // Formula:C30H50O2, Mass:442.381, SMILES:CC1(C)CCC2(CO)CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(C)C5CCC34C)C2C1
        public int ZEGUWBQDYDXBNS { get; set; } // Formula:C30H50O2, Mass:442.381, SMILES:CC1(C)CC(O)C2(C)CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(C)C5CCC34C)C2C1
        public int YWWQQPDLTAKFSH { get; set; } // Formula:C27H40O5, Mass:444.288, SMILES:CC1C2(CCC(C)CO2)OC2CC3C4CCC5=CC(=O)CCC5(C)C4CC(O)C3(C)C12O
        public int ZHIBERYBFVVFNK { get; set; } // Formula:C28H46O4, Mass:446.34, SMILES:COC1(CCC(C)CO)OC2CC3C4CC=C5CC(O)CCC5(C)C4CCC3(C)C2C1C
        public int XGSSLVNRUAYTBR { get; set; } // Formula:C28H48O4, Mass:448.355, SMILES:COC1(CCC(C)CO)OC2CC3C4CCC5CC(O)CCC5(C)C4CCC3(C)C2C1C
        public int RKWHWFONKJEUEF { get; set; } // Formula:C21H21O11, Mass:449.108, SMILES:OC=2C=C(O)C3=CC(OC1OC(CO)C(O)C(O)C1(O))=C([O+]=C3(C=2))C=4C=CC(O)=C(O)C=4
        public int WBCFUJSINLYPNY { get; set; } // Formula:C31H62O, Mass:450.48, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int VHOCUJPBKOZGJD { get; set; } // Formula:C30H60O2, Mass:452.459, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int ZZYWILWDFCFHLX { get; set; } // Formula:C29H45NO3, Mass:455.34, SMILES:CC1C2C(CC(C)CN2C2CC3C4CC=C5CC(O)CCC5(C)C4CCC3(C)C12)OC(C)=O
        public int ZZAHNEVHQYWAEU { get; set; } // Formula:C11H22O15P2, Mass:456.043, SMILES:O=P(O)(OCC1OCC(O)C1(O))OP(=O)(O)OC2OC(CO)C(O)C(O)C2(O)
        public int UIIWEQMFARGUFG { get; set; } // Formula:C18H32O13, Mass:456.184, SMILES:OC3COC(COC2OC(C)C(O)C(OC1OC(C)C(O)C(O)C1(O))C2(O))C(O)C3(O)
        public int FNRBOAGVUNHDIL { get; set; } // Formula:C30H48O3, Mass:456.36, SMILES:CC1(C)CC2C3=CCC4C5(C)CCC(O)C(C)(CO)C5CCC4(C)C3(C)CCC2(C)C(=O)C1
        public int JZFSMVXQUWRSIW { get; set; } // Formula:C30H48O3, Mass:456.36, SMILES:CC1(C)C(O)CCC2(C)C1CCC1(C)C2CC=C2C3CC(C)(CCC3(C)CCC12C)C(O)=O
        public int MIJYXULNPSFWEK { get; set; } // Formula:C30H48O3, Mass:456.36, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(C)C5CCC34C)C2C1)C(O)=O
        public int CLRSTYUKOZOYFN { get; set; } // Formula:C29H47NO3, Mass:457.356, SMILES:CC1C2C(CC(C)CN2C2CC3C4CCC5CC(O)CCC5(C)C4CCC3(C)C12)OC(C)=O
        public int YOQAQNKGFOLRGT { get; set; } // Formula:C30H50O3, Mass:458.376, SMILES:CC1(C)CC(O)C2(C)CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(CO)C5CCC34C)C2C1
        public int BMCWHUJLFHECDG { get; set; } // Formula:C19H26O13, Mass:462.137, SMILES:O=CC=3C(O)=CC(OC2OC(COC1OC(C)C(O)C(O)C1(O))C(O)C(O)C2(O))=CC=3(O)
        public int IZHLNVOWIUSANQ { get; set; } // Formula:C19H26O13, Mass:462.137, SMILES:O=CC=3C(O)=CC(OC2OC(CO)C(O)C(O)C2(OC1OC(C)C(O)C(O)C1(O)))=CC=3(O)
        public int NNZVKPZICXRDJI { get; set; } // Formula:C32H64O, Mass:464.496, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int ONLMUMPTRGEPCH { get; set; } // Formula:C31H62O2, Mass:466.475, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int RPPSKUJCSABBGU { get; set; } // Formula:C11H20O16P2, Mass:470.023, SMILES:O=C(O)C2OC(OP(=O)(O)OP(=O)(O)OCC1OCC(O)C1(O))C(O)C(O)C2(O)
        public int MPDGHEJMBKOTSU { get; set; } // Formula:C30H46O4, Mass:470.34, SMILES:CC1(C)C(O)CCC2(C)C1CCC1(C)C2C(=O)C=C2C3CC(C)(CCC3(C)CCC12C)C(O)=O
        public int QMHCWDVPABYZMC { get; set; } // Formula:C30H46O4, Mass:470.34, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(C=O)C5CCC34C)C2C1)C(O)=O
        public int TXIGVGWKASKMAT { get; set; } // Formula:C18H32O14, Mass:472.179, SMILES:OCC3OC(OCC1OCC(O)C(O)C1(O))C(O)C(O)C3(OC2OC(C)C(O)C(O)C2(O))
        public int MDZKJHQSJHYOHJ { get; set; } // Formula:C30H48O4, Mass:472.355, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CC(O)C(O)C(C)(C)C5CCC34C)C2C1)C(O)=O
        public int PGOYMURMZNDHNS { get; set; } // Formula:C30H48O4, Mass:472.355, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(CO)C5CCC34C)C2C1)C(O)=O
        public int CDDWAYFUFNQLRZ { get; set; } // Formula:C30H50O4, Mass:474.371, SMILES:CC1(C)CC2C3=CCC4C5(C)CCC(O)C(C)(CO)C5CCC4(C)C3(C)CCC2(C)C(O)C1O
        public int HAJRFFOSWOEITM { get; set; } // Formula:C20H28O13, Mass:476.153, SMILES:O=C(C=3C(O)=CC(OC2OC(CO)C(O)C(O)C2(OC1OC(C)C(O)C(O)C1(O)))=CC=3(O))C
        public int VYYXQADWBBPULR { get; set; } // Formula:C20H28O13, Mass:476.153, SMILES:O=C(C=3C(O)=CC(OC2OC(COC1OC(C)C(O)C(O)C1(O))C(O)C(O)C2(O))=CC=3(O))C
        public int FPPJXOQMWRDTCG { get; set; } // Formula:C33H66O, Mass:478.511, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int ICAIHSUWWZJGHD { get; set; } // Formula:C32H64O2, Mass:480.491, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int GSEPOEIKWTXTHS { get; set; } // Formula:C30H46O5, Mass:486.335, SMILES:CC12CCC(C)(CC1C1=CC(=O)C3C4(C)CCC(O)C(C)(CO)C4CCC3(C)C1(C)CC2)C(O)=O
        public int PAIBKVQNJKUVCE { get; set; } // Formula:C30H46O5, Mass:486.335, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CCC(O)C(C)(C5CCC34C)C(O)=O)C2C1)C(O)=O
        public int RCOKCENREZHIEA { get; set; } // Formula:C30H46O5, Mass:486.335, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CC(O)C(O)C(C)(C=O)C5CCC34C)C2C1)C(O)=O
        public int HORDMQGYQIDHMI { get; set; } // Formula:C21H28O13, Mass:488.153, SMILES:O=C3C4=C(O)C=C(OC2OC(CO)C(O)C(O)C2(OC1OC(C)C(O)C(O)C1(O)))C=C4(OCC3)
        public int RWNHLTKFBKYDOJ { get; set; } // Formula:C30H48O5, Mass:488.35, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CC(O)C(O)C(C)(CO)C5CCC34C)C2C1)C(O)=O
        public int FJZMHTUXQXEAPK { get; set; } // Formula:C29H46O6, Mass:490.329, SMILES:COC1(CCC2(C)C(CCC3C4CC5OC6(CCC(C)CO6)C(C)C5(O)C4(C)C(=O)CC23)C1)OC
        public int ZSRZISBVEUGQRU { get; set; } // Formula:C34H68O, Mass:492.527, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int HQRWEDFDJHDPJC { get; set; } // Formula:C33H66O2, Mass:494.506, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int LJNXEWZPPMBSJJ { get; set; } // Formula:C21H26O14, Mass:502.132, SMILES:O=C3C(OC2OC(COC1OC(C)C(O)C(O)C1(O))C(O)C(O)C2(O))=COC4=CC(O)=CC(O)=C34
        public int IDGXIXSKISLYAC { get; set; } // Formula:C30H46O6, Mass:502.329, SMILES:CC1(C)CCC2(CCC3(C)C(=CCC4C5(C)CC(O)C(O)C(C)(C5CCC34C)C(O)=O)C2C1)C(O)=O
        public int AAWVBAITTDKDJP { get; set; } // Formula:C35H70O, Mass:506.543, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int UTGPYHWDXYRYGT { get; set; } // Formula:C34H68O2, Mass:508.522, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int FJIJWARHPUGHGQ { get; set; } // Formula:C12H6Br4O3, Mass:513.705, SMILES:OC=2C=C(C=C(C=2(OC1=CC(=CC(=C1(O))Br)Br))Br)Br
        public int YOVPSVDERMWCLD { get; set; } // Formula:C12H6Br4O3, Mass:513.705, SMILES:OC2=CC=C(C=C2(OC1=C(O)C=C(C(=C1Br)Br)Br))Br
        public int PLERLWUIYWAWRU { get; set; } // Formula:C30H46O7, Mass:518.324, SMILES:CC1(C)CCC2(C(O)CC3(C)C(=CCC4C5(C)CC(O)C(O)C(C)(C5CCC34C)C(O)=O)C2C1)C(O)=O
        public int XHHLBVJECNRERM { get; set; } // Formula:C36H72O, Mass:520.558, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int HVUCKZJUWZBJDP { get; set; } // Formula:C35H70O2, Mass:522.538, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int NQNSAAVEWWLPEZ { get; set; } // Formula:C37H74O, Mass:534.574, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int LRKATBAZQAWAGV { get; set; } // Formula:C36H72O2, Mass:536.553, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int OKYUSFSRNKXIQJ { get; set; } // Formula:C38H76O, Mass:548.59, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int DEQQJCLFURALOA { get; set; } // Formula:C37H74O2, Mass:550.569, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int YIVPVEUBUPQPMV { get; set; } // Formula:C39H78O, Mass:562.605, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int AJQRZOBUACOSBG { get; set; } // Formula:C38H76O2, Mass:564.585, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int UEIBUXBHZHEFSR { get; set; } // Formula:C40H80O, Mass:576.621, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int SDUXWTQRQALDFW { get; set; } // Formula:C39H78O2, Mass:578.6, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int IANNWYUKEQXKQQ { get; set; } // Formula:C41H82O, Mass:590.637, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int CWXZMNMLGZGDSW { get; set; } // Formula:C40H80O2, Mass:592.616, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int JPVKCBUGZAKQOC { get; set; } // Formula:C42H84O, Mass:604.652, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int NNAUXQJYQTWAKI { get; set; } // Formula:C41H82O2, Mass:606.631, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int SZIIOZOEWWDAIV { get; set; } // Formula:C43H86O, Mass:618.668, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int GIGASBXFYYRSBK { get; set; } // Formula:C42H84O2, Mass:620.647, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int USUMOPGSPDLELV { get; set; } // Formula:C44H88O, Mass:632.684, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int GOEYVZMRFNVCCC { get; set; } // Formula:C43H86O2, Mass:634.663, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int LOPVYXZVTLQFQF { get; set; } // Formula:C45H90O, Mass:646.699, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int ZMVSDJGYGGPPIZ { get; set; } // Formula:C44H88O2, Mass:648.678, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int QCPOTKFRGCXZFU { get; set; } // Formula:C46H92O, Mass:660.715, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=O
        public int AIOCODLLACPMJZ { get; set; } // Formula:C45H90O2, Mass:662.694, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O
        public int PLPABYLKGGTKBQ { get; set; } // Formula:C46H92O2, Mass:676.71, SMILES:CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC(O)=O

    }

    public class MolecularDescriptorRingBasis
    {
        public int Bit40 { get; set; } //>= 1 any ring size 3
        public int Bit41 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 3
        public int Bit42 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 3
        public int Bit43 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 3
        public int Bit44 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 3
        public int Bit45 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 3
        public int Bit46 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 3
      
        public int Bit54 { get; set; } //>= 1 any ring size 4
        public int Bit55 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 4
        public int Bit56 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 4
        public int Bit57 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 4
        public int Bit58 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 4
        public int Bit59 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 4
        public int Bit60 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 4
        
        public int Bit68 { get; set; } //>= 1 any ring size 5
        public int Bit69 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 5
        public int Bit70 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 5
        public int Bit71 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 5
        public int Bit72 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 5
        public int Bit73 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 5
        public int Bit74 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 5
       
        public int Bit103 { get; set; } //>= 1 any ring size 6
        public int Bit104 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 6
        public int Bit105 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 6
        public int Bit106 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 6
        public int Bit107 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 6
        public int Bit108 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 6
        public int Bit109 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 6
        
        public int Bit138 { get; set; } //>= 1 any ring size 7
        public int Bit139 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 7
        public int Bit140 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 7
        public int Bit141 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 7
        public int Bit142 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 7
        public int Bit143 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 7
        public int Bit144 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 7
        
        public int Bit152 { get; set; } //>= 1 any ring size 8
        public int Bit153 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 8
        public int Bit154 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 8
        public int Bit155 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 8
        public int Bit156 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 8
        public int Bit157 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 8
        public int Bit158 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 8
        
        public int Bit166 { get; set; } //>= 1 any ring size 9
        public int Bit167 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 9
        public int Bit168 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 9
        public int Bit169 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 9
        public int Bit170 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 9
        public int Bit171 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 9
        public int Bit172 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 9
        
        public int Bit173 { get; set; } //>= 1 any ring size 10
        public int Bit174 { get; set; } //>= 1 saturated or aromatic carbon-only ring size 10
        public int Bit175 { get; set; } //>= 1 saturated or aromatic nitrogen-containing ring size 10
        public int Bit176 { get; set; } //>= 1 saturated or aromatic heteroatom-containing ring size 10
        public int Bit177 { get; set; } //>= 1 unsaturated non-aromatic carbon-only ring size 10
        public int Bit178 { get; set; } //>= 1 unsaturated non-aromatic nitrogen-containing ring size 10
        public int Bit179 { get; set; } //>= 1 unsaturated non-aromatic heteroatom-containing ring size 10
        
        public int Bit180 { get; set; } //>= 1 aromatic ring
        public int Bit181 { get; set; } //>= 1 hetero-aromatic ring
    }
}
