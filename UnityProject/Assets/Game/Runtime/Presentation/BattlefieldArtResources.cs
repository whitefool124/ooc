using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Shared by Editor and player builds. Adoption never depends on review switches.
    public static class BattlefieldArtResources
    {
        public const string Fields="Art/FormalBattlefieldFields32/";
        public const string Devices="Art/FormalBattlefieldDevices32/";
        private static readonly Dictionary<string,Texture2D> Cache=new Dictionary<string,Texture2D>();
        public static Texture2D Load(string path)
        {
            if(Cache.TryGetValue(path,out var texture)&&texture!=null)return texture;
            texture=Resources.Load<Texture2D>(path);
            if(texture==null)throw new InvalidOperationException("Missing adopted battlefield artwork: "+path);
            Cache[path]=texture;return texture;
        }
        public static bool Upright(Texture2D texture)=>texture!=null&&
            (texture.name=="field_paper_screen"||texture.name.StartsWith("field_smoke_",StringComparison.Ordinal)||texture.name.StartsWith("field_burning_",StringComparison.Ordinal));
        public static string GroundEffectId(TileState tile)=>tile?.IsLoosePaper==true?"field_loose_paper":tile?.HasPaperScreen==true?"field_paper_screen":
            tile?.IsBindingMark==true?"field_binding_mark":tile?.HasTrace==true?"field_trace":null;
        public static string DeviceId(TileState tile)=>tile==null||tile.IsDestroyed?null:tile.IsTowerMechanism?"device_tower_mechanism":
            tile.IsOverloadDevice?"device_overload":tile.IsWardGenerator?"device_ward_generator":null;
    }
}
