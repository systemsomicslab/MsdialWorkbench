using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
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
            PreviewSpectrumModel = new SpectrumModel("Preview");
            PreviewLipidModel = new LipidSelectionModel { ChainsType = "SubMolecularLevel" };
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

        public LipidMsEntryModel SelectedEntry {
            get => selectedEntry;
            set {
                if (SetProperty(ref selectedEntry, value)) {
                    SyncPreviewTargetToSelectedEntry();
                }
            }
        }
        private LipidMsEntryModel selectedEntry;

        // The class/adduct to preview are already fully determined by which <LipidMS> entry is
        // selected on the left; keep the preview lipid builder in sync so its class/adduct pickers
        // don't become a second, independent (and easily inconsistent) source of truth.
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
            set => SetProperty(ref previewAdduct, value);
        }
        private AdductIon previewAdduct;

        public SpectrumModel PreviewSpectrumModel { get; }

        private MoleculeMsReference previewedReference;

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
            // LipidSpectrumGeneratorTypeGenerator.Emit), which for entries like "EtherLPE_P" is
            // NOT the same as lipid.LipidClass (an LbmClass, "EtherLPE") - so look it up by the
            // selected entry's own name rather than derive it from the LbmClass enum, or every
            // suffixed entry would resolve to the wrong (or a nonexistent) generator class.
            var generatorClassName = SelectedEntry?.LipidClass;
            var result = previewService.Generate(Document.ToString(), constantsXml, lipid, PreviewAdduct, generatorClassName);

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

            LastPreviewMessages = result.Messages;
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
