using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace OCC.Combat.Presentation
{
 internal static class SkyFurnaceVoxelEmitter
 {
  public static ParticleSystem Create(Transform parent,int layer,string name,Vector3 origin,Mesh cube,Material material,uint seed,int maximum)
  {
   var node=new GameObject(name){layer=layer};node.transform.SetParent(parent,false);node.transform.position=origin;
   var ps=node.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=seed;
   var main=ps.main;main.loop=false;main.duration=16;main.playOnAwake=false;main.maxParticles=maximum;main.simulationSpace=ParticleSystemSimulationSpace.World;main.useUnscaledTime=true;main.startSize=1;main.startSpeed=0;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
   var emission=ps.emission;emission.rateOverTime=0;
   var renderer=node.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=cube;renderer.sharedMaterial=material;renderer.alignment=ParticleSystemRenderSpace.World;renderer.sortMode=ParticleSystemSortMode.Distance;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enableGPUInstancing=false;renderer.minParticleSize=0;renderer.maxParticleSize=1;
   renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color});return ps;
  }
  public static void Fade(ParticleSystem ps,float peak)
  {
   var colour=ps.colorOverLifetime;colour.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(peak,.06f),new GradientAlphaKey(peak,.55f),new GradientAlphaKey(0,1)});colour.color=g;
  }
  public static void Advance(ParticleSystem ps,float time,bool restart,float delta)
  {
   if(!restart){ps.Simulate(delta,false,false,false);return;}
   ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);float elapsed=0;bool first=true;
   while(elapsed<time){float step=Mathf.Min(1f/60,time-elapsed);ps.Simulate(step,false,first,false);elapsed+=step;first=false;}
   if(first)ps.Simulate(0,false,true,false);
  }
 }
 public sealed class SkyFurnaceSmokeParticles
 {
  public readonly ParticleSystem[] Systems=new ParticleSystem[2];
  public readonly ParticleSystemRenderer[] Renderers=new ParticleSystemRenderer[2];
  public readonly List<MeshRenderer> ImageRenderers=new List<MeshRenderer>();
  public const float CanvasMetres=1f/32,PixelMetres=1f/32;
  public int PeakParticles {get;private set;}
  public int FramesSeenMask=>0;
  public float PeakRadius {get;private set;}
  public float PeakHeight {get;private set;}
  public bool TextureFree=>!Renderers[0].sharedMaterial.HasProperty("_MainTex")&&!Renderers[1].sharedMaterial.HasProperty("_MainTex");
  public int Count=>Systems[0].particleCount+Systems[1].particleCount;
  private readonly ParticleSystem.Particle[] buffer=new ParticleSystem.Particle[144];
  private readonly Vector3 origin;private float last=-1;
  public SkyFurnaceSmokeParticles(Transform parent,int layer,Vector3 centre,Mesh cube,Material material,float impact)
  {
   origin=centre;
   for(int i=0;i<2;i++)
   {
    bool dust=i==0;var ps=SkyFurnaceVoxelEmitter.Create(parent,layer,dust?"无贴图烟尘立方粒子":"无贴图上升灰烬",centre+Vector3.up*(dust?.08f:.16f),cube,material,(uint)(7823+i*7919),144);Systems[i]=ps;ps.transform.rotation=Quaternion.Euler(-90,0,0);
    var main=ps.main;main.startLifetime=new ParticleSystem.MinMaxCurve(dust?.8f:1.2f,dust?1.25f:2.1f);main.startSpeed=new ParticleSystem.MinMaxCurve(dust?.42f:.3f,dust?.72f:.5f);main.startColor=dust?new Color(.58f,.38f,.20f,.9f):new Color(.26f,.29f,.33f,.9f);
    var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=dust?82:16;shape.radius=dust?.22f:.1f;shape.radiusThickness=1;
    var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=0;velocity.y=dust?.025f:.08f;velocity.z=0;velocity.radial=dust?.18f:.02f;
    var emission=ps.emission;emission.SetBursts(dust?new[]{new ParticleSystem.Burst(impact+.08f,56),new ParticleSystem.Burst(impact+.22f,24)}:new[]{new ParticleSystem.Burst(impact+.42f,28),new ParticleSystem.Burst(impact+.8f,14)});
    SkyFurnaceVoxelEmitter.Fade(ps,.85f);Renderers[i]=ps.GetComponent<ParticleSystemRenderer>();Renderers[i].sortingOrder=8;
   }
  }
  public int Fingerprint(){int hash=17;unchecked{foreach(var ps in Systems){int count=ps.GetParticles(buffer);hash=hash*31+count;for(int i=0;i<count;i++){hash=hash*31+buffer[i].position.GetHashCode();hash=hash*31+buffer[i].remainingLifetime.GetHashCode();}}}return hash;}
  public void Evaluate(float time)
  {
   bool restart=last<0||time<last||time-last>.25f;
   foreach(var ps in Systems){SkyFurnaceVoxelEmitter.Advance(ps,time,restart,Mathf.Max(0,time-last));int count=ps.GetParticles(buffer);for(int i=0;i<count;i++){var d=buffer[i].position-origin;PeakRadius=Mathf.Max(PeakRadius,new Vector2(d.x,d.z).magnitude+PixelMetres*.5f);PeakHeight=Mathf.Max(PeakHeight,d.y+PixelMetres*.5f);}}
   last=time;PeakParticles=Mathf.Max(PeakParticles,Count);
  }
  public void Clear(){foreach(var ps in Systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);last=-1;}
 }
}
