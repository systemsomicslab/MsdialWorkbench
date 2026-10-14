using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Lipidomics;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>
    /// The seed library of the staged lipid annotation, plus every reference spectrum generated while
    /// annotating (so results can be referred back to after a project is reloaded).
    /// </summary>
    [MessagePackObject]
    public sealed class StagedLipidDatabase : IReferenceDataBase, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>
    {
        public const string CidCategory = "Cid";

        private readonly ConcurrentDictionary<string, Lazy<MoleculeMsReference>> _referenceByKey = new ConcurrentDictionary<string, Lazy<MoleculeMsReference>>();
        private readonly List<MoleculeMsReference> _references = new List<MoleculeMsReference>();
        private readonly object _syncObject = new object();
        private IReadOnlyList<LipidSeed> _seeds;
        private LipidSeedLibrary? _library;

        public StagedLipidDatabase(string id, IEnumerable<LipidSeed> seeds, string category = CidCategory) {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Category = category ?? throw new ArgumentNullException(nameof(category));
            _seeds = seeds?.ToArray() ?? throw new ArgumentNullException(nameof(seeds));
        }

        [SerializationConstructor]
        public StagedLipidDatabase(string id, string category) : this(id, Array.Empty<LipidSeed>(), category ?? CidCategory) {

        }

        [Key(nameof(Id))]
        public string Id { get; }

        /// <summary>Which generated spectrum models to use (the XML category); only "Cid" exists for now.</summary>
        [Key(nameof(Category))]
        public string Category { get; }

        [IgnoreMember]
        public IReadOnlyList<LipidSeed> Seeds => _seeds;

        [IgnoreMember]
        public IReadOnlyList<ILipidSpectrumModel> Models {
            get {
                switch (Category) {
                    case CidCategory:
                        return CidLipidSpectrumModels.All;
                    default:
                        throw new NotSupportedException($"No lipid spectrum models for category '{Category}'.");
                }
            }
        }

        [IgnoreMember]
        public LipidSeedLibrary Library {
            get {
                lock (_syncObject) {
                    return _library ?? (_library = new LipidSeedLibrary(_seeds, Models));
                }
            }
        }

        /// <summary>
        /// The reference for a generated structure, created once per (name, adduct) and numbered by
        /// <see cref="MoleculeMsReference.ScanID"/> for <see cref="Refer"/>.
        /// </summary>
        public MoleculeMsReference GetOrAddReference(ILipid lipid, AdductIon adduct, double precursorMz, double? retentionTime, IReadOnlyList<SpectrumPeak> spectrum) {
            var key = $"{lipid.Name}\t{adduct.AdductIonName}";
            // Lazy: GetOrAdd may run the factory twice under contention, but only one value is published.
            return _referenceByKey.GetOrAdd(key, _ => new Lazy<MoleculeMsReference>(() => {
                var reference = new MoleculeMsReference
                {
                    Name = lipid.Name,
                    PrecursorMz = precursorMz,
                    AdductType = adduct,
                    IonMode = adduct.IonMode,
                    ChromXs = new ChromXs(retentionTime ?? 0d),
                    Spectrum = spectrum.ToList(),
                    CompoundClass = lipid.LipidClass.ToString(),
                };
                lock (_syncObject) {
                    reference.ScanID = _references.Count;
                    _references.Add(reference);
                }
                return reference;
            }, isThreadSafe: true)).Value;
        }

        // IMatchResultRefer
        [IgnoreMember]
        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => Id;

        public MoleculeMsReference? Refer(MsScanMatchResult result) {
            lock (_syncObject) {
                return result.LibraryID >= 0 && result.LibraryID < _references.Count ? _references[result.LibraryID] : null;
            }
        }

        // IReferenceDataBase
        public void Save(Stream stream, bool forceSerialize = false) {
            StagedLipidDatabaseContent content;
            lock (_syncObject) {
                content = new StagedLipidDatabaseContent(_seeds.Select(StagedLipidSeedRow.From).ToList(), _references.ToList());
            }
            MessagePackDefaultHandler.SaveToStream(content, stream);
        }

        public void Load(Stream stream, string folderpath) {
            var content = MessagePackDefaultHandler.LoadFromStream<StagedLipidDatabaseContent>(stream);
            var seeds = (content.Seeds ?? new List<StagedLipidSeedRow>()).Select(row => row.ToSeed()).Where(seed => seed != null).Select(seed => seed!).ToArray();
            var references = content.References ?? new List<MoleculeMsReference>();
            lock (_syncObject) {
                _seeds = seeds;
                _library = null;
                _references.Clear();
                _references.AddRange(references);
                _referenceByKey.Clear();
                foreach (var reference in references) {
                    var loaded = reference;
                    _referenceByKey[$"{reference.Name}\t{reference.AdductType?.AdductIonName}"] = new Lazy<MoleculeMsReference>(() => loaded, isThreadSafe: true);
                }
            }
        }
    }

    [MessagePackObject]
    public sealed class StagedLipidDatabaseContent
    {
        public StagedLipidDatabaseContent(List<StagedLipidSeedRow> seeds, List<MoleculeMsReference> references) {
            Seeds = seeds;
            References = references;
        }

        [Key(nameof(Seeds))]
        public List<StagedLipidSeedRow> Seeds { get; }

        [Key(nameof(References))]
        public List<MoleculeMsReference> References { get; }
    }

    /// <summary>
    /// A seed as stored in the project; the lipid is re-parsed from its name on load.
    /// </summary>
    [MessagePackObject]
    public sealed class StagedLipidSeedRow
    {
        public StagedLipidSeedRow(string name, double? retentionTime, string? adduct, double? precursorMz, string? formula) {
            Name = name;
            RetentionTime = retentionTime;
            Adduct = adduct;
            PrecursorMz = precursorMz;
            Formula = formula;
        }

        [Key(nameof(Name))]
        public string Name { get; }

        [Key(nameof(RetentionTime))]
        public double? RetentionTime { get; }

        [Key(nameof(Adduct))]
        public string? Adduct { get; }

        [Key(nameof(PrecursorMz))]
        public double? PrecursorMz { get; }

        [Key(nameof(Formula))]
        public string? Formula { get; }

        public static StagedLipidSeedRow From(LipidSeed seed) {
            return new StagedLipidSeedRow(seed.Name, seed.RetentionTime, seed.Adduct?.AdductIonName, seed.PrecursorMz, seed.Formula);
        }

        public LipidSeed? ToSeed() {
            if (FacadeLipidParser.Default.Parse(Name) is not ILipid lipid) {
                return null;
            }
            var adduct = Adduct is null ? null : AdductIon.GetAdductIon(Adduct);
            return new LipidSeed(Name, lipid, RetentionTime, adduct, adduct is null ? null : PrecursorMz, Formula);
        }
    }
}
