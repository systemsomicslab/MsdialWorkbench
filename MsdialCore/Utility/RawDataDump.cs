using CompMs.Common.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Utility {
    public sealed class RawDataDump {
        private RawDataDump() { }

        public static void Dump(string filepath) {
            var allSpectra = DataAccess.GetAllSpectra(filepath);
            foreach (var spec in allSpectra) {
                var precursorMz = spec.Precursor == null ? -1 : spec.Precursor.SelectedIonMz;
                // var specString = Common.Utility.ComponentsConverter.GetSpectrumString(spec.Spectrum);
                //Console.WriteLine("ID={0}, Time={1}, Polarity={2}, MS level={3}, Precursor mz={4}, Spectrum={5}",
                //    spec.ScanNumber, spec.ScanStartTime, spec.ScanPolarity, spec.MsLevel, precursorMz, specString);
                Console.WriteLine("ID={0}, Time={1}, Polarity={2}, MS level={3}, Precursor mz={4}, SpecCount={5}",
                    spec.ScanNumber, spec.ScanStartTime, spec.ScanPolarity, spec.MsLevel, precursorMz, spec.Spectrum.Length);
            }
        }
    }
}
