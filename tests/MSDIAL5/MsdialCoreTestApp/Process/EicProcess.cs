using CompMs.Common.Extension;
using CompMs.Common.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process
{
    public sealed class EicProcess
    {
        public int Run(string[] args) {
            if (args.Length < 2) {
                return argsError();
            }

            var subcommand = args[1];
            if (subcommand == "raw") {
                return RunRaw(args);
            }

            Console.Error.WriteLine($"Invalid eic subcommand: {subcommand}");
            return -1;
        }

        private int RunRaw(string[] args) {
            var inputFile = string.Empty;
            var outputFile = string.Empty;
            var targetMz = double.NaN;
            var mzTolerance = 0.01d;

            for (var i = 2; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) {
                    inputFile = args[i + 1];
                }
                else if (args[i] == "-o" && i + 1 < args.Length) {
                    outputFile = args[i + 1];
                }
                else if (args[i] == "-mz" && i + 1 < args.Length) {
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out targetMz)) {
                        return argsError();
                    }
                }
                else if (args[i] == "-tolerance" && i + 1 < args.Length) {
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out mzTolerance)) {
                        return argsError();
                    }
                }
            }

            if (inputFile.IsEmptyOrNull() || outputFile.IsEmptyOrNull() || double.IsNaN(targetMz)) {
                return argsError();
            }

            var spectra = LoadMeasurement(inputFile);
            if (spectra.Count == 0) {
                Console.Error.WriteLine("No raw spectra were found.");
                return -1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFile)) ?? ".");
            using var writer = new StreamWriter(outputFile, false, Encoding.ASCII);
            writer.WriteLine("ScanId,RT,Intensity");

            foreach (var spectrum in spectra.Where(n => n.MsLevel == 1)) {
                var intensity = spectrum.Spectrum
                    .Where(peak => Math.Abs(peak.Mz - targetMz) <= mzTolerance)
                    .Sum(peak => peak.Intensity);
                writer.WriteLine(string.Join(",", spectrum.ScanNumber, spectrum.ScanStartTime.ToString(CultureInfo.InvariantCulture), intensity.ToString(CultureInfo.InvariantCulture)));
            }

            return 0;
        }

        private static IReadOnlyList<RawSpectrum> LoadMeasurement(string inputFile) {
            using var access = new RawDataAccess(inputFile, 0, false, false, false);
            var measurement = access.GetMeasurement();
            return measurement?.SpectrumList ?? new List<RawSpectrum>();
        }

        private static int argsError() {
            Console.Error.WriteLine("MsdialConsoleApp.exe eic raw -i <input file> -o <output csv> -mz <target m/z> [-tolerance <mz tolerance>]");
            return -1;
        }
    }
}
