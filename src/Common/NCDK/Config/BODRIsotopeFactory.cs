/* Copyright (C) 2001-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *                    2013,2016  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
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

using NCDK.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NCDK.Config
{
    /// <summary>
    /// List of isotopes. Data is taken from the <see href="https://github.com/egonw/bodr">Blue Obelisk Data Repository</see>,
    /// <see href="https://github.com/egonw/bodr/releases/tag/BODR-10">version 10</see> <token>cdk-cite-BODR10</token>.
    /// The data set is described in the first Blue Obelisk paper <token>cdk-cite-Guha2006</token>.
    /// </summary>
    /// <remarks>
    /// <para>The "isotopes.dat" file that is used by this class is a binary class
    /// of this data, improving loading times over the BODR XML representation. It is created
    /// from the original BODR files using tools from the "cdk-build-util"
    /// repository.</para>
    /// </remarks>
    // @author      egonw
    // @cdk.module  core
    public class BODRIsotopeFactory 
        : IsotopeFactory
    {
        public static BODRIsotopeFactory Instance { get; } = new BODRIsotopeFactory();

        private BODRIsotopeFactory()
        {
            string configFile = "NCDK.Config.Data.isotopes.dat";
            var ins = ResourceLoader.GetAsStream(configFile);

            var buffer = new byte[8];
            ins.Read(buffer, 0, 4);
            Array.Reverse(buffer, 0, 4);
            int isotopeCount = BitConverter.ToInt32(buffer, 0);

            for (int i = 0; i < isotopeCount; i++)
            {
                var atomicNum = ins.ReadByte();
                ins.Read(buffer, 0, 2);
                Array.Reverse(buffer, 0, 2);
                var massNum = BitConverter.ToInt16(buffer, 0);
                ins.Read(buffer, 0, 8);
                Array.Reverse(buffer, 0, 8);
                var exactMass = BitConverter.ToDouble(buffer, 0);
                double natAbund;
                if (ins.ReadByte() == 1)
                {
                    ins.Read(buffer, 0, 8);
                    Array.Reverse(buffer, 0, 8);
                    natAbund = BitConverter.ToDouble(buffer, 0);
                }
                else
                {
                    natAbund = 0;
                }
                var isotope = new BODRIsotope(PeriodicTable.GetSymbol(atomicNum), atomicNum, massNum, exactMass, natAbund);
                Add(isotope);
            }
        }
        
        private static bool IsMajor(IIsotope atom)
        {
            var mass = atom.MassNumber;
            if (mass == null)
                return false;
            try
            {
                var major = Instance.GetMajorIsotope(atom.AtomicNumber);
                if (major == null)
                    return false; // no major isotope
                return major.MassNumber.Equals(mass);
            }
            catch (IOException e)
            {
                Trace.TraceError($"Could not load Isotope data: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear the isotope information from atoms that are major isotopes (e.g.
        /// <sup>12</sup>C, <sup>1</sup>H, etc).
        /// </summary>
        /// <param name="mol">the molecule</param>
        public static void ClearMajorIsotopes(IAtomContainer mol)
        {
            foreach (var atom in mol.Atoms)
            {
                if (IsMajor(atom))
                {
                    atom.MassNumber = null;
                    atom.ExactMass = null;
                    atom.Abundance = null;
                }
            }
        }

        /// <summary>
        /// Clear the isotope information from isotopes that are major (e.g.
        /// <sup>12</sup>C, <sup>1</sup>H, etc).
        /// </summary>
        /// <param name="formula">the formula</param>
        public static void ClearMajorIsotopes(IMolecularFormula formula)
        {
            var isotopesToRemove = new List<IIsotope>();
            var isotopesToAdd = new List<Tuple<IIsotope, int>>();
            foreach (var iso in formula.Isotopes.Where(n => IsMajor(n)))
            {
                var count = formula.GetCount(iso);
                isotopesToRemove.Add(iso);
                iso.MassNumber = null;
                // may be immutable
                var iso_ = iso;
                if (iso_.MassNumber != null)
                    iso_ = formula.Builder.NewIsotope(iso_.Symbol);
                iso_.ExactMass = null;
                iso_.Abundance = null;
                isotopesToAdd.Add(new Tuple<IIsotope, int>(iso_, count));
            }
            foreach (var isotope in isotopesToRemove)
                formula.Remove(isotope);
            foreach (var t in isotopesToAdd)
                formula.Add(t.Item1, t.Item2);
        }
    }
}
