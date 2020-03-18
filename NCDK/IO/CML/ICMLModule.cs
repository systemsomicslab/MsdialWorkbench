/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
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
    /// This interface describes the procedures classes must implement to be plugable into the <see cref="CMLHandler"/>.
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public interface ICMLModule
    {
        void StartDocument();
        void EndDocument();
        void StartElement(CMLStack xpath, XElement element);
        void EndElement(CMLStack xpath, XElement element);
        void CharacterData(CMLStack xpath, XElement element);
        IChemFile ReturnChemFile();
        void Inherit(ICMLModule conv);
    }
}
