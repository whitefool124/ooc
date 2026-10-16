#if UNITY_EDITOR
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class BattlefieldFieldArtCandidates
    {
        public static bool ReviewEnabled {get;set;}
        public static Texture2D Resolve(string id)
        {
            if(!ReviewEnabled||!Known(id))return null;
            var texture=Resources.Load<Texture2D>(BattlefieldArtResources.Fields+id);
            if(texture==null)throw new System.InvalidOperationException("Missing field QA source: "+id);
            return texture;
        }
        public static Texture2D Frame(string id,int frame)
        {
            if(id!="field_smoke"&&id!="field_burning")return null;
            return Resolve(id+"_"+((frame%6+6)%6).ToString("00"));
        }
        public static bool Upright(Texture2D texture)=>BattlefieldArtResources.Upright(texture);
        private static bool Known(string id)
        {
            if(id=="field_loose_paper"||id=="field_trace"||id=="field_binding_mark"||id=="field_paper_screen")return true;
            for(int i=0;i<6;i++)if(id=="field_smoke_"+i.ToString("00")||id=="field_burning_"+i.ToString("00"))return true;
            return false;
        }
    }
}
#endif
