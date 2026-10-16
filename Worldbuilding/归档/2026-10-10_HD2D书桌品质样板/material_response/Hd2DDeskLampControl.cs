using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // A local visual preference, independent of the player's run and combat data.
    internal sealed class Hd2DDeskLampControl : MonoBehaviour
    {
        internal const string PreferenceKey="OCC.Visual.DeskLampBrightness.v1";
        private Light lamp;
        private Material surface;
        private Slider slider;
        private Text label;
        private bool changed;
        internal void Initialize(MainMenuHd2DStage stage,Light source,Material material,Font font)
        {
            lamp=source;surface=material;
            var rect=GetComponent<RectTransform>();
            rect.sizeDelta=new Vector2(300,104);
            rect.position=stage.transform.TransformPoint(new Vector3(-3.25f,1.52f,-1.12f));
            rect.rotation=stage.transform.rotation*Quaternion.Euler(58,0,0);
            rect.localScale=Vector3.one*.0032f;
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            canvas.worldCamera=stage.sceneCamera;gameObject.AddComponent<Hd2DSceneGraphicRaycaster>();
            label=Child("晶灯亮度",rect,new Vector2(0,27),new Vector2(300,42)).gameObject.AddComponent<Text>();
            label.font=font;label.fontSize=24;label.alignment=TextAnchor.MiddleCenter;
            label.color=new Color(.98f,.90f,.71f);label.raycastTarget=false;
            var sliderRect=Child("晶灯亮度滑条",rect,new Vector2(0,-25),new Vector2(280,54));
            var hit=sliderRect.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var track=Child("亮度轨道",sliderRect,Vector2.zero,new Vector2(260,8)).gameObject.AddComponent<Image>();
            track.color=new Color(.43f,.35f,.255f);track.raycastTarget=false;
            var handleArea=Child("亮度手柄范围",sliderRect,Vector2.zero,new Vector2(260,42));
            // Slider stretches its handle on the vertical axis; zero extra height avoids doubling it.
            var handle=Child("亮度手柄",handleArea,Vector2.zero,new Vector2(24,0)).gameObject.AddComponent<Image>();
            handle.color=new Color(.96f,.85f,.57f);
            slider=sliderRect.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
            slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey,.4f)));
            Apply(slider.value);slider.onValueChanged.AddListener(value=>{changed=true;Apply(value);});
            var trigger=sliderRect.gameObject.AddComponent<EventTrigger>();
            foreach(var kind in new[]{EventTriggerType.PointerUp,EventTriggerType.Deselect})
            {var entry=new EventTrigger.Entry{eventID=kind};entry.callback.AddListener(_=>Commit());trigger.triggers.Add(entry);}
            foreach(var child in GetComponentsInChildren<Transform>(true))child.gameObject.layer=stage.gameObject.layer;
        }
        private RectTransform Child(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var child=new GameObject(name,typeof(RectTransform));child.transform.SetParent(parent,false);
            var rect=child.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        private void Apply(float value)
        {
            lamp.intensity=3f*value;lamp.enabled=value>.001f;
            // Retain ambient visibility when off; avoid the source looking fully emissive.
            surface.color=Color.white*Mathf.Lerp(.65f,.90f,value);surface.color=new Color(surface.color.r,surface.color.g,surface.color.b,1);
            label.text=value<.001f?"晶灯 关闭":"晶灯 "+Mathf.RoundToInt(value*100)+"%";
        }
        private void Commit()
        {
            if(!changed||slider==null)return;
            PlayerPrefs.SetFloat(PreferenceKey,slider.value);PlayerPrefs.Save();changed=false;
        }
        private void OnDisable()=>Commit();
    }
}
