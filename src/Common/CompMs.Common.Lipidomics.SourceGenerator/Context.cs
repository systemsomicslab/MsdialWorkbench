using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class Context {
    private readonly static Regex _snRegex, _acylRegex, _alkylRegex, _sphingoRegex;

    private readonly Dictionary<string, string> _constants;
    private readonly Dictionary<string, SubVar> _vars;

    static Context() {
        _snRegex = new Regex(@"SN\d+", RegexOptions.Compiled);
        _acylRegex = new Regex(@"Acyl(\d+)*", RegexOptions.Compiled);
        _alkylRegex = new Regex(@"Alkyl(\d+)*", RegexOptions.Compiled);
        _sphingoRegex = new Regex(@"Sph(\d+)*", RegexOptions.Compiled);
    }

    public Context((string Symbol, string Value)[] constants) {
        _constants = constants.ToDictionary(p => p.Symbol, p => p.Value);
        _vars = [];
    }

    public RegisterHandle CreateRegisterHandle() {
        return new RegisterHandle(this);
    }

    public void Register(SubVar var) {
        _vars[var.Name] = var;
    }

    public string Resolve(Sentence sentence) {
        return string.Join(
            " + ",
            sentence.Terms.Select(p => p.Item2 == 1
                ? $"({Resolve(p.Item1)})"
                : $"({Resolve(p.Item1)}) * {p.Item2}")
        );
    }

    public string Resolve(Term term) {
        if (FormulaStringParser.IsMarkupFormula(term.Raw, [.. _constants.Keys])) {
            var dict = FormulaStringParser.ParseMarkupFormula(term.Raw);
            return ConstructFormula(dict);
        }
        if (IsSpecialTerm(term)) {
            return term.Raw;
        }
        if (double.TryParse(term.Raw, out _)) {
            return term.Raw;
        }

        if (_vars.TryGetValue(term.Raw, out var subVar)) {
            if (string.IsNullOrEmpty(subVar.Resolved)) {
                subVar.Resolved = Resolve(subVar.Sentence);
            }
            return subVar.Resolved;
        }

        if (FormulaStringParser.CanConvertToFormulaDictionary(term.Raw)) {
            var dict = FormulaStringParser.ConvertToFormulaDictionary(term.Raw);
            return ConstructFormula(dict);
        }

        if (_constants.TryGetValue(term.Raw, out var val)) {
            return $"({val})";
        }

        throw new InvalidOperationException($"Cannot resolve term: {term.Raw}.");
    }

    private string ConstructFormula(Dictionary<string, int> dict) {
        var results = new List<string>();
        foreach (var kvp in dict) {
            var (symbol, number) = (kvp.Key, kvp.Value);
            if (_constants.ContainsKey(symbol)) {
                results.Add(number == 1 ? symbol : $"{symbol} * {number}");
            }
            else {
                throw new InvalidOperationException($"Cannot resolve term: {symbol}.");
            }
        }
        return string.Join(" + ", results);
    }

    public string[] GetConstants() => [.. _constants.Keys];

    private bool IsSpecialTerm(Term term) {
        return term.Raw == "M"
            || _snRegex.IsMatch(term.Raw)
            || _acylRegex.IsMatch(term.Raw)
            || _alkylRegex.IsMatch(term.Raw)
            || _sphingoRegex.IsMatch(term.Raw);
    }

    public class RegisterHandle(Context ctx) : IDisposable
    {
        private readonly Context ctx = ctx;
        private readonly Dictionary<string, SubVar?> _prevs = [];

        public void Register(SubVar var) {
            if (!ctx._vars.TryGetValue(var.Name, out var prev)) {
                prev = null;
            }
            ctx._vars[var.Name] = var;
            _prevs[var.Name] = prev;
        }

        public void Dispose() {
            foreach (var kvp in _prevs) {
                if (kvp.Value is null) {
                    ctx._vars.Remove(kvp.Key);
                }
                else {
                    ctx._vars[kvp.Key] = kvp.Value;
                }
            }
            _prevs.Clear();
        }
    }
}
