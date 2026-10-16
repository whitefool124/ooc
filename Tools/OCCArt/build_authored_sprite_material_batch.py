"""Batch numeric material data from explicit sprite geometry and declared regions.

Albedo pixels are only used for alpha registration and exact authored palette
labels. Heights are never inferred from colour/brightness. Output is QA_PENDING.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
from PIL import Image
from bake_authored_normal_fields import normal_pixels, png_bytes, ROOT


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bake(recipe_path, output):
    recipe = json.loads(recipe_path.read_text(encoding='utf-8-sig'))
    if recipe['schema'] != 'occ-authored-sprite-material-batch-v1':
        raise ValueError('Explicit sprite material recipe required')
    if output.resolve().is_relative_to(ROOT / 'UnityProject'):
        raise ValueError('Bake archive evidence before Unity admission')
    records = []
    for asset in recipe['assets']:
        source = ROOT / asset['source']['path']
        if sha(source) != asset['source']['sha256']:
            raise ValueError('Source identity changed: ' + asset['id'])
        image = Image.open(source).convert('RGBA'); w, h = image.size
        if [w, h] != asset['canvas']:
            raise ValueError('Canvas mismatch')
        pixels = list(image.get_flattened_data())
        heights = [[0.0] * w for _ in range(h)]
        for shape in asset['geometry']:
            if shape['kind'] == 'prism':
                x0,y0,x1,y1,peak = shape['values']; center = (x0+x1)*.5
                for y in range(y0,y1+1):
                    for x in range(x0,x1+1):
                        heights[y][x] = max(heights[y][x], max(0,1-abs(x-center)/max(1,(x1-x0)*.5))*peak)
            elif shape['kind'] == 'ellipse':
                cx,cy,rx,ry,peak = shape['values']
                for y in range(h):
                    for x in range(w):
                        radius = ((x-cx)/rx)**2 + ((y-cy)/ry)**2
                        if radius < 1: heights[y][x] = max(heights[y][x], peak*math.sqrt(1-radius))
            elif shape['kind'] == 'rect':
                x0,y0,x1,y1,depth = shape['values']
                for y in range(y0,y1+1):
                    for x in range(x0,x1+1): heights[y][x] = depth
            else: raise ValueError('Unknown declared geometry')
        _,_,normal = normal_pixels(heights,recipe['world_pixel_pitch']); normal = bytearray(normal)
        region_pixels = bytearray(); gloss_pixels = []
        palette = {tuple(c): role for role,colors in asset.get('palette_roles',{}).items() for c in colors}
        visible = {p[:3] for p in pixels if p[3]}
        if palette and visible != set(palette):
            raise ValueError('Palette declaration incomplete or stale: ' + asset['id'])
        for i,p in enumerate(pixels):
            x,y = i%w,i//w
            role = palette.get(p[:3],asset['default_role'])
            for region in asset.get('regions',[]):
                x0,y0,x1,y1 = region['bounds']
                if x0<=x<=x1 and y0<=y<=y1: role = region['role']
            material = recipe['materials'][role]
            if not p[3]:
                normal[i*3:i*3+3] = bytes([128,128,255]); material=recipe['materials']['void']
            region_pixels.extend(round(material[k]*255) for k in ['metallic','coat','energy'])
            gloss_pixels.append((round(material['metallic']*255),0,0,round((1-material['roughness'])*255)))
        folder = output/asset['id']; folder.mkdir(parents=True,exist_ok=True)
        fields = {'normal.png':png_bytes(w,h,bytes(normal))}
        if asset['output'] == 'crystal': fields['regions.png'] = png_bytes(w,h,bytes(region_pixels))
        elif asset['output'] == 'environment':
            gloss = Image.new('RGBA',(w,h));gloss.putdata(gloss_pixels)
            import io
            buffer=io.BytesIO();gloss.save(buffer,format='PNG');fields['metallic_gloss.png']=buffer.getvalue()
        for name,data in fields.items():
            target=folder/name
            if target.exists() and target.read_bytes()!=data: raise ValueError('Do not overwrite a different reviewed bake')
            target.write_bytes(data)
        (folder/'authored_heights.json').write_text(json.dumps({'heights_world_m':heights,'geometry':asset['geometry']},indent=2),encoding='utf-8')
        record={'schema':'occ-authored-sprite-material-v1','status':'QA_PENDING','id':asset['id'],
                'source':asset['source'],'recipe_sha256':sha(recipe_path),'native_canvas':[w,h],
                'world_pixel_pitch':recipe['world_pixel_pitch'],'height_basis':'explicit_authored_geometry',
                'albedo_brightness_used_for_height':False,'source_art_changed':False,
                'region_basis':asset['region_basis'],'fields':{name:sha(folder/name) for name in fields},
                'normal_convention':'OpenGL +Y; top-to-bottom image rows',
                'limitations':asset['limitations'],'runtime_review':'PENDING','human_registration_review':'PENDING'}
        (folder/'material_provenance.json').write_text(json.dumps(record,ensure_ascii=False,indent=2),encoding='utf-8');records.append(record)
    (output/'batch_result.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
    return records


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--recipes',type=Path,required=True);parser.add_argument('--output-dir',type=Path,required=True)
    args=parser.parse_args(); records=bake(args.recipes,args.output_dir)
    print(json.dumps({'status':'QA_PENDING','assets':len(records),'fields':sum(len(r['fields']) for r in records)}))
