/* Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
 *
 *  Contact: cdk-devel@list.sourceforge.net
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

using System.Collections;
using System.Collections.Generic;
using System.Windows.Media;

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// A group of rendering elements, of any type.
    /// </summary>
    // @cdk.module  renderbasic
    public class ElementGroup 
        : IRenderingElement, IEnumerable<IRenderingElement>
    {
        /// <summary>
        /// The elements in the group.
        /// </summary>
        private readonly List<IRenderingElement> elements;

        /// <summary>
        /// Create an empty element group.
        /// </summary>
        public ElementGroup()
        {
            elements = new List<IRenderingElement>();
        }

        public IEnumerator<IRenderingElement> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Add a new element to the group.
        /// </summary>
        /// <param name="element">the element to add to the group</param>
        public void Add(IRenderingElement element)
        {
            if (element != null)
            {
                if (element is ElementGroup e)
                    elements.AddRange(e.elements);
                else
                    elements.Add(element);
            }
        }


        /// <summary>
        /// Visit the members of the group.
        /// </summary>
        /// <param name="visitor">the class that will be visiting each element</param>
        public void Visit(IRenderingVisitor visitor)
        {
            foreach (var child in this.elements)
            {
                child.Accept(visitor);
            }
        }

        public void Accept(IRenderingVisitor visitor, Transform transform)
        {
            visitor.Visit(this, transform);
        }

        public virtual void Accept(IRenderingVisitor v)
        {
            v.Visit(this);
        }
    }
}
