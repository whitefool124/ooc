"""Retain verified Kenney CC0 packs outside Unity; never overwrite originals."""
from pathlib import Path
import concurrent.futures, hashlib, json, urllib.request, zipfile
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'ArtSource/Audio/FoleyExpansion20261003'
PACKS=[('rpg','https://kenney.nl/assets/rpg-audio','https://kenney.nl/media/pages/assets/rpg-audio/8e99002d76-1677590336/kenney_rpg-audio.zip'),('impact','https://kenney.nl/assets/impact-sounds','https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip')]
def fetch(pack):
 name,page,url=pack;DEST.mkdir(parents=True,exist_ok=True);archive=DEST/(name+'.zip')
 if not archive.exists():
  req=urllib.request.Request(url,headers={'User-Agent':'OCC audio asset research'})
  data=urllib.request.urlopen(req,timeout=45).read()
  if not data.startswith(b'PK'):raise ValueError('Not a ZIP: '+url)
  archive.write_bytes(data)
 target=(DEST/name).resolve()
 with zipfile.ZipFile(archive) as z:
  for f in z.infolist():
   if not (target/f.filename).resolve().is_relative_to(target):raise ValueError('Unsafe member')
  z.extractall(target)
 return dict(author='Kenney',license='CC0-1.0',source=page,download=url,path=archive.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(archive.read_bytes()).hexdigest())
if __name__=='__main__':
 with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:records=list(pool.map(fetch,PACKS))
 (DEST/'sources.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
 for p in sorted(DEST.rglob('*.ogg')):print(p.relative_to(DEST))
 extra=[]
 for n in [1,2]:
  url=f'https://opengameart.org/sites/default/files/splash{n}_0.wav';out=DEST/f'splash{n}.wav'
  if not out.exists():out.write_bytes(urllib.request.urlopen(url,timeout=45).read())
  extra.append(dict(author='Peludo',license='CC0-1.0',source='https://opengameart.org/content/water-splash-and-sand-footsteps',author_project='https://rnan.itch.io/',download=url,path=out.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(out.read_bytes()).hexdigest()))
 (DEST/'sources.json').write_text(json.dumps(records+extra,indent=2),encoding='utf-8')
