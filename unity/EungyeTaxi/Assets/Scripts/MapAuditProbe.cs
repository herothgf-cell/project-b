using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Opt-in district audit (-mapAudit [-mapAuditOutput <folder>]). Moves the observation point
// junction by junction, lets the ordinary NPC/pedestrian population run, records stuck cars,
// red-light entries, overlaps and pedestrian near misses, and captures fixed overhead/oblique
// views. Observation only: no map, save or settings changes. Not a human or physical-driving test.
public sealed class MapAuditProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-mapAudit")>=0)new GameObject("Map audit probe").AddComponent<MapAuditProbe>();}
 sealed class Track {public float travel,lastMove,lastDistance,redSince=-1;public TrafficRoute route;public bool stuckReported;public TrafficStop watched;}
 readonly Dictionary<TrafficCar,Track> tracks=new Dictionary<TrafficCar,Track>();
 readonly HashSet<string> overlapPairs=new HashSet<string>();
 readonly StringBuilder report=new StringBuilder();
 DriveSession session;string output;Camera lens;
 int redStraight,redLeft,lateRed,yellowEntries,greenEntries,stuck,overlaps,nearMisses,jaywalkers;

 static string Arg(string name,string fallback){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);return i>=0&&i+1<a.Length?a[i+1]:fallback;}
 static float Real(){return Time.realtimeSinceStartup;}

 IEnumerator Start(){
  output=Path.GetFullPath(Arg("-mapAuditOutput","artifacts/map-audit"));Directory.CreateDirectory(output);
  float t0=Real();
  while(!(session=FindObjectOfType<DriveSession>())||!session.StartupReady){if(Real()-t0>180){Finish("startup deadline");yield break;}yield return null;}
  yield return null;yield return null;
  session.business.preparing=false;session.paused=false;
  var taxi=session.taxi;taxi.automated=true;taxi.autoBrake=true;
  foreach(var c in taxi.GetComponentsInChildren<Collider>(true))c.enabled=false;
  foreach(var r in taxi.GetComponentsInChildren<Renderer>(true))r.enabled=false;
  if(taxi.body){taxi.body.velocity=Vector3.zero;taxi.body.isKinematic=true;}
  // Hide HUD layers so captures show the road only.
  foreach(var b in session.GetComponents<MonoBehaviour>()){var n=b.GetType().Name;if(b!=session&&(n.Contains("HUD")||n.EndsWith("UI")||n.Contains("Readability")||n.Contains("Hud")))b.enabled=false;}
  foreach(var canvas in FindObjectsOfType<Canvas>())canvas.enabled=false;
  var driving=session.drivingCamera;driving.enabled=false;lens=driving.GetComponent<Camera>();
  Screen.SetResolution(1600,900,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
  var junctions=session.traffic.junctions.Where(j=>j!=null).ToList();
  string layerList=Arg("-mapAuditLayers",null);
  if(layerList!=null){Layers(junctions,layerList.Split('|'));Finish(null);yield break;}
  int limit=int.Parse(Arg("-mapAuditLimit","999")),first=int.Parse(Arg("-mapAuditStart","1"));float settle=float.Parse(Arg("-mapAuditSettle","25")),observe=float.Parse(Arg("-mapAuditObserve","35"));
  int pedTotal=session.ambientPedestrians!=null?session.ambientPedestrians.actors.Count:-1,pedActive=session.ambientPedestrians!=null?session.ambientPedestrians.actors.Count(p=>p&&p.gameObject.activeInHierarchy):-1;
  report.AppendLine("MAP_AUDIT pedestrianActors="+pedTotal+" active="+pedActive+" junctions="+junctions.Count+" settle="+settle+" observe="+observe+" population="+session.traffic.cars.Count+" observationOnly=true humanPlay=false");
  int index=0;
  foreach(var j in junctions){
   if(index>=limit)break;index++;if(index<first)continue;
   var centre=j.center;
   taxi.transform.position=centre+Vector3.up*.65f;if(taxi.body)taxi.body.position=taxi.transform.position;Physics.SyncTransforms();
   tracks.Clear();
   Time.timeScale=6;float start=Time.time;
   while(Time.time-start<settle)yield return null;
   int beforeRed=redStraight+redLeft,beforeStuck=stuck,beforeOverlap=overlaps,beforeNear=nearMisses;int peds=0,crossing=0,jay=0;
   start=Time.time;
   while(Time.time-start<observe){Observe(j,centre);yield return null;}
   if(session.ambientPedestrians!=null)foreach(var p in session.ambientPedestrians.actors){if(!p||!p.gameObject.activeInHierarchy||(p.transform.position-centre).sqrMagnitude>70*70)continue;peds++;if(p.Crossing)crossing++;if(p.state!=null&&p.state.jaywalking){jay++;jaywalkers++;}}
   int cars=session.traffic.cars.Count(c=>c&&c.enabled&&c.Visible&&(c.transform.position-centre).sqrMagnitude<70*70);
   Time.timeScale=0;
   string tag=index.ToString("000")+"-"+string.Concat(j.name.Select(ch=>Path.GetInvalidFileNameChars().Contains(ch)?'_':ch));
   lens.fieldOfView=60;lens.transform.position=centre+Vector3.up*75;lens.transform.rotation=Quaternion.Euler(90,0,0);
   yield return Capture(tag+"-top");
   var axis=j.direction.sqrMagnitude>.1f?j.direction.normalized:Vector3.forward;
   lens.fieldOfView=55;lens.transform.position=centre-axis*38+Vector3.up*16;lens.transform.LookAt(centre);
   yield return Capture(tag+"-oblique");
   report.AppendLine("JUNCTION "+tag+" pos="+centre.ToString("F0")+" signalized="+j.signalized+" cars="+cars+" pedestrians="+peds+" crossing="+crossing+" jaywalking="+jay+" redEntries="+(redStraight+redLeft-beforeRed)+" stuck="+(stuck-beforeStuck)+" overlaps="+(overlaps-beforeOverlap)+" pedNearMiss="+(nearMisses-beforeNear));
   File.WriteAllText(Path.Combine(output,"map-audit.txt"),report.ToString());
  }
  Finish(null);
 }

 void Observe(TrafficJunction junction,Vector3 centre){
  var traffic=session.traffic;var visible=new List<TrafficCar>();
  foreach(var car in traffic.cars){
   if(!car||!car.enabled||!car.Visible)continue;visible.Add(car);
   Track t;if(!tracks.TryGetValue(car,out t)){t=new Track{travel=car.totalTravel,lastMove=Time.time,lastDistance=car.distance,route=car.route};tracks.Add(car,t);continue;}
   if(car.route!=t.route){t.route=car.route;t.lastDistance=car.distance;t.travel=car.totalTravel;t.lastMove=Time.time;t.stuckReported=false;continue;}
   // Signal entry, judged with the same movement signal the NPC itself uses (editor program
   // per lane, otherwise MovementSignal incl. authored permissive left). A red that began less
   // than 1.5 s before the crossing is counted separately as a late (dilemma-zone) entry.
   if(car.route!=null&&car.route.stops!=null)foreach(var stop in car.route.stops){
    if(stop==null||stop.junction==null||!stop.junction.signalized||stop.junction.facility==null||stop.junction.facility.signalMode<=0)continue;
    int turn=stop.Movement(car.route);if(turn>0||turn==UturnMovement.Turn)continue;
    bool approaching=car.distance>=stop.distance-25&&car.distance<stop.distance;bool crossed=t.lastDistance<stop.distance&&car.distance>=stop.distance;
    if(!approaching&&!crossed)continue;
    var color=EntrySignal(car,stop,turn);
    if(approaching){if(t.watched!=stop){t.watched=stop;t.redSince=-1;}if(color==SignalColor.Red){if(t.redSince<0)t.redSince=Time.time;}else t.redSince=-1;continue;}
    int sign=traffic.Sign(car.route.Tangent(stop.distance),stop.axis,stop.junction);
    if(color==SignalColor.Red){bool late=t.watched!=stop||t.redSince<0||Time.time-t.redSince<1.5f;if(late)lateRed++;else if(turn<0)redLeft++;else redStraight++;report.AppendLine((late?"LATE_RED ":"RED_ENTRY ")+(turn<0?"left":"straight")+" junction="+stop.junction.name+" axis="+stop.axis+" sign="+sign+" car="+car.name+" speed="+car.speed.ToString("F1")+" redFor="+(t.redSince<0?-1:Time.time-t.redSince).ToString("F1")+" editor="+(stop.junction.editorProgram!=null)+" leftMode="+stop.junction.leftTurnMode);}
    else if(color==SignalColor.Yellow)yellowEntries++;else greenEntries++;
    t.watched=null;t.redSince=-1;
   }
   if(Mathf.Abs(car.totalTravel-t.travel)>.15f){t.travel=car.totalTravel;t.lastMove=Time.time;}
   else if(!t.stuckReported&&Time.time-t.lastMove>90){t.stuckReported=true;stuck++;report.AppendLine("STUCK car="+car.name+" pos="+car.transform.position.ToString("F1")+" near="+junction.name+" signal="+car.waitingForSignal+" obstacle="+car.waitingForObstacle+" junctionWait="+car.waitingForJunction+" merge="+car.waitingForMerge+" blocked="+car.blockedName+" mergeStatus="+car.mergeStatus);}
   t.lastDistance=car.distance;
  }
  for(int a=0;a<visible.Count;a++)for(int b=a+1;b<visible.Count;b++){
   var d=visible[a].transform.position-visible[b].transform.position;d.y=0;if(d.sqrMagnitude>2.0f*2.0f)continue;
   string key=Mathf.Min(visible[a].GetInstanceID(),visible[b].GetInstanceID())+":"+Mathf.Max(visible[a].GetInstanceID(),visible[b].GetInstanceID());
   if(overlapPairs.Add(key)){overlaps++;report.AppendLine("OVERLAP "+visible[a].name+" / "+visible[b].name+" at="+visible[a].transform.position.ToString("F1")+" gap="+d.magnitude.ToString("F2")+" near="+junction.name);}
  }
  if(session.ambientPedestrians!=null)foreach(var p in session.ambientPedestrians.actors){
   if(!p||!p.gameObject.activeInHierarchy||!p.Crossing)continue;
   foreach(var car in visible){var d=car.transform.position-p.transform.position;d.y=0;if(car.speed>1.5f&&d.sqrMagnitude<1.8f*1.8f){nearMisses++;report.AppendLine("PED_NEAR_MISS ped="+p.name+" car="+car.name+" at="+p.transform.position.ToString("F1")+" carSpeed="+car.speed.ToString("F1")+" near="+junction.name);break;}}
  }
 }

 // Paint-layer diagnosis: the same top view with one paint source visible at a time.
 static string Layer(Renderer r){var t=r.transform;while(t!=null){var n=t.name;if(n.StartsWith("원본 노면선"))return "source";if(n=="White road paint"||n=="Yellow centre paint")return "generated";if(n.StartsWith("사용자 시설"))return "facility";t=t.parent;}return null;}
 void Layers(List<TrafficJunction> junctions,string[] names){
  var groups=FindObjectsOfType<Renderer>().Select(r=>new{r,layer=Layer(r)}).Where(x=>x.layer!=null).ToList();
  foreach(var c in session.traffic.cars)if(c)foreach(var r in c.GetComponentsInChildren<Renderer>())r.enabled=false;
  Time.timeScale=0;
  foreach(var name in names){
   var j=junctions.FirstOrDefault(x=>x.name==name);if(j==null){report.AppendLine("LAYER_MISSING "+name);continue;}
   lens.fieldOfView=60;lens.transform.position=j.center+Vector3.up*75;lens.transform.rotation=Quaternion.Euler(90,0,0);
   string tag=string.Concat(name.Select(ch=>Path.GetInvalidFileNameChars().Contains(ch)||ch==' '?'_':ch));
   foreach(var mode in new[]{"all","source","generated","facility"}){
    foreach(var g in groups)if(g.r)g.r.enabled=mode=="all"||g.layer==mode;
    CaptureLayer("layer-"+tag+"-"+mode);
   }
   foreach(var g in groups)if(g.r)g.r.enabled=true;
   report.AppendLine("LAYERS "+name+" source="+groups.Count(g=>g.layer=="source")+" generated="+groups.Count(g=>g.layer=="generated")+" facility="+groups.Count(g=>g.layer=="facility"));
  }
 }
 void CaptureLayer(string name){
  const int width=1600,height=900;
  var frame=RenderTexture.GetTemporary(width,height,24);
  var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
  var prior=lens.targetTexture;
  var active=RenderTexture.active;
  try{
   lens.targetTexture=frame;
   lens.Render();
   RenderTexture.active=frame;
   pixels.ReadPixels(new Rect(0,0,width,height),0,0);
   pixels.Apply();
   File.WriteAllBytes(Path.Combine(output,name+".jpg"),pixels.EncodeToJPG(80));
  }catch(Exception e){report.AppendLine("CAPTURE_FAIL "+name+" "+e.Message);}
  finally{
   lens.targetTexture=prior;
   RenderTexture.active=active;
   RenderTexture.ReleaseTemporary(frame);
   Destroy(pixels);
  }
 }
 SignalColor EntrySignal(TrafficCar car,TrafficStop stop,int turn){
  var traffic=session.traffic;var j=stop.junction;var direction=car.route.graphPath==null?car.route.direction:car.route.Tangent(Mathf.Max(0,stop.distance));int sign=traffic.Sign(direction,stop.axis,j);
  if(j.editorProgram!=null){string lane=null;var graph=car.district!=null?car.district.graph:null;if(car.route.graphPath!=null&&graph!=null)lane=graph.edges[car.route.graphPath.EdgeAt(Mathf.Max(0,stop.distance))].id;return j.editorProgram.Movement(stop.axis,sign,turn,traffic.Clock,lane);}
  return traffic.MovementSignal(j,stop.axis,turn,sign);
 }
 IEnumerator Capture(string name){
  yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
  var frame=ScreenCapture.CaptureScreenshotAsTexture();
  try{File.WriteAllBytes(Path.Combine(output,name+".jpg"),frame.EncodeToJPG(80));}catch(Exception e){report.AppendLine("CAPTURE_FAIL "+name+" "+e.Message);}finally{Destroy(frame);}
 }

 void Finish(string failure){
  report.AppendLine("SUMMARY redStraight="+redStraight+" redLeft="+redLeft+" lateRed="+lateRed+" yellow="+yellowEntries+" green="+greenEntries+" stuck="+stuck+" overlaps="+overlaps+" pedNearMiss="+nearMisses+" jaywalkers="+jaywalkers+(failure==null?"":" failure="+failure));
  if(output!=null)File.WriteAllText(Path.Combine(output,"map-audit.txt"),report.ToString());
  Debug.Log("MAP_AUDIT_DONE "+(failure??"ok"));Application.Quit(failure==null?0:1);
 }
}
