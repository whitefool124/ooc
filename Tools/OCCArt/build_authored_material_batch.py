"""Expand explicit structure recipes into height/semantic material fields.

Reads albedo identity metadata and hashes only. Never infers material or height
from colour. These are authored numeric shader inputs, not replacement art.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import shutil
from bake_authored_normal_fields import bake_batch, png_bytes, ROOT


def contains(layer, x, y):
    shape = layer['shape']
    if shape == 'rect':
        a,b,c,d = layer['bounds']
        return a<=x<=c and b<=y<=d
    if shape == 'circle':
        a,b = layer['center']
        radius = math.hypot(x-a,y-b)
        return layer.get('inner_radius',0)<=radius<=layer['radius']
    if shape == 'line':
        a,b = layer['a'];c,d = layer['b']
        length2 = (c-a)**2+(d-b)**2
        t = max(0,min(1,((x-a)*(c-a)+(y-b)*(d-b))/length2)) if length2 else 0
        return math.hypot(x-a-t*(c-a),y-b-t*(d-b))<=layer['width']*.5
    raise ValueError('Unknown explicit shape '+shape)


def expand(asset, canvas, materials):
    w,h=canvas
    heights=[[0.0]*w for _ in range(h)]
    labels=[['void']*w for _ in range(h)]
    for layer in asset['layers']:
        if layer['material'] not in materials:raise ValueError('Undeclared material')
        if 'height_m' in layer and not math.isfinite(layer['height_m']):raise ValueError('Invalid height')
        for y in range(h):
            for x in range(w):
                if contains(layer,x,y):
                    labels[y][x]=layer['material']
                    if 'height_m' in layer:heights[y][x]=layer['height_m']
    pixels=bytearray()
    for row in labels:
        for ident in row:
            role=materials[ident]
            values=[role[k] for k in ['roughness','metallic','coat']]
            if any(not math.isfinite(v) or not 0<=v<=1 for v in values):raise ValueError('Invalid surface parameter')
            pixels.extend(round(v*255) for v in values)
    return heights,labels,bytes(pixels)


def build(recipes_path, inventory_path, output):
    recipes=json.loads(recipes_path.read_text(encoding='utf-8-sig'))
    if recipes.get('schema')!='occ-authored-material-recipes-v1':raise ValueError('Wrong recipe schema')
    refs={r['id']:r for r in json.loads(inventory_path.read_text(encoding='utf-8-sig'))}
    canvas=recipes['native_canvas'];pitch=recipes['world_pixel_pitch']
    entries=[];fields=[]
    for asset in recipes['assets']:
        ref=refs[asset['asset_id']]['source']
        if ref['dimensions']!=canvas:raise ValueError('Reference/native canvas mismatch')
        height,labels,rgb=expand(asset,canvas,recipes['materials'])
        entries.append(dict(asset_id=asset['asset_id'],height_basis='explicit_authored_geometry',
            albedo_reference=dict(path=ref['path'],sha256=ref['sha256']),native_canvas=canvas,
            world_pixel_pitch=pitch,heights_world_m=height,geometry_notes=asset['geometry_notes']))
        fields.append((asset,labels,rgb))
    output.mkdir(parents=True,exist_ok=True)
    height_path=output/'authored_heights.json'
    height_path.write_text(json.dumps(dict(schema='occ-authored-height-batch-v1',assets=entries),indent=2)+'\n',encoding='utf-8')
    normal_records=bake_batch(height_path,output/'fields')
    records=[]
    recipe_hash=hashlib.sha256(recipes_path.read_bytes()).hexdigest()
    for (asset,labels,rgb),normal in zip(fields,normal_records):
        folder=output/'fields'/asset['asset_id']
        surface=folder/'surface.png';surface.write_bytes(png_bytes(*canvas,rgb))
        label_file=folder/'authored_regions.json'
        label_file.write_text(json.dumps(dict(labels_top_to_bottom=labels,roles=recipes['materials']),indent=2)+'\n',encoding='utf-8')
        record=dict(schema='occ-ground-material-data-v1',status='QA_PENDING',asset_id=asset['asset_id'],
            template_family=asset['template_family'],source_recipe=str(recipes_path),source_recipe_sha256=recipe_hash,
            height_record=str(height_path),height_record_sha256=hashlib.sha256(height_path.read_bytes()).hexdigest(),
            normal=normal,surface_path=str(surface),surface_sha256=hashlib.sha256(surface.read_bytes()).hexdigest(),
            regions_path=str(label_file),regions_sha256=hashlib.sha256(label_file.read_bytes()).hexdigest(),
            channels='linear RGB: R roughness, G metallic, B clearcoat; no emission/AO/transmission claim',
            source_type='explicit_geometry_and_semantic_roles_numeric_bake',albedo_pixels_used=False,
            missing_features=['Glass/water coating approximation only; no physical transmission'],
            source_art_modified=False,human_registration_review='PENDING',runtime_review='PENDING')
        (folder/'material_provenance.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        records.append(record)
    (output/'material_batch_result.json').write_text(json.dumps(records,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    return records


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--recipes',required=True,type=Path)
    p.add_argument('--reference-inventory',required=True,type=Path)
    p.add_argument('--output-dir',required=True,type=Path)
    args=p.parse_args()
    records=build(args.recipes,args.reference_inventory,args.output_dir)
    print(json.dumps(dict(status='QA_PENDING',assets=len(records),normal_fields=len(records),surface_fields=len(records))))
