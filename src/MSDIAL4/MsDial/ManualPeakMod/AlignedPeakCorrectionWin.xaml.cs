using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CompMs.RawDataHandler.Core;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;

namespace Rfx.Riken.OsakaUniv.ManualPeakMod
{
    /// <summary>
    /// AlignedPeakCorrectionWin.xaml の相互作用ロジック
    /// </summary>
    public partial class AlignedChromatogramModificationWin : Window
    {
        public AlignedChromatogramModificationVM VM { get; set; }
        public AlignedChromatogramModificationWin() {
            InitializeComponent();
        }

        public AlignedChromatogramModificationWin(AlignedData alignedData, AlignmentPropertyBean bean, 
            ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean projectPropety,
            AnalysisParametersBean param) {
            InitializeComponent();
            var peakPropertyList = Model.SetPeakProperty(alignedData, bean, files, param, projectPropety);
            VM = new AlignedChromatogramModificationVM(bean, peakPropertyList, projectPropety, param);
            this.DataContext = VM;
        }

        public AlignedChromatogramModificationWin(AlignedData alignedData, AlignedDriftSpotPropertyBean bean,
            ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean projectPropety,
            AnalysisParametersBean param, List<SolidColorBrush> solidColorBrushList) {
            InitializeComponent();
            var peakPropertyList = Model.SetPeakProperty(alignedData, bean, files, param, projectPropety);
            VM = new AlignedChromatogramModificationVM(bean, peakPropertyList, projectPropety, param);
            this.DataContext = VM;
        }

        public AlignedChromatogramModificationWin(AlignedData alignedData, AlignmentPropertyBean bean,
          ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean projectPropety,
          AnalysisParamOfMsdialGcms param) {
            InitializeComponent();
            var peakPropertyList = Model.SetPeakProperty(alignedData, bean, files, param, projectPropety);
            VM = new AlignedChromatogramModificationVM(bean, peakPropertyList, projectPropety, param);
            this.DataContext = VM;
        }

        private void Button_UpdatePeakInfo_Click(object sender, RoutedEventArgs e) {
            VM.UpdatePeakInfo();
        }

        public void ChangeVM(AlignedData alignedData, AlignmentPropertyBean bean, ObservableCollection<AnalysisFileBean> files,
            ProjectPropertyBean projectPropety, AnalysisParametersBean param) {
            var peakPropertyList = Model.SetPeakProperty(alignedData, bean, files, param, projectPropety);
            VM = new AlignedChromatogramModificationVM(bean, peakPropertyList, projectPropety, param);
            this.DataContext = VM;
        }

        public void ChangeVM(AlignedData alignedData, AlignedDriftSpotPropertyBean bean, ObservableCollection<AnalysisFileBean> files,
            ProjectPropertyBean projectPropety, AnalysisParametersBean param) {
            var peakPropertyList = Model.SetPeakProperty(alignedData, bean, files, param, projectPropety);
            VM = new AlignedChromatogramModificationVM(bean, peakPropertyList, projectPropety, param);
            this.DataContext = VM;
        }

        private void Button_ClearRtAlignment_Click(object sender, RoutedEventArgs e) {
            VM.ClearRtAlignment();
        }

    }

    public class Model
    {
        public static List<PeakProperty> SetPeakProperty(AlignedData alignedData, 
            AlignmentPropertyBean bean, ObservableCollection<AnalysisFileBean> files, 
            AnalysisParametersBean param, ProjectPropertyBean project) {
            var peakPropArr = new PeakProperty[alignedData.NumAnalysisFiles];
            var classnameToBytes = project.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);

