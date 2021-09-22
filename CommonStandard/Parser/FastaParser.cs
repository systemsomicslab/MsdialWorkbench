using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.Common.Parser {

    public sealed class FastaParser {
        private FastaParser() { }

        public static List<FastaProperty> ReadFastaUniProtKB(string file) {
            var queries = new List<FastaProperty>();
            var counter = 0;
            using (var sr = new StreamReader(file, Encoding.ASCII)) {
                var wkstr = sr.ReadLine();
                var isRecordStarted = wkstr.StartsWith(">");
                while (sr.Peek() > -1) {
                    if (isRecordStarted) {
                        var fastaQuery = new FastaProperty() { Header = wkstr };
                        readUniProtKbHeader(fastaQuery);

                        fastaQuery.Sequence = string.Empty;
                        while (sr.Peek() > -1) {
                            wkstr = sr.ReadLine();
                            if (wkstr.StartsWith(">")) break;
                            fastaQuery.Sequence += wkstr.Trim();
                        }
                        if (fastaQuery.Sequence.Contains("X") || fastaQuery.Sequence.Contains("*") || fastaQuery.Sequence.Contains("-")) {
                            fastaQuery.IsValidated = false;
                        }
                        else {
                            fastaQuery.IsValidated = true;
                        }
                        fastaQuery.Index = counter;
                        queries.Add(fastaQuery);
                        counter++;
                    }
                    else {
                        wkstr = sr.ReadLine();
                        isRecordStarted = wkstr.StartsWith(">");
                    }
                }
            }

            return queries;
        }

        private static void readUniProtKbHeader(FastaProperty query) {

            // example
            // >sp|P31689|DNJA1_HUMAN DnaJ homolog subfamily A member 1 OS=Homo sapiens OX=9606 GN=DNAJA1 PE=1 SV=2

            var header = query.Header;
            var headerArray = header.Split('|');

            query.DB = headerArray[0];
            if (headerArray.Length == 1) return;
            query.UniqueIdentifier = headerArray[1];
            if (headerArray.Length == 2) return;
            query.Description = headerArray[2];
            var descriptions = headerArray[2].Split(' ');
            query.EntryName = descriptions[0];

            var pattern = query.EntryName + @"\s(?<ProteinName>.+?)(\sOS=)(?<OrganismName>.+?)(\sOX=)(?<OrganismIdentifier>.+?)(\sGN=)(?<GeneName>.+?)(\sPE=)(?<ProteinExistence>.+?)(\sSV=)(?<SequenceVersion>.+?)";
            var regexes = Regex.Match(headerArray[2], pattern).Groups;
            query.ProteinName = regexes["ProteinName"].Value;
            query.OrganismName = regexes["OrganismName"].Value;
            query.OrganismIdentifier = regexes["OrganismIdentifier"].Value;
            query.GeneName = regexes["GeneName"].Value;
            query.ProteinExistence = regexes["ProteinExistence"].Value;
            query.SequenceVersion = regexes["SequenceVersion"].Value;
        }

        public static List<FastaProperty> ReadFastaUniProtKB2(string file) {
            var queries = new List<FastaProperty>();
            var counter = 0;
            using (var sr = new StreamReader(file, Encoding.ASCII)) {
                var wkstr = sr.ReadLine();
                var isRecordStarted = wkstr.StartsWith(">");
                while (sr.Peek() > -1) {
                    if (isRecordStarted) {
                        var fastaQuery = readUniProtKbHeader2(wkstr);

                        fastaQuery.Sequence = string.Empty;
                        while (sr.Peek() > -1) {
                            wkstr = sr.ReadLine();
                            if (wkstr.StartsWith(">")) break;
                            fastaQuery.Sequence += wkstr.Trim();
                        }
                        if (fastaQuery.Sequence.Contains("X") || fastaQuery.Sequence.Contains("*") || fastaQuery.Sequence.Contains("-")) {
                            fastaQuery.IsValidated = false;
                        }
                        else {
                            fastaQuery.IsValidated = true;
                        }
                        fastaQuery.Index = counter;
                        queries.Add(fastaQuery);
                        counter++;
                    }
                    else {
                        wkstr = sr.ReadLine();
                        isRecordStarted = wkstr.StartsWith(">");
                    }
                }
            }

            return queries;
        }


        private static FastaProperty readUniProtKbHeader2(string s) {

            // example
            // >sp|P31689|DNJA1_HUMAN DnaJ homolog subfamily A member 1 OS=Homo sapiens OX=9606 GN=DNAJA1 PE=1 SV=2

            var exceptSeparator = Parsers.Satisfy(c => c != '|').Some();
            var exceptSpace = Parsers.Satisfy(c => !char.IsWhiteSpace(c)).Some();
            var exceptEqual = Parsers.Satisfy(c => c != '=').Some();
            var start = Parsers.Char('>');
            var separator = Parsers.Char('|');
            var db = exceptSeparator;
            var identifier = exceptSeparator;
            var entryName = exceptSpace;
            var proteinName = exceptEqual;
            var organismName = exceptEqual;
            var organismIdentifier = exceptEqual;
            var geneName = exceptEqual;
            var proteinExistence = exceptEqual;
            var sequenceVersion = exceptEqual;
            var description = new[]
            {
                entryName,
                proteinName.Token(),
                Parsers.String("OS=").Right(organismName).Token(),
                Parsers.String("OX=").Right(organismIdentifier).Token(),
                Parsers.String("GN=").Right(geneName).Token().Or(Parsers.Return(Enumerable.Empty<char>())),
                Parsers.String("PE=").Right(proteinExistence).Token(),
                Parsers.String("SV=").Right(sequenceVersion).Token(),
            }.Sequence().Select(xs => xs.ToArray());

            var header =
                from db_ in start.Right(db)
                from identifier_ in separator.Right(identifier).Or(Parsers.Return(string.Empty))
                from description_ in separator.Right(Parsers.Line.Back()).Or(Parsers.Return(string.Empty))
                from descriptors in description.Or(Parsers.Return(new IEnumerable<char>[7]))
                select new FastaProperty
                {
                    Header = s,
                    DB = toStr(db_),
                    UniqueIdentifier = toStr(identifier_),
                    Description = toStr(description_),
                    EntryName = toStr(descriptors[0]),
                    ProteinName = toStr(descriptors[1]),
                    OrganismName = toStr(descriptors[2]),
                    OrganismIdentifier = toStr(descriptors[3]),
                    GeneName = toStr(descriptors[4]),
                    ProteinExistence = toStr(descriptors[5]),
                    SequenceVersion = toStr(descriptors[6]),
                };
            var query = header.Parse(s);
            return query;
        }

        private static string toStr(IEnumerable<char> cs) {
            return cs is null ? string.Empty : string.Concat(cs);
        }

        public static List<FastaProperty> ReadFastaUniProtKB3(string file) {
            var queries = new List<FastaProperty>();
            var counter = 0;
            using (var sr = new StreamReader(file, Encoding.ASCII)) {
                queries = readUniProtKbHeader3(sr.ReadToEnd()).ToList();
            }
            foreach (var query in queries) {
                query.Index = counter++;
            }

            return queries;
        }

        private static IEnumerable<FastaProperty> readUniProtKbHeader3(string s) {

            // example
            // >sp|P31689|DNJA1_HUMAN DnaJ homolog subfamily A member 1 OS=Homo sapiens OX=9606 GN=DNAJA1 PE=1 SV=2

            var exceptSeparator = Parsers.Satisfy(c => c != '|').Some();
            var exceptSpace = Parsers.Satisfy(c => !char.IsWhiteSpace(c)).Some();
            var exceptEqual = Parsers.Satisfy(c => c != '=').Some();
            var exceptEqualSpace = Parsers.Satisfy(c => c != '=' && !char.IsWhiteSpace(c)).Some();
            var start = Parsers.Char('>');
            var separator = Parsers.Char('|');
            var db = exceptSeparator;
            var identifier = exceptSeparator;
            var entryName = exceptSpace;
            var proteinName = exceptEqual;
            var organismName = exceptEqual;
            var organismIdentifier = exceptEqualSpace;
            var geneName = exceptEqualSpace;
            var proteinExistence = exceptEqualSpace;
            var sequenceVersion = exceptEqualSpace;
            var description = new[]
            {
                entryName,
                proteinName.Token(),
                Parsers.String("OS=").Right(organismName).Token(),
                Parsers.String("OX=").Right(organismIdentifier).Token(),
                Parsers.String("GN=").Right(geneName).Token().Or(Parsers.Return(Enumerable.Empty<char>())),
                Parsers.String("PE=").Right(proteinExistence).Token(),
                Parsers.String("SV=").Right(sequenceVersion).Token(),
            }.Sequence().Select(xs => xs.ToArray());
            var sequence = Parsers.Satisfy(char.IsLetter).Or(Parsers.Satisfy(c => "*-".Contains(c))).Some().Left(Parsers.LineBreak).Some().Select(xs => string.Concat(xs.SelectMany(ys => ys)));

            var header =
                from db_ in start.Right(db)
                from identifier_ in separator.Right(identifier).Or(Parsers.Return(string.Empty))
                from description_ in separator.Right(Parsers.Line.Back()).Or(Parsers.Return(string.Empty))
                from descriptors in description.Or(Parsers.Return(new string[7]))
                from seq in Parsers.LineBreak.Right(sequence)
                select new FastaProperty
                {
                    Header = s,
                    DB = toStr(db_),
                    UniqueIdentifier = toStr(identifier_),
                    Description = description_,
                    EntryName = toStr(descriptors[0]),
                    ProteinName = toStr(descriptors[1]),
                    OrganismName = toStr(descriptors[2]),
                    OrganismIdentifier = toStr(descriptors[3]),
                    GeneName = toStr(descriptors[4]),
                    ProteinExistence = toStr(descriptors[5]),
                    SequenceVersion = toStr(descriptors[6]),
                    Sequence = seq,
                    IsValidated = !seq.Contains("X") && !seq.Contains("*") && !seq.Contains("-"),
                };
            var query = header.Many().Parse(s);
            return query;
        }
    }
}
