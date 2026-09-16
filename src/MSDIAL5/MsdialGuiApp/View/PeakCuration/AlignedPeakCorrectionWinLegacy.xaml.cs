using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

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

    internal sealed class AlignedChromatogramModificationModelLegacy : DisposableModelBase {
        public ReactiveProperty<bool> IsRI { get; }
        public ReactiveProperty<bool> IsDrift { get; }
        public List<PeakChromatogram>? Chromatograms { get; }
        public List<AnalysisFileBean> Files { get; }
        public ReadOnlyReactivePropertySlim<PeakPropertiesLegacy?> ObservablePeakProperties { get; }

        public AlignedChromatogramModificationModelLegacy(IObservable<AlignedChromatograms?> spotChromatograms, List<AnalysisFileBean> files, ParameterBase parameter, FilePropertiesModel filePropertiesModel) {
            if (files is null) {
                throw new ArgumentNullException(nameof(files));
            }

            IsRI = spotChromatograms.Select(s => s?.Spot.ChromXType == ChromXType.RI).ToReactiveProperty().AddTo(Disposables);
            IsDrift = spotChromatograms.Select(s => s?.Spot.ChromXType == ChromXType.Drift).ToReactiveProperty().AddTo(Disposables);
            Files = files;
            ObservablePeakProperties = LoadPeakProperty(spotChromatograms, files, parameter, filePropertiesModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public void UpdatePeakInfo() {
            ObservablePeakProperties.Value?.UpdatePeakInfo();
        }

        public void ClearRtAlignment() {
            ObservablePeakProperties.Value?.ClearRtAlignment();
        }
       
        public static IObservable<PeakPropertiesLegacy?> LoadPeakProperty(IObservable<AlignedChromatograms?> spotChromatograms, List<AnalysisFileBean> files, ParameterBase parameter, FilePropertiesModel filePropertiesModel) {
            var classnameToBytes = parameter.ClassnameToColorBytes;
            var classnameToBrushes = ChartBrushes.ConvertToSolidBrushDictionary(classnameToBytes);
            var handlers = (parameter as MsdialGcmsParameter)?.GetRIHandlers();
            var cls2brsh = filePropertiesModel.ClassProperties.ToDictionary(
                p => p.Name,
                p => p.ObserveProperty(p_ => p_.Color).Select(c => {
                    var b = new SolidColorBrush(c);
                    b.Freeze();
                    return b;
                }).Replay(1).RefCount());
            var fileProps = files.Select(f => {
                if (!cls2brsh.TryGetValue(f.AnalysisFileClass, out var brush)) {
                    brush = Observable.Return(ChartBrushes.GetChartBrush(f.AnalysisFileId));
                }
                var handler = (handlers?.TryGetValue(f.AnalysisFileId, out var h) ?? false) ? h : null;
                return new FileProp(f, brush, handler);
            }).ToList();
            return spotChromatograms.DefaultIfNull(s => s.Chromatograms.CombineLatest(s.Spot.AlignedPeakPropertiesModelProperty, (chromatograms, peaks) => {
                if (peaks is null) {
                    return Observable.Return<PeakPropertiesLegacy?>(null);
                }
                var peakPropArr = fileProps.ZipInternal(peaks).Where(pair => pair.Item1.File.AnalysisFileIncluded)
                    .Zip(chromatograms, (pair, chromatogram) => {
                        using var smoothed = chromatogram.Convert().ChromatogramSmoothing(parameter.SmoothingMethod, parameter.SmoothingLevel);
                        var offset = pair.Item2.ChromXsTop.Value - s.Spot.TimesCenter;
                        var speaks = smoothed.AsPeakArray();
                        return pair.Item1.Brush.Select(b => {
                            var peakProp = new PeakPropertyLegacy(pair.Item2, b, speaks, pair.Item1.Handler);
                            peakProp.SetAlignOffSet(offset);
                            peakProp.AverageRt = s.Spot.TimesCenter;
                            return peakProp;
                        });
                    }).CombineLatest();
                return peakPropArr.Select(arr => new PeakPropertiesLegacy(s.Spot, arr.ToArray()));
            }).Switch().Prepend(null), Observable.Return<PeakPropertiesLegacy?>(null)).Switch();
        }
    }

    class FileProp(AnalysisFileBean file, IObservable<SolidColorBrush> brush, RetentionIndexHandler? handler)
    {
        public AnalysisFileBean File { get; } = file;
        public IObservable<SolidColorBrush> Brush { get; } = brush;
        public RetentionIndexHandler? Handler { get; } = handler;
    }
}
