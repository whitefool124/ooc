using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class MainMenuHd2DStage:MonoBehaviour
    {
        public Camera sceneCamera;
        public SpriteRenderer bookMaterialSource;
        public Material bookLitMaterial;
        public Transform bookAnchor,cloth,plant;
        public Light lampLight;
        public Volume sceneVolume;
        public int rendererIndex=1;
        [SerializeField] private Hd2DDeskLightingRig lighting;
        public Hd2DDeskLightingRig Lighting=>lighting;
        private RenderTexture output;
        private RawImage display;
        private Camera inputCamera;
        private readonly Vector3[] displayCorners=new Vector3[4];
        internal void BindDisplay(RawImage image)=>display=image;
        internal Camera InputCamera
        {
            get
            {
                if(inputCamera==null)
                {
                    var node=new GameObject("书桌输入投影",typeof(Camera));node.transform.SetParent(transform,false);
                    inputCamera=node.GetComponent<Camera>();inputCamera.enabled=false;inputCamera.cullingMask=0;
                }
                // A non-rendering camera exposes the displayed viewport to
                // standard uGUI controls too (Slider, ScrollRect, InputField).
                inputCamera.transform.SetPositionAndRotation(sceneCamera.transform.position,sceneCamera.transform.rotation);
                // Desk controls start at the 5.5m reading plane or farther.
                // A 1m input near plane avoids ill-conditioned inverse matrices
                // from the scene camera's .05m near plane at isolated origins.
                inputCamera.nearClipPlane=Mathf.Max(1f,sceneCamera.nearClipPlane);inputCamera.farClipPlane=sceneCamera.farClipPlane;
                inputCamera.orthographic=sceneCamera.orthographic;inputCamera.orthographicSize=sceneCamera.orthographicSize;
                inputCamera.fieldOfView=sceneCamera.fieldOfView;
                inputCamera.pixelRect=DisplayRect;inputCamera.aspect=sceneCamera.aspect;inputCamera.ResetProjectionMatrix();
                return inputCamera;
            }
        }
        internal Rect DisplayRect
        {
            get
            {
                if(display==null)return new Rect(0,0,Screen.width,Screen.height);
                display.rectTransform.GetWorldCorners(displayCorners);
                var canvas=display.canvas;
                var view=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
                var min=RectTransformUtility.WorldToScreenPoint(view,displayCorners[0]);
                var max=RectTransformUtility.WorldToScreenPoint(view,displayCorners[2]);
                return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            }
        }
        private DeskBookRig book;
        private DepthOfField depthOfField;
        private VolumeProfile runtimeProfile;
        private Quaternion clothRotation,plantRotation;
        public RenderTexture Output=>output;
        public DeskBookRig Book=>book;
        private void LateUpdate()
        {
            if(output==null||sceneCamera==null)return;
            float scale=Mathf.Min(1,UiResolutionLayout.ScreenScale(Screen.width,Screen.height));
            int w=Mathf.Max(480,Mathf.RoundToInt(1920*scale)),h=Mathf.Max(270,Mathf.RoundToInt(1080*scale));
            if(output.width==w&&output.height==h)return;
            sceneCamera.targetTexture=null;output.Release();output.width=w;output.height=h;output.Create();sceneCamera.targetTexture=output;
        }
        public void Initialize(int isolatedLayer=31)
        {
            foreach(var item in GetComponentsInChildren<Transform>(true))item.gameObject.layer=isolatedLayer;
            foreach(var light in GetComponentsInChildren<Light>(true))light.cullingMask=1<<isolatedLayer;
            sceneCamera.cullingMask=1<<isolatedLayer;
            sceneCamera.fieldOfView=Hd2DViewProfile.DioramaFieldOfView;
            foreach(string name in new[]{"独立桌边植物","独立以太灯"})
            {
                var item=transform.Find(name);
                if(item!=null)item.localRotation=Quaternion.Euler(0,Hd2DViewProfile.DioramaYaw,0);
            }
            output=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf){name="E HD2D 1920x1080",filterMode=FilterMode.Bilinear,antiAliasing=1,useMipMap=false};
            output.Create();sceneCamera.targetTexture=output;sceneCamera.allowHDR=true;sceneCamera.allowMSAA=false;
            var data=sceneCamera.GetUniversalAdditionalCameraData();data.SetRenderer(rendererIndex);data.renderPostProcessing=true;data.volumeLayerMask=1<<isolatedLayer;
            book=bookAnchor.gameObject.AddComponent<DeskBookRig>();
            book.Initialize(Resources.Load<Sprite>("Art/MainMenuE/BookRigReview/cover"),bookMaterialSource,bookLitMaterial);
            bookMaterialSource.enabled=false;
            runtimeProfile=sceneVolume.profile;runtimeProfile.TryGet(out depthOfField);
            clothRotation=cloth.localRotation;plantRotation=plant.localRotation;
            SetState(0,0,0,0);
            Hd2DDeskArtCandidate.Apply(this);
            if(lighting==null)throw new System.InvalidOperationException("Missing shared desk lighting prefab");
            lighting.Apply(this);
            lighting.RefreshEnvironment(this);
        }
        public void SetState(float opened,float focus,float clock,float motion)
        {
            if(book==null)return;
            book.SetProgress(opened);
            focus=Mathf.Clamp01(focus);
            var target=Vector3.Lerp(new Vector3(0,1.9f,.5f),new Vector3(-.6f,1.45f,-.15f),focus);
            sceneCamera.transform.localPosition=Hd2DViewProfile.CameraPosition(target,Mathf.Lerp(14f,6f,focus));
            sceneCamera.transform.rotation=transform.rotation*Hd2DViewProfile.DioramaRotation;
            if(depthOfField!=null)depthOfField.focusDistance.value=Vector3.Distance(sceneCamera.transform.position,bookAnchor.position+new Vector3(.2f,.03f,.4f));
            lampLight.intensity=2.4f+.08f*Mathf.Sin(clock*1.7f)*motion;
            cloth.localRotation=clothRotation*Quaternion.Euler(0,Mathf.Sin(clock*.7f)*.7f*motion,0);
            plant.localRotation=plantRotation*Quaternion.Euler(0,0,Mathf.Sin(clock*.9f)*.35f*motion);
        }
        private void OnDestroy()
        {
            if(sceneCamera!=null)sceneCamera.targetTexture=null;
            if(output!=null){if(RenderTexture.active==output)RenderTexture.active=null;output.Release();Destroy(output);}
            if(runtimeProfile!=null){foreach(var component in runtimeProfile.components)Destroy(component);Destroy(runtimeProfile);}
        }
