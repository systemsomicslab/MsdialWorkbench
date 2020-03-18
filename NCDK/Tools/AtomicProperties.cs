/* Copyright (C) 2006-2007  Todd Martin (Environmental Protection Agency)
 *
 * Contact: cdk-devel@lists.sourceforge.net
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

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NCDK.Tools
{
    /// <summary>
    /// Provides atomic property values for descriptor calculations.
    /// This class currently provides values for mass, van der Waals volume, electronegativity and polarizability.
    /// </summary>
    // @author     Todd Martin
    // @cdk.module qsar
    public class AtomicProperties
    {
        public static AtomicProperties Instance { get; } = new AtomicProperties();

        private readonly Dictionary<string, double> htMass = new Dictionary<string, double>();
        private readonly Dictionary<string, double> htVdWVolume = new Dictionary<string, double>();
        private readonly Dictionary<string, double> htElectronegativity = new Dictionary<string, double>();
        private readonly Dictionary<string, double> htPolarizability = new Dictionary<string, double>();

        private AtomicProperties()
        {
            var configFile = "NCDK.Config.Data.whim_weights.txt";

            using (var bufferedReader = new StreamReader(ResourceLoader.GetAsStream(configFile)))
            {
                bufferedReader.ReadLine(); // header

                string line;
                while (true)
                {
                    line = bufferedReader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    var components = line.Split('\t');

                    var symbol = components[0];
                    htMass[symbol] = double.Parse(components[1], NumberFormatInfo.InvariantInfo);
                    htVdWVolume[symbol] = double.Parse(components[2], NumberFormatInfo.InvariantInfo);
                    htElectronegativity[symbol] = double.Parse(components[3], NumberFormatInfo.InvariantInfo);
                    htPolarizability[symbol] = double.Parse(components[4], NumberFormatInfo.InvariantInfo);
                }
            }
        }

        public virtual double GetVdWVolume(string symbol)
        {
            return htVdWVolume[symbol];
        }

        public virtual double GetNormalizedVdWVolume(string symbol)
        {
            return this.GetVdWVolume(symbol) / this.GetVdWVolume("C");
        }

        public virtual double GetElectronegativity(string symbol)
        {
            return htElectronegativity[symbol];
        }

        public virtual double GetNormalizedElectronegativity(string symbol)
        {
            return this.GetElectronegativity(symbol) / this.GetElectronegativity("C");
        }

        public virtual double GetPolarizability(string symbol)
        {
            return htPolarizability[symbol];
        }

        public virtual double GetNormalizedPolarizability(string symbol)
        {
            return this.GetPolarizability(symbol) / this.GetPolarizability("C");
        }

        public virtual double GetMass(string symbol)
        {
            return htMass[symbol];
        }

        public virtual double GetNormalizedMass(string symbol)
        {
            return this.GetMass(symbol) / this.GetMass("C");
        }
    }
}
