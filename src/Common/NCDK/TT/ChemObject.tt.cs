



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
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
 *
 */

using NCDK.Common.Collections;
using System.Collections.Generic;

namespace NCDK.Default
{
    /// <summary>
    /// The base class for all chemical objects in this cdk. It provides methods for
    /// adding listeners and for their notification of events, as well a a hash
    /// table for administration of physical or chemical properties
    /// </summary>
    // @author        steinbeck
    // @cdk.module data
    public class ChemObject
        : IChemObject
    {
        /*private protected*/
        internal int flags;

        private ICollection<IChemObjectListener> listeners;
        private bool notification = true;

        public virtual IChemObjectBuilder Builder => ChemObjectBuilder.Instance;

        /// <summary>
        /// List for listener administration.
        /// </summary>
        public ICollection<IChemObjectListener> Listeners 
        { 
            get
            {
                if (listeners == null)
                    listeners = new HashSet<IChemObjectListener>(); 
                return listeners;
            }
        }

        public bool Notification
        {
            get => notification;
            set => notification = value;
        }

        public bool IsPlaced
        {
            get 
            { 
                return (flags & CDKConstants.IsPlacedMask) != 0;
            }
            
            set
            {
                if (value)
                    flags |= CDKConstants.IsPlacedMask;
                else
                    flags &= ~CDKConstants.IsPlacedMask;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Flag is set if chemobject has been visited
        /// </summary>
        public bool IsVisited
        {
            get 
            { 
                return (flags & CDKConstants.IsVisitedMask) != 0;
            }
            
            set
            {
                if (value)
                    flags |= CDKConstants.IsVisitedMask;
                else
                    flags &= ~CDKConstants.IsVisitedMask;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Constructs a new IChemObject.
        /// </summary>
        public ChemObject()
            : this(null)
        {
        }

        /// <summary>
        /// Constructs a new IChemObject by copying the flags, and the. It does not copy the listeners and properties.
        /// </summary>
        /// <param name="chemObject">the object to copy</param>
        public ChemObject(IChemObject chemObject)
        {
            if (chemObject != null)
            {
                // copy the flags
                IsVisited = chemObject.IsVisited;
                IsPlaced = chemObject.IsPlaced;
                // copy the identifier
                id = chemObject.Id;
            }
        }

        /// <summary>
        /// This should be triggered by an method that changes the content of an object
        ///  to that the registered listeners can react to it.
        /// </summary>
        public void NotifyChanged()
        {
            if (Notification)
                NotifyChanged(new ChemObjectChangeEventArgs(this));
        }

        /// <summary>
        /// This should be triggered by an method that changes the content of an object
        /// to that the registered listeners can react to it. This is a version of
        /// NotifyChanged() which allows to propagate a change event while preserving
        /// the original origin.
        /// </summary>
        /// <param name="evt">A ChemObjectChangeEvent pointing to the source of where the change happened</param>
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

#if DEBUG
        private static IList<System.Type> AcceptablePropertyKeyTypes { get; } = new List<System.Type>()
        {
            typeof(string),
            typeof(NCDK.Dict.DictRef),
			typeof(System.Guid),
        };
#endif

        /// <summary>
        /// A dictionary for the storage of any kind of properties of this object.
        /// </summary>
        private Dictionary<object, object> properties;

        private void InitProperties()
        {
            properties = new Dictionary<object, object>();
        }

        /// <inheritdoc/>
        public virtual void SetProperty(object description, object value)
        {
#if DEBUG
            if (description != null && !AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            if (this.properties == null)
                InitProperties();
            properties[description] = value;
            NotifyChanged();
        }

        /// <inheritdoc/>
        public virtual void RemoveProperty(object description)
        {
#if DEBUG
            if (!AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            if (this.properties == null)
                return;
            var removed = properties.Remove(description);
            if (removed)
                NotifyChanged();
        }

        /// <inheritdoc/>
        public virtual T GetProperty<T>(object description)
        {
#if DEBUG
            if (!AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            return GetProperty(description, default(T));
        }

        /// <inheritdoc/>
        public virtual T GetProperty<T>(object description, T defaultValue)
        {
#if DEBUG
            if (!AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            if (this.properties == null)
                return defaultValue;
            if (properties.TryGetValue(description, out object property) && property != null)
                return (T)property;
            return defaultValue;
        }

        private static readonly IReadOnlyDictionary<object, object> emptyProperties = NCDK.Common.Collections.Dictionaries.Empty<object, object>();

        /// <inheritdoc/>
        public virtual IReadOnlyDictionary<object, object> GetProperties() 
        {
            if (this.properties == null)
                return emptyProperties;
            return this.properties;
        }

        /// <inheritdoc/>
        public void SetProperties(IEnumerable<KeyValuePair<object, object>> properties)
        {
            this.properties = null;
            if (properties == null)
                return;
            AddProperties(properties);
        }

        /// <inheritdoc/>
        public virtual void AddProperties(IEnumerable<KeyValuePair<object, object>> properties)
        {
            if (properties == null)
                return;
            if (this.properties == null)
                InitProperties();
            foreach (var pair in properties)
                this.properties[pair.Key] = pair.Value;
            NotifyChanged();
        }

        public virtual object Clone()
        {
            return Clone(new CDKObjectMap());
        }

        public virtual ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (ChemObject)MemberwiseClone();

            // clone the properties - using the Dictionary copy constructor
            // this doesn't deep clone the keys/values but this wasn't happening
            // already
            clone.SetProperties(this.properties);
            // delete all listeners
            clone.listeners = null;
            return clone;
        }

        /// <summary>
        /// Compares a <see cref="IChemObject"/> with this <see cref="IChemObject"/>.
        /// </summary>
        /// <param name="obj">Object of type <see cref="AtomType"/></param>
        /// <returns><see langword="true"/> if the atom types are equal</returns>
        public virtual bool Compare(object obj)
        {
            return !(obj is IChemObject o) ? false : Id == o.Id;
        }

        private string id;
        /// <summary>
        /// The identifier (ID) of this object.
        /// </summary>
        public string Id
        {
            get { return id; }
            set
            {
                id = value;
                NotifyChanged();
            }
        }

        public override string ToString() => CDKStuff.ToString(this);
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// The base class for all chemical objects in this cdk. It provides methods for
    /// adding listeners and for their notification of events, as well a a hash
    /// table for administration of physical or chemical properties
    /// </summary>
    // @author        steinbeck
    // @cdk.module data
    public class ChemObject
        : IChemObject
    {
        /*private protected*/
        internal int flags;

        private ICollection<IChemObjectListener> listeners;
        private bool notification = true;

        public virtual IChemObjectBuilder Builder => ChemObjectBuilder.Instance;

        /// <summary>
        /// List for listener administration.
        /// </summary>
        public ICollection<IChemObjectListener> Listeners 
        { 
            get
            {
                if (listeners == null)
                    listeners = new ImmutableCollection<IChemObjectListener>(); 
                return listeners;
            }
        }

        public bool Notification
        {
            get => notification;
            set => notification = value;
        }

        public bool IsPlaced
        {
            get 
            { 
                return (flags & CDKConstants.IsPlacedMask) != 0;
            }
            
            set
            {
                if (value)
                    flags |= CDKConstants.IsPlacedMask;
                else
                    flags &= ~CDKConstants.IsPlacedMask;
            }
        }

        /// <summary>
        /// Flag is set if chemobject has been visited
        /// </summary>
        public bool IsVisited
        {
            get 
            { 
                return (flags & CDKConstants.IsVisitedMask) != 0;
            }
            
            set
            {
                if (value)
                    flags |= CDKConstants.IsVisitedMask;
                else
                    flags &= ~CDKConstants.IsVisitedMask;
            }
        }

        /// <summary>
        /// Constructs a new IChemObject.
        /// </summary>
        public ChemObject()
            : this(null)
        {
        }

        /// <summary>
        /// Constructs a new IChemObject by copying the flags, and the. It does not copy the listeners and properties.
        /// </summary>
        /// <param name="chemObject">the object to copy</param>
        public ChemObject(IChemObject chemObject)
        {
            if (chemObject != null)
            {
                // copy the flags
                IsVisited = chemObject.IsVisited;
                IsPlaced = chemObject.IsPlaced;
                // copy the identifier
                id = chemObject.Id;
            }
        }

        /// <summary>
        /// This should be triggered by an method that changes the content of an object
        ///  to that the registered listeners can react to it.
        /// </summary>
        public void NotifyChanged()
        {
        }

        /// <summary>
        /// This should be triggered by an method that changes the content of an object
        /// to that the registered listeners can react to it. This is a version of
        /// NotifyChanged() which allows to propagate a change event while preserving
        /// the original origin.
        /// </summary>
        /// <param name="evt">A ChemObjectChangeEvent pointing to the source of where the change happened</param>
        public virtual void NotifyChanged(ChemObjectChangeEventArgs evt)
        {
        }

#if DEBUG
        private static IList<System.Type> AcceptablePropertyKeyTypes { get; } = new List<System.Type>()
        {
            typeof(string),
            typeof(NCDK.Dict.DictRef),
			typeof(System.Guid),
        };
#endif

        /// <summary>
        /// A dictionary for the storage of any kind of properties of this object.
        /// </summary>
        private Dictionary<object, object> properties;

        private void InitProperties()
        {
            properties = new Dictionary<object, object>();
        }

        /// <inheritdoc/>
        public virtual void SetProperty(object description, object value)
        {
#if DEBUG
            if (description != null && !AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            if (this.properties == null)
                InitProperties();
            properties[description] = value;
        }

        /// <inheritdoc/>
        public virtual void RemoveProperty(object description)
        {
#if DEBUG
            if (!AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            if (this.properties == null)
                return;
            var removed = properties.Remove(description);
        }

        /// <inheritdoc/>
        public virtual T GetProperty<T>(object description)
        {
#if DEBUG
            if (!AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            return GetProperty(description, default(T));
        }

        /// <inheritdoc/>
        public virtual T GetProperty<T>(object description, T defaultValue)
        {
#if DEBUG
            if (!AcceptablePropertyKeyTypes.Contains(description.GetType()))
                throw new System.Exception();
#endif
            if (this.properties == null)
                return defaultValue;
            if (properties.TryGetValue(description, out object property) && property != null)
                return (T)property;
            return defaultValue;
        }

        private static readonly IReadOnlyDictionary<object, object> emptyProperties = NCDK.Common.Collections.Dictionaries.Empty<object, object>();

        /// <inheritdoc/>
        public virtual IReadOnlyDictionary<object, object> GetProperties() 
        {
            if (this.properties == null)
                return emptyProperties;
            return this.properties;
        }

        /// <inheritdoc/>
        public void SetProperties(IEnumerable<KeyValuePair<object, object>> properties)
        {
            this.properties = null;
            if (properties == null)
                return;
            AddProperties(properties);
        }

        /// <inheritdoc/>
        public virtual void AddProperties(IEnumerable<KeyValuePair<object, object>> properties)
        {
            if (properties == null)
                return;
            if (this.properties == null)
                InitProperties();
            foreach (var pair in properties)
                this.properties[pair.Key] = pair.Value;
        }

        public virtual object Clone()
        {
            return Clone(new CDKObjectMap());
        }

        public virtual ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (ChemObject)MemberwiseClone();

            // clone the properties - using the Dictionary copy constructor
            // this doesn't deep clone the keys/values but this wasn't happening
            // already
            clone.SetProperties(this.properties);
            // delete all listeners
            clone.listeners = null;
            return clone;
        }

        /// <summary>
        /// Compares a <see cref="IChemObject"/> with this <see cref="IChemObject"/>.
        /// </summary>
        /// <param name="obj">Object of type <see cref="AtomType"/></param>
        /// <returns><see langword="true"/> if the atom types are equal</returns>
        public virtual bool Compare(object obj)
        {
            return !(obj is IChemObject o) ? false : Id == o.Id;
        }

        private string id;
        /// <summary>
        /// The identifier (ID) of this object.
        /// </summary>
        public string Id
        {
            get { return id; }
            set
            {
                id = value;
            }
        }

        public override string ToString() => CDKStuff.ToString(this);
    }
}
