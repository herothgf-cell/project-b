using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class TrafficDistributionTest : MonoBehaviour {
 readonly StringBuilder report=new StringBuilder();bool ok=true;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-trafficDistributionTest")>=0)new GameObject("Focused traffic distribution validation").AddComponent<TrafficDistributionTest>();}
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);Debug.Log("TRAFFIC_DISTRIBUTION "+message+"="+value);}
 IEnumerator Start(){
  var session=FindObjectOfType<DriveSession>();if(!session){Check(false,"session exists");Finish();yield break;}
  session.business.enabledCampaign=false;session.business.enabled=false;session.paused=false;session.roadConditions.enabled=false;
  Check(session.business.SavePath.IndexOf("artifacts",StringComparison.OrdinalIgnoreCase)>=0,"Test flag isolates campaign save path="+session.business.SavePath);
  Time.timeScale=3;var traffic=session.traffic;var district=traffic.GetComponent<DistrictTraffic>();bool safe;int visible,cells;
  string snapshot=TrafficDistributionSnapshot.Inspect(traffic,out safe,out visible,out cells);Check(visible>=24&&cells>=8&&traffic.cars.Count<=48,"initial "+snapshot);Check(safe,"initial ordinary starts, >=10m spacing, player/static clearance");
  // Exercise actual collider admission rather than only a clear default taxi pose.
  var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Traffic admission blocker fixture";blocker.transform.position=new Vector3(-5000,.85f,-5000);blocker.transform.localScale=Vector3.one;
  foreach(int layer in new[]{0,8}){blocker.layer=layer;Physics.SyncTransforms();Check(district.Occupied(new Vector3(-5000,0,-5000),Vector3.forward,null),"null-self admission detects blocker layer="+layer);}
  blocker.SetActive(false);Destroy(blocker);Physics.SyncTransforms();
  TrafficCar seeded=null;foreach(var car in traffic.cars)if(car.Visible){seeded=car;break;}
  Check(seeded!=null,"saved-pose overlap seed fixture exists");
  if(seeded){
   float seedDistance=seeded.distance;var seedPosition=seeded.transform.position;var seedRotation=seeded.transform.rotation;
   var original=new LiveCampaignCheckpoint{position=session.taxi.transform.position,rotation=session.taxi.transform.rotation,mapRevision=session.world.map.editorRevision,jobs=session.jobs.CaptureLive(),preparing=false};
   var atSeed=new LiveCampaignCheckpoint{position=seedPosition+Vector3.up*.65f,rotation=seedRotation,mapRevision=original.mapRevision,jobs=session.jobs.CaptureLive(),preparing=false};
   // Invoke the production saved-live restore, including NPC cleanup before unpausing.
   session.business.SendMessage("RestoreLive",atSeed,SendMessageOptions.RequireReceiver);Physics.SyncTransforms();
   Check(!session.business.ResumeBlocked&&Vector3.Distance(session.taxi.transform.position,atSeed.position)<.01f,"saved taxi pose restored at seeded NPC without moving player");
   bool playerClear=true;foreach(var car in traffic.cars)if(car.Visible&&Physics.CheckBox(car.transform.position+Vector3.up*.85f,new Vector3(1.1f,.55f,4),car.transform.rotation,1<<8,QueryTriggerInteraction.Ignore))playerClear=false;
   Check(!seeded.Visible&&playerClear,"restore-at-seed defers overlapping NPC before simulation resumes");
   session.business.SendMessage("RestoreLive",original,SendMessageOptions.RequireReceiver);seeded.Place(seedDistance);session.paused=false;Time.timeScale=3;
  }
  session.taxi.body.isKinematic=true;session.taxi.gameObject.SetActive(false);Physics.SyncTransforms();
  int overlaps=0;var initial=new Dictionary<TrafficCar,float>();foreach(var car in traffic.cars)initial[car]=car.totalTravel;
  for(int sample=0;sample<20;sample++){
   yield return new WaitForSeconds(1);
   foreach(var car in traffic.cars)if(car.Visible)foreach(var hit in Physics.OverlapBox(car.transform.position+Vector3.up*.85f,new Vector3(.7f,.4f,1.6f),car.transform.rotation,1<<9,QueryTriggerInteraction.Ignore)){var other=hit.GetComponentInParent<TrafficCar>();if(other&&other!=car&&other.Visible)overlaps++;}
  }
  int moved=0;float travel=0;foreach(var car in traffic.cars){float delta=car.totalTravel-initial[car];if(delta>3)moved++;travel+=delta;}
  Check(moved>=12&&travel>200&&overlaps==0,"20s natural motion moved="+moved+" metres="+travel+" bodyOverlaps="+overlaps);
  TrafficRoute fixture=null;TrafficStop signal=null;TrafficCar continuation=null;
  foreach(var car in traffic.cars){if(continuation==null)continuation=car;foreach(var stop in car.route.stops)if(signal==null&&stop.junction.signalized&&stop.Movement(car.route)==0&&stop.distance>40&&stop.exitDistance<car.route.length-40){fixture=car.route;signal=stop;}}
  foreach(var car in traffic.cars){car.Recycle();car.enabled=false;}
  Check(continuation!=null,"continuation fixture");
  if(continuation){continuation.Place(continuation.route.length-.01f);int before=continuation.completedRoutes;continuation.enabled=true;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Check(continuation.completedRoutes==before+1&&continuation.distance<1&&continuation.Visible,"connected onward route continues");continuation.Recycle();continuation.enabled=false;}
  Check(signal!=null,"mapped signal queue fixture");
  if(signal!=null){
   traffic.manualClock=true;int sign=traffic.Sign(fixture.Tangent(signal.distance),signal.axis,signal.junction);float red=-1,green=-1;
   for(int t=0;t<300;t++){traffic.manualTime=t;if(traffic.Signal(signal.junction,signal.axis,sign)==SignalColor.Red)red=t;if(traffic.Signal(signal.junction,signal.axis,sign)==SignalColor.Green)green=t;if(red>=0&&green>=0)break;}
   Check(red>=0&&green>=0,"mapped red and green phases exist");var queue=new List<TrafficCar>();traffic.manualTime=red;
   for(int i=0;i<3;i++){var car=traffic.Spawn(fixture,signal.distance-10-i*12,100+i);if(car){car.district=district;queue.Add(car);}}
   Check(queue.Count==3,"three vehicle queue admitted");yield return new WaitForSeconds(8);bool held=true;foreach(var car in queue)held&=car.distance<=signal.distance+.05f;Check(held,"mapped red holds queue");
   traffic.manualTime=green;yield return new WaitForSeconds(20);bool departed=true;foreach(var car in queue)departed&=car.distance>signal.distance+1;Check(departed,"mapped green releases queue");
  }
  Finish();
 }
 void Finish(){string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"traffic-distribution-runtime.txt"),"success="+ok+"\n"+report);Application.Quit(ok?0:1);}
}
