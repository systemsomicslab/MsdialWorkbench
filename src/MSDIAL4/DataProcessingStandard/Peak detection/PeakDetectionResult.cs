using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This is the storage of peak detection result.
    /// </summary>
    public class PeakDetectionResult
    {
        int peakID;
        int scanNumAtLeftPeakEdge, scanNumAtRightPeakEdge, scanNumAtPeakTop;

        float intensityAtLeftPeakEdge, intensityAtRightPeakEdge, intensityAtPeakTop;
        float areaAboveZero, areaAboveBaseline;
        float rtAtLeftPeakEdge, rtAtRightPeakEdge, rtAtPeakTop;
        float peakPureValue, shapnessValue, gaussianSimilarityValue, idealSlopeValue, basePeakValue, symmetryValue, amplitudeScoreValue, amplitudeOrderValue;
        float estimatedNoise, signalToNoise;
      
        #region // properties
        public int PeakID
        {
            get { return peakID; }
            set { peakID = value; }
        }

        public int ScanNumAtPeakTop
        {
            get { return scanNumAtPeakTop; }
            set { scanNumAtPeakTop = value; }
        }

        public int ScanNumAtRightPeakEdge
        {
            get { return scanNumAtRightPeakEdge; }
            set { scanNumAtRightPeakEdge = value; }
        }

        public int ScanNumAtLeftPeakEdge
        {
            get { return scanNumAtLeftPeakEdge; }
            set { scanNumAtLeftPeakEdge = value; }
        }

        public float IntensityAtPeakTop
        {
            get { return intensityAtPeakTop; }
            set { intensityAtPeakTop = value; }
        }

        public float IntensityAtRightPeakEdge
        {
            get { return intensityAtRightPeakEdge; }
            set { intensityAtRightPeakEdge = value; }
        }

        public float IntensityAtLeftPeakEdge
        {
            get { return intensityAtLeftPeakEdge; }
            set { intensityAtLeftPeakEdge = value; }
        }

        public float RtAtPeakTop
        {
            get { return rtAtPeakTop; }
            set { rtAtPeakTop = value; }
        }

        public float RtAtRightPeakEdge
        {
            get { return rtAtRightPeakEdge; }
            set { rtAtRightPeakEdge = value; }
        }

        public float RtAtLeftPeakEdge
        {
            get { return rtAtLeftPeakEdge; }
            set { rtAtLeftPeakEdge = value; }
        }

        public float AmplitudeOrderValue
        {
            get { return amplitudeOrderValue; }
            set { amplitudeOrderValue = value; }
        }

        public float AmplitudeScoreValue
        {
            get { return amplitudeScoreValue; }
            set { amplitudeScoreValue = value; }
        }

        public float SymmetryValue
        {
            get { return symmetryValue; }
            set { symmetryValue = value; }
        }

        public float BasePeakValue
        {
            get { return basePeakValue; }
            set { basePeakValue = value; }
        }

        public float IdealSlopeValue
        {
            get { return idealSlopeValue; }
            set { idealSlopeValue = value; }
        }

        public float GaussianSimilarityValue
        {
            get { return gaussianSimilarityValue; }
            set { gaussianSimilarityValue = value; }
        }

        public float ShapnessValue
        {
            get { return shapnessValue; }
            set { shapnessValue = value; }
        }

        public float PeakPureValue
        {
            get { return peakPureValue; }
            set { peakPureValue = value; }
        }

        public float AreaAboveBaseline
        {
            get { return areaAboveBaseline; }
            set { areaAboveBaseline = value; }
        }

        public float AreaAboveZero
        {
            get { return areaAboveZero; }
            set { areaAboveZero = value; }
        }

        public float EstimatedNoise {
            get {
                return estimatedNoise;
            }

            set {
                estimatedNoise = value;
            }
        }

        public float SignalToNoise {
            get {
                return signalToNoise;
            }

            set {
                signalToNoise = value;
            }
        }

        #endregion
    }
}
