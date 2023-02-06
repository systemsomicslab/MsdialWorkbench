using Msdial.Gcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class TableExportUtility
    {
        private TableExportUtility() { }

        public static void SaveSpectraTabel(string spectraTableFormat, string exportFilePath, object target,
            PeakAreaBean peakAreaBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = 
                    ((MassSpectrogramWithReferenceUI)((MassSpectrogramWithReferenceUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    saveSpectraTableAsMassBankFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    saveSpectraTableAsNistFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    saveSpectraTableAsMascotFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        peakAreaBean, projectProp, mspDB);
                }
            }
            else if (target.GetType() == typeof(MassSpectrogramUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = ((MassSpectrogramUI)((MassSpectrogramUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    saveSpectraTableAsMassBankFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    saveSpectraTableAsNistFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    saveSpectraTableAsMascotFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        peakAreaBean, projectProp, mspDB);
                }
            }
        }

        public static void SaveSpectraTabel(string spectraTableFormat, string exportFilePath, object target,
            MS1DecResult ms1DecResult, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = 
                    ((MassSpectrogramWithReferenceUI)((MassSpectrogramWithReferenceUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    saveSpectraTableAsMassBankFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    saveSpectraTableAsNistFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    saveSpectraTableAsMascotFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        ms1DecResult, projectProp, mspDB);
                }
            }
            else if (target.GetType() == typeof(MassSpectrogramUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = ((MassSpectrogramUI)((MassSpectrogramUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    saveSpectraTableAsMassBankFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    saveSpectraTableAsNistFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    saveSpectraTableAsMascotFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        ms1DecResult, projectProp, mspDB);
                }
            }
        }

        public static void SaveSpectraTabel(string spectraTableFormat, string exportFilePath, object target,
            AlignmentPropertyBean alignmentPropertyBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = 
                    ((MassSpectrogramWithReferenceUI)((MassSpectrogramWithReferenceUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    saveSpectraTableAsMassBankFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        alignmentPropertyBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    saveSpectraTableAsNistFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        alignmentPropertyBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    saveSpectraTableAsMascotFormat(exportFilePath, massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        alignmentPropertyBean, projectProp, mspDB);
                }
            }
        }

        public static void CopySpectraTable(string spectraTableFormat, object target, PeakAreaBean peakAreaBean, 
            ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = 
                    ((MassSpectrogramWithReferenceUI)((MassSpectrogramWithReferenceUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    copyToClipboardSpectraTableAsMassBankFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    copyToClipboardSpectraTableAsNistFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    copyToClipboardSpectraTableAsMascotFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
            }
            else if (target.GetType() == typeof(MassSpectrogramUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = ((MassSpectrogramUI)((MassSpectrogramUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    copyToClipboardSpectraTableAsMassBankFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    copyToClipboardSpectraTableAsNistFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    copyToClipboardSpectraTableAsMascotFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        peakAreaBean, projectProp, mspDB);
                }
            }
        }

        public static void CopySpectraTable(string spectraTableFormat, object target,
            AlignmentPropertyBean alignmentPropertyBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = ((MassSpectrogramWithReferenceUI)((MassSpectrogramWithReferenceUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    copyToClipboardSpectraTableAsMassBankFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        alignmentPropertyBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    copyToClipboardSpectraTableAsNistFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        alignmentPropertyBean, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    copyToClipboardSpectraTableAsMascotFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        alignmentPropertyBean, projectProp, mspDB);
                }
            }
        }

        public static void CopySpectraTable(string spectraTableFormat, object target,
            MS1DecResult ms1DecResult, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = 
                    ((MassSpectrogramWithReferenceUI)((MassSpectrogramWithReferenceUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    copyToClipboardSpectraTableAsMassBankFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    copyToClipboardSpectraTableAsNistFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    copyToClipboardSpectraTableAsMascotFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        ms1DecResult, projectProp, mspDB);
                }
            }
            else if (target.GetType() == typeof(MassSpectrogramUI))
            {
                MassSpectrogramViewModel massSpectrogramViewModel = 
                    ((MassSpectrogramUI)((MassSpectrogramUI)target).Content).MassSpectrogramViewModel;
                if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || 
                    massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;

                if (spectraTableFormat.Equals(".txt"))
                {
                    copyToClipboardSpectraTableAsMassBankFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection,
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".msp"))
                {
                    copyToClipboardSpectraTableAsNistFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        ms1DecResult, projectProp, mspDB);
                }
                else if (spectraTableFormat.Equals(".mgf"))
                {
                    copyToClipboardSpectraTableAsMascotFormat(massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection, 
                        ms1DecResult, projectProp, mspDB);
                }
            }
        }

        public static void SaveChromatogramTableAsText(string exportFilePath, object target,
            PeakAreaBean peakAreaBean)
        {
            if (target.GetType() == typeof(ChromatogramMrmUI))
            {
                ChromatogramMrmViewModel chromatogramMrmViewModel = ((ChromatogramMrmUI)((ChromatogramMrmUI)target).Content).ChromatogramMrmViewModel;
                if (chromatogramMrmViewModel == null || chromatogramMrmViewModel.ChromatogramBeanCollection == null || chromatogramMrmViewModel.ChromatogramBeanCollection.Count == 0) return;
                saveChromatogramDataTableAsText(exportFilePath, chromatogramMrmViewModel, peakAreaBean);

            }
            else if (target.GetType() == typeof(ChromatogramTicEicUI))
            {
                ChromatogramTicEicViewModel chromatogramTicEicViewModel = ((ChromatogramTicEicUI)((ChromatogramTicEicUI)target).Content).ChromatogramTicEicViewModel;
                if (chromatogramTicEicViewModel == null || chromatogramTicEicViewModel.ChromatogramBeanCollection == null || chromatogramTicEicViewModel.ChromatogramBeanCollection.Count == 0) return;
                saveChromatogramDataTableAsText(exportFilePath, chromatogramTicEicViewModel);
            }
            else if (target.GetType() == typeof(ChromatogramXicUI)) {
                var chromXicVM = ((ChromatogramXicUI)((ChromatogramXicUI)target).Content).ChromatogramXicViewModel;
                if (chromXicVM == null || chromXicVM.ChromatogramBean == null || chromXicVM.ChromatogramBean.ChromatogramDataPointCollection == null || chromXicVM.ChromatogramBean.ChromatogramDataPointCollection.Count == 0) return;
                saveChromatogramDataTableAsText(exportFilePath, chromXicVM);
            }
            else
                return;
        }


        public static void SaveChromatogramTableAsText(string exportFilePath, object target,
            MS1DecResult ms1DecResult)
        {
            if (target.GetType() == typeof(ChromatogramMrmUI))
            {
                ChromatogramMrmViewModel chromatogramMrmViewModel = ((ChromatogramMrmUI)((ChromatogramMrmUI)target).Content).ChromatogramMrmViewModel;
                if (chromatogramMrmViewModel == null || chromatogramMrmViewModel.ChromatogramBeanCollection == null || chromatogramMrmViewModel.ChromatogramBeanCollection.Count == 0) return;
                saveChromatogramDataTableAsText(exportFilePath, chromatogramMrmViewModel, ms1DecResult);

            }
            else if (target.GetType() == typeof(ChromatogramTicEicUI))
            {
                ChromatogramTicEicViewModel chromatogramTicEicViewModel = ((ChromatogramTicEicUI)((ChromatogramTicEicUI)target).Content).ChromatogramTicEicViewModel;
                if (chromatogramTicEicViewModel == null || chromatogramTicEicViewModel.ChromatogramBeanCollection == null || chromatogramTicEicViewModel.ChromatogramBeanCollection.Count == 0) return;
                saveChromatogramDataTableAsText(exportFilePath, chromatogramTicEicViewModel);
            }
            else if (target.GetType() == typeof(ChromatogramXicUI)) {
                var chromXicVM = ((ChromatogramXicUI)((ChromatogramXicUI)target).Content).ChromatogramXicViewModel;
                if (chromXicVM == null || chromXicVM.ChromatogramBean == null || chromXicVM.ChromatogramBean.ChromatogramDataPointCollection == null || chromXicVM.ChromatogramBean.ChromatogramDataPointCollection.Count == 0) return;
                saveChromatogramDataTableAsText(exportFilePath, chromXicVM);
            }
            else
                return;
        }

        public static void SaveMultivariableResultTableAsTextFormat(string output, MultivariateAnalysisResult maResult) {

            var option = maResult.MultivariateAnalysisOption;
            if (option == MultivariateAnalysisOption.Pca) {
                writePcaResult(output, maResult);
            }
            else if (option == MultivariateAnalysisOption.Plsda || option == MultivariateAnalysisOption.Plsr) {
                writePlsResult(output, maResult);
            }
            else if (option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr) {
                writeOplsResult(output, maResult);
            }
        }

        private static void writePlsResult(string output, MultivariateAnalysisResult maResult) {
            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Method\t" + maResult.MultivariateAnalysisOption.ToString());
                sw.WriteLine("Optimized factor\t" + maResult.OptimizedFactor);
                sw.WriteLine();
                sw.WriteLine("Cross validation N fold\t" + maResult.NFold);
                sw.WriteLine("Component\tSSCV\tPRESS\tQ2\tQ2cum");
                for (int i = 0; i < maResult.Presses.Count; i++) {
                    sw.WriteLine((i + 1).ToString() + "\t" + maResult.SsCVs[i] +
                        "\t" + maResult.Presses[i] + "\t" + maResult.Q2Values[i] +
                        "\t" + maResult.Q2Cums[i]);
                }
                sw.WriteLine();

                var scoreSeq = new List<string>();
                var loadSeq = new List<string>();

                for (int i = 0; i < maResult.OptimizedFactor; i++) {
                    scoreSeq.Add("T" + (i + 1).ToString());
                    loadSeq.Add("P" + (i + 1).ToString());
                }

                scoreSeq.Add("Y experiment"); scoreSeq.Add("Y predicted");
                loadSeq.Add("VIP"); loadSeq.Add("Coefficients");

                var scoreSeqString = String.Join("\t", scoreSeq);
                var loadSeqString = String.Join("\t", loadSeq);

                //header set
                var tpredSize = maResult.TPreds.Count;
                var toPredSize = maResult.ToPreds.Count;
                var metSize = maResult.StatisticsObject.XIndexes.Count;
                var fileSize = maResult.StatisticsObject.YIndexes.Count;

                sw.WriteLine("Score" + "\t" + scoreSeqString);

                //Scores
                for (int i = 0; i < fileSize; i++) {
                    var tList = new List<double>();
                    for (int j = 0; j < maResult.TPreds.Count; j++) {
                        tList.Add(maResult.TPreds[j][i]);
                    }
                    tList.Add(maResult.StatisticsObject.YVariables[i]);
                    tList.Add(maResult.PredictedYs[i]);

                    sw.WriteLine(maResult.StatisticsObject.YLabels[i] + "\t" +
                        String.Join("\t", tList));
                }
                sw.WriteLine();

                //Loadings
                sw.WriteLine("Loading" + "\t" + loadSeqString);
                for (int i = 0; i < metSize; i++) {
                    var pList = new List<double>();
                    for (int j = 0; j < maResult.PPreds.Count; j++) {
                        pList.Add(maResult.PPreds[j][i]);
                    }
                    pList.Add(maResult.Vips[i]);
                    pList.Add(maResult.Coefficients[i]);

                    sw.WriteLine(maResult.StatisticsObject.XLabels[i] + "\t" +
                        String.Join("\t", pList));
                }
            }
        }

        private static void writeOplsResult(string output, MultivariateAnalysisResult maResult) {
            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Method\t" + maResult.MultivariateAnalysisOption.ToString());
                sw.WriteLine("Optimized biological factor\t" + maResult.OptimizedFactor);
                sw.WriteLine("Optimized orthogonal factor\t" + maResult.OptimizedOrthoFactor);
                sw.WriteLine();
                sw.WriteLine("Cross validation N fold\t" + maResult.NFold);
                sw.WriteLine("Component\tSSCV\tPRESS\tQ2\tQ2cum");
                for (int i = 0; i < maResult.Presses.Count; i++) {
                    sw.WriteLine((i + 1).ToString() + "\t" + maResult.SsCVs[i] +
                        "\t" + maResult.Presses[i] + "\t" + maResult.Q2Values[i] +
                        "\t" + maResult.Q2Cums[i]);
                }
                sw.WriteLine();

                var scoreSeq = new List<string>();
                var loadSeq = new List<string>();

                for (int i = 0; i < maResult.OptimizedFactor; i++) {
                    scoreSeq.Add("T" + (i + 1).ToString());
                    loadSeq.Add("P" + (i + 1).ToString());
                }

                for (int i = 0; i < maResult.OptimizedOrthoFactor; i++) {
                    scoreSeq.Add("To" + (i + 1).ToString());
                    loadSeq.Add("Po" + (i + 1).ToString());
                }

                scoreSeq.Add("Y experiment"); scoreSeq.Add("Y predicted");
                loadSeq.Add("VIP"); loadSeq.Add("Coefficients");

                var scoreSeqString = String.Join("\t", scoreSeq);
                var loadSeqString = String.Join("\t", loadSeq);

                //header set
                var tpredSize = maResult.TPreds.Count;
                var toPredSize = maResult.ToPreds.Count;
                var metSize = maResult.StatisticsObject.XIndexes.Count;
                var fileSize = maResult.StatisticsObject.YIndexes.Count;

                sw.WriteLine("Score" + "\t" + scoreSeqString);

                //Scores
                for (int i = 0; i < fileSize; i++) {
                    var tList = new List<double>();
                    for (int j = 0; j < maResult.TPreds.Count; j++) {
                        tList.Add(maResult.TPreds[j][i]);
                    }
                    for (int j = 0; j < maResult.ToPreds.Count; j++) {
                        tList.Add(maResult.ToPreds[j][i]);
                    }
                    tList.Add(maResult.StatisticsObject.YVariables[i]);
                    tList.Add(maResult.PredictedYs[i]);

                    sw.WriteLine(maResult.StatisticsObject.YLabels[i] + "\t" + 
                        String.Join("\t", tList));
                }
                sw.WriteLine();

                //Loadings
                sw.WriteLine("Loading" + "\t" + loadSeqString);
                for (int i = 0; i < metSize; i++) {
                    var pList = new List<double>();
                    for (int j = 0; j < maResult.PPreds.Count; j++) {
                        pList.Add(maResult.PPreds[j][i]);
                    }
                    for (int j = 0; j < maResult.PoPreds.Count; j++) {
                        pList.Add(maResult.PoPreds[j][i]);
                    }
                    pList.Add(maResult.Vips[i]);
                    pList.Add(maResult.Coefficients[i]);

                    sw.WriteLine(maResult.StatisticsObject.XLabels[i] + "\t" +
                        String.Join("\t", pList));
                }
            }
        }

        private static void writePcaResult(string output, MultivariateAnalysisResult maResult) {

            using (StreamWriter sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //header set
                sw.WriteLine("Contribution");
                for (int i = 0; i < maResult.Contributions.Count; i++)
                    sw.WriteLine((i + 1).ToString() + "\t" + maResult.Contributions[i]);
                sw.WriteLine();

                var compSize = maResult.Contributions.Count;
                var filesize = maResult.StatisticsObject.YLabels.Count;
                var metsize = maResult.StatisticsObject.XLabels.Count;
                var compSequence = new List<int>();
                for (int i = 0; i < compSize; i++) {
                    compSequence.Add(i + 1);
                }
                var compSeqString = String.Join("\t", compSequence);

                //header set
                sw.WriteLine("Score" + "\t" + compSeqString);

                for (int i = 0; i < filesize; i++) {
                    var tList = new List<double>();
                    for (int j = 0; j < compSize; j++)
                        tList.Add(maResult.TPreds[j][i]);
                    sw.WriteLine(maResult.StatisticsObject.YLabels[i] + "\t" + String.Join("\t", tList));
                }

                sw.WriteLine();

                //header set
                sw.WriteLine("Loading" + "\t" + compSeqString);

                for (int i = 0; i < metsize; i++) {
                    var pList = new List<double>();
                    for (int j = 0; j < compSize; j++)
                        pList.Add(maResult.PPreds[j][i]);
                    sw.WriteLine(maResult.StatisticsObject.XLabels[i] + "\t" + String.Join("\t", pList));
                }
            }
        }

        
        private static void saveSpectraTableAsMassBankFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            AlignmentPropertyBean alignmentPropertyBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("ACCESSION: ......");

                sw.Write("RECORD_TITLE: ");
                if (alignmentPropertyBean.MetaboliteName == string.Empty) sw.WriteLine("Unknown");
                else sw.WriteLine(alignmentPropertyBean.MetaboliteName);

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                    sw.WriteLine("COMMENT: " + projectProp.Comment);
                }

                sw.WriteLine("CH$NAME: " + alignmentPropertyBean.MetaboliteName);
                sw.WriteLine("CH$INCHIKEY: " + MspDataRetrieve.GetInChIKey(alignmentPropertyBean.LibraryID, mspDB));
                sw.WriteLine("CH$SMILES: " + MspDataRetrieve.GetSMILES(alignmentPropertyBean.LibraryID, mspDB));
                sw.WriteLine("CH$FORMULA: " + MspDataRetrieve.GetFormula(alignmentPropertyBean.LibraryID, mspDB));

                sw.WriteLine("AC$CHROMATOGRAPHY: RETENTION_TIME " + alignmentPropertyBean.CentralRetentionTime);

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("AC$COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("AC$INSTRUMENT_TYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("AC$INSTRUMENT: " + projectProp.Instrument);
                }

                sw.WriteLine("MS$FOCUSED_ION: PRECURSOR_M/Z " + alignmentPropertyBean.CentralAccurateMass);
                sw.WriteLine("MS$FOCUSED_ION: ADDUCT_Ion " + alignmentPropertyBean.AdductIonName);

                if (massSpectra == null || massSpectra.Count == 0)
                {
                    sw.WriteLine("PK$NUM_PEAK: 0");
                    sw.WriteLine("PK$PEAK: m/z int. rel.int.");
                    sw.WriteLine("//");
                    sw.WriteLine();
                }
                else
                {
                    double maxIntensity = double.MinValue;
                    int peakNumber = 0;
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (maxIntensity < massSpectra[i][1]) maxIntensity = massSpectra[i][1];
                        if (massSpectra[i][1] > 0) peakNumber++;
                    }

                    sw.WriteLine("PK$NUM_PEAK: " + peakNumber);
                    sw.WriteLine("PK$PEAK: m/z int. rel.int.");

                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + " " + Math.Round(massSpectra[i][1] / maxIntensity * 999, 3));
                    }

                    sw.WriteLine("//");

                    sw.WriteLine();
                }
            }
        }

        private static void saveSpectraTableAsNistFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            AlignmentPropertyBean alignmentPropertyBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.Write("NAME: ");
                if (alignmentPropertyBean.MetaboliteName == string.Empty) sw.WriteLine("Unknown");
                else sw.WriteLine(alignmentPropertyBean.MetaboliteName);

                sw.WriteLine("RETENTIONTIME: " + alignmentPropertyBean.CentralRetentionTime);
                sw.WriteLine("PRECURSORMZ: " + alignmentPropertyBean.CentralAccurateMass);
                sw.WriteLine("PRECURSORTYPE: " + alignmentPropertyBean.AdductIonName);

                var adduct = AdductIonParcer.GetAdductIonBean(alignmentPropertyBean.AdductIonName);
                var ionMode = adduct.IonMode.ToString();
                sw.WriteLine("IONMODE: " + ionMode);

                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(alignmentPropertyBean.LibraryID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(alignmentPropertyBean.LibraryID, mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(alignmentPropertyBean.LibraryID, mspDB));

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
                }

                if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                    sw.WriteLine("COMMENT: " + projectProp.Comment);
                }

                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                    if (massSpectra[i][1] > 0) peakNumber++;

                sw.WriteLine("Num Peaks: " + peakNumber);

                if (massSpectra != null && peakNumber > 0)
                {
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
                    }
                }

                sw.WriteLine();
            }
        }

        private static void saveSpectraTableAsMascotFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            AlignmentPropertyBean alignmentPropertyBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("BEGIN IONS");

                sw.Write("TITLE=");
                if (alignmentPropertyBean.MetaboliteName == string.Empty) sw.WriteLine("Unknown");
                else sw.WriteLine(alignmentPropertyBean.MetaboliteName);

                sw.WriteLine("RTINMINUTES=" + alignmentPropertyBean.CentralRetentionTime);
                sw.WriteLine("PEPMASS=" + alignmentPropertyBean.CentralAccurateMass);
                sw.WriteLine("METABOLITENAME=" + alignmentPropertyBean.MetaboliteName);
                sw.WriteLine("ADDUCTIONNAME=" + alignmentPropertyBean.AdductIonName);

                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                    if (massSpectra[i][1] > 0) peakNumber++;

                if (massSpectra != null && peakNumber > 0)
                {
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0));
                    }
                }

                sw.WriteLine("END IONS");

                sw.WriteLine();
            }
        }

        private static void saveSpectraTableAsMassBankFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            PeakAreaBean peakAreaBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("ACCESSION: ......");

                sw.Write("RECORD_TITLE: ");
                if (peakAreaBean.MetaboliteName == string.Empty) sw.WriteLine("Unknown");
                else sw.WriteLine(peakAreaBean.MetaboliteName);

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                    sw.WriteLine("COMMENT: " + projectProp.Comment);
                }

                sw.WriteLine("CH$NAME: " + peakAreaBean.MetaboliteName);
                sw.WriteLine("CH$INCHIKEY: " + MspDataRetrieve.GetInChIKey(peakAreaBean.LibraryID, mspDB));
                sw.WriteLine("CH$SMILES: " + MspDataRetrieve.GetSMILES(peakAreaBean.LibraryID, mspDB));
                sw.WriteLine("CH$FORMULA: " + MspDataRetrieve.GetFormula(peakAreaBean.LibraryID, mspDB));
                sw.WriteLine("AC$CHROMATOGRAPHY: RETENTION_TIME " + peakAreaBean.RtAtPeakTop);
                sw.WriteLine("MS$FOCUSED_ION: SCAN_NUMBER "+ peakAreaBean.ScanNumberAtPeakTop);
                sw.WriteLine("MS$FOCUSED_ION: PRECURSOR_M/Z " + peakAreaBean.AccurateMass);
                sw.WriteLine("MS$FOCUSED_ION: ADDUCT_Ion " + peakAreaBean.AdductIonName);
                sw.WriteLine("MS$FOCUSED_ION: ISOTOPE " + "M + " + peakAreaBean.IsotopeWeightNumber.ToString());
              
                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("AC$COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("AC$INSTRUMENT_TYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("AC$INSTRUMENT: " + projectProp.Instrument);
                }

                if (massSpectra == null || massSpectra.Count == 0)
                {
                    sw.WriteLine("PK$NUM_PEAK: 0");
                    sw.WriteLine("PK$PEAK: m/z int. rel.int.");
                    sw.WriteLine("//");
                    sw.WriteLine();
                }
                else
                {
                    double maxIntensity = double.MinValue;

                    int peakNumber = 0;
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (maxIntensity < massSpectra[i][1]) maxIntensity = massSpectra[i][1];
                        if (massSpectra[i][1] > 0) peakNumber++;
                    }

                    sw.WriteLine("PK$NUM_PEAK: " + peakNumber);
                    sw.WriteLine("PK$PEAK: m/z int. rel.int.");
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + " " + Math.Round(massSpectra[i][1] / maxIntensity * 999, 3));
                    }
                    sw.WriteLine("//");

                    sw.WriteLine();
                }
            }
        }

        private static void saveSpectraTableAsNistFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            PeakAreaBean peakAreaBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.Write("NAME: ");
                if (peakAreaBean.MetaboliteName == string.Empty) sw.WriteLine("Unknown");
                else sw.WriteLine(peakAreaBean.MetaboliteName);

                sw.WriteLine("SCANNUMBER: " + peakAreaBean.ScanNumberAtPeakTop);
                sw.WriteLine("RETENTIONTIME: " + peakAreaBean.RtAtPeakTop);
                sw.WriteLine("PRECURSORMZ: " + peakAreaBean.AccurateMass);
                sw.WriteLine("PRECURSORTYPE: " + peakAreaBean.AdductIonName);

                var adduct = AdductIonParcer.GetAdductIonBean(peakAreaBean.AdductIonName);
                var ionMode = adduct.IonMode.ToString();
                sw.WriteLine("IONMODE: " + ionMode);

                sw.WriteLine("INTENSITY: " + peakAreaBean.IntensityAtPeakTop);
                sw.WriteLine("ISOTOPE: " + "M + " + peakAreaBean.IsotopeWeightNumber.ToString());

                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(peakAreaBean.LibraryID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(peakAreaBean.LibraryID, mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(peakAreaBean.LibraryID, mspDB));

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
                }

                if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                    sw.WriteLine("COMMENT: " + projectProp.Comment);
                }


                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                    if (massSpectra[i][1] > 0) peakNumber++;

                sw.WriteLine("Num Peaks: " + peakNumber);

                if (massSpectra != null && peakNumber > 0)
                {
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
                    }
                }

                sw.WriteLine();
            }
        }

        private static void saveSpectraTableAsMascotFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            PeakAreaBean peakAreaBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("BEGIN IONS");

                sw.Write("TITLE=");
                if (peakAreaBean.MetaboliteName == string.Empty) sw.WriteLine("Unknown");
                else sw.WriteLine(peakAreaBean.MetaboliteName);

                sw.WriteLine("SCANS=" + peakAreaBean.ScanNumberAtPeakTop);
                sw.WriteLine("RTINMINUTES=" + peakAreaBean.RtAtPeakTop);
                sw.WriteLine("PEPMASS=" + peakAreaBean.AccurateMass);
                sw.WriteLine("INTENSITY=" + peakAreaBean.IntensityAtPeakTop);
                sw.WriteLine("METABOLITENAME=" + peakAreaBean.MetaboliteName);
                sw.WriteLine("ADDUCTIONNAME=" + peakAreaBean.AdductIonName);
                sw.WriteLine("ISOTOPE=" + "M + " + peakAreaBean.IsotopeWeightNumber.ToString());

                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                    if (massSpectra[i][1] > 0) peakNumber++;

                if (massSpectra != null && peakNumber > 0)
                {
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0));
                    }
                }

                sw.WriteLine("END IONS");

                sw.WriteLine();
            }
        }

        private static void saveSpectraTableAsMassBankFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            MS1DecResult ms1DecResult, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("ACCESSION: ......");

                sw.Write("RECORD_TITLE: ");
                sw.WriteLine(ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz);

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                    sw.WriteLine("COMMENT: " + projectProp.Comment);
                }

                sw.Write("CH$NAME: ");
                sw.WriteLine(ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz);
                sw.WriteLine("CH$INCHIKEY: " + MspDataRetrieve.GetInChIKey(ms1DecResult.MspDbID, mspDB));
                sw.WriteLine("CH$SMILES: " + MspDataRetrieve.GetSMILES(ms1DecResult.MspDbID, mspDB));
                sw.WriteLine("CH$FORMULA: " + MspDataRetrieve.GetFormula(ms1DecResult.MspDbID, mspDB));

                sw.Write("AC$CHROMATOGRAPHY: RETENTION_TIME ");
                sw.WriteLine(ms1DecResult.RetentionTime);

                sw.Write("AC$CHROMATOGRAPHY: RETENTION_INDEX ");
                sw.WriteLine(ms1DecResult.RetentionIndex);

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("AC$COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("AC$INSTRUMENT_TYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("AC$INSTRUMENT: " + projectProp.Instrument);
                }

                sw.Write("MS$FOCUSED_ION: SCAN_NUMBER ");
                sw.WriteLine(ms1DecResult.ScanNumber);

                sw.Write("MS$FOCUSED_ION: QUANT_M/Z ");
                sw.WriteLine(ms1DecResult.BasepeakMz);

                if (massSpectra == null || massSpectra.Count == 0)
                {
                    sw.WriteLine("PK$NUM_PEAK: 0");
                    sw.WriteLine("PK$PEAK: m/z int. rel.int.");
                    sw.WriteLine("//");
                    sw.WriteLine();
                }
                else
                {
                    double maxIntensity = double.MinValue;

                    int peakNumber = 0;
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (maxIntensity < massSpectra[i][1]) maxIntensity = massSpectra[i][1];
                        if (massSpectra[i][1] > 0) peakNumber++;
                    }

                    sw.Write("PK$NUM_PEAK: ");
                    sw.WriteLine(peakNumber);

                    sw.WriteLine("PK$PEAK: m/z int. rel.int.");
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + " " + Math.Round(massSpectra[i][1] / maxIntensity * 999, 3));
                    }
                    sw.WriteLine("//");

                    sw.WriteLine();
                }
            }
        }

        private static void saveSpectraTableAsNistFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            MS1DecResult ms1DecResult, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.Write("NAME: ");
                sw.WriteLine(ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz);

                sw.WriteLine("SCANNUMBER: " + ms1DecResult.ScanNumber);
                sw.WriteLine("RETENTIONTIME: " + ms1DecResult.RetentionTime);
                sw.WriteLine("RETENTIONINDEX: " + ms1DecResult.RetentionIndex);
                sw.WriteLine("QUANTMASS: " + ms1DecResult.BasepeakMz);
                sw.WriteLine("INTENSITY: " + ms1DecResult.BasepeakHeight);
                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(ms1DecResult.MspDbID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(ms1DecResult.MspDbID, mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(ms1DecResult.MspDbID, mspDB));

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
                }

                if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                    sw.WriteLine("COMMENT: " + projectProp.Comment);
                }

                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                    if (massSpectra[i][1] > 0) peakNumber++;

                sw.WriteLine("Num Peaks: " + peakNumber);

                if (massSpectra != null && peakNumber > 0)
                {
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
                    }
                }

                sw.WriteLine();
            }
        }

        private static void saveSpectraTableAsMascotFormat(string exportFilePath, ObservableCollection<double[]> massSpectra,
            MS1DecResult ms1DecResult, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("BEGIN IONS");

                sw.Write("TITLE=");
                sw.WriteLine(ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz);

                sw.WriteLine("SCANS=" + ms1DecResult.ScanNumber);
                sw.WriteLine("RTINMINUTES=" + ms1DecResult.RetentionTime);
                sw.WriteLine("RETENTIONINDEX=" + ms1DecResult.RetentionIndex);
                sw.WriteLine("QUANTMASS=" + ms1DecResult.BasepeakMz);
                sw.WriteLine("INTENSITY=" + ms1DecResult.BasepeakHeight);

                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                    if (massSpectra[i][1] > 0) peakNumber++;

                if (massSpectra != null && peakNumber > 0)
                {
                    for (int i = 0; i < massSpectra.Count; i++)
                    {
                        if (massSpectra[i][1] <= 0) continue;
                        sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0));
                    }
                }

                sw.WriteLine("END IONS");

                sw.WriteLine();
            }
        }


        private static void copyToClipboardSpectraTableAsMassBankFormat(ObservableCollection<double[]> massSpectra,
            PeakAreaBean peakAreaBean, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "ACCESSION: ......" + "\r\n";

            text += "RECORD_TITLE: ";
            if (peakAreaBean.MetaboliteName == string.Empty) text += "Unknown" + "\r\n";
            else text += peakAreaBean.MetaboliteName + "\r\n";

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                text += "AUTHORS: " + projectProp.Authors + "\r\n";
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                text += "LICENSE: " + projectProp.License + "\r\n";
            }

            if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                text += "COMMENT: " + projectProp.Comment + "\r\n";
            }

            text += "CH$NAME: ";
            text += peakAreaBean.MetaboliteName + "\r\n";

            text += "CH$INCHIKEY: " + MspDataRetrieve.GetInChIKey(peakAreaBean.LibraryID, mspDB) + "\r\n";
            text += "CH$SMILES: " + MspDataRetrieve.GetSMILES(peakAreaBean.LibraryID, mspDB) + "\r\n";
            text += "CH$FORMULA: " + MspDataRetrieve.GetFormula(peakAreaBean.LibraryID, mspDB) + "\r\n";

            text += "AC$CHROMATOGRAPHY: RETENTION_TIME ";
            text += peakAreaBean.RtAtPeakTop + "\r\n";

            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                text += "AC$COLLISIONENERGY: " + projectProp.CollisionEnergy + "\r\n";
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                text += "AC$INSTRUMENT_TYPE: " + projectProp.InstrumentType + "\r\n";
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                text += "AC$INSTRUMENT: " + projectProp.Instrument + "\r\n";
            }

            text += "MS$FOCUSED_ION: SCAN_NUMBER ";
            text += peakAreaBean.ScanNumberAtPeakTop + "\r\n";

            text += "MS$FOCUSED_ION: PRECURSOR_M/Z ";
            text += peakAreaBean.AccurateMass + "\r\n";

            text += "MS$FOCUSED_ION: ADDUCT_Ion ";
            text += peakAreaBean.AdductIonName + "\r\n";

            text += "MS$FOCUSED_ION: ISOTOPE ";
            text += "M + " + peakAreaBean.IsotopeWeightNumber.ToString() + "\r\n";

            if (massSpectra == null || massSpectra.Count == 0)
            {
                text += "PK$NUM_PEAK: 0" + "\r\n";
                text += "PK$PEAK: m/z int. rel.int." + "\r\n";
                text += "//" + "\r\n";
            }
            else
            {
                double maxIntensity = double.MinValue;

                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (maxIntensity < massSpectra[i][1]) maxIntensity = massSpectra[i][1];
                    if (massSpectra[i][1] > 0) peakNumber++;
                }

                text += "PK$NUM_PEAK: ";
                text += peakNumber + "\r\n";

                text += "PK$PEAK: m/z int. rel.int." + "\r\n";

                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + " " + Math.Round(massSpectra[i][1] / maxIntensity * 999, 3) + "\r\n";
                }

                text += "//" + "\r\n";
            }

            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsNistFormat(ObservableCollection<double[]> massSpectra, PeakAreaBean peakAreaBean
            , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "NAME: ";
            if (peakAreaBean.MetaboliteName == string.Empty) text += "Unknown" + "\r\n";
            else text += peakAreaBean.MetaboliteName + "\r\n";

            text += "SCANNUMBER: " + peakAreaBean.ScanNumberAtPeakTop + "\r\n";
            text += "RETENTIONTIME: " + peakAreaBean.RtAtPeakTop + "\r\n";
            text += "PRECURSORMZ: " + peakAreaBean.AccurateMass + "\r\n";
            text += "PRECURSORTYPE: " + peakAreaBean.AdductIonName + "\r\n";

            var adduct = AdductIonParcer.GetAdductIonBean(peakAreaBean.AdductIonName);
            var ionMode = adduct.IonMode.ToString();
            text += "IONMODE: " + ionMode + "\r\n";
            text += "INCHIKEY: " + MspDataRetrieve.GetInChIKey(peakAreaBean.LibraryID, mspDB) + "\r\n";
            text += "SMILES: " + MspDataRetrieve.GetSMILES(peakAreaBean.LibraryID, mspDB) + "\r\n";
            text += "FORMULA: " + MspDataRetrieve.GetFormula(peakAreaBean.LibraryID, mspDB) + "\r\n";
            text += "INTENSITY: " + peakAreaBean.IntensityAtPeakTop + "\r\n";
            text += "ISOTOPE: " + "M + " + peakAreaBean.IsotopeWeightNumber.ToString() + "\r\n";

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                text += "AUTHORS: " + projectProp.Authors + "\r\n";
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                text += "LICENSE: " + projectProp.License + "\r\n";
            }

            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                text += "COLLISIONENERGY: " + projectProp.CollisionEnergy + "\r\n";
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                text += "INSTRUMENTTYPE: " + projectProp.InstrumentType + "\r\n";
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                text += "INSTRUMENT: " + projectProp.Instrument + "\r\n";
            }

            if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                text += "COMMENT: " + projectProp.Comment + "\r\n";
            }

            int peakNumber = 0;
            for (int i = 0; i < massSpectra.Count; i++)
                if (massSpectra[i][1] > 0) peakNumber++;

            text += "Num Peaks: " + peakNumber + "\r\n";

            if (massSpectra != null && peakNumber > 0)
            {
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\r\n";
                }
            }

            text += "\r\n";
            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsMascotFormat(ObservableCollection<double[]> massSpectra, PeakAreaBean peakAreaBean
            , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "BEGIN IONS" + "\r\n";

            text += "TITLE=";
            if (peakAreaBean.MetaboliteName == string.Empty) text += "Unknown" + "\r\n";
            else text += peakAreaBean.MetaboliteName + "\r\n";

            text += "SCANS=";
            text += peakAreaBean.ScanNumberAtPeakTop + "\r\n";

            text += "RTINMINUTES=";
            text += peakAreaBean.RtAtPeakTop + "\r\n";

            text += "PEPMASS=";
            text += peakAreaBean.AccurateMass + "\r\n";

            text += "INTENSITY=";
            text += peakAreaBean.IntensityAtPeakTop + "\r\n";

            text += "METABOLITENAME=";
            text += peakAreaBean.MetaboliteName + "\r\n";

            text += "ADDUCTIONNAME=";
            text += peakAreaBean.AdductIonName + "\r\n";

            text += "ISOTOPE=";
            text += "M + " + peakAreaBean.IsotopeWeightNumber.ToString() + "\r\n";

            int peakNumber = 0;
            for (int i = 0; i < massSpectra.Count; i++)
                if (massSpectra[i][1] > 0) peakNumber++;

            if (massSpectra != null && peakNumber > 0)
            {
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + "\r\n";
                }
            }

            text += "END IONS" + "\r\n";

            text += "\r\n";
            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsMassBankFormat(ObservableCollection<double[]> massSpectra, AlignmentPropertyBean alignmentPropertyBean
            , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "ACCESSION: ......" + "\r\n";

            text += "RECORD_TITLE: ";
            if (alignmentPropertyBean.MetaboliteName == string.Empty) text += "Unknown" + "\r\n";
            else text += alignmentPropertyBean.MetaboliteName + "\r\n";

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                text += "AUTHORS: " + projectProp.Authors + "\r\n";
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                text += "LICENSE: " + projectProp.License + "\r\n";
            }

            if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                text += "COMMENT: " + projectProp.Comment + "\r\n";
            }

            text += "CH$NAME: " + alignmentPropertyBean.MetaboliteName + "\r\n";
            text += "CH$INCHIKEY: " + MspDataRetrieve.GetInChIKey(alignmentPropertyBean.LibraryID, mspDB) + "\r\n";
            text += "CH$SMILES: " + MspDataRetrieve.GetSMILES(alignmentPropertyBean.LibraryID, mspDB) + "\r\n";
            text += "CH$FORMULA: " + MspDataRetrieve.GetFormula(alignmentPropertyBean.LibraryID, mspDB) + "\r\n";

            text += "AC$CHROMATOGRAPHY: RETENTION_TIME " + alignmentPropertyBean.CentralRetentionTime + "\r\n";
            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                text += "AC$COLLISIONENERGY: " + projectProp.CollisionEnergy + "\r\n";
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                text += "AC$INSTRUMENT_TYPE: " + projectProp.InstrumentType + "\r\n";
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                text += "AC$INSTRUMENT: " + projectProp.Instrument + "\r\n";
            }

            text += "MS$FOCUSED_ION: PRECURSOR_M/Z " + alignmentPropertyBean.CentralAccurateMass + "\r\n";
            text += "MS$FOCUSED_ION: ADDUCT_Ion " + alignmentPropertyBean.AdductIonName + "\r\n";

            if (massSpectra == null || massSpectra.Count == 0)
            {
                text += "PK$NUM_PEAK: 0" + "\r\n";
                text += "PK$PEAK: m/z int. rel.int." + "\r\n";
                text += "//" + "\r\n";
            }
            else
            {
                double maxIntensity = double.MinValue;
                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (maxIntensity < massSpectra[i][1]) maxIntensity = massSpectra[i][1];
                    if (massSpectra[i][1] > 0) peakNumber++;
                }

                text += "PK$NUM_PEAK: ";
                text += peakNumber + "\r\n";

                text += "PK$PEAK: m/z int. rel.int." + "\r\n";

                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + " " + Math.Round(massSpectra[i][1] / maxIntensity * 999, 3) + "\r\n";
                }

                text += "//" + "\r\n";
            }

            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsNistFormat(ObservableCollection<double[]> massSpectra, AlignmentPropertyBean alignmentPropertyBean
            , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "NAME: ";
            if (alignmentPropertyBean.MetaboliteName == string.Empty) text += "Unknown" + "\r\n";
            else text += alignmentPropertyBean.MetaboliteName + "\r\n";

            text += "RETENTIONTIME: " + alignmentPropertyBean.CentralRetentionTime + "\r\n";
            text += "PRECURSORMZ: " + alignmentPropertyBean.CentralAccurateMass + "\r\n";
            text += "PRECURSORTYPE: " + alignmentPropertyBean.AdductIonName + "\r\n";

            var adduct = AdductIonParcer.GetAdductIonBean(alignmentPropertyBean.AdductIonName);
            var ionMode = adduct.IonMode.ToString();
            text += "IONMODE: " + ionMode + "\r\n";

            text += "INCHIKEY: " + MspDataRetrieve.GetInChIKey(alignmentPropertyBean.LibraryID, mspDB) + "\r\n";
            text += "SMILES: " + MspDataRetrieve.GetSMILES(alignmentPropertyBean.LibraryID, mspDB) + "\r\n";
            text += "FORMULA: " + MspDataRetrieve.GetFormula(alignmentPropertyBean.LibraryID, mspDB) + "\r\n";

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                text += "AUTHORS: " + projectProp.Authors + "\r\n";
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                text += "LICENSE: " + projectProp.License + "\r\n";
            }

            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                text += "COLLISIONENERGY: " + projectProp.CollisionEnergy + "\r\n";
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                text += "INSTRUMENTTYPE: " + projectProp.InstrumentType + "\r\n";
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                text += "INSTRUMENT: " + projectProp.Instrument + "\r\n";
            }

            if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                text += "COMMENT: " + projectProp.Comment + "\r\n";
            }

            int peakNumber = 0;
            for (int i = 0; i < massSpectra.Count; i++)
                if (massSpectra[i][1] > 0) peakNumber++;

            text += "Num Peaks: " + peakNumber + "\r\n";

            if (massSpectra != null && peakNumber > 0)
            {
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\r\n";
                }
            }

            text += "\r\n";
            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsMascotFormat(ObservableCollection<double[]> massSpectra, AlignmentPropertyBean alignmentPropertyBean
            , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "BEGIN IONS" + "\r\n";

            text += "TITLE=";
            if (alignmentPropertyBean.MetaboliteName == string.Empty) text += "Unknown" + "\r\n";
            else text += alignmentPropertyBean.MetaboliteName + "\r\n";

            text += "RTINMINUTES=";
            text += alignmentPropertyBean.CentralRetentionTime + "\r\n";

            text += "PEPMASS=";
            text += alignmentPropertyBean.CentralAccurateMass + "\r\n";

            text += "METABOLITENAME=";
            text += alignmentPropertyBean.MetaboliteName + "\r\n";

            text += "ADDUCTIONNAME=";
            text += alignmentPropertyBean.AdductIonName + "\r\n";

            int peakNumber = 0;
            for (int i = 0; i < massSpectra.Count; i++)
                if (massSpectra[i][1] > 0) peakNumber++;

            if (massSpectra != null && peakNumber > 0)
            {
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + "\r\n";
                }
            }

            text += "END IONS" + "\r\n";

            text += "\r\n";
            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsMassBankFormat(ObservableCollection<double[]> massSpectra, MS1DecResult ms1DecResult
            , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "ACCESSION: ......" + "\r\n";

            text += "RECORD_TITLE: ";
            text += ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz + "\r\n";

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                text += "AUTHORS: " + projectProp.Authors + "\r\n";
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                text += "LICENSE: " + projectProp.License + "\r\n";
            }

            if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                text += "COMMENT: " + projectProp.Comment + "\r\n";
            }


            text += "CH$NAME: ";
            text += ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz + "\r\n";
            text += "CH$INCHIKEY: " + MspDataRetrieve.GetInChIKey(ms1DecResult.MspDbID, mspDB) + "\r\n";
            text += "CH$SMILES: " + MspDataRetrieve.GetSMILES(ms1DecResult.MspDbID, mspDB) + "\r\n";
            text += "CH$FORMULA: " + MspDataRetrieve.GetFormula(ms1DecResult.MspDbID, mspDB) + "\r\n";

            text += "AC$CHROMATOGRAPHY: RETENTION_TIME " + ms1DecResult.RetentionTime + "\r\n";
            text += "AC$CHROMATOGRAPHY: RETENTION_INDEX " + ms1DecResult.RetentionIndex + "\r\n";

            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                text += "AC$COLLISIONENERGY: " + projectProp.CollisionEnergy + "\r\n";
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                text += "AC$INSTRUMENT_TYPE: " + projectProp.InstrumentType + "\r\n";
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                text += "AC$INSTRUMENT: " + projectProp.Instrument + "\r\n";
            }

            text += "MS$FOCUSED_ION: QUANT_M/Z " + ms1DecResult.BasepeakMz + "\r\n";

            if (massSpectra == null || massSpectra.Count == 0)
            {
                text += "PK$NUM_PEAK: 0" + "\r\n";
                text += "PK$PEAK: m/z int. rel.int." + "\r\n";
                text += "//" + "\r\n";
            }
            else
            {
                double maxIntensity = double.MinValue;
                int peakNumber = 0;
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (maxIntensity < massSpectra[i][1]) maxIntensity = massSpectra[i][1];
                    if (massSpectra[i][1] > 0) peakNumber++;
                }

                text += "PK$NUM_PEAK: " + peakNumber + "\r\n";
                text += "PK$PEAK: m/z int. rel.int." + "\r\n";

                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + " " + Math.Round(massSpectra[i][1] / maxIntensity * 999, 3) + "\r\n";
                }

                text += "//" + "\r\n";
            }

            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsNistFormat(ObservableCollection<double[]> massSpectra, MS1DecResult ms1DecResult
             , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "NAME: ";
            text += ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz + "\r\n";

            text += "RETENTIONTIME: " + ms1DecResult.RetentionTime + "\r\n";
            text += "RETENTIONINDEX: " + ms1DecResult.RetentionIndex + "\r\n";
            text += "QUANTMASS: " + ms1DecResult.BasepeakMz + "\r\n";

            text += "INCHIKEY: " + MspDataRetrieve.GetInChIKey(ms1DecResult.MspDbID, mspDB) + "\r\n";
            text += "SMILES: " + MspDataRetrieve.GetSMILES(ms1DecResult.MspDbID, mspDB) + "\r\n";
            text += "FORMULA: " + MspDataRetrieve.GetFormula(ms1DecResult.MspDbID, mspDB) + "\r\n";

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                text += "AUTHORS: " + projectProp.Authors + "\r\n";
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                text += "LICENSE: " + projectProp.License + "\r\n";
            }

            if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                text += "COLLISIONENERGY: " + projectProp.CollisionEnergy + "\r\n";
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                text += "INSTRUMENTTYPE: " + projectProp.InstrumentType + "\r\n";
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                text += "INSTRUMENT: " + projectProp.Instrument + "\r\n";
            }

            if (projectProp.Comment != null && projectProp.Comment != string.Empty) {
                text += "COMMENT: " + projectProp.Comment + "\r\n";
            }

            int peakNumber = 0;
            for (int i = 0; i < massSpectra.Count; i++)
                if (massSpectra[i][1] > 0) peakNumber++;

            text += "Num Peaks: " + peakNumber + "\r\n";

            if (massSpectra != null && peakNumber > 0)
            {
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\r\n";
                }
            }

            text += "\r\n";
            Clipboard.SetDataObject(text, true);
        }

        private static void copyToClipboardSpectraTableAsMascotFormat(ObservableCollection<double[]> massSpectra, MS1DecResult ms1DecResult
            , ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB)
        {
            string text = string.Empty;
            text += "BEGIN IONS" + "\r\n";

            text += "TITLE=";
            text += ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz + "\r\n";

            text += "RTINMINUTES=";
            text += ms1DecResult.RetentionTime + "\r\n";

            text += "RETENTIONINDEX=";
            text += ms1DecResult.RetentionIndex + "\r\n";

            text += "QUANTMASS=";
            text += ms1DecResult.BasepeakMz + "\r\n";

            int peakNumber = 0;
            for (int i = 0; i < massSpectra.Count; i++)
                if (massSpectra[i][1] > 0) peakNumber++;

            if (massSpectra != null && peakNumber > 0)
            {
                for (int i = 0; i < massSpectra.Count; i++)
                {
                    if (massSpectra[i][1] <= 0) continue;
                    text += Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + "\r\n";
                }
            }

            text += "END IONS" + "\r\n";

            text += "\r\n";
            Clipboard.SetDataObject(text, true);
        }


        private static void saveChromatogramDataTableAsText(string exportFilePath, ChromatogramMrmViewModel chromatogramMrmViewModel, PeakAreaBean peakAreaBean)
        {
            if (chromatogramMrmViewModel == null || chromatogramMrmViewModel.ChromatogramBeanCollection == null || chromatogramMrmViewModel.ChromatogramBeanCollection.Count == 0)
            {
                MessageBox.Show("The chromatogram data cannot be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                int chromatoCount = chromatogramMrmViewModel.ChromatogramBeanCollection.Count;

                sw.Write("NAME: ");
                if (peakAreaBean.MetaboliteName == string.Empty) sw.WriteLine("Unknown");
                else sw.WriteLine(peakAreaBean.MetaboliteName);

                sw.Write("SCANNUMBER: ");
                sw.WriteLine(peakAreaBean.ScanNumberAtPeakTop);

                sw.Write("RETENTIONTIME: ");
                sw.WriteLine(peakAreaBean.RtAtPeakTop);

                sw.Write("PRECURSORMZ: ");
                sw.WriteLine(peakAreaBean.AccurateMass);

                sw.Write("INTENSITY: ");
                sw.WriteLine(peakAreaBean.IntensityAtPeakTop);

                sw.Write("METABOLITENAME: ");
                sw.WriteLine(peakAreaBean.MetaboliteName);

                sw.Write("ADDUCTIONNAME: ");
                sw.WriteLine(peakAreaBean.AdductIonName);

                sw.Write("ISOTOPE: ");
                sw.WriteLine("M + " + peakAreaBean.IsotopeWeightNumber.ToString());

                sw.WriteLine();

                ChromatogramBean chromatogramBean;
                for (int i = 0; i < chromatoCount; i++)
                {
                    chromatogramBean = chromatogramMrmViewModel.ChromatogramBeanCollection[i];

                    sw.Write("PRECURSOR_MZ: ");
                    sw.WriteLine(chromatogramBean.PrecursorMz);

                    sw.Write("PRODUCT_MZ: ");
                    sw.WriteLine(chromatogramBean.ProductMz);

                    sw.WriteLine("SCAN RETENTIONTIME BASEMZ INTENSITY");
                    for (int j = 0; j < chromatogramBean.ChromatogramDataPointCollection.Count; j++)
                    {
                        sw.WriteLine(chromatogramBean.ChromatogramDataPointCollection[j][0] + " " + chromatogramBean.ChromatogramDataPointCollection[j][1] + " " + chromatogramBean.ChromatogramDataPointCollection[j][2] + " " + chromatogramBean.ChromatogramDataPointCollection[j][3]);
                    }

                    sw.WriteLine();
                }
            }
        }

        private static void saveChromatogramDataTableAsText(string exportFilePath, ChromatogramMrmViewModel chromatogramMrmViewModel, MS1DecResult ms1DecResult)
        {
            if (chromatogramMrmViewModel == null || chromatogramMrmViewModel.ChromatogramBeanCollection == null || chromatogramMrmViewModel.ChromatogramBeanCollection.Count == 0)
            {
                MessageBox.Show("The chromatogram data cannot be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                int chromatoCount = chromatogramMrmViewModel.ChromatogramBeanCollection.Count;

                sw.Write("NAME: ");
                sw.WriteLine(ms1DecResult.ScanNumber + "-" + ms1DecResult.RetentionTime + "-" + ms1DecResult.RetentionIndex + "-" + ms1DecResult.BasepeakMz);

                sw.Write("SCANNUMBER: ");
                sw.WriteLine(ms1DecResult.ScanNumber);

                sw.Write("RETENTIONTIME: ");
                sw.WriteLine(ms1DecResult.RetentionTime);

                sw.Write("RETENTIONINDEX: ");
                sw.WriteLine(ms1DecResult.RetentionIndex);

                sw.Write("QUANTMASS: ");
                sw.WriteLine(ms1DecResult.BasepeakMz);

                sw.Write("INTENSITY: ");
                sw.WriteLine(ms1DecResult.BasepeakHeight);

                sw.WriteLine();

                ChromatogramBean chromatogramBean;
                for (int i = 0; i < chromatoCount; i++)
                {
                    chromatogramBean = chromatogramMrmViewModel.ChromatogramBeanCollection[i];
                    sw.Write("MZ: ");
                    sw.WriteLine(chromatogramBean.ProductMz);

                    sw.WriteLine("SCAN RETENTIONTIME BASEMZ INTENSITY");
                    for (int j = 0; j < chromatogramBean.ChromatogramDataPointCollection.Count; j++)
                    {
                        sw.WriteLine(chromatogramBean.ChromatogramDataPointCollection[j][0] + " " + chromatogramBean.ChromatogramDataPointCollection[j][1] + " " + chromatogramBean.ChromatogramDataPointCollection[j][2] + " " + chromatogramBean.ChromatogramDataPointCollection[j][3]);
                    }

                    sw.WriteLine();
                }
            }
        }

        private static void saveChromatogramDataTableAsText(string exportFilePath, ChromatogramTicEicViewModel chromatogramTicEicViewModel)
        {
            if (chromatogramTicEicViewModel == null || chromatogramTicEicViewModel.ChromatogramBeanCollection == null || chromatogramTicEicViewModel.ChromatogramBeanCollection.Count == 0)
            {
                MessageBox.Show("The chromatogram data cannot be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII))
            {
                int chromatoCount = chromatogramTicEicViewModel.ChromatogramBeanCollection.Count;
                ChromatogramBean chromatogramBean;
                for (int i = 0; i < chromatoCount; i++)
                {
                    chromatogramBean = chromatogramTicEicViewModel.ChromatogramBeanCollection[i];

                    sw.Write("Name: ");
                    sw.WriteLine(chromatogramBean.MetaboliteName);

                    sw.Write("MZ: ");
                    sw.WriteLine(chromatogramBean.Mz);

                    sw.Write("MASSTOLERANCE: ");
                    sw.WriteLine(chromatogramBean.MassTolerance);

                    sw.WriteLine("SCAN RETENTIONTIME BASEMZ INTENSITY");
                    for (int j = 0; j < chromatogramBean.ChromatogramDataPointCollection.Count; j++)
                    {
                        sw.WriteLine(chromatogramBean.ChromatogramDataPointCollection[j][0] + " " + chromatogramBean.ChromatogramDataPointCollection[j][1] + " " + chromatogramBean.ChromatogramDataPointCollection[j][2] + " " + chromatogramBean.ChromatogramDataPointCollection[j][3]);
                    }

                    sw.WriteLine();
                }
            }
        }

        private static void saveChromatogramDataTableAsText(string exportFilePath, ChromatogramXicViewModel chrom) {
            if (chrom == null || chrom.ChromatogramBean == null || chrom.ChromatogramBean.ChromatogramDataPointCollection == null || chrom.ChromatogramBean.ChromatogramDataPointCollection.Count == 0) {
                MessageBox.Show("The chromatogram data cannot be found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII)) {

                var chromatogram = chrom.ChromatogramBean;
                var chrompoints = chromatogram.ChromatogramDataPointCollection;

                sw.WriteLine("MZ: " + chromatogram.Mz);
                sw.WriteLine("Tolerance: " + chromatogram.MassTolerance);
                sw.WriteLine("SCAN\tRETENTIONTIME\tMZ\tINTENSITY");
                for (int i = 0; i < chrompoints.Count; i++) {
                    var point = chrompoints[i];
                    sw.WriteLine(point[0] + "\t" + point[1] + "\t" + point[2] + "\t" + point[3]);
                }
            }
        }

        private static double getCollisionEnergy(string ce)
        {
            if (ce == null || ce == string.Empty) return -1;
            string figure = string.Empty;
            double ceValue = 0.0;
            for (int i = 0; i < ce.Length; i++) {
                if (Char.IsNumber(ce[i]) || ce[i] == '.') {
                    figure += ce[i];
                }
                else {
                    double.TryParse(figure, out ceValue);
                    return ceValue;
                }
            }
            double.TryParse(figure, out ceValue);
            return ceValue;
        }
   
    }
}
