using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // A separate world-space scene owns lighting; uGUI only presents its low-res output.
    public sealed class MainMenuPixelStage : MonoBehaviour
    {
        public Camera sceneCamera;
        public SpriteRenderer closedBook, openBook;
        public Light2D aetherLight;
        public float aetherBaseIntensity=.48f;
        public ShadowCaster2D closedShadow,openShadow;
        private RenderTexture output;
        private DeskBookRig bookRig;
        public DeskBookRig BookRig => bookRig;
        public RenderTexture Output => output;
        public void Initialize()
        {
            output=new RenderTexture(320,180,24,RenderTextureFormat.ARGB32)
            {name="E scene native 320x180",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,
                antiAliasing=1,useMipMap=false,autoGenerateMips=false};
            output.Create();sceneCamera.targetTexture=output;
            sceneCamera.allowHDR=false;sceneCamera.allowMSAA=false;sceneCamera.allowDynamicResolution=false;
            var cover=Resources.Load<Sprite>("Art/MainMenuE/BookRigReview/cover");
            if(cover==null)throw new System.InvalidOperationException("Missing processed book rig cover");
            var root=new GameObject("桌面书本分层结构"){layer=closedBook.gameObject.layer};root.transform.SetParent(transform,false);
            root.transform.localPosition=closedBook.transform.localPosition+new Vector3(-DeskBookRig.Width/64,-30f/32,0);
            bookRig=root.AddComponent<DeskBookRig>();bookRig.Initialize(cover,closedBook);
            SetState(0,1,0,0,0);
        }
        public void SetState(float opened,float zoom,float focus,float clock,float motion)
        {
            if(sceneCamera==null)return;
            closedBook.color=Color.white;openBook.color=Color.white;
            closedBook.enabled=false;openBook.enabled=false;
            if(bookRig!=null)bookRig.SetProgress(opened);
            // One ground caster follows the opening footprint continuously.
            // Switching two differently centred casters made the shadow jump.
            if(closedShadow!=null)
            {
                closedShadow.enabled=true;
                closedShadow.transform.localPosition=new Vector3(Mathf.Round(-DeskBookRig.Width*.5f*opened)/32,-30f/32,0);
                closedShadow.transform.localScale=new Vector3(Mathf.Round(Mathf.Lerp(DeskBookRig.Width*.5f,DeskBookRig.Width,opened))/26,1,1);
            }
            if(openShadow!=null)openShadow.enabled=false;
            // Camera motion is snapped before rasterization, never by scaling a finished sprite.
            Vector2 centre=Vector2.Lerp(new Vector2(160,90),new Vector2(154,36),focus);
            centre=new Vector2(Mathf.Round(centre.x),Mathf.Round(centre.y));
            sceneCamera.transform.localPosition=new Vector3(centre.x/32,centre.y/32,-10);
            sceneCamera.orthographicSize=180f/(64*Mathf.Max(1,zoom));
            var pixel=sceneCamera.GetComponent<PixelPerfectCamera>();
            if(pixel!=null)pixel.assetsPPU=Mathf.RoundToInt(32*Mathf.Max(1,zoom));
            aetherLight.intensity=aetherBaseIntensity+.02f*Mathf.Sin(Mathf.Floor(clock*12)/12*1.4f)*motion;
        }
        private void OnDestroy()
        {
            if(sceneCamera!=null)sceneCamera.targetTexture=null;
            if(output!=null){output.Release();Destroy(output);}
        }
    }
}
