using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.SpectrumViewer.Model
{
    public class SpectrumModel : BindableBase
    {
        public SpectrumModel(string name) {
            Name = name;
            displayScans = new ObservableCollection<DisplayScan>();
            DisplayScans = new ReadOnlyObservableCollection<DisplayScan>(displayScans);
            IsLabelVisible = true;
        }

        public SpectrumModel(int serialNumber) : this($"No. {serialNumber}") {

        }

        public string Name { get; }

        public ReadOnlyObservableCollection<DisplayScan> DisplayScans { get; }
        private ObservableCollection<DisplayScan> displayScans;

        public bool IsLabelVisible {
            get => _isLabelVisible;
            set => SetProperty(ref _isLabelVisible, value);
        }
        private bool _isLabelVisible;

        public bool Contains(DisplayScan scan) {
            return displayScans.Contains(scan);
        }

        public void AddScan(IMSScanProperty scan) {
            var scan_ = DisplayScan.WrapScan(scan);
            displayScans.Add(scan_);
        }

        public void RemoveScanIfContains(DisplayScan scan) {
            if (displayScans.Contains(scan)) {
                displayScans.Remove(scan);
            }
        }

        public void RemoveScan(IMSScanProperty scan) {
            if (!(scan is DisplayScan dscan)) {
                dscan = displayScans.FirstOrDefault(ds => ds.Scan == scan);
            }
            if (dscan != null) {
                RemoveScanIfContains(dscan);
            }
        }
    }
}
