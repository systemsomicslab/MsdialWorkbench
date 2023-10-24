using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.View.PeakCuration
{
    /// <summary>
    /// Interaction logic for AlignedPeakCorrectionWinLegacy.xaml
    /// </summary>
    public partial class AlignedPeakCorrectionWinLegacy : Window {
        public AlignedPeakCorrectionWinLegacy() {
            InitializeComponent();
        }

        private void Button_UpdatePeakInfo_Click(object sender, RoutedEventArgs e) {
            (DataContext as AlignedChromatogramModificationViewModelLegacy)?.UpdatePeakInfo();
        }

        private void Button_ClearRtAlignment_Click(object sender, RoutedEventArgs e) {
            (DataContext as AlignedChromatogramModificationViewModelLegacy)?.ClearRtAlignment();
        }
    }

    public sealed class AlignedChromatogramModificationModelLegacy : BindableBase {
        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> ObservableModel { get; }
        public ReactiveProperty<bool> IsRI { get; }
        public ReactiveProperty<bool> IsDrift { get; }
        public List<PeakChromatogram> Chromatograms { get; }
        public IObservable<List<PeakChromatogram>> ObservableChromatograms { get; }
        public List<AnalysisFileBean> Files { get; }
        public ReadOnlyReactivePropertySlim<PeakPropertiesLegacy> ObservablePeakProperties { get; }

        public AlignedChromatogramModificationModelLegacy(
            IObservable<AlignmentSpotPropertyModel?> model,
            IObservable<List<PeakChromatogram>> chromatoramSource,
            List<AnalysisFileBean> files, 
            ParameterBase parameter) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (chromatoramSource is null) {
                throw new ArgumentNullException(nameof(chromatoramSource));
            }

            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            ObservableModel = model.ToReadOnlyReactivePropertySlim();
            IsRI = model.Select(m => m?.ChromXType == ChromXType.RI).ToReactiveProperty();
            IsDrift = model.Select(m => m?.ChromXType == ChromXType.Drift).ToReactiveProperty();
            ObservableChromatograms = chromatoramSource;
            Files = files;
            ObservablePeakProperties = LoadPeakProperty(model, chromatoramSource, files, parameter).ToReadOnlyReactivePropertySlim();
        }

        public void UpdatePeakInfo() {
            ObservablePeakProperties.Value?.UpdatePeakInfo();
        }

        public void ClearRtAlignment() {
            ObservablePeakProperties.Value?.ClearRtAlignment();
        }
       
        public static IObservable<PeakPropertiesLegacy> LoadPeakProperty(
            IObservable<AlignmentSpotPropertyModel> model,
            IObservable<List<PeakChromatogram>> chromatogramSource,
            List<AnalysisFileBean> files,
            ParameterBase parameter) {
            var classnameToBytes = parameter.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
            return model.Select(spot =>
            {
                var observablePeaks = spot.AlignedPeakPropertiesModelProperty;
                return observablePeaks.CombineLatest(chromatogramSource, (peaks, chromatograms) =>
                {
                    var chromatograms_ = chromatograms ?? Enumerable.Empty<PeakChromatogram>();
                    var peaks_ = peaks ?? Enumerable.Empty<AlignmentChromPeakFeatureModel>();
                    var peakPropArr = files.Zip(peaks_).Where(pair => pair.Item1.AnalysisFileIncluded)
                        .Zip(chromatograms_, (pair, chromatogram) =>
                    {
                        var brush = classnameToBrushes.TryGetValue(pair.Item1.AnalysisFileClass, out var b) ? b : ChartBrushes.GetChartBrush(pair.Item1.AnalysisFileId);
                        var speaks = chromatogram.Convert().Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel);
                        var peakProp = new PeakPropertyLegacy(pair.Item2, brush, speaks);
                        var offset = pair.Item2.ChromXsTop.Value - spot.TimesCenter;
                        peakProp.SetAlignOffSet((float)offset);
                        peakProp.AverageRt = (float)spot.TimesCenter;
                        return peakProp;
                    }).ToArray();
                    return new PeakPropertiesLegacy(spot, peakPropArr);
                });
            }).Switch();
        }
    }
}
