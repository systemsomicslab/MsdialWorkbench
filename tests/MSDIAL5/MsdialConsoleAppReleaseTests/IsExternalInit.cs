namespace System.Runtime.CompilerServices
{
    // Lets C# 9 init accessors compile for .NET Framework, which does not define this type.
    internal static class IsExternalInit { }
}
