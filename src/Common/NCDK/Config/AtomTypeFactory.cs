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

using NCDK.Common.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NCDK.Config
{
    /// <summary>
    /// General class for defining <see cref="IAtomType"/>s. This class itself does not define the
    /// items types; for this classes implementing the <see cref="IAtomTypeConfigurator"/>
    /// interface are used.
    /// </summary>
    /// <remarks>
    /// To see which AtomTypeConfigurator's CDK provides, one should check the
    /// AtomTypeConfigurator API.
    /// </remarks>
    /// <example>
    /// <para>
    /// The AtomTypeFactory is a singleton class, which means that there exists
    /// only one instance of the class. Well, almost. For each atom type table,
    /// there is one AtomTypeFactory instance. An instance of this class is
    /// obtained with:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Config.AtomTypeFactory_Example.cs+1"]/*' />
    /// </para>
    /// <para>
    /// For each atom type list a separate AtomTypeFactory is instantiated.
    /// </para>
    /// <para>
    /// To get all the atom types of an element from a specific list, this
    /// code can be used:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Config.AtomTypeFactory_Example.cs+2"]/*' />
    /// </para>
    /// </example>
    /// <seealso cref="IAtomTypeConfigurator"/>
    // @cdk.module core
    // @author     steinbeck
    // @cdk.created    2001-08-29
    // @cdk.keyword    atom, type
    public class AtomTypeFactory
    {
        private const string TXT_EXTENSION = ".txt";
        private const string XML_EXTENSION = ".xml";
        private const string OWL_EXTENSION = ".owl";

        private static ConcurrentDictionary<string, AtomTypeFactory> tables = new ConcurrentDictionary<string, AtomTypeFactory>();
        private Dictionary<string, IAtomType> atomTypes = new Dictionary<string, IAtomType>();

        private AtomTypeFactory(string configFile)
        {
            ReadConfiguration(configFile);
        }

        private AtomTypeFactory(Stream ins, string format)
        {
            ReadConfiguration(ins, format);
        }

        /// <summary>
        /// Method to create a default AtomTypeFactory, using the given InputStream.
        /// An AtomType of this kind is not cached.
        /// </summary>
        /// <param name="ins">InputStream containing the data</param>
        /// <param name="format">String representing the possible formats ('xml' and 'txt')</param>
        /// <returns>The AtomTypeFactory for the given data file</returns>
        public static AtomTypeFactory GetInstance(Stream ins, string format)
        {
            return new AtomTypeFactory(ins, format);
        }

        /// <summary>
        /// Method to create a default AtomTypeFactory, using the structgen atom type list.
        /// </summary>
        /// <returns>the <see cref="AtomTypeFactory"/> for the given data file</returns>
        public static AtomTypeFactory GetInstance()
        {
            return GetInstance("NCDK.Config.Data.structgen_atomtypes.xml");
        }

        /// <summary>
        /// Method to create a specialized AtomTypeFactory. 
        /// </summary>
        /// <remarks>
        /// Available lists in CDK are:
        /// <list type="bullet">
        /// <item>NCDK.Config.Data.jmol_atomtypes.txt</item>
        /// <item>NCDK.Config.Data.mol2_atomtypes.xml</item>
        /// <item>NCDK.Config.Data.structgen_atomtypes.xml</item>
        /// <item>NCDK.Config.Data.mm2_atomtypes.xml</item>
        /// <item>NCDK.Config.Data.mmff94_atomtypes.xml</item>
        /// <item>NCDK.Config.Data.cdk-atom-types.owl</item>
        /// <item>NCDK.Config.Data.sybyl-atom-types.owl   </item>     
        /// <item></item>
        /// </list>
        /// </remarks>
        /// <param name="configFile">the name of the data file</param>
        /// <returns>the <see cref="AtomTypeFactory"/> for the given data file</returns>
        public static AtomTypeFactory GetInstance(string configFile)
        {
            return tables.GetOrAdd(configFile, n => new AtomTypeFactory(n));
        }

        /// <summary>
        /// Read the configuration from a text file.
        /// </summary>
        /// <param name="fileName">name of the configuration file</param>
        private void ReadConfiguration(string fileName)
        {
            Trace.TraceInformation($"Reading config file from {fileName}");
            var ins = ResourceLoader.GetAsStream(fileName);

            string format = Path.GetExtension(fileName);
            switch (format)
            {
                case TXT_EXTENSION:
                case XML_EXTENSION:
                case OWL_EXTENSION:
                    break;
                default:
                    format = XML_EXTENSION;
                    break;
            }

            ReadConfiguration(ins, format);
        }

        private static IAtomTypeConfigurator ConstructConfigurator(string format)
        {
            if (!format.StartsWithChar('.'))
                format = "." + format;
            switch (format)
            {
                case TXT_EXTENSION:
                    return new TXTBasedAtomTypeConfigurator();
                case XML_EXTENSION:
                    return new CDKBasedAtomTypeConfigurator();
                case OWL_EXTENSION:
                    return new OWLBasedAtomTypeConfigurator();
                default:
                    return null;
            }
        }

        private void ReadConfiguration(Stream ins, string format)
        {
            var atc = ConstructConfigurator(format);
            if (atc != null)
            {
                atc.SetStream(ins);
                try
                {
                    foreach (var type in atc.ReadAtomTypes())
                        atomTypes[type.AtomTypeName] = new ImmutableAtomType(type);
                }
                catch (Exception exc)
                {
                    Trace.TraceError($"Could not read AtomType's from file due to: {exc.Message}");
                    Debug.WriteLine(exc);
                }
            }
            else
            {
                Debug.WriteLine("AtomTypeConfigurator was null!");
                atomTypes = new Dictionary<string, IAtomType>();
            }
        }

        /// <summary>
        /// the number of atom types in this list.
        /// </summary>
        public int Count
        {
            get { return atomTypes.Count; }
        }

        /// <summary>
        /// Get an <see cref="IAtomType"/> with the given ID.
        /// </summary>
        /// <param name="identifier">an ID for a particular atom type (like C$)</param>
        /// <returns>The <see cref="IAtomType"/> for this id</returns>
        /// <exception cref="NoSuchAtomTypeException">Thrown if the atom type does not exist.</exception>
        public IAtomType GetAtomType(string identifier)
        {
            if (identifier != null && atomTypes.TryGetValue(identifier, out IAtomType type))
                return type;
            throw new NoSuchAtomTypeException($"The AtomType {identifier} could not be found");
        }

        /// <summary>
        /// Get an array of all atomTypes known to the AtomTypeFactory for the given
        /// element <paramref name="symbol"/> and <see cref="IAtomType"/> class.
        /// </summary>
        /// <param name="symbol">An element symbol to search for</param>
        /// <returns>An <see cref="IEnumerable{IAtomType}"/> that matches the given element symbol and <see cref="IAtomType"/> class</returns>
        public IEnumerable<IAtomType> GetAtomTypes(string symbol)
        {
            return atomTypes.Values.Where(n => string.Equals(n.Symbol, symbol, StringComparison.Ordinal));
        }

        /// <summary>
        /// Gets the all <see cref="IAtomType"/>s attribute of the <see cref="AtomTypeFactory"/> object.
        /// </summary>
        /// <returns>The all <see cref="IAtomType"/> value</returns>
        public IEnumerable<IAtomType> GetAllAtomTypes()
        {
            return atomTypes.Values;
        }

        /// <summary>
        /// Configures an atom. Finds the correct element type by looking at the Atom's
        /// atom type name, and if that fails, picks the first atom type matching
        /// the Atom's element symbol..
        /// </summary>
        /// <param name="atom">The atom to be configured</param>
        /// <returns>The configured atom</returns>
        /// <exception cref="CDKException">when it could not recognize and configure the <see cref="IAtom"/></exception>
        public IAtom Configure(IAtom atom)
        {
            if (atom is IPseudoAtom)
            {
                // do not try to configure PseudoAtom's
                return atom;
            }
            try
            {
                IAtomType atomType;
                if (string.IsNullOrEmpty(atom.AtomTypeName))
                    atomType = GetAtomTypes(atom.Symbol).First();
                else
                    atomType = GetAtomType(atom.AtomTypeName);
                atom.Symbol = atomType.Symbol;
                atom.MaxBondOrder = atomType.MaxBondOrder;
                atom.BondOrderSum = atomType.BondOrderSum;
                atom.CovalentRadius = atomType.CovalentRadius;
                atom.Hybridization = atomType.Hybridization;
                atom.SetProperty(CDKPropertyName.Color, atomType.GetProperty<object>(CDKPropertyName.Color));
                atom.AtomicNumber = atomType.AtomicNumber;
                atom.ExactMass = atomType.ExactMass;
            }
            catch (CDKException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new CDKException(exception.Message, exception);
            }
            return atom;
        }
    }
}
