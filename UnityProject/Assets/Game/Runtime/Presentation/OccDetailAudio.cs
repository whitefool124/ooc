using UnityEngine;

namespace OCC.Combat.Presentation
{
    // All room identity and audition voices belong to the presentation host, not rebuilt pages.
    public sealed class OccDetailAudio : MonoBehaviour
    {
        private AudioSource audition;
        private AudioSource atmosphere;
        private AudioSource fadingAtmosphere;
        private float fadingGain;
        private AudioLowPassFilter roomFilter, fadingFilter;
        private string room = "";
        private float previewUntil = -1, importantUntil = -1, ambienceGain;
        public string Room => room;
        public bool AuditionPlaying => audition != null && audition.isPlaying;
        public static float PriorityGain { get; private set; } = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => PriorityGain = 1;
        public static OccDetailAudio For(IRogueliteUiHost source)
        {
            var host=source as CombatPrototypeBootstrap;
            if(host==null || !Application.isPlaying)return null;
            var existing=host.GetComponent<OccDetailAudio>();
            return existing!=null?existing:host.gameObject.AddComponent<OccDetailAudio>();
        }
        private void Awake()
        {
            audition=Voice("音量试听");atmosphere=Voice("空间环境声");
            fadingAtmosphere=Voice("环境声交接");fadingAtmosphere.loop=true;fadingFilter=fadingAtmosphere.gameObject.AddComponent<AudioLowPassFilter>();
            atmosphere.loop=true;roomFilter=atmosphere.gameObject.AddComponent<AudioLowPassFilter>();
            OccSoundEffects.Played+=OnSound;
        }
        private AudioSource Voice(string name)
        {
            var child=new GameObject(name);child.transform.SetParent(transform,false);
            var voice=child.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;return voice;
        }
        public void SetRoom(string next)
        {
            next=next??"";if(next==room)return;
            StopAudition();
            if(room=="shop" && next=="outdoor")OccSoundEffects.Detail("shop_exit",.10f);
            room=next;
            if(next=="shop" || next=="workshop" || next=="medical")OccSoundEffects.Detail("room_"+next,.14f);
            AudioClip clip=next=="medical" || string.IsNullOrEmpty(next)?null:Resources.Load<AudioClip>("Audio/OCC/Ambience/"+(next=="outdoor"?"wind":"room"));
            fadingFilter.cutoffFrequency=roomFilter.cutoffFrequency;
            roomFilter.cutoffFrequency=next=="outdoor"?7500:1600;
            if(atmosphere.clip!=clip)
            {
                fadingAtmosphere.Stop();fadingAtmosphere.clip=atmosphere.clip;fadingAtmosphere.timeSamples=atmosphere.clip!=null?atmosphere.timeSamples:0;
                fadingGain=ambienceGain;fadingAtmosphere.volume=fadingGain;
                if(fadingAtmosphere.clip!=null && fadingGain>0)fadingAtmosphere.Play();
                atmosphere.Stop();atmosphere.clip=clip;ambienceGain=0;
            }
            if(clip!=null && !string.IsNullOrEmpty(next) && !atmosphere.isPlaying)atmosphere.Play();
        }
        public void Preview(float volume)
        {
            StopAudition(true);if(volume<=0)return;
            audition.clip=Resources.Load<AudioClip>("Audio/OCC/SFX/Foley/preview");
            if(audition.clip==null)return;
            audition.volume=.30f;audition.Play();previewUntil=Time.unscaledTime+.32f;
        }
        public void StopAudition(bool immediate=false)
        {
            if(audition==null || !audition.isPlaying){previewUntil=-1;return;}
            if(immediate){audition.Stop();previewUntil=-1;}
            else previewUntil=Mathf.Min(previewUntil>0?previewUntil:float.MaxValue,Time.unscaledTime+.05f);
        }
        private void OnSound(string path)
        {
            if(path.EndsWith("/coins") || path.EndsWith("/forge") || path.EndsWith("/meal") || path.Contains("/meal_") ||
               path.EndsWith("/practicum_complete") || path.EndsWith("/archive_reward"))
                importantUntil=Time.unscaledTime+.65f;
        }
        private void Update()
        {
            PriorityGain=Mathf.MoveTowards(PriorityGain,Time.unscaledTime<importantUntil?.72f:1f,Time.unscaledDeltaTime*3);
            if(audition.isPlaying && previewUntil>0)
            {
                audition.volume=.30f*Mathf.Clamp01((previewUntil-Time.unscaledTime)/.10f);
                if(Time.unscaledTime>=previewUntil)StopAudition(true);
            }
            float target=string.IsNullOrEmpty(room) || room=="medical" || AudioListener.volume<=0?0:(room=="outdoor"?.10f:.04f);
            ambienceGain=Mathf.MoveTowards(ambienceGain,target,Time.unscaledDeltaTime*.12f);
            atmosphere.volume=ambienceGain*PriorityGain;
            fadingGain=Mathf.MoveTowards(fadingGain,0,Time.unscaledDeltaTime*.12f);
            fadingAtmosphere.volume=fadingGain*PriorityGain;
            if(fadingGain==0 && fadingAtmosphere.isPlaying)fadingAtmosphere.Stop();
            if(target==0 && ambienceGain==0 && atmosphere.isPlaying)atmosphere.Stop();
            else if(target>0 && atmosphere.clip!=null && !atmosphere.isPlaying)atmosphere.Play();
        }
        private void OnDisable(){StopAudition(true);if(atmosphere!=null)atmosphere.Stop();if(fadingAtmosphere!=null)fadingAtmosphere.Stop();PriorityGain=1;}
        private void OnDestroy(){OccSoundEffects.Played-=OnSound;PriorityGain=1;}
    }
}
