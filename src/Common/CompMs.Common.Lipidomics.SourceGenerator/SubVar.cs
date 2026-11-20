namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class SubVar {
    public string Name { get; set; } = string.Empty;
    public Sentence Sentence { get; set; } = new();
    public string Resolved { get; set; } = string.Empty;
}
