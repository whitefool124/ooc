using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // A local material instance preserves the sprite, outline and UI stencil ownership.
    public sealed class OccItemSurface : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Image artwork;
        public Image Artwork =>artwork;
        private Material effect,original;
        private Func<float> intensity;
        private float entered=-10;
        public static OccItemSurface Attach(GameObject hitOwner,Image image,string definition,Func<float> animation)
        {
            if(image==null || image.sprite==null)return null;
            string kind=OccSoundEffects.InventoryMaterial(definition,null);
            if(kind!="metal" && kind!="glass" && kind!="leather" && kind!="cloth")return null;
            var component=hitOwner.GetComponent<OccItemSurface>();
            if(component==null)component=hitOwner.AddComponent<OccItemSurface>();
            component.Bind(image,kind,animation,definition=="ACA-EQ-HD01");return component;
        }
        private void Bind(Image image,string kind,Func<float> animation,bool goggles)
        {
            Release();artwork=image;original=image.material;intensity=animation;
            var shader=Shader.Find("OCC/UI/ItemSurface");if(shader==null)return;
            effect=new Material(shader){hideFlags=HideFlags.DontSave};
            effect.SetFloat("_Glass",goggles?1:0);
            effect.SetFloat("_Rough",kind=="cloth"?1:kind=="leather"?.65f:0);
            effect.SetFloat("_Phase",-2);artwork.material=effect;
        }
        public static void AttachByIcon(GameObject owner,Image icon,Func<float> animation)
        {
            if(icon==null || icon.sprite==null)return;
            string name=icon.sprite.name.ToUpperInvariant().Replace("_","-");
            if(name.StartsWith("ACA-EQ-"))Attach(owner,icon,name,animation);
        }
        public void OnPointerEnter(PointerEventData e){entered=Time.unscaledTime;}
        public void OnPointerExit(PointerEventData e){entered=-10;if(effect!=null)effect.SetFloat("_Phase",-2);}
        private void Update()
        {
            if(effect==null)return;
            float level=intensity==null?0:intensity();float age=Time.unscaledTime-entered;
            effect.SetFloat("_Phase",level>.01f && age<.55f?age/.55f:-2);
        }
        private void OnDisable(){if(effect!=null)effect.SetFloat("_Phase",-2);}
        private void OnDestroy()=>Release();
        private void Release(){if(artwork!=null)artwork.material=original;if(effect!=null)Destroy(effect);effect=null;}
    }
}
