/* Copyright (C) 1997-2007  Bradley A. Smith <bradley@baysmith.com>
 *
 * Contact: cdk-developers@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NCDK.Config
{
    /// <summary>
    /// AtomType list configurator that uses the AtomTypes originally
    /// defined in Jmol v5.This class was added to be able to port
    /// Jmol to CDK.The AtomType's themselves seems have a computational
    /// background, but this is not clear.
    /// </summary>
    // @cdk.module core
    // @author Bradley A. Smith &lt;bradley@baysmith.com&gt;
    // @cdk.keyword    atom, type
    public class TXTBasedAtomTypeConfigurator
        : IAtomTypeConfigurator
    {
        private readonly IChemObjectBuilder builder = Silent.ChemObjectBuilder.Instance;
        private const string configFile = "NCDK.Config.Data.jmol_atomtypes.txt";
        private Stream stream;

        /// <inheritdoc/>
        public Stream GetStream()
        {
            return stream;
        }

        /// <inheritdoc/>
        public void SetStream(Stream value)
        {
            stream = value;
        }

        public TXTBasedAtomTypeConfigurator() { }

        /// <summary>
        /// Reads a text based configuration file.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{IAtomType}"/> with read <see cref="IAtomType"/>'s.</returns>
        /// <exception cref="IOException">when a problem occurred with reading from the <see cref="GetStream()"/></exception>
        public IEnumerable<IAtomType> ReadAtomTypes()
        {
            if (GetStream() == null)
            {
                SetStream(ResourceLoader.GetAsStream(configFile));
            }

            using (var reader = new StreamReader(GetStream()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWithChar('#'))
                        continue;
                    var tokens = line.Split("\t ,;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length != 9)
                        throw new IOException("AtomTypeTable.ReadAtypes: " + "Wrong Number of fields");

                    IAtomType atomType;
                    try
                    {
                        var name = tokens[0];
                        var rootType = tokens[1];
                        var san = tokens[2];
                        var sam = tokens[3];
                        // skip the vdw radius value
                        var scovalent = tokens[5];
                        var sColorR = tokens[6];
                        var sColorG = tokens[7];
                        var sColorB = tokens[8];

                        var mass = double.Parse(sam, NumberFormatInfo.InvariantInfo);
                        var covalent = double.Parse(scovalent, NumberFormatInfo.InvariantInfo);
                        var atomicNumber = int.Parse(san, NumberFormatInfo.InvariantInfo);
                        var colorR = int.Parse(sColorR, NumberFormatInfo.InvariantInfo);
                        var colorG = int.Parse(sColorG, NumberFormatInfo.InvariantInfo);
                        var colorB = int.Parse(sColorB, NumberFormatInfo.InvariantInfo);

                        atomType = builder.NewAtomType(name, rootType);
                        atomType.AtomicNumber = atomicNumber;
                        atomType.ExactMass = mass;
                        atomType.CovalentRadius = covalent;
                        atomType.SetProperty(CDKPropertyName.Color, CDKPropertyName.RGB2Int(colorR, colorG, colorB));
                    }
                    catch (FormatException)
                    {
                        throw new IOException("AtomTypeTable.ReadAtypes: " + "Malformed Number");
                    }
                    yield return atomType;
                }
            }
            yield break;
        }
    }
}