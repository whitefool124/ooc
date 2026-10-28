using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // The reveal state owns a cleared physical worktop, not another fullscreen camera.
    internal sealed class Hd2DRewardGallery : MonoBehaviour
    {
        private AcademyDeskBackdrop backdrop;
        private GameObject worktop;
        private readonly Dictionary<GameObject,bool> decorations = new Dictionary<GameObject,bool>();
        private float started;
        private bool animated;
        internal static void Attach(GameObject window,bool animated)
        {
            var view=window.AddComponent<Hd2DRewardGallery>();
            view.backdrop=window.GetComponentInParent<AcademyDeskBackdrop>();
            view.animated=animated;view.started=Time.unscaledTime;
        }
        private void LateUpdate()
        {
            if(backdrop==null||backdrop.Stage==null)return;
            var stage=backdrop.Stage;
            if(worktop==null)
            {
                foreach(string name in new[]{"独立桌边植物","独立以太灯","灯内晶体","实体茶碟","实体杯身","杯中茶面","杯把外侧","杯把接头","桌上固定书脊","独立像素杯碟候选"})
                    foreach(Transform item in stage.transform)
                        if(item.name==name){decorations[item.gameObject]=item.gameObject.activeSelf;item.gameObject.SetActive(false);}
                // Reuse the textured dielectric wood desk already owned by the stage.
                worktop=new GameObject("奖励揭晓补光");
                worktop.layer=stage.gameObject.layer;worktop.transform.SetParent(stage.transform,false);
                var go=new GameObject("候选物件柔和补光",typeof(Light));go.transform.SetParent(worktop.transform,false);
                go.transform.position=stage.transform.TransformPoint(new Vector3(0,3.5f,-2));
                var light=go.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.91f,.78f);
                light.intensity=18;light.range=9;light.shadows=LightShadows.None;light.cullingMask=1<<stage.gameObject.layer;
            }
            float progress=animated?Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-started)/.45f)):1;
            backdrop.SetDocumentFraming(0,.12f*progress);
            if(stage.sceneVolume.profile.TryGet<DepthOfField>(out var depth))
                depth.focusDistance.value=Vector3.Distance(stage.sceneCamera.transform.position,stage.transform.TransformPoint(new Vector3(0,1.48f,1)));
        }
        private void OnDisable() => ReleasePresentation();
        private void OnDestroy() => ReleasePresentation();
        private void ReleasePresentation()
        {
            bool restore=backdrop!=null&&!backdrop.IsReleasingStage&&backdrop.Stage!=null;
            if(restore)
                foreach(var item in decorations)if(item.Key!=null)item.Key.SetActive(item.Value);
            decorations.Clear();
            if(worktop!=null)Destroy(worktop);
            worktop=null;
            if(restore)backdrop.SetDocumentFraming(0,0);
        }
    }
}
