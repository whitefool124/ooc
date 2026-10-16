from pathlib import Path
from PIL import Image

folder = Path(__file__).parent
selection_times = [float(v) for v in (folder / 'position-times.txt').read_text().splitlines()]
commit_times = [float(v.split()[0]) for v in (folder / 'motion.txt').read_text().splitlines()]
frames, durations = [], []

def add(name, duration):
    frames.append(Image.open(folder / name).convert('RGB'))
    durations.append(max(20, round(duration / 10) * 10))

add('empty.png', 700)
for i, t in enumerate(selection_times):
    add(f'position-{i:02}.png', (selection_times[i+1]-t)*1000 if i+1<len(selection_times) else 80)
add('selected.png', 550)
for i, t in enumerate(commit_times):
    add(f'frame-{i:02}.png', (commit_times[i+1]-t)*1000 if i+1<len(commit_times) else 700)

for name, crop in [('detail-preview.gif', (72,238,424,495)), ('full-preview.gif', None)]:
    images = [(f.crop(crop).resize((704,514),Image.Resampling.NEAREST) if crop else f).quantize(colors=256,dither=Image.Dither.NONE) for f in frames]
    images[0].save(folder/name,save_all=True,append_images=images[1:],duration=durations,loop=0,disposal=2)
print('Preview: actual captured timing; static pauses added only at empty/selected/end. Detail is screenshot crop at 2x nearest.')
