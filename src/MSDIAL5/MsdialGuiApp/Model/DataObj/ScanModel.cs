using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class ScanModel : BindableBase
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

    }
}
