using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm {
    public sealed class MS2DecCalc {
        private MS2DecCalc() { }

		/// <summary>
		/// Execute a least square method to get MS2Dec class variable.
		/// The method is optimized for each 'co-elution pattern'.
		/// </summary>
        public static MS2DecResult Execute(ModelChromVector modelChromVector, List<List<Peak>> chromatograms) {
            switch (modelChromVector.Ms2DecPattern) {
                case Ms2DecPattern.C: return ms2DecPatternSingle(modelChromVector, chromatograms);
                case Ms2DecPattern.BC: return ms2DecPatternDouble(modelChromVector.Ms2DecPattern, modelChromVector, chromatograms);
                case Ms2DecPattern.CD: return ms2DecPatternDouble(modelChromVector.Ms2DecPattern, modelChromVector, chromatograms);
                case Ms2DecPattern.ABC: return ms2DecPatternTriple(modelChromVector.Ms2DecPattern, modelChromVector, chromatograms);
                case Ms2DecPattern.BCD: return ms2DecPatternTriple(modelChromVector.Ms2DecPattern, modelChromVector, chromatograms);
                case Ms2DecPattern.CDE: return ms2DecPatternTriple(modelChromVector.Ms2DecPattern, modelChromVector, chromatograms);
                case Ms2DecPattern.ABCD: return ms2DecPatternQuadruple(modelChromVector.Ms2DecPattern, modelChromVector, chromatograms);
                case Ms2DecPattern.BCDE: return ms2DecPatternQuadruple(modelChromVector.Ms2DecPattern, modelChromVector, chromatograms);
                case Ms2DecPattern.ABCDE: return ms2DecPatternQuintuple(modelChromVector, chromatograms);
            }
            return null;
        }

        private static MS2DecResult ms2DecPatternSingle(ModelChromVector modelChromVector, List<List<Peak>> chromatograms) {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++) {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
            }
            #endregion

            var t_t = BasicMathematics.SumOfSquare(tModelArray);
            var la_la = BasicMathematics.SumOfSquare(linearArray);
            var ca_ca = BasicMathematics.SumOfSquare(constArray);

            var t_la = BasicMathematics.InnerProduct(tModelArray, linearArray);
            var t_ca = BasicMathematics.InnerProduct(tModelArray, constArray);
            var la_ca = BasicMathematics.InnerProduct(linearArray, constArray);

            var matrix = new double[3, 3]{
                {t_t, t_la, t_ca},
                {t_la, la_la, la_ca},
                {t_ca, la_ca, ca_ca}
            };
            var luMatrix = MatrixCalculate.MatrixDecompose(matrix);
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern single)"); return null; }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern single)"); return null; }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<Peak>>();
            var originalChromatograms = new List<List<Peak>>();
            foreach (var chrom in chromatograms) {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_la + invMatrix[0, 2] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms2DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MS2DecResult ms2DecPatternDouble(Ms2DecPattern pattern, ModelChromVector modelChromVector, List<List<Peak>> chromatograms) {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++) {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
                if (pattern == Ms2DecPattern.BC)
                    aModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                else if (pattern == Ms2DecPattern.CD)
                    aModelArray[i] = modelChromVector.OneRightIntensityArray[i];
            }
            #endregion

            var t_t = BasicMathematics.SumOfSquare(tModelArray);
            var a_a = BasicMathematics.SumOfSquare(aModelArray);
            var la_la = BasicMathematics.SumOfSquare(linearArray);
            var ca_ca = BasicMathematics.SumOfSquare(constArray);

            var t_a = BasicMathematics.InnerProduct(tModelArray, aModelArray);
            var t_la = BasicMathematics.InnerProduct(tModelArray, linearArray);
            var t_ca = BasicMathematics.InnerProduct(tModelArray, constArray);

            var a_la = BasicMathematics.InnerProduct(aModelArray, linearArray);
            var a_ca = BasicMathematics.InnerProduct(aModelArray, constArray);

            var la_ca = BasicMathematics.InnerProduct(linearArray, constArray);

            var matrix = new double[4, 4]{
                {t_t, t_a, t_la, t_ca},
                {t_a, a_a, a_la, a_ca},
                {t_la, a_la, la_la, la_ca},
                {t_ca, a_ca, la_ca, ca_ca}
            };

            var luMatrix = MatrixCalculate.MatrixDecompose(matrix);
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern double)"); return ms2DecPatternSingle(modelChromVector, chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern double)"); return ms2DecPatternSingle(modelChromVector, chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<Peak>>();
            var originalChromatograms = new List<List<Peak>>();
            foreach (var chrom in chromatograms) {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_la + invMatrix[0, 3] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms2DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MS2DecResult ms2DecPatternTriple(Ms2DecPattern pattern, ModelChromVector modelChromVector, List<List<Peak>> chromatograms) {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var bModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++) {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
                if (pattern == Ms2DecPattern.ABC) {
                    aModelArray[i] = modelChromVector.TwoLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                }
                else if (pattern == Ms2DecPattern.BCD) {
                    aModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                }
                else if (pattern == Ms2DecPattern.CDE) {
                    aModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                    bModelArray[i] = modelChromVector.TwoRightInetnsityArray[i];
                }
            }
            #endregion

            var t_t = BasicMathematics.SumOfSquare(tModelArray);
            var a_a = BasicMathematics.SumOfSquare(aModelArray);
            var b_b = BasicMathematics.SumOfSquare(bModelArray);
            var la_la = BasicMathematics.SumOfSquare(linearArray);
            var ca_ca = BasicMathematics.SumOfSquare(constArray);

            var t_a = BasicMathematics.InnerProduct(tModelArray, aModelArray);
            var t_b = BasicMathematics.InnerProduct(tModelArray, bModelArray);
            var t_la = BasicMathematics.InnerProduct(tModelArray, linearArray);
            var t_ca = BasicMathematics.InnerProduct(tModelArray, constArray);

            var a_b = BasicMathematics.InnerProduct(aModelArray, bModelArray);
            var a_la = BasicMathematics.InnerProduct(aModelArray, linearArray);
            var a_ca = BasicMathematics.InnerProduct(aModelArray, constArray);

            var b_la = BasicMathematics.InnerProduct(bModelArray, linearArray);
            var b_ca = BasicMathematics.InnerProduct(bModelArray, constArray);

            var la_ca = BasicMathematics.InnerProduct(linearArray, constArray);

            var matrix = new double[5, 5]{
                {t_t, t_a, t_b, t_la, t_ca},
                {t_a, a_a, a_b, a_la, a_ca},
                {t_b, a_b, b_b, b_la, b_ca},
                {t_la, a_la, b_la, la_la, la_ca},
                {t_ca, a_ca, b_ca, la_ca, ca_ca}
            };

            var luMatrix = MatrixCalculate.MatrixDecompose(matrix);
            if (luMatrix == null) {
                Debug.WriteLine("LU Matrix null (pattern triple)");
                if (pattern == Ms2DecPattern.ABC)
                    return ms2DecPatternDouble(Ms2DecPattern.BC, modelChromVector, chromatograms);
                else
                    return ms2DecPatternDouble(Ms2DecPattern.CD, modelChromVector, chromatograms);
            }

            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) {
                Debug.WriteLine("Det A zero (pattern triple)");
                if (pattern == Ms2DecPattern.ABC)
                    return ms2DecPatternDouble(Ms2DecPattern.BC, modelChromVector, chromatograms);
                else
                    return ms2DecPatternDouble(Ms2DecPattern.CD, modelChromVector, chromatograms);
            }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<Peak>>();
            var originalChromatograms = new List<List<Peak>>();
            foreach (var chrom in chromatograms) {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_la + invMatrix[0, 4] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms2DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MS2DecResult ms2DecPatternQuadruple(Ms2DecPattern pattern, ModelChromVector modelChromVector, List<List<Peak>> chromatograms) {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var bModelArray = new double[modelChromVector.ChromScanList.Count];
            var cModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++) {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
                if (pattern == Ms2DecPattern.ABCD) {
                    aModelArray[i] = modelChromVector.TwoLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                    cModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                }
                else if (pattern == Ms2DecPattern.BCDE) {
                    aModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                    cModelArray[i] = modelChromVector.TwoRightInetnsityArray[i];
                }
            }
            #endregion

            var t_t = BasicMathematics.SumOfSquare(tModelArray);
            var a_a = BasicMathematics.SumOfSquare(aModelArray);
            var b_b = BasicMathematics.SumOfSquare(bModelArray);
            var c_c = BasicMathematics.SumOfSquare(cModelArray);
            var la_la = BasicMathematics.SumOfSquare(linearArray);
            var ca_ca = BasicMathematics.SumOfSquare(constArray);

            var t_a = BasicMathematics.InnerProduct(tModelArray, aModelArray);
            var t_b = BasicMathematics.InnerProduct(tModelArray, bModelArray);
            var t_c = BasicMathematics.InnerProduct(tModelArray, cModelArray);
            var t_la = BasicMathematics.InnerProduct(tModelArray, linearArray);
            var t_ca = BasicMathematics.InnerProduct(tModelArray, constArray);

            var a_b = BasicMathematics.InnerProduct(aModelArray, bModelArray);
            var a_c = BasicMathematics.InnerProduct(aModelArray, cModelArray);
            var a_la = BasicMathematics.InnerProduct(aModelArray, linearArray);
            var a_ca = BasicMathematics.InnerProduct(aModelArray, constArray);

            var b_c = BasicMathematics.InnerProduct(bModelArray, cModelArray);
            var b_la = BasicMathematics.InnerProduct(bModelArray, linearArray);
            var b_ca = BasicMathematics.InnerProduct(bModelArray, constArray);

            var c_la = BasicMathematics.InnerProduct(cModelArray, linearArray);
            var c_ca = BasicMathematics.InnerProduct(cModelArray, constArray);

            var la_ca = BasicMathematics.InnerProduct(linearArray, constArray);

            var matrix = new double[6, 6]{
                {t_t, t_a, t_b, t_c, t_la, t_ca},
                {t_a, a_a, a_b, a_c, a_la, a_ca},
                {t_b, a_b, b_b, b_c, b_la, b_ca},
                {t_c, a_c, b_c, c_c, c_la, c_ca},
                {t_la, a_la, b_la, c_la, la_la, la_ca},
                {t_ca, a_ca, b_ca, c_ca, la_ca, ca_ca}
            };

            var luMatrix = MatrixCalculate.MatrixDecompose(matrix);
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern quadruple)"); return ms2DecPatternTriple(Ms2DecPattern.BCD, modelChromVector, chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern quadruple)"); return ms2DecPatternTriple(Ms2DecPattern.BCD, modelChromVector, chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<Peak>>();
            var originalChromatograms = new List<List<Peak>>();
            foreach (var chrom in chromatograms) {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_c = BasicMathematics.InnerProduct(cModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_c + invMatrix[0, 4] * z_la + invMatrix[0, 5] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms2DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MS2DecResult ms2DecPatternQuintuple(ModelChromVector modelChromVector, List<List<Peak>> chromatograms) {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var bModelArray = new double[modelChromVector.ChromScanList.Count];
            var cModelArray = new double[modelChromVector.ChromScanList.Count];
            var dModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++) {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
                aModelArray[i] = modelChromVector.TwoLeftIntensityArray[i];
                bModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                cModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                dModelArray[i] = modelChromVector.TwoRightInetnsityArray[i];
            }
            #endregion

            var t_t = BasicMathematics.SumOfSquare(tModelArray);
            var a_a = BasicMathematics.SumOfSquare(aModelArray);
            var b_b = BasicMathematics.SumOfSquare(bModelArray);
            var c_c = BasicMathematics.SumOfSquare(cModelArray);
            var d_d = BasicMathematics.SumOfSquare(dModelArray);
            var la_la = BasicMathematics.SumOfSquare(linearArray);
            var ca_ca = BasicMathematics.SumOfSquare(constArray);

            var t_a = BasicMathematics.InnerProduct(tModelArray, aModelArray);
            var t_b = BasicMathematics.InnerProduct(tModelArray, bModelArray);
            var t_c = BasicMathematics.InnerProduct(tModelArray, cModelArray);
            var t_d = BasicMathematics.InnerProduct(tModelArray, dModelArray);
            var t_la = BasicMathematics.InnerProduct(tModelArray, linearArray);
            var t_ca = BasicMathematics.InnerProduct(tModelArray, constArray);

            var a_b = BasicMathematics.InnerProduct(aModelArray, bModelArray);
            var a_c = BasicMathematics.InnerProduct(aModelArray, cModelArray);
            var a_d = BasicMathematics.InnerProduct(aModelArray, dModelArray);
            var a_la = BasicMathematics.InnerProduct(aModelArray, linearArray);
            var a_ca = BasicMathematics.InnerProduct(aModelArray, constArray);

            var b_c = BasicMathematics.InnerProduct(bModelArray, cModelArray);
            var b_d = BasicMathematics.InnerProduct(bModelArray, dModelArray);
            var b_la = BasicMathematics.InnerProduct(bModelArray, linearArray);
            var b_ca = BasicMathematics.InnerProduct(bModelArray, constArray);

            var c_d = BasicMathematics.InnerProduct(cModelArray, dModelArray);
            var c_la = BasicMathematics.InnerProduct(cModelArray, linearArray);
            var c_ca = BasicMathematics.InnerProduct(cModelArray, constArray);

            var d_la = BasicMathematics.InnerProduct(dModelArray, linearArray);
            var d_ca = BasicMathematics.InnerProduct(dModelArray, constArray);

            var la_ca = BasicMathematics.InnerProduct(linearArray, constArray);

            var matrix = new double[7, 7]{
                {t_t, t_a, t_b, t_c, t_d, t_la, t_ca},
                {t_a, a_a, a_b, a_c, a_d, a_la, a_ca},
                {t_b, a_b, b_b, b_c, b_d, b_la, b_ca},
                {t_c, a_c, b_c, c_c, c_d, c_la, c_ca},
                {t_d, a_d, b_d, c_d, d_d, d_la, d_ca},
                {t_la, a_la, b_la, c_la, d_la, la_la, la_ca},
                {t_ca, a_ca, b_ca, c_ca, d_ca, la_ca, ca_ca}
            };

            var luMatrix = MatrixCalculate.MatrixDecompose(matrix);
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern quintuple)"); return ms2DecPatternTriple(Ms2DecPattern.BCD, modelChromVector, chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern quintuple)"); return ms2DecPatternTriple(Ms2DecPattern.BCD, modelChromVector, chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<Peak>>();
            var originalChromatograms = new List<List<Peak>>();
            foreach (var chrom in chromatograms) {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_c = BasicMathematics.InnerProduct(cModelArray, expIntArray);
                var z_d = BasicMathematics.InnerProduct(dModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_c + invMatrix[0, 4] * z_d + invMatrix[0, 5] * z_la + invMatrix[0, 6] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms2DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static bool tryGetAdhocCoefficient(double[] expIntArray, double[] tModelArray, int peakTop, out double coefficient) {
            coefficient = -1;

            if (peakTop < 4) return false;
            if (peakTop > expIntArray.Length - 5) return false;

            if (expIntArray[peakTop - 1] > expIntArray[peakTop - 2] && expIntArray[peakTop - 2] > expIntArray[peakTop - 3] && expIntArray[peakTop - 3] > expIntArray[peakTop - 4] &&
                expIntArray[peakTop + 1] > expIntArray[peakTop + 2] && expIntArray[peakTop + 2] > expIntArray[peakTop + 3] && expIntArray[peakTop + 3] > expIntArray[peakTop + 4]) {
                coefficient = expIntArray[peakTop] / tModelArray[peakTop];
                return true;
            }
            else
                return false;
        }

        private static MS2DecResult ms2DecResultProperty(ModelChromVector modelChromVector, List<List<Peak>> deconvolutedPeaklistList, List<List<Peak>> chromatograms) {
            if (deconvolutedPeaklistList == null || deconvolutedPeaklistList.Count == 0) return null;

            var peakTopOfDeconvolutedChrom = modelChromVector.TargetScanTopInModelChromVector - modelChromVector.TargetScanLeftInModelChromVector;
            var peakTopOfOriginalChrom = modelChromVector.TargetScanTopInModelChromVector;
            var ms2DecResult = new MS2DecResult();

            double sumArea = 0, sumHeight = 0, minModelMzDiff = double.MaxValue;
            int modelMzID = -1;
            for (int i = 0; i < deconvolutedPeaklistList.Count; i++) {
                var chromatogram = deconvolutedPeaklistList[i];
                if (Math.Abs(chromatogram[peakTopOfDeconvolutedChrom].Mz - modelChromVector.ModelMzList[0]) < minModelMzDiff) {
                    minModelMzDiff = Math.Abs(chromatogram[peakTopOfDeconvolutedChrom].Mz - modelChromVector.ModelMzList[0]);
                    modelMzID = i;
                }

                for (int j = 0; j < chromatogram.Count; j++) {
                    if (j == peakTopOfDeconvolutedChrom && chromatogram[j].Intensity > 0) {
                        ms2DecResult.MassSpectra.Add(new double[]{ chromatogram[j].Mz, chromatogram[j].Intensity });
                        sumHeight += chromatogram[j].Intensity;
                    }
                    if (j < chromatogram.Count - 1)
                        sumArea += (chromatogram[j].Intensity + chromatogram[j + 1].Intensity) * (chromatogram[j + 1].RetentionTime - chromatogram[j].RetentionTime) * 0.5;
                }
            }

            var quantChromatogram = deconvolutedPeaklistList[modelMzID];
            ms2DecResult.UniqueMs = (float)quantChromatogram[peakTopOfDeconvolutedChrom].Mz;
            ms2DecResult.Ms2DecPeakHeight = (float)quantChromatogram[peakTopOfDeconvolutedChrom].Intensity;

            sumArea = 0;
            for (int i = 0; i < quantChromatogram.Count; i++) {
                ms2DecResult.BaseChromatogram.Add(new double[] { quantChromatogram[i].ScanNumber, quantChromatogram[i].RetentionTime, quantChromatogram[i].Mz, quantChromatogram[i].Intensity });
                if (i == quantChromatogram.Count - 1) break;
                sumArea += (quantChromatogram[i].Intensity + quantChromatogram[i + 1].Intensity)
                    * (quantChromatogram[i + 1].RetentionTime - quantChromatogram[i].RetentionTime) * 0.5;
            }
            ms2DecResult.Ms2DecPeakArea = (float)sumArea * 60;

            ms2DecResult.MassSpectra = ms2DecResult.MassSpectra.OrderBy(n => n[0]).ToList();
            ms2DecResult.ModelMasses = modelChromVector.ModelMzList.ToList();

            return ms2DecResult;
        }

        private static List<Peak> getDeconvolutedPeaklist(double coefficient, ModelChromVector modelChromVector, double targetMz, double[] expIntArray) {
            var targetLeft = modelChromVector.TargetScanLeftInModelChromVector;
            var targetRight = modelChromVector.TargetScanRightInModelChromVector;
            var targetTop = modelChromVector.TargetScanTopInModelChromVector;

            if (expIntArray[targetTop] < coefficient * modelChromVector.TargetIntensityArray[targetTop])
                coefficient = expIntArray[targetTop] / modelChromVector.TargetIntensityArray[targetTop];

            var dPeaklist = new List<Peak>();
            for (int i = targetLeft; i <= targetRight; i++) {
                var intensity = coefficient * modelChromVector.TargetIntensityArray[i];
                if (i == targetTop && intensity <= 0) return null;

                dPeaklist.Add(new Peak() {
                    ScanNumber = modelChromVector.ChromScanList[i],
                    RetentionTime = modelChromVector.RtArray[i],
                    Mz = targetMz,
                    Intensity = intensity,
                });
            }
            return dPeaklist;
        }
    }
}
