from pathlib import Path
import argparse,json,shutil,subprocess,sys
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('--round',default='r5',choices=['r5','r6']);args=parser.parse_args()
results=[]
for item in json.loads((OUT/f'crystal_albedo_batch_{args.round}.json').read_text(encoding='utf-8')):
 folder=OUT/f'crystal_batch_{args.round}'/item['id'];folder.mkdir(parents=True,exist_ok=True)
 raw=folder/'raw.png';shutil.copy2(item['source'],raw)
 run=subprocess.run([sys.executable,str(ROOT/'Tools/OCCArt/decode_generated_pixel_grid.py'),'--source',str(raw),'--decoded',str(folder/'logical.png'),'--output-32',str(folder/'candidate.png'),'--qa',str(folder/'decode.json'),'--target-size','64x64','--palette-max','15','--confidence-min','1.2'],capture_output=True,text=True,encoding='utf-8')
 if (folder/'decode.json').exists():
  qa=json.loads((folder/'decode.json').read_text(encoding='utf-8'));results.append(dict(id=item['id'],status=qa['status'],pitch=qa['inferred_subject_pixel_pitch'],confidence=qa['subject_lattice_confidence'],subject=qa['subject_logical_size'],raw=str(raw)))
 else:results.append(dict(id=item['id'],status='DECODE_FAILED',error=run.stderr))
(OUT/f'crystal_batch_{args.round}_result.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(results))
