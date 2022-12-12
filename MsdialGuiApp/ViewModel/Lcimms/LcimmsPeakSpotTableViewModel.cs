using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    abstract class LcimmsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        protected LcimmsPeakSpotTableViewModel(
            ILcimmsPeakSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<double> dtLower,
            IReactiveProperty<double> dtUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword)
            : base(model, metaboliteFilterKeyword, commentFilterKeyword, ontologyFilterKeyword, adductFilterKeyword) {
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

            DtMin = model.DtMin;
            DtMax = model.DtMax;
            DtLower = dtLower;
            DtUpper = dtUpper;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower { get; }
        public IReactiveProperty<double> MassUpper { get; }

        public double RtMin { get; }
        public double RtMax { get; }
        public IReactiveProperty<double> RtLower { get; }
        public IReactiveProperty<double> RtUpper { get; }

        public double DtMin { get; }
        public double DtMax { get; }
        public IReactiveProperty<double> DtLower { get; }
        public IReactiveProperty<double> DtUpper { get; }
    }

    internal sealed class LcimmsAnalysisPeakTableViewModel : LcimmsPeakSpotTableViewModel
    {
        public LcimmsAnalysisPeakTableViewModel(
            ILcimmsPeakSpotTableModel model,
            IObservable<EicLoader> eicLoader,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<double> dtLower,
            IReactiveProperty<double> dtUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword,
            IReactiveProperty<bool> isEditting)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  dtLower,
                  dtUpper,
                  metaboliteFilterKeyword,
                  commentFilterKeyword,
                  ontologyFilterKeyword,
                  adductFilterKeyword) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsEditting = isEditting ?? throw new ArgumentNullException(nameof(isEditting));
        }
        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }
   
    internal sealed class LcimmsAlignmentSpotTableViewModel : LcimmsPeakSpotTableViewModel
    {
        public LcimmsAlignmentSpotTableViewModel(
            LcimmsAlignmentSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<double> dtLower,
            IReactiveProperty<double> dtUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword,
            IReactiveProperty<bool> isEditting)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  dtLower,
                  dtUpper,
                  metaboliteFilterKeyword,
                  commentFilterKeyword,
                  ontologyFilterKeyword,
                  adductFilterKeyword) {
            BarItemsLoader = model.BarItemsLoader;
            ClassBrush = model.ClassBrush.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsEditting = isEditting ?? throw new ArgumentNullException(nameof(isEditting));
            FileClassPropertiesModel = model.FileClassProperties;
        }

        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public ReadOnlyReactivePropertySlim<IBrushMapper<BarItem>> ClassBrush { get; }
        public FileClassPropertiesModel FileClassPropertiesModel { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }
}
