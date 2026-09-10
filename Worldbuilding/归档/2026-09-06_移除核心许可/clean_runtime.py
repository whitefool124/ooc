from pathlib import Path
import json, re, shutil

ROOT = Path('E:/数据库/OCC_Codex')
AUDIT = ROOT / 'Worldbuilding/归档/2026-09-06_移除核心许可'
changed = {}

def read(path):
    if path not in changed:
        changed[path] = (ROOT / path).read_text(encoding='utf-8-sig')
    return changed[path]

def put(path, text):
    changed[path] = text

def replace(path, old, new, count=None):
    text = read(path)
    actual = text.count(old)
    assert actual and (count is None or actual == count), (path, old, actual)
    put(path, text.replace(old, new))

R = 'UnityProject/Assets/Game/Runtime/'
M = R + 'Campaign/RogueliteMapRun.cs'
C = R + 'Campaign/AcademyNodeContentCatalog.cs'
P = R + 'Campaign/UiPresentationModels.cs'
V = R + 'Roguelite/RoguePresentationModels.cs'
U = R + 'Presentation/FormalRogueliteUi.cs'

# Canonical node/choice names. Historical spellings are handled only at save input.
for path in (ROOT / 'UnityProject/Assets/Game').rglob('*.cs'):
    rel = path.relative_to(ROOT).as_posix()
    original = path.read_text(encoding='utf-8-sig')
    if 'permit_archive' in original or 'EV16_permit' in original:
        put(rel, original.replace('permit_archive', 'records_archive').replace('EV16_permit', 'EV16_assessment'))

replace(M, '        public const int CorePermitRequirement = 2;\n', '')
replace(M, '        public int RequiredAccessCards { get; }\n        public int GrantedAccessCards { get; }\n', '')
replace(M, ', int requiredAccessCards, int grantedAccessCards, params string[] nextIds', ', params string[] nextIds')
replace(M, '            RequiredAccessCards = requiredAccessCards; GrantedAccessCards = grantedAccessCards; NextIds = nextIds ?? Array.Empty<string>();',
        '            NextIds = nextIds ?? Array.Empty<string>();')
for path in [M, R + 'Campaign/FirstRunExperience.cs']:
    text, count = re.subn(r'(new RogueliteMapNode\([^\n]+?,\s*-?\d+,\s*-?\d+),\s*\d+,\s*\d+(,\s*"[^\n]+)', r'\1\2', read(path))
    assert count == (40 if path == M else 11), (path, count)
    put(path, text)
replace(M, 'Supplies, ScoutingBeacon, AccessCard, Reward, Aether, Recovery, Economy, Intelligence',
        'Supplies = 0, ScoutingBeacon = 1, Reward = 3, Aether = 4, Recovery = 5, Economy = 6, Intelligence = 7')
replace(M, '        public bool GrantsCorePermit { get; }\n', '')
replace(M, 'int healthGain = 0, int manaGain = 0,\n            bool grantsCorePermit = false)', 'int healthGain = 0, int manaGain = 0)')
replace(M, 'HealthGain = healthGain; ManaGain = manaGain; GrantsCorePermit = grantsCorePermit;', 'HealthGain = healthGain; ManaGain = manaGain;')
replace(M, '收益：+1 权限卡；后果：进入一场额外战斗。", RogueliteNodeContentEffect.AccessCard',
        '进入一场已公开的额外战斗，完成后结算节点。", RogueliteNodeContentEffect.Economy')
