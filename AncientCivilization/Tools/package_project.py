"""Package the Unity source and retained art references; this does not build a player."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import argparse
import hashlib
import re


EXCLUDED = {
    '.git', '.vs', 'Library', 'Temp', 'Obj', 'obj', 'bin', 'Logs',
    'Build', 'Builds', 'UserSettings', '__pycache__', 'work',
}


def main():
    project = Path(__file__).resolve().parents[1]
    repository = project.parent
    settings = (project / 'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8-sig')
    match = re.search(r'^\s*bundleVersion:\s*([\w.\-]+)\s*$', settings, re.MULTILINE)
    if not match:
        raise RuntimeError('ProjectSettings has no bundleVersion.')
    version = match.group(1)
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=repository / f'PLUGIN_v{version}_LowerDistrict_Unity_6000.6.7f1.zip')
    args = parser.parse_args()
    output = args.output.resolve()
    references = repository / 'ArtReferences'
    if not references.is_dir():
        parser.error('Keep ArtReferences beside AncientCivilization before packaging.')
    if output == project or project in output.parents or output == references or references in output.parents:
        parser.error('Write the ZIP outside the two packaged source directories.')
    if output.suffix.lower() != '.zip':
        parser.error('--output must end in .zip.')
    files = []
    for folder in (project, references):
        for path in sorted(folder.rglob('*')):
            relative = path.relative_to(repository)
            if path.is_file() and not path.is_symlink() and not EXCLUDED.intersection(relative.parts):
                if path.name not in ('.DS_Store', 'Thumbs.db'):
                    files.append(path)
    output.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(output, 'w', ZIP_DEFLATED, compresslevel=9) as archive:
        for path in files:
            archive.write(path, path.relative_to(repository).as_posix())
    with ZipFile(output) as archive:
        if archive.testzip() is not None:
            raise RuntimeError('Archive CRC validation failed.')
        if set(archive.namelist()) != {path.relative_to(repository).as_posix() for path in files}:
            raise RuntimeError('Archive file inventory differs from the selected source.')
        for path in files:
            if archive.read(path.relative_to(repository).as_posix()) != path.read_bytes():
                raise RuntimeError(f'Archive differs from source: {path.relative_to(repository)}')
    digest = hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix(output.suffix + '.sha256').write_text(f'{digest}  {output.name}\n', encoding='utf-8')
    print(f'PASS: {len(files)} source/reference files, CRC and byte equality')
    print(f'{output.name}: {output.stat().st_size} bytes')
    print(f'SHA256: {digest}')
    print('Source archive only; Unity import, Play, FPS and player build are not validated by packaging.')


if __name__ == '__main__':
    main()
