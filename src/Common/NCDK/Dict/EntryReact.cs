/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

using System.Collections.Generic;
using System.Linq;

namespace NCDK.Dict
{
    /// <summary>
    /// Entry in a Dictionary for reactions.
    /// </summary>
    /// <seealso cref="EntryDictionary"/>
    // @author       Miguel Rojas <miguelrojasch@users.sf.net>
    // @cdk.created  2008-01-01
    // @cdk.keyword  dictionary
    // @cdk.module   dict
    public class EntryReact : Entry
    {
        private readonly List<string> reactionInfo;
        private readonly List<string> representations;
        private readonly Dictionary<string, string> parameters;
        private readonly List<string> parametersValue;
        private readonly List<string> reactionExample;
        private readonly List<IReadOnlyList<string>> parameterClass;

        /// <summary>
        /// Constructor of the EntryReact.
        /// </summary>
        /// <param name="identifier">The ID value</param>
        /// <param name="term"></param>
        public EntryReact(string identifier, string term)
            : base(identifier, term)
        {
            this.representations = new List<string>();
            this.parameters = new Dictionary<string, string>();
            this.parametersValue = new List<string>();
            this.reactionExample = new List<string>();
            this.parameterClass = new List<IReadOnlyList<string>>();
            this.reactionInfo = new List<string>();
        }

        /// <summary>
        /// Constructor of the EntryReact.
        /// </summary>
        /// <param name="identifier">The ID value</param>
        public EntryReact(string identifier)
            : this(identifier, "")
        {
        }

        public void AddReactionMetadata(string metadata)
        {
            this.reactionInfo.Add(metadata);
        }

        public IReadOnlyList<string> ReactionMetadata => this.reactionInfo;

        /// <summary>
        /// Set the representation of the reaction.
        /// </summary>
        /// <param name="contentRepr">The representation of the reaction as string</param>
        public void AddRepresentation(string contentRepr)
        {
            this.representations.Add(contentRepr);
        }

        /// <summary>
        /// The Representation of the reaction.
        /// </summary>
        public IReadOnlyList<string> Representations => this.representations;

        /// <summary>
        /// Set the parameters of the reaction.
        /// </summary>
        /// <param name="nameParam">The parameter names of the reaction as string</param>
        /// <param name="typeParam">The parameter types of the reaction as string</param>
        /// <param name="value">The value default of the parameter</param>
        public void SetParameters(string nameParam, string typeParam, string value)
        {
            this.parameters.Add(nameParam, typeParam);
            this.parametersValue.Add(value);
        }

        /// <summary>
        /// The parameters of the reaction.
        /// </summary>
        public IReadOnlyDictionary<string, string> Parameters => this.parameters;

        /// <summary>
        /// The IParameterReact's of the reaction.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> ParameterClass=> this.parameterClass;

        /// <summary>
        /// Add a IParameterReact's of the reaction.
        /// </summary>
        /// <param name="param">Strings containing the information about this parameter.</param>
        public void AddParameter(IEnumerable<string> param)
        {
            this.parameterClass.Add(param.ToReadOnlyList());
        }

        /// <summary>
        /// The parameter value of the reaction.
        /// </summary>
        public IReadOnlyList<string> ParameterValue => this.parametersValue;

        /// <summary>
        /// The mechanism of this reaction.
        /// </summary>
        public string Mechanism { get; set; }

        /// <summary>
        /// add a example for this reaction.
        /// </summary>
        /// <param name="xml">A reaction in XML scheme</param>
        public void AddExampleReaction(string xml)
        {
            this.reactionExample.Add(xml);
        }

        /// <summary>
        /// A List of reactions in XML schema.
        /// </summary>
        public IReadOnlyList<string> ExampleReactions => this.reactionExample;
    }
}
