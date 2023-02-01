// Copyright (C) 2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>
// This file is under LGPL-2.1 


namespace NCDK
{
    using System.Collections.Generic;

    public partial interface IChemObject 
    {
        /// <summary>
        /// Sets a property for a <see cref="IChemObject"/>.
        /// </summary>
        /// <param name="description">An object description of the property (most likely a unique string)</param>
        /// <param name="value">An object with the property itself</param>
        /// <seealso cref="GetProperty{T}(object)"/>
        /// <seealso cref="RemoveProperty(object)"/>
        void SetProperty(object description, object value);

        /// <summary>
        /// Set the properties of this object to the provided map (shallow copy). Any
        /// existing properties are removed.
        /// </summary>
        /// <param name="properties">key-value pairs</param>
        void SetProperties(IEnumerable<KeyValuePair<object, object>> properties);

        /// <summary>
        /// Add properties to this object, duplicate keys will replace any existing value.
        /// </summary>
        /// <param name="properties"><see cref="KeyValuePair{T, T}"/>s specifying the property values</param>
        void AddProperties(IEnumerable<KeyValuePair<object, object>> properties);

        /// <summary>
        /// Removes a property for a <see cref="IChemObject"/>.
        /// </summary>
        /// <param name="description">The object description of the property (most likely a unique string)</param>
        /// <seealso cref="SetProperty(object, object)"/>
        /// <seealso cref="GetProperty{T}(object)"/>
        void RemoveProperty(object description);

        /// <summary>
        /// Returns a property for the <see cref="IChemObject"/> - the object is automatically
        /// cast to <typeparamref name="T"/> type. 
        /// </summary>
        /// <typeparam name="T">Generic return type</typeparam>
        /// <param name="description">An object description of the property</param>
        /// <returns>The property</returns>
        /// <exception cref="System.InvalidCastException">If the wrong type is provided.</exception>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Property_Example.cs"]/*' />
        /// </example>
        /// <seealso cref="SetProperty(object, object)"/>
        /// <seealso cref="GetProperties"/>
        /// <seealso cref="RemoveProperty(object)"/>
        T GetProperty<T>(object description);

        /// <summary>
        /// Returns a property for the <see cref="IChemObject"/> or <paramref name="defaultValue"/> if the <paramref name="description"/> key is not in it. 
        /// </summary>
        /// <typeparam name="T">Generic return type</typeparam>
        /// <param name="description">An object description of the property</param>
        /// <param name="defaultValue">A default value</param>
        /// <returns>The property</returns>
        T GetProperty<T>(object description, T defaultValue);

        /// <summary>
        /// Returns a <see cref="IDictionary{T,T}"/> with the <see cref="IChemObject"/>'s properties.
        /// </summary>
        /// <returns>The object's properties as an <see cref="IDictionary{T, T}"/></returns>
        /// <seealso cref="AddProperties(IEnumerable{KeyValuePair{object, object}})"/>
        IReadOnlyDictionary<object, object> GetProperties();
    }    
}
namespace NCDK
{
    using System.Collections.Generic;

    public partial interface IMolecularFormula 
    {
        /// <summary>
        /// Sets a property for a <see cref="IMolecularFormula"/>.
        /// </summary>
        /// <param name="description">An object description of the property (most likely a unique string)</param>
        /// <param name="value">An object with the property itself</param>
        /// <seealso cref="GetProperty{T}(object)"/>
        /// <seealso cref="RemoveProperty(object)"/>
        void SetProperty(object description, object value);

        /// <summary>
        /// Set the properties of this object to the provided map (shallow copy). Any
        /// existing properties are removed.
        /// </summary>
        /// <param name="properties">key-value pairs</param>
        void SetProperties(IEnumerable<KeyValuePair<object, object>> properties);

        /// <summary>
        /// Add properties to this object, duplicate keys will replace any existing value.
        /// </summary>
        /// <param name="properties"><see cref="KeyValuePair{T, T}"/>s specifying the property values</param>
        void AddProperties(IEnumerable<KeyValuePair<object, object>> properties);

        /// <summary>
        /// Removes a property for a <see cref="IMolecularFormula"/>.
        /// </summary>
        /// <param name="description">The object description of the property (most likely a unique string)</param>
        /// <seealso cref="SetProperty(object, object)"/>
        /// <seealso cref="GetProperty{T}(object)"/>
        void RemoveProperty(object description);

        /// <summary>
        /// Returns a property for the <see cref="IMolecularFormula"/> - the object is automatically
        /// cast to <typeparamref name="T"/> type. 
        /// </summary>
        /// <typeparam name="T">Generic return type</typeparam>
        /// <param name="description">An object description of the property</param>
        /// <returns>The property</returns>
        /// <exception cref="System.InvalidCastException">If the wrong type is provided.</exception>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Property_Example.cs"]/*' />
        /// </example>
        /// <seealso cref="SetProperty(object, object)"/>
        /// <seealso cref="GetProperties"/>
        /// <seealso cref="RemoveProperty(object)"/>
        T GetProperty<T>(object description);

        /// <summary>
        /// Returns a property for the <see cref="IMolecularFormula"/> or <paramref name="defaultValue"/> if the <paramref name="description"/> key is not in it. 
        /// </summary>
        /// <typeparam name="T">Generic return type</typeparam>
        /// <param name="description">An object description of the property</param>
        /// <param name="defaultValue">A default value</param>
        /// <returns>The property</returns>
        T GetProperty<T>(object description, T defaultValue);

        /// <summary>
        /// Returns a <see cref="IDictionary{T,T}"/> with the <see cref="IMolecularFormula"/>'s properties.
        /// </summary>
        /// <returns>The object's properties as an <see cref="IDictionary{T, T}"/></returns>
        /// <seealso cref="AddProperties(IEnumerable{KeyValuePair{object, object}})"/>
        IReadOnlyDictionary<object, object> GetProperties();
    }    
}
