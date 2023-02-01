using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.App.MsdialConsole.Parser {
    public class FileParser {
        public void FastaParserTest(string file) {
            var queries = FastaParser.ReadFastaUniProtKB(file);
            foreach (var query in queries) {
                Console.WriteLine("DB={0},UniqueIdentifier={1},EntryName={2},ProteinName={3},OrganismName={4},OrganismIdentifier={5},GeneName={6},ProteinExistence={7},SequenceVersion={8}",
                    query.DB, query.UniqueIdentifier, query.EntryName, query.ProteinName, query.OrganismName, query.OrganismIdentifier, query.GeneName, query.ProteinExistence, query.SequenceVersion);
                Console.WriteLine(query.Sequence);
            }
        }
    }
}
