using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Explicit Editor QA preview only. Authored formal assets and Player routes
    // retain their original albedos until a real replacement decision is made.
    public static class Hd2DBattleCrystalCandidates
    {
        public static bool ReviewEnabled {get;set;}
        private static readonly HashSet<string> ids=new HashSet<string>{"academy_aether_crystal_intact","academy_aether_crystal_damaged","academy_pressure_crystal_intact","academy_pressure_crystal_damaged"};
        private static readonly Dictionary<string,Texture2D> loaded=new Dictionary<string,Texture2D>();
        public static Texture2D Resolve(string id)
        {
#if UNITY_EDITOR
            if(!ReviewEnabled||!ids.Contains(id))return null;
            if(!loaded.TryGetValue(id,out var texture))
            {
                texture=Resources.Load<Texture2D>("Art/HD2DBattleCrystalCandidates/"+id);
                if(texture==null)throw new System.InvalidOperationException("Missing registered crystal QA candidate "+id);
                loaded.Add(id,texture);
            }
            return texture;
#else
            return null;
#endif
        }
    }
}
