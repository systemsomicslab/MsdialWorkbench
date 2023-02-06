using CompMs.Common.Components;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Common.Algorithm.CowAlignment {
    /// <summary>
    /// This class is not used in MS-DIAL and MS-FINDER programs.
    /// This class is now being used in MRM-PROBS and MRM-DIFF programs.
    /// </summary>

    public enum TraceDirection
    {
        Ali, Ver, Hor
    }

    public enum BorderLimit
    {
        Constant, Linear, Quad, Diamond, Gaussian
    }

    /// <summary>
    /// This is the parameters of correlation optimized warping algorithm.
    /// Please see Nielsen et.al. J. Chromatogr. A 805, 17–35 (1998).
    /// </summary>
    public class CowParameter
    {
        private int referenceID;
        private int slack;
        private int segmentSize;

        public int ReferenceID
        {
            get { return referenceID; }
            set { referenceID = value; }
        }

        public int Slack
        {
            get { return slack; }
            set { slack = value; }
        }

        public int SegmentSize
        {
            get { return segmentSize; }
            set { segmentSize = value; }
        }
    }

    public sealed class CowAlignment
    {
        private CowAlignment() { }

        /// <summary>
        /// This is the alignment program of correlation optimized warping. (see Nielsen et.al. J. Chromatogr. A 805, 17–35 (1998).)
        /// This program returns the chromatogram information as the list of ChromatogramPeak containing scan number, retention time, m/z, intensity.
        /// As long as you use 'Constant' enum as the borderlimit, you do not have to mind maxSlack (second arg).
        /// Now I'm making some border limits but please do not use others except for 'Constant' yet.
        /// The first argument, minSlack, should be 1 or 2 as long as ODS columns or GC are used.
        /// The second argument is please the same as the first argument.
        /// The third argument, segment size, should be set to the data point number of detected peaks (recommended).
        /// The sample chromatogram will be aligned to the reference chromatogram.
        /// The border limit please should be set to constant.
        /// </summary>
        /// <param name="minSlack"></param>
        /// <param name="maxSlack"></param>
        /// <param name="segmentSize"></param>
        /// <param name="referenceChromatogram"></param>
        /// <param name="sampleChromatogram"></param>
        /// <param name="borderLimit"></param>
        /// <returns></returns>
        public static List<ChromatogramPeak> CorrelationOptimizedWarping(int minSlack, int maxSlack, int segmentSize, 
            List<ChromatogramPeak> referenceChromatogram, List<ChromatogramPeak> sampleChromatogram, BorderLimit borderLimit)
        {
            var alignedChromatogram = new List<ChromatogramPeak>();
            int referenceDatapointNumber = referenceChromatogram.Count,
                sampleDatapointNumber = sampleChromatogram.Count;

            int segmentNumber = (int)(sampleDatapointNumber / segmentSize);
            int delta = (int)(referenceDatapointNumber / sampleDatapointNumber * segmentSize - segmentSize);
            int enabledLength = (segmentSize + delta) * segmentNumber;

            FunctionMatrix functionMatrixBean = new FunctionMatrix(segmentNumber + 1, enabledLength + 1);
            FunctionElement functionElementBean;

            //Slack parameter set
            # region
            List<int> slack = new List<int>();
            for (int i = 0; i < segmentNumber; i++)
            {
                if (borderLimit == BorderLimit.Constant)
                    slack.Add(minSlack);
                else if (borderLimit == BorderLimit.Linear)
                    slack.Add(minSlack + (int)((maxSlack - minSlack) * i / (segmentNumber - 1)));
                else if (borderLimit == BorderLimit.Quad)
                    slack.Add((int)(minSlack + (double)((double)(maxSlack - minSlack) * i / (double)Math.Pow(segmentNumber, 2))));
                else if (borderLimit == BorderLimit.Diamond)
                    slack.Add((int)(maxSlack - (double)(2 / ((double)segmentNumber - 1)) * Math.Abs(i - (double)(((double)segmentNumber - 1) / 2))));
                else if (borderLimit == BorderLimit.Gaussian)
                    slack.Add((int)(BasicMathematics.GaussianFunction(maxSlack - minSlack, (double)((double)segmentNumber / 2), (double)((double)segmentNumber / 4), i)) + minSlack);
            }
            #endregion

            //Initialize
            #region
            for (int i = 0; i <= segmentNumber; i++)
            {
                for (int j = 0; j <= enabledLength; j++)
                {
                    functionElementBean = new FunctionElement(double.MinValue, 0);
                    functionMatrixBean[i, j] = functionElementBean;
                }
            }
            functionMatrixBean[segmentNumber, enabledLength].Score = 0;
            #endregion

            //score matrix calculation
            #region
            int intervalStart, intervalEnd;
            double cumCoefficient;
            for (int i = segmentNumber - 1; i >= 0; i--)
            {
                intervalStart = Math.Max(i * (segmentSize + delta - slack[i]), enabledLength - (segmentNumber - i) * (segmentSize + delta + slack[i]));
                intervalEnd = Math.Min(i * (segmentSize + delta + slack[i]), enabledLength - (segmentNumber - i) * (segmentSize + delta - slack[i]));

                //bool amplitudeCheck = checkAmplitude(1000, referenceChromatogram, intervalStart, intervalEnd + segmentSize + slack[i]);

                for (int x = intervalStart; x <= intervalEnd; x++)
                {
                    for (int u = delta - slack[i]; u <= delta + slack[i]; u++)
                    {
                        if (0 <= x + segmentSize + u && x + segmentSize + u <= enabledLength)
                        {
                            cumCoefficient = functionMatrixBean[i + 1, x + segmentSize + u].Score + 
                                cowFunctionCalculation(i, x, u, segmentSize, referenceChromatogram, sampleChromatogram);

                            if (cumCoefficient > functionMatrixBean[i, x].Score)
                            {
                                functionMatrixBean[i, x].Score = cumCoefficient;
                                functionMatrixBean[i, x].Warp = u;
                            }
                        }
                    }
                }
            }
            #endregion

            //Backtrace
            #region
            int endPosition, positionFlont, positionEnd = 0, totalWarp, warp, counter;
            double warpedPosition, fraction, score;

            //Initialize
            endPosition = 0; warp = 0; score = 0; totalWarp = 0; counter = 0;
            for (int i = 0; i < segmentNumber; i++)
            {
                warp = functionMatrixBean[i, endPosition].Warp;
                score = functionMatrixBean[i, endPosition].Score;

                if (totalWarp > slack[i] * 2) { warp = -slack[i]; } else if (totalWarp < -1 * slack[i] * 2) warp = slack[i];

                for (int j = 0; j < segmentSize + warp; j++)
                {
                    if (endPosition + j > referenceDatapointNumber - 1) break;

                    //get warped position, and linear interpolation
                    warpedPosition = (double)j * segmentSize / (segmentSize + warp);
                    if (Math.Floor(warpedPosition) < 0) warpedPosition = 0;
                    if (Math.Ceiling(warpedPosition) > segmentSize) warpedPosition = segmentSize;

                    fraction = warpedPosition - Math.Floor(warpedPosition);
                    positionFlont = i * segmentSize + (int)Math.Floor(warpedPosition);
                    positionEnd = i * segmentSize + (int)Math.Ceiling(warpedPosition);

                    if (positionFlont > sampleDatapointNumber - 1) positionFlont = sampleDatapointNumber - 1;
                    if (positionEnd > sampleDatapointNumber - 1) positionEnd = sampleDatapointNumber - 1;

                    //Set
                    var peakInformation = new ChromatogramPeak(
                        referenceChromatogram[counter].ID,
                        (1 - fraction) * sampleChromatogram[positionFlont].Mass + fraction * sampleChromatogram[positionEnd].Mass,
                        (1 - fraction) * sampleChromatogram[positionFlont].Intensity + fraction * sampleChromatogram[positionEnd].Intensity,
                        referenceChromatogram[counter].ChromXs);
                    alignedChromatogram.Add(peakInformation);
                    counter++;
                }
                endPosition += segmentSize + warp; totalWarp += warp;
                //Debug.Print(endPosition + "\t" + totalWarp + "\t" + warp);

            }

            //Reminder
            if (enabledLength < referenceDatapointNumber)
            {
                for (int i = enabledLength; i < referenceDatapointNumber; i++)
                {
                    positionEnd++;
                    if (positionEnd > sampleDatapointNumber - 1) positionEnd = sampleDatapointNumber - 1;

                    var peakInformation = new ChromatogramPeak(
                        referenceChromatogram[counter].ID,
                        sampleChromatogram[positionEnd].Mass,
                        sampleChromatogram[positionEnd].Intensity,
                        referenceChromatogram[counter].ChromXs);
                    alignedChromatogram.Add(peakInformation);
                    counter++;
                }
            }
            #endregion

            //Debug.Print(referenceChromatogram.Count + "\t" + sampleChromatogram.Count + "\t" + alignedChromatogram.Count);

            return alignedChromatogram;
        }

        /// <summary>
        /// The point of dynamic programming based alignment is to get the suitable reference chromatogram.
        /// Selecting the reference chromatogram which should look like 'center' of chromatograms will be better to get nice alignment results.
        /// So, this program is used to get the suitable reference chromatogram from imported chromatograms.
        /// Please see Tsugawa et. al. Front. Genet. 5:471, 2015
        /// </summary>
        /// <param name="chromatograms"></param>
        /// <returns></returns>
        public static CowParameter AutomaticParameterDefinder(List<double[]> chromatograms)
        {
            int chromatogramsNumber = chromatograms[0].Length - 3;
            double[] gravityArray = new double[chromatogramsNumber];
            double maxIntensity = double.MinValue, totalIntensity, sum;
            for (int i = 0; i < chromatogramsNumber; i++)
            {
                sum = 0; totalIntensity = 0;
                for (int j = 0; j < chromatograms.Count; j++)
                {
                    sum += chromatograms[j][1] * chromatograms[j][3 + i];
                    totalIntensity += chromatograms[j][3 + i];
                    if (maxIntensity < chromatograms[j][3 + i]) maxIntensity = chromatograms[j][3 + i];
                }
                gravityArray[i] = sum / totalIntensity;
            }

            double maxGravity, minGravity, centerGravity;
            maxGravity = BasicMathematics.Max(gravityArray);
            minGravity = BasicMathematics.Min(gravityArray);
            centerGravity = (maxGravity + minGravity) / 2;

            int referenceID = BasicMathematics.GetNearestIndex(gravityArray, centerGravity);
            int slack = (int)((maxGravity - minGravity) * (chromatograms[chromatograms.Count - 1][0] - chromatograms[0][0]) / (chromatograms[chromatograms.Count - 1][1] - chromatograms[0][1]));

            CowParameter alignmentParameter = new CowParameter(){ ReferenceID = referenceID, Slack = slack };

            return alignmentParameter;
        }

        /// <summary>
        /// This is the simple alignment program (maybe can be used to GC).
        /// The sample chromatogram will be aligned to reference chromatogram so that the correlation coefficient should be maximum withing moveTime param.
        /// See Jonsson, P. et al. Anal. Chem. 76, 1738–45 (2004).
        /// </summary>
        /// <param name="moveTime"></param>
        /// <param name="referenceChromatogram"></param>
        /// <param name="sampleChromatogram"></param>
        /// <returns></returns>
        public static List<double[]> LinearAlignment(double moveTime, List<double[]> referenceChromatogram, List<double[]> sampleChromatogram)
        {
            List<double[]> alignedChromatogram = new List<double[]>();
            int referenceDatapointNumber = referenceChromatogram.Count, sampleDatapointNumber = sampleChromatogram.Count;
            int movePoint = (int)(moveTime * referenceDatapointNumber / (referenceChromatogram[referenceDatapointNumber - 1][1] - referenceChromatogram[0][1]));

            double referenceMean = BasicMathematics.Mean(referenceChromatogram, 3);
            double sampleMean = BasicMathematics.Mean(sampleChromatogram, 3);

            double covariance, covarianceMax = double.MinValue;
            int covarianceMaxId = 0;
            for (int i = -movePoint; i <= movePoint; i++)
            {
                covariance = 0;
                for (int j = 0; j < referenceChromatogram.Count; j++)
                {
                    if (j + i < 0) continue;
                    if (j + i > sampleChromatogram.Count - 1) break;
                    covariance += (referenceChromatogram[j][3] - referenceMean) * (sampleChromatogram[j + i][3] - sampleMean);
                }
                if (covariance > covarianceMax) { covarianceMax = covariance; covarianceMaxId = i; }
            }

            if (covarianceMaxId < 0)
            {
                for (int j = 0; j < referenceDatapointNumber; j++)
                {
                    if (j + covarianceMaxId < 0) alignedChromatogram.Add(new double[] { j, referenceChromatogram[j][1], sampleChromatogram[0][0], sampleChromatogram[0][3] });
                    else if (j + covarianceMaxId > sampleDatapointNumber - 1) alignedChromatogram.Add(new double[] { j, referenceChromatogram[j][1], sampleChromatogram[sampleDatapointNumber - 1][0], sampleChromatogram[sampleDatapointNumber - 1][3] });
                    else alignedChromatogram.Add(new double[] { j, referenceChromatogram[j][1], sampleChromatogram[j + covarianceMaxId][0], sampleChromatogram[j + covarianceMaxId][3] });
                }
            }
            else if (covarianceMaxId > 0)
            {
                for (int j = 0; j < referenceDatapointNumber; j++)
                {
                    if (j + covarianceMaxId > sampleDatapointNumber - 1) alignedChromatogram.Add(new double[] { j, referenceChromatogram[j][1], sampleChromatogram[sampleDatapointNumber - 1][0], sampleChromatogram[sampleDatapointNumber - 1][3] });
                    else alignedChromatogram.Add(new double[] { j, referenceChromatogram[j][1], sampleChromatogram[j + covarianceMaxId][0], sampleChromatogram[j + covarianceMaxId][3] });
                }
            }
            else
            {
                for (int j = 0; j < referenceDatapointNumber; j++)
                {
                    if (j > sampleDatapointNumber - 1) alignedChromatogram.Add(new double[] { j, referenceChromatogram[j][1], sampleChromatogram[sampleDatapointNumber - 1][0], sampleChromatogram[sampleDatapointNumber - 1][3] });
                    else alignedChromatogram.Add(new double[] { j, referenceChromatogram[j][1], sampleChromatogram[j][0], sampleChromatogram[j][3] });
                }
            }
            return alignedChromatogram;
        }

        private static double cowFunctionCalculation(int rowPosition, int columnPosition, int u, int segmentSize, 
            List<ChromatogramPeak> referenceChromatogram, List<ChromatogramPeak> sampleChromatogram)
        {
            int positionFlont, positionEnd;
            double warpedPosition, fraction, wT, wS;
            double[] targetArray = new double[segmentSize + u + 1], alingedArray = new double[segmentSize + u + 1];

            for (int j = 0; j <= segmentSize + u; j++)
            {
                if (columnPosition + j >= referenceChromatogram.Count) break;

                //get warped position, and linear interpolation
                warpedPosition = (double)(j * segmentSize / (segmentSize + u));
                if (Math.Floor(warpedPosition) < 0) warpedPosition = 0;
                if (Math.Ceiling(warpedPosition) > segmentSize) warpedPosition = segmentSize;

                fraction = warpedPosition - Math.Floor(warpedPosition);
                positionFlont = rowPosition * segmentSize + (int)Math.Floor(warpedPosition);
                positionEnd = rowPosition * segmentSize + (int)Math.Ceiling(warpedPosition);

                if (positionFlont >= sampleChromatogram.Count) positionFlont = sampleChromatogram.Count - 1;
                if (positionEnd >= sampleChromatogram.Count) positionEnd = sampleChromatogram.Count - 1;

                wT = referenceChromatogram[columnPosition + j].Intensity;
                wS = (1 - fraction) * sampleChromatogram[positionFlont].Intensity + fraction * sampleChromatogram[positionEnd].Intensity;

                targetArray[j] = wT;
                alingedArray[j] = wS;
            }
            return Math.Abs(BasicMathematics.Coefficient(targetArray, alingedArray));
        }

        private static bool checkAmplitude(double thresholdAmplitude, List<double[]> referenceChromatogram, int startIndex, int endIndex) 
        {
            int massSpectraNumber = referenceChromatogram[0].Length - 3;
            double maxIntensity = double.MinValue;
            for (int i = 0; i < massSpectraNumber; i++)
                for (int j = startIndex; j <= endIndex; j++)
                    if (referenceChromatogram[j][3 + i] > maxIntensity) maxIntensity = referenceChromatogram[j][3 + i];

            if (maxIntensity < thresholdAmplitude) return false;
            else return true;
        }
    }
}
