/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer. 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution. 
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies, 
 * either expressed or implied, of the FreeBSD Project.
 */

using System;
using System.Collections;
using static NCDK.Beam.Element;

namespace NCDK.Beam
{
    /// <summary>
    /// Defines a model to determine the number of p electrons a particular element
    /// in a certain environment donates. There is no universally accepted 'correct'
    /// model and different models can produce very different results.
    /// </summary>
    // @author John May
    internal abstract class ElectronDonation
    {
        /// <summary>The Daylight model implementation.</summary>
        private static readonly ElectronDonation DAYLIGHT = new DaylightImpl();

        /// <summary>
        /// The number p electrons dominated by the atom label at vertex 'u' in the
        /// 'cycle' of the graph 'g'. Additionally the 'cyclic' bit set indicates the
        /// vertices are members of any cycle.
        ///
        /// Depending on the perception method the 'cycle' may not be known in which
        /// case a runtime error will be thrown if it is needed for determining the
        /// donation. The cycle will be Unknown if the method use the donation model
        /// builds up the cycle iteratively counting the number of p electrons.
        /// </summary>
        /// <param name="u">the vertex under consideration</param>
        /// <param name="g">the graph the vertex is referring to</param>
        /// <param name="cycle"> the cycle under consideration</param>
        /// <param name="cyclic">all cyclic vertices</param>
        /// <returns>the number of p electrons contributed to if the element or -1 if
        ///         the vertex should not be used</returns>
        public abstract int Contribution(int u, Graph g, ICycle cycle, BitArray cyclic);

        /// <summary>
        /// The Daylight aromatic model (aprox).
        ///
        /// <returns>electron donation model</returns>
        /// </summary>
        public static ElectronDonation Daylight => DAYLIGHT;

        /// <summary>The cyclic vertices.</summary>
        public interface ICycle
        {
            bool Contains(int u);
        }

        /// <summary>
        /// Daylight donation model - interpreted from various sources and testing
        /// the Daylight Depict service, http://www.daylight.com/daycgi/depict.
        /// </summary>
        private sealed class DaylightImpl : ElectronDonation
        {
            /// <inheritdoc/>
            public override int Contribution(int u, Graph g, ICycle cycle, BitArray cyclic)
            {

                if (!cyclic[u])
                    return -1;

                IAtom atom = g.GetAtom(u);
                Element elem = atom.Element;

                // the element isn't allow to be aromatic (Daylight spec)
                if (!elem.IsAromatic(Element.AromaticSpecification.Daylight) || elem == Element.Unknown)
                    return -1;

                // count cyclic and acyclic double bonds
                int nCyclic = 0, nAcyclic = 0;
                int deg = g.Degree(u) + g.ImplHCount(u);
                Edge acyclic = null;
                int sum = 0;
                foreach (var e in g.GetEdges(u))
                {
                    sum += e.Bond.Order;
                    if (e.Bond.Order == 2)
                    {
                        if (!cyclic[e.Other(u)])
                        {
                            nAcyclic++;
                            acyclic = e;
                        }
                        else
                        {
                            nCyclic++;
                        }
                    }
                }

                int charge = atom.Charge;
                int valence = sum + g.ImplHCount(u);

                if (!atom.Element.Verify(valence, charge))
                    return -1;
                if (deg > 3)
                    return -1;
                if (nCyclic > 1)
                    return -1;
                if (nCyclic == 1 && nAcyclic == 1)
                {
                    // [P|N](=O)(=*)* - note arsenic not allowed 
                    if ((elem == Nitrogen || elem == Phosphorus)
                            && g.GetAtom(acyclic.Other(u)).Element == Oxygen)
                        return 1;
                    return -1;
                }
                else if (nCyclic == 1 && nAcyclic == 0)
                {
                    // any element (except Arsenic) with a single cyclic double bond
                    // contributes 1 p electron
                    return elem != Arsenic ? 1 : -1;
                }
                else if (nCyclic == 0 && nAcyclic == 1)
                {
                    // a cyclic exo-cyclic double bond - how many electrons determine
                    // by AcyclicContribution()
                    return AcyclicContribution(atom, g.GetAtom(acyclic.Other(u)), charge);
                }
                else if (nCyclic == 0 && nAcyclic == 0 && charge > -3)
                {
                    // no double bonds - do we have any lone pairs to contribute?
                    int v = Valence(elem, charge);
                    if (v - sum >= 2 && charge <= 0)
                        return 2;
                    if (charge == 1 && atom.Element == Carbon)
                        return 0;
                }

                return -1;
            }

            private static int Valence(Element elem, int q)
            {
                return Valence(Element.OfNumber(elem.AtomicNumber - q));
            }

            private static int Valence(Element elem)
            {
                if (
                    elem == Boron ||
                    elem == Aluminium ||
                    elem == Gallium)
                {
                    return 3;
                }
                else if (
                    elem == Carbon ||
                    elem == Silicon ||
                    elem == Germanium)
                {
                    return 4;
                }
                else if (
                    elem == Nitrogen ||
                    elem == Phosphorus ||
                    elem == Arsenic)
                {
                    return 5;
                }
                else if (
                    elem == Oxygen ||
                    elem == Sulfur ||
                    elem == Selenium)
                {
                    return 6;
                }
                else if (
                    elem == Fluorine ||
                    elem == Chlorine ||
                    elem == Bromine)
                {
                    return 7;
                }
                else
                    throw new NotSupportedException("Valence not yet handled for element with atomic number " + elem);
            }

            /// <summary>
            /// For a 'cyclic' atom double bonded to an 'acyclic' atom how many
            /// electrons should be donated?
            ///
            /// <param name="cyclic"> cyclic atom</param>
            /// <param name="acyclic">acyclic atom double bonded to the 'cyclic' atom</param>
            /// <param name="charge"> charge on the cyclic atom</param>
            /// <returns>number of donated electrons</returns>
            /// </summary>
            static int AcyclicContribution(IAtom cyclic, IAtom acyclic, int charge)
            {
                var aa = cyclic.Element;
                if (aa == Carbon)
                {
                    // carbon bonded to any exocyclic element (other than carbon)
                    // gives 1 electron
                    return acyclic.Element != Carbon ? 0 : 1;
                }
                else if (aa == Nitrogen || aa == Phosphorus)
                {
                    return charge == 1 ? 1 : -1;
                }
                else if (aa == Sulfur)
                { 
                    return charge == 0 && acyclic.Element == Oxygen ? 2 : -1;
                }
                return -1;
            }
        }
    }
}
