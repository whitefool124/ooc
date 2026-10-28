#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class AcademyTutorialVisualProbe : MonoBehaviour
    {
        private IDisposable scope;
        private string folder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Attach()
        {
            string path = UnityEditor.SessionState.GetString("OCC.Teaching.VisualProbe", "");
            if (string.IsNullOrEmpty(path)) return;
            UnityEditor.SessionState.EraseString("OCC.Teaching.VisualProbe");
            var obj = new GameObject("Isolated teaching visual verification"); DontDestroyOnLoad(obj);
            var probe = obj.AddComponent<AcademyTutorialVisualProbe>(); probe.folder = path;
            probe.scope = FirstExperienceSaveRouting.BeginVerificationScope("teaching-visual-" + DateTime.UtcNow.Ticks);
            FirstExperienceSaveRouting.SelectSlot(0);
        }
        private IEnumerator Start()
        {
            Application.runInBackground = true; Directory.CreateDirectory(folder);
            var work = Verify(); string failure = null;
            while (true)
            {
                bool next; object value = null;
                try { next = work.MoveNext(); if (next) value = work.Current; }
                catch (Exception error) { failure = error.ToString(); break; }
                if (!next) break;
                yield return value;
            }
            File.WriteAllText(Path.Combine(folder, "visual-check.txt"), (failure == null ? "PASS" : "FAIL\n" + failure) + "\n\nActive buttons\n" + string.Join("\n", FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b => b.gameObject.activeInHierarchy).Select(b => b.name + "  " + b.GetComponentInChildren<Text>()?.text)));
            scope?.Dispose(); scope = null;
            UnityEditor.EditorApplication.isPlaying = false;
        }
        private IEnumerator Verify()
        {
            yield return new WaitForSecondsRealtime(5);
            var host = FindAnyObjectByType<CombatPrototypeBootstrap>(); var opening = FindAnyObjectByType<FirstExperiencePrototypeController>();
            if (host == null || opening == null) throw new InvalidOperationException("Normal scene did not initialize.");
            opening.ShowLanding(); yield return new WaitForSecondsRealtime(.6f);
            opening.StartGameFromDesk(); yield return new WaitForSecondsRealtime(.6f);
            if (opening.DebugStage != "Configuration" || opening.FixedFirstExperienceEnabled || !opening.OperationTutorialSelected) throw new InvalidOperationException("New-save defaults are incorrect: " + opening.DebugStage);
            Capture("01_configuration");
            Click(FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "按钮_固定首次体验开关")); yield return new WaitForSecondsRealtime(.2f);
            if (!opening.FixedFirstExperienceEnabled || !opening.OperationTutorialSelected) throw new InvalidOperationException("Fixed-route switch changed teaching.");
            Capture("01b_fixed_optional");
            Click(FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "按钮_固定首次体验开关")); yield return new WaitForSecondsRealtime(.2f);
            ClickText("以此角色开始"); yield return new WaitForSecondsRealtime(.6f);
            ClickText("进入学院地图"); yield return new WaitForSecondsRealtime(2);
            if (host.CurrentMapRun == null || host.CurrentMapRun.IsTutorialPhase || !AcademyTutorialStore.Load().enabled) throw new InvalidOperationException("Normal route or contextual teaching did not enter.");
            Capture("02_gift");
            var gift = FindAnyObjectByType<LampkeeperGiftView>();
            if (gift == null) throw new InvalidOperationException("Normal opening gift was not visible.");
            int index = Array.FindIndex(host.CurrentMapRun.OpeningGift.CandidateIds, id => id != "LK-05"); Click(gift.ChoiceButtons[index]);
            yield return new WaitForSecondsRealtime(1);
            Capture("03_departure");
            var depart = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.gameObject.activeInHierarchy && b.IsInteractable() && (b.GetComponentInChildren<Text>()?.text ?? "").Contains("确认出发"));
            if (depart == null) throw new InvalidOperationException("Normal departure control was not found.");
            Click(depart); yield return new WaitForSecondsRealtime(1); Capture("04_map");
            var teaching = host.GetComponent<AcademyTutorialPresenter>();
            string before = AcademyTutorialObservation.Fingerprint(host.CurrentMapRun);
            teaching.OpenReview(); yield return new WaitForSecondsRealtime(.5f); Capture("05_review");
            for (int i = 0; i < 10; i++)
            {
                var choice = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "教学索引_E12");
                if (choice != null) { Click(choice); break; }
                ClickText("下一页"); yield return new WaitForSecondsRealtime(.1f);
            }
            yield return new WaitForSecondsRealtime(.3f); ClickText("开始隔离演练"); yield return new WaitForSecondsRealtime(.3f); Capture("06_practice_preview");
            var execute = FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "执行演练"); Click(execute); yield return new WaitForSecondsRealtime(.3f); Capture("07_practice_result");
            if (AcademyTutorialObservation.Fingerprint(host.CurrentMapRun) != before) throw new InvalidOperationException("Practice changed the live run.");
            Click(FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "退出演练"));
            yield return new WaitForSecondsRealtime(.3f);
            ClickText("暂停或恢复提示"); yield return new WaitForSecondsRealtime(.2f);
            if (AcademyTutorialStore.Load().enabled) throw new InvalidOperationException("Teaching pause did not persist.");
            ClickText("暂停或恢复提示"); yield return new WaitForSecondsRealtime(.2f);
            if (!AcademyTutorialStore.Load().enabled) throw new InvalidOperationException("Teaching resume did not persist.");
            teaching.CloseReview(); yield return new WaitForSecondsRealtime(.5f);
            var combatNode = host.CurrentMapRun.AvailableNodes.First(n => n.Type == RogueliteMapNodeType.Combat);
            Click(FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.name == "map.node." + combatNode.Id));
            yield return new WaitForSecondsRealtime(.6f); Capture("08_combat_briefing");
            Click(FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.gameObject.activeInHierarchy && b.name == "按钮_进入战斗"));
            for (int i = 0; i < 90 && (!host.IsDeveloperCombatActive || host.IsCombatEntryBlocking); i++) yield return new WaitForSecondsRealtime(.4f);
            if (!host.IsDeveloperCombatActive || host.IsCombatEntryBlocking) throw new InvalidOperationException("Normal combat entrance did not complete.");
            yield return new WaitForSecondsRealtime(1); Capture("09_first_combat_objective");
            var spotlight = host.GetComponent<CombatTutorialPresenter>();
            for (int i = 0; i < 4; i++)
            {
                var next = FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.name == "教学继续" && b.gameObject.activeInHierarchy);
                if (next == null) break;
                Click(next); yield return new WaitForSecondsRealtime(.7f);
            }
            Capture("10_first_combat_operation");
            if (spotlight != null && spotlight.BlocksInput) throw new InvalidOperationException("Combat tutorial did not release the player to act.");
            var hero = host.CurrentState.GetUnit("hero"); int ap = hero.ActionPoints;
            GridPosition destination = Enumerable.Range(0, host.CurrentState.Map.Width)
                .SelectMany(x => Enumerable.Range(0, host.CurrentState.Map.Height).Select(y => new GridPosition(x, y)))
                .First(p => p.ManhattanDistance(hero.Position) == 1 && !host.CurrentState.IsOccupied(p, hero.Id) && CombatMovementQuery.FindPath(host.CurrentState, hero, p).Count > 1);
            host.SubmitBattlefieldQuickMove(destination); yield return new WaitForSecondsRealtime(.2f);
            host.SubmitBattlefieldQuickMove(destination); yield return new WaitForSecondsRealtime(.8f);
            if (hero.Position != destination || hero.ActionPoints != ap - 1) throw new InvalidOperationException("Normal movement confirmation did not commit once.");
            if (!AcademyTutorialStore.Load().Find("C04").evidence.Contains("move-accepted")) throw new InvalidOperationException("Actual movement evidence was not saved.");
            Capture("11_first_move_committed");
        }
        private void Capture(string name)
        { OccPlayerRenderCapture.Capture(null, 1920, 1080, Path.Combine(folder, name + ".png")); OccPlayerRenderCapture.Capture(null, 960, 540, Path.Combine(folder, name + "_960.png")); }
        private static void ClickText(string label) => Click(FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.gameObject.activeInHierarchy && b.IsInteractable() && b.GetComponentInChildren<Text>()?.text == label));
        private static void Click(Button button) => typeof(OccPlayerStartupProbe).GetMethod("ClickThroughRaycast", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { button });
        private void OnDestroy() { scope?.Dispose(); }
    }
}
#endif
