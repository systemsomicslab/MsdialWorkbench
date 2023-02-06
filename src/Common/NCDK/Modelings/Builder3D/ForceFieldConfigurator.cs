/* Copyright (C) 2004-2007  Christian Hoppe <chhoppe@users.sf.net>
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

using NCDK.Aromaticities;
using NCDK.Graphs;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NCDK.Modelings.Builder3D
{
    /// <summary>
    ///  Reads in a force field configuration file, set the atom types into a vector, and the data into a hashtable
    ///  Therefore, it uses the class <see cref="MM2BasedParameterSetReader"/>.
    ///  private Dictionary parameterSet;
    ///  key=nameofdatafield+atomid1+;atomid2;atomxid
    ///
    ///  <para>MM2 and MMFF94 force field are implemented
    ///  With force field data it configures the cdk atom (assign atomtype, van der Waals radius, charge...)
    /// </para>
    /// </summary>
    // @author     chhoppe
    // @cdk.created    2004-09-07
    // @cdk.module     forcefield
    public class ForceFieldConfigurator
    {
        private string ffName = "mmff94";
        private IReadOnlyList<IAtomType> atomTypes;
        private IReadOnlyDictionary<string, object> parameterSet = null;
        private MM2BasedParameterSetReader mm2 = null;
        private MMFF94BasedParameterSetReader mmff94 = null;
        private Stream ins = null;
        private string[] fftypes = { "mm2", "mmff94" };

        public ForceFieldConfigurator() { }

        /// <summary>
        /// the inputStream attribute of the ForceFieldConfigurator object
        /// </summary>
        /// <param name="ins">The new inputStream value</param>
        public void SetInputStream(Stream ins)
        {
            this.ins = ins;
        }

        /// <summary>
        /// gives a list of possible force field types
        /// </summary>
        /// <returns>the list</returns>
        public string[] GetFfTypes()
        {
            return fftypes;
        }

        /// <summary>
        ///  Sets the forceFieldType attribute of the ForceFieldConfigurator object
        /// </summary>
        /// <param name="ffname">The new forceFieldType name</param>
        public bool CheckForceFieldType(string ffname)
        {
            bool check = false;
            for (int i = 0; i < fftypes.Length; i++)
            {
                if (fftypes[i].Equals(ffname, StringComparison.Ordinal))
                {
                    check = true;
                    break;
                }
            }
            if (!check)
            {
                //            Debug.WriteLine("FFError:checkForceFieldType> Unknown forcefield:" + ffname + "Take default:"+ffName);
                return false;
            }
            return true;
        }

        /// <summary>
        ///Constructor for the ForceFieldConfigurator object
        /// </summary>
        /// <param name="ffname">name of the force field data file</param>
        public void SetForceFieldConfigurator(string ffname)
        {
            ffname = ffname.ToLowerInvariant();
            bool check = false;

            if (ffname == ffName && parameterSet != null)
            {
            }
            else
            {
                check = this.CheckForceFieldType(ffname);
                ffName = ffname;
                if (string.Equals(ffName, "mm2", StringComparison.Ordinal))
                {
                    ins = ResourceLoader.GetAsStream("NCDK.Modelings.ForceField.Data.mm2.prm");
                    mm2 = new MM2BasedParameterSetReader();
                    mm2.SetInputStream(ins);
                    try
                    {
                        this.SetMM2Parameters();
                    }
                    catch (Exception ex1)
                    {
                        throw new CDKException("Problems with set MM2Parameters due to " + ex1.ToString(), ex1);
                    }
                }
                else if (ffName.Equals("mmff94", StringComparison.Ordinal) || !check)
                {
                    ins = ResourceLoader.GetAsStream("NCDK.Modelings.ForceField.Data.mmff94.prm");
                    mmff94 = new MMFF94BasedParameterSetReader();

                    mmff94.SetInputStream(ins);
                    try
                    {
                        this.SetMMFF94Parameters();
                    }
                    catch (Exception ex2)
                    {
                        throw new CDKException("Problems with set MM2Parameters due to" + ex2.ToString(), ex2);
                    }
                }
            }
        }

        /// <summary>
        ///  Sets the atomTypes attribute of the ForceFieldConfigurator object
        /// </summary>
        /// <param name="atomtypes">The new atomTypes</param>
        public void SetAtomTypes(List<IAtomType> atomtypes)
        {
            atomTypes = atomtypes;
        }

        /// <summary>
        ///  Sets the parameters attribute of the ForceFieldConfigurator object
        /// </summary>
        /// <param name="parameterset">The new parameter values</param>
        public void SetParameters(IReadOnlyDictionary<string, object> parameterset)
        {
            parameterSet = parameterset;
        }

        /// <summary>Sets the parameters attribute of the ForceFieldConfigurator object, default is mm2 force field</summary>
        public void SetMM2Parameters()
        {
            try
            {
                if (mm2 == null)
                    mm2 = new MM2BasedParameterSetReader();
                mm2.ReadParameterSets();
            }
            catch (Exception ex1)
            {
                throw new CDKException("Problem within readParameterSets due to:" + ex1.ToString(), ex1);
            }
            parameterSet = mm2.GetParamterSet();
            atomTypes = mm2.AtomTypes;
        }

        public void SetMMFF94Parameters()
        {
            if (mmff94 == null)
                mmff94 = new MMFF94BasedParameterSetReader();
            mmff94.ReadParameterSets();
            parameterSet = mmff94.GetParamterSet();
            atomTypes = mmff94.AtomTypes;
        }

        /// <summary>
        ///  Gets the atomTypes attribute of the ForceFieldConfigurator object
        /// </summary>
        /// <returns>The atomTypes vector</returns>
        public IReadOnlyList<IAtomType> AtomTypes => atomTypes;

        /// <summary>
        ///  Gets the parameterSet attribute of the ForceFieldConfigurator object
        /// </summary>
        /// <returns>The parameterSet hashtable</returns>
        public IReadOnlyDictionary<string, object> GetParameterSet()
        {
            return this.parameterSet;
        }

        /// <summary>
        ///  Find the atomType for a id
        /// </summary>
        /// <param name="id">Atomtype id of the forcefield</param>
        /// <returns>The atomType</returns>
        /// <exception cref="NoSuchAtomTypeException"> atomType is not known.</exception>
        private IAtomType GetAtomType(string id)
        {
            IAtomType at = null;
            for (int i = 0; i < atomTypes.Count; i++)
            {
                at = (IAtomType)atomTypes[i];
                if (at.AtomTypeName.Equals(id, StringComparison.Ordinal))
                {
                    return at;
                }
            }
            throw new NoSuchAtomTypeException($"AtomType {id} could not be found");
        }

        /// <summary>
        ///  Method assigns atom types to atoms (calculates sssr and aromaticity)
        /// </summary>
        /// <returns>sssrf set</returns>
        /// <exception cref="CDKException"> Problems detecting aromaticity or making hose codes.</exception>
        public IRingSet AssignAtomTyps(IAtomContainer molecule)
        {
            IAtom atom = null;
            string hoseCode = "";
            HOSECodeGenerator hcg = new HOSECodeGenerator();
            int NumberOfRingAtoms = 0;
            IRingSet ringSetMolecule = Cycles.FindSSSR(molecule).ToRingSet();
            bool isInHeteroRing = false;
            try
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(molecule);
                Aromaticity.CDKLegacy.Apply(molecule);
            }
            catch (Exception cdk1)
            {
                throw new CDKException("AROMATICITYError: Cannot determine aromaticity due to: " + cdk1.Message, cdk1);
            }

            for (int i = 0; i < molecule.Atoms.Count; i++)
            {
                atom = molecule.Atoms[i];
                if (ringSetMolecule.Contains(atom))
                {
                    NumberOfRingAtoms = NumberOfRingAtoms + 1;
                    atom.IsInRing = true;
                    atom.IsAliphatic = false;
                    var ringSetA = ringSetMolecule.GetRings(atom).ToList();
                    RingSetManipulator.Sort(ringSetA);
                    IRing sring = (IRing)ringSetA[ringSetA.Count - 1];
                    atom.SetProperty("RING_SIZE", sring.RingSize);
                    foreach (var ring in ringSetA)
                    {
                        if (IsHeteroRingSystem(ring))
                            break;
                    }
                }
                else
                {
                    atom.IsAliphatic = true;
                    atom.IsInRing = false;
                    isInHeteroRing = false;
                }
                atom.SetProperty("MAX_BOND_ORDER", molecule.GetMaximumBondOrder(atom).Numeric());

                try
                {
                    hoseCode = hcg.GetHOSECode(molecule, atom, 3);
                    //Debug.WriteLine("HOSECODE GENERATION: ATOM "+i+" HoseCode: "+hoseCode+" ");
                }
                catch (CDKException ex1)
                {
                    Console.Out.WriteLine("Could not build HOSECode from atom " + i + " due to " + ex1.ToString());
                    throw new CDKException("Could not build HOSECode from atom " + i + " due to " + ex1.ToString(), ex1);
                }
                try
                {
                    ConfigureAtom(atom, hoseCode, isInHeteroRing);
                }
                catch (CDKException ex2)
                {
                    Console.Out.WriteLine("Could not final configure atom " + i + " due to " + ex2.ToString());
                    throw new CDKException("Could not final configure atom due to problems with force field", ex2);
                }
            }

            //        IBond[] bond = molecule.Bonds;
            string bondType;
            foreach (var bond in molecule.Bonds)
            {
                //Debug.WriteLine("bond[" + i + "] properties : " + molecule.Bonds[i].GetProperties());
                bondType = "0";
                if (bond.Order == BondOrder.Single)
                {
                    if ((bond.Begin.AtomTypeName.Equals("Csp2", StringComparison.Ordinal))
                        && ((bond.End.AtomTypeName.Equals("Csp2", StringComparison.Ordinal))
                        || (bond.End.AtomTypeName.Equals("C=", StringComparison.Ordinal))))
                    {
                        bondType = "1";
                    }

                    if ((bond.Begin.AtomTypeName.Equals("C=", StringComparison.Ordinal))
                        && ((bond.End.AtomTypeName.Equals("Csp2", StringComparison.Ordinal))
                        || (bond.End.AtomTypeName.Equals("C=", StringComparison.Ordinal))))
                    {
                        bondType = "1";
                    }

                    if ((bond.Begin.AtomTypeName.Equals("Csp", StringComparison.Ordinal))
                        && (bond.End.AtomTypeName.Equals("Csp", StringComparison.Ordinal)))
                    {
                        bondType = "1";
                    }
                }
                //            molecule.Bonds[i].SetProperty("MMFF94 bond type", bondType);
                bond.SetProperty("MMFF94 bond type", bondType);
                //Debug.WriteLine("bond[" + i + "] properties : " + molecule.Bonds[i].GetProperties());
            }

            return ringSetMolecule;
        }

        /// <summary>
        ///  Returns true if atom is in hetero ring system
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <returns>true/false</returns>
        private static bool IsHeteroRingSystem(IAtomContainer ac)
        {
            if (ac != null)
            {
                for (int i = 0; i < ac.Atoms.Count; i++)
                {
                    switch (ac.Atoms[i].AtomicNumber)
                    {
                        case AtomicNumbers.H:
                        case AtomicNumbers.C:
                            break;
                        default:
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///  Assigns an atom type to an atom
        /// </summary>
        /// <param name="atom">The atom to be aasigned</param>
        /// <param name="ID">the atom type id</param>
        /// <exception cref="NoSuchAtomTypeException"> atomType is not known</exception>
        /// <returns>the assigned atom</returns>
        private IAtom SetAtom(IAtom atom, string ID)
        {
            IAtomType at = GetAtomType(ID);
            if (atom.Symbol == null)
            {
                atom.Symbol = at.Symbol;
            }
            atom.AtomTypeName = at.AtomTypeName;
            atom.FormalNeighbourCount = at.FormalNeighbourCount;
            string key = "vdw" + ID;
            var data = (System.Collections.IList)parameterSet[key];
            double value = (double)data[0];
            key = "charge" + ID;
            if (parameterSet.ContainsKey(key))
            {
                data = (System.Collections.IList)parameterSet[key];
                value = (double)data[0];
                atom.Charge = value;
            }
            var color = at.GetProperty<object>(CDKPropertyName.Color);
            if (color != null)
            {
                atom.SetProperty(CDKPropertyName.Color, color);
            }
            if (at.AtomicNumber != 0)
            {
                atom.AtomicNumber = at.AtomicNumber;
            }
            if (at.ExactMass > 0.0)
            {
                atom.ExactMass = at.ExactMass;
            }
            return atom;
        }

        public IAtom ConfigureAtom(IAtom atom, string hoseCode, bool flag)
        {
            if (string.Equals(ffName, "mm2", StringComparison.Ordinal))
            {
                return ConfigureMM2BasedAtom(atom, hoseCode, flag);
            }
            else if (string.Equals(ffName, "mmff94", StringComparison.Ordinal))
            {
                return ConfigureMMFF94BasedAtom(atom, hoseCode, flag);
            }
            return atom;
        }

        /// <summary>
        /// Configures an atom to a mm2 based atom type
        /// </summary>
        /// <param name="atom">atom to be configured</param>
        /// <param name="hoseCode">the 4 sphere hose code of the atom</param>
        /// <returns>atom</returns>
        /// <exception cref="NoSuchAtomTypeException"> atomType is not known</exception>
        public IAtom ConfigureMM2BasedAtom(IAtom atom, string hoseCode, bool hetRing)
        {
            //Debug.WriteLine("CONFIGURE MM2 ATOM");
            MM2BasedAtomTypePattern atp = new MM2BasedAtomTypePattern();
            var atomTypePattern = atp.AtomTypePatterns;
            string ID = "";
            bool atomTypeFlag = false;

            if (atom is IPseudoAtom)
            {
                return atom;
            }

            hoseCode = RemoveAromaticityFlagsFromHoseCode(hoseCode);

            string[] ids = {"C", "Csp2", "C=", "Csp", "HC", "O", "O=", "N", "Nsp2", "Nsp", "F", "CL", "BR", "I", "S", "S+",
                ">SN", "SO2", "SI", "LP", "HO", "CR3R", "HN", "HOCO", "P", "B", "BTET", "HN2", "C.", "C+", "GE", "SN",
                "PB", "SE", "TE", "D", "NPYD", "CE3R", "N+", "NPYL", "Oar", "Sthi", "N2OX", "HS", "=N=", "NO3", "OM",
                "HN+", "OR", "Car", "HE", "NE", "AR", "KR", "XE", "", "", "", "MG", "PTET", "FE", "FE", "NI", "NI",
                "CO", "CO", "", "", "OX", "OK", "C++", "N=C", "NPD+", "N+=", "N2OX"};

            for (int j = 0; j < atomTypePattern.Count; j++)
            {
                Regex p = atomTypePattern[j];
                var mat = p.Match(hoseCode);
                if (mat.Success)
                {
                    ID = ids[j];
                    //CHECK Rings 1,2,8,9? Thiole 44? AZO 9? Radical - ? Amid 23/enol 21?
                    if (j == 0)
                    {
                        //csp3
                        if (atom.IsInRing)
                        {
                            if (atom.GetProperty<int>("RING_SIZE").Equals(3))
                            {
                                ID = ids[21];
                            }
                            else if (atom.GetProperty<int>("RING_SIZE").Equals(6)
                                  && atom.IsAromatic)
                            {
                                ID = ids[1];
                            }
                            else if (atom.IsAromatic)
                            {
                                ID = ids[1];
                            }
                        }
                    }
                    else if (j == 1)
                    {
                        //csp2
                        if (atom.IsInRing)
                        {
                            if (atom.GetProperty<int>("RING_SIZE").Equals(6)
                                    && atom.IsAromatic)
                            {
                            }
                            else if (atom.GetProperty<int>("RING_SIZE").Equals(3))
                            {
                                ID = ids[37];
                            }
                            else
                            {
                                ID = ids[1];
                            }
                        }
                        p = atomTypePattern[2];
                        //COOH
                        mat = p.Match(hoseCode);
                        if (mat.Success && !atom.IsInRing)
                        {
                            ID = ids[2];
                        }

                    }
                    else if (j == 5)
                    {
                        //OH/Ether
                        if (atom.IsInRing)
                        {
                            if (atom.GetProperty<int>("RING_SIZE").Equals(3))
                            {
                                ID = ids[48];
                                //EPOXY
                            }
                            else if (atom.GetProperty<int>("RING_SIZE").Equals(5)
                                  && atom.IsAromatic)
                            {
                                ID = ids[40];
                            }
                            else
                            {
                                ID = ids[5];
                            }
                        }
                    }
                    else if (j == 7)
                    {
                        //n sp3
                        if (atom.IsInRing && atom.IsAromatic)
                        {
                            if (atom.GetProperty<int>("RING_SIZE").Equals(5))
                            {
                                ID = ids[39];
                            }
                        }
                        //Amid
                        p = atomTypePattern[77];
                        mat = p.Match(hoseCode);
                        if (mat.Success && !atom.IsInRing)
                        {
                            ID = ids[8];
                        }
                    }
                    else if (j == 8)
                    {
                        //nsp2
                        if (atom.IsInRing)
                        {
                            if (atom.GetProperty<int>("RING_SIZE").Equals(6))
                            {
                                ID = ids[36];
                            }
                        }
                        p = atomTypePattern[36];
                        //AZO
                        mat = p.Match(hoseCode);
                        if (mat.Success && !atom.IsInRing)
                        {
                            ID = ids[36];
                        }

                    }
                    else if (j == 43)
                    {
                        //h thiol
                        var d_tmp = atom.GetProperty<double>("MAX_BOND_ORDER");
                        if (d_tmp > 1)
                        {
                            ID = ids[4];
                        }
                    }
                    else if (j == 20)
                    {
                        //h alcohol,ether
                        p = atomTypePattern[76];
                        //Enol
                        mat = p.Match(hoseCode);
                        if (mat.Success && !atom.IsInRing)
                        {
                            ID = ids[27];
                        }
                        p = atomTypePattern[23];
                        //COOH
                        mat = p.Match(hoseCode);
                        if (mat.Success && !atom.IsInRing)
                        {
                            ID = ids[23];
                        }
                    }
                    else if (j == 22)
                    {
                        p = atomTypePattern[75];
                        //Amid
                        mat = p.Match(hoseCode);
                        if (mat.Success)
                        {
                            ID = ids[27];
                        }
                    }

                    atomTypeFlag = true;
                    //Debug.WriteLine("Atom Symbol:" + atom.Symbol + " MATCH AtomType> " + ID + " HoseCode>" + hoseCode + " ");
                    break;
                }//IF
            }//for end
            if (atomTypeFlag)
            {
                atomTypeFlag = false;
                return SetAtom(atom, ID);
            }
            else
            {
                throw new NoSuchAtomTypeException("Atom is unkown: Symbol:" + atom.Symbol
                        + " does not MATCH AtomType. HoseCode:" + hoseCode);
            }
        }

        public static string RemoveAromaticityFlagsFromHoseCode(string hoseCode)
        {
            string hosecode = "";
            for (int i = 0; i < hoseCode.Length; i++)
            {
                if (hoseCode[i] != '*')
                {
                    hosecode = hosecode + hoseCode[i];
                }
            }
            return hosecode;
        }

        /// <summary>
        ///  Configures an atom to a mmff94 based atom type
        /// </summary>
        /// <param name="atom">atom to be configured</param>
        /// <param name="hoseCode">the 4 sphere hose code of the atom</param>
        /// <returns>atom</returns>
        /// <exception cref="NoSuchAtomTypeException"> atomType is not known</exception>
        public IAtom ConfigureMMFF94BasedAtom(IAtom atom, string hoseCode, bool isInHetRing)
        {
            //Debug.WriteLine("****** Configure MMFF94 AtomType ******");
            MMFF94BasedAtomTypePattern atp = new MMFF94BasedAtomTypePattern();
            var atomTypePattern = atp.AtomTypePatterns;
            bool atomTypeFlag = false;
            string ID = "";
            hoseCode = RemoveAromaticityFlagsFromHoseCode(hoseCode);

            string[] ids = {"C", "Csp2", "C=", "Csp", "CO2M", "CNN+", "C%", "CIM+", "CR4R", "CR3R", "CE4R", "Car", "C5A",
                "C5B", "C5", "HC", "HO", "HN", "HOCO", "HN=C", "HN2", "HOCC", "HOH", "HOS", "HN+", "HO+", "HO=+", "HP",
                "O", "O=", "OX", "OM", "O+", "O=+", "OH2", "Oar", "N", "N=C", "NC=C", "NSP", "=N=", "NAZT", "N+",
                "N2OX", "N3OX", "NC#N", "NO3", "N=O", "NC=O", "NSO", "N+=", "NCN+", "NGD+", "NR%", "NM", "N5M", "NPYD",
                "NPYL", "NPD+", "N5A", "N5B", "NPOX", "N5OX", "N5+", "N5", "S", "S=C", ">SN", "SO2", "SX", "SO2M",
                "=SO", "Sthi", "PTET", "P", "-P=C", "F", "CL", "BR", "I", "SI", "CL04", "FE+2", "FE+3", "F-", "CL-",
                "BR-", "LI+", "NA+", "K+", "ZN+2", "CA+2", "CU+1", "CU+2", "MG+2", "Du"};

            if (atom is IPseudoAtom)
            {
                return atom;
            }

            for (int j = 0; j < atomTypePattern.Count; j++)
            {
                var p = atomTypePattern[j];
                var mat = p.Match(hoseCode);
                if (mat.Success)
                {
                    ID = ids[j];
                    if (j == 0)
                    {//csp3
                        if (atom.IsInRing)
                        {
                            p = atomTypePattern[13];//c beta heteroaromatic ring
                            mat = p.Match(hoseCode);
                            var p2 = atomTypePattern[12];//c alpha heteroaromatic ring
                            var mat2 = p2.Match(hoseCode);
                            if (mat.Success && isInHetRing && atom.IsAromatic
                                    && atom.GetProperty<int>("RING_SIZE").Equals(5))
                            {
                                ID = ids[13];
                            }
                            else if (mat2.Success && isInHetRing && atom.IsAromatic
                                  && atom.GetProperty<int>("RING_SIZE").Equals(5))
                            {
                                ID = ids[12];
                            }
                            else if (atom.GetProperty<int>("RING_SIZE").Equals(3)
                                  && !atom.IsAromatic)
                            {
                                ID = ids[9];//sp3 3mem rings
                            }
                            else if (atom.GetProperty<int>("RING_SIZE").Equals(4)
                                  && !atom.IsAromatic)
                            {
                                ID = ids[8];//sp3 4mem rings
                            }
                            else if (atom.IsAromatic && isInHetRing
                                  && atom.GetProperty<int>("RING_SIZE").Equals(5))
                            {
                                ID = ids[14];//C in het 5 ring
                            }
                            else if (atom.IsAromatic)
                            {
                                ID = ids[11];//C in benzene, pyroll
                            }
                        }
                        else
                        {
                            p = atomTypePattern[66];//S=C
                            mat = p.Match(hoseCode);
                            if (mat.Success)
                            {
                                ID = ids[66];
                            }
                        }
                    }
                    else if (j == 1)
                    {//csp2
                        if (atom.IsInRing)
                        {
                            if (atom.GetProperty<int>("RING_SIZE").Equals(4)
                                    && !atom.IsAromatic && !isInHetRing)
                            {
                                ID = ids[29];//C= in 4 ring
                            }
                        }

                    }
                    else if (j == 2)
                    {//csp2 C=Hetatom
                        if (atom.IsInRing && isInHetRing && atom.IsAromatic)
                        {
                            ID = ids[12];
                        }

                    }
                    else if (j == 36)
                    {//n sp3
                     //Amid
                        p = atomTypePattern[48];
                        mat = p.Match(hoseCode);
                        if (mat.Success && !atom.IsInRing)
                        {
                            ID = ids[48];
                        }

                        p = atomTypePattern[44];//sp3 n-oxide
                        mat = p.Match(hoseCode);
                        if (mat.Success)
                        {
                            ID = ids[44];
                        }

                        p = atomTypePattern[56];//npyd
                        mat = p.Match(hoseCode);

                        if (atom.IsAromatic)
                        {//id in pyridin, pyrol etc...                        if (mat.Success && atom.IsAromatic && atom.GetProperty<int>("RING_SIZE").Equals(5)){
                            if (atom.GetProperty<int>("RING_SIZE").Equals(6) && mat.Success)
                            {
                                ID = ids[56];
                            }
                            else if (atom.GetProperty<int>("RING_SIZE").Equals(5) && mat.Success)
                            {
                                ID = ids[57];
                            }
                            else
                            {
                                ID = ids[64];
                            }
                        }

                        p = atomTypePattern[61];//npyd
                        mat = p.Match(hoseCode);
                        if (atom.IsAromatic)
                        {//id in pyridin, pyrol etc...                        if (mat.Success && atom.IsAromatic && atom.GetProperty<int>("RING_SIZE").Equals(5)){
                            if (atom.GetProperty<int>("RING_SIZE").Equals(6) && mat.Success)
                            {
                                ID = ids[61];
                            }
                            else if (atom.GetProperty<int>("RING_SIZE").Equals(5) && mat.Success)
                            {
                                ID = ids[62];
                            }
                            else
                            {
                                ID = ids[43];
                            }
                        }

                        p = atomTypePattern[45];//NC#N
                        mat = p.Match(hoseCode);
                        if (mat.Success)
                        {
                            ID = ids[45];
                        }

                    }
                    else if (j == 37)
                    {//N=C n in imine
                        p = atomTypePattern[59];//n beta heteroaromatic ring
                        mat = p.Match(hoseCode);
                        if (atom.IsInRing)
                        {
                            if (mat.Success && isInHetRing && atom.IsAromatic
                                    && atom.GetProperty<int>("RING_SIZE").Equals(5))
                            {
                                ID = ids[59];
                            }
                            else if (atom.IsAromatic
                                  && atom.GetProperty<int>("RING_SIZE").Equals(6))
                            {
                                ID = ids[56];
                            }
                            else if (atom.IsAromatic
                                  && atom.GetProperty<int>("RING_SIZE").Equals(5))
                            {
                                ID = ids[57];
                            }
                        }

                        p = atomTypePattern[43];//N2OX
                        mat = p.Match(hoseCode);
                        if (mat.Success)
                        {
                            if (atom.IsAromatic
                                    && atom.GetProperty<int>("RING_SIZE").Equals(6))
                            {
                                ID = ids[61];//npox
                            }
                            else if (mat.Success && atom.IsAromatic
                                  && atom.GetProperty<int>("RING_SIZE").Equals(5))
                            {
                                ID = ids[62];//n5ox
                            }
                            else
                            {
                                ID = ids[43];
                            }
                        }

                    }
                    else if (j == 43)
                    {//sp2 n oxide
                        if (atom.IsInRing && atom.IsAromatic
                                && atom.GetProperty<int>("RING_SIZE").Equals(5))
                        {
                            ID = ids[62];
                        }
                        else if (atom.IsInRing && atom.IsAromatic
                              && atom.GetProperty<int>("RING_SIZE").Equals(6))
                        {
                            ID = ids[61];
                        }
                    }
                    else if (j == 40 || j == 41)
                    {//n in c=n=n or terminal n in azido
                        if (atom.IsInRing && atom.IsAromatic
                                && atom.GetProperty<int>("RING_SIZE").Equals(5))
                        {
                            ID = ids[59];//aromatic N 5R alpha
                        }
                    }
                    else if (j == 50)
                    {//n+=
                        if (atom.IsInRing && atom.IsAromatic
                                && atom.GetProperty<int>("RING_SIZE").Equals(5))
                        {
                            ID = ids[63];//n5+
                        }
                        else if (atom.IsInRing && atom.IsAromatic
                              && atom.GetProperty<int>("RING_SIZE").Equals(6))
                        {
                            ID = ids[58];//npd+
                        }
                    }
                    else if (j == 28)
                    {//O ->furan
                        if (atom.IsInRing && atom.IsAromatic
                                && atom.GetProperty<int>("RING_SIZE").Equals(5))
                        {
                            ID = ids[35];
                        }
                    }
                    else if (j == 16)
                    {//H-object-> enol
                        p = atomTypePattern[21];//enol
                        mat = p.Match(hoseCode);
                        if (mat.Success)
                        {
                            ID = ids[21];
                        }
                        p = atomTypePattern[18];//enol
                        mat = p.Match(hoseCode);
                        if (mat.Success)
                        {
                            ID = ids[18];
                        }

                    }
                    else if (j == 74)
                    {//P
                        p = atomTypePattern[75];//-P=C
                        mat = p.Match(hoseCode);
                        if (mat.Success)
                        {
                            ID = ids[75];
                        }
                    }

                    atomTypeFlag = true;
                    //Debug.WriteLine("Atom Symbol:" + atom.Symbol + " MATCH AtomType> " + ID + " HoseCode>" + hoseCode + " ");
                    break;
                }//IF
            }//for end
            if (atomTypeFlag)
            {
                atomTypeFlag = false;
                return SetAtom(atom, ID);
            }
            else
            {
                throw new NoSuchAtomTypeException("Atom is unkown: Symbol:" + atom.Symbol
                        + " does not MATCH AtomType. HoseCode:" + hoseCode);
            }
        }
    }
}
