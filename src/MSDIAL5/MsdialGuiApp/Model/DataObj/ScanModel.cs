using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class ScanModel : BindableBase, IMSScanProperty
    {
        private readonly IMSScanProperty _scan;

        public ScanModel(IMSScanProperty scan) {
            _scan = scan;
            Spectrum = _scan.Spectrum.AsReadOnly();
        }

        public int ScanID => _scan.ScanID;
        public ReadOnlyCollection<SpectrumPeak> Spectrum { get; }
        public ChromXs ChromXs => _scan.ChromXs;
        public IonMode IonMode => _scan.IonMode;
        public double PrecursorMz => _scan.PrecursorMz;

        int IMSScanProperty.ScanID { get => _scan.ScanID; set => _scan.ScanID = value; }
        List<SpectrumPeak> IMSScanProperty.Spectrum { get => _scan.Spectrum; set => _scan.Spectrum = value; }
        ChromXs IMSProperty.ChromXs { get => _scan.ChromXs; set => _scan.ChromXs = value; }
        IonMode IMSProperty.IonMode { get => _scan.IonMode; set => _scan.IonMode = value; }
        double IMSProperty.PrecursorMz { get => _scan.PrecursorMz; set => _scan.PrecursorMz = value; }

        void IMSScanProperty.AddPeak(double mass, double intensity, string comment) {
            _scan.AddPeak(mass, intensity, comment);
        }
    }
}
