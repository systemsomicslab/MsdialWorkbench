using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class Ms2ChromatogramsModel : DisposableModelBase
    {
        private static readonly int NUMBER_OF_CHROMATOGRAMS = 10;
        private static readonly ReadOnlyCollection<Pen> DECONVOLUTION_PENS = ChartBrushes.GetSolidColorPenList(1d, DashStyles.Solid);
        private static readonly ReadOnlyCollection<Pen> RAW_PENS = ChartBrushes.GetSolidColorPenList(1d, DashStyles.Dash);

        public Ms2ChromatogramsModel(IObservable<ChromatogramPeakFeatureModel> peak, IObservable<MSDecResult> msScan, IMsSpectrumLoader<ChromatogramPeakFeatureModel> loader, IDataProvider provider, ParameterBase parameter) {
            NumberOfChromatograms = new ReactiveProperty<int>(NUMBER_OF_CHROMATOGRAMS).AddTo(Disposables);

            var rawChromatograms = peak.Where(t => !(t is null))
                .Select(t => loader.LoadSpectrumAsObservable(t).CombineLatest(NumberOfChromatograms, (spectrum, number) => (t, spectrum: spectrum.OrderByDescending(peak_ => peak_.Intensity).Take(number))))
                .Switch()
                .Select(pair => DataAccess.GetMs2ValuePeaks(provider, pair.t.Mass, pair.t.MS1RawSpectrumIdLeft, pair.t.MS1RawSpectrumIdRight, pair.spectrum.Select(peak_ => (double)peak_.Mass).ToArray(), parameter))
                .Select(chromatograms => new ChromatogramsModel(
                    "Raw MS/MS chromatogram",
                    chromatograms.Zip(RAW_PENS, (chromatogram, pen) => new DisplayChromatogram(chromatogram.Select(peak_ => peak_.ConvertToChromatogramPeak(ChromXType.RT, ChromXUnit.Min)).ToList(), linePen: pen, title: chromatogram.FirstOrDefault().Mz.ToString("F5"))).ToList(), // TODO: [magic number] ChromXType, ChromXUnit
                    "Raw MS/MS chromatogram",
                    "Retention time [min]", // TODO: [magic number] Retention time 
                    "Abundance"));
            var deconvolutedChromatograms = msScan.Where(t => !(t is null))
                .CombineLatest(NumberOfChromatograms, (result, number) => result.DecChromPeaks(number))
                .Select(chromatograms => new ChromatogramsModel(
                    "Deconvoluted MS/MS chromatogram",
                    chromatograms.Zip(DECONVOLUTION_PENS, (chromatogram, pen) => new DisplayChromatogram(chromatogram, linePen: pen, title: chromatogram.FirstOrDefault()?.Mass.ToString("F5") ?? "NA")).ToList(),
                    "Deconvoluted MS/MS chromatogram",
                    "Retention time [min]", // TODO: [magic number] Retention time 
                    "Abundance"));
            var bothChromatograms = deconvolutedChromatograms.CombineLatest(rawChromatograms, (dec, raw) => dec.Merge(raw));

            Loader = loader as MultiMsRawSpectrumLoader;

            var isSwath = parameter.AcquisitionType == CompMs.Common.Enum.AcquisitionType.SWATH ||
                parameter.AcquisitionType == CompMs.Common.Enum.AcquisitionType.AIF;
            IsRawSelected = new ReactivePropertySlim<bool>(!isSwath).AddTo(Disposables);
            IsDeconvolutedSelected = new ReactivePropertySlim<bool>(isSwath).AddTo(Disposables);
            IsBothSelected = new ReactivePropertySlim<bool>(false).AddTo(Disposables);

            IsRawEnabled = Observable.Return(true).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsDeconvolutedEnabled = Observable.Return(isSwath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsBothEnabled = Observable.Return(isSwath).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ChromatogramsModel = new[]
            {
                IsRawSelected.Where(x => x).Select(_ => rawChromatograms),
                IsDeconvolutedSelected.Where(x => x).Select(_ => deconvolutedChromatograms),
                IsBothSelected.Where(x => x).Select(_ => bothChromatograms),
            }.Merge()
            .Switch()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ChromatogramsModel> ChromatogramsModel { get; }

        public ReactivePropertySlim<bool> IsRawSelected { get; }
        public ReactivePropertySlim<bool> IsDeconvolutedSelected { get; }
        public ReactivePropertySlim<bool> IsBothSelected { get; }

        public ReadOnlyReactivePropertySlim<bool> IsRawEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsDeconvolutedEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsBothEnabled { get; }

        public ReactiveProperty<int> NumberOfChromatograms { get; }

        public MultiMsRawSpectrumLoader Loader { get; }
    }
}
