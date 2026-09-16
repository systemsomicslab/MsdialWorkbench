using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ShotgunProteomicsDB : IDisposable, IReferenceDataBase, IMatchResultRefer<PeptideMsReference, MsScanMatchResult> {
        [Key(19)]
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
        [Key(20)]
        public string FastPeptidesSerializeFile { get; set; }
        [Key(6)]
        public string PeptidesBinaryFile { get; set; }
        [Key(22)]
        public string FastPeptidesBinaryFile { get; set; }
        [IgnoreMember]
        public List<FastaProperty> FastaQueries { get; set; }
        [IgnoreMember]
        public List<PeptideMsReference> FastPeptideMsReference { get; set; }
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
        [Key(14)]
        public string PeptideMsFile { get; set; }
        [Key(21)]
        public string FastPeptideMsFile { get; set; }
        [IgnoreMember]
        public Stream PeptideMsStream { get; set; }
        [IgnoreMember]
        public Stream FastPeptideMsStream { get; set; }
        [Key(15)]
        public string DecoyMsFile { get; set; }
        [IgnoreMember]
        public Stream DecoyMsStream { get; set; }

        [IgnoreMember]
        string IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Key => Id;

        //[Key(16)]
        //public double MinPeptideMass { get; set; } = 100;

        //[Key(17)]
        //public double MassRangeEnd { get; set; } = 1250;
        //[Key(18)]
        //public CollisionType CollisionType { get; set; } = CollisionType.HCD;

        PeptideMsReference IMatchResultRefer<PeptideMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) {
            if (result.IsDecoy) {
                if (result.LibraryID < 0 ||
                    result.LibraryID >= DecoyPeptideMsRef.Count || 
                    DecoyPeptideMsRef[result.LibraryID].ScanID != result.LibraryID) {
                    return null;
                    //return DecoyPeptideMsRef.FirstOrDefault(reference => reference.ScanID == result.LibraryID);
                }
                return DecoyPeptideMsRef[result.LibraryID];
            }
            else {
                if (result.LibraryID < 0 ||
                    result.LibraryID >= PeptideMsRef.Count || 
                    PeptideMsRef[result.LibraryID].ScanID != result.LibraryID) {
                    if (result.LibraryID >= 0 && result.LibraryID < PeptideMsRef.Count) {
                        Console.WriteLine("Refer error scan id {0}, library id {1} in count {2}", PeptideMsRef[result.LibraryID].ScanID, result.LibraryID, PeptideMsRef.Count);
                    }
                    return null;
                    //return PeptideMsRef.FirstOrDefault(reference => reference.ScanID == result.LibraryID);
                }
              return PeptideMsRef[result.LibraryID];
            }
        }

        public ShotgunProteomicsDB() {

        }

        public ShotgunProteomicsDB(string file, string id, ProteomicsParameter proteomicsParam, string projectFolder) {
            FastaFile = file;
            Id = id;
            ProteomicsParameter = proteomicsParam;
            DataBaseSource = DataBaseSource.Fasta;

            var dt = DateTime.Now;
            var refid = "_" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();
            var filename = System.IO.Path.GetFileNameWithoutExtension(file) + refid;
            //var folderpath = Path.GetDirectoryName(file);

            var folderpath = Path.Combine(projectFolder, "ProteomicsDB");
            if (!Directory.Exists(folderpath)) Directory.CreateDirectory(folderpath);

            var filetemp = Path.Combine(folderpath, filename);
            //var filetemp = filename;
            PeptideMsFile = filetemp + "." + MsdialDataStorageFormat.msf;
            FastPeptideMsFile = filetemp + "_fast." + MsdialDataStorageFormat.msf;
            DecoyMsFile = filetemp + "_decoy." + MsdialDataStorageFormat.msf;

            FastaQueryBinaryFile = filetemp + "." + MsdialDataStorageFormat.bfasta;
            DecoyQueryBinaryFile = filetemp + "_decoy." + MsdialDataStorageFormat.bfasta;

            PeptidesSerializeFile = filetemp + "." + MsdialDataStorageFormat.spep;
            FastPeptidesSerializeFile = filetemp + "_fast." + MsdialDataStorageFormat.spep;
            DecoyPeptidesSerializeFile = filetemp + "_decoy." + MsdialDataStorageFormat.spep;

            PeptidesBinaryFile = filetemp + "." + MsdialDataStorageFormat.bpep;
            FastPeptidesBinaryFile = filetemp + "_fast." + MsdialDataStorageFormat.bpep;
            DecoyPeptidesBinaryFile = filetemp + "_decoy." + MsdialDataStorageFormat.bpep;

            Generate();
            //Generate_v2();
        }

        public void Generate_v2() {
            if (FastaFile == null || FastaFile == string.Empty || !System.IO.File.Exists(FastaFile)) return;
            Console.WriteLine("Loading FASTA queries");

            FastaQueries = FastaParser.ReadFastaUniProtKB(FastaFile);
            DecoyQueries = DecoyCreator.Convert2DecoyQueries(FastaQueries);

            CleavageSites = ProteinDigestion.GetCleavageSites(ProteomicsParameter.EnzymesForDigestion);
            ModificationContainer = ModificationUtility.GetModificationContainer(ProteomicsParameter.FixedModifications, ProteomicsParameter.VariableModifications);

            Console.WriteLine("Preparing peptide queries");
            var peptides = LibraryHandler.GenerateFastTargetPeptideReference(FastaQueries, CleavageSites, ModificationContainer, ProteomicsParameter);
            FastPeptideMsReference = MsfPepFileParser.GenerateFastPeptideMsObjcts(FastPeptideMsFile, FastPeptidesBinaryFile, peptides, ModificationContainer.Code2ID, ProteomicsParameter.MinMs2Mz, ProteomicsParameter.MaxMs2Mz, ProteomicsParameter.CollisionType, out Stream pFS);
            FastPeptideMsStream = pFS;

            Console.WriteLine("Peptide count {0}", peptides.Count);
            Console.WriteLine("Save");
            Save_v2();

            Console.WriteLine("Done");
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
            //var decoyPeptides = LibraryHandler.GenerateDecoyPeptideReference(peptides);

            Console.WriteLine("Peptide count {0}", peptides.Count);

            Console.WriteLine("MS peptide queries");
            PeptideMsRef = MsfPepFileParser.GeneratePeptideMsObjcts(PeptideMsFile, PeptidesBinaryFile, peptides, ModificationContainer.Code2ID, ProteomicsParameter.MinMs2Mz, ProteomicsParameter.MaxMs2Mz, ProteomicsParameter.CollisionType, out Stream pFS);
            DecoyPeptideMsRef = MsfPepFileParser.GeneratePeptideMsObjcts(DecoyMsFile, DecoyPeptidesBinaryFile, decoyPeptides, ModificationContainer.Code2ID, ProteomicsParameter.MinMs2Mz, ProteomicsParameter.MaxMs2Mz, ProteomicsParameter.CollisionType, out Stream dFS);

            //for (int i = 0; i < PeptideMsRef.Count; i++) {
            //    var forward = PeptideMsRef[i];
            //    var reverse = DecoyPeptideMsRef[i];

            //    Console.WriteLine(forward.Peptide.ModifiedSequence + "\t" + reverse.Peptide.ModifiedSequence);
            //    for (int j = 0; j < forward.Spectrum.Count; j++) {
            //        if (forward.Spectrum.Count - 1 < j || reverse.Spectrum.Count - 1 < j) break;
            //        Console.WriteLine(forward.Spectrum[j].Mass + "\t" + reverse.Spectrum[j].Mass);
            //    }
            //}


            Console.WriteLine("Peptide MS count {0}", PeptideMsRef.Count);
            PeptideMsStream = pFS;
            DecoyMsStream = dFS;

            Console.WriteLine("Save");
            Save();

            Console.WriteLine("Done");
        }

        public void Save_v2() {
            using (var fs = File.Open(FastaQueryBinaryFile, FileMode.Create)) {
                LargeListMessagePack.Serialize(fs, FastaQueries);
            }

            using (var fs = File.Open(DecoyQueryBinaryFile, FileMode.Create)) {
                LargeListMessagePack.Serialize(fs, DecoyQueries);
            }
            
            using (var fs = File.Open(FastPeptidesSerializeFile, FileMode.Create)) {
                LargeListMessagePack.Serialize(fs, FastPeptideMsReference);
            }

        }

        public void Save() {
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

        public void Save(Stream stream, bool forceSerialize = false) {

        }

        public void Load(string projectFolder) {
            if (this.FastaQueries != null) return;
            var folderpath = Path.Combine(projectFolder, "ProteomicsDB");

            var filetemp = Path.Combine(folderpath, Path.GetFileNameWithoutExtension(PeptideMsFile));
            PeptideMsFile = filetemp + "." + MsdialDataStorageFormat.msf;
            DecoyMsFile = filetemp + "_decoy." + MsdialDataStorageFormat.msf;

            FastaQueryBinaryFile = filetemp + "." + MsdialDataStorageFormat.bfasta;
            DecoyQueryBinaryFile = filetemp + "_decoy." + MsdialDataStorageFormat.bfasta;

            PeptidesSerializeFile = filetemp + "." + MsdialDataStorageFormat.spep;
            DecoyPeptidesSerializeFile = filetemp + "_decoy." + MsdialDataStorageFormat.spep;

            PeptidesBinaryFile = filetemp + "." + MsdialDataStorageFormat.bpep;
            DecoyPeptidesBinaryFile = filetemp + "_decoy." + MsdialDataStorageFormat.bpep;

            using (var fs = File.Open(FastaQueryBinaryFile, FileMode.Open)) {
                this.FastaQueries = LargeListMessagePack.Deserialize<FastaProperty>(fs);
            }

            using (var fs = File.Open(DecoyQueryBinaryFile, FileMode.Open)) {
                this.DecoyQueries = LargeListMessagePack.Deserialize<FastaProperty>(fs);
            }

            using (var fs = File.Open(PeptidesSerializeFile, FileMode.Open)) {
                this.PeptideMsRef = LargeListMessagePack.Deserialize<PeptideMsReference>(fs);
            }
            if (!this.PeptideMsRef.IsEmptyOrNull()) {
                foreach (var ms in this.PeptideMsRef) {
                    ms.MinMs2 = this.ProteomicsParameter.MinMs2Mz;
                    ms.MaxMs2 = this.ProteomicsParameter.MaxMs2Mz;
                    ms.CollisionType = this.ProteomicsParameter.CollisionType;
                }
            }

            MsfPepFileParser.LoadPeptideInformation(PeptidesBinaryFile, PeptideMsRef, ModificationContainer.ID2Code, ModificationContainer.Code2AminoAcidObj);

            using (var fs = File.Open(DecoyPeptidesSerializeFile, FileMode.Open)) {
                this.DecoyPeptideMsRef = LargeListMessagePack.Deserialize<PeptideMsReference>(fs);
            }
            if (!this.DecoyPeptideMsRef.IsEmptyOrNull()) {
                foreach (var ms in this.DecoyPeptideMsRef) {
                    ms.MinMs2 = this.ProteomicsParameter.MinMs2Mz;
                    ms.MaxMs2 = this.ProteomicsParameter.MaxMs2Mz;
                    ms.CollisionType = this.ProteomicsParameter.CollisionType;
                }
            }

            MsfPepFileParser.LoadPeptideInformation(DecoyPeptidesBinaryFile, DecoyPeptideMsRef, ModificationContainer.ID2Code, ModificationContainer.Code2AminoAcidObj);
            PeptideMsStream = File.Open(PeptideMsFile, FileMode.Open, FileAccess.ReadWrite);
            DecoyMsStream = File.Open(DecoyMsFile, FileMode.Open, FileAccess.ReadWrite);

            foreach (var query in this.PeptideMsRef) {
                query.Fs = PeptideMsStream;
            }

            foreach (var query in this.DecoyPeptideMsRef) {
                query.Fs = DecoyMsStream;
            }
        }

        public void Load(Stream stream, string folderpath) {
            Load(folderpath);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (PeptideMsStream != null)
                        PeptideMsStream.Close();
                    if (DecoyMsStream != null)
                        DecoyMsStream.Close();
                    if (FastPeptideMsStream != null)
                        FastPeptideMsStream.Close();
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
