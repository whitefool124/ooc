using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.EditorTools
{
    // Explicit local presentation fixtures. No simulation clock, weather rules or save state.
    public static class Hd2DBattleWeatherPreview
    {
        public static void Apply(Hd2DBattlefieldSample sample,CombatPrototypeBootstrap host,bool night)
        {
            if(!sample.TrySetAtmosphere(night?Hd2DBattleTimeOfDay.Night:Hd2DBattleTimeOfDay.Sunset,
                night?Hd2DBattleWeather.Rain:Hd2DBattleWeather.Clear,0))
                throw new InvalidOperationException("Battle atmosphere not ready");
        }
        public static void WriteEvidence(Hd2DBattlefieldSample sample,string folder,bool night)
        {
            var root=sample.RenderCamera.transform.parent;
            var rain=root.GetComponentInChildren<Hd2DWeatherRainPreview>();
            if(rain!=null)for(int i=0;i<8;i++)
            {rain.SetTime(i*.1f);Hd2DBattleCanvasCapture.Capture(sample,960,540,Path.Combine(folder,"rain_frame_"+i.ToString("00")+".png"));}
            File.WriteAllText(Path.Combine(folder,"weather_preview.json"),JsonConvert.SerializeObject(new{
                status="QA_PENDING",scope="Editor-only visual preview; same RainPrismCourt, camera and source assets",
                preset=night?"RainNight":"Sunset",gameplayWeather=false,simulationClock=false,savesModified=false,
                rain=night?"440 seeded, depth-tested world-space animated streak quads; no rain texture generated":"none",
                lights=root.GetComponentsInChildren<Light>().Select(l=>new{l.name,type=l.type.ToString(),color=new[]{l.color.r,l.color.g,l.color.b},l.intensity,l.range,euler=new[]{l.transform.localEulerAngles.x,l.transform.localEulerAngles.y,l.transform.localEulerAngles.z}}).ToArray(),
                limitations=new[]{"No weather gameplay, persistence or day cycle", "Rain preview has no roof occlusion or splashes", "Full density review and Player GPU budget pending"}
            },Formatting.Indented));
        }
    }
}
