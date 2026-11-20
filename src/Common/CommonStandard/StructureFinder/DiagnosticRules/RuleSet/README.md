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
or library formula.

The current version, 0.1.0, is the first shipped set: 106 rules, 58 scaffold lists, 106 motifs and 28 nesting rows.
Its element, adduct and unit tables are those of the 0.0.1-skeleton format release, which already held the WS8 P3
draft's unit and adduct requests (the table rows first, then the request files in path order). Its rules and
scaffold lists are the draft's, and its motif and nesting tables are the draft's motif and nesting staging rows in
path order, so every rule's `rule_semantic_sha256` is the one the draft gave. No rule has had the author's review
yet: every rule is `author_reviewed: false` with `review: null`, so no outcome is claim-eligible (each lists
`not_reviewed`). The motifs carry ChemOnt nodes, which are report-only (below).

## ChemOnt nodes

Each motif row's `chemont` lists ChemOnt nodes as `{id, name}`, sorted by id: `compatible` holds the classes a
compound carrying the motif is expected to fall in, `broader` the ancestors and wider classes that still contain it.
A report compares a ClassyFire class or FSEA term with them by name (`agree`, `broader`, `differs`), so the names are
the ontology's own, spelling included. The field is report-only: it is left out of every `rule_semantic_sha256`, so
editing it changes the rule-set sha256 but no rule's semantics or review.

The class compared is a candidate's ClassyFire direct parent, often a deep class, and the comparison is by name, so
`compatible` lists the deep classes as well:

- Descendants. Each descendant of a `compatible` node that is the direct parent of compounds in the MS-FINDER
  structure database (MsfinderStructureDB-VS15 joined with InchikeyClassyfireDB-VS5 by InChIKey; up to 400
  compounds per class) was checked with the motif's SMARTS in NCDK. It is `compatible` when at least two of its
  compounds, and at least a quarter of those checked, carry the motif; a descendant whose compounds do not (vitamin D
  under `steroid`, say) is left out, so a candidate there reads `differs`.
- Nesting. A compound carrying a child motif carries its parent, so every class `compatible` with the child is
  `compatible` or `broader` for the parent (`MotifTests` checks it).

- Source: ChemOnt 2.1, the ClassyFire ontology file `ChemOnt_2_1.obo` (OBO header `data-version: 2.1`,
  `date: 26:08:2016 16:28`; ClassyFire download path `/system/downloads/1_0/chemont/ChemOnt_2_1.obo.zip`), sha256
  `8616a6ecb96c8aeb204739a4de045cd290eab9a0e164d0ceaa5d888d0751fe22` (2,032,131 bytes). Every id and name was
  checked against that file. The ontology itself is not committed.
- Citation: Djoumbou Feunang Y. et al., ClassyFire: automated chemical classification with a comprehensive,
  computable taxonomy. J. Cheminform. 8, 61 (2016), doi:10.1186/s13321-016-0174-y.
- In 0.1.0 all 106 motifs carry nodes (1,293 in all, 593 distinct); 12 have an empty `compatible` list because no
  ChemOnt class matches the motif itself, and one (`c_prenyl_aryl`) has an empty `broader` list.
