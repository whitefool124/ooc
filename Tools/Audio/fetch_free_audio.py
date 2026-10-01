"""Fetch individually verified CC0 audio; retain provenance and hashes."""
from pathlib import Path
import concurrent.futures, hashlib, json, shutil, urllib.request, zipfile

ROOT = Path(__file__).resolve().parents[2]
RUNTIME = ROOT / 'UnityProject/Assets/Resources/Audio/OCC'
LIBRARY = ROOT / 'ArtSource/Audio/FreeLibrary'
BASE = 'https://opengameart.org/'
FILES = [
 ('Music/archive_piano.mp3', 'JRPG Piano', 'Joth', 'jrpg-piano', 'JRPG%20Piano.mp3'),
 ('Music/academy_town.mp3', 'Town Theme RPG', 'cynicmusic', 'town-theme-rpg', 'TownTheme.mp3'),
 ('Music/magic_town.mp3', 'Magic Town', 'controllerhead', 'magic-town', 'Magic%20Town_0.mp3'),
 ('Music/jrpg_practicum.ogg', 'JRPG Trailer / Theme', 'Juhani Junkala', 'jrpg-trailer-theme', 'Juhani%20Junkala%20-%20JRPG%20Theme%20%5BLoop%20Ready%5D_0.ogg'),
 ('Music/boss_exam.ogg', 'Boss Battle Theme', 'Cleyton Kauffman', 'boss-battle-theme', 'CleytonRX%20-%20Battle%20RPG%20Theme%20Var_0.ogg'),
 ('library/80-CC0-RPG-SFX.zip', '80 CC0 RPG SFX', 'rubberduck', '80-cc0-rpg-sfx', '80-CC0-RPG-SFX_0.zip'),
] + [(f'SFX/Free/magical_{n}.ogg', 'Magic Spell SFX', 'JaggedStone', 'magic-spell-sfx', f'magical_{n}{"_0" if n in (1, 6, 7) else ""}.ogg') for n in range(1, 8)]

def fetch(entry):
    relative, title, author, page, filename = entry
    path = LIBRARY / filename if relative.startswith('library/') else RUNTIME / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    url = BASE + 'sites/default/files/' + filename
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({'https': 'http://127.0.0.1:7897'}))
    data = opener.open(url, timeout=90).read()
    if data[:20].lower().startswith(b'<!doctype'): raise ValueError(f'HTML instead of audio: {url}')
    path.write_bytes(data)
    return dict(title=title, author=author, license='CC0-1.0', license_url='https://creativecommons.org/publicdomain/zero/1.0/',
                source=BASE+'content/'+page, download=url, path=path.relative_to(ROOT).as_posix(), bytes=len(data), sha256=hashlib.sha256(data).hexdigest())

if __name__ == '__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        records = list(pool.map(fetch, FILES))
    with zipfile.ZipFile(LIBRARY/'80-CC0-RPG-SFX_0.zip') as archive:
        destination = (LIBRARY/'rubberduck').resolve()
        for info in archive.infolist():
            target = (destination/info.filename).resolve()
            if not target.is_relative_to(destination): raise ValueError('Unsafe archive path')
        archive.extractall(destination)
    for name in ['book_01', 'book_02', 'book_03', 'book_04', 'item_coins_01', 'item_gem_01', 'lock_01', 'metal_01', 'spell_01', 'spell_02', 'spell_fire_01']:
        path = RUNTIME/'SFX/Free'/f'{name}.ogg'
        shutil.copyfile(destination/f'{name}.ogg', path)
        data = path.read_bytes()
        records.append(dict(title='80 CC0 RPG SFX', author='rubberduck', license='CC0-1.0',
            source=BASE+'content/80-cc0-rpg-sfx', archive_member=f'{name}.ogg',
            path=path.relative_to(ROOT).as_posix(), bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
    (ROOT/'ArtSource/Audio/free_audio_sources.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(records, ensure_ascii=False, indent=2))
