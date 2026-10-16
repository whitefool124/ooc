# 历史首轮批处理脚本，仅留作追溯；后续火场强度已按总案修正为8，禁止重放。
import csv,json,re
from pathlib import Path
root=Path(__file__).resolve().parents[3]
plan=json.loads((Path(__file__).parent/'数值方案.json').read_text(encoding='utf-8-sig'))
src=root/'UnityProject/Assets/Game/Runtime/Combat/FireSpellCatalog.cs'
text=src.read_text(encoding='utf-8-sig')
for short,cost in plan['costs'].items():
    pattern=r'(\{ "F-P-'+short+r'", new\[\] \{ )\d+, \d+, \d+( \} \})'
    text,n=re.subn(pattern,lambda m:m[1]+', '.join(map(str,cost))+m[2],text)
    assert n==1,(short,n)
replacements={}
for short,rules in plan['changes'].items():
    # Approved revisions supersede authored definitions; edit only the active branch.
    pattern=r'(?m)^.*case "F-P-'+short+r'": return Revised\(.*$'
    matches=list(re.finditer(pattern,text))
    if not matches:
        pattern=r'(?m)^.*S\("F-P-'+short+r'".*$'
        matches=list(re.finditer(pattern,text))
    assert len(matches)==1,(short,len(matches))
    line=matches[0][0]
    nums={}
    for kind,value in rules.items():
        rx=r'(R\(FireRuleKind\.'+kind+r',\s*)(\d+)'
        olds=re.findall(rx,line)
        if not olds:
            # A semantic revision may reuse the authored rule list.
            authored=re.search(r'(?m)^.*S\("F-P-'+short+r'".*$',text)
            assert authored,(short,kind)
            olds=re.findall(rx,authored[0]);assert olds,(short,kind)
            updated=re.sub(rx,lambda m:m[1]+str(value),authored[0])
            text=text[:authored.start()]+updated+text[authored.end():]
            # Re-find the semantic branch after changing an earlier line's length.
            matches=list(re.finditer(pattern,text));assert len(matches)==1
        for _,old in olds: nums[old]=str(value)
        line=re.sub(rx,lambda m:m[1]+str(value),line)
    text=text[:matches[0].start()]+line+text[matches[0].end():]
    replacements['F-P-'+short]=nums
text=text.replace('fire-personal-spells-v0.5-three-builds','fire-personal-spells-v0.6-full-balance')
src.write_text(text,encoding='utf-8',newline='')
copy=root/'UnityProject/Assets/Game/Runtime/Combat/FireSpellCardCopyCatalog.cs'
copytext=copy.read_text(encoding='utf-8-sig')
for spell,nums in replacements.items():
    rx=r'(?m)^.*"'+spell+r'".*$'
    def replace_line(m):
        return re.sub(r'\d+',lambda n:nums.get(n[0],n[0]),m[0][m[0].index(',')+1:])
    # Preserve the ID itself while replacing numeric effect prose.
    copytext=re.sub(rx,lambda m:m[0][:m[0].index(',')+1]+replace_line(m),copytext)
copy.write_text(copytext,encoding='utf-8',newline='')
art=root/'UnityProject/Assets/Game/Runtime/Combat/ArtifactCatalog.cs'
at=art.read_text(encoding='utf-8-sig')
at=at.replace('FireSpellRule(FireRuleKind.Damage, 16,','FireSpellRule(FireRuleKind.Damage, 10,')
at=at.replace('FireSpellRule(FireRuleKind.CreateFireground, 8, 3,','FireSpellRule(FireRuleKind.CreateFireground, 4, 2,')
artpatches={
 'DemolitionCanister':[('造成 16 点','造成 10 点'),('火场 3 来源','火场 2 来源'),('E(ArtifactEffectKind.Damage, 16,','E(ArtifactEffectKind.Damage, 10,'),('CreateFireground, 8, 3,','CreateFireground, 4, 2,')],
 'AegisFold':[('ItemRarity.Uncommon, 3,','ItemRarity.Uncommon, 2,'),('20 点','12 点'),('RestoreShield, 20','RestoreShield, 12')],
 'SurveyLens':[('ItemRarity.Common, 4,','ItemRarity.Common, 2,')],
 'BreachWedge':[('8 点伤害','12 点伤害'),('DamageObject, 8','DamageObject, 12')],
 'TurnLedger':[('ItemRarity.Rare, 2,','ItemRarity.Rare, 1,')],
 'AnchorBrace':[('ItemRarity.Uncommon, 4,','ItemRarity.Uncommon, 3,')],
 'DecoyLantern':[('12 耐久','8 耐久'),('DeployDecoy, 12','DeployDecoy, 8')],
 'FortuneSeal':[('16 点','10 点'),('Damage, 16','Damage, 10')]
}
for name,patches in artpatches.items():
    rx=r'(public static readonly ArtifactDefinition '+name+r' = A\(.*?)(?=\r?\n\s*public static readonly)'
    m=re.search(rx,at,re.S); assert m,name
    line=m[0]
    for old,new in patches:
        assert old in line,(name,old)
        line=line.replace(old,new)
    at=at[:m.start()]+line+at[m.end():]
