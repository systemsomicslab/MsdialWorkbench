



using System.Collections.Generic;

namespace NCDK.Formula
{
    public partial class MolecularFormula : IMolecularFormula
    {
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
    }
}
