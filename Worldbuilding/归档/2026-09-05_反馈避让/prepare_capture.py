from pathlib import Path
root = Path(__file__).resolve().parents[3]
source = (root/'Worldbuilding/归档/2026-09-05_寻迹兽轮廓返修/RenderCandidate.cs').read_text(encoding='utf-8-sig')
source = source.replace('const bool USE_CANDIDATE = true;', 'const bool USE_CANDIDATE = false;')
source = source.replace('const int width = 1920, height = 1080;', 'const int width = 1920, height = 1080;')
needle = 'var hud = go.AddComponent<FormalCombatHud>(); hud.Initialize(bootstrap); Call(hud, "Refresh");'
assert needle in source
source = source.replace(needle, needle + '''
            var feedback = go.AddComponent<CombatVisualFeedback>(); feedback.Initialize(bootstrap); feedback.enabled = false;
            var target = bootstrap.CurrentState.GetUnit("hero").Position;
            Call(feedback, "ShowFloatingText", target, "束缚 2", Color.cyan, "bound");
            Call(feedback, "ShowFloatingText", target, "破甲 2", Color.red, "armor_break");
            Call(feedback, "ShowFloatingText", target, "迟缓 2", Color.yellow, "slow");
            Call(feedback, "ShowDamagePopup", new CombatFeedbackEvent(CombatFeedbackKind.Damage, target, 6));
''')
source = source.replace('return width +', 'return width +')
(Path(__file__).parent/'RenderFeedback.cs').write_text(source, encoding='utf-8')
print('Prepared production static feedback capture')
