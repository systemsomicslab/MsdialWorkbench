/*
 * Copyright (c) 2017 John Mayfield <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Collections.Generic;

namespace NCDK
{
    public class ChemObjectRef 
        : IChemObject
    {
        private readonly IChemObject chemobj;

        internal ChemObjectRef(IChemObject chemobj)
        {
            this.chemobj = chemobj ?? throw new ArgumentNullException(nameof(chemobj), "Proxy object can not be null!");
        }

        /// <inheritdoc/>
        public IChemObjectBuilder Builder => chemobj.Builder;

        /// <inheritdoc/>
        public ICollection<IChemObjectListener> Listeners => chemobj.Listeners;

        /// <inheritdoc/>
        public bool Notification { get; set; }

        /// <inheritdoc/>
        public void NotifyChanged()
        {
            chemobj.NotifyChanged();
        }

        /// <inheritdoc/>
        public virtual void NotifyChanged(ChemObjectChangeEventArgs evt)
        {
            if (Notification)
            {
                foreach (var listener in Listeners)
                {
                    listener.OnStateChanged(evt);
                }
            }
        }

        /// <inheritdoc/>
        public void SetProperty(object description, object property)
        {
            chemobj.SetProperty(description, property);
        }

        /// <inheritdoc/>
        public void RemoveProperty(object description)
        {
            chemobj.RemoveProperty(description);
        }

        /// <inheritdoc/>
        public T GetProperty<T>(object description)
        {
            return chemobj.GetProperty<T>(description);
        }

        public T GetProperty<T>(object description, T defaultValue)
        {
            return chemobj.GetProperty(description, defaultValue);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<object, object> GetProperties()
        {
            return chemobj.GetProperties();
        }

        /// <inheritdoc/>
        public string Id
        {
            get { return chemobj.Id; }
            set { chemobj.Id = value; }
        }

        public bool IsPlaced { get { return chemobj.IsPlaced; } set { chemobj.IsPlaced = value; } }
        public bool IsVisited { get { return chemobj.IsVisited; } set { chemobj.IsVisited = value; } }
        
        public void SetProperties(IEnumerable<KeyValuePair<object, object>> properties)
        {
            chemobj.SetProperties(properties);
        }

        public void AddProperties(IEnumerable<KeyValuePair<object, object>> properties)
        {
            chemobj.AddProperties(properties);
        }

        public virtual bool Compare(object obj)
        {
            return chemobj.Compare(obj);
        }

        /// <inheritdoc/>
        public virtual object Clone()
        {
            return Clone(new CDKObjectMap());
        }

        public virtual ICDKObject Clone(CDKObjectMap map)
        {
            return new ChemObjectRef((IChemObject)chemobj.Clone(map));
        }
    }
}
