using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Loading
{
    /// <summary>One load-time refusal: a code of <see cref="RuleErrorCodes"/>, the file (a path relative to the
    /// rule-set root, with '/') and an RFC 6901 pointer into it ("" for the whole file).</summary>
    public sealed class RuleLoadError : IEquatable<RuleLoadError>
    {
        public RuleLoadError(string code, string file, string pointer) {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            File = file ?? throw new ArgumentNullException(nameof(file));
            Pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
        }

        public string Code { get; }
        public string File { get; }
        public string Pointer { get; }

        /// <summary>code, file and pointer separated by tabs, as the Python reference prints an error.</summary>
        public override string ToString() => Code + "\t" + File + "\t" + Pointer;

        public bool Equals(RuleLoadError? other) =>
            other != null && string.Equals(Code, other.Code, StringComparison.Ordinal)
            && string.Equals(File, other.File, StringComparison.Ordinal)
            && string.Equals(Pointer, other.Pointer, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as RuleLoadError);

        public override int GetHashCode() {
            unchecked {
                var h = StringComparer.Ordinal.GetHashCode(Code);
                h = h * 31 + StringComparer.Ordinal.GetHashCode(File);
                return h * 31 + StringComparer.Ordinal.GetHashCode(Pointer);
            }
        }

        /// <summary>Distinct errors sorted by (file, pointer, code) in code-point order, as the reference sorts them.</summary>
        public static List<RuleLoadError> Sort(IEnumerable<RuleLoadError> errors) {
            var distinct = new List<RuleLoadError>(new HashSet<RuleLoadError>(errors));
            distinct.Sort(Compare);
            return distinct;
        }

        private static int Compare(RuleLoadError a, RuleLoadError b) {
            var c = CanonicalJson.CodePointOrder.Compare(a.File, b.File);
            if (c != 0) {
                return c;
            }
            c = CanonicalJson.CodePointOrder.Compare(a.Pointer, b.Pointer);
            return c != 0 ? c : CanonicalJson.CodePointOrder.Compare(a.Code, b.Code);
        }
    }

    /// <summary>A load refused as a whole (design 3.6: all or nothing), with every error.</summary>
    public sealed class DiagnosticRuleSetException : Exception
    {
        public DiagnosticRuleSetException(IReadOnlyList<RuleLoadError> errors)
            : base(Describe(errors)) {
            Errors = errors;
        }

        public IReadOnlyList<RuleLoadError> Errors { get; }

        private static string Describe(IReadOnlyList<RuleLoadError> errors) {
            var lines = new List<string> { "The diagnostic rule set was refused:" };
            for (var i = 0; i < errors.Count && i < 20; i++) {
                lines.Add(errors[i].ToString());
            }
            if (errors.Count > 20) {
                lines.Add("...");
            }
            return string.Join("\n", lines);
        }
    }

    /// <summary>Collects the errors of one file (the reference's Ctx).</summary>
    internal sealed class ErrorSink
    {
        private readonly List<RuleLoadError> _all;

        public ErrorSink(List<RuleLoadError> all, string file) {
            _all = all;
            File = file;
        }

        public string File { get; }

        /// <summary>The number of errors collected so far, over every file.</summary>
        public int N => _all.Count;

        public void Err(string code, string pointer) => _all.Add(new RuleLoadError(code, File, pointer));
    }
}
