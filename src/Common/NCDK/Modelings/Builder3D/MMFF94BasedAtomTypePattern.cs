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
    ///  Class stores hose code patterns to identify mm2 force field atom types
    /// </summary>
    // @author     chhoppe
    // @cdk.created    2004-09-07
    // @cdk.module     forcefield
    public class MMFF94BasedAtomTypePattern
    {
        private List<Regex> atomTypePatterns = new List<Regex>();

        /// <summary>
        ///Constructor for the MM2BasedAtomTypePattern object
        /// </summary>
        internal MMFF94BasedAtomTypePattern()
        {
            CreatePattern();
        }

        /// <summary>
        ///  Gets the atomTypePatterns attribute of the MM2BasedAtomTypePattern object
        /// </summary>
        /// <returns>The atomTypePatterns as a vector</returns>
        public List<Regex> AtomTypePatterns => atomTypePatterns;

        /// <summary>
        /// Creates the atom type pattern
        /// </summary>
        // TODO: convert Java regex to .NET regex
        private void CreatePattern()
        {
            atomTypePatterns.Add(new Regex(@"C\-[0-4][\+]?\;[A-Za-z\+\-]{0,6}[\(].*", RegexOptions.Compiled));
            //Csp3
            atomTypePatterns.Add(new Regex(@"[C]\-[1-3]\;[H]{0,2}[A-Za-z]*\=[C]{1}.*", RegexOptions.Compiled));
            //Csp2
            atomTypePatterns.Add(new Regex(@"[CS]\-[0-3]\;[H]{0,2}[A-Za-z]*\=[NOPS]{1}.*", RegexOptions.Compiled));
            //Csp2 C=
            atomTypePatterns.Add(new Regex(@"C\-[0-2]\;[H]{0,1}\%.*", RegexOptions.Compiled));
            //csp
            atomTypePatterns.Add(new Regex(@"[CS]\-[3][\-]?\;[A-Za-z]{0,2}\=O\=O[A-Za-z]{0,2}[\(].*", RegexOptions.Compiled));
            //C(S)O2-M
            atomTypePatterns.Add(new Regex(@"C\-[2-3]\;[H]{0,1}\=N[\+]?N[\+]?C[\(].*", RegexOptions.Compiled));
            //CNN+ N+=C-N
            atomTypePatterns.Add(new Regex(@"C\-1[\-]?\;\%N[\+]?[\(].*", RegexOptions.Compiled));
            //c in isonitrile C%
            atomTypePatterns.Add(new Regex(@"C\-2\;\=N[\+]?N[\(].*", RegexOptions.Compiled));
            //imidazolium IM+
            atomTypePatterns.Add(new Regex(@"C\-[0-4]\;[A-Za-z\+\-]{1,6}[\(].*", RegexOptions.Compiled));
            //CR4R Csp3 in 4 member rings -> in configure atom type (20)
            atomTypePatterns.Add(new Regex(@"C\-[0-4]\;[A-Za-z\+\-]{1,6}[\(].*", RegexOptions.Compiled));
            //CR3R Csp3 in 3 member rings -> in configure atom type (10)
            atomTypePatterns.Add(new Regex(@"[C]\-[0-3]\;[H]{0,2}[A-Za-z]*\=[A-Z]{1,2}.*", RegexOptions.Compiled));
            //CE4R Csp2 ->configure atom 4mRing
            atomTypePatterns.Add(new Regex(@"[C]\-[0-3]\;[H]{0,2}[A-Za-z]*\=[A-Z]{1,2}.*", RegexOptions.Compiled));
            //Car Csp2 aromatic
            atomTypePatterns.Add(new Regex(@"C\-[2-3]\;[H]?[C]{1}[A-BD-Z]{1}[\(].*", RegexOptions.Compiled));
            //C5A atom configure alpha carbon 5 mem. hetero ring
            atomTypePatterns.Add(new Regex(@"C\-[2-3]\;[H]?[C]{2,3}[\(][HC]{0,2}[\,]?[A-BD-Z]{1}.*", RegexOptions.Compiled));
            //C5B atom configure beta carbon 5 mem. hetero ring
            atomTypePatterns.Add(new Regex(@"NO PATTERN", RegexOptions.Compiled));
            // C5 c or n in heteroaromtaic ring, not alpha or beta C5/N5 (15)
            atomTypePatterns.Add(new Regex(@"H\-[1]\;(C|Si)[\+]?[\(].*", RegexOptions.Compiled));
            //HC
            atomTypePatterns.Add(new Regex(@"H\-[1]\;O[\(].{2,}.*", RegexOptions.Compiled));
            //HO
            atomTypePatterns.Add(new Regex(@"H@-[0-1]@;[N][@(].*", RegexOptions.Compiled));
            //HN
            atomTypePatterns.Add(new Regex(@"H\-1\;O[\(]C\/[H]{0,1}\=O.*", RegexOptions.Compiled));
            //HO COOH-> configure Atom
            atomTypePatterns.Add(new Regex(@"H\-[0-1]\;N[\(][H]{0,2}\=C.*", RegexOptions.Compiled));
            //HN=C -> configure atom (20)
            atomTypePatterns.Add(new Regex(@"H\-[0-1]\;N[\(][H]{0,2}\=[A-BD-Z].*", RegexOptions.Compiled));
            //HN2 HN=X -> configure atom
            atomTypePatterns.Add(new Regex(@"H\-[0-1]\;O[\(]C\/[H]?\=C.*", RegexOptions.Compiled));
            //HOCC enol phenol
            atomTypePatterns.Add(new Regex(@"H\-[0-1]\;O[\(]H[\)]", RegexOptions.Compiled));
            //HOH
            atomTypePatterns.Add(new Regex(@"H\-[0-1]\;O[\(][H]{0,2}\=?S.*", RegexOptions.Compiled));
            //HOS
            atomTypePatterns.Add(new Regex(@"H\-[0-1]\;[N][\+][\(].*", RegexOptions.Compiled));
            //HN+
            atomTypePatterns.Add(new Regex(@"H\-[1]\;O[\+][\(][A-Za-z]{1,4}.*", RegexOptions.Compiled));
            //HO+
            atomTypePatterns.Add(new Regex(@"H-[1]\;O[\+][\(].*", RegexOptions.Compiled));
            //HO=+
            atomTypePatterns.Add(new Regex(@"H\-[1]\;[SP].*", RegexOptions.Compiled));
            //H on S or P (28)

            atomTypePatterns.Add(new Regex(@"O\-[2]\;[HCSN]{1,2}[\+]?[\(].*", RegexOptions.Compiled));
            //O Ether,Alcohol
            atomTypePatterns.Add(new Regex(@"O\-[1]\;\=.*", RegexOptions.Compiled));
            //0= (30)
            atomTypePatterns.Add(new Regex(@"O\-[1]\;\=[A-BD-Za-z]{1,2}.*", RegexOptions.Compiled));
            //O=X
            atomTypePatterns.Add(new Regex(@"O\-[1][\-]?\;.*", RegexOptions.Compiled));
            //OM O-
            atomTypePatterns.Add(new Regex(@"O\-[3][\+]\;.*", RegexOptions.Compiled));
            //O+
            atomTypePatterns.Add(new Regex(@"O\-[1-2][\+]\;[A-Za-z]{0,2}\=.*", RegexOptions.Compiled));
            //O=+
            atomTypePatterns.Add(new Regex(@"O\-[1-2]\;[H]{0,2}.*", RegexOptions.Compiled));
            //O in water
            atomTypePatterns.Add(new Regex(@"O\-2\;CC.\=C.*\&.*\&.*", RegexOptions.Compiled));
            //osp2furan (36)

            atomTypePatterns.Add(new Regex(@"N\-[0-3]\;[A-Za-z ]{1,3}.*", RegexOptions.Compiled));
            //N nsp3
            atomTypePatterns.Add(new Regex(@"N\-[1-3]\;[H]{0,2}[A-Za-z]*\=[CN].*", RegexOptions.Compiled));
            //N=C n imides
            atomTypePatterns.Add(new Regex(@"N\-[1-3]\;[H]{0,3}[C]*[\(].*\=C.*", RegexOptions.Compiled));
            //NC=C
            atomTypePatterns.Add(new Regex(@"N\-[1-2][\+]?\;\%.*", RegexOptions.Compiled));
            //nsp (40)
            atomTypePatterns.Add(new Regex(@"N\-[2][\+]?\;\=[NC]\=[NC][\-]?[\(].*", RegexOptions.Compiled));
            //n =N= C=N=N N=N=N)
            atomTypePatterns.Add(new Regex(@"N\-1[\+\-]?\;\%?\=?N[\+]?[\(]\=?N[\-]?.*", RegexOptions.Compiled));
            //NAZT terminal n in azido);
            atomTypePatterns.Add(new Regex(@"N\-4[\+]\;.*", RegexOptions.Compiled));
            //N+ nsp3 ammonium
            atomTypePatterns.Add(new Regex(@"N\-[2-3][\+]?\;\=[A-NP-Z]{1,2}O[\-]?[\(].*", RegexOptions.Compiled));
            //N2OX n aromatic n oxide sp2
            atomTypePatterns.Add(new Regex(@"N\-[1-3]\;[H]{0,2}[O]{0,1}[\-]?([A-Za-z]{2}[O]?|[A-Za-z][O])[\-]?[\(].*", RegexOptions.Compiled));
            //N3OX aromatic n oxide sp3
            atomTypePatterns.Add(new Regex(@"N\-[1-3][\+]?\;[H]{0,2}[A-Za-z]{0,6}[\(].*\%C.*\%N.*", RegexOptions.Compiled));
            //NC#N N->CN
            atomTypePatterns.Add(new Regex(@"N\-3[\+]\;\=OCO\-.*", RegexOptions.Compiled));
            //n no2
            atomTypePatterns.Add(new Regex(@"N\-2\;[A-NP-Z]{0,1}\=O[A-NP-Z]{0,1}[\(].*", RegexOptions.Compiled));
            //n N=O

            //cdk.bug 3515122 fixed
            atomTypePatterns.Add(new Regex(@"N\-[1-3]\;[CHN]{1,3}.{1}[A-Z]{0,3}[\,]?\=O[CNXO].*", RegexOptions.Compiled));
            //NC=0 amid
            atomTypePatterns.Add(new Regex(@"N\-[1-2]\;[CH]{1}\=S[\(].*", RegexOptions.Compiled)); 
            //NSO (50)
            atomTypePatterns.Add(new Regex(@"N\-[1-3][\+]\;[H]{0,2}\=[A-Za-z]{1,3}[\(].*", RegexOptions.Compiled));
            //n N+=
            atomTypePatterns.Add(new Regex(@"N\-[0-3][\+]\;[H]{0,2}\=C[\(][A-MO-Za-z]{0,7}[N]{1}\/.*", RegexOptions.Compiled));
            //n NCN+
            atomTypePatterns.Add(new Regex(@"N\-[0-3][\+]\;[H]{0,2}\=C[\(][N]{2}\/.*", RegexOptions.Compiled));
            //n NGD+
            atomTypePatterns.Add(new Regex(@"N\-[1-2][\+]\;[H]{0,1}\%[NC][\-]?[\(].*", RegexOptions.Compiled));
            //NR% n in isonitrile, diazo
            atomTypePatterns.Add(new Regex(@"N\-[1-2][\-]\;[H]{0,1}S[A-Z]{0,1}[\(][H]{0,4}\=?O[\-]?.*", RegexOptions.Compiled));
            //NM n deproonated sulfonamid
            atomTypePatterns.Add(new Regex(@"N\-[2][\-]\;.*", RegexOptions.Compiled));
            //N5M neg charged n
            atomTypePatterns.Add(new Regex(@"N\-[2-3]\;[H]{0,1}[A-MO-Za-z]{2,3}[\(].*", RegexOptions.Compiled));
            //NPYD n aromatic 6
            atomTypePatterns.Add(new Regex(@"N\-[2-3]\;[H]{0,1}[A-MO-Za-z]{2,3}[\(].*", RegexOptions.Compiled));
            //NPYL n aromtiac 5
            atomTypePatterns.Add(new Regex(@"N\-[2-3][\+]\;[H]{0,1}[A-MP-Za-z]{2,3}[\(].*", RegexOptions.Compiled));
            //n npyd+ NCN+ Pyrimidinium
            atomTypePatterns
                    .Add(new Regex(@"N\-[2-3][\+]?\;[H]{0,1}\=?(N|O|S){0,1}[\+]?\=?C[\+]?(N|O|S){0,1}[\(].*", RegexOptions.Compiled));
            //N5A n aromatic 5 CN=N (60)
            atomTypePatterns.Add(new Regex(@"N\-(2|3)\;[H]{0,1}\=CC[\(][H]{0,3}\=?[A-BD-Z].*", RegexOptions.Compiled));
            //N5B n aromatic 5 N=CN
            atomTypePatterns.Add(new Regex(@"N\-[3][\+]\;[A-MP-Z]{2}O[\-]?[\(].*", RegexOptions.Compiled));
            //NPOX n aromatic n oxide aromatic 6 ring -> configure
            atomTypePatterns.Add(new Regex(@"N\-[3][\+]\;[A-MP-Z]{2}O[\-]?[\(].*", RegexOptions.Compiled));
            //N5Ox
            atomTypePatterns.Add(new Regex(@"NO PATTERN", RegexOptions.Compiled));
            //N5+
            atomTypePatterns.Add(new Regex(@"N\-[1-3]\;[H]{0,1}[A-Za-z]+[\(].*", RegexOptions.Compiled));
            //N5 (65)

            atomTypePatterns.Add(new Regex(@"S\-[1-2]\;[HCNO]{1,2}[\+]?[\(].*", RegexOptions.Compiled));
            //S thioether, mercaptane
            atomTypePatterns.Add(new Regex(@"S\-[1]\;[H]{0,2}\=C.*", RegexOptions.Compiled));
            //terminal S=C
            atomTypePatterns.Add(new Regex(@"S\-[1-3]\;[H]{0,2}\=[ON].*", RegexOptions.Compiled));
            //>SN
            atomTypePatterns.Add(new Regex(@"S\-[3-4]\;[H]{0,2}\=[OCN]\=[OCN]\=?[OCN]{0,2}[\(].*", RegexOptions.Compiled));
            //SO2
            atomTypePatterns.Add(new Regex(@"S\-[1-2][\-]?;[H]{0,1}[A-Za-z]{0,2}[\(].*", RegexOptions.Compiled));
            //temrinal SX (70)
            atomTypePatterns.Add(new Regex(@"S\-[3]\;\=OO[\-]?[A-Za-z]{1,2}[\-]?[\(].*", RegexOptions.Compiled));
            //S SO2 in negativly charged SO2R group
            atomTypePatterns.Add(new Regex(@"S\-[2]\;\=[A-Za-z]{1,2}\=O+[\(].*", RegexOptions.Compiled));
            //=SO
            atomTypePatterns.Add(new Regex(@"S\-[2]\;[H]{0,3}\=C.*", RegexOptions.Compiled));
            //Stringin thiophen (73)

            atomTypePatterns.Add(new Regex(@"P\-[4]\;.*", RegexOptions.Compiled));
            //P tetra ->configure Atom for P
            atomTypePatterns.Add(new Regex(@"P\-[0-3]\;.*", RegexOptions.Compiled));
            //P tri -> configure atom for P=C
            atomTypePatterns.Add(new Regex(@"P\-[2]\;\=C[A-Za-z]{1,2}[\(].*", RegexOptions.Compiled));
            //P C=P-
            atomTypePatterns.Add(new Regex(@"F\-[0-7][\+]?\;.*", RegexOptions.Compiled));
            //F
            atomTypePatterns.Add(new Regex(@"Cl\-[0-7][\+]?\;.*", RegexOptions.Compiled));
            //Cl
            atomTypePatterns.Add(new Regex(@"Br\-[0-7][\+]?\;.*", RegexOptions.Compiled));
            //Br
            atomTypePatterns.Add(new Regex(@"I.*", RegexOptions.Compiled));
            //I
            atomTypePatterns.Add(new Regex(@"Si.*", RegexOptions.Compiled));
            //Silane
            atomTypePatterns.Add(new Regex(@"Cl[4]\;.*", RegexOptions.Compiled));
            //cl in perchlorat anion
            atomTypePatterns.Add(new Regex(@"Fe2[\+].*", RegexOptions.Compiled));
            //Fe 2
            atomTypePatterns.Add(new Regex(@"Fe3[\+].*", RegexOptions.Compiled));
            //Fe 3
            atomTypePatterns.Add(new Regex(@"F\-[0-2][\-]\;.*", RegexOptions.Compiled));
            //F
            atomTypePatterns.Add(new Regex(@"Cl\-[0-2][\-]\;.*", RegexOptions.Compiled));
            //Cl
            atomTypePatterns.Add(new Regex(@"Br\-[0-2][\-]\;.*", RegexOptions.Compiled));
            //Br
            atomTypePatterns.Add(new Regex(@"Li\-[0-2][\+]\;.*", RegexOptions.Compiled));
            //Li+
            atomTypePatterns.Add(new Regex(@"Na[\+]\;.*", RegexOptions.Compiled));
            //Na+
            atomTypePatterns.Add(new Regex(@"K[\+]\;.*", RegexOptions.Compiled));
            //K+
            atomTypePatterns.Add(new Regex(@"Zn2[\+]\;.*", RegexOptions.Compiled));
            //Zn2+
            atomTypePatterns.Add(new Regex(@"Ca2[\+]\;.*", RegexOptions.Compiled));
            //Ca2+
            atomTypePatterns.Add(new Regex(@"Cu[\+]\;.*", RegexOptions.Compiled));
            //Cu1+
            atomTypePatterns.Add(new Regex(@"Cu2[\+]\;.*", RegexOptions.Compiled));
            //Cu2+
            atomTypePatterns.Add(new Regex(@"Mg2[\+]\;.*", RegexOptions.Compiled));
            //Mg2+
        }
    }
}
