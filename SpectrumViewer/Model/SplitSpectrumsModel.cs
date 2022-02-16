using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.SpectrumViewer.Model
{
    public class SplitSpectrumsModel : BindableBase
    {
        public SplitSpectrumsModel(string name) {
            Name = name;
            displayScans = new ObservableCollection<DisplayScan>();
            DisplayScans = new ReadOnlyObservableCollection<DisplayScan>(displayScans);
            UpperSpectrumModel = new SpectrumModel(name + "_upper");
            LowerSpectrumModel = new SpectrumModel(name + "_lower");

            (var add, var remove) = Common.ObservableExtension.Calm(
                new[]
                {
                    UpperSpectrumModel.DisplayScans.ToObservable(),
                    UpperSpectrumModel.DisplayScans.ObserveAddChanged(),
                    LowerSpectrumModel.DisplayScans.ToObservable(),
                    LowerSpectrumModel.DisplayScans.ObserveAddChanged(),
                }.Merge(),
                new[]
                {
                    UpperSpectrumModel.DisplayScans.ObserveRemoveChanged(),
                    LowerSpectrumModel.DisplayScans.ObserveRemoveChanged(),
                }.Merge(),
                TimeSpan.FromMilliseconds(100));

            add.Subscribe(InternalAdd);
            remove.Subscribe(InternalRemove);
        }

        public SplitSpectrumsModel(int serialNumber) : this($"No. {serialNumber}") {

        }

        public string Name { get; }

        public SpectrumModel UpperSpectrumModel { get; }
        public SpectrumModel LowerSpectrumModel { get; }

        public ReadOnlyObservableCollection<DisplayScan> DisplayScans { get; }
        private ObservableCollection<DisplayScan> displayScans;

        object objlock = new object();
        private void InternalAdd(DisplayScan scan) {
            lock (objlock) {
                displayScans.Add(scan);
            }
        }

        private void InternalRemove(DisplayScan scan) {
            lock (objlock) {
                displayScans.Remove(scan);
            }
        }


        public void MoveToLower(DisplayScan scan) {
            MoveTo(scan, UpperSpectrumModel, LowerSpectrumModel);
        }

        public void MoveToUpper(DisplayScan scan) {
            MoveTo(scan, LowerSpectrumModel, UpperSpectrumModel);
        }

        private void MoveTo(DisplayScan scan, SpectrumModel from, SpectrumModel to) {
            if (scan == null || !from.Contains(scan) || to.Contains(scan)) {
                throw new ArgumentException(nameof(scan));
            }
            from.RemoveScan(scan);
            to.AddScan(scan);
        }

        public void AddScan(IMSScanProperty scan) {
            UpperSpectrumModel.AddScan(scan);
        }

        public void RemoveScan(DisplayScan scan) {
            UpperSpectrumModel.RemoveScanIfContains(scan);
            LowerSpectrumModel.RemoveScanIfContains(scan);
        }

        public void RemoveScan(IMSScanProperty scan) {
            UpperSpectrumModel.RemoveScan(scan);
            LowerSpectrumModel.RemoveScan(scan);
        }
    }
}
