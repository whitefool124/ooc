"""Read-only subject-grid diagnostic for full-bleed opaque ground sources."""
import sys, json
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(root/'Tools/OCCArt'))
from decode_generated_pixel_grid import infer_subject_lattice, decode_subject_lattice
for path in sorted((Path(__file__).resolve().parents[1]/'raw').glob(sys.argv[1] if len(sys.argv)>1 else '*.png')):
    im=Image.open(path).convert('RGBA')
    pitch,x,y,confidence,bounds=infer_subject_lattice(im)
    decoded=decode_subject_lattice(im,bounds,pitch,x,y)
    print(json.dumps(dict(source=path.name,pitch=pitch,phase=[x,y],confidence=round(confidence,4),decoded=list(decoded.size),source_opaque=im.getchannel('A').getextrema()==(255,255))),flush=True)
