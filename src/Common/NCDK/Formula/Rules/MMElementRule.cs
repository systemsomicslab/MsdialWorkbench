/* Copyright (C) 2007  Miguel Rojasch <miguelrojasch@users.sf.net>
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

using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Formula.Rules
{
    /// <summary>
    /// This class validate if the occurrence of the IElements in the IMolecularFormula, for
    /// metabolites, are into a maximal limit according paper: The study is from 2 different mass spectral
    /// databases and according different mass of the metabolites. The analysis don't
    /// take account if the IElement is not contained in the matrix. It will be jumped. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rules is based from Tobias Kind paper with the title "Seven Golden Rules for heuristic
    /// filtering of molecular formula" <token>cdk-cite-kind2007</token>.
    /// </para>
    /// Table 1: Parameters set by this rule.
    /// <list type="table">
    /// <listheader>
    ///   <term>Name</term>
    ///   <term>Default</term>
    ///   <term>Description</term>
    /// </listheader>
    /// <item>
    ///   <term>database</term>
    ///   <term><see cref="Database.Wiley"/></term>
    ///   <term>Mass spectral databases extraction</term>
    /// </item>
    /// <item>
    ///   <term>massRange</term>
    ///   <term><see cref="RangeMass.Minus500"/></term>
    ///   <term>Mass to take account</term>
    /// </item>
    /// </list>
    /// </remarks>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    public class MMElementRule : IRule
    {
        /// <summary> Database used. As default Willey.</summary>
        private Database databaseUsed = Database.Wiley;

        /// <summary> Mass range used. As default lower than 500.</summary>
        private RangeMass rangeMassUsed = RangeMass.Minus500;

        private Dictionary<string, int> hashMap;

        /// <summary> A enumeration of the possible mass range according the rules. </summary>
        internal sealed class RangeMass
        {
            /// <summary>IMolecularFormula from a metabolite with a mass lower than 500 Da.</summary>
            public static readonly RangeMass Minus500 = new RangeMass();
            /// <summary>IMolecularFormula from a metabolite with a mass lower than 1000 Da.</summary>
            public static readonly RangeMass Minus1000 = new RangeMass();
            /// <summary>IMolecularFormula from a metabolite with a mass lower than 2000 Da.</summary>
            public static readonly RangeMass Minus2000 = new RangeMass();
            /// <summary>IMolecularFormula from a metabolite with a mass lower than 3000 Da.</summary>
            public static readonly RangeMass Minus3000 = new RangeMass();
        }

        /// <summary> A enumeration of the possible databases according the rules.</summary>
        internal sealed class Database
        {
            /// <summary>Wiley mass spectral database.</summary>
            public static readonly Database Wiley = new Database();
            /// <summary>Dictionary of Natural Products Online mass spectral database.</summary>
            public static readonly Database DictionaryNaturalProductsOnline = new Database();
        }

        /// <summary>
        /// Constructor for the MMElementRule object.
        /// </summary>
        public MMElementRule()
        {
            // initiate Hashmap default
            this.hashMap = GetWisley_500();
        }

        /// <summary>
        /// The parameters attribute of the MMElementRule object.
        /// </summary>
        public IReadOnlyList<object> Parameters
        {
            get
            {
                // return the parameters as used for the rule validation
                object[] params_ = new object[2];
                params_[0] = databaseUsed;
                params_[1] = rangeMassUsed;
                return params_;
            }

            set
            {
                if (value.Count > 2)
                    throw new CDKException("MMElementRule only expects maximal two parameters");

                if (value[0] != null)
                {
                    if (!(value[0] is Database))
                        throw new CDKException("The parameter must be of type Database enum");
                    databaseUsed = (Database)value[0];
                }

                if (value.Count > 1 && value[1] != null)
                {
                    if (!(value[1] is RangeMass))
                        throw new CDKException("The parameter must be of type RangeMass enum");
                    rangeMassUsed = (RangeMass)value[1];
                }

                if ((databaseUsed == Database.DictionaryNaturalProductsOnline) && (rangeMassUsed == RangeMass.Minus500))
                    this.hashMap = GetDNP_500();
                else if ((databaseUsed == Database.DictionaryNaturalProductsOnline) && (rangeMassUsed == RangeMass.Minus1000))
                    this.hashMap = GetDNP_1000();
                else if ((databaseUsed == Database.DictionaryNaturalProductsOnline) && (rangeMassUsed == RangeMass.Minus2000))
                    this.hashMap = GetDNP_2000();
                else if ((databaseUsed == Database.DictionaryNaturalProductsOnline) && (rangeMassUsed == RangeMass.Minus3000))
                    this.hashMap = GetDNP_3000();
                else if ((databaseUsed == Database.Wiley) && (rangeMassUsed == RangeMass.Minus500))
                    this.hashMap = GetWisley_500();
                else if ((databaseUsed == Database.Wiley) && (rangeMassUsed == RangeMass.Minus1000))
                    this.hashMap = GetWisley_1000();
                else if ((databaseUsed == Database.Wiley) && (rangeMassUsed == RangeMass.Minus2000))
                    this.hashMap = GetWisley_2000();
            }
        }
        
        /// <summary>
        /// Validate the occurrence of this IMolecularFormula.
        /// </summary>
        /// <param name="formula">Parameter is the IMolecularFormula</param>
        /// <returns>An ArrayList containing 9 elements in the order described above</returns>
        public double Validate(IMolecularFormula formula)
        {
            Trace.TraceInformation("Start validation of ", formula);
            double isValid = 1.0;
            foreach (var element in MolecularFormulaManipulator.Elements(formula))
            {
                int occur = MolecularFormulaManipulator.GetElementCount(formula, element);
                if (occur > hashMap[element.Symbol])
                {
                    isValid = 0.0;
                    break;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Get the map linking the symbol of the element and number maximum of occurrence.
        /// For the analysis with the DNP database and mass lower than 500 Da.
        /// </summary>
        /// <returns>The HashMap of the symbol linked with the maximum occurrence</returns>
        private Dictionary<string, int> GetDNP_500()
        {
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                ["C"] = 29,
                ["H"] = 72,
                ["N"] = 10,
                ["O"] = 18,
                ["P"] = 4,
                ["S"] = 7,
                ["F"] = 15,
                ["Cl"] = 8,
                ["Br"] = 5
            };

            return map;
        }

        /// <summary>
        /// Get the map linking the symbol of the element and number maximum of occurrence.
        /// For the analysis with the DNP database and mass lower than 1000 Da.
        /// </summary>
        /// <returns>The HashMap of the symbol linked with the maximum occurrence</returns>
        private Dictionary<string, int> GetDNP_1000()
        {
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                ["C"] = 66,
                ["H"] = 126,
                ["N"] = 25,
                ["O"] = 27,
                ["P"] = 6,
                ["S"] = 8,
                ["F"] = 16,
                ["Cl"] = 11,
                ["Br"] = 8
            };

            return map;
        }

        /// <summary>
        /// Get the map linking the symbol of the element and number maximum of occurrence.
        /// For the analysis with the DNP database and mass lower than 2000 Da.
        /// </summary>
        /// <returns>The HashMap of the symbol linked with the maximum occurrence</returns>
        private Dictionary<string, int> GetDNP_2000()
        {
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                ["C"] = 115,
                ["H"] = 236,
                ["N"] = 32,
                ["O"] = 63,
                ["P"] = 6,
                ["S"] = 8,
                ["F"] = 16,
                ["Cl"] = 11,
                ["Br"] = 8
            };

            return map;
        }

        /// <summary>
        /// Get the map linking the symbol of the element and number maximum of occurrence.
        /// For the analysis with the DNP database and mass lower than 3000 Da.
        /// </summary>
        /// <returns>The HashMap of the symbol linked with the maximum occurrence</returns>
        private Dictionary<string, int> GetDNP_3000()
        {
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                ["C"] = 162,
                ["H"] = 208,
                ["N"] = 48,
                ["O"] = 78,
                ["P"] = 6,
                ["S"] = 9,
                ["F"] = 16,
                ["Cl"] = 11,
                ["Br"] = 8
            };

            return map;
        }

        /// <summary>
        /// Get the map linking the symbol of the element and number maximum of occurrence.
        /// For the analysis with the Wisley database and mass lower than 500 Da.
        /// </summary>
        /// <returns>The HashMap of the symbol linked with the maximum occurrence</returns>
        private Dictionary<string, int> GetWisley_500()
        {
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                ["C"] = 39,
                ["H"] = 72,
                ["N"] = 20,
                ["O"] = 20,
                ["P"] = 9,
                ["S"] = 10,
                ["F"] = 16,
                ["Cl"] = 10,
                ["Br"] = 4,
                ["Si"] = 8
            };

            return map;
        }

        /// <summary>
        /// Get the map linking the symbol of the element and number maximum of occurrence.
        /// For the analysis with the Wisley database and mass lower than 1000 Da.
        /// </summary>
        /// <returns>The HashMap of the symbol linked with the maximum occurrence</returns>
        private Dictionary<string, int> GetWisley_1000()
        {
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                ["C"] = 78,
                ["H"] = 126,
                ["N"] = 20,
                ["O"] = 27,
                ["P"] = 9,
                ["S"] = 14,
                ["F"] = 34,
                ["Cl"] = 12,
                ["Br"] = 8,
                ["Si"] = 14
            };

            return map;
        }

        /// <summary>
        /// Get the map linking the symbol of the element and number maximum of occurrence.
        /// For the analysis with the Wisley database and mass lower than 2000 Da.
        /// </summary>
        /// <returns>The HashMap of the symbol linked with the maximum occurrence</returns>
        private Dictionary<string, int> GetWisley_2000()
        {
            Dictionary<string, int> map = new Dictionary<string, int>
            {
                ["C"] = 156,
                ["H"] = 180,
                ["N"] = 20,
                ["O"] = 40,
                ["P"] = 9,
                ["S"] = 14,
                ["F"] = 48,
                ["Cl"] = 12,
                ["Br"] = 10,
                ["Si"] = 15
            };

            return map;
        }
    }
}

