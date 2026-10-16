import json
from pathlib import Path
root=Path(__file__).resolve().parents[3]
path=root/'Tools/OCCArt/occ_art_contract_v1.json'
d=json.loads(path.read_text(encoding='utf-8-sig'))
d['marker_ladder']={
    'source':'CombatBattlefieldRangeGraphic shared native32 integer mesh; A.17 approved square and fill language',
    'geometry':{'movement':'inset cyan square and low-alpha fill','selectable':'dark green square','player_effect':'green fill and connected outer boundary','enemy_range':'amber inset boundary','enemy_effect':'red fill and connected outer boundary','risk':'red centre block','focus':'white corner marks'},
    'compositing':'160ms entrance and 0.91-1.0 pulse with per-cell phase; stable when animations are disabled',
    'result':'400ms pale-green accepted or red rejected corners; never receives input',
    'overlap':'move square insets further inside enemy amber range',
    'eligibility':'resolver/path/line-of-sight/resource/cooldown/turn queries; count only committable cells',
    'layout':'under terrain objects and units; expire on changed action, turn, or battle'
}
d['attack_range_presentation'].update(reach_envelope='green selectable square, filtered by authoritative sight-line',committable_target='green actual effect geometry; valid alive target and resource/turn gates',dead_zone='never appears as committable; shares authoritative minimum-range eligibility',review='2026-10-04 focused EditMode and normal B1 combat entry and attack interaction verified; broader encounters not covered')
d['spell_reach_presentation'].update(reach_envelope='green selectable square for authoritative visible single anchors; self and shaped abilities omit unrelated square range',review='2026-10-04 uses the same eligibility and native geometry as A.17')
d['enemy_intent_destination']={'source':'CombatBattlefieldRangeGraphic native32 geometry','marker':'orange path nodes, destination square and centre direction marker','asset_promotion':False,'change':'850ms amber icon and new route/effect borders, first 300ms 2px icon lift, short change glyph within existing badge','baseline':'first display and identical public plan do not signal','animation_disabled':'stable short tint and change glyph','lifetime':'clear on hidden/restarted/replaced battle; no AI or combat mutations'}
d['feedback_presentation']['damage_popup']={'merge':False,'health_font_reference':72,'shield_font_reference':48,'minimum_rect_reference':[168,104],'long_numbers':'reserve preferred text width plus 16px rounded to 2px; no wrap, crop or text scaling','shield_text':'盾 -N','health_text':'-N','shield_color':'#B8C2CC','outline_reference_px':1,'lifetime_ms':720,'kick_ms':80,'kick_reference_px':8,'hold_until_ms':200,'fade_start_ms':480,'total_rise_reference_px':52,'animation_disabled':'static actual text for 720ms','raycast':False}
d['context_menu_preview']['confirmation']='hover previews; row click submits; pointer first click retains preview and second same-cell click submits non-move; move requires double click; no delayed auto-submit'
d['battlefield_display_policy']['overview']='explicit full-board action fits and centres complete logical board at multiples of 32 reference pixels; startup retains existing default zoom'
path.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
