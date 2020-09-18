using CompMs.RawDataHandler.Wiff1Net4;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SciexWiff1ReaderTestApp {
    class Program {
        static void Main(string[] args) {
            /*
            var spectrum = new Wiff1ReaderDotNet4().ReadSciexWiff1Data(@"\\mtbdt\Mtb_info\data\MS-DIAL demo files\Raw\HILIC-Pos-SWATH-25Da.wiff", 0, 1, out string errorString);
            foreach (var spec in spectrum) {
                Console.WriteLine($"{spec.Index} {spec.ScanStartTime} {spec.BasePeakMz} {spec.BasePeakIntensity}");
            }
            */
            var spectrum = new Wiff1ReaderDotNet4().ReadSciexWiff1Data(args[0], int.Parse(args[1]), double.Parse(args[2]), out string errorString);
            Send(spectrum, errorString);
        }

        static void Send(List<Wiff1RawSpectrum> spectrum, string errorString) {
            using (var stream = new NamedPipeServerStream("Wiff1Reader", PipeDirection.Out, 1)) {

                byte[] buffer;
                IFormatter formatter = new BinaryFormatter();
                using (var memory = new MemoryStream()) {
                    formatter.Serialize(memory, spectrum);
                    buffer = memory.ToArray();
                }

                stream.WaitForConnection();
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(buffer.Length);
                    writer.Write(buffer);
                    writer.Write(errorString);
                }
            }
        }
    }
}
