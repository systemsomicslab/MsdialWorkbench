/* Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NCDK.Modelings.Builder3D
{
    /// <summary>
    ///  Class stores hose code patterns to identify mm2 force field atom types.
    /// </summary>
    // @author      chhoppe
    // @cdk.created 2004-09-07
    // @cdk.module  forcefield
    public class MM2BasedAtomTypePattern
    {
        private List<Regex> atomTypePatterns = new List<Regex>();

        /// <summary>
        /// Constructor for the MM2BasedAtomTypePattern object
        /// </summary>
        internal MM2BasedAtomTypePattern()
        {
            this.CreatePattern();
        }

        /// <summary>
        /// Gets the atomTypePatterns attribute of the MM2BasedAtomTypePattern object
        /// </summary>
        /// <returns>The atomTypePatterns as a vector</returns>
        public IReadOnlyList<Regex> AtomTypePatterns => atomTypePatterns;

        /// <summary>
        ///  Creates the atom type pattern
        /// </summary>
        private void CreatePattern()
        {
            atomTypePatterns.Add(new Regex(@"[CSP]\-[0-4]\-?\;[A-Za-z\+\-]{0,6}\(.*", RegexOptions.Compiled));
            //Csp3
            atomTypePatterns.Add(new Regex(@"[CS]\-[0-3]\;[H]{0,2}[A-Za-z]*\=[A-Z]{1,2}.*", RegexOptions.Compiled));
            //Csp2
            atomTypePatterns.Add(new Regex(@"C\-[0-3]\;\=O.*", RegexOptions.Compiled));
            //C carbonyl
            atomTypePatterns.Add(new Regex(@"C\-[1-2]\-?\;[H]{0,1}\%.*", RegexOptions.Compiled));
            //csp
            atomTypePatterns.Add(new Regex(@"H\-[0-1]\;[C].*", RegexOptions.Compiled));
            //H
            atomTypePatterns.Add(new Regex(@"[OS]\-[0-2]\-?\;[A-Za-z]{1,4}[\+]?[\(].*", RegexOptions.Compiled));
            //O Ether,Alcohol
            atomTypePatterns.Add(new Regex(@"O\-[1-2][\+]?\;[H]{0,1}\=[SPC].[^O]+.*", RegexOptions.Compiled));
            //=0 Carbonyl
            atomTypePatterns.Add(new Regex(@"N\-[0-3][\+\-]?\;[A-Z &&[^\=\%]]{1,3}.*", RegexOptions.Compiled));
            //nsp3
            atomTypePatterns.Add(new Regex(@"N\-[1-3][\-\+]?\;\=?[ON]?[+]?[CH]*.(\=O)?.*", RegexOptions.Compiled));
            //nsp2amide
            atomTypePatterns.Add(new Regex(@"N\-[1-2][\+]?\;\%.*", RegexOptions.Compiled));
            //nsp (10)
            atomTypePatterns.Add(new Regex(@"F.*", RegexOptions.Compiled));
            //F
            atomTypePatterns.Add(new Regex(@"Cl.*", RegexOptions.Compiled));
            //Cl
            atomTypePatterns.Add(new Regex(@"Br.*", RegexOptions.Compiled));
            //Br
            atomTypePatterns.Add(new Regex(@"I.*", RegexOptions.Compiled));
            //I
            atomTypePatterns.Add(new Regex(@"S\-[1-2][\-]?\;[HCSON]{1,2}[\(].*", RegexOptions.Compiled));
            //S Sulfide
            atomTypePatterns.Add(new Regex(@"S\-3+\;.?[A-Za-z]+.*", RegexOptions.Compiled));
            //S+Sulfonium
            atomTypePatterns.Add(new Regex(@"S\-[1-2][\+]?\;\=[OCNP][A-Z]+.*", RegexOptions.Compiled));
            //S=0
            atomTypePatterns.Add(new Regex(@"S\-4\;\=O\=O[A-Za-z]+.*", RegexOptions.Compiled));
            //So2
            atomTypePatterns.Add(new Regex(@"Si.*", RegexOptions.Compiled));
            //Silane
            atomTypePatterns.Add(new Regex(@"LP.*", RegexOptions.Compiled));
            //Lonepair (20)
            atomTypePatterns.Add(new Regex(@"H\-1\;O[\+\-]?.[PSCN]{0,2}\/.*", RegexOptions.Compiled));
            //H- OH
            atomTypePatterns.Add(new Regex(@"C\-3\;CCC..?\&?[A-Za-z]?\,?.?\&?\,?.?\&?.*", RegexOptions.Compiled));
            //C Cyclopropane
            atomTypePatterns.Add(new Regex(@"H\-1\;[NP][\+]?[\(][H]{0,2}\=?[A-Z]{0,2}\/.*", RegexOptions.Compiled));
            //H- NH amine
            atomTypePatterns.Add(new Regex(@"H\-1\;O[\+]?.\=?C\/\=?[OCSP]{1,2}\/.*", RegexOptions.Compiled));
            //H- COOH
            atomTypePatterns.Add(new Regex(@"P\-[0-3]\;[A-Za-z]{1,3}[\(].*", RegexOptions.Compiled));
            //>P
            atomTypePatterns.Add(new Regex(@"B\-[0-3]\;[A-Za-z]{1,2}.*", RegexOptions.Compiled));
            //>B
            atomTypePatterns.Add(new Regex(@"B\-4\;[A-Za-z]{1,4}.*", RegexOptions.Compiled));
            //>B<
            atomTypePatterns.Add(new Regex("SPECIAL DEFINITON ", RegexOptions.Compiled));
            //H- Amide/Enol
            atomTypePatterns.Add(new Regex("NOT Implemented", RegexOptions.Compiled));
            //C* Carbonradical
            atomTypePatterns.Add(new Regex(@"C\-[0-9][\+]\;.*", RegexOptions.Compiled));
            //C+ (30)
            atomTypePatterns.Add(new Regex(@"Ge.*", RegexOptions.Compiled));
            //Ge
            atomTypePatterns.Add(new Regex(@"Sn.*", RegexOptions.Compiled));
            //Sn
            atomTypePatterns.Add(new Regex(@"Pb.*", RegexOptions.Compiled));
            //Pb
            atomTypePatterns.Add(new Regex(@"Se.*", RegexOptions.Compiled));
            //Se
            atomTypePatterns.Add(new Regex(@"Te.*", RegexOptions.Compiled));
            //Te
            atomTypePatterns.Add(new Regex(@"D\-1\;.*", RegexOptions.Compiled));
            //D
            atomTypePatterns.Add(new Regex(@"N\-2\;\=CC..*", RegexOptions.Compiled));
            //-N= azo,Pyridin
            atomTypePatterns.Add(new Regex(@"C\-2\;\=CC..?[A-Za-z]?\,?\&?\,?C?\&?.*", RegexOptions.Compiled));
            //Csp2 Cyclopropene
            atomTypePatterns.Add(new Regex(@"N\-4[\+]?\;.*", RegexOptions.Compiled));
            //nsp3 ammonium
            atomTypePatterns.Add(new Regex(@"N\-[2-3]\;H?CC.[^\(\=O\)].*", RegexOptions.Compiled));
            //nsp2pyrrole (40)
            atomTypePatterns.Add(new Regex(@"O\-2\;CC.\=C.*\&.*\&.*", RegexOptions.Compiled));
            //osp2furan
            atomTypePatterns.Add(new Regex(@"S\-2\;CC.*", RegexOptions.Compiled));
            //s sp2 thiophene
            atomTypePatterns.Add(new Regex(@"N\-[2-3][\+]?\;\=N.*C?O?[\-]?.*", RegexOptions.Compiled));
            //-N=N-O
            atomTypePatterns.Add(new Regex(@"H\-1\;S.*", RegexOptions.Compiled));
            //H- S hiol
            atomTypePatterns.Add(new Regex(@"N\-2[\+]\;\=?\%?[NC][\-\=]{0,2}[NC][\-]?.*", RegexOptions.Compiled));
            //Azide Center n
            atomTypePatterns.Add(new Regex(@"N\-3[\+]\;\=O[A-Z]\-?[A-Z]\-?.*", RegexOptions.Compiled));
            //n no2
            atomTypePatterns.Add(new Regex(@"O\-1\-?\;\=?[CS][\(][\=0]?[OCSNH]*\/.*", RegexOptions.Compiled));
            //0 carboxylate
            atomTypePatterns.Add(new Regex(@"H\-1\;N[\+].[A-Z]{0,3}\/.*", RegexOptions.Compiled));
            //h ammonium
            atomTypePatterns.Add(new Regex(@"O\-2\;CC.H?\,?H?\,?\&\,\&\.*", RegexOptions.Compiled));
            //Epoxy
            atomTypePatterns.Add(new Regex(@"C\-2\;\=CC\.*", RegexOptions.Compiled));
            //C Benzene (50)
            atomTypePatterns.Add(new Regex(@"He.*", RegexOptions.Compiled));
            //He
            atomTypePatterns.Add(new Regex(@"Ne.*", RegexOptions.Compiled));
            //Ne
            atomTypePatterns.Add(new Regex(@"Ar.*", RegexOptions.Compiled));
            //Ar
            atomTypePatterns.Add(new Regex(@"Kr.*", RegexOptions.Compiled));
            //Kr
            atomTypePatterns.Add(new Regex(@"Xe.*", RegexOptions.Compiled));
            //Xe
            atomTypePatterns.Add(new Regex("NotImplemented", RegexOptions.Compiled));
            atomTypePatterns.Add(new Regex("NotImplemented", RegexOptions.Compiled));
            atomTypePatterns.Add(new Regex("NotImplemented", RegexOptions.Compiled));
            atomTypePatterns.Add(new Regex(@"Mg.*", RegexOptions.Compiled));
            //Mg
            atomTypePatterns.Add(new Regex(@"P\-[2-4]\;.*", RegexOptions.Compiled));
            //P (60)
            atomTypePatterns.Add(new Regex(@"Fe.*", RegexOptions.Compiled));
            //Fe 2
            atomTypePatterns.Add(new Regex(@"Fe.*", RegexOptions.Compiled));
            //Fe 3
            atomTypePatterns.Add(new Regex(@"Ni.*", RegexOptions.Compiled));
            //Ni 2
            atomTypePatterns.Add(new Regex(@"Ni.*", RegexOptions.Compiled));
            //Ni 3
            atomTypePatterns.Add(new Regex(@"Co.*", RegexOptions.Compiled));
            //Co 2
            atomTypePatterns.Add(new Regex(@"Co.*", RegexOptions.Compiled));
            //Co 3
            atomTypePatterns.Add(new Regex("NotImplemented", RegexOptions.Compiled));
            atomTypePatterns.Add(new Regex("NotImplemented", RegexOptions.Compiled));
            atomTypePatterns.Add(new Regex(@"O\-1[\-]?\;\=?N.*", RegexOptions.Compiled));
            //Amineoxide
            atomTypePatterns.Add(new Regex(@"O\-3[\+]\;[H]{0,3}[C]{0,3}[\(].*", RegexOptions.Compiled));
            //Ketoniumoxygen (70)
            atomTypePatterns.Add(new Regex(@"C\-1NotImplemented", RegexOptions.Compiled));
            //Ketoniumcarbon
            atomTypePatterns.Add(new Regex(@"N\-2\;\=C[^CO].*", RegexOptions.Compiled));
            //N =N-Imine,Oxime
            atomTypePatterns.Add(new Regex(@"N\-3[\+]\;[H]{0,2}\=?[C]{0,3}[\(].*", RegexOptions.Compiled));
            //N+ =N+Pyridinium
            atomTypePatterns.Add(new Regex(@"N\-[2-3][\+]\;\=C[CO]{2}.?[\(].*", RegexOptions.Compiled));
            //N+ =N+Imminium
            atomTypePatterns.Add(new Regex(@"N\-[2-3][\+]?\;\=CO.*", RegexOptions.Compiled));
            //N-0H Oxime
            atomTypePatterns.Add(new Regex(@"H\-1\;N[\(]{1}[CH]{2,2}\/[H]{0,3}[\,]?\=OC.*", RegexOptions.Compiled));
            //H- Amide
            atomTypePatterns.Add(new Regex(@"H\-1\;O.C\/\=CC\/.*", RegexOptions.Compiled));
            //H- AEnol (77)
            atomTypePatterns.Add(new Regex(@"N\-[1-3]\;[CH]{1,3}.{1}[A-Z]{0,3}[\,]?\=OC.*", RegexOptions.Compiled));
            //amid
        }
    }
}
