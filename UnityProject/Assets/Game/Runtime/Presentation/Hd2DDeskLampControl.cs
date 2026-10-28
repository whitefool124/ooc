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
        private System.Action reflectionChanged;
        internal void Initialize(MainMenuHd2DStage stage,Light source,Material material,Font font,System.Action onReflectionChanged)
        {
            lamp=source;surface=material;
            reflectionChanged=onReflectionChanged;
            var canvas=GetComponent<Canvas>();
            slider=GetComponentInChildren<Slider>(true);
            var labelTransform=transform.Find("晶灯亮度");
            label=labelTransform==null?null:labelTransform.GetComponent<Text>();
            if(canvas==null||slider==null||label==null)throw new System.InvalidOperationException("Missing authored lamp slider or label");
            canvas.worldCamera=stage.sceneCamera;label.font=font;
            slider.onValueChanged.RemoveAllListeners();
            slider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey,.4f)));
            Apply(slider.value);slider.onValueChanged.AddListener(value=>{changed=true;Apply(value);});
            var trigger=slider.GetComponent<EventTrigger>();
            if(trigger==null)throw new System.InvalidOperationException("Missing authored lamp input trigger");
            trigger.triggers.Clear();
            foreach(var kind in new[]{EventTriggerType.PointerUp,EventTriggerType.Deselect})
            {var entry=new EventTrigger.Entry{eventID=kind};entry.callback.AddListener(_=>Commit());trigger.triggers.Add(entry);}
            foreach(var child in GetComponentsInChildren<Transform>(true))child.gameObject.layer=stage.gameObject.layer;
        }
        private void Apply(float value)
        {
            lamp.intensity=6f*value;lamp.enabled=value>.001f;
            // Only the crystal emits. Brass remains a reflective, normally lit frame.
            surface.color=Color.white;
            if(surface.HasProperty("_EmissionStrength"))surface.SetFloat("_EmissionStrength",10f*value);
            label.text=value<.001f?"晶灯 关闭":"晶灯 "+Mathf.RoundToInt(value*100)+"%";
            reflectionChanged?.Invoke();
        }
        private void Commit()
        {
            if(!changed||slider==null)return;
            PlayerPrefs.SetFloat(PreferenceKey,slider.value);PlayerPrefs.Save();changed=false;
        }
        private void OnDisable()=>Commit();
    }
}
