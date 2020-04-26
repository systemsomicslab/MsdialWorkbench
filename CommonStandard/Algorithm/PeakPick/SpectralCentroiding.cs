using CompMs.Common.Components;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CompMs.Common.Algorithm.PeakPick {
    public sealed class SpectralCentroiding
    {
        private SpectralCentroiding() { }

        /// <summary>
        /// This is the spectrum centroid method for MS-DIAL/MS-FINDER program.
        /// This method require one argument, list of 'Peak' class (please see Peak.cs of Common assembly).
        /// This program returns list of Peak.cs.
        /// </summary>
        /// <param name="spectraCollection"></param>
        /// <returns></returns>
        public static List<SpectrumPeak> Centroid(List<SpectrumPeak> spectraCollection, double threshold = 0.0)
        {
            #region // peak detection based centroid
            if (spectraCollection == null || spectraCollection.Count == 0) return null;
            var centroidedSpectra = new List<SpectrumPeak>();

            //Differential calculation
            #region
            List<double> firstDiffPeaklist = new List<double>();
            List<double> secondDiffPeaklist = new List<double>();
            double[] firstDiffCoeff = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
            double[] secondDiffCoeff = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };
            double firstDiff, secondDiff, maxFirstDiff = double.MinValue, maxSecondDiff = double.MinValue, maxAmplitudeDiff = double.MinValue;
            int halfDatapoint = (int)(firstDiffCoeff.Length / 2), peakID = 0;
            for (int i = 0; i < spectraCollection.Count; i++)
            {
                if (i < halfDatapoint)
                {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }
                if (i >= spectraCollection.Count - halfDatapoint)
                {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }

                firstDiff = secondDiff = 0;
                for (int j = 0; j < firstDiffCoeff.Length; j++)
                {
                    firstDiff += firstDiffCoeff[j] * spectraCollection[i + j - halfDatapoint].Intensity;
                    secondDiff += secondDiffCoeff[j] * spectraCollection[i + j - halfDatapoint].Intensity;
                }
                firstDiffPeaklist.Add(firstDiff);
                secondDiffPeaklist.Add(secondDiff);

                if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
                if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
                if (Math.Abs(spectraCollection[i].Intensity - spectraCollection[i - 1].Intensity) > maxAmplitudeDiff) maxAmplitudeDiff = Math.Abs(spectraCollection[i].Intensity - spectraCollection[i - 1].Intensity);
            }
            #endregion

            //Noise estimate
            #region
            List<double> amplitudeNoiseCandidate = new List<double>();
            List<double> slopeNoiseCandidate = new List<double>();
            List<double> peaktopNoiseCandidate = new List<double>();
            double amplitudeNoiseThresh = maxAmplitudeDiff * 0.05, slopeNoiseThresh = maxFirstDiff * 0.05, peaktopNoiseThresh = maxSecondDiff * 0.05;
            double amplitudeNoise, slopeNoise, peaktopNoise;
            for (int i = 2; i < spectraCollection.Count - 2; i++)
            {
                if (Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity) < amplitudeNoiseThresh && Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity) > 0) amplitudeNoiseCandidate.Add(Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity));
                if (Math.Abs(firstDiffPeaklist[i]) < slopeNoiseThresh && Math.Abs(firstDiffPeaklist[i]) > 0) slopeNoiseCandidate.Add(Math.Abs(firstDiffPeaklist[i]));
                if (secondDiffPeaklist[i] < 0 && Math.Abs(secondDiffPeaklist[i]) < peaktopNoiseThresh && Math.Abs(secondDiffPeaklist[i]) > 0) peaktopNoiseCandidate.Add(Math.Abs(secondDiffPeaklist[i]));
            }
            if (amplitudeNoiseCandidate.Count == 0) amplitudeNoise = 0.0001; else amplitudeNoise = BasicMathematics.Median(amplitudeNoiseCandidate.ToArray());
            if (slopeNoiseCandidate.Count == 0) slopeNoise = 0.0001; else slopeNoise = BasicMathematics.Median(slopeNoiseCandidate.ToArray());
            if (peaktopNoiseCandidate.Count == 0) peaktopNoise = 0.0001; else peaktopNoise = BasicMathematics.Median(peaktopNoiseCandidate.ToArray());
            #endregion

            //Search peaks
            #region
            List<double[]> datapoints;
            double peakTopIntensity;
            int peaktopCheckPoint, peakTopId = -1;
            bool peaktopCheck = false;

            double minimumDatapointCriteria = 1;
            double slopeNoiseFoldCriteria = 1;
            double peaktopNoiseFoldCriteria = 1;
            double minimumAmplitudeCriteria = 1;
            double peakHalfDiff, peakHwhm, resolution;
            int leftPeakHalfId, rightPeakHalfId, peakHalfId;

            for (int i = 0; i < spectraCollection.Count; i++)
            {
                if (i >= spectraCollection.Count - 1 - minimumDatapointCriteria) break;
                //1. Left edge criteria
                if (firstDiffPeaklist[i] >= 0 && firstDiffPeaklist[i + 1] > slopeNoise * slopeNoiseFoldCriteria)
                {
                    datapoints = new List<double[]>();
                    datapoints.Add(new double[] { spectraCollection[i].Mass, spectraCollection[i].Intensity, firstDiffPeaklist[i], secondDiffPeaklist[i] });

                    //2. Right edge criteria
                    #region
                    peaktopCheck = false;
                    peaktopCheckPoint = i;
                    while (true)
                    {
                        if (i + 1 == spectraCollection.Count - 1) break;

                        i++;
                        datapoints.Add(new double[] { spectraCollection[i].Mass, spectraCollection[i].Intensity, firstDiffPeaklist[i], secondDiffPeaklist[i] });
                        if (peaktopCheck == false && firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i] < 0 && secondDiffPeaklist[i] < -1 * peaktopNoise * peaktopNoiseFoldCriteria) { peaktopCheck = true; peaktopCheckPoint = i; }
                        if (peaktopCheck == true && peaktopCheckPoint + 2 <= i - 1 && firstDiffPeaklist[i] > -1 * slopeNoise * slopeNoiseFoldCriteria) break;
                        if (Math.Abs(datapoints[0][0] - datapoints[datapoints.Count - 1][0]) > 1) break;

                    }
                    #endregion

                    //3. Check minimum datapoint criteria
                    #region
                    if (datapoints.Count < minimumDatapointCriteria) continue;
                    #endregion

                    //4. Check peak half height at half width
                    #region
                    peakTopIntensity = double.MinValue;
                    peakTopId = -1;
                    for (int j = 0; j < datapoints.Count; j++)
                    {
                        if (peakTopIntensity < datapoints[j][1])
                        {
                            peakTopIntensity = datapoints[j][1];
                            peakTopId = j;
                        }
                    }
                    if (datapoints[peakTopId][1] < minimumAmplitudeCriteria) continue;
                    if (datapoints[peakTopId][1] < threshold) continue;

                    peakHalfDiff = double.MaxValue;
                    leftPeakHalfId = 0;
                    for (int j = peakTopId; j >= 0; j--)
                    {
                        if (peakHalfDiff > Math.Abs((datapoints[peakTopId][1] - datapoints[0][1]) / 2 - (datapoints[j][1] - datapoints[0][1])))
                        {
                            peakHalfDiff = Math.Abs((datapoints[peakTopId][1] - datapoints[0][1]) / 2 - (datapoints[j][1] - datapoints[0][1]));
                            leftPeakHalfId = j;
                        }
                    }

                    peakHalfDiff = double.MaxValue;
                    rightPeakHalfId = 0;
                    for (int j = peakTopId; j <= datapoints.Count - 1; j++)
                    {
                        if (peakHalfDiff > Math.Abs((datapoints[peakTopId][1] - datapoints[datapoints.Count - 1][1]) / 2 - (datapoints[j][1] - datapoints[datapoints.Count - 1][1])))
                        {
                            peakHalfDiff = Math.Abs((datapoints[peakTopId][1] - datapoints[datapoints.Count - 1][1]) / 2 - (datapoints[j][1] - datapoints[datapoints.Count - 1][1]));
                            rightPeakHalfId = j;
                        }
                    }

                    if (datapoints[0][1] <= datapoints[datapoints.Count - 1][1]) peakHalfId = leftPeakHalfId;
                    else peakHalfId = rightPeakHalfId;
                    peakHwhm = Math.Abs(datapoints[peakHalfId][0] - datapoints[peakTopId][0]);
                    resolution = datapoints[peakTopId][0] / peakHwhm * 0.5;
                    #endregion

                    //5. Set peakInforamtion
                    #region
                    var peak = new SpectrumPeak() { Mass = datapoints[peakTopId][0], Intensity = datapoints[peakTopId][1], Resolution = resolution, Comment = string.Empty };
                    centroidedSpectra.Add(peak);
                    peakID++;
                    #endregion
                }
            }
            #endregion

            if (centroidedSpectra.Count == 0) return null;

            var filteredCentroidedSpectra = new List<SpectrumPeak>();

            centroidedSpectra = centroidedSpectra.OrderByDescending(n => n.Intensity).ToList();
            double maxIntensity = centroidedSpectra[0].Intensity;
            for (int i = 0; i < centroidedSpectra.Count; i++)
                if (centroidedSpectra[i].Intensity > maxIntensity * 0.000001) { filteredCentroidedSpectra.Add(centroidedSpectra[i]); } else break;
            filteredCentroidedSpectra = filteredCentroidedSpectra.OrderBy(n => n.Mass).ToList();

            return filteredCentroidedSpectra;
            #endregion
        }

        ///// <summary>
        ///// This is the spectrum centroid method for MS-DIAL program
        ///// </summary>
        ///// <param name="spectraCollection"></param>
        ///// <returns></returns>
        //public static List<SpectrumPeak> PeakDetectionBasedCentroid(List<SpectrumPeak> spectraCollection) {
        //    #region // peak detection based centroid
        //    var centroidedSpectra = new List<SpectrumPeak>();

        //    //Differential calculation
        //    #region
        //    List<double> firstDiffPeaklist = new List<double>();
        //    List<double> secondDiffPeaklist = new List<double>();
        //    double[] firstDiffCoeff = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
        //    double[] secondDiffCoeff = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };
        //    double firstDiff, secondDiff, maxFirstDiff = double.MinValue, maxSecondDiff = double.MinValue, maxAmplitudeDiff = double.MinValue;
        //    int halfDatapoint = (int)(firstDiffCoeff.Length / 2), peakID = 0;
        //    for (int i = 0; i < spectraCollection.Count; i++) {
        //        if (i < halfDatapoint) {
        //            firstDiffPeaklist.Add(0);
        //            secondDiffPeaklist.Add(0);
        //            continue;
        //        }
        //        if (i >= spectraCollection.Count - halfDatapoint) {
        //            firstDiffPeaklist.Add(0);
        //            secondDiffPeaklist.Add(0);
        //            continue;
        //        }

        //        firstDiff = secondDiff = 0;
        //        for (int j = 0; j < firstDiffCoeff.Length; j++) {
        //            firstDiff += firstDiffCoeff[j] * spectraCollection[i + j - halfDatapoint].Intensity;
        //            secondDiff += secondDiffCoeff[j] * spectraCollection[i + j - halfDatapoint].Intensity;
        //        }
        //        firstDiffPeaklist.Add(firstDiff);
        //        secondDiffPeaklist.Add(secondDiff);

        //        if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
        //        if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
        //        if (Math.Abs(spectraCollection[i].Intensity - spectraCollection[i - 1].Intensity) > maxAmplitudeDiff)
        //            maxAmplitudeDiff = Math.Abs(spectraCollection[i].Intensity - spectraCollection[i - 1].Intensity);
        //    }
        //    #endregion

        //    //Noise estimate
        //    #region
        //    List<double> amplitudeNoiseCandidate = new List<double>();
        //    List<double> slopeNoiseCandidate = new List<double>();
        //    List<double> peaktopNoiseCandidate = new List<double>();
        //    double amplitudeNoiseThresh = maxAmplitudeDiff * 0.05, slopeNoiseThresh = maxFirstDiff * 0.05, peaktopNoiseThresh = maxSecondDiff * 0.05;
        //    double amplitudeNoise, slopeNoise, peaktopNoise;
        //    for (int i = 2; i < spectraCollection.Count - 2; i++) {
        //        if (Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity) < amplitudeNoiseThresh &&
        //            Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity) > 0)
        //            amplitudeNoiseCandidate.Add(Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity));
        //        if (Math.Abs(firstDiffPeaklist[i]) < slopeNoiseThresh && Math.Abs(firstDiffPeaklist[i]) > 0) slopeNoiseCandidate.Add(Math.Abs(firstDiffPeaklist[i]));
        //        if (secondDiffPeaklist[i] < 0 && Math.Abs(secondDiffPeaklist[i]) < peaktopNoiseThresh && Math.Abs(secondDiffPeaklist[i]) > 0)
        //            peaktopNoiseCandidate.Add(Math.Abs(secondDiffPeaklist[i]));
        //    }
        //    if (amplitudeNoiseCandidate.Count == 0) amplitudeNoise = 0.0001; else amplitudeNoise = BasicMathematics.Median(amplitudeNoiseCandidate.ToArray());
        //    if (slopeNoiseCandidate.Count == 0) slopeNoise = 0.0001; else slopeNoise = BasicMathematics.Median(slopeNoiseCandidate.ToArray());
        //    if (peaktopNoiseCandidate.Count == 0) peaktopNoise = 0.0001; else peaktopNoise = BasicMathematics.Median(peaktopNoiseCandidate.ToArray());
        //    #endregion

        //    //Search peaks
        //    #region
        //    List<double[]> datapoints;
        //    double peakTopIntensity;
        //    int peaktopCheckPoint, peakTopId = -1;
        //    bool peaktopCheck = false;

        //    double minimumDatapointCriteria = 1;
        //    double slopeNoiseFoldCriteria = 1;
        //    double peaktopNoiseFoldCriteria = 1;
        //    double minimumAmplitudeCriteria = 1;

        //    for (int i = 0; i < spectraCollection.Count; i++) {
        //        if (i >= spectraCollection.Count - 1 - minimumDatapointCriteria) break;
        //        //1. Left edge criteria
        //        if (firstDiffPeaklist[i] >= 0 && firstDiffPeaklist[i + 1] > slopeNoise * slopeNoiseFoldCriteria) {
        //            datapoints = new List<double[]>();
        //            datapoints.Add(new double[] { spectraCollection[i].Mass, spectraCollection[i].Intensity, firstDiffPeaklist[i], secondDiffPeaklist[i] });

        //            //2. Right edge criteria
        //            #region
        //            peaktopCheck = false;
        //            peaktopCheckPoint = i;
        //            while (true) {
        //                if (i + 1 == spectraCollection.Count - 1) break;

        //                i++;
        //                datapoints.Add(new double[] { spectraCollection[i].Mass, spectraCollection[i].Intensity, firstDiffPeaklist[i], secondDiffPeaklist[i] });
        //                if (peaktopCheck == false && firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i] < 0 &&
        //                    secondDiffPeaklist[i] < -1 * peaktopNoise * peaktopNoiseFoldCriteria) { peaktopCheck = true; peaktopCheckPoint = i; }
        //                if (peaktopCheck == true && peaktopCheckPoint + 2 <= i - 1 && firstDiffPeaklist[i] > -1 * slopeNoise * slopeNoiseFoldCriteria) break;
        //                if (Math.Abs(datapoints[0][0] - datapoints[datapoints.Count - 1][0]) > 1) break;

        //            }
        //            #endregion

        //            //3. Check minimum datapoint criteria
        //            #region
        //            if (datapoints.Count < minimumDatapointCriteria) continue;
        //            #endregion

        //            //4. Check peak half height at half width
        //            #region
        //            peakTopIntensity = double.MinValue;
        //            peakTopId = -1;
        //            for (int j = 0; j < datapoints.Count; j++) {
        //                if (peakTopIntensity < datapoints[j][1]) {
        //                    peakTopIntensity = datapoints[j][1];
        //                    peakTopId = j;
        //                }
        //            }
        //            if (datapoints[peakTopId][1] < minimumAmplitudeCriteria) continue;
        //            #endregion

        //            //5. Set peakInforamtion
        //            #region
        //            var detectedPeakInformation = new SpectrumPeak { Mass = datapoints[peakTopId][0], Intensity = datapoints[peakTopId][1] };
        //            centroidedSpectra.Add(detectedPeakInformation);
        //            peakID++;
        //            #endregion
        //        }
        //    }
        //    #endregion

        //    if (centroidedSpectra.Count == 0) return null;

        //    var filteredCentroidedSpectra = new List<SpectrumPeak>();

        //    centroidedSpectra = centroidedSpectra.OrderByDescending(n => n.Intensity).ToList();
        //    double maxIntensity = centroidedSpectra[0].Intensity;
        //    for (int i = 0; i < centroidedSpectra.Count; i++)
        //        if (centroidedSpectra[i].Intensity > maxIntensity * 0.000001) { filteredCentroidedSpectra.Add(centroidedSpectra[i]); } else break;
        //    filteredCentroidedSpectra = filteredCentroidedSpectra.OrderBy(n => n.Mass).ToList();

        //    return filteredCentroidedSpectra;
        //    #endregion
        //}


        //private static int getStartIndex(double focusedMass, double ms1Tolerance, List<SpectrumPeak> spectra)
        //{
        //    if (spectra.Count == 0) return 0;

        //    double targetMass = focusedMass - ms1Tolerance;
        //    int startIndex = 0, endIndex = spectra.Count - 1;
        //    int counter = 0;
        //    while (counter < 10)
        //    {
        //        if (spectra[startIndex].Mass <= targetMass && targetMass < spectra[(startIndex + endIndex) / 2].Mass)
        //        {
        //            endIndex = (startIndex + endIndex) / 2;
        //        }
        //        else if (spectra[(startIndex + endIndex) / 2].Mass <= targetMass && targetMass < spectra[endIndex].Mass)
        //        {
        //            startIndex = (startIndex + endIndex) / 2;
        //        }
        //        counter++;
        //    }
        //    return startIndex;
        //}

        ///// <summary>
        ///// This is the spectrum centroid method for MS-DIAL program.
        ///// This method will return array ([0]m/z, [1]intensity) list as the observablecollection.
        ///// The first arg is the spectrum arrary collection. Each array, i.e. double[], should be [0]m/z and [1]intensity.
        ///// The second arg should be not required as long as peakdetectionBasedCentroid is true.
        ///// Although I prepared two type centroidings for MS-DIAL paper, now I recommend to use 'peakdetectionbasedcentroid' method, 
        ///// that is, we do not have to set bin (second arg) parameter.
        ///// </summary>
        ///// <param name="spectraCollection"></param>
        ///// <param name="bin"></param>
        ///// <param name="peakdetectionBasedCentroid"></param>
        ///// <returns></returns>
        //public static List<SpectrumPeak> Centroid(List<SpectrumPeak> spectraCollection, double bin, bool peakdetectionBasedCentroid) {
        //    if (peakdetectionBasedCentroid) {
        //        #region // peak detection based centroid
        //        var centroidedSpectra = new List<SpectrumPeak>();

        //        //Differential calculation
        //        #region
        //        List<double> firstDiffPeaklist = new List<double>();
        //        List<double> secondDiffPeaklist = new List<double>();
        //        double[] firstDiffCoeff = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
        //        double[] secondDiffCoeff = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };
        //        double firstDiff, secondDiff, maxFirstDiff = double.MinValue, maxSecondDiff = double.MinValue, maxAmplitudeDiff = double.MinValue;
        //        int halfDatapoint = (int)(firstDiffCoeff.Length / 2), peakID = 0;
        //        for (int i = 0; i < spectraCollection.Count; i++) {
        //            if (i < halfDatapoint) {
        //                firstDiffPeaklist.Add(0);
        //                secondDiffPeaklist.Add(0);
        //                continue;
        //            }
        //            if (i >= spectraCollection.Count - halfDatapoint) {
        //                firstDiffPeaklist.Add(0);
        //                secondDiffPeaklist.Add(0);
        //                continue;
        //            }

        //            firstDiff = secondDiff = 0;
        //            for (int j = 0; j < firstDiffCoeff.Length; j++) {
        //                firstDiff += firstDiffCoeff[j] * spectraCollection[i + j - halfDatapoint].Intensity;
        //                secondDiff += secondDiffCoeff[j] * spectraCollection[i + j - halfDatapoint].Intensity;
        //            }
        //            firstDiffPeaklist.Add(firstDiff);
        //            secondDiffPeaklist.Add(secondDiff);

        //            if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
        //            if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
        //            if (Math.Abs(spectraCollection[i].Intensity - spectraCollection[i - 1].Intensity) > maxAmplitudeDiff)
        //                maxAmplitudeDiff = Math.Abs(spectraCollection[i].Intensity - spectraCollection[i - 1].Intensity);
        //        }
        //        #endregion

        //        //Noise estimate
        //        #region
        //        List<double> amplitudeNoiseCandidate = new List<double>();
        //        List<double> slopeNoiseCandidate = new List<double>();
        //        List<double> peaktopNoiseCandidate = new List<double>();
        //        double amplitudeNoiseThresh = maxAmplitudeDiff * 0.05, slopeNoiseThresh = maxFirstDiff * 0.05, peaktopNoiseThresh = maxSecondDiff * 0.05;
        //        double amplitudeNoise, slopeNoise, peaktopNoise;
        //        for (int i = 2; i < spectraCollection.Count - 2; i++) {
        //            if (Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity) < amplitudeNoiseThresh &&
        //                Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity) > 0)
        //                amplitudeNoiseCandidate.Add(Math.Abs(spectraCollection[i + 1].Intensity - spectraCollection[i].Intensity));
        //            if (Math.Abs(firstDiffPeaklist[i]) < slopeNoiseThresh && Math.Abs(firstDiffPeaklist[i]) > 0) slopeNoiseCandidate.Add(Math.Abs(firstDiffPeaklist[i]));
        //            if (secondDiffPeaklist[i] < 0 && Math.Abs(secondDiffPeaklist[i]) < peaktopNoiseThresh &&
        //                Math.Abs(secondDiffPeaklist[i]) > 0) peaktopNoiseCandidate.Add(Math.Abs(secondDiffPeaklist[i]));
        //        }
        //        if (amplitudeNoiseCandidate.Count == 0) amplitudeNoise = 0.0001; else amplitudeNoise = BasicMathematics.Median(amplitudeNoiseCandidate.ToArray());
        //        if (slopeNoiseCandidate.Count == 0) slopeNoise = 0.0001; else slopeNoise = BasicMathematics.Median(slopeNoiseCandidate.ToArray());
        //        if (peaktopNoiseCandidate.Count == 0) peaktopNoise = 0.0001; else peaktopNoise = BasicMathematics.Median(peaktopNoiseCandidate.ToArray());
        //        #endregion

        //        //Search peaks
        //        #region
        //        List<double[]> datapoints;
        //        double peakTopIntensity;
        //        int peaktopCheckPoint, peakTopId = -1;
        //        bool peaktopCheck = false;

        //        double minimumDatapointCriteria = 1;
        //        double slopeNoiseFoldCriteria = 1;
        //        double peaktopNoiseFoldCriteria = 1;
        //        double minimumAmplitudeCriteria = 1;

        //        for (int i = 0; i < spectraCollection.Count; i++) {
        //            if (i >= spectraCollection.Count - 1 - minimumDatapointCriteria) break;
        //            //1. Left edge criteria
        //            if (firstDiffPeaklist[i] >= 0 && firstDiffPeaklist[i + 1] > slopeNoise * slopeNoiseFoldCriteria) {
        //                datapoints = new List<double[]>();
        //                datapoints.Add(new double[] { spectraCollection[i].Mass, spectraCollection[i].Intensity, firstDiffPeaklist[i], secondDiffPeaklist[i] });

        //                //2. Right edge criteria
        //                #region
        //                peaktopCheck = false;
        //                peaktopCheckPoint = i;
        //                while (true) {
        //                    if (i + 1 == spectraCollection.Count - 1) break;

        //                    i++;
        //                    datapoints.Add(new double[] { spectraCollection[i].Mass, spectraCollection[i].Intensity, firstDiffPeaklist[i], secondDiffPeaklist[i] });
        //                    if (peaktopCheck == false && firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i] < 0 && secondDiffPeaklist[i] < -1 * peaktopNoise * peaktopNoiseFoldCriteria) { peaktopCheck = true; peaktopCheckPoint = i; }
        //                    if (peaktopCheck == true && peaktopCheckPoint + 2 <= i - 1 && firstDiffPeaklist[i] > -1 * slopeNoise * slopeNoiseFoldCriteria) break;
        //                    if (Math.Abs(datapoints[0][0] - datapoints[datapoints.Count - 1][0]) > 1) break;

        //                }
        //                #endregion

        //                //3. Check minimum datapoint criteria
        //                #region
        //                if (datapoints.Count < minimumDatapointCriteria) continue;
        //                #endregion

        //                //4. Check peak half height at half width
        //                #region
        //                peakTopIntensity = double.MinValue;
        //                peakTopId = -1;
        //                for (int j = 0; j < datapoints.Count; j++) {
        //                    if (peakTopIntensity < datapoints[j][1]) {
        //                        peakTopIntensity = datapoints[j][1];
        //                        peakTopId = j;
        //                    }
        //                }
        //                if (datapoints[peakTopId][1] < minimumAmplitudeCriteria) continue;
        //                #endregion

        //                //5. Set peakInforamtion
        //                #region
        //                var detectedPeakInformation = new SpectrumPeak() { Mass = datapoints[peakTopId][0], Intensity = datapoints[peakTopId][1] };
        //                centroidedSpectra.Add(detectedPeakInformation);
        //                peakID++;
        //                #endregion
        //            }
        //        }
        //        #endregion

        //        if (centroidedSpectra.Count == 0) return null;

        //        var filteredCentroidedSpectra = new List<SpectrumPeak>();

        //        centroidedSpectra = centroidedSpectra.OrderByDescending(n => n.Intensity).ToList();
        //        double maxIntensity = centroidedSpectra[0].Intensity;
        //        for (int i = 0; i < centroidedSpectra.Count; i++)
        //            if (centroidedSpectra[i].Intensity > maxIntensity * 0.000001) { filteredCentroidedSpectra.Add(centroidedSpectra[i]); } else break;
        //        filteredCentroidedSpectra = filteredCentroidedSpectra.OrderBy(n => n.Mass).ToList();

        //        return filteredCentroidedSpectra;
        //        #endregion
        //    }
        //    else {
        //        #region // sweep bin based centroid

        //        bin = 0.1;
        //        var centroidedSpectra = new List<SpectrumPeak>();
        //        double minMz = spectraCollection[0].Mass;
        //        double maxMz = spectraCollection[spectraCollection.Count - 1].Mass;

        //        spectraCollection = spectraCollection.OrderBy(n => n.Intensity).ToList();

        //        double minInt = spectraCollection[0].Intensity;
        //        spectraCollection = spectraCollection.OrderBy(n => n.Mass).ToList();

        //        double focusedMz = minMz;
        //        int startIndex, remaindIndex = 0;
        //        int counter = 0;
        //        double sumXY = 0;
        //        double sumY = 0;

        //        while (focusedMz <= maxMz) {
        //            sumXY = 0;
        //            sumY = 0;
        //            counter = 0;
        //            startIndex = getStartIndex(focusedMz, bin, spectraCollection);
        //            for (int i = startIndex; i < spectraCollection.Count; i++) {
        //                if (spectraCollection[i].Mass < focusedMz - bin) continue;
        //                else if (spectraCollection[i].Mass > focusedMz + bin) {
        //                    remaindIndex = i;
        //                    break;
        //                }
        //                else {
        //                    sumXY += spectraCollection[i].Mass * spectraCollection[i].Intensity;
        //                    sumY += spectraCollection[i].Intensity;
        //                    counter++;
        //                }
        //            }

        //            if (sumY == 0) { focusedMz = Math.Max(focusedMz + bin, spectraCollection[remaindIndex].Mass - bin); continue; }
        //            if (counter == 1 && sumY < minInt + 5) {
        //                focusedMz = Math.Max(focusedMz + bin, spectraCollection[remaindIndex].Mass - bin); continue;
        //            }
        //            else if (counter == 2 && sumY < minInt * 2 + 10) {
        //                focusedMz = Math.Max(focusedMz + bin, spectraCollection[remaindIndex].Mass - bin); continue;
        //            }
        //            else if (counter == 3 && sumY < minInt * 3 + 15) {
        //                focusedMz = Math.Max(focusedMz + bin, spectraCollection[remaindIndex].Mass - bin); continue;
        //            }

        //            if (centroidedSpectra.Count != 0) {
        //                if (Math.Abs(centroidedSpectra[centroidedSpectra.Count - 1].Mass - sumXY / sumY) < bin) {
        //                    if (centroidedSpectra[centroidedSpectra.Count - 1].Intensity < sumY) {
        //                        centroidedSpectra[centroidedSpectra.Count - 1].Mass = sumXY / sumY;
        //                        centroidedSpectra[centroidedSpectra.Count - 1].Intensity = sumY;
        //                    }
        //                }
        //                else {
        //                    centroidedSpectra.Add(new SpectrumPeak { Mass = sumXY / sumY, Intensity = sumY });
        //                }
        //            }
        //            else {
        //                centroidedSpectra.Add(new SpectrumPeak { Mass = sumXY / sumY, Intensity = sumY });
        //            }

        //            focusedMz = Math.Max(focusedMz + bin, spectraCollection[remaindIndex].Mass - bin);
        //        }

        //        var filteredCentroidedSpectra = new List<SpectrumPeak>();

        //        if (centroidedSpectra.Count == 0) return new List<SpectrumPeak>(); ;

        //        centroidedSpectra = centroidedSpectra.OrderByDescending(n => n.Intensity).ToList();
        //        double maxIntensity = centroidedSpectra[0].Intensity;
        //        for (int i = 0; i < centroidedSpectra.Count; i++)
        //            if (centroidedSpectra[i].Intensity > maxIntensity * 0.001) { filteredCentroidedSpectra.Add(centroidedSpectra[i]); } else break;
        //        filteredCentroidedSpectra = filteredCentroidedSpectra.OrderBy(n => n.Mass).ToList();

        //        return filteredCentroidedSpectra;
        //        #endregion
        //    }
        //}
    }

}

