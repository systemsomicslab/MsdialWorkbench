/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
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
using System.Collections;
using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// The base class for all data objects in this CDK.
    /// </summary>
    // @author        egonw
    // @cdk.module    interfaces
    public interface ICDKObject
        : ICloneable
    {
        /// <summary>
        /// <see cref="IChemObjectBuilder"/> for the data classes that extend this class.
        /// </summary>
        /// <value>The <see cref="IChemObjectBuilder"/> matching this <see cref="ICDKObject"/></value>
        IChemObjectBuilder Builder { get; }

        /// <summary>
        /// A deep clone of this object.
        /// </summary>
        /// <param name="map">A map of the original atoms/bonds to the cloned atoms/bonds.</param>
        /// <returns>Object the clone of this object.</returns>
        ICDKObject Clone(CDKObjectMap map);
    }

    /// <summary>
    /// A mapping of the original atoms/bonds to the cloned atoms/bonds.
    /// </summary>
    public class CDKObjectMap
    {
        internal Dictionary<IChemObject, IChemObject> map = null;

        Dictionary<IChemObject, IChemObject> Map
        {
            get
            {
                if (map == null)
                {
                    map = new Dictionary<IChemObject, IChemObject>();
                }
                return map;
            }
        }

        public CDKObjectMap()
        { }

        public void Add<T>(T a, T b) where T : IChemObject
        {
            Map.Add(a, b);
        }

        public T Get<T>(T key) where T : IChemObject
        {
            return (T)Map[key];
        }

        public void Set<T>(T key, T value) where T : IChemObject
        {
            Map[key] = value;
        }

        public bool TryGetValue<T>(T key, out T value) where T : IChemObject
        {
            bool ret = Map.TryGetValue(key, out IChemObject v);
            value = ret ? (T)v : default(T);
            return ret;
        }

        public bool ContainsKey<T>(T key) where T : IChemObject
        {
            return Map.ContainsKey(key);
        }

        public bool ContainsValue<T>(T key) where T : IChemObject
        {
            return Map.ContainsValue(key);
        }

        public bool Any()
        {
            return Map.Count > 0;
        }
    }
}
