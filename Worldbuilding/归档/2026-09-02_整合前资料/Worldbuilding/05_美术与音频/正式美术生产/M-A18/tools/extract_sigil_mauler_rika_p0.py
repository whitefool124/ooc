"""Select reviewed chronological Rika frames 01-06 and uniformly sample them to M-A18 64px."""
import json
from pathlib import Path
from PIL import Image

SOURCE = Path(r"E:/数据库/pixelbench/examples/occ_ma18/processed/sigil_mauler_strike_local_master/frames")
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "normalized" / "enemy_actions" / "sigil_mauler"
QA = ROOT / "QA" / "enemy_actions" / "sigil_mauler"

def main():
    OUT.mkdir(parents=True, exist_ok=True); QA.mkdir(parents=True, exist_ok=True)
    contact=Image.new("RGBA", (768,128), (28,28,28,255)); frames=[]
    for target, source_index in enumerate(range(1,7)):
        source=Image.open(SOURCE/f"frame_{source_index:02d}.png").convert("RGBA")
        image=source.resize((64,64),Image.Resampling.NEAREST)
        image.save(OUT/f"frame_{target:02d}.png")
        preview=image.resize((128,128),Image.Resampling.NEAREST); preview.save(QA/f"frame_{target:02d}_4x.png"); contact.alpha_composite(preview,(target*128,0))
        alpha=set(image.getchannel("A").getdata()); colors={pixel[:3] for pixel in image.getdata() if pixel[3]}; bounds=image.getchannel("A").getbbox()
        frames.append({"targetFrame":target,"sourceFrame":source_index,"size":[64,64],"hardAlpha":alpha.issubset({0,255}),"colors":len(colors),"bounds":list(bounds) if bounds else None})
    contact.save(QA/"sigil_mauler_rika_selected_contact_4x.png")
    passed=all(f["size"]==[64,64] and f["hardAlpha"] and f["colors"]<=24 and f["bounds"] and 57<=f["bounds"][3]<=59 for f in frames)
    (QA/"sigil_mauler_rika_selected_report.json").write_text(json.dumps({"status":"PASS" if passed else "WARN","selection":"Rika frames 01-06 after human review; uniform whole-canvas nearest 128->64 sampling","frames":frames},indent=2),encoding="utf-8")
if __name__=="__main__": main()
