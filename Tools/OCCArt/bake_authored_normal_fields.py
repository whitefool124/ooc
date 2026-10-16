"""Deterministic normal data from explicit physical heights; never from albedo.

Input: occ-authored-height-batch-v1, world-space metres and rows top-to-bottom.
Source-art PNGs are only fingerprinted. Their pixels are never opened or used
to infer geometry. Output is candidate numeric shader data, not generated art.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import zlib

ROOT = Path(__file__).resolve().parents[2]


def normal_pixels(heights, world_pixel_pitch):
    if not isinstance(world_pixel_pitch, (int, float)) or not math.isfinite(world_pixel_pitch) or world_pixel_pitch <= 0:
        raise ValueError('Positive finite world_pixel_pitch required')
    height = len(heights)
    width = len(heights[0]) if height else 0
    if width < 2 or height < 2 or any(len(row) != width for row in heights):
        raise ValueError('Rectangular height grid of at least2x2 required')
    if any(not isinstance(v, (int, float)) or not math.isfinite(v) for row in heights for v in row):
        raise ValueError('Every explicit height must be a finite number in metres')
    pixels = bytearray()
    for y in range(height):
        for x in range(width):
            xa, xb = max(0, x-1), min(width-1, x+1)
            ya, yb = max(0, y-1), min(height-1, y+1)
            dx = (heights[y][xb]-heights[y][xa])/((xb-xa)*world_pixel_pitch)
            dy = (heights[yb][x]-heights[ya][x])/((yb-ya)*world_pixel_pitch)
            # Image Y runs down; tangent-space +Y runs toward increasing V/up.
            inv = 1/math.sqrt(dx*dx+dy*dy+1)
            normal = (-dx*inv, dy*inv, inv)
            pixels.extend(max(0, min(255, round((v*.5+.5)*255))) for v in normal)
    return width, height, bytes(pixels)


def png_bytes(width, height, rgb):
    def chunk(kind, payload):
        return struct.pack('>I', len(payload))+kind+payload+struct.pack('>I', zlib.crc32(kind+payload)&0xffffffff)
    scanlines = b''.join(b'\0'+rgb[y*width*3:(y+1)*width*3] for y in range(height))
    return b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0))+chunk(b'IDAT', zlib.compress(scanlines, 9))+chunk(b'IEND', b'')


def bake_batch(source_path, output_dir):
    source = json.loads(source_path.read_text(encoding='utf-8-sig'))
    if source.get('schema') != 'occ-authored-height-batch-v1':
        raise ValueError('Explicit authored-height batch schema required')
    if output_dir.resolve().is_relative_to(ROOT/'UnityProject'):
        raise ValueError('Bake into review/archive first; Unity admission is separate')
    source_hash = hashlib.sha256(source_path.read_bytes()).hexdigest()
    records, ids = [], set()
    for entry in source['assets']:
        ident = entry['asset_id']
        if not ident or any(c not in 'abcdefghijklmnopqrstuvwxyz0123456789_-' for c in ident) or ident in ids:
            raise ValueError('Unique safe asset_id required')
        ids.add(ident)
        if entry.get('height_basis') != 'explicit_authored_geometry':
            raise ValueError('Albedo-derived/undeclared height basis forbidden')
        reference = entry['albedo_reference']
        reference_path = (ROOT/reference['path']).resolve()
        if not reference_path.is_relative_to(ROOT) or hashlib.sha256(reference_path.read_bytes()).hexdigest() != reference['sha256']:
            raise ValueError('Albedo identity fingerprint mismatch')
        w, h, pixels = normal_pixels(entry['heights_world_m'], entry['world_pixel_pitch'])
        if [w, h] != entry['native_canvas']:
            raise ValueError('Height/albedo registered canvas mismatch')
        data = png_bytes(w, h, pixels)
        folder = output_dir/ident
        folder.mkdir(parents=True, exist_ok=True)
        target = folder/'normal.png'
        if target.exists() and target.read_bytes() != data:
            raise ValueError('Changed bake would overwrite reviewed data; choose a new output directory')
        target.write_bytes(data)
        record = dict(schema='occ-authored-normal-field-v1', status='QA_PENDING', asset_id=ident,
            source_type='deterministic_numeric_bake_from_explicit_physical_heights', source_height_record=str(source_path),
            source_height_sha256=source_hash, albedo_reference=reference, native_canvas=[w,h],
            world_pixel_pitch=entry['world_pixel_pitch'], normal_convention='OpenGL +Y, image rows top-down',
            geometry_notes=entry['geometry_notes'], normal_sha256=hashlib.sha256(data).hexdigest(),
            normal_path=str(target), albedo_pixels_read=False, interpolation=False, source_art_changed=False,
            human_registration_review='PENDING', runtime_review='PENDING')
        (folder/'provenance.json').write_text(json.dumps(record, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
        records.append(record)
    output_dir.mkdir(parents=True, exist_ok=True)
    (output_dir/'batch_result.json').write_text(json.dumps(records, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return records


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    records = bake_batch(args.source, args.output_dir)
    print(json.dumps(dict(status='QA_PENDING', assets=len(records), albedo_modified=False)))
