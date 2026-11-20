using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Compiled and executed separately against current and historical assemblies.
// No historical DTOs or readers are reimplemented in this harness.
internal static class Program
{
    private static void Main(string[] args) {
        if (args.Length < 2) { throw new ArgumentException("Expected: write prefix | verify prefix [resave-prefix]"); }
        Console.WriteLine("MessagePack: " + typeof(MessagePackSerializer).Assembly.FullName);
        if (args[0] == "write") {
            if (typeof(MoleculeMsReference).GetMethod("TryCompactSpectrum") == null) {
                throw new Exception("The writer must contain the compact-spectrum implementation.");
            }
            Write(args[1], Records());
        }
        else if (args[0] == "verify") {
            var expected = MessagePackSerializer.Deserialize<MoleculeMsReference[]>(File.ReadAllBytes(args[1] + ".expected"));
            List<MoleculeMsReference> sidecar;
            using (var stream = File.OpenRead(args[1] + ".msp2")) { sidecar = MoleculeMsRefMethods.LoadMspFromStream(stream); }
            Equal(expected, sidecar);
            DataBaseStorage storage;
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Metabolomics };
            using (var stream = File.OpenRead(args[1] + ".dbs")) {
                storage = DataBaseStorage.Load(stream, new StandardLoadAnnotatorVisitor(parameter),
                    new StandardAnnotationQueryFactoryGenerationVisitor(parameter.PeakPickBaseParam, parameter.RefSpecMatchBaseParam), "");
            }
            var database = storage.MetabolomicsDataBases.Single().DataBase;
            Equal(expected, database.Database.ToList());
            if (args.Length == 3) { Write(args[2], database.Database.ToList()); }
            Console.WriteLine("PASS: .dbs and .msp2; " + expected.Length + " records; all serialized fields and double bits match.");
        }
        else { throw new ArgumentException("Unknown command."); }
    }

    private static void Write(string prefix, IList<MoleculeMsReference> records) {
        // Write the expected representation before compaction. A reader compares it with
        // the optimized output using its own serializer, independently of writer version.
        File.WriteAllBytes(prefix + ".expected", MessagePackSerializer.Serialize(records.ToArray()));
        var compact = typeof(MoleculeMsReference).GetMethod("TryCompactSpectrum");
        if (compact != null) {
            foreach (var record in records) { compact.Invoke(record, null); }
            if (!(bool)typeof(MoleculeMsReference).GetProperty("IsSpectrumCompact").GetValue(records[0])) {
                throw new Exception("The numeric compatibility record was not compacted.");
            }
        }
        var database = new MoleculeDataBase(records, "Compatibility", DataBaseSource.Msp, SourceType.MspDB, "synthetic.msp");
        var storage = DataBaseStorage.CreateEmpty();
        storage.AddMoleculeDataBase(database, new List<IAnnotatorParameterPair<MoleculeDataBase>>());
        using (var stream = File.Create(prefix + ".dbs")) { storage.Save(stream); }
        using (var stream = File.Create(prefix + ".msp2")) { new Sidecar { MspDB = records.ToList() }.Write(stream); }
    }

    private static void Equal(IList<MoleculeMsReference> expected, IList<MoleculeMsReference> actual) {
        if (expected.Count != actual.Count) { throw new Exception("Record count mismatch."); }
        for (var i = 0; i < expected.Count; i++) {
            // Access Spectrum deliberately: this also verifies that compact data expands
            // into the historical public mutable model with the same default values.
            var a = expected[i].Spectrum;
            var b = actual[i].Spectrum;
            if ((a == null) != (b == null) || (a != null && a.Count != b.Count)) { throw new Exception("Peak count mismatch."); }
            if (a != null) {
                for (var j = 0; j < a.Count; j++) {
                    if (BitConverter.DoubleToInt64Bits(a[j].Mass) != BitConverter.DoubleToInt64Bits(b[j].Mass)
                        || BitConverter.DoubleToInt64Bits(a[j].Intensity) != BitConverter.DoubleToInt64Bits(b[j].Intensity)) {
                        throw new Exception("Numeric bits differ at " + i + ":" + j);
                    }
                }
            }
            if (!MessagePackSerializer.Serialize(expected[i]).SequenceEqual(MessagePackSerializer.Serialize(actual[i]))) {
                throw new Exception("Serialized fields differ in record " + i);
            }
        }
    }

    private static List<MoleculeMsReference> Records() {
        var numbers = new List<double> { 0, 127, 128, 255, 256, 65535, 65536, -1, -33, -129, -32769,
            int.MinValue, uint.MaxValue, (double)uint.MaxValue + 1, 73.25, 73.123456789,
            double.Epsilon, float.Epsilon, float.MaxValue, double.MaxValue, double.MinValue,
            double.PositiveInfinity, double.NegativeInfinity, BitConverter.Int64BitsToDouble(long.MinValue),
            BitConverter.Int64BitsToDouble(0x7ff8000000001234) };
        var random = new Random(924);
        var bytes = new byte[8];
        for (var i = 0; i < 10000; i++) { random.NextBytes(bytes); numbers.Add(BitConverter.Int64BitsToDouble(BitConverter.ToInt64(bytes, 0))); }
        var records = new List<MoleculeMsReference> {
            new MoleculeMsReference { Name = "Exact numbers", SMILES = "C[C@H](O)C(=O)O", InChIKey = "UNCHANGED",
                Formula = new Formula(3, 6, 0, 3, 0, 0, 0, 0, 0, 0, 0), AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Comment = "Reference metadata", Ontology = "Hydroxy acid",
                Spectrum = numbers.Select((n, i) => new SpectrumPeak(n, numbers[numbers.Count - i - 1])).ToList() },
            new MoleculeMsReference { Name = "Null", Spectrum = null },
            new MoleculeMsReference { Name = "Empty" },
        };
        for (var field = 0; field < 7; field++) {
            var peak = new SpectrumPeak(73.123456789, 456.987654321);
            switch (field) {
                case 0: peak.Comment = ""; break;
                case 1: peak.Comment = "diagnostic"; break;
                case 2: peak.PeakQuality = (PeakQuality)1; break;
                case 3: peak.PeakID = 42; break;
                case 4: peak.SpectrumComment = SpectrumComment.doublebond; break;
                case 5: peak.IsAbsolutelyRequiredFragmentForAnnotation = true; break;
                case 6: peak.FragmentationScore = BitConverter.Int64BitsToDouble(long.MinValue); break;
            }
            records.Add(new MoleculeMsReference { Name = "Annotated " + field, Spectrum = new List<SpectrumPeak> { peak } });
        }
        return records;
    }

    private sealed class Sidecar : MsdialDataStorageBase
    {
        public void Write(Stream stream) => SaveMspDB(stream);
    }
}
