"""Run portable source/asset and .NET rule checks; this does not launch Unity."""
from pathlib import Path
import argparse
import shutil
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default='dotnet', help='Path to a .NET 8 SDK dotnet host')
    args = parser.parse_args()
    dotnet = shutil.which(args.dotnet)
    if dotnet is None:
        parser.error('A .NET 8 SDK is required. Install it or pass --dotnet /path/to/dotnet.')
    dotnet = str(Path(dotnet).resolve())
    root = Path(__file__).resolve().parents[1]
    sdk = subprocess.run([dotnet, '--list-sdks'], check=True, text=True, capture_output=True)
    if not any(line.startswith('8.') for line in sdk.stdout.splitlines()):
        parser.error('The selected dotnet host needs an installed .NET 8 SDK; a runtime alone cannot build these projects.')
    subprocess.run([sys.executable, str(root / 'Tools/validate_project.py')], cwd=root, check=True)
    projects = sorted((root / 'Tools').glob('*Checks.csproj'))
    if not projects:
        raise RuntimeError('No rule check projects found in Tools.')
    for project in projects:
        print('\nRUN:', project.name, flush=True)
        subprocess.run([dotnet, 'run', '--project', str(project), '--configuration', 'Release', '--no-launch-profile'], cwd=root, check=True)
    print('\nPASS: source/assets and', len(projects), 'rule suites completed.')
    print('Unity import, physics, rendering, native smoke checks, FPS and a full playthrough were not run.')


if __name__ == '__main__':
    main()
