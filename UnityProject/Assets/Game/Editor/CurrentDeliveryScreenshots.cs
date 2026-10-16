using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    // Batch-only wrapper: the established live HUD capture owns entry/rendering.
    [InitializeOnLoad]
    public static class CurrentDeliveryScreenshots
    {
        private const string Key="OCC.CurrentDeliveryScreenshots";
        private const string CaptureKey="OCC.ManualBattleScreenshots20261013";
        static CurrentDeliveryScreenshots(){EditorApplication.update+=Tick;}
        public static void Begin()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Background Editor required");
            Hd2DManualBattleScreenshots.Begin("current_delivery");
            SessionState.SetBool(Key,true);
            SessionState.SetFloat(Key+".deadline",(float)EditorApplication.timeSinceStartup+180);
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Key,false))return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".deadline",0))
            {SessionState.SetBool(Key,false);EditorApplication.Exit(2);return;}
            if(SessionState.GetBool(CaptureKey,false)||EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool(Key,false);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-16_当前实机图"));
            EditorApplication.Exit(File.Exists(Path.Combine(folder,"capture.json"))?0:1);
        }
    }
}
