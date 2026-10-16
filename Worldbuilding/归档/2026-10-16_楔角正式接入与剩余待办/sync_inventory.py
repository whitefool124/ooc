from pathlib import Path
import csv,io,json,re,shutil,hashlib
root=Path(__file__).resolve().parents[3];folder=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
rel=lambda p:p.relative_to(root).as_posix()
registry=(root/'UnityProject/Assets/Game/Runtime/Combat/FormalArtRegistry.cs').read_text('utf-8-sig')
def entries(name):
 block=re.search(r'\b'+name+r' = new\[\]\s*\{(.*?)\n        \};',registry,re.S).group(1)
 return [dict(asset_id=a,runtime_id=b,resource=c,exists=(root/'UnityProject/Assets/Game/Resources'/(c+'.png')).is_file()) for a,b,c in re.findall(r'new FormalArtEntry\("([^"]+)", "([^"]+)", "([^"]+)"\)',block)]
units=entries('Units');vfx=entries('Vfx');skills=entries('RuntimeSkills')
resource_root=root/'UnityProject/Assets/Game/Resources/Art'
inventory={'scope':'Source bindings and on-disk resources; not proof of all gameplay or aesthetic quality','units':units,'vfx':vfx,'runtime_skills':skills,
 'file_counts':{name:len(list((resource_root/name).rglob('*.png'))) for name in ['FormalVfx32','FormalSkillIcons32','EnemyUnitCandidates32x64','FieldEffectCandidates32','DeviceMaterialCandidates32']},
 'remaining_preview_only':{'enemies':['signal_keeper','elder_tracker_hound','wind_librarian','legacy_storekeeper'],'fields':['field_loose_paper','field_trace','field_binding_mark','field_paper_screen','field_smoke[6]','field_burning[6]'],'devices_materials':['device_overload','device_ward_generator','device_tower_mechanism','material_crystal_shards']},
 'not_backlog':['Full per-unit animation sheets: removed by user2026-10-16','Distinct mechanism artwork: replaced by names/state/effect text','Rebuild all skill/VFX assets because directories are empty: obsolete claim']}
(folder/'inventory.json').write_text(json.dumps(inventory,ensure_ascii=False,indent=2),'utf-8')
changes=json.loads((folder/'table_changes.json').read_text('utf-8')) if (folder/'table_changes.json').exists() else []
for name in ['OCC_学院第一阶段美术资产生产清单_v1.0.csv','OCC_第一阶段战斗地图与敌人美术缺口矩阵_v1.0.csv','OCC_学院第一阶段全流程内容清单_v1.0.csv']:
 p=root/'Worldbuilding/数据表'/name;backup=folder/(name+'.before')
 if not backup.exists():shutil.copy2(p,backup)
 rows=list(csv.reader(io.StringIO(p.read_text('utf-8-sig'))))
 for row in rows[1:]:
  before=row.copy()
  if name.startswith('OCC_学院第一阶段美术'):
   if row[3]=='breach_ram':
    row[6]='用户已确认采用本版三角头；已迁入FormalUnits64并绑定默认运行入口，保留原GUID；不再依赖候选开关'
    row[7]='正式资源接入完成；保留静态待机、位移与抖动，以及真实意图和状态文字'
   if row[2]=='第一阶段敌人静态单位':
    row[6]='楔角已使用独立三角头默认资源；四个新敌人独立候选齐备且有实机图，但默认注册仍复用旧剪影。elite_vanguard／prototype_hand已有静态资源。'
    row[7]='下一项优先收四个新敌人的默认接线；不重新生成已存在的候选'
   if row[2]=='教程术式与被动图标':
    row[6]=f'FormalSkillIcons32已有{inventory["file_counts"]["FormalSkillIcons32"]}张PNG，运行时术式注册{len(skills)}项；旧空目录及三轮上限记录不再作为缺图依据'
    row[7]='仅补当前真实入口的缺失映射或语义错误，不重生整套教程图标'
   if row[2]=='教程战斗VFX':
    row[6]=f'FormalVfx32已有{inventory["file_counts"]["FormalVfx32"]}张PNG，FormalArtRegistry.Vfx已登记{len(vfx)}个语义；目录非空，逐类实际效果仍须按结算核对'
    row[7]='复用现有VFX和真实事件接线；只修已观察到的缺失或错误反馈，不重生全部特效'
  elif name.startswith('OCC_第一阶段战斗') and row[1]=='FIRST-X':
   row[6]='楔角独立三角头已迁入FormalUnits64，默认取图与ArtId均为breach_ram；GUID保持稳定'
   row[7]='独立外形接入完成；动作使用位移和抖动，意图／状态使用文字'
  elif name.startswith('OCC_学院第一阶段全流程') and row[3]=='内容表现':
   row[8]=row[8].replace('楔角三角头待机候选已接入预览','楔角三角头已接入默认正式资源').replace('候选未晋升正式美术','其余四个敌人、场地和装置候选仍待默认接线；整场品质验收另记')
  if before!=row:changes.append({'file':name,'before':before,'after':row})
 out=io.StringIO(newline='');csv.writer(out,quoting=csv.QUOTE_ALL,lineterminator='\n').writerows(rows);p.write_text('\ufeff'+out.getvalue(),'utf-8')
(folder/'table_changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2),'utf-8')
print(json.dumps({'units':len(units),'vfx':len(vfx),'skills':len(skills),'files':inventory['file_counts'],'changed_rows':len(changes)},ensure_ascii=False))
