"""Crop regular source sheets and losslessly decode each independent frame."""
import argparse,json,subprocess,sys
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
p=argparse.ArgumentParser();p.add_argument('name');p.add_argument('round');p.add_argument('--sheet',action='store_true');a=p.parse_args()
base=HERE/a.name;source=base/'raw'/f'{a.round}_source.png'
inputs=[source]
if a.sheet:
    im=Image.open(source).convert('RGBA');inputs=[]
    for i in range(6):
        x=i%3;y=i//3
        bounds=(round(x*im.width/3),round(y*im.height/2),round((x+1)*im.width/3),round((y+1)*im.height/2))
        out=base/'raw'/f'{a.round}_crop_{i:02}.png';im.crop(bounds).save(out);inputs.append(out)
    (base/'qa'/f'{a.round}_sheet_crops.json').write_text(json.dumps({'source':source.relative_to(ROOT).as_posix(),'layout':[3,2],'source_size':im.size,'crop_only':True,'resized':False}),encoding='utf-8')
for i,inp in enumerate(inputs):
    key=a.round+(f'_{i:02}' if a.sheet else '')
    qa=base/'qa'/f'{key}_decode.json'
    cmd=[sys.executable,str(ROOT/'Tools/OCCArt/decode_generated_pixel_grid.py'),'--source',str(inp),'--decoded',str(base/'qa'/f'{key}_decoded.png'),'--output-32',str(base/'qa'/f'{key}_candidate.png'),'--qa',str(qa),'--target-size','32x32','--safe-border','2','--palette-max','12','--preserve-components']
    subprocess.run(cmd,stdout=subprocess.DEVNULL,check=True)
    d=json.loads(qa.read_text());print(a.name,key,{k:d[k] for k in ['status','subject_lattice_confidence','subject_logical_size']},flush=True)
