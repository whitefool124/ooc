#!/usr/bin/env python3
from __future__ import annotations
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; M30=ROOT/"Worldbuilding/05_美术与音频/正式美术生产/M-A30"; ART=ROOT/"UnityProject/Artifacts/UiChapterDividers5"
ASSETS={
 "teaching_record":"a slim worn wooden teaching pointer laid along a shallow chalk trough, two short ivory chalk pieces and one blank warm-paper corner tab",
 "workshop_record":"a narrow worn wooden tool rail carrying one compact forged-iron caliper and two simple empty iron pegs",
 "infirmary_record":"a narrow warm wooden divider with one folded muted grey-green bandage and a small white enamel tray lip, no medical symbol",
 "field_survey":"a weathered wooden survey ruler crossed by a tied natural-fibre measuring cord and one pressed olive leaf",
 "sealed_dossier":"a narrow charcoal cloth dossier spine crossed by one restrained sealed-red binding strap and one compact forged-iron paper clamp",
}
def main():
 (M30/"manifests").mkdir(parents=True,exist_ok=True); catalog=[]
 for stem,subject in ASSETS.items():
  (ART/stem).mkdir(parents=True,exist_ok=True)
  prompt=("Use case: stylized-concept\nAsset type: independent reusable OCC chapter divider source; final logical pixel canvas 128x32\nPrimary request: "+subject+"\nScene/backdrop: genuinely transparent background, isolated horizontal object arrangement only\nStyle/medium: coarse hand-clustered pixel art readable at native 128x32, strong low horizontal silhouette, deliberate square stair-step contour, restrained material texture\nLighting/mood: fixed upper-left light, short hard-edged self-shadow only, no glow\nColor palette: warm wood and paper, charcoal forged iron, muted grey-green cloth, off-white enamel, small oxidized brass; sealed red only when explicitly requested\nComposition/framing: single horizontal divider, one structural rail plus at most two category cues, calm central rhythm, clean transparent safety space, original orientation\nConstraints: actual transparent background, no interface, no text, no letters, no numbers, no insignia, no symbols, no pseudo-writing, no button, no complete panel or frame\nAvoid: blue crystal, cyan energy, magic glow, neon, hologram, terminal grid, scanlines, steampunk gears or pipes, medieval scroll curls, wax seals, gradients, bloom, soft focus, anti-aliasing, watermark")
  catalog.append({"stem":stem,"subject":subject,"prompt":prompt})
  m={"schema":"occ-art-manifest-v1","contract_version":1,"asset_id":f"ui.divider.{stem}","role":"ui_chapter_divider_128x32","status":"QA_PENDING","provenance":{"source_channel":"codex_builtin_imagegen","source_descriptor":"Independent single transparent chapter-divider source; no board slicing","source_path":f"UnityProject/Artifacts/UiChapterDividers5/{stem}/source.png","source_sha256":"PENDING_GENERATION"},"delivery":{"output_path":f"UnityProject/Assets/Game/Resources/Art/ValidationUIChapterDividers/{stem}.png","output_sha256":"PENDING_NORMALIZATION","native_output_path":None,"logical_cells":None,"palette_max":12,"required_color_families":[]},"application":{"runtime_draw_rect":"non-interactive title-adjacent divider at native 128x32, nearest-neighbour 2x or 4x","default_integer_scale":4,"minimum_integer_scale":2},"evidence":{"one_x":f"UnityProject/Artifacts/UiChapterDividers5/{stem}/1x.png","four_x":f"UnityProject/Artifacts/UiChapterDividers5/{stem}/4x.png","grayscale":f"UnityProject/Artifacts/UiChapterDividers5/{stem}/grayscale.png","checker":f"UnityProject/Artifacts/UiChapterDividers5/{stem}/checker.png","application_contact":"UnityProject/Artifacts/UiChapterDividers5/contacts/PENDING.png"},"human_review":{"overall":"PENDING","reviewer":"","date":"","silhouette":"PENDING","material":"PENDING","perspective":"PENDING","style":"PENDING","application":"PENDING","notes":subject},"unity_import":None}
  (M30/"manifests"/f"divider_{stem}.occ-art-manifest-v1.json").write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
 (M30/"ui_chapter_dividers_5_catalog.json").write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); print(json.dumps({"status":"PASS","prepared":5}))
if __name__=="__main__":main()
