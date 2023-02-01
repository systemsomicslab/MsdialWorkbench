



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2002-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using System;
using NCDK.Numerics;

namespace NCDK.Default
{
    /// <summary>
    /// Class representing a molecular crystal.
    /// The crystal is described with molecules in fractional
    /// coordinates and three cell axes: a,b and c.
    /// </summary>
    /// <remarks>
    /// The crystal is designed to store only the asymmetric atoms.
    /// Though this is not enforced, it is assumed by all methods.
    /// </remarks>
    // @cdk.keyword crystal
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class Crystal
        : AtomContainer, ICrystal, ICloneable
    {
        /// <summary>The a axis.</summary>
        private Vector3 a = Vector3.Zero;

        /// <summary>
        /// The A unit cell axes in Cartesian coordinates in a Euclidean space.
        /// </summary>
        public Vector3 A
        {
            get { return a; }
            set { a = value;  NotifyChanged();  }
        }

        /// <summary>The b axis.</summary>
        private Vector3 b = Vector3.Zero;
        /// <summary>
        /// The B unit cell axes in Cartesian coordinates in a Euclidean space.
        /// </summary>
        public Vector3 B
        {
            get { return b; }
            set { b = value;  NotifyChanged();  }
        }

        /// <summary>The c axis.</summary>
        private Vector3 c = Vector3.Zero;
        /// <summary>
        /// The C unit cell axes in Cartesian coordinates in a Euclidean space.
        /// </summary>
        public Vector3 C
        {
            get { return c; }
            set { c = value;  NotifyChanged();  }
        }

         /// <summary>Number of symmetry related atoms.</summary>
        private string spaceGroup = "P1";

        /// <summary>
        /// The space group of this crystal.
        /// </summary>
        public string SpaceGroup
        {
            get { return spaceGroup; }
            set { spaceGroup = value;  NotifyChanged();  }
        }

        /// <summary>Number of symmetry related atoms.</summary>
        private int? z = 1;
        /// <summary>
        /// The number of asymmetric parts in the unit cell.
        /// </summary>
        public int? Z
        {
            get { return z; }
            set { z = value;  NotifyChanged();  }
        }

        /// <summary>
        /// Constructs a new crystal with zero length cell axis.
        /// </summary>
        public Crystal()
            : base()
        {
        }

        /// <summary>
        /// Constructs a new crystal with zero length cell axis
        /// and adds the atoms in the AtomContainer as cell content.
        /// </summary>
        /// <param name="container">the AtomContainer providing the atoms and bonds</param>
        public Crystal(IAtomContainer container)
            : base(container)
        {
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// Class representing a molecular crystal.
    /// The crystal is described with molecules in fractional
    /// coordinates and three cell axes: a,b and c.
    /// </summary>
    /// <remarks>
    /// The crystal is designed to store only the asymmetric atoms.
    /// Though this is not enforced, it is assumed by all methods.
    /// </remarks>
    // @cdk.keyword crystal
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class Crystal
        : AtomContainer, ICrystal, ICloneable
    {
        /// <summary>The a axis.</summary>
        private Vector3 a = Vector3.Zero;

        /// <summary>
        /// The A unit cell axes in Cartesian coordinates in a Euclidean space.
        /// </summary>
        public Vector3 A
        {
            get { return a; }
            set { a = value;  }
        }

        /// <summary>The b axis.</summary>
        private Vector3 b = Vector3.Zero;
        /// <summary>
        /// The B unit cell axes in Cartesian coordinates in a Euclidean space.
        /// </summary>
        public Vector3 B
        {
            get { return b; }
            set { b = value;  }
        }

        /// <summary>The c axis.</summary>
        private Vector3 c = Vector3.Zero;
        /// <summary>
        /// The C unit cell axes in Cartesian coordinates in a Euclidean space.
        /// </summary>
        public Vector3 C
        {
            get { return c; }
            set { c = value;  }
        }

         /// <summary>Number of symmetry related atoms.</summary>
        private string spaceGroup = "P1";

        /// <summary>
        /// The space group of this crystal.
        /// </summary>
        public string SpaceGroup
        {
            get { return spaceGroup; }
            set { spaceGroup = value;  }
        }

        /// <summary>Number of symmetry related atoms.</summary>
        private int? z = 1;
        /// <summary>
        /// The number of asymmetric parts in the unit cell.
        /// </summary>
        public int? Z
        {
            get { return z; }
            set { z = value;  }
        }

        /// <summary>
        /// Constructs a new crystal with zero length cell axis.
        /// </summary>
        public Crystal()
            : base()
        {
        }

        /// <summary>
        /// Constructs a new crystal with zero length cell axis
        /// and adds the atoms in the AtomContainer as cell content.
        /// </summary>
        /// <param name="container">the AtomContainer providing the atoms and bonds</param>
        public Crystal(IAtomContainer container)
            : base(container)
        {
        }
    }
}
