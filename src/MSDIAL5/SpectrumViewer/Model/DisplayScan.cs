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
        private DisplayScan(IMSScanProperty scan) {
            Scan = scan ?? throw new ArgumentNullException(nameof(scan));
            switch (scan) {
                case MoleculeMsReference reference:
                    Name = $"{reference.Name} {reference.AdductType}";
                    break;
                case IMoleculeProperty property:
                    Name = property.Name;
                    break;
                default:
                    Name = $"Precursor m/z: {scan.PrecursorMz}";
                    break;
            }
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
            return scan as DisplayScan ?? new DisplayScan(scan);
        }
    }
}
