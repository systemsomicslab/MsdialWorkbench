using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class FormulaStringParser
{
    private static readonly Regex _atomRegex, _rawFormulaRegex;

    static FormulaStringParser() {
        var atom = @"[A-Z][a-z]?|\[\d+[A-Z][a-z]?\]";
        var atoms = @$"({atom})(\d*)";
        var rawFormula = $@"^\s*({atoms})+\s*$";

        _atomRegex = new Regex(atoms, RegexOptions.Compiled);
        _rawFormulaRegex = new Regex(rawFormula, RegexOptions.Compiled);
    }

    public static bool CanConvertToFormulaDictionary(string formulaString) {
        return _rawFormulaRegex.IsMatch(formulaString);
    }

    public static Dictionary<string, int> ConvertToFormulaDictionary(string formulaString) {
        var matches = _atomRegex.Matches(formulaString);
        var res = new List<(string, int)>();
        foreach (Match match in matches) {
            var element = match.Groups[1].Value;
            var number = match.Groups[2].Value;

            res.Add((element, int.TryParse(number, out var result) ? result : 1));
        }
        return ToFormulaDictionary(res);
    }

    private static Dictionary<string, int> ToFormulaDictionary(IEnumerable<(string, int)> elements) {
        var result = new Dictionary<string, int>();
        foreach ((var element, var number) in elements) {
            if (result.ContainsKey(element)) {
                result[element] += number;
            }
            else {
                result[element] = number;
            }
        }
        return result;
    }

    public static bool IsMarkupFormula(string rawMarkup, string[] constants) {
        var regex = new Regex(@$"^\s*(:?<(:?{string.Join("|", constants)})>\s*\d+\s*</\2>\s*)+$", RegexOptions.Compiled);
        return regex.IsMatch(rawMarkup);
    }
}
