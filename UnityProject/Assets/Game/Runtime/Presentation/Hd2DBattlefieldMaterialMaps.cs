using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Candidate material fields are registered to exact admitted albedos, never
    // selected from tint, brightness or a partial name on an unrelated texture.
    public static class Hd2DBattlefieldMaterialMaps
    {
        public sealed class Entry
        {
            public readonly string Id,AlbedoResource,NormalResource,RoughnessResource;
            public readonly string Family;
            public readonly float NormalStrength;
            public readonly bool FlipGreen;
            internal Texture2D Albedo,Normal,Roughness;
            internal Entry(string family,char variant)
            {
                Family=family;
                Id="academy_block_"+family+"_"+variant;
                AlbedoResource="Art/FormalAcademyIndependentFloors32/"+Id;
                bool previous=family=="court"&&variant=='b';
                string root=previous?"HD2DMaterialData/CourtyardStone/":"HD2DMaterialData/Battlefield/"+Id+"/";
                NormalResource=root+"normal";RoughnessResource=root+"roughness";
                NormalStrength=family=="earth"?.22f:family=="road"?.46f:family=="ruin"?.48f:.50f;
                FlipGreen=previous;
            }
        }
        private static readonly List<Entry> entries=CreateEntries();
        public static IReadOnlyList<Entry> Entries=>entries;
        private static List<Entry> CreateEntries()
        {
            var result=new List<Entry>();
            foreach(string family in new[]{"court","earth","road","ruin"})
                for(char variant='a';variant<='d';variant++)result.Add(new Entry(family,variant));
            return result;
        }
        internal static Texture2D[] NativeVariants(Texture source,out bool stone)
        {
            stone=false;Entry matched=null;
            foreach(var entry in entries)
            {
                if(entry.Albedo==null)entry.Albedo=Resources.Load<Texture2D>(entry.AlbedoResource);
                if(source!=null&&source==entry.Albedo){matched=entry;break;}
            }
            if(matched==null)return Array.Empty<Texture2D>();
            stone=matched.Family!="earth";var variants=new List<Texture2D>();
            foreach(var entry in entries)
            {
                if(entry.Family!=matched.Family)continue;
                if(entry.Albedo==null)entry.Albedo=Resources.Load<Texture2D>(entry.AlbedoResource);
                if(entry.Albedo==null||entry.Albedo.width!=32||entry.Albedo.height!=32||entry.Albedo.filterMode!=FilterMode.Point)
                    throw new InvalidOperationException("Native surface variant unavailable: "+entry.Id);
                variants.Add(entry.Albedo);
            }
            return variants.ToArray();
        }
        public static bool Apply(Material material,Texture sourceAlbedo)
        {
            if(sourceAlbedo==null)return false;
#if UNITY_EDITOR
            if(Hd2DGroundMaterialReview.Current!=null&&Hd2DGroundMaterialReview.Current.Apply(material,sourceAlbedo))return true;
#endif
            foreach(var entry in entries)
            {
                if(entry.Albedo==null)entry.Albedo=Resources.Load<Texture2D>(entry.AlbedoResource);
                if(sourceAlbedo!=entry.Albedo)continue;
                if(material==null||!material.HasProperty("_BumpMap")||!material.HasProperty("_RoughnessMap"))
                    throw new InvalidOperationException("Battlefield material lacks paired map inputs: "+entry.Id);
                if(entry.Normal==null)entry.Normal=Resources.Load<Texture2D>(entry.NormalResource);
                if(entry.Roughness==null)entry.Roughness=Resources.Load<Texture2D>(entry.RoughnessResource);
                foreach(var map in new[]{entry.Normal,entry.Roughness})
                    if(map==null||map.width!=32||map.height!=32||map.filterMode!=FilterMode.Point)
                        throw new InvalidOperationException("Missing/invalid paired native32 material data: "+entry.Id);
                material.SetTexture("_BumpMap",entry.Normal);material.SetFloat("_BumpScale",entry.NormalStrength);
                material.SetTexture("_RoughnessMap",entry.Roughness);
                material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCC_ROUGHNESS_MAP");
                return true;
            }
            return false;
        }
        // Runtime native crops must carry the exact same normal/roughness region.
        // The copy callback owns its textures and preserves importer channel packing.
        internal static bool ApplyRegion(Material material,Texture sourceAlbedo,Func<Texture2D,Texture2D> copyRegion)
        {
            if(!Apply(material,sourceAlbedo))return false;
            foreach(string property in new[]{"_BumpMap","_RoughnessMap"})
                material.SetTexture(property,copyRegion((Texture2D)material.GetTexture(property)));
            return true;
        }
    }
}
