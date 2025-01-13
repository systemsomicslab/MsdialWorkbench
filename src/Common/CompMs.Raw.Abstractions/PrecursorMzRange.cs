namespace CompMs.Raw.Abstractions;

public sealed class PrecursorMzRange {
    public double Mz { get; set; }
    public double Tolerance { get; set; }

    public double Start => Mz - Tolerance;
    public double End => Mz + Tolerance;
}
