"""Smoke run and summary for the MS-FINDER console `diagrules` benchmark (WS8-P3 design section 8, PR 5).

Standard library only. Three subcommands:

  build-msp  --fixtures <dir> --out <file.msp>
      Writes one multi-record MSP from the committed public per-rule fixture spectra
      (tests/Common/CommonStandardTests/Resources/DiagnosticRuleFixtures/rules/*.json): every case of every rule,
      in file and case order. PRECURSORTYPE is the adduct of the case's constructed hypothesis when the description
      names one (else the polarity's protonated or deprotonated form); COLLISIONENERGY cycles through a few spellings
      (and one absent record in five) so every collision-energy bin is exercised.
  run        --console <MsfinderConsoleApp(.exe|.dll)> --input <msp|folder> --method <ini> --out <dir>
      Runs `diagrules` once and prints the summary of the manifest it wrote.
  summarize  --manifest <Diagrules run-*.json>
      Prints, per pass, the manifest counts and the evidence status stratified by instrument class and
      collision-energy bin (the standing decision for every measurement). It reads the evidence files named by the
      manifest from the manifest's folder and writes nothing.

Do not use `build-msp` on anything but public fixtures. Outputs of a VS21 input are named *.local-vs21.* by the
console and must never be committed.
"""
import argparse
import collections
import json
import os
import re
import subprocess
import sys

ENERGIES = ["35", "20 eV", "NCE=45 %", "10 V", None]
HYPOTHESIS_ADDUCT = re.compile(r"h\* = [^,;]*\|(\[[^\]]+\][0-9]*[+-])")


def build_msp(fixtures, out):
    names = sorted(n for n in os.listdir(fixtures) if n.endswith(".json"))
    if os.path.exists(out):
        sys.exit("refusing to overwrite " + os.path.basename(out))
    n = 0
    lines = []
    for name in names:
        with open(os.path.join(fixtures, name), encoding="utf-8") as f:
            doc = json.load(f)
        rule_id = name[:-5]
        for case in doc["cases"]:
            q = case["input"]
            positive = q["ion_mode"] == "pos"
            m = HYPOTHESIS_ADDUCT.search(case.get("description", ""))
            adduct = m.group(1) if m else ("[M+H]+" if positive else "[M-H]-")
            lines.append("NAME: %s/%s" % (rule_id, case["name"]))
            lines.append("PRECURSORMZ: " + q["precursor_mz"])
            lines.append("PRECURSORTYPE: " + adduct)
            lines.append("IONMODE: " + ("Positive" if positive else "Negative"))
            if q.get("instrument_type"):
                lines.append("INSTRUMENTTYPE: " + q["instrument_type"])
            energy = ENERGIES[n % len(ENERGIES)]
            if energy is not None:
                lines.append("COLLISIONENERGY: " + energy)
            lines.append("SPECTRUMTYPE: Centroid")
            lines.append("Num Peaks: %d" % len(q["peaks"]))
            lines.extend("%s\t%s" % (mz, intensity) for mz, intensity in q["peaks"])
            lines.append("")
            n += 1
    with open(out, "x", encoding="ascii", newline="\r\n") as f:
        f.write("\n".join(lines) + "\n")
    print("wrote %d records to %s" % (n, os.path.basename(out)))


def console_command(console):
    return ["dotnet", console] if console.lower().endswith(".dll") else [console]


def summarize(manifest_path):
    folder = os.path.dirname(os.path.abspath(manifest_path))
    with open(manifest_path, encoding="ascii") as f:
        manifest = json.load(f)
    print("rule set %s (%s), engine %s, records %d, local_vs21 %s" % (
        manifest["rule_set"]["version"], manifest["rule_set"]["sha256"][:12], manifest["engine_semantics"],
        manifest["reader"]["records"], manifest["local_vs21"]))
    print("collision_energy_bins " + json.dumps(manifest["reader"]["collision_energy_bins"], sort_keys=True))
    for pass_name in sorted(manifest["passes"]):
        p = manifest["passes"][pass_name]
        c = p["counts"]
        print("pass %-20s queries %4d evaluated %4d fired %4d veto_blocked %3d none %4d skipped %4d errors %d refused %d conflicts %d" % (
            pass_name, c["queries"], c["evaluated"], c["fired"], c["veto_blocked"], c["none"], c["skipped"], c["errors"],
            c["refused"], c["conflicts"]))
        if c["skipped_by_reason"]:
            print("    skipped_by_reason " + json.dumps(c["skipped_by_reason"], sort_keys=True))
        strata = collections.Counter()
        with open(os.path.join(folder, p["evidence"]), encoding="ascii") as f:
            for line in f:
                e = json.loads(line)
                r = e["result"]
                fired = r is not None and any(o.get("fired") for o in r.get("outcomes", []))
                outcome = "fired" if fired else (e["status"] if r is None else "not_fired")
                klass = (r or {}).get("instrument_class") or "-"
                strata[(klass, e["collision_energy_bin"], outcome)] += 1
        for (klass, energy, outcome), k in sorted(strata.items()):
            print("    %-12s %-18s %-36s %d" % (klass, energy, outcome, k))


def run(console, input_path, method, out):
    os.makedirs(out, exist_ok=True)
    before = set(os.listdir(out))
    proc = subprocess.run(console_command(console) + ["diagrules", "-i", input_path, "-m", method, "-o", out],
                          capture_output=True, text=True)
    sys.stdout.write(proc.stdout)
    if proc.returncode != 0:
        sys.exit("diagrules exited with %d" % proc.returncode)
    manifests = sorted(n for n in set(os.listdir(out)) - before if n.startswith("Diagrules run-"))
    if len(manifests) != 1:
        sys.exit("expected one new run manifest, found %d" % len(manifests))
    summarize(os.path.join(out, manifests[0]))


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = ap.add_subparsers(dest="cmd", required=True)
    b = sub.add_parser("build-msp")
    b.add_argument("--fixtures", required=True)
    b.add_argument("--out", required=True)
    r = sub.add_parser("run")
    r.add_argument("--console", required=True)
    r.add_argument("--input", required=True)
    r.add_argument("--method", required=True)
    r.add_argument("--out", required=True)
    s = sub.add_parser("summarize")
    s.add_argument("--manifest", required=True)
    a = ap.parse_args()
    if a.cmd == "build-msp":
        build_msp(a.fixtures, a.out)
    elif a.cmd == "run":
        run(a.console, a.input, a.method, a.out)
    else:
        summarize(a.manifest)


if __name__ == "__main__":
    main()