#if UNITY_EDITOR
        private const string MaterialRoot="Assets/Game/Resources/Art/MainMenuE/Hd2D/";
        private static Material Material(string name,Color color,Texture texture=null,bool alpha=false,bool emission=false)
        {
            var path=MaterialRoot+name+".mat";
            var material=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null)return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
            if(texture!=null)material.mainTexture=texture;
            material.SetFloat("_Smoothness",.05f);material.SetFloat("_Cull",0);
            if(alpha){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");}
            if(emission){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*2);}
            UnityEditor.AssetDatabase.CreateAsset(material,path);return material;
        }
        private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=31;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        private static Transform Pixel(Transform parent,string name,string textureName,Rect pixels,Vector3 position,float yaw=0,float worldWidth=0)
        {
            var sprite=Resources.Load<Sprite>("Art/MainMenuE/Stage/"+textureName);if(sprite==null)throw new System.InvalidOperationException("Missing stage sprite "+textureName);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=31};go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            float width=worldWidth>0?worldWidth:pixels.width/32;float height=width*pixels.height/pixels.width;
            var mesh=new Mesh{name=name+" sprite plane"};mesh.vertices=new[]{new Vector3(-width*.5f,0,0),new Vector3(width*.5f,0,0),new Vector3(width*.5f,height,0),new Vector3(-width*.5f,height,0)};
            float x=pixels.x/sprite.texture.width,y=(sprite.texture.height-pixels.yMax)/sprite.texture.height,w=pixels.width/sprite.texture.width,h=pixels.height/sprite.texture.height;
            mesh.uv=new[]{new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
            var meshPath=MaterialRoot+name+".asset";var saved=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved==null){UnityEditor.AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}else DestroyImmediate(mesh);
            go.GetComponent<MeshFilter>().sharedMesh=saved;
            go.GetComponent<MeshRenderer>().sharedMaterial=Material(textureName+"_pixel",Color.white,sprite.texture,true);
            return go.transform;
        }
        private static Light Lamp(Transform parent,string name,LightType type,Vector3 position,Color color,float intensity,float range)
        {
            var go=new GameObject(name,typeof(Light)){layer=31};go.transform.SetParent(parent,false);go.transform.localPosition=position;var light=go.GetComponent<Light>();light.type=type;light.color=color;light.intensity=intensity;light.range=range;light.cullingMask=1<<31;light.shadows=LightShadows.Soft;return light;
        }
        private static GameObject Cylinder(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.layer=31;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        public static MainMenuHd2DStage CreateTemplate(VolumeProfile profile)
        {
            var root=new GameObject("E HD2D窗边书桌",typeof(MainMenuHd2DStage)){layer=31};var stage=root.GetComponent<MainMenuHd2DStage>();var p=root.transform;
            var wood=Material("warm timber",new Color(.32f,.17f,.08f));var grain=Material("timber plank",new Color(.39f,.23f,.12f));
            var plaster=Material("warm plaster",new Color(.73f,.68f,.57f));var trim=Material("window timber",new Color(.19f,.10f,.055f));
            var navy=Material("navy cloth",new Color(.04f,.075f,.13f));var brass=Material("brass fittings",new Color(.55f,.38f,.13f));
            Box(p,"实体木地板",new Vector3(0,-.12f,1),new Vector3(10,.24f,9),wood);
            for(int i=0;i<18;i++)Box(p,"地板木条"+i,new Vector3(0,.002f,-3.25f+i*.5f),new Vector3(10,.02f,.47f),i%3==0?wood:grain);
            Box(p,"窗下实体墙",new Vector3(0,.8f,3.2f),new Vector3(10,1.6f,.24f),plaster);
            Box(p,"左墙翼",new Vector3(-6.15f,4.8f,3.2f),new Vector3(7.7f,6.4f,.24f),plaster);
            Box(p,"右墙翼",new Vector3(6.15f,4.8f,3.2f),new Vector3(7.7f,6.4f,.24f),plaster);
            Box(p,"窗上墙",new Vector3(0,6.13f,3.2f),new Vector3(4.6f,3.74f,.24f),plaster);
            Box(p,"左侧实体墙",new Vector3(-5,2.25f,1),new Vector3(.24f,4.5f,4.6f),plaster);
            for(int i=0;i<10;i++)Box(p,"护墙木板"+i,new Vector3(-4.5f+i,.66f,3.03f),new Vector3(.96f,1.22f,.11f),wood);
            foreach(float x in new[]{-2.3f,-.77f,.77f,2.3f})Box(p,"窗框竖梁",new Vector3(x,2.95f,3.07f),new Vector3(.12f,2.65f,.24f),trim);
            foreach(float y in new[]{1.64f,2.9f,4.26f})Box(p,"窗框横梁",new Vector3(0,y,3.07f),new Vector3(4.72f,.12f,.24f),trim);
            Box(p,"深窗台",new Vector3(0,1.6f,2.91f),new Vector3(4.9f,.15f,.65f),wood);
            Pixel(p,"独立学院窗景","vista",new Rect(121,17,144,77),new Vector3(0,1.4f,3.75f),0,5.8f);
            Box(p,"桌面实体",new Vector3(0,1.3f,0),new Vector3(5.8f,.20f,2.8f),wood);
            for(int i=0;i<7;i++)Box(p,"桌面木板"+i,new Vector3(0,1.405f,-1.2f+i*.4f),new Vector3(5.75f,.03f,.385f),i%3==0?wood:grain);
            Box(p,"桌前沿",new Vector3(0,1.17f,-1.35f),new Vector3(5.8f,.28f,.16f),trim);
            foreach(float x in new[]{-2.4f,2.4f})foreach(float z in new[]{-1f,1f})Box(p,"桌腿",new Vector3(x,.6f,z),new Vector3(.17f,1.2f,.17f),wood);
            Pixel(p,"墙边书架","shelf",new Rect(1,1,27,76),new Vector3(-3.55f,.15f,2.83f));
            stage.plant=Pixel(p,"独立桌边植物","plant",new Rect(1,1,23,34),new Vector3(-2.15f,1.425f,.7f),-18);
            stage.cloth=Pixel(p,"学院布幔","banner",new Rect(1,1,32,67),new Vector3(3.1f,1.85f,3.01f));
            var ceramic=Material("cream ceramic",new Color(.82f,.76f,.61f));
            Cylinder(p,"实体茶碟",new Vector3(1.55f,1.448f,-.3f),new Vector3(.72f,.022f,.72f),ceramic);
            Cylinder(p,"实体杯身",new Vector3(1.55f,1.64f,-.3f),new Vector3(.42f,.17f,.42f),ceramic);
            Cylinder(p,"杯中茶面",new Vector3(1.55f,1.811f,-.3f),new Vector3(.345f,.002f,.345f),Material("tea",new Color(.09f,.045f,.018f)));
            Box(p,"杯把外侧",new Vector3(1.84f,1.65f,-.3f),new Vector3(.055f,.24f,.075f),ceramic);
            foreach(float y in new[]{1.55f,1.75f})Box(p,"杯把接头",new Vector3(1.78f,y,-.3f),new Vector3(.15f,.045f,.075f),ceramic);
            Pixel(p,"独立以太灯","lamp",new Rect(25,28,14,34),new Vector3(2.27f,1.425f,.3f),-18);
            Box(p,"灯内晶体",new Vector3(2.27f,1.96f,.32f),new Vector3(.10f,.26f,.10f),Material("active aether",new Color(.05f,.75f,1f),null,false,true));
            stage.lampLight=Lamp(p,"以太灯真实局部光",LightType.Point,new Vector3(2.15f,2.05f,-.05f),new Color(.55f,.8f,1),2.4f,1.7f);
            var sun=Lamp(p,"窗外统一日光",LightType.Directional,new Vector3(0,5,6),new Color(1,.87f,.67f),1.9f,30);sun.transform.localRotation=Quaternion.Euler(38,-145,0);sun.shadowBias=.035f;sun.shadowNormalBias=.08f;
            Lamp(p,"窗边柔和补光",LightType.Point,new Vector3(-1.2f,3.2f,2.3f),new Color(.68f,.78f,1),8,8);
            Lamp(p,"室内暖补光",LightType.Point,new Vector3(-2.5f,3,-2.5f),new Color(1,.9f,.76f),11,8);
            var source=new GameObject("书本材质来源",typeof(SpriteRenderer)){layer=31};source.transform.SetParent(p,false);stage.bookMaterialSource=source.GetComponent<SpriteRenderer>();stage.bookMaterialSource.sprite=Resources.Load<Sprite>("Art/MainMenuE/BookRigReview/cover");stage.bookLitMaterial=Material("book pixel lit",Color.white,stage.bookMaterialSource.sprite.texture,true);stage.bookMaterialSource.sharedMaterial=null;stage.bookMaterialSource.enabled=false;
            var anchor=new GameObject("桌上固定书脊"){layer=31};anchor.transform.SetParent(p,false);anchor.transform.localPosition=new Vector3(-.6f,1.425f,-.7f);anchor.transform.localRotation=Quaternion.Euler(30,0,0);stage.bookAnchor=anchor.transform;
            var camera=new GameObject("HD2D场景相机",typeof(Camera),typeof(UniversalAdditionalCameraData)){layer=31};camera.transform.SetParent(p,false);stage.sceneCamera=camera.GetComponent<Camera>();stage.sceneCamera.orthographic=false;stage.sceneCamera.fieldOfView=38;stage.sceneCamera.nearClipPlane=.05f;stage.sceneCamera.farClipPlane=60;stage.sceneCamera.cullingMask=1<<31;stage.sceneCamera.depth=-30;stage.sceneCamera.clearFlags=CameraClearFlags.SolidColor;stage.sceneCamera.backgroundColor=new Color(.11f,.085f,.07f);
            var volume=new GameObject("主菜单独立景深与色调",typeof(Volume)){layer=31};volume.transform.SetParent(p,false);stage.sceneVolume=volume.GetComponent<Volume>();stage.sceneVolume.isGlobal=true;stage.sceneVolume.sharedProfile=profile;stage.sceneVolume.priority=10;
            return stage;
        }
#endif
    }
}
