using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    /// <summary>Scene-owned cinematic. Never accesses battle state or game random.</summary>
    public sealed class SkyFurnaceDirector : MonoBehaviour
    {
        public const float Duration = 16f, Impact = 7.4f, PixelPitch = 1f / 32;
        public Shader EnergyShader, SmokeShader, HeatShader, PixelLayerShader;
        public Material CutoutTemplate;
        public Material VfxLitCardTemplate;
        public Hd2DFireLightRecipe FireLightRecipe;
        public bool EnableEffectLighting=true;
        public bool EnableActorShadow=true;
        private MeshRenderer actorShadowRenderer;
        public bool AutoPlay = true;
        public bool ShowControls = true;
        public float Age { get; private set; }
        public bool Paused { get; private set; }
        public Camera RenderCamera { get; private set; }
        public int BrokenPieces { get; private set; }
        public int OwnedResourceCount => owned.Count;
        public int PlayingAudioCount { get {int count=0;foreach(var voice in voices)if(voice!=null&&voice.isPlaying)count++;return count;} }
        public float EffectLightIntensity => (staffLight!=null?staffLight.intensity:0)+(coreLight!=null?coreLight.intensity:0)+(impactLight!=null?impactLight.intensity:0);
        private const int Layer = 29;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly List<RingTrack> rings = new List<RingTrack>();
        private readonly List<Transform> furnaceFlow = new List<Transform>();
        private readonly List<Cloud> clouds = new List<Cloud>();
        private readonly List<LineRenderer> conduits = new List<LineRenderer>();
        private readonly List<AudioSource> voices = new List<AudioSource>();
        private MaterialPropertyBlock props;
        private Material cold, hot, smoke, masonry, floor, actorMat, fireMaterial, sealMaterial, coreMaterial;
        private Texture2D fireTexture, ashTexture, sealTexture, coreTexture, bandTexture, chamberTexture;
        private Material bandMaterial, chamberMaterial, crownAccentMaterial;
        private Texture2D waveTexture;
        private Transform chamber, castSeal;
        private readonly List<MeshRenderer> furnaceBands=new List<MeshRenderer>();
        private Mesh flameMesh;
        private MeshRenderer flameRenderer;
        private Vector3 fixedCameraPosition;
        private Quaternion fixedCameraRotation;
        private float fixedFieldOfView;
        public bool CameraLocked => Vector3.Distance(RenderCamera.transform.position,fixedCameraPosition)<.00001f && Quaternion.Angle(RenderCamera.transform.rotation,fixedCameraRotation)<.0001f && Mathf.Abs(RenderCamera.fieldOfView-fixedFieldOfView)<.00001f;
        public string PhaseName => Age<1?"起手":Age<3?"地阵通电":Age<5?"天炉成形":Age<7?"炉芯压缩":Age<Impact?"贯穿接触":Age<9?"主爆发":Age<11.5f?"火冠回落":Age<14?"烟尘余波":"退场终态";
        private readonly List<MeshRenderer> seals=new List<MeshRenderer>();
        private readonly List<Transform> embers=new List<Transform>();
        private Texture2D[] actorFrames;
        private MeshRenderer actorRenderer;
        private RenderPipelineAsset previousPipeline;
        private UniversalRenderPipelineAsset localPipeline;
        private bool previousBackground;
        private Transform stage, actor, core, blast, heat;
        private LineRenderer lance, shock;
        private Light sun, fill, staffLight, coreLight, impactLight;
        private Vector3 actorOrigin = new Vector3(-2.65f, .03f, -2.45f);
        private Vector3 centre = new Vector3(0, 0, .3f);
        private float previousAge;
        private bool initialized;
        private GUIStyle heading, caption, button;
        private sealed class RingTrack
        { public Transform root; public MeshRenderer renderer; public float radius, height, start, speed, phase; public int layer; }
        private sealed class Cloud
        { public Transform node; public MeshRenderer renderer; public Vector3 origin, velocity; public float birth, life, radius, seed; public bool fire; }

        private T Own<T>(T value) where T : UnityEngine.Object { owned.Add(value); return value; }
        private GameObject Node(string name, Transform parent = null)
        { var obj = new GameObject(name) { layer = Layer }; obj.transform.SetParent(parent == null ? stage : parent, false); return obj; }
        private void Awake()
        { previousBackground=Application.runInBackground;Application.runInBackground=true;props=new MaterialPropertyBlock();Build(); if (AutoPlay) Replay(); else ResetStage();SkyFurnacePlayerProbe.TryAttach(this); }
        private void Build()
        {
            if (initialized) return;
            stage = new GameObject("天穹熔炉 · 独立演出舞台").transform;
            stage.SetParent(transform, false); stage.gameObject.layer = Layer;
            if (EnergyShader == null || SmokeShader == null || HeatShader == null)
                throw new MissingReferenceException("Sky Furnace authoring shader references missing.");
            cold = Own(new Material(EnergyShader)); cold.SetColor("_Tint", new Color(1.15f, .24f, .025f, 1));
            hot = Own(new Material(EnergyShader)); hot.SetColor("_Tint", new Color(1.8f, .58f, .055f, 1));
            smoke = Own(new Material(SmokeShader));
            fireTexture=RequireTexture("Art/SkyFurnace/fire_crown");ashTexture=RequireTexture("Art/SkyFurnace/smoke_wisp_v2");sealTexture=RequireTexture("Art/SkyFurnace/circuit_seal");waveTexture=RequireTexture("Art/SkyFurnace/fire_wave");
            coreTexture=RequireTexture("Art/SkyFurnace/compressed_core");bandTexture=RequireTexture("Art/SkyFurnace/ring_band");chamberTexture=RequireTexture("Art/SkyFurnace/chamber_fire");
            var pixelShader=PixelLayerShader;if(pixelShader==null)throw new MissingReferenceException("Sky Furnace pixel shader missing");
            smoke=Own(new Material(pixelShader));smoke.SetTexture("_MainTex",ashTexture);smoke.SetFloat("_SoftDepth",.25f);smoke.SetFloat("_ThinScatter",.65f);
            fireMaterial=Own(new Material(pixelShader));fireMaterial.SetTexture("_MainTex",fireTexture);fireMaterial.SetFloat("_Emission",1.6f);
            crownAccentMaterial=Own(new Material(pixelShader));crownAccentMaterial.SetTexture("_MainTex",fireTexture);crownAccentMaterial.SetFloat("_Emission",.55f);
            sealMaterial=Own(new Material(pixelShader));sealMaterial.SetTexture("_MainTex",sealTexture);sealMaterial.SetFloat("_Emission",.55f);
            coreMaterial=Own(new Material(pixelShader));coreMaterial.SetTexture("_MainTex",coreTexture);coreMaterial.SetFloat("_Emission",1.25f);
            bandMaterial=Own(new Material(pixelShader));bandMaterial.SetTexture("_MainTex",Repeat(bandTexture));bandMaterial.SetFloat("_Emission",.45f);
            bandMaterial.SetVector("_UVRect",new Vector4(13/128f,1/32f,101/128f,21/32f));bandMaterial.SetFloat("_RepeatU",1);
            chamberMaterial=Own(new Material(pixelShader));chamberMaterial.SetTexture("_MainTex",chamberTexture);chamberMaterial.SetFloat("_Emission",.7f);
            masonry = WallMaterial();
            floor = Lit("原生32PPU演出石坪", "Art/FormalAcademyIndependentFloors32/academy_block_court_a", Color.white);
            BuildEnvironment(); BuildCamera(); BuildActor(); BuildArrays(); BuildAtmosphere(); BuildAudio();
            initialized = true;
        }
        private Material Lit(string name, string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new MissingReferenceException("URP Lit missing");
            var m = Own(new Material(shader) { name = name });
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) throw new MissingReferenceException(path);
            var tiled=Own(new Texture2D(texture.width,texture.height,texture.format,false));Graphics.CopyTexture(texture,tiled);
            tiled.wrapMode=TextureWrapMode.Repeat;tiled.filterMode=FilterMode.Point;
            m.SetTexture("_BaseMap", tiled); m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .08f);
            return m;
        }
        private static Texture2D RequireTexture(string path)
        {var texture=Resources.Load<Texture2D>(path);if(texture==null)throw new MissingReferenceException(path);return texture;}
        // Face UVs are measured in metres: a native32 texture repeats each metre.
        private Mesh BoxMesh(Vector3 size)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3 h = size*.5f;
            Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach (var normal in normals)
            {
                Vector3 u = Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.Cross(Vector3.up,normal);
                Vector3 v = Vector3.Cross(normal,u);
                float a=Mathf.Abs(u.x)*size.x+Mathf.Abs(u.y)*size.y+Mathf.Abs(u.z)*size.z;
                float b=Mathf.Abs(v.x)*size.x+Mathf.Abs(v.y)*size.y+Mathf.Abs(v.z)*size.z;
                Vector3 c=Vector3.Scale(normal,h); int n=vertices.Count;
                vertices.Add(c-u*a*.5f-v*b*.5f);vertices.Add(c+u*a*.5f-v*b*.5f);
                vertices.Add(c+u*a*.5f+v*b*.5f);vertices.Add(c-u*a*.5f+v*b*.5f);
                uv.Add(Vector2.zero);uv.Add(new Vector2(a,0));uv.Add(new Vector2(a,b));uv.Add(new Vector2(0,b));
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            var mesh=Own(new Mesh{name="32PPU world UV masonry"});mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private Transform Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var obj=Node(name);obj.transform.localPosition=position;
            obj.AddComponent<MeshFilter>().sharedMesh=BoxMesh(size);obj.AddComponent<MeshRenderer>().sharedMaterial=material;return obj.transform;
        }

        private void BuildEnvironment()
        {
            // The seven metre spell field is embedded in an intact courtyard.
            Box("学院庭院承接地面",new Vector3(0,-.28f,1),new Vector3(18,.25f,18),floor);
            Box("7×7完整实训石坪",new Vector3(0,-.125f,0),new Vector3(7,.25f,7),floor);
            Box("永久后墙左",new Vector3(-4.4f,.65f,5.3f),new Vector3(5.5f,1.3f,.5f),masonry);
            Box("永久后墙右",new Vector3(4.4f,.65f,5.3f),new Vector3(5.5f,1.3f,.5f),masonry);
            for(int step=0;step<5;step++)
                Box("学院阶梯 "+step,new Vector3(0,(step+1)*.10f,4.05f+step*.25f),new Vector3(3.3f,.20f,.5f),masonry);
            EnvironmentCard("学院象牙石楼与木门", "Art/HD2DAcademyReview/academy_facade",new Vector3(0,.52f,5.8f));
            for(int side=-1;side<=1;side+=2)
            {
                Box("阶梯门柱 "+side,new Vector3(side*1.75f,1.10f,5.0f),new Vector3(.375f,2.2f,.375f),masonry);
                Box("庭院侧墙 "+side,new Vector3(side*6.5f,.65f,1.7f),new Vector3(.5f,1.3f,7.2f),masonry);
                EnvironmentCard("庭院树木 "+side, "Art/HD2DAcademyReview/academy_tree",new Vector3(side*4.9f,0,3.5f));
                EnvironmentCard("远景树木 "+side, "Art/HD2DAcademyReview/academy_tree",new Vector3(side*6.2f,0,6.3f));
                for(int i=0;i<3;i++)
                {
                    float z=-1.5f+i*2.4f;
                    EnvironmentCard("庭院完整花槽", "Art/FormalAcademyCombat32/academy_light_planter_intact",new Vector3(side*5,0,z));
                    EnvironmentCard("庭院晶灯", "Art/FormalAcademyCombat32/academy_decoy_lantern_active",new Vector3(side*4.4f,0,z));
                    var light=Lamp("庭院固定灯 "+side+"/"+i,LightType.Point,new Color(1,.62f,.28f),.5f,2.7f);light.transform.position=new Vector3(side*4.4f,.85f,z);
                }
            }
            // These remain whole for the complete film; no destruction nodes exist.
            foreach(var position in new[]{new Vector3(-1.85f,.35f,-.5f),new Vector3(1.85f,.35f,-.5f),new Vector3(0,.35f,2.25f)})
                Box("完整石障 · 不参与破坏",position,new Vector3(1.2f,.7f,.32f),masonry);
        }
        private Light Lamp(string name, LightType type, Color color, float intensity, float range)
        {
            var l=Node(name).AddComponent<Light>();l.type=type;l.color=color;l.intensity=intensity;l.range=range;
            l.cullingMask=1<<Layer;return l;
        }

        private void BuildCamera()
        {
            var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(source==null)throw new MissingReferenceException("Sky Furnace requires URP");
            previousPipeline=QualitySettings.renderPipeline;localPipeline=Own(Instantiate(source));
            localPipeline.name="Sky Furnace owned full-resolution candidate";localPipeline.renderScale=1;localPipeline.additionalLightsShadowmapResolution=2048;QualitySettings.renderPipeline=localPipeline;
            var obj=Node("固定45度战场相机");obj.tag="MainCamera";
            RenderCamera=obj.AddComponent<Camera>();obj.AddComponent<AudioListener>();
            RenderCamera.allowHDR=true;RenderCamera.allowMSAA=false;RenderCamera.nearClipPlane=.1f;RenderCamera.farClipPlane=80;
            RenderCamera.cullingMask=1<<Layer;RenderCamera.clearFlags=CameraClearFlags.SolidColor;RenderCamera.backgroundColor=new Color(.17f,.20f,.27f);
            fixedCameraRotation=Quaternion.Euler(45,0,0);fixedCameraPosition=new Vector3(0,.35f,.3f)-fixedCameraRotation*Vector3.forward*13.2f;fixedFieldOfView=34;
            RenderCamera.transform.SetPositionAndRotation(fixedCameraPosition,fixedCameraRotation);RenderCamera.fieldOfView=fixedFieldOfView;
            var data=RenderCamera.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=true;
            data.volumeLayerMask=1<<Layer;data.requiresColorTexture=true;data.requiresDepthTexture=true;
            var profile=Own(ScriptableObject.CreateInstance<VolumeProfile>());profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
            var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1.5f;bloom.intensity.value=.18f;bloom.scatter.value=.35f;
            var volume=Node("演出独立后期").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            sun=Lamp("庭院夕光",LightType.Directional,new Color(1,.79f,.59f),1.25f,100);
            sun.transform.rotation=Quaternion.Euler(42,65,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.025f;sun.shadowNormalBias=.04f;
            fill=Lamp("天空填充",LightType.Directional,new Color(.59f,.72f,1),.42f,100);fill.transform.rotation=Quaternion.Euler(35,-115,0);
            staffLight=Lamp("杖端火矢",LightType.Point,new Color(1,.43f,.10f),0,2.5f);
            coreLight=Lamp("炉腔琥珀受光",LightType.Point,new Color(1,.48f,.12f),0,5.5f);
            coreLight.shadows=LightShadows.Soft;coreLight.shadowBias=.02f;coreLight.shadowNormalBias=.015f;
            var lightData=coreLight.GetUniversalAdditionalLightData();lightData.usePipelineSettings=false;lightData.additionalLightsShadowResolutionTier=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
            impactLight=Lamp("爆发真实照明",LightType.Point,new Color(1,.42f,.10f),0,6);impactLight.shadows=LightShadows.Soft;
        }
        private void BuildActor()
        {
            var texture=RequireTexture("Art/FormalUnits64/pyromancer");
            actorFrames=new[]{texture,RequireTexture("Art/SkyFurnace/pyromancer_cast")};
            var obj=Node("火矢生 · 演出施术者");actor=obj.transform;actor.position=actorOrigin;
            var mesh=Own(Quad());var v=mesh.vertices;
            for(int i=0;i<v.Length;i++)v[i]=new Vector3(v[i].x*texture.width/32f,(v[i].y+.5f)*texture.height/32f-.03125f,0);
            mesh.vertices=v;mesh.RecalculateNormals();mesh.RecalculateBounds();obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();
            actorRenderer=obj.AddComponent<MeshRenderer>();
            if(CutoutTemplate==null)throw new MissingReferenceException("Sky Furnace serialized cutout material missing");
            if(VfxLitCardTemplate==null||FireLightRecipe==null)throw new MissingReferenceException("VFX light recipe / lit card missing");
            actorMat=Own(new Material(VfxLitCardTemplate));
            actorMat.SetTexture("_BaseMap",texture);actorMat.SetFloat("_Cutoff",.5f);actorMat.SetFloat("_Cull",0);
            actorMat.EnableKeyword("_ALPHATEST_ON");actorMat.SetFloat("_ReceiveShadows",0);actorMat.EnableKeyword("_RECEIVE_SHADOWS_OFF");actorRenderer.sharedMaterial=actorMat;
            actorRenderer.shadowCastingMode=ShadowCastingMode.Off;
            var shadow=Node("火矢生直立投影");shadow.transform.position=actorOrigin;shadow.AddComponent<MeshFilter>().sharedMesh=mesh;
            actorShadowRenderer=shadow.AddComponent<MeshRenderer>();actorShadowRenderer.sharedMaterial=actorMat;actorShadowRenderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
            actorShadowRenderer.receiveShadows=false;
        }
        private LineRenderer Line(string name, Material material, int count, float width, Transform parent=null)
        {
            var l=Node(name,parent).AddComponent<LineRenderer>();l.sharedMaterial=material;l.positionCount=count;
            l.useWorldSpace=false;l.widthMultiplier=width;l.numCapVertices=0;l.numCornerVertices=0;
            l.shadowCastingMode=ShadowCastingMode.Off;l.textureMode=LineTextureMode.Tile;return l;
        }
        private RingTrack MakeRing(string name,float radius,float height,float start,float speed,int layer)
        {
            var root=Node(name).transform;root.position=centre+Vector3.up*height;
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            // Interrupted conductors and engineered radial junctions; mesh lives in the actual ring plane.
            Action<Vector3,Vector3,float> strip=(a,b,w)=>
            {
                Vector3 n=Vector3.Cross(Vector3.up,(b-a).normalized)*w*.5f;int k=vertices.Count;
                vertices.AddRange(new[]{a-n,a+n,b+n,b-n});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});
                triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            };
            for(int band=0;band<2;band++)
            {
                float r=radius-band*Mathf.Min(.28f,radius*.15f);
                for(int i=0;i<192;i++)
                {
                    if(band==1&&i%16>11)continue;
                    float a=i*Mathf.PI*2/192,b=(i+1)*Mathf.PI*2/192;
                    strip(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),new Vector3(Mathf.Cos(b)*r,0,Mathf.Sin(b)*r),band==0?.0625f:.03125f);
                }
            }
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI*2/8;Vector3 radial=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Vector3 tangent=new Vector3(-radial.z,0,radial.x);
                strip(radial*radius*.87f,radial*radius*.68f,.0625f);
                Vector3 c=radial*radius*.8f;float s=Mathf.Min(.125f,radius*.1f);
                strip(c+radial*s,c+tangent*s,.0625f);strip(c+tangent*s,c-radial*s,.0625f);
                strip(c-radial*s,c-tangent*s,.0625f);strip(c-tangent*s,c+radial*s,.0625f);
            }
            var mesh=Own(new Mesh{name=name});mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=cold;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            var ring=new RingTrack{root=root,renderer=renderer,radius=radius,height=height,start=start,speed=speed,layer=layer,phase=rings.Count*.41f};rings.Add(ring);return ring;
        }

        private void BuildArrays()
        {
            MakeRing("赤金地阵 · 顺序通电",2.85f,.04f,1,0,0);
            MakeRing("地阵内沿",2.35f,.05f,1.45f,0,0);MakeRing("目标收束口",.55f,.06f,2,0,0);
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI/4;
                var line=Line("地表向心导流 "+i,hot,2,.0625f);line.SetPosition(0,centre+new Vector3(Mathf.Cos(angle)*2.45f,.055f,Mathf.Sin(angle)*2.45f));
                line.SetPosition(1,centre+new Vector3(Mathf.Cos(angle)*.55f,.055f,Mathf.Sin(angle)*.55f));conduits.Add(line);
            }
            var castTrack=MakeRing("火矢生脚下赤金起手阵",.55f,.035f,0,0,0);castSeal=castTrack.root;castSeal.position=new Vector3(actorOrigin.x,.035f,actorOrigin.z);
            for(int i=0;i<3;i++)
            {
                float radius=new[]{1.65f,1.25f,.6f}[i],height=new[]{3.15f,2.20f,1.20f}[i];
                var obj=Node("同轴天炉阵带 "+i);obj.transform.position=centre+Vector3.up*height;
                var mesh=Own(CylinderSheet(radius,21/32f,101/32f));obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=bandMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;furnaceBands.Add(renderer);
            }
            chamber=PixelCard("天炉火流腔",chamberTexture,chamberMaterial,centre+Vector3.up*2.35f);
            core=PixelCard("压缩小炉芯",coreTexture,coreMaterial,centre+Vector3.up*2.35f);
            var flowMesh=Own(Quad());
            for(int i=0;i<12;i++)
            {var obj=Node("炉腔内部环流 "+i);obj.AddComponent<MeshFilter>().sharedMesh=flowMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=hot;obj.transform.localScale=new Vector3(4*PixelPitch,2*PixelPitch,1);furnaceFlow.Add(obj.transform);}
            lance=Line("唯一垂直贯穿火柱",hot,2,.125f);shock=Line("水平压力波",hot,128,.09375f);shock.loop=true;
            blast=Node("连续地面火冠").transform;blast.position=centre;
            fireMaterial.SetTexture("_MainTex",Repeat(waveTexture));fireMaterial.SetFloat("_Emission",.65f);
            fireMaterial.SetVector("_UVRect",new Vector4(16/128f,1/32f,95/128f,22/32f));fireMaterial.SetFloat("_RepeatU",1);
            flameMesh=Own(CylinderSheet(.25f,22/32f,95/32f));
            // Cylinder vertices carry metre-based UVs; radius motion never scales sprite pixels.
            var vertices=flameMesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]+=Vector3.up*(22/64f);flameMesh.vertices=vertices;flameMesh.RecalculateBounds();
            blast.gameObject.AddComponent<MeshFilter>().sharedMesh=flameMesh;flameRenderer=blast.gameObject.AddComponent<MeshRenderer>();flameRenderer.sharedMaterial=fireMaterial;
            heat=Node("局部热浪").transform;heat.gameObject.AddComponent<MeshFilter>().sharedMesh=Own(Quad());heat.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Own(new Material(HeatShader));
        }
        private static Mesh Octahedron()
        {
            var mesh=new Mesh{name="Energy octahedron"};mesh.vertices=new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.forward,Vector3.left,Vector3.back};
            mesh.triangles=new[]{0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};mesh.RecalculateNormals();return mesh;
        }
        private static Mesh Quad()
        {
            var mesh=new Mesh{name="Spatial effect billboard"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();return mesh;
        }

        // No terrain or object destruction is present in this revision.
        private void EvaluateFragments(float t) { BrokenPieces=0; }
        private void AddCloud(string name,Vector3 origin,Vector3 velocity,float birth,float life,float radius,float seed,bool fire)
        {
            var obj=Node(name,fire?blast:stage);obj.AddComponent<MeshFilter>().sharedMesh=Own(Quad());
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=fire?crownAccentMaterial:smoke;renderer.shadowCastingMode=ShadowCastingMode.Off;
            var texture=fire?fireTexture:ashTexture;obj.transform.localScale=new Vector3(texture.width/32f,texture.height/32f,1);
            clouds.Add(new Cloud{node=obj.transform,renderer=renderer,origin=origin,velocity=velocity,birth=birth,life=life,radius=radius,seed=seed,fire=fire});
        }

        private void BuildAtmosphere()
        {
            for(int i=0;i<2;i++)
            {float angle=i*Mathf.PI+.5f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));AddCloud("火冠翻卷峰 "+i,centre+direction*.45f+Vector3.up*1.0f,direction*.55f,Impact+.08f,1.2f,1,i*.631f,true);}
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI*2/16;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                AddCloud("贴地尘浪 "+i,centre+direction*.6f+Vector3.up*.18f,direction*.6f,Impact+.35f+i%4*.04f,5.4f,1,i*.739f,false);
            }
            for(int i=0;i<6;i++)
            {
                float angle=i*2.399963f;var origin=centre+new Vector3(Mathf.Cos(angle)*.2f,.55f+i%3*.18f,Mathf.Sin(angle)*.2f);
                AddCloud("中央上升灰烟 "+i,origin,new Vector3(Mathf.Cos(angle)*.12f,.36f+i%3*.04f,Mathf.Sin(angle)*.12f),Impact+.9f+i*.30f,6.4f,1,i*.921f,false);
            }
            var emberMesh=Own(Octahedron());
            for(int i=0;i<40;i++){var obj=Node("稀疏余烬 "+i);obj.AddComponent<MeshFilter>().sharedMesh=emberMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=hot;obj.transform.localScale=Vector3.one*PixelPitch;embers.Add(obj.transform);}
        }
        private void BuildAudio()
        {
            foreach(string name in new[]{"music_16s","sfx_16s"})
            {
                var clip=Resources.Load<AudioClip>("Audio/OCC/SkyFurnace/"+name);
                if(clip==null)throw new MissingReferenceException("Sky Furnace audio stem missing: "+name);
                var v=Node(name=="music_16s"?"16秒短篇配乐":"分层魔法／空气音轨").AddComponent<AudioSource>();
                v.clip=clip;v.playOnAwake=false;v.loop=false;v.spatialBlend=0;v.volume=.85f;voices.Add(v);
            }
        }
        public void Replay()
        {
            if(!initialized) return;StopAudio();
            Age=0;previousAge=-.01f;Paused=false;Evaluate();StartAudioAt(0);
        }
        public void SetPaused(bool value)
        { if(Paused==value)return;Paused=value;if(value){foreach(var v in voices)v.Pause();}else if(Age<Duration)StartAudioAt(Age); }
        public void Seek(float age)
        { StopAudio();Age=Mathf.Clamp(age,0,Duration);previousAge=Age;Paused=true;Evaluate(); }
        public void Skip(){Seek(Duration);}
        public void ResetStage(){Seek(0);}
        private void StopAudio(){foreach(var v in voices)if(v!=null)v.Stop();}
        private void StartAudioAt(float age)
        { double start=AudioSettings.dspTime+.025;foreach(var v in voices){v.Stop();v.time=Mathf.Clamp(age,0,Mathf.Max(0,v.clip.length-.01f));v.PlayScheduled(start);} }
        private void Update()
        {
            var k=Keyboard.current;
            if(k!=null){if(k.spaceKey.wasPressedThisFrame)Replay();if(k.pKey.wasPressedThisFrame)SetPaused(!Paused);if(k.escapeKey.wasPressedThisFrame)Skip();if(k.rKey.wasPressedThisFrame)ResetStage();}
            if(Paused||Age>=Duration)return;
            previousAge=Age;Age=Mathf.Min(Duration,Age+Time.unscaledDeltaTime);Evaluate();
            if(Age>=Duration)StopAudio();
        }
        private static float Ramp(float t,float start,float end)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,t));

        private void Evaluate()
        {
            if(!initialized)return;float t=Age,after=t-Impact;EvaluateActor(t);
            // Camera is configured once; evaluation never changes its transform or projection.
            float groundFade=1-Ramp(t,9,12),compression=Ramp(t,5,7);
            foreach(var ring in rings)
            {
                float reveal=Ramp(t,ring.start,ring.start+1.2f);ring.root.gameObject.SetActive(reveal>0&&groundFade>0);
                props.Clear();props.SetFloat("_Reveal",reveal);props.SetFloat("_Intensity",groundFade*(.68f-.24f*compression));ring.renderer.SetPropertyBlock(props);
            }
            for(int i=0;i<conduits.Count;i++)conduits[i].enabled=t>1.3f+i*.13f&&t<10.5f;
            float furnaceFade=1-Ramp(t,Impact,8.7f);
            for(int i=0;i<furnaceBands.Count;i++)
            {var renderer=furnaceBands[i];float reveal=Ramp(t,3+i*.22f,3.9f+i*.22f);renderer.gameObject.SetActive(reveal>0&&furnaceFade>0);renderer.transform.rotation=Quaternion.Euler(0,(i%2==0?12:-15)*(Mathf.Min(t,5)+2*compression),0);SetLayer(renderer,reveal*furnaceFade,.55f-.4f*compression,1-.65f*compression);}
            for(int i=0;i<furnaceFlow.Count;i++)
            {
                var flow=furnaceFlow[i];flow.gameObject.SetActive(t>3.6f&&t<7);float angle=i*2.399963f+t*2.4f,radius=(1.05f+i%3*.1f)*(1-compression)+.10f;
                flow.position=centre+new Vector3(Mathf.Cos(angle)*radius,Mathf.Lerp(1.7f+i%4*.25f,2.35f,compression),Mathf.Sin(angle)*radius);flow.rotation=fixedCameraRotation*Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            }
            chamber.gameObject.SetActive(t>3.25f&&t<7.55f);chamber.rotation=fixedCameraRotation;
            SetLayer(chamber.GetComponent<MeshRenderer>(),Ramp(t,3.25f,4.6f)*(1-Ramp(t,5.1f,6.7f))*(1-Ramp(t,7.3f,7.55f)),.75f);
            core.gameObject.SetActive(t>5.05f&&t<7.55f);core.position=centre+Vector3.up*2.35f;core.rotation=fixedCameraRotation;
            SetLayer(core.GetComponent<MeshRenderer>(),Ramp(t,5.05f,6)*(1-Ramp(t,7.4f,7.55f)),1.0f+compression*.5f);
            lance.enabled=t>=7&&t<7.85f;lance.SetPosition(0,centre+Vector3.up*2.35f);
            lance.SetPosition(1,centre+Vector3.up*Mathf.Lerp(2.35f,.07f,Ramp(t,7,Impact)));lance.widthMultiplier=t<Impact?.09375f:.1875f*(1-Ramp(t,Impact,7.85f));
            shock.enabled=after>=0&&after<1.1f;shock.widthMultiplier=.09375f*(1-Ramp(after,.45f,1.1f));
            for(int i=0;i<128;i++){float a=i*Mathf.PI*2/128,r=Mathf.Min(3.25f,.15f+Mathf.Max(0,after)*4);shock.SetPosition(i,centre+new Vector3(Mathf.Cos(a)*r,.045f,Mathf.Sin(a)*r));}
            blast.gameObject.SetActive(after>=0&&t<10.6f);
            if(after>=0&&t<10.6f)
            {
                float radius=Mathf.Lerp(.2f,2.5f,Ramp(after,0,1.15f));var vertices=flameMesh.vertices;var uv=flameMesh.uv;
                for(int i=0;i<vertices.Length;i++){float angle=i/2*Mathf.PI*2/128;vertices[i].x=Mathf.Cos(angle)*radius;vertices[i].z=Mathf.Sin(angle)*radius;uv[i].x=angle*radius/(95/32f);}
                flameMesh.vertices=vertices;flameMesh.uv=uv;flameMesh.RecalculateBounds();SetLayer(flameRenderer,Ramp(after,0,.12f)*(1-Ramp(t,8.65f,10.6f)),.85f);
            }
            FireLightRecipe.Evaluate(t,Impact,coreLight,sun,centre,1.25f);impactLight.intensity=0;
            if(!EnableEffectLighting){coreLight.intensity=0;staffLight.intensity=0;}
            actorShadowRenderer.enabled=EnableActorShadow;
            EvaluateFragments(t);EvaluateClouds(t);
            for(int i=0;i<embers.Count;i++)
            {
                float a=after-i%5*.05f;bool visible=a>=0&&a<4.5f+i%3;embers[i].gameObject.SetActive(visible);if(!visible)continue;
                float angle=i*2.399963f,radius=Mathf.Min(a*.65f,2.55f);embers[i].position=centre+new Vector3(Mathf.Cos(angle)*radius,Mathf.Max(.07f,.5f+a*(.7f+i%4*.07f)-a*a*.16f),Mathf.Sin(angle)*radius);
            }
            float heatAge=t-7.6f;heat.gameObject.SetActive(heatAge>=0&&heatAge<3.5f);heat.position=centre+Vector3.up*1.15f;heat.rotation=fixedCameraRotation;heat.localScale=new Vector3(5.8f,2.6f,1);
            props.Clear();props.SetFloat("_Age",t);props.SetFloat("_Strength",heatAge>=0?.0035f*(1-Ramp(heatAge,.4f,3.5f)):0);heat.GetComponent<MeshRenderer>().SetPropertyBlock(props);
        }

        private void EvaluateActor(float t)
        {
            actor.position=actorOrigin;actor.rotation=fixedCameraRotation;actorMat.SetTexture("_BaseMap",actorFrames[t>=.55f&&t<8.6f?1:0]);
            staffLight.transform.position=actorOrigin+Vector3.up*1.4f+Vector3.right*.3f;staffLight.intensity=1.15f*Ramp(t,0,.7f)*(1-Ramp(t,7,Impact));
        }

        private void EvaluateCamera(float t) { /* Permanently locked; no shot animation. */ }

        private void EvaluateClouds(float t)
        {
            foreach(var c in clouds)
            {
                float a=t-c.birth,u=a/c.life;bool active=a>=0&&u<1;c.node.gameObject.SetActive(active);if(!active)continue;
                c.node.position=c.origin+c.velocity*(c.fire?a:Mathf.Log(1+a)*2.3f);
                c.node.rotation=RenderCamera.transform.rotation*Quaternion.Euler(0,0,c.fire?Mathf.Sin(c.seed)*18:Mathf.Sin(c.seed)*10);
                float alpha=Ramp(u,0,.08f)*(1-Ramp(u,c.fire?.25f:.55f,1));
                props.Clear();props.SetFloat("_Seed",c.seed);props.SetFloat("_Age",a);
                props.SetFloat("_Opacity",alpha*(c.fire?.75f:.36f));
                c.renderer.SetPropertyBlock(props);
            }
        }

        private Texture2D Repeat(Texture2D source)
        {var copy=Own(new Texture2D(source.width,source.height,source.format,false));Graphics.CopyTexture(source,copy);copy.wrapMode=TextureWrapMode.Repeat;copy.filterMode=FilterMode.Point;return copy;}
        private Material WallMaterial()
        {
            var source=RequireTexture("Art/AcademyPermanentWallCandidates20260926/academy_permanent_wall_nesw_a");
            var region=Own(new Texture2D(32,21,source.format,false));Graphics.CopyTexture(source,0,0,0,4,32,21,region,0,0,0,0);region.filterMode=FilterMode.Point;region.wrapMode=TextureWrapMode.Repeat;
            var material=Own(new Material(Shader.Find("Universal Render Pipeline/Lit")));material.SetTexture("_BaseMap",region);material.SetTextureScale("_BaseMap",new Vector2(1,32/21f));material.SetFloat("_Smoothness",.04f);return material;
        }
        private void SetLayer(MeshRenderer renderer,float opacity,float emission,float shade=1)
        {props.Clear();props.SetFloat("_Opacity",Mathf.Clamp01(opacity));props.SetFloat("_Emission",emission);props.SetFloat("_ShadeLevel",shade);renderer.SetPropertyBlock(props);}
        private Transform PixelCard(string name,Texture2D texture,Material material,Vector3 position)
        {
            var obj=Node(name);obj.transform.position=position;
            var region=texture==chamberTexture?new Rect(3,1,90,64):new Rect(9,1,13,13);
            material.SetVector("_UVRect",new Vector4(region.x/texture.width,region.y/texture.height,region.width/texture.width,region.height/texture.height));
            obj.transform.localScale=new Vector3(region.width/32f,region.height/32f,1);obj.AddComponent<MeshFilter>().sharedMesh=Own(Quad());
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;return obj.transform;
        }
        private void EnvironmentCard(string name,string source,Vector3 foot)
        {
            if(CutoutTemplate==null)throw new MissingReferenceException("Sky Furnace serialized cutout material missing");
            var texture=RequireTexture(source);var material=Own(new Material(CutoutTemplate));material.SetTexture("_BaseMap",texture);
            var obj=Node(name);obj.transform.position=foot;obj.transform.rotation=Quaternion.Euler(45,0,0);
            var mesh=Own(Quad());var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(vertices[i].x*texture.width/32f,(vertices[i].y+.5f)*texture.height/32f,0);mesh.vertices=vertices;mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        private static Mesh CylinderSheet(float radius,float height,float textureWidthMetres)
        {
            var vertices=new Vector3[258];var uv=new Vector2[258];var triangles=new int[768];
            for(int i=0;i<=128;i++)
            {
                float angle=i*Mathf.PI*2/128;for(int row=0;row<2;row++){int k=i*2+row;vertices[k]=new Vector3(Mathf.Cos(angle)*radius,(row-.5f)*height,Mathf.Sin(angle)*radius);uv[k]=new Vector2(angle*radius/textureWidthMetres,row);}
                if(i==128)continue;int t=i*6,n=i*2;triangles[t]=n;triangles[t+1]=n+1;triangles[t+2]=n+3;triangles[t+3]=n;triangles[t+4]=n+3;triangles[t+5]=n+2;
            }
            var mesh=new Mesh{name="Native32 metre UV curved spell sheet"};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        private void OnGUI()
        {
            if(!ShowControls)return;
            if(heading==null){heading=new GUIStyle(GUI.skin.label){font=FormalUiKit.Font,fontSize=28,alignment=TextAnchor.UpperCenter};heading.normal.textColor=new Color(.92f,.82f,.6f);caption=new GUIStyle(heading){fontSize=17};caption.normal.textColor=new Color(.75f,.84f,.92f);button=new GUIStyle(GUI.skin.button){font=FormalUiKit.Font,fontSize=18};}
            GUI.Label(new Rect(0,20,Screen.width,40),"天 穹 熔 炉",heading);
            GUI.Label(new Rect(0,Screen.height-76,Screen.width,28),"空格 重播　 P 暂停／继续　 Esc 跳过　 R 复位",caption);
            GUI.Label(new Rect(0,Screen.height-46,Screen.width,25),$"{Age:00.00} / 16.00 秒　{(Paused?"暂停":Age>=Duration?"结束":PhaseName)}",caption);
            float left=(Screen.width-480)*.5f;
            if(GUI.Button(new Rect(left,Screen.height-120,112,32),"重播",button))Replay();
            if(GUI.Button(new Rect(left+122,Screen.height-120,112,32),Paused?"继续":"暂停",button))SetPaused(!Paused);
            if(GUI.Button(new Rect(left+244,Screen.height-120,112,32),"跳过",button))Skip();
            if(GUI.Button(new Rect(left+366,Screen.height-120,112,32),"复位",button))ResetStage();
        }
        private void OnEnable(){if(initialized&&stage!=null&&!stage.gameObject.activeSelf){Application.runInBackground=true;if(localPipeline!=null)QualitySettings.renderPipeline=localPipeline;stage.gameObject.SetActive(true);ResetStage();}}
        private void OnDisable(){StopAudio();if(staffLight!=null)staffLight.intensity=0;if(coreLight!=null)coreLight.intensity=0;if(impactLight!=null)impactLight.intensity=0;if(stage!=null)stage.gameObject.SetActive(false);if(localPipeline!=null&&QualitySettings.renderPipeline==localPipeline)QualitySettings.renderPipeline=previousPipeline;Application.runInBackground=previousBackground;}
        private void OnDestroy(){StopAudio();foreach(var resource in owned)if(resource!=null)Destroy(resource);owned.Clear();}
    }
}
