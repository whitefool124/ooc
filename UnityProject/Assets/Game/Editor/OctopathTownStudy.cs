#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    // Local reference study: assets load from the user's extracted game archive.
    // No shipping Resources, imported art, gameplay saves or authored scenes touched.
    public static class OctopathTownStudy
    {
        public static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../Worldbuilding/归档/2026-10-10_八方旅人还原样板"));
        private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static Camera Camera { get; private set; }
        public static GameObject Root { get; private set; }
        public static string Status { get; private set; } = "Not built";
        private static string Package(string path) => path.Substring(0, path.LastIndexOf('.'));
        private static Vector3 Vector(JToken t, Vector3 fallback)
        {
            return t == null ? fallback : new Vector3((float)t["X"], (float)t["Z"], (float)t["Y"]);
        }
        private static Quaternion Rotation(JToken t)
        {
            if(t == null) return Quaternion.identity;
            float p=(float)(t["Pitch"]??0)*Mathf.Deg2Rad*.5f;
            float y=(float)(t["Yaw"]??0)*Mathf.Deg2Rad*.5f;
            float r=(float)(t["Roll"]??0)*Mathf.Deg2Rad*.5f;
            float sp=Mathf.Sin(p),cp=Mathf.Cos(p),sy=Mathf.Sin(y),cy=Mathf.Cos(y),sr=Mathf.Sin(r),cr=Mathf.Cos(r);
            return new Quaternion(cr*sp*sy-sr*cp*cy,cr*cp*sy-sr*sp*cy,-cr*sp*cy-sr*cp*sy,-cr*cp*cy-sr*sp*sy);
        }
        private static Texture2D Texture(string path, bool normal=false)
        {
            if(string.IsNullOrEmpty(path)) return null;
            string file=Path.Combine(Folder,"scene_data",Package(path)+".png");
            string key=file+(normal?" linear":" colour");
            if(textures.TryGetValue(key,out var loaded))return loaded;
            if(!File.Exists(file))return null;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,normal){name=Path.GetFileNameWithoutExtension(file),filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            if(!texture.LoadImage(File.ReadAllBytes(file)))throw new InvalidDataException(file);
            textures.Add(key,texture);return texture;
        }
        private static Color Colour(JToken t, Color fallback)
        {
            return t==null?fallback:new Color((float)t["R"],(float)t["G"],(float)t["B"],1);
        }
        private static Material Material(string path)
        {
            if(path==null)throw new InvalidDataException("Missing material reference");
            string package=Package(path);
            if(materials.TryGetValue(package,out var value))return value;
            string file=Path.Combine(Folder,"scene_data",package+".json");
            if(!File.Exists(file))throw new FileNotFoundException("Material export missing",file);
            var data=JObject.Parse(File.ReadAllText(file));var maps=data["Textures"] as JObject;
            string diffuse=(string)(maps?["DoorTexture"]??(maps?["CombinedColor"]!=null?maps?["BaseTexture"]:null)??maps?["ColorMap"]??maps?["PM_Diffuse"]??maps?["BaseTexture"]);
            string normal=(string)(maps?["NormalMap"]??maps?["BaseNormal"]??maps?["PM_Normals"]);
            var shader=Shader.Find("OCC/Research/OctopathSurface");
            if(shader==null)throw new InvalidOperationException("Missing study shader");
            var material=new Material(shader){name=Path.GetFileName(package)};
            var albedo=Texture(diffuse);if(albedo!=null)material.SetTexture("_BaseMap",albedo);
            material.SetFloat("_Smoothness",.15f);
            var normalMap=Texture(normal,true);
            if(normalMap!=null){material.SetTexture("_NormalMap",normalMap);material.SetFloat("_UseNormalMap",1);}
            var pars=data["Parameters"];var scalars=pars?["Scalars"];var colours=pars?["Colors"];
            // Only the M_KSObject family references this collection. Do not impose
            // object defaults on unrelated terrain/background materials.
            if(pars?["Properties"]?["MaterialParameterCollectionInfos"] is JArray collections &&
                collections.Any(c=>((string)c["ParameterCollection"]?["ObjectName"])=="MaterialParameterCollection'MPC_Object'"))
                material.SetFloat("_UseSourceParameters",1);
            if((bool?)(pars?["Switches"]?["GradationSwitch"])==true)
            {
                material.SetFloat("_UseGradient",1);material.SetColor("_GradientBottom",Colour(colours?["BottomColor"],Color.white));
                material.SetColor("_GradientTop",Colour(colours?["TopColor"],Color.white));
                material.SetFloat("_GradientPos",(float?)(scalars?["GradientPos"])??0);
                material.SetFloat("_GradientScale",(float?)(scalars?["GradientScale"])??200);
            }
            if(maps?["CombinedColor"]!=null)
            {
                material.SetTexture("_CombinedMap",Texture((string)maps["CombinedColor"]));
                material.SetTexture("_SlantMap",Texture((string)maps["SlantTexture"]));
                material.SetFloat("_UseWorldGround",1);
            }
            // Alpha-cut geometry and foliage are retained from original textures.
            if(diffuse!=null&&(int?)pars?["BlendMode"]==1){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.35f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;}
            materials.Add(package,material);return material;
        }
        [MenuItem("OCC/Research/Build Octopath town study (Play Mode)")]
        public static void Build()
        {
            if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play Mode required");
            Application.runInBackground=true;
            if(Root!=null)throw new InvalidOperationException("Study already built; exit Play Mode before rebuilding");
            meshes.Clear();materials.Clear();textures.Clear();
            var scene=JArray.Parse(File.ReadAllText(Path.Combine(Folder,"T_Plain_L_A.json")));
            var meshData=JObject.Parse(File.ReadAllText(Path.Combine(Folder,"scene_data/meshes.json")));
            foreach(var item in meshData.Properties())
            {
                var d=item.Value;var mesh=new Mesh{name=item.Name,indexFormat=IndexFormat.UInt32};
                mesh.vertices=d["vertices"].Select(v=>new Vector3((float)v[0],(float)v[2],(float)v[1])*.01f).ToArray();
                mesh.normals=d["normals"].Select(v=>new Vector3((float)v[0],(float)v[2],(float)v[1])).ToArray();
                mesh.uv=d["uv"].Select(v=>new Vector2((float)v[0],1-(float)v[1])).ToArray();
                if(d["extraUV"]?.First!=null)mesh.uv2=d["extraUV"].First.Select(v=>new Vector2((float)v[0],1-(float)v[1])).ToArray();
                if(d["colors"] is JArray cols)mesh.colors=cols.Select(c=>new Color((float)c[0],(float)c[1],(float)c[2],(float)c[3])).ToArray();
                int[] indices=d["indices"].Select(i=>(int)i).ToArray();var sections=d["sections"] as JArray;mesh.subMeshCount=sections.Count;
                for(int i=0;i<sections.Count;i++)
                {
                    int first=(int)sections[i]["FirstIndex"],count=(int)sections[i]["NumFaces"]*3;var triangles=indices.Skip(first).Take(count).ToArray();
                    mesh.SetTriangles(triangles,i);
                }
                mesh.RecalculateBounds();mesh.RecalculateTangents();meshes.Add(item.Name,mesh);
            }
            foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())root.SetActive(false);
            Root=new GameObject("Octopath original-data study - local only");Root.layer=30;
            var cleanup=Root.AddComponent<OctopathTownStudyControls>();cleanup.Owned=meshes.Values.Cast<Object>().ToList();
            var rendering=new OctopathStudyRendering();cleanup.Rendering=rendering;rendering.Initialize();
            var nodes=new Dictionary<int,Transform>();var actors=new Dictionary<string,JToken>();
            foreach(var o in scene)if((string)o["Outer"]=="PersistentLevel")actors[(string)o["Name"]]=o;
            for(int i=0;i<scene.Count;i++)if(((string)scene[i]["Type"]).EndsWith("Component"))
            {
                var obj=new GameObject((string)scene[i]["Outer"]+" / "+(string)scene[i]["Name"]);obj.layer=30;obj.transform.SetParent(Root.transform,false);nodes[i]=obj.transform;
            }
            foreach(var pair in nodes)
            {
                var o=scene[pair.Key];var p=o["Properties"];var t=pair.Value;string parent=(string)p?["AttachParent"]?["ObjectPath"];
                if(parent==null&&actors.TryGetValue((string)o["Outer"],out var actor))parent=(string)actor["Properties"]?["RootComponent"]?["ObjectPath"];
                if(parent!=null&&int.TryParse(parent.Substring(parent.LastIndexOf('.')+1),out int idx)&&idx!=pair.Key&&nodes.TryGetValue(idx,out var pt))t.SetParent(pt,false);
                t.localPosition=Vector(p?["RelativeLocation"],Vector3.zero)*.01f;t.localRotation=Rotation(p?["RelativeRotation"]);t.localScale=Vector(p?["RelativeScale3D"],Vector3.one);
            }
            var missing=new List<string>();int rendered=0;
            foreach(var pair in nodes)
            {
                var o=scene[pair.Key];var p=o["Properties"];string path=(string)p?["StaticMesh"]?["ObjectPath"];
                if(path==null||!meshes.TryGetValue(path,out var mesh))continue;
                if((bool?)p["bHiddenInGame"]==true||(bool?)p["bVisible"]==false)continue;
                if(actors.TryGetValue((string)o["Outer"],out var actor)&&(bool?)actor["Properties"]?["bHidden"]==true)continue;
                // Collision boxes and Godray meshes need separate shader translation.
                if(path.Contains("1M_Cube")||path.Contains("Godray"))continue;
                var sections=meshData[path]["sections"];var overrides=p["OverrideMaterials"] as JArray;
                var assigned=new List<Material>();bool valid=true;
                for(int i=0;i<mesh.subMeshCount;i++)try{
                    string mp=overrides!=null&&i<overrides.Count?(string)overrides[i]["ObjectPath"]:null;
                    if(mp==null)mp=(string)sections[i]["material"];assigned.Add(Material(mp));
                }catch(Exception e){missing.Add(pair.Value.name+": "+e.Message);valid=false;break;}
                if(!valid)continue;
                pair.Value.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=pair.Value.gameObject.AddComponent<MeshRenderer>();r.sharedMaterials=assigned.ToArray();r.shadowCastingMode=ShadowCastingMode.On;rendered++;
            }
            var cameraObject=new GameObject("Study camera");cameraObject.transform.SetParent(Root.transform);Camera=cameraObject.AddComponent<Camera>();
            Camera.cullingMask=1<<30;Camera.clearFlags=CameraClearFlags.SolidColor;Camera.backgroundColor=new Color(.045f,.05f,.055f);Camera.allowHDR=true;Camera.allowMSAA=false;Camera.fieldOfView=30;Camera.nearClipPlane=.1f;Camera.farClipPlane=200;Camera.depth=100;
            Camera.GetUniversalAdditionalCameraData().SetRenderer(1);Camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;Camera.GetUniversalAdditionalCameraData().volumeLayerMask=1<<30;
            var renderers=Root.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            cleanup.Target=new Vector3(-10,1.7f,0);cleanup.Distance=34;cleanup.StudyCamera=Camera;cleanup.ApplyCamera();
            var sunObject=new GameObject("Original sun direction");sunObject.transform.SetParent(Root.transform);var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=3;sun.color=new Color(1,1,.98f);sun.shadows=LightShadows.Soft;sun.shadowStrength=1;sun.cullingMask=1<<30;sun.shadowBias=.03f;sun.shadowNormalBias=.2f;sun.transform.rotation=Quaternion.Euler(32,55,0);RenderSettings.sun=sun;
            rendering.BindCloud(sun);rendering.BindMaterials(materials.Values);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.22f,.27f,.29f);RenderSettings.ambientEquatorColor=new Color(.15f,.16f,.15f);RenderSettings.ambientGroundColor=new Color(.08f,.07f,.065f);RenderSettings.ambientIntensity=1;
            var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.17f,.19f,.19f));RenderSettings.ambientProbe=ambient;
            // Original DXT1 environment strip. Face orientation remains a study
            // conversion, while all reflection pixels come from the original.
            var strip=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            strip.LoadImage(File.ReadAllBytes(Path.Combine(Folder,"scene_data/ambient_cube/cube_strip.png")));
            var cube=new Cubemap(strip.width,TextureFormat.RGBA32,true){name="Original EnTX_Sky_Plain_Town"};
            int[] faceMap={0,1,4,5,2,3};Color mean=Color.black;
            for(int face=0;face<6;face++)
            {
                var pixels=strip.GetPixels(0,(5-faceMap[face])*strip.width,strip.width,strip.width);
                cube.SetPixels(pixels,(CubemapFace)face);foreach(var pixel in pixels)mean+=pixel/(6*strip.width*strip.width);
            }
            cube.Apply(true);RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=cube;RenderSettings.reflectionIntensity=.3f;
            // Diffuse approximation uses a separate irradiance scale until the
            // original UE convolution and material functions are reconstructed.
            foreach(var m in materials.Values)m.SetColor("_StudyAmbient",mean*.6f);
            cleanup.Owned.Add(cube);cleanup.Owned.Add(strip);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.563742f,.757799f,1);RenderSettings.fogStartDistance=40;RenderSettings.fogEndDistance=90;
            var volumeObject=new GameObject("Original-data postprocessing translation");volumeObject.layer=30;volumeObject.transform.SetParent(Root.transform);var volume=volumeObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=ScriptableObject.CreateInstance<VolumeProfile>();
            var profile=volume.profile;var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(.35f);grade.saturation.Override(-10);grade.contrast.Override(8);
            var liftGamma=profile.Add<LiftGammaGain>(true);liftGamma.gamma.Override(new Vector4(1,1,1,.2f));
            rendering.BindGrade(grade,liftGamma);
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.5f);bloom.threshold.Override(1);bloom.scatter.Override(.75f);
            var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.4f);vignette.smoothness.Override(.65f);
            var dof=profile.Add<DepthOfField>(true);dof.mode.Override(DepthOfFieldMode.Bokeh);dof.focusDistance.Override(34);dof.focalLength.Override(200);dof.aperture.Override(1.4f);cleanup.Focus=dof;
            cleanup.Owned.AddRange(materials.Values);cleanup.Owned.AddRange(textures.Values);cleanup.Owned.AddRange(profile.components);cleanup.Owned.Add(profile);cleanup.Owned.Add(volume.sharedProfile);
            Directory.CreateDirectory(Path.Combine(Folder,"unity_evidence"));
            File.WriteAllText(Path.Combine(Folder,"unity_evidence/build.json"),new JObject{{"rendered_mesh_components",rendered},{"unique_meshes",meshes.Count},{"materials",materials.Count},{"textures",textures.Count},{"missing",new JArray(missing)},{"bounds_center",bounds.center.ToString()},{"bounds_size",bounds.size.ToString()}}.ToString());
            Status=$"{rendered} original mesh instances; {materials.Count} materials; {missing.Count} missing. Bounds {bounds.center} {bounds.size}";Debug.Log(Status);
            EditorApplication.QueuePlayerLoopUpdate();
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Repaint();
        }
    }
    public sealed class OctopathTownStudyControls : MonoBehaviour
    {
        public OctopathStudyRendering Rendering;
        public List<Object> Owned;public Camera StudyCamera;public Vector3 Target;public float Distance;public DepthOfField Focus;
        private bool postKeyDown;
        public void ApplyCamera(){if(StudyCamera==null)return;StudyCamera.transform.rotation=Quaternion.Euler(20,90,0);StudyCamera.transform.position=Target-StudyCamera.transform.forward*Distance;if(Focus!=null)Focus.focusDistance.value=Distance;}
        private void Update()
        {
            if(Rendering!=null)Rendering.Tick();
            var k=Keyboard.current;if(k==null)return;float speed=Time.unscaledDeltaTime*8;
            if(k.aKey.isPressed)Target.z+=speed;if(k.dKey.isPressed)Target.z-=speed;if(k.wKey.isPressed)Target.x+=speed;if(k.sKey.isPressed)Target.x-=speed;
            if(k.qKey.isPressed)Distance+=speed;if(k.eKey.isPressed)Distance=Mathf.Max(10,Distance-speed);
            bool postPressed=k.fKey.isPressed;
            if(postPressed&&!postKeyDown){var data=StudyCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=!data.renderPostProcessing;}
            postKeyDown=postPressed;
            ApplyCamera();
        }
        private void OnDestroy(){if(Rendering!=null)Rendering.Cleanup();if(Owned!=null)foreach(var obj in Owned.Distinct())if(obj!=null)Object.Destroy(obj);}
    }
}
#endif
