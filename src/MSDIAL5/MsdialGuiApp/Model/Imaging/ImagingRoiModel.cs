using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingRoiModel : DisposableModelBase
    {
        public ImagingRoiModel(string id, RoiModel roi, IEnumerable<ChromatogramPeakFeatureModel> peaks, IObservable<ChromatogramPeakFeatureModel?> selectedPeak, RawIntensityOnPixelsLoader intensitiesLoader) {
            Id = id;
            Roi = roi ?? throw new ArgumentNullException(nameof(roi));

            RoiPeakSummaries = new ObservableCollection<RoiPeakSummaryModel>(peaks.Select((peak, idx) => new RoiPeakSummaryModel(roi, peak, intensitiesLoader, idx)));
            RoiPeakSummaries.Select(m => selectedPeak.Where(p => m.Peak == p).ToConstant(m)).Merge().Subscribe(m => SelectedRoiPeakSummary = m).AddTo(Disposables);
        }

        public string Id { get; }
        public RoiModel Roi { get; }
        public ObservableCollection<RoiPeakSummaryModel> RoiPeakSummaries { get; }

        public RoiPeakSummaryModel? SelectedRoiPeakSummary {
            get => _selectedRoiPeakSummary;
            set => SetProperty(ref _selectedRoiPeakSummary, value);
        }
        private RoiPeakSummaryModel? _selectedRoiPeakSummary;

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected;

        public void Select() {
            IsSelected = true;
        }
    }
}
