using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static class AudioExpansionVerification
    {
        private static readonly Dictionary<string,int> Counts=new Dictionary<string,int>();
        private static readonly HashSet<string> Heard = new HashSet<string>();
        private static string ledgerFolder;
        private static EventInfo played;
        private static readonly Action<string> Listener = Record;
        [MenuItem("OCC/Audio/Import Foley")]
        public static void Import()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets" || Application.isPlaying)
                throw new InvalidOperationException("Main project Edit Mode required");
            const string folder="Assets/Resources/Audio/OCC/SFX/Foley";
            AssetDatabase.Refresh();
            int count=0;
            foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{folder}))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.forceToMono=true;importer.loadInBackground=false;
                var settings=importer.defaultSampleSettings;
                settings.preloadAudioData=true;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();count++;
            }
            Debug.Log("OCC Foley imported: "+count+" mono PCM clips");
        }
        public static void StartLedger(string folder)
        {
            Heard.Clear();Counts.Clear();ledgerFolder=folder;Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"playback-ledger.txt"),"Successful AudioSource.PlayOneShot dispatches; not an acoustic recording.\n");
            played=typeof(InventoryLiftDetail).Assembly.GetType("OCC.Combat.Presentation.OccSoundEffects").GetEvent("Played",BindingFlags.Static|BindingFlags.Public);
            played.RemoveEventHandler(null,Listener);played.AddEventHandler(null,Listener);
        }
        private static void Record(string path)
        {
            string name=Path.GetFileName(path);Heard.Add(name);Counts[name]=Count(name)+1;
            File.AppendAllText(Path.Combine(ledgerFolder,"playback-ledger.txt"),Time.unscaledTime.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" "+path+"\n");
        }
        public static int Count(string name)=>Counts.TryGetValue(name,out int count)?count:0;
        public static void RequireClip(string name)
        {
            if(!Heard.Contains(name))throw new InvalidOperationException("Expected actual playback missing: "+name);
        }
    }
}
