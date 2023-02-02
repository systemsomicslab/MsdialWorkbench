using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Algorithm
{
    //spectrum matching is performed by the article of Stephen E. Stein & Donald R. Scott (J Am Soc Mass Spectrom 1994, 5, 859-866) and Stephen Stein (J Am Soc Mass Spectrom 1999, 10, 770-781) 
    //The below comment is from Stephen Stein to describe the actual equation

    //------------------------------
    //The match factor calculations are based on the article by Stephen E. Stein, Donald R. Scott, 
    //"Optimization and Testing of Mass Spectral Library Search Algorithms for Compound Identification", 
    //Journal of the American Society for Mass Spectrometry, Volume 5, Issue 9, September 1994, Pages 859-866.
    //
    //There are two spectrum search types: Identity and Similarity. 
    //Here I describe Identity Normal (which is described in the article by Stein and Scott as composite) and 
    //Similarity Simple (which is described as Dot-Product).

    //To compare your results with those from NIST MS Search you may want to set all Limits in the Library Search Options to ON, 
    //select Apply Limits, and set
    //Minimum Abundance=1
    //Minimum m/z=1
    //Maximum m/z=2000

    //In formulas (see p. 862 of the magazine) for Dot-Product and Composite use sqrt(intensity), 
    //that is, n=0.5; for Identity Normal use m=0.5; for Similarity Simple use m=0
    //Actual Composite calculation is different from that described in the article; it uses n=0.5, m=0 in calculating WX,i (where X= L or U):
    //Let Ri = (WL,i x WU,i-1) / (WL,i-1 x WU,i)
    //Let FR = (sigmaRi > 0: min(Rv1/Ri) x Massi)/(sigmaRi>0: Massi)
    //Note that only peaks for which Ri > 0 are included in both sums, 
    //which means peaks at Massi-1 *and* Massi must be present in both spectra.
    //Identity match factor = (a*Dot.Prod+b*FR)/(a+b), where a=number of non-zero terms in Dot Product calculation and b=number of non-zero terms in FR calculation.
    //Similarity match factor = Dot.Prod.

    //(2) The current MS Search version rounds peak mass to charge ratios to the nearest integer. Using C library functions,
    //int mz = floor( input_mz + 0.5 );
    //which means rounding to the nearest integer, with input_mz=(n+0.5) rounded to (n+1)
    //Multiple peaks with identical rounded to integer m/z values are treated in
    //two different ways, depending on whether the spectrum is ms1 or ms/ms
    //ms1: select the greatest intensity
    //ms/ms: add intensities
    //Match factors are output as (int)(1000*(match factor))
    //Whenever the result=1000, NIST MS Search outputs 999.
    //--------------------------------

    public sealed class GcmsScoring
    {
        private GcmsScoring() { }

        public static float GetRetentionIndexByAlkanes(Dictionary<int, float> retentionIndexDictionary, float retentionTime) {
            var leftCarbon = retentionIndexDictionary.Min(n => n.Key);
            var rightCarbon = retentionIndexDictionary.Max(n => n.Key);
            var leftRtValue = retentionIndexDictionary[leftCarbon];
            var rightRtValue = retentionIndexDictionary[rightCarbon];

            double leftMinDiff = double.MaxValue, rightMinDiff = double.MaxValue;

            if (retentionTime < leftRtValue || rightRtValue < retentionTime)
                return 100.0F * ((float)leftCarbon + (float)(rightCarbon - leftCarbon) * (retentionTime - retentionIndexDictionary[leftCarbon]) / (retentionIndexDictionary[rightCarbon] - retentionIndexDictionary[leftCarbon]));

            foreach (var dict in retentionIndexDictionary) {
                if (dict.Value <= retentionTime && leftMinDiff > Math.Abs(dict.Value - retentionTime)) {
                    leftMinDiff = Math.Abs(dict.Value - retentionTime);
                    leftCarbon = dict.Key;
                }

                if (dict.Value >= retentionTime && rightMinDiff > Math.Abs(dict.Value - retentionTime)) {
                    rightMinDiff = Math.Abs(dict.Value - retentionTime);
                    rightCarbon = dict.Key;
                }
            }

            if (leftCarbon == rightCarbon) {
                return leftCarbon * 100.0F;
            }
            else {
                return 100.0F * ((float)leftCarbon + (float)(rightCarbon - leftCarbon) * (retentionTime - retentionIndexDictionary[leftCarbon]) / (retentionIndexDictionary[rightCarbon] - retentionIndexDictionary[leftCarbon]));
            }
        }

        public static float ConvertKovatsRiToRetentiontime(Dictionary<int, float> retentionIndexDictionary, float retentionIndex) {
            var leftCarbon = retentionIndexDictionary.Min(n => n.Key);
            var rightCarbon = retentionIndexDictionary.Max(n => n.Key);
            var leftRtValue = retentionIndexDictionary[leftCarbon];
            var rightRtValue = retentionIndexDictionary[rightCarbon];
            var putativeCarbonNum = retentionIndex * 0.01F;

            var carbonDiff = (float)(rightCarbon - leftCarbon);
            var rtDiff = rightRtValue - leftRtValue;
            var internalValue = (float)rightCarbon * leftRtValue - (float)leftCarbon * rightRtValue;

            if (putativeCarbonNum < leftCarbon || putativeCarbonNum > rightCarbon)
                return (rtDiff * putativeCarbonNum + internalValue) / carbonDiff;

            double leftMinDiff = double.MaxValue, rightMinDiff = double.MaxValue;
            foreach (var dict in retentionIndexDictionary) {
                if (dict.Key <= putativeCarbonNum && leftMinDiff > Math.Abs(dict.Key - putativeCarbonNum)) {
                    leftMinDiff = Math.Abs(dict.Key - putativeCarbonNum);
                    leftCarbon = dict.Key;
                }

                if (dict.Key >= putativeCarbonNum && rightMinDiff > Math.Abs(dict.Key - putativeCarbonNum)) {
                    rightMinDiff = Math.Abs(dict.Key - putativeCarbonNum);
                    rightCarbon = dict.Key;
                }
            }

            leftRtValue = retentionIndexDictionary[leftCarbon];
            rightRtValue = retentionIndexDictionary[rightCarbon];

            carbonDiff = (float)(rightCarbon - leftCarbon);
            rtDiff = rightRtValue - leftRtValue;
            internalValue = (float)rightCarbon * leftRtValue - (float)leftCarbon * rightRtValue;

            if (carbonDiff == 0) return leftRtValue;
            else
                return (rtDiff * putativeCarbonNum + internalValue) / carbonDiff;
        }

        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
        /// </summary>
        /// <param name="spectrumA">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="spectrumB">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetPresencePercentage(List<Peak> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin, double userMinMass, double userMaxMass)
        {
            if (librarySpectra.Count == 0) return 0;

            double sumM = 0, sumL = 0;
            double minMz = librarySpectra[0].Mz; if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz; if (maxMz > userMaxMass) maxMz = userMaxMass;
            double maxLibIntensity = librarySpectra.Max(n => n.Intensity);
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;
            int libCounter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++)
                {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }
                if (sumL >= 0.01 * maxLibIntensity) {
                    libCounter++;
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++)
                {
                    if (measuredSpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin)
                        sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0 && sumL >= 0.01 * maxLibIntensity)
                {
                    counter++;
                }

                if (focusedMz + bin > librarySpectra[librarySpectra.Count - 1].Mz) break;
                focusedMz = librarySpectra[remaindIndexL].Mz;
            }

            if (libCounter == 0) return 0;
            else
                return (double)counter / (double)libCounter;
        }

        /// <summary>
        /// This program will return so called reverse dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS with respect to library spectrum will be calculated in this method.
        /// </summary>
        /// <param name="spectrumA">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="spectrumB">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetReverseDotProduct(List<Peak> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin, double userMinMass, double userMaxMass)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumL = 0;

            if (librarySpectra.Count == 0) return 0;

            double minMz = librarySpectra[0].Mz; if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz; if (maxMz > userMaxMass) maxMz = userMaxMass;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++)
                {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++)
                {
                    if (measuredSpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin)
                        sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM <= 0)
                {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;
                }
                else
                {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;

                    counter++;
                }

                if (focusedMz + bin > librarySpectra[librarySpectra.Count - 1].Mz) break;
                focusedMz = librarySpectra[remaindIndexL].Mz;
            }

            if (baseM == 0 || baseR == 0) return 0;

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }
           
            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var nCoeff = 0.5;
            var mCoeff = 0.0;
            var cutoff = 0.01;

            for (int i = 0; i < measuredMassList.Count; i++) {
                if (referenceMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        /// <summary>
        /// This program will return so called dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS will be calculated in this method.
        /// </summary>
        /// <param name="measuredSpectra">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="librarySpectra">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetDotProduct(List<Peak> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin, double userMinMass, double userMaxMass)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (measuredSpectra.Count == 0) return 0;
            if (librarySpectra.Count == 0) return 0;

            double minMz = Math.Min(measuredSpectra[0].Mz, librarySpectra[0].Mz); if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = Math.Max(measuredSpectra[measuredSpectra.Count - 1].Mz, librarySpectra[librarySpectra.Count - 1].Mz); if (maxMz > userMaxMass) maxMz = userMaxMass;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz)
            {
                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++)
                {
                    if (measuredSpectra[i].Mz < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin)
                        sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++)
                {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumR += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumM <= 0 && sumR > 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR)
                        baseR = sumR;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }

                if (focusedMz + bin > Math.Max(measuredSpectra[measuredSpectra.Count - 1].Mz, librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin > librarySpectra[remaindIndexL].Mz && focusedMz + bin <= measuredSpectra[remaindIndexM].Mz) 
                    focusedMz = measuredSpectra[remaindIndexM].Mz;
                else if (focusedMz + bin <= librarySpectra[remaindIndexL].Mz && focusedMz + bin > measuredSpectra[remaindIndexM].Mz) 
                    focusedMz = librarySpectra[remaindIndexL].Mz;
                else 
                    focusedMz = Math.Min(measuredSpectra[remaindIndexM].Mz, librarySpectra[remaindIndexL].Mz);
            }

            if (baseM == 0 || baseR == 0) return 0;

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++)
            {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;
            
            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var nCoeff = 0.5;
            var mCoeff = 1.0;
            var cutoff = 0.01;

            for (int i = 0; i < measuredMassList.Count; i++) {
                
                if (measuredMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        /// <summary>
        /// This method is to calculate the similarity of retention time differences or precursor ion difference from the library information as described in the previous report.
        /// Tsugawa, H. et al. Anal.Chem. 85, 5191-5199, 2013.
        /// </summary>
        /// <param name="actual">
        /// Add the experimental m/z or retention time.
        /// </param>
        /// <param name="reference">
        /// Add the theoretical m/z or library's retention time.
        /// </param>
        /// <param name="tolrance">
        /// Add the user-defined search tolerance.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetGaussianSimilarity(float actual, float reference, float tolrance)
        {
            return Math.Exp(-0.5 * Math.Pow((actual - reference) / tolrance, 2));
        }

        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
        /// </summary>
        /// <param name="spectrumA">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="spectrumB">
        /// Add another MS/MS spectrum.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetPresencePercentage(List<Peak> spectrumA, List<Peak> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            if (spectrumB.Count == 0) return 0;

            double sumM = 0, sumL = 0;
            double minMz = spectrumB[0].Mz; if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = spectrumB[spectrumB.Count - 1].Mz; if (maxMz > userMaxMass) maxMz = userMaxMass;
            double maxSpecBIntensity = spectrumB.Max(n => n.Intensity);
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;
            int bCounter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < spectrumB.Count; i++)
                {
                    if (spectrumB[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumB[i].Mz && spectrumB[i].Mz < focusedMz + bin)
                        sumL += spectrumB[i].Intensity;
                    else { remaindIndexL = i; break; }
                }
                if (sumL >= 0.01 * maxSpecBIntensity) {
                    bCounter++;
                }

                sumM = 0;
                for (int i = remaindIndexM; i < spectrumA.Count; i++)
                {
                    if (spectrumA[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumA[i].Mz && spectrumA[i].Mz < focusedMz + bin)
                        sumM += spectrumA[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0 && sumL >= 0.01 * maxSpecBIntensity)
                {
                    counter++;
                }

                if (focusedMz + bin > spectrumB[spectrumB.Count - 1].Mz) break;
                focusedMz = spectrumB[remaindIndexL].Mz;
            }

            if (bCounter == 0) return 0;
            else
                return (double)counter / (double)bCounter;
        }

        /// <summary>
        /// This program will return so called reverse dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS with respect to library spectrum will be calculated in this method.
        /// </summary>
        /// <param name="spectrumA">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="spectrumB">
        /// Add another MS/MS spectrum.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetReverseDotProduct(List<Peak> spectrumA, List<Peak> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumL = 0;

            if (spectrumB.Count == 0) return 0;

            double minMz = spectrumB[0].Mz; if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = spectrumB[spectrumB.Count - 1].Mz; if (maxMz > userMaxMass) maxMz = userMaxMass;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < spectrumB.Count; i++)
                {
                    if (spectrumB[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumB[i].Mz && spectrumB[i].Mz < focusedMz + bin)
                        sumL += spectrumB[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < spectrumA.Count; i++)
                {
                    if (spectrumA[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumA[i].Mz && spectrumA[i].Mz < focusedMz + bin)
                        sumM += spectrumA[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM <= 0)
                {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;
                }
                else
                {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;

                    counter++;
                }

                if (focusedMz + bin > spectrumB[spectrumB.Count - 1].Mz) break;
                focusedMz = spectrumB[remaindIndexL].Mz;
            }

            if (baseM == 0 || baseR == 0) return 0;

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;

            for (int i = 0; i < measuredMassList.Count; i++) {
                if (referenceMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        /// <summary>
        /// This program will return so called dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS will be calculated in this method.
        /// </summary>
        /// <param name="measuredSpectra">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="librarySpectra">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetDotProduct(List<Peak> measuredSpectra, List<Peak> librarySpectra, float bin, double userMinMass, double userMaxMass)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (measuredSpectra.Count == 0) return 0;
            if (librarySpectra.Count == 0) return 0;

            double minMz = Math.Min(measuredSpectra[0].Mz, librarySpectra[0].Mz); if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = Math.Max(measuredSpectra[measuredSpectra.Count - 1].Mz, librarySpectra[librarySpectra.Count - 1].Mz); if (maxMz > userMaxMass) maxMz = userMaxMass;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz)
            {
                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++)
                {
                    if (measuredSpectra[i].Mz < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin)
                        sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++)
                {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumR += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumM <= 0 && sumR > 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }

                if (focusedMz + bin > Math.Max(measuredSpectra[measuredSpectra.Count - 1].Mz, librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin > librarySpectra[remaindIndexL].Mz && focusedMz + bin <= measuredSpectra[remaindIndexM].Mz) focusedMz = measuredSpectra[remaindIndexM].Mz;
                else if (focusedMz + bin <= librarySpectra[remaindIndexL].Mz && focusedMz + bin > measuredSpectra[remaindIndexM].Mz) focusedMz = librarySpectra[remaindIndexL].Mz;
                else focusedMz = Math.Min(measuredSpectra[remaindIndexM].Mz, librarySpectra[remaindIndexL].Mz);
            }

            if (baseM == 0 || baseR == 0) return 0;

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;

            for (int i = 0; i < measuredMassList.Count; i++) {
                if (measuredMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
        /// </summary>
        /// <param name="spectrumA">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="spectrumB">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetPresencePercentage(List<MzIntensityCommentBean> spectrumA, List<MzIntensityCommentBean> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            if (spectrumB.Count == 0) return 0;

            double sumM = 0, sumL = 0;
            double minMz = spectrumB[0].Mz; if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = spectrumB[spectrumB.Count - 1].Mz; if (maxMz > userMaxMass) maxMz = userMaxMass;
            double maxSpecBIntensity = spectrumB.Max(n => n.Intensity);
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;
            int bCounter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < spectrumB.Count; i++) {
                    if (spectrumB[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumB[i].Mz && spectrumB[i].Mz < focusedMz + bin)
                        sumL += spectrumB[i].Intensity;
                    else { remaindIndexL = i; break; }
                }
                if (sumL >= 0.01 * maxSpecBIntensity) {
                    bCounter++;
                }

                sumM = 0;
                for (int i = remaindIndexM; i < spectrumA.Count; i++) {
                    if (spectrumA[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumA[i].Mz && spectrumA[i].Mz < focusedMz + bin)
                        sumM += spectrumA[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0 && sumL >= 0.01 * maxSpecBIntensity) {
                    counter++;
                }

                if (focusedMz + bin > spectrumB[spectrumB.Count - 1].Mz) break;
                focusedMz = spectrumB[remaindIndexL].Mz;
            }

            if (bCounter == 0) return 0;
            else
                return (double)counter / (double)bCounter;
        }

        /// <summary>
        /// This program will return so called reverse dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS with respect to library spectrum will be calculated in this method.
        /// </summary>
        /// <param name="spectrumA">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="spectrumB">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetReverseDotProduct(List<MzIntensityCommentBean> spectrumA, List<MzIntensityCommentBean> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumL = 0;

            if (spectrumB.Count == 0) return 0;

            double minMz = spectrumB[0].Mz; if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = spectrumB[spectrumB.Count - 1].Mz; if (maxMz > userMaxMass) maxMz = userMaxMass;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < spectrumB.Count; i++) {
                    if (spectrumB[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumB[i].Mz && spectrumB[i].Mz < focusedMz + bin)
                        sumL += spectrumB[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < spectrumA.Count; i++) {
                    if (spectrumA[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumA[i].Mz && spectrumA[i].Mz < focusedMz + bin)
                        sumM += spectrumA[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM <= 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;

                    counter++;
                }

                if (focusedMz + bin > spectrumB[spectrumB.Count - 1].Mz) break;
                focusedMz = spectrumB[remaindIndexL].Mz;
            }

            if (baseM == 0 || baseR == 0) return 0;

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;

            for (int i = 0; i < measuredMassList.Count; i++) {
                if (referenceMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        /// <summary>
        /// This program will return so called dot product similarity as described in the previous resport.
        /// Stein, S. E. An Integrated Method for Spectrum Extraction. J.Am.Soc.Mass.Spectrom, 10, 770-781, 1999.
        /// The spectrum similarity of MS/MS will be calculated in this method.
        /// </summary>
        /// <param name="spectrumA">
        /// Add the experimental MS/MS spectrum.
        /// </param>
        /// <param name="spectrumB">
        /// Add the theoretical MS/MS spectrum. The theoretical MS/MS spectrum is supposed to be retreived in MSP parcer.
        /// </param>
        /// <param name="bin">
        /// Add the bin value to merge the abundance of m/z.
        /// </param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetDotProduct(List<MzIntensityCommentBean> spectrumA, List<MzIntensityCommentBean> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (spectrumA.Count == 0) return 0;
            if (spectrumB.Count == 0) return 0;

            double minMz = Math.Min(spectrumA[0].Mz, spectrumB[0].Mz); if (minMz < userMinMass) minMz = userMinMass;
            double maxMz = Math.Max(spectrumA[spectrumA.Count - 1].Mz, spectrumB[spectrumB.Count - 1].Mz); if (maxMz > userMaxMass) maxMz = userMaxMass;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumM = 0;
                for (int i = remaindIndexM; i < spectrumA.Count; i++) {
                    if (spectrumA[i].Mz < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= spectrumA[i].Mz && spectrumA[i].Mz < focusedMz + bin)
                        sumM += spectrumA[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < spectrumB.Count; i++) {
                    if (spectrumB[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= spectrumB[i].Mz && spectrumB[i].Mz < focusedMz + bin)
                        sumR += spectrumB[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumM <= 0 && sumR > 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }

                if (focusedMz + bin > Math.Max(spectrumA[spectrumA.Count - 1].Mz, spectrumB[spectrumB.Count - 1].Mz)) break;
                if (focusedMz + bin > spectrumB[remaindIndexL].Mz && focusedMz + bin <= spectrumA[remaindIndexM].Mz) focusedMz = spectrumA[remaindIndexM].Mz;
                else if (focusedMz + bin <= spectrumB[remaindIndexL].Mz && focusedMz + bin > spectrumA[remaindIndexM].Mz) focusedMz = spectrumB[remaindIndexL].Mz;
                else focusedMz = Math.Min(spectrumA[remaindIndexM].Mz, spectrumB[remaindIndexL].Mz);
            }

            if (baseM == 0 || baseR == 0) return 0;

            var eSpectrumCounter = 0;
            var lSpectrumCounter = 0;
            for (int i = 0; i < measuredMassList.Count; i++) {
                measuredMassList[i][1] = measuredMassList[i][1] / baseM;
                referenceMassList[i][1] = referenceMassList[i][1] / baseR;
                sumMeasure += measuredMassList[i][1];
                sumReference += referenceMassList[i][1];

                if (measuredMassList[i][1] > 0.1) eSpectrumCounter++;
                if (referenceMassList[i][1] > 0.1) lSpectrumCounter++;
            }

            var peakCountPenalty = 1.0;
            if (lSpectrumCounter == 1) peakCountPenalty = 0.75;
            else if (lSpectrumCounter == 2) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 3) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 4) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            var cutoff = 0.01;

            for (int i = 0; i < measuredMassList.Count; i++) {
                if (measuredMassList[i][1] < cutoff)
                    continue;

                scalarM += measuredMassList[i][1] * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] * referenceMassList[i][0];
                covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]) * measuredMassList[i][0];
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        public static double GetEiSpectraSimilarity(List<Peak> spectrumA, List<MzIntensityCommentBean> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            var dotProduct = GetDotProduct(spectrumA, spectrumB, bin, userMinMass, userMaxMass);
            var revDotProd = GetReverseDotProduct(spectrumA, spectrumB, bin, userMinMass, userMaxMass);
            var percentSpec = GetPresencePercentage(spectrumA, spectrumB, bin, userMinMass, userMaxMass);

            return (3 * dotProduct + 2 * revDotProd + percentSpec) / 6.0;
        }

        public static double GetEiSpectraSimilarity(List<Peak> spectrumA, List<Peak> spectrumB, float bin, double userMinMass, double userMaxMass)
        {
            var dotProduct = GetDotProduct(spectrumA, spectrumB, bin, userMinMass, userMaxMass);
            var revDotProd = GetReverseDotProduct(spectrumA, spectrumB, bin, userMinMass, userMaxMass);
            var percentSpec = GetPresencePercentage(spectrumA, spectrumB, bin, userMinMass, userMaxMass);

            return (3 * dotProduct + 2 * revDotProd + percentSpec) / 6.0;
        }

        public static double GetEiSpectraSimilarity(double dotProduct, double revDotProd, double precentSpec)
        {
            return (3 * dotProduct + 2 * revDotProd + precentSpec) / 6.0;
        }

        public static double GetTotalSimilarity(double rtSimilarity, double eiSimilarity, bool isUseRT)
        {
            if (rtSimilarity < 0 || !isUseRT)
            {
                return eiSimilarity;
            }
            else
            {
                return (0.6 * eiSimilarity + 0.4 * rtSimilarity);
            }
        }
    }
}
