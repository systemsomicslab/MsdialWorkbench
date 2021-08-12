using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ShotgunProteomicsDB : IDisposable {
        [Key(10)]
        private bool disposedValue;

        [Key(0)]
        public string FastaFile { get; set; }
        [Key(1)]
        public string Id { get; set; }
        [Key(2)]
        public DataBaseSource DataBaseSource { get; set; }

        [Key(3)]
        public List<FastaProperty> FastaQueries { get; set; }
        [IgnoreMember]
        public List<PeptideMsReference> PeptideMsRef { get; set; }

        [Key(4)]
        public List<FastaProperty> DecoyQueries { get; set; }
        [IgnoreMember]
        public List<PeptideMsReference> DecoyPeptideMsRef { get; set; }

        [Key(5)]
        public ModificationContainer ModificationContainer { get; set; }
        [Key(6)]
        public List<string> CleavageSites { get; set; }
        [Key(7)]
        public ProteomicsParameter ProteomicsParameter { get; set; }
        [Key(8)]
        public MsRefSearchParameterBase MsRefSearchParameter { get; set; }
        [Key(9)]
        public string PeptideMsFile { get; set; }
        [IgnoreMember]
        public Stream PeptideMsStream { get; set; }
        [Key(10)]
        public string DecoyMsFile { get; set; }
        [IgnoreMember]
        public Stream DecoyMsStream { get; set; }

        public void LoadNewFile(string file, string id, ProteomicsParameter proteomicsParam, MsRefSearchParameterBase msrefSearchParam) {
            FastaFile = file;
            Id = id;
            ProteomicsParameter = proteomicsParam;
            MsRefSearchParameter = msrefSearchParam;
            DataBaseSource = DataBaseSource.Fasta;

            FastaQueries = FastaParser.ReadFastaUniProtKB(file);
            DecoyQueries = DecoyCreator.Convert2DecoyQueries(FastaQueries);

            CleavageSites = ProteinDigestion.GetCleavageSites(proteomicsParam.EnzymesForDigestion);
            ModificationContainer = ModificationUtility.GetModificationContainer(proteomicsParam.FixedModifications, proteomicsParam.VariableModifications);


            var peptides = LibraryHandler.GenerateTargetPeptideReference(FastaQueries, CleavageSites, ModificationContainer, ProteomicsParameter);
            var decoyPeptides = LibraryHandler.GenerateTargetPeptideReference(DecoyQueries, CleavageSites, ModificationContainer, ProteomicsParameter);

            var dt = DateTime.Now;
            var refid = "_" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();
            var filename = System.IO.Path.GetFileNameWithoutExtension(file);

            PeptideMsFile = filename + refid + "." + MsdialDataStorageFormat.msf;
            DecoyMsFile = "decoy_" + filename + refid + "." + MsdialDataStorageFormat.msf;

            Stream pFS = null, dFS = null;
            Parallel.Invoke(
                () => MsfFileParser.GeneratePeptideMsObjcts(PeptideMsFile, peptides, msrefSearchParam.MassRangeBegin, msrefSearchParam.MassRangeEnd, out pFS),
                () => MsfFileParser.GeneratePeptideMsObjcts(DecoyMsFile, decoyPeptides, msrefSearchParam.MassRangeBegin, msrefSearchParam.MassRangeEnd, out dFS)
                );
            PeptideMsStream = pFS;
            DecoyMsStream = dFS;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (PeptideMsStream != null)
                        PeptideMsStream.Close();
                    if (DecoyMsStream != null)
                        DecoyMsStream.Close();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

       
        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
