using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Lipidomics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CompMs.App.SpectrumViewer.Model.LipidSpectrumXml
{
    public sealed class LipidSpectrumPreviewResult
    {
        public LipidSpectrumPreviewResult(List<SpectrumPeak> peaks, IReadOnlyList<string> messages) {
            Peaks = peaks;
            Messages = messages;
        }

        public List<SpectrumPeak> Peaks { get; }
        public IReadOnlyList<string> Messages { get; }
        public bool Success => Peaks != null;
    }

    // Hosts CompMs.Common.Lipidomics.SourceGenerator.LipidSpectrumGeneratorTypeGenerator directly
    // (the same Roslyn generator the real build uses) against the XML currently open in the editor,
    // compiles the generated {Class}CidLipidSpectrumGenerator in memory, and invokes it via
    // reflection. This guarantees the preview always matches the actual generator behavior with no
    // separate reimplementation of the formula-resolution logic.
    //
    // The produced assembly is loaded into the current AppDomain and never unloaded (.NET Framework
    // has no AssemblyLoadContext); a long editing session accumulates a modest amount of
    // unused-but-resident memory (one assembly per distinct class/XML/Constants combination
    // actually generated, see the caching in Generate() below) that is freed when the tool is
    // restarted. Re-opening or editing the XML always previews against the latest content.
    public class LipidSpectrumPreviewService
    {
        // AppDomain.CurrentDomain.GetAssemblies() barely changes once the app has started, but
        // MetadataReference.CreateFromFile() re-reads and re-parses every one of them (100+ in this
        // WPF app) on every call - cache the list and only rebuild it if the assembly count changes
        // (a cheap, if imperfect, staleness check; a reload/unload without a net count change is not
        // a case this app hits in practice).
        private List<MetadataReference> cachedReferences;
        private int cachedReferenceAssemblyCount = -1;

        // The compile (source-generate + Roslyn compile) is the expensive part; skip it entirely
        // when neither the (already class-filtered, see LipidSpectrumXmlEditorModel.
        // BuildFilteredLipidModelXml) lipid-model XML nor Constants.xml changed since last time, e.g.
        // re-generating after only the adduct changed.
        private string cachedLipidModelXml;
        private string cachedConstantsXml;
        private Assembly cachedAssembly;

        // generatorClassName is the exact <LipidClass> text of the XML entry being previewed
        // (e.g. "EtherLPE_P"), which is what the source generator actually names the emitted type
        // after - it is not always the same as lipid.LipidClass (an LbmClass), so callers that
        // know which entry they're previewing should always pass it explicitly. Falls back to
        // lipid.LipidClass for callers with no entry context (e.g. previewing a hand-built lipid
        // against a class whose rules aren't split by chain subtype).
        public LipidSpectrumPreviewResult Generate(string lipidModelXml, string constantsXml, Lipid lipid, AdductIon adduct, string generatorClassName = null) {
            var messages = new List<string>();
            try {
                Assembly assembly;
                if (cachedAssembly != null && cachedLipidModelXml == lipidModelXml && cachedConstantsXml == constantsXml) {
                    assembly = cachedAssembly;
                }
                else {
                    var xmlText = new InMemoryAdditionalText("LipidModel.xml", lipidModelXml);
                    var constantsText = new InMemoryAdditionalText("Constants.xml", constantsXml);

                    var optionsProvider = new MapAnalyzerConfigOptionsProvider();
                    optionsProvider.SetOptions(xmlText, new DictionaryAnalyzerConfigOptions(
                        new Dictionary<string, string> { ["build_metadata.AdditionalFiles.Category"] = "Cid" }));
                    optionsProvider.SetOptions(constantsText, new DictionaryAnalyzerConfigOptions(
                        new Dictionary<string, string> { ["build_metadata.AdditionalFiles.IsConstants"] = "true" }));

                    var compilation = CSharpCompilation.Create(
                        "LipidSpectrumPreview_" + Guid.NewGuid().ToString("N"),
                        references: GetReferences(),
                        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    var generator = new CompMs.Common.Lipidomics.SourceGenerator.LipidSpectrumGeneratorTypeGenerator().AsSourceGenerator();
                    var driver = CSharpGeneratorDriver.Create(
                        generators: new[] { generator },
                        additionalTexts: new AdditionalText[] { xmlText, constantsText },
                        parseOptions: CSharpParseOptions.Default,
                        optionsProvider: optionsProvider);

                    driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
                    foreach (var d in generatorDiagnostics) {
                        messages.Add("[generator] " + d);
                    }

                    using var peStream = new MemoryStream();
                    var emitResult = outputCompilation.Emit(peStream);
                    foreach (var d in emitResult.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning)) {
                        messages.Add("[compile] " + d);
                    }
                    if (!emitResult.Success) {
                        return new LipidSpectrumPreviewResult(null, messages);
                    }

                    assembly = Assembly.Load(peStream.ToArray());
                    cachedAssembly = assembly;
                    cachedLipidModelXml = lipidModelXml;
                    cachedConstantsXml = constantsXml;
                }

                var resolvedClassName = string.IsNullOrEmpty(generatorClassName) ? lipid.LipidClass.ToString() : generatorClassName;
                var typeName = $"CompMs.Common.Lipidomics.{resolvedClassName}CidLipidSpectrumGenerator";
                var type = assembly.GetType(typeName);
                if (type is null) {
                    messages.Add($"Generated type not found (no rule for this class/category?): {typeName}");
                    return new LipidSpectrumPreviewResult(null, messages);
                }

                var instance = Activator.CreateInstance(type);
                var method = type.GetMethod("Generate");
                var peaks = method?.Invoke(instance, new object[] { lipid, adduct }) as List<SpectrumPeak>;
                if (peaks is null) {
                    messages.Add($"{typeName}.Generate(...) returned null (no rule for adduct {adduct.AdductIonName}?)");
                }
                return new LipidSpectrumPreviewResult(peaks, messages);
            }
            catch (Exception ex) {
                messages.Add(ex.ToString());
                return new LipidSpectrumPreviewResult(null, messages);
            }
        }

        private List<MetadataReference> GetReferences() {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (cachedReferences != null && assemblies.Length == cachedReferenceAssemblyCount) {
                return cachedReferences;
            }
            cachedReferences = assemblies
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => {
                    try { return (MetadataReference)MetadataReference.CreateFromFile(a.Location); }
                    catch { return null; }
                })
                .Where(r => r != null)
                .ToList();
            cachedReferenceAssemblyCount = assemblies.Length;
            return cachedReferences;
        }

        private sealed class InMemoryAdditionalText : AdditionalText
        {
            private readonly SourceText text;

            public InMemoryAdditionalText(string path, string content) {
                Path = path;
                text = SourceText.From(content ?? string.Empty);
            }

            public override string Path { get; }

            public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default) => text;
        }

        private sealed class DictionaryAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> values;

            public DictionaryAnalyzerConfigOptions(Dictionary<string, string> values) {
                this.values = values;
            }

            public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value);
        }

        private sealed class MapAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly Dictionary<AdditionalText, AnalyzerConfigOptions> textOptions = new();

            public override AnalyzerConfigOptions GlobalOptions { get; } = new DictionaryAnalyzerConfigOptions(new Dictionary<string, string>());

            public void SetOptions(AdditionalText text, AnalyzerConfigOptions options) => textOptions[text] = options;

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
                textOptions.TryGetValue(textFile, out var options) ? options : GlobalOptions;
        }
    }
}
