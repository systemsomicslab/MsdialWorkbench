using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
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

namespace CompMs.App.Msdial.View.PeakCuration {
    /// <summary>
    /// Interaction logic for AlignedPeakCorrectionWinLegacy.xaml
    /// </summary>
    public partial class AlignedPeakCorrectionWinLegacy : Window {
        public AlignedChromatogramModificationViewModelLegacy VM { get; set; }
        public AlignedPeakCorrectionWinLegacy() {
            InitializeComponent();
        }



        public AlignedPeakCorrectionWinLegacy(ChromatogramSpotInfo alignedData, AlignmentSpotProperty bean, List<AnalysisFileBean> files, ParameterBase param) {
            InitializeComponent();
            var peakPropertyList = AlignedChromatogramModificationModelLegacy.LoadPeakProperty(alignedData, bean, files, param);
            VM = new AlignedChromatogramModificationViewModelLegacy(bean, peakPropertyList, param);
            this.DataContext = VM;
        }

        private void Button_UpdatePeakInfo_Click(object sender, RoutedEventArgs e) {
            VM.UpdatePeakInfo();
        }

        private void Button_ClearRtAlignment_Click(object sender, RoutedEventArgs e) {
            VM.ClearRtAlignment();
        }
    }

    public class AlignedChromatogramModificationModelLegacy : BindableBase {
        public IObservable<AlignmentSpotPropertyModel> Model { get; }
        public IObservable<List<Chromatogram>> Chromatograms { get; }
        public List<AnalysisFileBean> Files { get; }
        public ParameterBase Parameter { get; }
        public List<PeakPropertyLegacy> PeakProperties { get; }
        public AlignedChromatogramModificationModelLegacy(
            IObservable<AlignmentSpotPropertyModel> model,
            IObservable<List<Chromatogram>> chromatoramSource,
            List<AnalysisFileBean> files, 
            ParameterBase param,
            List<PeakPropertyLegacy> peakProperties) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (chromatoramSource is null) {
                throw new ArgumentNullException(nameof(chromatoramSource));
            }

            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            if (param is null) {
                throw new ArgumentNullException(nameof(param));
            }

            if (peakProperties is null) {
                throw new ArgumentNullException(nameof(peakProperties));
            }

            Model = model;
            Chromatograms = chromatoramSource;
            Files = files;
            Parameter = param;
            PeakProperties = peakProperties;
        }

        public static AlignedChromatogramModificationModelLegacy Create(
            IObservable<AlignmentSpotPropertyModel> model,
            IObservable<List<Chromatogram>> chromatoramSource,
            List<AnalysisFileBean> files,
            ParameterBase param) {
            return null;
        }

        //public static List<PeakPropertyLegacy> LoadPeakProperty(
        //    AlignmentSpotPropertyModel model,
        //    List<Chromatogram> chromatoramSource, 
        //    List<AnalysisFileBean> files, ParameterBase param) {

        //    var peakPropArr = new PeakPropertyLegacy[files.Count];
        //    var classnameToBytes = param.ClassnameToColorBytes;
        //    var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
        //    //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);

        //    Parallel.For(0, files.Count, (i) => {
        //        //(var i = 0; i < alignedData.NumAnalysisFiles; i++) {
        //        var brush = classnameToBrushes[files[i].AnalysisFileClass];
        //        var peaks = chromatoramSource[i].Chromatogram;
        //        var speaks = DataAccess.GetSmoothedPeaklist(peaks, param.SmoothingMethod, param.SmoothingLevel);
        //        var peakProp = new PeakPropertyLegacy(model.AlignedPeakProperties[i], alignedData.PeakInfos[i], brush, speaks);
        //        var offset = bean.AlignedPeakProperties[i].ChromXsTop.Value - bean.TimesCenter.Value;
        //        peakProp.SetAlignOffSet((float)offset);
        //        peakPropArr[i] = peakProp;
        //    });
        //    var peakPropList = peakPropArr.ToList();
        //    peakPropList[0].AverageRt = (float)bean.TimesCenter.Value;
        //    return peakPropList;
        //}

        public static List<PeakPropertyLegacy> LoadPeakProperty(ChromatogramSpotInfo alignedData, AlignmentSpotProperty bean, List<AnalysisFileBean> files, ParameterBase param) {
           
            var peakPropArr = new PeakPropertyLegacy[files.Count];
            var classnameToBytes = param.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);

            Parallel.For(0, files.Count, (i) => {
                //(var i = 0; i < alignedData.NumAnalysisFiles; i++) {
                var brush = classnameToBrushes[files[i].AnalysisFileClass];
                var peaks = alignedData.PeakInfos[i].Chromatogram;
                var speaks = DataAccess.GetSmoothedPeaklist(peaks, param.SmoothingMethod, param.SmoothingLevel);
                var peakProp = new PeakPropertyLegacy(bean.AlignedPeakProperties[i], alignedData.PeakInfos[i], brush, speaks);
                var offset = bean.AlignedPeakProperties[i].ChromXsTop.Value - bean.TimesCenter.Value;
                peakProp.SetAlignOffSet((float)offset);
                peakPropArr[i] = peakProp;
            });
            var peakPropList = peakPropArr.ToList();
            peakPropList[0].AverageRt = (float)bean.TimesCenter.Value;
            return peakPropList;
        }
    }
}
