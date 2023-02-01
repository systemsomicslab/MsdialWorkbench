namespace CompMs.Common.Lipidomics
{
    public interface ILipidParser
    {
        string Target { get; }
        ILipid Parse(string lipidStr);
    }
}
