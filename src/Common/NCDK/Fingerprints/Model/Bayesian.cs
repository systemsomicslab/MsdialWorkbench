/* $Revision$ $Author$ $Date$
 *
 * Copyright (c) 2015 Collaborative Drug Discovery, Inc. <alex@collaborativedrug.com>
 *
 * Implemented by Alex M. Clark, produced by Collaborative Drug Discovery, Inc.
 * Made available to the CDK community under the terms of the GNU LGPL.
 *
 *    http://collaborativedrug.com
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

using NCDK.Common.Collections;
using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NCDK.Fingerprints.Model
{
    /// <summary>
    /// Bayesian models using fingerprints: provides model creation, analysis, prediction and serialisation.
    /// </summary>
    /// <remarks>
    /// <para>Uses a variation of the classic Bayesian model, using a Laplacian correction, which sums log
    /// values of ratios rather than multiplying them together. This is an effective way to work with large
    /// numbers of fingerprints without running into extreme numerical precision issues, but it also means
    /// that the outgoing predictor is an arbitrary value rather than a probability, which introduces
    /// the need for an additional calibration step prior to interpretation.</para>
    /// 
    /// <para>For more information about the method, see:
    ///         J. Chem. Inf. Model, v.46, pp.1124-1133 (2006)
    ///         J. Biomol. Screen., v.10, pp.682-686 (2005)
    ///         Molec. Divers., v.10, pp.283-299 (2006)</para>
    /// 
    /// <para>Currently only the <see cref="CircularFingerprinter"/> fingerprints are supported (i.e. ECFP_n and FCFP_n).</para>
    /// 
    /// <para>Model building is done by selecting the fingerprinting method and folding size, then providing
    /// a series of molecules &amp; responses. Individual model contributions are kept around in order to
    /// produce the analysis data (e.g. the ROC curve), but is discarded during serialise/deserialise
    /// cycles.</para>
    /// 
    /// <para>Fingerprint "folding" is optional, but recommended, because it places an upper limit on the model
    /// size. If folding is not used (folding=0) then the entire 32-bits are used, which means that in the
    /// diabolical case, the number of Bayesian contributions that needs to be stored is 4 billion. In practice
    /// the improvement in predictivity tends to plateaux out at around 1024 bits, so values of 2048 or 4096
    /// are generally safe. Folding values must be integer powers of 2.</para>
    /// </remarks>
    // @author         am.clark
    // @cdk.created    2015-01-05
    // @cdk.keyword    fingerprint
    // @cdk.keyword    bayesian
    // @cdk.keyword    model
    // @cdk.module     standard
    public class Bayesian
    {
        /// <summary>
        /// Access to the fingerprint type, one of <see cref="CircularFingerprinterClass"/>.
        /// </summary>
        public CircularFingerprinterClass ClassType { get; private set; }

        /// <summary>
        /// Access to the fingerprint folding extent, either 0 (for none) or a power of 2.
        /// </summary>
        public int Folding { get; private set; } = 0;

        /// <summary>
        /// Whether stereochemistry should be re-perceived from 2D/3D
        /// coordinates. By default stereochemistry encoded as <see cref="IStereoElement{TFocus, TCarriers}"/>s
        /// are used.
        /// </summary>
        public bool PerceiveStereo { get; set; }

        // incoming hash codes: actual values, and subsumed values are {#active,#total}
        private int numActive = 0;

        protected IReadOnlyDictionary<int, int[]> InHash => inHash;
        private Dictionary<int, int[]> inHash = new Dictionary<int, int[]>();

        public IReadOnlyList<int[]> Training => training;
        private List<int[]> training = new List<int[]>();

        public IReadOnlyList<bool> Activity => activity;
        private List<bool> activity = new List<bool>();

        /// <summary>
        /// built model: contributions for each hash code
        /// </summary>
        public IReadOnlyDictionary<int, double> Contributions => contribs;
        private Dictionary<int, double> contribs = new Dictionary<int, double>();

        public double LowThreshold { get; private set; } = 0;
        public double HighThreshold { get; private set; } = 0;

        private double range = 0;

        /// <summary>
        /// cached to speed up scaling calibration
        /// </summary>
        private double invRange = 0;

        /// <summary>
        /// self-validation metrics: can optionally be calculated after a build
        /// </summary>
        public IReadOnlyList<double> Estimates => estimates;

        private double[] estimates = null;

        /// <summary>
        /// X-values that can be used to plot the ROC-curve.
        /// </summary>
        public IReadOnlyList<double> RocX => rocX;

        private double[] rocX = null;

        /// <summary>
        /// Y-values that can be used to plot the ROC-curve.
        /// </summary>
        public IReadOnlyList<double> RocY => rocY;

        private double[] rocY = null;

        /// <summary>
        /// a string description of the method used to create the ROC curve (e.g. "leave-one-out" or "five-fold").
        /// </summary>
        public string RocType { get; private set; } = null;

        /// <summary>
        /// the integral of the area-under-the-curve of the receiver-operator-characteristic. A value of 1
        /// means perfect recall, 0.5 is pretty much random.
        /// </summary>
        public double RocAUC { get; private set; } = double.NaN;

        /// <summary>
        /// the size of the training set, i.e. the total number of molecules used to create the model.
        /// </summary>
        public int TrainingSize { get; private set; } = 0;

        /// <summary>
        /// the number of actives in the training set that was used to create the model.
        /// </summary>
        /// this is serialised, while the actual training set is not
        public int TrainingActives { get; private set; } = 0;

        // optional text attributes (serialisable)
        private string noteTitle = null;
        private string noteOrigin = null;
        private string[] noteComments = null;

        private static readonly Regex PTN_HASHLINE = new Regex("^(-?\\d+)=([\\d\\.Ee-]+)", RegexOptions.Compiled);

        // ----------------- public methods -----------------

        /// <summary>
        /// Instantiate a Bayesian model with no data.
        /// </summary>
        /// <param name="classType">one of the CircularFingerprinter.CLASS_* constants</param>
        public Bayesian(CircularFingerprinterClass classType)
        {
            this.ClassType = classType;
        }

        /// <summary>
        /// Instantiate a Bayesian model with no data.
        /// </summary>
        /// <param name="classType">one of the <see cref="CircularFingerprinterClass"/> enum</param>
        /// <param name="folding">the maximum number of fingerprint bits, which must be a power of 2 (e.g. 1024, 2048) or 0 for no folding</param>
        public Bayesian(CircularFingerprinterClass classType, int folding)
        {
            this.ClassType = classType;
            this.Folding = folding;

            // make sure the folding is valid
            bool bad = false;
            if (folding > 0)
            {
                for (int f = folding; f > 0; f = f >> 1)
                {
                    if ((f & 1) == 1 && f != 1)
                    {
                        bad = true;
                        break;
                    }
                }
            }
            if (folding < 0 || bad)
                throw new ArithmeticException("Fingerprint folding " + folding + " invalid: must be 0 or power of 2.");
        }

        /// <summary>
        /// Appends a new row to the model source data, which consists of a molecule and whether or not it
        /// is considered active.
        /// </summary>
        /// <param name="mol">molecular structure, which must be non-blank</param>
        /// <param name="active">whether active or not</param>
        public void AddMolecule(IAtomContainer mol, bool active)
        {
            if (mol == null || mol.Atoms.Count == 0)
                throw new CDKException("Molecule cannot be blank or null.");

            var circ = new CircularFingerprinter(ClassType)
            {
                PerceiveStereo = this.PerceiveStereo
            };
            circ.Calculate(mol);

            // gather all of the (folded) fingerprints into a sorted set
            int AND_BITS = Folding - 1; // e.g. 1024/0x400 -> 1023/0x3FF: chop off higher order bits
            var hashset = new SortedSet<int>();
            for (int n = circ.FPCount - 1; n >= 0; n--)
            {
                int code = circ.GetFP(n).Hash;
                if (Folding > 0)
                    code &= AND_BITS;
                hashset.Add(code);
            }

            // convert the set into a sorted primitive array
            var hashes = new int[hashset.Count];
            int p = 0;
            foreach (var h in hashset)
                hashes[p++] = h;

            // record the processed information for model building purposes        
            if (active)
                numActive++;
            training.Add(hashes);
            activity.Add(active);
            foreach (var h in hashes)
            {
                if (!inHash.TryGetValue(h, out int[] stash))
                    stash = new int[] { 0, 0 };
                if (active) stash[0]++;
                stash[1]++;
                inHash[h] = stash;
            }
        }

        /// <summary>
        /// Performs that Bayesian model generation, using the {molecule:activity} pairs that have been submitted up to this
        /// point. Once this method has finished, the object can be used to generate predictions, validation data or to
        /// serialise for later use.
        /// </summary>
        public void Build()
        {
            TrainingSize = training.Count; // for posterity
            TrainingActives = numActive;

            contribs.Clear();

            // the primary model building step: go over all of the hash codes that were discovered during the molecule
            // contributing phase, and convert their ratios into "contributions", each of which is basically the log
            // value of a ratio

            var sz = training.Count;
            var invSz = 1.0 / sz;
            var P_AT = numActive * invSz;

            foreach (var hash in inHash.Keys)
            {
                var AT = inHash[hash];
                var A = AT[0];
                var T = AT[1];
                var Pcorr = (A + 1) / (T * P_AT + 1);
                var P = Math.Log(Pcorr);
                contribs[hash] = P;
            }

            // note thresholds and ranges, for subsequent use

            LowThreshold = double.PositiveInfinity;
            HighThreshold = double.NegativeInfinity;
            foreach (var fp in training)
            {
                double val = 0;
                foreach (var hash in fp)
                    val += contribs[hash];
                LowThreshold = Math.Min(LowThreshold, val);
                HighThreshold = Math.Max(HighThreshold, val);
            }
            range = HighThreshold - LowThreshold;
            invRange = range > 0 ? 1 / range : 0;
        }

        /// <summary>
        /// For a given molecule, determines its fingerprints and uses them to calculate a Bayesian prediction. Note that this
        /// value is unscaled, and so it only has relative meaning within the confines of the model, i.e. higher is more likely to
        /// be active.
        /// </summary>
        /// <param name="mol">molecular structure which cannot be blank or null</param>
        /// <returns>predictor value</returns>
        public double Predict(IAtomContainer mol)
        {
            if (mol == null || mol.Atoms.Count == 0)
                throw new CDKException("Molecule cannot be blank or null.");

            var circ = new CircularFingerprinter(ClassType)
            {
                PerceiveStereo = this.PerceiveStereo
            };
            circ.Calculate(mol);

            // gather all of the (folded) fingerprints (eliminating duplicates)
            int AND_BITS = Folding - 1; // e.g. 1024/0x400 -> 1023/0x3FF: chop off higher order bits
            var hashset = new HashSet<int>();
            for (int n = circ.FPCount - 1; n >= 0; n--)
            {
                int code = circ.GetFP(n).Hash;
                if (Folding > 0)
                    code &= AND_BITS;
                hashset.Add(code);
            }

            // sums the corresponding contributor for each hash code generated from the molecule; note that if the
            // molecule generates hash codes not originally in the model, they are discarded (i.e. 0 contribution)
            double val = 0;
            foreach (var h in hashset)
            {
                var c = contribs[h];
                val += c;
            }
            return val;
        }

        /// <summary>
        /// Converts a raw Bayesian prediction and transforms it into a probability-like range, i.e. most values within the domain
        /// are between 0..1, and assigning a cutoff of activie = scaled_prediction &gt; 0.5 is reasonable. The transform (scale/translation)
        /// is determined by the ROC-analysis, if any. The resulting value can be used as a probability by capping the values so that
        /// 0 ÅÖ p ÅÖ 1.
        /// </summary>
        /// <param name="pred">raw prediction, as provided by the Predict(..) method</param>
        /// <returns>scaled prediction</returns>
        public double ScalePredictor(double pred)
        {
            // special case: if there is no differentiation scale, it's either above or below (typically happens only with tiny models)
            if (range == 0)
                return pred >= HighThreshold ? 1 : 0;

            return (pred - LowThreshold) * invRange;
        }

        /// <summary>
        /// Produces an ROC validation set, using the inputs provided prior to the model building, using leave-one-out. Note that
        /// this should only be used for small datasets, since it is very thorough, and scales as O(N^2) relative to training set
        /// size.
        /// </summary>
        public void ValidateLeaveOneOut()
        {
            var sz = training.Count;
            estimates = new double[sz];
            for (int n = 0; n < sz; n++)
                estimates[n] = SingleLeaveOneOut(n);
            CalculateRoc();
            RocType = "leave-one-out";
        }

        /// <summary>
        /// Produces a ROC validation set by partitioning the inputs into 5 groups, and performing five separate 80% in/20% out
        /// model simulations. This is quite efficient, and takes approximately 5 times as long as building the original
        /// model: it should be used for larger datasets.
        /// </summary>
        public void ValidateFiveFold()
        {
            RocType = "five-fold";
            ValidateNfold(5);
        }

        /// <summary>
        /// Produces a ROC validation set by partitioning the inputs into 3 groups, and performing three separate 66% in/33% out
        /// model simulations. This is quite efficient, and takes approximately 3 times as long as building the original
        /// model: it should be used for larger datasets.
        /// </summary>
        public void ValidateThreeFold()
        {
            RocType = "three-fold";
            ValidateNfold(3);
        }

        /// <summary>
        /// Clears out the training set, to free up memory.
        /// </summary>
        public void ClearTraining()
        {
            training.Clear();
            activity.Clear();
        }

        /// <summary>
        /// the optional title used to describe the model.
        /// </summary>
        public string NoteTitle
        {
            get
            {
                return noteTitle;
            }

            set
            {
                if (value.IndexOf('\n') >= 0 || value.IndexOf('\t') >= 0)
                    throw new ArgumentException("Comments cannot contain newlines or tabs.");
                noteTitle = value;
            }
        }

        /// <summary>
        /// the optional description of the source for the model.
        /// </summary>
        public string NoteOrigin
        {
            get
            {
                return noteOrigin;
            }

            set
            {
                noteOrigin = value;
            }
        }

        /// <summary>
        /// the optional comments, which is a list of arbitrary text strings.
        /// </summary>
        public IReadOnlyList<string> NoteComments
        {
            get
            {
                return noteComments == null ? null : Arrays.CopyOf(noteComments, noteComments.Length);
            }

            set
            {
                if (value != null)
                    foreach (var comment in value)
                        if (comment.IndexOf('\n') >= 0 || comment.IndexOf('\t') >= 0)
                            throw new ArgumentException("Comments cannot contain newlines or tabs.");
                noteComments = value?.ToArray();
            }
        }

        private static string CToE(CircularFingerprinterClass cc)
        {
            switch (cc)
            {
                case CircularFingerprinterClass.ECFP0: return "ECFP0";
                case CircularFingerprinterClass.ECFP2: return "ECFP2";
                case CircularFingerprinterClass.ECFP4: return "ECFP4";
                case CircularFingerprinterClass.ECFP6: return "ECFP6";
                case CircularFingerprinterClass.FCFP0: return "FCFP0";
                case CircularFingerprinterClass.FCFP2: return "FCFP2";
                case CircularFingerprinterClass.FCFP4: return "FCFP4";
                case CircularFingerprinterClass.FCFP6: return "FCFP6";
                default: return "?";
            }
        }

        private static CircularFingerprinterClass EToC(string str)
        {
            switch (str)
            {
                case "ECFP0": return CircularFingerprinterClass.ECFP0;
                case "ECFP2": return CircularFingerprinterClass.ECFP2;
                case "ECFP4": return CircularFingerprinterClass.ECFP4;
                case "ECFP6": return CircularFingerprinterClass.ECFP6;
                case "FCFP0": return CircularFingerprinterClass.FCFP0;
                case "FCFP2": return CircularFingerprinterClass.FCFP2;
                case "FCFP4": return CircularFingerprinterClass.FCFP4;
                case "FCFP6": return CircularFingerprinterClass.FCFP6;
                default: return 0;
            }
        }

        /// <summary>
        /// Converts the current model into a serialised string representation. The serialised form omits the original data
        /// that was used to build the model, but otherwise contains all of the information necessary to recreate the model
        /// and use it to make predictions against new molecules. The format used is a concise text-based format that is
        /// easy to recognise by its prefix, and is reasonably efficient with regard to storage space.
        /// </summary>
        /// <returns>serialised model</returns>
        public string Serialise()
        {
            var buff = new StringBuilder();
            string fpname = CToE(ClassType);
            buff.Append("Bayesian!(" + fpname + "," + Folding + "," + LowThreshold + "," + HighThreshold + ")\n");

            // primary payload: the bit contributions
            var sorted = new SortedSet<int>();
            foreach (var hash in contribs.Keys)
                sorted.Add(hash);
            foreach (var hash in sorted)
            {
                double c = contribs[hash];
                buff.Append(hash + "=" + c + "\n");
            }

            // other information
            buff.Append("training:size=").Append(TrainingSize).Append('\n');
            buff.Append("training:actives=").Append(TrainingActives).Append('\n');

            if (!double.IsNaN(RocAUC))
                buff.Append("roc:auc=").Append(RocAUC).Append('\n');
            if (RocType != null)
                buff.Append("roc:type=").Append(RocType).Append('\n');
            if (RocX != null && RocY != null)
            {
                buff.Append("roc:x=");
                for (int n = 0; n < rocX.Length; n++)
                    buff.Append((n == 0 ? "" : ",") + RocX[n]);
                buff.Append('\n');

                buff.Append("roc:y=");
                for (int n = 0; n < rocY.Length; n++)
                    buff.Append((n == 0 ? "" : ",") + RocY[n]);
                buff.Append('\n');
            }

            if (noteTitle != null)
                buff.Append("note:title=").Append(noteTitle).Append('\n');
            if (noteOrigin != null)
                buff.Append("note:origin=").Append(noteOrigin).Append('\n');
            if (noteComments != null)
            {
                foreach (var comment in noteComments)
                {
                    buff.Append("note:comment=").Append(comment).Append('\n');
                }
            }

            buff.Append("!End\n");

            return buff.ToString();
        }

        /// <summary>
        /// Converts a given string into a Bayesian model instance, or throws an exception if it is not valid.
        /// </summary>
        /// <param name="str">string containing the serialised model</param>
        /// <returns>instantiated model that can be used for predictions</returns>
        public static Bayesian Deserialise(string str)
        {
            using (var rdr = new StringReader(str))
            {
                var model = Deserialise(rdr);
                return model;
            }
        }

        /// <summary>
        /// Reads the incoming stream and attempts to convert it into an instantiated model. The input most be compatible
        /// with the format used by the Serialise() method, otherwise an exception will be thrown.
        /// </summary>
        /// <param name="rdr">reader</param>
        /// <returns>instantiated model that can be used for predictions</returns>
        public static Bayesian Deserialise(TextReader rdr)
        {
            var line = rdr.ReadLine();
            if (line == null || !line.StartsWith("Bayesian!(", StringComparison.Ordinal) || !line.EndsWithChar(')'))
                throw new IOException("Not a serialised Bayesian model.");
            var bits = line.Substring(10, line.Length - 11).Split(',');
            if (bits.Length < 4)
                throw new IOException("Invalid header content");

            var classType = EToC(bits[0]);
            if (classType == 0)
                throw new IOException($"Unknown fingerprint type: {bits[0]}");

            int folding = int.Parse(bits[1], NumberFormatInfo.InvariantInfo);
            if (folding > 0)
            {
                for (int f = folding; f > 0; f = f >> 1)
                {
                    if ((f & 1) == 1 && f != 1)
                    {
                        folding = -1;
                        break;
                    }
                }
            }
            if (folding < 0)
                throw new IOException("Fingerprint folding " + bits[1] + " invalid: must be 0 or power of 2.");

            var model = new Bayesian(classType, folding)
            {
                LowThreshold = double.Parse(bits[2], NumberFormatInfo.InvariantInfo),
                HighThreshold = double.Parse(bits[3], NumberFormatInfo.InvariantInfo)
            };
            model.range = model.HighThreshold - model.LowThreshold;
            model.invRange = model.range > 0 ? 1 / model.range : 0;

            while (true)
            {
                line = rdr.ReadLine();
                if (line == null)
                    throw new IOException("Missing correct terminator line.");
                if (string.Equals(line, "!End", StringComparison.Ordinal))
                    break;

                var m = PTN_HASHLINE.Match(line);
                if (m.Success)
                {
                    int hash = int.Parse(m.Groups[1].Value, NumberFormatInfo.InvariantInfo);
                    double c = double.Parse(m.Groups[2].Value, NumberFormatInfo.InvariantInfo);
                    model.contribs[hash] = c;
                }
                else if (line.StartsWith("training:size=", StringComparison.Ordinal))
                {
                    try
                    {
                        model.TrainingSize = int.Parse(line.Substring(14), NumberFormatInfo.InvariantInfo);
                    }
                    catch (FormatException)
                    {
                        throw new IOException("Invalid training info line: " + line);
                    }
                }
                else if (line.StartsWith("training:actives=", StringComparison.Ordinal))
                {
                    try
                    {
                        model.TrainingActives = int.Parse(line.Substring(17), NumberFormatInfo.InvariantInfo);
                    }
                    catch (FormatException)
                    {
                        throw new IOException("Invalid training info line: " + line);
                    }
                }
                else if (line.StartsWith("roc:auc=", StringComparison.Ordinal))
                {
                    try
                    {
                        model.RocAUC = double.Parse(line.Substring(8), NumberFormatInfo.InvariantInfo);
                    }
                    catch (FormatException)
                    {
                        throw new IOException("Invalid AUC line: " + line);
                    }
                }
                else if (line.StartsWith("roc:type=", StringComparison.Ordinal))
                    model.RocType = line.Substring(9);
                else if (line.StartsWith("roc:x=", StringComparison.Ordinal))
                {
                    string[] nums = line.Substring(6).Split(',');
                    model.rocX = new double[nums.Length];
                    for (int n = 0; n < nums.Length; n++)
                    {
                        try
                        {
                            model.rocX[n] = double.Parse(nums[n], NumberFormatInfo.InvariantInfo);
                        }
                        catch (FormatException)
                        {
                            throw new IOException("Invalid ROC X coordinates, number=" + nums[n]);
                        }
                    }
                }
                else if (line.StartsWith("roc:y=", StringComparison.Ordinal))
                {
                    string[] nums = line.Substring(6).Split(',');
                    model.rocY = new double[nums.Length];
                    for (int n = 0; n < nums.Length; n++)
                    {
                        try
                        {
                            model.rocY[n] = double.Parse(nums[n], NumberFormatInfo.InvariantInfo);
                        }
                        catch (FormatException)
                        {
                            throw new IOException("Invalid ROC Y coordinates, number=" + nums[n]);
                        }
                    }
                }
                else if (line.StartsWith("note:title=", StringComparison.Ordinal))
                    model.noteTitle = line.Substring(11);
                else if (line.StartsWith("note:origin=", StringComparison.Ordinal))
                    model.noteOrigin = line.Substring(12);
                else if (line.StartsWith("note:comment=", StringComparison.Ordinal))
                {
                    model.noteComments = model.noteComments == null ? new string[1] : Arrays.CopyOf(model.noteComments,
                            model.noteComments.Length + 1);
                    model.noteComments[model.noteComments.Length - 1] = line.Substring(13);
                }
                // (else... silently ignore)
            }

            return model;
        }

        // ----------------- private methods -----------------

        /// <summary>
        /// estimate leave-one-out predictor for a given training entry
        /// </summary>
        private double SingleLeaveOneOut(int N)
        {
            var exclActive = activity[N];
            var exclFP = training[N];

            var sz = training.Count;
            var szN = sz - 1;
            var invSzN = 1.0 / szN;
            var activeN = exclActive ? numActive - 1 : numActive;
            var P_AT = activeN * invSzN;

            double val = 0;
            foreach (var hash in inHash.Keys)
            {
                if (Array.BinarySearch(exclFP, hash) < 0)
                    continue;
                var AT = inHash[hash];
                var A = AT[0] - (exclActive ? 1 : 0);
                var T = AT[1] - 1;

                var Pcorr = (A + 1) / (T * P_AT + 1);
                var P = Math.Log(Pcorr);
                val += P;
            }
            return val;
        }

        /// <summary>
        /// performs cross-validation, splitting into N different segments
        /// </summary>
        private void ValidateNfold(int nsegs)
        {
            var sz = training.Count;
            var order = new int[sz];
            int p = 0;
            for (int n = 0; n < sz; n++)
                if (activity[n])
                    order[p++] = n;
            for (int n = 0; n < sz; n++)
                if (!activity[n])
                    order[p++] = n;

            // build 5 separate contribution models: each one of them build from the 80% that are *not* in the segment
            var segContribs = new Dictionary<int, double>[nsegs];
            for (int n = 0; n < nsegs; n++)
                segContribs[n] = BuildPartial(order, n, nsegs);

            // use the separate models to estimate the cases that were not covered
            estimates = new double[sz];
            for (int n = 0; n < sz; n++)
                estimates[order[n]] = EstimatePartial(order, n, segContribs[n % nsegs]);
            CalculateRoc();
        }

        /// <summary>
        /// generates a contribution model based on all the training set for which (n%div)!=seg; e.g. for 5-fold, it would use the 80% of the training set
        /// that is not implied by the current skein
        /// </summary>
        private Dictionary<int, double> BuildPartial(int[] order, int seg, int div)
        {
            var sz = training.Count;
            int na = 0, nt = 0;
            var ih = new Dictionary<int, int[]>();
            for (int n = 0; n < sz; n++)
                if (n % div != seg)
                {
                    var active = activity[order[n]];
                    if (active)
                        na++;
                    nt++;
                    foreach (var h in training[order[n]])
                    {
                        if (!ih.TryGetValue(h, out int[] stash))
                            stash = new int[] { 0, 0 };
                        if (active)
                            stash[0]++;
                        stash[1]++;
                        ih[h] = stash;
                    }
                }

            var segContribs = new Dictionary<int, double>();

            double invSz = 1.0 / nt;
            double P_AT = na * invSz;
            foreach (var hash in ih.Keys)
            {
                var AT = ih[hash];
                var A = AT[0];
                var T = AT[1];
                var Pcorr = (A + 1) / (T * P_AT + 1);
                var P = Math.Log(Pcorr);
                segContribs[hash] = P;
            }

            return segContribs;
        }

        /// <summary>
        /// using contributions build from some partial section of the training set, uses that to estimate for an untrained entry
        /// </summary>
        private double EstimatePartial(int[] order, int N, Dictionary<int, double> segContrib)
        {
            double val = 0;
            foreach (var h in training[order[N]])
            {
                if (segContrib.TryGetValue(h, out double c))
                    val += c;
            }
            return val;
        }

        private class EstimatesComparator : IComparer<int>
        {
            private Bayesian parent;

            public EstimatesComparator(Bayesian parent)
            {
                this.parent = parent;
            }

            public int Compare(int i1, int i2)
            {
                double v1 = parent.Estimates[i1], v2 = parent.Estimates[i2];
                return v1 < v2 ? -1 : v1 > v2 ? 1 : 0;
            }
        }

        /// <summary>
        /// assumes estimates already calculated, fills in the ROC data
        /// </summary>
        private void CalculateRoc()
        {
            // sort the available estimates, and take midpoints 
            var sz = training.Count;

            var idx = new int[sz];
            for (int n = 0; n < sz; n++)
                idx[n] = n;
            Array.Sort(idx, new EstimatesComparator(this));

            var thresholds = new double[sz + 1];
            int tsz = 0;
            thresholds[tsz++] = LowThreshold - 0.01 * range;
            for (int n = 0; n < sz - 1; n++)
            {
                var th1 = Estimates[idx[n]];
                var th2 = Estimates[idx[n + 1]];
                if (th1 == th2)
                    continue;
                thresholds[tsz++] = 0.5 * (th1 + th2);
            }
            thresholds[tsz++] = HighThreshold + 0.01 * range;

            // x = false positives / actual negatives
            // y = true positives / actual positives
            rocX = new double[tsz];
            rocY = new double[tsz];
            var rocT = new double[tsz];

            int posTrue = 0, posFalse = 0, ipos = 0;
            var invPos = 1.0 / numActive;
            var invNeg = 1.0 / (sz - numActive);
            int rsz = 0;
            for (int n = 0; n < tsz; n++)
            {
                var th = thresholds[n];
                for (; ipos < sz; ipos++)
                {
                    if (th < Estimates[idx[ipos]])
                        break;
                    if (activity[idx[ipos]])
                        posTrue++;
                    else
                        posFalse++;
                }
                var x = posFalse * invNeg;
                var y = posTrue * invPos;
                if (rsz > 0 && x == RocX[rsz - 1] && y == RocY[rsz - 1])
                    continue;

                rocX[rsz] = 1 - x;
                rocY[rsz] = 1 - y;
                rocT[rsz] = th;
                rsz++;
            }

            rocX = Reverse(Resize(rocX, rsz));
            rocY = Reverse(Resize(rocY, rsz));
            rocT = Reverse(Resize(rocT, rsz));

            CalibrateThresholds(rocX, rocY, rocT);

            // calculate area-under-curve
            RocAUC = 0;
            for (int n = 0; n < rsz - 1; n++)
            {
                double w = RocX[n + 1] - RocX[n], h = 0.5 * (RocY[n] + RocY[n + 1]);
                RocAUC += w * h;
            }

            // collapse the {x,y} coords: no sensible reason to have huge number of points
            var DIST = 0.002;
            var DSQ = DIST * DIST;
            var gx = new double[rsz];
            var gy = new double[rsz];
            gx[0] = RocX[0];
            gy[0] = RocY[0];
            int gsz = 1;
            for (int i = 1; i < rsz - 1; i++)
            {
                var dx = RocX[i] - gx[gsz - 1];
                var dy = RocY[i] - gy[gsz - 1];
                if (dx * dx + dy * dy < DSQ)
                    continue;
                gx[gsz] = RocX[i];
                gy[gsz] = RocY[i];
                gsz++;
            }
            gx[gsz] = RocX[rsz - 1];
            gy[gsz] = RocY[rsz - 1];
            gsz++;
            rocX = Resize(gx, gsz);
            rocY = Resize(gy, gsz);
        }

        /// <summary>
        /// rederives the low/high thresholds, using ROC curve data: once the analysis is complete, the midpoint will be the optimum balance 
        /// </summary> 
        private void CalibrateThresholds(double[] x, double[] y, double[] t)
        {
            int sz = t.Length;
            int idx = 0;
            for (int n = 1; n < sz; n++)
                if (y[n] - x[n] > y[idx] - x[idx])
                    idx = n;
            double midThresh = t[idx];
            int idxX = 0, idxY = sz - 1;
            for (; idxX < idx - 1; idxX++)
                if (x[idxX] > 0)
                    break;
            for (; idxY > idx + 1; idxY--)
                if (y[idxY] < 1)
                    break;
            double delta = Math.Min(t[idxX] - midThresh, midThresh - t[idxY]);
            LowThreshold = midThresh - delta;
            HighThreshold = midThresh + delta;
            range = 2 * delta;
            invRange = range > 0 ? 1 / range : 0;
        }

        /// <summary>
        /// convenience functions    
        /// </summary> 
        private static double[] Resize(double[] arr, int sz)
        {
            var ret = new double[sz];
            for (int n = (arr == null ? 0 : Math.Min(sz, arr.Length)) - 1; n >= 0; n--)
                ret[n] = arr[n];
            return ret;
        }

        private static double[] Reverse(double[] arr)
        {
            var ret = new double[arr.Length];
            for (int i = 0, j = arr.Length - 1; j >= 0; i++, j--)
                ret[j] = arr[i];
            return ret;
        }
    }
}
