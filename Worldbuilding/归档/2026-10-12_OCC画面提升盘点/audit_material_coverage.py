"""Read-only source/resource inventory; does not start Unity or prove runtime quality."""
from pathlib import Path
import hashlib,json,re,struct
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
P=ROOT/'UnityProject/Assets/Game/Runtime/Presentation'
R=ROOT/'UnityProject/Assets/Game/Resources'
def binding(path):
 return {'path':path.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
def ids(section,text):
 match=re.search(r'private static readonly string\[\] '+section+r'\s*=\s*\{(.*?)\};',text,re.S)
 assert match,section
 return re.findall(r'"([^"]+)"',match.group(1))
catalog=(P/'AcademyBattlefieldLayoutCatalog.cs').read_text(encoding='utf-8')
binder=(P/'Hd2DBattlefieldPropMaps.cs').read_text(encoding='utf-8')
registered=set(re.findall(r'new Entry\("([^"]+)"',binder))
covers=ids('LightCoverIds',catalog)+ids('HeavyCoverIds',catalog)
attachments=ids('GroundAttachmentIds',catalog)
states=['academy_light_'+family+'_'+state for family in ['stone_bench','planter'] for state in ['damaged','rubble']]
states+=['academy_heavy_archive_stack_damaged','academy_heavy_masonry_screen_damaged','academy_heavy_training_barricade_rubble']
special=['academy_aether_pillar_'+s for s in ['intact','damaged','rubble']]
special+=['academy_'+f+'_crystal_'+s for f in ['aether','pressure'] for s in ['intact','damaged']]
special+=['academy_loot_chest_'+s for s in ['closed','open','empty']]+['academy_decoy_lantern_active']
assert len(set(covers+states+special))==23
def row(id):
 sources=list((R/'Art').rglob(id+'.png'))
 assert sources,id
 dimensions=[]
 for path in sources:
  raw=path.read_bytes();assert raw[:8]==b'\x89PNG\r\n\x1a\n'
  dimensions.append({**binding(path),'dimensions':list(struct.unpack('>II',raw[16:24]))})
 maps=R/'HD2DMaterialData/Props'/id
 return {'id':id,'source_candidates':dimensions,'registered_in_prop_binder':id in registered,'normal_exists':(maps/'normal.png').exists(),'roughness_exists':(maps/'roughness.png').exists(),'status':'QA_PENDING'}
report={'schema':'occ-visual-work-inventory-v1','date':'2026-10-12','scope':'Static primary academy prop routes, not exhaustive scene inventory or runtime approval','inputs':[binding(P/name) for name in ['AcademyBattlefieldLayoutCatalog.cs','CombatBattlefieldCellPresenter.cs','Hd2DBattlefieldPropMaps.cs','Hd2DBattlefieldMaterialMaps.cs','Hd2DBattlefieldProfile.cs','Hd2DAcademyEnvironment.cs']],
 'groups':{'intact_covers':[row(x) for x in covers],'cover_damage_rubble':[row(x) for x in states],'crystals_pillars_loot_lantern':[row(x) for x in special],'ground_attachments':[row(x) for x in attachments]},
 'summary':{'ground_albedos_registered':16,'prop_binder_entries':len(registered),'primary_route_props':23,'primary_route_props_registered':len(set(covers+states+special)&registered),'primary_route_props_missing':len(set(covers+states+special)-registered),'intact_covers_registered':len(set(covers)&registered),'intact_covers_total':len(covers),'ground_attachment_albedos':len(attachments),'ground_attachments_registered':len(set(attachments)&registered),'paired_normal_files':len(list((R/'HD2DMaterialData').rglob('normal.png'))),'paired_roughness_files':len(list((R/'HD2DMaterialData').rglob('roughness.png'))),'region_files':len(list((R/'HD2DMaterialData').rglob('regions.png')))},
 'limitations':['Source routes do not prove each ID appears in a current encounter','9 registered prop entries include legacy intact/rubble routes; 7 match the primary routes counted here','Attachments can share material role profiles but need exact source registration; do not copy a floor normal onto iron/wood','Vegetation, background facades, permanent-wall source identities, training arena and additional special props remain outside this minimum inventory','No aesthetic completion percentage can be inferred from file counts']}
(OUT/'material_coverage.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report['summary']))
