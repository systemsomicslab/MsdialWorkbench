"""Run identical bounded synthetic workloads in fresh, alternating processes.

Requires Python 3, Windows and prebuilt optimized x64 net48 benchmark executables.
No private data, parallel workers, search cache or UI timing is involved.
"""
import argparse
import json
from pathlib import Path
import statistics
import subprocess


def generate(path, fractional, records):
    with path.open('w', encoding='ascii', newline='\n') as output:
        for record in range(records):
            output.write(f'NAME: Reference-{record}\nPRECURSORMZ: 500\nIONMODE: Positive\n'
                         'PRECURSORTYPE: [M+H]+\nFORMULA: C20H40\nRETENTIONTIME: 2\nNum Peaks: 64\n')
            for peak in range(64):
                mass = 50 + peak * 5 + record % 7
                intensity = 10 + (peak * 37 + record % 997) % 990
                if fractional:
                    output.write(f'{mass}.123456789 {intensity}.375\n')
                else:
                    output.write(f'{mass} {intensity}\n')
            output.write('\n')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', required=True, type=Path)
    parser.add_argument('--candidate', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--records', default=30000, type=int)
    parser.add_argument('--pairs', default=3, type=int)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    rows = []
    for mode in ('integer', 'fractional'):
        source = args.output / (mode + '.msp')
        generate(source, mode == 'fractional', args.records)
        for pair in range(args.pairs):
            order = ('baseline', 'candidate') if pair % 2 == 0 else ('candidate', 'baseline')
            for version in order:
                stem = args.output / f'{mode}-{pair + 1}-{version}'
                report = stem.with_suffix('.json')
                print(f'Running {stem.name}', flush=True)
                with stem.with_suffix('.log').open('w', encoding='utf-8') as log:
                    subprocess.run([str(getattr(args, version).resolve()), mode, str(source.resolve()),
                                    str(stem.with_suffix('.bin').resolve()), str(report.resolve())],
                                   stdout=log, stderr=subprocess.STDOUT, check=True)
                row = json.loads(report.read_text())
                row.update(mode=mode, pair=pair + 1, version=version)
                rows.append(row)
                print(f"  retained={row['retainedManagedBytes'] / 1048576:.2f} MiB, "
                      f"import={row['importMs']:.1f} ms, save={row['saveMs']:.1f} ms", flush=True)
                # Retain reports and logs, not repeated large intermediate archives.
                stem.with_suffix('.bin').unlink()
        selected = [row for row in rows if row['mode'] == mode]
        for field in ('inputSha256', 'archiveSha256', 'archiveBytes', 'eiResultSha256', 'lcResultSha256'):
            if len({row[field] for row in selected}) != 1:
                raise RuntimeError(f'{mode}: {field} differs between runs or versions')
        for row in selected:
            expected = args.records if row['version'] == 'candidate' else 0
            for field in ('compactAfterImport', 'compactAfterScoring', 'compactAfterSave', 'compactAfterLoad'):
                if row[field] != expected:
                    raise RuntimeError(f'{mode}: unexpected expansion in {field}: {row[field]}')
    (args.output / 'results.json').write_text(json.dumps(rows, indent=2) + '\n')
    for mode in ('integer', 'fractional'):
        print(mode)
        for metric in ('retainedManagedBytes', 'loadedRetainedManagedBytes', 'importMs', 'saveMs', 'loadMs',
                       'eiPassMs', 'lcPassMs', 'processPeakWorkingSetBytes'):
            values = []
            for version in ('baseline', 'candidate'):
                sample = [row[metric] for row in rows if row['mode'] == mode and row['version'] == version]
                # First full pass is reported separately in the raw data; summarize two warmed passes.
                values.append(statistics.median(statistics.median(item[1:]) if isinstance(item, list) else item for item in sample))
            print(f'  {metric}: {values[0]:.3f} -> {values[1]:.3f} ({(values[1] / values[0] - 1) * 100:+.1f}%)')


if __name__ == '__main__':
    main()
