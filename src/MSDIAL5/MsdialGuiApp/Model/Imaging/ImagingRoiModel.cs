using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingRoiModel : DisposableModelBase {
        private readonly RoiAccess _access;

        public ImagingRoiModel(string id, RoiModel roi, RoiModel? wholeRoi, IEnumerable<ChromatogramPeakFeatureModel> peaks, IObservable<ChromatogramPeakFeatureModel?> selectedPeak, RawIntensityOnPixelsLoader intensitiesLoader) {
            Id = id;
            Roi = roi ?? throw new ArgumentNullException(nameof(roi));
            _access = new RoiAccess(roi, wholeRoi);

            RoiPeakSummaries = new ObservableCollection<RoiPeakSummaryModel>(peaks.Select((peak, idx) => new RoiPeakSummaryModel(_access, peak, intensitiesLoader, idx)));
            RoiPeakSummaries.Select(m => selectedPeak.Where(p => m.Peak == p).ToConstant(m)).Merge().Subscribe(m => SelectedRoiPeakSummary = m).AddTo(Disposables);
        }

        public string Id {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        private string _id = string.Empty;
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

        public async Task SavePositionsAsync(CancellationToken token = default) {
            using var stream = File.Open($"{Id}.csv", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var header = string.Join(",", ["XIndex", "YIndex", "Pos"]);
            var encoded = UTF8Encoding.Default.GetBytes(header + "\n");
            await stream.WriteAsync(encoded, 0, encoded.Length).ConfigureAwait(false);
            var builder = new StringBuilder();
            foreach (var info in Roi.Frames.Infos) {
                builder.AppendFormat("{0},{1},{0}_{1}\n", info.XIndexPos, info.YIndexPos);
            }
            var contents = builder.ToString();
            var cEncoded = UTF8Encoding.Default.GetBytes(contents);
            await stream.WriteAsync(cEncoded, 0, cEncoded.Length).ConfigureAwait(false);
        }
    }
}
