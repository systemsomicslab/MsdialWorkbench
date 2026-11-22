"""Runtime measurement of MS-FINDER diagnostic-ion rule evaluation (WS8-P3 design 5.8).

Standard library only. It runs `predict` twice on identical inputs, sequentially: once with a method file whose
DiagnosticRuleEvaluation is off and once with one where it is on (the two files must differ only in that key; the
script checks it). Each run gets BelowNormal priority and an affinity of --cores logical processors (Windows), and
the launcher records the process wall and CPU time. From the "on" run's "Diagnostic rule timing-*.tsv" it reports
per query the median, p95 and maximum of eval, match and eval+match ms, their ratio to the formula + structure step
time, and the fire rates from its run manifest. The added CPU per spectrum is (cpu_on - cpu_off) / n.

For clean per-spectrum values run it again with --cores 1 on a subsample (about 200 spectra) the operator prepares;
the script never copies or selects input files itself.

  python diagrules_runtime.py --console <MsfinderConsoleApp(.exe|.dll)> --input <folder> \
      --method-off <off.ini> --method-on <on.ini> --out <dir> [--cores 8]

It writes "Diagrules runtime-<stamp>.json" (".local-vs21" before the extension when the input or a file in it is
named vs21) into --out, with the off and on outputs in --out/off-<stamp> and --out/on-<stamp>. Nothing so named
.local-vs21 may be committed. The bound itself is pre-registered elsewhere (design Q10); this script only measures.
"""
import argparse
import csv
import json
import os
import subprocess
import sys
import time

BELOW_NORMAL_PRIORITY_CLASS = 0x00004000


def ini_pairs(path):
    pairs = {}
    with open(path, encoding="utf-8-sig") as f:
        for line in f:
            if "=" in line and not line.lstrip().startswith("#"):
                k, v = line.split("=", 1)
                pairs[k.strip()] = v.strip()
    return pairs


def check_methods(off, on):
    a, b = ini_pairs(off), ini_pairs(on)
    key = "DiagnosticRuleEvaluation"
    if a.get(key, "FALSE").upper() == "TRUE" or b.get(key, "").upper() != "TRUE":
        sys.exit("the off method must have %s off and the on method must have it TRUE" % key)
    differ = sorted(k for k in set(a) | set(b) if k != key and a.get(k) != b.get(k))
    if differ:
        sys.exit("the method files differ in more than %s: %s" % (key, ", ".join(differ)))


def cpu_seconds_windows(handle):
    import ctypes
    from ctypes import wintypes
    times = [wintypes.FILETIME() for _ in range(4)]
    if not ctypes.windll.kernel32.GetProcessTimes(wintypes.HANDLE(handle), *[ctypes.byref(t) for t in times]):
        return None

    def sec(ft):
        return ((ft.dwHighDateTime << 32) | ft.dwLowDateTime) / 1e7
    return sec(times[2]) + sec(times[3])


