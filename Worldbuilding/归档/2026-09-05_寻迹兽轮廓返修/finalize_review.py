"""Assemble captured Unity evidence; record direction approval without promotion."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
ARCHIVE = Path(__file__).resolve().parent
OUT = ROOT / "Artifacts/OCC_UnitSilhouetteReview_20260905"
QA = OUT / "qa"


def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def write(p,v):
    p.write_text(json.dumps(v,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")


font=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",18)
small=ImageFont.truetype("C:/Windows/Fonts/msyh.ttc",15)
sheet=Image.new("RGB",(1280,920),(22,29,32))
d=ImageDraw.Draw(sheet)
d.text((24,16),"寻迹兽 · Unity 生产界面实际静态接触审核",font=font,fill="#e0ded0")
d.text((24,48),"左：当前资产    右：方向已认可的规范化候选    /    同镜头、同单位尺寸、同灯藤层级",font=small,fill="#c2c9c5")
diffs=[]
for width in (960,1920):
    for zoom in (False,True):
        mode="zoom" if zoom else "overview"
        a=Image.open(QA/f"unity_old_{mode}_{width}.png").convert("RGB")
        b=Image.open(QA/f"unity_candidate_{mode}_{width}.png").convert("RGB")
        mask=np.any(np.array(a)!=np.array(b),axis=2)
        ys,xs=np.where(mask)
        diffs.append({"resolution":[width,width*9//16],"mode":mode,"changed_pixels":int(mask.sum()),
                      "difference_bbox":[int(xs.min()),int(ys.min()),int(xs.max())+1,int(ys.max())+1]})
for col,kind in enumerate(("old","candidate")):
    image=Image.open(QA/f"unity_{kind}_overview_960.png").convert("RGB")
    # Integer crop, no resampling of game content.
    sheet.paste(image.crop((152,80,568,392)),(24+col*624,112))
    d.text((24+col*624,84),"960×540 全场 · 原生像素裁切",font=small,fill="#c2c9c5")
    image=Image.open(QA/f"unity_{kind}_zoom_1920.png").convert("RGB")
    sheet.paste(image.crop((536,140,1112,496)),(24+col*624,470))
    d.text((24+col*624,442),"1920×1080 放大档 · 原生像素裁切",font=small,fill="#c2c9c5")
d.text((24,846),"临时内存纹理：64×64 / Point / Clamp；正式资源未替换。纵向邻接遮挡与动作一致性仍须继续打磨。",font=small,fill="#c2c9c5")
d.text((24,877),"美术方向已认可；应用人工验收、配套动画、Importer 与动态实机尚未通过，状态仍为 QA_PENDING。",font=small,fill="#c2c9c5")
sheet.save(QA/"unity_review_board.png")
write(ARCHIVE/"unity-image-differences.json",diffs)

m_path=OUT/"tether_hound_compact_v1.occ-art.json"
m=json.loads(m_path.read_text(encoding="utf-8"))
m["evidence"]["application_contact"]=(QA/"unity_review_board.png").relative_to(ROOT).as_posix()
m["direction_review"]={"status":"APPROVED","reviewer":"user","date":"2026-09-05",
    "decision":"认可方向，进入后续应用审核","scope":"收尾、四分之三朝向和棕色材质方向；不是最终应用或动态验收"}
m["human_review"]["notes"]="User approved art direction only. Four Unity resolution/zoom pairs captured with a temporary in-memory texture. Application aesthetic review, animation consistency and runtime verification remain PENDING. No Assets import or formal promotion."
m["application"]["static_review"]={"resolutions":[[960,540],[1920,1080]],"reference_unit_rects":[128,256],
    "texture":"64x64 Point Clamp in memory; disposed after capture","scene_saved":False,"play_mode_entered":False,
    "capture_metadata":(ARCHIVE/"unity-capture-metadata.json").relative_to(ROOT).as_posix()}
write(m_path,m)
baseline=json.loads((ARCHIVE/"normalization-metrics.json").read_text(encoding="utf-8"))["formal_hashes"]
assert all(digest(ROOT/p)==h for p,h in baseline.items())
write(ARCHIVE/"final-state.json",{"formal_png_and_meta_hashes_unchanged":True,"formal_hashes":baseline,
    "unity_readback":"E:/数据库/OCC_Codex/UnityProject/Assets|playing=False|compiling=False|scene=Assets/Scenes/CombatPrototype.unity|dirty=False|count=1|candidateTextures=0",
    "asset_status":"QA_PENDING","direction_approved":True,"application_approved":False})
print(json.dumps(diffs,indent=2))
