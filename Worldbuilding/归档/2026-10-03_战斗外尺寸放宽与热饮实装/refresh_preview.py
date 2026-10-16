from pathlib import Path
from PIL import Image
folder=Path(__file__).parent
times=[float(v) for v in (folder/'frame-times.txt').read_text().splitlines()]
frames=[Image.open(folder/f'steam-{i:02}.png').convert('RGB') for i in range(len(times))]
durations=[max(20,round(((times[i+1]-t)*1000 if i+1<len(times) else 80)/10)*10) for i,t in enumerate(times)]
for name,crop in [('detail-preview.gif',(1028,566,1426,676)),('full-preview.gif',None)]:
    imgs=[(f.crop(crop).resize((796,220),Image.Resampling.NEAREST) if crop else f).quantize(colors=256,dither=Image.Dither.NONE) for f in frames]
    imgs[0].save(folder/name,save_all=True,append_images=imgs[1:],duration=durations,loop=0,disposal=2)
