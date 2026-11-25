using CompMs.Common.StructureFinder.DiagnosticRules.Json;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Chemistry
{
    /// <summary>
    /// <c>tables/elements.json</c> (design 3.4): monoisotopic element masses and the electron mass, each held as
    /// its JSON lexeme and as exact pico-daltons (10^-12 Da) taken from that lexeme. Compositions are count
    /// vectors indexed by this table's order (the file's key order).
    /// </summary>
    public sealed class ElementTable
    {
        public const long PdaPerDa = 1000000000000L;

        private readonly string[] _symbols;
        private readonly Dictionary<string, int> _index;
        private readonly JsonFloat[] _masses;
        private readonly BigInteger[] _pda;

        internal ElementTable(IReadOnlyList<KeyValuePair<string, JsonFloat>> masses, JsonFloat electron) {
            _symbols = new string[masses.Count];
            _masses = new JsonFloat[masses.Count];
            _pda = new BigInteger[masses.Count];
            _index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < masses.Count; i++) {
                _symbols[i] = masses[i].Key;
                _masses[i] = masses[i].Value;
                _pda[i] = DecimalLexeme.ToPda(masses[i].Value);
                _index.Add(masses[i].Key, i);
            }
            Electron = electron;
            ElectronPdaExact = DecimalLexeme.ToPda(electron);
            HydrogenIndex = _index.TryGetValue("H", out var h) ? h : -1;
        }

        public int Count => _symbols.Length;

        public IReadOnlyList<string> Symbols => _symbols;

        /// <summary>The electron mass as written in elements.json.</summary>
        public JsonFloat Electron { get; }

        public BigInteger ElectronPdaExact { get; }

        /// <summary>The index of H, or -1 when the table has none.</summary>
        public int HydrogenIndex { get; }

        public bool TryGetIndex(string symbol, out int index) => _index.TryGetValue(symbol, out index);

        public bool Contains(string symbol) => _index.ContainsKey(symbol);

        /// <summary>The element mass as written in elements.json.</summary>
        public JsonFloat Mass(int index) => _masses[index];

        public BigInteger PdaExact(int index) => _pda[index];

        /// <summary>Exact mass of a composition in pDa.</summary>
        public BigInteger MassPdaExact(long[] counts) {
            var total = BigInteger.Zero;
            for (var i = 0; i < counts.Length; i++) {
                if (counts[i] != 0) {
                    total += counts[i] * _pda[i];
                }
            }
            return total;
        }

        /// <summary>Exact m/z numerator of an ion of charge z (+1 or -1) in pDa: mass - z * electron.</summary>
        public BigInteger IonPdaExact(long[] counts, int charge) => MassPdaExact(counts) - charge * ElectronPdaExact;

        /// <summary>m/z or Da from pDa: one IEEE division of exact operands (design 4.1), as Python's pda / 1e12.</summary>
        public static double ToDa(long pda) => pda / 1e12;
    }
}
