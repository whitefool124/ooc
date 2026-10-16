from pathlib import Path
import difflib
import hashlib
import json

folder = Path(__file__).resolve().parent
root = folder.parents[2]
files = []
patch = []
for baseline in sorted((folder / 'before').rglob('*')):
    if not baseline.is_file() or baseline.name in ('git-status.txt', 'head.txt'):
        continue
    relative = baseline.relative_to(folder / 'before')
    if len(relative.parts) == 1 and relative.suffix == '.cs':
        relative = Path('UnityProject/Assets/Game/Runtime/Presentation') / relative
    current = root / relative
    if not current.exists():
        raise RuntimeError(str(current))
    old = baseline.read_text(encoding='utf-8-sig').splitlines(keepends=True)
    new = current.read_text(encoding='utf-8-sig').splitlines(keepends=True)
    if old == new:
        continue
    patch.extend(difflib.unified_diff(old, new, fromfile='before/' + relative.as_posix(), tofile=relative.as_posix()))
    files.append({'path': relative.as_posix(), 'baseline': baseline.relative_to(folder).as_posix(), 'sha256': hashlib.sha256(current.read_bytes()).hexdigest()})
for relative in (
    'UnityProject/Assets/Game/Runtime/Presentation/CombatTimelineDrawer.cs',
    'UnityProject/Assets/Game/Runtime/Campaign/CombatActionOrderPresentation.cs',
    'UnityProject/Assets/Game/Tests/EditMode/CombatActionOrderPresentationTests.cs',
    'UnityProject/Assets/Game/Editor/CombatHudRedesignVerification.cs',
):
    current = root / relative
    patch.extend(difflib.unified_diff([], current.read_text(encoding='utf-8-sig').splitlines(keepends=True), fromfile='/dev/null', tofile=relative))
    files.append({'path': relative, 'new': True, 'sha256': hashlib.sha256(current.read_bytes()).hexdigest()})
(folder / 'implementation.patch').write_text(''.join(patch), encoding='utf-8')
(folder / 'changed-files.json').write_text(json.dumps(files, ensure_ascii=False, indent=2), encoding='utf-8')
print('Archived incremental patch and hashes:', len(files), 'files')
