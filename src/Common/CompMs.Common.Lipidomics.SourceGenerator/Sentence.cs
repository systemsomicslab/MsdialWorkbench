namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class Sentence {
    public (Term, int)[] Terms { get; set; } = [];
    public string Raw { get; set; } = string.Empty;
}
