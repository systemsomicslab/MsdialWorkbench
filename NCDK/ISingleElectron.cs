/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
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

namespace NCDK
{
    /// <summary>
    /// A single electron is an orbital which is occupied by only one electron.
    /// </summary>
    /// <example>
    /// A radical in CDK is represented by an <see cref="IAtomContainer"/> that contains an <see cref="IAtom"/>  
    /// and a <see cref="ISingleElectron"/> type <see cref="IElectronContainer"/>:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.ISingleElectron_Example.cs"]/*' />
    /// </example>
    // @cdk.module interfaces
    // @cdk.keyword radical
    // @cdk.keyword electron, unpaired
    public interface ISingleElectron 
        : IElectronContainer
    {
        /// <summary>
        /// The associated <see cref="IAtom"/>.
        /// </summary>
        IAtom Atom { get; set; }

        /// <summary>
        /// Returns <see langword="true"/> if the given atom participates in this <see cref="ISingleElectron"/>.
        /// </summary>
        /// <param name="atom">The atom to be tested if it participates in this bond</param>
        /// <returns><see langword="true"/> if this <see cref="ISingleElectron"/> is associated with the atom</returns>
        bool Contains(IAtom atom);
    }
}
