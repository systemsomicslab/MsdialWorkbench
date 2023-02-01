using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class CommandLineParserTests
    {
        [TestMethod()]
        public void ParseTest() {
            var args = new string[]
            {
                "1",
                "2",
                "--bool", "true",
                "--byte", "1",
                "--sbyte", "12",
                "--int", "123",
                "--uint", "1234",
                "--long", "12345",
                "--ulong", "123456",
                "--float", "1.23",
                "--double", "123.456",
                "--char", "c",
                "--string", "ss",
                "-i", "1.2",
                "--array", "1.1", "2.2", "3.3", "4.4",
            };
            var result = CommandLineParser.Parse<DataType>(args);
            Assert.AreEqual(1, result.Position0);
            Assert.AreEqual(2, result.Position1);
            Assert.AreEqual(true, result.Bool);
            Assert.AreEqual(1, result.Byte);
            Assert.AreEqual(12, result.Sbyte);
            Assert.AreEqual(123, result.Int);
            Assert.AreEqual(1234u, result.Uint);
            Assert.AreEqual(12345l, result.Long);
            Assert.AreEqual(123456ul, result.Ulong);
            Assert.AreEqual(1.23f, result.Float);
            Assert.AreEqual(123.456d, result.Double);
            Assert.AreEqual('c', result.Char);
            Assert.AreEqual("ss", result.String);
            Assert.AreEqual(1.2f, result.Short);
            CollectionAssert.AreEqual(new[] { 1.1f, 2.2f, 3.3f, 4.4f }, result.Array);
        }
    }

    internal class DataType {
        [PositionArgument(1)]
        public int Position1 { get; set; }
        [PositionArgument(0)]
        public int Position0 { get; set; }

        [ShortStyleArgument("-i")]
        public float Short { get; set; }


        [LongStyleArgument("--bool")]
        public bool Bool { get; set; }
        [LongStyleArgument("--byte")]
        public byte Byte { get; set; }
        [LongStyleArgument("--sbyte")]
        public sbyte Sbyte { get; set; }
        [LongStyleArgument("--int")]
        public int Int { get; set; }
        [LongStyleArgument("--uint")]
        public uint Uint { get; set; }
        [LongStyleArgument("--long")]
        public long Long { get; set; }
        [LongStyleArgument("--ulong")]
        public ulong Ulong { get; set; }
        [LongStyleArgument("--float")]
        public float Float { get; set; }
        [LongStyleArgument("--double")]
        public double Double { get; set; }
        [LongStyleArgument("--char")]
        public char Char { get; set; }
        [LongStyleArgument("--string")]
        public string String { get; set; }

        [LongStyleArgument("--array", length: 4)]
        public float[] Array { get; set; }
    }
}