def launch(command, cores):
    windows = os.name == "nt"
    flags = BELOW_NORMAL_PRIORITY_CLASS if windows else 0
    start = time.perf_counter()
    if not windows:
        import resource
        before = resource.getrusage(resource.RUSAGE_CHILDREN)
    proc = subprocess.Popen(command, creationflags=flags, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    if windows:
        import ctypes
        ctypes.windll.kernel32.SetProcessAffinityMask(int(proc._handle), (1 << cores) - 1)
    else:
        os.sched_setaffinity(proc.pid, set(range(cores)))
        os.setpriority(os.PRIO_PROCESS, proc.pid, 10)
    output, _ = proc.communicate()
    wall = time.perf_counter() - start
    if windows:
        cpu = cpu_seconds_windows(int(proc._handle))
    else:
        after = resource.getrusage(resource.RUSAGE_CHILDREN)
        cpu = (after.ru_utime - before.ru_utime) + (after.ru_stime - before.ru_stime)
    if proc.returncode != 0:
        sys.stdout.write(output)
        sys.exit("predict exited with %d" % proc.returncode)
    return wall, cpu


def quantiles(values):
    if not values:
        return None
    v = sorted(values)

    def q(p):  # nearest rank
        return v[max(0, min(len(v) - 1, int(-(-p * len(v) // 1)) - 1))]
    return {"n": len(v), "median": q(0.5), "p95": q(0.95), "max": v[-1]}


def only(folder, prefix):
    names = [n for n in os.listdir(folder) if n.startswith(prefix)]
    if len(names) != 1:
        sys.exit("expected one %s* file in the on run, found %d" % (prefix, len(names)))
    return os.path.join(folder, names[0])


def is_vs21(path):
    named = [os.path.basename(os.path.normpath(path))]
    if os.path.isdir(path):
        named += os.listdir(path)
    return any("vs21" in n.lower() for n in named)


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--console", required=True)
    ap.add_argument("--input", required=True)
    ap.add_argument("--method-off", required=True)
    ap.add_argument("--method-on", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--cores", type=int, default=8)
    a = ap.parse_args()
    check_methods(a.method_off, a.method_on)
    stamp = time.strftime("%Y%m%d%H%M%S")
    suffix = ".local-vs21" if is_vs21(a.input) else ""
    summary_path = os.path.join(a.out, "Diagrules runtime-%s%s.json" % (stamp, suffix))
    off_dir, on_dir = os.path.join(a.out, "off-" + stamp), os.path.join(a.out, "on-" + stamp)
    for p in (summary_path, off_dir, on_dir):
        if os.path.exists(p):
            sys.exit("error:output_exists:" + os.path.basename(p))
    os.makedirs(off_dir)
    os.makedirs(on_dir)
    base = ["dotnet", a.console] if a.console.lower().endswith(".dll") else [a.console]
    off_wall, off_cpu = launch(base + ["predict", "-i", a.input, "-m", a.method_off, "-o", off_dir], a.cores)
    on_wall, on_cpu = launch(base + ["predict", "-i", a.input, "-m", a.method_on, "-o", on_dir], a.cores)

    rows = []
    with open(only(on_dir, "Diagnostic rule timing-"), encoding="ascii", newline="") as f:
        for row in csv.DictReader(f, delimiter="\t"):
            rows.append({k: float(row[k]) for k in ("Eval ms", "Match ms", "Formula step ms", "Structure step ms")})
    with open(only(on_dir, "Diagnostic rule run-"), encoding="ascii") as f:
        counts = json.load(f).get("counts", {})
    n = len(rows)
    added = [r["Eval ms"] + r["Match ms"] for r in rows]
    steps = [r["Formula step ms"] + r["Structure step ms"] for r in rows]
    summary = {
        "cores": a.cores,
        "counts": counts,
        "fire_rate": (counts.get("fired", 0) / counts["evaluated"]) if counts.get("evaluated") else None,
        "in_run_ms": {
            "eval": quantiles([r["Eval ms"] for r in rows]),
            "match": quantiles([r["Match ms"] for r in rows]),
            "eval_plus_match": quantiles(added),
            "ratio_to_formula_plus_structure": (sum(added) / sum(steps)) if sum(steps) > 0 else None,
        },
        "launcher": {
            "off": {"cpu_s": off_cpu, "wall_s": off_wall},
            "on": {"cpu_s": on_cpu, "wall_s": on_wall},
            "added_cpu_ms_per_spectrum": ((on_cpu - off_cpu) * 1000.0 / n) if n and off_cpu is not None and on_cpu is not None else None,
        },
        "queries": n,
        "stamp": stamp,
    }
    with open(summary_path, "x", encoding="ascii", newline="\n") as f:
        json.dump(summary, f, indent=1, sort_keys=True)
        f.write("\n")
    print(json.dumps(summary, indent=1, sort_keys=True))


if __name__ == "__main__":
    main()
