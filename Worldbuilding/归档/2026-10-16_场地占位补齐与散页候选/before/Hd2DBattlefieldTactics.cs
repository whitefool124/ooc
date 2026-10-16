using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Shared tactical semantics on a horizontal world canvas; screen text remains unprojected.
    [DefaultExecutionOrder(50)]
    public sealed class Hd2DBattlefieldTactics:MonoBehaviour
    {
        private Hd2DBattlefieldSample sample;
        private CombatPrototypeBootstrap host;
        private Transform world;
        private RectTransform ground;
        private CombatEnemyMoveRouteGraphic routes;
        private readonly Dictionary<GridPosition,CombatBattlefieldRangeGraphic> ranges=new Dictionary<GridPosition,CombatBattlefieldRangeGraphic>();
        private readonly Dictionary<GridPosition,CombatWaterRippleGraphic> waterRipples=new Dictionary<GridPosition,CombatWaterRippleGraphic>();
        private readonly Dictionary<GridPosition,CombatStopDustGraphic> stopDust=new Dictionary<GridPosition,CombatStopDustGraphic>();
        private readonly Dictionary<string,RectTransform> vitals=new Dictionary<string,RectTransform>();
        private readonly Dictionary<string,Rect> bodies=new Dictionary<string,Rect>();
        private readonly List<Rect> information=new List<Rect>(),occupied=new List<Rect>(),placed=new List<Rect>();
        private readonly CombatIntentChangeTracker changes=new CombatIntentChangeTracker();
        private readonly Dictionary<GridPosition,float> changedCells=new Dictionary<GridPosition,float>();
        private CombatState observed;
        private FormalBattlefieldView formal;
        private RectTransform actorFrame;
        private string markedActor;
        private Vector2 markerStart;
        private float markerElapsed;
        private RawImage timelineArrow;
        private readonly Dictionary<string,FormalResourceBarView> healthBars=new Dictionary<string,FormalResourceBarView>(),manaBars=new Dictionary<string,FormalResourceBarView>();
        private readonly Dictionary<string,Text> shieldBadges=new Dictionary<string,Text>();
        private FormalResourceBarView objectDurability;
        public bool IsObjectDurabilityVisible=>objectDurability!=null&&objectDurability.gameObject.activeSelf;
        private int layer;
        private Material tacticalEmission;
        public void Initialize(Hd2DBattlefieldSample view,CombatPrototypeBootstrap source,Transform root,int isolatedLayer)
        {
            sample=view;host=source;world=root;layer=isolatedLayer;
            tacticalEmission=new Material(Shader.Find("UI/Default")){name="Owned HDR tactical line pixels"};
            tacticalEmission.SetColor("_Color",new Color(2.1f,2.1f,2.1f,1));
            formal=source.GetComponent<FormalBattlefieldView>();
            var go=new GameObject("HD2D真实地面战术层",typeof(RectTransform),typeof(Canvas)){layer=layer};
            ground=(RectTransform)go.transform;ground.SetParent(world,false);ground.pivot=Vector2.zero;
            ground.sizeDelta=new Vector2(source.CurrentState.Map.Width*32,source.CurrentState.Map.Height*32);
            ground.localPosition=new Vector3(0,.035f,0);ground.localRotation=Quaternion.Euler(90,0,0);ground.localScale=Vector3.one/32f;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=sample.RenderCamera;
            var route=new GameObject("公开敌方移动路径",typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatEnemyMoveRouteGraphic)){layer=layer};
            route.transform.SetParent(ground,false);routes=route.GetComponent<CombatEnemyMoveRouteGraphic>();
            routes.material=tacticalEmission;Hd2DTacticalGlow.Attach(routes,.55f);
            routes.rectTransform.pivot=Vector2.zero;routes.rectTransform.anchorMin=routes.rectTransform.anchorMax=Vector2.zero;routes.rectTransform.sizeDelta=ground.sizeDelta;routes.raycastTarget=false;
        }
        private void LateUpdate()
        {
            if(sample==null||host?.CurrentState==null||!host.IsBattlefieldVisible)return;
            var state=host.CurrentState;
            if(observed!=state){observed=state;changes.Clear();changedCells.Clear();}
            var models=sample.FrameModels;
            var plans=models.Values.Where(m=>m?.Unit!=null&&!m.Unit.IsHero&&m.Unit.IsAlive&&m.Intent!=null).ToArray();
            foreach(var m in plans)if(changes.Observe(m.Unit.Id,m.Intent,Time.unscaledTime))
            {
                foreach(var p in m.Intent.AffectedCells.Concat(m.Intent.Route))changedCells[p]=Time.unscaledTime;
                if(m.Intent.HasDestination)changedCells[m.Intent.Destination]=Time.unscaledTime;
            }
            var destinations=new HashSet<GridPosition>(plans.Where(m=>m.Intent.HasDestination).Select(m=>m.Intent.Destination));
            var enemyEffect=new HashSet<GridPosition>(plans.SelectMany(m=>m.Intent.AffectedCells));
            var playerEffect=new HashSet<GridPosition>(models.Where(k=>k.Value?.IsPlayerEffectCell==true).Select(k=>k.Key));
            var inspected=plans.FirstOrDefault(m=>sample.HoveredCell.HasValue&&m.Position==sample.HoveredCell.Value)??plans.FirstOrDefault(m=>m.Unit.Id==host.SelectedTargetId);
            bool moving=host.SelectedAction=="移动"||models.Values.Any(m=>m!=null&&m.MoveMarker!=BattlefieldCellMarker.None);
            var threats=new HashSet<GridPosition>(moving?plans.SelectMany(m=>m.Intent.AttackRange):inspected?.Intent.AttackRange??System.Array.Empty<GridPosition>());
            foreach(var pair in models)
            {
                var p=pair.Key;var m=pair.Value;if(m==null)continue;
                if(!ranges.TryGetValue(p,out var graphic))
                {
                    var go=new GameObject("投影预览格_"+p,typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatBattlefieldRangeGraphic)){layer=layer};go.transform.SetParent(ground,false);
                    graphic=go.GetComponent<CombatBattlefieldRangeGraphic>();var rt=graphic.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=Vector2.zero;rt.sizeDelta=new Vector2(32,32);rt.anchoredPosition=new Vector2(p.X*32,p.Y*32);ranges[p]=graphic;
                    graphic.material=tacticalEmission;Hd2DTacticalGlow.Attach(graphic,.55f);
                    var water=new GameObject("真实足点水纹",typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatWaterRippleGraphic)){layer=layer};water.transform.SetParent(go.transform,false);FormalUiKit.Stretch((RectTransform)water.transform);waterRipples[p]=water.GetComponent<CombatWaterRippleGraphic>();
                    var dust=new GameObject("真实停步尘点",typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatStopDustGraphic)){layer=layer};dust.transform.SetParent(go.transform,false);FormalUiKit.Stretch((RectTransform)dust.transform);stopDust[p]=dust.GetComponent<CombatStopDustGraphic>();
                }
                var tile=state.Map.GetTile(p);waterRipples[p].Refresh(host.WaterRipples,p,tile.IsWater,Time.unscaledTime);stopDust[p].Refresh(host.StopDust,p,!tile.IsWater,Time.unscaledTime);
                graphic.Refresh(m.MoveMarker,m.AttackMarker,m.SkillMarker,threats.Contains(p),enemyEffect.Contains(p),Edges(enemyEffect,p),m.IsPlayerEffectCell,Edges(playerEffect,p),false,destinations.Contains(p),m.SelectionOverlayTexture!=null,host.UiPreferences.AnimationIntensity,changedCells.TryGetValue(p,out var at)?Time.unscaledTime-at:CombatIntentChangeTracker.Duration);
            }
            routes.transform.SetAsLastSibling();routes.Refresh(plans.Where(m=>m.Intent.Route.Count>1&&m.Intent.Route.All(models.ContainsKey)).Select(m=>m.Intent.Route.Select(p=>new Vector2(p.X*32+16,p.Y*32+16)).ToArray()),32,host.UiPreferences.AnimationIntensity);
            RefreshAnnotations(models);
            RefreshObjectDurability(state,models);
        }
        private static byte Edges(HashSet<GridPosition> cells,GridPosition p)
        {
            if(!cells.Contains(p))return 0;byte e=0;
            if(!cells.Contains(new GridPosition(p.X,p.Y+1)))e|=CombatBattlefieldRangeGraphic.Top;
            if(!cells.Contains(new GridPosition(p.X+1,p.Y)))e|=CombatBattlefieldRangeGraphic.Right;
            if(!cells.Contains(new GridPosition(p.X,p.Y-1)))e|=CombatBattlefieldRangeGraphic.Bottom;
            if(!cells.Contains(new GridPosition(p.X-1,p.Y)))e|=CombatBattlefieldRangeGraphic.Left;return e;
        }
        private Text Label(string name,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(transform,false);
            var rt=(RectTransform)go.transform;rt.sizeDelta=size;go.GetComponent<Image>().color=Color.clear;go.GetComponent<Image>().raycastTarget=false;
            var child=new GameObject("正文",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));child.transform.SetParent(rt,false);var tr=(RectTransform)child.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(5,2);tr.offsetMax=new Vector2(-5,-2);
            var text=child.GetComponent<Text>();text.font=FormalUiKit.Font;text.fontSize=16;text.color=new Color(.96f,.94f,.84f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;return text;
        }
        private Rect BodyRect(MeshRenderer renderer)
        {
            var b=renderer.GetComponent<MeshFilter>().sharedMesh.bounds;var min=new Vector2(float.MaxValue,float.MaxValue);var max=new Vector2(float.MinValue,float.MinValue);
            foreach(var v in new[]{new Vector3(b.min.x,0,0),new Vector3(b.max.x,0,0),new Vector3(b.min.x,b.max.y,0),new Vector3(b.max.x,b.max.y,0)}){var p=sample.ProjectLocal(renderer.transform.TransformPoint(v));min=Vector2.Min(min,p);max=Vector2.Max(max,p);}return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private void RefreshAnnotations(Dictionary<GridPosition,BattlefieldCellPresentation> models)
        {
            bodies.Clear();occupied.Clear();placed.Clear();information.Clear();
            var active=models.Values.Where(m=>m?.Unit!=null&&m.Unit.IsAlive&&sample.UnitRenderer(m.Position)!=null).ToArray();
            foreach(var pair in vitals)if(!active.Any(m=>m.Unit.Id==pair.Key))pair.Value.gameObject.SetActive(false);
            foreach(var m in active){var b=BodyRect(sample.UnitRenderer(m.Position));bodies[m.Unit.Id]=b;occupied.Add(b);}
            var viewport=((RectTransform)transform).rect;
            foreach(var m in active)
            {
                var id=m.Unit.Id;var body=bodies[id];
                if(!vitals.TryGetValue(id,out var label))
                {
                    var group=new GameObject("HD2D脚下资源_"+id,typeof(RectTransform));group.transform.SetParent(transform,false);
                    label=(RectTransform)group.transform;label.sizeDelta=new Vector2(164,28);vitals[id]=label;
                    var parent=label.transform;
                    healthBars[id]=FormalResourceBarView.InstantiateUnit(parent);healthBars[id].ConfigureCompact("生命","unit_bar_health_16",Color.white);
                    manaBars[id]=FormalResourceBarView.InstantiateUnit(parent);manaBars[id].ConfigureCompact("魔力",null,new Color(.25f,.6f,.85f));
                    var shield=Label("护盾徽记",new Vector2(32,16));shield.transform.parent.SetParent(parent,false);shieldBadges[id]=shield;((RectTransform)shield.transform.parent).anchoredPosition=new Vector2(-82,6);shield.fontSize=12;shield.alignment=TextAnchor.MiddleRight;shield.horizontalOverflow=HorizontalWrapMode.Overflow;shield.verticalOverflow=VerticalWrapMode.Overflow;
                    var icon=new GameObject("盾牌",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));icon.transform.SetParent(shield.transform.parent,false);var image=icon.GetComponent<Image>();image.sprite=Resources.Load<Sprite>(FormalArtRegistry.ResourceMetricPath("shield"));image.raycastTarget=false;image.rectTransform.sizeDelta=new Vector2(16,16);image.rectTransform.anchoredPosition=new Vector2(-8,0);
                    foreach(var graphic in parent.GetComponentsInChildren<Graphic>())graphic.raycastTarget=false;
                }
                label.gameObject.SetActive(true);shieldBadges[id].transform.parent.gameObject.SetActive(m.Unit.Shield>0);shieldBadges[id].text=m.Unit.Shield.ToString();
                var resource=m.Vitals??CombatUnitVitalsPresentation.From(m.Unit,null);
                RefreshResource(healthBars[id],resource.Health,new Vector2(0,6),16,true);
                RefreshResource(manaBars[id],resource.Mana,new Vector2(0,-8),8,false);
                // Foot resources stay attached to their unit; label avoidance must not move them beside another unit.
                var result=new Rect(Mathf.Clamp(body.center.x-82,viewport.xMin,viewport.xMax-164),Mathf.Clamp(body.yMin-34,viewport.yMin,viewport.yMax-28),164,28);
                label.anchoredPosition=result.center;placed.Add(result);information.Add(result);
            }
            RefreshMarkers(viewport);
        }
        private static void RefreshResource(FormalResourceBarView bar,CombatUnitVitalPresentation vital,Vector2 offset,float height,bool value)
        {
            Hd2DTacticalGlow.Attach(bar.Fill);Hd2DTacticalGlow.Attach(bar.Forecast);Hd2DTacticalGlow.Attach(bar.Value,1);
            var r=bar.Root;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=offset;r.sizeDelta=new Vector2(128,height);
            bar.gameObject.SetActive(vital.Maximum>0);bar.Fill.fillAmount=vital.RemainingRatio;
            bar.Forecast.gameObject.SetActive(vital.ForecastLoss>0);bar.Forecast.rectTransform.anchorMin=new Vector2(vital.RemainingRatio,0);bar.Forecast.rectTransform.anchorMax=new Vector2(vital.CurrentRatio,1);bar.Forecast.color=new Color(1,.6f,.25f,.9f);
            bar.Value.gameObject.SetActive(value);bar.Value.fontSize=12;bar.Value.text=vital.Current+"/"+vital.Maximum+(vital.ForecastLoss>0?" → "+vital.Remaining:"");
            bar.Marker.gameObject.SetActive(false);
        }
        private void RefreshObjectDurability(CombatState state,Dictionary<GridPosition,BattlefieldCellPresentation> models)
        {
            bool visible=false;
            if(sample.HoveredCell.HasValue&&!host.IsInteractionModalOpen&&models.TryGetValue(sample.HoveredCell.Value,out var model)&&!string.IsNullOrWhiteSpace(model.ObjectHoverText))
            {
                var tile=state.Map.GetTile(sample.HoveredCell.Value);
                if(tile.MaxDurability>0&&tile.Durability>0)
                {
                    if(objectDurability==null){objectDurability=FormalResourceBarView.InstantiateUnit(transform);objectDurability.ConfigureCompact("物块耐久",null,FormalUiTheme.Amber);foreach(var g in objectDurability.GetComponentsInChildren<Graphic>())g.raycastTarget=false;}
                    var ratio=new CombatUnitVitalPresentation(tile.Durability,tile.MaxDurability,0,tile.Durability);
                    var local=(Vector2)transform.InverseTransformPoint(GetComponentInParent<Canvas>().rootCanvas.transform.TransformPoint(sample.GridToFeedbackPosition(sample.HoveredCell.Value)));
                    RefreshResource(objectDurability,ratio,local-new Vector2(0,14),16,true);visible=true;
                }
            }
            if(objectDurability!=null)objectDurability.gameObject.SetActive(visible);
        }
        private void RefreshMarkers(Rect viewport)
        {
            if(actorFrame==null)
            {
                var go=new GameObject("当前行动者黑底白框",typeof(RectTransform));go.transform.SetParent(transform,false);actorFrame=(RectTransform)go.transform;
                for(int i=0;i<4;i++)
                {
                    var corner=new GameObject("框角"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));corner.transform.SetParent(actorFrame,false);var image=corner.GetComponent<Image>();image.color=Color.white;image.raycastTarget=false;
                    var r=image.rectTransform;r.anchorMin=r.anchorMax=new Vector2(i%2,i/2);r.sizeDelta=new Vector2(12,4);r.anchoredPosition=Vector2.zero;
                    var edge=corner.AddComponent<Outline>();edge.effectColor=Color.black;edge.effectDistance=new Vector2(2,-2);
                    var upright=new GameObject("框角竖线"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));upright.transform.SetParent(actorFrame,false);var line=upright.GetComponent<Image>();line.color=Color.white;line.raycastTarget=false;line.rectTransform.anchorMin=line.rectTransform.anchorMax=r.anchorMin;line.rectTransform.sizeDelta=new Vector2(4,12);line.rectTransform.anchoredPosition=Vector2.zero;var border=upright.AddComponent<Outline>();border.effectColor=Color.black;border.effectDistance=new Vector2(2,-2);
                }
            }
            bool shown=bodies.TryGetValue(host.CurrentState.ActiveUnitId??"",out var active)&&active.Overlaps(viewport);actorFrame.gameObject.SetActive(shown);
            if(shown)
            {
                if(markedActor!=host.CurrentState.ActiveUnitId){markerStart=markedActor==null?active.center:actorFrame.anchoredPosition;markedActor=host.CurrentState.ActiveUnitId;markerElapsed=0;}
                markerElapsed+=Time.unscaledDeltaTime;float t=host.UiPreferences.AnimationIntensity>0?Mathf.Clamp01(markerElapsed/.32f):1;
                actorFrame.sizeDelta=active.size+Vector2.one*8;actorFrame.anchoredPosition=Vector2.Lerp(markerStart,active.center,t*t*(3-2*t));
            }
            if(timelineArrow==null)
            {
                var go=new GameObject("行动条悬浮头顶箭头",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));go.transform.SetParent(transform,false);timelineArrow=go.GetComponent<RawImage>();timelineArrow.raycastTarget=false;
                var sprite=Resources.Load<Sprite>(BattlefieldMarkerLadder.TimelineHoverArrowPath);if(sprite!=null)timelineArrow.texture=sprite.texture;timelineArrow.rectTransform.sizeDelta=new Vector2(32,32);
            }
            bool hovered=formal!=null&&bodies.TryGetValue(formal.TimelineHoveredUnitId??"",out var target);timelineArrow.gameObject.SetActive(hovered);
            if(hovered){var b=bodies[formal.TimelineHoveredUnitId];timelineArrow.rectTransform.anchoredPosition=new Vector2(b.center.x,b.yMax+22+Mathf.Sin(Time.unscaledTime*6)*3);}
        }
        public void ShowResult(GridPosition p,bool accepted){if(ranges.TryGetValue(p,out var graphic))graphic.ShowResult(accepted);}
        public void AppendFeedbackObstacles(List<Rect> destination,bool protectBodies,string owner)
        {
            var canvas=GetComponentInParent<Canvas>().rootCanvas.transform;
            void Append(Rect r){var min=(Vector2)canvas.InverseTransformPoint(transform.TransformPoint(r.min));var max=(Vector2)canvas.InverseTransformPoint(transform.TransformPoint(r.max));destination.Add(Rect.MinMaxRect(min.x,min.y,max.x,max.y));}
            foreach(var r in information)Append(r);
            foreach(var pair in bodies)if(protectBodies||pair.Key!=owner)Append(pair.Value);
        }
        private void OnDisable(){changes.Clear();changedCells.Clear();if(routes!=null)routes.Clear();}
        private void OnDestroy(){if(tacticalEmission!=null)Destroy(tacticalEmission);}
    }
}
