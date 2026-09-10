from pathlib import Path
import re, shutil, json
ROOT = Path('E:/数据库/OCC_Codex')
AUDIT = ROOT / 'Worldbuilding/归档/2026-09-06_移除核心许可'
changes = {}
def read(p):
    if p not in changes: changes[p] = (ROOT / p).read_text(encoding='utf-8-sig')
    return changes[p]
def replace(p, old, new):
    assert old in read(p), (p, old)
    changes[p] = read(p).replace(old, new)
def remove_lines(p, token):
    assert token in read(p), (p, token)
    changes[p] = '\n'.join(line for line in read(p).split('\n') if token not in line)
T = 'UnityProject/Assets/Game/Tests/EditMode/'
R = 'UnityProject/Assets/Game/Runtime/'
p = T + 'AcademyMapTuningTests.cs'
replace(p, 'AcademyFinale_RequiresBothPublishedThresholdsAndExplainsTheGap', 'AcademyFinale_RequiresCompletedNodesAndExplainsTheGap')
remove_lines(p, 'Assert.That(run.CorePermits')
replace(p, '还不能参加终考：再完成 4 个地点，并拿到 0 枚核心许可', '还不能参加终考：再完成 4 个地点')
replace(p, 'AcademyFinale_RemainsLockedAtEnoughProgressWithOnlyOneCorePermit', 'AcademyFinale_OpensAtEnoughCompletedProgressWithoutAnItemRequirement')
replace(p, 'Assert.That(run.IsNodeAvailable("core_finale"), Is.False);\n            Assert.That(RogueliteMapVisualPresentation.RestrictionText',
    'Assert.That(run.IsNodeAvailable("core_finale"), Is.True);\n            Assert.That(RogueliteMapVisualPresentation.RestrictionText')
replace(p, '还不能参加终考：再完成 0 个地点，并拿到 1 枚核心许可', '可以直接前往')
remove_lines(p, 'node.GrantedAccessCards')

p = T + 'AcademyNodeContentPlayableTests.cs'
text, count = re.subn(r'        \[Test\]\n        public void FixedPermitNode_NeverReceivesAnEventThatCanGrantASecondPermit\(\)\n        \{.*?\n        \}\n\n', '', read(p), flags=re.S)
assert count == 1
changes[p] = text
replace(p, 'Assert.That(combatSummary, Does.Contain("核心许可"));', 'Assert.That(combatSummary, Does.Contain("3金 + 2学院贡献"));')
replace(p, 'int goldBefore = run.Gold, contributionBefore = run.StageContribution, permitsBefore = run.CorePermits;', 'int goldBefore = run.Gold, contributionBefore = run.StageContribution;')
replace(p, 'EventCombatVictory_AppliesUniqueRewardOrPermitOnlyAfterVictory', 'EventCombatVictory_SettlesBaseCurrencyOnlyAfterVictory')
replace(p, '            int permitsBefore = run.CorePermits;\n            run.ChooseCurrentNodeContent("EV03_fight");\n            Assert.That(run.CorePermits, Is.EqualTo(permitsBefore));',
    '            int goldBefore = run.Gold, contributionBefore = run.StageContribution;\n            run.ChooseCurrentNodeContent("EV03_fight");\n            Assert.That(run.Gold, Is.EqualTo(goldBefore));\n            Assert.That(run.StageContribution, Is.EqualTo(contributionBefore));')
replace(p, '            Assert.That(run.CorePermits, Is.EqualTo(permitsBefore + 1));\n            Assert.That(run.ClaimedRewards.Count(id => id == "permit:EV03"), Is.EqualTo(1));',
    '            Assert.That(run.Gold, Is.EqualTo(goldBefore + 3));\n            Assert.That(run.StageContribution, Is.EqualTo(contributionBefore + 2));\n            Assert.That(run.HasPendingContentCombat, Is.False);\n            Assert.Throws<InvalidOperationException>(() => run.CompletePendingContentCombat());')
remove_lines(p, 'CorePermits')
replace(p, 'Assert.That(AcademyNodeContentCatalog.FunctionChoices(towerEvent).Any(value => value.GrantsCorePermit), Is.True);',
    'Assert.That(AcademyNodeContentCatalog.FunctionChoices(towerEvent).Single().RewardId, Is.EqualTo("G-T19"));')
