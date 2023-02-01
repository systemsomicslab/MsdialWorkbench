/* 
 * Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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

namespace NCDK.Config.AtomTypes
{
    internal static class OWLConstants
    {
        public static readonly XNamespace NS_AtomType = "http://cdk.sf.net/ontologies/atomtypes#";
        public static readonly XName XName_AtomType = NS_AtomType + "AtomType";
        public static readonly XName XName_hasElement = NS_AtomType + "hasElement";
        public static readonly XName XName_formalCharge = NS_AtomType + "formalCharge";
        public static readonly XName XName_formalNeighbourCount = NS_AtomType + "formalNeighbourCount";
        public static readonly XName XName_lonePairCount = NS_AtomType + "lonePairCount";
        public static readonly XName XName_singleElectronCount = NS_AtomType + "singleElectronCount";
        public static readonly XName XName_piBondCount = NS_AtomType + "piBondCount";
        public static readonly XName XName_hybridization = NS_AtomType + "hybridization";
        public static readonly XName XName_formalBondType = NS_AtomType + "formalBondType";

        public static readonly XNamespace NS_rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public static readonly XName XName_ID = NS_rdf + "ID";
        public static readonly XName XName_rdf_about = NS_rdf + "about";
        public static readonly XName XName_rdf_resource = NS_rdf + "resource";

        public static readonly XNamespace NS_AtomTypeMapping = "http://cdk.sf.net/ontologies/atomtypemappings#";

        public static readonly XName XName_AtomTypeMapping_mapsToType = NS_AtomTypeMapping + "mapsToType";
        public static readonly XName XName_AtomTypeMapping_equivalentAsType = NS_AtomTypeMapping + "equivalentAsType";

        public static readonly XNamespace NS_OWL = "http://www.w3.org/2002/07/owl#";

        public static readonly XName XName_OWL_Thing = NS_OWL + "Thing";
    }
}
