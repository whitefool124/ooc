using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Numerical light data aligned to an unchanged source, never new albedo.
    // Bind each animation source separately; a different source is not relabelled.
    public sealed class Hd2DPixelEmission : MonoBehaviour
    {
        public readonly struct Pixel
        {
            public readonly Vector2Int Texel;
            public readonly Vector3 LinearGain;
            public Pixel(int x,int y,Vector3 linearGain)
            {Texel=new Vector2Int(x,y);LinearGain=linearGain;}
        }
        private Renderer receiver;
        private Texture2D source;
        private GraphicsBuffer field;
        public static bool Supports(Material material)=>material!=null&&
            (material.shader.name=="OCC/HD2D/GroundedSprite"||material.shader.name=="OCC/HD2D/BakedArchitecture"||material.shader.name=="OCC/HD2D/WetCourtyard");
        private Material original,owned;
        private readonly System.Collections.Generic.List<Light> lights=new System.Collections.Generic.List<Light>();
        public int PixelCount {get;private set;}
        public bool EmissionEnabled {get;set;}=true;
        public int LightCount=>lights.Count;
        public void Initialize(Renderer target,Texture2D registeredSource,Pixel[] pixels)
        {
            if(owned!=null)throw new InvalidOperationException("Pixel emission already bound");
            if(target==null||registeredSource==null||pixels==null||pixels.Length==0)
                throw new ArgumentException("Renderer, exact source and explicit texels required");
            var material=target.sharedMaterial;
            if(!Supports(material)||material.mainTexture!=registeredSource)
                throw new ArgumentException("Registered source and supported OCC primary material required");
            var values=new Vector4[registeredSource.width*registeredSource.height];
            var used=new System.Collections.Generic.HashSet<Vector2Int>();
            foreach(var pixel in pixels)
            {
                var p=pixel.Texel;var r=pixel.LinearGain;
                if(p.x<0||p.y<0||p.x>=registeredSource.width||p.y>=registeredSource.height||!used.Add(p)||
                    float.IsNaN(r.x+r.y+r.z)||float.IsInfinity(r.x+r.y+r.z)||r.x<0||r.y<0||r.z<0||Mathf.Max(r.x,r.y,r.z)>32)
                    throw new ArgumentException("Unique in-bounds texel and finite linear gain 0..32 required");
                values[p.y*registeredSource.width+p.x]=new Vector4(r.x,r.y,r.z,1);
            }
            receiver=target;source=registeredSource;original=material;PixelCount=pixels.Length;
            field=new GraphicsBuffer(GraphicsBuffer.Target.Structured,values.Length,16);
            field.SetData(values);
            owned=new Material(original){name=original.name+" / owned pixel emission"};
            Bind();
        }
        public Light AddLightGroup(Vector3 localPosition,Color colour,float intensity,float range)
        {
            if(owned==null||lights.Count>=4||intensity<0||intensity>4||range<=0||range>3)
                throw new ArgumentException("Bound emitter, at most four groups, intensity 0..4 and range 0..3m required");
            var go=new GameObject("Pixel emission light group",typeof(Light)){layer=receiver.gameObject.layer};
            go.transform.SetParent(transform,false);go.transform.localPosition=localPosition;
            var light=go.GetComponent<Light>();light.type=LightType.Point;light.color=colour;
            light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;
            light.cullingMask=1<<receiver.gameObject.layer;lights.Add(light);return light;
        }
        private void LateUpdate()=>RefreshBinding();
        public void RefreshBinding()
        {
            if(receiver==null||owned==null)return;
            var current=receiver.sharedMaterial;
            bool matching=Supports(current)&&current.mainTexture==source;
            if(EmissionEnabled)owned.EnableKeyword("_OCC_PIXEL_EMISSION");else owned.DisableKeyword("_OCC_PIXEL_EMISSION");
            foreach(var light in lights)if(light!=null)light.enabled=EmissionEnabled&&matching&&receiver.enabled&&receiver.gameObject.activeInHierarchy;
            if(matching&&current!=owned){original=current;owned.CopyPropertiesFromMaterial(current);Bind();}
        }
        private void Bind()
        {owned.SetBuffer("_PixelEmissionBuffer",field);if(EmissionEnabled)owned.EnableKeyword("_OCC_PIXEL_EMISSION");else owned.DisableKeyword("_OCC_PIXEL_EMISSION");receiver.sharedMaterial=owned;}
        private void OnDisable(){foreach(var light in lights)if(light!=null)light.enabled=false;}
        private void OnDestroy()
        {
            if(receiver!=null&&receiver.sharedMaterial==owned)receiver.sharedMaterial=original;
            foreach(var light in lights)if(light!=null)Destroy(light.gameObject);
            if(owned!=null)Destroy(owned);if(field!=null)field.Dispose();
        }
    }
}