p = T + 'RogueliteMapSaveCoordinatorTests.cs'
remove_lines(p, 'CorePermits')
p = T + 'RogueliteCompletionLineTests.cs'
replace(p, 'Assert.That(run.AccessCards, Is.GreaterThanOrEqualTo(1));', 'Assert.That(run.CompletedNodes, Does.Contain("records_archive"));')
p = T + 'RogueliteDeveloperRunTests.cs'
replace(p, 'MapRun_VisitedRoomsCanBeRevisitedAndPermissionGateNeedsCard', 'MapRun_VisitedRoomsCanBeRevisitedAndConnectedTowerCanBeEntered')
replace(p, 'MapRun_VisualStatesDescribeCurrentReachabilityAndPermissionGates', 'MapRun_VisualStatesDescribeCurrentReachability')
replace(p, 'MapRun_RiskyEventOnlyStartsDisclosedCombatThenGrantsCard', 'MapRun_RiskyEventOnlyCompletesAfterDisclosedCombat')
replace(p, 'Assert.That(RogueliteMapRun.FromJson(run.ToJson()).AccessCards, Is.EqualTo(1));', 'Assert.That(RogueliteMapRun.FromJson(run.ToJson()).CurrentNodeId, Is.EqualTo("transmission_tower"));')
replace(p, 'Assert.That(run.AccessCards, Is.EqualTo(0));', 'Assert.That(run.CompletedNodes, Does.Not.Contain("switchyard"));')
remove_lines(p, 'run.AccessCards')
replace(p, 'run.VisualStateFor("transmission_tower"), Is.EqualTo(RogueliteMapNodeVisualState.Locked)', 'run.VisualStateFor("transmission_tower"), Is.EqualTo(RogueliteMapNodeVisualState.Available)')
p = T + 'FormalArtRegistryTests.cs'
replace(p, 'FormalArtRegistry.ResourceMetrics.Count, Is.EqualTo(16)', 'FormalArtRegistry.ResourceMetrics.Count, Is.EqualTo(15)')
p = R + 'Campaign/AcademyNodeContentCatalog.cs'
replace(p, '存活失败只得一半', '输了且存活时只得一半')

p = R + 'Roguelite/RoguePresentationModels.cs'
replace(p, 'ExploredNodes = run.CompletedAcademyNodeCount;', 'ExploredNodes = run.IsFirstRunExperience ? run.CompletedNodes.Count : run.CompletedAcademyNodeCount;')
for p in [p, R + 'Presentation/FormalRogueliteUi.cs']:
    replace(p, 'ExploredNodes', 'CompletedNodes')
p = R + 'Presentation/FormalRogueliteUi.cs'
replace(p, '"走过"', '"已完成"')
# Reflow the six remaining resource chips across the same status area.
lines = read(p).split('\n')
for index, line in enumerate(lines):
    if 'MetricChip(status.transform,' not in line: continue
    for old, new in [(1052,1232),(844,988),(636,744),(428,500),(220,256)]:
        line = line.replace(f'transform, {old}, -16', f'transform, {new}, -16')
    lines[index] = line.replace(', 198);', ', 232);')
changes[p] = '\n'.join(lines)
p = 'Tools/Art/generate_rogue_ui_semantic_assets.py'
text, count = re.subn(r'    elif name == "core_permit":\n.*?(?=    elif name == "risk":)', '', read(p), flags=re.S)
assert count == 1
changes[p] = text.replace('"explored", "core_permit",', '"explored",')
p = 'Tools/OCCArt/prepare_element_resource_icons_24.py'
remove_lines(p, '"core_permit"')

for path, content in changes.items():
    target = ROOT / path
    original = target.read_bytes()
    backup = AUDIT / 'before' / path
    backup.parent.mkdir(parents=True, exist_ok=True)
    if not backup.exists(): shutil.copy2(target, backup)
    newline = '\r\n' if b'\r\n' in original else '\n'
    target.write_bytes(content.replace('\n', newline).encode('utf-8-sig' if original.startswith(b'\xef\xbb\xbf') else 'utf-8'))
print('Updated', len(changes), 'files')
