using CompMs.Common.DataObj.Property;
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

    public static class MoelculePropertyExtension {
        private static readonly IChemObjectBuilder CHEM_OBJECT_BUILDER = CDK.Builder;
        private static readonly SmilesParser SMILES_PARSER = new SmilesParser(CHEM_OBJECT_BUILDER);

        public static IAtomContainer ToAtomContainer(this IMoleculeProperty molecule) {
            if (string.IsNullOrEmpty(molecule.SMILES)) {
                throw new InvalidSmilesException("No information on SMILES.");
            }
            IAtomContainer container = SMILES_PARSER.ParseSmiles(molecule.SMILES);
            if (!ConnectivityChecker.IsConnected(container)) {
                throw new InvalidSmilesException("The connectivity is not correct.");
            }
            return container;
        }

        public static bool IsValidInChIKey(this IMoleculeProperty molecule) {
            return !string.IsNullOrWhiteSpace(molecule.InChIKey) && molecule.InChIKey.Length == 27;
        }
    }
}
