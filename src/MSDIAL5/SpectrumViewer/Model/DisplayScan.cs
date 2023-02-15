using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;

namespace CompMs.App.SpectrumViewer.Model
{
    public class DisplayScan : BindableBase, IMSScanProperty
    {
        private DisplayScan(IMSScanProperty scan, string name) {
            Scan = scan ?? throw new ArgumentNullException(nameof(scan));
            Name = name;
        }

        public IMSScanProperty Scan { get; }

        public string Name { get; }

        public bool Visible {
            get => visible;
            set => SetProperty(ref visible, value);
        }
        private bool visible = true;

        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        private bool _isSelected = false;

        public int ScanID { get => Scan.ScanID; set => Scan.ScanID = value; }
        public List<SpectrumPeak> Spectrum { get => Scan.Spectrum; set => Scan.Spectrum = value; }
        public ChromXs ChromXs { get => Scan.ChromXs; set => Scan.ChromXs = value; }
        public IonMode IonMode { get => Scan.IonMode; set => Scan.IonMode = value; }
        public double PrecursorMz { get => Scan.PrecursorMz; set => Scan.PrecursorMz = value; }

        public void AddPeak(double mass, double intensity, string comment = null) {
            Scan.AddPeak(mass, intensity, comment);
        }

        public static DisplayScan WrapScan(IMSScanProperty scan) {
            if (scan is DisplayScan ds) {
                return ds;
            }
            switch (scan) {
                case MoleculeMsReference reference:
                    return new DisplayScan(scan, $"{reference.Name} {reference.AdductType}");
                case IMoleculeProperty property:
                    return new DisplayScan(scan, property.Name);
                default:
                    return new DisplayScan(scan, $"Precursor m/z: {scan.PrecursorMz}");
            }
        }

        public static DisplayScan WrapScan(IMSScanProperty scan, string name) {
            if (scan is DisplayScan ds) {
                return new DisplayScan(ds.Scan, name);
            }
            return new DisplayScan(scan, name);
        }
    }
}
