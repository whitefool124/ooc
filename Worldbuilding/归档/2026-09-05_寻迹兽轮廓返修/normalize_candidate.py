"""Reproducible, user-authorized normalization; outputs stay outside Unity.

Source: independent built-in image generation. No painting of new anatomy.
Near-neutral pale baked checker -> alpha; aspect-preserving nearest fit;
opaque-only nondithered quantization; native center X32 / baseline Y58.
"""
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "Artifacts/OCC_UnitSilhouetteReview_20260905"
ARCHIVE = Path(__file__).resolve().parent
ART = ROOT / "UnityProject/Assets/Game/Resources/Art"
SOURCE = OUT / "raw/tether_hound_compact_v1.png"
NEAREST = Image.Resampling.NEAREST


def save_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def metrics(im):
    a = np.array(im.convert("RGBA"))
    b = im.getchannel("A").getbbox()
    return {"size": list(im.size), "bounds": list(b), "body_size": [b[2]-b[0], b[3]-b[1]],
            "center_x": (b[0]+b[2])/2, "baseline_y": b[3],
            "opaque_pixels": int((a[:,:,3] > 0).sum()),
            "visible_colors": len(set(map(tuple, a[a[:,:,3] > 0,:3]))),
            "alpha_values": sorted(map(int, np.unique(a[:,:,3]))),
            "width_in_logical_cells": (b[2]-b[0])/32}


