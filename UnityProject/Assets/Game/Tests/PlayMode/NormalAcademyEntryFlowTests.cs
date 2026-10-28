using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;
using System.IO;

namespace OCC.Combat.Tests
{
    public sealed class NormalAcademyEntryFlowTests
    {
        private Scene scene;
        private IDisposable scope;
        private readonly Dictionary<string, string> originalSlots = new Dictionary<string, string>();

        [UnityTest]
        public IEnumerator RealMenuButtons_CreateDefaultNormalRoundAndRestoreItsDeparture()
        {
            Assert.That(Application.dataPath.Replace('\\','/'), Is.EqualTo("E:/数据库/OCC_Codex/UnityProject/Assets"));
            for (int i=0;i<SceneManager.sceneCount;i++)
                TestContext.WriteLine("Scene preflight: "+SceneManager.GetSceneAt(i).path+" dirty="+SceneManager.GetSceneAt(i).isDirty);
            for(int i=0;i<3;i++)
                foreach(var key in new[]{"OCC.FirstExperiencePrototype.v2.slot."+i,"occ.first_experience.map_run.slot."+i})
                    originalSlots[key]=PlayerPrefs.GetString(key,"<missing>");
            scope=FirstExperienceSaveRouting.BeginVerificationScope("normal-opening-"+Guid.NewGuid().ToString("N"));
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var host=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatPrototypeBootstrap>()).Single();
            var flow=host.GetComponent<FirstExperiencePrototypeController>();
            Assert.That(flow,Is.Not.Null);
            // Starts at the real homepage; only branding/settings are outside this local check.
            flow.ShowLanding();
            yield return Click("开始游戏");
            Assert.That(flow.CurrentStage,Is.EqualTo(FirstExperiencePrototypeController.FlowStage.Configuration));
            Assert.That(flow.FixedFirstExperienceEnabled,Is.False);
            yield return Click("固定首次体验开关");
            Assert.That(flow.FixedFirstExperienceEnabled,Is.True);
            yield return Click("固定首次体验开关");
            Assert.That(flow.FixedFirstExperienceEnabled,Is.False);
            yield return Click("以此角色开始");
            yield return Click("进入首次地图");
            if(flow.CurrentStage==FirstExperiencePrototypeController.FlowStage.TutorialChoice)
                yield return Click("不需要教程");
            var run=host.CurrentMapRun;
            Assert.That(run,Is.Not.Null,flow.HandoffError);
            Assert.That(run.IsInAcademyLayer,Is.True);
            Assert.That(run.FirstRunExperience,Is.Null);
            Assert.That(run.DeparturePending,Is.True);
            Assert.That(run.RogueRunState.MasteredSpellIds.Count,Is.EqualTo(4));
            Assert.That(run.OpeningGiftPending,Is.True);
            yield return CaptureGift("initial");
            yield return Click("守灯人查看地图");
            Assert.That(run.CompletedNodes,Is.Empty);
            yield return Click("返回守灯人");
            yield return Click("守灯人查看配置");
            yield return Click("返回守灯人");
            var originalCandidates=run.OpeningGift.CandidateIds.ToArray();
            yield return Click("守灯人返回首页");
            yield return Click("继续游戏");
            Assert.That(host.CurrentMapRun.OpeningGift.CandidateIds,Is.EqualTo(originalCandidates));
            yield return Click("守灯人选择_0",raycast:true);
            if(host.CurrentMapRun.OpeningGiftPending)
            {
                yield return CaptureGift("inner");
                yield return Click("守灯人返回首页");yield return Click("继续游戏");
                yield return Click("守灯人选择_0",raycast:true);
            }
            run=host.CurrentMapRun;
            Assert.That(run.OpeningGift.Resolved,Is.True);
            Assert.That(run.RogueRunState.MasteredSpellIds.Count,Is.InRange(4,5));
            var saves=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
            Assert.That(saves.TryLoadMapRun(out var restored),Is.True,saves.LastError);
            Assert.That(restored.Seed,Is.EqualTo(run.Seed));
            Assert.That(restored.FirstRunExperience,Is.Null);
            Assert.That(restored.DeparturePending,Is.True);
            Assert.That(restored.OpeningGift.SelectedId,Is.EqualTo(run.OpeningGift.SelectedId));
            TestContext.WriteLine("Real homepage -> normal new slot -> seeded gift -> map/configuration previews -> home/continue -> selection -> departure; isolated disk save restored; gift choice pointer raycasts verified at actual Screen resolution.");
        }

