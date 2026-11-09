using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// Explicit -roadviewSignalReview fixture only. Poses and fixed existing stages
// verify Player rendering; they do not represent physical or manual driving.
public sealed partial class BrightUIProbe {
 float RoadviewStageTime(TrafficJunction junction,int axis,int sign,int turn){
  var program=junction.editorProgram??junction.gameProgram;Check(program!=null,"roadview existing signal program "+junction.name);if(program==null)return 0;
  float at=0;foreach(var stage in program.stages){if(stage.groups.Any(id=>program.groups.Any(g=>g.id==id&&!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==turn)))return at+Mathf.Min(1,stage.green*.5f)-(junction.editorProgram==null?junction.offset:0);at+=stage.green+stage.yellow+stage.clearance;}
  Check(false,"roadview requested existing stage "+junction.name+" turn="+turn);return 0;
 }
 void RoadviewPhysicalHead(TrafficJunction junction,int axis,int sign,int expected){
  var head=SignalHeadPresentation.Resolve(s.world.map,junction,axis,sign);Check(head.lamps==expected,"roadview HUD head "+junction.name+" "+expected+" apertures");
  if(junction.editorProgram!=null){
   var physical=s.traffic.GetComponentsInChildren<RoadMapLamp>(true).Where(l=>l.yellow&&l.program!=null&&l.program.junctionId==junction.name&&l.axis==axis&&l.sign==sign).ToArray();
   Check(physical.Length>0,"roadview authored physical head exists "+junction.name);
   foreach(var lamp in physical)Check(lamp.red&&lamp.yellow&&lamp.green&&(lamp.leftLens!=null)==(expected==4),"roadview authored physical RGB/left aperture "+junction.name);
  }else {
   int index=-1;for(int i=0;i<junction.lampAxes.Count;i++)if(junction.lampAxes[i]==axis&&junction.lampSigns[i]==sign){index=i;break;}
   Check(index>=0,"roadview generated physical head exists "+junction.name);
   if(index>=0){var rgb=junction.lamps[index];Check(rgb.Length==3&&rgb.All(r=>r),"roadview generated RGB renderers "+junction.name);if(rgb.Length>0&&rgb[0])Check((rgb[0].transform.parent.Find("Signal lamp Left")!=null)==(expected==4),"roadview generated left aperture matches HUD "+junction.name);}
  }
 }
 IEnumerator ReviewRoadview(){
  var taxi=s.taxi;var camera=s.drivingCamera;var view=camera.GetComponent<Camera>();var hud=s.GetComponent<DriveHUD>();var readability=s.GetComponent<DrivingReadability>();var graph=s.jobs.nav.hd;
  Check(graph!=null&&ReferenceEquals(graph.edges,s.world.map.hdEdges),"roadview Player uses composed project graph");
  string savePath=s.business.SavePath;int cash=s.business.ledger.Account.Cash;double elapsed=s.business.ledger.Elapsed;
  float scale=Time.timeScale,manualTime=s.traffic.manualTime;bool manualClock=s.traffic.manualClock,taxiEnabled=taxi.enabled,businessEnabled=s.business.enabled,jobsEnabled=s.jobs.enabled,firstPerson=camera.FirstPerson;
  var position=taxi.transform.position;var rotation=taxi.transform.rotation;var velocity=taxi.body.velocity;var angular=taxi.body.angularVelocity;
  var cars=s.traffic.cars.Where(c=>c).ToArray();var enabled=cars.Select(c=>c.enabled).ToArray();
  try {
   Time.timeScale=0;s.traffic.manualClock=true;taxi.enabled=s.business.enabled=s.jobs.enabled=false;taxi.body.velocity=taxi.body.angularVelocity=Vector3.zero;foreach(var car in cars)car.enabled=false;
   Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
   var ids=new[]{"J7917_2565","J7973_3744","J1927_5181","J6480_2573"};
   var lanes=new[]{"local:7322:2546:7735:2544","A2218G610050:0","local:4861:4677:2204:5298","local:5868:2554:7086:2547:part:2"};
   int[] axes={1,1,1,0},signs={-1,-1,1,1},lamps={4,4,3,3};
   for(int n=0;n<ids.Length;n++){
    var junction=s.traffic.junctions.Single(j=>j.name==ids[n]);int edge=Array.FindIndex(graph.edges,e=>e.id==lanes[n]);Check(edge>=0,"roadview reported approach lane "+lanes[n]);if(edge<0)continue;
    var path=graph.paths[edge];var approach=s.traffic.AxisDirection(junction,axes[n])*signs[n];float error;float at=path.Project(s.traffic.StopPosition(junction,axes[n],signs[n])-approach*6,out error);at=Mathf.Clamp(at,1,Mathf.Max(1,path.length-2));var heading=path.Tangent(at);var point=path.Position(at)+Vector3.up*.65f;
    float stopGap=Vector3.Dot(junction.center-point,approach)-junction.StopDistance(axes[n],signs[n]),endpointGap=Vector3.Dot(junction.center-path.Position(path.length),approach)-junction.StopDistance(axes[n],signs[n]);
    Debug.Log("ROADVIEW_POSE junction="+ids[n]+" axis="+axes[n]+" sign="+signs[n]+" lane="+lanes[n]+" at="+at.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" length="+path.length.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" stopGap="+stopGap.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" endpointStopGap="+endpointGap.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" position="+point);
    taxi.body.position=point;taxi.body.rotation=Quaternion.LookRotation(heading);taxi.transform.SetPositionAndRotation(point,taxi.body.rotation);Physics.SyncTransforms();s.law.ResetObservation();s.manners.Suppress(2);
    s.traffic.manualTime=RoadviewStageTime(junction,axes[n],signs[n],0);camera.SetFirstPerson(false);yield return null;readability.SelectSignal();
    Check(Vector3.Dot(heading,approach)>.8f,"roadview pose matches reported incoming direction "+ids[n]);Check(readability.SignalTarget==junction&&readability.SignalAxis==axes[n]&&readability.SignalSign==signs[n],"roadview actual HUD selects reported approach "+ids[n]);RoadviewPhysicalHead(junction,axes[n],signs[n],lamps[n]);
    if(n==2)Check(!s.traffic.HasLeftMovement(junction,axes[n],signs[n])&&!readability.SignalHead.left,"roadview westbound three-head retains no-left permission");
    yield return Capture("roadview-0"+(n+1)+"-"+ids[n]);
    if(n==1){
     var arrows=LaneDirectionPaint.Layout(s.world.map);foreach(string id in new[]{"A2218G610050:0","A2218G610051:0","A2218G610052:0"}){int native=Array.FindIndex(graph.edges,e=>e.id==id);Check(arrows.Any(a=>a.edgeIndex==native&&a.turns==(id=="A2218G610050:0"?LaneDirectionPaint.Left:LaneDirectionPaint.Straight)),"roadview actual Player native role "+id);}
     s.traffic.manualTime=RoadviewStageTime(junction,axes[n],signs[n],-1);yield return null;Check(s.traffic.LeftSignal(junction,axes[n],signs[n])==SignalColor.Green,"roadview existing protected left stage rendered northbound");yield return Capture("roadview-02-native-left-green");
     // The chase car hides the short native arrow interval. Only the camera
     // and car visibility change for this supplemental paint inspection.
     bool cameraEnabled=camera.enabled,hudEnabled=hud.enabled,readabilityEnabled=readability.enabled;var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;float fov=view.fieldOfView;
     var renderers=taxi.GetComponentsInChildren<Renderer>(true);var visibility=renderers.Select(r=>r.enabled).ToArray();
     try {
      camera.enabled=hud.enabled=readability.enabled=false;foreach(var renderer in renderers)renderer.enabled=false;
      var nativeArrows=arrows.Where(a=>graph.edges[a.edgeIndex].id=="A2218G610050:0"||graph.edges[a.edgeIndex].id=="A2218G610051:0"||graph.edges[a.edgeIndex].id=="A2218G610052:0").ToArray();
      if(nativeArrows.Length>0){var centre=nativeArrows.Aggregate(Vector3.zero,(v,a)=>v+a.centre)/nativeArrows.Length;camera.transform.position=centre+Vector3.up*28-approach*8;camera.transform.LookAt(centre);view.fieldOfView=42;yield return Capture("roadview-02-native-lanes-overhead");}
     }finally{for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=visibility[i];camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);view.fieldOfView=fov;camera.enabled=cameraEnabled;hud.enabled=hudEnabled;readability.enabled=readabilityEnabled;camera.SetFirstPerson(false);}
    }
   }
   Check(s.business.SavePath==savePath&&s.business.ledger.Account.Cash==cash&&s.business.ledger.Elapsed==elapsed,"roadview poses change no save route, payment or campaign time");
  }finally{
   taxi.body.position=position;taxi.body.rotation=rotation;taxi.transform.SetPositionAndRotation(position,rotation);taxi.body.velocity=velocity;taxi.body.angularVelocity=angular;Physics.SyncTransforms();
   for(int i=0;i<cars.Length;i++)if(cars[i])cars[i].enabled=enabled[i];taxi.enabled=taxiEnabled;s.business.enabled=businessEnabled;s.jobs.enabled=jobsEnabled;s.traffic.manualClock=manualClock;s.traffic.manualTime=manualTime;camera.SetFirstPerson(firstPerson);Time.timeScale=scale;
  }
 }
}
