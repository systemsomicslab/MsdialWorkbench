/* Copyright (C) 2006-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Primitives;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NCDK.QSAR.Descriptors.Atomic
{
    internal static class AtomicHOSEDescriptor
    {
        /// <summary>
        /// Looking if the Atom belongs to the halogen family.
        /// </summary>
        /// <param name="atom">The IAtom</param>
        /// <returns>True, if it belongs</returns>
        internal static bool FamilyHalogen(IAtom atom)
        {
            switch (atom.AtomicNumber)
            {
                case AtomicNumbers.F:
                case AtomicNumbers.Cl:
                case AtomicNumbers.Br:
                case AtomicNumbers.I:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary> Maximum spheres to use by the HoseCode model.</summary>
        const int maxSpheresToUse = 10;

        /// <summary>
        /// Class defining the database containing the relation between the energy for ionizing and the HOSEcode
        /// fingerprints
        /// </summary>
        // @author Miguel Rojas
        public static IReadOnlyDictionary<string, double> MakeHOSEVSEnergy(string path)
        {
            using (var insr = new StreamReader(ResourceLoader.GetAsStream(path)))
            {
                return ExtractAttributes(insr);
            }
        }

        /// <summary>
        /// extract from the db the ionization energy.
        /// </summary>
        /// <param name="container">The IAtomContainer</param>
        /// <param name="atom">The IAtom</param>
        /// <returns>The energy value</returns>
        public static double ExtractIP(IAtom atom, IAtomContainer container,
             IReadOnlyDictionary<string, double> hoseVSenergy,
             IReadOnlyDictionary<string, double> hoseVSenergyS)
        {
            if (!FamilyHalogen(atom))
                return 0;

            var hcg = new HOSECodeGenerator();
            //Check starting from the exact sphere hose code and maximal a value of 10
            int exactSphere = 0;
            string hoseCode = "";
            for (int spheres = maxSpheresToUse; spheres > 0; spheres--)
            {
                hcg.GetSpheres(container, atom, spheres, true);
                var atoms = hcg.GetNodesInSphere(spheres);
                if (atoms.Any())
                {
                    exactSphere = spheres;
                    hoseCode = hcg.GetHOSECode(container, atom, spheres, true);
                    if (hoseVSenergy.ContainsKey(hoseCode))
                    {
                        return hoseVSenergy[hoseCode];
                    }
                    if (hoseVSenergyS.ContainsKey(hoseCode))
                    {
                        return hoseVSenergyS[hoseCode];
                    }
                    break;
                }
            }
            //Check starting from the rings bigger and smaller
            //TODO:IP: Better application
            for (int i = 0; i < 3; i++)
            { // two rings
                for (int plusMinus = 0; plusMinus < 2; plusMinus++)
                { // plus==bigger, minus==smaller
                    int sign = -1;
                    if (plusMinus == 1) sign = 1;

                    var st = Strings.Tokenize(hoseCode, '(', ')', '/').GetEnumerator();
                    var hoseCodeBuffer = new StringBuilder();
                    int sum = exactSphere + sign * (i + 1);
                    for (int k = 0; k < sum; k++)
                    {
                        if (st.MoveNext())
                        {
                            string partcode = st.Current;
                            hoseCodeBuffer.Append(partcode);
                        }
                        switch (k)
                        {
                            case 0:
                                hoseCodeBuffer.Append('(');
                                break;
                            case 3:
                                hoseCodeBuffer.Append(')');
                                break;
                            default:
                                hoseCodeBuffer.Append('/');
                                break;
                        }
                    }
                    string hoseCodeBU = hoseCodeBuffer.ToString();

                    if (hoseVSenergyS.ContainsKey(hoseCodeBU))
                    {
                        return hoseVSenergyS[hoseCodeBU];
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Extract the Hose code and energy
        /// </summary>
        /// <param name="input">The BufferedReader</param>
        /// <returns>HashMap with the Hose vs energy attributes</returns>
        internal static Dictionary<string, double> ExtractAttributes(TextReader input)
        {
            var hoseVSenergy = new Dictionary<string, double>();
            string line;
            while ((line = input.ReadLine()) != null)
            {
                if (line.StartsWithChar('#'))
                    continue;
                var values = ExtractInfo(line);
                if (values[1].Length == 0)
                    continue;
                hoseVSenergy[values[0]] = double.Parse(values[1], NumberFormatInfo.InvariantInfo);
            }
            return hoseVSenergy;
        }

        /// <summary>
        /// Extract the information from a line which contains HOSE_ID &amp; energy.
        /// </summary>
        /// <param name="str">string with the information</param>
        /// <returns>List with string = HOSECode and string = energy</returns>
        private static List<string> ExtractInfo(string str)
        {
            int beg = 0;
            int end = 0;
            int len = str.Length;
            var parts = new List<string>();
            while (end < len && !char.IsWhiteSpace(str[end]))
                end++;
            parts.Add(str.Substring(beg, end - beg));
            while (end < len && char.IsWhiteSpace(str[end]))
                end++;
            beg = end;
            while (end < len && !char.IsWhiteSpace(str[end]))
                end++;
            parts.Add(str.Substring(beg, end - beg));
            return parts;
        }
    }

    /// <summary>
    /// This class returns the ionization potential of an atom containing lone
    /// pair electrons. It is
    /// based on a decision tree which is extracted from Weka(J48) from
    /// experimental values. Up to now is only possible predict for
    /// Cl,Br,I,N,P,O,S Atoms and they are not belong to
    /// conjugated system or not adjacent to an double bond.
    /// </summary>
    /// <remarks>
    /// Parameters for this descriptor:
    /// <list type="table">
    /// <listheader>
    ///   <term>Name</term>
    ///   <term>Default</term>
    ///   <term>Description</term>
    /// </listheader>
    /// <item>
    ///   <term></term>
    ///   <term></term>
    ///   <term>no parameters</term>
    /// </item>
    /// </list>
    /// </remarks>
    // @author       Miguel Rojas
    // @cdk.created  2006-05-26
    // @cdk.module   qsaratomic
    // @cdk.dictref  qsar-descriptors:ionizationPotential
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#ionizationPotential")]
    public class IPAtomicHOSEDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedContainer;

        public IPAtomicHOSEDescriptor(IAtomContainer container)
        {
            this.container = container;

            container = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
            CDK.LonePairElectronChecker.Saturate(container);
            this.clonedContainer = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.AtomicHOSE = value;
            }

            [DescriptorResultProperty("ipAtomicHOSE")]
            public double AtomicHOSE { get; private set; }

            public double Value => AtomicHOSE;
        }

        static readonly IReadOnlyDictionary<string, double> hoseVSenergy = AtomicHOSEDescriptor.MakeHOSEVSEnergy("NCDK.QSAR.Descriptors.Atomic.Data.X_IP_HOSE.db");
        static readonly IReadOnlyDictionary<string, double> hoseVSenergyS = AtomicHOSEDescriptor.MakeHOSEVSEnergy("NCDK.QSAR.Descriptors.Atomic.Data.X_IP_HOSE_S.db");

        /// <summary>
        /// This method calculates the ionization potential of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> to ionize.</param>
        /// <returns>The ionization potential. Not possible the ionization.</returns>
        public Result Calculate(IAtom atom)
        {
            var index = container.Atoms.IndexOf(atom);
            var ca = clonedContainer.Atoms[index];
            var value = AtomicHOSEDescriptor.ExtractIP(ca, clonedContainer, hoseVSenergy, hoseVSenergyS);
            return new Result(value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }

    /// <summary>
    /// This class returns the proton affinity of an atom containing.
    /// </summary>
    // @author       Miguel Rojas
    // @cdk.created  2006-05-26
    // @cdk.module   qsaratomic
    // @cdk.dictref  qsar-descriptors:protonaffinity
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#ionizationPotential")]
    public partial class ProtonAffinityHOSEDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        private IAtomContainer container;
        private IAtomContainer clonedContainer;

        public ProtonAffinityHOSEDescriptor(IAtomContainer container)
        {
            this.container = container;

            container = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
            CDK.LonePairElectronChecker.Saturate(container);

            this.clonedContainer = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.AtomicHOSE = value;
            }

            [DescriptorResultProperty("protonAffiHOSE")]
            public double AtomicHOSE { get; private set; }

            public double Value => AtomicHOSE;
        }

        static readonly IReadOnlyDictionary<string, double> hoseVSenergy = AtomicHOSEDescriptor.MakeHOSEVSEnergy("NCDK.QSAR.Descriptors.Atomic.Data.X_AffiProton_HOSE.db");
        static readonly IReadOnlyDictionary<string, double> hoseVSenergyS = AtomicHOSEDescriptor.MakeHOSEVSEnergy("NCDK.QSAR.Descriptors.Atomic.Data.X_AffiProton_HOSE_S.db");

        /// <summary>
        /// This method calculates the ionization potential of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> to ionize.</param>
        /// <returns>The ionization potential. Not possible the ionization.</returns>
        public Result Calculate(IAtom atom)
        {
            atom = clonedContainer.Atoms[container.Atoms.IndexOf(atom)];
            var value = AtomicHOSEDescriptor.ExtractIP(atom, clonedContainer, hoseVSenergy, hoseVSenergyS);
            return new Result(value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
