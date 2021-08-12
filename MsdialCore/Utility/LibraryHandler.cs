using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.Common.Query;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Utility {
    public sealed class LibraryHandler {
        private LibraryHandler() { }

        public static List<MoleculeMsReference> ReadLipidMsLibrary(string filepath, ParameterBase param) {

            var container = param.LipidQueryContainer;
            var collosionType = container.CollisionType;
            var solventType = container.SolventType;

            var ionMode = param.IonMode;
            var queries = new List<LbmQuery>();

            foreach (var lQuery in container.LbmQueries) {
                if (lQuery.IsSelected == true && lQuery.IonMode == ionMode)
                    queries.Add(lQuery);
            }

            List<MoleculeMsReference> mspQueries = null;
            var extension = System.IO.Path.GetExtension(filepath).ToLower();
            if (extension == ".lbm")
                mspQueries = MspFileParser.LbmFileReader(filepath, queries, ionMode, solventType, collosionType);
            else if (extension == ".lbm2")
                mspQueries = MspFileParser.ReadSerializedLbmLibrary(filepath, queries, ionMode, solventType, collosionType);

            return mspQueries;
        }

        public static List<MoleculeMsReference> ReadMspLibrary(string filepath) {
            List<MoleculeMsReference> mspQueries = null;
            var extension = System.IO.Path.GetExtension(filepath).ToLower();
            if (extension.Contains("2"))
                mspQueries = MspFileParser.ReadSerializedMspObject(filepath);
            else
                mspQueries = MspFileParser.MspFileReader(filepath);

            return mspQueries;
        }
        public static List<Peptide> GenerateTargetPeptideReference(List<FastaProperty> quereis,
                List<string> cleavageSites, ModificationContainer modContainer, ProteomicsParameter parameter) {
            var maxMissedCleavage = parameter.MaxMissedCleavage;
            var maxNumberOfModificationsPerPeptide = parameter.MaxNumberOfModificationsPerPeptide;
            var adduct = AdductIonParser.GetAdductIonBean("[M+H]+");
            var minimumPeptideLength = parameter.MinimumPeptideLength;
            var maxPeptideMass = parameter.MaxPeptideMass;
            var char2AA = PeptideCalc.GetSimpleChar2AminoAcidDictionary();
            var syncObj = new object();
            var error = string.Empty;
            var peptides = new List<Peptide>();

            Parallel.ForEach(quereis, fQuery => {
                if (fQuery.IsValidated) {
                    var sequence = fQuery.Sequence;
                    var digestedPeptides = ProteinDigestion.GetDigestedPeptideSequences(sequence, cleavageSites, char2AA, maxMissedCleavage, fQuery.UniqueIdentifier, fQuery.Index);
                    if (!digestedPeptides.IsEmptyOrNull()) {
                        var mPeptides = ModificationUtility.GetModifiedPeptides(digestedPeptides, modContainer, maxNumberOfModificationsPerPeptide);
                        lock (syncObj) {
                            foreach (var peptide in mPeptides) {
                                peptides.Add(peptide);
                            }
                        }
                    }
                }
            });
            return peptides.OrderBy(n => n.ExactMass).ToList();
        }

       
    }
}
