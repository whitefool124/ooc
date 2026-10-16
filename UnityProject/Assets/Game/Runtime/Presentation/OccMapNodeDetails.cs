using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class OccMapNodeDetails : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform icon;
        private Transform board;
        private IReadOnlyList<Vector2> route;
        private GameObject guide;
        private Func<float> animation;
        private Vector2 baseline;
        private bool current;
        private float landed=-1,previousZoom=1;
        public void Configure(RectTransform artwork,Transform map,IReadOnlyList<Vector2> availableRoute,
            bool isCurrent,bool arrived,Func<float> intensity)
        {
            icon=artwork;board=map;route=availableRoute;current=isCurrent;animation=intensity;
            if(icon!=null)baseline=icon.anchoredPosition;
            landed=arrived?Time.unscaledTime:-1;
        }
        private void LateUpdate()
        {
            if(icon==null || board==null)return;
            float zoom=board.localScale.x;
            if(current && zoom!=previousZoom){landed=Time.unscaledTime;previousZoom=zoom;}
            icon.localScale=Vector3.one*(current?1f/Mathf.Max(1,zoom):1);
            float age=Time.unscaledTime-landed;
            float offset=animation!=null && animation()>.01f && landed>=0 && age<.24f?
                Mathf.Round((age<.09f?-4:2)*(1-age/.24f)*Mathf.Clamp01(animation())):0;
            icon.anchoredPosition=baseline+Vector2.up*(offset/Mathf.Max(1,zoom));
        }
        public void OnPointerEnter(PointerEventData data)
        {
            if(route==null || route.Count<2 || guide!=null)return;
            guide=new GameObject("可达路线预览",typeof(RectTransform));guide.transform.SetParent(board,false);
            var root=(RectTransform)guide.transform;root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);
            guide.transform.SetAsFirstSibling();
            for(int i=1;i<route.Count;i++)
            {
                Vector2 a=route[i-1],b=route[i],delta=b-a;
                var go=new GameObject("路线段",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(root,false);
                var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
                r.anchoredPosition=(a+b)*.5f;r.sizeDelta=new Vector2(delta.magnitude,3);
                r.localEulerAngles=new Vector3(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                var image=go.GetComponent<Image>();image.color=new Color(.30f,.69f,.62f,.48f);image.raycastTarget=false;
            }
        }
        public void OnPointerExit(PointerEventData data)=>Clear();
        private void OnDisable()=>Clear();
        private void Clear(){if(guide!=null){guide.SetActive(false);Destroy(guide);guide=null;}}
    }
}