replace(M, '"许可档案", "帮管理员处理积压记录，可以换到一枚核心许可。"', '"档案整理", "帮助管理员整理积压的学院记录。"')
replace(M, '只有持有核心许可的人才能进入塔楼。', '沿相连道路前往塔楼，处理塔内的异常。')
replace(M, '完成这场艰难考察，可以拿到一枚核心许可。', '完成这场艰难考察，领取精英战奖励。')
replace(M, '管理员让你在稀有法宝和核心许可之间选一个。', '管理员允许你领取一件封存的法宝。')
replace(M, '        public int AccessCards { get; private set; }\n', '')
replace(M, '        public int CorePermits => IsFirstRunExperience ? (FirstRunExperience.EliteRewardClaimed ? 1 : 0) : completed.Count(id => RogueliteMapCatalog.Node(id).GrantedAccessCards > 0) +\n            claimedRewards.Count(id => id.StartsWith("permit:", StringComparison.Ordinal));\n        public int ProgressPermits => UsesRogue11 ? CorePermits : AccessCards;\n', '')
replace(M, '(CompletedAcademyNodeCount >= AcademyMapTuning.BossMinimumProgress && CorePermits >= AcademyMapTuning.CorePermitRequirement)', 'CompletedAcademyNodeCount >= AcademyMapTuning.BossMinimumProgress')
replace(M, '!IsAdjacentToCurrent(nodeId) || ProgressPermits < node.RequiredAccessCards', '!IsAdjacentToCurrent(nodeId)')
replace(M, '(ProgressPermits < node.RequiredAccessCards || IsAcademyFinaleGateLocked(node))', 'IsAcademyFinaleGateLocked(node)')
replace(M, 'Academy finale requires 12 explored nodes and 2 core permits.', 'Academy finale requires 12 completed nodes.')
replace(M, '                if (choice.GrantsCorePermit)\n                {\n                    string permitId = "permit:" + contentSourceId;\n                    if (!claimedRewards.Contains(permitId)) claimedRewards.Add(permitId);\n                }\n', '')
replace(M, '            else if (choice.Effect == RogueliteNodeContentEffect.AccessCard) AccessCards++;\n', '')
replace(M, '            AccessCards += node.GrantedAccessCards;\n', '')
replace(M, 'Level, Experience, AccessCards, Supplies', 'Level, Experience, 0, Supplies')
put(M, re.sub(r'AccessCards = int.Parse\(parts\[\d+\]\), ', '', read(M)))
replace(M, '            if (run.visited.Contains("core_finale")) run.AccessCards = 1;\n', '')

# Normalize old wire IDs and retire collected progress markers without rerolling content.
replace(M, 'CurrentNodeId = dto.CurrentNodeId,', 'CurrentNodeId = AcademyMapSaveMigration.NodeId(dto.CurrentNodeId),')
replace(M, 'PendingContentChoiceId = dto.PendingContentChoiceId,', 'PendingContentChoiceId = AcademyMapSaveMigration.ChoiceId(dto.PendingContentChoiceId),')
replace(M, 'run.claimedRewards.AddRange(dto.ClaimedContentIds);', 'run.claimedRewards.AddRange(dto.ClaimedContentIds.Where(id => !AcademyMapSaveMigration.IsRetiredProgressMarker(id)));')
replace(M, 'new RogueliteEncounterAssignment(row.Substring(0, separator), row.Substring(separator + 1))', 'new RogueliteEncounterAssignment(AcademyMapSaveMigration.NodeId(row.Substring(0, separator)), row.Substring(separator + 1))')
replace(M, 'new AcademyEventAssignment(row.Substring(0, separator), row.Substring(separator + 1))', 'new AcademyEventAssignment(AcademyMapSaveMigration.NodeId(row.Substring(0, separator)), row.Substring(separator + 1))')
replace(M, '        public static RogueliteMapRun FromLegacyMap10(string json)\n        {', '''        public static RogueliteMapRun FromLegacyMap10(string json)
        {
            RogueliteMapRun run = ReadLegacyMapData(json);
            run.CurrentNodeId = AcademyMapSaveMigration.NodeId(run.CurrentNodeId);
            run.PendingContentChoiceId = AcademyMapSaveMigration.ChoiceId(run.PendingContentChoiceId);
            run.claimedRewards.RemoveAll(AcademyMapSaveMigration.IsRetiredProgressMarker);
            return run;
        }
        private static RogueliteMapRun ReadLegacyMapData(string json)
        {''')
replace(M, 'source.Split(new[] { \',\' }, StringSplitOptions.RemoveEmptyEntries)) if',
        'source.Split(new[] { \',\' }, StringSplitOptions.RemoveEmptyEntries).Select(AcademyMapSaveMigration.NodeId)) if')

