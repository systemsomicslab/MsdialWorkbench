/* Copyright (C) 2007  Todd Martin (Environmental Protection Agency)  <Martin.Todd@epamail.epa.gov>
 * Copyright (C) 2007  Nikolay Kochev <nick@argon.acad.bg>
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

using NCDK.AtomTypes;
using NCDK.RingSearches;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This class calculates ALOGP (Ghose-Crippen LogKow) and the
    /// Ghose-Crippen molar refractivity <token>cdk-cite-GHOSE1986</token>; <token>cdk-cite-GHOSE1987</token>.
    /// </summary>
    /// <remarks>
    /// <note type="note">
    /// The code assumes that aromaticity has been detected before
    /// evaluating this descriptor. The code also expects that the molecule
    /// will have hydrogens explicitly set. For SD files, this is usually not
    /// a problem since hydrogens are explicit. But for the case of molecules
    /// obtained from SMILES, hydrogens must be made explicit.
    /// </note>
    /// <para>TODO: what should sub return if have missing fragment?
    /// Just report sum for other fragments? Or report as -9999 and
    /// then do not use descriptor if have this value for any
    /// chemicals in cluster?</para>
    /// <para>
    /// Returns three values
    /// <list type="bullet">
    /// <item><term>ALogP</term><description>Ghose-Crippen LogKow</description></item>
    /// <item><term>ALogP2</term><description>ALogP<sup>2</sup></description></item>
    /// <item><term>amr</term><description>molar refractivity</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    // @author     Todd Martin
    // @cdk.module qsarmolecular
    // @cdk.keyword logP
    // @cdk.keyword lipophilicity
    // @cdk.keyword refractivity
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#ALOGP")]
    public class ALogPDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        /// <summary>
        /// needed to retrieve electronegativities
        /// </summary>
        readonly static AtomicProperties ap = Tools.AtomicProperties.Instance;

        /// <summary>
        /// coefficients for alogp model
        /// </summary>
        readonly static double[] FRAGVAL = new double[121];

        /// <summary>
        /// coefficients for refractivity model
        /// </summary>
        readonly static double[] REFRACVAL = new double[121];

        static ALogPDescriptor()
        {
            // fragments for ALOGP from Ghose et al., 1998
            FRAGVAL[1] = -1.5603;
            FRAGVAL[2] = -1.012;
            FRAGVAL[3] = -0.6681;
            FRAGVAL[4] = -0.3698;
            FRAGVAL[5] = -1.788;
            FRAGVAL[6] = -1.2486;
            FRAGVAL[7] = -1.0305;
            FRAGVAL[8] = -0.6805;
            FRAGVAL[9] = -0.3858;
            FRAGVAL[10] = 0.7555;
            FRAGVAL[11] = -0.2849;
            FRAGVAL[12] = 0.02;
            FRAGVAL[13] = 0.7894;
            FRAGVAL[14] = 1.6422;
            FRAGVAL[15] = -0.7866;
            FRAGVAL[16] = -0.3962;
            FRAGVAL[17] = 0.0383;
            FRAGVAL[18] = -0.8051;
            FRAGVAL[19] = -0.2129;
            FRAGVAL[20] = 0.2432;
            FRAGVAL[21] = 0.4697;
            FRAGVAL[22] = 0.2952;
            FRAGVAL[23] = 0;
            FRAGVAL[24] = -0.3251;
            FRAGVAL[25] = 0.1492;
            FRAGVAL[26] = 0.1539;
            FRAGVAL[27] = 0.0005;
            FRAGVAL[28] = 0.2361;
            FRAGVAL[29] = 0.3514;
            FRAGVAL[30] = 0.1814;
            FRAGVAL[31] = 0.0901;
            FRAGVAL[32] = 0.5142;
            FRAGVAL[33] = -0.3723;
            FRAGVAL[34] = 0.2813;
            FRAGVAL[35] = 0.1191;
            FRAGVAL[36] = -0.132;
            FRAGVAL[37] = -0.0244;
            FRAGVAL[38] = -0.2405;
            FRAGVAL[39] = -0.0909;
            FRAGVAL[40] = -0.1002;
            FRAGVAL[41] = 0.4182;
            FRAGVAL[42] = -0.2147;
            FRAGVAL[43] = -0.0009;
            FRAGVAL[44] = 0.1388;
            FRAGVAL[45] = 0;
            FRAGVAL[46] = 0.7341;
            FRAGVAL[47] = 0.6301;
            FRAGVAL[48] = 0.518;
            FRAGVAL[49] = -0.0371;
            FRAGVAL[50] = -0.1036;
            FRAGVAL[51] = 0.5234;
            FRAGVAL[52] = 0.6666;
            FRAGVAL[53] = 0.5372;
            FRAGVAL[54] = 0.6338;
            FRAGVAL[55] = 0.362;
            FRAGVAL[56] = -0.3567;
            FRAGVAL[57] = -0.0127;
            FRAGVAL[58] = -0.0233;
            FRAGVAL[59] = -0.1541;
            FRAGVAL[60] = 0.0324;
            FRAGVAL[61] = 1.052;
            FRAGVAL[62] = -0.7941;
            FRAGVAL[63] = 0.4165;
            FRAGVAL[64] = 0.6601;
            FRAGVAL[65] = 0;
            FRAGVAL[66] = -0.5427;
            FRAGVAL[67] = -0.3168;
            FRAGVAL[68] = 0.0132;
            FRAGVAL[69] = -0.3883;
            FRAGVAL[70] = -0.0389;
            FRAGVAL[71] = 0.1087;
            FRAGVAL[72] = -0.5113;
            FRAGVAL[73] = 0.1259;
            FRAGVAL[74] = 0.1349;
            FRAGVAL[75] = -0.1624;
            FRAGVAL[76] = -2.0585;
            FRAGVAL[77] = -1.915;
            FRAGVAL[78] = 0.4208;
            FRAGVAL[79] = -1.4439;
            FRAGVAL[80] = 0;
            FRAGVAL[81] = 0.4797;
            FRAGVAL[82] = 0.2358;
            FRAGVAL[83] = 0.1029;
            FRAGVAL[84] = 0.3566;
            FRAGVAL[85] = 0.1988;
            FRAGVAL[86] = 0.7443;
            FRAGVAL[87] = 0.5337;
            FRAGVAL[88] = 0.2996;
            FRAGVAL[89] = 0.8155;
            FRAGVAL[90] = 0.4856;
            FRAGVAL[91] = 0.8888;
            FRAGVAL[92] = 0.7452;
            FRAGVAL[93] = 0.5034;
            FRAGVAL[94] = 0.8995;
            FRAGVAL[95] = 0.5946;
            FRAGVAL[96] = 1.4201;
            FRAGVAL[97] = 1.1472;
            FRAGVAL[98] = 0;
            FRAGVAL[99] = 0.7293;
            FRAGVAL[100] = 0.7173;
            FRAGVAL[101] = 0;
            FRAGVAL[102] = -2.6737;
            FRAGVAL[103] = -2.4178;
            FRAGVAL[104] = -3.1121;
            FRAGVAL[105] = 0;
            FRAGVAL[106] = 0.6146;
            FRAGVAL[107] = 0.5906;
            FRAGVAL[108] = 0.8758;
            FRAGVAL[109] = -0.4979;
            FRAGVAL[110] = -0.3786;
            FRAGVAL[111] = 1.5188;
            FRAGVAL[112] = 1.0255;
            FRAGVAL[113] = 0;
            FRAGVAL[114] = 0;
            FRAGVAL[115] = 0;
            FRAGVAL[116] = -0.9359;
            FRAGVAL[117] = -0.1726;
            FRAGVAL[118] = -0.7966;
            FRAGVAL[119] = 0.6705;
            FRAGVAL[120] = -0.4801;

            // fragments for amr from Viswanadhan et al., 1989
            REFRACVAL[1] = 2.968;
            REFRACVAL[2] = 2.9116;
            REFRACVAL[3] = 2.8028;
            REFRACVAL[4] = 2.6205;
            REFRACVAL[5] = 3.015;
            REFRACVAL[6] = 2.9244;
            REFRACVAL[7] = 2.6329;
            REFRACVAL[8] = 2.504;
            REFRACVAL[9] = 2.377;
            REFRACVAL[10] = 2.5559;
            REFRACVAL[11] = 2.303;
            REFRACVAL[12] = 2.3006;
            REFRACVAL[13] = 2.9627;
            REFRACVAL[14] = 2.3038;
            REFRACVAL[15] = 3.2001;
            REFRACVAL[16] = 4.2654;
            REFRACVAL[17] = 3.9392;
            REFRACVAL[18] = 3.6005;
            REFRACVAL[19] = 4.487;
            REFRACVAL[20] = 3.2001;
            REFRACVAL[21] = 3.4825;
            REFRACVAL[22] = 4.2817;
            REFRACVAL[23] = 3.9556;
            REFRACVAL[24] = 3.4491;
            REFRACVAL[25] = 3.8821;
            REFRACVAL[26] = 3.7593;
            REFRACVAL[27] = 2.5009;
            REFRACVAL[28] = 2.5;
            REFRACVAL[29] = 3.0627;
            REFRACVAL[30] = 2.5009;
            REFRACVAL[31] = 0;
            REFRACVAL[32] = 2.6632;
            REFRACVAL[33] = 3.4671;
            REFRACVAL[34] = 3.6842;
            REFRACVAL[35] = 2.9372;
            REFRACVAL[36] = 4.019;
            REFRACVAL[37] = 4.777;
            REFRACVAL[38] = 3.9031;
            REFRACVAL[39] = 3.9964;
            REFRACVAL[40] = 3.4986;
            REFRACVAL[41] = 3.4997;
            REFRACVAL[42] = 2.7784;
            REFRACVAL[43] = 2.6267;
            REFRACVAL[44] = 2.5;
            REFRACVAL[45] = 0;
            REFRACVAL[46] = 0.8447;
            REFRACVAL[47] = 0.8939;
            REFRACVAL[48] = 0.8005;
            REFRACVAL[49] = 0.832;
            REFRACVAL[50] = 0.8;
            REFRACVAL[51] = 0.8188;
            REFRACVAL[52] = 0.9215;
            REFRACVAL[53] = 0.9769;
            REFRACVAL[54] = 0.7701;
            REFRACVAL[55] = 0;
            REFRACVAL[56] = 1.7646;
            REFRACVAL[57] = 1.4778;
            REFRACVAL[58] = 1.4429;
            REFRACVAL[59] = 1.6191;
            REFRACVAL[60] = 1.3502;
            REFRACVAL[61] = 1.945;
            REFRACVAL[62] = 0;
            REFRACVAL[63] = 0;
            REFRACVAL[64] = 11.1366;
            REFRACVAL[65] = 13.1149;
            REFRACVAL[66] = 2.6221;
            REFRACVAL[67] = 2.5;
            REFRACVAL[68] = 2.898;
            REFRACVAL[69] = 3.6841;
            REFRACVAL[70] = 4.2808;
            REFRACVAL[71] = 3.6189;
            REFRACVAL[72] = 2.5;
            REFRACVAL[73] = 2.7956;
            REFRACVAL[74] = 2.7;
            REFRACVAL[75] = 4.2063;
            REFRACVAL[76] = 4.0184;
            REFRACVAL[77] = 3.0009;
            REFRACVAL[78] = 4.7142;
            REFRACVAL[79] = 0;
            REFRACVAL[80] = 0;
            REFRACVAL[81] = 0.8725;
            REFRACVAL[82] = 1.1837;
            REFRACVAL[83] = 1.1573;
            REFRACVAL[84] = 0.8001;
            REFRACVAL[85] = 1.5013;
            REFRACVAL[86] = 5.6156;
            REFRACVAL[87] = 6.1022;
            REFRACVAL[88] = 5.9921;
            REFRACVAL[89] = 5.3885;
            REFRACVAL[90] = 6.1363;
            REFRACVAL[91] = 8.5991;
            REFRACVAL[92] = 8.9188;
            REFRACVAL[93] = 8.8006;
            REFRACVAL[94] = 8.2065;
            REFRACVAL[95] = 8.7352;
            REFRACVAL[96] = 13.9462;
            REFRACVAL[97] = 14.0792;
            REFRACVAL[98] = 14.073;
            REFRACVAL[99] = 12.9918;
            REFRACVAL[100] = 13.3408;
            REFRACVAL[101] = 0;
            REFRACVAL[102] = 0;
            REFRACVAL[103] = 0;
            REFRACVAL[104] = 0;
            REFRACVAL[105] = 0;
            REFRACVAL[106] = 7.8916;
            REFRACVAL[107] = 7.7935;
            REFRACVAL[108] = 9.4338;
            REFRACVAL[109] = 7.7223;
            REFRACVAL[110] = 5.7558;
            REFRACVAL[111] = 0;
            REFRACVAL[112] = 0;
            REFRACVAL[113] = 0;
            REFRACVAL[114] = 0;
            REFRACVAL[115] = 0;
            REFRACVAL[116] = 5.5306;
            REFRACVAL[117] = 5.5152;
            REFRACVAL[118] = 6.836;
            REFRACVAL[119] = 10.0101;
            REFRACVAL[120] = 5.2806;
        }

        public ALogPDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            [DescriptorResultProperty]
            public double ALogP { get; private set; }
            [DescriptorResultProperty]
            public double ALogP2 { get; private set; }
            [DescriptorResultProperty]
            public double AMR { get; private set; }

            public Result(double ALogP, double ALogP2, double AMR)
            {
                this.ALogP = ALogP;
                this.ALogP2 = ALogP2;
                this.AMR = AMR;
            }

            public Result(Exception e)
                : base(e)
            {
            }

            public double Value => ALogP;
        }

        /// <remarks>
        /// TODO: Ideally we should explicit H addition should be cached
        /// </remarks>
        /// <returns>the result of the calculation</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
            var hAdder = CDK.HydrogenAdder;
            hAdder.AddImplicitHydrogens(container);
            AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(container);

            return new Calculator(container).Calculate();
        }

        class Calculator
        {
            IAtomContainer atomContainer;

            /// <summary>
            /// estate fragments for each atom
            /// </summary>
            private readonly string[] fragment;

            IRingSet rs;

            /// <summary>
            /// counts of each type of fragment in the molecule
            /// </summary>
            private readonly int[] frags = new int[121];

            /// <summary>
            /// alogp fragments for each atom (used to see which atoms have missing fragments)
            /// </summary>
            private int[] alogpfrag;

            string unassignedAtoms = "";

            private void FindUnassignedAtoms()
            {
                unassignedAtoms = "";
                for (int i = 0; i <= atomContainer.Atoms.Count - 1; i++)
                {
                    if (alogpfrag[i] == 0)
                        unassignedAtoms += $"{i + 1}({fragment[i]}),";
                }
            }

            public Calculator(IAtomContainer atomContainer)
            {
                this.atomContainer = atomContainer;
                this.fragment = new string[atomContainer.Atoms.Count];
            }

            /// <summary>
            /// The AlogP descriptor.
            /// </summary>
            /// <remarks>
            /// TODO: Ideally we should explicit H addition should be cached
            /// </remarks>
            /// <returns>the result of the calculation</returns>
            public Result Calculate()
            {
                var arf = new AllRingsFinder();
                rs = arf.FindAllRings(atomContainer);

                var eStateMatcher = new EStateAtomTypeMatcher(rs);

                foreach (var ring in rs)
                {
                    bool arom = true;
                    foreach (var bond in ring.Bonds)
                    {
                        if (!bond.IsAromatic)
                        {
                            arom = false;
                            break;
                        }
                    }
                    ring.IsAromatic = arom;
                }

                for (int i = 0; i < atomContainer.Atoms.Count; i++)
                {
                    var atomType = eStateMatcher.FindMatchingAtomType(atomContainer, atomContainer.Atoms[i]);
                    if (atomType == null)
                    {
                        fragment[i] = null;
                    }
                    else
                    {
                        fragment[i] = atomType.AtomTypeName;
                    }
                }

                double alogp = 0.0;
                double amr = 0.0;
                double alogp2 = 0.0;

                alogpfrag = new int[atomContainer.Atoms.Count];

                for (int i = 1; i <= 120; i++)
                {
                    frags[i] = 0;
                }

                for (int i = 0; i <= atomContainer.Atoms.Count - 1; i++)
                {
                    try
                    {
                        if (fragment[i] != null)
                        {
                            CalcGroup001_005(i);
                            CalcGroup002_006_007(i);
                            CalcGroup003_008_009_010(i);
                            CalcGroup004_011_to_014(i);
                            CalcGroup015(i);
                            CalcGroup016_018_036_037(i);
                            CalcGroup017_019_020_038_to_041(i);
                            CalcGroup021_to_023_040(i);
                            CalcGroup024_027_030_033_042(i);
                            CalcGroup025_026_028_029_031_032_034_035_043_044(i);
                            CalcGroup056_57(i);
                            CalcGroup058_61(i);
                            CalcGroup059_060_063(i);
                            CalcGroup066_to_079(i);
                            CalcGroup081_to_085(i);
                            CalcGroup086_to_090(i);
                            CalcGroup091_to_095(i);
                            CalcGroup096_to_100(i);
                            CalcGroup101_to_104(i);
                            CalcGroup106(i);
                            CalcGroup107(i);
                            CalcGroup108(i);
                            CalcGroup109(i);
                            CalcGroup110(i);
                            CalcGroup111(i);
                            CalcGroup112(i);
                            CalcGroup115(i);
                            CalcGroup116_117_120(i);
                            CalcGroup118_119(i);
                        }
                    }
                    catch (Exception e)
                    {
                        return new Result(e);
                    }
                } // end i atom loop

                Debug.WriteLine("\nFound fragments and frequencies ");

                for (int i = 1; i <= 120; i++)
                {
                    alogp += FRAGVAL[i] * frags[i];
                    amr += REFRACVAL[i] * frags[i];
                    if (frags[i] > 0)
                    {
                        Debug.WriteLine($"frag {i}  --> {frags[i]}");
                    }
                }
                alogp2 = alogp * alogp;

                this.FindUnassignedAtoms();

                return new Result(alogp, alogp2, amr);
            }

            private static bool IsHetero(IAtom atom)
            {
                switch (atom.AtomicNumber)
                {
                    case 5: // B
                    case 7: // N
                    case 8: // O
                    case 9: // F
                    case 14: // Si
                    case 15: // P
                    case 16: // S
                    case 17: // Cl
                    case 34: // Se
                    case 35: // Br
                    case 53: // I
                        return true;
                    default:
                        return false;
                }
            }

            private void CalcGroup001_005(int i)
            {
                // C in CH3R
                switch (fragment[i])
                {
                    case "SsCH3":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var atom = atomContainer.Atoms[i];
                        int htype = GetHAtomType(atom, ca);
                        frags[htype] += atom.ImplicitHydrogenCount.Value;
                        foreach (var a in ca)
                        {
                            switch (a.AtomicNumber)
                            {
                                case AtomicNumbers.C:
                                    frags[1]++;
                                    alogpfrag[i] = 1;
                                    break;
                                case AtomicNumbers.H:
                                    frags[htype]++;
                                    alogpfrag[atomContainer.Atoms.IndexOf(a)] = htype;
                                    break;
                                default:
                                    frags[5]++;
                                    alogpfrag[i] = 5;
                                    break;
                            }
                        }
                        break;
                }
            }

            private void CalcGroup002_006_007(int i)
            {
                // C in CH2RX
                switch (fragment[i])
                {
                    case "SssCH2":
                        var atom = atomContainer.Atoms[i];
                        var nbors = atomContainer.GetConnectedAtoms(atom);
                        int htype = GetHAtomType(atom, nbors);
                        frags[htype] += atom.ImplicitHydrogenCount.Value;

                        int carbonCount = 0;
                        int heteroCount = 0;
                        // logger.debug("here");
                        foreach (var a in nbors)
                        {
                            switch (a.AtomicNumber)
                            {
                                case AtomicNumbers.C:
                                    carbonCount++;
                                    break;
                                case AtomicNumbers.H:
                                    frags[htype]++;
                                    alogpfrag[atomContainer.Atoms.IndexOf(a)] = htype;
                                    break;
                                default:
                                    heteroCount++;
                                    break;
                            }
                        }

                        if (carbonCount == 2 && heteroCount == 0)
                        {
                            frags[2]++;
                            alogpfrag[i] = 2;
                        }
                        else if (carbonCount == 1 && heteroCount == 1)
                        {
                            frags[6]++;
                            alogpfrag[i] = 6;
                        }
                        else if (carbonCount == 0 && heteroCount == 2)
                        {
                            frags[7]++;
                            alogpfrag[i] = 7;
                        }
                        break;
                }
            }

            private void CalcGroup003_008_009_010(int i)
            {
                switch (fragment[i])
                {
                    case "SsssCH":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var atom = atomContainer.Atoms[i];
                        int htype = GetHAtomType(atom, ca);
                        frags[htype] += atom.ImplicitHydrogenCount.Value;

                        int carbonCount = 0;
                        int heteroCount = 0;
                        // Debug.WriteLine("here");
                        foreach (var a in ca)
                        {
                            switch (a.AtomicNumber)
                            {
                                case AtomicNumbers.C:
                                    carbonCount++;
                                    break;
                                case AtomicNumbers.H:
                                    frags[htype]++;
                                    alogpfrag[a.Index] = htype;
                                    break;
                                default:
                                    heteroCount++;
                                    break;
                            }
                        }

                        if (carbonCount == 3 && heteroCount == 0)
                        {
                            frags[3]++;
                            alogpfrag[i] = 3;
                        }
                        else if (carbonCount == 2 && heteroCount == 1)
                        {
                            frags[8]++;
                            alogpfrag[i] = 8;
                        }
                        else if (carbonCount == 1 && heteroCount == 2)
                        {
                            frags[9]++;
                            alogpfrag[i] = 9;
                        }
                        else if (carbonCount == 0 && heteroCount == 3)
                        {
                            frags[10]++;
                            alogpfrag[i] = 10;
                        }
                        break;
                }
            }

            private void CalcGroup004_011_to_014(int i)
            {
                // C in CR4, CR3X, CX4
                if (string.Equals(fragment[i], "SssssC", StringComparison.Ordinal))
                {
                    var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                    int carbonCount = 0;
                    int heteroCount = 0;
                    // Debug.WriteLine("here");
                    foreach (var a in ca)
                    {
                        switch (a.AtomicNumber)
                        {
                            case AtomicNumbers.C:
                                carbonCount++;
                                break;
                            default:
                                heteroCount++;
                                break;
                        }
                    }

                    if (carbonCount == 4 && heteroCount == 0)
                    {
                        frags[4]++;
                        alogpfrag[i] = 4;
                    }
                    else if (carbonCount == 3 && heteroCount == 1)
                    {
                        frags[11]++;
                        alogpfrag[i] = 11;
                    }
                    else if (carbonCount == 2 && heteroCount == 2)
                    {
                        frags[12]++;
                        alogpfrag[i] = 12;
                    }
                    else if (carbonCount == 1 && heteroCount == 3)
                    {
                        frags[13]++;
                        alogpfrag[i] = 13;
                    }
                    else if (carbonCount == 0 && heteroCount == 4)
                    {
                        frags[14]++;
                        alogpfrag[i] = 14;
                    }
                }
            }

            private void CalcGroup015(int i)
            {
                if (string.Equals(fragment[i], "SdCH2", StringComparison.Ordinal))
                {
                    frags[15]++;
                    alogpfrag[i] = 15;
                    var atom = atomContainer.Atoms[i];
                    int htype = GetHAtomType(atom, null);
                    frags[htype] += 2;
                }
            }

            private void CalcGroup016_018_036_037(int i)
            {

                IAtom ai = atomContainer.Atoms[i];
                if (!string.Equals(fragment[i], "SdsCH", StringComparison.Ordinal))
                    return;

                var ca = atomContainer.GetConnectedAtoms(ai);
                int htype = GetHAtomType(atomContainer.Atoms[i], ca);
                frags[htype]++;

                bool haveCdX = false;
                bool haveCsX = false;
                bool haveCsAr = false;

                foreach (var a in ca)
                {
                    if (a.AtomicNumber.Equals(AtomicNumbers.H))
                    {
                        alogpfrag[atomContainer.Atoms.IndexOf(a)] = htype;
                        continue;
                    }

                    switch (atomContainer.GetBond(ai, a).Order)
                    {
                        case BondOrder.Single:
                            if (!a.AtomicNumber.Equals(AtomicNumbers.C))
                            {
                                haveCsX = true;
                            }

                            if (a.IsAromatic)
                            {
                                haveCsAr = true;
                            }
                            break;
                        case BondOrder.Double:
                            if (!a.AtomicNumber.Equals(AtomicNumbers.C))
                            {
                                haveCdX = true;
                            }
                            break;
                    }
                }

                if (haveCdX)
                {
                    if (haveCsAr)
                    {
                        frags[37]++;
                        alogpfrag[i] = 37;
                    }
                    else
                    {
                        frags[36]++;
                        alogpfrag[i] = 36;
                    }
                }
                else
                {
                    if (haveCsX)
                    {
                        frags[18]++;
                        alogpfrag[i] = 18;
                    }
                    else
                    {
                        frags[16]++;
                        alogpfrag[i] = 16;
                    }
                }
            }

            private void CalcGroup017_019_020_038_to_041(int i)
            {

                IAtom ai = atomContainer.Atoms[i];

                switch (fragment[i])
                {
                    case "SdssC":
                    case "SdaaC":
                        break;
                    default:
                        return;
                }

                var ca = atomContainer.GetConnectedAtoms(ai);

                int rCount = 0;
                int xCount = 0;
                bool haveCdX = false;
                int aromaticCount = 0;

                foreach (var bond in ai.Bonds)
                {
                    var nbor = bond.GetOther(ai);
                    switch (bond.Order)
                    {
                        case BondOrder.Single:
                            if (nbor.AtomicNumber == AtomicNumbers.C)
                            {
                                rCount++;
                                if (nbor.IsAromatic)
                                    aromaticCount++;
                            }
                            else
                                xCount++;
                            break;
                        case BondOrder.Double:
                            if (IsHetero(nbor))
                            {
                                haveCdX = true;
                            }
                            break;
                    }
                }

                if (haveCdX)
                {
                    if (aromaticCount >= 1)
                    {
                        // Ar-C(=X)-R 39
                        // Ar-C(=X)-X 40
                        if (xCount == 1)
                        {
                            frags[40]++;
                            alogpfrag[i] = 40;
                        }
                        else
                        {
                            frags[39]++;
                            alogpfrag[i] = 39;
                        }
                    }
                    else if (aromaticCount == 0)
                    {
                        if (rCount == 1 && xCount == 1)
                        {
                            frags[40]++;
                            alogpfrag[i] = 40;
                        }
                        else if (rCount == 0 && xCount == 2)
                        {
                            frags[41]++;
                            alogpfrag[i] = 41;
                        }
                        else
                        {
                            frags[38]++;
                            alogpfrag[i] = 38;
                        }
                    }
                }
                else
                {
                    if (rCount == 2 && xCount == 0)
                    {
                        frags[17]++;
                        alogpfrag[i] = 17;
                    }
                    else if (rCount == 1 && xCount == 1)
                    {
                        frags[19]++;
                        alogpfrag[i] = 19;
                    }
                    else if (rCount == 0 && xCount == 2)
                    {
                        frags[20]++;
                        alogpfrag[i] = 20;
                    }
                }

            }

            private void CalcGroup021_to_023_040(int i)
            {
                var nbors = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]).ToReadOnlyList();
                var ai = atomContainer.Atoms[i];

                if (string.Equals(fragment[i], "StCH", StringComparison.Ordinal))
                {
                    frags[21]++;
                    alogpfrag[i] = 21;
                    int htype = GetHAtomType(atomContainer.Atoms[i], nbors);
                    frags[htype]++;
                    foreach (var nbor in nbors)
                    {
                        if (nbor.AtomicNumber == AtomicNumbers.H)
                            alogpfrag[nbor.Index] = htype;
                    }
                }
                else if (string.Equals(fragment[i], "SddC", StringComparison.Ordinal))
                {
                    if (nbors[0].AtomicNumber == AtomicNumbers.C && nbors[1].AtomicNumber == AtomicNumbers.C)
                    {
                        // R==C==R
                        frags[22]++;
                        alogpfrag[i] = 22;
                    }
                    else if (nbors[0].AtomicNumber != AtomicNumbers.C && nbors[1].AtomicNumber != AtomicNumbers.C)
                    {
                        // X==C==X
                        frags[40]++;
                        alogpfrag[i] = 40;
                    }
                }
                else if (string.Equals(fragment[i], "StsC", StringComparison.Ordinal))
                {

                    bool haveCtX = false;
                    bool haveCsX = false;

                    foreach (var a in nbors)
                    {
                        switch (atomContainer.GetBond(ai, a).Order)
                        {
                            case BondOrder.Single:
                                if (!a.AtomicNumber.Equals(AtomicNumbers.C))
                                {
                                    haveCsX = true;
                                }
                                break;
                            case BondOrder.Triple:
                                if (!a.AtomicNumber.Equals(AtomicNumbers.C))
                                {
                                    haveCtX = true;
                                }
                                break;
                        }
                    }

                    if (haveCtX && !haveCsX)
                    {
                        frags[40]++;
                        alogpfrag[i] = 40;
                    }
                    else if (haveCsX)
                    {
                        // #C-X
                        frags[23]++;
                        alogpfrag[i] = 23;
                    }
                    else if (!haveCsX)
                    { 
                        // #C-R
                        frags[22]++;
                        alogpfrag[i] = 22;
                    }
                }
            }

            private void CalcGroup024_027_030_033_042(int i)
            {
                // 24: C in R--CH--R
                // 27: C in R--CH--X
                // 30: C in X--CH--X
                // 33: C in R--CH...X
                // 42: C in X--CH...X

                if (!string.Equals(fragment[i], "SaaCH", StringComparison.Ordinal))
                    return;

                var atom = atomContainer.Atoms[i];
                var nbors = atomContainer.GetConnectedAtoms(atom).ToReadOnlyList();
                var htype = GetHAtomType(atom, nbors);
                frags[htype]++;
                IAtom ca0;
                IAtom ca1;
                // determine which neighbor is the H atom
                if (atom.ImplicitHydrogenCount.Value == 1)
                {
                    ca0 = nbors[0];
                    ca1 = nbors[1];
                }
                else if (nbors[0].AtomicNumber == 1)
                {
                    alogpfrag[atomContainer.Atoms.IndexOf(nbors[0])] = htype;
                    ca0 = nbors[1];
                    ca1 = nbors[2];
                }
                else if (nbors[1].AtomicNumber == 1)
                {
                    ca0 = nbors[0];
                    alogpfrag[atomContainer.Atoms.IndexOf(nbors[1])] = htype;
                    ca1 = nbors[2];
                }
                else if (nbors[2].AtomicNumber == 1)
                {
                    ca0 = nbors[0];
                    ca1 = nbors[1];
                    alogpfrag[atomContainer.Atoms.IndexOf(nbors[2])] = htype;
                }
                else
                    throw new ApplicationException();
                
                if (ca0.AtomicNumber.Equals(AtomicNumbers.C) && ca1.AtomicNumber.Equals(AtomicNumbers.C))
                {
                    frags[24]++;
                    alogpfrag[i] = 24;
                    return;
                }

                // check if both hetero atoms have at least one double bond
                var bonds = atomContainer.GetConnectedBonds(ca0);
                bool haveDouble1 = false;
                foreach (var bond in bonds)
                {
                    if (bond.Order == BondOrder.Double)
                    {
                        haveDouble1 = true;
                        break;
                    }
                }

                bonds = atomContainer.GetConnectedBonds(ca1);
                bool haveDouble2 = false;
                foreach (var bond in bonds)
                {
                    if (bond.Order == BondOrder.Double)
                    {
                        haveDouble2 = true;
                        break;
                    }
                }

                if (!ca0.AtomicNumber.Equals(AtomicNumbers.C) 
                 && !nbors[1].AtomicNumber.Equals(AtomicNumbers.C))
                {
                    if (haveDouble1 && haveDouble2)
                    { 
                        // X--CH--X
                        frags[30]++;
                        alogpfrag[i] = 30;
                    }
                    else
                    { 
                        // X--CH...X
                        frags[42]++;
                        alogpfrag[i] = 42;
                    }

                }
                else if (ca0.AtomicNumber.Equals(AtomicNumbers.C) && !ca1.AtomicNumber.Equals(AtomicNumbers.C)
                      || (!ca0.AtomicNumber.Equals(AtomicNumbers.C) && ca1.AtomicNumber.Equals(AtomicNumbers.C)))
                {

                    if (haveDouble1 && haveDouble2)
                    { // R--CH--X
                        frags[27]++;
                        alogpfrag[i] = 27;
                    }
                    else
                    {// R--CH...X
                        frags[33]++;
                        alogpfrag[i] = 33;

                    }
                }
            }

            private void CalcGroup025_026_028_029_031_032_034_035_043_044(int i)
            {
                // 25: R--CR--R
                // 26: R--CX--R
                // 28: R--CR--X
                // 29: R--CX--X
                // 31: X--CR--X
                // 32: X--CX--X
                // 34: X--CR...X
                // 35: X--CX...X
                // 43: X--CR...X
                // 43: X--CX...X

                switch (fragment[i])
                {
                    case "SsaaC":
                    case "SaaaC":
                        break;
                    default:
                        return;
                }

                var atm = atomContainer.Atoms[i];
                var nbors = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]).ToList();

                var sameringatoms = new IAtom[2];
                var nonringatom = atomContainer.Builder.NewAtom();

                int sameringatomscount = 0;
                foreach (var a in nbors)
                {
                    if (InSameAromaticRing(atomContainer, atm, a, rs))
                    {
                        sameringatomscount++;
                    }
                }

                if (sameringatomscount == 2)
                {
                    int count = 0;
                    foreach (var a in nbors)
                    {
                        if (InSameAromaticRing(atomContainer, atm, a, rs))
                        {
                            sameringatoms[count] = a;
                            count++;
                        }
                        else
                        {
                            nonringatom = a;
                        }
                    }
                }
                else
                {
                    // sameringsatomscount==3
                    // arbitrarily assign atoms: (no way to decide consistently)
                    // but to match VEGA we choose to but hetero atoms in the ring
                    nbors.Sort((a, b) => -IsHetero(a).CompareTo(IsHetero(b)));
                    sameringatoms[0] = nbors[0];
                    sameringatoms[1] = nbors[1];
                    nonringatom = nbors[2];
                }

                // check if both hetero atoms have at least one double bond
                var bonds = atomContainer.GetConnectedBonds(sameringatoms[0]);

                bool haveDouble1 = false;

                foreach (var bond in bonds)
                {
                    if (bond.Order == BondOrder.Double)
                    {
                        haveDouble1 = true;
                        break;
                    }

                }

                bonds = atomContainer.GetConnectedBonds(sameringatoms[1]);

                bool haveDouble2 = false;

                foreach (var bond in bonds)
                {
                    if (bond.Order == BondOrder.Double)
                    {
                        haveDouble2 = true;
                        break;
                    }

                }

                if (!string.Equals(sameringatoms[0].Symbol, "C", StringComparison.Ordinal) && !string.Equals(sameringatoms[1].Symbol, "C", StringComparison.Ordinal))
                {
                    if (haveDouble1 && haveDouble2)
                    { // X--CR--X
                        if (nonringatom.AtomicNumber.Equals(AtomicNumbers.C))
                        {
                            frags[31]++;
                            alogpfrag[i] = 31;
                        }
                        else
                        { // X--CX--X
                            frags[32]++;
                            alogpfrag[i] = 32;
                        }

                    }
                    else
                    {

                        if (nonringatom.AtomicNumber.Equals(AtomicNumbers.C))
                        { // X--CR..X
                            frags[43]++;
                            alogpfrag[i] = 43;

                        }
                        else
                        { // X--CX...X
                            frags[44]++;
                            alogpfrag[i] = 44;
                        }

                    }
                }
                else if (string.Equals(sameringatoms[0].Symbol, "C", StringComparison.Ordinal) && string.Equals(sameringatoms[1].Symbol, "C", StringComparison.Ordinal))
                {

                    if (nonringatom.AtomicNumber.Equals(AtomicNumbers.C))
                    {// R--CR--R
                        frags[25]++;
                        alogpfrag[i] = 25;
                    }
                    else
                    { // R--CX--R
                        frags[26]++;
                        alogpfrag[i] = 26;
                    }

                }
                else if ((string.Equals(sameringatoms[0].Symbol, "C", StringComparison.Ordinal) && !string.Equals(sameringatoms[1].Symbol, "C", StringComparison.Ordinal))
                      || (!string.Equals(sameringatoms[0].Symbol, "C", StringComparison.Ordinal) && string.Equals(sameringatoms[1].Symbol, "C", StringComparison.Ordinal)))
                {

                    if (haveDouble1 && haveDouble2)
                    { // R--CR--X
                        if (nonringatom.AtomicNumber.Equals(AtomicNumbers.C))
                        {
                            frags[28]++;
                            alogpfrag[i] = 28;
                        }
                        else
                        { // R--CX--X
                            frags[29]++;
                            alogpfrag[i] = 29;
                        }

                    }
                    else
                    {

                        if (nonringatom.AtomicNumber.Equals(AtomicNumbers.C))
                        { // R--CR..X
                            frags[34]++;
                            alogpfrag[i] = 34;

                        }
                        else
                        { // R--CX...X
                            frags[35]++;
                            alogpfrag[i] = 35;
                        }
                    }
                }
            }

            private bool IsPyrroleLikeHetero(IAtom atom)
            {
                if (!atom.IsAromatic)
                    return false;
                switch (atom.AtomicNumber)
                {
                    case 7:
                    case 15:
                        if (atom.Bonds.Count == 3 && atom.FormalCharge == 0)
                            return true;
                        if (atom.Bonds.Count == 2 && atom.ImplicitHydrogenCount == 1)
                            return true;
                        if (atom.Bonds.Count == 2 && atom.FormalCharge == -1)
                            return true;
                        return false;
                    case 8:
                    case 16:
                        return true;
                }
                return false;
            }

            private int GetHAtomType(IAtom atom, IEnumerable<IAtom> connectedAtoms)
            {
                //ai is the atom connected to a H atoms.
                //ai environment determines what is the H atom type
                //This procedure is applied only for carbons
                //i.e. H atom type 50 is never returned

                Hybridization hyb;

                int ndoub = 0;
                int ntrip = 0;
                int oxNum = 0;
                int xCount = 0;
                bool hasConjHetereo = false;

                foreach (var bond in atom.Bonds)
                {
                    if (bond.Order == BondOrder.Double)
                        ndoub++;
                    else if (bond.Order == BondOrder.Triple)
                        ntrip++;
                    var nbor = bond.GetOther(atom);
                    if (IsHetero(nbor))
                    {
                        if (bond.IsAromatic)
                        {
                            if (bond.Order == BondOrder.Single)
                            {
                                if (!IsPyrroleLikeHetero(nbor) && !hasConjHetereo)
                                {
                                    oxNum += 2;
                                    hasConjHetereo = true;
                                }
                                else
                                    oxNum++;
                            }
                            else if (!hasConjHetereo)
                            {
                                hasConjHetereo = true;
                                oxNum += 2;
                            }
                            else
                            {
                                oxNum++;
                            }
                        }
                        else
                            oxNum += bond.Order.Numeric();
                    }
                    else if (nbor.AtomicNumber == 6)
                    {
                        foreach (var bond2 in nbor.Bonds)
                        {
                            var nbor2 = bond2.GetOther(nbor);
                            if (IsHetero(nbor2))
                                xCount++;
                        }
                    }
                }

                if (ndoub == 0 && ntrip == 0)
                    hyb = Hybridization.SP3;
                else if (ndoub == 1 && ntrip == 0)
                    hyb = Hybridization.SP2;
                else if (ndoub == 2 || ntrip == 1)
                    hyb = Hybridization.SP1;
                else
                    return 0; // unknown

                // first check for alpha carbon:
                // -C=X, -C#X and -C:X
                bool isAlphaC = false;
                if (atom.AtomicNumber == 6 && hyb == Hybridization.SP3)
                {
                    foreach (var bond in atom.Bonds)
                    {
                        var nbor = bond.GetOther(atom);
                        if (IsHetero(nbor))
                        {
                            isAlphaC = false;
                            break;
                        }
                        else if (nbor.AtomicNumber == 6)
                        {
                            int numDoubX = 0, numTripX = 0, numAromX = 0;
                            foreach (var bond2 in nbor.Bonds)
                            {
                                var nbor2 = bond2.GetOther(nbor);
                                if (IsHetero(nbor2))
                                {
                                    switch (bond2.Order)
                                    {
                                        case BondOrder.Single:
                                            if (bond2.IsAromatic)
                                                numAromX++;
                                            break;
                                        case BondOrder.Double:
                                            if (bond2.IsAromatic)
                                                numAromX++;
                                            else
                                                numDoubX++;
                                            break;
                                        case BondOrder.Triple:
                                            numTripX++;
                                            break;
                                    }
                                }
                            }
                            if (numDoubX + numTripX + numAromX == 1)
                                isAlphaC = true;
                        }
                    }
                }

                if (isAlphaC)
                    return 51;
                switch (hyb)
                {
                    case Hybridization.SP1:
                        if (oxNum == 0)
                            return 48;
                        if (oxNum == 1)
                            return 49;
                        break;
                    case Hybridization.SP2:
                        if (oxNum == 0)
                            return 47;
                        if (oxNum == 1)
                            return 48;
                        if (oxNum == 2 || oxNum == 3)
                            return 49;
                        break;
                    case Hybridization.SP3:
                        if (oxNum == 0)
                        {
                            if (xCount == 0)
                                return 46;
                            else if (xCount == 1)
                                return 52;
                            else if (xCount == 2)
                                return 53;
                            else if (xCount == 3)
                                return 54;
                            else if (xCount == 4)
                                return 55;
                        }
                        if (oxNum == 1)
                            return 47;
                        if (oxNum == 2)
                            return 48;
                        if (oxNum == 3)
                            return 49;
                        break;
                }

                return 0;
            }

            private void CalcGroup056_57(int i)
            {
                // 56: O in =O
                // 57: O in phenol, enol, and carboxyl
                // enol : compound containing a hydroxyl group bonded to a carbon atom
                // that in turn forms a double bond with another carbon atom.
                // enol = HO-C=C-
                // carboxyl= HO-C(=O)-

                if (!string.Equals(fragment[i], "SsOH", StringComparison.Ordinal))
                    return;

                var atm = atomContainer.Atoms[i];
                int htype = 50; //H atom attached to a hetero atom
                frags[htype]++;

                IAtom ca0 = null;
                foreach (var bond in atm.Bonds)
                {
                    var nbor = bond.GetOther(atm);
                    switch (nbor.AtomicNumber)
                    {
                        case AtomicNumbers.C:
                            ca0 = nbor;
                            break;
                        case AtomicNumbers.H:
                            alogpfrag[nbor.Index] = htype;
                            break;
                    }
                }

                if (ca0 != null)
                {
                    if (ca0.IsAromatic)
                    { // phenol
                        frags[57]++;
                        alogpfrag[i] = 57;
                        return;
                    }

                    // Check for C=COH, and C(OH)=O
                    foreach (var bond in ca0.Bonds)
                    {
                        IAtom nbor2 = bond.GetOther(ca0);
                        if (nbor2 == atm)
                            continue;
                        if (bond.Order == BondOrder.Double &&
                            (nbor2.AtomicNumber == 6 || nbor2.AtomicNumber == 8))
                        {
                            frags[57]++;
                            alogpfrag[i] = 57;
                            return;
                        }
                    }
                }

                frags[56]++;
                alogpfrag[i] = 56;
            }

            private void CalcGroup058_61(int i)
            {
                var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]).ToReadOnlyList();

                // 58: O in =O
                // 61: --O in nitro, N-oxides
                // 62: O in O-

                if (string.Equals(fragment[i], "SsOm", StringComparison.Ordinal))
                {
                    var ca0 = ca[0];
                    if (ca0.AtomicNumber.Equals(AtomicNumbers.N) && ca0.FormalCharge == 1)
                    {
                        frags[61]++;
                        alogpfrag[i] = 61;
                    }
                    else
                    {
                        frags[62]++;
                        alogpfrag[i] = 62;
                    }
                }
                else if (string.Equals(fragment[i], "SdO", StringComparison.Ordinal))
                {
                    var ca0 = ca[0];
                    if (ca0.AtomicNumber.Equals(AtomicNumbers.N) && ca0.FormalCharge == 1)
                    {
                        frags[61]++;
                        alogpfrag[i] = 61;
                    }
                    else
                    {
                        frags[58]++;
                        alogpfrag[i] = 58;
                    }
                }
            }

            private void CalcGroup059_060_063(int i)
            {
                // O in Al-O-Ar, Ar2O, R...O...R, ROC=X
                // ... = aromatic single bonds

                // Al-O-Ar, Ar2O
                switch (fragment[i])
                {
                    case "SssO":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]).ToReadOnlyList();
                        var ca0 = ca[0];
                        var ca1 = ca[1];
                        if (ca0.IsAromatic || ca1.IsAromatic)
                        {
                            frags[60]++;
                            alogpfrag[i] = 60;
                        }
                        else
                        {
                            foreach (var a in ca)
                            {
                                // O-P(=S)
                                // was considered to count as group 60

                                var ca2 = atomContainer.GetConnectedAtoms(a);
                                foreach (var a2 in ca2)
                                {
                                    if (atomContainer.GetBond(a, a2).Order == BondOrder.Double)
                                    {
                                        if (!a2.AtomicNumber.Equals(AtomicNumbers.C))
                                        {
                                            frags[60]++;
                                            alogpfrag[i] = 60;
                                            return;
                                        }
                                    }
                                }
                            } // end j ca loop

                            if (ca0.AtomicNumber.Equals(AtomicNumbers.O) || ca1.AtomicNumber.Equals(AtomicNumbers.O))
                            {
                                frags[63]++;
                                alogpfrag[i] = 63;
                            }
                            else
                            {
                                frags[59]++;
                                alogpfrag[i] = 59;
                            }
                        }
                        break;
                    case "SaaO":
                        frags[60]++;
                        alogpfrag[i] = 60;
                        break;
                }
            }

            private void CalcGroup066_to_079(int i)
            {
                int nAr = 0;
                int nAl = 0;
                var atom = atomContainer.Atoms[i];
                var ai = atom;
                if (!ai.AtomicNumber.Equals(AtomicNumbers.N))
                    return;
                var nbors = atomContainer.GetConnectedAtoms(atom).ToList();

                int htype = 50; //H atom attached to a hetero atom
                foreach (var nbor in nbors)
                {
                    if (nbor.AtomicNumber == AtomicNumbers.H)
                    {
                        alogpfrag[nbor.Index] = htype;
                        frags[htype]++;
                    }
                }
                frags[htype] += atom.ImplicitHydrogenCount.Value;

                foreach (var a in nbors)
                {
                    if (a.AtomicNumber == AtomicNumbers.H)
                        continue;
                    if (a.IsAromatic)
                        nAr++;
                    else
                        nAl++;
                }

                switch (fragment[i])
                {
                    case "SsssN":
                    case "SssNH":
                    case "SsNH2":
                        // first check if have RC(=O)N or NX=X
                        foreach (var a in nbors)
                        {
                            if (a.AtomicNumber.Equals(AtomicNumbers.H))
                                continue;
                            var ca2 = atomContainer.GetConnectedAtoms(a);
                            foreach (var a2 in ca2)
                            {
                                var ca2k = a2;
                                if (atomContainer.Atoms.IndexOf(ca2k) != i)
                                {
                                    if (!ca2k.AtomicNumber.Equals(AtomicNumbers.C))
                                    {
                                        if (!ca2k.IsAromatic
                                         && !a.IsAromatic
                                         && !ai.IsAromatic)
                                        {
                                            if (atomContainer.GetBond(a, ca2k).Order == BondOrder.Double)
                                            {
                                                frags[72]++;
                                                alogpfrag[i] = 72;
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }

                switch (fragment[i])
                {
                    case "SsNH2":
                        {
                            IAtom ca0 = null;
                            //Find which neigbpur is not the hydrogen atom
                            foreach (var nbor in nbors)
                            {
                                if (nbor.AtomicNumber != AtomicNumbers.H)
                                {
                                    ca0 = nbor;
                                    break;
                                }
                            }
                            if (ca0.IsAromatic || ca0.AtomicNumber != AtomicNumbers.C)
                            {
                                frags[69]++;
                                alogpfrag[i] = 69;
                            }
                            else
                            {
                                frags[66]++;
                                alogpfrag[i] = 66;
                            }
                        }
                        break;
                    case "SaaNH":
                    case "SsaaN":
                    case "SaaaN":
                    case "SaaNm":
                        // R...NH...R
                        frags[73]++;
                        alogpfrag[i] = 73;
                        break;
                    case "SssNH":
                        if (nAr == 2 && nAl == 0)
                        { // Ar2NH
                            frags[73]++;
                            alogpfrag[i] = 73;
                        }
                        else if (nAr == 1 && nAl == 1)
                        { // Ar-NH-Al
                            frags[70]++;
                            alogpfrag[i] = 70;

                        }
                        else if (nAr == 0 && nAl == 2)
                        { // Al2NH
                            frags[67]++;
                            alogpfrag[i] = 67;
                        }
                        break;
                    case "SsssN":
                        if ((nAr == 3 && nAl == 0) || (nAr == 2 && nAl == 1))
                        { 
                            // Ar3N &
                            // Ar2NAl
                            frags[73]++;
                            alogpfrag[i] = 73;
                        }
                        else if (nAr == 1 && nAl == 2)
                        {
                            frags[71]++;
                            alogpfrag[i] = 71;
                        }
                        else if (nAr == 0 && nAl == 3)
                        {
                            frags[68]++;
                            alogpfrag[i] = 68;
                        }
                        break;
                    case "SaaN":
                        {
                            frags[75]++;
                            alogpfrag[i] = 75;
                        }
                        break;
                    case "SdssNp":
                    case "SddsN":
                        {
                            int haveSsOm = 0;
                            int haveSdO = 0;
                            bool ar = false;

                            foreach (var nbor in nbors)
                            {
                                switch (fragment[atomContainer.Atoms.IndexOf(nbor)])
                                {
                                    case "SsOm":
                                        haveSsOm++;
                                        break;
                                    case "SdO":
                                        haveSdO++;
                                        break;
                                    default:
                                        if (nbor.IsAromatic)
                                        {
                                            ar = true;
                                        }
                                        break;
                                }
                            }

                            bool isNitro = haveSdO == 2 || (haveSsOm >= 1 && haveSdO >= 1);

                            if (isNitro && ar)
                            {
                                frags[76]++;
                                alogpfrag[i] = 76;
                            }
                            else if (isNitro && !ar)
                            {
                                frags[77]++;
                                alogpfrag[i] = 77;
                            }
                            else
                            {
                                frags[79]++;
                                alogpfrag[i] = 79;
                            }
                        }
                        break;
                    case "StN":
                        {
                            var ca0 = nbors[0];
                            if (ca0.AtomicNumber == AtomicNumbers.C)
                            { // R#N
                                frags[74]++;
                                alogpfrag[i] = 74;
                            }
                        }
                        break;
                    case "SdsN":
                        // test for RO-NO
                        {
                            var ca0 = nbors[0];
                            var ca1 = nbors[1];
                            if (ca0.AtomicNumber == AtomicNumbers.O
                             && ca1.AtomicNumber == AtomicNumbers.O)
                            {
                                frags[76]++;
                                alogpfrag[i] = 76;
                                return;
                            }
                        }
                        goto case "SdNH";
                    case "SdNH":
                        {
                            bool flag1 = false;
                            bool flag2 = false;

                            foreach (var a in nbors)
                            {
                                if (a.AtomicNumber == AtomicNumbers.H)
                                    continue;

                                if (atomContainer.GetBond(ai, a).Order == BondOrder.Double)
                                {
                                    if (a.AtomicNumber == AtomicNumbers.C)
                                    {
                                        frags[74]++;
                                        alogpfrag[i] = 74;
                                        return;
                                    }
                                    else
                                    {
                                        flag1 = true;
                                    }
                                }
                                else
                                {
                                    if (a.AtomicNumber != AtomicNumbers.C
                                     || a.IsAromatic)
                                    {
                                        flag2 = true;
                                    }
                                }

                                if (flag1 && flag2)
                                { // X-N=X or Ar-N=X
                                    frags[78]++;
                                    alogpfrag[i] = 78;
                                    return;
                                }
                                else
                                {
                                    //Debug.WriteLine("missing group: R-N=X");
                                }
                            }
                        }
                        break;
                    default:
                        if (fragment[i].IndexOf('p') > -1)
                        {
                            frags[79]++;
                            alogpfrag[i] = 79;
                        }
                        break;

                        // TODO add code for R--N(--R)--O
                        // first need to have program correctly read in structures with this
                        // fragment (pyridine-n-oxides)
                }
            }

            private static string CountToHybrid(IEnumerable<IBond> bonds)
            {
                int doublebondcount = 0;
                int triplebondcount = 0;

                foreach (var bond in bonds)
                {
                    var bj = bond;
                    if (bj.Order == BondOrder.Double)
                    {
                        doublebondcount++;
                    }
                    else if (bj.Order == BondOrder.Triple)
                    {
                        triplebondcount++;
                    }
                }

                if (doublebondcount == 0 && triplebondcount == 0)
                {
                    return "sp3";
                }
                else if (doublebondcount == 1)
                {
                    return "sp2";
                }
                else if (doublebondcount == 2 || triplebondcount == 1)
                {
                    return "sp";
                }

                return "";
            }

            private void SetCHybrid(int i, IAtom ca0, string hybrid, int oxNum, int start)
            {
                if (ca0.AtomicNumber == AtomicNumbers.C)
                {
                    switch (hybrid)
                    {
                        case "sp3":
                            switch (oxNum)
                            {
                                case 1:
                                    frags[start]++;
                                    alogpfrag[i] = start;
                                    break;
                                case 2:
                                    frags[start + 1]++;
                                    alogpfrag[i] = start + 1;
                                    break;
                                case 3:
                                    frags[start + 2]++;
                                    alogpfrag[i] = start + 2;
                                    break;
                                case 4:
                                    frags[start + 4]++;
                                    alogpfrag[i] = start + 4;
                                    break;
                            }
                            break;
                        case "sp2":
                            switch (oxNum)
                            {
                                case 1:
                                    frags[start + 3]++;
                                    alogpfrag[i] = start + 3;
                                    break;
                                default:
                                    if (oxNum > 1)
                                    {
                                        frags[start + 4]++;
                                        alogpfrag[i] = start + 4;
                                        break;
                                    }
                                    break;
                            }
                            break;
                        case "sp":
                            if (oxNum > 1)
                            {
                                frags[start + 4]++;
                                alogpfrag[i] = start + 4;
                                break;
                            }
                            break;
                    }
                }
                else
                {
                    frags[start + 4]++;
                    alogpfrag[i] = start + 4;
                }
            }

            private void CalcGroup081_to_085(int i)
            {
                switch (fragment[i])
                {
                    case "SsF":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ca0 = (IAtom)ca.ElementAt(0);
                        var bonds = atomContainer.GetConnectedBonds(ca0);
                        var hybrid = CountToHybrid(bonds);
                        var ca2 = atomContainer.GetConnectedAtoms(ca0);
                        int oxNum = 0;

                        foreach (var a2 in ca2)
                        {
                            var ca2j = a2;

                            if (IsHetero(ca2j))
                                oxNum += atomContainer.GetBond(ca0, ca2j).Order.Numeric();
                        }

                        SetCHybrid(i, ca0, hybrid, oxNum, 81);
                        break;
                }
            }

            private void CalcGroup086_to_090(int i)
            {
                switch (fragment[i])
                {
                    case "SsCl":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ca0 = (IAtom)ca.ElementAt(0);

                        var bonds = atomContainer.GetConnectedBonds(ca0);
                        var hybrid = CountToHybrid(bonds);
                        var ca2 = atomContainer.GetConnectedAtoms(ca0);

                        int oxNum = 0;
                        foreach (var a2 in ca2)
                        {
                            var ca2j = a2;
                            string s = ca2j.Symbol;

                            if (ap.GetNormalizedElectronegativity(s) > 1)
                            {
                                // // F,O,Cl,Br,N
                                oxNum += (int)BondManipulator.DestroyBondOrder(atomContainer.GetBond(ca0, ca2j).Order);
                            }
                        }

                        SetCHybrid(i, ca0, hybrid, oxNum, 86);
                        break;
                }
            }

            private void CalcGroup091_to_095(int i)
            {
                switch (fragment[i])
                {
                    case "SsBr":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ca0 = (IAtom)ca.ElementAt(0);

                        var bonds = atomContainer.GetConnectedBonds(ca0);
                        var hybrid = CountToHybrid(bonds);
                        var ca2 = atomContainer.GetConnectedAtoms(ca0);

                        int oxNum = 0;
                        foreach (var a2 in ca2)
                        {
                            var ca2j = a2;

                            // // F,O,Cl,Br,N
                            if (ap.GetNormalizedElectronegativity(ca2j.Symbol) > 1)
                            {
                                oxNum += (int)BondManipulator.DestroyBondOrder(atomContainer.GetBond(ca0, ca2j).Order);
                            }
                        }

                        SetCHybrid(i, ca0, hybrid, oxNum, 91);
                        break;
                }
            }

            private void CalcGroup096_to_100(int i)
            {
                switch (fragment[i])
                {
                    case "SsI":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ca0 = ca.ElementAt(0);

                        var bonds = atomContainer.GetConnectedBonds(ca0);
                        var hybrid = CountToHybrid(bonds);
                        var ca2 = atomContainer.GetConnectedAtoms(ca0);

                        int oxNum = 0;
                        foreach (var a2 in ca2)
                        {
                            var ca2j = a2;

                            // // F,O,Cl,Br,N
                            if (ap.GetNormalizedElectronegativity(ca2j.Symbol) > 1)
                            {
                                oxNum += (int)BondManipulator.DestroyBondOrder(atomContainer.GetBond(ca0, ca2j).Order);
                            }
                        }

                        SetCHybrid(i, ca0, hybrid, oxNum, 96);
                        break;
                }
            }

            private void CalcGroup101_to_104(int i)
            {
                var ai = atomContainer.Atoms[i];

                if (ai.FormalCharge == -1
                 || ai.FormalCharge == 0 && IsBondedToHydrogenOnly(ai))
                {
                    switch (ai.AtomicNumber)
                    {
                        case AtomicNumbers.F:
                            frags[101]++;
                            alogpfrag[i] = 101;
                            break;
                        case AtomicNumbers.Cl:
                            frags[102]++;
                            alogpfrag[i] = 102;
                            break;
                        case AtomicNumbers.Br:
                            frags[103]++;
                            alogpfrag[i] = 103;
                            break;
                        case AtomicNumbers.I:
                            frags[104]++;
                            alogpfrag[i] = 104;
                            break;
                    }
                }
            }

            private static bool IsBondedToHydrogenOnly(IAtom ai)
            {
                return ai.Bonds.Count == 0 && ai.ImplicitHydrogenCount == 1 
                    || ai.Bonds.Count == 1 && ai.Bonds[0].GetOther(ai).AtomicNumber == AtomicNumbers.H;
            }

            private void CalcGroup106(int i)
            {
                // S in SH
                switch (fragment[i])
                {
                    case "SsSH":
                        frags[106]++;
                        alogpfrag[i] = 106;
                        int htype = 50;
                        frags[htype]++;
                        var atom = atomContainer.Atoms[i];
                        foreach (var bond in atom.Bonds)
                        {
                            var nbor = bond.GetOther(atom);
                            if (nbor.AtomicNumber == AtomicNumbers.H)
                                alogpfrag[nbor.Index] = htype;
                        }
                        break;
                }
            }

            private void CalcGroup107(int i)
            {
                // S in R2S, RS-SR
                // R = any group linked through C

                // In ALOGP, for malathion PSC is consider to have group 107 (even
                // though has P instead of R)

                // for lack of fragment, use this fragment for SaaS

                switch (fragment[i])
                {
                    case "SssS":
                    case "SaaS":
                        frags[107]++;
                        alogpfrag[i] = 107;
                        break;
                }
            }

            private void CalcGroup108(int i)
            {
                // S in R=S
                // In ALOGP, for malathion P=S is consider to have group 108 (even
                // though has P instead of R)
                switch (fragment[i])
                {
                    case "SdS":
                        frags[108]++;
                        alogpfrag[i] = 108;
                        break;
                }
            }

            private void CalcGroup109(int i)
            {
                // for now S in O-S(=O)-O is assigned to this group
                // (it doesn't check which atoms are singly bonded to S
                switch (fragment[i])
                {
                    case "SdssS":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ai = atomContainer.Atoms[i];
                        int sdOCount = 0;
                        int ssCCount = 0;

                        foreach (var a in ca)
                        {
                            if (atomContainer.GetBond(ai, a).Order == BondOrder.Single)
                            {
                                if (a.AtomicNumber.Equals(AtomicNumbers.C))
                                {
                                    ssCCount++;
                                }
                            }
                            else if (atomContainer.GetBond(ai, a).Order == BondOrder.Double)
                            {
                                if (a.AtomicNumber.Equals(AtomicNumbers.O))
                                {
                                    sdOCount++;
                                }
                            }
                        }
                        if (sdOCount == 1)
                        { // for now dont check if ssCCount==2
                            frags[109]++;
                            alogpfrag[i] = 109;
                        }
                        break;
                }
            }

            private void CalcGroup110(int i)
            {
                switch (fragment[i])
                {
                    case "SddssS":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ai = atomContainer.Atoms[i];
                        int sdOCount = 0;
                        int ssCCount = 0;

                        foreach (var a in ca)
                        {
                            if (atomContainer.GetBond(ai, a).Order == BondOrder.Single)
                            {
                                if (a.AtomicNumber.Equals(AtomicNumbers.C))
                                {
                                    ssCCount++;
                                }
                            }
                            else if (atomContainer.GetBond(ai, a).Order == BondOrder.Double)
                            {
                                if (a.AtomicNumber.Equals(AtomicNumbers.O))
                                {
                                    sdOCount++;
                                }
                            }
                        }
                        if (sdOCount == 2)
                        { 
                            // for now dont check if ssCCount==2
                            frags[110]++;
                            alogpfrag[i] = 110;
                        }
                        break;
                }
            }

            private void CalcGroup111(int i)
            {
                switch (fragment[i])
                {
                    case "SssssSi":
                        frags[111]++;
                        alogpfrag[i] = 111;
                        break;
                }
            }

            private void CalcGroup112(int i)
            {
                switch (fragment[i])
                {
                    case "SsssB":
                    case "SssBm":
                        frags[112]++;
                        alogpfrag[i] = 112;
                        break;
                }
            }

            private void CalcGroup115(int i)
            {
                switch (fragment[i])
                {
                    case "SssssPp":
                        frags[115]++;
                        alogpfrag[i] = 115;
                        break;
                }
            }

            private void CalcGroup116_117_120(int i)
            {
                // S in R=S
                switch (fragment[i])
                {
                    case "SdsssP":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ai = atomContainer.Atoms[i];

                        int xCount = 0;
                        int rCount = 0;
                        bool pdX = false;

                        foreach (var a in ca)
                        {
                            switch (atomContainer.GetBond(ai, a).Order)
                            {
                                case BondOrder.Single:
                                    if (a.AtomicNumber == AtomicNumbers.C)
                                    {
                                        rCount++;
                                    }
                                    else
                                    {
                                        xCount++;
                                    }
                                    break;
                                case BondOrder.Double:
                                    if (a.AtomicNumber != AtomicNumbers.C)
                                    {
                                        pdX = true;
                                    }
                                    break;
                            }
                        }

                        if (pdX)
                        {
                            if (rCount == 3)
                            {
                                frags[116]++;
                                alogpfrag[i] = 116;
                            }
                            else if (xCount == 3)
                            {
                                frags[117]++;
                                alogpfrag[i] = 117;
                            }
                            else if (xCount == 2 && rCount == 1)
                            {
                                frags[120]++;
                                alogpfrag[i] = 120;
                            }
                        }
                        break;
                }
            }

            private void CalcGroup118_119(int i)
            {
                switch (fragment[i])
                {
                    case "SsssP":
                        var ca = atomContainer.GetConnectedAtoms(atomContainer.Atoms[i]);
                        var ai = atomContainer.Atoms[i];
                        int xCount = 0;
                        int rCount = 0;

                        foreach (var a in ca)
                        {
                            if (atomContainer.GetBond(ai, a).Order == BondOrder.Single)
                            {
                                if (a.AtomicNumber.Equals(AtomicNumbers.C))
                                {
                                    rCount++;
                                }
                                else
                                {
                                    xCount++;
                                }
                            }
                        }

                        if (xCount == 3)
                        {
                            frags[118]++;
                            alogpfrag[i] = 118;
                        }
                        else if (rCount == 3)
                        {
                            frags[119]++;
                            alogpfrag[i] = 119;
                        }
                        break;
                }
            }

            private static bool InSameAromaticRing(IAtomContainer atomContainer, IAtom atom1, IAtom atom2, IRingSet rs)
            {
                bool sameRing = false;

                for (int i = 0; i <= rs.Count - 1; i++)
                {
                    var r = rs[i];

                    if (!r.IsAromatic)
                        continue;

                    // ArrayList al=new ArrayList();

                    bool haveOne = false;
                    bool haveTwo = false;

                    for (int j = 0; j <= r.Atoms.Count - 1; j++)
                    {
                        if (atomContainer.Atoms.IndexOf(r.Atoms[j]) == atomContainer.Atoms.IndexOf(atom1))
                            haveOne = true;
                        if (atomContainer.Atoms.IndexOf(r.Atoms[j]) == atomContainer.Atoms.IndexOf(atom2))
                            haveTwo = true;
                    }

                    if (haveOne && haveTwo)
                    {
                        sameRing = true;
                        return sameRing;
                    }

                } // end ring for loop

                return sameRing;
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
