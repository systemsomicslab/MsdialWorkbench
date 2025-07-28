using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.Raw.Abstractions;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class Ms2ChromatogramsModel : DisposableModelBase
    {
        private static readonly int NUMBER_OF_CHROMATOGRAMS = 10;
        private static readonly ReadOnlyCollection<Pen> DECONVOLUTION_PENS = ChartBrushes.GetSolidColorPenList(1d, DashStyles.Solid);
        private static readonly ReadOnlyCollection<Pen> RAW_PENS = ChartBrushes.GetSolidColorPenList(1d, DashStyles.Dash);
        private readonly IMessageBroker _broker;

        public Ms2ChromatogramsModel(IObservable<ChromatogramPeakFeatureModel?> peak, IObservable<MSDecResult?> msScan, IMsSpectrumLoader<ChromatogramPeakFeatureModel> loader, IDataProvider provider, ParameterBase parameter, AcquisitionType acquisitionType, IMessageBroker broker) {
            NumberOfChromatograms = new ReactiveProperty<int>(NUMBER_OF_CHROMATOGRAMS).AddTo(Disposables);
            ProductIonSelectingModel = new ProductIonSelectingModel().AddTo(Disposables);

            var smoother = new Smoothing();
            IObservable<(ChromatogramPeakFeatureModel peak, double[] mzs)> intensityTopIons = peak.SkipNull()
                .Do(ProductIonSelectingModel.SelectPeak)
                .SelectSwitch(p => {
                    UseUserSelectedIons = false;
                    var loadSpectrum = Observable.Defer(() => {
                        return loader.LoadScanAsObservable(p).Select(scan => {
                            var spectrum = (scan?.Spectrum ?? []);
                            var ordered = spectrum.OrderByDescending(peak_ => peak_.Intensity);
                            return ordered.Select(peak_ => peak_.Mass).ToArray();
                        });
                    });
                    return this.ObserveProperty(m => m.UseUserSelectedIons).Publish(ox => {
                        return Observable.Merge([
                            ox.Where(b => !b).Select(_ => loadSpectrum.CombineLatest(NumberOfChromatograms, (mzs, number) => mzs.Take(number).OrderBy(mz => mz).ToArray())),
                            ox.Where(b => b).Select(_ => ProductIonSelectingModel.GetRequiredProductIonsAsObservable()),
                        ]);
                    }).Switch().Select(mzs => (peak: p, mzs: mzs));
                });
            var rawChromatograms = intensityTopIons
                .CombineLatest(ProductIonSelectingModel.GetRtRangeAsObservable(), (pair, range) => {
                    var type = ChromXType.RT; // TODO: [magic number] ChromXType, ChromXUnit
                    var unit = ChromXUnit.Min;
                    return Observable.FromAsync(async token => {
                        try {
                            var chromatograms = await DataAccess.GetMs2ValuePeaksAsync(provider, pair.peak.Mass, range.Left, range.Right, pair.mzs, parameter, acquisitionType, type: type, unit: unit).ConfigureAwait(false);
                            var smootheds = chromatograms.Select(c => smoother.LinearWeightedMovingAverage(c, parameter.SmoothingLevel)).ToList();
                            return smootheds.Zip(pair.mzs, (smoothed, mz) => new ExtractedIonChromatogram(smoothed, ChromXType.RT, ChromXUnit.Min, extractedMz: mz)).ToArray();
                        }
                        catch (OperationCanceledException) {
                            return [];
                        }
                    });
                }).Switch()
                .Select(chromatograms => new ChromatogramsModel(
                    "Raw MS/MS chromatogram",
                    chromatograms.Zip(RAW_PENS, (chromatogram, pen) => new DisplayChromatogram(chromatogram, linePen: pen, name: chromatogram.ExtractedMz.ToString("F5"))).ToList(),
                    "Raw MS/MS chromatogram",
                    "Retention time [min]", // TODO: [magic number] Retention time 
                    "Abundance"));
            var deconvolutedChromatograms = msScan.SkipNull()
                .CombineLatest(NumberOfChromatograms, (result, number) => result.DecChromPeaks(number))
                .Select(chromatograms => new ChromatogramsModel(
                    "Deconvoluted MS/MS chromatogram",
                    chromatograms.Zip(DECONVOLUTION_PENS, (chromatogram, pen) => new DisplayChromatogram(chromatogram, linePen: pen, name: chromatogram.ExtractedMz.ToString("F5") ?? "NA")).ToList(),
                    "Deconvoluted MS/MS chromatogram",
                    "Retention time [min]", // TODO: [magic number] Retention time 
                    "Abundance"));
            var bothChromatograms = deconvolutedChromatograms.CombineLatest(rawChromatograms, (dec, raw) => dec.Merge(raw));

            Loader = loader as MultiMsmsRawSpectrumLoader;

            var isSwath = MsmsAcquisition.GetOrDefault(acquisitionType).IsDia;
            IsRawSelected = new ReactivePropertySlim<bool>(!isSwath).AddTo(Disposables);
            IsDeconvolutedSelected = new ReactivePropertySlim<bool>(isSwath).AddTo(Disposables);
            IsBothSelected = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            IsRawEnabled = Observable.Return(true).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsDeconvolutedEnabled = Observable.Return(isSwath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsBothEnabled = Observable.Return(isSwath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ChromatogramsModel = new[]
            {
                IsRawSelected.Where(x => x).ToConstant(rawChromatograms),
                IsDeconvolutedSelected.Where(x => x).ToConstant(deconvolutedChromatograms),
                IsBothSelected.Where(x => x).ToConstant(bothChromatograms),
            }.Merge()
            .Switch()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            _broker = broker;
        }

        public ReadOnlyReactivePropertySlim<ChromatogramsModel?> ChromatogramsModel { get; }

        public ReactivePropertySlim<bool> IsRawSelected { get; }
        public ReactivePropertySlim<bool> IsDeconvolutedSelected { get; }
        public ReactivePropertySlim<bool> IsBothSelected { get; }

        public ReadOnlyReactivePropertySlim<bool> IsRawEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsDeconvolutedEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsBothEnabled { get; }

        public ReactiveProperty<int> NumberOfChromatograms { get; }

        public ProductIonSelectingModel ProductIonSelectingModel { get; }

        public bool UseUserSelectedIons {
            get => _useUserSelectedIons;
            set => SetProperty(ref _useUserSelectedIons, value);
        }
        private bool _useUserSelectedIons = false;

        public MultiMsmsRawSpectrumLoader? Loader { get; }

        public void CopyAsTable() {
            if (!(ChromatogramsModel.Value is ChromatogramsModel chromatograms)) {
                return;
            }
            using (var stream = new MemoryStream()) {
                chromatograms.ExportAsync(stream, "\t").Wait();
                Clipboard.SetDataObject(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        public async Task SaveAsTableAsync() {
            if (!(ChromatogramsModel.Value is ChromatogramsModel chromatograms)) {
                return;
            }
            var fileName = string.Empty;
            var request = new SaveFileNameRequest(name => fileName = name)
            {
                AddExtension = true,
                Filter = "tab separated values|*.txt",
                RestoreDirectory = true,
            };
            _broker.Publish(request);
            if (request.Result == true) {
                using (var stream = File.Open(fileName, FileMode.Create)) {
                    await chromatograms.ExportAsync(stream, "\t").ConfigureAwait(false);
                }
            }
        }
    }
}
