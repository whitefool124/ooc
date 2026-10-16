using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // Dedicated visual laboratory. No battle model, player profile or game random state.
    [DisallowMultipleComponent]
    public sealed class Hd2DVfxShowcase : MonoBehaviour
    {
        public const float ImpactTime = .30f;
        public const float Duration = 2.6f;
        public Shader GroundRingShader;
        public Shader SmokeShader;
        public Shader ParticleShader;
        private const int StageLayer = 29;
        private readonly List<Object> owned = new List<Object>();
        private readonly List<Transform> debris = new List<Transform>();
        private readonly List<ParticleSystem> particles = new List<ParticleSystem>();
        private readonly Vector3[] directions = new Vector3[20];
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private Camera view;
        private Light burstLight;
        private Transform stage, projectile, core;
        private MeshRenderer castRing, shockRing, outerRing;
        private LineRenderer ribbon;
        private Material energy, ringMaterial;
        private Vector3 source = new Vector3(-2.4f, .85f, -.7f);
        private Vector3 target = new Vector3(1.15f, 0, .8f);
        private float clock = Duration;
        private bool paused, slow;
        private GUIStyle title, hint;
        public bool LightEnabled { get; private set; } = true;
        public bool LayersEnabled { get; private set; } = true;
        public float Age => clock;
        public bool Paused => paused;
        public int ActiveParticleCount
        {
            get { int count = 0; foreach (var p in particles) if (p != null) count += p.particleCount; return count; }
        }
        public float BurstIntensity => burstLight != null ? burstLight.intensity : 0;

        private void Awake()
        {
            BuildStage();
            BuildEffect();
            Replay();
        }

        private Material Keep(Material value) { owned.Add(value); return value; }
        private GameObject Node(string name, Transform parent)
        {
            var node = new GameObject(name) { layer = StageLayer };
            node.transform.SetParent(parent, false);
            return node;
        }
        private Material Lit(string name, Color color, float smoothness = .05f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new MissingReferenceException("URP Lit missing in HD2D VFX scene.");
            var material = Keep(new Material(shader) { name = name });
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
            return material;
        }
        private Transform Box(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.layer = StageLayer;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = size;
            obj.GetComponent<MeshRenderer>().sharedMaterial = material;
            Destroy(obj.GetComponent<Collider>());
            return obj.transform;
        }

        private void BuildStage()
        {
            stage = Node("HD2D真实光影实验台", transform).transform;
            var floor = Lit("学院石台", new Color(.32f, .36f, .39f));
            Texture2D texture = Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_court_a");
            if (texture != null) floor.SetTexture("_BaseMap", texture);
            Box("地台厚度", new Vector3(0, -.2f, 0), new Vector3(9, .4f, 7), Lit("石台侧面",new Color(.17f,.2f,.23f)), stage);
            for (int z = -3; z <= 3; z++) for (int x = -4; x <= 4; x++)
                Box("石格 " + x + "," + z, new Vector3(x, -.015f, z), new Vector3(.98f,.03f,.98f),floor,stage);
            Material wall = Lit("墙面受光参考", new Color(.4f,.45f,.5f));
            Box("后方承光墙",new Vector3(.4f,.65f,2.95f),new Vector3(7.5f,1.3f,.32f),wall,stage);
            Box("左侧遮挡柱",new Vector3(-3.45f,.85f,.7f),new Vector3(.55f,1.7f,.55f),wall,stage);
            Box("右侧遮挡柱",new Vector3(3.45f,.85f,.7f),new Vector3(.55f,1.7f,.55f),wall,stage);
            Material brass = Lit("黄铜测光件",new Color(.64f,.36f,.12f),.55f); brass.SetFloat("_Metallic",.65f);
            Box("黄铜承光件",new Vector3(2.4f,.35f,1.7f),new Vector3(.45f,.7f,.45f),brass,stage);

            var cameraNode = Node("HD2D特效相机",stage); view = cameraNode.AddComponent<Camera>();
            cameraNode.tag = "MainCamera"; cameraNode.AddComponent<AudioListener>();
            view.transform.position = new Vector3(0,9,-9); view.transform.rotation = Quaternion.Euler(45,0,0);
            view.fieldOfView = 43; view.nearClipPlane=.1f; view.farClipPlane=50;
            view.backgroundColor=new Color(.025f,.032f,.048f);view.clearFlags=CameraClearFlags.SolidColor;
            view.cullingMask=1<<StageLayer;view.allowHDR=true;view.allowMSAA=false;
            var cameraData=view.GetUniversalAdditionalCameraData();cameraData.SetRenderer(1);
            cameraData.renderPostProcessing=true;cameraData.volumeLayerMask=1<<StageLayer;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();owned.Add(profile);
            profile.Add<Tonemapping>(true).mode.value=TonemappingMode.ACES;
            var bloom=profile.Add<Bloom>(true);bloom.threshold.value=1.15f;bloom.intensity.value=.48f;bloom.scatter.value=.55f;
            var grade=profile.Add<ColorAdjustments>(true);grade.contrast.value=12;grade.postExposure.value=.1f;
            var volume=Node("实验台独立后期",stage).AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            var sun=Node("冷色庭院光",stage).AddComponent<Light>();sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(48,-30,0);sun.color=new Color(.64f,.77f,1);sun.intensity=.75f;
            sun.cullingMask=1<<StageLayer;sun.shadows=LightShadows.Soft;sun.shadowBias=.025f;sun.shadowNormalBias=.04f;
            burstLight=Node("爆发真实受光",stage).AddComponent<Light>();burstLight.type=LightType.Point;
            burstLight.color=new Color(1,.28f,.035f);burstLight.range=6;burstLight.shadows=LightShadows.None;
            burstLight.cullingMask=1<<StageLayer;burstLight.intensity=0;
        }

        private MeshRenderer Ring(string name)
        {
            var mesh=new Mesh {name=name};owned.Add(mesh);
            mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();
            var obj=Node(name,stage);obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=ringMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;return renderer;
        }

        private void BuildEffect()
        {
            Shader ringShader=GroundRingShader;
            Shader particleShader=ParticleShader;
            if(ringShader==null||particleShader==null)throw new MissingReferenceException("HD2D showcase shaders missing.");
            ringMaterial=Keep(new Material(ringShader));
            energy=Lit("HDR火焰主芯",Color.white);energy.EnableKeyword("_EMISSION");energy.SetColor("_EmissionColor",new Color(12,2.8f,.18f));
            projectile=Box("火带前锋",Vector3.zero,new Vector3(.15f,.15f,.35f),energy,stage);
            core=Box("爆发压缩核心",Vector3.zero,Vector3.one,energy,stage);
            castRing=Ring("向心蓄势环");shockRing=Ring("水平冲击波");outerRing=Ring("第二层地面余波");
            var trailMaterial=Keep(new Material(particleShader));trailMaterial.SetColor("_Color",new Color(5,1.15f,.07f,1));
            trailMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);trailMaterial.SetFloat("_DstBlend",(float)BlendMode.One);
            ribbon=Node("立体火带",stage).AddComponent<LineRenderer>();ribbon.sharedMaterial=trailMaterial;
            ribbon.positionCount=12;ribbon.useWorldSpace=true;ribbon.numCapVertices=2;ribbon.textureMode=LineTextureMode.Stretch;
            ribbon.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.7f,.5f),new Keyframe(1,1));
            var random=new System.Random(90109);var stone=Lit("飞散石屑",new Color(.31f,.28f,.24f));
            for(int i=0;i<20;i++)
            {
                float angle=i*Mathf.PI*2/20+(float)random.NextDouble()*.25f;
                directions[i]=new Vector3(Mathf.Cos(angle)*(1.4f+(float)random.NextDouble()*1.8f),1.4f+(float)random.NextDouble()*2.2f,Mathf.Sin(angle)*(1.4f+(float)random.NextDouble()*1.8f));
                debris.Add(Box("碎屑 "+i,Vector3.zero,Vector3.one*.09f,stone,stage));
            }
            AddParticles("定向火星","sparkle",particleShader,150,.72f,4.2f,.065f,new Color(5,1.1f,.06f,1),true);
            AddParticles("火焰爆发主形","explosion_sharp_soft_3x3",particleShader,4,.40f,.45f,1.8f,new Color(3.5f,.8f,.035f,.9f),true);
            Shader smokeShader=SmokeShader;
            if(smokeShader==null)throw new MissingReferenceException("HD2D lit smoke shader missing.");
            AddParticles("真实受光烟尘","cloud_magic",smokeShader,22,1.45f,1.0f,.7f,new Color(.65f,.6f,.56f,.38f),false);
            AddParticles("消散余烬","sparkle",particleShader,70,1.5f,1.8f,.038f,new Color(3,.38f,.025f,1),true);
        }

        private void AddParticles(string name,string textureName,Shader shader,int count,float life,float speed,float size,Color color,bool additive)
        {
            Texture2D texture=Resources.Load<Texture2D>("Art/PrototypeToonParticles/EpicToonFxSource/"+textureName);
            if(texture==null)throw new MissingReferenceException("Missing existing prototype particle: "+textureName);
            var material=Keep(new Material(shader));material.SetTexture("_MainTex",texture);material.SetColor("_Color",Color.white);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)(additive?BlendMode.One:BlendMode.OneMinusSrcAlpha));
            var p=Node(name,stage).AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=p.main;main.playOnAwake=false;main.loop=false;main.duration=Duration;main.maxParticles=count;
            main.startLifetime=new ParticleSystem.MinMaxCurve(life*.65f,life);main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.3f,speed);
            main.startSize=new ParticleSystem.MinMaxCurve(size*.5f,size);main.startColor=color;main.gravityModifier=additive?.3f:-.04f;
            main.simulationSpace=ParticleSystemSimulationSpace.World;p.useAutoRandomSeed=false;p.randomSeed=(uint)(count*97);
            var emission=p.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(ImpactTime,(short)count)});
            var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.18f;
            p.transform.rotation=Quaternion.Euler(-90,0,0);
            var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.35f),new GradientAlphaKey(0,1)});
            var colors=p.colorOverLifetime;colors.enabled=true;colors.color=fade;
            var sizes=p.sizeOverLifetime;sizes.enabled=true;sizes.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,additive?1:.4f,1,additive?0:1.5f));
            p.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;particles.Add(p);
            if(textureName=="explosion_sharp_soft_3x3")
            {
                var sheet=p.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=3;sheet.numTilesY=3;
                sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));sheet.cycleCount=1;
            }
        }

        public void Replay()
        {
            clock=0;paused=false;
            foreach(var p in particles){p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);p.transform.position=target+Vector3.up*.18f;}
            Evaluate();
        }
        public void SetAge(float age)
        {
            clock=Mathf.Clamp(age,0,Duration);paused=true;
            foreach(var p in particles){p.transform.position=target+Vector3.up*.18f;p.Simulate(clock,true,true,false);}
            Evaluate();
        }
        public void SetLightEnabled(bool value){LightEnabled=value;Evaluate();}
        public void SetLayersEnabled(bool value){LayersEnabled=value;Evaluate();}
        public void ClearEffect(){SetAge(Duration);foreach(var p in particles)p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}

        private void Update()
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.spaceKey.wasPressedThisFrame)Replay();
                if(keyboard.pKey.wasPressedThisFrame)paused=!paused;
                if(keyboard.sKey.wasPressedThisFrame)slow=!slow;
                if(keyboard.lKey.wasPressedThisFrame)SetLightEnabled(!LightEnabled);
                if(keyboard.oKey.wasPressedThisFrame)SetLayersEnabled(!LayersEnabled);
            }
            var mouse=Mouse.current;
            if(mouse!=null && mouse.leftButton.wasPressedThisFrame && mouse.position.ReadValue().y<Screen.height-135)
            {
                var plane=new Plane(Vector3.up,Vector3.zero);Ray ray=view.ScreenPointToRay(mouse.position.ReadValue());
                if(plane.Raycast(ray,out float distance)){Vector3 point=ray.GetPoint(distance);target=new Vector3(Mathf.Clamp(point.x,-3,3),0,Mathf.Clamp(point.z,-2,2.3f));Replay();}
            }
            if(!paused && clock<Duration)
            {
                float delta=Mathf.Min(Time.unscaledDeltaTime*(slow?.25f:1),Duration-clock);clock+=delta;
                foreach(var p in particles)p.Simulate(delta,true,false,false);
                Evaluate();
            }
        }

        private void SetRing(MeshRenderer renderer,Vector3 position,float diameter,float opacity,Color color,float width)
        {
            renderer.enabled=LayersEnabled&&opacity>.001f;
            renderer.transform.position=position+Vector3.up*.035f;renderer.transform.localScale=Vector3.one*diameter;
            block.Clear();block.SetColor("_Tint",new Color(color.r,color.g,color.b,opacity));block.SetFloat("_Width",width);
            renderer.SetPropertyBlock(block);
        }
        private void Evaluate()
        {
            float flight=Mathf.Clamp01((clock-.12f)/.18f),after=clock-ImpactTime;
            Vector3 end=target+Vector3.up*.65f;
            Vector3 head=Vector3.Lerp(source,end,flight)+Vector3.up*(Mathf.Sin(flight*Mathf.PI)*.35f);
            projectile.gameObject.SetActive(LayersEnabled&&clock>=.12f&&after<0);projectile.position=head;
            projectile.rotation=Quaternion.LookRotation((end-source).normalized);projectile.localScale=new Vector3(.18f,.18f,.55f);
            ribbon.enabled=LayersEnabled&&clock>=.12f&&after<.1f;
            ribbon.widthMultiplier=.22f*(after<0?1:Mathf.Clamp01(1-after/.1f));
            for(int i=0;i<12;i++){float t=Mathf.Lerp(Mathf.Max(0,flight-.36f),flight,i/11f);ribbon.SetPosition(i,Vector3.Lerp(source,end,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.35f);}
            float flash=after>=0?Mathf.Exp(-after*13):0;
            core.gameObject.SetActive(LayersEnabled&&flash>.006f);core.position=end;
            core.localScale=Vector3.one*(.16f+Mathf.Sin(Mathf.Clamp01(after/.16f)*Mathf.PI)*1.1f)*Mathf.Sqrt(flash);
            core.rotation=Quaternion.Euler(25,clock*160,45);
            burstLight.transform.position=after<0?head:target+Vector3.up*.85f;
            burstLight.intensity=LightEnabled&&clock<Duration&&after<1.2f?(after<0?(clock>=.12f?1.4f:0):22*Mathf.Exp(-after*6)):0;
            SetRing(castRing,new Vector3(source.x,0,source.z),Mathf.Lerp(1.4f,.25f,Mathf.Clamp01(clock/.12f)),clock<.12f?1:0,new Color(4,.9f,.04f),.035f);
            SetRing(shockRing,target,1+Mathf.Max(0,after)*8,after>=0&&after<.5f?1-after/.5f:0,new Color(5,1.3f,.04f),.025f);
            SetRing(outerRing,target,1+Mathf.Max(0,after-.06f)*6,after>=.06f&&after<.7f?.45f*(1-after/.7f):0,new Color(2,.23f,.025f),.013f);
            for(int i=0;i<debris.Count;i++)
            {
                var piece=debris[i];bool visible=LayersEnabled&&after>=0&&after<1.0f;piece.gameObject.SetActive(visible);
                if(!visible)continue;
                Vector3 p=target+Vector3.up*.3f+directions[i]*after+Vector3.down*4.5f*after*after;
                p.y=Mathf.Max(.05f,p.y);piece.position=p;piece.rotation=Quaternion.Euler(i*31+after*270,i*59+after*150,after*330);
                piece.localScale=Vector3.one*(.07f+(i%3)*.035f)*Mathf.Clamp01((1-after)/.35f);
            }
            foreach(var p in particles)p.GetComponent<ParticleSystemRenderer>().enabled=LayersEnabled;
        }

        private void OnGUI()
        {
            if(title==null){title=new GUIStyle(GUI.skin.label){font=FormalUiKit.Font,fontSize=26,alignment=TextAnchor.UpperCenter};title.normal.textColor=new Color(1,.83f,.54f);hint=new GUIStyle(title){fontSize=18};hint.normal.textColor=new Color(.8f,.84f,.9f);}
            GUI.Label(new Rect(0,18,Screen.width,40),"HD-2D　以太爆破实验场",title);
            GUI.Label(new Rect(0,58,Screen.width,30),"点击地面改变落点　空格重播　P暂停　S慢放　L真实光对比　O只看受光",hint);
            GUI.Label(new Rect(0,90,Screen.width,30),$"{clock:0.00}s　粒子 {ActiveParticleCount}　爆发光 {(LightEnabled?"开启":"关闭")}　{(paused?"暂停":slow?"慢放":"播放")}",hint);
        }
        private void OnDisable(){if(burstLight!=null)ClearEffect();}
        private void OnDestroy(){foreach(var value in owned)if(value!=null)Destroy(value);owned.Clear();}
    }
}