def checker(size, step=8):
    im = Image.new("RGBA", size, (212, 215, 213, 255))
    d = ImageDraw.Draw(im)
    for y in range(0,size[1],step):
        for x in range(0,size[0],step):
            if (x//step+y//step)%2:
                d.rectangle((x,y,x+step-1,y+step-1),fill=(166,174,173,255))
    return im


def normalize():
    a = np.array(Image.open(SOURCE).convert("RGB"))
    rgb = a.astype(np.int16)
    bg = (rgb.min(axis=2) >= 210) & ((rgb.max(axis=2)-rgb.min(axis=2)) <= 35)
    rgba = np.dstack((a, np.where(bg,0,255).astype(np.uint8)))
    rgba[bg,:3] = 0
    masked = Image.fromarray(rgba)
    bounds = masked.getchannel("A").getbbox()
    cropped = masked.crop(bounds)
    scale = min(40/cropped.width,38/cropped.height)
    fitted = cropped.resize((round(cropped.width*scale),round(cropped.height*scale)),NEAREST)
    p = np.array(fitted)
    opaque = p[:,:,3] > 0
    prgb = p[:,:,:3].astype(np.int16)
    cyan = opaque & (prgb[:,:,1] >= prgb[:,:,0]+16) & (prgb[:,:,2] >= prgb[:,:,0]+16)
    # Reserve the tiny functional cyan indicator, avoiding its absorption into brown.
    for select, count in ((opaque & ~cyan, 22 if cyan.any() else 24), (cyan, 2)):
        if not select.any():
            continue
        sample = Image.fromarray(p[select,:3][None,:,:])
        quantized = sample.quantize(colors=count, method=Image.Quantize.MEDIANCUT,
                                   dither=Image.Dither.NONE).convert("RGB")
        p[select,:3] = np.array(quantized)[0]
    p[~opaque] = 0
    fitted = Image.fromarray(p)
    im = Image.new("RGBA",(64,64))
    im.alpha_composite(fitted,(round(32-fitted.width/2),58-fitted.height))
    return im, {"source_bounds": list(bounds), "fit_scale": scale,
                "fitted_size": list(fitted.size), "cyan_pixels": int(cyan.sum()),
                "mask_rule": "min RGB >= 210 and channel range <= 35 becomes transparent",
                "resampling": "nearest; aspect retained within integer rounding",
                "palette": "opaque-only median cut without dithering; reserve up to 2 cyan colors"}


def contact(hound, scale, vine=False):
    # Controlled offline composition using production source PNGs and layout formula.
    # Native logical cell=32; native unit canvas=64. This is NOT a Unity screenshot.
    im = Image.new("RGBA", (192,192))
    tile = Image.open(ART/"FormalAcademyCombat32/academy_stone_road_a.png").convert("RGBA")
    assert tile.size == (32,32)
    for y in range(6):
        for x in range(6):
            im.alpha_composite(tile,(x*32,y*32))
    units = [(2,2,Image.open(ART/"FormalUnits64/hero.png").convert("RGBA")),
             (3,2,Image.open(ART/"FormalUnits64/pyromancer.png").convert("RGBA")),
             (2,3,hound)]
    foreground = None
    if vine:
        v = Image.open(ART/"FormalFirstBattle32/lamp_vine.png").convert("RGBA")
        for x,y in ((2,2),(3,2),(2,3)):
            im.alpha_composite(v.crop((0,0,32,22)),(x*32,y*32))
        foreground = v.crop((0,22,32,32))
    overlap = []
    masks = []
    for x,y,u in sorted(units,key=lambda v:(v[1],v[0])):
        # Corresponds to UnitPresentationRect: x+(cell-size)/2, y+cell-size.
        pos = (x*32-16,y*32-32)
        layer = Image.new("RGBA",im.size)
        layer.alpha_composite(u,pos)
        mask = np.array(layer.getchannel("A")) > 0
        overlap.extend(int((mask&m).sum()) for m in masks)
        masks.append(mask)
    for y in range(6):
        for x,uy,u in sorted(units,key=lambda v:v[0]):
            if uy==y:
                im.alpha_composite(u,(x*32-16,y*32-32))
        if foreground is not None:
            for x,vy in ((2,2),(3,2),(2,3)):
                if vy==y:
                    im.alpha_composite(foreground,(x*32,y*32+22))
    return im.resize((192*scale,192*scale),NEAREST), overlap


def main():
    (OUT/"processed").mkdir(exist_ok=True)
    (OUT/"qa").mkdir(exist_ok=True)
    formal = [ART/"FormalUnits64"/f for f in ("hero.png","hero.png.meta","tether_hound.png","tether_hound.png.meta","pyromancer.png","pyromancer.png.meta")]
    hashes_before = {p.relative_to(ROOT).as_posix(): digest(p) for p in formal}
    candidate, process = normalize()
    output = OUT/"processed/tether_hound_compact_v1.png"
    candidate.save(output)
    old = Image.open(ART/"FormalUnits64/tether_hound.png").convert("RGBA")
    candidate.save(OUT/"qa/one_x.png")
    candidate.resize((256,256),NEAREST).save(OUT/"qa/four_x.png")
    gray = ImageOps.grayscale(candidate).convert("RGBA")
    gray.putalpha(candidate.getchannel("A"))
    gray.resize((256,256),NEAREST).save(OUT/"qa/grayscale.png")
    board = checker((64,64))
    board.alpha_composite(candidate)
    board.resize((256,256),NEAREST).save(OUT/"qa/checker.png")
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",18)
    small = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",14)
    sheet = Image.new("RGB",(1000,1040),(22,29,32))
    d = ImageDraw.Draw(sheet)
    d.text((24,16),"寻迹兽轮廓审核 · 64×64 同画布 / 同倍率 / 脚底 Y58",font=font,fill="#e0ded0")
    for x,u,label in ((24,old,"当前资产：61×34 px"),(280,candidate,"规范化候选：40×33 px")):
        bg = checker((64,64)); bg.alpha_composite(u)
        sheet.paste(bg.resize((256,256),NEAREST).convert("RGB"),(x,72))
        d.text((x,44),label,font=small,fill="#c2c9c5")
    sheet.paste(gray.resize((256,256),NEAREST),(568,72),gray.resize((256,256),NEAREST))
    d.text((568,44),"候选灰阶 · 4×",font=small,fill="#c2c9c5")
    d.text((840,44),"原生 1×",font=small,fill="#c2c9c5")
    sheet.paste(candidate,(850,80),candidate)
    pairs = {}
    for row,vine in enumerate((False,True)):
        y=380+row*310
        for col,(label,u) in enumerate((("当前",old),("候选",candidate))):
            img, overlap = contact(u,2,vine)
            # Crop empty outside rows without scaling pixels.
            img = img.crop((32,32,352,288))
            x=24+col*360
            sheet.paste(img.convert("RGB"),(x,y))
            d.text((x,y-28),label+ (" · 灯藤接触" if vine else " · 相邻角色接触"),font=small,fill="#c2c9c5")
            pairs[label+str(vine)] = overlap
            contact(u,1,vine)[0].save(OUT/f"qa/contact_{'old' if col==0 else 'candidate'}_{'vine' if vine else 'stone'}_1x.png")
            contact(u,2,vine)[0].save(OUT/f"qa/contact_{'old' if col==0 else 'candidate'}_{'vine' if vine else 'stone'}_2x.png")
    d.text((24,978),"外部接触重建，非 Unity 实机截图；使用现有地砖、角色和灯藤源图。",font=small,fill="#c2c9c5")
    d.text((24,1002),"候选仅 QA_PENDING；人工审美、Unity 应用与动态审核待完成。水面 / 灯藤 v3 批准仍为 0/2。",font=small,fill="#c2c9c5")
    sheet.save(OUT/"qa/review_board.png")
    hashes_after = {p.relative_to(ROOT).as_posix(): digest(p) for p in formal}
    assert hashes_before == hashes_after
    report = {"old":metrics(old),"candidate":metrics(candidate),"process":process,
              "pairwise_alpha_overlap_native_pixels":pairs,
              "contact_evidence_scope":"offline reconstruction from production PNGs; no HUD, shadows, Unity importer or runtime verification",
              "formal_hashes_unchanged":hashes_before==hashes_after,"formal_hashes":hashes_after}
    save_json(ARCHIVE/"normalization-metrics.json",report)
    manifest_path = OUT/"tether_hound_compact_v1.occ-art.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    manifest["status"] = "QA_PENDING"
    manifest["delivery"].update(output_path=output.relative_to(ROOT).as_posix(),output_sha256=digest(output),
        required_color_families=[{"family":"cyan","min_opaque_pixels":1}])
    manifest["evidence"] = {key:(OUT/"qa"/name).relative_to(ROOT).as_posix() for key,name in
        {"one_x":"one_x.png","four_x":"four_x.png","grayscale":"grayscale.png",
         "checker":"checker.png","application_contact":"review_board.png"}.items()}
    manifest["human_review"]["notes"] = "Script normalization authorized by user. Human review PENDING. Application evidence is an offline reconstruction only, not Unity application approval. No import or formal promotion."
    manifest["processing"] = {"authorization":"允许脚本规范化审核候选", "script":Path(__file__).relative_to(ROOT).as_posix(),
                              "script_sha256":digest(Path(__file__)),"parameters":process}
    save_json(manifest_path,manifest)
    print(json.dumps(report,ensure_ascii=False,indent=2))


if __name__ == "__main__":
    main()
