/* Copyright (C) 2006-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
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

using System;

namespace NCDK.Reactions
{
    /// <summary>
    /// Class that is used to distribute reactions specifications.
    /// </summary>
    // @author      Miguel Rojas
    // @cdk.module  reaction
    public class ReactionSpecification : IImplementationSpecification
    {
        private string specificationReference;
        private string implementationTitle;
        private string implementationIdentifier;
        private string implementationVendor;

        /// <summary>
        /// Container for specifying the type of reaction.
        /// </summary>
        /// <param name="specificationReference">Reference to a formal definition in a
        ///          dictionary (e.g. in STMML format) of the descriptor, preferably
        ///          refering to the original article. The format of the content is
        ///          expected to be &lt;dictionaryNameSpace&gt;:&lt;entryID&gt;.</param>
        /// <param name="implementationTitle">Title for the reaction process.</param>
        /// <param name="implementationIdentifier">Unique identifier for the actual
        ///          implementation, preferably including the exact version number of
        ///          the source code. E.g. $Id$ can be used when the source code is
        ///          in a CVS repository.</param>
        /// <param name="implementationVendor">Name of the organisation/person/program/whatever
        ///          who wrote/packaged the implementation.</param>
        public ReactionSpecification(string specificationReference, string implementationTitle,
                string implementationIdentifier, string implementationVendor)
        {
            this.specificationReference = specificationReference;
            this.implementationTitle = implementationTitle;
            this.implementationIdentifier = implementationIdentifier;
            this.implementationVendor = implementationVendor;
        }


        public string SpecificationReference => this.specificationReference;

        public string ImplementationTitle => this.implementationTitle;

        public string ImplementationIdentifier => this.implementationIdentifier;

        public string ImplementationVendor => this.implementationVendor;

        string IImplementationSpecification.SpecificationReference
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string IImplementationSpecification.ImplementationTitle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string IImplementationSpecification.ImplementationIdentifier
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string IImplementationSpecification.ImplementationVendor
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
