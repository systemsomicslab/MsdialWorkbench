namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class Sentence {
    public (Term, double)[] Terms { get; set; } = [];
    public string Raw { get; set; } = string.Empty;
}
