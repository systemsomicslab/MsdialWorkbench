/*
 * Copyright 2006-2011 Sam Adams <sea36 at users.sourceforge.net>
 *
 * This file is part of JNI-InChI.
 *
 * JNI-InChI is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * JNI-InChI is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with JNI-InChI.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Encapsulates properites of InChI Stereo Parity.  See <tt>inchi_api.h</tt>.
    /// </summary>
    // @author Sam Adams
    internal class NInchiStereo0D
    {
        /// <summary>
        /// Indicates non-existent (central) atom. Value from inchi_api.h.
        /// </summary>
        public const int NO_ATOM = -1;

        /// <summary>
        /// Neighbouring atoms.
        /// </summary>
        public NInchiAtom[] Neighbors { get; private set; } = new NInchiAtom[4];

        /// <summary>
        /// Central atom.
        /// </summary>
        public NInchiAtom CentralAtom { get; private set; }

        /// <summary>
        /// Stereo parity type.
        /// </summary>
        public INCHI_STEREOTYPE StereoType { get; private set; }

        /// <summary>
        /// Parity.
        /// </summary>
        public INCHI_PARITY Parity {
            get;
#if !DEBUG
            private 
#endif
            set; }

        /// <summary>
        /// Second parity (for disconnected systems).
        /// </summary>
        public INCHI_PARITY DisconnectedParity {
            get;
            internal set;
        } = INCHI_PARITY.None;

        /// <summary>
        /// Constructor.  See <tt>inchi_api.h</tt> for details of usage.
        /// </summary>
        /// <param name="atC">Central atom</param>
        /// <param name="at0">Neighbour atom 0</param>
        /// <param name="at1">Neighbour atom 1</param>
        /// <param name="at2">Neighbour atom 2</param>
        /// <param name="at3">Neighbour atom 3</param>
        /// <param name="type">Stereo parity type</param>
        /// <param name="parity">Parity</param>
        /// <see cref="CreateNewTetrahedralStereo0D(NInchiAtom, NInchiAtom, NInchiAtom, NInchiAtom, NInchiAtom, INCHI_PARITY)"/> 
        /// <see cref="CreateNewDoublebondStereo0D(NInchiAtom, NInchiAtom, NInchiAtom, NInchiAtom, INCHI_PARITY)"/> 
        public NInchiStereo0D(NInchiAtom atC, NInchiAtom at0,
                 NInchiAtom at1, NInchiAtom at2, NInchiAtom at3,
                 INCHI_STEREOTYPE type, INCHI_PARITY parity)
        {
            CentralAtom = atC;
            Neighbors[0] = at0;
            Neighbors[1] = at1;
            Neighbors[2] = at2;
            Neighbors[3] = at3;

            this.StereoType = type;
            this.Parity = parity;
        }

        NInchiStereo0D(NInchiAtom atC, NInchiAtom at0,
                 NInchiAtom at1, NInchiAtom at2, NInchiAtom at3,
                 int type, int parity)
            : this(atC, at0, at1, at2, at3, (INCHI_STEREOTYPE)type, (INCHI_PARITY)parity)
        { }

        /// <summary>
        /// Generates string representation of information on stereo parity,
        /// for debugging purposes.
        /// </summary>
        public string ToDebugString()
        {
            return ("InChI Stereo0D: "
                + (CentralAtom == null ? "-" : CentralAtom.ElementType)
                + " [" + Neighbors[0].ElementType + "," + Neighbors[1].ElementType
                + "," + Neighbors[2].ElementType + "," + Neighbors[3].ElementType + "] "
                + "Type::" + StereoType + " // "
                + "Parity:" + Parity
                );
        }

        /// <summary>
        /// Outputs information on stereo parity, for debugging purposes.
        /// </summary>
        public void PrintDebug()
        {
            Console.Out.WriteLine(ToDebugString());
        }

        /// <summary>
        /// Convenience method for generating 0D stereo parities at tetrahedral
        /// atom centres.
        ///</summary>
        /// <remarks>
        /// <b>Usage notes from <i>inchi_api.h</i>:</b>
        /// <pre>
        ///  4 neighbors
        ///
        ///           X                    neighbor[4] : {#W, #X, #Y, #Z}
        ///           |                    central_atom: #A
        ///        W--A--Y                 type        : INCHI_StereoType_Tetrahedral
        ///           |
        ///           Z
        ///  parity: if (X,Y,Z) are clockwize when seen from W then parity is 'e' otherwise 'o'
        ///  Example (see AXYZW above): if W is above the plane XYZ then parity = 'e'
        ///
        ///  3 neighbors
        ///
        ///             Y          Y       neighbor[4] : {#A, #X, #Y, #Z}
        ///            /          /        central_atom: #A
        ///        X--A  (e.g. O=S   )     type        : INCHI_StereoType_Tetrahedral
        ///            \          \
        ///             Z          Z
        ///
        ///  parity: if (X,Y,Z) are clockwize when seen from A then parity is 'e',
        ///                                                         otherwise 'o'
        ///  unknown parity = 'u'
        ///  Example (see AXYZ above): if A is above the plane XYZ then parity = 'e'
        ///  This approach may be used also in case of an implicit H attached to A.
        ///
        ///  ==============================================
        ///  Note. Correspondence to CML 0D stereo parities
        ///  ==============================================
        ///  a list of 4 atoms corresponds to CML atomRefs4
        ///
        ///  tetrahedral atom
        ///  ================
        ///  CML atomParity &gt; 0 &lt;=&gt; INCHI_PARITY_EVEN
        ///  CML atomParity &lt; 0 &lt;=&gt; INCHI_PARITY_ODD
        ///
        ///                               | 1   1   1   1  |  where xW is x-coordinate of
        ///                               | xW  xX  xY  xZ |  atom W, etc. (xyz is a
        ///  CML atomParity = determinant | yW  yX  yY  yZ |  'right-handed' Cartesian
        ///                               | zW  zX  xY  zZ |  coordinate system)
        /// </pre>
        /// </remarks>
        /// <param name="atC">Central atom</param>
        /// <param name="at0">Neighbour atom 0</param>
        /// <param name="at1">Neighbour atom 1</param>
        /// <param name="at2">Neighbour atom 2</param>
        /// <param name="at3">Neighbour atom 3</param>
        /// <param name="parity">Parity</param>
        public static NInchiStereo0D CreateNewTetrahedralStereo0D(NInchiAtom atC, NInchiAtom at0,
                 NInchiAtom at1, NInchiAtom at2, NInchiAtom at3,
                INCHI_PARITY parity)
        {
            NInchiStereo0D stereo = new NInchiStereo0D(atC, at0, at1, at2, at3, INCHI_STEREOTYPE.Tetrahedral, parity);
            return stereo;
        }

        /// <summary>
        /// Convenience method for generating 0D stereo parities at stereogenic
        /// double bonds.
        /// </summary>
        /// <remarks>
        /// <b>Usage notes from <i>inchi_api.h</i>:</b>
        /// <pre>
        ///  =============================================
        ///  stereogenic bond &gt;A=B&lt; or cumulene &gt;A=C=C=B&lt;
        ///  =============================================
        ///
        ///                              neighbor[4]  : {#X,#A,#B,#Y} in this order
        ///  X                           central_atom : NO_ATOM
        ///   \            X      Y      type         : INCHI_StereoType_DoubleBond
        ///    A==B         \    /
        ///        \         A==B
        ///         Y
        ///
        ///  parity= 'e'    parity= 'o'   unknown parity = 'u'
        ///
        ///  ==============================================
        ///  Note. Correspondence to CML 0D stereo parities
        ///  ==============================================
        ///
        ///  stereogenic double bond and (not yet defined in CML) cumulenes
        ///  ==============================================================
        ///  CML 'C' (cis)      &lt;=&gt; INCHI_PARITY_ODD
        ///  CML 'T' (trans)    &lt;=&gt; INCHI_PARITY_EVEN
        /// </pre>
        /// </remarks>
        /// <param name="at0">Neighbour atom 0</param>
        /// <param name="at1">Neighbour atom 1</param>
        /// <param name="at2">Neighbour atom 2</param>
        /// <param name="at3">Neighbour atom 3</param>
        /// <param name="parity">Parity</param>
        /// <returns></returns>
        public static NInchiStereo0D CreateNewDoublebondStereo0D(NInchiAtom at0,
                 NInchiAtom at1, NInchiAtom at2, NInchiAtom at3,
                 INCHI_PARITY parity)
        {
            NInchiStereo0D stereo = new NInchiStereo0D(null, at0, at1, at2, at3, INCHI_STEREOTYPE.DoubleBond, parity);
            return stereo;
        }
    }
}
