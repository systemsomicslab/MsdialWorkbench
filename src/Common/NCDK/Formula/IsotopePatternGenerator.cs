/* Copyright (C) 2007  Miguel Rojasch <miguelrojasch@users.sf.net>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
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

using NCDK.Config;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Formula
{
    /// <summary>
    /// Generates all Combinatorial chemical isotopes given a structure.
    /// </summary>
    // @cdk.module  formula
    // @author      Miguel Rojas Cherto
    // @cdk.created 2007-11-20
    // @cdk.keyword isotope pattern
    public class IsotopePatternGenerator
    {
        private IChemObjectBuilder builder = null;
        private IsotopeFactory isoFactory = CDK.IsotopeFactory;

        /// <summary>
        /// Minimal abundance of the isotopes to be added in the combinatorial search.
        /// </summary>
        private double minIntensity = 0.00001;
        private readonly double minAbundance = 1E-10; // n.b. not actually abundance
        private double resolution = 0.00005f;
        private bool storeFormula = false;

        /// <summary>
        /// Maximum tolerance between two mass 
        /// </summary>
        private const double Tolerance = 0.00005;

        /// <summary>
        /// Constructor for the IsotopeGenerator. The minimum abundance is set to 
        ///                         0.1 (10% abundance) by default. 
        /// </summary>
        public IsotopePatternGenerator()
            : this(0.1)
        { }

        /// <param name="minIntensity">Minimal intensity of the isotopes to be added in the combinatorial search (scale 0.0 to 1.0)</param>
        public IsotopePatternGenerator(double minIntensity)
        {
            this.minIntensity = minIntensity;
            Trace.TraceInformation("Generating all Isotope structures with IsotopeGenerator");
        }

        /// <summary>
        /// Set the minimum (normalised) intensity to generate.
        /// </summary>
        /// <param name="minIntensity">the minimum intensity</param>
        /// <returns>self for method chaining</returns>
        public IsotopePatternGenerator SetMinIntensity(double minIntensity)
        {
            this.minIntensity = minIntensity;
            return this;
        }

        /// <summary>
        /// Set the minimum resolution at which peaks within this mass difference
        /// should be considered equivalent.
        /// </summary>the minimum resolution
        /// <param name="resolution"></param>
        /// <returns>self for method chaining</returns>
        public IsotopePatternGenerator SetMinResolution(double resolution)
        {
            this.resolution = resolution;
            return this;
        }

        /// <summary>
        /// When generating the isotope containers store the MF for each
        /// <see cref="IsotopeContainer"/>.
        /// </summary>
        /// <param name="storeFormula">formulas should be stored</param>
        /// <returns>self for method chaining</returns>
        public IsotopePatternGenerator SetStoreFormulas(bool storeFormula)
        {
            this.storeFormula = storeFormula;
            return this;
        }

        /// <summary>
        /// Minimum abundance
        /// </summary>
        public double MinAbundance => minAbundance;

        /// <summary>
        /// Get all combinatorial chemical isotopes given a structure.
        /// </summary>
        /// <param name="molFor">The IMolecularFormula to start</param>
        /// <returns>A IsotopePattern object containing the different combinations</returns>
        public IsotopePattern GetIsotopes(IMolecularFormula molFor)
        {
            if (builder == null)
            {
                try
                {
                    isoFactory = CDK.IsotopeFactory;
                    builder = molFor.Builder;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.StackTrace);
                }
            }
            var mf = MolecularFormulaManipulator.GetString(molFor, true);

            var molecularFormula = MolecularFormulaManipulator.GetMajorIsotopeMolecularFormula(mf, builder);

            IsotopePattern abundance_Mass = null;

            foreach (var isos in molecularFormula.Isotopes)
            {
                var elementSymbol = isos.Symbol;
                var atomCount = molecularFormula.GetCount(isos);

                // Generate possible isotope containers for the current atom's
                // these will then me 'multiplied' with the existing patten
                var additional = new List<IsotopeContainer>();
                foreach (var isotope in isoFactory.GetIsotopes(elementSymbol))
                {
                    double mass = isotope.ExactMass.Value;
                    double abundance = isotope.Abundance.Value;
                    if (abundance <= 0.000000001)
                        continue;
                    IsotopeContainer container = new IsotopeContainer(mass, abundance);
                    if (storeFormula)
                        container.Formula = AsFormula(isotope);
                    additional.Add(container);
                }
                for (int i = 0; i < atomCount; i++)
                    abundance_Mass = CalculateAbundanceAndMass(abundance_Mass, additional);
            }

            var isoP = IsotopePatternManipulator.SortAndNormalizedByIntensity(abundance_Mass);
            isoP = CleanAbundance(isoP, minIntensity);
            var isoPattern = IsotopePatternManipulator.SortByMass(isoP);
            return isoPattern;
        }

        private IMolecularFormula AsFormula(IIsotope isotope)
        {
            var mf = builder.NewMolecularFormula();
            mf.Add(isotope);
            return mf;
        }

        private IMolecularFormula Union(IMolecularFormula a, IMolecularFormula b)
        {
            var mf = builder.NewMolecularFormula();
            mf.Add(a);
            mf.Add(b);
            return mf;
        }

        private static IsotopeContainer FindExisting(List<IsotopeContainer> containers, double mass, double treshhold)
        {
            foreach (var container in containers)
            {
                if (Math.Abs(container.Mass - mass) <= treshhold)
                {
                    return container;
                }
            }
            return null;
        }

        private void AddDistinctFormula(IsotopeContainer container, IMolecularFormula mf)
        {
            foreach (var curr in container.Formulas)
                if (MolecularFormulaManipulator.Compare(curr, mf))
                    return;
            container.AddFormula(mf);
        }

        /// <summary>
        /// Calculates the mass and abundance of all isotopes generated by adding one
        /// atom. Receives the periodic table element and calculate the isotopes, if
        /// there exist a previous calculation, add these new isotopes. In the
        /// process of adding the new isotopes, remove those that has an abundance
        /// less than setup parameter minIntensity, and remove duplicated masses.
        /// </summary>
        /// <param name="additional">additional isotopes to 'multiple' the current pattern by</param>
        /// <returns>the calculation was successful</returns>
        private IsotopePattern CalculateAbundanceAndMass(IsotopePattern current, List<IsotopeContainer> additional)
        {
            if (additional == null || additional.Count == 0)
                return current;

            var containers = new List<IsotopeContainer>();

            // Verify if there is a previous calculation. If it exists, add the new
            // isotopes
            if (current == null)
            {
                current = new IsotopePattern();
                foreach (var container in additional)
                    current.isotopes.Add(container);
            }
            else
            {
                foreach (var container in current.Isotopes)
                {
                    foreach (IsotopeContainer other in additional)
                    {
                        var abundance = container.Intensity * other.Intensity * 0.01;
                        var mass = container.Mass + other.Mass;

                        // merge duplicates with some resolution
                        var existing = FindExisting(containers, mass, resolution);
                        if (existing != null)
                        {
                            var newIntensity = existing.Intensity + abundance;
                            // moving weighted avg
                            existing.Mass = (existing.Mass * existing.Intensity +
                                             mass * abundance) / newIntensity;
                            existing.Intensity = newIntensity;
                            if (storeFormula)
                            {
                                foreach (var mf in container.Formulas)
                                    AddDistinctFormula(existing, Union(mf, other.Formula));
                            }
                            continue;
                        }

                        // Filter isotopes too small
                        if (abundance > minAbundance)
                        {
                            var newcontainer = new IsotopeContainer(mass, abundance);
                            if (storeFormula)
                            {
                                foreach (var mf in container.Formulas)
                                    newcontainer.AddFormula(Union(mf, other.Formula));
                            }
                            containers.Add(newcontainer);
                        }
                    }
                }

                current = new IsotopePattern();
                foreach (var container in containers)
                {
                    current.isotopes.Add(container);
                }
            }
            return current;
        }

        /// <summary>
        /// Normalize the intensity (relative abundance) of all isotopes in relation
        /// of the most abundant isotope.
        /// </summary>
        /// <param name="isopattern">The IsotopePattern object</param>
        /// <param name="minIntensity">The minimum abundance</param>
        /// <returns>The IsotopePattern cleaned</returns>
        private static IsotopePattern CleanAbundance(IsotopePattern isopattern, double minIntensity)
        {
            double intensity;
            double biggestIntensity = 0;

            foreach (var sc in isopattern.Isotopes)
            {
                intensity = sc.Intensity;
                if (intensity > biggestIntensity)
                    biggestIntensity = intensity;
            }

            foreach (var sc in isopattern.Isotopes)
            {
                intensity = sc.Intensity;
                intensity /= biggestIntensity;
                if (intensity < 0)
                    intensity = 0;

                sc.Intensity = intensity;
            }

            var sortedIsoPattern = new IsotopePattern
            {
                MonoIsotope = new IsotopeContainer(isopattern.Isotopes[0])
            };
            for (int i = 1; i < isopattern.Isotopes.Count; i++)
            {
                if (isopattern.Isotopes[i].Intensity >= (minIntensity))
                {
                    var container = new IsotopeContainer(isopattern.Isotopes[i]);
                    sortedIsoPattern.isotopes.Add(container);
                }
            }
            return sortedIsoPattern;
        }
    }
}
