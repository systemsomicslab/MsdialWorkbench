using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class Context {
    private readonly static Regex _snRegex, _acylRegex, _alkylRegex;

    private readonly Dictionary<string, string> _constants;

    static Context() {
        _snRegex = new Regex(@"SN\d+", RegexOptions.Compiled);
        _acylRegex = new Regex(@"Acyl\d+", RegexOptions.Compiled);
        _alkylRegex = new Regex(@"Alkyl\d+", RegexOptions.Compiled);
    }

    public Context((string Symbol, string Value)[] constants) {
        _constants = constants.ToDictionary(p => p.Symbol, p => p.Value);
    }

    public string Resolve(Term term) {
        if (IsSpecialTerm(term)) {
            return term.Raw;
        }
        if (double.TryParse(term.Raw, out _)) {
            return term.Raw;
        }

        if (FormulaStringParser.CanConvertToFormulaDictionary(term.Raw)) {
            var dict = FormulaStringParser.ConvertToFormulaDictionary(term.Raw);
            var results = new List<string>();
            foreach (var kvp in dict) {
                var (symbol, number) = (kvp.Key, kvp.Value);
                if (_constants.ContainsKey(symbol)) {
                    results.Add($"{symbol} * {number}");
                }
                else {
                    throw new InvalidOperationException($"Cannot resolve term: {symbol}.");
                }
            }
            return string.Join(" + ", results);
        }

        if (_constants.TryGetValue(term.Raw, out var val)) {
            return val;
        }

        throw new InvalidOperationException($"Cannot resolve term: {term.Raw}.");
    }

    private bool IsSpecialTerm(Term term) {
        return term.Raw == "M"
            || _snRegex.IsMatch(term.Raw)
            || _acylRegex.IsMatch(term.Raw)
            || _alkylRegex.IsMatch(term.Raw);
    }
}
