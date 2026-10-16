using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // A real lit cutout on a solid display stand. The existing UI rectangle remains
    // the authoritative pointer/focus target; no second camera or physics input.
    internal sealed class Hd2DRewardObject : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private Image source;
        private AcademyDeskBackdrop backdrop;
        private GameObject display;
        private Transform cutout, stand;
        private Mesh mesh;
        private Material objectMaterial, standMaterial, trimMaterial;
        private readonly Vector3[] corners = new Vector3[4];
        private float emphasis, revealTime;
        private bool focused, animated;

        internal static void Attach(GameObject target, Image icon, bool animated)
        {
            if (!Application.isPlaying || icon == null || icon.sprite == null) return;
            var component = target.AddComponent<Hd2DRewardObject>();
            component.source = icon;
            component.backdrop = target.GetComponentInParent<AcademyDeskBackdrop>();
            component.revealTime = Time.unscaledTime;
            component.animated=animated;
        }

        private void Build(MainMenuHd2DStage stage)
        {
            display = new GameObject("HD2D奖励实体展示台");
            display.transform.SetParent(stage.transform, false);
            display.layer = stage.gameObject.layer;
            standMaterial = new Material(stage.bookLitMaterial) { name = "奖励台哑光织物", mainTexture = null, color = new Color(.11f,.17f,.19f) };
            standMaterial.DisableKeyword("_ALPHATEST_ON");standMaterial.SetFloat("_AlphaClip",0);
            trimMaterial = new Material(standMaterial) { name = "奖励台黄铜", color = new Color(.65f,.44f,.16f) };
            stand = Box("有厚度的陈列底座", display.transform, Vector3.zero, Vector3.one, standMaterial).transform;
            for (int i=0;i<4;i++)
            {
                var edge=Box("黄铜镶边"+i,stand,Vector3.zero,Vector3.one,trimMaterial);
                edge.transform.localPosition=i<2?new Vector3(i==0?-.48f:.48f,.56f,0):new Vector3(0,.56f,i==2?-.48f:.48f);
                edge.transform.localScale=i<2?new Vector3(.035f,.06f,1):new Vector3(1,.06f,.035f);
            }
            var face = new GameObject("受光像素物件",typeof(MeshFilter),typeof(MeshRenderer));
            face.layer=display.layer;face.transform.SetParent(display.transform,false);cutout=face.transform;
            var sprite=source.sprite;var rect=sprite.textureRect;var texture=sprite.texture;
            mesh=new Mesh {name="奖励像素面片"};
            mesh.vertices=new[]{new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(.5f,1,0),new Vector3(-.5f,1,0)};
            mesh.uv=new[]{new Vector2(rect.xMin/texture.width,rect.yMin/texture.height),new Vector2(rect.xMax/texture.width,rect.yMin/texture.height),new Vector2(rect.xMax/texture.width,rect.yMax/texture.height),new Vector2(rect.xMin/texture.width,rect.yMax/texture.height)};
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
            face.GetComponent<MeshFilter>().sharedMesh=mesh;
            objectMaterial=new Material(stage.bookLitMaterial){name="奖励像素受光材质",mainTexture=texture,color=Color.white};
            var renderer=face.GetComponent<MeshRenderer>();renderer.sharedMaterial=objectMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            source.enabled=false;
        }

        private static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.layer=parent.gameObject.layer;
            box.transform.SetParent(parent,false);box.transform.localPosition=position;box.transform.localScale=scale;
            Destroy(box.GetComponent<Collider>());box.GetComponent<MeshRenderer>().sharedMaterial=material;return box;
        }

        private void LateUpdate()
        {
            if (source==null || backdrop==null || backdrop.Stage==null) return;
            var stage=backdrop.Stage;
            if(display==null)Build(stage);
            var camera=stage.sceneCamera;
            source.rectTransform.GetWorldCorners(corners);
            var canvas=source.GetComponentInParent<Canvas>();
            var uiCamera=canvas!=null && canvas.renderMode==RenderMode.WorldSpace?canvas.worldCamera:null;
            Vector2 min=RectTransformUtility.WorldToScreenPoint(uiCamera,corners[0]);
            Vector2 max=RectTransformUtility.WorldToScreenPoint(uiCamera,corners[2]);
            if(uiCamera!=null)
            {
                min=new Vector2(min.x*Screen.width/uiCamera.pixelWidth,min.y*Screen.height/uiCamera.pixelHeight);
                max=new Vector2(max.x*Screen.width/uiCamera.pixelWidth,max.y*Screen.height/uiCamera.pixelHeight);
            }
            Vector2 foot=new Vector2((min.x+max.x)*.5f,min.y);
            var ray=camera.ViewportPointToRay(new Vector3(foot.x/Screen.width,foot.y/Screen.height,0));
            var table=new Plane(stage.transform.up,stage.transform.TransformPoint(new Vector3(0,1.48f,0)));
            if(!table.Raycast(ray,out float distance))return;
            Vector3 position=ray.GetPoint(distance);
            float depth=Vector3.Dot(position-camera.transform.position,camera.transform.forward);
            float worldPixel=2*depth*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/Screen.height;
            float width=(max.x-min.x)*worldPixel,height=(max.y-min.y)*worldPixel;
            float motion=animated?1:0;
            emphasis=Mathf.MoveTowards(emphasis,focused?1:0,Time.unscaledDeltaTime*6);
            float enter=animated?Mathf.Clamp01((Time.unscaledTime-revealTime)/.32f):1;
            display.transform.position=position;
            stand.localScale=new Vector3(width*1.28f,.075f,width*.82f);
            stand.localPosition=new Vector3(0,-.04f,0);
            cutout.rotation=camera.transform.rotation;
            cutout.localPosition=new Vector3(0,Mathf.Lerp(-.14f,0,Mathf.SmoothStep(0,1,enter))+.06f*emphasis*motion,0);
            float aspect=source.sprite.rect.width/source.sprite.rect.height;
            if(aspect>1)height=width/aspect;else width=height*aspect;
            cutout.localScale=new Vector3(width,height,1);
            trimMaterial.color=Color.Lerp(new Color(.65f,.44f,.16f),new Color(1,.78f,.32f),emphasis);
        }

        public void OnPointerEnter(PointerEventData e)=>focused=true;
        public void OnPointerExit(PointerEventData e)=>focused=false;
        public void OnSelect(BaseEventData e)=>focused=true;
        public void OnDeselect(BaseEventData e)=>focused=false;
        private void OnDisable(){if(display!=null)display.SetActive(false);}
        private void OnEnable(){if(display!=null)display.SetActive(true);}
        private void OnDestroy()
        {
            if(source!=null)source.enabled=true;
            if(display!=null)Destroy(display);
            if(mesh!=null)Destroy(mesh);
            if(objectMaterial!=null)Destroy(objectMaterial);
            if(standMaterial!=null)Destroy(standMaterial);
            if(trimMaterial!=null)Destroy(trimMaterial);
        }
    }
}
