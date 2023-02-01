/* 
 * Copyright (C) 2008  Miguel Rojas <miguelrojasch@users.sf.net>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Dict;
using NCDK.Reactions.Types.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Reactions
{
    /// <summary>
    /// The base class for all chemical reactions objects in this cdk.
    /// It provides methods for adding parameters
    /// </summary>
    // @author         Miguel Rojas
    // @cdk.created    2008-02-01
    // @cdk.module     reaction
    public class ReactionEngine
    {
        private EntryDictionary dictionary;

        public IReadOnlyDictionary<string, object> ParamsMap { get; set; }
        public IReactionMechanism Mechanism { get; set; }

        public ReactionEngine()
        {
            try
            {
                var reaction = (IReactionProcess)this;
                var entry = InitiateDictionary("reaction-processes", reaction);
                InitiateParameterMap2(entry);
                reaction.ParameterList = ParameterList;
                // extract mechanism dependence, if there is one
                if (!string.IsNullOrEmpty(entry.Mechanism))
                    ExtractMechanism(entry);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
        }

        /// <summary>
        /// Extract the mechanism necessary for this reaction.
        /// </summary>
        /// <param name="entry">The EntryReact object</param>
        private void ExtractMechanism(EntryReact entry)
        {
            var mechanismName = $"NCDK.Reactions.Mechanisms.{entry.Mechanism}";
            try
            {
                Mechanism = (IReactionMechanism)this.GetType().Assembly.GetType(mechanismName).GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
                Trace.TraceInformation("Loaded mechanism: ", mechanismName);
            }
            catch (ArgumentException exception)
            {
                Trace.TraceError($"Could not find this IReactionMechanism: {mechanismName}");
                Debug.WriteLine(exception);
            }
            catch (Exception exception)
            {
                Trace.TraceError($"Could not load this IReactionMechanism: {mechanismName}");
                Debug.WriteLine(exception);
            }
        }

        /*protected*/
        internal void CheckInitiateParams(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents)
        {
            Debug.WriteLine($"initiate reaction: {GetType().Name}");

            if (reactants.Count != 1)
            {
                throw new CDKException($"{GetType().Name} only expects one reactant");
            }
            if (agents != null)
            {
                throw new CDKException($"{GetType().Name} don't expects agents");
            }
        }

        /// <summary>
        /// Open the Dictionary OWLReact.
        /// </summary>
        /// <param name="nameDict">Name of the Dictionary</param>
        /// <param name="reaction">The IReactionProcess</param>
        /// <returns>The entry for this reaction</returns>
        private EntryReact InitiateDictionary(string nameDict, IReactionProcess reaction)
        {
            var db = new DictionaryDatabase();
            dictionary = db.GetDictionary(nameDict);
            var entryString = reaction.Specification.SpecificationReference;
            entryString = entryString.Substring(entryString.IndexOf('#') + 1);

            return (EntryReact)dictionary[entryString.ToLowerInvariant()];
        }

        /// <summary>
        /// Creates a map with the name and type of the parameters.
        /// </summary>
        private void InitiateParameterMap2(EntryReact entry)
        {
            var paramDic = entry.ParameterClass;

            this.ParameterList = new List<IParameterReaction>();
            foreach (var param in paramDic)
            {
                string paramName = "NCDK.Reactions.Types.Parameters." + param[0];
                try
                {
                    var ipc = (IParameterReaction)this.GetType().Assembly.GetType(paramName).GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
                    ipc.IsSetParameter = bool.Parse(param[1]);
                    ipc.Value = param[2];

                    Trace.TraceInformation("Loaded parameter class: ", paramName);
                    ParameterList.Add(ipc);
                }
                catch (ArgumentException exception)
                {
                    Trace.TraceError($"Could not find this IParameterReact: {paramName}");
                    Debug.WriteLine(exception);
                }
                catch (Exception exception)
                {
                    Trace.TraceError($"Could not load this IParameterReact: {paramName}");
                    Debug.WriteLine(exception);
                }
            }
        }

        /// <summary>
        /// The current parameter Dictionary for this reaction.
        /// </summary>
        /// <remarks>Must be done before calling calculate as the parameters influence the calculation outcome.</remarks>
        public IList<IParameterReaction> ParameterList { get; set; }

        /// <summary>
        /// Return the IParameterReact if it exists given the class.
        /// </summary>
        /// <param name="paramClass">The class</param>
        /// <returns>The IParameterReact</returns>
        public IParameterReaction GetParameterClass(Type paramClass)
        {
            foreach (var ipr in ParameterList)
            {
                if (ipr.GetType().Equals(paramClass))
                    return ipr;
            }

            return null;
        }
    }
}
