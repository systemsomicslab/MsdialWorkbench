/* Copyright (C) 2004-2008  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
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

using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Collections;
using NCDK.IO.Iterator;

namespace NCDK
{
    /// <summary>
    /// A memory-efficient data structure to store conformers for a single molecule.
    /// </summary>
    /// <remarks>
    /// Since all the conformers for a given molecule only differ in their 3D coordinates
    /// this data structure stores a single <see cref="IAtomContainer"/> containing the atom and bond
    /// details and a List of 3D coordinate sets, each element being the set of 3D coordinates
    /// for a given conformer.
    /// </remarks>
    /// <example>
    /// The class behaves in many ways as a <see cref="IList{T}"/> of <see cref="IAtomContainer"/> object, though a few methods are not
    /// implemented. Though it is possible to add conformers by hand, this data structure is
    /// probably best used in combination with <see cref="EnumerableMDLConformerReader"/> as
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.ConformerContainer_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="EnumerableMDLConformerReader"/>
    /// <seealso cref="IO.Iterator"/>
    // @cdk.module data
    // @author Rajarshi Guha
    public class ConformerContainer : IList<IAtomContainer>
    {
        private IAtomContainer atomContainer = null;
        private IList<Vector3[]> coordinates;

        private static Vector3[] GetCoordinateList(IAtomContainer atomContainer)
        {
            var tmp = new Vector3[atomContainer.Atoms.Count];
            for (int i = 0; i < atomContainer.Atoms.Count; i++)
            {
                var atom = atomContainer.Atoms[i];
                if (atom.Point3D == null)
                    throw new ArgumentException("Molecule must have 3D coordinates");
                tmp[i] = atom.Point3D.Value;
            }
            return tmp;
        }

        public ConformerContainer()
        {
            coordinates = new List<Vector3[]>();
        }

        /// <summary>
        /// Create a ConformerContainer object from a single molecule object.
        /// <para>
        /// Using this constructor, the resultant conformer container will
        /// contain a single conformer. More conformers can be added using the
        /// <see cref="Add(IAtomContainer)"/> method.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// The constructor will use the title of the input molecule
        /// when adding new molecules as conformers. That is, the title of any molecule
        /// to be added as a conformer should match the title of the input molecule.
        /// </note>
        /// </remarks>
        /// <param name="atomContainer">The base molecule (or first conformer).</param>
        public ConformerContainer(IAtomContainer atomContainer)
        {
            this.atomContainer = atomContainer;
            Title = atomContainer.Title;
            coordinates = new List<Vector3[]>
            {
                GetCoordinateList(atomContainer)
            };
        }

        /// <summary>
        /// Create a ConformerContainer from an array of molecules.
        /// </summary>
        /// <remarks>
        /// This constructor can be used when you have an array of conformers of a given
        /// molecule. Note that this constructor will assume that all molecules in the
        /// input array will have the same title.
        /// </remarks>
        /// <param name="atomContainers">The array of conformers</param>
        public ConformerContainer(IAtomContainer[] atomContainers)
        {
            if (atomContainers.Length == 0)
                throw new ArgumentException("Can't use a zero-length molecule array");

            // lets check that the titles match
            Title = atomContainers[0].Title;
            foreach (var atomContainer in atomContainers)
            {
                string nextTitle = atomContainer.Title;
                if (Title != null && !nextTitle.Equals(Title, StringComparison.Ordinal))
                    throw new ArgumentException("Titles of all molecules must match");
            }

            this.atomContainer = atomContainers[0];
            coordinates = new List<Vector3[]>();
            foreach (var container in atomContainers)
            {
                coordinates.Add(GetCoordinateList(container));
            }
        }

        /// <summary>
        /// The title of the conformers.
        /// </summary>
        /// <remarks>
        /// <note type="note">All conformers for a given molecule will have the same title.</note>
        /// </remarks>
        public string Title { get; private set; } = null;

        /// <summary>
        /// The number of conformers stored.
        /// </summary>
        public int Count => coordinates.Count;

        /// <summary>
        /// Checks whether any conformers are stored or not.
        /// </summary>
        public bool IsEmpty() => coordinates.Count == 0;

        /// <summary>
        /// Checks to see whether the specified conformer is currently stored.
        /// </summary>
        /// <remarks>
        /// This method first checks whether the title of the supplied molecule
        /// matches the stored title. If not, it returns false. If the title matches
        /// it then checks all the coordinates to see whether they match. If all
        /// coordinates match it returns true else false.
        /// </remarks>
        /// <param name="o">The <see cref="IAtomContainer"/> to check for</param>
        /// <returns>true if it is present, false otherwise</returns>
        public bool Contains(IAtomContainer o)
        {
            return IndexOf(o) != -1;
        }

        /// <summary>
        /// Returns the conformers in the form of an array of <see cref="IAtomContainer"/>s.
        /// <para>
        /// Beware that if you have a large number of conformers you may run out
        /// memory during construction of the array since <see cref="IAtomContainer"/>'s are not
        /// light weight objects!
        /// </para>
        /// </summary>
        /// <returns>The conformers as an array of individual <see cref="IAtomContainer"/>s.</returns>
        public IAtomContainer[] ToArray()
        {
            var ret = new IAtomContainer[coordinates.Count];
            int index = 0;
            foreach (var coords in coordinates)
            {
                var conf = (IAtomContainer)atomContainer.Clone();
                for (int i = 0; i < coords.Length; i++)
                {
                    var atom = conf.Atoms[i];
                    atom.Point3D = coords[i];
                }
                ret[index++] = conf;
            }
            return ret;
        }

        /// <summary>
        /// Add a conformer to the end of the list.
        /// <para>
        /// This method allows you to add a <see cref="IAtomContainer"/> object as another conformer.
        /// Before adding it ensures that the title of specific object matches the
        /// stored title for these conformers. It will also check that the number of
        /// atoms in the specified molecule match the number of atoms in the current set
        /// of conformers.
        /// </para>
        /// <para>
        /// This method will not check for duplicate conformers.
        /// </para>
        /// </summary>
        /// <param name="atomContainer">The new conformer to add.</param>
        public void Add(IAtomContainer atomContainer)
        {
            if (this.atomContainer == null)
            {
                this.atomContainer = atomContainer;
                Title = atomContainer.Title;
            }
            if (Title == null)
            {
                throw new ArgumentException("At least one of the input molecules does not have a title");
            }
            if (!Title.Equals(atomContainer.Title, StringComparison.Ordinal))
                throw new ArgumentException("The input molecules does not have the same title ('" + Title
                        + "') as the other conformers ('" + atomContainer.Title + "')");

            if (atomContainer.Atoms.Count != this.atomContainer.Atoms.Count)
                throw new ArgumentException("Doesn't have the same number of atoms as the rest of the conformers");

            coordinates.Add(GetCoordinateList(atomContainer));
        }

        /// <summary>
        /// Remove the specified conformer.
        /// </summary>
        /// <param name="atomContainer">The conformer to remove (should be castable to <see cref="IAtomContainer"/>)</param>
        /// <returns>true if the specified conformer was present and removed, false if not found</returns>
        public bool Remove(IAtomContainer atomContainer)
        {
            // we should never have a null conformer
            if (atomContainer == null)
                return false;

            int index = IndexOf(atomContainer);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get rid of all the conformers but keeps atom and bond information.
        /// </summary>
        public void Clear()
        {
            coordinates.Clear();
        }

        /// <summary>
        /// The conformer at a specified position.
        /// </summary>
        /// <param name="i">The position of the requested conformer</param>
        public IAtomContainer this[int i]
        {
            get
            {
                var tmp = coordinates[i];
                for (int j = 0; j < atomContainer.Atoms.Count; j++)
                {
                    var atom = atomContainer.Atoms[j];
                    atom.Point3D = tmp[j];
                }
                return atomContainer;
            }
            set
            {
                Set(i, value);
            }
        }

        public IAtomContainer Set(int i, IAtomContainer atomContainer)
        {
            if (!Title.Equals(atomContainer.Title, StringComparison.Ordinal))
                throw new ArgumentException("The input molecules does not have the same title as the other conformers");
            var tmp = GetCoordinateList(atomContainer);
            var oldAtomContainer = this[i];
            coordinates[i] = tmp;
            return oldAtomContainer;
        }

        public void Insert(int i, IAtomContainer atomContainer)
        {
            if (this.atomContainer == null)
            {
                this.atomContainer = atomContainer;
                Title = (string)atomContainer.Title;
            }

            if (!Title.Equals(atomContainer.Title, StringComparison.Ordinal))
                throw new ArgumentException("The input molecules does not have the same title as the other conformers");

            if (atomContainer.Atoms.Count != this.atomContainer.Atoms.Count)
                throw new ArgumentException("Doesn't have the same number of atoms as the rest of the conformers");

            var tmp = GetCoordinateList(atomContainer);
            coordinates.Insert(i, tmp);
        }

        /// <summary>
        /// Removes the conformer at the specified position.
        /// </summary>
        /// <param name="i">The position in the list to remove</param>
        public void RemoveAt(int i)
        {
            coordinates.RemoveAt(i);
        }

        /// <summary>
        /// Returns the lowest index at which the specific <see cref="IAtomContainer"/> appears in the list or -1 if is not found.
        /// <para>
        /// A given <see cref="IAtomContainer"/> will occur in the list if the title matches the stored title for
        /// the conformers in this container and if the coordinates for each atom in the specified molecule
        /// are equal to the coordinates of the corresponding atoms in a conformer.
        /// </para>
        /// </summary>
        /// <param name="atomContainer">The <see cref="IAtomContainer"/> whose presence is being tested</param>
        /// <returns>The index where o was found</returns>
        public int IndexOf(IAtomContainer atomContainer)
        {
            if (!atomContainer.Title.Equals(Title, StringComparison.Ordinal))
                return -1;

            if (atomContainer.Atoms.Count != this.atomContainer.Atoms.Count)
                return -1;

            bool coordsMatch;
            int index = 0;
            foreach (var coords in coordinates)
            {
                coordsMatch = true;
                for (int i = 0; i < atomContainer.Atoms.Count; i++)
                {
                    var p = atomContainer.Atoms[i].Point3D.Value;
                    if (!(p.X == coords[i].X && p.Y == coords[i].Y && p.Z == coords[i].Z))
                    {
                        coordsMatch = false;
                        break;
                    }
                }
                if (coordsMatch)
                    return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Returns the highest index at which the specific <see cref="IAtomContainer"/> appears in the list or -1 if is not found.
        /// <para>
        /// A given <see cref="IAtomContainer"/> will occur in the list if the title matches the stored title for
        /// the conformers in this container and if the coordinates for each atom in the specified molecule
        /// are equal to the coordinates of the corresponding atoms in a conformer.
        /// </para>
        /// </summary>
        /// <param name="o">The <see cref="IAtomContainer"/> whose presence is being tested</param>
        /// <returns>The index where o was found</returns>
        public int LastIndexOf(IAtomContainer o)
        {
            if (!atomContainer.Title.Equals(Title, StringComparison.Ordinal))
                return -1;

            if (atomContainer.Atoms.Count != coordinates[0].Length)
                return -1;

            bool coordsMatch;
            for (int j = coordinates.Count - 1; j >= 0; j--)
            {
                Vector3[] coords = coordinates[j];
                coordsMatch = true;
                for (int i = 0; i < atomContainer.Atoms.Count; i++)
                {
                    Vector3 p = atomContainer.Atoms[i].Point3D.Value;
                    if (!(p.X == coords[i].X && p.Y == coords[i].Y && p.Z == coords[i].Z))
                    {
                        coordsMatch = false;
                        break;
                    }
                }
                if (coordsMatch) return j;
            }
            return -1;
        }

        public bool IsReadOnly => false;

        public void CopyTo(IAtomContainer[] array, int arrayIndex)
        {
            throw new InvalidOperationException();
        }

        public IEnumerator<IAtomContainer> GetEnumerator()
        {
            foreach (var tmp in coordinates)
            {
                for (int j = 0; j < tmp.Length; j++)
                    atomContainer.Atoms[j].Point3D = tmp[j];
                yield return atomContainer;
            }
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
