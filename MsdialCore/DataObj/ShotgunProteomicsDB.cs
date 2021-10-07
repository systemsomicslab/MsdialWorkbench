using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ShotgunProteomicsDB : IDisposable, IReferenceDataBase, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> {
        [Key(0)]
        private bool disposedValue;

        [Key(1)]
        public string FastaFile { get; set; }
        [Key(2)]
        public string Id { get; set; }
        [Key(3)]
        public DataBaseSource DataBaseSource { get; set; }
        [Key(4)]
        public string FastaQueryBinaryFile { get; set; }
        [Key(5)]
        public string PeptidesSerializeFile { get; set; }
        [Key(6)]
        public string PeptidesBinaryFile { get; set; }
        [IgnoreMember]
        public List<FastaProperty> FastaQueries { get; set; }
        [IgnoreMember]
        public List<PeptideMsReference> PeptideMsRef { get; set; }
        [Key(7)]
        public string DecoyQueryBinaryFile { get; set; }
        [Key(8)]
        public string DecoyPeptidesSerializeFile { get; set; }
        [Key(9)]
        public string DecoyPeptidesBinaryFile { get; set; }
        [IgnoreMember]
        public List<FastaProperty> DecoyQueries { get; set; }
        [IgnoreMember]
        public List<PeptideMsReference> DecoyPeptideMsRef { get; set; }

        [Key(10)]
        public ModificationContainer ModificationContainer { get; set; }
        [Key(11)]
        public List<string> CleavageSites { get; set; }
        [Key(12)]
        public ProteomicsParameter ProteomicsParameter { get; set; }
        [Key(13)]
        public MsRefSearchParameterBase MsRefSearchParameter { get; set; }
        [Key(14)]
        public string PeptideMsFile { get; set; }
        [IgnoreMember]
        public Stream PeptideMsStream { get; set; }
        [Key(15)]
        public string DecoyMsFile { get; set; }
        [IgnoreMember]
        public Stream DecoyMsStream { get; set; }

        [IgnoreMember]
        string IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Key => Id;

        PeptideMsReference IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) {
            if (result.IsDecoy) {
                if (result.LibraryID < 0 ||
                    result.LibraryID >= DecoyPeptideMsRef.Count || 
                    DecoyPeptideMsRef[result.LibraryID].ScanID != result.LibraryID) {
                    return DecoyPeptideMsRef.FirstOrDefault(reference => reference.ScanID == result.LibraryID);
                }
                return DecoyPeptideMsRef[result.LibraryID];
            }
            else {
                if (result.LibraryID < 0 ||
                    result.LibraryID >= PeptideMsRef.Count || 
                    PeptideMsRef[result.LibraryID].ScanID != result.LibraryID) {
                    return PeptideMsRef.FirstOrDefault(reference => reference.ScanID == result.LibraryID);
                }
                return PeptideMsRef[result.LibraryID];
            }
        }

        public ShotgunProteomicsDB() {

        }

        public ShotgunProteomicsDB(string file, string id, ProteomicsParameter proteomicsParam, MsRefSearchParameterBase msrefSearchParam) {
            FastaFile = file;
            Id = id;
            ProteomicsParameter = proteomicsParam;
            MsRefSearchParameter = msrefSearchParam;
            DataBaseSource = DataBaseSource.Fasta;

            var dt = DateTime.Now;
            var refid = "_" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();
            var filename = System.IO.Path.GetFileNameWithoutExtension(file) + refid;
            var folderpath = Path.GetDirectoryName(file);
            var filetemp = Path.Combine(folderpath, filename);
            PeptideMsFile = filetemp + "." + MsdialDataStorageFormat.msf;
            DecoyMsFile = filetemp + "_decoy." + MsdialDataStorageFormat.msf;

            FastaQueryBinaryFile = filetemp + "." + MsdialDataStorageFormat.bfasta;
            DecoyQueryBinaryFile = filetemp + "_decoy." + MsdialDataStorageFormat.bfasta;

            PeptidesSerializeFile = filetemp + "." + MsdialDataStorageFormat.spep;
            DecoyPeptidesSerializeFile = filetemp + "_decoy." + MsdialDataStorageFormat.spep;

            PeptidesBinaryFile = filetemp + "." + MsdialDataStorageFormat.bpep;
            DecoyPeptidesBinaryFile = filetemp + "_decoy." + MsdialDataStorageFormat.bpep;

            Generate();
        }

        public void Generate() {
            if (FastaFile == null || FastaFile == string.Empty || !System.IO.File.Exists(FastaFile)) return;
            Console.WriteLine("Loading FASTA queries");

            FastaQueries = FastaParser.ReadFastaUniProtKB(FastaFile);
            DecoyQueries = DecoyCreator.Convert2DecoyQueries(FastaQueries);

            CleavageSites = ProteinDigestion.GetCleavageSites(ProteomicsParameter.EnzymesForDigestion);
            ModificationContainer = ModificationUtility.GetModificationContainer(ProteomicsParameter.FixedModifications, ProteomicsParameter.VariableModifications);

            Console.WriteLine("Preparing peptide queries");
            var peptides = LibraryHandler.GenerateTargetPeptideReference(FastaQueries, CleavageSites, ModificationContainer, ProteomicsParameter);
            var decoyPeptides = LibraryHandler.GenerateTargetPeptideReference(DecoyQueries, CleavageSites, ModificationContainer, ProteomicsParameter);

            Console.WriteLine("MS peptide queries");
            PeptideMsRef = MsfPepFileParser.GeneratePeptideMsObjcts(PeptideMsFile, PeptidesBinaryFile, peptides, ModificationContainer.Code2ID, MsRefSearchParameter.MassRangeBegin, MsRefSearchParameter.MassRangeEnd, out Stream pFS);
            DecoyPeptideMsRef = MsfPepFileParser.GeneratePeptideMsObjcts(DecoyMsFile, DecoyPeptidesBinaryFile, decoyPeptides, ModificationContainer.Code2ID, MsRefSearchParameter.MassRangeBegin, MsRefSearchParameter.MassRangeEnd, out Stream dFS);

            PeptideMsStream = pFS;
            DecoyMsStream = dFS;

            Console.WriteLine("Save");
            Save(null);

            Console.WriteLine("Done");
        }

        public void Save(Stream stream) {

            using (var fs = File.Open(FastaQueryBinaryFile, FileMode.Create)) {
                LargeListMessagePack.Serialize(fs, FastaQueries);
            }

            using (var fs = File.Open(DecoyQueryBinaryFile, FileMode.Create)) {
                LargeListMessagePack.Serialize(fs, DecoyQueries);
            }

            using (var fs = File.Open(PeptidesSerializeFile, FileMode.Create)) {
                LargeListMessagePack.Serialize(fs, PeptideMsRef);
            }

            using (var fs = File.Open(DecoyPeptidesSerializeFile, FileMode.Create)) {
                LargeListMessagePack.Serialize(fs, DecoyPeptideMsRef);
            }
        }

        public void Load(Stream stream) {
            if (this.FastaQueries != null) return;
            using (var fs = File.Open(FastaQueryBinaryFile, FileMode.Create)) {
                this.FastaQueries = LargeListMessagePack.Deserialize<FastaProperty>(fs);
            }

            using (var fs = File.Open(DecoyQueryBinaryFile, FileMode.Create)) {
                this.DecoyQueries = LargeListMessagePack.Deserialize<FastaProperty>(fs);
            }

            using (var fs = File.Open(PeptidesSerializeFile, FileMode.Create)) {
                this.PeptideMsRef = LargeListMessagePack.Deserialize<PeptideMsReference>(fs);
            }
            MsfPepFileParser.LoadPeptideInformation(PeptidesBinaryFile, PeptideMsRef, ModificationContainer.ID2Code, ModificationContainer.Code2AminoAcidObj);

            using (var fs = File.Open(DecoyPeptidesSerializeFile, FileMode.Create)) {
                this.DecoyPeptideMsRef = LargeListMessagePack.Deserialize<PeptideMsReference>(fs);
            }
            MsfPepFileParser.LoadPeptideInformation(DecoyPeptidesBinaryFile, DecoyPeptideMsRef, ModificationContainer.ID2Code, ModificationContainer.Code2AminoAcidObj);
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
