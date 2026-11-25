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

Version 0.1.0 is the first shipped set: 106 rules, 58 scaffold lists, 106 motifs and 28 nesting rows.
Its element, adduct and unit tables are those of the 0.0.1-skeleton format release, which already held the WS8 P3
draft's unit and adduct requests (the table rows first, then the request files in path order). Its rules and
scaffold lists are the draft's, and its motif and nesting tables are the draft's motif and nesting staging rows in
path order, so every rule's `rule_semantic_sha256` is the one the draft gave. In 0.1.0 no rule had the author's
review: every rule was `author_reviewed: false` with `review: null`, so no outcome was claim-eligible (each listed
`not_reviewed`). The motifs carry ChemOnt nodes, which are report-only (below).

The current version, 0.2.0, changes only the review state of two rules. The WS8 P3 benchmark run
`p3cs-20261124-5a8fc37a` found no rule that met the strict tier. Two rules met the provisional tier, and the author
reviewed both: `triterpenoid_saponin.triterpenoid_saponin.neg` and
`flavonoid_o_glycoside.listed_aglycone_glycoside.pos`. Each now has `author_reviewed: true` and a `review` block.
Its basis is `benchmark`, measured at the p2 product tolerance and a 0.01 Da precursor tolerance, on QTOF-type,
Orbitrap/FT and other instruments. The other 104 rules keep `author_reviewed: false`. No rule's semantics changed, so
every `rule_semantic_sha256` is unchanged.

## ChemOnt nodes

Each motif row's `chemont` lists ChemOnt nodes as `{id, name}`, sorted by id, and no node is in both lists:

- `compatible` holds classes that a compound carrying the motif is expected to fall in. A listed class is not one
  whose members mostly carry the motif: a class picked by hand stays `compatible` however few of its database
  compounds carry it (`Stilbenes` for `resorcinol_stilbene`, say).
- `broader` holds every ancestor of a `compatible` class below the kingdoms (the root and `Organic compounds` are
  never listed), and wider classes picked by hand that hold the motif's compounds among many that do not. It is not
  a list of every class a carrier can fall in.

A report compares a ClassyFire class or FSEA term with them by name (`agree`, `broader`, `differs`), so the names are
the ontology's own, spelling included. A class in neither list reads `differs`, including one whose compounds carry
the motif but that no step below reached; hierarchy-aware comparison is the harness's, with the local ChemOnt (design
5.7). The field is report-only: it is left out of every `rule_semantic_sha256`, so editing it changes the rule-set
sha256 but no rule's semantics or review.

The class compared is a candidate's ClassyFire direct parent, often a deep class, and the comparison is by name, so
the lists name the deep classes as well. They were built in four steps:

- Hand-picked. Per motif, from its SMARTS and sources, the classes the motif's compounds belong to (`compatible`)
  and wider classes (`broader`).
- Checked classes. A class is checked when it is the direct parent of compounds in the MS-FINDER structure database
  (MsfinderStructureDB-VS15 joined with InchikeyClassyfireDB-VS5 by InChIKey; up to 400 compounds per class) and its
  compounds are matched with the motif's SMARTS in NCDK. It is added to `compatible` when at least two of its
  compounds, and at least a quarter of those checked, carry the motif. Checked were every descendant of a
  `compatible` class and, for classes ChemOnt files elsewhere, every class under `Alkaloids and derivatives` for the
  four indole motifs, the O-methylated flavonoid classes for the five flavone, flavonol, flavanone and
  dihydroflavonol motifs, and the prenylated xanthone and neoflavonoid classes for `c_prenyl_aryl`. A checked class
  that fails is left out (vitamin D for `steroid`, `Harmala alkaloids` for `beta_carboline_unsaturated_omethyl`,
  `Prenylated neoflavonoids`), except that an O-methylated flavonoid class with at least one carrier is `broader`, as
  the O-methylated isoflavonoid classes are for `isoflavone_core`.
- Nesting. A compound carrying a child motif carries its parent, so every class `compatible` with the child is
  `compatible` or `broader` for the parent (`MotifTests` checks it).
- Ancestors. Every ancestor of a `compatible` class below the kingdoms that is not `compatible` is `broader`, so a
  class between two listed ones (`Steroid lactones` between `Steroids and steroid derivatives` and `Cardenolides`)
  reads `broader`, not `differs` (`ChemOntLocalTests` checks it, below).

- Source: ChemOnt 2.1, the ClassyFire ontology file `ChemOnt_2_1.obo` (OBO header `data-version: 2.1`,
  `date: 26:08:2016 16:28`; ClassyFire download path `/system/downloads/1_0/chemont/ChemOnt_2_1.obo.zip`), sha256
  `8616a6ecb96c8aeb204739a4de045cd290eab9a0e164d0ceaa5d888d0751fe22` (2,032,131 bytes). Every id and name was
  checked against that file. The ontology itself is not committed, so the check is the local test
  `ChemOntLocalTests`, which runs when `MSDIAL_CHEMONT_OBO` names a copy of that file (sha256 checked) and is
  skipped otherwise; rerun it after any edit to the nodes.
- Citation: Djoumbou Feunang Y. et al., ClassyFire: automated chemical classification with a comprehensive,
  computable taxonomy. J. Cheminform. 8, 61 (2016), doi:10.1186/s13321-016-0174-y.
- In 0.1.0 all 106 motifs carry nodes (1,598 in all: 1,021 `compatible` and 577 `broader`; 671 distinct); 9 have an
  empty `compatible` list because no ChemOnt class matches the motif itself.
- The version of the ontology is recorded here only: the motif table's closed format has no field for it, and the
  README is not hashed. A ClassyFire database built on a later ChemOnt that renames a class would read `differs`
  for it.
