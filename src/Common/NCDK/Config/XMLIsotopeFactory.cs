/* Copyright (C) 2001-2007  Christoph Steinbeck <steinbeck@users.sf.net>
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

using NCDK.Config.Isotopes;
using System;
using System.Diagnostics;
using System.IO;

namespace NCDK.Config
{
    /// <summary>
    /// Used to store and return data of a particular isotope. As this class is a
    /// singleton class, one gets an instance with:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Config.XMLIsotopeFactory_Example.cs+1"]/*' />
    /// </summary>
    /// <remarks>
    /// Data about the isotopes are read from the NCDK.Config.Data.isotopes.xml resource.
    /// Part of the data in this file was collected from
    /// the website <see href="http://www.webelements.org">webelements.org</see>.
    /// </remarks>
    /// <example>
    /// The use of this class is exemplified as follows. To get information
    /// about the major isotope of hydrogen, one can use this code:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Config.XMLIsotopeFactory_Example.cs+example"]/*' />
    /// </example>
    // @author     steinbeck
    // @cdk.created    2001-08-29
    // @cdk.keyword    isotope
    // @cdk.keyword    element
    public class XMLIsotopeFactory 
        : IsotopeFactory
    {
        public static XMLIsotopeFactory Instance { get; } = new XMLIsotopeFactory();

        /// <summary>
        /// Private constructor for the IsotopeFactory object.
        /// </summary>
        /// <exception cref="IOException">A problem with reading the isotopes.xml file</exception>
        private XMLIsotopeFactory()
        {
            Trace.TraceInformation("Creating new IsotopeFactory");

            // ObjIn in = null;
            var errorMessage = $"There was a problem getting NCDK.Config.Data.isotopes.xml as a stream";
            var configFile = "NCDK.Config.Data.isotopes.xml";
            Debug.WriteLine($"Getting stream for {configFile}");
            using (var reader = new IsotopeReader(ResourceLoader.GetAsStream(configFile)))
            {
                var isotopes = reader.ReadIsotopes();
                foreach (var isotope in isotopes)
                    Add(isotope);
                Debug.WriteLine($"Found #isotopes in file: {isotopes.Count}");
            }
        }
    }
}
