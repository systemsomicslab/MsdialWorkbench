/* Copyright (C) 2008  Rajarshi Guha <rajarshi@users.sf.net>
 *               2011  Jonathan Alvarsson <jonalv@users.sf.net>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 * Contact: cdk-devel@lists.sf.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Config;
using System.Collections.Generic;

namespace NCDK.Tools
{
    /// <summary>
    /// Represents elements of the Periodic Table. This utility class was
    /// previously useful when one wants generic properties of elements such as
    /// atomic number, VdW radius etc. The new approach to this is to use the
    /// <see cref="NaturalElement"/> enumeration.
    /// </summary>
    // @author Rajarshi Guha
    // @cdk.created 2008-06-12
    // @cdk.keyword element
    // @cdk.keyword periodic table
    // @cdk.keyword radius, vanderwaals
    // @cdk.keyword electronegativity
    // @cdk.module core
    public sealed class PeriodicTable
    {
        /// <summary>
        /// Get the Van der Waals radius for the element in question.
        /// </summary>
        /// <param name="symbol">The symbol of the element</param>
        /// <returns>the Van der waals radius</returns>
        public static double? GetVdwRadius(string symbol)
        {
            return GetVdwRadius(ChemicalElement.OfSymbol(symbol).AtomicNumber);
        }

        public static double? GetVdwRadius(int atomicNumber)
        {
            return NaturalElement.VdwRadiuses[atomicNumber];
        }

        /// <summary>
        /// Get the covalent radius for an element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the covalent radius</returns>
        public static double? GetCovalentRadius(string symbol)
        {
            return NaturalElement.CovalentRadiuses[ChemicalElement.OfSymbol(symbol).AtomicNumber];
        }

        /// <summary>
        /// Get the CAS ID for an element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the CAS ID</returns>
        public static string GetCASId(string symbol)
        {
            return MapToCasId[ChemicalElement.OfSymbol(symbol).AtomicNumber]; 
        }

        /// <summary>
        /// Get the chemical series for an element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the chemical series of the element</returns>
        public static string GetChemicalSeries(string symbol)
        {
            if (!MapToSeries.TryGetValue(ChemicalElement.OfSymbol(symbol).AtomicNumber, out string series))
                return "";
            return series;
        }

        /// <summary>
        /// Get the group of the element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the group</returns>
        public static int GetGroup(string symbol)
        {
            return GetGroup(ChemicalElement.OfSymbol(symbol).AtomicNumber);
        }

        public static int GetGroup(int number)
        {
            return NaturalElement.Groups[number];
        }

        /// <summary>
        /// Get the name of the element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the name of the element</returns>
        public static string GetName(string symbol)
        {
            return GetName(ChemicalElement.OfSymbol(symbol).AtomicNumber);
        }

        /// <summary>
        /// Get the name of the element.
        /// </summary>
        /// <param name="atomicNumber">Atomic number</param>
        /// <returns>The name of the element</returns>
        public static string GetName(int atomicNumber)
        {
            return ChemicalElement.Of(atomicNumber).Name;
        }

        /// <summary>
        /// Get the period of the element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the period</returns>
        public static int GetPeriod(string symbol)
        {
            return NaturalElement.Periods[ChemicalElement.OfSymbol(symbol).AtomicNumber];
        }

        /// <summary>
        /// Get the period of the element.
        /// </summary>
        /// <param name="number">Atomic number</param>
        /// <returns>The period</returns>
        public static int GetPeriod(int number)
        {
            return NaturalElement.Periods[number];
        }

        /// <summary>
        /// Get the phase of the element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the phase of the element</returns>
        public static string GetPhase(string symbol)
        {
            if (!MapToPhase.TryGetValue(ChemicalElement.OfSymbol(symbol).AtomicNumber, out string phase))
                return "";
            return phase;
        }

        /// <summary>
        /// Get the atomic number of the element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the atomic number</returns>
        public static int GetAtomicNumber(string symbol)
        {
            return ChemicalElement.OfSymbol(symbol).AtomicNumber;
        }

        /// <summary>
        /// Get the Pauling electronegativity of an element.
        /// </summary>
        /// <param name="symbol">the symbol of the element</param>
        /// <returns>the Pauling electronegativity</returns>
        public static double? GetPaulingElectronegativity(string symbol)
        {
            return NaturalElement.Electronegativities[ChemicalElement.OfSymbol(symbol).AtomicNumber];
        }

        /// <summary>
        /// Get the symbol for the specified atomic number.
        /// </summary>
        /// <param name="atomicNumber">the atomic number of the element</param>
        /// <returns>the corresponding symbol</returns>
        public static string GetSymbol(int atomicNumber)
        {
            return ChemicalElement.Of(atomicNumber).Symbol;
        }

        private static Dictionary<int, string> MapToCasId { get; } = new Dictionary<int, string>
            {
                { AtomicNumbers.Hydrogen, "1333-74-0" },
                { AtomicNumbers.Helium, "7440-59-7" },
                { AtomicNumbers.Lithium, "7439-93-2" },
                { AtomicNumbers.Beryllium, "7440-41-7" },
                { AtomicNumbers.Boron, "7440-42-8" },
                { AtomicNumbers.Carbon, "7440-44-0" },
                { AtomicNumbers.Nitrogen, "7727-37-9" },
                { AtomicNumbers.Oxygen, "7782-44-7" },
                { AtomicNumbers.Fluorine, "7782-41-4" },
                { AtomicNumbers.Neon, "7440-01-9" },
                { AtomicNumbers.Sodium, "7440-23-5" },
                { AtomicNumbers.Magnesium, "7439-95-4" },
                { AtomicNumbers.Aluminium, "7429-90-5" },
                { AtomicNumbers.Silicon, "7440-21-3" },
                { AtomicNumbers.Phosphorus, "7723-14-0" },
                { AtomicNumbers.Sulfur, "7704-34-9" },
                { AtomicNumbers.Chlorine, "7782-50-5" },
                { AtomicNumbers.Argon, "7440-37-1" },
                { AtomicNumbers.Potassium, "7440-09-7" },
                { AtomicNumbers.Calcium, "7440-70-2" },
                { AtomicNumbers.Scandium, "7440-20-2" },
                { AtomicNumbers.Titanium, "7440-32-6" },
                { AtomicNumbers.Vanadium, "7440-62-2" },
                { AtomicNumbers.Chromium, "7440-47-3" },
                { AtomicNumbers.Manganese, "7439-96-5" },
                { AtomicNumbers.Iron, "7439-89-6" },
                { AtomicNumbers.Cobalt, "7440-48-4" },
                { AtomicNumbers.Nickel, "7440-02-0" },
                { AtomicNumbers.Copper, "7440-50-8" },
                { AtomicNumbers.Zinc, "7440-66-6" },
                { AtomicNumbers.Gallium, "7440-55-3" },
                { AtomicNumbers.Germanium, "7440-56-4" },
                { AtomicNumbers.Arsenic, "7440-38-2" },
                { AtomicNumbers.Selenium, "7782-49-2" },
                { AtomicNumbers.Bromine, "7726-95-6" },
                { AtomicNumbers.Krypton, "7439-90-9" },
                { AtomicNumbers.Rubidium, "7440-17-7" },
                { AtomicNumbers.Strontium, "7440-24-6" },
                { AtomicNumbers.Yttrium, "7440-65-5" },
                { AtomicNumbers.Zirconium, "7440-67-7" },
                { AtomicNumbers.Niobium, "7440-03-1" },
                { AtomicNumbers.Molybdenum, "7439-98-7" },
                { AtomicNumbers.Technetium, "7440-26-8" },
                { AtomicNumbers.Ruthenium, "7440-18-8" },
                { AtomicNumbers.Rhodium, "7440-16-6" },
                { AtomicNumbers.Palladium, "7440-05-3" },
                { AtomicNumbers.Silver, "7440-22-4" },
                { AtomicNumbers.Cadmium, "7440-43-9" },
                { AtomicNumbers.Indium, "7440-74-6" },
                { AtomicNumbers.Tin, "7440-31-5" },
                { AtomicNumbers.Antimony, "7440-36-0" },
                { AtomicNumbers.Tellurium, "13494-80-9" },
                { AtomicNumbers.Iodine, "7553-56-2" },
                { AtomicNumbers.Xenon, "7440-63-3" },
                { AtomicNumbers.Caesium, "7440-46-2" },
                { AtomicNumbers.Barium, "7440-39-3" },
                { AtomicNumbers.Lanthanum, "7439-91-0" },
                { AtomicNumbers.Cerium, "7440-45-1" },
                { AtomicNumbers.Praseodymium, "7440-10-0" },
                { AtomicNumbers.Neodymium, "7440-00-8" },
                { AtomicNumbers.Promethium, "7440-12-2" },
                { AtomicNumbers.Samarium, "7440-19-9" },
                { AtomicNumbers.Europium, "7440-53-1" },
                { AtomicNumbers.Gadolinium, "7440-54-2" },
                { AtomicNumbers.Terbium, "7440-27-9" },
                { AtomicNumbers.Dysprosium, "7429-91-6" },
                { AtomicNumbers.Holmium, "7440-60-0" },
                { AtomicNumbers.Erbium, "7440-52-0" },
                { AtomicNumbers.Thulium, "7440-30-4" },
                { AtomicNumbers.Ytterbium, "7440-64-4" },
                { AtomicNumbers.Lutetium, "7439-94-3" },
                { AtomicNumbers.Hafnium, "7440-58-6" },
                { AtomicNumbers.Tantalum, "7440-25-7" },
                { AtomicNumbers.Tungsten, "7440-33-7" },
                { AtomicNumbers.Rhenium, "7440-15-5" },
                { AtomicNumbers.Osmium, "7440-04-2" },
                { AtomicNumbers.Iridium, "7439-88-5" },
                { AtomicNumbers.Platinum, "7440-06-4" },
                { AtomicNumbers.Gold, "7440-57-5" },
                { AtomicNumbers.Mercury, "7439-97-6" },
                { AtomicNumbers.Thallium, "7440-28-0" },
                { AtomicNumbers.Lead, "7439-92-1" },
                { AtomicNumbers.Bismuth, "7440-69-9" },
                { AtomicNumbers.Polonium, "7440-08-6" },
                { AtomicNumbers.Astatine, "7440-08-6" },
                { AtomicNumbers.Radon, "10043-92-2" },
                { AtomicNumbers.Francium, "7440-73-5" },
                { AtomicNumbers.Radium, "7440-14-4" },
                { AtomicNumbers.Actinium, "7440-34-8" },
                { AtomicNumbers.Thorium, "7440-29-1" },
                { AtomicNumbers.Protactinium, "7440-13-3" },
                { AtomicNumbers.Uranium, "7440-61-1" },
                { AtomicNumbers.Neptunium, "7439-99-8" },
                { AtomicNumbers.Plutonium, "7440-07-5" },
                { AtomicNumbers.Americium, "7440-35-9" },
                { AtomicNumbers.Curium, "7440-51-9" },
                { AtomicNumbers.Berkelium, "7440-40-6" },
                { AtomicNumbers.Californium, "7440-71-3" },
                { AtomicNumbers.Einsteinium, "7429-92-7" },
                { AtomicNumbers.Fermium, "7440-72-4" },
                { AtomicNumbers.Mendelevium, "7440-11-1" },
                { AtomicNumbers.Nobelium, "10028-14-5" },
                { AtomicNumbers.Lawrencium, "22537-19-5" },
                { AtomicNumbers.Rutherfordium, "53850-36-5" },
                { AtomicNumbers.Dubnium, "53850-35-4" },
                { AtomicNumbers.Seaborgium, "54038-81-2" },
                { AtomicNumbers.Bohrium, "54037-14-8" },
                { AtomicNumbers.Hassium, "54037-57-9" },
                { AtomicNumbers.Meitnerium, "54038-01-6" },
                { AtomicNumbers.Darmstadtium, "54083-77-1" },
                { AtomicNumbers.Roentgenium, "54386-24-2" },
                { AtomicNumbers.Copernicium, "54084-26-3" },
                { AtomicNumbers.Nihonium, "" },
                { AtomicNumbers.Flerovium, "54085-16-4" },
                { AtomicNumbers.Moscovium, "" },
                { AtomicNumbers.Livermorium, "54100-71-9" },
                { AtomicNumbers.Tennessine, "" },
                { AtomicNumbers.Oganesson, "" }
            };

        private static void AddToMap(Dictionary<int, string> map, string text, params int[] numbers)
        {
            foreach (var n in numbers)
                map[n] = text;
        }

        private static Dictionary<int, string> MapToSeries { get; } = MakeMapToSeries();
        
        private static Dictionary<int, string> MakeMapToSeries()
        {
            var ids = new Dictionary<int, string>();
            AddToMap(ids, "Non Metal", AtomicNumbers.Sulfur, AtomicNumbers.Selenium, AtomicNumbers.Oxygen, AtomicNumbers.Carbon, AtomicNumbers.Phosphorus, AtomicNumbers.Hydrogen, AtomicNumbers.Nitrogen);
            AddToMap(ids, "Noble Gas", AtomicNumbers.Helium, AtomicNumbers.Krypton, AtomicNumbers.Xenon, AtomicNumbers.Argon, AtomicNumbers.Radon, AtomicNumbers.Neon);
            AddToMap(ids, "Alkali Metal", AtomicNumbers.Sodium, AtomicNumbers.Rubidium, AtomicNumbers.Potassium, AtomicNumbers.Caesium, AtomicNumbers.Francium, AtomicNumbers.Lithium);
            AddToMap(ids, "Alkali Earth Metal", AtomicNumbers.Strontium, AtomicNumbers.Radium, AtomicNumbers.Calcium, AtomicNumbers.Magnesium, AtomicNumbers.Barium, AtomicNumbers.Beryllium);
            AddToMap(ids, "Metalloid", AtomicNumbers.Silicon, AtomicNumbers.Arsenic, AtomicNumbers.Tellurium, AtomicNumbers.Germanium, AtomicNumbers.Antimony, AtomicNumbers.Polonium, AtomicNumbers.Boron);
            AddToMap(ids, "Halogen", AtomicNumbers.Fluorine, AtomicNumbers.Iodine, AtomicNumbers.Chlorine, AtomicNumbers.Astatine, AtomicNumbers.Bromine);
            AddToMap(ids, "Metal", AtomicNumbers.Gallium, AtomicNumbers.Indium, AtomicNumbers.Aluminium, AtomicNumbers.Thallium, AtomicNumbers.Tin, AtomicNumbers.Lead, AtomicNumbers.Bismuth);
            AddToMap(ids, "Transition Metal", AtomicNumbers.Seaborgium, AtomicNumbers.Hafnium,
                AtomicNumbers.Roentgenium, AtomicNumbers.Iridium, AtomicNumbers.Nickel, AtomicNumbers.Meitnerium, AtomicNumbers.Yttrium, AtomicNumbers.Copper, AtomicNumbers.Rutherfordium, AtomicNumbers.Tungsten, AtomicNumbers.Copernicium,
                AtomicNumbers.Rhodium, AtomicNumbers.Cobalt, AtomicNumbers.Zinc, AtomicNumbers.Platinum, AtomicNumbers.Gold, AtomicNumbers.Cadmium, AtomicNumbers.Manganese, AtomicNumbers.Darmstadtium, AtomicNumbers.Dubnium, AtomicNumbers.Palladium, AtomicNumbers.Vanadium,
                AtomicNumbers.Titanium, AtomicNumbers.Tantalum, AtomicNumbers.Chromium, AtomicNumbers.Molybdenum, AtomicNumbers.Ruthenium, AtomicNumbers.Zirconium, AtomicNumbers.Osmium, AtomicNumbers.Bohrium, AtomicNumbers.Rhenium, AtomicNumbers.Niobium,
                AtomicNumbers.Scandium, AtomicNumbers.Technetium, AtomicNumbers.Hassium, AtomicNumbers.Mercury, AtomicNumbers.Iron, AtomicNumbers.Silver);
            AddToMap(ids, "Lanthanide", AtomicNumbers.Terbium, AtomicNumbers.Samarium, AtomicNumbers.Lutetium,
                AtomicNumbers.Neodymium, AtomicNumbers.Cerium, AtomicNumbers.Europium, AtomicNumbers.Gadolinium, AtomicNumbers.Thulium, AtomicNumbers.Lanthanum, AtomicNumbers.Erbium, AtomicNumbers.Promethium, AtomicNumbers.Holmium, AtomicNumbers.Praseodymium,
                AtomicNumbers.Dysprosium, AtomicNumbers.Ytterbium);
            AddToMap(ids, "Actinide", AtomicNumbers.Fermium, AtomicNumbers.Protactinium, AtomicNumbers.Plutonium, AtomicNumbers.Thorium, AtomicNumbers.Lawrencium, AtomicNumbers.Einsteinium,
                AtomicNumbers.Nobelium, AtomicNumbers.Actinium, AtomicNumbers.Americium, AtomicNumbers.Curium, AtomicNumbers.Berkelium, AtomicNumbers.Mendelevium, AtomicNumbers.Uranium, AtomicNumbers.Californium, AtomicNumbers.Neptunium);
            return ids;
        }

        private static Dictionary<int, string> MapToPhase { get; } = MakeMapToPhase();

        private static Dictionary<int, string> MakeMapToPhase()
        {
            var ids = new Dictionary<int, string>();
            AddToMap(ids, "Solid", AtomicNumbers.Sulfur, AtomicNumbers.Hafnium, AtomicNumbers.Terbium, AtomicNumbers.Calcium, AtomicNumbers.Gadolinium, AtomicNumbers.Nickel, AtomicNumbers.Cerium, AtomicNumbers.Germanium, AtomicNumbers.Phosphorus, AtomicNumbers.Copper, AtomicNumbers.Polonium,
                AtomicNumbers.Lead, AtomicNumbers.Gold, AtomicNumbers.Iodine, AtomicNumbers.Cadmium, AtomicNumbers.Ytterbium, AtomicNumbers.Manganese, AtomicNumbers.Lithium, AtomicNumbers.Palladium, AtomicNumbers.Vanadium, AtomicNumbers.Chromium, AtomicNumbers.Molybdenum,
                AtomicNumbers.Potassium, AtomicNumbers.Ruthenium, AtomicNumbers.Osmium, AtomicNumbers.Boron, AtomicNumbers.Bismuth, AtomicNumbers.Rhenium, AtomicNumbers.Holmium, AtomicNumbers.Niobium, AtomicNumbers.Praseodymium, AtomicNumbers.Barium,
                AtomicNumbers.Antimony, AtomicNumbers.Thallium, AtomicNumbers.Iron, AtomicNumbers.Silver, AtomicNumbers.Silicon, AtomicNumbers.Caesium, AtomicNumbers.Astatine, AtomicNumbers.Iridium, AtomicNumbers.Francium, AtomicNumbers.Lutetium, AtomicNumbers.Yttrium,
                AtomicNumbers.Rubidium, AtomicNumbers.Lanthanum, AtomicNumbers.Tungsten, AtomicNumbers.Erbium, AtomicNumbers.Selenium, AtomicNumbers.Gallium, AtomicNumbers.Carbon, AtomicNumbers.Rhodium, AtomicNumbers.Uranium, AtomicNumbers.Dysprosium, AtomicNumbers.Cobalt,
                AtomicNumbers.Zinc, AtomicNumbers.Platinum, AtomicNumbers.Protactinium, AtomicNumbers.Titanium, AtomicNumbers.Arsenic, AtomicNumbers.Tantalum, AtomicNumbers.Thorium, AtomicNumbers.Samarium, AtomicNumbers.Europium, AtomicNumbers.Neodymium,
                AtomicNumbers.Zirconium, AtomicNumbers.Radium, AtomicNumbers.Thulium, AtomicNumbers.Sodium, AtomicNumbers.Scandium, AtomicNumbers.Tellurium, AtomicNumbers.Indium, AtomicNumbers.Beryllium, AtomicNumbers.Aluminium, AtomicNumbers.Strontium, AtomicNumbers.Tin,
                AtomicNumbers.Magnesium);
            AddToMap(ids, "Liquid", AtomicNumbers.Bromine, AtomicNumbers.Mercury);
            AddToMap(ids, "Gas", AtomicNumbers.Fluorine, AtomicNumbers.Oxygen, AtomicNumbers.Xenon, AtomicNumbers.Argon, AtomicNumbers.Chlorine, AtomicNumbers.Helium, AtomicNumbers.Krypton, AtomicNumbers.Hydrogen, AtomicNumbers.Radon, AtomicNumbers.Nitrogen, AtomicNumbers.Neon);
            AddToMap(ids, "Synthetic", AtomicNumbers.Fermium, AtomicNumbers.Seaborgium, AtomicNumbers.Plutonium, AtomicNumbers.Roentgenium, AtomicNumbers.Lawrencium, AtomicNumbers.Meitnerium, AtomicNumbers.Einsteinium, AtomicNumbers.Nobelium, AtomicNumbers.Actinium,
                AtomicNumbers.Rutherfordium, AtomicNumbers.Americium, AtomicNumbers.Curium, AtomicNumbers.Bohrium, AtomicNumbers.Berkelium, AtomicNumbers.Promethium, AtomicNumbers.Copernicium, AtomicNumbers.Technetium, AtomicNumbers.Hassium,
                AtomicNumbers.Californium, AtomicNumbers.Mendelevium, AtomicNumbers.Neptunium, AtomicNumbers.Darmstadtium, AtomicNumbers.Dubnium);
            return ids;
        }

        /// <summary>
        /// Utility method to determine if an atomic number is a metal.
        /// </summary>
        /// <param name="atomicNumber">atomic number</param>
        /// <returns>the atomic number is a metal (or not)</returns>
        public static bool IsMetal(int atomicNumber)
        {
            switch (atomicNumber)
            {
                case 0:  // *
                case 1:  // H
                case 2:  // He
                case 6:  // C
                case 7:  // N
                case 8:  // O
                case 9:  // F
                case 10: // Ne
                case 15: // P
                case 16: // S
                case 17: // Cl
                case 18: // Ar
                case 34: // Se
                case 35: // Br
                case 36: // Kr
                case 53: // I
                case 54: // Xe
                case 86: // Rn
                    return false;
                case 5:   // B
                case 14:  // Si
                case 32:  // Ge
                case 33:  // As
                case 51:  // Sb
                case 52:  // Te
                case 85:  // At
                    return false;
            }
            return true;
        }
    }
}
