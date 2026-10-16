using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public enum Hd2DCrystalAppearance { Original, Faceted }
    // Two exact source sets; original remains the default and neither is overwritten.
    // Faceted appearance is user-approved, while whole-scene quality remains pending.
    public static class Hd2DBattleCrystalCandidates
    {
        public static Hd2DCrystalAppearance Appearance {get;set;}=Hd2DCrystalAppearance.Original;
        public static bool ReviewEnabled {get;set;}
        private static readonly HashSet<string> ids=new HashSet<string>{"academy_aether_crystal_intact","academy_aether_crystal_damaged","academy_pressure_crystal_intact","academy_pressure_crystal_damaged"};
        private static readonly Dictionary<string,Texture2D> loaded=new Dictionary<string,Texture2D>();
        public static Texture2D Resolve(string id)
        {
            bool faceted=Appearance==Hd2DCrystalAppearance.Faceted;
#if UNITY_EDITOR
            faceted|=ReviewEnabled;
#endif
            if(!faceted||!ids.Contains(id))return null;
            if(!loaded.TryGetValue(id,out var texture))
            {
                texture=Resources.Load<Texture2D>("Art/HD2DBattleCrystalCandidates/"+id);
                if(texture==null)throw new System.InvalidOperationException("Missing registered crystal QA candidate "+id);
                loaded.Add(id,texture);
            }
            return texture;
        }
    }
}