art.write_text(at,encoding='utf-8',newline='')
enemy=root/'UnityProject/Assets/Game/Runtime/Combat/EnemyArchetypes.cs'
et=enemy.read_text(encoding='utf-8-sig')
for eid,hp in plan['hp'].items():
    rx=r'(?m)^.*new EnemyArchetype\("'+eid+r'".*$'
    m=re.search(rx,et); assert m,eid
    line=m[0]
    if 'maxHealth:' in line:
        line=re.sub(r'maxHealth: \d+',f'maxHealth: {hp}',line)
    else:
        line,n=re.subn(r'(true, )\d+(, ")',lambda m:m[1]+str(hp)+m[2],line)
        assert n==1,eid
    et=et[:m.start()]+line+et[m.end():]
enemy.write_text(et,encoding='utf-8',newline='')
data=root/'Worldbuilding/数据表'
for filename in ['OCC_学院敌人数据表_v1.0.csv','OCC_单位配置表_v1.0.csv']:
    path=data/filename
    with path.open(encoding='utf-8-sig',newline='') as f:
        reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
    for row in rows:
        eid=row.get('ID',row.get('单位ID'))
        if eid in plan['hp']:row['生命基线' if '生命基线' in row else '生命']=str(plan['hp'][eid])
    with path.open('w',encoding='utf-8',newline='') as f:
        writer=csv.DictWriter(f,fields,quoting=csv.QUOTE_ALL);writer.writeheader();writer.writerows(rows)
path=data/'OCC_技能配置表_v1.0.csv'
with path.open(encoding='utf-8-sig',newline='') as f:
    reader=csv.DictReader(f);fields=reader.fieldnames;rows=list(reader)
artifact_nums={'ARTIFACT-F-T01':{'16':'10','8':'4','3':'2'},'ARTIFACT-G-T01':{'20':'12'},'ARTIFACT-G-T08':{'8':'12'},'ARTIFACT-G-T15':{'12':'8'},'ARTIFACT-G-T19':{'16':'10'}}
uses={'ARTIFACT-G-T01':2,'ARTIFACT-G-T04':2,'ARTIFACT-G-T12':1,'ARTIFACT-G-T13':3}
for row in rows:
    eid=row['技能ID'];short=eid[4:] if eid.startswith('F-P-') else ''
    if short in plan['costs']:
        ap,mp,cd=plan['costs'][short];row['AP']=str(ap);row['魔力']=str(mp);row['冷却']=str(cd)+'自身回合'
    nums=replacements.get(eid,artifact_nums.get(eid))
    if nums:
        for field in ['卡面文案','效果链']:
            row[field]=re.sub(r'\d+',lambda n:nums.get(n[0],n[0]),row[field])
    if eid in uses:row['次数']=str(uses[eid])
    if eid in replacements or short in plan['costs'] or eid in artifact_nums or eid in uses:
        row['版本']='2'
        row['结算规则']=re.sub(r'状态：待固定种子验证；','',row['结算规则'])
        row['结算规则']+='；全目录数值版2026-10-06'
with path.open('w',encoding='utf-8',newline='') as f:
    writer=csv.DictWriter(f,fields,quoting=csv.QUOTE_ALL);writer.writeheader();writer.writerows(rows)
mirror=root/'Worldbuilding/策划案/OCC_项目总策划案_v1.0.md'
mt=mirror.read_text(encoding='utf-8-sig')
xml=(Path(__file__).parent/'总案数值补丁.xml').read_text(encoding='utf-8-sig')
paragraphs=re.findall(r'<p>(.*?)</p>',xml)
anchor='#### 3.5.6.6 技能强度、发动方式与生效时点'
assert mt.count(anchor)==1
mt=mt.replace(anchor,anchor+'\n\n'+'\n\n'.join(paragraphs))
for old,new in [('立即获得16点护盾','立即获得12点护盾'),('立即获得16点普通护盾','立即获得12点普通护盾'),('折盾匣提供一次20点护盾','折盾匣提供一次12点护盾'),('冒险封签对单体造成16点伤害','冒险封签对单体造成10点伤害'),('单件最多提供60点护盾','单件最多提供24点护盾'),('最多抵消4次推拉','最多抵消3次推拉')]: mt=mt.replace(old,new)
mirror.write_text(mt,encoding='utf-8',newline='')
# Catalog version tests follow the approved numerical revision, rather than requiring an obsolete version.
for path in (root/'UnityProject/Assets/Game/Tests/EditMode').glob('*.cs'):
    t=path.read_text(encoding='utf-8-sig')
    if 'fire-personal-spells-v0.5-three-builds' in t:
        path.write_text(t.replace('fire-personal-spells-v0.5-three-builds','fire-personal-spells-v0.6-full-balance'),encoding='utf-8',newline='')
print('Updated 28 personal spells, 8 artifacts, 7 enemy baselines; 80/20/17 catalog coverage pending verification.')
