"""Windows/net48 cross-version persistence test against separately built assemblies.

The legacy directory must contain unmodified upstream Core/Common assemblies and
MessagePack 1.7.3.7; the current directory must contain the compact-write change.
"""
import argparse
from pathlib import Path
import shutil
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--current-bins', required=True, type=Path)
    parser.add_argument('--legacy-bins', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    project = Path(__file__).with_name('LegacyLibraryCompatibility.csproj')
    executables = {}
    for name, bins in [('current', args.current_bins), ('legacy', args.legacy_bins)]:
        destination = output / name
        subprocess.run(['dotnet', 'build', str(project), '-t:Rebuild', '-c', 'Release',
                        '-p:LibraryAssemblyDirectory=' + str(bins.resolve()),
                        '-o', str(destination)], check=True)
        for dll in bins.glob('*.dll'):
            shutil.copy2(dll, destination / dll.name)
        executables[name] = destination / 'LegacyLibraryCompatibility.exe'

    def run(version, *arguments):
        result = subprocess.run([str(executables[version]), *map(str, arguments)],
                                capture_output=True, text=True)
        print(result.stdout, end='')
        if result.returncode:
            raise RuntimeError(result.stderr)
        if version == 'legacy' and 'Version=1.7.3.7,' not in result.stdout:
            raise RuntimeError('The historical process did not load MessagePack 1.7.3.7')
        return result.stdout

    current, legacy = output / 'new', output / 'old-resave'
    logs = [run('current', 'write', current),
            run('current', 'verify', current),
            run('legacy', 'verify', current, legacy),
            run('current', 'verify', legacy)]
    (output / 'results.txt').write_text('\n'.join(logs), encoding='utf-8')


if __name__ == '__main__':
    main()
