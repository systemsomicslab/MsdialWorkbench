using CompMs.Common.MessagePack;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Parser {
    public static class MsdialProteomicsSerializer {
        public static void SaveProteinResultContainer(string file, ProteinResultContainer container) {
         MessagePackHandler.SaveToFile<ProteinResultContainer>(container, file);
        }

        public static ProteinResultContainer LoadProteinResultContainer(string file) {
            var obj = MessagePackHandler.LoadFromFile<ProteinResultContainer>(file);
            if (obj is null) {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj.Parameter is null) {
                throw new ArgumentNullException(nameof(obj.Parameter));
            }

            if (obj.ProteinGroups is null) {
                throw new ArgumentNullException(nameof(obj.ProteinGroups));
            }

            if (obj.DB2ModificationContainer is null) {
                throw new ArgumentNullException(nameof(obj.DB2ModificationContainer));
            }

            var db2modContainer = obj.DB2ModificationContainer;
            foreach (var group in obj.ProteinGroups) {
                foreach (var protein in group.ProteinMsResults) {
                    foreach (var pepMsObj in protein.MatchedPeptideResults) {
                        var peptide = pepMsObj.Peptide;
                        var databaseID = pepMsObj.ShotgunProteomicsDatabaseID;
                        peptide.GenerateSequenceObj(protein.FastaProperty.Sequence, peptide.Position.Start, peptide.Position.End,
                            peptide.ResidueCodeIndexToModificationIndex, db2modContainer[databaseID].ID2Code, db2modContainer[databaseID].Code2AminoAcidObj);
                        pepMsObj.PropertyUpdates();
                    }
                    protein.PropertyUpdates();
                }
            }

            return obj;
        }
    }
}
