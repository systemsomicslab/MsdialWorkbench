# MS-FINDER diagnostic-ion rule set

This folder is the rule set that `CompMs.Common.StructureFinder.DiagnosticRules.DiagnosticRuleSet.LoadEmbedded()`
reads. Every `*.json` file and `ruleset-history.tsv` are embedded in the Common assembly under the resource name
`DiagnosticRules/RuleSet/<path>`. This README is not embedded.

| Path | Content |
|---|---|
| `ruleset.json` | `{"format": "msfinder-diagnostic-ruleset", "format_version": 1, "rule_set_version": ...}` |
| `ruleset-history.tsv` | `rule_set_version<TAB>ruleset_sha256<TAB>yyyy-mm-dd`, append-only; not hashed |
| `tables/elements.json` | monoisotopic element masses and the electron mass (RDKit 2026.09.2 `GetMostCommonIsotopeMass`; electron 0.000549) |
| `tables/adducts.json` | `{"format": "msfinder-diagnostic-adducts", "format_version": 1, "adducts": [rows]}` |
| `tables/units.json` | `{"format": "msfinder-diagnostic-units", "format_version": 1, "units": [rows]}`; the row order is the canonical unit order |
| `tables/motifs.json` | `{"format": "msfinder-diagnostic-motifs", "format_version": 1, "motifs": [rows]}` |
| `tables/motif_nesting.json` | `{"format": "msfinder-diagnostic-motif-nesting", "format_version": 1, "nesting": [rows]}` |
| `scaffolds/<list_id>.json` | enumerated scaffold lists |
| `rules/<rule_id>.json` | one rule record per file |

The four tables are objects with a format header around the row array, because the strict reader refuses a
top-level array and every `*.json` is hashed through it.

## Hashes and the version pin

- Each file's sha256 is taken over its canonical JSON (sorted keys, `(",", ":")` separators, ASCII escapes), so line
  endings, indentation and key order on disk do not change it.
- The rule-set sha256 is the sha256 of the canonical JSON of
  `{"files": {path: file sha256}, "format": "msfinder-diagnostic-ruleset-hash", "format_version": 1}` over every
  hashed file.
- The embedded set loads only when `ruleset-history.tsv` has a row for its `rule_set_version` with that sha256. A
  change to any hashed file needs a new version and a new history row. A directory set loads unpinned.

## Storage form

Files are written as Python's `json.dumps(obj, sort_keys=True, indent=1, ensure_ascii=True) + "\n"` with LF line
endings, which `tools/ws8_p3/rule_format.py fmt` in msdial_spectrum_catalog writes. `.gitattributes` keeps git from
converting them.

## Content

Rule files carry facts and citations only: DOI or reference with a page, table or figure locator, and notes of at
most one paraphrased sentence. They hold formulas, never typed masses, and no key that reads a precursor, product
or library formula. The current version is the format skeleton: the element, adduct and unit tables, and empty
motif and nesting tables, with no scaffold list and no rule.
