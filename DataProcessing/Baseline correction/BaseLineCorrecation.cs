using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class BaseLineCorrecation
    {
        private BaseLineCorrecation() { }

        /// <summary>
        /// This is the base line correction method. This method will return the baseline corrected peak list (list of double arrays).
        /// The first arg, List<double[]> peaklist, should be the list of double arrays including [0]scan number, [1]retention time, [2]m/z, and [3]intensity.
        /// The second arg, bandWidth, is the decision criteria to get local minimum in the chromatogram. (Recommended 3 - 5)
        /// The third arg, segment number  is the parameter to segment chromatogram so that the baseline correction is performed in each segmented range.
        /// The detail is described in MS-DIAL papaer.
        /// </summary>
        /// <param name="peaklist"></param>
        /// <param name="bandWidth"></param>
        /// <param name="segmentNumber"></param>
        /// <returns></returns>
        public static List<double[]> NonBiaseBaseLineCorrection(List<double[]> peaklist, int bandWidth, int segmentNumber)
        {
            var correctedPeaklist = new List<double[]>();
            var filledList = new List<int>();

            //check baseline criteria
            int segmentWidth = peaklist.Count / segmentNumber;
            double median = 0;
            var scanList = new List<int>();
            var intensityList = new List<double>();
            for (int i = 0; i < peaklist.Count; i++)
            {
                for (int j = -bandWidth; j <= bandWidth; j++)
                {
                    if (i + j < 0) continue;
                    if (i + j >= peaklist.Count - 1) break;
                    if (j < 0 && peaklist[i + j][3] < peaklist[i + j + 1][3]) break;
                    if (j > 0 && peaklist[i + j - 1][3] > peaklist[i + j][3]) break;

                    if (j == bandWidth)
                    {
                        scanList.Add(i);
                        intensityList.Add(peaklist[i][3]);
                    }
                }

                if (i == segmentWidth)
                {
                    segmentWidth += peaklist.Count / segmentNumber;

                    if (scanList.Count == 0 || intensityList.Count == 0) continue;

                    median = BasicMathematics.Median(intensityList.ToArray());
                    for (int j = 0; j < intensityList.Count; j++)
                    {
                        if (intensityList[j] < median)
                            filledList.Add(scanList[j]);
                    }

                    scanList = new List<int>();
                    intensityList = new List<double>();
                }

                if (i == peaklist.Count - 1 && i != segmentWidth)
                {
                    if (scanList.Count == 0 || intensityList.Count == 0) break;
                    median = BasicMathematics.Median(intensityList.ToArray());
                    for (int j = 0; j < intensityList.Count; j++)
                    {
                        if (intensityList[j] < median)
                            filledList.Add(scanList[j]);
                    }
                }
            }

            if (filledList.Count == 0)
            {
                filledList.Add(0);
                filledList.Add(peaklist.Count - 1);
            }

            if (filledList[0] != 0) 
                filledList.Insert(0, 0); 
            if (filledList[filledList.Count - 1] != peaklist.Count - 1)
                filledList.Add(peaklist.Count - 1);

            double startPosition, endPosition, startIntensity, endIntensity, coefficient, intercept, correctedIntensity;
            for (int i = 0; i < filledList.Count - 1; i++)
            {
                startPosition = filledList[i];
                endPosition = filledList[i + 1];
                startIntensity = peaklist[(int)startPosition][3];
                endIntensity = peaklist[(int)endPosition][3];

                coefficient = (endIntensity - startIntensity) / (endPosition - startPosition);
                intercept = (startIntensity * endPosition - startPosition * endIntensity) / (endPosition - startPosition);
                
                for (int j = filledList[i]; j < filledList[i + 1]; j++)
                {
                    correctedIntensity = coefficient * j + intercept;
                    if (correctedIntensity < 0) correctedIntensity = 0;
                    if (peaklist[j][3] - correctedIntensity > 0)
                        correctedPeaklist.Add(new double[] { peaklist[j][0], peaklist[j][1], peaklist[j][2], peaklist[j][3] - correctedIntensity });
                    else
                        correctedPeaklist.Add(new double[] { peaklist[j][0], peaklist[j][1], peaklist[j][2], 0 });
                }

                if (i == filledList.Count - 2)
                {
                    correctedIntensity = coefficient * filledList[i + 1] + intercept;
                    if (correctedIntensity < 0) correctedIntensity = 0;
                    if (peaklist[peaklist.Count - 1][3] - correctedIntensity > 0)
                        correctedPeaklist.Add(new double[] { peaklist[peaklist.Count - 1][0], peaklist[peaklist.Count - 1][1], peaklist[peaklist.Count - 1][2], peaklist[peaklist.Count - 1][3] - correctedIntensity });
                    else
                        correctedPeaklist.Add(new double[] { peaklist[peaklist.Count - 1][0], peaklist[peaklist.Count - 1][1], peaklist[peaklist.Count - 1][2], 0 });
                }
            }
            return correctedPeaklist;
        }
    }
}
