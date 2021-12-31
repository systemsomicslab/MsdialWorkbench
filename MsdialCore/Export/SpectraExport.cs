using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public class SpectraExport
    {
        public static void SaveSpectraTable(
            ExportSpectraFileFormat spectraFormat, string exportFilePath,
            ChromatogramPeakFeature chromPeakFeature, IMSScanProperty scan,
            ParameterBase parameter)
        {
            if (spectraFormat == ExportSpectraFileFormat.msp)
            {
                using (var file = File.Open(exportFilePath, FileMode.Create)) {
                    SaveSpectraTable(spectraFormat, file, chromPeakFeature, scan, parameter);
                }
            }
        }

        public static void SaveSpectraTable(
            ExportSpectraFileFormat spectraFormat, Stream exportStream,
            ChromatogramPeakFeature chromPeakFeature, IMSScanProperty scan,
            ParameterBase parameter)
        {
            if (spectraFormat == ExportSpectraFileFormat.msp)
            {
                SaveSpectraTableAsNistFormat(exportStream, chromPeakFeature, scan.Spectrum, parameter);
            }
        }

        public static void SaveSpectraTableAsNistFormat(
            Stream stream,
            ChromatogramPeakFeature chromPeakFeature, IEnumerable<ISpectrumPeak> massSpectra,
            ParameterBase parameter)
        {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true))
            {
                WriteChromPeakFeatureInfoAsNist(sw, chromPeakFeature);
                WriteParameterInfoAsNist(sw, parameter);
                WriteSpectrumPeakInfoAsNist(sw, massSpectra);
                sw.WriteLine();
            }
        }

        public static void SaveSpectraTable(
            ExportSpectraFileFormat spectraFormat, string exportFilePath,
            AlignmentSpotProperty spotProperty, IMSScanProperty scan,
            ParameterBase parameter)
        {
            if (spectraFormat == ExportSpectraFileFormat.msp)
            {
                using (var file = File.Open(exportFilePath, FileMode.Create)) {
                    SaveSpectraTable(spectraFormat, file, spotProperty, scan, parameter);
                }
            }
        }

        public static void SaveSpectraTable(
            ExportSpectraFileFormat spectraFormat, Stream exportStream,
            AlignmentSpotProperty spotProperty, IMSScanProperty scan,
            ParameterBase parameter)
        {
            if (spectraFormat == ExportSpectraFileFormat.msp)
            {
                SaveSpectraTableAsNistFormat(exportStream, spotProperty, scan.Spectrum, parameter);
            }
        }

        public static void SaveSpectraTableAsNistFormat(
            Stream stream,
            AlignmentSpotProperty spotProperty, IEnumerable<ISpectrumPeak> massSpectra,
            ParameterBase parameter)
        {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.ASCII, 4096, true))
            {
                WriteAlignmentSpotPropertyInfoAsNist(sw, spotProperty);
                WriteParameterInfoAsNist(sw, parameter);
                WriteSpectrumPeakInfoAsNist(sw, massSpectra);
                sw.WriteLine();
            }
        }

        private static void WriteChromPeakFeatureInfoAsNist(StreamWriter sw, ChromatogramPeakFeature chromPeakFeature)
        {
            sw.Write("NAME: ");
            if (chromPeakFeature.Name == string.Empty)
                sw.WriteLine("Unknown");
            else
                sw.WriteLine(chromPeakFeature.Name);
            sw.WriteLine("SCANNUMBER: " + chromPeakFeature.ChromScanIdTop);
            sw.WriteLine("RETENTIONTIME: " + chromPeakFeature.ChromXsTop.RT.Value);
            sw.WriteLine("PRECURSORMZ: " + chromPeakFeature.PrecursorMz);
            sw.WriteLine("PRECURSORTYPE: " + chromPeakFeature.AdductType.AdductIonName);
            sw.WriteLine("IONMODE: " + chromPeakFeature.IonMode);
            sw.WriteLine("INTENSITY: " + chromPeakFeature.PeakHeightTop);
            sw.WriteLine("ISOTOPE: " + "M + " + chromPeakFeature.PeakCharacter.IsotopeWeightNumber.ToString());
            sw.WriteLine("INCHIKEY: " + chromPeakFeature.InChIKey);
            sw.WriteLine("SMILES: " + chromPeakFeature.SMILES);
            sw.WriteLine("FORMULA: " + chromPeakFeature.Formula);
        }

        private static void WriteAlignmentSpotPropertyInfoAsNist(StreamWriter sw, AlignmentSpotProperty spotProperty)
        {
            sw.Write("NAME: ");
            if (spotProperty.Name == string.Empty)
                sw.WriteLine("Unknown");
            else
                sw.WriteLine(spotProperty.Name);
            sw.WriteLine("RETENTIONTIME: " + spotProperty.TimesCenter.RT.Value);
            sw.WriteLine("PRECURSORMZ: " + spotProperty.MassCenter);
            sw.WriteLine("PRECURSORTYPE: " + spotProperty.AdductType.AdductIonName);
            sw.WriteLine("IONMODE: " + spotProperty.IonMode);
            sw.WriteLine("ISOTOPE: " + "M + " + spotProperty.PeakCharacter.IsotopeWeightNumber.ToString());
            sw.WriteLine("INCHIKEY: " + spotProperty.InChIKey);
            sw.WriteLine("SMILES: " + spotProperty.SMILES);
            sw.WriteLine("FORMULA: " + spotProperty.Formula);
        }

        private static void WriteParameterInfoAsNist(StreamWriter sw, ParameterBase parameter)
        {
            if (!string.IsNullOrEmpty(parameter.Authors)) {
                sw.WriteLine("AUTHORS: " + parameter.Authors);
            }

            if (!string.IsNullOrEmpty(parameter.License)) {
                sw.WriteLine("LICENSE: " + parameter.License);
            }

            if (!string.IsNullOrEmpty(parameter.CollisionEnergy)) {
                sw.WriteLine("COLLISIONENERGY: " + parameter.CollisionEnergy);
            }

            if (!string.IsNullOrEmpty(parameter.InstrumentType)) {
                sw.WriteLine("INSTRUMENTTYPE: " + parameter.InstrumentType);
            }

            if (!string.IsNullOrEmpty(parameter.Instrument)) {
                sw.WriteLine("INSTRUMENT: " + parameter.Instrument);
            }

            if (!string.IsNullOrEmpty(parameter.Comment)) {
                sw.WriteLine("COMMENT: " + parameter.Comment);
            }
        }

        private static void WriteSpectrumPeakInfoAsNist(StreamWriter sw, IEnumerable<ISpectrumPeak> massSpectra)
        {
            if (!massSpectra.IsEmptyOrNull())
            {
                var peaks = massSpectra.Where(spec => spec.Intensity > 0).ToList();
                sw.WriteLine("Num Peaks: " + peaks.Count);
                foreach (var peak in peaks)
                {
                    sw.WriteLine(Math.Round(peak.Mass, 5) + "\t" + Math.Round(peak.Intensity, 0));
                }
            }
        }
    }
}
