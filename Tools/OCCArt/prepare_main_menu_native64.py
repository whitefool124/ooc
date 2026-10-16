"""E menu: diagnose native grids, preserve subjects, assemble at one pixel density.

No arbitrary downscale or geometry resize. Integer previews are review-only.
Community reconstructions are retained separately for comparison, never called lossless.
"""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image, ImageFilter
import decode_generated_pixel_grid as decoder

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'ArtSource/MainMenuE_Native64_20261008'

def background_grid(source):
    # The old isolated-object detector searches 8..40px. A wide plate can have
    # smaller authored cells; diagnose 4..40 here without lowering confidence.
    rgb = np.asarray(source.convert('RGB').filter(ImageFilter.GaussianBlur(1.4)), dtype=np.float32)
    profiles = [np.abs(np.diff(rgb, axis=1)).sum(axis=(0,2)), np.abs(np.diff(rgb, axis=0)).sum(axis=(1,2))]
    candidates = []
    for pitch in range(4, 41):
        sx, px, cx = decoder.edge_profile_score(profiles[0], pitch)
        sy, py, cy = decoder.edge_profile_score(profiles[1], pitch)
        candidates.append((sx*sy*min(cx,cy)**.15, pitch, px, py))
    candidates.sort(reverse=True)
    score,pitch,px,py = candidates[0]
    runner = next(v for v in candidates[1:] if v[1] not in {pitch*2,pitch//2})
    confidence = score/max(runner[0], 1e-9)
    report = dict(pitch=pitch,phase=[px,py],confidence=confidence,threshold=1.2,candidates=candidates[:8],geometric_resize=False)
    if confidence < 1.2:
        return None, report
    result = decoder.decode_subject_lattice(source,(0,0,source.width,source.height),pitch,px,py)
    return decoder.consolidate_to_observed_palette(result,32), report

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def evidence(folder, image):
    image.save(folder/'one_x.png')
    image.resize((image.width*4,image.height*4),Image.Resampling.NEAREST).save(folder/'four_x.png')
    image.convert('L').convert('RGB').save(folder/'grayscale.png')
    board=Image.new('RGBA',image.size,(210,210,210,255))
    for y in range(image.height):
        for x in range(image.width):
            if (x//8+y//8)%2: board.putpixel((x,y),(240,240,240,255))
    board.alpha_composite(image);board.save(folder/'checker.png')

def assemble(background):
    width,height = 320,180
    if background.width>width or background.height>height:
        raise ValueError(f'Background does not fit intact scene: {background.size}')
    # A neutral viewport border is not painted source detail.
    plate=Image.new('RGBA',(width,height),(39,31,26,255))
    plate.alpha_composite(background,((width-background.width)//2,(height-background.height)//2))
    def prop(name):
        im=Image.open(ART/name/'native.png').convert('RGBA')
        return im.crop(im.getchannel('A').getbbox())
    positions={'cup':(244,154),'lamp':(289,160),'book_closed':(169,163),'book_open':(169,163)}
    for name in ['cup','lamp']:
        im=prop(name);cx,bottom=positions[name];plate.alpha_composite(im,(cx-im.width//2,bottom-im.height))
    states={}
    for state,book in [('scene_closed','book_closed'),('scene_open','book_open')]:
        im=prop(book);scene=plate.copy();cx,bottom=positions[book];scene.alpha_composite(im,(cx-im.width//2,bottom-im.height));states[state]=scene
    # Common source-observed palette; never invent blended colours or dither.
    strip=Image.new('RGBA',(width,height*2));strip.alpha_composite(states['scene_closed']);strip.alpha_composite(states['scene_open'],(0,height))
    strip=decoder.consolidate_to_observed_palette(strip,64)
    for i,state in enumerate(states):
        folder=ART/state;folder.mkdir(exist_ok=True);im=strip.crop((0,i*height,width,(i+1)*height));im.save(folder/'native.png');evidence(folder,im)
        im.resize((1920,1080),Image.Resampling.NEAREST).save(folder/'six_x.png')
    return positions

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser();parser.add_argument('--background-source');parser.add_argument('--assemble',action='store_true');parser.add_argument('--art-dir',type=Path);args=parser.parse_args()
    if args.art_dir: ART=args.art_dir.resolve()
    if args.background_source:
        source=Image.open(args.background_source).convert('RGBA');image,report=background_grid(source)
        (ART/'background/native-grid-qa.json').write_text(json.dumps(report,indent=2),encoding='utf8');print(json.dumps(report))
        if image is None: raise SystemExit(1)
        image.save(ART/'background/native.png');evidence(ART/'background',image)
    if args.assemble: print(assemble(Image.open(ART/'background/native.png').convert('RGBA')))
