#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Processed archive candidates only. No importer/formal asset or Player changes.
    public sealed class Hd2DGroundMaterialReview : IDisposable
    {
        public static Hd2DGroundMaterialReview Current {get;set;}
        private readonly Texture2D[] variants;
        private readonly Texture2D normal,roughness;
        private readonly HashSet<Texture> original=new HashSet<Texture>();
        private bool disposed;
        public IReadOnlyList<Texture2D> Variants=>variants;
        public Hd2DGroundMaterialReview(Texture2D[] colours,Texture2D normalMap,Texture2D roughnessMap)
        {
            if(colours==null||colours.Length!=3)throw new ArgumentException("Three independent ground variants required");
            variants=(Texture2D[])colours.Clone();normal=normalMap;roughness=roughnessMap;
            foreach(var t in variants)Validate(t);Validate(normal);Validate(roughness);
            foreach(char v in new[]{'a','b','c','d'})
            {
                var t=Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_court_"+v);
                if(t==null)throw new InvalidOperationException("Missing original court texture");original.Add(t);
            }
        }
        private static void Validate(Texture2D t)
        {if(t==null||t.width!=32||t.height!=32||t.filterMode!=FilterMode.Point)throw new InvalidOperationException("Processed native32 point field required");}
        public bool Replaces(Texture source)=>!disposed&&original.Contains(source);
        public bool Contains(Texture source)=>!disposed&&Array.IndexOf(variants,source)>=0;
        public int VariantIndex(int x,int y)
        {
            unchecked{uint h=(uint)x*0x9e3779b1u^(uint)y*0x85ebca6bu^0x4f434347u;h^=h>>16;h*=0x7feb352du;h^=h>>15;return(int)(h%3);}
        }
        public Texture2D Resolve(Texture2D source,int x,int y)=>Replaces(source)?variants[VariantIndex(x,y)]:source;
        public bool Apply(Material material,Texture source)
        {
            if(!Contains(source))return false;
            if(material==null||!material.HasProperty("_BumpMap")||!material.HasProperty("_RoughnessMap"))
                throw new InvalidOperationException("Candidate ground surface needs paired data");
            material.SetTexture("_BumpMap",normal);material.SetTexture("_RoughnessMap",roughness);material.SetFloat("_BumpScale",.5f);
            material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCC_ROUGHNESS_MAP");return true;
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;if(Current==this)Current=null;
            var owned=new HashSet<Texture2D>(variants){normal,roughness};
            foreach(var t in owned)if(t!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(t);else UnityEngine.Object.DestroyImmediate(t);}
        }
    }
}
#endif