        private static IEnumerator Click(string fragment,bool raycast=false)
        {
            Button button=null;
            for(int frame=0;frame<90;frame++)
            {
                button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b=>b.isActiveAndEnabled&&b.interactable&&b.name.Contains(fragment));
                if(button!=null)break;
                yield return null;
            }
            Assert.That(button,Is.Not.Null,"Missing button "+fragment);
            if(raycast)
            {
                Canvas.ForceUpdateCanvases();
                var rect=(RectTransform)button.transform;var canvas=rect.GetComponentInParent<Canvas>();
                var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                var raycaster=canvas.GetComponent<GraphicRaycaster>();
                if(raycaster!=null)camera=raycaster.eventCamera;
                var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
                if(camera!=null&&camera.targetTexture!=null)point=new Vector2(point.x*Screen.width/camera.pixelWidth,point.y*Screen.height/camera.pixelHeight);
                var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
                Assert.That(hits.Count,Is.GreaterThan(0),"No pointer hit: "+fragment);
                Assert.That(hits[0].gameObject.transform.IsChildOf(button.transform),Is.True,"Blocked by "+hits[0].gameObject.name);
                data.pointerCurrentRaycast=hits[0];data.pointerPressRaycast=hits[0];
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);yield return null;
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
            }
            else button.onClick.Invoke();
            if(fragment=="开始游戏"||fragment=="继续游戏")yield return new WaitForSecondsRealtime(MainMenuDeskScene.OpenDuration+.2f);
            for(int i=0;i<4;i++)yield return null;
        }

        [UnityTest]
        public IEnumerator BlankAnnotation_RestoresSavedNestedSelectionAndDoesNotAutoEquip()
        {
            for(int i=0;i<3;i++)
                foreach(var key in new[]{"OCC.FirstExperiencePrototype.v2.slot."+i,"occ.first_experience.map_run.slot."+i})
                    originalSlots[key]=PlayerPrefs.GetString(key,"<missing>");
            scope=FirstExperienceSaveRouting.BeginVerificationScope("lampkeeper-inner-"+Guid.NewGuid().ToString("N"));
            RogueliteMapRun run=null;
            for(int seed=1;seed<100;seed++)
            {
                var candidate=RogueliteMapRun.CreateSubsequentAcademyRun(seed);
                if(candidate.OpeningGift.CandidateIds.Contains("LK-05")){run=candidate;break;}
            }
            Assert.That(run,Is.Not.Null);run.ChooseOpeningGift("LK-05");
            var choices=run.OpeningGift.SpellCandidateIds.ToArray();
            FirstExperienceSaveRouting.SelectSlot(0);
            var saves=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
            Assert.That(saves.SaveNewMapRun(run),Is.True,saves.LastError);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var host=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatPrototypeBootstrap>()).Single();
            host.GetComponent<FirstExperiencePrototypeController>().ShowLanding();
            yield return Click("继续游戏");
            Assert.That(host.CurrentMapRun.OpeningGift.SelectedId,Is.EqualTo("LK-05"));
            Assert.That(host.CurrentMapRun.OpeningGiftPending,Is.True);
            Assert.That(host.CurrentMapRun.OpeningGift.SpellCandidateIds,Is.EqualTo(choices));
            yield return CaptureGift("blank-inner");
            yield return Click("守灯人选择_0",raycast:true);
            Assert.That(saves.TryLoadMapRun(out var restored),Is.True,saves.LastError);
            Assert.That(restored.OpeningGift.SelectedSpellId,Is.EqualTo(choices[0]));
            Assert.That(restored.RogueRunState.EquippedSpellIds,Does.Not.Contain(choices[0]));
            Assert.That(restored.RogueRunState.MasteredSpellIds,Does.Contain(choices[0]));
            TestContext.WriteLine("Isolated production-store resume fixture: LK-05 inner candidates restored, actual view raycast selected the same spell, learned once without automatic equipment.");
        }

        private static IEnumerator CaptureGift(string suffix)
        {
            yield return new WaitForSecondsRealtime(.4f);
            for(int i=0;i<8;i++)yield return null;
            var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-21_守灯人赠礼实现/ui"));Directory.CreateDirectory(folder);
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.Hd2DBattleCanvasCapture")).First(t=>t!=null);
            var method=type.GetMethod("Capture",BindingFlags.NonPublic|BindingFlags.Static);
            var sample=UnityEngine.Object.FindAnyObjectByType<Hd2DBattlefieldSample>();
            foreach(var art in UnityEngine.Object.FindAnyObjectByType<LampkeeperGiftView>().GetComponentsInChildren<Image>(true).Where(x=>x.name=="守灯人"))
                TestContext.WriteLine("Keeper image: active="+art.gameObject.activeInHierarchy+" enabled="+art.enabled+" sprite="+art.sprite+" rect="+art.rectTransform.rect+" color="+art.color+" pos="+art.transform.position+" scale="+art.transform.lossyScale);
            foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(960,540)})
                method.Invoke(null,new object[]{sample,size.x,size.y,Path.Combine(folder,suffix+"-"+size.x+".png"),false});
            TestContext.WriteLine("Gift captures are offscreen 1920/960; actual pointer Screen="+Screen.width+"x"+Screen.height);
        }

        [UnityTest]
        public IEnumerator GiftArtwork_AllSixAppearOnSavedRoundResume()
        {
            scope=FirstExperienceSaveRouting.BeginVerificationScope("lampkeeper-art-"+Guid.NewGuid().ToString("N"));
            FirstExperienceSaveRouting.SelectSlot(0);
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/CombatPrototype.unity",LoadSceneMode.Additive);
            scene=SceneManager.GetSceneByPath("Assets/Scenes/CombatPrototype.unity");
            var host=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatPrototypeBootstrap>()).Single();
            var flow=host.GetComponent<FirstExperiencePrototypeController>();
            var seen=new HashSet<string>();
            for(int seed=1;seed<100&&seen.Count<6;seed++)
            {
                var run=RogueliteMapRun.CreateSubsequentAcademyRun(seed);
                if(run.OpeningGift.CandidateIds.All(seen.Contains))continue;
                var saves=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
                Assert.That(saves.SaveNewMapRun(run),Is.True,saves.LastError);
                flow.ShowLanding();yield return Click("继续游戏");
                Assert.That(host.CurrentMapRun.Seed,Is.EqualTo(seed));
                yield return CaptureGift("art-seed-"+seed);
                foreach(string id in run.OpeningGift.CandidateIds)seen.Add(id);
                TestContext.WriteLine("Artwork resume seed="+seed+" gifts="+string.Join(",",run.OpeningGift.CandidateIds));
            }
            Assert.That(seen.Count,Is.EqualTo(6));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            scope?.Dispose();scope=null;
            foreach(var pair in originalSlots)
                Assert.That(PlayerPrefs.GetString(pair.Key,"<missing>"),Is.EqualTo(pair.Value),"Player slot changed: "+pair.Key);
        }
    }
}
