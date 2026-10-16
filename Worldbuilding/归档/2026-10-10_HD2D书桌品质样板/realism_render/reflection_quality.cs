using UnityEngine;
public static class ReflectionQualityRead {public static string Run(){return Newtonsoft.Json.JsonConvert.SerializeObject(new {level=QualitySettings.GetQualityLevel(),names=QualitySettings.names,realtime=QualitySettings.realtimeReflectionProbes});}}
