using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    internal sealed class AlignmentMassBankRecordExporter : IAlignmentSpectraExporter
    {
        private readonly MassBankRecordHandler _handler;

        public AlignmentMassBankRecordExporter(IonMode ionMode, string instrumentType) {
            var splash = new NSSplash.Splash();
            _handler = new MassBankRecordHandler(ionMode, instrumentType, scan => CreateSplash(scan, splash));
        }

        public string CreateExportFileName(AlignmentSpotProperty spot) {
            return _handler.GetAccession(spot) + ".txt";
        }

        void IAlignmentSpectraExporter.Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult) {
            _handler.WriteRecord(stream, spot, spot, msdecResult, spot);
        }

        private static string CreateSplash(Common.Interfaces.IMSScanProperty scan, NSSplash.Splash splash) {
            return scan.Spectrum.IsEmptyOrNull()
                ? "NA"
                : splash.splashIt(new NSSplash.impl.MSSpectrum(string.Join(" ", scan.Spectrum.Select(p => $"{p.Mass}:{p.Intensity}"))));
        }
    }
}
