using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.SpectrumViewer.Model
{
    public class SpectrumModel : BindableBase
    {
        public SpectrumModel(int serialNumber) {
            Name = $"No. {serialNumber}";
            displayScans = new ObservableCollection<DisplayScan>();
            DisplayScans = new ReadOnlyObservableCollection<DisplayScan>(displayScans);
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
    }
}
