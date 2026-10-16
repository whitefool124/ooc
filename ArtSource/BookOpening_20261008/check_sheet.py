from pathlib import Path
import subprocess,sys,json
from PIL import Image
root=Path(__file__).resolve().parent
attempt=sys.argv[1] if len(sys.argv)>1 else 'r1'
source=Image.open(root/f'source-{attempt}.png')
folder=root/attempt
folder.mkdir(exist_ok=True)
result=[]
for i in range(8):
    row,col=divmod(i,4)
    cell=source.crop((col*source.width//4,row*source.height//2,(col+1)*source.width//4,(row+1)*source.height//2))
    path=folder/f'cell-{i}.png';cell.save(path)
    subprocess.run([sys.executable,'Tools/OCCArt/decode_generated_pixel_grid.py','--source',str(path),'--decoded',str(folder/f'decoded-{i}.png'),'--output-32',str(folder/f'native-{i}.png'),'--qa',str(folder/f'qa-{i}.json'),'--target-size','auto','--palette-max','24'],capture_output=True,check=True)
    qa=json.loads((folder/f'qa-{i}.json').read_text())
    result.append({key:qa.get(key) for key in ['status','inferred_subject_pixel_pitch','subject_lattice_confidence','subject_logical_size','subject_bounds_on_logical_grid']})
(folder/'sheet-qa.json').write_text(json.dumps(result,ensure_ascii=False,indent=2))
print(json.dumps(result))
