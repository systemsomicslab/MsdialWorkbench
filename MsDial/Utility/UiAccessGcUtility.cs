using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CompMs.RawDataHandler.Core;
using CompMs.Common.DataObj;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class UiAccessGcUtility
    {
        private UiAccessGcUtility() { }

        public static PairwisePlotBean GetRtMzPairwisePlotPeakViewBean(AnalysisFileBean file, List<PeakAreaBean> peakAreaList, List<MS1DecResult> ms1DecResults)
        {
            var xAxisRtDatapointCollection = new ObservableCollection<double>();
            var yAxisMzDatapointCollection = new ObservableCollection<double>();
            var plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            var peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            string xAxisTitle = "Retention time [min]";
            string yAxisTitle = "m/z";
            string graphTitle = file.AnalysisFilePropertyBean.AnalysisFileName;

            for (int i = 0; i < peakAreaList.Count; i++)
            {
                xAxisRtDatapointCollection.Add(peakAreaList[i].RtAtPeakTop);
                yAxisMzDatapointCollection.Add(peakAreaList[i].AccurateMass);
                plotBrushCollection.Add(new SolidColorBrush(Color.FromArgb(180, (byte)(255 * peakAreaList[i].AmplitudeScoreValue), (byte)(255 * (1 - Math.Abs(peakAreaList[i].AmplitudeScoreValue - 0.5))), (byte)(255 - 255 * peakAreaList[i].AmplitudeScoreValue))));
                peakAreaBeanCollection.Add(peakAreaList[i]);
            }

            return new PairwisePlotBean(graphTitle, xAxisTitle, yAxisTitle, xAxisRtDatapointCollection, yAxisMzDatapointCollection, peakAreaBeanCollection, ms1DecResults, plotBrushCollection, PairwisePlotDisplayLabel.None);
        }

        public static ChromatogramXicViewModel GetChromatogramXicVM(List<RawSpectrum> spectrumList, float targetMz, float targetRt, AnalysisParamOfMsdialGcms param, string chromTitle)
        {
            var smoothedPeaklist = DataAccessGcUtility.GetSmoothedPeaklist(DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, targetMz, param.MassAccuracy, param.RetentionTimeBegin, param.RetentionTimeEnd, param.IonMode), param.SmoothingMethod, param.SmoothingLevel);
            var chromatogramBean = new ChromatogramBean(true, Brushes.Blue, 1, chromTitle, targetMz, param.MassSliceWidth, new ObservableCollection<double[]>(smoothedPeaklist));

            string graphTitle = chromTitle + " chromatogram of " + Math.Round(targetMz, 4).ToString() + " Tolerance [Da]: " + param.MassSliceWidth.ToString() + "  Max intensity: " + Math.Round(chromatogramBean.MaxIntensity, 0);
            return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, 0, graphTitle, targetMz, param.MassAccuracy, targetRt, true);
        }

        public static MassSpectrogramViewModel GetRawMs1MassSpectrogramVM(List<RawSpectrum> spectrumList, float targetMz, float targetRt, int msScanPoint, AnalysisParamOfMsdialGcms param)
        {
            if (msScanPoint < 0) return null;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(spectrumList, msScanPoint, param);
            if (massSpectrogramBean == null) return null;

            string graphTitle = "MS1 spectra " + "Max intensity: " + Math.Round(massSpectrogramBean.MaxIntensity, 0).ToString() + "\n" + massSpectrogramBean.SplashKey;

            return new MassSpectrogramViewModel(massSpectrogramBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, targetMz, graphTitle);
        }

        public static MassSpectrogramViewModel GetRawMs1MassSpectrogramVM(List<RawSpectrum> spectrumList, MS1DecResult ms1DecResult, AnalysisParamOfMsdialGcms param)
        {
            float targetRt = ms1DecResult.RetentionTime;
            int msScanPoint = ms1DecResult.ScanNumber;

            if (msScanPoint == -1) return null;

            var massSpectrogramBean = getMassSpectrogramBean(spectrumList, msScanPoint, param);
            if (massSpectrogramBean == null) return null;

            string graphTitle = "Raw EI spectrum\n" + massSpectrogramBean.SplashKey;

            return new MassSpectrogramViewModel(massSpectrogramBean, null, MassSpectrogramIntensityMode.Absolute, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetReversibleSpectrumVM(List<RawSpectrum> spectrumList, MS1DecResult ms1DecResult, AnalysisParamOfMsdialGcms param, ReversibleMassSpectraView reversibleMassSpectraView, List<MspFormatCompoundInformationBean> mspDB)
        {
            float targetRt = ms1DecResult.RetentionTime;
            int msScanPoint = ms1DecResult.ScanNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean;

            if (reversibleMassSpectraView == ReversibleMassSpectraView.raw)
                massSpectrogramBean = getMassSpectrogramBean(spectrumList, msScanPoint, param);
            else
                massSpectrogramBean = getMassSpectrogramBean(ms1DecResult, Brushes.Black);

            string graphTitle = "EI spectra " + "quant mass: ";
            if (param.AccuracyType == AccuracyType.IsNominal) graphTitle += Math.Round(ms1DecResult.BasepeakMz, 1).ToString();
            else graphTitle += Math.Round(ms1DecResult.BasepeakMz, 5).ToString();

            if (mspDB != null && mspDB.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspDB, ms1DecResult.MspDbID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetReversibleSpectrumVM(MS1DecResult ms1DecResult, AnalysisParamOfMsdialGcms param, ReversibleMassSpectraView reversibleMassSpectraView, List<MspFormatCompoundInformationBean> mspDB)
        {
            float targetRt = ms1DecResult.RetentionTime;
            int msScanPoint = ms1DecResult.ScanNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean;

            massSpectrogramBean = getMassSpectrogramBean(ms1DecResult, Brushes.Black);

            string graphTitle = "EI spectra " + "quant mass: ";
            if (param.AccuracyType == AccuracyType.IsNominal) graphTitle += Math.Round(ms1DecResult.BasepeakMz, 1).ToString();
            else graphTitle += Math.Round(ms1DecResult.BasepeakMz, 5).ToString();

            if (mspDB != null && mspDB.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspDB, ms1DecResult.MspDbID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2DeconvolutedMassSpectrogramVM(MS1DecResult ms1DecResult)
        {
            float targetRt = ms1DecResult.RetentionTime;
            int msScanPoint = ms1DecResult.ScanNumber;
            var massSpectrogramBean = getMassSpectrogramBean(ms1DecResult, Brushes.Black);
            if (massSpectrogramBean == null) return null;

            string graphTitle = "Deconvoluted EI spectrum\n" + massSpectrogramBean.SplashKey;

			return new MassSpectrogramViewModel(massSpectrogramBean, null, MassSpectrogramIntensityMode.Absolute, msScanPoint, targetRt, graphTitle);
        }

        public static ChromatogramMrmViewModel GetDeconvolutedChromatogramVM(List<RawSpectrum> spectrumList, MS1DecResult ms1DecResult, AnalysisParamOfMsdialGcms param, MrmChromatogramView mrmChromatogramView, List<SolidColorBrush> solidColorBrushList)
        {
            if (ms1DecResult.ScanNumber < 0) return null;

            var chromatograms = new ObservableCollection<ChromatogramBean>();

            float rtWidth = (float)(ms1DecResult.BasepeakChromatogram[ms1DecResult.BasepeakChromatogram.Count - 1].RetentionTime - ms1DecResult.BasepeakChromatogram[0].RetentionTime);
            float startRt = ms1DecResult.RetentionTime - rtWidth;
            float endRt = ms1DecResult.RetentionTime + rtWidth;

            List<double[]> ms2Peaklist = new List<double[]>();
            List<double[]> centroidedSpectra;

            if (mrmChromatogramView == MrmChromatogramView.raw)
            {
                centroidedSpectra = DataAccessGcUtility.GetCentroidMasasSpectra(spectrumList, param.DataType, ms1DecResult.ScanNumber, param.MassAccuracy, param.AmplitudeCutoff, param.MassRangeBegin, param.MassRangeEnd);
                if (centroidedSpectra == null || centroidedSpectra.Count == 0) return null;
                centroidedSpectra = centroidedSpectra.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectra.Count; i++)
                {
                    ms2Peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, (float)centroidedSpectra[i][0], param.MassAccuracy, startRt, endRt, param.IonMode);
                    ms2Peaklist = DataAccessGcUtility.GetSmoothedPeaklist(ms2Peaklist, param.SmoothingMethod, param.SmoothingLevel);

                    if (i < 10)
                        chromatograms.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, -1, (float)centroidedSpectra[i][0], new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, -1, (float)centroidedSpectra[i][0], new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, -1, (float)centroidedSpectra[i][0], new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }
            else if (mrmChromatogramView == MrmChromatogramView.component)
            {
                var maxIntensity = ms1DecResult.BasepeakChromatogram.Max(n => n.Intensity);
                var spectrum = ms1DecResult.Spectrum.OrderByDescending(n => n.Intensity).ToList();
                var baseChromatogram = ms1DecResult.BasepeakChromatogram;
                for (int i = 0; i < spectrum.Count; i++)
                {
                    var productMz = (float)spectrum[i].Mz;
                    ms2Peaklist = new List<double[]>();
                    for (int j = 0; j < baseChromatogram.Count; j++)
                        ms2Peaklist.Add(new double[] { baseChromatogram[j].ScanNumber, baseChromatogram[j].RetentionTime, productMz, 
                            baseChromatogram[j].Intensity * spectrum[i].Intensity / maxIntensity });

                    ms2Peaklist.Insert(0, new double[] { 0, startRt, 0, 0 });
                    ms2Peaklist.Add(new double[] { 0, endRt, 0, 0 });

                    if (i < 10)
                        chromatograms.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.5, -1, productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.5, -1, productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.5, -1, productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }
            else
            {
                centroidedSpectra = DataAccessGcUtility.GetCentroidMasasSpectra(spectrumList, param.DataType, ms1DecResult.ScanNumber, param.MassAccuracy, param.AmplitudeCutoff, param.MassRangeBegin, param.MassRangeEnd);
                if (centroidedSpectra == null || centroidedSpectra.Count == 0) return null;
                centroidedSpectra = centroidedSpectra.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectra.Count; i++)
                {
                    ms2Peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, (float)centroidedSpectra[i][0], param.MassAccuracy, startRt, endRt, param.IonMode);
                    ms2Peaklist = DataAccessGcUtility.GetSmoothedPeaklist(ms2Peaklist, param.SmoothingMethod, param.SmoothingLevel);

                    if (i < 10)
                        chromatograms.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, -1, (float)centroidedSpectra[i][0], new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, -1, (float)centroidedSpectra[i][0], new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, -1, (float)centroidedSpectra[i][0], new ObservableCollection<double[]>(ms2Peaklist), null));
                }

                var maxIntensity = ms1DecResult.BasepeakChromatogram.Max(n => n.Intensity);
                var spectrum = ms1DecResult.Spectrum.OrderByDescending(n => n.Intensity).ToList();
                var baseChromatogram = ms1DecResult.BasepeakChromatogram;

                for (int i = 0; i < spectrum.Count; i++)
                {
                    var productMz = (float)spectrum[i].Mz;
                    ms2Peaklist = new List<double[]>();
                    for (int j = 0; j < baseChromatogram.Count; j++)
                        ms2Peaklist.Add(new double[] { baseChromatogram[j].ScanNumber, baseChromatogram[j].RetentionTime, productMz
                            , baseChromatogram[j].Intensity * spectrum[i].Intensity / maxIntensity });

                    ms2Peaklist.Insert(0, new double[] { 0, startRt, 0, 0 });
                    ms2Peaklist.Add(new double[] { 0, endRt, 0, 0 });

                    if (i < 10)
                        chromatograms.Add(new ChromatogramBean(true, solidColorBrushList[i], 2.0, -1, productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[i], 2.0, -1, productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatograms.Add(new ChromatogramBean(false, solidColorBrushList[0], 2.0, -1, productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }

            return new ChromatogramMrmViewModel(chromatograms, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, -1, -1, "EI chromatograms ", -1, "", "", "", "", -1, -1, ms1DecResult.RetentionTime, null, -1, -1);
        }

        public static ChromatogramTicEicViewModel GetChromatogramTicViewModel(List<RawSpectrum> spectrumList, AnalysisFileBean file, AnalysisParamOfMsdialGcms param)
        {
            if (spectrumList == null || spectrumList.Count == 0) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var peaklist = DataAccessGcUtility.GetTicPeaklist(spectrumList, param.IonMode);

            if (peaklist == null || peaklist.Count == 0) return null;

            peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Black, "TIC", new ObservableCollection<double[]>(peaklist), null));

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "TIC", file.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, file.AnalysisFilePropertyBean.AnalysisFileType.ToString(), file.AnalysisFilePropertyBean.AnalysisFileClass, file.AnalysisFilePropertyBean.AnalysisFileName, file.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetChromatogramEicViewModel(ObservableCollection<ExtractedIonChromatogramDisplaySettingBean> eicChromatograms, 
            AnalysisFileBean file, AnalysisParamOfMsdialGcms param, List<SolidColorBrush> solidColorBrushList, List<RawSpectrum> spectrumList)
        {
            if (spectrumList == null || spectrumList.Count == 0) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            ChromatogramBean chromatogramBean;
            List<double[]> peaklist;

            for (int i = 0; i < eicChromatograms.Count; i++)
            {
                peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, (float)eicChromatograms[i].ExactMass, (float)eicChromatograms[i].MassTolerance, float.MinValue, float.MaxValue, param.IonMode);
                if (peaklist == null || peaklist.Count == 0) continue;
                peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                if (i <= solidColorBrushList.Count - 1)
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[i], 1.0, eicChromatograms[i].EicName, (float)eicChromatograms[i].ExactMass, (float)eicChromatograms[i].MassTolerance, new ObservableCollection<double[]>(peaklist));
                else
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[0], 1.0, eicChromatograms[i].EicName, (float)eicChromatograms[i].ExactMass, (float)eicChromatograms[i].MassTolerance, new ObservableCollection<double[]>(peaklist));

                chromatogramBeanCollection.Add(chromatogramBean);
            }

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "EIC", file.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, file.AnalysisFilePropertyBean.AnalysisFileType.ToString(), file.AnalysisFilePropertyBean.AnalysisFileClass, file.AnalysisFilePropertyBean.AnalysisFileName, file.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetMultiFilesEicsOfTargetPeak(AlignmentPropertyBean alignedSpot, ObservableCollection<AnalysisFileBean> files, int focusedFileID, 
            List<RawSpectrum> focusedSpectra, RdamPropertyBean rdamProperty, AnalysisParamOfMsdialGcms param, ProjectPropertyBean project)
        {
            if (focusedSpectra == null || focusedSpectra.Count == 0) return null;
            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var rtBegin = alignedSpot.CentralRetentionTime - 0.1F;
            var rtEnd = alignedSpot.CentralRetentionTime + 0.1F;

            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = project.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);

            foreach (var file in files.Where(n => n.AnalysisFilePropertyBean.AnalysisFileIncluded == true)) { // draw the included samples

                var fileProperty = file.AnalysisFilePropertyBean;
                var targetMz = alignedSpot.QuantMass;
                var peaklist = new List<double[]>();

                if (fileProperty.AnalysisFileId == focusedFileID) {
                    peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(focusedSpectra, targetMz, param.MassAccuracy, rtBegin, rtEnd, param.IonMode);
                }
                else {
                    peaklist = DataAccessGcUtility.GetChromatogramPeaklist(file, rtBegin, rtEnd, targetMz, param.MassAccuracy, param.IonMode, rdamProperty);
                }

                if (peaklist == null || peaklist.Count == 0) continue;
                peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var chromatogramBean = new ChromatogramBean(true, classnameToBrushes[file.AnalysisFilePropertyBean.AnalysisFileClass], 1.0, fileProperty.AnalysisFileName, targetMz, param.MassAccuracy, new ObservableCollection<double[]>(peaklist));

                chromatogramBeanCollection.Add(chromatogramBean);
            }
            var title = "Name: " + alignedSpot.MetaboliteName + "; RT: " + alignedSpot.CentralRetentionTime + "; RI: " + alignedSpot.CentralRetentionIndex + "; Quant mass: " + alignedSpot.QuantMass;
            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "EICs of Multi samples", -1, title, title, title, -1);
        }

        public static ChromatogramTicEicViewModel GetChromatogramBpcViewModel(ObservableCollection<ExtractedIonChromatogramDisplaySettingBean> eicChromatograms, 
            AnalysisFileBean file, AnalysisParamOfMsdialGcms param, List<SolidColorBrush> solidColorBrushList, List<RawSpectrum> spectrumList)
        {
            if (spectrumList == null || spectrumList.Count == 0) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            ChromatogramBean chromatogramBean;

            List<double[]> peaklist;
            for (int i = 0; i < eicChromatograms.Count; i++)
            {
                peaklist = DataAccessGcUtility.GetMs1PeaklistAsBPC(spectrumList, (float)eicChromatograms[i].ExactMass, (float)eicChromatograms[i].MassTolerance, float.MinValue, float.MaxValue, param.IonMode);
                if (peaklist == null || peaklist.Count == 0) continue;
                peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                if (i <= solidColorBrushList.Count - 1)
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[i], 1.0, eicChromatograms[i].EicName, (float)eicChromatograms[i].ExactMass, (float)eicChromatograms[i].MassTolerance, new ObservableCollection<double[]>(peaklist));
                else
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[0], 1.0, eicChromatograms[i].EicName, (float)eicChromatograms[i].ExactMass, (float)eicChromatograms[i].MassTolerance, new ObservableCollection<double[]>(peaklist));

                chromatogramBeanCollection.Add(chromatogramBean);
            }

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "BPC", file.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, file.AnalysisFilePropertyBean.AnalysisFileType.ToString(), file.AnalysisFilePropertyBean.AnalysisFileClass, file.AnalysisFilePropertyBean.AnalysisFileName, file.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetChromatogramBpcViewModel(
           AnalysisFileBean file, AnalysisParamOfMsdialGcms param, 
           List<RawSpectrum> spectrumList) {
            if (spectrumList == null || spectrumList.Count == 0) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var peaklist = DataAccessGcUtility.GetMs1PeaklistAsBPC(spectrumList, param.MassAccuracy, param.IonMode);
            if (peaklist == null || peaklist.Count == 0) return null;
            peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Red, "BPC", new ObservableCollection<double[]>(peaklist), null));

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "TIC", file.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, file.AnalysisFilePropertyBean.AnalysisFileType.ToString(), file.AnalysisFilePropertyBean.AnalysisFileClass, file.AnalysisFilePropertyBean.AnalysisFileName, file.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetChromatogramTicBpcHighestEicViewModel(
          AnalysisFileBean file, AnalysisParamOfMsdialGcms param,
          List<PeakAreaBean> peakspots,
          List<RawSpectrum> spectrumList) {
            if (spectrumList == null || spectrumList.Count == 0) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var bpcpeaklist = DataAccessGcUtility.GetMs1PeaklistAsBPC(spectrumList, param.MassAccuracy, param.IonMode);
            if (bpcpeaklist == null || bpcpeaklist.Count == 0) return null;
            bpcpeaklist = DataAccessGcUtility.GetSmoothedPeaklist(bpcpeaklist, param.SmoothingMethod, param.SmoothingLevel);

            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Red, "BPC", new ObservableCollection<double[]>(bpcpeaklist), null));

            var ticpeaklist = DataAccessGcUtility.GetTicPeaklist(spectrumList, param.IonMode);

            if (ticpeaklist == null || ticpeaklist.Count == 0) return null;

            ticpeaklist = DataAccessGcUtility.GetSmoothedPeaklist(ticpeaklist, param.SmoothingMethod, param.SmoothingLevel);
            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Black, "TIC", new ObservableCollection<double[]>(ticpeaklist), null));

            var maxSpotID = 0;
            var maxIntensity = double.MinValue;
            for (int i = 0; i < peakspots.Count; i++) {
                if (peakspots[i].IntensityAtPeakTop > maxIntensity) {
                    maxIntensity = peakspots[i].IntensityAtPeakTop;
                    maxSpotID = i;
                }
            }
            var hSpot = peakspots[maxSpotID];

            var eicpeaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, (float)hSpot.AccurateMass, 
                (float)param.MassAccuracy, float.MinValue, float.MaxValue, param.IonMode);
            if (eicpeaklist == null || eicpeaklist.Count == 0) return null;
            eicpeaklist = DataAccessGcUtility.GetSmoothedPeaklist(eicpeaklist, param.SmoothingMethod, param.SmoothingLevel);

            var chromatogramBean = new ChromatogramBean(true, Brushes.Blue, 1.0, "EIC",
                hSpot.AccurateMass, (float)param.MassAccuracy, new ObservableCollection<double[]>(eicpeaklist));

            chromatogramBeanCollection.Add(chromatogramBean);

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, 
                ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height,
                ChromatogramIntensityMode.Absolute, "TIC, BPC, and most abundant ion's EIC",
                file.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, 
                file.AnalysisFilePropertyBean.AnalysisFileType.ToString(), 
                file.AnalysisFilePropertyBean.AnalysisFileClass, 
                file.AnalysisFilePropertyBean.AnalysisFileName,
                file.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static PairwisePlotBean GetRtMzPairwisePlotAlignmentViewBean(AlignmentFileBean alignmentFileBean, AlignmentResultBean alignmentResultBean, List<MS1DecResult> ms1DecResults)
        {
            ObservableCollection<double> xAxisRtDatapointCollection = new ObservableCollection<double>();
            ObservableCollection<double> yAxisMzDatapointCollection = new ObservableCollection<double>();
            ObservableCollection<SolidColorBrush> plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanCollection = new ObservableCollection<AlignmentPropertyBean>();

            var param = alignmentResultBean.AnalysisParamForGC;
            var index = AlignmentIndexType.RT; if (param != null && param.AlignmentIndexType == AlignmentIndexType.RI) index = AlignmentIndexType.RI;

            string xAxisTitle = "Retention time [min]"; if (index == AlignmentIndexType.RI) xAxisTitle = "Retention index";
            string yAxisTitle = "Quant mass";
            string graphTitle = alignmentFileBean.FileName;

            for (int i = 0; i < alignmentResultBean.AlignmentPropertyBeanCollection.Count; i++)
            {
                if (index == AlignmentIndexType.RT)
                    xAxisRtDatapointCollection.Add(alignmentResultBean.AlignmentPropertyBeanCollection[i].CentralRetentionTime);
                else
                    xAxisRtDatapointCollection.Add(alignmentResultBean.AlignmentPropertyBeanCollection[i].CentralRetentionIndex);

                yAxisMzDatapointCollection.Add(alignmentResultBean.AlignmentPropertyBeanCollection[i].QuantMass);

               // Console.WriteLine(alignmentResultBean.AlignmentPropertyBeanCollection[i].RelativeAmplitudeValue);
                plotBrushCollection.Add(new SolidColorBrush(Color.FromArgb(180, (byte)(255 * alignmentResultBean.AlignmentPropertyBeanCollection[i].RelativeAmplitudeValue), (byte)(255 * (1 - Math.Abs(alignmentResultBean.AlignmentPropertyBeanCollection[i].RelativeAmplitudeValue - 0.5))), (byte)(255 - 255 * alignmentResultBean.AlignmentPropertyBeanCollection[i].RelativeAmplitudeValue))));
                alignmentPropertyBeanCollection.Add(alignmentResultBean.AlignmentPropertyBeanCollection[i]);
            }

            var retentionUnit = RetentionUnit.RT; if (index == AlignmentIndexType.RI) retentionUnit = RetentionUnit.RI;
            return new PairwisePlotBean(graphTitle, xAxisTitle, yAxisTitle, xAxisRtDatapointCollection, 
                yAxisMzDatapointCollection, alignmentPropertyBeanCollection, ms1DecResults,
                plotBrushCollection, PairwisePlotDisplayLabel.None) { RetentionUnit = retentionUnit };
        }

        private static MassSpectrogramBean getMassSpectrogramBean(List<RawSpectrum> spectrumList, int msScanPoint, AnalysisParamOfMsdialGcms param)
        {
            if (msScanPoint < 0) return null;

            var masslist = new ObservableCollection<double[]>();
            var centroidedMassSpectra = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            spectrum = spectrumList[msScanPoint];
            massSpectra = spectrum.Spectrum;
           
            for (int i = 0; i < massSpectra.Length; i++)
                masslist.Add(new double[] { massSpectra[i].Mz, massSpectra[i].Intensity });

            if (param.DataType == DataType.Profile)
            {
                centroidedMassSpectra = SpectralCentroiding.Centroid(masslist, param.MassAccuracy, true);
            }
            else
            {
                centroidedMassSpectra = masslist;
            }

            if (centroidedMassSpectra == null || centroidedMassSpectra.Count == 0)
            {
                for (int i = 0; i < masslist.Count; i++)
                    massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

                return new MassSpectrogramBean(Brushes.Blue, 1.0, masslist, massSpectraDisplayLabelCollection);
            }
            else
            {
                for (int i = 0; i < centroidedMassSpectra.Count; i++)
                    massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = centroidedMassSpectra[i][0], Intensity = centroidedMassSpectra[i][1], Label = Math.Round(centroidedMassSpectra[i][0], 4).ToString() });

                return new MassSpectrogramBean(Brushes.Blue, 1.0, centroidedMassSpectra, massSpectraDisplayLabelCollection);
            }
        }

        private static MassSpectrogramBean getMassSpectrogramBean(MS1DecResult ms1DecResult, SolidColorBrush spectrumBrush)
        {
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            List<double[]> masslist = new List<double[]>();
            for (int i = 0; i < ms1DecResult.Spectrum.Count; i++)
                masslist.Add(new double[] { ms1DecResult.Spectrum[i].Mz, ms1DecResult.Spectrum[i].Intensity });

            masslist = masslist.OrderBy(n => n[0]).ToList();

            for (int i = 0; i < masslist.Count; i++)
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

            return new MassSpectrogramBean(spectrumBrush, 1.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
        }

        private static MassSpectrogramBean getReferenceSpectra(List<MspFormatCompoundInformationBean> mspDB, int libraryID, SolidColorBrush spectrumBrush)
        {
            ObservableCollection<double[]> masslist = new ObservableCollection<double[]>();
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectrogramDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            if (libraryID < 0) return new MassSpectrogramBean(Brushes.Red, 1.0, null);

            for (int i = 0; i < mspDB[libraryID].MzIntensityCommentBeanList.Count; i++)
            {
                var mz = (double)mspDB[libraryID].MzIntensityCommentBeanList[i].Mz;
                var intensity = (double)mspDB[libraryID].MzIntensityCommentBeanList[i].Intensity;
                var comment = mspDB[libraryID].MzIntensityCommentBeanList[i].Comment;
                var commentString = Math.Round(mz, 4).ToString();
                if (comment != null && comment != string.Empty) {
                    //commentString += "\r\n" + comment;
                }

                masslist.Add(new double[] { mz, intensity });
                massSpectrogramDisplayLabelCollection.Add(
                    new MassSpectrogramDisplayLabel() {
                        Mass = mz, Intensity = intensity, Label = commentString
                    });
            }

            return new MassSpectrogramBean(spectrumBrush, 1.0, masslist, massSpectrogramDisplayLabelCollection);
        }

        public static ChromatogramTicEicViewModel GetAlignedEicChromatogram(
            AlignedData alignedData,
            ObservableCollection<AlignedPeakPropertyBean> alignedPeakPropertyBeans,
            ObservableCollection<AnalysisFileBean> files, 
            ProjectPropertyBean projectProperty, 
            AnalysisParamOfMsdialGcms param) {
            if (alignedData == null) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var targetMz = alignedData.Mz;
            var numAnalysisfiles = alignedData.NumAnalysisFiles;
            // var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = projectProperty.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            for (int i = 0; i < numAnalysisfiles; i++) { // draw the included samples
                var peaks = alignedData.PeakLists[i].PeakList;
                var peaklist = new List<double[]>();
                for (int j = 0; j < peaks.Count; j++) {
                    peaklist.Add(new double[] { j, (double)peaks[j][0], targetMz, (double)peaks[j][1] });
                }
                peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                var rttop = alignedPeakPropertyBeans[i].RetentionTime;
                var rtleft = alignedPeakPropertyBeans[i].RetentionTimeLeft;
                var rtright = alignedPeakPropertyBeans[i].RetentionTimeRight;

                if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                    rttop = alignedPeakPropertyBeans[i].RetentionIndex;
                    rtleft = alignedPeakPropertyBeans[i].RetentionIndexLeft;
                    rtright = alignedPeakPropertyBeans[i].RetentionIndexRight;
                }


                var chromatogramBean = new ChromatogramBean(true,
                    classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass],
                    1.0, files[i].AnalysisFilePropertyBean.AnalysisFileName, targetMz,
                    param.MassAccuracy, rttop, rtleft, rtright,
                    alignedData.PeakLists[i].GapFilled,
                    new ObservableCollection<double[]>(peaklist));
                chromatogramBeanCollection.Add(chromatogramBean);
            }

            var chromVM = new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.AnnotatedMetabolite, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "EICs of aligned results", -1, "Selected files", "Selected files", "Selected files", -1);
            if (param.AlignmentIndexType == AlignmentIndexType.RI)
                chromVM.XAxisTitle = "Retention index";

            return chromVM;
        }
    }

}
