/* Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *                     2008  Egon Willighagen <egonw@users.sf.net>
 *
 *  Contact: cdk-devel@list.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
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

using NCDK.Tools;
using System;
using System.Text.RegularExpressions;

namespace NCDK.Charges
{
    /// <summary>
    /// Assigns charges to atom types.
    /// </summary>
    // @author      chhoppe
    // @cdk.created 2004-11-03
    // @cdk.module  charges
    public class AtomTypeCharges : IChargeCalculator
    {
        HOSECodeGenerator hcg = new HOSECodeGenerator();
        Regex pOC = new Regex("O-[1][-];=?+C[(]=?+O.*+", RegexOptions.Compiled);
        Regex pOP = new Regex("O-[1][-];=?+P.*+", RegexOptions.Compiled);
        Regex pOS = new Regex("O-[1][-];=?+S.*+", RegexOptions.Compiled);
        Regex p_p = new Regex("[A-Za-z]{1,2}+[-][0-6].?+[+].*+", RegexOptions.Compiled);
        Regex p_n = new Regex("[A-Za-z]{1,2}+[-][0-6].?+[-].*+", RegexOptions.Compiled);

        /// <summary>
        ///  Constructor for the AtomTypeCharges object.
        /// </summary>
        AtomTypeCharges() { }

        /// <summary>
        ///  Sets initial charges for atom types.
        /// +1 for cationic atom types
        /// -1 for anionic atom types
        /// carboxylic oxygen -0.5
        /// phosphorylic oxygen -0.66
        /// sulfanilic oxygen -0.5
        /// or to formal charge (which must be determined elsewhere or set manually)
        /// polycations are not handled by this approach
        /// </summary>
        /// <param name="atomContainer">AtomContainer</param>
        /// <returns>AtomContainer with set charges</returns>
        public IAtomContainer SetCharges(IAtomContainer atomContainer)
        {
            atomContainer = SetInitialCharges(atomContainer);
            return atomContainer;
        }

        private static string RemoveAromaticityFlagsFromHoseCode(string hoseCode)
        {
            //clean hosecode
            string hosecode = "";
            for (int i = 0; i < hoseCode.Length; i++)
            {
                if (hoseCode[i] == '*')
                {
                }
                else
                {
                    hosecode = hosecode + hoseCode[i];
                }
            }
            return hosecode;
        }

        /// <summary>
        ///  Sets the initialCharges attribute of the AtomTypeCharges object.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <returns>AtomContainer with (new) partial charges</returns>
        /// <exception cref="CDKException"></exception>
        private IAtomContainer SetInitialCharges(IAtomContainer ac)
        {
            Match matOC = null;
            Match matOP = null;
            Match matOS = null;
            Match mat_p = null;
            Match mat_n = null;
            string hoseCode = "";

            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                try
                {
                    hoseCode = hcg.GetHOSECode(ac, ac.Atoms[i], 3);
                }
                catch (CDKException ex1)
                {
                    throw new CDKException($"Could not build HOSECode from atom {i} due to {ex1.ToString()}", ex1);
                }
                hoseCode = RemoveAromaticityFlagsFromHoseCode(hoseCode);

                matOC = pOC.Match(hoseCode);
                matOP = pOP.Match(hoseCode);
                matOS = pOS.Match(hoseCode);
                mat_p = p_p.Match(hoseCode);
                mat_n = p_n.Match(hoseCode);

                if (matOC.Success)
                {
                    ac.Atoms[i].Charge = -0.500;
                }
                else if (matOP.Success)
                {
                    ac.Atoms[i].Charge = -0.666;
                }
                else if (matOS.Success)
                {
                    ac.Atoms[i].Charge = -0.500;
                }
                else if (mat_p.Success)
                {
                    ac.Atoms[i].Charge = +1.000;
                }
                else if (mat_n.Success)
                {
                    ac.Atoms[i].Charge = -1.000;
                }
                else
                {
                    ac.Atoms[i].Charge = ac.Atoms[i].FormalCharge;
                }
            }
            return ac;
        }

        public void CalculateCharges(IAtomContainer container)
        {
            try
            {
                this.SetInitialCharges(container);
            }
            catch (Exception exception)
            {
                throw new CDKException($"Could not calculate Gasteiger-Marsili PEPE charges: {exception.Message}", exception);
            }
        }
    }
}
