using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.IO;
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
    }
}
