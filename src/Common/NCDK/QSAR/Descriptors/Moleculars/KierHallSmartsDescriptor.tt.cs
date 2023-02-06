



/* Copyright (C) 2008 Rajarshi Guha  <rajarshi@users.sourceforge.net>
 *
 *  Contact: rajarshi@users.sourceforge.net
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

using NCDK.Config.Fragments;
using NCDK.SMARTS;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// A fragment count descriptor that uses e-state fragments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Traditionally the e-state descriptors identify the relevant fragments and
    /// then evaluate the actual e-state value. However it has been
    /// <see href="http://www.mdpi.org/molecules/papers/91201004.pdf">shown</see> in <token>cdk-cite-BUTINA2004</token>
    /// that simply using the <i>counts</i> of the e-state fragments can lead to QSAR models
    /// that exhibit similar performance to those built using the actual e-state indices.
    /// </para>
    /// <para>
    /// Atom typing and aromaticity perception should be performed prior to calling this
    /// descriptor. The atom type definitions are taken from <token>cdk-cite-HALL1995</token>.
    /// The SMARTS definitions were obtained from <see href="http://www.rdkit.org">RDKit</see>.
    /// </para>
    /// <para>
    /// The descriptor returns an integer array result of 79 values with the
    /// names (see <see href="http://www.edusoft-lc.com/molconn/manuals/350/appV.html">here</see>
    /// for the corresponding chemical groups).
    /// </para></remarks>
    // @author Rajarshi Guha
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:kierHallSmarts
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#kierHallSmarts")]
    public class KierHallSmartsDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public KierHallSmartsDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(IReadOnlyList<int> values)
            {
                this.Values = values;
            }

            /// <summary>
            /// Number of "[LiD1]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sLi")]
            public int KHSsLi => Values[0];
            /// <summary>
            /// Number of "[BeD2](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssBe")]
            public int KHSssBe => Values[1];
            /// <summary>
            /// Number of "[BeD4](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssBe")]
            public int KHSssssBe => Values[2];
            /// <summary>
            /// Number of "[BD2H](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssBH")]
            public int KHSssBH => Values[3];
            /// <summary>
            /// Number of "[BD3](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssB")]
            public int KHSsssB => Values[4];
            /// <summary>
            /// Number of "[BD4](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssB")]
            public int KHSssssB => Values[5];
            /// <summary>
            /// Number of "[CD1H3]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sCH3")]
            public int KHSsCH3 => Values[6];
            /// <summary>
            /// Number of "[CD1H2]=*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dCH2")]
            public int KHSdCH2 => Values[7];
            /// <summary>
            /// Number of "[CD2H2](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssCH2")]
            public int KHSssCH2 => Values[8];
            /// <summary>
            /// Number of "[CD1H]#*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.tCH")]
            public int KHStCH => Values[9];
            /// <summary>
            /// Number of "[CD2H](=*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dsCH")]
            public int KHSdsCH => Values[10];
            /// <summary>
            /// Number of "[C,c;D2H](:*):*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aaCH")]
            public int KHSaaCH => Values[11];
            /// <summary>
            /// Number of "[CD3H](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssCH")]
            public int KHSsssCH => Values[12];
            /// <summary>
            /// Number of "[CD2H0](=*)=*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ddC")]
            public int KHSddC => Values[13];
            /// <summary>
            /// Number of "[CD2H0](#*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.tsC")]
            public int KHStsC => Values[14];
            /// <summary>
            /// Number of "[CD3H0](=*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dssC")]
            public int KHSdssC => Values[15];
            /// <summary>
            /// Number of "[C,c;D3H0](:*)(:*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aasC")]
            public int KHSaasC => Values[16];
            /// <summary>
            /// Number of "[C,c;D3H0](:*)(:*):*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aaaC")]
            public int KHSaaaC => Values[17];
            /// <summary>
            /// Number of "[CD4H0](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssC")]
            public int KHSssssC => Values[18];
            /// <summary>
            /// Number of "[ND1H3]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sNH3")]
            public int KHSsNH3 => Values[19];
            /// <summary>
            /// Number of "[ND1H2]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sNH2")]
            public int KHSsNH2 => Values[20];
            /// <summary>
            /// Number of "[ND2H2](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssNH2")]
            public int KHSssNH2 => Values[21];
            /// <summary>
            /// Number of "[ND1H]=*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dNH")]
            public int KHSdNH => Values[22];
            /// <summary>
            /// Number of "[ND2H](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssNH")]
            public int KHSssNH => Values[23];
            /// <summary>
            /// Number of "[N,nD2H](:*):*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aaNH")]
            public int KHSaaNH => Values[24];
            /// <summary>
            /// Number of "[ND1H0]#*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.tN")]
            public int KHStN => Values[25];
            /// <summary>
            /// Number of "[ND3H](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssNH")]
            public int KHSsssNH => Values[26];
            /// <summary>
            /// Number of "[ND2H0](=*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dsN")]
            public int KHSdsN => Values[27];
            /// <summary>
            /// Number of "[N,nD2H0](:*):*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aaN")]
            public int KHSaaN => Values[28];
            /// <summary>
            /// Number of "[ND3H0](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssN")]
            public int KHSsssN => Values[29];
            /// <summary>
            /// Number of "[ND3H0](~[OD1H0])(~[OD1H0])-,:*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ddsN")]
            public int KHSddsN => Values[30];
            /// <summary>
            /// Number of "[N,nD3H0](:*)(:*)-,:*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aasN")]
            public int KHSaasN => Values[31];
            /// <summary>
            /// Number of "[ND4H0](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssN")]
            public int KHSssssN => Values[32];
            /// <summary>
            /// Number of "[OD1H]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sOH")]
            public int KHSsOH => Values[33];
            /// <summary>
            /// Number of "[OD1H0]=*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dO")]
            public int KHSdO => Values[34];
            /// <summary>
            /// Number of "[OD2H0](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssO")]
            public int KHSssO => Values[35];
            /// <summary>
            /// Number of "[O,oD2H0](:*):*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aaO")]
            public int KHSaaO => Values[36];
            /// <summary>
            /// Number of "[FD1]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sF")]
            public int KHSsF => Values[37];
            /// <summary>
            /// Number of "[SiD1H3]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sSiH3")]
            public int KHSsSiH3 => Values[38];
            /// <summary>
            /// Number of "[SiD2H2](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssSiH2")]
            public int KHSssSiH2 => Values[39];
            /// <summary>
            /// Number of "[SiD3H1](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssSiH")]
            public int KHSsssSiH => Values[40];
            /// <summary>
            /// Number of "[SiD4H0](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssSi")]
            public int KHSssssSi => Values[41];
            /// <summary>
            /// Number of "[PD1H2]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sPH2")]
            public int KHSsPH2 => Values[42];
            /// <summary>
            /// Number of "[PD2H1](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssPH")]
            public int KHSssPH => Values[43];
            /// <summary>
            /// Number of "[PD3H0](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssP")]
            public int KHSsssP => Values[44];
            /// <summary>
            /// Number of "[PD4H0](=*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dsssP")]
            public int KHSdsssP => Values[45];
            /// <summary>
            /// Number of "[PD5H0](-*)(-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssssP")]
            public int KHSsssssP => Values[46];
            /// <summary>
            /// Number of "[SD1H1]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sSH")]
            public int KHSsSH => Values[47];
            /// <summary>
            /// Number of "[SD1H0]=*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dS")]
            public int KHSdS => Values[48];
            /// <summary>
            /// Number of "[SD2H0](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssS")]
            public int KHSssS => Values[49];
            /// <summary>
            /// Number of "[S,sD2H0](:*):*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aaS")]
            public int KHSaaS => Values[50];
            /// <summary>
            /// Number of "[SD3H0](=*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dssS")]
            public int KHSdssS => Values[51];
            /// <summary>
            /// Number of "[SD4H0](~[OD1H0])(~[OD1H0])(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ddssS")]
            public int KHSddssS => Values[52];
            /// <summary>
            /// Number of "[ClD1]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sCl")]
            public int KHSsCl => Values[53];
            /// <summary>
            /// Number of "[GeD1H3](-*)" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sGeH3")]
            public int KHSsGeH3 => Values[54];
            /// <summary>
            /// Number of "[GeD2H2](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssGeH2")]
            public int KHSssGeH2 => Values[55];
            /// <summary>
            /// Number of "[GeD3H1](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssGeH")]
            public int KHSsssGeH => Values[56];
            /// <summary>
            /// Number of "[GeD4H0](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssGe")]
            public int KHSssssGe => Values[57];
            /// <summary>
            /// Number of "[AsD1H2]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sAsH2")]
            public int KHSsAsH2 => Values[58];
            /// <summary>
            /// Number of "[AsD2H1](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssAsH")]
            public int KHSssAsH => Values[59];
            /// <summary>
            /// Number of "[AsD3H0](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssAs")]
            public int KHSsssAs => Values[60];
            /// <summary>
            /// Number of "[AsD4H0](=*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssdAs")]
            public int KHSsssdAs => Values[61];
            /// <summary>
            /// Number of "[AsD5H0](-*)(-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssssAs")]
            public int KHSsssssAs => Values[62];
            /// <summary>
            /// Number of "[SeD1H1]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sSeH")]
            public int KHSsSeH => Values[63];
            /// <summary>
            /// Number of "[SeD1H0]=*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dSe")]
            public int KHSdSe => Values[64];
            /// <summary>
            /// Number of "[SeD2H0](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssSe")]
            public int KHSssSe => Values[65];
            /// <summary>
            /// Number of "[SeD2H0](:*):*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.aaSe")]
            public int KHSaaSe => Values[66];
            /// <summary>
            /// Number of "[SeD3H0](=*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.dssSe")]
            public int KHSdssSe => Values[67];
            /// <summary>
            /// Number of "[SeD4H0](=*)(=*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ddssSe")]
            public int KHSddssSe => Values[68];
            /// <summary>
            /// Number of "[BrD1]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sBr")]
            public int KHSsBr => Values[69];
            /// <summary>
            /// Number of "[SnD1H3]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sSnH3")]
            public int KHSsSnH3 => Values[70];
            /// <summary>
            /// Number of "[SnD2H2](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssSnH2")]
            public int KHSssSnH2 => Values[71];
            /// <summary>
            /// Number of "[SnD3H1](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssSnH")]
            public int KHSsssSnH => Values[72];
            /// <summary>
            /// Number of "[SnD4H0](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssSn")]
            public int KHSssssSn => Values[73];
            /// <summary>
            /// Number of "[ID1]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sI")]
            public int KHSsI => Values[74];
            /// <summary>
            /// Number of "[PbD1H3]-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sPbH3")]
            public int KHSsPbH3 => Values[75];
            /// <summary>
            /// Number of "[PbD2H2](-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssPbH2")]
            public int KHSssPbH2 => Values[76];
            /// <summary>
            /// Number of "[PbD3H1](-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.sssPbH")]
            public int KHSsssPbH => Values[77];
            /// <summary>
            /// Number of "[PbD4H0](-*)(-*)(-*)-*" fragments.
            /// </summary>
            [DescriptorResultProperty("khs.ssssPb")]
            public int KHSssssPb => Values[78];
            public new IReadOnlyList<int> Values { get; private set; }
        }

        private static readonly IReadOnlyList<SmartsPattern> SMARTS = EStateFragments.Patterns;

        /// <summary>
        /// This method calculates occurrences of the Kier &amp; Hall E-state fragments.
        /// </summary>
        /// <returns>Counts of the fragments</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = AtomContainerManipulator.RemoveHydrogens(container);
            
            var counts = new int[SMARTS.Count];
            SmartsPattern.Prepare(container);
            for (int i = 0; i < SMARTS.Count; i++)
                counts[i] = SMARTS[i].MatchAll(container).CountUnique();

            return new Result(counts);
        }
   
        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
