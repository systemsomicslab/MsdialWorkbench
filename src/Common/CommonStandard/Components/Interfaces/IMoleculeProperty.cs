using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using MessagePack;
using MessagePack.Formatters;
using NCDK;
using NCDK.Graphs;
using NCDK.Smiles;

namespace CompMs.Common.Interfaces
{
    public interface IMoleculeProperty
    {
        string Name { get; set; }
        Formula Formula { get; set; }
        string Ontology { get; set; }
        string SMILES { get; set; }
        string InChIKey { get; set; }
    }

    public static class MoleculePropertyExtension {
        private static readonly IChemObjectBuilder CHEM_OBJECT_BUILDER = CDK.Builder;
        private static readonly SmilesParser SMILES_PARSER = new SmilesParser(CHEM_OBJECT_BUILDER);

        public static IAtomContainer ToAtomContainer(this IMoleculeProperty molecule) {
            if (string.IsNullOrEmpty(molecule.SMILES)) {
                throw new InvalidSmilesException("No information on SMILES.");
            }
            IAtomContainer container = SMILES_PARSER.ParseSmiles(molecule.SMILES);
            if (container is null || !ConnectivityChecker.IsConnected(container)) {
                throw new InvalidSmilesException("The connectivity is not correct.");
            }
            return container;
        }

        public static bool IsValidInChIKey(this IMoleculeProperty molecule) {
            return !string.IsNullOrWhiteSpace(molecule.InChIKey) && molecule.InChIKey.Length == 27;
        }

        public static IMoleculeProperty AsPutative(this IMoleculeProperty molecule) {
            return new MoleculeProperty($"Putative: {molecule.Name}", molecule.Formula, molecule.Ontology, molecule.SMILES, molecule.InChIKey);
        }

        public static readonly IMessagePackFormatter<IMoleculeProperty> Formatter = new MoleculePropertyFormatter();

        class MoleculePropertyFormatter : IMessagePackFormatter<IMoleculeProperty>
        {
            public IMoleculeProperty Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                var count = reader.ReadArrayHeader();
                if (count != 5) {
                    throw new MessagePackSerializationException($"Unexpected array length for {nameof(IMoleculeProperty)}: {count}.");
                }
                var name = reader.ReadString();
                var formula = options.Resolver.GetFormatterWithVerify<Formula>().Deserialize(ref reader, options);
                var ontology = reader.ReadString();
                var smiles = reader.ReadString();
                var inchikey = reader.ReadString();
                return new MoleculeProperty(name, formula, ontology, smiles, inchikey);
            }

            public void Serialize(ref MessagePackWriter writer, IMoleculeProperty value, MessagePackSerializerOptions options) {
                writer.WriteArrayHeader(5);
                writer.Write(value?.Name);
                options.Resolver.GetFormatterWithVerify<Formula>().Serialize(ref writer, value?.Formula, options);
                writer.Write(value?.Ontology);
                writer.Write(value?.SMILES);
                writer.Write(value?.InChIKey);
            }
        }
    }
}
