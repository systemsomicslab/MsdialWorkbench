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
            public IMoleculeProperty Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize) {
                var currentOffset = offset;
                var contentSize = MessagePackBinary.ReadArrayHeader(bytes, currentOffset, out var readTmp);
                currentOffset += readTmp;
                var name = MessagePackBinary.ReadString(bytes, currentOffset, out readTmp);
                currentOffset += readTmp;
                var formula = formatterResolver.GetFormatterWithVerify<Formula>().Deserialize(bytes, currentOffset, formatterResolver, out readTmp);
                currentOffset += readTmp;
                var ontology = MessagePackBinary.ReadString(bytes, currentOffset, out readTmp);
                currentOffset += readTmp;
                var smiles = MessagePackBinary.ReadString(bytes, currentOffset, out readTmp);
                currentOffset += readTmp;
                var inchikey = MessagePackBinary.ReadString(bytes, currentOffset, out readTmp);
                currentOffset += readTmp;
                readSize = currentOffset - offset;
                return new MoleculeProperty(name, formula, ontology, smiles, inchikey);
            }

            public int Serialize(ref byte[] bytes, int offset, IMoleculeProperty value, IFormatterResolver formatterResolver) {
                var currentOffset = offset;
                currentOffset += MessagePackBinary.WriteArrayHeader(ref bytes, currentOffset, 5);
                currentOffset += MessagePackBinary.WriteString(ref bytes, currentOffset, value?.Name);
                currentOffset += formatterResolver.GetFormatterWithVerify<Formula>().Serialize(ref bytes, currentOffset, value?.Formula, formatterResolver);
                currentOffset += MessagePackBinary.WriteString(ref bytes, currentOffset, value?.Ontology);
                currentOffset += MessagePackBinary.WriteString(ref bytes, currentOffset, value?.SMILES);
                currentOffset += MessagePackBinary.WriteString(ref bytes, currentOffset, value?.InChIKey);
                return currentOffset - offset;
            }
        }
    }
}
