using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Live HD-2D renderer shared by formal entry and visual review tools.
    public sealed class Hd2DBattlefieldSample:MonoBehaviour,IPointerClickHandler,IPointerMoveHandler,IPointerExitHandler,IScrollHandler,IBeginDragHandler,IDragHandler
    {
        private const int Layer=15;
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly Dictionary<Texture2D,Material> textured=new Dictionary<Texture2D,Material>();
        private readonly Dictionary<Texture2D,Material> billboardTextured=new Dictionary<Texture2D,Material>();
        private GameObject world;
        private Camera cameraView;
        private RenderTexture output;
        private VolumeProfile profile;
        private Hd2DBattleLighting localLighting;
        private Hd2DBattleAtmosphere atmosphere;
        public bool TrySetAtmosphere(Hd2DBattleTimeOfDay timeOfDay,Hd2DBattleWeather weather,float transitionSeconds=.8f)
        {
            if(atmosphere==null)return false;
            atmosphere.Set(timeOfDay,weather,transitionSeconds);return true;
        }
        public int LocalDiffuseUpdates=>localLighting!=null?localLighting.DiffuseUpdates:0;
        public string LocalDiffuseStatus=>localLighting!=null?localLighting.DiffuseStatus:"not_initialized";
        public int LocalDiffuseSurfaces=>localLighting!=null?localLighting.BoundSurfaces:0;
        public float LocalCapturedRadianceRange=>localLighting!=null?localLighting.CapturedRadianceRange:0;
        public void PumpLocalLightingReview()
        {if(localLighting!=null)localLighting.TickReflectionCapture();}
        public void SetLocalLightingReview(bool contact,bool diffuse)
        {if(localLighting==null)return;localLighting.ContactEnabled=contact;localLighting.DiffuseEnabled=diffuse;localLighting.ApplyToSurfaces();}
        private LineRenderer selection;
        private int width,height;
        private CombatPrototypeBootstrap host;
        private Hd2DBattlefieldProfile rendering;
        private CombatState boundState;
        private Vector3 overviewPosition;
        private float overviewFieldOfView;
        private float overviewDistance;
        private int drawnCombatVersion=-1,drawnActionVersion=-1,drawnEnvironmentFrame=-1;
        private string drawnAction,drawnTarget;
        private GridPosition? drawnHover,drawnSelection;
        private float motionUntil;
        public void ResetOverview(){cameraView.transform.localPosition=overviewPosition;cameraView.fieldOfView=overviewFieldOfView;}
        public void FollowVisualPosition(Vector2 foot)
        {
            if(cameraView==null||cameraView.fieldOfView>=overviewFieldOfView)return;
            Vector3 point=world.transform.TransformPoint(new Vector3(foot.x+.5f,0,foot.y+.5f));
            var viewport=cameraView.WorldToViewportPoint(point);
            var safe=new Vector2(Mathf.Clamp(viewport.x,.12f,.88f),Mathf.Clamp(viewport.y,.12f,.88f));
            if(Mathf.Abs(safe.x-viewport.x)<.001f&&Mathf.Abs(safe.y-viewport.y)<.001f)return;
            var ray=cameraView.ViewportPointToRay(safe);
            if(new Plane(Vector3.up,world.transform.position).Raycast(ray,out float distance))
            {
                var delta=point-ray.GetPoint(distance);cameraView.transform.position+=new Vector3(delta.x,0,delta.z);
            }
        }
        public void OnScroll(PointerEventData e)
        {
            if(host.IsInteractionModalOpen)return;
            cameraView.fieldOfView=Mathf.Clamp(cameraView.fieldOfView-e.scrollDelta.y*2,16,36);
            OnPointerExit(e);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||host.IsInteractionModalOpen)return;
            e.eligibleForClick=false;OnPointerExit(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||host.IsInteractionModalOpen)return;
            var r=(RectTransform)transform;float scale=GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
            float units=2*overviewDistance*Mathf.Tan(cameraView.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,r.rect.height*scale);
            var p=cameraView.transform.localPosition;p.x-=e.delta.x*units;p.z-=e.delta.y*units/Mathf.Sin(Hd2DViewProfile.BattlefieldPitch*Mathf.Deg2Rad);
            p.x=Mathf.Clamp(p.x,overviewPosition.x-width*.5f,overviewPosition.x+width*.5f);p.z=Mathf.Clamp(p.z,overviewPosition.z-height*.5f,overviewPosition.z+height*.5f);cameraView.transform.localPosition=p;
        }
        private System.Action<GridPosition,int> primary;
        private System.Action<GridPosition> context;
        private readonly Dictionary<string, MeshRenderer> faces = new Dictionary<string, MeshRenderer>();
        private readonly Dictionary<string, MeshRenderer> spriteShadows = new Dictionary<string, MeshRenderer>();
        private readonly Dictionary<string,(Rect uv,float w,float h,bool floor,float rotation)> shapes = new Dictionary<string,(Rect,float,float,bool,float)>();
        private readonly Dictionary<GridPosition,LineRenderer> ranges = new Dictionary<GridPosition,LineRenderer>();
        private readonly Dictionary<GridPosition,Text> labels = new Dictionary<GridPosition,Text>();
        private MaterialPropertyBlock tint;
        private readonly Dictionary<GridPosition,BattlefieldCellPresentation> frameModels=new Dictionary<GridPosition,BattlefieldCellPresentation>();
        public Dictionary<GridPosition,BattlefieldCellPresentation> FrameModels=>frameModels;
        private Material rangeMaterial;
        private Material stoneMaterial;
        private readonly Dictionary<GridPosition,Material> waterMaterials=new Dictionary<GridPosition,Material>();
        private bool[] cachedWaterLayout;
        private readonly Dictionary<GridPosition,Light> crystalLights=new Dictionary<GridPosition,Light>();
        private Texture2D masonryTexture;
        private ReflectionProbe courtyardReflection;
        private int reflectionVersion=-1;
        private float reflectionChangedAt,lastReflectionAt=-10;
        public int ReflectionCaptures{get;private set;}
        private Hd2DAcademyEnvironment environment;
        private readonly Dictionary<Texture2D,Material> groundedFloorMaterials=new Dictionary<Texture2D,Material>();
        private void SetRainWetness(float amount)
        {
            foreach(var pair in groundedFloorMaterials)ApplyRainCoating(pair.Value,pair.Key,amount);
            if(environment!=null)environment.SetRainWetness(amount);
        }
        private static void ApplyRainCoating(Material material,Texture2D source,float amount)
        {
            if(material.shader.name!="OCC/HD2D/BakedArchitecture")return;
            bool stone=false,attachment=false;
            foreach(var entry in Hd2DBattlefieldMaterialMaps.Entries)
                if(entry.Albedo==source){stone=entry.Family!="earth";break;}
            foreach(var entry in Hd2DGroundAttachmentMaps.Entries)
                if(entry.Albedo==source){attachment=true;break;}
            if(!stone&&!attachment)return;
            material.SetFloat("_ClearCoatMask",Mathf.Clamp01(amount)*.42f);
            if(amount>.001f)material.EnableKeyword("_OCC_WEATHER_WET");else material.DisableKeyword("_OCC_WEATHER_WET");
        }
        private Material GroundLit(Texture2D texture)
        {
            if(groundedFloorMaterials.TryGetValue(texture,out var material))return material;
            bool rain=rendering.WetStone;
            material=new Material(Lit(texture)){shader=Shader.Find(rendering.GroundShader)};
            if(rain){material.SetFloat("_Smoothness",.08f);material.SetFloat("_ClearCoatSmoothness",.88f);}
            Hd2DBattlefieldMaterialMaps.Apply(material,texture);
            Hd2DGroundAttachmentMaps.Apply(material,texture);
            if(atmosphere!=null)ApplyRainCoating(material,texture,atmosphere.RainAmount);
            materials.Add(material);groundedFloorMaterials.Add(texture,material);return material;
        }
        private readonly Dictionary<GridPosition,MeshRenderer> walls=new Dictionary<GridPosition,MeshRenderer>();
        public void ConfigureInput(System.Action<GridPosition,int> onPrimary, System.Action<GridPosition> onContext)
        {primary=onPrimary;context=onContext;}
        public Vector2 GridToFeedbackPosition(GridPosition position)
        {
            var viewport=cameraView.WorldToViewportPoint(world.transform.TransformPoint(new Vector3(position.X+.5f,0,position.Y+.5f)));
            var rect=(RectTransform)transform;
            var local=new Vector2(rect.rect.xMin+viewport.x*rect.rect.width,rect.rect.yMin+viewport.y*rect.rect.height);
            var root=GetComponentInParent<Canvas>().rootCanvas;
            return root.transform.InverseTransformPoint(rect.TransformPoint(local));
        }
        public GridPosition? SelectedCell{get;private set;}
        private GridPosition? hovered;
        public GridPosition? HoveredCell=>hovered;
        public Camera RenderCamera=>cameraView;
        public MeshRenderer UnitRenderer(GridPosition p)=>faces.TryGetValue("单位_"+p,out var body)&&body.gameObject.activeSelf?body:null;
        public Vector2 ProjectLocal(Vector3 point)
        {
            var v=cameraView.WorldToViewportPoint(point);var r=((RectTransform)transform).rect;
            return new Vector2(r.xMin+v.x*r.width,r.yMin+v.y*r.height);
        }
        public Vector2 UnitToFeedbackPosition(GridPosition p)
        {
            var body=UnitRenderer(p);
            // Retain the last rendered anchor for a lethal hit after the unit has disappeared.
            if(body==null&&!faces.TryGetValue("单位_"+p,out body))return GridToFeedbackPosition(p);
            var point=body.transform.TransformPoint(new Vector3(0,body.GetComponent<MeshFilter>().sharedMesh.bounds.max.y,0));
            var local=ProjectLocal(point);var root=GetComponentInParent<Canvas>().rootCanvas;
            return root.transform.InverseTransformPoint(transform.TransformPoint(local));
        }
        public bool Matches(CombatState state)=>ReferenceEquals(state,boundState);
        public static Hd2DBattlefieldSample Create(CombatPrototypeBootstrap host,Transform parent)
        {
            if(host==null||host.CurrentState==null)throw new System.InvalidOperationException("A real current combat model is required");
            var go=new GameObject("HD2D战场迁移样板",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(Hd2DBattlefieldSample));
            go.transform.SetParent(parent,false);FormalUiKit.Stretch((RectTransform)go.transform);
            var canvas=go.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=UiLayoutContract.BattlefieldSortingOrder+1;
            go.AddComponent<GraphicRaycaster>();
            var sample=go.GetComponent<Hd2DBattlefieldSample>();sample.Build(host);
            return sample;
        }
        private Material Lit(Texture2D texture)
        {
            if(textured.TryGetValue(texture,out var saved))return saved;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){mainTexture=texture,color=Color.white};
            material.SetFloat("_Smoothness",0);material.SetFloat("_Cull",0);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");
            materials.Add(material);textured.Add(texture,material);return material;
        }
        private Material BillboardLit(Texture2D texture)
        {
            if(billboardTextured.TryGetValue(texture,out var saved))return saved;
            var shader=Shader.Find("OCC/HD2D/GroundedSprite");
            if(shader==null)throw new System.InvalidOperationException("Missing grounded sprite shader");
            var material=new Material(Lit(texture)){shader=shader};material.SetFloat("_ReceiveShadows",0);
            material.EnableKeyword("_ALPHATEST_ON");material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            Hd2DBattlefieldPropMaps.Apply(material,texture);
            materials.Add(material);billboardTextured.Add(texture,material);return material;
        }
        private MeshRenderer Face(string name,Texture2D texture,Rect uv,Vector3 position,float w,float h,bool floor,float rotation=0)
        {
            if(texture==null)
            {
                if(faces.TryGetValue(name,out var hidden))hidden.gameObject.SetActive(false);
                if(spriteShadows.TryGetValue(name,out var shadow))shadow.gameObject.SetActive(false);
                return null;
            }
            MeshRenderer renderer;
            Mesh mesh;
            GameObject go;
            if(faces.TryGetValue(name,out renderer))
            {go=renderer.gameObject;go.SetActive(true);mesh=go.GetComponent<MeshFilter>().sharedMesh;}
            else
            {
                go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=Layer};go.transform.SetParent(world.transform,false);
                mesh=new Mesh{name=name};meshes.Add(mesh);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                renderer=go.GetComponent<MeshRenderer>();faces.Add(name,renderer);
            }
            go.transform.localPosition=position;
            go.transform.localRotation=floor?Quaternion.identity:Hd2DViewProfile.BattlefieldUnitRotation;
            if(!floor)
            {
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                var spriteMaterial=Lit(texture);
                spriteMaterial.SetFloat("_ReceiveShadows",0);
                spriteMaterial.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                if(!spriteShadows.TryGetValue(name,out var shadow))
                {
                    var caster=new GameObject(name+"_直立投影",typeof(MeshFilter),typeof(MeshRenderer)){layer=Layer};
                    caster.transform.SetParent(world.transform,false);caster.GetComponent<MeshFilter>().sharedMesh=mesh;
                    shadow=caster.GetComponent<MeshRenderer>();shadow.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
                    shadow.receiveShadows=false;spriteShadows.Add(name,shadow);
                }
                shadow.gameObject.SetActive(true);shadow.transform.localPosition=position;
                shadow.sharedMaterial=Lit(texture);
            }
            var shape=(uv,w,h,floor,rotation);
            if(shapes.TryGetValue(name,out var previous)&&previous.Equals(shape))
            {renderer.sharedMaterial=floor?GroundLit(texture):BillboardLit(texture);return renderer;}
            shapes[name]=shape;
            mesh.vertices=floor?new[]{Vector3.zero,new Vector3(w,0,0),new Vector3(w,0,h),new Vector3(0,0,h)}:
                new[]{new Vector3(-w*.5f,0,0),new Vector3(w*.5f,0,0),new Vector3(w*.5f,h,0),new Vector3(-w*.5f,h,0)};
            var coords=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            for(int i=0;i<coords.Length;i++)
            {
                var p=Quaternion.Euler(0,0,rotation)*new Vector3(coords[i].x-.5f,coords[i].y-.5f,0);
                coords[i]=new Vector2(uv.x+(p.x+.5f)*uv.width,uv.y+(p.y+.5f)*uv.height);
            }
            mesh.uv=coords;mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            if(floor){var vertices=mesh.vertices;var mask=new Vector2[vertices.Length];for(int i=0;i<vertices.Length;i++)mask[i]=new Vector2(environment!=null?environment.GroundVisibility(position+vertices[i]):1,0);mesh.uv2=mask;}
            renderer.sharedMaterial=floor?GroundLit(texture):BillboardLit(texture);
            return renderer;
        }
        private void Build(CombatPrototypeBootstrap host)
        {
            this.host=host;
            rendering=Hd2DBattlefieldProfile.Resolve(host.CurrentLevelId);
            boundState=host.CurrentState;
            tint=new MaterialPropertyBlock();
            width=host.CurrentState.Map.Width;height=host.CurrentState.Map.Height;
            world=new GameObject("HD2D战场独立空间"){layer=Layer};world.transform.position=new Vector3(400,500,400);
            var cameraObject=new GameObject("HD2D战场相机",typeof(Camera),typeof(UniversalAdditionalCameraData)){layer=Layer};cameraObject.transform.SetParent(world.transform,false);
            cameraView=cameraObject.GetComponent<Camera>();cameraView.fieldOfView=Hd2DViewProfile.DioramaFieldOfView;cameraView.nearClipPlane=.1f;cameraView.farClipPlane=100;cameraView.cullingMask=1<<Layer;cameraView.backgroundColor=new Color(.09f,.085f,.075f);cameraView.clearFlags=CameraClearFlags.SolidColor;
            int renderWidth=Mathf.RoundToInt(host.CurrentBattlefieldViewport.Width),renderHeight=Mathf.RoundToInt(host.CurrentBattlefieldViewport.Height);
            cameraView.aspect=renderWidth/(float)renderHeight;
            var target=new Vector3(width*.5f,.5f,height*.5f+.5f);float yaw=Hd2DViewProfile.DioramaYaw*Mathf.Deg2Rad;
            float span=width*Mathf.Cos(yaw)+height*Mathf.Sin(yaw);
            float depth=height*Mathf.Cos(yaw)+width*Mathf.Sin(yaw);
            float visibleHeight=Mathf.Max(span/cameraView.aspect,depth*Mathf.Sin(Hd2DViewProfile.BattlefieldPitch*Mathf.Deg2Rad)+2f)+1f;
            visibleHeight=Mathf.Max(visibleHeight,6.6f);
            float distance=visibleHeight/(2*Mathf.Tan(cameraView.fieldOfView*.5f*Mathf.Deg2Rad));
            cameraObject.transform.localPosition=Hd2DViewProfile.BattlefieldCameraPosition(target,distance);cameraObject.transform.localRotation=Hd2DViewProfile.BattlefieldRotation;
            overviewPosition=cameraObject.transform.localPosition;overviewFieldOfView=cameraView.fieldOfView;overviewDistance=distance;
            output=new RenderTexture(renderWidth,renderHeight,24,RenderTextureFormat.ARGBHalf){filterMode=FilterMode.Bilinear};output.Create();cameraView.targetTexture=output;cameraView.allowHDR=true;
            var data=cameraView.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=true;data.volumeLayerMask=1<<Layer;
            localLighting=world.AddComponent<Hd2DBattleLighting>();localLighting.Initialize(cameraView,world.transform);
            var volumeObject=new GameObject("战场样板色调",typeof(Volume)){layer=Layer};volumeObject.transform.SetParent(world.transform,false);
            profile=ScriptableObject.CreateInstance<VolumeProfile>();var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
            var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.value=rendering.Exposure;grade.contrast.value=9f;grade.saturation.value=2f;
            var split=profile.Add<SplitToning>(true);split.shadows.value=new Color(.43f,.48f,.57f);split.highlights.value=new Color(.57f,.53f,.46f);split.balance.value=5f;
            var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1.25f;bloom.intensity.value=.18f;bloom.scatter.value=.45f;bloom.tint.value=new Color(1,.92f,.76f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.16f;vignette.smoothness.value=.5f;
            // Only distant scenery falls out of focus; every interactive ground cell stays sharp.
            var focus=profile.Add<DepthOfField>(true);focus.mode.value=DepthOfFieldMode.Gaussian;focus.gaussianStart.value=distance+3.5f;focus.gaussianEnd.value=distance+10f;focus.gaussianMaxRadius.value=.6f;
            var volume=volumeObject.GetComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            var sunObject=new GameObject("战场统一日光",typeof(Light)){layer=Layer};sunObject.transform.SetParent(world.transform,false);sunObject.transform.localRotation=Quaternion.Euler(rendering.MainEuler);
            var sun=sunObject.GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=rendering.MainIntensity;sun.color=rendering.MainColour;sun.cullingMask=1<<Layer;sun.shadows=LightShadows.Soft;sun.shadowStrength=.82f;sun.shadowBias=.025f;sun.shadowNormalBias=.035f;
            sun.GetUniversalAdditionalLightData().softShadowQuality=SoftShadowQuality.High;
            var fillObject=new GameObject("战场柔和补光",typeof(Light)){layer=Layer};fillObject.transform.SetParent(world.transform,false);fillObject.transform.localPosition=target+new Vector3(0,4,-2);
            fillObject.transform.localRotation=Quaternion.Euler(55,-130,0);
            var fill=fillObject.GetComponent<Light>();fill.type=LightType.Directional;fill.color=rendering.FillColour;fill.intensity=rendering.FillIntensity;fill.shadows=LightShadows.None;fill.cullingMask=1<<Layer;
            var lineObject=new GameObject("投影选格边界",typeof(LineRenderer)){layer=Layer};lineObject.transform.SetParent(world.transform,false);selection=lineObject.GetComponent<LineRenderer>();selection.useWorldSpace=false;selection.loop=true;selection.positionCount=4;selection.widthMultiplier=.025f;
            var lineMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit")){color=new Color(.7f,.9f,.85f)};materials.Add(lineMaterial);selection.sharedMaterial=lineMaterial;selection.enabled=false;
            rangeMaterial=lineMaterial;
            stoneMaterial=new Material(Shader.Find("OCC/HD2D/BakedArchitecture")){color=new Color(.82f,.84f,.86f)};
            var masonrySource=Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_court_b");
            if(masonrySource!=null)
            {
                masonryTexture=new Texture2D(masonrySource.width,masonrySource.height,masonrySource.format,masonrySource.mipmapCount>1,
                    !UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(masonrySource.graphicsFormat));
                Graphics.CopyTexture(masonrySource,masonryTexture);
                masonryTexture.name="墙体石材32PPU运行时采样";
                masonryTexture.wrapMode=TextureWrapMode.Repeat;masonryTexture.filterMode=FilterMode.Point;
                stoneMaterial.mainTexture=masonryTexture;
            }
            Hd2DBattlefieldMaterialMaps.Apply(stoneMaterial,masonrySource);
            stoneMaterial.SetFloat("_Smoothness",0);materials.Add(stoneMaterial);
            var foundation=GameObject.CreatePrimitive(PrimitiveType.Cube);foundation.name="真实地台厚度";foundation.layer=Layer;
            foundation.transform.SetParent(world.transform,false);foundation.transform.localPosition=new Vector3(width*.5f,-.14f,height*.5f);
            foundation.transform.localScale=new Vector3(width,.28f,height);foundation.GetComponent<MeshRenderer>().sharedMaterial=stoneMaterial;
            MapStoneBlock(foundation);
            Destroy(foundation.GetComponent<Collider>());
            environment=world.AddComponent<Hd2DAcademyEnvironment>();environment.Build(width,height,Layer);
            if(rendering.LocalReflection)
            {
                var probeObject=new GameObject("雨后庭院局部反射",typeof(ReflectionProbe)){layer=Layer};probeObject.transform.SetParent(world.transform,false);
                probeObject.transform.localPosition=new Vector3(width*.5f,2.5f,height*.5f);
                courtyardReflection=probeObject.GetComponent<ReflectionProbe>();
                courtyardReflection.mode=ReflectionProbeMode.Realtime;courtyardReflection.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
                courtyardReflection.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;courtyardReflection.resolution=256;courtyardReflection.hdr=true;
                courtyardReflection.size=new Vector3(width+30,12,height+40);courtyardReflection.boxProjection=true;courtyardReflection.blendDistance=1;
                courtyardReflection.cullingMask=1<<Layer;courtyardReflection.nearClipPlane=.1f;courtyardReflection.farClipPlane=50;
                courtyardReflection.clearFlags=ReflectionProbeClearFlags.SolidColor;courtyardReflection.backgroundColor=new Color(.2f,.27f,.34f);courtyardReflection.intensity=.8f;
            }
            int moduleIndex=0;
            foreach(var placement in AcademyBattlefieldLayoutCatalog.VisualModules(host.CurrentLevelId))
            {
                var texture=Resources.Load<Texture2D>("Art/FormalAcademyStructures32/"+placement.AssetId);
                if(texture==null)continue;
                bool ground=placement.IsGroundAttachment;
                Face("学院结构_"+moduleIndex++,texture,new Rect(0,0,1,1),
                    ground?new Vector3(placement.X,.018f,placement.TopY-placement.HeightCells+1):
                        new Vector3(placement.X+placement.WidthCells*.5f,.02f,placement.TopY-placement.HeightCells+1),
                    ground?placement.WidthCells:texture.width/32f,ground?placement.HeightCells:texture.height/32f,ground,placement.QuarterTurns*90);
            }
            gameObject.AddComponent<Hd2DBattlefieldTactics>().Initialize(this,host,world.transform,Layer);
            var plate=GetComponent<RawImage>();plate.texture=output;plate.raycastTarget=true;
            atmosphere=world.AddComponent<Hd2DBattleAtmosphere>();
            atmosphere.Initialize(cameraView,width,height,()=>reflectionChangedAt=Time.unscaledTime,SetRainWetness);
        }
        public bool TryProjectCell(Vector2 viewport,out GridPosition cell)
        {
            cell=default;if(cameraView==null||viewport.x<0||viewport.x>1||viewport.y<0||viewport.y>1)return false;
            var ray=cameraView.ViewportPointToRay(new Vector3(viewport.x,viewport.y,0));var plane=new Plane(Vector3.up,world.transform.position);
            if(!plane.Raycast(ray,out float distance)||distance<0)return false;
            var local=world.transform.InverseTransformPoint(ray.GetPoint(distance));int x=Mathf.FloorToInt(local.x),y=Mathf.FloorToInt(local.z);
            if(x<0||x>=width||y<0||y>=height)return false;cell=new GridPosition(x,y);return true;
        }
        public int CheckCellRoundTrips()
        {
            int count=0;for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                var v=cameraView.WorldToViewportPoint(world.transform.TransformPoint(new Vector3(x+.5f,0,y+.5f)));
                if(!TryProjectCell(v,out var p)||p.X!=x||p.Y!=y)throw new System.InvalidOperationException("HD2D projection mismatch at "+x+","+y);count++;
            }return count;
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,eventData.position,eventData.pressEventCamera,out var local))return;
            var viewport=new Vector2((local.x-rect.rect.xMin)/rect.rect.width,(local.y-rect.rect.yMin)/rect.rect.height);
            if(!TryProjectCell(viewport,out var p))return;
            SelectedCell=p;
            if(eventData.button==PointerEventData.InputButton.Right){context?.Invoke(p);return;}
            if(eventData.button!=PointerEventData.InputButton.Left)return;
            primary?.Invoke(p,eventData.clickCount);
            selection.enabled=false;
            selection.SetPositions(new[]{new Vector3(p.X,.025f,p.Y),new Vector3(p.X+1,.025f,p.Y),new Vector3(p.X+1,.025f,p.Y+1),new Vector3(p.X,.025f,p.Y+1)});
        }
        private void LateUpdate()
        {
            if(host==null||host.CurrentState==null||world==null)return;
            var viewRect=((RectTransform)transform).rect;float scale=GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
            int rw=Mathf.Max(1,Mathf.RoundToInt(viewRect.width*scale)),rh=Mathf.Max(1,Mathf.RoundToInt(viewRect.height*scale));
            if(output.width!=rw||output.height!=rh)
            {
                cameraView.targetTexture=null;output.Release();output.width=rw;output.height=rh;output.Create();cameraView.targetTexture=output;cameraView.aspect=rw/(float)rh;
            }
            if(host.CurrentState.Map.Width!=width||host.CurrentState.Map.Height!=height)return;
            int combatVersion=host.UiPresentationVersions.Version(UiPresentationArea.Combat),actionVersion=host.CombatActionPresentationVersion,environmentFrame=Mathf.FloorToInt(Time.unscaledTime*8);
            if(courtyardReflection!=null)
            {
                if(reflectionVersion!=combatVersion){reflectionVersion=combatVersion;reflectionChangedAt=Time.unscaledTime;}
                if(!host.IsCombatActionPlaying&&!host.IsCombatEntryBlocking&&Time.unscaledTime-reflectionChangedAt>.2f&&Time.unscaledTime-lastReflectionAt>1f&&
                    (localLighting==null||!localLighting.CaptureInProgress)&&
                    (lastReflectionAt<reflectionChangedAt))
                {localLighting.RequestReflectionCapture(courtyardReflection);lastReflectionAt=Time.unscaledTime;ReflectionCaptures++;}
                if(localLighting!=null)localLighting.TickReflectionCapture();
            }
            if(combatVersion!=drawnCombatVersion)RefreshWaterLayout();
            bool changed=combatVersion!=drawnCombatVersion||actionVersion!=drawnActionVersion||drawnAction!=host.SelectedAction||drawnTarget!=host.SelectedTargetId||drawnHover!=hovered||drawnSelection!=SelectedCell;
            if(changed)motionUntil=Time.unscaledTime+.45f;
            // Continuous movement and hit poses retain full-rate updates. Static models only need
            // rebuilding on actual interaction/state changes or the existing 8 Hz environment tick.
            if(!changed&&!host.IsCombatActionPlaying&&!host.IsCombatEntryBlocking&&Time.unscaledTime>motionUntil&&drawnEnvironmentFrame==environmentFrame)return;
            drawnCombatVersion=combatVersion;drawnActionVersion=actionVersion;drawnAction=host.SelectedAction;drawnTarget=host.SelectedTargetId;drawnHover=hovered;drawnSelection=SelectedCell;drawnEnvironmentFrame=environmentFrame;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                var p=new GridPosition(x,y);var model=host.PresentBattlefieldCell(p);frameModels[p]=model;if(model==null)continue;
                RefreshCrystalLight(p);
                var groundBody=Face("地面_"+p,model.FloorTexture,model.FloorUv,new Vector3(x,0,y),1,1,true,model.FloorRotationDegrees);
                if(rendering.WetStone&&groundBody!=null)
                {
                    // Only native stone and explicitly defined water cells receive wetness.
                    var tile=host.CurrentState.Map.GetTile(p);
                    tint.Clear();tint.SetFloat("_ClearCoatMask",tile.IsWater?1f:model.FloorTexture!=null&&model.FloorTexture.name.StartsWith("academy_block_court")?.42f:0f);
                    groundBody.SetPropertyBlock(tint);
                }
                Texture2D obj=model.ObjectTexture;
                bool wall=host.CurrentState.Map.GetTile(p).IsPermanentWall;
                RefreshWall(p,wall);if(wall)obj=null;
                Face("物件_"+p,obj,new Rect(0,0,1,1),new Vector3(x+.5f,.01f,y+.5f),obj!=null?obj.width/32f:1,obj!=null?obj.height/32f:1,false);
                Texture2D unit=model.UnitTexture;
                var offset=new Vector3(model.UnitTravelOffset.x/64f+model.UnitOffset.x/32f, -model.UnitOffset.y/32f,-model.UnitTravelOffset.y/64f);
                var body=Face("单位_"+p,unit,model.UnitUv,new Vector3(x+.5f,.02f,y+.5f)+offset,unit!=null?unit.width*model.UnitUv.width/32f:1,unit!=null?unit.height*model.UnitUv.height/32f:1,false);
                if(body!=null)
                {
                    // Pivot remains at the native foot anchor; only the unit plane leans back.
                    body.transform.localRotation=Hd2DViewProfile.BattlefieldUnitRotation;
                    tint.SetColor("_BaseColor",model.UnitTint);body.SetPropertyBlock(tint);
                }
                var environmentBody=Face("环境_"+p,model.EnvironmentTexture,new Rect(0,0,1,1),new Vector3(x,.006f,y),1,1,true);
                if(rendering.WetStone&&environmentBody!=null)
                {tint.Clear();tint.SetFloat("_ClearCoatMask",host.CurrentState.Map.GetTile(p).IsWater?1f:0f);environmentBody.SetPropertyBlock(tint);}
                if(environmentBody!=null&&host.CurrentState.Map.GetTile(p).IsWater&&
                    model.EnvironmentTexture!=null&&model.EnvironmentTexture.name=="rain_court_water")
                {
                    environmentBody.sharedMaterial=WaterMaterial(p,model.EnvironmentTexture);environmentBody.SetPropertyBlock(null);
                }
                Face("战利品_"+p,model.LootTexture,new Rect(0,0,1,1),new Vector3(x+.5f,.012f,y+.5f),1,1,false);
                // Projected tactics and labels consume the current rendered bodies after this update.
            }
            if(localLighting!=null)localLighting.ApplyToSurfaces();
        }
        private void RefreshWaterLayout()
        {
            if(cachedWaterLayout==null)cachedWaterLayout=new bool[width*height];
            bool changed=false;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int index=y*width+x;bool water=host.CurrentState.Map.GetTile(new GridPosition(x,y)).IsWater;
                if(cachedWaterLayout[index]!=water){changed=true;cachedWaterLayout[index]=water;}
            }
            if(!changed)return;
            // Spells can create water and artifacts can remove it. Rebuild the
            // affected material regions after topology changes, not every frame.
            foreach(var material in new HashSet<Material>(waterMaterials.Values))
            {materials.Remove(material);Destroy(material);}
            waterMaterials.Clear();
        }
        private Material WaterMaterial(GridPosition position,Texture2D pattern)
        {
            if(waterMaterials.TryGetValue(position,out var existing))return existing;
            var cells=new HashSet<GridPosition>();var pending=new Stack<GridPosition>();pending.Push(position);
            int minX=position.X,maxX=position.X,minY=position.Y,maxY=position.Y;
            while(pending.Count>0)
            {
                var p=pending.Pop();
                if(p.X<0||p.Y<0||p.X>=width||p.Y>=height||cells.Contains(p)||!host.CurrentState.Map.GetTile(p).IsWater)continue;
                cells.Add(p);minX=Mathf.Min(minX,p.X);maxX=Mathf.Max(maxX,p.X);minY=Mathf.Min(minY,p.Y);maxY=Mathf.Max(maxY,p.Y);
                pending.Push(new GridPosition(p.X-1,p.Y));pending.Push(new GridPosition(p.X+1,p.Y));
                pending.Push(new GridPosition(p.X,p.Y-1));pending.Push(new GridPosition(p.X,p.Y+1));
            }
            var basin=Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_court_b");
            var material=Hd2DCourtyardWaterMaterial.Create(basin,pattern,
                new Vector4(world.transform.position.x+minX,world.transform.position.z+minY,maxX-minX+1,maxY-minY+1),.12f);
            materials.Add(material);foreach(var p in cells)waterMaterials.Add(p,material);
            return material;
        }
        private void RefreshCrystalLight(GridPosition position)
        {
            var tile=host.CurrentState.Map.GetTile(position);
            bool active=tile.IsAetherCrystal&&tile.Durability>0;
            if(!crystalLights.TryGetValue(position,out var light))
            {
                if(!active)return;
                // Energy is declared by gameplay, never inferred from blue pixels.
                // A world-owned light contributes only to this isolated battlefield.
                var go=new GameObject("蓄能晶簇局部光_"+position,typeof(Light)){layer=Layer};
                go.transform.SetParent(world.transform,false);
                go.transform.localPosition=new Vector3(position.X+.5f,.5f,position.Y+.5f);
                light=go.GetComponent<Light>();light.type=LightType.Point;
                light.color=rendering.CrystalColour;light.intensity=rendering.CrystalIntensity;light.range=rendering.CrystalRange;
                light.shadows=LightShadows.None;light.cullingMask=1<<Layer;
                crystalLights.Add(position,light);
                if(atmosphere!=null)atmosphere.RegisterLocalLight(light);
            }
            light.enabled=active;
        }
        public void OnPointerMove(PointerEventData eventData)
        {
            if(host==null||host.IsInteractionModalOpen){OnPointerExit(eventData);return;}
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,eventData.position,eventData.enterEventCamera,out var local))return;
            var viewport=new Vector2((local.x-rect.rect.xMin)/rect.rect.width,(local.y-rect.rect.yMin)/rect.rect.height);
            if(!TryProjectCell(viewport,out var p)){OnPointerExit(eventData);return;}
            if(hovered.HasValue&&hovered.Value==p)return;
            if(hovered.HasValue)host.SetBattlefieldPreviewPosition(hovered.Value,false);
            hovered=p;host.SetBattlefieldPreviewPosition(p,true);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if(hovered.HasValue&&host!=null)host.SetBattlefieldPreviewPosition(hovered.Value,false);
            hovered=null;
        }
        private void MapStoneBlock(GameObject block)
        {
            var filter=block.GetComponent<MeshFilter>();
            var mesh=Object.Instantiate(filter.sharedMesh);mesh.name="32PPU石材体块";
            var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var p=Vector3.Scale(vertices[i],block.transform.localScale)+block.transform.localPosition;
                var n=normals[i];var u=Mathf.Abs(n.y)>.8f?Vector3.right:Vector3.up;var v=Vector3.Cross(n,u);
                uv[i]=new Vector2(Vector3.Dot(p,u),Vector3.Dot(p,v));
            }
            mesh.uv=uv;var visibility=new Vector2[vertices.Length];for(int i=0;i<visibility.Length;i++)visibility[i]=Vector2.right;mesh.uv2=visibility;mesh.RecalculateTangents();meshes.Add(mesh);filter.sharedMesh=mesh;
        }
        private void RefreshWall(GridPosition p,bool visible)
        {
            if(!walls.TryGetValue(p,out var wall))
            {
                if(!visible)return;
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="立体墙_"+p;go.layer=Layer;go.transform.SetParent(world.transform,false);
                float wallHeight=Hd2DViewProfile.BattlefieldWallHeight;
                go.transform.localPosition=new Vector3(p.X+.5f,wallHeight*.5f,p.Y+.5f);go.transform.localScale=new Vector3(1,wallHeight,1);
                MapStoneBlock(go);
                Destroy(go.GetComponent<Collider>());wall=go.GetComponent<MeshRenderer>();wall.sharedMaterial=stoneMaterial;walls.Add(p,wall);
            }
            wall.gameObject.SetActive(visible);
        }
        private void RefreshRange(GridPosition p,BattlefieldCellPresentation model)
        {
            if(!ranges.TryGetValue(p,out var line))
            {
                var go=new GameObject("地面范围_"+p,typeof(LineRenderer)){layer=Layer};go.transform.SetParent(world.transform,false);
                line=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=4;line.widthMultiplier=.02f;
                line.sharedMaterial=rangeMaterial;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
                line.SetPositions(new[]{new Vector3(p.X+.04f,.04f,p.Y+.04f),new Vector3(p.X+.96f,.04f,p.Y+.04f),new Vector3(p.X+.96f,.04f,p.Y+.96f),new Vector3(p.X+.04f,.04f,p.Y+.96f)});ranges.Add(p,line);
            }
            bool move=model.MoveMarker!=BattlefieldCellMarker.None, attack=model.AttackMarker!=BattlefieldCellMarker.None,skill=model.SkillMarker!=BattlefieldCellMarker.None;
            line.enabled=move||attack||skill||model.IsPlayerEffectCell;
            var color=move?new Color(.42f,.84f,.75f):attack?new Color(1,.56f,.38f):new Color(.87f,.7f,1);
            line.startColor=line.endColor=color;
            tint.SetColor("_BaseColor",color);line.SetPropertyBlock(tint);
        }
        private void RefreshLabel(GridPosition p,BattlefieldCellPresentation model,MeshRenderer body)
        {
            if(!labels.TryGetValue(p,out var label))
            {
                var go=new GameObject("单位信息_"+p,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(transform,false);
                label=go.GetComponent<Text>();label.font=FormalUiKit.Font;label.fontSize=20;label.alignment=TextAnchor.LowerCenter;label.color=new Color(1,.94f,.8f);label.raycastTarget=false;
                var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.05f,.04f,.03f,.95f);outline.effectDistance=new Vector2(1,-1);
                label.rectTransform.sizeDelta=new Vector2(210,78);labels.Add(p,label);
            }
            label.gameObject.SetActive(body!=null&&model.Unit!=null);
            if(body==null||model.Unit==null)return;
            var health=model.Vitals?.Health;
            label.text=model.Unit.DisplayName+"  "+model.Unit.Health+"/"+model.Unit.MaxHealth+
                (health!=null&&health.ForecastLoss>0?" → "+health.Remaining:"")+
                (model.Unit.Shield>0?"  盾"+model.Unit.Shield:"")+
                (model.Intent!=null?"\n"+model.Intent.CompactText:"");
            var top=body.transform.TransformPoint(new Vector3(0,body.GetComponent<MeshFilter>().sharedMesh.bounds.max.y,0));
            var v=cameraView.WorldToViewportPoint(top);var rect=(RectTransform)transform;
            label.rectTransform.anchorMin=label.rectTransform.anchorMax=Vector2.zero;label.rectTransform.pivot=new Vector2(.5f,0);
            label.rectTransform.anchoredPosition=new Vector2(v.x*rect.rect.width,v.y*rect.rect.height+6);
        }
        private void OnEnable(){if(world!=null)world.SetActive(true);}
        private void OnDisable(){OnPointerExit(null);if(world!=null)world.SetActive(false);}
        private void OnDestroy()
        {
            if(cameraView!=null)cameraView.targetTexture=null;
            if(output!=null){if(RenderTexture.active==output)RenderTexture.active=null;output.Release();Destroy(output);}
            if(world!=null)Destroy(world);foreach(var material in materials)Destroy(material);foreach(var mesh in meshes)Destroy(mesh);if(masonryTexture!=null)Destroy(masonryTexture);
            if(profile!=null){foreach(var component in profile.components)Destroy(component);Destroy(profile);}
        }
    }
}


