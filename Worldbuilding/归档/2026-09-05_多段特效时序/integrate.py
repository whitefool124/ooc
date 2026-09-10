from pathlib import Path
import re
root=Path('E:/数据库/OCC_Codex')
def change(path, fn):
 p=root/path; original=p.read_text(encoding='utf-8-sig'); result=fn(original); assert original!=result,path; p.write_text(result,encoding='utf-8')
def rep(s,a,b):
 assert s.count(a)==1,(a[:90],s.count(a)); return s.replace(a,b)
def visual(s):
 s=s.replace('using System.Collections;\n','')
 s=rep(s,'''        private readonly Dictionary<GridPosition, GameObject> activeVfx = new Dictionary<GridPosition, GameObject>();
        private readonly Dictionary<GridPosition, int> activeVfxPriority = new Dictionary<GridPosition, int>();''','''        private readonly CombatVfxPlayback vfxPlayback = new CombatVfxPlayback();
        private readonly Dictionary<CombatVfxSlot, Image> activeVfx = new Dictionary<CombatVfxSlot, Image>();
        private readonly HashSet<CombatVfxSlot> visibleVfx = new HashSet<CombatVfxSlot>();
        private readonly List<CombatVfxSlot> expiredVfx = new List<CombatVfxSlot>();
        private RectTransform vfxRoot;''')
 s=rep(s,'            if (bootstrap == null || !bootstrap.IsDeveloperCombatActive || bootstrap.CurrentState == null) return;','''            if (bootstrap == null || !bootstrap.IsDeveloperCombatActive || bootstrap.CurrentState == null)
            { ClearVfx(); return; }
            RefreshVfx(Time.unscaledTime);''')
 a=s.index('        public void ResetBattleFeedback()'); b=s.index('        public void BeginEnemyAction(',a)
 s=s[:a]+'''        public void ResetBattleFeedback()
        {
            lastOutcome = null;
            healthCache.Clear(); shieldCache.Clear(); manaCache.Clear(); positionCache.Clear();
            durabilityCache.Clear(); statusCache.Clear(); hitUntil.Clear(); unitMotions.Clear(); activeUnitId = null;
            ClearVfx(); CancelEnemyAction();
            if (canvas != null)
            {
                foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                { DOTween.Kill(child); DOTween.Kill(child.gameObject); }
                foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
                    if (child != battlefieldClip) DestroyFeedbackObject(child.gameObject);
                if (battlefieldClip != null)
                    foreach (Transform child in battlefieldClip.Cast<Transform>().ToArray()) DestroyFeedbackObject(child.gameObject);
            }
            vfxRoot = null; activeDamagePopups.Clear(); damagePopupSerial = 0;
        }

        private void OnDisable() => ResetBattleFeedback();

        private void OnDestroy()
        {
            ResetBattleFeedback();
            if (canvas != null && canvas.gameObject != gameObject) DestroyFeedbackObject(canvas.gameObject);
        }

'''+s[b:]
 a=s.index('        public void NotifyFireSpell(');b=s.index('        public static IReadOnlyList<string> FireVfxModules',a)
 s=s[:a]+'''        public void NotifyFireSpell(FireSpellExecution execution)
        {
            if (execution?.Preview?.Spell == null || !AnimationsEnabled) return;
            UnitState sourceUnit = bootstrap?.CurrentState?.GetUnit(execution.SourceUnitId);
            GridPosition primary = execution.Preview.Cells.FirstOrDefault();
            if (!execution.IsTriggered && sourceUnit != null)
                PlayUnitMotion(sourceUnit, UnitMotionKind.Cast, .34f,
                    GridDirection(execution.SourcePosition, primary), Vector2.zero);
            vfxPlayback.ReplaceAbility(FireVfxSequence.From(execution), Time.unscaledTime);
            RefreshVfx(Time.unscaledTime);
        }

'''+s[b:]
 a=s.index('        private void PlayFormalVfx(');b=s.index('        private Sprite[] FormalVfxFrames',a)
 s=s[:a]+'''        private void PlayFormalVfx(GridPosition position, string effect)
        {
            if (!AnimationsEnabled) return;
            vfxPlayback.PlayReaction(position, effect, VfxPriority(effect), Time.unscaledTime);
            RefreshVfx(Time.unscaledTime);
        }

        private void RefreshVfx(float now)
        {
            if (!AnimationsEnabled) { ClearVfx(); unitMotions.Clear(); return; }
            IReadOnlyList<CombatVfxSample> samples = vfxPlayback.Sample(now);
            if (samples.Count > 0)
            {
                EnsureCanvas();
                if (vfxRoot == null)
                {
                    GameObject root = new GameObject("分层战斗特效", typeof(RectTransform));
                    vfxRoot = root.GetComponent<RectTransform>(); vfxRoot.SetParent(FeedbackParent, false);
                    vfxRoot.anchorMin = Vector2.zero; vfxRoot.anchorMax = Vector2.one;
                    vfxRoot.offsetMin = vfxRoot.offsetMax = Vector2.zero;
                    vfxRoot.SetAsFirstSibling();
                }
            }
            visibleVfx.Clear();
            foreach (CombatVfxSample sample in samples)
            {
                visibleVfx.Add(sample.Slot);
                if (!activeVfx.TryGetValue(sample.Slot, out Image view) || view == null)
                {
                    GameObject root = new GameObject("正式VFX", typeof(RectTransform), typeof(Image));
                    root.transform.SetParent(vfxRoot, false); view = root.GetComponent<Image>();
                    view.rectTransform.anchorMin = view.rectTransform.anchorMax = new Vector2(.5f, .5f);
                    view.preserveAspect = true; view.raycastTarget = false; view.color = new Color(1f, 1f, 1f, .88f);
                    activeVfx[sample.Slot] = view;
                }
                view.name = "正式VFX_" + sample.Cue.Effect;
                Sprite[] frames = FormalVfxFrames(sample.Cue.Effect);
                view.sprite = frames[Mathf.Clamp(Mathf.FloorToInt(sample.Progress * frames.Length), 0, frames.Length - 1)];
                Vector2 position = CurrentGridFeedbackPosition(sample.Cue.Position);
                if (sample.Cue.Travels)
                    position = Vector2.Lerp(CurrentGridFeedbackPosition(sample.Cue.Origin), position, sample.Progress);
                view.rectTransform.anchoredPosition = new Vector2(Mathf.Round(position.x / 2f) * 2f, Mathf.Round(position.y / 2f) * 2f);
                float size = CurrentFeedbackCellSize(); view.rectTransform.sizeDelta = new Vector2(size, size);
            }
            expiredVfx.Clear();
            foreach (var entry in activeVfx)
                if (!visibleVfx.Contains(entry.Key)) expiredVfx.Add(entry.Key);
                else if (entry.Key.Layer == CombatVfxLayer.Reaction) entry.Value.transform.SetAsLastSibling();
            foreach (CombatVfxSlot slot in expiredVfx)
            { if (activeVfx[slot] != null) DestroyFeedbackObject(activeVfx[slot].gameObject); activeVfx.Remove(slot); }
        }

        private void ClearVfx()
        {
            vfxPlayback.Clear();
            foreach (Image view in activeVfx.Values) if (view != null) DestroyFeedbackObject(view.gameObject);
            activeVfx.Clear(); visibleVfx.Clear(); expiredVfx.Clear();
            if (battlefieldClip != null)
                foreach (Transform child in battlefieldClip.Cast<Transform>().Where(child => child.name == "战斗反馈脉冲").ToArray())
                { DOTween.Kill(child.gameObject); DestroyFeedbackObject(child.gameObject); }
        }

        private static void DestroyFeedbackObject(GameObject value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

'''+s[b:]
 s=rep(s,'DOVirtual.DelayedCall(.42f, () => { if (textObject != null) Destroy(textObject); }).SetUpdate(true);','DOVirtual.DelayedCall(.42f, () => { if (textObject != null) Destroy(textObject); }).SetUpdate(true).SetTarget(textObject);')
 s=rep(s,'Sequence sequence = DOTween.Sequence().SetUpdate(true);','Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(textObject);')
 s=rep(s,'DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .14f).SetUpdate(true)','DOTween.To(() => group.alpha, value => group.alpha = value, 0f, .14f).SetUpdate(true).SetTarget(banner)')
 return re.sub(r'(?<![.\w])Destroy\(', 'DestroyFeedbackObject(',s)
