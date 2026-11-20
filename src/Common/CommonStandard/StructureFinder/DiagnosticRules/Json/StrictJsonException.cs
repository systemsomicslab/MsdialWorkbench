using System;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>
    /// A refusal of the strict reader (design 3.1), or of a number or formula rule of the format, with its stable
    /// error code (the codes of the Python reference, <c>rule_format.ERROR_CODES</c>).
    /// </summary>
    public sealed class StrictJsonException : Exception
    {
        public StrictJsonException(string code) : base(code) {
            Code = code;
        }

        public string Code { get; }
    }
}
