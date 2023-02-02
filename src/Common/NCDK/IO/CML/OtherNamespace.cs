/* Copyright (C) 2014  Egon Willighagen <egonw@users.sf.net>
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

using System.Xml.Linq;

namespace NCDK.IO.CML
{
    /// <summary>
    /// Modules that ignore all content, for use in XML in namespaces other than CML.
    /// </summary>
    // @author egonw
    sealed class OtherNamespace : ICMLModule
    {
        /// <inheritdoc/>
        public void StartDocument()
        {
            // ignore content from other namespaces
        }

        /// <inheritdoc/>
        public void EndDocument()
        {
            // ignore content from other namespaces
        }

        /// <inheritdoc/>
        public void StartElement(CMLStack xpath, XElement element)
        {
            // ignore content from other namespaces
        }

        /// <inheritdoc/>
        public void EndElement(CMLStack xpath, XElement element)
        {
            // ignore content from other namespaces
        }

        public void CharacterData(CMLStack xpath, XElement element)
        {
            // ignore content from other namespaces
        }

        /// <inheritdoc/>
        public IChemFile ReturnChemFile()
        {
            // ignore content from other namespaces
            return null;
        }

        /// <inheritdoc/>
        public void Inherit(ICMLModule conv) { }
    }
}
