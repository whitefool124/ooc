using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>Reusable optical envelope. World units, HDR light energy and seconds are separate from sprite size.</summary>
    [CreateAssetMenu(menuName="OCC/VFX/Fire Light Recipe")]
    public sealed class Hd2DFireLightRecipe : ScriptableObject
    {
        public Color fireColour=new Color(1,.38f,.075f);
        [Min(0)] public float chamberEnergy=18, compressedEnergy=28, impactEnergy=95, afterglowEnergy=12;
        [Min(.1f)] public float range=9, impactDecay=5, afterglowDecay=1.25f;
        [Range(0,1)] public float daylightSuppression=.38f;
        [Min(0)] public float chargeStart=3, chargeFull=4.5f, compressionStart=5, compressionFull=7;
        [Min(0)] public float afterglowDuration=6.6f, daylightRecoveryStart=1.6f, daylightRecoveryEnd=5.6f;
        [Min(0)] public float chamberHeight=2.2f, contactHeight=.85f;
        public void Evaluate(float time,float impact,Light hero,Light key,Vector3 centre,float daylight)
        {
            float compression=Mathf.SmoothStep(0,1,Mathf.InverseLerp(compressionStart,compressionFull,time));
            float ramp=Mathf.SmoothStep(0,1,Mathf.InverseLerp(chargeStart,chargeFull,time));
            float after=time-impact;
            hero.transform.position=centre+Vector3.up*(after<0?chamberHeight:contactHeight);
            hero.color=fireColour;hero.range=range;
            hero.intensity=after<0?ramp*Mathf.Lerp(chamberEnergy,compressedEnergy,compression):
                impactEnergy*Mathf.Exp(-after*impactDecay)+afterglowEnergy*Mathf.Exp(-after*afterglowDecay);
            if(after>=afterglowDuration)hero.intensity=0;
            key.intensity=daylight*(1-daylightSuppression*ramp*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(impact+daylightRecoveryStart,impact+daylightRecoveryEnd,time))));
        }
    }
}
