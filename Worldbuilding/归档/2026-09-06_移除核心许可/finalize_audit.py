from pathlib import Path
import difflib
import hashlib
import json
import subprocess

root = Path(__file__).resolve().parents[3]
audit = Path(__file__).resolve().parent
before = audit / 'before'
rows, diffs = [], []
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
for old in sorted(before.rglob('*')):
    if not old.is_file():
        continue
    relative = old.relative_to(before)
    current = root / relative
    if digest(old) == digest(current):
        continue
    rows.append({'path': relative.as_posix(), 'status': 'modified' if current.exists() else 'deleted', 'before_sha256': digest(old), 'after_sha256': digest(current)})
    if old.suffix != '.png':
        diffs.extend(difflib.unified_diff(old.read_text(encoding='utf-8-sig').splitlines(True), current.read_text(encoding='utf-8-sig').splitlines(True) if current.exists() else [], fromfile='before/' + relative.as_posix(), tofile='after/' + relative.as_posix()))
for relative in ('UnityProject/Assets/Game/Runtime/Campaign/AcademyMapSaveMigration.cs', 'UnityProject/Assets/Game/Tests/EditMode/AcademyMapProgressCleanupTests.cs'):
    for suffix in ('', '.meta'):
        path = root / (relative + suffix)
        assert path.is_file(), path
        rows.append({'path': relative + suffix, 'status': 'added', 'before_sha256': None, 'after_sha256': digest(path)})
        diffs.extend(difflib.unified_diff([], path.read_text(encoding='utf-8-sig').splitlines(True), fromfile='/dev/null', tofile='after/' + relative + suffix))
(audit / 'final-changed-files.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding='utf-8')
(audit / 'task-only.diff').write_text(''.join(diffs), encoding='utf-8')
master = json.loads((audit / 'master-after.json').read_text(encoding='utf-8-sig'))['data']['document']
local = (root / 'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md').read_text(encoding='utf-8-sig')
assert local.rstrip() == master['content'].rstrip(), 'Master mirror differs from fetched revision'
args = ['rg', '-n', '--hidden', '--no-ignore', '核心许可|CorePermit|core_permit|权限卡|AccessCard|permit_archive|permit:', '.']
for ext in ('cs', 'ts', 'tsx', 'js', 'json', 'csv', 'md', 'py', 'txt', 'html'):
    args += ['-g', '*.' + ext]
for excluded in ('Worldbuilding/归档/**', 'UnityProject/Library/**', 'UnityProject/Logs/**', 'UnityProject/Temp/**', 'UnityProject/obj/**', '.git/**', '**/node_modules/**', '**/.next/**', '**/dist/**', '**/Builds/**', '**/build/**', '**/TestResults/**', '**/.vs/**'):
    args += ['-g', '!' + excluded]
scan = subprocess.run(args, cwd=root, capture_output=True, text=True, encoding='utf-8')
assert scan.returncode in (0, 1), scan.stderr
(audit / 'final-remaining-locations.txt').write_text(scan.stdout, encoding='utf-8')
assert all('AcademyMapSaveMigration.cs:' in row or 'AcademyMapProgressCleanupTests.cs:' in row for row in scan.stdout.splitlines()), scan.stdout
print(json.dumps({'changed_files': len(rows), 'master_revision': master['revision_id'], 'mirror_matches': True, 'remaining_files': 2}, ensure_ascii=False))
