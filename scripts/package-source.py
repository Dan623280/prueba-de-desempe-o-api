"""Create the assessment source archive without local secrets, caches or build output."""
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parents[1]
output = root / 'artifacts/SkillHub-source.zip'
output.parent.mkdir(exist_ok=True)
folders = ['src', 'tests', 'scripts', 'docs', 'data']
files = [root / name for name in ['README.md', 'SkillHub.sln', 'Directory.Build.props', 'global.json', 'dotnet-tools.json', 'compose.yaml', '.editorconfig', '.env.example', '.gitignore']]
excluded = {'bin', 'obj', 'node_modules', '__pycache__'}
for folder in folders:
    files.extend(path for path in (root / folder).rglob('*') if path.is_file() and not excluded.intersection(path.relative_to(root).parts))
with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
    for path in sorted(files):
        archive.write(path, Path('SkillHub') / path.relative_to(root))
with zipfile.ZipFile(output) as archive:
    assert archive.testzip() is None
    assert not any(Path(name).name == '.env' for name in archive.namelist())
print(f'Created {output.name}: {len(files)} files, {output.stat().st_size:,} bytes; local secrets excluded.')