replace(C, 'int healthGain = 0, int manaGain = 0,\n            bool permit = false)', 'int healthGain = 0, int manaGain = 0)')
replace(C, 'manaGain: manaGain, grantsCorePermit: permit', 'manaGain: manaGain')
replace(C, ', permit:true', '')
replace(C, '打完一场演练。赢了得核心许可；输了没有许可。', '完成档案库演练，胜利结算 3 金币与 2 学院贡献；存活失败只得一半。')
replace(C, '赶去打完一场救援演练。赢了得核心许可；输了没有许可。', '完成救援演练，胜利结算 3 金币与 2 学院贡献；存活失败只得一半。')
replace(C, '打赢维护队就能拿到核心许可；输了没有许可。', '完成维护队考核，胜利结算 3 金币与 2 学院贡献；存活失败只得一半。')
replace(C, '领取防御训练许可', '领取防御训练器材')
replace(C, 'new RogueliteNodeContentChoice("vault_fire_cache", "拿走冒险封签", "带走冒险封签，就不能再拿核心许可；不花时间。", RogueliteNodeContentEffect.Reward, "G-T19"),\n                        new RogueliteNodeContentChoice("vault_core_permit", "拿走核心许可", "带走 1 枚核心许可，就不能再拿冒险封签；不花时间。", RogueliteNodeContentEffect.AccessCard, grantsCorePermit:true)',
        'new RogueliteNodeContentChoice("vault_fire_cache", "拿走冒险封签", "领取冒险封签，结算此节点。", RogueliteNodeContentEffect.Reward, "G-T19")')
replace(C, 'AcademyEventDefinition selected = remaining.FirstOrDefault(value =>\n                    node.GrantedAccessCards <= 0 || value.Choices.All(choice => !choice.GrantsCorePermit));\n                if (selected == null) throw new InvalidOperationException("Academy event assignment cannot avoid a duplicate permit source.");',
        'AcademyEventDefinition selected = remaining.First();')

replace(P, '        public int AccessCards { get; }\n', '')
replace(P, '            AccessCards = run.AccessCards;\n', '')
replace(P, '            AccessCards == other.AccessCards && AwaitingReward == other.AwaitingReward;', '            AwaitingReward == other.AwaitingReward;')
replace(P, '" 个地点，并拿到 " + Math.Max(0, AcademyMapTuning.CorePermitRequirement - run.CorePermits) + " 枚核心许可"', '" 个地点"')
replace(P, '            if (state == RogueliteMapNodeVisualState.Locked) return "核心许可不足：需要 " + node.RequiredAccessCards + "，当前 " + run.ProgressPermits;\n', '')
replace(P, '" 个地点并拿到 " +\n                Math.Max(0, AcademyMapTuning.CorePermitRequirement - run.CorePermits) + " 枚许可"', '" 个地点"')
replace(P, '" · 核心许可 " + run.CorePermits + "/" + AcademyMapTuning.CorePermitRequirement + " · " + finale', '" · " + finale')
replace(P, '            if (choice.GrantsCorePermit) outcomes.Add("核心许可");\n', '')
replace(P, '            if (choice.RequiresCombat) outcomes.Insert(0, outcomes.Count == 0 ? "进入战斗" : "胜利");', '''            if (choice.RequiresCombat)
            {
                if (run?.UsesRogue11 == true) outcomes.Add("3金 + 2学院贡献");
                outcomes.Insert(0, outcomes.Count == 0 ? "进入战斗" : "胜利");
            }''')
