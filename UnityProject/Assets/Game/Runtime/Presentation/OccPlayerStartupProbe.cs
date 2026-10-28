using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    // Inactive in normal games. Explicit standalone-only delivery smoke check;
    // Startup only observes; render mode uses explicitly isolated verification slots.
    public sealed partial class OccPlayerStartupProbe : MonoBehaviour
    {
        private string output;private bool renderCheck,uiCheck,borderCheck,loadoutCheck,workshopCheck,battleVfxCheck;private IDisposable saveScope;
        private readonly List<string> errors=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AttachIfRequested()
        {
            if(Application.isEditor)return;
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-occ-startup-check"||args[i]=="-occ-render-check"||args[i]=="-occ-ui-check"||args[i]=="-aether-border-check"||args[i]=="-aether-loadout-check"||args[i]=="-aether-workshop-check"||args[i]=="-aether-battle-vfx-check")
            {
                var go=new GameObject("OCC delivery startup check");DontDestroyOnLoad(go);
                var probe=go.AddComponent<OccPlayerStartupProbe>();probe.output=Path.GetFullPath(args[i+1]);
                probe.uiCheck=args[i]=="-occ-ui-check";probe.renderCheck=args[i]!="-occ-startup-check";
                probe.borderCheck=args[i]=="-aether-border-check";
                probe.loadoutCheck=args[i]=="-aether-loadout-check";
                probe.workshopCheck=args[i]=="-aether-workshop-check";
                probe.battleVfxCheck=args[i]=="-aether-battle-vfx-check";
                if(probe.renderCheck)
                {
                    probe.saveScope=FirstExperienceSaveRouting.BeginVerificationScope("player-render-"+DateTime.UtcNow.Ticks);
                    FirstExperienceSaveRouting.SelectSlot(0);
                }
                Application.runInBackground=true;return;
            }
        }
        private IEnumerator Start()
        {
            Application.logMessageReceived+=OnLog;
            int start=Time.frameCount;float began=Time.realtimeSinceStartup;
            while((Time.frameCount-start<60||Time.realtimeSinceStartup-began<5)&&Time.realtimeSinceStartup-began<30)yield return null;
            var host=FindAnyObjectByType<CombatPrototypeBootstrap>();
            var opening=FindAnyObjectByType<FirstExperiencePrototypeController>();
            var shader=Shader.Find("OCC/HD2D/DeskSurface");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if(renderCheck)
            {
                var work=battleVfxCheck?RenderBattleVfxSample(host):workshopCheck?RenderWorkshopSample(host):loadoutCheck?RenderLoadoutSample(host):borderCheck?RenderBorderSample(host):uiCheck?RenderUiStages(host):RenderStages(host);
                while(true)
                {
                    object current=null;bool next=false;
                    try{next=work.MoveNext();if(next)current=work.Current;}
                    catch(Exception error){errors.Add(error.ToString());}
                    if(!next)break;
                    yield return current;
                }
            }
            bool passed=host!=null&&opening!=null&&Time.frameCount-start>=30&&
                SceneManager.GetActiveScene().name=="CombatPrototype"&&GraphicsSettings.currentRenderPipeline!=null&&
                shader!=null&&shader.isSupported&&errors.Count==0;
            var report=new Report{status=passed?"PASS":"FAIL",scene=SceneManager.GetActiveScene().name,
                version=Application.unityVersion,dataPath=Application.dataPath,graphicsDevice=SystemInfo.graphicsDeviceName,
                pipeline=GraphicsSettings.currentRenderPipeline==null?"<null>":GraphicsSettings.currentRenderPipeline.name,
                framesAdvanced=Time.frameCount-start,elapsedSeconds=Time.realtimeSinceStartup-began,
                frontEndStage=opening==null?"<missing>":opening.DebugStage,
                gameplaySaveActionsInvoked=renderCheck,isolatedVerificationSlots=renderCheck,errors=errors.ToArray()};
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
            Application.logMessageReceived-=OnLog;RestoreWorkshopPreference();saveScope?.Dispose();Application.Quit(passed?0:1);
        }
        private IEnumerator RenderBorderSample(CombatPrototypeBootstrap host)
        {
            var settings=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
                .First(b=>b.GetComponentInChildren<UnityEngine.UI.Text>()?.text=="设置");
            var navigationPointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(settings.gameObject,navigationPointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.15f);
            if(settings.targetGraphic.color.a<.1f)throw new InvalidOperationException("Quiet navigation has no visible hover state.");
            foreach(var navigation in FindObjectsByType<AetherQuietNavigation>(FindObjectsSortMode.None))
            {
                var focus=navigation.transform.Find("像素焦点框");
                if(focus!=null&&focus.gameObject.activeSelf&&UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject!=navigation.gameObject)
                    throw new InvalidOperationException("Unfocused quiet navigation has a persistent focus frame.");
            }
            Capture("menu_navigation_hover");
            ClickThroughRaycast(settings);yield return new WaitForSecondsRealtime(.6f);Capture("settings_from_menu");
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            SaveAndResume(run,host);yield return new WaitForSecondsRealtime(1.2f);
            Capture("gift");
            var gift=FindAnyObjectByType<LampkeeperGiftView>();
            if(gift==null||gift.ChoiceButtons.Length!=3)throw new InvalidOperationException("Gift comparison sample missing.");
            var button=gift.ChoiceButtons[0];
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.25f);Capture("gift_hover");
            string expected=run.OpeningGift.CandidateIds[0];
            ClickThroughRaycast(button);yield return new WaitForSecondsRealtime(.8f);
            if(host.CurrentMapRun.OpeningGift.SelectedId!=expected)throw new InvalidOperationException("Gift click did not commit the expected choice.");
            SaveAndResume(host.CurrentMapRun,host);yield return new WaitForSecondsRealtime(.6f);
            if(host.CurrentMapRun.OpeningGift.SelectedId!=expected)throw new InvalidOperationException("Gift selection did not restore.");
            // The longer spell-choice branch uses the same sheet and controls.
            RogueliteMapRun spellRun=null;
            for(int seed=1;seed<=100;seed++)
            {var candidate=RogueliteMapRun.CreateSubsequentAcademyRun(seed);if(candidate.OpeningGift.CandidateIds.Contains("LK-05")){spellRun=candidate;break;}}
            if(spellRun==null)throw new InvalidOperationException("No spell gift fixture found.");
            SaveAndResume(spellRun,host);yield return new WaitForSecondsRealtime(.6f);
            gift=FindAnyObjectByType<LampkeeperGiftView>();
            ClickThroughRaycast(gift.ChoiceButtons[Array.IndexOf(spellRun.OpeningGift.CandidateIds,"LK-05")]);
            yield return new WaitForSecondsRealtime(.8f);Capture("gift_spell");
            gift=FindAnyObjectByType<LampkeeperGiftView>();
            string expectedSpell=host.CurrentMapRun.OpeningGift.SpellCandidateIds[0];
            ClickThroughRaycast(gift.ChoiceButtons[0]);yield return new WaitForSecondsRealtime(.6f);
            if(!host.CurrentMapRun.OpeningGift.Resolved||host.CurrentMapRun.OpeningGift.SelectedSpellId!=expectedSpell)
                throw new InvalidOperationException("Spell gift branch did not resolve.");
            void Capture(string name)
            {
                string folder=Path.GetDirectoryName(output);
                OccPlayerRenderCapture.Capture(null,1920,1080,Path.Combine(folder,name+".png"));
                OccPlayerRenderCapture.Capture(null,960,540,Path.Combine(folder,name+"_960.png"));
            }
        }
        private IEnumerator RenderStages(CombatPrototypeBootstrap host)
        {
            string folder=Path.GetDirectoryName(output);
            OccPlayerRenderCapture.Capture(null,1920,1080,Path.Combine(folder,"player_landing.png"));
            OccPlayerRenderCapture.Capture(null,960,540,Path.Combine(folder,"player_landing_960.png"));
            foreach(string name in new[]{"Universal Render Pipeline/Unlit","OCC/HD2D/BakedArchitecture","OCC/HD2D/GroundedSprite","OCC/HD2D/BattleAerialPerspective"})
                if(Shader.Find(name)==null||!Shader.Find(name).isSupported)throw new InvalidOperationException("Missing Player shader: "+name);
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            run.ChooseOpeningGift(run.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));
            run.ConfirmAcademyDeparture();
            SaveAndResume(run,host);
            yield return new WaitForSecondsRealtime(3);
            OccPlayerRenderCapture.Capture(null,1920,1080,Path.Combine(folder,"player_map.png"));
            OccPlayerRenderCapture.Capture(null,960,540,Path.Combine(folder,"player_map_960.png"));
            run.SelectNode("dorm_drill");run.BeginCombatJournal();SaveAndResume(run,host);
            yield return new WaitForSecondsRealtime(5);
            var battle=FindAnyObjectByType<Hd2DBattlefieldSample>();
            if(battle==null||battle.RenderCamera==null)throw new InvalidOperationException("Player battlefield did not initialize.");
            OccPlayerRenderCapture.Capture(battle,1920,1080,Path.Combine(folder,"player_battle.png"));
            OccPlayerRenderCapture.Capture(battle,960,540,Path.Combine(folder,"player_battle_960.png"));
        }
        private static void SaveAndResume(RogueliteMapRun run,CombatPrototypeBootstrap host)
        {
            var gateway=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());
            if(!gateway.SaveMapRun(run))throw new InvalidOperationException(gateway.LastError);
            host.GetComponent<FirstExperiencePrototypeController>().enabled=false;
            if(!host.EnterFirstExperienceFromOpening(true))throw new InvalidOperationException("Player continue entry failed.");
        }
        private IEnumerator RenderUiStages(CombatPrototypeBootstrap host)
        {
            string folder=Path.GetDirectoryName(output);
            CapturePage("landing");
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            SaveAndResume(run,host);yield return new WaitForSecondsRealtime(1.2f);CapturePage("gift");
            run.ChooseOpeningGift(run.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));run.ConfirmAcademyDeparture();
            SaveAndResume(run,host);yield return new WaitForSecondsRealtime(1.2f);CapturePage("map");
            var graph=FindObjectsByType<OccMapNodeDetails>(FindObjectsSortMode.None);
            if(graph.Length==0)throw new InvalidOperationException("Normal map has no node views.");
            AssertMapHasVisibleContent(Path.Combine(folder,"map.png"));
            foreach(string page in new[]{"loadout","spells","settings","room"})
            {
                var ui=FindAnyObjectByType<FormalRogueliteUi>();
                string error=ui.PreviewUiCapturePage(page,page=="room"?"dorm_drill":null);
                if(error!=null)throw new InvalidOperationException(error);
                yield return new WaitForSecondsRealtime(1.3f);CapturePage(page);
                if(page=="spells")
                {
                    var second=FindSpellSlot(5);
                    ClickThroughRaycast(second);yield return new WaitForSecondsRealtime(.6f);CapturePage("spells_5_8");
                    var eighth=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_8  ",StringComparison.Ordinal));
                    ClickThroughRaycast(eighth);yield return new WaitForSecondsRealtime(.6f);CapturePage("spells_slot8");
                    if(!FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("目标槽位 8")))
                        throw new InvalidOperationException("Second spell group cannot target slot eight.");
                }
            }
            foreach(var type in new[]{RogueliteMapNodeType.Event,RogueliteMapNodeType.Workshop,RogueliteMapNodeType.Medical,RogueliteMapNodeType.Shop})
            {
                var snapshot=new RogueliteSaveGateway(new PlayerPrefsRogueliteSaveStore());snapshot.SaveNewMapRun(run);
                var dto=Rogue11Serializer.Deserialize(new PlayerPrefsRogueliteSaveStore().GetString(RogueliteSaveGateway.MapRunKey));
                string node=run.MapNodes.First(n=>n.Type==type).Id;dto.CurrentNodeId=node;
                if(!dto.VisitedNodeIds.Contains(node))dto.VisitedNodeIds.Add(node);
                var service=RogueliteMapRun.FromRogue11(dto);SaveAndResume(service,host);
                yield return new WaitForSecondsRealtime(.4f);
                FindAnyObjectByType<FormalRogueliteUi>().PreviewUiCapturePage("room",node);
                yield return new WaitForSecondsRealtime(1.3f);CapturePage(type.ToString().ToLowerInvariant());
                if(type==RogueliteMapNodeType.Shop)
                {
                    var offer=service.CurrentShopService.Offers.First(o=>!o.Sold&&service.Gold>=o.Price&&service.CanAcceptFirstRunOffer(o.OfferId));
                    var pick=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="货架商品_"+offer.OfferId);
                    ClickThroughRaycast(pick);yield return new WaitForSecondsRealtime(.6f);CapturePage("shop_selected");
                    int beforeGold=host.CurrentMapRun.Gold;
                    var buy=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name.StartsWith("按钮_购买 ·",StringComparison.Ordinal)&&b.interactable);
                    ClickThroughRaycast(buy);yield return new WaitForSecondsRealtime(.8f);CapturePage("shop_sold");
                    if(host.CurrentMapRun.Gold!=beforeGold-offer.Price||!host.CurrentMapRun.CurrentShopService.Offers.First(o=>o.OfferId==offer.OfferId).Sold)
                        throw new InvalidOperationException("Shelf purchase did not commit its price and sold state.");
                    var saved=Rogue11Serializer.Deserialize(new PlayerPrefsRogueliteSaveStore().GetString(RogueliteSaveGateway.MapRunKey));
                    var restored=RogueliteMapRun.FromRogue11(saved);
                    if(restored.Gold!=beforeGold-offer.Price||!restored.CurrentShopService.Offers.First(o=>o.OfferId==offer.OfferId).Sold)
                        throw new InvalidOperationException("Shelf purchase did not survive save restore.");
                    SaveAndResume(restored,host);yield return new WaitForSecondsRealtime(.5f);
                    FindAnyObjectByType<FormalRogueliteUi>().PreviewUiCapturePage("room",node);
                    yield return new WaitForSecondsRealtime(.6f);CapturePage("shop_restored");
                }
            }
            run.SelectNode("dorm_drill");run.CompleteCurrentCombat();run.OpenPendingRewardChoices();
            SaveAndResume(run,host);yield return new WaitForSecondsRealtime(1.3f);CapturePage("reward");
            var rewardChoice=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="揭晓卡牌_0");
            ClickThroughRaycast(rewardChoice);yield return new WaitForSecondsRealtime(.7f);CapturePage("reward_selected");
            var battle=RogueliteMapRun.CreateSubsequentAcademyRun(4311);
            battle.ChooseOpeningGift(battle.OpeningGift.CandidateIds.First(id=>id!="LK-05"&&id!="LK-06"));battle.ConfirmAcademyDeparture();
            battle.SelectNode("dorm_drill");battle.BeginCombatJournal();SaveAndResume(battle,host);
            yield return new WaitForSecondsRealtime(3);CapturePage("battle");
            yield return new WaitForSecondsRealtime(.15f);
            var feedback=FindAnyObjectByType<CombatVisualFeedback>();
            var hero=host.CurrentState.Units.Values.First(u=>u.Id==host.CurrentState.ActiveUnitId);
            float heroStarted=Time.realtimeSinceStartup;
            feedback.BeginHeroAction(hero,host.CurrentState.TurnSequence);
            // Check duration before synchronous PNG readbacks can stall the player.
            yield return new WaitForSecondsRealtime(.25f);
            while(Time.realtimeSinceStartup<heroStarted+1.4f)yield return null;
            var banner=GameObject.Find("回合行动提示");
            if(banner==null||banner.GetComponent<CanvasGroup>().alpha<.95f)throw new InvalidOperationException("Hero turn notice did not stay readable for 1.4 seconds.");
            var ribbon=banner.GetComponent<RectTransform>();
            if(ribbon.anchorMin!=new Vector2(.5f,.5f)||ribbon.rect.width<ribbon.rect.height*4)
                throw new InvalidOperationException("Turn notice is not a central horizontal ribbon.");
            if(banner.GetComponentsInChildren<UnityEngine.UI.Graphic>().Any(g=>g.raycastTarget))throw new InvalidOperationException("Turn notice blocks pointer input.");
            CapturePage("turn_hero");
            var enemy=host.CurrentState.Units.Values.First(u=>u.Id!=hero.Id);
            feedback.BeginEnemyAction(enemy,null,2f);
            yield return new WaitForSecondsRealtime(.25f);CapturePage("turn_enemy");
            yield return new WaitForSecondsRealtime(.15f);
            var preferences=host.UiPreferences;var previous=RogueliteUiPreferences.FromDataString(preferences.ToDataString());
            preferences.Configure(previous.MasterVolume,0,previous.ScreenShake,previous.FloatingText,previous.HighContrast,previous.LargeText,previous.KeyHints);
            heroStarted=Time.realtimeSinceStartup;
            feedback.BeginHeroAction(hero,host.CurrentState.TurnSequence);
            yield return new WaitForSecondsRealtime(.25f);
            while(Time.realtimeSinceStartup<heroStarted+1.4f)yield return null;
            banner=GameObject.Find("回合行动提示");
            if(banner==null||banner.GetComponent<CanvasGroup>().alpha<.95f)throw new InvalidOperationException("Static turn notice disappeared early.");
            CapturePage("turn_static");
            preferences.Configure(previous.MasterVolume,previous.AnimationIntensity,previous.ScreenShake,previous.FloatingText,previous.HighContrast,previous.LargeText,previous.KeyHints);
            yield return new WaitForSecondsRealtime(.2f);
            feedback.CancelEnemyAction();
            var turnLog=new List<object>();
            var end=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name=="结束行动");
            ClickThroughRaycast(end);
            float until=Time.realtimeSinceStartup+20f;string lastActor=null;int frame=0;bool enemySeen=false,heroReturned=false;
            while(Time.realtimeSinceStartup<until)
            {
                string active=host.CurrentState.ActiveUnitId;
                turnLog.Add(new{time=Time.realtimeSinceStartup,active,turn=host.CurrentState.TurnSequence,
                    bannerCount=FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Count(t=>t.name=="行动提示标题"&&t.gameObject.activeInHierarchy),
                    heroHealth=host.CurrentState.GetUnit("hero").Health});
                if(active!="hero")enemySeen=true;
                if(active=="hero"&&enemySeen)heroReturned=true;
                if(active!=lastActor&&frame<5){CapturePage("turn_live_"+frame++);lastActor=active;}
                File.WriteAllText(Path.Combine(folder,"live-turns.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{enemySeen,heroReturned,turnLog},Newtonsoft.Json.Formatting.Indented));
                if(FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Count(t=>t.name=="行动提示标题"&&t.gameObject.activeInHierarchy)>1)
                    throw new InvalidOperationException("Consecutive turns left duplicate banners.");
                if(heroReturned)break;
                yield return new WaitForSecondsRealtime(.1f);
            }
            File.WriteAllText(Path.Combine(folder,"live-turns.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{enemySeen,heroReturned,turnLog},Newtonsoft.Json.Formatting.Indented));
            if(!enemySeen||!heroReturned)throw new InvalidOperationException("Actual end-turn sequence did not return from enemies to hero.");
            void CapturePage(string name)
            {
                var sample=FindAnyObjectByType<Hd2DBattlefieldSample>();
                OccPlayerRenderCapture.Capture(sample,1920,1080,Path.Combine(folder,name+".png"));
                if(name=="gift"||name=="spells")AssertReadingInk(Path.Combine(folder,name+".png"),name=="spells");
                OccPlayerRenderCapture.Capture(sample,960,540,Path.Combine(folder,name+"_960.png"));
                var labels=FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Where(t=>t.enabled&&t.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(t.text)).ToArray();
                if(name=="medical")foreach(var label in labels)
                    if(label.name.StartsWith("餐食效果",StringComparison.Ordinal)&&label.preferredHeight>label.rectTransform.rect.height+label.fontSize*.34f)
                        throw new InvalidOperationException("Meal effect text is clipped: "+label.text);
                File.WriteAllText(Path.Combine(folder,name+"_text.json"),Newtonsoft.Json.JsonConvert.SerializeObject(labels.Select(t=>new{
                    t.name,t.text,t.fontSize,bestFit=t.resizeTextForBestFit,box=t.rectTransform.rect.size.ToString(),
                    preferredHeight=t.preferredHeight,preferredWidth=t.preferredWidth}),Newtonsoft.Json.Formatting.Indented));
            }
        }
        private static void ClickThroughRaycast(UnityEngine.UI.Button button)
        {
            var system=UnityEngine.EventSystems.EventSystem.current;
            if(system==null||!button.IsInteractable())throw new InvalidOperationException("Unavailable pointer target: "+button.name);
            var canvas=button.GetComponentInParent<Canvas>();
            Camera view=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:Hd2DSceneGraphicRaycaster.ResolveEventCamera(canvas.worldCamera);
            var rect=(RectTransform)button.transform;
            var point=RectTransformUtility.WorldToScreenPoint(view,rect.TransformPoint(rect.rect.center));
            var pointer=new UnityEngine.EventSystems.PointerEventData(system){position=point,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left};
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();system.RaycastAll(pointer,hits);
            var hit=hits.FirstOrDefault(h=>UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(h.gameObject)!=null);
            var target=hit.gameObject==null?null:UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hit.gameObject);
            if(target!=button.gameObject)throw new InvalidOperationException("Pointer blocked at "+button.name+" by "+(target==null?"<nothing>":target.name)+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name)));
            pointer.pointerCurrentRaycast=hit;
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
            UnityEngine.EventSystems.ExecuteEvents.Execute(target,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        }
        private static void AssertReadingInk(string path,bool spells)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));var pixels=texture.GetPixels32();int ink=0;
                for(int y=spells?145:100;y<(spells?195:190);y++)for(int x=spells?140:180;x<(spells?430:880);x++)
                {var c=pixels[(texture.height-1-y)*texture.width+x];if(c.r+c.g+c.b<210)ink++;}
                if(ink<200)throw new InvalidOperationException("Reading canvas is covered: page title has no visible ink: "+path+"");
            }
            finally{Destroy(texture);}
        }
        private static void AssertMapHasVisibleContent(string path)
        {
            var texture=new Texture2D(2,2,TextureFormat.RGB24,false);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));var pixels=texture.GetPixels32();int dark=0;
                for(int y=220;y<Mathf.Min(900,texture.height);y++)for(int x=160;x<Mathf.Min(1760,texture.width);x++)
                {var c=pixels[y*texture.width+x];if(c.r+c.g+c.b<240)dark++;}
                if(dark<1000)throw new InvalidOperationException("Map capture is blank: graph content not visible.");
            }
            finally{Destroy(texture);}
        }
        private void OnLog(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        [Serializable] private sealed class Report
        {
            public string status,scene,version,dataPath,graphicsDevice,pipeline,frontEndStage;
            public int framesAdvanced;public float elapsedSeconds;public bool focusRequested=false,gameplaySaveActionsInvoked=false,isolatedVerificationSlots=false;public string[] errors;
        }
    }
}
