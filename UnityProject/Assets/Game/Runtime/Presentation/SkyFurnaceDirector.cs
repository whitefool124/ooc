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
        public Shader VoxelSurfaceShader;
        private readonly Dictionary<Texture2D,Mesh> cardMeshes=new Dictionary<Texture2D,Mesh>();
        private readonly List<PrimitiveRecord> effectPrimitives=new List<PrimitiveRecord>();
        private readonly HashSet<MeshRenderer> sceneRenderers=new HashSet<MeshRenderer>();
        private readonly List<MeshRenderer> columns=new List<MeshRenderer>();
        private Material columnMaterial;
        [Serializable] private sealed class PrimitiveRecord {public string kind;public MeshRenderer renderer;public Texture2D texture;public int padding;}
        private Mesh Card(Texture2D texture)
        {if(!cardMeshes.TryGetValue(texture,out var mesh)){mesh=Own(PixelEffectCard.Build(texture,PixelPitch));cardMeshes.Add(texture,mesh);}return mesh;}
        private void RegisterPrimitive(string kind,MeshRenderer renderer,Texture2D texture=null,int padding=0)
        {effectPrimitives.Add(new PrimitiveRecord{kind=kind,renderer=renderer,texture=texture,padding=padding});}
        [Serializable] public sealed class PrimitiveAuditItem
        {public string name,kind,texture;public float[] worldDimensions;public float pixelPitch;public bool valid;public int particles;}
        public int SmokeParticleCount=>smokeParticles!=null?smokeParticles.Count:0;
        public int PeakSmokeParticles=>smokeParticles!=null?smokeParticles.PeakParticles:0;
        public int SmokeFramesSeen=>smokeParticles!=null?smokeParticles.FramesSeenMask:0;
        public float SmokeFootprintDiameter=>smokeParticles!=null?smokeParticles.PeakRadius*2:0;
        public float SmokeTopHeight=>smokeParticles!=null?smokeParticles.PeakHeight:0;
        public int SmokeStateFingerprint=>smokeParticles!=null?smokeParticles.Fingerprint():0;
        public bool PixelEmissionValid
        {get{foreach(var ring in rings)if(ring.ignition.PixelCount==0||ring.ignition.LitCount(1)!=ring.ignition.PixelCount||ring.ignition.LitCount(.25f)>=ring.ignition.LitCount(.75f)||ring.ignition.Texture.filterMode!=FilterMode.Point)return false;return rings.Count==4;}}
        public object IgnitionAudit()
        {
            var items=new List<object>();foreach(var ring in rings)items.Add(new{name=ring.root.name,nativeTexels=ring.ignition.PixelCount,connectedInkComponents=ring.ignition.Components,colonies=ring.ignition.ColonyCount,maximumBranches=ring.ignition.MaximumBranches,detachedBirths=ring.ignition.DetachedBirths,pattern=ring.ignition.Pattern,currentProgress=ring.litProgress,currentLitTexels=ring.ignition.LitCount(ring.litProgress),quarterLit=ring.ignition.LitCount(.25f),halfLit=ring.ignition.LitCount(.5f),allLit=ring.ignition.LitCount(1),worldHeight=ring.root.position.y,planeAngles=new[]{ring.root.eulerAngles.x,ring.root.eulerAngles.y,ring.root.eulerAngles.z},mapSampling=ring.ignition.Texture.filterMode.ToString()});
            return new{schema="occ-native-texel-ignition-v1",route="Two initial roots per main seal; attached buds branch and connect;24pooled inward fire pixels",absorbingActive=ActiveAbsorbingPixels,maximumAbsorptionTravel=AbsorptionMaximumTravel,absorbingPixelMetres=PixelPitch,items};
        }
        [Serializable] public sealed class PrimitiveAuditReport
        {
            public string schema="occ-effect-three-primitives-v1",humanAestheticReview="PENDING";
            public float worldPitch;public int skyFurnaceModels,imageCards,columns,cubeParticles,unclassifiedEffectRenderers,smokeParticleSystems,activeSmokeParticles,peakSmokeParticles;public bool technicalPass,auxiliaryTextureFree;public float smokeFootprintDiameter,smokeTopHeight,smokeCanvasMetres,smokePixelMetres;public int smokeFramesSeenMask,smokeAnimationFrames;public List<PrimitiveAuditItem> items;
        }
        public PrimitiveAuditReport PrimitiveAudit()
        {
            var items=new List<PrimitiveAuditItem>();bool pass=true;int images=0,pillars=0,cubes=0,sky=0;
            foreach(var tr in GetComponentsInChildren<Transform>(true))if(tr.name.StartsWith("体素天炉"))sky++;
            foreach(var item in effectPrimitives)
            {
                var r=item.renderer;var mesh=r.GetComponent<MeshFilter>().sharedMesh;var size=mesh.bounds.size;
                float x=r.transform.TransformVector(Vector3.right*size.x).magnitude,y=r.transform.TransformVector(Vector3.up*size.y).magnitude,z=r.transform.TransformVector(Vector3.forward*size.z).magnitude;
                bool valid;
                if(item.kind=="one_pixel_thick_image"){images++;valid=item.texture!=null&&Mathf.Abs(z-PixelPitch)<.00001f&&Mathf.Abs(x-(item.texture.width+item.padding*2)*PixelPitch)<.00001f&&Mathf.Abs(y-(item.texture.height+item.padding*2)*PixelPitch)<.00001f;}
                else if(item.kind=="one_pixel_column"){pillars++;valid=Mathf.Abs(x-PixelPitch)<.00001f&&Mathf.Abs(z-PixelPitch)<.00001f;}
                else if(item.kind=="one_pixel_cube"){cubes++;valid=Mathf.Abs(x-PixelPitch)<.00001f&&Mathf.Abs(y-PixelPitch)<.00001f&&Mathf.Abs(z-PixelPitch)<.00001f;}
                else valid=false;
                pass&=valid;items.Add(new PrimitiveAuditItem{name=r.name,kind=item.kind,worldDimensions=new[]{x,y,z},texture=item.texture!=null?item.texture.name:null,valid=valid});
            }
            var native=new List<ParticleSystem>();if(smokeParticles!=null)native.AddRange(smokeParticles.Systems);if(burstParticles!=null)native.AddRange(burstParticles.Systems);if(disassembly!=null)native.AddRange(disassembly.Systems);if(rain!=null)native.AddRange(rain.Systems);
            foreach(var system in native)
            {
                var renderer=system.GetComponent<ParticleSystemRenderer>();var dimensions=renderer.mesh.bounds.size;
                float pitch=rain!=null&&system==rain.Systems[0]?SkyFurnaceRainParticles.DropPitch:PixelPitch;
                bool cube=(dimensions-Vector3.one*pitch).sqrMagnitude<.00000001f;bool column=Mathf.Abs(dimensions.x-pitch)<.000001f&&Mathf.Abs(dimensions.z-pitch)<.000001f&&dimensions.y>=pitch;
                bool valid=(cube||column)&&system.main.startSize.constant==1&&!system.sizeOverLifetime.enabled&&!system.sizeBySpeed.enabled&&renderer.sharedMaterial.GetTexturePropertyNames().Length==0;
                pass&=valid;if(cube)cubes+=system.particleCount;else pillars+=system.particleCount;items.Add(new PrimitiveAuditItem{name=system.name,pixelPitch=pitch,kind=cube?"one_pixel_cube":"one_pixel_column",worldDimensions=new[]{dimensions.x,dimensions.y,dimensions.z},texture=null,valid=valid,particles=system.particleCount});
            }
            int unknown=0;
            foreach(var renderer in stage.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer is MeshRenderer mr&&sceneRenderers.Contains(mr))continue;bool found=false;
                foreach(var item in effectPrimitives)if(item.renderer==renderer){found=true;break;}
                if(smokeParticles!=null){foreach(var particleRenderer in smokeParticles.Renderers)if(particleRenderer==renderer)found=true;foreach(var image in smokeParticles.ImageRenderers)if(image==renderer)found=true;}
                if(burstParticles!=null)foreach(var particleRenderer in burstParticles.Renderers)if(particleRenderer==renderer)found=true;
                if(disassembly!=null)foreach(var particleRenderer in disassembly.Renderers)if(particleRenderer==renderer)found=true;
                if(rain!=null)foreach(var particleRenderer in rain.Renderers)if(particleRenderer==renderer)found=true;
                if(!found)unknown++;
            }
            return new PrimitiveAuditReport{worldPitch=PixelPitch,smokeFootprintDiameter=SmokeFootprintDiameter,smokeTopHeight=SmokeTopHeight,smokeCanvasMetres=SkyFurnaceSmokeParticles.CanvasMetres,smokePixelMetres=SkyFurnaceSmokeParticles.PixelMetres,smokeFramesSeenMask=SmokeFramesSeen,smokeAnimationFrames=0,skyFurnaceModels=sky,imageCards=images,columns=pillars,cubeParticles=cubes,unclassifiedEffectRenderers=unknown,smokeParticleSystems=smokeParticles!=null?2:0,activeSmokeParticles=SmokeParticleCount,peakSmokeParticles=PeakSmokeParticles,auxiliaryTextureFree=AuxiliaryTextureFree,technicalPass=pass&&sky==0&&unknown==0&&AuxiliaryTextureFree,items=items};
        }
        public int BurstParticleCount=>burstParticles!=null?burstParticles.Count:0;
        public int BurstStateFingerprint=>burstParticles!=null?burstParticles.Fingerprint():0;
        public bool SmokeUsesTextures=>smokeParticles==null||!smokeParticles.TextureFree;
        public bool AuxiliaryTextureFree
        {
            get
            {
                foreach(var item in effectPrimitives)if(item.kind!="one_pixel_thick_image"&&item.renderer.sharedMaterial.GetTexturePropertyNames().Length>0)return false;
                return smokeParticles!=null&&smokeParticles.TextureFree&&burstParticles!=null&&burstParticles.TextureFree&&disassembly!=null&&disassembly.TextureFree&&rain!=null&&rain.TextureFree;
            }
        }
        public object BurstAudit()
        {
            int cores=0,rays=0;foreach(var r in burstCore)if(r.gameObject.activeInHierarchy)cores++;foreach(var r in burstRays)if(r.gameObject.activeInHierarchy)rays++;
            return new{disassemblySourcePixels=disassembly.SourcePixels,disassemblyPeakSpan=new[]{disassembly.PeakSpan.x,disassembly.PeakSpan.y,disassembly.PeakSpan.z},disassemblyPixels=disassembly.Count,peakDisassemblyPixels=disassembly.Peak,maximumFragmentTravel=disassembly.MaximumTravel,fragmentReadback=disassembly.Audit(),age=Age,coreCards=cores,radialColumns=rays,shockAndSparkParticles=burstParticles.Count,peakBurstParticles=burstParticles.PeakParticles,burstDiameter=burstParticles.PeakRadius*2,burstTop=burstParticles.PeakHeight,
                smokeParticles=SmokeParticleCount,peakSmokeParticles=PeakSmokeParticles,smokeDiameter=SmokeFootprintDiameter,smokeTop=SmokeTopHeight,auxiliaryTextureFree=AuxiliaryTextureFree,cubeEdgeMetres=PixelPitch,coreCanvasMetres=3,corePaperThickness=PixelPitch};
        }
        public Material VfxLitCardTemplate;
        public Hd2DFireLightRecipe FireLightRecipe;
        public bool EnableEffectLighting=true;
        public bool EnableActorShadow=true;
        public bool NightRain;
        public bool EnhancedPixelGlow=true;
        private SkyFurnaceRainParticles rain;
        private Bloom localBloom;
        private Material fragmentMaterial;
        private readonly List<GlowTrack> contourGlows=new List<GlowTrack>();
        private readonly Dictionary<Material,Color> environmentColours=new Dictionary<Material,Color>();
        private sealed class GlowTrack {public MeshRenderer source,halo;}
        public void SetNightRain(bool value){NightRain=value;if(initialized)Evaluate();}
        public void SetEnhancedPixelGlow(bool value){EnhancedPixelGlow=value;if(initialized)Evaluate();}
        public object OpticsAudit()
        {
            var layers=new List<object>();foreach(var track in contourGlows){var block=new MaterialPropertyBlock();track.source.GetPropertyBlock(block);layers.Add(new{name=track.source.name,sourceVisible=track.source.gameObject.activeInHierarchy,haloVisible=track.halo.gameObject.activeInHierarchy,pixelEmission=block.GetFloat("_Emission"),rimEmission=block.GetFloat("_EdgeEnergy")});}
            return new{nightRain=NightRain,enhancedGlow=EnhancedPixelGlow,haloModel="continuous_gaussian",haloSigmaPixels=1.4f,haloPaddingPixels=6,haloEnergy=.16f,pixelEmissionGain=EnhancedPixelGlow?1.05f:1,hardRimEnergy=0,bloomThreshold=localBloom.threshold.value,bloomIntensity=localBloom.intensity.value,bloomScatter=localBloom.scatter.value,sunIntensity=sun.intensity,fillIntensity=fill.intensity,rainParticles=rain.Count,wetFloorSmoothness=floor.GetFloat("_Smoothness"),fragmentMaterialEnergy=fragmentMaterial.GetFloat("_Energy"),layers};
        }
        private MeshRenderer actorShadowRenderer;
        public bool AutoPlay = true;
        public bool ShowControls = true;
        public float Age { get; private set; }
        public bool Paused { get; private set; }
        public Camera RenderCamera { get; private set; }
        public int BrokenPieces { get; private set; }
        public int OwnedResourceCount => owned.Count;
        public int PlayingAudioCount { get {int count=0;foreach(var voice in voices)if(voice!=null&&voice.isPlaying)count++;return count;} }
        public float EffectLightIntensity => (staffLight!=null?staffLight.intensity:0)+(coreLight!=null?coreLight.intensity:0)+(impactLight!=null?impactLight.intensity:0)+(ignitionLights[0]!=null?ignitionLights[0].intensity:0)+(ignitionLights[1]!=null?ignitionLights[1].intensity:0);
        private const int Layer = 29;
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private readonly List<RingTrack> rings = new List<RingTrack>();
        private SkyFurnaceSmokeParticles smokeParticles;
        private SkyFurnaceBurstParticles burstParticles;
        private SkyFurnacePixelDisassembly disassembly;
        public int DisassemblyCount=>disassembly.Count;
        public int RainParticleCount=>rain!=null?rain.Count:0;
        public int RainFingerprint=>rain!=null?rain.Fingerprint():0;
        public int DisassemblyFingerprint=>disassembly.Fingerprint();
        private readonly List<MeshRenderer> burstCore=new List<MeshRenderer>();
        private readonly List<MeshRenderer> burstRays=new List<MeshRenderer>();
        private Mesh effectCubeMesh;
        private readonly Light[] ignitionLights=new Light[2];
        private readonly List<AudioSource> voices = new List<AudioSource>();
        private MaterialPropertyBlock props;
        private Material cold, hot, smoke, masonry, floor, actorMat;
        private Transform castSeal;
        private Vector3 fixedCameraPosition;
        private Quaternion fixedCameraRotation;
        private float fixedFieldOfView;
        public bool CameraLocked => Vector3.Distance(RenderCamera.transform.position,fixedCameraPosition)<.00001f && Quaternion.Angle(RenderCamera.transform.rotation,fixedCameraRotation)<.0001f && Mathf.Abs(RenderCamera.fieldOfView-fixedFieldOfView)<.00001f;
        public string PhaseName => Age<1?"起手":Age<3?"地阵通电":Age<5?"法阵叠合":Age<7?"符印蓄能":Age<Impact?"贯穿接触":Age<9?"主爆发":Age<11.5f?"余辉回落":Age<14?"烟尘余波":"退场终态";
        private readonly List<Transform> embers=new List<Transform>();
        private readonly List<MeshRenderer> absorbingPixels=new List<MeshRenderer>();
        public int ActiveAbsorbingPixels {get{int count=0;foreach(var pixel in absorbingPixels)if(pixel.gameObject.activeInHierarchy)count++;return count;}}
        public float AbsorptionMaximumTravel {get;private set;}
        private Texture2D[] actorFrames;
        private MeshRenderer actorRenderer;
        private RenderPipelineAsset previousPipeline;
        private UniversalRenderPipelineAsset localPipeline;
        private bool previousBackground;
        private Transform stage, actor;
        private Light sun, fill, staffLight, coreLight, impactLight;
        private Vector3 actorOrigin = new Vector3(-2.65f, .03f, -2.45f);
        private Vector3 centre = new Vector3(0, 0, .3f);
        private float previousAge;
        private bool initialized;
        private GUIStyle heading, caption, button;
        private sealed class RingTrack
        { public Transform root; public MeshRenderer renderer; public float radius, height, start, speed, phase; public int layer; public Vector3 origin; public PixelIgnitionMap ignition; public float litProgress; }
        private T Own<T>(T value) where T : UnityEngine.Object { owned.Add(value); return value; }
        private GameObject Node(string name, Transform parent = null)
        { var obj = new GameObject(name) { layer = Layer }; obj.transform.SetParent(parent == null ? stage : parent, false); return obj; }
        private void Awake()
        { previousBackground=Application.runInBackground;Application.runInBackground=true;props=new MaterialPropertyBlock();foreach(string argument in Environment.GetCommandLineArgs())if(argument=="-occ-night-rain")NightRain=true;Build(); if (AutoPlay) Replay(); else ResetStage();SkyFurnacePlayerProbe.TryAttach(this); }
        private void Build()
        {
            if (initialized) return;
            stage = new GameObject("天穹熔炉 · 独立演出舞台").transform;
            stage.SetParent(transform, false); stage.gameObject.layer = Layer;
            if (EnergyShader == null || SmokeShader == null || HeatShader == null)
                throw new MissingReferenceException("Sky Furnace authoring shader references missing.");
            cold = Own(new Material(EnergyShader)); cold.SetColor("_Tint", new Color(2.0f, 1.0f, .18f, 1));
            hot = Own(new Material(EnergyShader)); hot.SetColor("_Tint", new Color(1.8f, .58f, .055f, 1));
            var pixelShader=PixelLayerShader;if(pixelShader==null)throw new MissingReferenceException("Sky Furnace pixel shader missing");
            var solidShader=Resources.Load<Shader>("Shaders/SkyFurnaceSolidPixel");if(solidShader==null)throw new MissingReferenceException("Solid pixel shader missing");
            smoke=Own(new Material(solidShader));smoke.SetColor("_Tint",Color.white);smoke.SetFloat("_ZWrite",0);smoke.SetFloat("_Energy",.08f);smoke.SetFloat("_GroundHeight",.015f);
            masonry = WallMaterial();
            floor = Lit("原生32PPU演出石坪", "Art/FormalAcademyIndependentFloors32/academy_block_court_a", Color.white);
            BuildEnvironment(); BuildCamera(); BuildActor();
            foreach(var renderer in stage.GetComponentsInChildren<MeshRenderer>(true))sceneRenderers.Add(renderer);
            BuildArrays(); BuildAtmosphere();BuildBurst();BuildDisassembly();BuildOptics();
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
            foreach(var position in new[]{new Vector3(-3.6f,.35f,-.5f),new Vector3(3.6f,.35f,-.5f),new Vector3(0,.35f,3.7f)})
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
            RenderCamera=obj.AddComponent<Camera>();
            RenderCamera.allowHDR=true;RenderCamera.allowMSAA=false;RenderCamera.nearClipPlane=.1f;RenderCamera.farClipPlane=80;
            RenderCamera.cullingMask=1<<Layer;RenderCamera.clearFlags=CameraClearFlags.SolidColor;RenderCamera.backgroundColor=new Color(.17f,.20f,.27f);
            fixedCameraRotation=Quaternion.Euler(45,0,0);fixedCameraPosition=new Vector3(0,.35f,.3f)-fixedCameraRotation*Vector3.forward*13.2f;fixedFieldOfView=34;
            RenderCamera.transform.SetPositionAndRotation(fixedCameraPosition,fixedCameraRotation);RenderCamera.fieldOfView=fixedFieldOfView;
            var data=RenderCamera.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=true;
            data.volumeLayerMask=1<<Layer;data.requiresColorTexture=true;data.requiresDepthTexture=true;
            var profile=Own(ScriptableObject.CreateInstance<VolumeProfile>());profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
            var bloom=profile.Add<Bloom>(true);localBloom=bloom;bloom.threshold.value=1.15f;bloom.intensity.value=.30f;bloom.scatter.value=.60f;
            var volume=Node("演出独立后期").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            sun=Lamp("庭院夕光",LightType.Directional,new Color(1,.79f,.59f),1.25f,100);
            sun.transform.rotation=Quaternion.Euler(42,65,0);sun.shadows=LightShadows.Soft;sun.shadowBias=.025f;sun.shadowNormalBias=.04f;
            fill=Lamp("天空填充",LightType.Directional,new Color(.59f,.72f,1),.42f,100);fill.transform.rotation=Quaternion.Euler(35,-115,0);
            staffLight=Lamp("杖端火矢",LightType.Point,new Color(1,.43f,.10f),0,2.5f);
            coreLight=Lamp("炉腔琥珀受光",LightType.Point,new Color(1,.48f,.12f),0,5.5f);
            coreLight.shadows=LightShadows.Soft;coreLight.shadowBias=.02f;coreLight.shadowNormalBias=.015f;
            var lightData=coreLight.GetUniversalAdditionalLightData();lightData.usePipelineSettings=false;lightData.additionalLightsShadowResolutionTier=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
            impactLight=Lamp("爆发真实照明",LightType.Point,new Color(1,.42f,.10f),0,6);impactLight.shadows=LightShadows.None;
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
        private RingTrack MakeGeneratedSeal(string name,Texture2D texture,float height,float start,float speed,float yaw,Vector3 position)
        {
            var root=Node(name).transform;root.position=position+Vector3.up*height;
            root.rotation=Quaternion.Euler(90,yaw,0);root.localScale=new Vector3(texture.width/32f,texture.height/32f,1);
            var mesh=Card(texture);root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var ignition=new PixelIgnitionMap(texture,rings.Count<3?rings.Count:-1);Own(ignition.Texture);var material=Own(new Material(PixelLayerShader));material.SetTexture("_ChargeMap",ignition.Texture);material.SetTexture("_MainTex",texture);material.SetFloat("_ShadeLevel",.72f);material.SetFloat("_Emission",.7f);material.SetFloat("_SealMode",1);
            var renderer=root.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.sortingOrder=rings.Count;RegisterPrimitive("one_pixel_thick_image",renderer,texture);
            var track=new RingTrack{root=root,renderer=renderer,height=height,start=start,speed=speed,phase=yaw,origin=position,ignition=ignition};rings.Add(track);return track;
        }
        private void BuildArrays()
        {
            var outer=RequireTexture("Art/SkyFurnace/magic_card_outer");var sigils=RequireTexture("Art/SkyFurnace/magic_card_sigils");var core=RequireTexture("Art/SkyFurnace/magic_card_core");
            MakeGeneratedSeal("参考稿裁切 · 完整外环",outer,PixelPitch,1,-2,0,centre);
            MakeGeneratedSeal("参考稿裁切 · 独立六符印",sigils,PixelPitch*16,1.5f,4,0,centre);
            MakeGeneratedSeal("参考稿裁切 · 中心交织术式",core,PixelPitch*32,2.1f,-3,0,centre);
            var castTrack=MakeGeneratedSeal("参考稿裁切 · 施法中心术印",RequireTexture("Art/SkyFurnace/magic_card_caster"),PixelPitch,0,0,0,actorOrigin);castSeal=castTrack.root;
            BuildColumns();
            for(int i=0;i<2;i++){ignitionLights[i]=Lamp("纹素通电前沿光 "+i,LightType.Point,new Color(1,.36f,.055f),0,2.1f);ignitionLights[i].shadows=LightShadows.None;}
        }
        private static Mesh Quad()
        {
            var mesh=new Mesh{name="Scene card"};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();return mesh;
        }
        private void BuildColumns()
        {
            columnMaterial=Own(new Material(Resources.Load<Shader>("Shaders/SkyFurnaceSolidPixel")));columnMaterial.SetColor("_Tint",new Color(1,.55f,.10f,1));columnMaterial.SetFloat("_LightResponse",.25f);columnMaterial.SetFloat("_Energy",2.1f);columnMaterial.SetFloat("_ZWrite",1);columnMaterial.renderQueue=2000;
            // Actual cross section is one pixel. Native world UVs never stretch with height.
            var mesh=BoxMesh(new Vector3(PixelPitch,3,PixelPitch));var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=Vector3.up*1.5f;mesh.vertices=v;mesh.RecalculateBounds();
            for(int i=0;i<5;i++)
            {
                var node=Node(i==4?"一像素贯穿长柱":"一像素引导长柱 "+i);node.AddComponent<MeshFilter>().sharedMesh=mesh;
                node.transform.position=centre+(i==4?Vector3.zero:new Vector3(Mathf.Cos(i*Mathf.PI*.5f)*.9f,0,Mathf.Sin(i*Mathf.PI*.5f)*.9f));
                var renderer=node.AddComponent<MeshRenderer>();renderer.sharedMaterial=columnMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;columns.Add(renderer);RegisterPrimitive("one_pixel_column",renderer);
            }
        }
        private void EvaluateColumns(float t)
        {
            for(int i=0;i<columns.Count;i++)
            {
                bool centreColumn=i==4;float charge=Ramp(t,3.5f+i*.12f,6.8f),release=Ramp(t,Impact,Impact+.3f);
                float height=centreColumn?3*Ramp(t,7,Impact):(.4f+2.0f*charge)*(1-release);
                bool visible=centreColumn?t>=7&&t<Impact+.18f:t>=3.5f+i*.12f&&t<Impact+.3f;columns[i].gameObject.SetActive(visible);
                props.Clear();props.SetFloat("_Reveal",1);props.SetFloat("_ClipHeight",Mathf.Round(height/PixelPitch)*PixelPitch);props.SetFloat("_Energy",centreColumn?4.2f:2.0f+charge*1.1f);columns[i].SetPropertyBlock(props);
            }
        }

        // No terrain or object destruction is present in this revision.
        private void EvaluateFragments(float t) { BrokenPieces=0; }
        private void BuildAtmosphere()
        {
            var emberMesh=BoxMesh(Vector3.one*PixelPitch);effectCubeMesh=emberMesh;
            smokeParticles=new SkyFurnaceSmokeParticles(stage,Layer,centre,emberMesh,smoke,Impact);
            for(int i=0;i<12;i++){var obj=Node("稀疏余烬 "+i);obj.AddComponent<MeshFilter>().sharedMesh=emberMesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=columnMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;obj.transform.localScale=Vector3.one;RegisterPrimitive("one_pixel_cube",renderer);embers.Add(obj.transform);}
            for(int i=0;i<24;i++)
            {
                var obj=Node("汇入生长点的火元素像素 "+i);obj.AddComponent<MeshFilter>().sharedMesh=emberMesh;
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=columnMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
                RegisterPrimitive("one_pixel_cube",renderer);absorbingPixels.Add(renderer);obj.SetActive(false);
            }
        }
        private void BuildBurst()
        {
            var texture=RequireTexture("Art/SkyFurnace/fire_crown");var mesh=Card(texture);
            var coreMaterial=Own(new Material(PixelLayerShader));coreMaterial.SetTexture("_MainTex",texture);coreMaterial.SetFloat("_BurstMode",1);coreMaterial.SetFloat("_Emission",1.8f);
            for(int i=0;i<3;i++)
            {
                var obj=Node("核心魔法火冠纸板 "+i);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.transform.localScale=new Vector3(3,3,1);obj.transform.rotation=Quaternion.Euler(0,i*60,0);
                var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=coreMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.sortingOrder=5;burstCore.Add(renderer);RegisterPrimitive("one_pixel_thick_image",renderer,texture);obj.SetActive(false);
            }
            var rayMesh=BoxMesh(new Vector3(PixelPitch,2,PixelPitch));var v=rayMesh.vertices;for(int i=0;i<v.Length;i++)v[i]+=Vector3.up;rayMesh.vertices=v;rayMesh.RecalculateBounds();
            for(int i=0;i<12;i++)
            {
                var obj=Node("径向冲击一像素长柱 "+i);obj.AddComponent<MeshFilter>().sharedMesh=rayMesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=columnMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;burstRays.Add(renderer);RegisterPrimitive("one_pixel_column",renderer);obj.SetActive(false);
            }
            var fire=Own(new Material(columnMaterial));fire.renderQueue=3000;fire.SetFloat("_ZWrite",0);fire.SetColor("_Tint",Color.white);fire.SetFloat("_Energy",3);fire.SetFloat("_GroundHeight",.015f);
            burstParticles=new SkyFurnaceBurstParticles(stage,Layer,centre,effectCubeMesh,fire,Impact);
        }
        private void RingPose(int i,float t,out Vector3 position,out Quaternion rotation)
        {
            var ring=rings[i];bool caster=i==3;float end=caster?.75f:i==0?2.35f:i==1?4.6f:6.6f;float compression=Ramp(t,5,7);
                float unwind=Ramp(t,Impact,8.1f),settle=Ramp(t,8.1f,10.5f);
                // The perimeter anchors the scene. Sigils settle first, then the core turns into lock.
                float ready=Ramp(t,end,end+.65f);
                float angle=i==1?-8*ready*(1-unwind):i==2?10*ready+24*unwind-8*settle:0;
                float tilt=i==1?3*ready*(1-unwind):i==2?-5*ready*(1-unwind):0;
                rotation=Quaternion.Euler(90+tilt,angle,0);
                // Separate horizontal paper tiers in world space; keep one-pixel thickness and fixed native density.
                float releaseLift=i==2?20*Ramp(t,Impact-.08f,Impact+.22f)*(1-Ramp(t,Impact+.22f,8.5f)):0;
                int lift=caster||i==0?0:Mathf.RoundToInt((i==1?2:4)*compression*(1-unwind)+releaseLift);
                float press=i==2?12*Ramp(t,Impact-.17f,Impact)*(1-Ramp(t,Impact,Impact+.10f)):0;
                float recoil=i==1?6*Ramp(t,Impact,Impact+.08f)*(1-Ramp(t,Impact+.08f,Impact+.35f)):0;
                position=ring.origin+Vector3.up*(ring.height+(lift-Mathf.RoundToInt(press)+Mathf.RoundToInt(recoil))*PixelPitch);
        }
        private Matrix4x4 DeparturePose(int i,float start)
        {RingPose(i,start,out var position,out var rotation);return Matrix4x4.TRS(position,rotation,rings[i].root.localScale);}
        private Matrix4x4 CoreDeparturePose(int i)
        {return Matrix4x4.TRS(centre+Vector3.up*(1.5f+Mathf.Round(.35f*Ramp(.2f,0,.6f)/PixelPitch)*PixelPitch),Quaternion.Euler(0,i*60+(i%2==0?1:-1)*18*Ramp(.2f,0,.6f),0),burstCore[i].transform.localScale);}
        private void BuildDisassembly()
        {
            var material=Own(new Material(columnMaterial));fragmentMaterial=material;material.SetColor("_Tint",Color.white);material.SetFloat("_Energy",4.8f);material.SetFloat("_LightResponse",.15f);material.SetFloat("_ZWrite",1);
            disassembly=new SkyFurnacePixelDisassembly(stage,Layer,effectCubeMesh,material);
            for(int i=0;i<rings.Count;i++)
            {
                int index=i;float start=i==3?Impact+.1f:i==0?9:i==1?8.5f:Impact+.16f,end=i==3?8.2f:i==0?10.8f:i==1?10:Impact+.65f;
                var m=rings[i].renderer.sharedMaterial;var texture=(Texture2D)m.GetTexture("_MainTex");
                disassembly.Add(texture,start,end,i+1,()=>DeparturePose(index,start));
            }
            for(int i=0;i<burstCore.Count;i++)
            {
                int index=i;var m=burstCore[i].sharedMaterial;var texture=(Texture2D)m.GetTexture("_MainTex");
                disassembly.Add(texture,Impact+.20f,Impact+.78f,11,()=>CoreDeparturePose(index));
            }
        }
        private void EvaluateBurst(float t)
        {
            float after=t-Impact;
            for(int i=0;i<burstCore.Count;i++)
            {
                var r=burstCore[i];bool visible=after>=0&&after<.20f;r.gameObject.SetActive(visible);if(!visible)continue;
                float poseAge=Mathf.Min(after,.20f);r.transform.position=centre+Vector3.up*(1.5f+Mathf.Round(.35f*Ramp(poseAge,0,.6f)/PixelPitch)*PixelPitch);
                r.transform.rotation=Quaternion.Euler(0,i*60+(i%2==0?1:-1)*18*Ramp(poseAge,0,.6f),0);
                props.Clear();props.SetFloat("_BurstMode",1);props.SetFloat("_BurstRise",Ramp(after,0,.12f));props.SetFloat("_BurstBurnout",0);props.SetFloat("_Opacity",1);props.SetFloat("_Emission",(after<.16f?2.6f:1.5f)*(EnhancedPixelGlow?1.05f:1));props.SetFloat("_EdgeEnergy",0);r.SetPropertyBlock(props);
            }
            for(int i=0;i<burstRays.Count;i++)
            {
                var r=burstRays[i];float a=after-i%3*.018f;bool visible=a>=0&&a<.38f;r.gameObject.SetActive(visible);if(!visible)continue;
                float u=a/.38f,angle=i*Mathf.PI/6;var direction=new Vector3(Mathf.Cos(angle),.10f+.12f*(i%3),Mathf.Sin(angle)).normalized;
                float distance=.12f+2.05f*(1-(1-u)*(1-u));r.transform.position=centre+Vector3.up*.25f+direction*distance;r.transform.rotation=Quaternion.FromToRotation(Vector3.up,direction);
                float length=(.2f+1.0f*Ramp(a,0,.08f))*(1-Ramp(a,.10f,.38f));
                props.Clear();props.SetFloat("_ClipHeight",Mathf.Round(length/PixelPitch)*PixelPitch);props.SetFloat("_Energy",4.5f*(1-Ramp(a,.18f,.38f)));props.SetColor("_Tint",new Color(1,.88f,.28f,1));r.SetPropertyBlock(props);
            }
        }

        private void BuildOptics()
        {
            var shader=Resources.Load<Shader>("Shaders/SkyFurnaceContourGlow");if(shader==null)throw new MissingReferenceException("Contour glow shader missing");
            var originals=new List<PrimitiveRecord>(effectPrimitives);
            foreach(var item in originals)
            {
                if(item.kind!="one_pixel_thick_image")continue;
                var source=item.renderer;var obj=Node(source.name+" 内外轮廓辉光",source.transform);obj.transform.localPosition=Vector3.zero;obj.transform.localRotation=Quaternion.identity;obj.transform.localScale=Vector3.one;
                obj.AddComponent<MeshFilter>().sharedMesh=Own(PixelEffectCard.BuildGlowCanvas(item.texture,PixelPitch,6));
                var material=Own(new Material(shader));material.SetTexture("_MainTex",item.texture);
                if(source.sharedMaterial.HasProperty("_ChargeMap"))material.SetTexture("_ChargeMap",source.sharedMaterial.GetTexture("_ChargeMap"));
                var halo=obj.AddComponent<MeshRenderer>();halo.sharedMaterial=material;halo.shadowCastingMode=ShadowCastingMode.Off;
                RegisterPrimitive("one_pixel_thick_image",halo,item.texture,6);contourGlows.Add(new GlowTrack{source=source,halo=halo});
            }
            foreach(var renderer in sceneRenderers)foreach(var material in renderer.sharedMaterials)if(material!=null&&material.HasProperty("_BaseColor")&&!environmentColours.ContainsKey(material))environmentColours.Add(material,material.GetColor("_BaseColor"));
            var column=Own(BoxMesh(new Vector3(SkyFurnaceRainParticles.DropPitch,PixelPitch*4,SkyFurnaceRainParticles.DropPitch)));
            var rainMaterial=Own(new Material(columnMaterial));rainMaterial.SetColor("_Tint",Color.white);rainMaterial.SetFloat("_Energy",.025f);rainMaterial.SetFloat("_LightResponse",1.05f);rainMaterial.SetFloat("_GroundHeight",.025f);rainMaterial.renderQueue=2450;
            rain=new SkyFurnaceRainParticles(stage,Layer,centre,column,effectCubeMesh,rainMaterial);
        }
        private void EvaluateOptics(float t)
        {
            localBloom.threshold.value=1.15f;localBloom.intensity.value=.30f;localBloom.scatter.value=.60f;
            fragmentMaterial.SetFloat("_Energy",EnhancedPixelGlow?4.8f:3.2f);
            foreach(var track in contourGlows)
            {
                bool visible=EnhancedPixelGlow&&track.source.gameObject.activeInHierarchy;track.halo.gameObject.SetActive(visible);if(!visible)continue;
                track.source.GetPropertyBlock(props);props.SetFloat("_GlowEnergy",.16f);track.halo.SetPropertyBlock(props);
            }
            sun.color=NightRain?new Color(.35f,.50f,1):new Color(1,.79f,.59f);fill.intensity=NightRain?.065f:.42f;
            RenderCamera.backgroundColor=NightRain?new Color(.025f,.04f,.075f):new Color(.17f,.20f,.27f);
            foreach(var entry in environmentColours)entry.Key.SetColor("_BaseColor",entry.Value*(NightRain?new Color(.50f,.58f,.72f,1):Color.white));
            floor.SetFloat("_Smoothness",NightRain?.74f:.08f);floor.SetFloat("_Metallic",NightRain?.12f:0);masonry.SetFloat("_Smoothness",NightRain?.28f:.04f);
            if(NightRain)rain.Evaluate(t);else rain.Clear();
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
            if(k!=null){if(k.spaceKey.wasPressedThisFrame)Replay();if(k.pKey.wasPressedThisFrame)SetPaused(!Paused);if(k.escapeKey.wasPressedThisFrame)Skip();if(k.rKey.wasPressedThisFrame)ResetStage();if(k.nKey.wasPressedThisFrame)SetNightRain(!NightRain);}
            if(Paused||Age>=Duration)return;
            previousAge=Age;Age=Mathf.Min(Duration,Age+Time.unscaledDeltaTime);Evaluate();
            if(Age>=Duration)StopAudio();
        }
        private static float Ramp(float t,float start,float end)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,t));

        private void Evaluate()
        {
            if(!initialized)return;float t=Age,after=t-Impact;EvaluateActor(t);
            // Camera is configured once; evaluation never changes its transform or projection.
            float compression=Ramp(t,5,7);
            for(int i=0;i<rings.Count;i++)
            {
                var ring=rings[i];bool caster=i==3;
                float start=caster?0:i==0?.65f:i==1?2.25f:4.45f;
                float end=caster?.75f:i==0?2.35f:i==1?4.6f:6.6f;
                // Independent pixel birth times inside compact colonies; no shared frame gate.
                float inkTime=t;
                float reveal=Mathf.Clamp01((t-start)/(end-start));ring.litProgress=reveal;
                // The centre releases first; the ground perimeter dies last.
                float fadeStart=caster?Impact+.1f:(i==0?9.0f:i==1?8.5f:Impact+.16f);
                float fadeEnd=caster?8.2f:(i==0?10.8f:i==1?10.0f:Impact+.65f);
                float fade=t<fadeStart?1:0;ring.root.gameObject.SetActive(reveal>0&&fade>0);
                RingPose(i,Mathf.Min(t,fadeStart),out var ringPosition,out var ringRotation);
                ring.root.SetPositionAndRotation(ringPosition,ringRotation);
                float contact=Ramp(t,Impact-.12f,Impact)*(1-Ramp(t,Impact+.06f,Impact+.25f));
                float releaseFlash=Ramp(t,Impact-.06f,Impact)*(1-Ramp(t,Impact+.04f,Impact+.24f));
                // First appear already lit, hold the complete seal, then surge together at release.
                float energy=(i==0?1.8f:i==1?2.2f:caster?1.9f:2.7f)+compression*.15f+contact*.2f+(i==2?2.4f:.9f)*releaseFlash;
                props.Clear();props.SetFloat("_Opacity",1);
                props.SetFloat("_ShadeLevel",.72f);props.SetFloat("_Emission",energy*(EnhancedPixelGlow?1.05f:1));props.SetFloat("_EdgeEnergy",0);
                props.SetFloat("_SealMode",1);props.SetFloat("_SealReveal",reveal);
                props.SetFloat("_SealClock",t*(caster?.18f:.32f)+i*.13f);props.SetFloat("_SealFlow",(.35f+compression*.15f)*fade);
                ring.renderer.SetPropertyBlock(props);
            }
            EvaluateColumns(t);
            EvaluateAbsorption(t);EvaluateBurst(t);
            FireLightRecipe.Evaluate(t,Impact,coreLight,sun,centre,NightRain?.075f:1.25f);
            impactLight.transform.position=centre+Vector3.up*.70f;impactLight.range=4.8f;
            impactLight.intensity=EnableEffectLighting&&after>=0&&after<1.1f?(6*Mathf.Exp(-after*10)+1.6f*Mathf.Exp(-after*3)):0;
            // Follow the visible floating core, then bring the release light back to contact.
            float lightHeight=t<4.45f?.22f:t<Impact?rings[2].root.position.y+.20f:Mathf.Lerp(1.1f,.35f,Ramp(t,Impact,8.1f));
            coreLight.transform.position=centre+Vector3.up*lightHeight;
            coreLight.intensity*=t<Impact?.52f:.65f;
            coreLight.intensity*=1-.65f*Ramp(t,6.9f,7.12f)*(1-Ramp(t,7.28f,Impact));
            if(!EnableEffectLighting){coreLight.intensity=0;staffLight.intensity=0;}
            actorShadowRenderer.enabled=EnableActorShadow;
            EvaluateFragments(t);smokeParticles.Evaluate(t);burstParticles.Evaluate(t);disassembly.Evaluate(t);EvaluateOptics(t);
            for(int i=0;i<ignitionLights.Length;i++)
            {
                var track=rings[i];var front=track.ignition.Front(track.litProgress);ignitionLights[i].transform.position=track.root.TransformPoint(new Vector3(front.x,front.y,0))+Vector3.up*.22f;
                ignitionLights[i].intensity=EnableEffectLighting&&track.litProgress>.001f?.8f*track.ignition.Fraction(track.litProgress)*(1-Ramp(track.litProgress,.92f,1)):0;
            }
            for(int i=0;i<embers.Count;i++)
            {
                float a=after-i%5*.05f;bool visible=a>=0&&a<4.5f+i%3;embers[i].gameObject.SetActive(visible);if(!visible)continue;
                float angle=i*2.399963f,radius=Mathf.Min(a*.65f,2.55f);embers[i].position=centre+new Vector3(Mathf.Cos(angle)*radius,Mathf.Max(.07f,.5f+a*(.7f+i%4*.07f)-a*a*.16f),Mathf.Sin(angle)*radius);
            }

        }

        private void EvaluateAbsorption(float t)
        {
            for(int i=0;i<absorbingPixels.Count;i++)
            {
                var pixel=absorbingPixels[i];int layer=i/6,slot=i%6;var ring=rings[layer];
                float start=layer==3?0:layer==0?.65f:layer==1?2.25f:4.45f;
                float end=layer==3?.75f:layer==0?2.35f:layer==1?4.6f:6.6f;
                const float life=.28f,period=.43f;
                float phase=slot*.073f,cycle=Mathf.Floor((t-start-phase)/period);
                float birth=start+phase+cycle*period,age=t-birth,arrival=birth+life;
                bool visible=t>start+.03f&&cycle>=0&&age>=0&&age<life&&arrival<end;
                pixel.gameObject.SetActive(visible);if(!visible)continue;
                var cell=ring.ignition.Front((birth-start)/(end-start));
                Vector3 target=ring.root.TransformPoint(new Vector3(cell.x,cell.y,0))+Vector3.up*PixelPitch*.5f;
                float angle=(slot*2.399963f+cycle*.73f),radius=.18f+.035f*(slot%5);
                Vector3 offset=new Vector3(Mathf.Cos(angle)*radius,.13f+.035f*(slot%6),Mathf.Sin(angle)*radius);
                AbsorptionMaximumTravel=Mathf.Max(AbsorptionMaximumTravel,offset.magnitude);
                float u=age/life,pull=1-(1-u)*(1-u);
                Vector3 position=target+offset*(1-pull)+Vector3.up*(.045f*Mathf.Sin(u*Mathf.PI));
                pixel.transform.position=new Vector3(Mathf.Round(position.x/PixelPitch),Mathf.Round(position.y/PixelPitch),Mathf.Round(position.z/PixelPitch))*PixelPitch;
                props.Clear();props.SetColor("_Tint",u<.35f?new Color(1,.55f,.08f,1):new Color(1,.95f,.55f,1));props.SetFloat("_Energy",u<.35f?1.8f:u<.8f?3:4.5f);pixel.SetPropertyBlock(props);
            }
        }

        private void EvaluateActor(float t)
        {
            actor.position=actorOrigin;actor.rotation=fixedCameraRotation;actorMat.SetTexture("_BaseMap",actorFrames[t>=.55f&&t<8.6f?1:0]);
            staffLight.transform.position=actorOrigin+Vector3.up*1.4f+Vector3.right*.3f;staffLight.intensity=1.15f*Ramp(t,0,.7f)*(1-Ramp(t,7,Impact));
        }

        private void EvaluateCamera(float t) { /* Permanently locked; no shot animation. */ }

        private Material WallMaterial()
        {
            var source=RequireTexture("Art/AcademyPermanentWallCandidates20260926/academy_permanent_wall_nesw_a");
            var region=Own(new Texture2D(32,21,source.format,false));Graphics.CopyTexture(source,0,0,0,4,32,21,region,0,0,0,0);region.filterMode=FilterMode.Point;region.wrapMode=TextureWrapMode.Repeat;
            var material=Own(new Material(Shader.Find("Universal Render Pipeline/Lit")));material.SetTexture("_BaseMap",region);material.SetTextureScale("_BaseMap",new Vector2(1,32/21f));material.SetFloat("_Smoothness",.04f);return material;
        }
        private void EnvironmentCard(string name,string source,Vector3 foot)
        {
            if(CutoutTemplate==null)throw new MissingReferenceException("Sky Furnace serialized cutout material missing");
            var texture=RequireTexture(source);var material=Own(new Material(CutoutTemplate));material.SetTexture("_BaseMap",texture);
            var obj=Node(name);obj.transform.position=foot;obj.transform.rotation=Quaternion.Euler(45,0,0);
            var mesh=Own(Quad());var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(vertices[i].x*texture.width/32f,(vertices[i].y+.5f)*texture.height/32f,0);mesh.vertices=vertices;mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }

        private void OnGUI()
        {
            if(!ShowControls)return;
            if(heading==null){heading=new GUIStyle(GUI.skin.label){font=FormalUiKit.Font,fontSize=28,alignment=TextAnchor.UpperCenter};heading.normal.textColor=new Color(.92f,.82f,.6f);caption=new GUIStyle(heading){fontSize=17};caption.normal.textColor=new Color(.75f,.84f,.92f);button=new GUIStyle(GUI.skin.button){font=FormalUiKit.Font,fontSize=18};}
            GUI.Label(new Rect(0,20,Screen.width,40),"天 穹 熔 炉",heading);
            GUI.Label(new Rect(0,Screen.height-76,Screen.width,28),"空格 重播　 P 暂停／继续　 Esc 跳过　 R 复位　 N 日间／夜雨",caption);
            GUI.Label(new Rect(0,Screen.height-46,Screen.width,25),$"{Age:00.00} / 16.00 秒　{(Paused?"暂停":Age>=Duration?"结束":PhaseName)}",caption);
            float left=(Screen.width-480)*.5f;
            if(GUI.Button(new Rect(left,Screen.height-120,112,32),"重播",button))Replay();
            if(GUI.Button(new Rect(left+122,Screen.height-120,112,32),Paused?"继续":"暂停",button))SetPaused(!Paused);
            if(GUI.Button(new Rect(left+244,Screen.height-120,112,32),"跳过",button))Skip();
            if(GUI.Button(new Rect(left+366,Screen.height-120,112,32),"复位",button))ResetStage();
        }
        private void OnEnable(){if(initialized&&stage!=null&&!stage.gameObject.activeSelf){Application.runInBackground=true;if(localPipeline!=null)QualitySettings.renderPipeline=localPipeline;stage.gameObject.SetActive(true);ResetStage();}}
        private void OnDisable(){StopAudio();if(smokeParticles!=null)smokeParticles.Clear();if(burstParticles!=null)burstParticles.Clear();if(disassembly!=null)disassembly.Clear();if(rain!=null)rain.Clear();foreach(var light in ignitionLights)if(light!=null)light.intensity=0;if(staffLight!=null)staffLight.intensity=0;if(coreLight!=null)coreLight.intensity=0;if(impactLight!=null)impactLight.intensity=0;if(stage!=null)stage.gameObject.SetActive(false);if(localPipeline!=null&&QualitySettings.renderPipeline==localPipeline)QualitySettings.renderPipeline=previousPipeline;Application.runInBackground=previousBackground;}
        private void OnDestroy(){StopAudio();foreach(var resource in owned)if(resource!=null)Destroy(resource);owned.Clear();}
    }
}

