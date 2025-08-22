using System.Text.RegularExpressions;

namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class FormulaSentenceParser
{
    private static readonly Regex _termRegex, _sentenceRegex;

    static FormulaSentenceParser()
    {
        var term = @"(?<sign>[+-]?)\s*(?:(?<count>\d*)(?<element>[A-Za-z][A-Za-z0-9]*)|(?<number>\d*\.?\d+))(?:/(?<divisor>\d+))?";
        var sentence = $@"^\s*({term})(\s*{term})*\s*$";

        _termRegex = new Regex(term, RegexOptions.Compiled);
        _sentenceRegex = new Regex(sentence, RegexOptions.Compiled);
    }

    public Sentence? Parse(string value)
    {
        if (FormulaStringParser.IsMarkupFormula(value))
        {
            return new Sentence { Raw = value, Terms = [(new Term { Raw = value }, 1)] };
        }
        if (!_sentenceRegex.IsMatch(value)) return null;

        var result = new List<(Term, double)>();

        foreach (Match match in _termRegex.Matches(value))
        {
            var sign = string.IsNullOrEmpty(match.Groups["sign"].Value) ? "+" : match.Groups["sign"].Value;

            // 元の数字
            double factor = 1;

            if (!string.IsNullOrEmpty(match.Groups["element"].Value))
            {
                var numStr = match.Groups["count"].Value;
                factor = string.IsNullOrEmpty(numStr) ? 1 : int.Parse(numStr);
                factor = double.Parse(sign + factor.ToString());
                // divisor があれば割る
                if (!string.IsNullOrEmpty(match.Groups["divisor"].Value))
                {
                    factor = factor / double.Parse(match.Groups["divisor"].Value);
                }
                result.Add((new Term { Raw = match.Groups["element"].Value }, factor));
            }
            else if (!string.IsNullOrEmpty(match.Groups["number"].Value))
            {
                factor = double.Parse(sign + "1");
                if (!string.IsNullOrEmpty(match.Groups["divisor"].Value))
                {
                    factor = (factor / double.Parse(match.Groups["divisor"].Value));
                }
                result.Add((new Term { Raw = match.Groups["number"].Value }, factor));
            }
        }

        return new Sentence { Terms = [.. result], Raw = value };
    }

    public SubVar? Parse(string name, string value) {
        if (Parse(value) is not { } sentence) {
            return null;
        }
        return new SubVar
        {
            Name = name,
            Sentence = sentence,
        };
    }
}
