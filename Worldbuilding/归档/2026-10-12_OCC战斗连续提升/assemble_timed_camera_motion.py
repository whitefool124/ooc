"""Encode diagnostic GIFs from timed, actual Unity Canvas capture frames."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ARCHIVE=Path(__file__).resolve().parent
families=['AcademyCourt','Conservatory','StoneRoad','RuinSite','InteriorHall','AetherTower']
for family in families:
    folder=ARCHIVE/'profiles'/family
    record=json.loads((folder/'timed_motion_capture.json').read_text(encoding='utf-8'))
    bindings=[]
    for resolution in [[1920,1080],[960,540]]:
        rows=[f for f in record['frames'] if f['resolution']==resolution]
        assert len(rows)==8 and [f['index'] for f in rows]==list(range(8))
        timestamps=[f['editorTimestamp'] for f in rows]
        assert all(b>a for a,b in zip(timestamps,timestamps[1:]))
        images=[]
        for row in rows:
            path=folder/row['path'];image=Image.open(path).convert('RGB');assert list(image.size)==resolution
            row['sha256']=hashlib.sha256(path.read_bytes()).hexdigest();images.append(image)
        durations=[max(10,round((b-a)*1000/10)*10) for a,b in zip(timestamps,timestamps[1:])]
        durations.append(durations[-1])
        palette=images[0].quantize(colors=256)
        images=[image.quantize(palette=palette,dither=Image.Dither.NONE) for image in images]
        path=folder/f'timed_camera_motion_{resolution[0]}x{resolution[1]}.gif'
        images[0].save(path,save_all=True,append_images=images[1:],duration=durations,loop=0,optimize=False)
        bindings.append({'resolution':resolution,'path':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),
            'duration_ms':durations,'encoding':'256-colour diagnostic GIF; original RGB PNG frames remain authoritative',
            'fov_range':[min(f['fieldOfView'] for f in rows),max(f['fieldOfView'] for f in rows)]})
    record['recordings']=bindings
    record['scope']='Timed live Editor camera through legal public scroll/drag handlers; real rendered UI frames at recorded wall-clock intervals, isolated preview, not normal input journey or FPS benchmark'
    (folder/'timed_motion_capture.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'families':6,'recordings':12,'actual_png_frames':96,'status':'REVIEW_PENDING'}))
