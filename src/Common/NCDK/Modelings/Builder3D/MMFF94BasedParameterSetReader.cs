/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Common.Primitives;
using NCDK.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NCDK.Modelings.Builder3D
{
    /// <summary>
    /// AtomType list configurator that uses the ParameterSet originally defined in
    /// mmff94.prm from moe. This class was added to be able to port mmff94 to CDK.
    /// </summary>
    // @author chhoppe
    // @cdk.created 2004-09-07
    // @cdk.module forcefield
    // @cdk.keyword atom type, mmff94
    public class MMFF94BasedParameterSetReader
    {
        private static readonly IChemObjectBuilder builder = Silent.ChemObjectBuilder.Instance;

        private const string configFile = "NCDK.Modelings.ForceField.Data.mmff94.prm";
        private Stream ins = null;
        private Dictionary<string, object> parameterSet;
        private List<IAtomType> atomTypes;
        private IEnumerator<string> st;
        private string sid;

        private const string configFilevdW = "NCDK.Modelings.ForceField.Data.mmffvdw.prm";
        private Stream insvdW = null;
        private IEnumerator<string> stvdW;
        private string sidvdW;

        private const string configFileDFSB = "NCDK.Modelings.ForceField.Data.mmffdfsb.par";
        private Stream insDFSB;
        private IEnumerator<string> stDFSB;

        /// <summary>
        /// Constructor for the MM2BasedParameterSetReader object
        /// </summary>
        public MMFF94BasedParameterSetReader()
        {
            parameterSet = new Dictionary<string, object>();
            atomTypes = new List<IAtomType>();
        }

        public IReadOnlyDictionary<string, object> GetParamterSet()
        {
            return parameterSet;
        }

        public IReadOnlyList<IAtomType> AtomTypes => atomTypes;

        /// <summary>
        /// Sets the file containing the config data
        /// </summary>
        /// <param name="ins">The new inputStream type Stream</param>
        public void SetInputStream(Stream ins)
        {
            this.ins = ins;
        }

        /// <summary>
        /// Read a text based configuration file out of the force field mm2 file
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetAtomTypeData()
        {
            string sradius;
            string sq0;
            {
                var key = "data" + sid;
                var data = new List<object>();

                sradius = st.Current; st.MoveNext();
                string swell = st.Current; st.MoveNext();
                string sapol = st.Current; st.MoveNext();
                string sNeff = st.Current; st.MoveNext();
                string sDA = st.Current; st.MoveNext();
                sq0 = st.Current; st.MoveNext();
                string spbci = st.Current; st.MoveNext();
                string sfcadj = st.Current; st.MoveNext();

                stvdW.MoveNext();
                stvdW.MoveNext();
                string sA = stvdW.Current; stvdW.MoveNext();
                string sG = stvdW.Current; stvdW.MoveNext();

                try
                {
                    double well = double.Parse(swell, NumberFormatInfo.InvariantInfo);
                    double apol = double.Parse(sapol, NumberFormatInfo.InvariantInfo);
                    double Neff = double.Parse(sNeff, NumberFormatInfo.InvariantInfo);
                    double fcadj = double.Parse(sfcadj, NumberFormatInfo.InvariantInfo);
                    //double pbci = double.Parse(spbci, NumberFormatInfo.InvariantInfo);
                    double a = double.Parse(sA, NumberFormatInfo.InvariantInfo);
                    double g = double.Parse(sG, NumberFormatInfo.InvariantInfo);

                    data.Add(well);
                    data.Add(apol);
                    data.Add(Neff);
                    data.Add(sDA);
                    data.Add(fcadj);
                    data.Add(spbci);
                    data.Add(a);
                    data.Add(g);
                }
                catch (FormatException nfe)
                {
                    throw new IOException("Data: Malformed Number due to:" + nfe);
                }

                Debug.WriteLine($"data : well,apol,Neff,sDA,fcadj,spbci,a,g {data}");
                parameterSet[key] = data;
            }

            {
                var key = "vdw" + sid;
                var data = new List<double>();
                try
                {
                    double radius = double.Parse(sradius, NumberFormatInfo.InvariantInfo);
                    data.Add(radius);
                }
                catch (FormatException nfe2)
                {
                    Debug.WriteLine($"vdwError: Malformed Number due to:{nfe2}");
                }
                parameterSet[key] = data;

                key = "charge" + sid;
                data = new List<double>();
                try
                {
                    double q0 = double.Parse(sq0, NumberFormatInfo.InvariantInfo);
                    data.Add(q0);
                }
                catch (FormatException nfe3)
                {
                    Console.Error.WriteLine($"Charge: Malformed Number due to: {nfe3}");
                }
                parameterSet[key] = data;
            }
        }

        /// <summary>
        /// Read and stores the atom types in a vector
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetAtomTypes()
        {
            string name = "";
            string rootType = "";
            //int an = 0;
            int rl = 255;
            int gl = 20;
            int bl = 147;
            int maxbond = 0;
            int atomNr = 0;

            double mass = 0.0;
            st.MoveNext();
            string sid = st.Current; st.MoveNext();
            rootType = st.Current; st.MoveNext();
            string smaxbond = st.Current; st.MoveNext();
            string satomNr = st.Current; st.MoveNext();
            string smass = st.Current; st.MoveNext();
            name = st.Current; st.MoveNext();

            try
            {
                maxbond = int.Parse(smaxbond, NumberFormatInfo.InvariantInfo);
                mass = double.Parse(smass, NumberFormatInfo.InvariantInfo);
                atomNr = int.Parse(satomNr, NumberFormatInfo.InvariantInfo);

            }
            catch (FormatException)
            {
                throw new IOException("AtomTypeTable.ReadAtypes: " + "Malformed Number");
            }

            IAtomType atomType = builder.NewAtomType(name, rootType);
            atomType.AtomicNumber = atomNr;
            atomType.ExactMass = mass;
            atomType.MassNumber = MassNumber(atomNr, mass);
            atomType.FormalNeighbourCount = maxbond;
            atomType.Symbol = rootType;
            var co = CDKPropertyName.RGB2Int(rl, gl, bl);
            atomType.SetProperty(CDKPropertyName.Color, co);
            atomType.AtomTypeName = sid;
            atomTypes.Add(atomType);
        }

        /// <summary>
        /// Sets the bond attribute stored into the parameter set
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetBond()
        {
            var data = new List<double>();
            st.MoveNext();
            string scode = st.Current; st.MoveNext();
            string sid1 = st.Current; st.MoveNext();
            string sid2 = st.Current; st.MoveNext();
            string slen = st.Current; st.MoveNext();
            string sk2 = st.Current; st.MoveNext();
            string sk3 = st.Current; st.MoveNext();
            string sk4 = st.Current; st.MoveNext();
            string sbci = st.Current; st.MoveNext();

            try
            {
                double len = double.Parse(slen, NumberFormatInfo.InvariantInfo);
                double k2 = double.Parse(sk2, NumberFormatInfo.InvariantInfo);
                double k3 = double.Parse(sk3, NumberFormatInfo.InvariantInfo);
                double k4 = double.Parse(sk4, NumberFormatInfo.InvariantInfo);
                double bci = double.Parse(sbci, NumberFormatInfo.InvariantInfo);
                data.Add(len);
                data.Add(k2);
                data.Add(k3);
                data.Add(k4);
                data.Add(bci);

            }
            catch (FormatException nfe)
            {
                throw new IOException("setBond: Malformed Number due to:" + nfe);
            }
            //        key = "bond" + scode + ";" + sid1 + ";" + sid2;
            var key = "bond" + sid1 + ";" + sid2;
            parameterSet[key] = data;
        }

        /// <summary>
        /// Sets the angle attribute stored into the parameter set
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetAngle()
        {
            IList<double> data = new List<double>();
            st.MoveNext();
            string scode = st.Current; st.MoveNext(); // string scode
            string sid1 = st.Current; st.MoveNext();
            string sid2 = st.Current; st.MoveNext();
            string sid3 = st.Current; st.MoveNext();
            string value1 = st.Current; st.MoveNext();
            string value2 = st.Current; st.MoveNext();
            string value3 = st.Current; st.MoveNext();
            string value4 = st.Current; st.MoveNext();

            try
            {
                //int code=new Integer(scode).Value;
                double va1 = double.Parse(value1, NumberFormatInfo.InvariantInfo);
                double va2 = double.Parse(value2, NumberFormatInfo.InvariantInfo);
                double va3 = double.Parse(value3, NumberFormatInfo.InvariantInfo);
                double va4 = double.Parse(value4, NumberFormatInfo.InvariantInfo);
                data.Add(va1);
                data.Add(va2);
                data.Add(va3);
                data.Add(va4);

                //            key = "angle" + scode + ";" + sid1 + ";" + sid2 + ";" + sid3;
                var key = "angle" + sid1 + ";" + sid2 + ";" + sid3;
                if (parameterSet.ContainsKey(key))
                {
                    data = (IList<double>)parameterSet[key];
                    data.Add(va1);
                    data.Add(va2);
                    data.Add(va3);
                    data.Add(va4);
                }
                parameterSet[key] = data;
            }
            catch (FormatException nfe)
            {
                throw new IOException("setAngle: Malformed Number due to:" + nfe);
            }
        }

        /// <summary>
        /// Sets the strBnd attribute stored into the parameter set
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetStrBnd()
        {
            IList<double> data = new List<double>();
            st.MoveNext();
            string scode = st.Current; st.MoveNext(); // string scode
            string sid1 = st.Current; st.MoveNext();
            string sid2 = st.Current; st.MoveNext();
            string sid3 = st.Current; st.MoveNext();
            string value1 = st.Current; st.MoveNext();
            string value2 = st.Current; st.MoveNext();

            try
            {
                //int code=new Integer(scode).Value;
                double va1 = double.Parse(value1, NumberFormatInfo.InvariantInfo);
                double va2 = double.Parse(value2, NumberFormatInfo.InvariantInfo);
                data.Add(va1);
                data.Add(va2);

            }
            catch (FormatException nfe)
            {
                throw new IOException("setStrBnd: Malformed Number due to:" + nfe);
            }
            var key = "strbnd" + scode + ";" + sid1 + ";" + sid2 + ";" + sid3;
            Debug.WriteLine($"key ={key}");
            parameterSet[key] = data;
        }

        /// <summary>
        /// Sets the torsion attribute stored into the parameter set
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetTorsion()
        {
            IList<double> data = null;
            st.MoveNext();
            string scode = st.Current; st.MoveNext(); // string scode
            string sid1 = st.Current; st.MoveNext();
            string sid2 = st.Current; st.MoveNext();
            string sid3 = st.Current; st.MoveNext();
            string sid4 = st.Current; st.MoveNext();
            string value1 = st.Current; st.MoveNext();
            string value2 = st.Current; st.MoveNext();
            string value3 = st.Current; st.MoveNext();
            string value4 = st.Current; st.MoveNext();
            string value5 = st.Current; st.MoveNext();

            try
            {
                double va1 = double.Parse(value1, NumberFormatInfo.InvariantInfo);
                double va2 = double.Parse(value2, NumberFormatInfo.InvariantInfo);
                double va3 = double.Parse(value3, NumberFormatInfo.InvariantInfo);
                double va4 = double.Parse(value4, NumberFormatInfo.InvariantInfo);
                double va5 = double.Parse(value5, NumberFormatInfo.InvariantInfo);

                var key = "torsion" + scode + ";" + sid1 + ";" + sid2 + ";" + sid3 + ";" + sid4;
                Debug.WriteLine($"key = {key}");
                if (parameterSet.ContainsKey(key))
                {
                    data = (IList<double>)parameterSet[key];
                    data.Add(va1);
                    data.Add(va2);
                    data.Add(va3);
                    data.Add(va4);
                    data.Add(va5);
                    Debug.WriteLine($"data = {data}");
                }
                else
                {
                    data = new List<double>
                    {
                        va1,
                        va2,
                        va3,
                        va4,
                        va5
                    };
                    Debug.WriteLine($"data = {data}");
                }
                parameterSet[key] = data;
            }
            catch (FormatException nfe)
            {
                throw new IOException("setTorsion: Malformed Number due to:" + nfe);
            }
        }

        /// <summary>
        /// Sets the opBend attribute stored into the parameter set
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetOpBend()
        {
            IList<double> data = new List<double>();
            st.MoveNext();
            string sid1 = st.Current; st.MoveNext();
            string sid2 = st.Current; st.MoveNext();
            string sid3 = st.Current; st.MoveNext();
            string sid4 = st.Current; st.MoveNext();
            string value1 = st.Current; st.MoveNext();

            try
            {
                double va1 = double.Parse(value1, NumberFormatInfo.InvariantInfo);
                data.Add(va1);
                var key = "opbend" + sid1 + ";" + sid2 + ";" + sid3 + ";" + sid4;
                if (parameterSet.ContainsKey(key))
                {
                    data = (IList<double>)parameterSet[key];
                    data.Add(va1);
                }
                parameterSet[key] = data;
            }
            catch (FormatException nfe)
            {
                throw new IOException("setOpBend: Malformed Number due to:" + nfe);
            }
        }

        /// <summary>
        /// Sets the Default Stretch-Bend Parameters into the parameter set
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        private void SetDefaultStrBnd()
        {
            Debug.WriteLine("Sets the Default Stretch-Bend Parameters");
            IList<double> data = new List<double>();
            stDFSB.MoveNext();
            string sIR = stDFSB.Current; stDFSB.MoveNext();
            string sJR = stDFSB.Current; stDFSB.MoveNext();
            string sKR = stDFSB.Current; stDFSB.MoveNext();
            string skbaIJK = stDFSB.Current; stDFSB.MoveNext();
            string skbaKJI = stDFSB.Current; stDFSB.MoveNext();

            try
            {
                var key = "DFSB" + sIR + ";" + sJR + ";" + sKR;
                double kbaIJK = double.Parse(skbaIJK, NumberFormatInfo.InvariantInfo);
                double kbaKJI = double.Parse(skbaKJI, NumberFormatInfo.InvariantInfo);
                data.Add(kbaIJK);
                data.Add(kbaKJI);
                parameterSet[key] = data;
            }
            catch (FormatException nfe)
            {
                throw new IOException("setDFSB: Malformed Number due to:" + nfe);
            }
        }

        /// <summary>
        /// The main method which parses through the force field configuration file
        /// </summary>
        /// <exception cref="Exception">Description of the Exception</exception>
        public void ReadParameterSets()
        {
            //vdW,bond,angle,strbond,opbend,torsion,data
            Debug.WriteLine("------ Read MMFF94 ParameterSets ------");

            if (ins == null)
            {
                ins = ResourceLoader.GetAsStream(configFile);
            }
            if (ins == null)
            {
                throw new IOException("There was a problem getting the default stream: " + configFile);
            }

            var r = new StreamReader(ins);
            string s;
            int[] a = { 0, 0, 0, 0, 0, 0, 0, 0 };

            if (insvdW == null)
            {
                insvdW = ResourceLoader.GetAsStream(configFilevdW);
            }
            if (insvdW == null)
            {
                throw new IOException("There was a problem getting the default stream: " + configFilevdW);
            }

            var rvdW = new StreamReader(insvdW);
            string svdW;
            int ntvdW;

            if (insDFSB == null)
            {
                insDFSB = ResourceLoader.GetAsStream(configFileDFSB);
            }
            if (insDFSB == null)
            {
                throw new IOException("There was a problem getting the default stream: " + configFileDFSB);
            }

            var rDFSB = new StreamReader(insDFSB);
            string sDFSB;
            int ntDFSB;

            try
            {
                while (true)
                {
                    s = r.ReadLine();
                    if (s == null)
                    {
                        break;
                    }
                    var e_st = Strings.Tokenize(s, '\t', ';', ' '); st = e_st.GetEnumerator(); st.MoveNext();
                    int nt = e_st.Count;
                    if (s.StartsWith("atom", StringComparison.Ordinal) && nt <= 8)
                    {
                        SetAtomTypes();
                        a[0]++;
                    }
                    else if (s.StartsWith("bond", StringComparison.Ordinal) && nt == 9)
                    {
                        SetBond();
                        a[1]++;
                    }
                    else if (s.StartsWith("angle", StringComparison.Ordinal) && nt <= 10)
                    {
                        SetAngle();
                        a[2]++;
                    }
                    else if (s.StartsWith("strbnd", StringComparison.Ordinal) && nt == 7)
                    {
                        SetStrBnd();
                        a[3]++;
                    }
                    else if (s.StartsWith("torsion", StringComparison.Ordinal) && nt == 11)
                    {
                        SetTorsion();
                        a[4]++;
                    }
                    else if (s.StartsWith("opbend", StringComparison.Ordinal) && nt == 6)
                    {
                        SetOpBend();
                        a[5]++;
                    }
                    else if (s.StartsWith("data", StringComparison.Ordinal) && nt == 10)
                    {
                        while (true)
                        {
                            svdW = rvdW.ReadLine();
                            if (svdW == null)
                            {
                                break;
                            }
                            var e_stvdW = Strings.Tokenize(svdW, '\t', ';', ' '); stvdW = e_stvdW.GetEnumerator(); stvdW.MoveNext();
                            ntvdW = e_stvdW.Count;
                            Debug.WriteLine($"ntvdW : {ntvdW}");
                            if (svdW.StartsWith("vdw", StringComparison.Ordinal) && ntvdW == 9)
                            {
                                st.MoveNext();
                                sid = st.Current; st.MoveNext();
                                stvdW.MoveNext();
                                sidvdW = stvdW.Current; stvdW.MoveNext();
                                if (sid.Equals(sidvdW, StringComparison.Ordinal))
                                {
                                    SetAtomTypeData();
                                    a[6]++;
                                }
                                goto break_readatmmffvdw;
                            }
                        }// end while
                        break_readatmmffvdw:
                        ;
                    }
                }// end while

                ins.Close();
                insvdW.Close();
            }
            catch (IOException)
            {
                throw new IOException("There was a problem parsing the mmff94 forcefield");
            }

            try
            {
                Debug.WriteLine("Parses the Default Stretch-Bend Parameters");
                while (true)
                {
                    sDFSB = rDFSB.ReadLine();
                    Debug.WriteLine($"sDFSB = {sDFSB}");
                    if (sDFSB == null)
                    {
                        Debug.WriteLine("sDFSB == null, break");
                        break;
                    }
                    var e_stDFSB = Strings.Tokenize(sDFSB, '\t', ';', ' '); stDFSB = e_stDFSB.GetEnumerator(); stDFSB.MoveNext();
                    ntDFSB = e_stDFSB.Count;
                    Debug.WriteLine($"ntDFSB : {ntDFSB}");
                    if (sDFSB.StartsWith("DFSB", StringComparison.Ordinal) && ntDFSB == 6)
                    {
                        SetDefaultStrBnd();
                    }
                }
                insDFSB.Close();
                Debug.WriteLine("insDFSB closed");
            }
            catch (IOException)
            {
                throw new IOException("There was a problem parsing the Default Stretch-Bend Parameters (mmffdfsb.par)");
            }
        }

        /// <summary>
        /// Mass number for a atom with a given atomic number and exact mass.
        /// </summary>
        /// <param name="atomicNumber">atomic number</param>
        /// <param name="exactMass">exact mass</param>
        /// <returns>the mass number (or null) if no mass number was found</returns>
        /// <exception cref="IOException">isotope configuration could not be loaded</exception>
        private static int? MassNumber(int atomicNumber, double exactMass)
        {
            var symbol = PeriodicTable.GetSymbol(atomicNumber);
            var isotope = CDK.IsotopeFactory.GetIsotope(symbol, exactMass, 0.001);
            return isotope?.MassNumber;
        }
    }
}
