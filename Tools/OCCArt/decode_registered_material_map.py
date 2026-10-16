"""Decode an opaque numeric material field registered to a known native UV grid.

Unlike the sprite decoder, this must not delete grey or neutral normal vectors as
background or reduce a numerical map to an art palette. It selects observed RGB
medoids inside registered macro cells, without geometric interpolation. This
material-field route remains REVIEW_READY pending registration/material review.
"""
import argparse,hashlib,json
from pathlib import Path
import numpy as np
from PIL import Image

parser=argparse.ArgumentParser()
parser.add_argument('--source',type=Path,required=True)
parser.add_argument('--reference',type=Path,required=True)
parser.add_argument('--kind',choices=['normal','roughness'],required=True)
parser.add_argument('--output',type=Path,required=True)
parser.add_argument('--report',type=Path,required=True)
args=parser.parse_args()
reference=Image.open(args.reference)
source=Image.open(args.source).convert('RGBA');rgba=np.asarray(source)
assert source.width==source.height and reference.size==(32,32),'Registered square native32 field required'
assert np.all(rgba[:,:,3]==255),'Numeric field must be fully opaque'
out=np.zeros((32,32,4),dtype=np.uint8)
cell_variance=[]
for y in range(32):
 for x in range(32):
  # Central half-cell avoids generator edge antialiasing; choose an existing
  # colour, never an interpolated RGB or a generated normal from albedo.
  x0=int((x+.25)*source.width/32);x1=int((x+.75)*source.width/32)
  y0=int((y+.25)*source.height/32);y1=int((y+.75)*source.height/32)
  samples=rgba[y0:y1,x0:x1,:3].reshape(-1,3)
  median=np.median(samples.astype(float),axis=0)
  picked=samples[np.argmin(np.sum((samples.astype(float)-median)**2,axis=1))]
  out[y,x,:3]=picked;out[y,x,3]=255
  cell_variance.append(float(np.max(np.std(samples.astype(float),axis=0))))
if args.kind=='normal':
 assert np.min(out[:,:,2])>=190,'Normal source has invalid shallow-surface blue channel'
 center=out[2:30,2:30,:3];dominant=np.median(center.reshape(-1,3),axis=0)
 assert abs(float(dominant[0])-128)<=16 and abs(float(dominant[1])-128)<=16,'Dominant plane unexpectedly tilted'
else:
 assert np.max(np.max(out[:,:,:3],axis=2)-np.min(out[:,:,:3],axis=2))<=5,'Roughness field is not grayscale'
 assert np.min(out[:,:,0])>=100 and np.max(out[:,:,0])<=255,'Unexpected mirror-like stone roughness'
Image.fromarray(out).save(args.output)
Image.fromarray(out).resize((512,512),Image.Resampling.NEAREST).save(args.output.with_name(args.output.stem+'_16x.png'))
record={'schema':'occ-registered-material-data-decode-v1','status':'REVIEW_READY','kind':args.kind,
 'source':str(args.source),'source_sha256':hashlib.sha256(args.source.read_bytes()).hexdigest(),
 'reference':str(args.reference),'reference_sha256':hashlib.sha256(args.reference.read_bytes()).hexdigest(),
 'native_grid':[32,32],'source_macro_pitch_xy':[source.width/32,source.height/32],
 'registration_basis':'Full opaque UV field; one rim cell on the supplied native32 stone; fixed normalized32x32 layout. Sparse/repetitive numeric fields are not sprite silhouettes.',
 'output':str(args.output),'output_sha256':hashlib.sha256(args.output.read_bytes()).hexdigest(),
 'geometry_resize_or_interpolation_performed':False,'new_rgb_colours_introduced':False,'background_pixels_removed':0,
 'macro_cell_rgb_selection':'observed central-cell RGB medoid','max_central_cell_std':max(cell_variance),
 'rgb_min':out[:,:,:3].min(axis=(0,1)).tolist(),'rgb_max':out[:,:,:3].max(axis=(0,1)).tolist(),
 'manual_registered_field_review_required':True,'human_registration_review':'PENDING',
 'limitation':'No lowered generic1.2 threshold and no automatic formal approval. Validate actual map alignment and neutral/default/rotated-light response.'}
args.report.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:record[k] for k in ['status','native_grid','rgb_min','rgb_max','max_central_cell_std']}))
