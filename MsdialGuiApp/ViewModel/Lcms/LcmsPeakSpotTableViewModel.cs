using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    abstract class LcmsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        protected LcmsPeakSpotTableViewModel(
            ILcmsPeakSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(model, metaboliteFilterKeyword, commentFilterKeyword) {
            if (massLower is null) {
                throw new ArgumentNullException(nameof(massLower));
            }

            if (massUpper is null) {
                throw new ArgumentNullException(nameof(massUpper));
            }

            if (rtLower is null) {
                throw new ArgumentNullException(nameof(rtLower));
            }

            if (rtUpper is null) {
                throw new ArgumentNullException(nameof(rtUpper));
            }

            MassMin = model.MassMin;
            MassMax = model.MassMax;
            MassLower = massLower;
            MassUpper = massUpper;

            RtMin = model.RtMin;
            RtMax = model.RtMax;
            RtLower = rtLower;
            RtUpper = rtUpper;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower { get; }
        public IReactiveProperty<double> MassUpper { get; }

        public double RtMin { get; }
        public double RtMax { get; }
        public IReactiveProperty<double> RtLower { get; }
        public IReactiveProperty<double> RtUpper { get; }
    }

    sealed class LcmsProteomicsPeakTableViewModel : LcmsPeakSpotTableViewModel {
        public LcmsProteomicsPeakTableViewModel(
            ILcmsPeakSpotTableModel model,
            IObservable<EicLoader> eicLoader,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> proteinFilterKeyword,
            IReactiveProperty<string> peptideFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  peptideFilterKeyword,
                  commentFilterKeyword) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            ProteinFilterKeyword = proteinFilterKeyword;
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public IReactiveProperty<string> PeptideFilterKeyword { get => this.MetaboliteFilterKeyword; }
        public IReactiveProperty<string> ProteinFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }

    sealed class LcmsAnalysisPeakTableViewModel : LcmsPeakSpotTableViewModel
    {
        public LcmsAnalysisPeakTableViewModel(
            ILcmsPeakSpotTableModel model,
            IObservable<EicLoader> eicLoader,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  metaboliteFilterKeyword,
                  commentFilterKeyword) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }
        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }
   
    sealed class LcmsAlignmentSpotTableViewModel : LcmsPeakSpotTableViewModel
    {
        public LcmsAlignmentSpotTableViewModel(
            LcmsAlignmentSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  metaboliteFilterKeyword,
                  commentFilterKeyword) {
        }
    }

    sealed class LcmsProteomicsAlignmentTableViewModel : LcmsPeakSpotTableViewModel {
        public LcmsProteomicsAlignmentTableViewModel(
            LcmsAlignmentSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> proteinFilterKeyword,
            IReactiveProperty<string> peptideFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  peptideFilterKeyword,
                  commentFilterKeyword) {
            ProteinFilterKeyword = proteinFilterKeyword;
        }
        public IReactiveProperty<string> PeptideFilterKeyword { get => this.MetaboliteFilterKeyword; }
        public IReactiveProperty<string> ProteinFilterKeyword { get; }
    }
}
