using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

// Isolated position-controlled integration, not manual driving or field certification.
public sealed class WorldFollowupProbe : MonoBehaviour {
 const string Crossing="J6480_2573";
 const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-worldFollowupTest")>=0)new GameObject("World followup integration").AddComponent<WorldFollowupProbe>();}
 void Check(bool value,string reason){checks++;report.AppendLine((value?"PASS ":"FAIL ")+reason);Debug.Log((value?"PASS ":"FAIL ")+reason);if(!value)throw new Exception(reason);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);float deadline=Time.realtimeSinceStartup+90;s=FindObjectOfType<DriveSession>();while(s&&!s.StartupReady&&Time.realtimeSinceStartup<deadline)yield return null;
  bool ok=false;try{Check(s&&s.StartupReady,"runtime initialization completed");string save=Path.GetFullPath(s.business.SavePath).Replace('\\','/');Check(save.IndexOf("/artifacts/",StringComparison.OrdinalIgnoreCase)>=0,"generic Test suffix isolates save path: "+save);DisableLifecycle();Freeze();MapAndRoute();Overtime();LegacyLeft();Captures();ok=true;}catch(Exception e){report.AppendLine(e.ToString());Debug.LogException(e);}
  Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/world-followup-runtime.txt","success="+ok+"\nchecks="+checks+"\n"+report);Debug.Log("WORLD_FOLLOWUP_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
 void DisableLifecycle(){
  var car=s.traffic.cars.Find(c=>c&&c.enabled&&c.gameObject.activeInHierarchy);Check(car!=null,"active production car exists for disable lifecycle");
  var junction=new TrafficJunction{name="disable-lifecycle-fixture"};s.traffic.junctions.Add(junction);
  try{
   junction.reservations.Add(car);junction.requests[car]=Time.time;junction.owner=car;
   car.enabled=false;
   Check(!junction.reservations.Contains(car)&&!junction.requests.ContainsKey(car)&&junction.owner==null,"Player enabled=false dispatch releases real car reservation, request and owner");
  }finally{s.traffic.junctions.Remove(junction);car.enabled=true;}
 }
 void Freeze(){
  s.business.enabled=false;s.jobs.enabled=false;s.jobs.nav.enabled=false;s.requests.enabled=false;s.law.enabled=false;s.manners.enabled=false;s.GetComponent<ServiceLawZones>().enabled=false;s.taxi.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.interpolation=RigidbodyInterpolation.None;
  foreach(var car in s.traffic.cars){car.Recycle();car.enabled=false;}s.traffic.enabled=false;var district=s.GetComponent<DistrictTraffic>();if(!district)district=s.traffic.GetComponent<DistrictTraffic>();if(district)district.enabled=false;
  s.business.settings=PrototypeSettings.ForThreeDays();s.business.ledger=new EconomyLedger(s.business.settings);s.business.enabledCampaign=s.business.useV2=true;s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();s.law.ids=new PrototypeIds("world-followup");s.law.ledger=new LawLedger();
 }
 void Pose(Vector3 point,Vector3 heading){var t=s.taxi;t.body.position=point+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(heading);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);t.body.velocity=Vector3.zero;Physics.SyncTransforms();}
 void MapAndRoute(){
  var map=s.world.map;Check(map.mapId=="eungye"&&map.editorDocument!=null,"actual authored Eungye map loaded");
  foreach(string id in new[]{"J5750_2576","J7204_2569"}){var rule=Array.Find(map.junctionRules,j=>j.id==id);var live=s.traffic.junctions.Find(j=>j.name==id);Check(rule!=null&&rule.signalMode==0&&rule.crosswalk,"authored unsignalized crosswalk "+id);Check(live!=null&&!live.signalized&&live.editorProgram==null,"runtime unsignalized semantics "+id);}
  var junction=s.traffic.junctions.Find(j=>j.name==Crossing);Check(junction!=null&&junction.signalized&&junction.editorProgram!=null,"new midblock live signal program");
  var facilities=map.editorDocument.facilities.Where(f=>f.junctionId==Crossing).ToArray();Check(facilities.Count(f=>f.kind=="crosswalk")==1&&facilities.Count(f=>f.kind=="vehicleSignal")==2&&facilities.Count(f=>f.kind=="pedestrianSignal")==2,"single authored crossing with two vehicle and two pedestrian signals");
  var program=junction.editorProgram;Check(program.groups.Count(g=>g.pedestrian)==2&&program.groups.Count(g=>!g.pedestrian)==2&&program.Cycle>0,"midblock program has explicit vehicle/pedestrian groups");bool vehicleSeen=false,walkSeen=false;
  for(float t=0;t<program.Cycle;t+=.25f){bool vehicle=program.groups.Any(g=>!g.pedestrian&&program.Color(g.id,t)!=SignalColor.Red),walk=program.groups.Any(g=>g.pedestrian&&program.Color(g.id,t)==SignalColor.Green);Check(!(vehicle&&walk),"vehicle/pedestrian phase exclusivity t="+t);vehicleSeen|=vehicle;walkSeen|=walk;}
  Check(vehicleSeen&&walkSeen,"both vehicle and walk phases are reachable");
  var district=FindObjectOfType<DistrictTraffic>();Check(district!=null,"real district traffic route builder exists");var graph=district.graph;int edge=-1;float mid=0,error=float.MaxValue;
  for(int i=0;i<graph.edges.Length;i++){var e=graph.edges[i];if(e.disabled||e.turn!=0||e.change!=0||e.id.StartsWith("join:"))continue;float d;float at=graph.paths[i].Project(new Vector3(648,0,253),out d);if(d<error&&Vector3.Dot(graph.paths[i].Tangent(at),Vector3.right)>.9f&&at>12&&graph.paths[i].length-at>20){edge=i;mid=at;error=d;}}
  Check(edge>=0&&error<8,"eastbound source lane spans the midblock with no fallback route");var route=district.CreateRoute(edge,Mathf.Max(0,mid-15),edge,Mathf.Min(graph.paths[edge].length,mid+55));
  Check(route!=null&&route.graphPath!=null&&route.graphPath.edges.All(i=>i==edge),"actual same-edge district route retains source geometry");var stop=route.stops.Find(x=>x.junction==junction);Check(stop!=null&&stop.distance>0&&stop.exitDistance>stop.distance,"same-edge midblock route has a positive real stop, no zero fallback");
 }
 int Cell(int index){var stops=s.jobs.stops;float minX=stops.Min(p=>p.x),maxX=stops.Max(p=>p.x),minZ=stops.Min(p=>p.z),maxZ=stops.Max(p=>p.z);var p=stops[index];return Mathf.Min(2,Mathf.FloorToInt(3*Mathf.InverseLerp(minX,maxX,p.x)))+3*Mathf.Min(2,Mathf.FloorToInt(3*Mathf.InverseLerp(minZ,maxZ,p.z)));}
 void Overtime(){
  var total=new HashSet<int>();var allCells=new HashSet<int>();var stops=s.jobs.stops;Pose(stops[0].Kerb,stops[0].Direction);
  foreach(int elapsed in new[]{1200,1500,1800}){
   s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());s.business.ledger.Advance(elapsed,0);s.jobs.ClearForDay();Check(s.business.ledger.State==BusinessState.Open,"fresh overtime business remains open at "+elapsed);
   for(int generation=0;generation<3;generation++){
    s.jobs.Generate();var offers=s.jobs.offers;Check(offers.Length==3&&offers.All(o=>o!=null)&&offers.Select(o=>o.pickup).Distinct().Count()==3,"three unique overtime pickup calls at "+elapsed+" generation "+generation);
    var cells=new HashSet<int>(offers.Select(o=>Cell(o.pickup)));foreach(var o in offers){total.Add(o.pickup);allCells.Add(Cell(o.pickup));Check(!float.IsNaN(o.distance)&&!float.IsInfinity(o.distance)&&o.distance>0&&!string.IsNullOrEmpty(o.candidateSeed),"connected offer preserves candidate identity "+o.candidateSeed);}
    Check(cells.Count>=2,"one generation covers multiple geographic cells at "+elapsed+" generation "+generation);report.AppendLine("OFFERS elapsed="+elapsed+" generation="+generation+" pickupIds="+string.Join(",",offers.Select(o=>o.pickupStopId).ToArray())+" cells="+string.Join(",",cells.Select(c=>c.ToString()).ToArray()));
    var identities=offers.Select(o=>o.candidateSeed).ToArray();var snapshot=JsonUtility.FromJson<PassengerJobsSnapshot>(JsonUtility.ToJson(s.jobs.CaptureLive()));s.jobs.RestoreLive(snapshot);Check(s.jobs.offers.Select(o=>o.candidateSeed).SequenceEqual(identities),"candidate IDs survive actual jobs JSON restore");
   }
  }
  Check(total.Count>=4&&allCells.Count>=3,"overtime generations cover at least four pickup locations and three cells");
 }
 void LawTick(){typeof(DrivingLaw).GetMethod("FixedUpdate",Private).Invoke(s.law,null);}
 void LegacyLeft(){
  var traffic=s.traffic;bool manual=traffic.manualClock;float clock=traffic.manualTime;traffic.manualClock=true;
  var j=new TrafficJunction{name="world-followup-legacy-left",center=new Vector3(3000,0,3000),direction=Vector3.forward,signalized=true,leftTurnMode=LeftTurnMode.ProtectedShared};traffic.junctions.Add(j);
  try{foreach(bool red in new[]{true,false,true}){
   s.law.ResetObservation();traffic.manualTime=red?20:2;int before=s.law.ledger.TotalScore,count=s.law.ledger.Count;Check(j.editorProgram==null&&traffic.LeftSignal(j,0,1)==(red?SignalColor.Red:SignalColor.Green),"actual legacy protected left phase "+red);
   // Front bumper coordinates; every movement is below the teleport reset threshold.
   foreach(var p in new[]{new Vector3(2,0,-14.5f),new Vector3(2,0,-13.5f),new Vector3(2,0,-7.5f),new Vector3(2,0,-3),new Vector3(-2,0,-3),new Vector3(-8,0,-3)}){Vector3 heading=p.x<0?Vector3.left:Vector3.forward;Pose(j.center+p-heading*2,heading);LawTick();}
   Check(s.law.ledger.Count==count+1,"one physical legacy left crossing records one incident");var e=s.law.ledger.Events.Last();Check(e.junction==j.name&&e.action=="좌회전"&&e.rules.Any(r=>r.rule=="L01"&&r.verdict==(red?LawVerdict.Violation:LawVerdict.Compliant)),"actual DrivingLaw crossing evaluates legacy left "+red);
   Check(s.law.ledger.TotalScore==(red?Math.Max(0,before-20):Math.Min(100,before+3)),"actual legacy left immediately changes cumulative score");LawTick();Check(s.law.ledger.Count==count+1,"stationary next frame cannot duplicate the incident");
  }}finally{traffic.junctions.Remove(j);traffic.manualClock=manual;traffic.manualTime=clock;s.law.ResetObservation();}
 }
 void Captures(){
  if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null){report.AppendLine("UNVERIFIED graphics device null: all visual captures skipped");return;}
  Directory.CreateDirectory("artifacts");var go=new GameObject("World followup capture camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.fieldOfView=65;camera.useOcclusionCulling=false;camera.clearFlags=CameraClearFlags.Skybox;
  try{foreach(string id in new[]{"J993_5620","J1927_5181","J5057_4449","J7973_3744","J8066_6364","J7801_9629"}){var j=Array.Find(s.world.map.junctionRules,x=>x.id==id);Check(j!=null,"capture target exists "+id);var center=new Vector3(j.x,0,j.z);Shot(camera,id+"-overhead",center+Vector3.up*90,center,Vector3.forward,true,55);var dir=new Vector3(j.dx,0,j.dz).normalized;Shot(camera,id+"-driver",center-dir*42+TrafficSystem.Right(dir)*2+Vector3.up*1.75f,center+Vector3.up*2,Vector3.up,false,0);}Shot(camera,"commercial-crossings-overview",new Vector3(648,200,257.3f),new Vector3(648,0,257.3f),Vector3.forward,true,120);}
  finally{Destroy(go);}
 }
 void Shot(Camera camera,string name,Vector3 position,Vector3 target,Vector3 up,bool ortho,float size){
  camera.transform.position=position;camera.transform.rotation=Quaternion.LookRotation(target-position,up);camera.orthographic=ortho;camera.orthographicSize=size;var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();float min=1,max=0;for(int y=0;y<720;y+=24)for(int x=0;x<1280;x+=32){float v=pixels.GetPixel(x,y).grayscale;min=Mathf.Min(min,v);max=Mathf.Max(max,v);}Check(max-min>.03f,"actual camera capture contains scene contrast "+name);File.WriteAllBytes("artifacts/world-followup-"+name+".png",pixels.EncodeToPNG());}
  finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(pixels);}
 }
}
