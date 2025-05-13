using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CompMs.Common.Lipidomics.SourceGenerator;

internal sealed class FormulaSentenceParser
{
    private static readonly Regex _termRegex, _sentenceRegex;

    static FormulaSentenceParser() {
        var term = @"([+-]?)\s*((\d*)([A-z][A-z0-9]*)|(\d*\.?\d+))";
        var sentence = $@"^\s*({term})(\s*{term})*\s*$";

        _termRegex = new Regex(term, RegexOptions.Compiled);
        _sentenceRegex = new Regex(sentence, RegexOptions.Compiled);
    }

    public Sentence? Parse(string value) {
        if (!_sentenceRegex.IsMatch(value)) {
            return null;
        }

        var matches = _termRegex.Matches(value);
        var result = new List<(Term, int)>();
        foreach (Match match in matches) {
            var sign = match.Groups[1].Value;
            if (string.IsNullOrEmpty(sign)) {
                sign = "+";
            }

            if (!string.IsNullOrEmpty(match.Groups[4].Value)) {
                var number = match.Groups[3].Value;
                if (string.IsNullOrEmpty(number)) {
                    number = "1";
                }
                var element = match.Groups[4].Value;
                var factor = int.Parse(sign + number);
                result.Add((new() { Raw = element }, factor));
            }
            else if (!string.IsNullOrEmpty(match.Groups[5].Value)) {
                var element = match.Groups[5].Value;
                var factor = int.Parse(sign + '1');
                result.Add((new() { Raw = element }, factor));
            }
            else {
                continue;
            }

        }

        return new() { Terms = [.. result], };
    }

    public Sentence Parse(XElement element) {
        throw new NotImplementedException();
    }

    public SubVar Parse(string name, string value) {
        return new SubVar
        {
            Name = name,
            RawValue = value
        };
    }
}

internal sealed class Sentence {
    public (Term, int)[] Terms { get; set; } = [];
}

internal sealed class Term {
    public string Raw { get; set; } = string.Empty;
}

internal sealed class SubVar {
    public string Name { get; set; } = string.Empty;
    public string RawValue { get; set; } = string.Empty;
}
