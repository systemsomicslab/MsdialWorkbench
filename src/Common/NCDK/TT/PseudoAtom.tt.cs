



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2003-2007  Egon Willighagen <egonw@users.sf.net>
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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Numerics;
using System;
using System.Text;

namespace NCDK.Default
{
    /// <inheritdoc cref="IPseudoAtom"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class PseudoAtom
        : Atom, ICloneable, IPseudoAtom
    {
        private string label;
        private int attachPointNum;

        public PseudoAtom(ChemicalElement element)
            : base(element)
        {
        }

        /// <summary>
        /// Constructs an empty PseudoAtom.
        /// </summary>
        public PseudoAtom()
            : this("*")
        {
        }

        /// <summary>
        /// Constructs an Atom from a String containing an element symbol.
        /// </summary>
        /// <param name="label">The String describing the PseudoAtom</param>
        public PseudoAtom(string label)
            : base("R")
        {
            this.label = label;
            base.fractionalPoint3D = null;
            base.point2D = null;
            base.point3D = null;
            base.stereoParity = 0;
        }

        /// <summary>
        /// Constructs an <see cref="PseudoAtom"/> from a <see cref="IAtom"/>.
        /// </summary>
        /// <param name="element"><see cref="IAtom"/> from which the <see cref="PseudoAtom"/> is constructed</param>
        public PseudoAtom(IElement element)
            : base(element)
        {
            AtomicNumber = 0;
            if (element is IPseudoAtom aa)
                this.label = aa.Label;
            else
            {
                this.label = "R";
            }
        }

        /// <summary>
        /// Constructs a <see cref="PseudoAtom"/> from an element name and a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="label">The <see cref="string"/> describing the <see cref="PseudoAtom"/></param>
        /// <param name="point3d">The 3D coordinates of the atom</param>
        public PseudoAtom(string label, Vector3 point3d)
            : this(label)
        {
            base.point3D = point3d;
        }

        /// <summary>
        /// Constructs a <see cref="PseudoAtom"/> from an element name and a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="label">The <see cref="string"/> describing the <see cref="PseudoAtom"/></param>
        /// <param name="point2d">The 2D coordinates of the atom</param>
        public PseudoAtom(string label, Vector2 point2d)
            : this(label)
        {
            base.point2D = point2d;
        }

        /// <summary>
        /// The label of this <see cref="PseudoAtom"/>.
        /// </summary>
        public virtual string Label
        {
            get { return label; }
            set { label = value;  NotifyChanged();  }
        }

        /// <inheritdoc/>
        public virtual int AttachPointNum
        {
            get { return attachPointNum; }
            set { attachPointNum = value;  NotifyChanged();  }
        }

        /// <inheritdoc/>
        public override int StereoParity
        {
            get { return base.StereoParity; }
            set { /* this is undefined, always */}
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("PseudoAtom(");
            sb.Append(GetHashCode());
            if (Label != null)
                sb.Append(", ").Append(Label);
            sb.Append(", ").Append(base.ToString());
            sb.Append(')');
            return sb.ToString();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            return base.Clone(map);
        }
    }
}
namespace NCDK.Silent
{
    /// <inheritdoc cref="IPseudoAtom"/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class PseudoAtom
        : Atom, ICloneable, IPseudoAtom
    {
        private string label;
        private int attachPointNum;

        public PseudoAtom(ChemicalElement element)
            : base(element)
        {
        }

        /// <summary>
        /// Constructs an empty PseudoAtom.
        /// </summary>
        public PseudoAtom()
            : this("*")
        {
        }

        /// <summary>
        /// Constructs an Atom from a String containing an element symbol.
        /// </summary>
        /// <param name="label">The String describing the PseudoAtom</param>
        public PseudoAtom(string label)
            : base("R")
        {
            this.label = label;
            base.fractionalPoint3D = null;
            base.point2D = null;
            base.point3D = null;
            base.stereoParity = 0;
        }

        /// <summary>
        /// Constructs an <see cref="PseudoAtom"/> from a <see cref="IAtom"/>.
        /// </summary>
        /// <param name="element"><see cref="IAtom"/> from which the <see cref="PseudoAtom"/> is constructed</param>
        public PseudoAtom(IElement element)
            : base(element)
        {
            AtomicNumber = 0;
            if (element is IPseudoAtom aa)
                this.label = aa.Label;
            else
            {
                this.label = "R";
            }
        }

        /// <summary>
        /// Constructs a <see cref="PseudoAtom"/> from an element name and a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="label">The <see cref="string"/> describing the <see cref="PseudoAtom"/></param>
        /// <param name="point3d">The 3D coordinates of the atom</param>
        public PseudoAtom(string label, Vector3 point3d)
            : this(label)
        {
            base.point3D = point3d;
        }

        /// <summary>
        /// Constructs a <see cref="PseudoAtom"/> from an element name and a <see cref="Vector2"/>.
        /// </summary>
        /// <param name="label">The <see cref="string"/> describing the <see cref="PseudoAtom"/></param>
        /// <param name="point2d">The 2D coordinates of the atom</param>
        public PseudoAtom(string label, Vector2 point2d)
            : this(label)
        {
            base.point2D = point2d;
        }

        /// <summary>
        /// The label of this <see cref="PseudoAtom"/>.
        /// </summary>
        public virtual string Label
        {
            get { return label; }
            set { label = value;  }
        }

        /// <inheritdoc/>
        public virtual int AttachPointNum
        {
            get { return attachPointNum; }
            set { attachPointNum = value;  }
        }

        /// <inheritdoc/>
        public override int StereoParity
        {
            get { return base.StereoParity; }
            set { /* this is undefined, always */}
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("PseudoAtom(");
            sb.Append(GetHashCode());
            if (Label != null)
                sb.Append(", ").Append(Label);
            sb.Append(", ").Append(base.ToString());
            sb.Append(')');
            return sb.ToString();
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            return base.Clone(map);
        }
    }
}
