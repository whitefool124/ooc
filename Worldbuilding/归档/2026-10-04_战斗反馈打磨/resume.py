from pathlib import Path
p=Path(__file__).with_name('implement.py')
t=p.read_text(encoding='utf-8').replace('p.read_text()', 'p.read_text(encoding="utf-8")')
p.write_text(t,encoding='utf-8')
root=Path(__file__).resolve().parents[3]
s=root/'UnityProject/Assets/Game/Runtime/Presentation/CombatSelectionController.cs'
lines=t.splitlines()
exec('\n'.join(lines[:9]+lines[24:]))
