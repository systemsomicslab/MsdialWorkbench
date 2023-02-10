using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class ParsersTests
    {
        [TestMethod()]
        public void CharTest() {
            var p = Parsers.Char('a');
            var xs = p.Parses("abc");
            CollectionAssert.AreEquivalent(new[] { Unit.Default }, xs.ToArray());

            var ys = p.Parses("bbc");
            CollectionAssert.AreEquivalent(new Unit[] { }, ys.ToArray());
        }

        [TestMethod()]
        public void StringTest() {
            var p = Parsers.String("ab");
            var xs = p.Parses("abc");
            CollectionAssert.AreEquivalent(new[] { Unit.Default }, xs.ToArray());

            var ys = p.Parses("acc");
            CollectionAssert.AreEquivalent(new Unit[] { }, ys.ToArray());

            var zs = p.Parses("cab");
            CollectionAssert.AreEquivalent(new Unit[] { }, zs.ToArray());
        }

        [TestMethod()]
        public void IntegerTest() {
            var p = Parsers.Integer;
            var xs = p.Parses("255");
            CollectionAssert.AreEquivalent(new[] { 2, 25, 255, }, xs.ToArray());

            var ys = p.Parses(" -2");
            CollectionAssert.AreEquivalent(new [] { -2, }, ys.ToArray());

            var zs = p.Parses("273.15");
            CollectionAssert.AreEquivalent(new [] { 2, 27, 273, }, zs.ToArray());
        }

        [TestMethod()]
        public void LineBreakTest() {
            var p = Parsers.LineBreak;
            var xs = p.Parses("\n");
            CollectionAssert.AreEquivalent(new[] { Unit.Default, }, xs.ToArray());

            var ys = p.Parses("\r\n");
            CollectionAssert.AreEquivalent(new [] { Unit.Default, }, ys.ToArray());

            var zs = p.Parses("\r");
            CollectionAssert.AreEquivalent(new [] { Unit.Default, }, zs.ToArray());

            var ws = p.Parses(" ");
            CollectionAssert.AreEquivalent(new Unit[] { }, ws.ToArray());
        }

        [TestMethod()]
        public void LineTest() {
            var p = Parsers.Line;
            var xs = p.Parses("abc\n");
            CollectionAssert.AreEquivalent(new[] { "abc", }, xs.ToArray());

            var ys = p.Parses("abc\r\ndef");
            CollectionAssert.AreEquivalent(new [] { "abc", }, ys.ToArray());

            var zs = p.Parses("\r");
            CollectionAssert.AreEquivalent(new [] { string.Empty }, zs.ToArray());

            var ws = p.Parses(" ");
            CollectionAssert.AreEquivalent(new [] { " " }, ws.ToArray());

            var us = p.Left(Parsers.LineBreak).Some().Select(ls => string.Concat(ls)).Parses("abc\r\ndef\r\n");
            CollectionAssert.AreEquivalent(new[] { "abcdef", "abc" }, us.ToArray());

            var vs = p.SomeWith(Parsers.LineBreak).Select(ls => string.Concat(ls)).Parses("abc\r\ndef");
            CollectionAssert.AreEquivalent(new[] { "abcdef", "abc" }, vs.ToArray());
        }


        [TestMethod()]
        public void OrTest() {
            var p = Parsers.String("ab").Or(Parsers.Integer.Right(Parsers.ReturnUnit));
            var xs = p.Parses("abc");
            CollectionAssert.AreEquivalent(new[] { Unit.Default }, xs.ToArray());

            var ys = p.Parses("12a");
            CollectionAssert.AreEquivalent(new[] { Unit.Default, Unit.Default, }, ys.ToArray());

            var zs = p.Parses("a12b");
            CollectionAssert.AreEquivalent(new Unit[] { }, zs.ToArray());
        }

        [TestMethod()]
        public void ManyTest() {
            var p = Parsers.String("ab");
            var xs = p.Many().Select(a => a.Count()).Parses("abababc");
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, xs.ToArray());

            var ys = p.Many().Select(a => a.Count()).Parses("cab");
            CollectionAssert.AreEquivalent(new[] { 0 }, ys.ToArray());
        }

        [TestMethod()]
        public void SomeTest() {
            var p = Parsers.String("ab");
            var xs = p.Some().Select(a => a.Count()).Parses("abababc");
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, xs.ToArray());

            var ys = p.Some().Select(a => a.Count()).Parses("cab");
            CollectionAssert.AreEquivalent(new int[] { }, ys.ToArray());
        }

        [TestMethod()]
        public void ManyWithTest() {
            var p = Parsers.Integer.ManyWith(Parsers.Char(','));
            var xs = p.Parses("12,34,56").ToArray();
            Assert.AreEqual(7, xs.Length);
            CollectionAssert.AreEquivalent(new[] { 12, 34, 56 }, xs.First().ToArray());

            var ys = p.Parses(",12,34,56").ToArray();
            Assert.AreEqual(1, ys.Length);
            CollectionAssert.AreEquivalent(new int[] { }, ys.First().ToArray());
        }

        [TestMethod()]
        public void SomeWithTest() {
            var p = Parsers.Integer.SomeWith(Parsers.Char(','));
            var xs = p.Parses("12,34,56").ToArray();
            Assert.AreEqual(6, xs.Length);
            CollectionAssert.AreEquivalent(new[] { 12, 34, 56 }, xs.First().ToArray());

            var ys = p.Parses(",12,34,56").ToArray();
            Assert.AreEqual(0, ys.Length);
        }

        [TestMethod()]
        public void ConsumeAllTest() {
            var p = Parsers.Integer;
            var xs = p.Parses("123").ToArray();
            Assert.AreEqual(3, xs.Length);
            CollectionAssert.AreEquivalent(new[] { 123, 12, 1 }, xs);

            var ys = p.ConsumeAll().Parses("123").ToArray();
            Assert.AreEqual(1, ys.Length);
            CollectionAssert.AreEquivalent(new[] { 123, }, ys);
        }

        [TestMethod()]
        public void ProtTest() {
            var s = ">sp|P31689|DNJA1_HUMAN DnaJ homolog subfamily A member 1 OS=Homo sapiens OX=9606 GN=DNAJA1 PE=1 SV=2";
            // var s = ">sp|P31689";
            // var s = ">sp";

            var letterOrDigit = Parsers.Satisfy(char.IsLetterOrDigit).Some().Select(xs => new string(xs.ToArray()));
            var letterOrDigitOrUnderScore = Parsers.Satisfy(char.IsLetterOrDigit).Or(Parsers.Satisfy(c => c == '_')).Some().Select(xs => new string(xs.ToArray()));
            var letterOrDigitOrSpace = Parsers.Satisfy(char.IsLetterOrDigit).Or(Parsers.Satisfy(char.IsWhiteSpace)).Some().Select(xs => new string(xs.ToArray()));
            var start = Parsers.Char('>');
            var separator = Parsers.Char('|');
            var db = letterOrDigit;
            var identifier = letterOrDigit;
            var entryName = letterOrDigitOrUnderScore;
            var proteinName = letterOrDigitOrSpace;
            var organismName = letterOrDigitOrSpace;
            var organismIdentifier = letterOrDigitOrSpace;
            var geneName = letterOrDigitOrSpace;
            var proteinExistence = letterOrDigitOrSpace;
            var sequenceVersion = letterOrDigitOrSpace;
            var description = new[]
            {
                entryName,
                proteinName,
                Parsers.String("OS=").Right(organismName),
                Parsers.String("OX=").Right(organismIdentifier),
                Parsers.String("GN=").Right(geneName),
                Parsers.String("PE=").Right(proteinExistence),
                Parsers.String("SV=").Right(sequenceVersion),
            }.SequenceWith(Parsers.Space).Select(xs => xs.ToArray());

            var header = start.Right(db.SelectMany(
                db_ => separator.Right(identifier.SelectMany(
                    identifier_ => separator.Right(Parsers.Line.Back().SelectMany(
                        d => description,
                        (d, description_) => new Components.FastaProperty
                            {
                                Description = d,
                                EntryName = description_[0],
                                ProteinName = description_[1],
                                OrganismName = description_[2],
                                OrganismIdentifier = description_[3],
                                GeneName = description_[4],
                                ProteinExistence = description_[5],
                                SequenceVersion = description_[6],
                            }))
                            .OrDefault(new Components.FastaProperty()),
                    (identifier_, q) => { q.UniqueIdentifier = identifier_; return q; }))
                        .OrDefault(new Components.FastaProperty()),
                (db_, q) => { q.DB = db_; return q; }));

            var query = header.Parse(s);
            Console.WriteLine(query.Description);
            Console.WriteLine("DB={0},UniqueIdentifier={1},EntryName={2},ProteinName={3},OrganismName={4},OrganismIdentifier={5},GeneName={6},ProteinExistence={7},SequenceVersion={8}",
                query.DB, query.UniqueIdentifier, query.EntryName, query.ProteinName, query.OrganismName, query.OrganismIdentifier, query.GeneName, query.ProteinExistence, query.SequenceVersion);

            var xs_ =
                from db_ in start.Right(db)
                from identifier_ in separator.Right(identifier).Or(Parsers.Return(string.Empty))
                from description_ in separator.Right(Parsers.Line.Back()).Or(Parsers.Return(string.Empty))
                from descriptors in description.Or(Parsers.Return(new string[7]))
                select new Components.FastaProperty
                {
                    DB = db_,
                    UniqueIdentifier = identifier_,
                    Description = description_,
                    EntryName = descriptors[0],
                    ProteinName = descriptors[1],
                    OrganismName = descriptors[2],
                    OrganismIdentifier = descriptors[3],
                    GeneName = descriptors[4],
                    ProteinExistence = descriptors[5],
                    SequenceVersion = descriptors[6],
                };
            var x = xs_.Parse(s);
            Console.WriteLine(x.Description);
            Console.WriteLine("DB={0},UniqueIdentifier={1},EntryName={2},ProteinName={3},OrganismName={4},OrganismIdentifier={5},GeneName={6},ProteinExistence={7},SequenceVersion={8}",
                x.DB, x.UniqueIdentifier, x.EntryName, x.ProteinName, x.OrganismName, x.OrganismIdentifier, x.GeneName, x.ProteinExistence, x.SequenceVersion);

        }
    }
}