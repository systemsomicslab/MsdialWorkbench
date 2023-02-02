/* Copyright (C) 2004-2008  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace NCDK.Pharmacophore
{
    /// <summary>
    /// Provides some utility methods for pharmacophore handling.
    /// </summary>
    // @author Rajarshi Guha
    // @cdk.module pcore
    // @cdk.keyword pharmacophore
    // @cdk.keyword 3D isomorphism
    public static class PharmacophoreUtils
    {
        /// <summary>
        /// Read in a set of pharmacophore definitions to create pharmacophore queries.
        /// <para>
        /// Pharmacophore queries can be saved in an XML format which is described XXX. The
        /// file can contain multiple definitions. This method will process all the definitions
        /// and return a list of <see cref="PharmacophoreQuery"/>  objects which can be used with
        /// the <see cref="PharmacophoreMatcher"/> class.
        /// </para>
        /// <para>
        /// The current schema for the document allows one to specify angle and distance
        /// constraints. Currently the CDK does not support angle constraints, so they are
        /// ignored.
        /// </para>
        /// <para>
        /// The schema also specifies a <i>units</i> attribute for a given constraint. The
        /// current reader ignores this and assumes that all distances are in Angstroms.
        /// </para>
        /// <para>
        /// Finally, if there is a description associated with a pharmacophore definition, it is
        /// available as the <i>"description"</i> property of the <see cref="PharmacophoreQuery"/> object.
        /// </para>
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Pharmacophore.PharmacophoreUtils_Example.cs+ReadPharmacophoreDefinitions"]/*' />
        /// </example>
        /// <param name="ins">The stream to read the definitions from</param>
        /// <returns>A <see cref="IList{T}"/> of <see cref="PharmacophoreQuery"/> objects</returns>
        /// <exception cref="CDKException">if there is an error in the format</exception>
        /// <exception cref="IOException"> if there is an error in opening the file</exception>
        /// <see cref="PharmacophoreQueryAtom"/>
        /// <see cref="PharmacophoreQueryBond"/>
        /// <see cref="PharmacophoreQuery"/>
        /// <see cref="PharmacophoreMatcher"/>
        public static IList<PharmacophoreQuery> ReadPharmacophoreDefinitions(Stream ins)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(ins);
            }
            catch (XmlException)
            {
                throw new CDKException("Invalid pharmacophore definition file");
            }
            return Getdefs(doc);
        }

        /// <summary>
        /// Write out one or more pharmacophore queries in the CDK XML format.
        /// </summary>
        /// <param name="query">The pharmacophore queries</param>
        /// <param name="output">The Stream to write to</param>
        /// <exception cref="IOException">if there is a problem writing the XML document</exception>
        public static void WritePharmacophoreDefinition(PharmacophoreQuery query, Stream output)
        {
            WritePharmacophoreDefinition(new PharmacophoreQuery[] { query }, output);
        }

        /// <summary>
        /// Write out one or more pharmacophore queries in the CDK XML format.
        /// </summary>
        /// <param name="queries">The pharmacophore queries</param>
        /// <param name="output">The Stream to write to</param>
        /// <exception cref="IOException">if there is a problem writing the XML document</exception>
        public static void WritePharmacophoreDefinition(IList<PharmacophoreQuery> queries, Stream output)
        {
            WritePharmacophoreDefinition(queries.ToArray(), output);
        }

        /// <summary>
        /// Write out one or more pharmacophore queries in the CDK XML format.
        /// </summary>
        /// <param name="queries">The pharmacophore queries</param>
        /// <param name="output">The Stream to write to</param>
        /// <exception cref="IOException">if there is a problem writing the XML document</exception>
        public static void WritePharmacophoreDefinition(PharmacophoreQuery[] queries, Stream output)
        {
            var root = new XElement("pharmacophoreContainer");
            root.SetAttributeValue("version", "1.0");
            foreach (var query in queries)
            {
                var pcore = new XElement("pharmacophore");

                var description = query.GetProperty<string>("description");
                if (description != null) pcore.SetAttributeValue("description", description);

                var name = query.Title;
                if (name != null) pcore.SetAttributeValue("name", name);

                // we add the pcore groups for this query as local to the group
                foreach (var atom in query.Atoms)
                {
                    var group = new XElement("group");
                    group.SetAttributeValue("id", atom.Symbol);
                    group.Value = ((PharmacophoreQueryAtom)atom).Smarts;
                    pcore.Add(group);
                }

                // now add the constraints
                foreach (var bond in query.Bonds)
                {
                    XElement elem = null;
                    switch (bond)
                    {
                        case PharmacophoreQueryBond dbond:
                            {
                                elem = new XElement("distanceConstraint");
                                elem.SetAttributeValue("lower", dbond.GetLower().ToString(NumberFormatInfo.InvariantInfo));
                                elem.SetAttributeValue("upper", dbond.GetUpper().ToString(NumberFormatInfo.InvariantInfo));
                                elem.SetAttributeValue("units", "A");
                            }
                            break;
                        case PharmacophoreQueryAngleBond dbond:
                            {
                                elem = new XElement("angleConstraint");
                                elem.SetAttributeValue("lower", dbond.GetLower().ToString(NumberFormatInfo.InvariantInfo));
                                elem.SetAttributeValue("upper", dbond.GetUpper().ToString(NumberFormatInfo.InvariantInfo));
                                elem.SetAttributeValue("units", "degrees");
                            }
                            break;
                    }

                    // now add the group associated with this constraint
                    foreach (var iAtom in bond.Atoms)
                    {
                        PharmacophoreQueryAtom atom = (PharmacophoreQueryAtom)iAtom;
                        var gelem = new XElement("groupRef");
                        gelem.SetAttributeValue("id", atom.Symbol);
                        if (elem != null)
                        {
                            elem.Add(gelem);
                        }
                    }
                    pcore.Add(elem);
                }
                root.Add(pcore);
            }
            var doc = new XDocument(root);
            doc.Save(output);
        }

        private static List<PharmacophoreQuery> Getdefs(XDocument doc)
        {
            var root = doc.Root;

            // ltes get the children of the container
            // these will be either group or pharmacophore elems
            List<PharmacophoreQuery> ret = new List<PharmacophoreQuery>();

            // get global group defs
            Dictionary<string, string> groups = GetGroupDefinitions(root);

            //now get the pcore defs
            var children = root.Elements();
            foreach (var e in children)
            {
                if (string.Equals(e.Name.LocalName, "pharmacophore", StringComparison.Ordinal)) ret.Add(ProcessPharmacophoreElement(e, groups));
            }
            return ret;
        }

        /// <summary>
        /// find all &lt;group&gt; elements that are directly under the supplied element so
        /// this wont recurse through sub elements that may contain group elements
        /// </summary>
        private static Dictionary<string, string> GetGroupDefinitions(XElement e)
        {
            Dictionary<string, string> groups = new Dictionary<string, string>();
            var children = e.Elements();
            foreach (var child in children)
            { 
                if (string.Equals(child.Name.LocalName, "group", StringComparison.Ordinal))
                {
                    string id = child.Attribute("id").Value.Trim();
                    string smarts = child.Value.Trim();
                    groups[id] = smarts;
                }
            }
            return groups;
        }

        /* process a single pcore definition */
        private static PharmacophoreQuery ProcessPharmacophoreElement(XElement e, Dictionary<string, string> global)
        {
            PharmacophoreQuery ret = new PharmacophoreQuery();
            ret.SetProperty("description", e.Attribute("description")?.Value);
            ret.Title = e.Attribute("name")?.Value;

            // first get any local group definitions
            Dictionary<string, string> local = GetGroupDefinitions(e);

            // now lets look at the constraints
            var children = e.Elements();
            foreach (var child in children)
            {
                if (string.Equals(child.Name.LocalName, "distanceConstraint", StringComparison.Ordinal))
                {
                    ProcessDistanceConstraint(child, local, global, ret);
                }
                else if (string.Equals(child.Name.LocalName, "angleConstraint", StringComparison.Ordinal))
                {
                    ProcessAngleConstraint(child, local, global, ret);
                }
            }
            return ret;
        }

        private static void ProcessDistanceConstraint(XElement child, Dictionary<string, string> local,
                Dictionary<string, string> global, PharmacophoreQuery ret)
        {
            double lower;
            var tmp = child.Attribute("lower");
            if (tmp == null)
                throw new CDKException("Must have a 'lower' attribute");
            else
                lower = double.Parse(tmp.Value, NumberFormatInfo.InvariantInfo);

            // we may not have an upper bound specified
            double upper;
            tmp = child.Attribute("upper");
            if (tmp != null)
                upper = double.Parse(tmp.Value, NumberFormatInfo.InvariantInfo);
            else
                upper = lower;

            // now get the two groups for this distance
            var grouprefs = child.Elements().ToReadOnlyList();
            if (grouprefs.Count != 2) throw new CDKException("A distance constraint can only refer to 2 groups.");
            string id1 = grouprefs[0].Attribute("id")?.Value;
            string id2 = grouprefs[1].Attribute("id")?.Value;

            // see if it's a local def, else get it from the global list
            string smarts1, smarts2;
            if (local.ContainsKey(id1))
                smarts1 = local[id1];
            else if (global.ContainsKey(id1))
                smarts1 = global[id1];
            else
                throw new CDKException("Referring to a non-existant group definition");

            if (local.ContainsKey(id2))
                smarts2 = local[id2];
            else if (global.ContainsKey(id2))
                smarts2 = global[id2];
            else
                throw new CDKException("Referring to a non-existant group definition");

            // now see if we already have a correpsondiong pcore atom
            // else create a new atom
            if (!ContainsPatom(ret, id1))
            {
                PharmacophoreQueryAtom pqa = new PharmacophoreQueryAtom(id1, smarts1);
                ret.Atoms.Add(pqa);
            }
            if (!ContainsPatom(ret, id2))
            {
                PharmacophoreQueryAtom pqa = new PharmacophoreQueryAtom(id2, smarts2);
                ret.Atoms.Add(pqa);
            }

            // now add the constraint as a bond
            IAtom a1 = null, a2 = null;
            foreach (var queryAtom in ret.Atoms)
            {
                if (queryAtom.Symbol.Equals(id1, StringComparison.Ordinal)) a1 = queryAtom;
                if (queryAtom.Symbol.Equals(id2, StringComparison.Ordinal)) a2 = queryAtom;
            }
            ret.Bonds.Add(new PharmacophoreQueryBond((PharmacophoreQueryAtom)a1, (PharmacophoreQueryAtom)a2, lower, upper));
        }

        private static void ProcessAngleConstraint(XElement child, Dictionary<string, string> local,
                Dictionary<string, string> global, PharmacophoreQuery ret)
        {
            double lower;
            XAttribute tmp;

            tmp = child.Attribute("lower");
            if (tmp == null)
                throw new CDKException("Must have a 'lower' attribute");
            else
                lower = double.Parse(tmp.Value, NumberFormatInfo.InvariantInfo);

            // we may not have an upper bound specified
            double upper;
            tmp = child.Attribute("upper");
            if (tmp != null)
                upper = double.Parse(tmp.Value, NumberFormatInfo.InvariantInfo);
            else
                upper = lower;

            // now get the three groups for this distance
            var grouprefs = child.Elements().ToReadOnlyList();
            if (grouprefs.Count != 3) throw new CDKException("An angle constraint can only refer to 3 groups.");
            string id1 = grouprefs[0].Attribute("id")?.Value;
            string id2 = grouprefs[1].Attribute("id")?.Value;
            string id3 = grouprefs[2].Attribute("id")?.Value;

            // see if it's a local def, else get it from the global list
            string smarts1, smarts2, smarts3;
            if (local.ContainsKey(id1))
                smarts1 = local[id1];
            else if (global.ContainsKey(id1))
                smarts1 = global[id1];
            else
                throw new CDKException("Referring to a non-existant group definition");

            if (local.ContainsKey(id2))
                smarts2 = local[id2];
            else if (global.ContainsKey(id2))
                smarts2 = global[id2];
            else
                throw new CDKException("Referring to a non-existant group definition");

            if (local.ContainsKey(id3))
                smarts3 = local[id3];
            else if (global.ContainsKey(id3))
                smarts3 = global[id3];
            else
                throw new CDKException("Referring to a non-existant group definition");

            // now see if we already have a correpsondiong pcore atom
            // else create a new atom
            if (!ContainsPatom(ret, id1))
            {
                PharmacophoreQueryAtom pqa = new PharmacophoreQueryAtom(id1, smarts1);
                ret.Atoms.Add(pqa);
            }
            if (!ContainsPatom(ret, id2))
            {
                PharmacophoreQueryAtom pqa = new PharmacophoreQueryAtom(id2, smarts2);
                ret.Atoms.Add(pqa);
            }
            if (!ContainsPatom(ret, id3))
            {
                PharmacophoreQueryAtom pqa = new PharmacophoreQueryAtom(id3, smarts3);
                ret.Atoms.Add(pqa);
            }

            // now add the constraint as a bond
            IAtom a1 = null, a2 = null;
            IAtom a3 = null;
            foreach (var queryAtom in ret.Atoms)
            {
                if (queryAtom.Symbol.Equals(id1, StringComparison.Ordinal)) a1 = queryAtom;
                if (queryAtom.Symbol.Equals(id2, StringComparison.Ordinal)) a2 = queryAtom;
                if (queryAtom.Symbol.Equals(id3, StringComparison.Ordinal)) a3 = queryAtom;
            }
            ret.Bonds.Add(new PharmacophoreQueryAngleBond((PharmacophoreQueryAtom)a1, (PharmacophoreQueryAtom)a2,
                    (PharmacophoreQueryAtom)a3, lower, upper));
        }

        private static bool ContainsPatom(PharmacophoreQuery q, string id)
        {
            foreach (var queryAtom in q.Atoms)
            {
                if (queryAtom.Symbol.Equals(id, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