replace(V, '        public int CorePermits { get; }\n        public int RequiredCorePermits { get; private set; } = AcademyMapTuning.CorePermitRequirement;\n', '')
replace(V, 'ExploredNodes = run.AcademyProgress; CorePermits = run.CorePermits;', 'ExploredNodes = run.CompletedAcademyNodeCount;')
replace(V, '                RequiredCorePermits = 1;\n', '')
replace(V, 'ExploredNodes >= RequiredExploredNodes && CorePermits >= RequiredCorePermits', 'ExploredNodes >= RequiredExploredNodes')
replace(V, ': node.GrantedAccessCards > 0 ? "核心许可" : "这里能找到的东西"', ': "这里能找到的东西"')
put(U, '\n'.join(line for line in read(U).split('\n') if '核心许可' not in line and '权限卡' not in line))
replace(R + 'Presentation/CombatPrototypeBootstrap.cs', '            PublishResourceChange("权限卡", before.AccessCards, after.AccessCards);\n', '')
I = R + 'Campaign/RogueliteMapInteractionService.cs'
replace(I, '        public int AccessCards { get; }\n', '')
replace(I, ', int accessCards, bool usesRogue11', ', bool usesRogue11')
replace(I, '            AccessCards = accessCards;\n', '')
replace(I, 'run.ScoutingBeacons, run.AccessCards, run.UsesRogue11', 'run.ScoutingBeacons, run.UsesRogue11')
F = R + 'Combat/FormalArtRegistry.cs'
put(F, '\n'.join(line for line in read(F).split('\n') if 'core_permit' not in line))
E = R + 'Campaign/RogueliteEncounterCatalog.cs'
replace(E, '可以从护障、显影或盾线中的任意一处切入；赢了才能拿到许可', '可以从护障、显影或盾线中的任意一处切入；胜利结算金币与学院贡献')
replace(E, '"核心许可"', '"金币与学院贡献"')
Q = R + 'Campaign/RogueliteMapRunValidator.cs'
replace(Q, ' || run.AccessCards < 0', '')
replace(Q, '!RewardIds.Contains(id) && !(id.StartsWith("permit:", StringComparison.Ordinal) && AcademyNodeContentCatalog.Events.Any(value => "permit:" + value.Id == id))', '!RewardIds.Contains(id)')

# Synchronize the local mother document from the newly verified remote content.
master = json.loads((AUDIT / 'master-after.json').read_text(encoding='utf-8-sig'))
assert master['ok'] and '核心许可' not in master['data']['document']['content']
put('Worldbuilding/策划案/OCC_项目总策划案_v1.0.md', master['data']['document']['content'].rstrip() + '\n')
D = 'Worldbuilding/数据表/OCC_核心规则数值表_v1.0.csv'
replace(D, '完成12节点并取得2核心许可', '完成12个节点')
D = 'Worldbuilding/数据表/OCC_学院节点内容数据表_v1.0.csv'
replace(D, '奖励或许可', '奖励')
replace(D, '许可/消耗品＋强池档', '消耗品＋强池档')
replace(D, '胜利获核心许可', '胜利结算3金币与2学院贡献')
replace(D, '冒险封签与核心许可二选一；不作为宝藏节点', '领取冒险封签；不作为宝藏节点')
D = 'Tools/RogueliteDecisionConsole/app/decision-data.ts'
replace(D, '低于 28 的提前首领门槛为 12 个已处理节点 + 2 枚核心许可。', '低于 28 的提前首领门槛为 12 个已完成节点。')
D = 'Artifacts/OpeningTenMinutes/OCC_开局十分钟体验工作稿_v0.1.md'
replace(D, '- 核心许可 1 份，固定获得，用于开启下一层学院内容。', '- 精英胜利并完成结算后开放商店。')
replace(D, '核心许可；灰炉导杖／低压回路护额／缚位框固定三选一', '灰炉导杖／低压回路护额／缚位框固定三选一')
replace(D, '胜利固定获得核心许可，并从灰炉导杖、低压回路护额、缚位框中三选一。', '胜利后从灰炉导杖、低压回路护额、缚位框中三选一。')

for path, content in changed.items():
    target = ROOT / path
    original = target.read_bytes()
    backup = AUDIT / 'before' / path
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists(): shutil.copy2(target, backup)
    newline = '\r\n' if b'\r\n' in original else '\n'
    target.write_bytes(content.replace('\n', newline).encode('utf-8-sig' if original.startswith(b'\xef\xbb\xbf') else 'utf-8'))
(AUDIT / 'changed-files.json').write_text(json.dumps(sorted(changed), ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Updated {len(changed)} files; pre-change copies preserved.')
