/* 
 * Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

using NCDK.Geometries;
using NCDK.Graphs;
using NCDK.Layout;
using NCDK.Numerics;
using NCDK.RingSearches;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NCDK.Modelings.Builder3D
{
    /// <summary>
    ///  The main class to generate the 3D coordinates of a molecule ModelBuilder3D.
    /// </summary>
    /// <example>
    ///  Its use looks like:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Modelings.Builder3D.ModelBuilder3D_Example.cs"]/*' />
    /// </example>
    /// <remarks>
    /// Standing problems:
    /// <list type="bullet">
    ///    <item>condensed ring systems which are unknown for the template class</item>
    ///    <item>vdWaals clashes</item>
    ///    <item>stereochemistry</item>
    ///    <item>chains running through ring systems</item>
    /// </list>
    /// </remarks>
    // @author      cho
    // @author      steinbeck
    // @cdk.created 2004-09-07
    // @cdk.module  builder3d
    // @cdk.keyword 3D coordinates
    // @cdk.keyword coordinate generation, 3D
    public class ModelBuilder3D
    {
        private static ConcurrentDictionary<string, ModelBuilder3D> memyselfandi = new ConcurrentDictionary<string, ModelBuilder3D>();
        private TemplateHandler3D templateHandler = null;
        private IReadOnlyDictionary<string, object> parameterSet = null;
        private readonly ForceFieldConfigurator ffc = new ForceFieldConfigurator();
        string forceFieldName = "mm2";

        /// <summary>
        /// Constructor for the ModelBuilder3D object.
        /// </summary>
        /// <param name="templateHandler">templateHandler Object</param>
        /// <param name="ffname">name of force field</param>
        private ModelBuilder3D(TemplateHandler3D templateHandler, string ffname)
        {
            SetTemplateHandler(templateHandler);
            SetForceField(ffname);
        }

        public static ModelBuilder3D GetInstance(TemplateHandler3D templateHandler, string ffname)
        {
            if (ffname == null || ffname.Length == 0)
                throw new CDKException("The given ffname is null or empty!");
            if (templateHandler == null)
                throw new CDKException("The given template handler is null!");

            var builderCode = $"{templateHandler.GetType().FullName}#{ffname}";
            return memyselfandi.GetOrAdd(builderCode, n => new ModelBuilder3D(templateHandler, ffname));
        }

        public static ModelBuilder3D GetInstance()
        {
            return GetInstance(TemplateHandler3D.Instance, "mm2");
        }

        /// <summary>
        /// Gives a list of possible force field types.
        /// </summary>
        /// <returns>the list</returns>
        public string[] GetFfTypes()
        {
            return ffc.GetFfTypes();
        }

        /// <summary>
        /// Sets the forceField attribute of the ModeSetForceFieldConfigurator(lBuilder3D object.
        /// </summary>
        /// <param name="ffname">forceField name</param>
        private void SetForceField(string ffname)
        {
            if (ffname == null)
            {
                ffname = "mm2";
            }
            try
            {
                forceFieldName = ffname;
                ffc.SetForceFieldConfigurator(ffname);
                parameterSet = ffc.GetParameterSet();
            }
            catch (CDKException ex1)
            {
                Trace.TraceError($"Problem with ForceField configuration due to>{ex1.Message}");
                Debug.WriteLine(ex1);
                throw new CDKException("Problem with ForceField configuration due to>" + ex1.Message, ex1);
            }
        }

        /// <summary>
        /// Generate 3D coordinates with force field information.
        /// </summary>
        public IAtomContainer Generate3DCoordinates(IAtomContainer molecule, bool clone)
        {
            var originalAtomTypeNames = molecule.Atoms.Select(n => n.AtomTypeName).ToArray();

            Debug.WriteLine("******** GENERATE COORDINATES ********");
            foreach (var atom in molecule.Atoms)
            {
                atom.IsPlaced = false;
                atom.IsVisited = false;
            }
            //CHECK FOR CONNECTIVITY!
            Debug.WriteLine($"#atoms>{molecule.Atoms.Count}");
            if (!ConnectivityChecker.IsConnected(molecule))
                throw new CDKException("Molecule is NOT connected, could not layout.");

            // setup helper classes
            AtomPlacer atomPlacer = new AtomPlacer();
            AtomPlacer3D ap3d = new AtomPlacer3D(parameterSet);
            AtomTetrahedralLigandPlacer3D atlp3d = new AtomTetrahedralLigandPlacer3D(parameterSet);

            if (clone)
                molecule = (IAtomContainer)molecule.Clone();
            atomPlacer.Molecule = molecule;

            if (ap3d.NumberOfUnplacedHeavyAtoms(molecule) == 1)
            {
                Debug.WriteLine("Only one Heavy Atom");
                ap3d.GetUnplacedHeavyAtom(molecule).Point3D = new Vector3(0.0, 0.0, 0.0);
                try
                {
                    atlp3d.Add3DCoordinatesForSinglyBondedLigands(molecule);
                }
                catch (CDKException ex3)
                {
                    Trace.TraceError($"PlaceSubstitutensERROR: Cannot place substitutents due to:{ex3.Message}");
                    Debug.WriteLine(ex3);
                    throw new CDKException("PlaceSubstitutensERROR: Cannot place substitutents due to:" + ex3.Message,
                            ex3);
                }
                return molecule;
            }
            //Assing Atoms to Rings,Aliphatic and Atomtype
            IRingSet ringSetMolecule = ffc.AssignAtomTyps(molecule);
            IRingSet largestRingSet = null;
            int numberOfRingAtoms = 0;

            IReadOnlyList<IRingSet> ringSystems = null;
            if (ringSetMolecule.Count > 0)
            {
                if (templateHandler == null)
                {
                    throw new CDKException("You are trying to generate coordinates for a molecule with rings, but you have no template handler set. Please do SetTemplateHandler() before generation!");
                }
                ringSystems = RingPartitioner.PartitionRings(ringSetMolecule);
                largestRingSet = RingSetManipulator.GetLargestRingSet(ringSystems);
                IAtomContainer largestRingSetContainer = RingSetManipulator.GetAllInOneContainer(largestRingSet);
                numberOfRingAtoms = largestRingSetContainer.Atoms.Count;
                templateHandler.MapTemplates(largestRingSetContainer, numberOfRingAtoms);
                if (!CheckAllRingAtomsHasCoordinates(largestRingSetContainer))
                {
                    throw new CDKException("RingAtomLayoutError: Not every ring atom is placed! Molecule cannot be layout.");
                }

                SetAtomsToPlace(largestRingSetContainer);
                SearchAndPlaceBranches(molecule, largestRingSetContainer, ap3d, atlp3d, atomPlacer);
                largestRingSet = null;
            }
            else
            {
                //Debug.WriteLine("****** Start of handling aliphatic molecule ******");
                IAtomContainer ac = AtomPlacer.GetInitialLongestChain(molecule);
                SetAtomsToUnVisited(molecule);
                SetAtomsToUnplaced(molecule);
                ap3d.PlaceAliphaticHeavyChain(molecule, ac);
                //ZMatrixApproach
                ap3d.ZMatrixChainToCartesian(molecule, false);
                SearchAndPlaceBranches(molecule, ac, ap3d, atlp3d, atomPlacer);
            }
            LayoutMolecule(ringSystems, molecule, ap3d, atlp3d, atomPlacer);
            //Debug.WriteLine("******* PLACE SUBSTITUENTS ******");
            try
            {
                atlp3d.Add3DCoordinatesForSinglyBondedLigands(molecule);
            }
            catch (CDKException ex3)
            {
                Trace.TraceError($"PlaceSubstitutensERROR: Cannot place substitutents due to:{ex3.Message}");
                Debug.WriteLine(ex3);
                throw new CDKException("PlaceSubstitutensERROR: Cannot place substitutents due to:" + ex3.Message, ex3);
            }
            // restore the original atom type names
            for (int i = 0; i < originalAtomTypeNames.Length; i++)
            {
                molecule.Atoms[i].AtomTypeName = originalAtomTypeNames[i];
            }

            return molecule;
        }

        /// <summary>
        /// Gets the ringSetOfAtom attribute of the ModelBuilder3D object.
        /// </summary>
        /// <returns>The ringSetOfAtom value</returns>
        private static IRingSet GetRingSetOfAtom(IReadOnlyList<IRingSet> ringSystems, IAtom atom)
        {
            IRingSet ringSetOfAtom = null;
            for (int i = 0; i < ringSystems.Count; i++)
            {
                if (((IRingSet)ringSystems[i]).Contains(atom))
                {
                    return (IRingSet)ringSystems[i];
                }
            }
            return ringSetOfAtom;
        }

        /// <summary>
        /// Layout the molecule, starts with ring systems and than aliphatic chains.
        /// </summary>
        /// <param name="ringSetMolecule">ringSystems of the molecule</param>
        private void LayoutMolecule(IReadOnlyList<IRingSet> ringSetMolecule, IAtomContainer molecule, AtomPlacer3D ap3d,
                AtomTetrahedralLigandPlacer3D atlp3d, AtomPlacer atomPlacer)
        {
            //Debug.WriteLine("****** LAYOUT MOLECULE MAIN *******");
            IAtomContainer ac = null;
            int safetyCounter = 0;
            IAtom atom = null;
            //Place rest Chains/Atoms
            do
            {
                safetyCounter++;
                atom = ap3d.GetNextPlacedHeavyAtomWithUnplacedRingNeighbour(molecule);
                if (atom != null)
                {
                    //Debug.WriteLine("layout RingSystem...");
                    var unplacedAtom = ap3d.GetUnplacedRingHeavyAtom(molecule, atom);
                    var ringSetA = GetRingSetOfAtom(ringSetMolecule, unplacedAtom);
                    var ringSetAContainer = RingSetManipulator.GetAllInOneContainer(ringSetA);
                    templateHandler.MapTemplates(ringSetAContainer, ringSetAContainer.Atoms.Count);

                    if (CheckAllRingAtomsHasCoordinates(ringSetAContainer))
                    {
                    }
                    else
                    {
                        throw new IOException("RingAtomLayoutError: Not every ring atom is placed! Molecule cannot be layout. Sorry");
                    }

                    Vector3 firstAtomOriginalCoord = unplacedAtom.Point3D.Value;
                    Vector3 centerPlacedMolecule = ap3d.GeometricCenterAllPlacedAtoms(molecule);

                    SetBranchAtom(molecule, unplacedAtom, atom, ap3d.GetPlacedHeavyAtoms(molecule, atom), ap3d, atlp3d);
                    LayoutRingSystem(firstAtomOriginalCoord, unplacedAtom, ringSetA, centerPlacedMolecule, atom, ap3d);
                    SearchAndPlaceBranches(molecule, ringSetAContainer, ap3d, atlp3d, atomPlacer);
                    //Debug.WriteLine("Ready layout Ring System");
                    ringSetA = null;
                    unplacedAtom = null;
                }
                else
                {
                    //Debug.WriteLine("layout chains...");
                    SetAtomsToUnVisited(molecule);
                    atom = ap3d.GetNextPlacedHeavyAtomWithUnplacedAliphaticNeighbour(molecule);
                    if (atom != null)
                    {
                        ac = atom.Builder.NewAtomContainer();
                        ac.Atoms.Add(atom);
                        SearchAndPlaceBranches(molecule, ac, ap3d, atlp3d, atomPlacer);
                        ac = null;
                    }
                }
            } while (!ap3d.AllHeavyAtomsPlaced(molecule) || safetyCounter > molecule.Atoms.Count);
        }

        /// <summary>
        /// Layout the ring system, rotate and translate the template.
        /// </summary>
        /// <param name="originalCoord">coordinates of the placedRingAtom from the template</param>
        /// <param name="placedRingAtom">placedRingAtom</param>
        /// <param name="ringSet">ring system which placedRingAtom is part of</param>
        /// <param name="centerPlacedMolecule">the geometric center of the already placed molecule</param>
        /// <param name="atomB">placed neighbour atom of placedRingAtom</param>
        private static void LayoutRingSystem(Vector3 originalCoord, IAtom placedRingAtom, IRingSet ringSet,
                Vector3 centerPlacedMolecule, IAtom atomB, AtomPlacer3D ap3d)
        {
            //Debug.WriteLine("****** Layout ring System ******");Console.Out.WriteLine(">around atom:"+molecule.Atoms.IndexOf(placedRingAtom));
            IAtomContainer ac = RingSetManipulator.GetAllInOneContainer(ringSet);
            Vector3 newCoord = placedRingAtom.Point3D.Value;
            Vector3 axis = new Vector3(atomB.Point3D.Value.X - newCoord.X, atomB.Point3D.Value.Y - newCoord.Y, atomB.Point3D.Value.Z - newCoord.Z);
            TranslateStructure(originalCoord, newCoord, ac);
            //Rotate Ringsystem to farthest possible point
            Vector3 startAtomVector = new Vector3(newCoord.X - atomB.Point3D.Value.X, newCoord.Y - atomB.Point3D.Value.Y, newCoord.Z - atomB.Point3D.Value.Z);
            IAtom farthestAtom = ap3d.GetFarthestAtom(placedRingAtom.Point3D.Value, ac);
            Vector3 farthestAtomVector = new Vector3(farthestAtom.Point3D.Value.X - newCoord.X, farthestAtom.Point3D.Value.Y - newCoord.Y, farthestAtom.Point3D.Value.Z - newCoord.Z);
            Vector3 n1 = Vector3.Cross(axis, farthestAtomVector);
            n1 = Vector3.Normalize(n1);
            double lengthFarthestAtomVector = farthestAtomVector.Length();
            Vector3 farthestVector = startAtomVector;
            farthestVector = Vector3.Normalize(farthestVector);
            farthestVector *= startAtomVector.Length() + lengthFarthestAtomVector;
            double dotProduct = Vector3.Dot(farthestAtomVector, farthestVector);
            double angle = Math.Acos(dotProduct / (farthestAtomVector.Length() * farthestVector.Length()));
            Vector3 ringCenter = new Vector3();

            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                if (!(ac.Atoms[i].IsPlaced))
                {
                    ringCenter.X = (ac.Atoms[i].Point3D).Value.X - newCoord.X;
                    ringCenter.Y = (ac.Atoms[i].Point3D).Value.Y - newCoord.Y;
                    ringCenter.Z = (ac.Atoms[i].Point3D).Value.Z - newCoord.Z;
                    ringCenter = AtomTetrahedralLigandPlacer3D.Rotate(ringCenter, n1, angle);
                    ac.Atoms[i].Point3D =
                            new Vector3(ringCenter.X + newCoord.X, ringCenter.Y + newCoord.Y, ringCenter.Z + newCoord.Z);
                    //ac.GetAtomAt(i).IsPlaced = true;
                }
            }

            //Rotate Ring so that geometric center is max from placed center
            //Debug.WriteLine("Rotate RINGSYSTEM");
            Vector3 pointRingCenter = GeometryUtil.Get3DCenter(ac);
            double distance = 0;
            double rotAngleMax = 0;
            angle = 1 / 180 * Math.PI;
            ringCenter = new Vector3(pointRingCenter.X, pointRingCenter.Y, pointRingCenter.Z);
            ringCenter.X = ringCenter.X - newCoord.X;
            ringCenter.Y = ringCenter.Y - newCoord.Y;
            ringCenter.Z = ringCenter.Z - newCoord.Z;
            for (int i = 1; i < 360; i++)
            {
                ringCenter = AtomTetrahedralLigandPlacer3D.Rotate(ringCenter, axis, angle);
                if (Vector3.Distance(centerPlacedMolecule, new Vector3(ringCenter.X, ringCenter.Y, ringCenter.Z)) > distance)
                {
                    rotAngleMax = i;
                    distance = Vector3.Distance(centerPlacedMolecule, new Vector3(ringCenter.X, ringCenter.Y, ringCenter.Z));
                }
            }

            //rotate ring around axis with best angle
            rotAngleMax = (rotAngleMax / 180) * Math.PI;
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                if (!(ac.Atoms[i].IsPlaced))
                {
                    ringCenter.X = (ac.Atoms[i].Point3D).Value.X;
                    ringCenter.Y = (ac.Atoms[i].Point3D).Value.Y;
                    ringCenter.Z = (ac.Atoms[i].Point3D).Value.Z;
                    ringCenter = AtomTetrahedralLigandPlacer3D.Rotate(ringCenter, axis, rotAngleMax);
                    ac.Atoms[i].Point3D = new Vector3(ringCenter.X, ringCenter.Y, ringCenter.Z);
                    ac.Atoms[i].IsPlaced = true;
                }
            }
        }

        /// <summary>
        /// Sets a branch atom to a ring or aliphatic chain.
        /// </summary>
        /// <param name="unplacedAtom">The new branchAtom</param>
        /// <param name="atomA">placed atom to which the unplaced atom is connected</param>
        /// <param name="atomNeighbours">placed atomNeighbours of atomA</param>
        private static void SetBranchAtom(IAtomContainer molecule, IAtom unplacedAtom, IAtom atomA, IAtomContainer atomNeighbours,
                AtomPlacer3D ap3d, AtomTetrahedralLigandPlacer3D atlp3d)
        {
            //Debug.WriteLine("****** SET Branch Atom ****** >"+molecule.Atoms.IndexOf(unplacedAtom));
            IAtomContainer noCoords = molecule.Builder.NewAtomContainer();
            noCoords.Atoms.Add(unplacedAtom);
            Vector3 centerPlacedMolecule = ap3d.GeometricCenterAllPlacedAtoms(molecule);
            IAtom atomB = atomNeighbours.Atoms[0];

            string atypeNameA = atomA.AtomTypeName;
            string atypeNameB = atomB.AtomTypeName;
            string atypeNameUnplaced = unplacedAtom.AtomTypeName;

            double length = ap3d.GetBondLengthValue(atypeNameA, atypeNameUnplaced);
            double angle = (ap3d.GetAngleValue(atypeNameB, atypeNameA, atypeNameUnplaced)) * Math.PI / 180;
            
            // Console.Out.WriteLine("A:"+atomA.Symbol+" "+atomA.AtomTypeName+
            // " B:"+atomB.Symbol+" "+atomB.AtomTypeName
            // +" unplaced Atom:"
            // +unplacedAtom.AtomTypeName+" BL:"+length+" Angle:"+angle
            // +" FormalNeighbour:"
            // +atomA.FormalNeighbourCount+" HYB:"+atomA.getFlag
            // (CDKConstants.HYBRIDIZATION_SP2)
            // +" #Neigbhours:"+atomNeighbours.Atoms.Count);
            IAtom atomC = ap3d.GetPlacedHeavyAtom(molecule, atomB, atomA);

            Vector3[] branchPoints = atlp3d.Get3DCoordinatesForLigands(atomA, noCoords, atomNeighbours, atomC,
                    (atomA.FormalNeighbourCount.Value - atomNeighbours.Atoms.Count), length, angle);
            double distance = 0;
            int farthestPoint = 0;
            for (int i = 0; i < branchPoints.Length; i++)
            {
                if (Math.Abs(Vector3.Distance(branchPoints[i], centerPlacedMolecule)) > Math.Abs(distance))
                {
                    distance = Vector3.Distance(branchPoints[i], centerPlacedMolecule);
                    farthestPoint = i;
                }
            }

            int stereo = -1;
            IBond unplacedBond = molecule.GetBond(atomA, unplacedAtom);
            if (atomA.StereoParity != 0
                    || (unplacedBond.Stereo == BondStereo.Up || unplacedBond.Stereo == BondStereo.Down)
                    && molecule.GetMaximumBondOrder(atomA) == BondOrder.Single)
            {
                if (atomNeighbours.Atoms.Count > 1)
                {
                    stereo = AtomTetrahedralLigandPlacer3D.MakeStereocenter(atomA.Point3D.Value, molecule.GetBond(atomA, unplacedAtom),
                            (atomNeighbours.Atoms[0]).Point3D.Value, (atomNeighbours.Atoms[1]).Point3D.Value,
                            branchPoints);
                }
            }
            if (stereo != -1)
            {
                farthestPoint = stereo;
            }
            unplacedAtom.Point3D = branchPoints[farthestPoint];
            unplacedAtom.IsPlaced = true;
        }

        /// <summary>
        /// Search and place branches of a chain or ring.
        /// </summary>
        /// <param name="chain">AtomContainer if atoms in an aliphatic chain or ring system</param>
        private void SearchAndPlaceBranches(IAtomContainer molecule, IAtomContainer chain, AtomPlacer3D ap3d,
                AtomTetrahedralLigandPlacer3D atlp3d, AtomPlacer atomPlacer)
        {
            //Debug.WriteLine("****** SEARCH AND PLACE ****** Chain length: "+chain.Atoms.Count);
            IAtomContainer branchAtoms = molecule.Builder.NewAtomContainer();
            IAtomContainer connectedAtoms = molecule.Builder.NewAtomContainer();
            for (int i = 0; i < chain.Atoms.Count; i++)
            {
                var atoms = molecule.GetConnectedAtoms(chain.Atoms[i]);
                foreach (var atom in atoms)
                {
                    if (!atom.AtomicNumber.Equals(AtomicNumbers.H) & !atom.IsPlaced & !atom.IsInRing)
                    {
                        connectedAtoms.Add(ap3d.GetPlacedHeavyAtoms(molecule, chain.Atoms[i]));
                        try
                        {
                            SetBranchAtom(molecule, atom, chain.Atoms[i], connectedAtoms, ap3d, atlp3d);
                        }
                        catch (CDKException ex2)
                        {
                            Trace.TraceError($"SearchAndPlaceBranchERROR: Cannot find enough neighbour atoms due to {ex2.ToString()}");
                            throw new CDKException($"SearchAndPlaceBranchERROR: Cannot find enough neighbour atoms: {ex2.Message}", ex2);
                        }
                        branchAtoms.Atoms.Add(atom);
                        connectedAtoms.RemoveAllElements();
                    }
                }

            }//for ac.getAtomCount
            PlaceLinearChains3D(molecule, branchAtoms, ap3d, atlp3d, atomPlacer);
        }

        /// <summary>
        /// Layout all aliphatic chains with ZMatrix.
        /// </summary>
        /// <param name="startAtoms">AtomContainer of possible start atoms for a chain</param>
        private void PlaceLinearChains3D(IAtomContainer molecule, IAtomContainer startAtoms, AtomPlacer3D ap3d,
                AtomTetrahedralLigandPlacer3D atlp3d, AtomPlacer atomPlacer)
        {
            //Debug.WriteLine("****** PLACE LINEAR CHAINS ******");
            IAtom dihPlacedAtom = null;
            IAtom thirdPlacedAtom = null;
            IAtomContainer longestUnplacedChain = molecule.Builder.NewAtomContainer();
            if (startAtoms.Atoms.Count == 0)
            {
                //no branch points ->linear chain
                //Debug.WriteLine("------ LINEAR CHAIN - FINISH ------");
            }
            else
            {
                for (int i = 0; i < startAtoms.Atoms.Count; i++)
                {
                    //Debug.WriteLine("FOUND BRANCHED ALKAN");
                    //Debug.WriteLine("Atom NOT NULL:" + molecule.Atoms.IndexOf(startAtoms.GetAtomAt(i)));
                    thirdPlacedAtom = ap3d.GetPlacedHeavyAtom(molecule, startAtoms.Atoms[i]);
                    dihPlacedAtom = ap3d.GetPlacedHeavyAtom(molecule, thirdPlacedAtom, startAtoms.Atoms[i]);
                    longestUnplacedChain.Atoms.Add(dihPlacedAtom);
                    longestUnplacedChain.Atoms.Add(thirdPlacedAtom);
                    longestUnplacedChain.Atoms.Add(startAtoms.Atoms[i]);
                    longestUnplacedChain.Add(AtomPlacer.GetLongestUnplacedChain(molecule, startAtoms.Atoms[i]));
                    SetAtomsToUnVisited(molecule);

                    if (longestUnplacedChain.Atoms.Count < 4)
                    {
                        //di,third,sec
                        //Debug.WriteLine("------ Single BRANCH METHYLTYP ------");
                        //break;
                    }
                    else
                    {
                        //Debug.WriteLine("LongestUnchainLength:"+longestUnplacedChain.Atoms.Count);
                        ap3d.PlaceAliphaticHeavyChain(molecule, longestUnplacedChain);
                        ap3d.ZMatrixChainToCartesian(molecule, true);
                        SearchAndPlaceBranches(molecule, longestUnplacedChain, ap3d, atlp3d, atomPlacer);
                    }
                    longestUnplacedChain.RemoveAllElements();
                }//for

            }
            //Debug.WriteLine("****** HANDLE ALIPHATICS END ******");
        }

        /// <summary>
        /// Translates the template ring system to new coordinates.
        /// </summary>
        /// <param name="originalCoord">original coordinates of the placed ring atom from template</param>
        /// <param name="newCoord">new coordinates from branch placement</param>
        /// <param name="ac">AtomContainer contains atoms of ring system</param>
        private static void TranslateStructure(Vector3 originalCoord, Vector3 newCoord, IAtomContainer ac)
        {
            Vector3 transVector = originalCoord - newCoord;
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                if (!(ac.Atoms[i].IsPlaced))
                {
                    ac.Atoms[i].Point3D -= transVector;
                    //ac.GetAtomAt(i).IsPlaced = true;
                }
            }
        }

        /// <summary>
        /// Returns the largest (number of atoms) ring set in a molecule.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <returns>bool</returns>
        private static bool CheckAllRingAtomsHasCoordinates(IAtomContainer ac)
        {
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                if (ac.Atoms[i].Point3D != null && ac.Atoms[i].IsInRing)
                {
                }
                else if (!ac.Atoms[i].IsInRing)
                {
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Sets the atomsToPlace attribute of the ModelBuilder3D object.
        /// </summary>
        /// <param name="ac">The new atomsToPlace value</param>
        private static void SetAtomsToPlace(IAtomContainer ac)
        {
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                ac.Atoms[i].IsPlaced = true;
            }
        }

        /// <summary>
        /// Sets the atomsToUnplaced attribute of the ModelBuilder3D object.
        /// </summary>
        private static void SetAtomsToUnplaced(IAtomContainer molecule)
        {
            for (int i = 0; i < molecule.Atoms.Count; i++)
            {
                molecule.Atoms[i].IsPlaced = false;
            }
        }

        /// <summary>
        /// Sets the atomsToUnVisited attribute of the ModelBuilder3D object.
        /// </summary>
        private static void SetAtomsToUnVisited(IAtomContainer molecule)
        {
            for (int i = 0; i < molecule.Atoms.Count; i++)
            {
                molecule.Atoms[i].IsVisited = false;
            }
        }

        /// <summary>
        /// Sets the templateHandler attribute of the ModelBuilder3D object.
        /// </summary>
        /// <param name="templateHandler">The new templateHandler value</param>
        private void SetTemplateHandler(TemplateHandler3D templateHandler)
        {
            this.templateHandler = templateHandler ?? throw new ArgumentNullException(nameof(templateHandler), "The given template handler is null!");
        }

        /// <summary>
        /// Returns the number of loaded templates. Note that it may return 0 because
        /// templates are lazy loaded, that is upon the first ring being laid out.
        /// </summary>
        /// <returns>0, if not templates are loaded</returns>
        public int TemplateCount => this.templateHandler.TemplateCount;
    }
}
