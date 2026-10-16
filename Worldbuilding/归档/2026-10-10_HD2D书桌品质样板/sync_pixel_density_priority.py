"""Mother-first scoped rule update; never replay an unknown write."""
import csv,difflib,html,json,shutil,subprocess
from pathlib import Path
import xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent/'pixel_density_priority'
OUT.mkdir(exist_ok=True)
DOC='Y1KydFPjMocJjYxMmFEcTblcnLf'
CLI=shutil.which('lark-cli.cmd') or shutil.which('lark-cli')
RULE='同场景像素大小统一（用户确认，2026-10-10，最高优先级）：同一场景的全部可见像素资产必须使用同一像素密度基准，包含角色、纸板、厚纸板、模型贴图、环境、陈设、场景内装饰和像素特效；美观、细节丰富度、画布自适应及战斗外素材库例外均不得放宽此要求。先选定场景颗粒参照，记录主体像素单元、世界尺寸、显示缩放及模型UV密度，再生成或复用素材；不能只凭图片尺寸、Point采样或粗像素提示词判断一致。以实际游戏镜头验收，相同深度与投影条件下可见像素大小必须统一；远近和斜面只允许同一密度经真实投影产生的变化，不得以透视为逐物件改密度的借口。源图中混用粗细格、同场景杯碟与灯等陈设颗粒不一致，均为最高优先级阻断项，先修复再扩量。书桌当前颗粒参照为咖啡杯右侧蓝金晶灯，只认可其像素大小与色簇表现，不等于批准整个场景。正文和系统文字保持既定清晰度，真实光照、阴影及后处理仍按高清空间表现配置，不强制整帧低清；它们不替代像素资产的一致性检查。入库与样板晋升必须提交全部可见像素资产的密度清单、源与缩放/UV追溯、1920×1080及960×540实际同框对比、运动/缩放检查和真实人工复核；缺失、待定或不一致时保持QA_PENDING。更换资产、尺寸、相机、UV或采样使相关证据失效，重新验证受影响范围，不用旧截图或机器PASS代替场景审核。'
targets=[ROOT/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md',ROOT/'Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv',ROOT/'Tools/OCCArt/occ_art_contract_v1.json',ROOT/'Tools/OCCArt/hd2d_baselines_v1.json',ROOT/'AGENTS.md',ROOT/'.codex/skills/occ-hd2d-quality/SKILL.md',Path('C:/Users/FNHF/.codex/skills/occ-art-direction/SKILL.md')]
def call(name,*args):
 p=subprocess.run([CLI,'docs',*args,'--as','user','--format','json'],cwd=ROOT,capture_output=True,encoding='utf-8')
 (OUT/(name+'.json')).write_text(p.stdout or p.stderr,encoding='utf-8')
 r=json.loads(p.stdout or p.stderr)
 if p.returncode or not r.get('ok') or r.get('data',{}).get('result') not in (None,'success') or r.get('data',{}).get('warnings'):raise RuntimeError('Inspect result before retry: '+name)
 return r['data']['document']
def main():
 if (OUT/'completed.json').exists():raise RuntimeError('Already completed; do not replay')
 before={}
 for i,p in enumerate(targets):
  b=OUT/'backups'/str(i)/p.name;b.parent.mkdir(parents=True,exist_ok=True)
  if not b.exists():shutil.copy2(p,b)
  before[p]=b.read_text(encoding='utf-8-sig')
 keyword='适用与输出。'
 if (OUT/'write.json').exists():raise RuntimeError('Previous write receipt exists; inspect readback before resuming')
 doc=call('before','+fetch','--doc',DOC,'--scope','keyword','--keyword',keyword,'--detail','full')
 blocks=[b for b in ET.fromstring(doc['content']) if keyword in ''.join(b.itertext())]
 if len(blocks)!=1 or blocks[0].tag!='p':raise RuntimeError('Ambiguous mother paragraph')
 old=''.join(blocks[0].itertext())
 local=[p for p in before[targets[0]].split('\n\n') if p.startswith(keyword)]
 if local!=[old]:raise RuntimeError('Mother/local conflict; stop and report')
 new=old+' '+RULE
 xml=OUT/'rule.xml';xml.write_text('<p>'+html.escape(new)+'</p>',encoding='utf-8')
 call('write','+update','--doc',DOC,'--command','block_replace','--block-id',blocks[0].get('id'),'--content','@./'+xml.relative_to(ROOT).as_posix())
 check=call('after','+fetch','--doc',DOC,'--scope','keyword','--keyword',keyword,'--detail','full')
 if not any(''.join(b.itertext())==new for b in ET.fromstring(check['content'])):raise RuntimeError('Mother readback mismatch')
 targets[0].write_text(before[targets[0]].replace(old,new,1),encoding='utf-8')
 rows=list(csv.reader(before[targets[1]].splitlines()))
 rows.append(['ART-HD2D-PIXEL-DENSITY-P0-20261010','共用表现','同场景像素大小统一：最高优先级','所有可见像素资产共享场景密度；同深度投影颗粒一致','书桌以杯右侧蓝金晶灯颗粒为参照；不等于整场景批准','生成前锁定像素单元与缩放UV；全资产清单和两档实际同框及运动证据；真实人工复核；未通过禁止入库晋升；战外例外不放宽密度'])
 with targets[1].open('w',encoding='utf-8',newline='') as f:csv.writer(f,lineterminator='\n').writerows(rows)
 c=json.loads(before[targets[2]]);policy=c['hd2d_quality_policy'];policy['version']='2026-10-10.3'
 policy['pixel_density_policy']={'priority':'highest','scope':'all_visible_pixel_assets_in_same_scene','comparison':'one effective world pixel pitch; compare equal depth/projection and verify actual screen clusters; natural projection only','noncombat_exception_waives_density':False,'desk_reference':'blue_gold_lantern_right_of_coffee_cup; pixel scale endorsed only, scene QA_PENDING','blockers':['mixed_source_pixel_pitch','inconsistent_scene_pixel_size','missing_density_inventory_or_actual_comparison'],'record_schema':'occ-hd2d-pixel-density-v1','configuration_binding':'pixel_density_manifest','machine_limit':'checks bound declared pitches and evidence completeness, not actual image segmentation or inventory completeness; genuine human review required'}
 policy['dimensions'].insert(0,'scene_pixel_size_consistency');policy['required_configuration'].append('pixel_density_manifest')
 for profile in policy['profiles'].values():profile['static_cases'].append('pixel_density_same_frame')
 targets[2].write_text(json.dumps(c,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 reg=json.loads(before[targets[3]])
 for entry in reg['baselines'].values():
  entry['status']='QA_PENDING';entry['pixel_density_review']='PENDING: require complete bound scene inventory and two-resolution same-frame evidence under highest priority density policy'
 targets[3].write_text(json.dumps(reg,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 note='\n\n## 同场景像素大小：最高优先级（用户确认，2026-10-10）\n\n'+RULE+'\n\n执行时先查总案A.2.4与机器合同pixel_density_policy。每个场景只用一个有效世界像素间距；完整清单将源主体像素单元、显示缩放或UV换算到此基准，再检查同框实际颗粒。生成与复用简报必须提供颗粒参照，不能只写coarse/pixel art。机器声明一致不证明图片真实一致；旧证据不得继承为批准。\n'
 for p in targets[4:]:p.write_text(before[p]+note,encoding='utf-8')
 diff=''.join(line for p in targets for line in difflib.unified_diff(before[p].splitlines(True),p.read_text(encoding='utf-8').splitlines(True),fromfile=str(p)+':before',tofile=str(p)+':after'))
 (OUT/'rules.diff').write_text(diff,encoding='utf-8')
 (OUT/'completed.json').write_text(json.dumps({'mother_readback_verified':True,'policy_version':policy['version'],'rule':RULE,'runtime_changed':False},ensure_ascii=False,indent=2),encoding='utf-8')
 print('Mother and scoped mirrors synchronized; sample approval remains pending.')
if __name__=='__main__':main()
