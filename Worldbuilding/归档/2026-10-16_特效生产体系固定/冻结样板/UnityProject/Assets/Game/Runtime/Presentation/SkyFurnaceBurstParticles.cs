using UnityEngine;
namespace OCC.Combat.Presentation
{
 public sealed class SkyFurnaceBurstParticles
 {
  public readonly ParticleSystem[] Systems=new ParticleSystem[2];
  public readonly ParticleSystemRenderer[] Renderers=new ParticleSystemRenderer[2];
  public int Count=>Systems[0].particleCount+Systems[1].particleCount;
  public int PeakParticles {get;private set;}
  public float PeakRadius {get;private set;}
  public float PeakHeight {get;private set;}
  public bool TextureFree=>!Renderers[0].sharedMaterial.HasProperty("_MainTex")&&!Renderers[1].sharedMaterial.HasProperty("_MainTex");
  private readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[256];private readonly Vector3 origin;private float last=-1;
  public SkyFurnaceBurstParticles(Transform parent,int layer,Vector3 centre,Mesh cube,Material material,float impact)
  {
   origin=centre;
   for(int i=0;i<2;i++)
   {
    bool wave=i==0;var ps=SkyFurnaceVoxelEmitter.Create(parent,layer,wave?"贴地冲击环 · 无贴图立方粒子":"径向爆发火星 · 无贴图立方粒子",centre+Vector3.up*(wave?.07f:.45f),cube,material,(uint)(68131+i*7793),256);Systems[i]=ps;
    var main=ps.main;main.startLifetime=wave?new ParticleSystem.MinMaxCurve(.40f):new ParticleSystem.MinMaxCurve(.6f,1.1f);main.startSpeed=wave?new ParticleSystem.MinMaxCurve(0):new ParticleSystem.MinMaxCurve(1.3f,2.5f);main.startColor=wave?new Color(1,.8f,.28f,1):new Color(1,.46f,.06f,1);main.gravityModifier=wave?0:.22f;
    var shape=ps.shape;shape.enabled=true;
    if(wave){ps.transform.rotation=Quaternion.Euler(90,0,0);shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.18f;shape.radiusThickness=0;shape.arcMode=ParticleSystemShapeMultiModeValue.BurstSpread;var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=0;velocity.y=0;velocity.z=0;velocity.radial=6.75f;}
    else{ps.transform.rotation=Quaternion.Euler(-90,0,0);shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=63;shape.radius=.1f;}
    var emission=ps.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(impact+(wave?.045f:.02f),(short)(wave?192:48))});
    Renderers[i]=ps.GetComponent<ParticleSystemRenderer>();Renderers[i].sortingOrder=6;
   }
  }
  public void Evaluate(float time)
  {
   bool restart=last<0||time<last||time-last>.25f;
   foreach(var ps in Systems){SkyFurnaceVoxelEmitter.Advance(ps,time,restart,Mathf.Max(0,time-last));int count=ps.GetParticles(buffer);for(int i=0;i<count;i++){var d=buffer[i].position-origin;PeakRadius=Mathf.Max(PeakRadius,new Vector2(d.x,d.z).magnitude+1f/64);PeakHeight=Mathf.Max(PeakHeight,d.y+1f/64);}}
   last=time;PeakParticles=Mathf.Max(PeakParticles,Count);
  }
  public int Fingerprint(){int hash=17;unchecked{foreach(var ps in Systems){int count=ps.GetParticles(buffer);hash=hash*31+count;for(int i=0;i<count;i++){hash=hash*31+buffer[i].position.GetHashCode();hash=hash*31+buffer[i].remainingLifetime.GetHashCode();}}}return hash;}
  public void Clear(){foreach(var ps in Systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);last=-1;}
 }
}
