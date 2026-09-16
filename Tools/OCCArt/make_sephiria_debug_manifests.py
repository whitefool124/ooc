#!/usr/bin/env python3
from hashlib import sha256
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ARCHIVE = ROOT / "Worldbuilding/归档/2026-09-14_赛菲莉娅主参考确认"
ART = ROOT / "UnityProject/Assets/Game/Resources/Art/CombatTestArenaSephiriaDebug"
EVIDENCE = ROOT / "UnityProject/Reports/CombatTestArena/sephiria_debug_evidence"

def digest(p): return sha256(Path(p).read_bytes()).hexdigest()
def rel(p): return str(Path(p).relative_to(ROOT)).replace("\\", "/")

def write(asset_id, role, source, output, logical, palette, low=None):
    data={"schema":"occ-art-manifest-v1","contract_version":1,"asset_id":asset_id,"role":role,"status":"QA_PENDING",
      "provenance":{"source_channel":"easycli_chatgpt_pro_gpt_image_2_5","source_descriptor":"Sephiria地图表现主参考方向的独立调试原料；只提炼地图组织与像素密度，不复制素材","source_path":rel(source),"source_sha256":digest(source)},
      "delivery":{"output_path":rel(output),"output_sha256":digest(output),"logical_cells":logical,"palette_max":palette},
      "application":{"runtime_draw_rect":"CombatTestArena only","default_integer_scale":1,"minimum_integer_scale":1},
      "evidence":{"one_x":rel(EVIDENCE/(Path(output).stem+"_contact.png")),"four_x":rel(EVIDENCE/(Path(output).stem+"_contact.png")),"grayscale":rel(EVIDENCE/(Path(output).stem+"_contact.png")),"checker":rel(EVIDENCE/(Path(output).stem+"_contact.png")),"application_contact":rel(EVIDENCE/(Path(output).stem+"_contact.png"))},
      "human_review":{"overall":"PENDING","reviewer":None,"notes":"新方向调试样板，等待用户审美确认"},
      "unity_import":{"asset_path":rel(output),"importer_verified":False,"runtime_verified":False}}
    if low:
      low_palette = 6 if role == "battlefield_floor_tile_64" else (10 if role == "battlefield_single_cell_prop_64" else 12)
      data["delivery"]["low_resolution_companion"]={"output_path":rel(low),"output_sha256":digest(low),"palette_max":low_palette}
    path=ARCHIVE/(asset_id+".occ-art-manifest-v1.json"); path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    return path

if __name__=="__main__":
  entries=[
   ("sephiria_debug_ground_v1","battlefield_floor_tile_64",ARCHIVE/"source/ground_sephiria_v1.png",ART/"sephiria_ground_64.png",[1,1],8,ART/"sephiria_ground_32.png"),
   ("sephiria_debug_unit_side_v1","tactical_unit_64",ARCHIVE/"source/unit_side_v1.png",ART/"sephiria_unit_side_64.png",[1,1],24,None),
   ("sephiria_debug_heavy_cover_v2","battlefield_single_cell_prop_64",ARCHIVE/"source/heavy_cover_grid_aligned_v2.png",ART/"sephiria_heavy_cover_64.png",[1,1],14,ART/"sephiria_heavy_cover_32.png"),
   ("sephiria_debug_heavy_cover_2x2_v1","battlefield_multi_cell_prop_64",ARCHIVE/"source/heavy_cover_2x2_v1.png",ART/"sephiria_heavy_cover_2x2_128.png",[2,2],16,ART/"sephiria_heavy_cover_2x2_64.png")]
  entries.extend([
   ("sephiria_debug_unit_post_outline_v1","tactical_unit_64",ARCHIVE/"source/unit_side_v1.png",ART/"sephiria_unit_side_post_outline_64.png",[1,1],24,None),
   ("sephiria_debug_heavy_cover_post_outline_v1","battlefield_single_cell_prop_64",ARCHIVE/"source/heavy_cover_grid_aligned_v2.png",ART/"sephiria_heavy_cover_post_outline_64.png",[1,1],14,ART/"sephiria_heavy_cover_post_outline_32.png"),
   ("sephiria_debug_heavy_cover_2x2_post_outline_v1","battlefield_multi_cell_prop_64",ARCHIVE/"source/heavy_cover_2x2_v1.png",ART/"sephiria_heavy_cover_2x2_post_outline_128.png",[2,2],16,ART/"sephiria_heavy_cover_2x2_post_outline_64.png")])
  for e in entries:
    p=write(*e); print(p)
