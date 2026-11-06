using UnityEngine;

// One local visual effect per world; bots and LAN replicas never create rain emitters.
public sealed class RainPresentation : MonoBehaviour {
 public DriveSession session;
 ParticleSystem drops;Material dropMaterial,wiperMaterial;LineRenderer[] wipers;
 Light sun;float drySun;Color dryAmbient,drySky;Camera view;bool wet;float phase;
 public bool WipersVisible {get{return wipers!=null&&wipers[0].enabled;}}
 public int ParticleCount {get{return drops?drops.particleCount:0;}}
 void Create(){
  view=session.drivingCamera.GetComponent<Camera>();drySky=view.backgroundColor;dryAmbient=RenderSettings.ambientLight;
  foreach(var l in FindObjectsOfType<Light>())if(l.type==LightType.Directional){sun=l;drySun=l.intensity;break;}
  var go=new GameObject("Local rain (bounded)");go.transform.SetParent(transform,false);drops=go.AddComponent<ParticleSystem>();drops.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=drops.main;main.loop=true;main.startLifetime=1.1f;main.startSpeed=22;main.startSize=.025f;main.maxParticles=900;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=new Color(.68f,.8f,.88f,.55f);main.playOnAwake=false;
  var shape=drops.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(30,30,.2f);
  var renderer=drops.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=4;renderer.velocityScale=.035f;
  dropMaterial=new Material(Resources.Load<Material>("RuntimeRain"));renderer.sharedMaterial=dropMaterial;
  wiperMaterial=new Material(Resources.Load<Material>("RuntimeWiper"));wipers=new LineRenderer[2];
  for(int i=0;i<2;i++){var blade=new GameObject("Automatic wiper "+i);blade.transform.SetParent(view.transform,false);blade.layer=10;var line=blade.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;line.startWidth=line.endWidth=.009f;line.sharedMaterial=wiperMaterial;line.startColor=line.endColor=new Color(.08f,.09f,.1f,1);line.enabled=false;wipers[i]=line;}
 }
 void LateUpdate(){
  if(!session||!session.drivingCamera||session.IsSimulationBot)return;
  if(!drops)Create();bool enabled=RainPolicy.Enabled;
  if(enabled!=wet){wet=enabled;RenderSettings.ambientLight=wet?Color.Lerp(dryAmbient,new Color(.46f,.52f,.59f),.35f):dryAmbient;if(sun)sun.intensity=wet?drySun*.75f:drySun;view.backgroundColor=wet?new Color(.46f,.53f,.59f):drySky;view.clearFlags=wet?CameraClearFlags.SolidColor:CameraClearFlags.Skybox;}
  drops.transform.SetPositionAndRotation(session.taxi.transform.position+Vector3.up*16,Quaternion.Euler(90,0,0));
  var emission=drops.emission;emission.rateOverTime=700*RainPolicy.Intensity;
  if(enabled&&RainPolicy.Intensity>0){if(Time.timeScale==0){if(drops.isPlaying)drops.Pause();}else if(!drops.isPlaying)drops.Play();}else if(!drops.isStopped)drops.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  phase+=Time.deltaTime/RainPolicy.WiperSeconds;
  bool visible=enabled&&session.drivingCamera.FirstPerson&&!session.drivingCamera.RearView&&Mathf.Abs(session.drivingCamera.LookYaw)<35;
  for(int i=0;i<2;i++){wipers[i].enabled=visible;if(!visible)continue;float angle=Mathf.Lerp(20,145,.5f-.5f*Mathf.Cos(phase*Mathf.PI*2))*Mathf.Deg2Rad;Vector3 origin=new Vector3(i==0?-.25f:.25f,-.42f,.9f);wipers[i].SetPosition(0,origin);wipers[i].SetPosition(1,origin+new Vector3(Mathf.Cos(angle)*.48f,Mathf.Sin(angle)*.48f,0));}
 }
 void OnDestroy(){if(wet){RenderSettings.ambientLight=dryAmbient;if(sun)sun.intensity=drySun;}if(drops)Destroy(drops.gameObject);if(wipers!=null)foreach(var w in wipers)if(w)Destroy(w.gameObject);if(dropMaterial)Destroy(dropMaterial);if(wiperMaterial)Destroy(wiperMaterial);}
}
