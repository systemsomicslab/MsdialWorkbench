using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public void FastaParserTest2(string file) {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var queries = FastaParser.ReadFastaUniProtKB(file);
            sw.Stop();
            Console.WriteLine($"Regex: {sw.ElapsedMilliseconds}");
            Console.WriteLine($"Count: {queries.Count}");
            foreach (var query in queries.TakeLast(5)) {
                Console.WriteLine("DB={0},UniqueIdentifier={1},EntryName={2},ProteinName={3},OrganismName={4},OrganismIdentifier={5},GeneName={6},ProteinExistence={7},SequenceVersion={8}",
                    query.DB, query.UniqueIdentifier, query.EntryName, query.ProteinName, query.OrganismName, query.OrganismIdentifier, query.GeneName, query.ProteinExistence, query.SequenceVersion);
                Console.WriteLine(query.Description);
                Console.WriteLine(query.Sequence);
            }

            sw.Restart();
            queries = FastaParser.ReadFastaUniProtKB2(file);
            sw.Stop();
            Console.WriteLine($"Combinator: {sw.ElapsedMilliseconds}");
            Console.WriteLine($"Count: {queries.Count}");
            foreach (var query in queries.TakeLast(5)) {
                Console.WriteLine("DB={0},UniqueIdentifier={1},EntryName={2},ProteinName={3},OrganismName={4},OrganismIdentifier={5},GeneName={6},ProteinExistence={7},SequenceVersion={8}",
                    query.DB, query.UniqueIdentifier, query.EntryName, query.ProteinName, query.OrganismName, query.OrganismIdentifier, query.GeneName, query.ProteinExistence, query.SequenceVersion);
                Console.WriteLine(query.Description);
                Console.WriteLine(query.Sequence);
            }

            sw.Restart();
            queries = FastaParser.ReadFastaUniProtKB3(file);
            sw.Stop();
            Console.WriteLine($"Combinator2: {sw.ElapsedMilliseconds}");
            Console.WriteLine($"Count: {queries.Count}");
            foreach (var query in queries.TakeLast(5)) {
                Console.WriteLine("DB={0},UniqueIdentifier={1},EntryName={2},ProteinName={3},OrganismName={4},OrganismIdentifier={5},GeneName={6},ProteinExistence={7},SequenceVersion={8}",
                    query.DB, query.UniqueIdentifier, query.EntryName, query.ProteinName, query.OrganismName, query.OrganismIdentifier, query.GeneName, query.ProteinExistence, query.SequenceVersion);
                Console.WriteLine(query.Description);
                Console.WriteLine(query.Sequence);
            }
        }
    }
}
