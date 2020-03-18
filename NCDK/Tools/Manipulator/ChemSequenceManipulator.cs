/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Class with convenience methods that provide methods from
    /// methods from ChemObjects within the ChemSequence.
    /// </summary>
    /// <seealso cref="IAtomContainer.RemoveAtomAndConnectedElectronContainers(IAtom)"/>
    // @cdk.module standard
    public static class ChemSequenceManipulator
    {
        /// <summary>
        /// Get the total number of atoms inside an IChemSequence.
        /// </summary>
        /// <param name="sequence">The IChemSequence object.</param>
        /// <returns>The number of Atom objects inside.</returns>
        public static int GetAtomCount(IChemSequence sequence)
        {
            int count = 0;
            for (int i = 0; i < sequence.Count; i++)
            {
                count += ChemModelManipulator.GetAtomCount(sequence[i]);
            }
            return count;
        }

        /// <summary>
        /// Get the total number of bonds inside an IChemSequence.
        ///
        /// <param name="sequence">The IChemSequence object.</param>
        /// <returns>The number of Bond objects inside.</returns>
        /// </summary>
        public static int GetBondCount(IChemSequence sequence)
        {
            int count = 0;
            for (int i = 0; i < sequence.Count; i++)
            {
                count += ChemModelManipulator.GetBondCount(sequence[i]);
            }
            return count;
        }

        /// <summary>
        /// Returns all the AtomContainer's of a ChemSequence.
        /// </summary>
        public static IEnumerable<IAtomContainer> GetAllAtomContainers(IChemSequence sequence)
        {
            foreach (var model in sequence)
                foreach (var e in ChemModelManipulator.GetAllAtomContainers(model))
                    yield return e;
            yield break;
        }

        /// <summary>
        /// An enumerable of all IChemObject inside a ChemSequence.
        /// </summary>
        /// <returns>An enumerable of all ChemObjects.</returns>
        public static IEnumerable<IChemObject> GetAllChemObjects(IChemSequence sequence)
        {
            var list = new List<IChemObject>();
            // list.Add(sequence);
            for (int i = 0; i < sequence.Count; i++)
            {
                yield return sequence[i];
                var current = ChemModelManipulator.GetAllChemObjects(sequence[i]);
                foreach (var chemObject in current)
                    if (!list.Contains(chemObject))
                    {
                        list.Add(chemObject);
                        yield return chemObject;
                    }
            }
            yield break;
        }

        public static IEnumerable<string> GetAllIDs(IChemSequence sequence)
        {
            if (sequence.Id != null)
                yield return sequence.Id;
            for (int i = 0; i < sequence.Count; i++)
                foreach (var e in ChemModelManipulator.GetAllIDs(sequence[i]))
                    yield return e;
            yield break;
        }
    }
}