            Parallel.For(0, alignedData.NumAnalysisFiles, (i) =>
            {
                //(var i = 0; i < alignedData.NumAnalysisFiles; i++) {
                var brush = classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass];
                var peaks = alignedData.PeakLists[i];
                var peaklist = new List<double[]>();
                foreach (var p in peaks.PeakList)
                {
                    peaklist.Add(new double[] { 0, p[0], 0, p[1] });
                }
                var speaks = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var peakList2 = new List<double[]>();
                foreach (var p in speaks)
                {
                    peakList2.Add(new double[] { p[1], p[3] });
                }
                var peakProp = new PeakProperty(bean.AlignedPeakPropertyBeanCollection[i], alignedData.PeakLists[i], brush, peakList2);
                var offset = bean.AlignedPeakPropertyBeanCollection[i].RetentionTime - bean.CentralRetentionTime;
                peakProp.SetAlignOffSet(offset);
                peakPropArr[i] = peakProp;
            });
            var peakPropList = peakPropArr.ToList();
            peakPropList[0].AverageRt = bean.CentralRetentionTime;
            return peakPropList;
        }

        public static List<PeakProperty> SetPeakProperty(AlignedData alignedData,
            AlignedDriftSpotPropertyBean bean, ObservableCollection<AnalysisFileBean> files,
            AnalysisParametersBean param, ProjectPropertyBean project) {
            var peakPropArr = new PeakProperty[alignedData.NumAnalysisFiles];
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = project.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);


            Parallel.For(0, alignedData.NumAnalysisFiles, (i) =>
            {
                //            for (var i = 0; i < alignedData.NumAnalysisFiles; i++) {
                var brush = classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass];
                var peaks = alignedData.PeakLists[i];
                var peaklist = new List<double[]>();
                foreach (var p in peaks.PeakList)
                {
                    peaklist.Add(new double[] { 0, p[0], 0, p[1] });
                }
                var speaks = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var peakList2 = new List<double[]>();
                foreach (var p in speaks)
                {
                    peakList2.Add(new double[] { p[1], p[3] });
                }
                var peakProp = new PeakProperty(bean.AlignedPeakPropertyBeanCollection[i], alignedData.PeakLists[i], brush, peakList2);
                var offset = bean.AlignedPeakPropertyBeanCollection[i].DriftTime - bean.CentralDriftTime;
                peakProp.SetAlignOffSet(offset);
                peakPropArr[i] = peakProp;
            });
            var peakPropList = peakPropArr.ToList();
            peakPropList[0].AverageRt = bean.CentralDriftTime;
            return peakPropList;
        }

        public static List<PeakProperty> SetPeakProperty(AlignedData alignedData,
            AlignmentPropertyBean bean, ObservableCollection<AnalysisFileBean> files,
            AnalysisParamOfMsdialGcms param, ProjectPropertyBean project)
        {
            var peakPropList = new List<PeakProperty>();
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = project.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);

            if (param.AlignmentIndexType == AlignmentIndexType.RI)
            {
                for (var i = 0; i < alignedData.NumAnalysisFiles; i++)
                {
                    var brush = classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass];
                    var peaks = alignedData.PeakLists[i];
                    var peaklist = new List<double[]>();

                    foreach (var p in peaks.PeakList)
                    {
                        peaklist.Add(new double[] { 0, p[0], 0, p[1] });
                    }
                    var speaks = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                    var peakList2 = new List<double[]>();
                    foreach (var p in speaks)
                    {
                        peakList2.Add(new double[] { p[1], p[3] });
                    }

                    var peakProp = new PeakProperty(bean.AlignedPeakPropertyBeanCollection[i], alignedData.PeakLists[i], brush, peakList2);
                    var offset = bean.AlignedPeakPropertyBeanCollection[i].RetentionIndex - bean.CentralRetentionIndex;
                    peakProp.SetAlignOffSet(offset);
                    peakPropList.Add(peakProp);
                }
                peakPropList[0].AverageRt = bean.CentralRetentionIndex;
                return peakPropList;
            }
            else
            {
                for (var i = 0; i < alignedData.NumAnalysisFiles; i++)
                {
                    var brush = classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass];
                    var peaks = alignedData.PeakLists[i];
                    var peaklist = new List<double[]>();

                    foreach (var p in peaks.PeakList)
                    {
                        peaklist.Add(new double[] { 0, p[0], 0, p[1] });
                    }
                    var speaks = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                    var peakList2 = new List<double[]>();
                    foreach (var p in speaks)
                    {
                        peakList2.Add(new double[] { p[1], p[3] });
                    }

                    var peakProp = new PeakProperty(bean.AlignedPeakPropertyBeanCollection[i], alignedData.PeakLists[i], brush, peakList2);
                    var offset = bean.AlignedPeakPropertyBeanCollection[i].RetentionTime - bean.CentralRetentionTime;
                    peakProp.SetAlignOffSet(offset);
                    peakPropList.Add(peakProp);
                }
                peakPropList[0].AverageRt = bean.CentralRetentionTime;
                return peakPropList;
            }
        }
    }
}
