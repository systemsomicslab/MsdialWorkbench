using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ShotgunProteomicsDB {
        [Key(0)]
        public string FastaFile { get; set; }
        [Key(1)]
        public string Id { get; set; }
        [Key(2)]
        public DataBaseSource DataBaseSource { get; set; }

        [Key(3)]
        public List<FastaProperty> FastaQueries { get; set; }
        [IgnoreMember]
        public List<Peptide> Peptides { get; set; }

        [Key(4)]
        public List<FastaProperty> DecoyQueries { get; set; }
        [IgnoreMember]
        public List<Peptide> DecoyPeptides { get; set; }

        [Key(5)]
        public ModificationContainer ModificationContainer { get; set; }
        [Key(6)]
        public List<string> CleavageSites { get; set; }
        [Key(7)]
        public ProteomicsParameter Parameter { get; set; }

        public void LoadNewFile(string file, string id, ProteomicsParameter param) {
            FastaFile = file;
            Id = id;
            Parameter = param;
            DataBaseSource = DataBaseSource.Fasta;

            FastaQueries = FastaParser.ReadFastaUniProtKB(file);
            DecoyQueries = DecoyCreator.Convert2DecoyQueries(FastaQueries);

            CleavageSites = ProteinDigestion.GetCleavageSites(param.EnzymesForDigestion);
            ModificationContainer = ModificationUtility.GetModificationContainer(param.FixedModifications, param.VariableModifications);

            Peptides = LibraryHandler.GenerateTargetPeptideReference(FastaQueries, CleavageSites, ModificationContainer, Parameter);
            DecoyPeptides = LibraryHandler.GenerateTargetPeptideReference(DecoyQueries, CleavageSites, ModificationContainer, Parameter);
        }
    }
}
