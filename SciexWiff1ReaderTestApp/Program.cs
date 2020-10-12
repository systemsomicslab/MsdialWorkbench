using CompMs.RawDataHandler.Wiff1Net4;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SciexWiff1ReaderTestApp {
    class Program {
        static Stopwatch sw = new Stopwatch();
        static void Main(string[] args) {


            /*
            var spectrum = new Wiff1ReaderDotNet4().ReadSciexWiff1Data(@"\\mtbdt\Mtb_info\data\MS-DIAL demo files\Raw\HILIC-Pos-SWATH-25Da.wiff", 0, 1, out string errorString);
            foreach (var spec in spectrum) {
                Console.WriteLine($"{spec.Index} {spec.ScanStartTime} {spec.BasePeakMz} {spec.BasePeakIntensity}");
            }
            */
            sw.Start();
            Console.WriteLine($"start: {sw.ElapsedMilliseconds}");
            var spectrum = new Wiff1ReaderDotNet4().ReadSciexWiff1Data(args[0], int.Parse(args[1]), double.Parse(args[2]), out string errorString);
            // var spectrum = new Wiff1ReaderDotNet4().ReadSciexWiff1Data(@"D:\0_Programs\SDK\20141003-RDAM_NET4\20141003-RDAM_NET4\ABFSimpleConverters\ABFCvtSvrABWf_viaMzWiff\bin\Debug\testIn.wiff", 0, 0, out string errorString);
            Console.WriteLine($"parse: {sw.ElapsedMilliseconds / 1_000d} second");
            Console.WriteLine($"error: {errorString}");
            Save(spectrum, errorString, args[3]);
            // Send2(spectrum, errorString);
            // Send3(spectrum, errorString, args[3]);
            Console.WriteLine($"all process: {sw.ElapsedMilliseconds / 1_000d} second");
        }

        static void Send(List<Wiff1RawSpectrum> spectrum, string errorString) {
            using (var stream = new NamedPipeServerStream("Wiff1Reader", PipeDirection.Out, 1)) {

                byte[] buffer;
                IFormatter formatter = new BinaryFormatter();
                using (var memory = new MemoryStream()) {
                    formatter.Serialize(memory, spectrum);
                    Console.WriteLine($"serialize: {sw.ElapsedMilliseconds / 1_000d} second");
                    buffer = memory.ToArray();
                    Console.WriteLine($"to byte array: {sw.ElapsedMilliseconds / 1_000d} second");
                }
                Console.WriteLine($"convert: {sw.ElapsedMilliseconds / 1_000d} second");
                Console.WriteLine($"Size: {buffer.Length / 1_000_000d} MB");

                stream.WaitForConnection();
                Console.WriteLine($"connect: {sw.ElapsedMilliseconds / 1_000d} second");
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(buffer.Length);
                    Console.WriteLine($"send length: {sw.ElapsedMilliseconds / 1_000d} second");
                    writer.Write(buffer);
                    Console.WriteLine($"send content: {sw.ElapsedMilliseconds / 1_000d} second");
                    writer.Write(errorString);
                    Console.WriteLine($"send error: {sw.ElapsedMilliseconds / 1_000d} second");
                }
                Console.WriteLine($"Send: {sw.ElapsedMilliseconds / 1_000d} second");
            }
        }

        static void Send2(List<Wiff1RawSpectrum> spectrum, string errorString) {
            using (var stream = new NamedPipeServerStream("Wiff1Reader", PipeDirection.Out, 1)) {

                stream.WaitForConnection();
                Console.WriteLine($"connect: {sw.ElapsedMilliseconds / 1_000d} second");

                Wiff1RawSpectrumSerializer.Serializer.SerializeAll(stream, spectrum);
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(errorString);
                    Console.WriteLine($"send error: {sw.ElapsedMilliseconds / 1_000d} second");
                }
                Console.WriteLine($"Send: {sw.ElapsedMilliseconds / 1_000d} second");
            }
        }

        static void Send3(List<Wiff1RawSpectrum> spectrum, string errorString, string handle) {
            using (var stream = new AnonymousPipeClientStream(PipeDirection.Out, handle)) {
                Wiff1RawSpectrumSerializer.Serializer.SerializeAll(stream, spectrum);
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(errorString);
                    Console.WriteLine($"send error: {sw.ElapsedMilliseconds / 1_000d} second");
                }
                Console.WriteLine($"Send: {sw.ElapsedMilliseconds / 1_000d} second");
            }
        }

        static void Save(List<Wiff1RawSpectrum> spectrum, string errorString, string dest) {
            using (var stream = File.OpenWrite(dest)) {
                Wiff1RawSpectrumSerializer.Serializer.SerializeAll(stream, spectrum);
                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(errorString);
                    Console.WriteLine($"send error: {sw.ElapsedMilliseconds / 1_000d} second");
                }
                Console.WriteLine($"save: {sw.ElapsedMilliseconds / 1_000d} second");
            }
        }

        static void Save2(List<Wiff1RawSpectrum> spectrum, string errorString, string dest) {
            using (var stream = File.OpenWrite(dest)) {
                byte[] buffer;
                IFormatter formatter = new BinaryFormatter();
                using (var memory = new MemoryStream()) {
                    formatter.Serialize(memory, spectrum);
                    Console.WriteLine($"serialize: {sw.ElapsedMilliseconds / 1_000d} second");
                    buffer = memory.ToArray();
                    Console.WriteLine($"to byte array: {sw.ElapsedMilliseconds / 1_000d} second");
                }
                Console.WriteLine($"convert: {sw.ElapsedMilliseconds / 1_000d} second");
                Console.WriteLine($"Size: {buffer.Length / 1_000_000d} MB");

                using (var writer = new BinaryWriter(stream)) {
                    writer.Write(buffer.Length);
                    Console.WriteLine($"send length: {sw.ElapsedMilliseconds / 1_000d} second");
                    writer.Write(buffer);
                    Console.WriteLine($"send content: {sw.ElapsedMilliseconds / 1_000d} second");
                    writer.Write(errorString);
                    Console.WriteLine($"send error: {sw.ElapsedMilliseconds / 1_000d} second");
                }
                Console.WriteLine($"save: {sw.ElapsedMilliseconds / 1_000d} second");
            }
        }
    }
}
