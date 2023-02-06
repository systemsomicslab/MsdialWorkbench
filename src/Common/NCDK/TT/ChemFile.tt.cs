



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
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

using System;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace NCDK.Default
{
    /// <summary>
    /// A Object containing a number of ChemSequences. This is supposed to be the
    /// top level container, which can contain all the concepts stored in a chemical
    /// document
    /// </summary>
    // @author        steinbeck
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class ChemFile
        : ChemObject, IChemFile, IChemObjectListener
    {
        /// <summary>
        /// List of ChemSquences.
        /// </summary>
        private IList<IChemSequence> chemSequences = new List<IChemSequence>();

        /// <summary>
        ///  Constructs an empty ChemFile.
        /// </summary>
        public ChemFile()
            : base()
        { }

        /// <summary>
        /// The ChemSequence at position <paramref name="number"/> in the container.
        /// </summary>
        /// <param name="number">The position of the ChemSequence</param>
        /// <returns>The ChemSequence at position <paramref name="number"/>.</returns>
        /// <seealso cref="Add(IChemSequence)"/>
        public IChemSequence this[int number]
        {
            get { return chemSequences[number]; }
            set { chemSequences[number] = value; }
        }

        /// <summary>
        /// The number of ChemSequences in this Container.
        /// </summary>
        public int Count => chemSequences.Count;

        public bool IsReadOnly => chemSequences.IsReadOnly;
        
        /// <summary>
        ///  Adds a ChemSequence to this container.
        /// </summary>
        /// <param name="chemSequence">The chemSequence to be added to this container</param>
        /// <seealso cref="this[int]"/>
        public void Add(IChemSequence chemSequence)
        {
 
            chemSequence.Listeners.Add(this);
            chemSequences.Add(chemSequence);
 
            NotifyChanged(); 
        }

        public void Clear()
        {
            foreach (var item in chemSequences)
                item.Listeners.Remove(this);
            chemSequences.Clear();
             NotifyChanged();         }

        public bool Contains(IChemSequence chemSequence)
        {
            return chemSequences.Contains(chemSequence);
        }

        public void CopyTo(IChemSequence[] array, int arrayIndex)
        {
            chemSequences.CopyTo(array, arrayIndex);
        }

        public IEnumerator<IChemSequence> GetEnumerator()
        {
            return chemSequences.GetEnumerator();
        }

        public int IndexOf(IChemSequence chemSequence)
        {
            return chemSequences.IndexOf(chemSequence);
        }

        public void Insert(int index, IChemSequence chemSequence)
        {
 
            chemSequence.Listeners.Add(this);
            chemSequences.Insert(index, chemSequence);
 
            NotifyChanged(); 
        }

        public bool Remove(IChemSequence chemSequence)
        {
            bool ret = chemSequences.Remove(chemSequence);
 
            chemSequence.Listeners.Remove(this);
            NotifyChanged(); 
            return ret;
        }

        /// <summary>
        /// Removes a ChemSequence from this container.
        /// </summary>
        /// <param name="pos">The position from which to remove</param>
        /// <seealso cref="chemSequences"/>
        /// <seealso cref="Add(IChemSequence)"/>
        public void RemoveAt(int pos)
        {
            chemSequences[pos].Listeners.Remove(this);
            chemSequences.RemoveAt(pos);
 
            NotifyChanged(); 
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (ChemFile)base.Clone(map);
            clone.chemSequences = new List<IChemSequence>();
            foreach (var chemSequence in chemSequences)
                clone.chemSequences.Add((IChemSequence)chemSequence.Clone());
            return clone;
        }

        /// <summary>
        ///  Called by objects to which this object has
        ///  registered as a listener.
        /// </summary>
        /// <param name="evt">A change event pointing to the source of the change</param>
        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
             NotifyChanged(evt);         }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// A Object containing a number of ChemSequences. This is supposed to be the
    /// top level container, which can contain all the concepts stored in a chemical
    /// document
    /// </summary>
    // @author        steinbeck
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class ChemFile
        : ChemObject, IChemFile, IChemObjectListener
    {
        /// <summary>
        /// List of ChemSquences.
        /// </summary>
        private IList<IChemSequence> chemSequences = new List<IChemSequence>();

        /// <summary>
        ///  Constructs an empty ChemFile.
        /// </summary>
        public ChemFile()
            : base()
        { }

        /// <summary>
        /// The ChemSequence at position <paramref name="number"/> in the container.
        /// </summary>
        /// <param name="number">The position of the ChemSequence</param>
        /// <returns>The ChemSequence at position <paramref name="number"/>.</returns>
        /// <seealso cref="Add(IChemSequence)"/>
        public IChemSequence this[int number]
        {
            get { return chemSequences[number]; }
            set { chemSequences[number] = value; }
        }

        /// <summary>
        /// The number of ChemSequences in this Container.
        /// </summary>
        public int Count => chemSequences.Count;

        public bool IsReadOnly => chemSequences.IsReadOnly;
        
        /// <summary>
        ///  Adds a ChemSequence to this container.
        /// </summary>
        /// <param name="chemSequence">The chemSequence to be added to this container</param>
        /// <seealso cref="this[int]"/>
        public void Add(IChemSequence chemSequence)
        {
            chemSequences.Add(chemSequence);
        }

        public void Clear()
        {
            chemSequences.Clear();
                    }

        public bool Contains(IChemSequence chemSequence)
        {
            return chemSequences.Contains(chemSequence);
        }

        public void CopyTo(IChemSequence[] array, int arrayIndex)
        {
            chemSequences.CopyTo(array, arrayIndex);
        }

        public IEnumerator<IChemSequence> GetEnumerator()
        {
            return chemSequences.GetEnumerator();
        }

        public int IndexOf(IChemSequence chemSequence)
        {
            return chemSequences.IndexOf(chemSequence);
        }

        public void Insert(int index, IChemSequence chemSequence)
        {
            chemSequences.Insert(index, chemSequence);
        }

        public bool Remove(IChemSequence chemSequence)
        {
            bool ret = chemSequences.Remove(chemSequence);
            return ret;
        }

        /// <summary>
        /// Removes a ChemSequence from this container.
        /// </summary>
        /// <param name="pos">The position from which to remove</param>
        /// <seealso cref="chemSequences"/>
        /// <seealso cref="Add(IChemSequence)"/>
        public void RemoveAt(int pos)
        {
            chemSequences.RemoveAt(pos);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (ChemFile)base.Clone(map);
            clone.chemSequences = new List<IChemSequence>();
            foreach (var chemSequence in chemSequences)
                clone.chemSequences.Add((IChemSequence)chemSequence.Clone());
            return clone;
        }

        /// <summary>
        ///  Called by objects to which this object has
        ///  registered as a listener.
        /// </summary>
        /// <param name="evt">A change event pointing to the source of the change</param>
        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
                    }
    }
}
