from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent
source = Image.open(root / 'source_sheet.png').convert('RGB')
# Explicit regions in the generated concept sheet, not a verified native sprite grid.
regions = [(0, 0, 687, 570), (687, 0, 1374, 570),
           (0, 570, 687, 1145), (687, 570, 1374, 1145)]
# Visually identified contact anchors. Last frame follows the dissipating result.
anchors = [(380, 350), (293, 360), (260, 360), (413, 380)]
frames = []
for index, (region, anchor) in enumerate(zip(regions, anchors)):
    cell = source.crop(region)
    frame = Image.new('RGB', (1000, 800), 'white')
    frame.paste(cell, (500 - anchor[0], 400 - anchor[1]))
    frame.save(root / f'concept_frame_{index + 1:02d}.png')
    frames.append(frame)
frames.append(Image.new('RGB', frames[0].size, 'white'))
# Contact stages total 180ms; repeat pause is for inspection only.
frames[0].save(root / 'fire_impact_preview.gif', save_all=True,
               append_images=frames[1:], duration=[30, 40, 50, 60, 900],
               loop=0, disposal=2, optimize=False)
frames[0].save(root / 'fire_impact_slow.gif', save_all=True,
               append_images=frames[1:], duration=[120, 160, 200, 240, 900],
               loop=0, disposal=2, optimize=False)
for name in ['fire_impact_preview.gif', 'fire_impact_slow.gif']:
    with Image.open(root / name) as gif:
        assert gif.n_frames == 5
        assert gif.size == (1000, 800)
        print(name, gif.n_frames, gif.size)
