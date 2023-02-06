/*
 *    Copyright 2011 Peter Murray-Rust et. al.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace NCDK.LibIO.CML
{
    /// <summary>
    /// user-modifiable class supporting formula. * The semantics of formula have
    /// been updated (2005-10) and the relationship between concise attribute and
    /// atomArray children is explicit. This class supports the parsing of a number
    /// of current inline structures but does not guarantee correctness as there are
    /// no agreed community syntax/semantics. This is a particular problem for
    /// charges which could be "2+", "++", "+2", etc. For robust inline interchange
    /// ONLY the concise representation is supported.
    /// </summary>
    /// <remarks>
    /// NOTE: The atomArray child, in array format, together with the formalCharge
    /// attribute is the primary means of holding the formula. There is now no lazy
    /// evaluation. The concise attribute can be auto-generated from the atomArray and
    /// formalCharge. If a formula is input with only concise then the atomArray is
    /// automatically generated from it.
    /// </remarks>
    public partial class CMLFormula : CMLElement
    {
        public const string SMILES = "SMILES";
        public const string SMILES1 = "cml:smiles";

        public enum FormulaType
        {
            /// <summary>
            /// the simplest representation. an input-only format. parsing is possible but fragile. The charge semantics are not defined. Not recommended for output.
            /// </summary>
            NoPunctuation,

            /// <summary>
            /// another simple representation. an input-only format. parsing is also fragile as charge semantics are not defined. Not recommended for output.
            /// </summary>
            ElementCountWhitespace,

            /// <summary>
            /// Yet another simple representation. an input-only format. Element counts of 1 should always be given. Fragile as charge field is likely to be undefined. Not recommended for output.
            /// </summary>
            ElementWhitespaceCount,

            /// <summary>
            /// the format used in concise. recommended as the main output form. Element counts of 1 should always be given. the charge shoudl always be included. See concise.xsd and formulaType.xsd for syntax.
            /// </summary>
            Concise,

            /// <summary>
            /// multipliers for moieties. an input only format. JUMBO will try to parse this correctly but no guarantee is given.
            /// </summary>
            MultipliedElementCountWhitespace,

            /// <summary>
            /// hierarchical formula. an input-only format. JUMBO will try to parse this correctly but no guarantee is given.
            /// </summary>
            NestedBrackets,

            /// <summary>
            /// an input only format. JUMBO will not yet parse this correctly. comments from IUCr
            /// </summary>
            IUPAC,

            /// <summary>
            /// Moiety, used by IUCr. an input-only format. moieties assumed to be comma separated then ELEMENT_COUNT_WHITESPACE, with optional brackets and post or pre-multipliers
            /// </summary>
            Moiety,

            /// <summary>
            /// Submoiety, used by IUCr. the part of a moiety within the brackets assumed to b ELEMENT_OPTIONALCOUNT followed by optional FORMULA
            /// </summary>
            Submoiety,

            /// <summary>
            /// Structural, used by IUCr. not currently implemented, I think. probably the same as nested brackets
            /// </summary>
            Structural,

            /// <summary>
            /// any of the above. input-only.
            /// </summary>
            Any,
        }

        public enum SortType
        {
            /// <summary>
            /// Sort alphabetically. output only. Not sure where this is.
            /// </summary>
            AlphabeticElements,

            /// <summary>
            /// C H and then alphabetically (output only).
            /// </summary>
            CHFirst,
        }

        /// <summary>
        /// type of hydrogen counting
        /// </summary>
        // @author pm286
        public enum HydrogenStrategyType
        {
            /// <summary>use hydrogen count attribute</summary>
            HydrogenCount,
            /// <summary>use explicit hydrogens</summary>
            ExplicitHydrogens,
        }

        // only edit insertion module!

        // marks whether concise has been processed before atomArray has been read
        internal bool processedConcise = false;
        private const bool allowNegativeCounts = false;

        // element:   formula
        // element:   atomArray

        public void Normalize()
        {
            // create all possible renderings of formula
            // any or all may be present
            // concise
            var conciseAtt = this.Attribute(Attribute_concise);
            // formal charge
            int formalCharge = 0;
            if (this.Attribute(Attribute_formalCharge) != null)
            {
                formalCharge = int.Parse(this.Attribute(Attribute_formalCharge).Value, NumberFormatInfo.InvariantInfo);
            }
            string conciseS = conciseAtt?.Value;
            // convention
            string convention = this.Convention;
            // inline formula (might be SMILES)
            string inline = this.Inline;
            if (inline != null)
            {
                inline = inline.Trim();
            }

            // atomArray
            CMLAtomArray atomArray = null;
            string atomArray2Concise = null;
            var atomArrays = this.Elements(XName_CML_atomArray).Cast<CMLAtomArray>().ToReadOnlyList();
            if (atomArrays.Count > 1)
            {
                throw new ApplicationException($"Only one atomArray child allowed for formula; found: {atomArrays.Count}");
            }
            else if (atomArrays.Count == 1)
            {
                atomArray = atomArrays.First();
                atomArray.Sort(SortType.CHFirst);
                atomArray2Concise = atomArray.GenerateConcise(formalCharge);
            }

            // concise from inline
            if (inline != null)
            {
                if (SMILES.Equals(convention, StringComparison.Ordinal) ||
                    SMILES1.Equals(convention, StringComparison.Ordinal))
                {
                }
            }
            if (conciseS == null)
            {
                if (atomArray2Concise != null)
                {
                    conciseS = atomArray2Concise;
                }
            }
            if (conciseS != null)
            {
                conciseS = NormalizeConciseAndFormalCharge(conciseS, formalCharge);
            }
            // if no atomArray, create
            if (atomArray == null)
            {
                // causes problems with Jmol
            }
            else
            {
                CheckAtomArrayFormat(atomArray);
            }
            if (atomArray != null)
            {
                atomArray.Sort(SortType.CHFirst);
            }
            // check consistency
            if (atomArray2Concise != null &&
                    !atomArray2Concise.Equals(conciseS, StringComparison.Ordinal))
            {
                throw new ApplicationException($"concise ({conciseS}) and atomArray ({atomArray2Concise}) differ");
            }
            if (conciseS != null)
            {
                // by this time we may have generated a non-zero formal charge, so normalize it into concise
                conciseS = NormalizeConciseAndFormalCharge(conciseS, this.FormalCharge);
                ForceConcise(conciseS);
            }
        }

        CMLAtomArray CreateAndAddAtomArrayAndFormalChargeFromConcise(string concise)
        {
            var atomArray = new CMLAtomArray();
            if (concise != null)
            {
                var elements = new List<string>();
                var counts = new List<double>();
                var tokens = Regex.Split(concise, @"\s");
                var nelement = tokens.Length / 2;
                for (int i = 0; i < nelement; i++)
                {
                    var elem = tokens[2 * i];
                    var ce = NCDK.ChemicalElement.OfSymbol(elem).AtomicNumber;
                    if (ce == AtomicNumbers.Unknown)
                    {
                        throw new ApplicationException($"Unknown chemical element: {elem}");
                    }
                    if (elements.Contains(elem))
                    {
                        throw new ApplicationException($"Duplicate element in concise: {elem}");
                    }
                    elements.Add(elem);
                    var countS = tokens[2 * i + 1];
                    try
                    {
                        counts.Add(double.Parse(countS, NumberFormatInfo.InvariantInfo));
                    }
                    catch (FormatException)
                    {
                        throw new ApplicationException($"Bad element count in concise: {countS}");
                    }
                }
                if (tokens.Length > nelement * 2)
                {
                    var chargeS = tokens[nelement * 2];
                    try
                    {
                        var formalCharge = int.Parse(chargeS, NumberFormatInfo.InvariantInfo);
                        FormalCharge = formalCharge;
                    }
                    catch (FormatException)
                    {
                        throw new ApplicationException($"Bad formal charge in concise: {chargeS}");
                    }
                }
                var countD = new double[nelement];
                for (int i = 0; i < nelement; i++)
                {
                    countD[i] = counts[i];
                }
                atomArray.ElementType = elements.ToArray();
                atomArray.Count = countD;
            }
            Add(atomArray);
            return atomArray;
        }

        /// <summary>
        /// checks that atomArray is in array format with unduplicated valid
        /// elements. must have elementType and count attributes of equal lengths.
        /// </summary>
        /// <param name="atomArray">to check (not modified)</param>
        /// <exception cref="ApplicationException">if invalid</exception>
        public static void CheckAtomArrayFormat(CMLAtomArray atomArray)
        {
            if (atomArray.HasElements)
            {
                throw new ApplicationException("No children allowed for formula/atomArray");
            }
            var elements = atomArray.ElementType;
            var counts = atomArray.Count;
            if (elements == null || counts == null)
            {
                throw new ApplicationException("formula/atomArray must have elementType and count attributes");
            }
            if (elements.Count != counts.Count)
            {
                throw new ApplicationException("formula/atomArray must have equal length elementType and count values");
            }
            var elementSet = new HashSet<string>();
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] != null && !(elements[i].Equals("null", StringComparison.Ordinal)))
                {
                    if (elementSet.Contains(elements[i]))
                    {
                        throw new ApplicationException($"formula/atomArray@elementType has duplicate element: {elements[i]}");
                    }
                    elementSet.Add(elements[i]);
                    if (counts[i] <= 0 && !allowNegativeCounts)
                    {
                        throw new ApplicationException($"formula/atomArray@count has nonPositive value: {counts[i]}  {elements[i]}");
                    }
                }
            }
        }

        /// <summary>
        /// set concise attribute. if atomArray is absent will automatically compute
        /// atomArray and formalCharge, so use with care if atomArray is present will
        /// throw CMLRuntime this logic may need to be revised
        /// </summary>
        /// <param name="value">concise value</param>
        /// <exception cref="ApplicationException">attribute wrong value/type</exception>
        public virtual string Concise
        {
            get { return Attribute(Attribute_concise).Value; }
            set
            {
                if (AtomArrayElements.Any())
                {
                    throw new ApplicationException("Cannot reset concise if atomArray is present");
                }
                ForceConcise(value);
            }
        }

        private void ForceConcise(string value)
        {
            SetAttributeValue(Attribute_concise, value);
            Normalize();
            // if building, then XOM attributes are processed before children
            // this flag will allow subsequent atomArray to override
            processedConcise = true;
        }

        // FIXME move to tools    
        private string NormalizeConciseAndFormalCharge(string conciseS, int formalCharge)
        {
            if (conciseS != null)
            {
                CMLAtomArray atomArray = CreateAndAddAtomArrayAndFormalChargeFromConcise(conciseS);
                if (atomArray != null)
                {
                    atomArray.Sort(SortType.CHFirst);
                    conciseS = atomArray.GenerateConcise(formalCharge);
                }
            }
            return conciseS;
        }

        /// <summary>
        /// An inline representation of the object.
        /// No description
        /// </summary>
        public IEnumerable<CMLAtomArray> AtomArrayElements
            => this.Elements(XName_CML_atomArray).Cast<CMLAtomArray>();

        /// <summary>
        /// Adds element and count to formula. If element is already known,
        /// increments the count.
        /// </summary>
        /// <param name="elementType">the element atomicSymbol</param>
        /// <param name="count">the element multiplier</param>
        public void Add(string elementType, double count)
        {
            CMLAtomArray atomArray = AtomArrayElements.FirstOrDefault();
            if (atomArray == null)
            {
                // if no atomArray , create from concise
                Normalize();
                // if still none, create new one with empty attributes
                if (atomArray == null)
                {
                    atomArray = new CMLAtomArray();
                    Add(atomArray);
                    atomArray.ElementType = Array.Empty<string>();
                    atomArray.Count = Array.Empty<double>();
                }
            }
            var elements = GetElementTypes();
            if (elements == null)
            {
                elements = Array.Empty<string>();
            }
            var counts = GetCounts()?.ToArray();
            if (counts == null)
            {
                counts = Array.Empty<double>();
            }
            var nelem = elements.Count;
            bool added = false;
            for (int i = 0; i < nelem; i++)
            {
                if (elements[i].Equals(elementType, StringComparison.Ordinal))
                {
                    counts[i] += count;
                    added = true;
                    break;
                }
            }
            if (!added)
            {
                var newElem = elements.ToList();
                newElem.Add(elementType);
                var newCount = counts.ToList();
                newCount.Add(count);
                atomArray.ElementType = newElem;
                atomArray.Count = newCount;
            }
            else
            {
                atomArray.ElementType = elements;
                atomArray.Count = counts;
            }
            int formalCharge = (this.Attribute(Attribute_formalCharge) == null) ? 0 : this.FormalCharge;
            string conciseS = atomArray.GenerateConcise(formalCharge);
            SetAttributeValue(Attribute_concise, conciseS);
        }

        /// <summary>
        /// Count for corresponding element.
        /// No defaults.
        /// </summary>
        /// <returns>double[] array of element counts; or null for none.</returns>
        public IReadOnlyList<double> GetCounts()
        {
            CMLAtomArray atomArray = AtomArrayElements.FirstOrDefault();
            return atomArray?.Count;
        }

        /// <summary>
        /// get atom count
        /// </summary>
        /// <returns>count</returns>
        public double GetTotalAtomCount()
        {
            //nwe23 - Fixed a bug here where GetCounts() returns null
            // for an empty formula, resulting in this crashing rather than
            // returning 0 as expected for empty formula
            var counts = GetCounts();
            if (counts == null)
            {
                return 0;
            }
            double total = 0;
            foreach (var count in counts)
            {
                total += count;
            }
            return total;
        }

        /// <summary>
        /// Count for corresponding element.
        /// No defaults.
        /// </summary>
        /// <returns>double[] array of element counts; or null for none.</returns>
        public IReadOnlyList<string> GetElementTypes()
        {
            CMLAtomArray atomArray = AtomArrayElements.FirstOrDefault();
            return atomArray?.ElementType;
        }
    }

    public static class CMLFormulaTools
    {
        private static readonly string[] namesTypes = new string[]
        {
            "No punctuation",
            "Element count whitespace",
            "Element whitespace count",
            "CML concise",
            "Multiplied element whitespace count",
            "Nested brackets",
            "IUPAC",
            "Moiety",
            "Submoiety",
            "Structural",
            "Any",
        };

        public static string Name(this CMLFormula.FormulaType value)
            => namesTypes[(int)value];

        private static readonly string[] namesSorts = new string[]
        {
            "Alphabetic elements",
            "C and H first",
        };

        public static string Name(this CMLFormula.SortType value)
            => namesSorts[(int)value];
    }
}
