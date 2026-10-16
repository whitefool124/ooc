from pathlib import Path
from PIL import Image
import numpy as np
import re,json

p=Path(__file__).parent
motion=(p/'motion.txt').read_text(encoding='utf-8')
coordinates=re.findall(r'\(([-\d.]+), ([-\d.]+), ([-\d.]+)\)',motion.split('rect=')[1])
x,y=map(float,coordinates[0][:2]); x2,y2=map(float,coordinates[2][:2])
originals=[Image.open(p/f'frame-{i:02}.png').convert('RGB') for i in range(28)]
w,h=originals[0].size
rect=(int(x),h-int(y2),int(x2),h-int(y))
times=[float(line.split()[0]) for line in (p/'times.txt').read_text().splitlines()]
active=[i for i,line in enumerate((p/'times.txt').read_text().splitlines()) if 'stops=1' in line]
clean=np.array(originals[-1]);scale=(x2-x)/32
# Exclude health bars and tile rims; look only at ground beside the feet.
foot=(int(x+8*scale),h-int(y+12*scale),int(x+24*scale),h-int(y+scale))
left,top,right,bottom=foot
counts=[]
for i in active:
    delta=np.any(np.array(originals[i])[top:bottom,left:right]!=clean[top:bottom,left:right],axis=2)
    counts.append(int(delta.sum()))
assert active and max(counts)>0, 'No visible change at foot contact'
crops=[im.crop((rect[0],rect[1]-int(24*scale),rect[2],rect[3])).resize((384,672),Image.Resampling.NEAREST) for im in originals]
durations=[max(20,round((times[i+1]-times[i])*1000)) for i in range(27)]+[350]
crops[0].save(p/'stone-stop-runtime.gif',save_all=True,append_images=crops[1:],duration=durations,loop=1)
originals[active[len(active)//2]].save(p/'runtime-context.png')
data={'screen':[w,h],'cell_rect':rect,'foot_roi':foot,'active_frames':active,'changed_foot_pixels':counts,'captured_seconds':times[-1]-times[0],'note':'Natural active-vs-expired actual screen pixels; not an isolated dust-only comparison. GIF is a nearest-neighbor crop of actual runtime frames.'}
(p/'pixel-evidence.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(data,ensure_ascii=False))
