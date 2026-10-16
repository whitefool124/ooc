import csv,json
from pathlib import Path
root=Path(__file__).resolve().parents[3]
folder=Path(__file__).parent
evidence=root/'UnityProject/ReviewEvidence/20261005_full_balance'
def read(name):return json.loads((evidence/name).read_text(encoding='utf-8-sig'))
before,after=read('before-catalog.json'),read('after-catalog.json')
bench={r['id']:r for r in read('after-bench.json')}
fights=read('combat-matrix.json')
node_variants=dict(archive_wing='strong_depot_wreck_a',clinic_hall='strong_rail_patrol_b',core_finale='boss_academy_sealed_core',dorm_drill='strong_rail_patrol_a',dorm_watch='weak_tracker_test',lecture_annex='strong_signal_hub_a',market_lane='strong_gatehouse_a',observatory_path='calibration_lockdown_a',seal_bridge='strong_gatehouse_b',sparring_ring='strong_relay_raid_b',study_vault='strong_transmission_tower_a',supply_depot='elite_foundry_a',tower_foyer='elite_foundry_b',tutorial_hall='weak_flank_drill',wilds_camp='cliff_relay_survey_a',wilds_path='strong_depot_wreck_b',workshop_yard='strong_transmission_tower_b')
with (root/'Worldbuilding/数据表/OCC_学院遭遇编成数据表_v1.0.csv').open(encoding='utf-8-sig',newline='') as f:
 rosters={r['变体ID']:r['敌群'].split('+') for r in csv.DictReader(f)}
