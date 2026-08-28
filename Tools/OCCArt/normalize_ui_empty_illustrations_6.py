#!/usr/bin/env python3
from __future__ import annotations

import hashlib, json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
M29 = ROOT / "Worldbuilding/05_美术与音频/正式美术生产/M-A29"
ARTIFACTS = ROOT / "UnityProject/Artifacts/UiEmptyIllustrations6"
STAGING = ROOT / "UnityProject/Assets/Game/Resources/Art/ValidationUIEmptyIllustrations"
STEMS = ["empty_archive_tray", "empty_inventory_pouch", "empty_route_case", "empty_reward_crate", "empty_loadout_rack", "locked_document_satchel"]

def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def normalized(source):
    image = Image.open(source).convert("RGBA")
    alpha = image.getchannel("A").point(lambda v: 255 if v >= 32 else 0)
    bounds = alpha.getbbox()
    if not bounds: raise RuntimeError(f"no visible content: {source}")
    image.putalpha(alpha); image = image.crop(bounds)
    scale = min(60 / image.width, 60 / image.height)
    size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
    image = image.resize(size, Image.Resampling.BOX)
    hard = image.getchannel("A").point(lambda v: 255 if v >= 96 else 0)
    image = image.quantize(colors=12, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert("RGBA"); image.putalpha(hard)
    canvas = Image.new("RGBA", (64,64), (0,0,0,0)); canvas.alpha_composite(image, ((64-size[0])//2, (64-size[1])//2)); return canvas

def checker(image, scale=4):
    enlarged=image.resize((image.width*scale,image.height*scale),Image.Resampling.NEAREST); result=Image.new("RGBA",enlarged.size); draw=ImageDraw.Draw(result)
    for y in range(0,result.height,8):
        for x in range(0,result.width,8):
            v=226 if (x//8+y//8)%2==0 else 178; draw.rectangle((x,y,x+7,y+7),fill=(v,v,v,255))
    result.alpha_composite(enlarged); return result

def main():
    STAGING.mkdir(parents=True, exist_ok=True); images={}
    for stem in STEMS:
        folder=ARTIFACTS/stem; source=folder/"source.png"; image=normalized(source); images[stem]=image; output=STAGING/f"{stem}.png"; image.save(output); image.save(folder/"1x.png")
        image.resize((128,128),Image.Resampling.NEAREST).save(folder/"2x.png"); image.resize((256,256),Image.Resampling.NEAREST).save(folder/"4x.png")
        gray=image.convert("L"); Image.merge("RGBA",(gray,gray,gray,image.getchannel("A"))).save(folder/"grayscale.png"); checker(image).save(folder/"checker.png")
        mp=M29/"manifests"/f"empty_{stem}.occ-art-manifest-v1.json"; m=json.loads(mp.read_text(encoding="utf-8-sig")); m["provenance"]["source_sha256"]=digest(source); m["delivery"]["output_sha256"]=digest(output); mp.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    contacts=ARTIFACTS/"contacts"; contacts.mkdir(parents=True,exist_ok=True); sheet=Image.new("RGB",(960,600),(42,40,35)); draw=ImageDraw.Draw(sheet)
    for i,(stem,image) in enumerate(images.items()):
        x,y=(i%3)*320,(i//3)*300; preview=checker(image).convert("RGB"); sheet.paste(preview,(x+32,y+32)); draw.text((x+12,y+10),stem,fill=(242,235,221)); draw.text((x+12,y+278),"64x64 | 4x checker",fill=(196,187,170))
    sheet.save(contacts/"ui_empty_illustrations_6_review.png"); print(json.dumps({"status":"PASS","normalized":len(images)}))

if __name__ == "__main__": main()
