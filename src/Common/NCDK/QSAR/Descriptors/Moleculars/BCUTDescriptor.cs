/* Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
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

using MathNet.Numerics.LinearAlgebra;
using NCDK.Aromaticities;
using NCDK.Charges;
using NCDK.Common.Collections;
using NCDK.Graphs;
using NCDK.Graphs.Matrix;
using NCDK.Tools.Manipulator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Eigenvalue based descriptor noted for its utility in chemical diversity.
    /// Described by Pearlman et al. <token>cdk-cite-PEA99</token>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The descriptor is based on a weighted version of the Burden matrix <token>cdk-cite-BUR89</token>; <token>cdk-cite-BUR97</token>
    /// which takes into account both the connectivity as well as atomic
    /// properties of a molecule. The weights are a variety of atom properties placed along the
    /// diagonal of the Burden matrix. Currently three weighting schemes are employed
    /// <list type="bullet">
    /// <item>atomic weight</item>
    /// <item>partial charge (Gasteiger Marsilli)</item>
    /// <item>polarizability <token>cdk-cite-KJ81</token></item>
    /// </list> 
    /// </para>
    /// <para>By default, the descriptor will return the highest and lowest eigenvalues for the three
    /// classes of descriptor in a single ArrayList (in the order shown above). However it is also
    /// possible to supply a parameter list indicating how many of the highest and lowest eigenvalues
    /// (for each class of descriptor) are required. The descriptor works with the hydrogen depleted molecule.
    /// </para>
    /// <para>
    /// A side effect of specifying the number of highest and lowest eigenvalues is that it is possible
    /// to get two copies of all the eigenvalues. That is, if a molecule has 5 heavy atoms, then specifying
    /// the 5 highest eigenvalues returns all of them, and specifying the 5 lowest eigenvalues returns
    /// all of them, resulting in two copies of all the eigenvalues.
    /// </para>
    /// <para>
    /// Note that it is possible to
    /// specify an arbitrarily large number of eigenvalues to be returned. However if the number
    /// (i.e., nhigh or nlow) is larger than the number of heavy atoms, the remaining eignevalues
    /// will be NaN.
    /// </para>
    /// <para>
    /// Given the above description, if the aim is to gt all the eigenvalues for a molecule, you should
    /// set nlow to 0 and specify the number of heavy atoms (or some large number) for nhigh (or vice versa).
    /// </para>
    /// Returns an array of values in the following order
    /// <list type="bullet">
    /// <item>BCUTw-1l, BCUTw-2l ... - <i>nhigh</i> lowest atom weighted BCUTS</item>
    /// <item>BCUTw-1h, BCUTw-2h ... - <i>nlow</i> highest atom weighted BCUTS</item>
    /// <item>BCUTc-1l, BCUTc-2l ... - <i>nhigh</i> lowest partial charge weighted BCUTS</item>
    /// <item>BCUTc-1h, BCUTc-2h ... - <i>nlow</i> highest partial charge weighted BCUTS</item>
    /// <item>BCUTp-1l, BCUTp-2l ... - <i>nhigh</i> lowest polarizability weighted BCUTS</item>
    /// <item>BCUTp-1h, BCUTp-2h ... - <i>nlow</i> highest polarizability weighted BCUTS</item>
    /// </list>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2004-11-30
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:BCUT
    // @cdk.keyword BCUT
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#BCUT")]
    public class BCUTDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly bool checkAromaticity;

        public BCUTDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        public class Result : IDescriptorResult
        {
            private readonly List<string> keys;
            public IReadOnlyList<double> Values { get; private set; }

            public int NumberOfHigh { get; private set; }
            public int NumberOfLow { get; private set; }

            public Result(IReadOnlyList<double> values, int nhigh, int nlow)
            {
                if ((3 * nhigh + 3 * nlow) != values.Count)
                    throw new ArgumentException();
                this.Values = values;
                this.NumberOfHigh = nhigh;
                this.NumberOfLow = nlow;
                this.keys = MakeKeys(nhigh, nlow);
            }

            public Exception Exception { get; private set; }

            public Result(Exception e)
            {
                this.Exception = e;
            }

            static List<string> MakeKeys(int nhigh, int nlow)
            {
                string[] suffix = { "w", "c", "p" };
                var names = new List<string>(3 * nhigh + 3 * nlow);
                int counter = 0;
                foreach (var aSuffix in suffix)
                {
                    for (int i = 0; i < nhigh; i++)
                        names.Add($"BCUT{aSuffix}-{i + 1}l");
                    for (int i = 0; i < nlow; i++)
                        names.Add($"BCUT{aSuffix}-{i + 1}h");
                }
                return names;
            }

            public bool ContainsKey(string key) => keys.Contains(key);

            public bool TryGetValue(string key, out double value)
            {
                var i = keys.IndexOf(key);
                if (i == -1)
                {
                    value = default(double);
                    return false;
                }
                value = Values[i];
                return true;
            }

            public int Count => keys.Count;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return new KeyValuePair<string, object>(keys[i], Values[i]);
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool TryGetValue(string key, out object value)
            {
                if (TryGetValue(key, out double v))
                {
                    value = v;
                    return true;
                }
                value = null;
                return false;
            }

            public object this[string key]
            {
                get
                {
                    var i = keys.IndexOf(key);
                    if (i == -1)
                        throw new KeyNotFoundException();
                    return Values[i];
                }
            }

            public IEnumerable<string> Keys => keys;

            IEnumerable<object> IReadOnlyDictionary<string, object>.Values => Values.Cast<object>();
        }
       
        private static bool HasUndefined(double[][] m)
        {
            foreach (var aM in m)
            {
                foreach (var mm in aM)
                {
                    if (double.IsNaN(mm) || double.IsInfinity(mm))
                        return true;
                }
            }
            return false;
        }

        private static class BurdenMatrix
        {
            public static double[][] EvalMatrix(IAtomContainer atomContainer, double[] vsd)
            {
                IAtomContainer local = AtomContainerManipulator.RemoveHydrogens(atomContainer);

                int natom = local.Atoms.Count;
                double[][] matrix = Arrays.CreateJagged<double>(natom, natom);
                for (int i = 0; i < natom; i++)
                {
                    for (int j = 0; j < natom; j++)
                    {
                        matrix[i][j] = 0.0;
                    }
                }

                /* set the off diagonal entries */
                for (int i = 0; i < natom - 1; i++)
                {
                    for (int j = i + 1; j < natom; j++)
                    {
                        for (int k = 0; k < local.Bonds.Count; k++)
                        {
                            var bond = local.Bonds[k];
                            if (bond.Contains(local.Atoms[i]) && bond.Contains(local.Atoms[j]))
                            {
                                if (bond.IsAromatic)
                                    matrix[i][j] = 0.15;
                                else if (bond.Order == BondOrder.Single)
                                    matrix[i][j] = 0.1;
                                else if (bond.Order == BondOrder.Double)
                                    matrix[i][j] = 0.2;
                                else if (bond.Order == BondOrder.Triple) matrix[i][j] = 0.3;

                                if (local.GetConnectedBonds(local.Atoms[i]).Count() == 1 || local.GetConnectedBonds(local.Atoms[j]).Count() == 1)
                                {
                                    matrix[i][j] += 0.01;
                                }
                                matrix[j][i] = matrix[i][j];
                            }
                            else
                            {
                                matrix[i][j] = 0.001;
                                matrix[j][i] = 0.001;
                            }
                        }
                    }
                }

                /* set the diagonal entries */
                for (int i = 0; i < natom; i++)
                {
                    if (vsd != null)
                        matrix[i][i] = vsd[i];
                    else
                        matrix[i][i] = 0.0;
                }
                return (matrix);
            }
        }

        /// <summary>
        /// Calculates the three classes of BCUT descriptors.
        /// </summary>
        /// <returns>An ArrayList containing the descriptors. The default is to return
        /// all calculated eigenvalues of the Burden matrices in the order described
        /// above. If a parameter list was supplied, then only the specified number
        /// of highest and lowest eigenvalues (for each class of BCUT) will be returned.
        /// </returns>
        /// <param name="nhigh">The number of highest eigenvalue</param>         
        /// <param name="nlow">The number of lowest eigenvalue</param>
        public Result Calculate(IAtomContainer container, int nhigh = 1, int nlow = 1)
        {
            container = (IAtomContainer)container.Clone();

            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
            var hAdder = CDK.HydrogenAdder;
            hAdder.AddImplicitHydrogens(container);
            AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(container);
            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }

            try
            {
                return CalculateMain(container, nhigh, nlow);
            }
            catch (CDKException e)
            {
                return new Result(e);
            }
        }

        private static Result CalculateMain(IAtomContainer container, int nhigh, int nlow)
        {
            var iso = CDK.IsotopeFactory;
            int nheavy = 0;

            // find number of heavy atoms
            nheavy += container.Atoms.Count(atom => !atom.AtomicNumber.Equals(AtomicNumbers.H));
            if (nheavy == 0)
                throw new CDKException("No heavy atoms in the container");

            var diagvalue = new double[nheavy];

            // get atomic mass weighted BCUT
            try
            {
                var counter = 0;
                foreach (var atom in container.Atoms.Where(atom => !atom.AtomicNumber.Equals(AtomicNumbers.H)))
                {
                    diagvalue[counter] = iso.GetMajorIsotope(atom.AtomicNumber).ExactMass.Value;
                    counter++;
                }
            }
            catch (Exception e)
            {
                throw new CDKException($"Could not calculate weight: {e.Message}", e);
            }

            double[] eval1, eval2, eval3;
            {
                var burdenMatrix = BurdenMatrix.EvalMatrix(container, diagvalue);
                if (HasUndefined(burdenMatrix))
                    throw new CDKException("Burden matrix has undefined values");
                var matrix = Matrix<double>.Build.DenseOfColumnArrays(burdenMatrix);
                var eigenDecomposition = matrix.Evd().EigenValues;
                eval1 = eigenDecomposition.Select(n => n.Real).ToArray();
            }
            try
            {
                // get charge weighted BCUT
                CDK.LonePairElectronChecker.Saturate(container);
                var charges = new double[container.Atoms.Count];
                var peoe = new GasteigerMarsiliPartialCharges();
                peoe.AssignGasteigerMarsiliSigmaPartialCharges(container, true);
                for (int i = 0; i < container.Atoms.Count; i++)
                    charges[i] += container.Atoms[i].Charge.Value;
                for (int i = 0; i < container.Atoms.Count; i++)
                    container.Atoms[i].Charge = charges[i];
            }
            catch (Exception e)
            {
                throw new CDKException("Could not calculate partial charges: " + e.Message, e);
            }
            {
                var counter = 0;
                foreach (var atom in container.Atoms.Where(atom => !atom.AtomicNumber.Equals(AtomicNumbers.H)))
                    diagvalue[counter++] = atom.Charge.Value;
            }
            {
                var burdenMatrix = BurdenMatrix.EvalMatrix(container, diagvalue);
                if (HasUndefined(burdenMatrix))
                    throw new CDKException("Burden matrix has undefined values");
                var matrix = Matrix<double>.Build.DenseOfColumnArrays(burdenMatrix);
                var eigenDecomposition = matrix.Evd().EigenValues;
                eval2 = eigenDecomposition.Select(n => n.Real).ToArray();
            }

            var topoDistance = PathTools.ComputeFloydAPSP(AdjacencyMatrix.GetMatrix(container));

            // get polarizability weighted BCUT
            {
                var counter = 0;
                foreach (var atom in container.Atoms.Where(atom => !atom.AtomicNumber.Equals(AtomicNumbers.H)))
                    diagvalue[counter++] = Polarizability.CalculateGHEffectiveAtomPolarizability(container, atom, false, topoDistance);
            }
            {
                var burdenMatrix = BurdenMatrix.EvalMatrix(container, diagvalue);
                if (HasUndefined(burdenMatrix))
                    throw new CDKException("Burden matrix has undefined values");
                var matrix = Matrix<double>.Build.DenseOfColumnArrays(burdenMatrix);
                var eigenDecomposition = matrix.Evd().EigenValues;
                eval3 = eigenDecomposition.Select(n => n.Real).ToArray();
            }

            // return only the n highest & lowest eigenvalues
            int lnlow, lnhigh, enlow, enhigh;
            if (nlow > nheavy)
            {
                lnlow = nheavy;
                enlow = nlow - nheavy;
            }
            else
            {
                lnlow = nlow;
                enlow = 0;
            }

            if (nhigh > nheavy)
            {
                lnhigh = nheavy;
                enhigh = nhigh - nheavy;
            }
            else
            {
                lnhigh = nhigh;
                enhigh = 0;
            }

            var retval = new List<double>((lnlow + enlow + lnhigh + enhigh) * 3);

            for (int i = 0; i < lnlow; i++)
                retval.Add(eval1[i]);
            for (int i = 0; i < enlow; i++)
                retval.Add(double.NaN);
            for (int i = 0; i < lnhigh; i++)
                retval.Add(eval1[eval1.Length - i - 1]);
            for (int i = 0; i < enhigh; i++)
                retval.Add(double.NaN);

            for (int i = 0; i < lnlow; i++)
                retval.Add(eval2[i]);
            for (int i = 0; i < enlow; i++)
                retval.Add(double.NaN);
            for (int i = 0; i < lnhigh; i++)
                retval.Add(eval2[eval2.Length - i - 1]);
            for (int i = 0; i < enhigh; i++)
                retval.Add(double.NaN);

            for (int i = 0; i < lnlow; i++)
                retval.Add(eval3[i]);
            for (int i = 0; i < enlow; i++)
                retval.Add(double.NaN);
            for (int i = 0; i < lnhigh; i++)
                retval.Add(eval3[eval3.Length - i - 1]);
            for (int i = 0; i < enhigh; i++)
                retval.Add(double.NaN);

            return new Result(retval, nhigh, nlow);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
