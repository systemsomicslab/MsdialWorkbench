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
        }

        public SpectrumModel(int serialNumber) : this($"No. {serialNumber}") {

        }

        public string Name { get; }

        public ReadOnlyObservableCollection<DisplayScan> DisplayScans { get; }
        private ObservableCollection<DisplayScan> displayScans;

        public void AddScan(IMSScanProperty scan) {
            displayScans.Add(scan as DisplayScan ?? new DisplayScan(scan));
        }

        public void RemoveScan(DisplayScan scan) {
            displayScans.Remove(scan);
        }

        public void RemoveScan(IMSScanProperty scan) {
            if (!(scan is DisplayScan dscan)) {
                dscan = displayScans.FirstOrDefault(ds => ds.Scan == scan);
            }
            if (dscan != null) {
                RemoveScan(dscan);
            }
        }
    }
}
