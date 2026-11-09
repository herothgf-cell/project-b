using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// Opt-in isolated UI fixture: position-controlled rendering, not physical driving.
public sealed partial class BrightUIProbe {
 IEnumerator ReviewLaneMovement(){
  var taxi=s.taxi;var camera=s.drivingCamera;var lens=camera.GetComponent<Camera>();var hud=s.GetComponent<DriveHUD>();var readability=s.GetComponent<DrivingReadability>();var graph=s.jobs.nav.hd;
  Check(graph!=null&&ReferenceEquals(graph.edges,s.world.map.hdEdges),"lane review uses final Player graph");
  string save=s.business.SavePath;int cash=s.business.ledger.Account.Cash;double elapsed=s.business.ledger.Elapsed;
  Time.timeScale=0;s.traffic.manualClock=true;taxi.enabled=s.business.enabled=s.jobs.enabled=false;taxi.body.velocity=taxi.body.angularVelocity=Vector3.zero;foreach(var car in s.traffic.cars)if(car)car.enabled=false;
  Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
  var ids=new[]{"J866_5285","J5057_4449","J5057_4449"};int[] axes={0,1,1},signs={-1,1,-1};
  // Determine the selected source approach from explicit reviewed metadata,
  // never from nearest provisional lanes that overlap the native carriageway.
  for(int n=0;n<ids.Length;n++){
   var junction=s.traffic.junctions.Single(j=>j.name==ids[n]);
   var metadata=s.world.map.editorDocument.facilities.Where(f=>f.kind=="lanePresentation"&&f.junctionId==ids[n]&&f.axis==axes[n]&&f.sign==signs[n]).ToArray();
   Check(metadata.Length>=4,"reviewed source approach is present "+ids[n]+" sign="+signs[n]);if(metadata.Length<4)continue;
   var arrows=LaneDirectionPaint.Layout(s.world.map).Where(a=>metadata.Any(f=>f.laneId==graph.edges[a.edgeIndex].id)).ToArray();
   Check(arrows.Length==4,"four nonduplicated physical lane roles "+ids[n]+" sign="+signs[n]);
   Check(arrows.Count(a=>a.turns==(LaneDirectionPaint.Uturn|LaneDirectionPaint.Left))==1,"median lane has existing U-turn and left branches "+n);
   Check(arrows.Count(a=>a.turns==LaneDirectionPaint.Straight)>=2,"through lane arrows survive "+n);
   if(arrows.Length==0)continue;
   var median=arrows.Single(a=>a.turns==(LaneDirectionPaint.Uturn|LaneDirectionPaint.Left));var path=graph.paths[median.edgeIndex];var approach=s.traffic.AxisDirection(junction,axes[n])*signs[n];
   Vector3 point=n==0?new Vector3(129.77594f,.65f,526.7805f):path.Position(Mathf.Max(1,path.length-5))+Vector3.up*.65f;
   var heading=n==0?Quaternion.Euler(0,276.4818f,0):Quaternion.LookRotation(path.Tangent(path.length));
   taxi.body.position=point;taxi.body.rotation=heading;taxi.transform.SetPositionAndRotation(point,heading);Physics.SyncTransforms();s.law.ResetObservation();s.manners.Suppress(2);
   s.traffic.manualTime=RoadviewStageTime(junction,axes[n],signs[n],0);camera.enabled=hud.enabled=readability.enabled=true;camera.SetFirstPerson(false);yield return null;readability.SelectSignal();
   Check(Vector3.Dot(taxi.transform.forward,approach)>.8f,"reported approach heading matches "+n);
   yield return Capture("lane-0"+(n+1)+"-"+ids[n]+"-"+signs[n]);
   var renderers=taxi.GetComponentsInChildren<Renderer>(true);var visibility=renderers.Select(r=>r.enabled).ToArray();
   camera.enabled=hud.enabled=readability.enabled=false;foreach(var renderer in renderers)renderer.enabled=false;
   var centre=arrows.Aggregate(Vector3.zero,(v,a)=>v+a.centre)/arrows.Length;camera.transform.position=centre+Vector3.up*35-approach*6;camera.transform.LookAt(centre);lens.fieldOfView=55;
   yield return Capture("lane-0"+(n+1)+"-paint-overhead");
   for(int i=0;i<renderers.Length;i++)renderers[i].enabled=visibility[i];
  }
  camera.enabled=hud.enabled=readability.enabled=false;
  var points=new[]{new Vector3(1120,0,900),new Vector3(800,0,500)};
  for(int n=0;n<points.Length;n++){camera.transform.position=points[n]+Vector3.up*125;camera.transform.rotation=Quaternion.Euler(90,0,0);lens.fieldOfView=60;yield return Capture("corridor-0"+(n+1)+"-overhead");}
  Check(s.business.SavePath==save&&s.business.ledger.Account.Cash==cash&&s.business.ledger.Elapsed==elapsed,"lane rendering leaves campaign payment/time/save route unchanged");
 }
}
