/* Copyright (C) 2000-2009  Christoph Steinbeck, Stefan Kuhn<shk3@users.sf.net>
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

using NCDK.Graphs;
using NCDK.Maths;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.StructGen.Stochastic.Operators
{
    /// <summary>
    /// Modified molecular structures by applying crossover operator on a pair of parent structures
    /// and generate a pair of offspring structures. Each of the two offspring structures inherits
    /// a certain fragments from both of its parents.
    /// </summary>
    // @cdk.module structgen
    public class CrossoverMachine
    {
        PartialFilledStructureMerger pfsm = CDK.PartialFilledStructureMerger;

        /// <summary>
        /// Indicates which mode <see cref="CrossoverMachine"/> is using.
        /// </summary>
        public enum SplitMode
        {
            /// <summary>Random mode.</summary>
            Random = 0,
            /// <summary>Depth first mode.</summary>
            DepthFirst = 1,
            /// <summary>Breadth first mode.</summary>
            BreadthFirst = 2,
        }

        /// <summary>selects a partitioning mode</summary>
        readonly SplitMode splitMode = SplitMode.BreadthFirst;
        /// <summary>selects a partitioning scale</summary>
        int numatoms = 5;

        /// <summary>Constructs a new CrossoverMachine operator.</summary>
        public CrossoverMachine()
        {
        }

        /// <summary>
        /// Performs the n point crossover of two <see cref="IAtomContainer"/>.
        /// </summary>
        /// <remarks>
        /// Precondition: The atoms in the molecules are ordered by properties to
        /// preserve (e. g. atom symbol). Due to its randomized nature, this method
        /// fails in around 3% of all cases. A <see cref="CDKException"/> with message "Could not
        /// mate these properly" will then be thrown.
        /// </remarks>
        /// <returns>The children.</returns>
        /// <exception cref="CDKException">if it was not possible to form off springs.</exception>
        public IReadOnlyList<IAtomContainer> DoCrossover(IAtomContainer dad, IAtomContainer mom)
        {
            int tries = 0;
            while (true)
            {
                int dim = dad.Atoms.Count;
                var redChild = new IAtomContainer[2];
                var blueChild = new IAtomContainer[2];

                var redAtoms = new List<int>();
                var blueAtoms = new List<int>();

                // randomly divide atoms into two parts: redAtoms and blueAtoms.
                if (splitMode == SplitMode.Random)
                {
                    // better way to randomly divide atoms into two parts: redAtoms
                    // and blueAtoms.
                    for (int i = 0; i < dim; i++)
                        redAtoms.Add(i);
                    for (int i = 0; i < (dim - numatoms); i++)
                    {
                        int ranInt = RandomNumbersTool.RandomInt(0, redAtoms.Count - 1);
                        redAtoms.RemoveAt(ranInt);
                        blueAtoms.Add(ranInt);
                    }
                }
                else
                {
                    // split graph using depth/breadth first traverse 
                    var graph = new ChemGraph(dad) { NumAtoms = numatoms };
                    if (splitMode == SplitMode.DepthFirst)
                    {
                        redAtoms = graph.PickDFGraph().ToList();
                    }
                    else
                    {
                        //this is SPLIT_MODE_BREADTH_FIRST
                        redAtoms = graph.PickBFGraph().ToList();
                    }

                    for (int i = 0; i < dim; i++)
                    {
                        int element = i;
                        if (!(redAtoms.Contains(element)))
                        {
                            blueAtoms.Add(element);
                        }
                    }
                }
                /* * dividing over ** */
                redChild[0] = dad.Builder.NewAtomContainer(dad);
                blueChild[0] = dad.Builder.NewAtomContainer(dad);
                redChild[1] = dad.Builder.NewAtomContainer(mom);
                blueChild[1] = dad.Builder.NewAtomContainer(mom);

                var blueAtomsInRedChild0 = new List<IAtom>();
                for (int j = 0; j < blueAtoms.Count; j++)
                {
                    blueAtomsInRedChild0.Add(redChild[0].Atoms[(int)blueAtoms[j]]);
                }
                for (int j = 0; j < blueAtomsInRedChild0.Count; j++)
                {
                    redChild[0].RemoveAtom(blueAtomsInRedChild0[j]);
                }
                var blueAtomsInRedChild1 = new List<IAtom>();
                for (int j = 0; j < blueAtoms.Count; j++)
                {
                    blueAtomsInRedChild1.Add(redChild[1].Atoms[(int)blueAtoms[j]]);
                }
                for (int j = 0; j < blueAtomsInRedChild1.Count; j++)
                {
                    redChild[1].RemoveAtom(blueAtomsInRedChild1[j]);
                }
                var redAtomsInBlueChild0 = new List<IAtom>();
                for (int j = 0; j < redAtoms.Count; j++)
                {
                    redAtomsInBlueChild0.Add(blueChild[0].Atoms[(int)redAtoms[j]]);
                }
                for (int j = 0; j < redAtomsInBlueChild0.Count; j++)
                {
                    blueChild[0].RemoveAtom(redAtomsInBlueChild0[j]);
                }
                var redAtomsInBlueChild1 = new List<IAtom>();
                for (int j = 0; j < redAtoms.Count; j++)
                {
                    redAtomsInBlueChild1.Add(blueChild[1].Atoms[(int)redAtoms[j]]);
                }
                for (int j = 0; j < redAtomsInBlueChild1.Count; j++)
                {
                    blueChild[1].RemoveAtom(redAtomsInBlueChild1[j]);
                }
                //if the two fragments of one and only one parent have an uneven number
                //of attachment points, we need to rearrange them
                var satCheck = CDK.SaturationChecker;
                double red1attachpoints = 0;
                for (int i = 0; i < redChild[0].Atoms.Count; i++)
                {
                    red1attachpoints += satCheck.GetCurrentMaxBondOrder(redChild[0].Atoms[i], redChild[0]);
                }
                double red2attachpoints = 0;
                for (int i = 0; i < redChild[1].Atoms.Count; i++)
                {
                    red2attachpoints += satCheck.GetCurrentMaxBondOrder(redChild[1].Atoms[i], redChild[1]);
                }
                bool isok = true;
                if (red1attachpoints % 2 == 1 ^ red2attachpoints % 2 == 1)
                {
                    isok = false;
                    var firstToBalance = redChild[1];
                    var secondToBalance = blueChild[0];
                    if (red1attachpoints % 2 == 1)
                    {
                        firstToBalance = redChild[0];
                        secondToBalance = blueChild[1];
                    }
                    //we need an atom which has
                    //- an uneven number of "attachment points" and
                    //- an even number of outgoing bonds
                    foreach (var atom in firstToBalance.Atoms)
                    {
                        if (satCheck.GetCurrentMaxBondOrder(atom, firstToBalance) % 2 == 1
                         && firstToBalance.GetBondOrderSum(atom) % 2 == 0)
                        {
                            //we remove this from it's current container and add it to the other one
                            firstToBalance.RemoveAtom(atom);
                            secondToBalance.Atoms.Add(atom);
                            isok = true;
                            break;
                        }
                    }
                }
                //if we have combinable fragments
                if (isok)
                {
                    //combine the fragments crosswise
                    var newstrucs = new IChemObjectSet<IAtomContainer>[2];
                    newstrucs[0] = dad.Builder.NewAtomContainerSet();
                    newstrucs[0].AddRange(ConnectivityChecker.PartitionIntoMolecules(redChild[0]));
                    newstrucs[0].AddRange(ConnectivityChecker.PartitionIntoMolecules(blueChild[1]));
                    newstrucs[1] = dad.Builder.NewAtomContainerSet();
                    newstrucs[1].AddRange(ConnectivityChecker.PartitionIntoMolecules(redChild[1]));
                    newstrucs[1].AddRange(ConnectivityChecker.PartitionIntoMolecules(blueChild[0]));

                    //and merge
                    var children = new List<IAtomContainer>(2);
                    for (int f = 0; f < 2; f++)
                    {
                        var structrue = pfsm.Generate2(newstrucs[f]);
                        if (structrue != null)
                        {
                            //if children are not correct, the outer loop will repeat,
                            //so we ignore this
                            children.Add(structrue);
                        }
                    }
                    if (children.Count == 2 
                     && ConnectivityChecker.IsConnected(children[0])
                     && ConnectivityChecker.IsConnected(children[1]))
                        return children;
                }
                tries++;
                if (tries > 20)
                    throw new CDKException("Could not mate these properly");
            }
        }
    }
}
