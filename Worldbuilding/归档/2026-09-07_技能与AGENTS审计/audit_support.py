"""Read-only inventory and reversible, hash-guarded text edits for this audit."""
from pathlib import Path
import hashlib
import json
import re
import shutil

ROOT = Path('E:/数据库/OCC_Codex')
OUT = Path(__file__).resolve().parent
ROOTS = {
    'personal': Path('C:/Users/FNHF/.codex/skills'),
    'global_lark': Path('C:/Users/FNHF/.agents/skills'),
    'project_lark': ROOT / '.agents/skills',
    'plugin_cache': Path('C:/Users/FNHF/.codex/plugins/cache'),
}
AGENTS = [Path('C:/Users/FNHF/.codex/AGENTS.md'), ROOT / 'AGENTS.md']

def sha(data):
    return hashlib.sha256(data).hexdigest()

def read(path):
    return Path(path).read_text(encoding='utf-8-sig')

def save_json(name, obj):
    (OUT / name).write_text(json.dumps(obj, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

def inventory():
    result = []
    for group, root in ROOTS.items():
        for entry in sorted(root.rglob('SKILL.md')):
            raw = entry.read_bytes()
            body = raw.decode('utf-8-sig')
            refs = list(entry.parent.rglob('*.md'))
            result.append(dict(group='system' if '.system' in entry.parts else group,
                               path=entry.as_posix(), sha256=sha(raw),
                               chars=len(body), lines=len(body.splitlines()),
                               markdown_files=len(refs)))
    for entry in AGENTS:
        raw = entry.read_bytes()
        result.append(dict(group='agents', path=entry.as_posix(), sha256=sha(raw),
                           chars=len(raw.decode('utf-8-sig')), lines=len(read(entry).splitlines())))
    return result

def edit(path, content, reason):
    path = Path(path)
    new = content.replace('\r\n', '\n').encode('utf-8')
    old = path.read_bytes() if path.exists() else None
    if old == new:
        return
    manifest_path = OUT / 'changes.json'
    changes = json.loads(read(manifest_path)) if manifest_path.exists() else []
    existing = next((x for x in changes if x['path'] == path.as_posix()), None)
    if existing is None:
        backup = OUT / 'before' / (path.drive.replace(':', '') or 'root') / Path(*path.parts[1:])
        backup = backup.with_name(backup.name + '.bak')
        if old is not None:
            backup.parent.mkdir(parents=True, exist_ok=True)
            backup.write_bytes(old)
        existing = dict(path=path.as_posix(), before_sha256=sha(old) if old is not None else None,
                        backup=backup.as_posix() if old is not None else None, reasons=[])
        changes.append(existing)
    if reason not in existing['reasons']:
        existing['reasons'].append(reason)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(new)
    existing['after_sha256'] = sha(new)
    save_json('changes.json', changes)

def scan_links():
    missing = []
    for group, root in ROOTS.items():
        for p in sorted(root.rglob('*.md')):
            # Ignore examples within fenced code; retain real relative document links.
            body = re.sub(r'(?ms)^```.*?^```[^\n]*', '', read(p))
            for match in re.finditer(r'\]\(([^)\n]+)\)', body):
                link = match.group(1).strip().split(' "')[0].strip('<>')
                if re.match(r'^[a-zA-Z]+:|^#', link) or any(c in link for c in '*{}$<>'):
                    continue
                target = link.split('#')[0]
                if target and not (p.parent / target).exists():
                    missing.append(dict(group=group, source=p.as_posix(), target=link))
    return missing

if __name__ == '__main__':
    import sys
    stage = sys.argv[1] if len(sys.argv) > 1 else 'before'
    items = inventory()
    save_json('inventory-' + stage + '.json', items)
    missing = scan_links()
    save_json('links-' + stage + '.json', missing)
    from collections import Counter
    print(json.dumps({'skills_and_agents': dict(Counter(x['group'] for x in items)),
                      'missing_links': dict(Counter(x['group'] for x in missing))}, ensure_ascii=False))
