using Riken.Metabolomics.Lipidomics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class SpectralSimilarity
    {
        private SpectralSimilarity() { }

        /// <summary>
        /// mspDB should be sorted by precursor mz values
        /// 
        /// </summary>
        /// <param name="precursorMz"></param>
        /// <param name="precursorTolerance"></param>
        /// <param name="rt"></param>
        /// <param name="rtTolerance"></param>
        /// <param name="ms2tolerance"></param>
        /// <param name="spectrum"></param>
        /// <param name="mspDB"></param>
        /// <returns></returns>
        public static List<int> GetHighSimilarityMspIDs(float precursorMz, float ms1tolerance,
            float rt, float rtTolerance, 
            float ms2tolerance,
            List<double[]> spectrum,
            List<MspFormatCompoundInformationBean> mspDB, 
            float cutoff,
            TargetOmics targetOmics, 
            bool isUseRT)
        {
            var cMspIDs = new List<int[]>();

            var startIndex = GetDatabaseStartIndex(precursorMz, ms1tolerance, mspDB);
            double totalSimilarity = 0, spectraSimilarity = -1, reverseSearchSimilarity = -1, rtSimilarity = -1, accurateMassSimilarity = -1, presenseSimilarity = -1, isotopeSimilarity = -1;

            for (int i = startIndex; i < mspDB.Count; i++) {
                if (mspDB[i].PrecursorMz > precursorMz + ms1tolerance) break;
                if (mspDB[i].PrecursorMz < precursorMz - ms1tolerance) continue;

                rtSimilarity = -1; isotopeSimilarity = -1; spectraSimilarity = -1; reverseSearchSimilarity = -1; accurateMassSimilarity = -1; presenseSimilarity = -1;

                accurateMassSimilarity = Math.Exp(-0.5 * Math.Pow((precursorMz - mspDB[i].PrecursorMz) / ms1tolerance, 2));
                if (mspDB[i].RetentionTime >= 0) rtSimilarity = Math.Exp(-0.5 * Math.Pow((rt - mspDB[i].RetentionTime) / rtTolerance, 2));
                if (spectrum.Count != 0) {
                    spectraSimilarity = GetMassSpectraSimilarity(spectrum, mspDB[i].MzIntensityCommentBeanList, ms2tolerance);
                    reverseSearchSimilarity = GetReverseSearchSimilarity(spectrum, mspDB[i].MzIntensityCommentBeanList, ms2tolerance);
                    presenseSimilarity = GetPresenceSimilarity(spectrum, mspDB[i].MzIntensityCommentBeanList, ms2tolerance);
                }

                var spectrumPenalty = false;
                if (mspDB[i].MzIntensityCommentBeanList != null && mspDB[i].MzIntensityCommentBeanList.Count <= 1) spectrumPenalty = true;

                totalSimilarity = GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity,
                    spectraSimilarity, reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, targetOmics, isUseRT);

                if (totalSimilarity * 100 > cutoff)
                    cMspIDs.Add(new int[] { mspDB[i].Id, (int)(totalSimilarity * 1000000) });
            }

            return cMspIDs.OrderByDescending(n => n[1]).Select(n => n[0]).ToList();
        }

        public static ObservableCollection<RawData> GetHighSimilarityQueries(float precursorMz, float ms1tolerance,
            float rt, float rtTolerance,
            float ms2tolerance,
            List<double[]> spectrum,
            List<RawData> queries,
            float cutoff,
            TargetOmics targetOmics,
            bool isUseRT) {

            var startIndex = GetDatabaseStartIndex(precursorMz, ms1tolerance, queries);
            double totalSimilarity = 0, spectraSimilarity = -1, reverseSearchSimilarity = -1, rtSimilarity = -1, accurateMassSimilarity = -1, presenseSimilarity = -1, isotopeSimilarity = -1;
            var selectedQueries = new List<RawData>();

            for (int i = startIndex; i < queries.Count; i++) {
                if (queries[i].PrecursorMz > precursorMz + ms1tolerance) break;
                if (queries[i].PrecursorMz < precursorMz - ms1tolerance) continue;
                if (queries[i].Ms2Spectrum.PeakList == null || queries[i].Ms2Spectrum.PeakList.Count == 0) continue;

                rtSimilarity = -1; isotopeSimilarity = -1; spectraSimilarity = -1; reverseSearchSimilarity = -1; accurateMassSimilarity = -1; presenseSimilarity = -1;

                accurateMassSimilarity = Math.Exp(-0.5 * Math.Pow((precursorMz - queries[i].PrecursorMz) / ms1tolerance, 2));
                if (queries[i].RetentionTime >= 0) rtSimilarity = Math.Exp(-0.5 * Math.Pow((rt - queries[i].RetentionTime) / rtTolerance, 2));
                if (spectrum.Count != 0) {
                    spectraSimilarity = GetMassSpectraSimilarity(spectrum, queries[i].Ms2Spectrum.PeakList, ms2tolerance);
                    reverseSearchSimilarity = GetReverseSearchSimilarity(spectrum, queries[i].Ms2Spectrum.PeakList, ms2tolerance);
                    presenseSimilarity = GetPresenceSimilarity(spectrum, queries[i].Ms2Spectrum.PeakList, ms2tolerance);
                }

                var spectrumPenalty = false;
                if (queries[i].Ms2Spectrum.PeakList != null && queries[i].Ms2Spectrum.PeakList.Count <= 1) spectrumPenalty = true;

                totalSimilarity = GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity,
                    spectraSimilarity, reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, targetOmics, isUseRT);

                if (totalSimilarity * 100 > cutoff) {
                    queries[i].Similarity = totalSimilarity * 100;
                    selectedQueries.Add(queries[i]);
                }
            }

            return new ObservableCollection<RawData>(selectedQueries.OrderByDescending(n => n.Similarity));
        }

        public static int GetDatabaseStartIndex(float accurateMass, float ms1Tolerance, List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList)
        {
            float targetMass = accurateMass - ms1Tolerance;
            int startIndex = 0, endIndex = mspFormatCompoundInformationBeanList.Count - 1;
            if (targetMass > mspFormatCompoundInformationBeanList[endIndex].PrecursorMz) return endIndex;

            int counter = 0;
            while (counter < 10) {
                if (mspFormatCompoundInformationBeanList[startIndex].PrecursorMz <= targetMass && targetMass < mspFormatCompoundInformationBeanList[(startIndex + endIndex) / 2].PrecursorMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (mspFormatCompoundInformationBeanList[(startIndex + endIndex) / 2].PrecursorMz <= targetMass && targetMass < mspFormatCompoundInformationBeanList[endIndex].PrecursorMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetDatabaseStartIndex(float accurateMass, float ms1Tolerance, List<RawData> queries) {
            float targetMass = accurateMass - ms1Tolerance;
            int startIndex = 0, endIndex = queries.Count - 1;
            if (targetMass > queries[endIndex].PrecursorMz) return endIndex;

            int counter = 0;
            while (counter < 10) {
                if (queries[startIndex].PrecursorMz <= targetMass && targetMass < queries[(startIndex + endIndex) / 2].PrecursorMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (queries[(startIndex + endIndex) / 2].PrecursorMz <= targetMass && targetMass < queries[endIndex].PrecursorMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }


        /// <summary>
        /// This method returns the presence similarity (% of matched fragments) between the experimental MS/MS spectrum and the standard MS/MS spectrum.
        /// So, this program will calculate how many fragments of library spectrum are found in the experimental spectrum and will return the %.
        /// double[] [0]m/z[1]intensity
        /// 
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
        public static double GetPresenceSimilarity(List<double[]> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin)
        {
            if (librarySpectra.Count == 0) return 0;

            double sumM = 0, sumL = 0;
            double minMz = librarySpectra[0].Mz;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz;
            double focusedMz = minMz;
            double maxLibIntensity = librarySpectra.Max(n => n.Intensity);
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;
            int libCounter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumL >= 0.01 * maxLibIntensity) {
                    libCounter++;
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin)
                        sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0 && sumL >= 0.01 * maxLibIntensity) {
                    counter++;
                }

                if (focusedMz + bin > librarySpectra[librarySpectra.Count - 1].Mz) break;
                focusedMz = librarySpectra[remaindIndexL].Mz;
            }

            if (libCounter == 0) return 0;
            else
                return (double)counter / (double)libCounter;
        }

        public static double GetPresenceSimilarity(List<double[]> measuredSpectra, List<Peak> librarySpectra, float bin) {
            if (librarySpectra.Count == 0) return 0;

            double sumM = 0, sumL = 0;
            double minMz = librarySpectra[0].Mz;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz;
            double focusedMz = minMz;
            double maxLibIntensity = librarySpectra.Max(n => n.Intensity);
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;
            int libCounter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumL >= 0.01 * maxLibIntensity) {
                    libCounter++;
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin)
                        sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0 && sumL >= 0.01 * maxLibIntensity) {
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
        public static double GetReverseSearchSimilarity(List<double[]> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumL = 0;

            if (librarySpectra.Count == 0) return 0;

            double minMz = librarySpectra[0].Mz;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin)
                        sumM += measuredSpectra[i][1];
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

        public static double GetReverseSearchSimilarity(List<double[]> measuredSpectra, List<Peak> librarySpectra, float bin) {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumL = 0;

            if (librarySpectra.Count == 0) return 0;

            double minMz = librarySpectra[0].Mz;
            double maxMz = librarySpectra[librarySpectra.Count - 1].Mz;
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin) continue;
                    else if (focusedMz - bin <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin)
                        sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) continue;
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin)
                        sumM += measuredSpectra[i][1];
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
        public static double GetMassSpectraSimilarity(List<double[]> measuredSpectra, List<MzIntensityCommentBean> librarySpectra, float bin)
        {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (measuredSpectra.Count == 0) return 0;
            if (librarySpectra.Count == 0) return 0;

            double minMz = Math.Min(measuredSpectra[0][0], librarySpectra[0].Mz);
            double maxMz = Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz);
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin) sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
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

                if (focusedMz + bin > Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin > librarySpectra[remaindIndexL].Mz && focusedMz + bin <= measuredSpectra[remaindIndexM][0])
                    focusedMz = measuredSpectra[remaindIndexM][0];
                else if (focusedMz + bin <= librarySpectra[remaindIndexL].Mz && focusedMz + bin > measuredSpectra[remaindIndexM][0])
                    focusedMz = librarySpectra[remaindIndexL].Mz;
                else
                    focusedMz = Math.Min(measuredSpectra[remaindIndexM][0], librarySpectra[remaindIndexL].Mz);
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

        public static double GetMassSpectraSimilarity(List<double[]> measuredSpectra, List<Peak> librarySpectra, float bin) {
            double scalarM = 0, scalarR = 0, covariance = 0;
            double sumM = 0, sumR = 0;

            if (measuredSpectra.Count == 0) return 0;
            if (librarySpectra.Count == 0) return 0;

            double minMz = Math.Min(measuredSpectra[0][0], librarySpectra[0].Mz);
            double maxMz = Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz);
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            double sumMeasure = 0, sumReference = 0, baseM = double.MinValue, baseR = double.MinValue;

            while (focusedMz <= maxMz) {
                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i][0] < focusedMz - bin) { continue; }
                    else if (focusedMz - bin <= measuredSpectra[i][0] && measuredSpectra[i][0] < focusedMz + bin) sumM += measuredSpectra[i][1];
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
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

                if (focusedMz + bin > Math.Max(measuredSpectra[measuredSpectra.Count - 1][0], librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin > librarySpectra[remaindIndexL].Mz && focusedMz + bin <= measuredSpectra[remaindIndexM][0])
                    focusedMz = measuredSpectra[remaindIndexM][0];
                else if (focusedMz + bin <= librarySpectra[remaindIndexL].Mz && focusedMz + bin > measuredSpectra[remaindIndexM][0])
                    focusedMz = librarySpectra[remaindIndexL].Mz;
                else
                    focusedMz = Math.Min(measuredSpectra[remaindIndexM][0], librarySpectra[remaindIndexL].Mz);
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
        /// MS-DIAL program utilizes the total similarity score to rank the compound candidates.
        /// This method is to calculate it from four scores including RT, isotopic ratios, m/z, and MS/MS similarities.
        /// </summary>
        /// <param name="accurateMassSimilarity"></param>
        /// <param name="rtSimilarity"></param>
        /// <param name="isotopeSimilarity"></param>
        /// <param name="spectraSimilarity"></param>
        /// <param name="reverseSearchSimilarity"></param>
        /// <param name="presenceSimilarity"></param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity,
            double spectraSimilarity, double reverseSearchSimilarity, double presenceSimilarity, bool spectrumPenalty, TargetOmics targetOmics, bool isUseRT)
        {
            var dotProductFactor = 3.0;
            var revesrseDotProdFactor = 2.0;
            var presensePercentageFactor = 1.0;

            var msmsFactor = 2.0;
            var rtFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.5;

            if (targetOmics == TargetOmics.Lipidomics) {
                dotProductFactor = 1.0; revesrseDotProdFactor = 2.0; presensePercentageFactor = 3.0; msmsFactor = 1.5; rtFactor = 0.5;
            }

            var msmsSimilarity =
                (dotProductFactor * spectraSimilarity + revesrseDotProdFactor * reverseSearchSimilarity + presensePercentageFactor * presenceSimilarity) /
                (dotProductFactor + revesrseDotProdFactor + presensePercentageFactor);

            if (spectrumPenalty == true && targetOmics == TargetOmics.Metablomics) msmsSimilarity = msmsSimilarity * 0.5;

            if (!isUseRT) {
                if (isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
                }
            }
            else {
                if (rtSimilarity < 0 && isotopeSimilarity < 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity)
                        / (msmsFactor + massFactor);
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + isotopeFactor);
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0) {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity)
                        / (msmsFactor + massFactor + rtFactor);
                }
                else {
                    return (msmsFactor * msmsSimilarity + massFactor * accurateMassSimilarity + rtFactor * rtSimilarity + isotopeFactor * isotopeSimilarity)
                        / (msmsFactor + massFactor + rtFactor + isotopeFactor);
                }
            }
        }

        /// <summary>
        /// MS-DIAL program also calculate the total similarity score without the MS/MS similarity scoring.
        /// It means that the total score will be calculated from RT, m/z, and isotopic similarities.
        /// </summary>
        /// <param name="accurateMassSimilarity"></param>
        /// <param name="rtSimilarity"></param>
        /// <param name="isotopeSimilarity"></param>
        /// <returns>
        /// The similarity score which is standadized from 0 (no similarity) to 1 (consistency) will be return.
        /// </returns>
        public static double GetTotalSimilarity(double accurateMassSimilarity, double rtSimilarity, double isotopeSimilarity, bool isUseRT)
        {
            if (!isUseRT) {
                if (isotopeSimilarity < 0) {
                    return accurateMassSimilarity;
                }
                else {
                    return (accurateMassSimilarity + 0.5 * isotopeSimilarity) / 1.5;
                }
            }
            else {
                if (rtSimilarity < 0 && isotopeSimilarity < 0) {
                    return accurateMassSimilarity;
                }
                else if (rtSimilarity < 0 && isotopeSimilarity >= 0) {
                    return (accurateMassSimilarity + 0.5 * isotopeSimilarity) / 1.5;
                }
                else if (isotopeSimilarity < 0 && rtSimilarity >= 0) {
                    return (accurateMassSimilarity + rtSimilarity) * 0.5;
                }
                else {
                    return (accurateMassSimilarity + rtSimilarity + 0.5 * isotopeSimilarity) * 0.4;
                }
            }
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
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++)
                {
                    if (librarySpectra[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin * 0.5) sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++)
                {
                    if (measuredSpectra[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin * 0.5) sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0)
                {
                    counter++;
                }

                if (focusedMz + bin * 0.5 > librarySpectra[librarySpectra.Count - 1].Mz) break;
                focusedMz = librarySpectra[remaindIndexL].Mz;
            }

            return (double)counter / (double)librarySpectra.Count;
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
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin * 0.5) sumL += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin * 0.5) sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM <= 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL * 0.5 });
                    if (sumL * 0.5 > baseR) baseR = sumL * 0.5;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;

                    counter++;
                }

                if (focusedMz + bin * 0.5 > librarySpectra[librarySpectra.Count - 1].Mz) break;
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
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.97;

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

                //scalarM += measuredMassList[i][1];
                //scalarR += referenceMassList[i][1];
                //covariance += Math.Sqrt(measuredMassList[i][1] * referenceMassList[i][1]);
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
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i].Mz < focusedMz - bin * 0.5) { continue; }
                    else if (focusedMz - bin * 0.5 <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin * 0.5) sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin * 0.5) sumR += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumM <= 0 && sumR > 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR * 0.5 });
                    if (sumR * 0.5 > baseR) baseR = sumR * 0.5;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }

                if (focusedMz + bin * 0.5 > Math.Max(measuredSpectra[measuredSpectra.Count - 1].Mz, librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin * 0.5 > librarySpectra[remaindIndexL].Mz && focusedMz + bin * 0.5 <= measuredSpectra[remaindIndexM].Mz)
                    focusedMz = measuredSpectra[remaindIndexM].Mz;
                else if (focusedMz + bin * 0.5 <= librarySpectra[remaindIndexL].Mz && focusedMz + bin * 0.5 > measuredSpectra[remaindIndexM].Mz)
                    focusedMz = librarySpectra[remaindIndexL].Mz;
                else
                    focusedMz = Math.Min(measuredSpectra[remaindIndexM].Mz, librarySpectra[remaindIndexL].Mz);
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
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.97;

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

                //scalarM += measuredMassList[i][1] / (1 + wM * measuredMassList[i][1]) * measuredMassList[i][0];
                //scalarR += referenceMassList[i][1] / (1 + wR * referenceMassList[i][1]) * referenceMassList[i][0];
                //covariance += measuredMassList[i][0] * Math.Sqrt(measuredMassList[i][1] / (1 + wM * measuredMassList[i][1]) * referenceMassList[i][1] / (1 + wR * referenceMassList[i][1]));
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
            double focusedMz = minMz;
            int remaindIndexM = 0, remaindIndexL = 0;
            int counter = 0;

            List<double[]> measuredMassList = new List<double[]>();
            List<double[]> referenceMassList = new List<double[]>();

            while (focusedMz <= maxMz)
            {
                sumL = 0;
                for (int i = remaindIndexL; i < spectrumB.Count; i++)
                {
                    if (spectrumB[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= spectrumB[i].Mz && spectrumB[i].Mz < focusedMz + bin * 0.5) sumL += spectrumB[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < spectrumA.Count; i++)
                {
                    if (spectrumA[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= spectrumA[i].Mz && spectrumA[i].Mz < focusedMz + bin * 0.5) sumM += spectrumA[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM > 0)
                {
                    counter++;
                }

                if (focusedMz + bin * 0.5 > spectrumB[spectrumB.Count - 1].Mz) break;
                focusedMz = spectrumB[remaindIndexL].Mz;
            }

            return (double)counter / (double)spectrumB.Count;
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
                for (int i = remaindIndexL; i < spectrumB.Count; i++) {
                    if (spectrumB[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= spectrumB[i].Mz && spectrumB[i].Mz < focusedMz + bin * 0.5) sumL += spectrumB[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                sumM = 0;
                for (int i = remaindIndexM; i < spectrumA.Count; i++) {
                    if (spectrumA[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= spectrumA[i].Mz && spectrumA[i].Mz < focusedMz + bin * 0.5) sumM += spectrumA[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                if (sumM <= 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL * 0.5 });
                    if (sumL * 0.5 > baseR) baseR = sumL * 0.5;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumL });
                    if (sumL > baseR) baseR = sumL;

                    counter++;
                }

                if (focusedMz + bin * 0.5 > spectrumB[spectrumB.Count - 1].Mz) break;
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
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            for (int i = 0; i < measuredMassList.Count; i++) {
                scalarM += measuredMassList[i][1] / (1 + wM * measuredMassList[i][1]) * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] / (1 + wR * referenceMassList[i][1]) * referenceMassList[i][0];
                covariance += measuredMassList[i][0] * Math.Sqrt(measuredMassList[i][1] / (1 + wM * measuredMassList[i][1]) * referenceMassList[i][1] / (1 + wR * referenceMassList[i][1]));
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
                for (int i = remaindIndexM; i < measuredSpectra.Count; i++) {
                    if (measuredSpectra[i].Mz < focusedMz - bin * 0.5) { continue; }
                    else if (focusedMz - bin * 0.5 <= measuredSpectra[i].Mz && measuredSpectra[i].Mz < focusedMz + bin * 0.5) sumM += measuredSpectra[i].Intensity;
                    else { remaindIndexM = i; break; }
                }

                sumR = 0;
                for (int i = remaindIndexL; i < librarySpectra.Count; i++) {
                    if (librarySpectra[i].Mz < focusedMz - bin * 0.5) continue;
                    else if (focusedMz - bin * 0.5 <= librarySpectra[i].Mz && librarySpectra[i].Mz < focusedMz + bin * 0.5) sumR += librarySpectra[i].Intensity;
                    else { remaindIndexL = i; break; }
                }

                if (sumM <= 0 && sumR > 0) {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR * 0.5 });
                    if (sumR * 0.5 > baseR) baseR = sumR * 0.5;
                }
                else {
                    measuredMassList.Add(new double[] { focusedMz, sumM });
                    if (sumM > baseM) baseM = sumM;

                    referenceMassList.Add(new double[] { focusedMz, sumR });
                    if (sumR > baseR) baseR = sumR;
                }

                if (focusedMz + bin * 0.5 > Math.Max(measuredSpectra[measuredSpectra.Count - 1].Mz, librarySpectra[librarySpectra.Count - 1].Mz)) break;
                if (focusedMz + bin * 0.5 > librarySpectra[remaindIndexL].Mz && focusedMz + bin * 0.5 <= measuredSpectra[remaindIndexM].Mz)
                    focusedMz = measuredSpectra[remaindIndexM].Mz;
                else if (focusedMz + bin * 0.5 <= librarySpectra[remaindIndexL].Mz && focusedMz + bin * 0.5 > measuredSpectra[remaindIndexM].Mz)
                    focusedMz = librarySpectra[remaindIndexL].Mz;
                else
                    focusedMz = Math.Min(measuredSpectra[remaindIndexM].Mz, librarySpectra[remaindIndexL].Mz);
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
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.88;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.94;
            else if (lSpectrumCounter == 1) peakCountPenalty = 0.97;

            double wM, wR;

            if (sumMeasure - 0.5 == 0) wM = 0;
            else wM = 1 / (sumMeasure - 0.5);

            if (sumReference - 0.5 == 0) wR = 0;
            else wR = 1 / (sumReference - 0.5);

            for (int i = 0; i < measuredMassList.Count; i++) {
                scalarM += measuredMassList[i][1] / (1 + wM * measuredMassList[i][1]) * measuredMassList[i][0];
                scalarR += referenceMassList[i][1] / (1 + wR * referenceMassList[i][1]) * referenceMassList[i][0];
                covariance += measuredMassList[i][0] * Math.Sqrt(measuredMassList[i][1] / (1 + wM * measuredMassList[i][1]) * referenceMassList[i][1] / (1 + wR * referenceMassList[i][1]));
            }

            if (scalarM == 0 || scalarR == 0) { return 0; }
            else { return Math.Pow(covariance, 2) / scalarM / scalarR * peakCountPenalty; }
        }

        public static double GetMsmsClusterSimilarityScore(List<Peak> eSpectra, double ePrecursorMz, List<Peak> rSpectra, double rPrecursorMz,
            double relativeAbundanceCutOff, double massTolerance, MassToleranceType massTolType) {
            if (eSpectra == null || eSpectra.Count == 0 || rSpectra == null || rSpectra.Count == 0) return 0;

            var refinedExperimantalSpectra = FragmentAssigner.GetRefinedPeaklist(eSpectra, relativeAbundanceCutOff, 0.0, ePrecursorMz, massTolerance, massTolType);
            var refinedReferenceSpectra = FragmentAssigner.GetRefinedPeaklist(rSpectra, relativeAbundanceCutOff, 0.0, rPrecursorMz, massTolerance, massTolType);

            if (refinedExperimantalSpectra.Count == 0 || refinedReferenceSpectra.Count == 0) return 0;

            var eMsmsPeaks = getNormalizedMsmsPeakList(refinedExperimantalSpectra, ePrecursorMz);
            var rMsmsPeaks = getNormalizedMsmsPeakList(refinedReferenceSpectra, rPrecursorMz);

            if (eMsmsPeaks.Count < 2 || rMsmsPeaks.Count < 2) return 0;
            if (eMsmsPeaks.Count(n => n.Intensity >= 100) < 2 || rMsmsPeaks.Count(n => n.Intensity >= 100) < 2) return 0;

            searchMatchedMsmsPeaks(eMsmsPeaks, rMsmsPeaks, massTolerance, massTolType);

            var msmsClusterScore = getMsmsClusterScore(eMsmsPeaks, rMsmsPeaks);

            return msmsClusterScore;
        }

        /// <summary>
        /// Calculate Ms/MS similarity for lipid MS/MS spectra clustering.
        /// Spectra should be centroided.
        /// </summary>
        public static double GetMsmsClusterSimilarityScore(List<Peak> eSpectra, double ePrecursorMz, List<Peak> rSpectra, double rPrecursorMz,
            double massShift, double relativeAbundanceCutOff, double massTolerance, MassToleranceType massTolType)
        {
            if (eSpectra == null || eSpectra.Count == 0 || rSpectra == null || rSpectra.Count == 0) return 0;

            var refinedExperimantalSpectra = FragmentAssigner.GetRefinedPeaklist(eSpectra, relativeAbundanceCutOff, 0.0, ePrecursorMz, massTolerance, massTolType);
            var refinedReferenceSpectra = FragmentAssigner.GetRefinedPeaklist(rSpectra, relativeAbundanceCutOff, 0.0, rPrecursorMz, massTolerance, massTolType);

            if (refinedExperimantalSpectra.Count == 0 || refinedReferenceSpectra.Count == 0) return 0;

            var eMsmsPeaks = getNormalizedMsmsPeakList(refinedExperimantalSpectra, ePrecursorMz);
            var rMsmsPeaks = getNormalizedMsmsPeakList(refinedReferenceSpectra, rPrecursorMz);

            if (eMsmsPeaks.Count < 4 || rMsmsPeaks.Count < 4) return 0;

            searchMatchedMsmsPeaks(eMsmsPeaks, rMsmsPeaks, massTolerance, massTolType);
            searchShiftMatchedMsmsPeaks(eMsmsPeaks, rMsmsPeaks, ePrecursorMz, rPrecursorMz, massShift);

            var msmsClusterScore = getMsmsClusterScore(eMsmsPeaks, rMsmsPeaks);

            return msmsClusterScore;
        }

        private static double getMsmsClusterScore(List<MsmsPeak> eMsmsPeaks, List<MsmsPeak> rMsmsPeaks)
        {
            var product = 0.0;
            var eScalar = 0.0;
            var rScalar = 0.0;
            var uEScalar = 0.0;
            var uRScalar = 0.0;

            var counter = 0;
            foreach (var ePeak in eMsmsPeaks) {
                if (ePeak.IsProductIonMatched == true || ePeak.IsNeutralLossMatched == true) {
                    counter++;
                    product += ePeak.Intensity * rMsmsPeaks[ePeak.MatchPeakId].Intensity;
                    eScalar += Math.Pow(ePeak.Intensity, 2);
                    rScalar += Math.Pow(rMsmsPeaks[ePeak.MatchPeakId].Intensity, 2);
                }
                else if (ePeak.IsShiftMatched == true) { // weighted by 0.5 * intensity for penalty
                    uEScalar += Math.Pow(ePeak.Intensity * 0.5, 2);
                    uRScalar += Math.Pow(rMsmsPeaks[ePeak.MatchPeakId].Intensity * 0.5, 2);
                }
                else {
                    uEScalar += Math.Pow(ePeak.Intensity, 2);
                }
            }

            if (counter < 2) return 0;

            foreach (var rPeak in rMsmsPeaks.Where(n => n.IsProductIonMatched == false)
                .Where(n => n.IsNeutralLossMatched == false)
                .Where(n => n.IsShiftMatched == false)) {
                uRScalar += Math.Pow(rPeak.Intensity, 2);
            }

            //return product / (Math.Sqrt(eScalar) * Math.Sqrt(rScalar) + uEScalar + uRScalar);
            return product / (product + uEScalar + uRScalar);
        }

        private static void searchShiftMatchedMsmsPeaks(List<MsmsPeak> eMsmsPeaks, List<MsmsPeak> rMsmsPeaks, double ePrecursorMz, double rPrecursorMz, double massShift)
        {
            var precursorDiff = Math.Abs(ePrecursorMz - rPrecursorMz);

            //currently, mass shifted assignment will be performed by nominal mass resolution
            if (precursorDiff > massShift) {
                var shiftMaxInc = Math.Floor(precursorDiff / massShift);
                for (int i = 0; i < eMsmsPeaks.Count; i++) {
                    if (eMsmsPeaks[i].IsProductIonMatched == true) continue;

                    var ePeak = eMsmsPeaks[i];
                    var ePeakMz = (int)Math.Round(ePeak.Mz, 0);
                    var ePeakNl = (int)Math.Round(ePeak.NeutralLoss, 0);

                    var minPeakID = -1;
                    var minIntensityDiff = double.MaxValue;

                    for (int j = 0; j < rMsmsPeaks.Count; j++) {
                        if (rMsmsPeaks[j].IsProductIonMatched == true || rMsmsPeaks[j].IsShiftMatched == true) continue;
                        var rPeak = rMsmsPeaks[j];

                        for (int k = 1; k <= shiftMaxInc; k++) {
                            var shiftedProductMz = rPrecursorMz > ePrecursorMz ? (int)Math.Round(rPeak.Mz - k * massShift, 0) : (int)Math.Round(rPeak.Mz + k * massShift, 0);
                            var shiftedNeutralLoss = rPrecursorMz > ePrecursorMz ? (int)Math.Round(rPeak.NeutralLoss - k * massShift, 0) : (int)Math.Round(rPeak.NeutralLoss + k * massShift, 0);

                            if (ePeakMz == shiftedProductMz || (ePeakNl == shiftedNeutralLoss && ePeakNl > 14)) {
                                var intensityDiff = Math.Abs(ePeak.Intensity - rPeak.Intensity);
                                if (intensityDiff < minIntensityDiff) {
                                    minIntensityDiff = intensityDiff;
                                    minPeakID = j;
                                }
                            }
                        }
                    }

                    if (minPeakID >= 0) {
                        ePeak.IsShiftMatched = true;
                        ePeak.MatchPeakId = minPeakID;
                        rMsmsPeaks[minPeakID].IsShiftMatched = true;
                        rMsmsPeaks[minPeakID].MatchPeakId = i;
                    }
                }
            }
        }

        
        private static void searchMatchedMsmsPeaks(List<MsmsPeak> eMsmsPeaks, List<MsmsPeak> rMsmsPeaks, 
            double massTolerance, MassToleranceType massTolType)
        {
            //match definition: if product ion or neutral loss are within the mass tolerance, it will be recognized as MATCH.
            //The smallest intensity difference will be recognized as highest match.
            //product ion matching
            for (int i = 0; i < eMsmsPeaks.Count; i++) {
                var ePeak = eMsmsPeaks[i];
                var massTol = massTolType == MassToleranceType.Da ? massTolerance : MolecularFormulaUtility.ConvertPpmToMassAccuracy(ePeak.Mz, massTolerance);
                var minPeakID = -1;
                var minIntensityDiff = double.MaxValue;
                var isProduct = false;
                for (int j = 0; j < rMsmsPeaks.Count; j++) {
                    var rPeak = rMsmsPeaks[j];
                    if (rPeak.IsProductIonMatched == true || rPeak.IsNeutralLossMatched == true) continue;

                    if (rPeak.Mz - massTol < ePeak.Mz && ePeak.Mz < rPeak.Mz + massTol) {
                        var intensityDiff = Math.Abs(ePeak.Intensity - rPeak.Intensity);
                        if (intensityDiff < minIntensityDiff) {
                            minIntensityDiff = intensityDiff;
                            minPeakID = j;
                            isProduct = true;
                        }
                    }
                    else if ((rPeak.NeutralLoss - massTol < ePeak.NeutralLoss && ePeak.NeutralLoss < rPeak.NeutralLoss + massTol)) {
                        var intensityDiff = Math.Abs(ePeak.Intensity - rPeak.Intensity);
                        if (intensityDiff < minIntensityDiff) {
                            minIntensityDiff = intensityDiff;
                            minPeakID = j;
                            isProduct = false;
                        }
                    }
                }

                if (minPeakID >= 0) {
                    ePeak.MatchPeakId = minPeakID;
                    rMsmsPeaks[minPeakID].MatchPeakId = i;

                    if (isProduct) {
                        ePeak.IsProductIonMatched = true;
                        rMsmsPeaks[minPeakID].IsProductIonMatched = true;
                    }
                    else {
                        ePeak.IsNeutralLossMatched = true;
                        rMsmsPeaks[minPeakID].IsNeutralLossMatched = true;
                    }
                }
            }
        }

       
        /// <summary>
        /// excluded if precursor - product is less than 0
        /// exclude isotopic ions
        /// base intensity is normalized to 1000
        /// Currently, top 5 peaks in 100 Da range are stored as final list (top 2 peaks from 0 Da to 100 Da will be used)
        /// </summary>
        private static List<MsmsPeak> getNormalizedMsmsPeakList(List<Peak> spectra, double precursorMz)
        {
            var msmsPeaks = new List<MsmsPeak>();
            var maxintensity = spectra.Max(n => n.Intensity);
            var massRangeMax = 20;

            var msmsDictionary = new Dictionary<int, List<MsmsPeak>>();
            for (int i = 0; i < massRangeMax; i++) { // curently, the mass range is set from 0 Da until 2000 Da
                msmsDictionary[i] = new List<MsmsPeak>();
            }

            foreach (var peak in spectra.Where(n => n.Comment == "M")) {
                if (precursorMz + 0.2 < peak.Mz) continue;
                var massRange = (int)(peak.Mz * 0.01);
                if (0 <= massRange && massRange < massRangeMax)
                    msmsDictionary[massRange].Add(new MsmsPeak(peak.Mz, precursorMz, peak.Intensity / maxintensity * 1000));
            }

            for (int i = 0; i < massRangeMax; i++) {
                if (msmsDictionary[i].Count == 0) continue;
                msmsDictionary[i] = msmsDictionary[i].OrderByDescending(n => n.Intensity).ToList();

                if (i == 0) {
                    for (int j = 0; j < msmsDictionary[i].Count; j++) {
                        if (j > 1) break; //top 2 peaks will be stored from 0 Da until 100 Da
                        msmsPeaks.Add(msmsDictionary[i][j]);
                    }
                }
                else {
                    for (int j = 0; j < msmsDictionary[i].Count; j++) {
                        if (j > 4) break; // top 5 peaks will be stored from 100 Da until 2000 Da
                        msmsPeaks.Add(msmsDictionary[i][j]);
                    }
                }
            }
            msmsPeaks = msmsPeaks.OrderBy(n => n.Mz).ToList();
            return msmsPeaks;
        }

        public static double GetEiSpectraSimilarity(double dotProduct, double revDotProd, double precentSpec)
        {
            return (2 * dotProduct + 2 * revDotProd + precentSpec) / 5.0;
        }
    }

    public class MsmsPeak : Peak
    {
        private bool isProductIonMatched;
        private bool isNeutralLossMatched;
        private bool isShiftMatched;
        private double neutralLoss;
        private int matchPeakId;

        public MsmsPeak(double productMz, double precursorMz, double intensity)
        {
            this.isProductIonMatched = false;
            this.isShiftMatched = false;

            this.Mz = productMz;
            this.neutralLoss = precursorMz - productMz;
            this.Intensity = intensity;
        }

        public bool IsProductIonMatched
        {
            get { return isProductIonMatched; }
            set { isProductIonMatched = value; }
        }

        public bool IsShiftMatched
        {
            get { return isShiftMatched; }
            set { isShiftMatched = value; }
        }

        public double NeutralLoss
        {
            get { return neutralLoss; }
            set { neutralLoss = value; }
        }

        public int MatchPeakId
        {
            get { return matchPeakId; }
            set { matchPeakId = value; }
        }

        public bool IsNeutralLossMatched {
            get {
                return isNeutralLossMatched;
            }

            set {
                isNeutralLossMatched = value;
            }
        }
    }
}
