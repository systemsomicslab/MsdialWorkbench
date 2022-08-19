using CompMs.Common.Components;
using CompMs.Common.Mathematics.Basic;
using System.Collections.Generic;

namespace CompMs.Common.Algorithm.PeakPick
{
    public static class BaseLineCorrecation
    {
        /// <summary>
        /// This is the base line correction method. This method will return the baseline corrected peak list (list of double arrays).
        /// The first arg, List<ChromatogramPeak> peaklist, should be the list of ChromatogramPeak including scan number, retention time, m/z, and intensity.
        /// The second arg, bandWidth, is the decision criteria to get local minimum in the chromatogram. (Recommended 3 - 5)
        /// The third arg, segment number  is the parameter to segment chromatogram so that the baseline correction is performed in each segmented range.
        /// The detail is described in MS-DIAL papaer.
        /// </summary>
        /// <param name="peaklist"></param>
        /// <param name="bandWidth"></param>
        /// <param name="segmentNumber"></param>
        /// <returns></returns>
        public static List<ChromatogramPeak> NonBiaseBaseLineCorrection(List<ChromatogramPeak> peaklist, int bandWidth, int segmentNumber)
        {
            var correctedPeaklist = new List<ChromatogramPeak>();
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
                    if (j < 0 && peaklist[i + j].Intensity < peaklist[i + j + 1].Intensity) break;
                    if (j > 0 && peaklist[i + j - 1].Intensity > peaklist[i + j].Intensity) break;

                    if (j == bandWidth)
                    {
                        scanList.Add(i);
                        intensityList.Add(peaklist[i].Intensity);
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
                startIntensity = peaklist[(int)startPosition].Intensity;
                endIntensity = peaklist[(int)endPosition].Intensity;

                coefficient = (endIntensity - startIntensity) / (endPosition - startPosition);
                intercept = (startIntensity * endPosition - startPosition * endIntensity) / (endPosition - startPosition);
                
                for (int j = filledList[i]; j < filledList[i + 1]; j++)
                {
                    correctedIntensity = coefficient * j + intercept;
                    if (correctedIntensity < 0) correctedIntensity = 0;
                    if (peaklist[j].Intensity - correctedIntensity > 0)
                        correctedPeaklist.Add(new ChromatogramPeak(peaklist[j].ID, peaklist[j].Mass, peaklist[j].Intensity - correctedIntensity, peaklist[j].ChromXs));
                    else
                        correctedPeaklist.Add(new ChromatogramPeak(peaklist[j].ID, peaklist[j].Mass, 0, peaklist[j].ChromXs));
                }

                if (i == filledList.Count - 2)
                {
                    correctedIntensity = coefficient * filledList[i + 1] + intercept;
                    if (correctedIntensity < 0) correctedIntensity = 0;
                    if (peaklist[peaklist.Count - 1].Intensity - correctedIntensity > 0)
                        correctedPeaklist.Add(new ChromatogramPeak(peaklist[peaklist.Count - 1].ID, peaklist[peaklist.Count - 1].Mass, peaklist[peaklist.Count - 1].Intensity - correctedIntensity, peaklist[peaklist.Count - 1].ChromXs));
                    else
                        correctedPeaklist.Add(new ChromatogramPeak(peaklist[peaklist.Count - 1].ID, peaklist[peaklist.Count - 1].Mass, 0, peaklist[peaklist.Count - 1].ChromXs));
                }
            }
            return correctedPeaklist;
        }
    }
}