change('UnityProject/Assets/Game/Runtime/Presentation/CombatVisualFeedback.cs',visual)
def publisher(s):
 s=rep(s,'void NotifyFireSpell(FireSpellDefinition spell, GridPosition source,\n            IReadOnlyList<GridPosition> targetCells, bool isTriggered = false);','void NotifyFireSpell(FireSpellExecution execution);')
 return rep(s,'sink?.NotifyFireSpell(spell, execution.SourcePosition,\n                        execution.Preview.Cells, execution.IsTriggered);','sink?.NotifyFireSpell(execution);')
change('UnityProject/Assets/Game/Runtime/Presentation/CombatFeedbackPublisher.cs',publisher)
def bootstrap(s):
 s=rep(s,'if (preview.NativeResult is FireSpellPreview firePreview && trainingRangeSession.CurrentFireSpell != null)\n                visualFeedback?.NotifyFireSpell(trainingRangeSession.CurrentFireSpell, source, firePreview.Cells);','if (report.NativeResult is FireSpellExecution fireExecution)\n                visualFeedback?.NotifyFireSpell(fireExecution);')
 return rep(s,'visualFeedback?.NotifyFireSpell(spell, source, preview.Cells);','visualFeedback?.NotifyFireSpell(execution);')
change('UnityProject/Assets/Game/Runtime/Presentation/CombatPrototypeBootstrap.cs',bootstrap)
def tests(s):
 return rep(s,'''public void NotifyFireSpell(FireSpellDefinition spell, GridPosition source,
                IReadOnlyList<GridPosition> targetCells, bool isTriggered = false)
            { FireSources.Add(source); FireTargets.Add(targetCells); FireTriggers.Add(isTriggered); }''','''public void NotifyFireSpell(FireSpellExecution execution)
            { FireSources.Add(execution.SourcePosition); FireTargets.Add(execution.Preview.Cells); FireTriggers.Add(execution.IsTriggered); }''')
change('UnityProject/Assets/Game/Tests/EditMode/CombatFeedbackPublisherTests.cs',tests)
print('Integrated result-driven layered VFX playback and reset cleanup')
