#if UNITY_EDITOR
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Explicit Editor review only. QA artwork never replaces the release lookup.
    public static class EnemyUnitArtCandidates
    {
        public static bool ReviewEnabled {get;set;}
        public static Texture2D Resolve(string unitId)
        {
            if(!ReviewEnabled||unitId!="signal_keeper")return null;
            var texture=Resources.Load<Texture2D>("Art/EnemyUnitCandidates32x64/signal_keeper");
            if(texture==null)throw new System.InvalidOperationException("Missing signal keeper QA battle sprite");
            return texture;
        }
    }
}
#endif