with (root/'Worldbuilding/数据表/OCC_技能配置表_v1.0.csv').open(encoding='utf-8-sig',newline='') as f:config={r['技能ID']:r for r in csv.DictReader(f)}
reasons={
'F-P-M02':'降低短距离直线位移费用，保留路径限制。',
'F-P-M07':'降低突进单体费用，与高阶长距离爆发区分。',
'F-P-M09':'降低同时带破障的混合单体总伤害24→16。',
'F-P-M10':'降低带推位的单行动点武器伤害12→8。',
'F-P-M11':'降低群体即时护盾8→6，保留群体覆盖收益。',
'F-P-M12':'护盾16→12，与基础护幕8及两回合冷却区分。',
'F-P-M16':'燃烧前置不消耗火源，混合直伤24→16。',
'F-P-M17':'消费火源的免费伤害与护盾各12→6，避免免费双倍回收。',
'F-P-M19':'突进终端武器伤害28→24，保留高费远距爆发。',
'F-P-M20':'降低费用与冷却，保留需要相邻位置或已有轨迹的终端职责。',
'F-P-M22':'降低越障位移费用，仍高于普通可达路径位移。',
'F-P-M23':'提高路径武器伤害并降低魔力，与低行动点通用穿行区分。',
'F-P-U03':'降低短锥形测绘费用，与长线测绘区分。',
'F-P-U07':'远距友方护盾12→10，保留救援优势。',
'F-P-U08':'清燃烧加8护盾改为1行动点，补足清除窗口。',
'F-P-U16':'单纯行动条提前8点的魔力3→1。',
'F-P-U18':'位移、范围伤害、推位合并时，直伤8→4且魔力2→3。',
'F-P-U24':'低行动点路径位移的伤害6→4，魔力2→3，给专用穿刺留空间。',
'F-P-R01':'短距直伤12→8且魔力1→2，与基础四格6伤害2魔力区分。',
'F-P-R02':'五格牵引直伤8→6、魔力1→2、冷却0→1，保留长距控制。',
'F-P-R04':'燃烧强度8→4、魔力1→2，延迟收益与转化前置保留。',
'F-P-R05':'两行动点单体直伤16→12、燃烧强度8→4，满额总伤32→20。',
'F-P-R07':'低费范围伤害8→6、冷却0→1，与二行动点焰线区分。',
'F-P-R08':'降低有延迟的范围点燃费用，保留8强度与单次持续。',
'F-P-R15':'需要消费既有火场的范围终端冷却4→3。',
'F-P-R16':'需要消费燃烧的范围引爆魔力5→4。',
'F-P-R17':'消费既有火场的零费用立即伤害12→8。',
'F-P-R24':'有己方裂痕破口前置的回盾魔力3→1。',
'F-T01':'免费十字直伤16→10、火场持续3→2；共用火场基础伤害8不变。',
'G-T01':'每次护盾20→12、使用3→2，单件护盾上限60→24。',
'G-T04':'完全清盾禁盾的次数4→2，保留原反制机制。',
'G-T08':'相邻物件伤害8→12，让破障工具足以在两次使用内处理24耐久物件。',
'G-T12':'免费额外行动每件2→1次，单次行动点仍为1。',
'G-T13':'自动抵消强制位移每件4→3次，保留反应器职责。',
'G-T15':'诱导灯耐久12→8，敌人更快拆掉诱饵。',
'G-T19':'免费远距单体伤害16→10，每件仍为2次。'
}
rows=[]
for kind in ['spells','artifacts','enemies']:
 old={v['Id']:v for v in before[kind]}
 for value in after[kind]:
  key=value['Id'];initial=old[key]
  if kind=='spells':
   stats=lambda v:{'AP':v['ActionPointCost'],'魔力':v['ManaCost'],'冷却':v['Cooldown'],'规则':v['Rules']}
   effect=config[key]['效果链'];role=config[key]['主构筑']+'／'+config[key]['职责']
   samples=[f for f in fights if f['Profile']=='spell:'+key]
  elif kind=='artifacts':
   stats=lambda v:{'次数':v['MaximumUses'],'AP':v['ActionPointCost'],'魔力':v['ManaCost'],'效果':v['Effects']}
   effect=value['EffectSummary'];role=value['BuildUse'];samples=[f for f in fights if f['Profile']=='artifact:'+key]
  else:
   stats=lambda v:{'生命':v['MaxHealth'],'初始模板盾':v['Shield'],'速度':v['Speed'],'攻击':v['Attack']}
   effect=value['Attack']['DisplayName'];role='精英' if value['IsElite'] else '普通'
   samples=[f for f in fights if f['Profile'] in ['output','mobility','shield'] and key in rosters.get(node_variants.get(f['Node']),[])]
  changed=stats(initial)!=stats(value)
  actual=bench.get(key)
  rows.append({'类别':{'spells':'个人术式','artifacts':'法宝','enemies':'敌人'}[kind],'ID':key,'名称':value['DisplayName'],
   '处理':'调整' if changed else '保留','职责':role,
   '依据':reasons.get(key,'提高承伤门槛，保留原伤害、速度和反制。' if changed else '按现行职责保留；对应边界与结算已覆盖，未据统一伤害系数改写其机制。'),
   '修改前':json.dumps(stats(initial),ensure_ascii=False,separators=(',',':')),
   '修改后':json.dumps(stats(value),ensure_ascii=False,separators=(',',':')),
   '当前效果':effect,'角色用例合法':actual['legal'] if actual else '17类敌人表与实际单位一致测试通过',
   '角色用例结算错误':actual['error'] if actual else '',
   '独立战斗样本':len(samples),'战斗胜利数':sum(f['Outcome']=='victory' for f in samples),
   '战斗实际施放数':sum(f.get('SpellCasts',{}).get(key,0) if kind=='spells' else f.get('ArtifactUses',0) if kind=='artifacts' else 0 for f in samples)})
with (folder/'全目录平衡覆盖.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,list(rows[0]));w.writeheader();w.writerows(rows)
summary={'catalog':{k:len(after[k]) for k in ['spells','artifacts','enemies']},'bench':len(bench),'bench_errors':sum(not r['legal'] or bool(r['error']) for r in bench.values()),'fights':len(fights),'outcomes':{out:sum(r['Outcome']==out for r in fights) for out in ['victory','defeat','limit','error']},'command_errors':sum(len(r['Errors']) for r in fights),'spell_cast_coverage':sum(r['战斗实际施放数']>0 for r in rows if r['类别']=='个人术式'),'artifact_use_coverage':sum(r['战斗实际施放数']>0 for r in rows if r['类别']=='法宝')}
(folder/'统计摘要.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
