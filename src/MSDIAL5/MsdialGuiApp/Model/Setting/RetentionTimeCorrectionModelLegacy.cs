using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Setting {
    public class RetentionTimeCorrectionModelLegacy {
        //#region Main Work

        //public static void UpdateRtCorrectionBean(List<AnalysisFileBean> files, ParallelOptions parallelOptions, RetentionTimeCorrectionParam rtParam, List<CommonStdData> commonStdList) {
        //    if (rtParam.RtDiffCalcMethod == RtDiffCalcMethod.SampleMinusSampleAverage) {
        //        Parallel.ForEach(files, parallelOptions, f => {
        //            if (f.RetentionTimeCorrectionBean.StandardList != null && f.RetentionTimeCorrectionBean.StandardList.Count > 0)
        //                f.RetentionTimeCorrectionBean = RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusAverage(rtParam, f.RetentionTimeCorrectionBean.StandardList, f.RetentionTimeCorrectionBean.OriginalRt.ToArray(), commonStdList);
        //        });
        //    }
        //    else {
        //        Parallel.ForEach(files, parallelOptions, f => {
        //            if (f.RetentionTimeCorrectionBean.StandardList != null && f.RetentionTimeCorrectionBean.StandardList.Count > 0)
        //                f.RetentionTimeCorrectionBean = RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusReference(
        //                    rtParam, f.RetentionTimeCorrectionBean.StandardList, f.RetentionTimeCorrectionBean.OriginalRt.ToArray());
        //        });
        //    }
        //}

        //public static List<CommonStdData> MakeCommonStdList(List<AnalysisFileBean> analysisFiles, List<MoleculeMsReference> iStdList) {
        //    var commonStdList = new List<CommonStdData>();
        //    var tmpStdList = iStdList.Where(x => x.IsTargetMolecule).OrderBy(x => x.ChromXs.RT.Value);
        //    foreach (var std in tmpStdList) {
        //        commonStdList.Add(new CommonStdData(std));
        //    }
        //    for (var i = 0; i < analysisFiles.Count; i++) {
        //        for (var j = 0; j < commonStdList.Count; j++) {
        //            commonStdList[j].SetStandard(analysisFiles[i].RetentionTimeCorrectionBean.StandardList[j]);
        //        }
        //    }
        //    foreach (var d in commonStdList) {
        //        d.CalcAverageRetentionTime();
        //    }
        //    return commonStdList;
        //}


        //#endregion

        #region Make Standard compound table
        public static List<StandardCompoundVM> InitializeStandardDataTable() {
            var list = new List<StandardCompoundVM>();
            for (var i = 0; i < 100; i++) {
                list.Add(new StandardCompoundVM() { ReferenceId = i, IsTarget = false });
            }
            return list;
        }

        public static List<StandardCompoundVM> LoadLibraryFile() {
            var ofd = new Microsoft.Win32.OpenFileDialog();

            ofd.Filter = "Text files(*.txt)|*.txt";
            ofd.Title = "Select standard library file (.txt format)";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true) {
                var error = string.Empty;
                var list = ConvertTextFormatToCompoundVM(TextLibraryParser.StandardTextLibraryReader(ofd.FileName, out error));
                if (error != string.Empty) {
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                if (list == null || list.Count == 0) return InitializeStandardDataTable();
                for (var i = list.Count; i < 100; i++) {
                    list.Add(new StandardCompoundVM() { ReferenceId = i, IsTarget = false });
                }
                return list;
            }
            return InitializeStandardDataTable();
        }

        public static List<StandardCompoundVM>? ConvertTextFormatToCompoundVM(List<MoleculeMsReference>? lib) {
            if (lib == null) return null;
            var stdVmList = new List<StandardCompoundVM>();
            foreach (var l in lib) {
                stdVmList.Add(new StandardCompoundVM(l) { IsTarget = true });
            }
            return stdVmList;
        }

        public static List<MoleculeMsReference> ConvertCompoundVMtoTextFormat(List<StandardCompoundVM> lib) {
            return lib.Where(n => n.AccurateMass > 0 && n.AccurateMassTolerance > 0).Select(x => x.TextFormatCompoundInformationBean).ToList();
        }


        #endregion

    }
}
