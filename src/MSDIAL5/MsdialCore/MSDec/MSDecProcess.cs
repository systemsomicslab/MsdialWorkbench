using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Mathematics.Matrix;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.MSDec {
    public sealed class MSDecProcess
    {
        private MSDecProcess() { }

		/// <summary>
		/// Execute a least square method to get MS2Dec class variable.
		/// The method is optimized for each 'co-elution pattern'.
		/// </summary>
        public static MSDecResult GetMsDecResult(ModelChromVector modelChromVector, List<List<ChromatogramPeak>> chromatograms)
        {
            if (chromatograms.IsEmptyOrNull()) return null;
            var ms1DecResult = new MSDecResult();
            switch (modelChromVector.Ms1DecPattern)
            {
                case MsDecPattern.C: return ms1DecPatternSingle(modelChromVector, chromatograms);
                case MsDecPattern.BC: return ms1DecPatternDouble(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.CD: return ms1DecPatternDouble(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.ABC: return ms1DecPatternTriple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.BCD: return ms1DecPatternTriple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.CDE: return ms1DecPatternTriple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.ABCD: return ms1DecPatternQuadruple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.BCDE: return ms1DecPatternQuadruple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.ABCDE: return ms1DecPatternQuintuple(modelChromVector, chromatograms);
            }
            return null;
        }

        public static MSDecResult GetMsDecResult(ModelChromVector modelChromVector, List<ValuePeak[]> chromatograms) {
            if (chromatograms.IsEmptyOrNull()) return null;
            var ms1DecResult = new MSDecResult();
            switch (modelChromVector.Ms1DecPattern) {
                case MsDecPattern.C: return ms1DecPatternSingle(modelChromVector, chromatograms);
                case MsDecPattern.BC: return ms1DecPatternDouble(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.CD: return ms1DecPatternDouble(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.ABC: return ms1DecPatternTriple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.BCD: return ms1DecPatternTriple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.CDE: return ms1DecPatternTriple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.ABCD: return ms1DecPatternQuadruple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.BCDE: return ms1DecPatternQuadruple(modelChromVector.Ms1DecPattern, modelChromVector, chromatograms);
                case MsDecPattern.ABCDE: return ms1DecPatternQuintuple(modelChromVector, chromatograms);
            }
            return null;
        }

        private static MSDecResult ms1DecPatternSingle(ModelChromVector modelChromVector, List<List<ChromatogramPeak>> ms1Chromatograms)
        {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++)
            {
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
         //   if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern single)"); return ms1DecPatternAdhoc(modelChromVector, ms1Chromatograms); }
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern single)"); return null; }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
         //   if (detA == 0) { Debug.WriteLine("Det A zero (pattern single)"); return ms1DecPatternAdhoc(modelChromVector, ms1Chromatograms); }
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern single)"); return null; }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<ChromatogramPeak>>();
            var originalChromatograms = new List<List<ChromatogramPeak>>();
            foreach (var chrom in ms1Chromatograms)
            {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_la + invMatrix[0, 2] * z_ca;
                if (coefficient <= 0) {
					if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) { 
						continue;
					}
				}
			
				var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mass, expIntArray);

                if (dPeaklist != null)
                {
					originalChromatograms.Add(chrom);
					deconvolutedPeaklistList.Add(dPeaklist);
                }
            }

            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternDouble(MsDecPattern pattern, ModelChromVector modelChromVector, List<List<ChromatogramPeak>> ms1Chromatograms)
        {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++)
            {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
                if (pattern == MsDecPattern.BC)
                    aModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                else if (pattern == MsDecPattern.CD)
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
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern double)"); return ms1DecPatternSingle(modelChromVector, ms1Chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern double)"); return ms1DecPatternSingle(modelChromVector, ms1Chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<ChromatogramPeak>>();
            var originalChromatograms = new List<List<ChromatogramPeak>>();
            foreach (var chrom in ms1Chromatograms)
            {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_la + invMatrix[0, 3] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mass, expIntArray);

                if (dPeaklist != null)
                {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternTriple(MsDecPattern pattern, ModelChromVector modelChromVector, List<List<ChromatogramPeak>> ms1Chromatograms)
        {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var bModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++)
            {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
                if (pattern == MsDecPattern.ABC)
                {
                    aModelArray[i] = modelChromVector.TwoLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                }
                else if (pattern == MsDecPattern.BCD)
                {
                    aModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                }
                else if (pattern == MsDecPattern.CDE)
                {
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
            if (luMatrix == null)
            {
                Debug.WriteLine("LU Matrix null (pattern triple)");
                if (pattern == MsDecPattern.ABC)
                    return ms1DecPatternDouble(MsDecPattern.BC, modelChromVector, ms1Chromatograms);
                else
                    return ms1DecPatternDouble(MsDecPattern.CD, modelChromVector, ms1Chromatograms);
            }
            
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) 
            { 
                Debug.WriteLine("Det A zero (pattern triple)");
                if (pattern == MsDecPattern.ABC)
                    return ms1DecPatternDouble(MsDecPattern.BC, modelChromVector, ms1Chromatograms);
                else
                    return ms1DecPatternDouble(MsDecPattern.CD, modelChromVector, ms1Chromatograms);
            }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<ChromatogramPeak>>();
            var originalChromatograms = new List<List<ChromatogramPeak>>();
            foreach (var chrom in ms1Chromatograms)
            {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_la + invMatrix[0, 4] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mass, expIntArray);

                if (dPeaklist != null)
                {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternQuadruple(MsDecPattern pattern, ModelChromVector modelChromVector, List<List<ChromatogramPeak>> ms1Chromatograms)
        {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var bModelArray = new double[modelChromVector.ChromScanList.Count];
            var cModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++)
            {
                constArray[i] = 1;
                linearArray[i] = i;
                tModelArray[i] = modelChromVector.TargetIntensityArray[i];
                if (pattern == MsDecPattern.ABCD)
                {
                    aModelArray[i] = modelChromVector.TwoLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                    cModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                }
                else if (pattern == MsDecPattern.BCDE)
                {
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
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern quadruple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern quadruple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<ChromatogramPeak>>();
            var originalChromatograms = new List<List<ChromatogramPeak>>();
            foreach (var chrom in ms1Chromatograms)
            {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_c = BasicMathematics.InnerProduct(cModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_c + invMatrix[0, 4] * z_la + invMatrix[0, 5] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mass, expIntArray);

                if (dPeaklist != null)
                {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternQuintuple(ModelChromVector modelChromVector, List<List<ChromatogramPeak>> ms1Chromatograms)
        {
            var constArray = new double[modelChromVector.ChromScanList.Count];
            var linearArray = new double[modelChromVector.ChromScanList.Count];
            var tModelArray = new double[modelChromVector.ChromScanList.Count];
            var aModelArray = new double[modelChromVector.ChromScanList.Count];
            var bModelArray = new double[modelChromVector.ChromScanList.Count];
            var cModelArray = new double[modelChromVector.ChromScanList.Count];
            var dModelArray = new double[modelChromVector.ChromScanList.Count];
            var expIntArray = new double[modelChromVector.ChromScanList.Count];

            #region initialize
            for (int i = 0; i < modelChromVector.ChromScanList.Count; i++)
            {
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
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern quintuple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern quintuple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<List<ChromatogramPeak>>();
            var originalChromatograms = new List<List<ChromatogramPeak>>();
            foreach (var chrom in ms1Chromatograms)
            {
                for (int i = 0; i < chrom.Count; i++) expIntArray[i] = chrom[i].Intensity;
                if (isSpikeNoise(expIntArray, modelChromVector.TargetScanTopInModelChromVector)) continue;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_c = BasicMathematics.InnerProduct(cModelArray, expIntArray);
                var z_d = BasicMathematics.InnerProduct(dModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_c + invMatrix[0, 4] * z_d + invMatrix[0, 5] * z_la + invMatrix[0, 6] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedPeaklist(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mass, expIntArray);

                if (dPeaklist != null)
                {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternSingle(ModelChromVector modelChromVector, List<ValuePeak[]> ms1Chromatograms) {
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
            //   if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern single)"); return ms1DecPatternAdhoc(modelChromVector, ms1Chromatograms); }
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern single)"); return null; }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            //   if (detA == 0) { Debug.WriteLine("Det A zero (pattern single)"); return ms1DecPatternAdhoc(modelChromVector, ms1Chromatograms); }
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern single)"); return null; }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<ValuePeak[]>();
            var originalChromatograms = new List<ValuePeak[]>();
            foreach (var chrom in ms1Chromatograms) {
                for (int i = 0; i < chrom.Length; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_la + invMatrix[0, 2] * z_ca;
                if (coefficient <= 0) {
                    if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) {
                        continue;
                    }
                }

                var dPeaklist = getDeconvolutedValuePeaks(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }

            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternDouble(MsDecPattern pattern, ModelChromVector modelChromVector, List<ValuePeak[]> ms1Chromatograms) {
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
                if (pattern == MsDecPattern.BC)
                    aModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                else if (pattern == MsDecPattern.CD)
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
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern double)"); return ms1DecPatternSingle(modelChromVector, ms1Chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern double)"); return ms1DecPatternSingle(modelChromVector, ms1Chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<ValuePeak[]>();
            var originalChromatograms = new List<ValuePeak[]>();
            foreach (var chrom in ms1Chromatograms) {
                for (int i = 0; i < chrom.Length; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_la + invMatrix[0, 3] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedValuePeaks(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternTriple(MsDecPattern pattern, ModelChromVector modelChromVector, List<ValuePeak[]> ms1Chromatograms) {
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
                if (pattern == MsDecPattern.ABC) {
                    aModelArray[i] = modelChromVector.TwoLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                }
                else if (pattern == MsDecPattern.BCD) {
                    aModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                }
                else if (pattern == MsDecPattern.CDE) {
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
                if (pattern == MsDecPattern.ABC)
                    return ms1DecPatternDouble(MsDecPattern.BC, modelChromVector, ms1Chromatograms);
                else
                    return ms1DecPatternDouble(MsDecPattern.CD, modelChromVector, ms1Chromatograms);
            }

            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) {
                Debug.WriteLine("Det A zero (pattern triple)");
                if (pattern == MsDecPattern.ABC)
                    return ms1DecPatternDouble(MsDecPattern.BC, modelChromVector, ms1Chromatograms);
                else
                    return ms1DecPatternDouble(MsDecPattern.CD, modelChromVector, ms1Chromatograms);
            }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<ValuePeak[]>();
            var originalChromatograms = new List<ValuePeak[]>();
            foreach (var chrom in ms1Chromatograms) {
                for (int i = 0; i < chrom.Length; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_la + invMatrix[0, 4] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedValuePeaks(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternQuadruple(MsDecPattern pattern, ModelChromVector modelChromVector, List<ValuePeak[]> ms1Chromatograms) {
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
                if (pattern == MsDecPattern.ABCD) {
                    aModelArray[i] = modelChromVector.TwoLeftIntensityArray[i];
                    bModelArray[i] = modelChromVector.OneLeftIntensityArray[i];
                    cModelArray[i] = modelChromVector.OneRightIntensityArray[i];
                }
                else if (pattern == MsDecPattern.BCDE) {
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
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern quadruple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern quadruple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<ValuePeak[]>();
            var originalChromatograms = new List<ValuePeak[]>();
            foreach (var chrom in ms1Chromatograms) {
                for (int i = 0; i < chrom.Length; i++) expIntArray[i] = chrom[i].Intensity;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_c = BasicMathematics.InnerProduct(cModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_c + invMatrix[0, 4] * z_la + invMatrix[0, 5] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedValuePeaks(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }

        private static MSDecResult ms1DecPatternQuintuple(ModelChromVector modelChromVector, List<ValuePeak[]> ms1Chromatograms) {
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
            if (luMatrix == null) { Debug.WriteLine("LU Matrix null (pattern quintuple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Debug.WriteLine("Det A zero (pattern quintuple)"); return ms1DecPatternTriple(MsDecPattern.BCD, modelChromVector, ms1Chromatograms); }
            var invMatrix = MatrixCalculate.MatrixInverse(luMatrix);

            var deconvolutedPeaklistList = new List<ValuePeak[]>();
            var originalChromatograms = new List<ValuePeak[]>();
            foreach (var chrom in ms1Chromatograms) {
                for (int i = 0; i < chrom.Length; i++) expIntArray[i] = chrom[i].Intensity;
                if (isSpikeNoise(expIntArray, modelChromVector.TargetScanTopInModelChromVector)) continue;
                var z_t = BasicMathematics.InnerProduct(tModelArray, expIntArray);
                var z_a = BasicMathematics.InnerProduct(aModelArray, expIntArray);
                var z_b = BasicMathematics.InnerProduct(bModelArray, expIntArray);
                var z_c = BasicMathematics.InnerProduct(cModelArray, expIntArray);
                var z_d = BasicMathematics.InnerProduct(dModelArray, expIntArray);
                var z_la = BasicMathematics.InnerProduct(linearArray, expIntArray);
                var z_ca = BasicMathematics.InnerProduct(constArray, expIntArray);

                var coefficient = invMatrix[0, 0] * z_t + invMatrix[0, 1] * z_a + invMatrix[0, 2] * z_b + invMatrix[0, 3] * z_c + invMatrix[0, 4] * z_d + invMatrix[0, 5] * z_la + invMatrix[0, 6] * z_ca;
                if (coefficient <= 0) if (!tryGetAdhocCoefficient(expIntArray, tModelArray, modelChromVector.TargetScanTopInModelChromVector, out coefficient)) continue;
                var dPeaklist = getDeconvolutedValuePeaks(coefficient, modelChromVector, chrom[modelChromVector.TargetScanTopInModelChromVector].Mz, expIntArray);

                if (dPeaklist != null) {
                    originalChromatograms.Add(chrom);
                    deconvolutedPeaklistList.Add(dPeaklist);
                }
            }
            return ms1DecResultProperty(modelChromVector, deconvolutedPeaklistList, originalChromatograms);
        }





        private static bool isSpikeNoise(double[] expIntArray, int peakTopID)
        {
            for (int i = peakTopID - 1; i >= 1; i--) { //check left spike
                if (peakTopID - i > 4) break;
                if (expIntArray[i] == 0 && expIntArray[i - 1] > 1000 && expIntArray[i + 1] > 1000) return true;
            }

            for (int i = peakTopID + 1; i <= expIntArray.Length - 2; i++) { //check right spike
                if (i - peakTopID > 4) break;
                if (expIntArray[i] == 0 && expIntArray[i - 1] > 1000 && expIntArray[i + 1] > 1000) return true;
            }
            return false;
        }

        private static bool tryGetAdhocCoefficient(double[] expIntArray, double[] tModelArray, int peakTop, out double coefficient)
        {
            coefficient = -1;

            if (peakTop < 4) return false;
            if (peakTop > expIntArray.Length - 5) return false;

            if (expIntArray[peakTop - 1] > expIntArray[peakTop - 2] && expIntArray[peakTop - 2] > expIntArray[peakTop - 3] && expIntArray[peakTop - 3] > expIntArray[peakTop - 4] && 
                expIntArray[peakTop + 1] > expIntArray[peakTop + 2] && expIntArray[peakTop + 2] > expIntArray[peakTop + 3] && expIntArray[peakTop + 3] > expIntArray[peakTop + 4])
            {
                coefficient = expIntArray[peakTop] / tModelArray[peakTop];
                return true;
            }
            else
                return false;
        }

		private static MSDecResult ms1DecResultProperty(ModelChromVector modelChromVector, 
            List<List<ChromatogramPeak>> deconvolutedPeaklistList, List<List<ChromatogramPeak>> ms1Chromatograms) {
			if (deconvolutedPeaklistList == null || deconvolutedPeaklistList.Count == 0) return null;

			var peakTopOfDeconvolutedChrom = modelChromVector.TargetScanTopInModelChromVector - modelChromVector.TargetScanLeftInModelChromVector;
			var peakTopOfOriginalChrom = modelChromVector.TargetScanTopInModelChromVector;
			var ms1DecResult = new MSDecResult();

			ms1DecResult.ScanID = modelChromVector.RdamScanList[modelChromVector.TargetScanTopInModelChromVector];
            ms1DecResult.ChromXs = new ChromXs(new RetentionTime(modelChromVector.RtArray[modelChromVector.TargetScanTopInModelChromVector]));

			double sumArea = 0, sumHeight = 0, minModelMzDiff = double.MaxValue;
			int modelMzID = -1;
			for (int i = 0; i < deconvolutedPeaklistList.Count; i++) {
				var chromatogram = deconvolutedPeaklistList[i];
				if (Math.Abs(chromatogram[peakTopOfDeconvolutedChrom].Mass - modelChromVector.ModelMzList[0]) < minModelMzDiff) {
					minModelMzDiff = Math.Abs(chromatogram[peakTopOfDeconvolutedChrom].Mass - modelChromVector.ModelMzList[0]);
					modelMzID = i;
				}

				for (int j = 0; j < chromatogram.Count; j++) {
					if (j == peakTopOfDeconvolutedChrom) {
						ms1DecResult.Spectrum.Add(new SpectrumPeak(chromatogram[j].Mass, chromatogram[j].Intensity));
						sumHeight += chromatogram[j].Intensity;

						if (chromatogram[j].Intensity > ms1Chromatograms[i][peakTopOfOriginalChrom].Intensity)
							ms1DecResult.Spectrum[ms1DecResult.Spectrum.Count - 1].PeakQuality = PeakQuality.Saturated;
					}
					if (j < chromatogram.Count - 1)
						sumArea += (chromatogram[j].Intensity + chromatogram[j + 1].Intensity) * (chromatogram[j + 1].ChromXs.Value - chromatogram[j].ChromXs.Value) * 0.5;
				}
			}

			ms1DecResult.IntegratedArea = (float)sumArea * 60;
			ms1DecResult.IntegratedHeight = (float)sumHeight;

			var quantChromatogram = deconvolutedPeaklistList[modelMzID];
			ms1DecResult.ModelPeakMz = (float)quantChromatogram[peakTopOfDeconvolutedChrom].Mass;
			ms1DecResult.ModelPeakHeight = (float)quantChromatogram[peakTopOfDeconvolutedChrom].Intensity;
			ms1DecResult.ModelPeakChromatogram = quantChromatogram;
			sumArea = 0;
			for (int i = 0; i < quantChromatogram.Count - 1; i++) {
				sumArea += (quantChromatogram[i].Intensity + quantChromatogram[i + 1].Intensity)
					* (quantChromatogram[i + 1].ChromXs.Value - quantChromatogram[i].ChromXs.Value) * 0.5;
			}
			ms1DecResult.ModelPeakArea = (float)sumArea * 60;

            var heightFromBaseline = Math.Max(ms1DecResult.ModelPeakHeight - quantChromatogram[0].Intensity,
                ms1DecResult.ModelPeakHeight - quantChromatogram[quantChromatogram.Count - 1].Intensity);
            ms1DecResult.EstimatedNoise = modelChromVector.EstimatedNoise;
            ms1DecResult.SignalNoiseRatio = (float)(heightFromBaseline / modelChromVector.EstimatedNoise);

            ms1DecResult.Spectrum = ms1DecResult.Spectrum.OrderBy(n => n.Mass).ToList();
			ms1DecResult.ModelMasses = modelChromVector.ModelMzList.ToList();

			return ms1DecResult;
        }

        private static MSDecResult ms1DecResultProperty(ModelChromVector modelChromVector,
            List<ValuePeak[]> deconvolutedPeaklistList, List<ValuePeak[]> ms1Chromatograms) {
            if (deconvolutedPeaklistList == null || deconvolutedPeaklistList.Count == 0) return null;

            var peakTopOfDeconvolutedChrom = modelChromVector.TargetScanTopInModelChromVector - modelChromVector.TargetScanLeftInModelChromVector;
            var peakTopOfOriginalChrom = modelChromVector.TargetScanTopInModelChromVector;
            var ms1DecResult = new MSDecResult();

            ms1DecResult.ScanID = modelChromVector.RdamScanList[modelChromVector.TargetScanTopInModelChromVector];
            ms1DecResult.ChromXs = new ChromXs(new RetentionTime(modelChromVector.RtArray[modelChromVector.TargetScanTopInModelChromVector]));

            double sumArea = 0, sumHeight = 0, minModelMzDiff = double.MaxValue;
            int modelMzID = -1;
            for (int i = 0; i < deconvolutedPeaklistList.Count; i++) {
                var chromatogram = deconvolutedPeaklistList[i];
                if (Math.Abs(chromatogram[peakTopOfDeconvolutedChrom].Mz - modelChromVector.ModelMzList[0]) < minModelMzDiff) {
                    minModelMzDiff = Math.Abs(chromatogram[peakTopOfDeconvolutedChrom].Mz - modelChromVector.ModelMzList[0]);
                    modelMzID = i;
                }

                for (int j = 0; j < chromatogram.Length; j++) {
                    if (j == peakTopOfDeconvolutedChrom) {
                        ms1DecResult.Spectrum.Add(new SpectrumPeak((float)chromatogram[j].Mz, (float)chromatogram[j].Intensity));
                        sumHeight += chromatogram[j].Intensity;

                        if (chromatogram[j].Intensity > ms1Chromatograms[i][peakTopOfOriginalChrom].Intensity)
                            ms1DecResult.Spectrum[ms1DecResult.Spectrum.Count - 1].PeakQuality = PeakQuality.Saturated;
                    }
                    if (j < chromatogram.Length - 1)
                        sumArea += (chromatogram[j].Intensity + chromatogram[j + 1].Intensity) * (chromatogram[j + 1].Time - chromatogram[j].Time) * 0.5;
                }
            }

            ms1DecResult.IntegratedArea = (float)sumArea * 60;
            ms1DecResult.IntegratedHeight = (float)sumHeight;

            var valuePeaks = deconvolutedPeaklistList[modelMzID];
            var quantChromatogram = new List<ChromatogramPeak>(valuePeaks.Length); 
            foreach (var peak in valuePeaks) {
                quantChromatogram.Add(new ChromatogramPeak(peak.Id, peak.Mz, peak.Intensity, new RetentionTime(peak.Time)));
            }

            ms1DecResult.ModelPeakMz = (float)quantChromatogram[peakTopOfDeconvolutedChrom].Mass;
            ms1DecResult.ModelPeakHeight = (float)quantChromatogram[peakTopOfDeconvolutedChrom].Intensity;
            ms1DecResult.ModelPeakChromatogram = quantChromatogram;
            sumArea = 0;
            for (int i = 0; i < quantChromatogram.Count - 1; i++) {
                sumArea += (quantChromatogram[i].Intensity + quantChromatogram[i + 1].Intensity)
                    * (quantChromatogram[i + 1].ChromXs.Value - quantChromatogram[i].ChromXs.Value) * 0.5;
            }
            ms1DecResult.ModelPeakArea = (float)sumArea * 60;

            var heightFromBaseline = Math.Max(ms1DecResult.ModelPeakHeight - quantChromatogram[0].Intensity,
                ms1DecResult.ModelPeakHeight - quantChromatogram[quantChromatogram.Count - 1].Intensity);
            ms1DecResult.EstimatedNoise = modelChromVector.EstimatedNoise;
            ms1DecResult.SignalNoiseRatio = (float)(heightFromBaseline / modelChromVector.EstimatedNoise);

            ms1DecResult.Spectrum = ms1DecResult.Spectrum.OrderBy(n => n.Mass).ToList();
            ms1DecResult.ModelMasses = modelChromVector.ModelMzList.ToList();

            return ms1DecResult;
        }

        private static List<ChromatogramPeak> getDeconvolutedPeaklist(double coefficient, ModelChromVector modelChromVector, double targetMz, double[] expIntArray)
        {
            var targetLeft = modelChromVector.TargetScanLeftInModelChromVector;
            var targetRight = modelChromVector.TargetScanRightInModelChromVector;
            var targetTop = modelChromVector.TargetScanTopInModelChromVector;

            if (expIntArray[targetTop] < coefficient * modelChromVector.TargetIntensityArray[targetTop])
                coefficient = expIntArray[targetTop] / modelChromVector.TargetIntensityArray[targetTop];

            var dPeaklist = new List<ChromatogramPeak>();
            for (int i = targetLeft; i <= targetRight; i++)
            {
                var intensity = coefficient * modelChromVector.TargetIntensityArray[i];
                if (i == targetTop && intensity <= 0) return null;
                
                dPeaklist.Add(new ChromatogramPeak(modelChromVector.RdamScanList[i], targetMz, intensity, new RetentionTime(modelChromVector.RtArray[i])));
            }
            return dPeaklist;
        }

        private static ValuePeak[] getDeconvolutedValuePeaks(double coefficient, ModelChromVector modelChromVector, double targetMz, double[] expIntArray) {
            var targetLeft = modelChromVector.TargetScanLeftInModelChromVector;
            var targetRight = modelChromVector.TargetScanRightInModelChromVector;
            var targetTop = modelChromVector.TargetScanTopInModelChromVector;

            if (expIntArray[targetTop] < coefficient * modelChromVector.TargetIntensityArray[targetTop])
                coefficient = expIntArray[targetTop] / modelChromVector.TargetIntensityArray[targetTop];

            var dPeaklist = new ValuePeak[targetRight - targetLeft + 1];
            for (int i = targetLeft; i <= targetRight; i++) {
                var intensity = coefficient * modelChromVector.TargetIntensityArray[i];
                if (i == targetTop && intensity <= 0) return null;

                dPeaklist[i - targetLeft] = new ValuePeak(modelChromVector.RdamScanList[i], modelChromVector.RtArray[i], targetMz, intensity);
            }
            return dPeaklist;
        }
    }
}
