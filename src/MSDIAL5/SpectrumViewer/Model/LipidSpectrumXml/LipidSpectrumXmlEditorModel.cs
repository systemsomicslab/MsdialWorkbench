using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.App.SpectrumViewer.Model.LipidSpectrumXml
{
    // Owns the spectrum-generator markup XML currently open in the editor, and the "generate a
    // theoretical spectrum for one lipid/adduct" preview workflow built on LipidSpectrumPreviewService.
    public class LipidSpectrumXmlEditorModel : BindableBase
    {
        private readonly LipidSpectrumPreviewService previewService = new();

        private static readonly string[] DefaultAdductNames = {
            "[M+H]+", "[M+NH4]+", "[M+Na]+", "[M-H]-", "[M+HCOO]-", "[M+CH3COO]-",
        };

        public LipidSpectrumXmlEditorModel() {
            Entries = new ObservableCollection<LipidMsEntryModel>();
            GeneratorCandidates = new ObservableCollection<LipidMsEntryModel>();
            LibraryReferences = new ObservableCollection<MoleculeMsReference>();
            LibraryCandidates = new ObservableCollection<MoleculeMsReference>();
            PreviewSpectrumModel = new SplitSpectrumsModel("Preview");
            PreviewLipidModel = new LipidSelectionModel { ChainsType = "SubMolecularLevel" };
            PreviewLipidModel.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(LipidSelectionModel.LipidClass)) {
                    RefreshGeneratorCandidates();
                    RefreshLibraryCandidates();
                }
            };
            Adducts = new ObservableCollection<AdductIon>(DefaultAdductNames.Select(AdductIon.GetAdductIon));
            PreviewAdduct = Adducts[0];
            ConstantsFilePath = TryFindDefaultConstantsPath();
        }

        public string FilePath {
            get => filePath;
            private set => SetProperty(ref filePath, value);
        }
        private string filePath;

        public XDocument Document {
            get => document;
            private set => SetProperty(ref document, value);
        }
        private XDocument document;

        public ObservableCollection<LipidMsEntryModel> Entries { get; }

        // The one entry currently being generated against and shown in the rule editor. Picking it
        // by hand (text search) fills the builder in as a shortcut; conversely, building a lipid+
        // adduct auto-picks the best-matching entry via GeneratorCandidates/RefreshGeneratorCandidates
        // below, as long as MatchCurrentLipid is on and the current pick doesn't already match.
        public LipidMsEntryModel SelectedEntry {
            get => selectedEntry;
            set {
                if (SetProperty(ref selectedEntry, value)) {
                    SyncPreviewTargetToSelectedEntry();
                }
            }
        }
        private LipidMsEntryModel selectedEntry;

        private void SyncPreviewTargetToSelectedEntry() {
            if (selectedEntry is null) {
                return;
            }
            if (TryResolveLbmClass(selectedEntry.LipidClass, out var lbmClass)) {
                PreviewLipidModel.LipidClass = lbmClass;
            }
            else {
                // Don't silently leave whatever class the previous entry left behind - the
                // generator XML's <LipidClass> key is a superset of LbmClass (e.g. "EtherLPE_P"/
                // "EtherLPE_O" split one LbmClass's ion rules by chain subtype), so plenty of
                // entries have no matching LbmClass at all. GeneratePreview() below no longer
                // depends on this picker for which generator class to invoke - it uses the
                // entry's raw name directly - but the picker still needs a real LbmClass to build
                // an ILipid for the mass calculation, so flag it instead of guessing.
                LastPreviewMessages = new[] {
                    $"'{selectedEntry.LipidClass}' has no matching Lipid class (LbmClass); pick one above manually before generating."
                };
            }
            var adduct = Adducts.FirstOrDefault(a => a.AdductIonName == selectedEntry.Adduct);
            if (adduct is null) {
                try {
                    adduct = AdductIon.GetAdductIon(selectedEntry.Adduct);
                    if (adduct != null) {
                        Adducts.Add(adduct);
                    }
                }
                catch (System.Exception ex) {
                    LastPreviewMessages = new[] { $"Could not parse adduct '{selectedEntry.Adduct}' from the selected entry: {ex.Message}" };
                }
            }
            if (adduct != null) {
                PreviewAdduct = adduct;
            }
            if (TryResolveChainCount(selectedEntry.LipidClass, out var chainCount)) {
                PreviewLipidModel.ChainCount = chainCount;
            }
        }

        // <LipidDefinitions> records exactly how many chains each raw <LipidClass> name has (the
        // same data LipidSpectrumGeneratorTypeGenerator.CollectDefinitions reads) - e.g. LPE/
        // EtherLPE_P/EtherLPE_O are 1-chain, PE is 2-chain. Quick chain notation needs this to
        // build a parser with the right capacity; left at the default (2), a 1-chain entry like
        // "EtherLPE_P" only matches the combined SubMolecularLevel/TotalChain pattern, which can't
        // represent a plasmalogen chain at all (TotalChain.ToString() always renders "O-", never
        // "P-" - a separate, pre-existing CommonStandard issue).
        private bool TryResolveChainCount(string rawClassName, out int chainCount) {
            chainCount = 0;
            if (Document is null || string.IsNullOrEmpty(rawClassName)) {
                return false;
            }
            var def = Document.Descendants("LipidDefinition")
                .FirstOrDefault(e => (string)e.Element("Name") == rawClassName);
            return def != null && int.TryParse((string)def.Element("Chain"), out chainCount);
        }

        // The generator XML occasionally splits a single LbmClass's ion rules by chain subtype
        // with a "_P" (plasmalogen)/"_O" (alkyl ether) suffix that isn't itself an LbmClass member
        // (e.g. "EtherLPE_P"/"EtherLPE_O" both belong to LbmClass.EtherLPE). Only these two known,
        // unambiguous suffixes are stripped here; the many other XML class names with no LbmClass
        // equivalent (N-acyl amino acid/ganglioside conjugates, etc.) are left unresolved rather
        // than guessed at.
        private static bool TryResolveLbmClass(string rawClassName, out LbmClass lbmClass) {
            if (System.Enum.TryParse(rawClassName, out lbmClass)) {
                return true;
            }
            foreach (var suffix in new[] { "_P", "_O" }) {
                if (rawClassName != null && rawClassName.EndsWith(suffix)
                    && System.Enum.TryParse(rawClassName.Substring(0, rawClassName.Length - suffix.Length), out lbmClass)) {
                    return true;
                }
            }
            return false;
        }

        public string ConstantsFilePath {
            get => constantsFilePath;
            set => SetProperty(ref constantsFilePath, value);
        }
        private string constantsFilePath;

        public LipidSelectionModel PreviewLipidModel { get; }

        public ObservableCollection<AdductIon> Adducts { get; }

        public AdductIon PreviewAdduct {
            get => previewAdduct;
            set {
                if (SetProperty(ref previewAdduct, value)) {
                    RefreshGeneratorCandidates();
                    RefreshLibraryCandidates();
                }
            }
        }
        private AdductIon previewAdduct;

        // Whether the entry list (right-hand panel) is narrowed down to entries whose class+adduct
        // match the lipid currently being built, in addition to the free-text Filter. Turn it off to
        // browse/search the full XML regardless of what's built above.
        public bool MatchCurrentLipid {
            get => matchCurrentLipid;
            set {
                if (SetProperty(ref matchCurrentLipid, value)) {
                    RefreshGeneratorCandidates();
                }
            }
        }
        private bool matchCurrentLipid = true;

        // Every <LipidMS> entry whose class resolves to the same LbmClass as the lipid currently
        // built above, and whose adduct matches - i.e. every generator that could plausibly produce
        // a spectrum for what's being previewed. Recomputed whenever the built lipid's class, the
        // adduct, or MatchCurrentLipid changes.
        public ObservableCollection<LipidMsEntryModel> GeneratorCandidates { get; }

        private void RefreshGeneratorCandidates() {
            GeneratorCandidates.Clear();
            if (Document is null) {
                return;
            }
            var lipidClass = PreviewLipidModel.LipidClass;
            var adductName = PreviewAdduct?.AdductIonName;
            foreach (var entry in Entries
                .Where(e => TryResolveLbmClass(e.LipidClass, out var lbm) && lbm == lipidClass && e.Adduct == adductName)
                .OrderBy(RankBySubtypeMatch)) {
                GeneratorCandidates.Add(entry);
            }
            // Only steer the selection when the caller actually wants matches enforced, and only
            // when the current pick isn't one - don't yank a deliberately-browsed selection away
            // just because the lipid above changed while MatchCurrentLipid happens to be on.
            if (MatchCurrentLipid && (SelectedEntry is null || !GeneratorCandidates.Contains(SelectedEntry))) {
                SelectedEntry = GeneratorCandidates.FirstOrDefault();
            }
        }

        // When more than one candidate shares a class+adduct (e.g. "EtherLPE"/"EtherLPE_P"/
        // "EtherLPE_O"), default-pick the one whose "_P"/"_O" chain subtype matches a chain actually
        // present in the built lipid, so the common case needs no manual pick; every candidate stays
        // listed and selectable for whenever this guess is wrong.
        private int RankBySubtypeMatch(LipidMsEntryModel entry) {
            IChain[] chains;
            try {
                chains = PreviewLipidModel.CreateChains()?.GetDeterminedChains() ?? System.Array.Empty<IChain>();
            }
            catch {
                chains = System.Array.Empty<IChain>();
            }
            var hasPlasmalogen = chains.OfType<AlkylChain>().Any(c => c.IsPlasmalogen);
            var hasAlkylEther = chains.OfType<AlkylChain>().Any(c => !c.IsPlasmalogen);
            if (entry.LipidClass.EndsWith("_P")) {
                return hasPlasmalogen ? 0 : 2;
            }
            if (entry.LipidClass.EndsWith("_O")) {
                return hasAlkylEther ? 0 : 2;
            }
            return 1;
        }

        // The generated theoretical spectrum lives in the upper pane, an existing library spectrum
        // (if one is loaded and matched) in the lower - the same mirror-plot component used
        // elsewhere in the app for comparing two spectra.
        public SplitSpectrumsModel PreviewSpectrumModel { get; }

        private MoleculeMsReference previewedReference;

        public string LibraryFilePath {
            get => libraryFilePath;
            private set => SetProperty(ref libraryFilePath, value);
        }
        private string libraryFilePath;

        public ObservableCollection<MoleculeMsReference> LibraryReferences { get; }

        // Every loaded library reference whose OntologyOrCompoundClass/adduct matches the lipid
        // currently built above - the library-comparison analogue of GeneratorCandidates.
        public ObservableCollection<MoleculeMsReference> LibraryCandidates { get; }

        public MoleculeMsReference SelectedLibraryReference {
            get => selectedLibraryReference;
            set {
                if (SetProperty(ref selectedLibraryReference, value)) {
                    if (previewedLibraryReference != null) {
                        PreviewSpectrumModel.LowerSpectrumModel.RemoveScan(previewedLibraryReference);
                        previewedLibraryReference = null;
                    }
                    if (value != null) {
                        previewedLibraryReference = value;
                        PreviewSpectrumModel.LowerSpectrumModel.AddScan(value);
                    }
                    RecomputeSimilarity();
                }
            }
        }
        private MoleculeMsReference selectedLibraryReference;
        private MoleculeMsReference previewedLibraryReference;

        public double? SimilarityScore {
            get => similarityScore;
            private set => SetProperty(ref similarityScore, value);
        }
        private double? similarityScore;

        private void RecomputeSimilarity() {
            SimilarityScore = previewedReference != null && SelectedLibraryReference != null
                ? MsScanMatching.GetSimpleDotProduct(previewedReference, SelectedLibraryReference, 0.01, 0, 2000)
                : (double?)null;
        }

        public void OpenLibrary(string path) {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            List<MoleculeMsReference> references;
            try {
                switch (extension) {
                    case ".lbm":
                    case ".lbm2":
                        var queries = new LipidQueryBean {
                            SolventType = SolventType.CH3COONH4,
                            LbmQueries = LbmQueryParcer.GetLbmQueries(isLabUseOnly: true),
                        };
                        references = LibraryHandler.ReadLipidMsLibrary(path, queries, PreviewAdduct?.IonMode ?? IonMode.Positive);
                        break;
                    case ".msp":
                    case ".msp2":
                        references = LibraryHandler.ReadMspLibrary(path);
                        break;
                    default:
                        LastPreviewMessages = new[] { $"Unsupported library file extension: '{extension}' (expected .lbm/.lbm2/.msp/.msp2)." };
                        return;
                }
            }
            catch (System.Exception ex) {
                LastPreviewMessages = new[] { $"Could not load library '{path}': {ex.Message}" };
                return;
            }
            LibraryReferences.Clear();
            foreach (var reference in references) {
                LibraryReferences.Add(reference);
            }
            LibraryFilePath = path;
            RefreshLibraryCandidates();
        }

        private void RefreshLibraryCandidates() {
            LibraryCandidates.Clear();
            if (LibraryReferences.Count == 0) {
                return;
            }
            var lipidClassName = PreviewLipidModel.LipidClass.ToString();
            var adductName = PreviewAdduct?.AdductIonName;
            foreach (var reference in LibraryReferences.Where(r =>
                    string.Equals(r.OntologyOrCompoundClass, lipidClassName, System.StringComparison.OrdinalIgnoreCase)
                    && r.AdductType?.AdductIonName == adductName)) {
                LibraryCandidates.Add(reference);
            }
            if (SelectedLibraryReference is null || !LibraryCandidates.Contains(SelectedLibraryReference)) {
                SelectedLibraryReference = LibraryCandidates.FirstOrDefault();
            }
        }

        public IReadOnlyList<string> LastPreviewMessages {
            get => lastPreviewMessages;
            private set => SetProperty(ref lastPreviewMessages, value);
        }
        private IReadOnlyList<string> lastPreviewMessages = System.Array.Empty<string>();

        public void Open(string path) {
            Document = XDocument.Load(path);
            FilePath = path;
            Entries.Clear();
            foreach (var element in Document.Descendants("LipidMS")) {
                Entries.Add(new LipidMsEntryModel(element));
            }
            RefreshGeneratorCandidates();
        }

        // Seeds the new entry's class/adduct from whatever is currently built on the left, since
        // that's almost always why you're adding one (an existing class/adduct combo needs a rule,
        // or a variant of one you're already looking at) - edit LipidClass/Adduct on the right
        // afterwards for anything else (a brand new class, a typo'd default, etc.).
        public void AddEntry() {
            if (Document is null) {
                LastPreviewMessages = new[] { "Load a lipid-model XML before adding a generator entry." };
                return;
            }
            var container = Document.Descendants("LipidMSs").FirstOrDefault();
            if (container is null) {
                LastPreviewMessages = new[] { "Could not find a <LipidMSs> container in the loaded XML to add the new entry to." };
                return;
            }
            var element = new XElement("LipidMS",
                new XElement("LipidClass", PreviewLipidModel.LipidClass.ToString()),
                new XElement("LSILevel", "MSL"),
                new XElement("Adduct", PreviewAdduct?.AdductIonName ?? string.Empty));
            container.Add(element);
            var entry = new LipidMsEntryModel(element);
            Entries.Add(entry);
            RefreshGeneratorCandidates();
            SelectedEntry = entry;
        }

        public void RemoveEntry(LipidMsEntryModel entry) {
            if (entry is null) {
                return;
            }
            entry.Element.Remove();
            Entries.Remove(entry);
            if (SelectedEntry == entry) {
                SelectedEntry = null;
            }
            RefreshGeneratorCandidates();
        }

        public void Save() {
            if (Document != null && !string.IsNullOrEmpty(FilePath)) {
                Document.Save(FilePath);
            }
        }

        public void SaveAs(string path) {
            if (Document is null) {
                return;
            }
            Document.Save(path);
            FilePath = path;
        }

        public void OpenConstants(string path) {
            ConstantsFilePath = path;
        }

        public void GeneratePreview() {
            if (Document is null || string.IsNullOrEmpty(ConstantsFilePath) || !File.Exists(ConstantsFilePath)) {
                LastPreviewMessages = new[] { "Load a lipid-model XML and a Constants.xml before generating." };
                return;
            }

            Lipid lipid;
            try {
                lipid = (Lipid)PreviewLipidModel.Create();
            }
            catch (System.Exception ex) {
                LastPreviewMessages = new[] { "Could not build the preview lipid from the settings above: " + ex.Message };
                return;
            }
            var constantsXml = File.ReadAllText(ConstantsFilePath);
            // The generated type is named after the XML's raw <LipidClass> text (see
            // LipidSpectrumGeneratorTypeGenerator.Emit), which for entries like "EtherLPE_P" is NOT
            // the same as lipid.LipidClass (an LbmClass, "EtherLPE") - so look it up by whichever
            // entry is selected (RefreshGeneratorCandidates keeps it aligned with the built lipid+
            // adduct) rather than derive it from the LbmClass enum, or every suffixed entry would
            // resolve to the wrong (or a nonexistent) generator class.
            var generatorClassName = SelectedEntry?.LipidClass;
            var lipidModelXml = BuildFilteredLipidModelXml(generatorClassName ?? lipid.LipidClass.ToString());
            var result = previewService.Generate(lipidModelXml, constantsXml, lipid, PreviewAdduct, generatorClassName);

            if (previewedReference != null) {
                PreviewSpectrumModel.RemoveScan(previewedReference);
                previewedReference = null;
            }

            if (result.Success) {
                previewedReference = new MoleculeMsReference {
                    Name = $"{lipid} {PreviewAdduct.AdductIonName} (preview)",
                    AdductType = PreviewAdduct,
                    Spectrum = result.Peaks.OrderBy(p => p.Mass).ToList(),
                };
                PreviewSpectrumModel.AddScan(previewedReference);
            }
            RecomputeSimilarity();

            LastPreviewMessages = result.Messages;
        }

        // The full XML has 500+ <LipidMS> entries across ~230 classes; compiling the generator
        // against all of it (as GeneratePreview() used to) means emitting and compiling C# for
        // every one of those classes just to preview a single one, which is most of why Generate
        // used to take several seconds. LipidSpectrumGeneratorTypeGenerator.Emit groups purely by
        // <LipidClass> and emits one type per group, so dropping every group but the one actually
        // being generated is equivalent to the full XML for that one type, and cuts what
        // LipidSpectrumPreviewService has to compile down to ~1/230th.
        private string BuildFilteredLipidModelXml(string className) {
            if (Document is null) {
                return null;
            }
            if (string.IsNullOrEmpty(className)) {
                return Document.ToString();
            }
            var filtered = new XDocument(Document);
            foreach (var lipidMS in filtered.Descendants("LipidMS")
                    .Where(e => (string)e.Element("LipidClass") != className)
                    .ToList()) {
                lipidMS.Remove();
            }
            return filtered.ToString();
        }

        private static string TryFindDefaultConstantsPath() {
            try {
                var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
                for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent) {
                    var candidate = Path.Combine(dir.FullName, "src", "Common", "CommonStandard", "Lipidomics", "Constants.xml");
                    if (File.Exists(candidate)) {
                        return candidate;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
