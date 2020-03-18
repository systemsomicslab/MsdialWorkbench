/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project. 
 */

using System.ComponentModel;

namespace NCDK.Beam
{
    /// <summary>
    /// Defines properties of a atom that can be encoded in SMILES. Atoms can be
    /// built using the <see cref="AtomBuilder"/> class.
    /// </summary>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    interface IAtom
    {
        /// <summary>
        /// The isotope number of the atom. If the isotope is undefined (default) a
        /// value -1 is returned.
        /// </summary>
        int Isotope { get; }

        /// <summary>
        /// The element of the atom.
        /// </summary>
        Element Element { get; }

        /// <summary>
        /// An label attached to an element (input only). Although invalid via the
        /// specification 'CCC[R]' etc can occur in the 'wild'. If found the parser
        /// provides an 'Unknown' element and a specified label. Not the labels are
        /// never written. By default the label is the element symbol.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Whether this atom is aromatic.
        /// </summary>
        /// <returns>atom is aromatic (true) or aliphatic (false)</returns>
        bool IsAromatic();

        /// <summary>
        /// Formal charge of the atom.
        /// </summary>
        int Charge { get; }

        /// <summary>
        /// Number of hydrogens this atom has. This value defines atoms with an
        /// explicit hydrogen count of bracket atoms (e.g. [CH4]).
        /// </summary>
        /// <returns>hydrogen count</returns>
        /// <exception cref="System.InvalidOperationException">
        /// if element is part of the organic subset and the number of hydrogens is implied by the bond Order sum.
        /// </exception>
        int NumOfHydrogens { get; }

        /// <summary>
        /// The class of the atom is defined as an integer value. The atom class is
        /// specified for bracketed atoms and is prefixed by a colon.
        /// <para>[CH:1](C)([C])[H:2]</para>
        /// </summary>
        int AtomClass { get; }

        /// <summary>
        /// Access an aromatic form of this atom. If the element can not be aromatic
        /// then the same atom is returned.
        /// </summary>
        /// <returns>the aromatic form of this atom (or if it can't be aromatic just this atom)</returns>
        IAtom AsAromaticForm();

        /// <summary>
        /// (internal) Is the atom a member of the organic (aromatic/aliphatic)
        /// subset implementation?
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Subset { get; }

        /// <summary>
        /// Access an aliphatic form of this atom. 
        /// </summary>
        /// <returns>the aliphatic form of this atom</returns>
        IAtom AsAliphaticForm();

        /// <summary>
        /// (internal) The number of hydrogens this atom would have if it were vertex
        /// '<paramref name="u"/>' in the graph '<paramref name="g"/>'. If the atom is in the organic subset the value is
        /// computed - otherwise the labelled hydrogen count is returned.
        /// </summary>
        /// <seealso cref="Graph.ImplHCount(int)"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetNumberOfHydrogens(Graph g, int u);

        /// <summary>
        /// (internal) The token to write for the atom when generating a SMILES
        /// string.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Generator.AtomToken Token { get; }
    }
}