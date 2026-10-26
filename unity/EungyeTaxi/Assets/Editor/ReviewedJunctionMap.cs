using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
// Reproducible field-observed lane roles. Every metre value below is a game estimate.
public static class ReviewedJunctionMap {
 public const string North="local:7868:6438:4994:4693",South="local:5034:4203:5033:2698",West="local:4861:4677:2204:5298";
 static RoadMapLane Lane(RoadMapDocument d,string id){return d.lanes.Single(l=>l.id==id);}
 static MapPoint Point(Vector3 v){return new MapPoint{x=v.x,z=v.z,elevation=v.y};}
 static AssistedRoadNetwork.Path Path(RoadMapLane l){return new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray());}
 static RoadMapEvidence Evidence(bool north){return new RoadMapEvidence{state="observed",url=north?"https://map.naver.com/p/entry/coordinates/3zafbw,2AGEMu?c=16,0,0,0,adh&p=dcuIEvnfmTeYM4mY2DWaXA,35,-8,50,Float":"https://map.naver.com/p/entry/coordinates/3zaffK,2AGAoR?c=17,0,0,0,adh&p=Po3cjZOgY9v4qPfVBPGTYg,0,-12,65,Float",month="2026-05",checkedAt="2026-10-26",appearanceConfirmed=true,ruleConfirmed=true,note=north?"북쪽 진입 좌1/직2/우3, 북서 분리 우회전과 교통섬 관찰. 폭·길이·합류·신호 주기는 게임 추정.":"북쪽 진입 좌1/직2, 반대 북행1. 동쪽 은계번영길 연결과 중앙 난간 관찰. 치수·신호 주기는 게임 추정."};}
 static void SetPoints(RoadMapLane l,IEnumerable<Vector3> points){l.points=points.Select(Point).ToArray();l.heights=new float[l.points.Length];}
 // Insert an exact station, then use the editor operation to preserve stop/service/facility lineage.
 static RoadMapLane Split(RoadMapDocument d,RoadMapLane lane,float cut){var path=Path(lane);var points=new List<Vector3>();for(float at=0;at<cut;at+=1)points.Add(path.Position(at));int split=points.Count;points.Add(path.Position(cut));for(float at=cut+1;at<path.length-.01f;at+=1)points.Add(path.Position(at));points.Add(path.Position(path.length));SetPoints(lane,points);return Lane(d,RoadMapAuthoringOperations.SplitLane(d,lane.id,split));}
 static RoadMapLane Branch(RoadMapDocument d,RoadMapLane tail,string id,int number,float offset,float length,bool north){var path=Path(tail);var l=new RoadMapLane{id=id,sourceId=tail.sourceId,fromNode=tail.fromNode,toNode=id+":end",ruleId=tail.ruleId,number=number,width=3.5f,status="provisional",evidence=Evidence(north)};var points=new List<Vector3>();for(float at=0;at<length-.01f;at+=1){float t=Mathf.Clamp01(at/25),blend=t*t*(3-2*t);points.Add(path.Position(at)-TrafficSystem.Right(path.Tangent(at))*offset*blend);}points.Add(path.Position(length)-TrafficSystem.Right(path.Tangent(length))*offset);SetPoints(l,points);d.lanes=d.lanes.Concat(new[]{l}).ToArray();return l;}
 static RoadMapLane Join(RoadMapDocument d,RoadMapLane from,string target,int turn,string id,bool north,int change=0){var generated=RoadMapAuthoringOperations.Connect(d,from.id,target,turn,change);var join=Lane(d,generated);join.id=id;from.next=from.next.Select(n=>n==generated?id:n).ToArray();join.evidence=Evidence(north);return join;}
 static void Surface(RoadMapDocument d,RoadMapLane l,float width=3.8f){d.scene.objects=d.scene.objects.Concat(new[]{new ProjectMapObject{id="road:"+l.id,kind="road",name="검토 교차로 노면 · 게임 치수",width=width,lanes=1,oneWay=true,suppressPaint=true,points=l.points}}).ToArray();}
 static void Crossing(RoadMapDocument d,ProjectMapPackage p,string id,string junction,Vector3 center,Vector3 heading,float width,string laneId,bool stop=false){var r=p.baseline.junctionRules.Single(x=>x.id==junction);var axisDir=new Vector3(r.dx,0,r.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(heading,axisDir))>.7f?0:1;int sign=Vector3.Dot(heading,axis==0?axisDir:TrafficSystem.Right(axisDir))>=0?1:-1;d.facilities=d.facilities.Concat(new[]{new RoadMapFacility{id=id,kind=stop?"stopLine":"crosswalk",junctionId=junction,laneId=laneId,x=center.x,z=center.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,width=width,length=stop?.4f:2.4f,axis=axis,sign=sign,evidence=Evidence(junction=="J5057_4449")}}).ToArray();}
 [MenuItem("Taxi/Project Map/Apply Reviewed Northern Junctions")]
 public static void Apply(){
  var root=ProjectMapBuild.Root;var token=ProjectMapStore.Fingerprint(root);var package=ProjectMapStore.Load(root);var d=package.document.Copy();
  if(d.lanes.Any(l=>l.id=="reviewed:J5057-N:left")){ReviewedJunctionMapTests.Run();return;}
  var north=Lane(d,North);var tail=Split(d,north,Path(north).length-55);var tailPath=Path(tail);tail.number=2;
  var left=Branch(d,tail,"reviewed:J5057-N:left",1,6.8f,tailPath.length,true);
  var right=Branch(d,tail,"reviewed:J5057-N:right",3,0,27,true);
  // Canonical through centre moves inward one lane; curb lane becomes the slip approach.
  var shifted=new List<Vector3>();for(float at=0;at<tailPath.length-.01f;at+=1){float t=Mathf.Clamp01(at/25);shifted.Add(tailPath.Position(at)-TrafficSystem.Right(tailPath.Tangent(at))*3.5f*t*t*(3-2*t));}shifted.Add(tailPath.Position(tailPath.length)-TrafficSystem.Right(tailPath.Tangent(tailPath.length))*3.5f);SetPoints(tail,shifted);tail.evidence=Evidence(true);
  foreach(var id in tail.next.ToArray())RoadMapAuthoringOperations.DeleteLane(d,id);tail.next=new string[0];
  Join(d,tail,South,0,"join:reviewed:J5057-N:straight",true);
  Join(d,left,"A2218G611338:0",-1,"join:reviewed:J5057-N:left",true);
  var slip=Join(d,right,"A2218G611369:0",1,"join:reviewed:J5057-N:right",true);
  var westOuter=Lane(d,"A2218G611369:0");Split(d,westOuter,20);var westInner=Split(d,Lane(d,"A2218G611375:0"),45);
  var merge=Join(d,westOuter,westInner.id,0,"join:reviewed:J5057-W:merge",true,-1);
  merge.evidence=new RoadMapEvidence{state="game",checkedAt="2026-10-26",note="NGII B2218G612649 type212 dashed separator between outer4 and3. Provisional 25m game lane change; field permission and exact taper unverified. Entrance through A2218G611370 retained."};
  // Retire unconnected imported north approach stubs superseded by the routable reviewed approach.
  foreach(var id in new[]{"A2218G611322:0","A2218G611323:0","A2218G611324:0","A2218G611326:0","A2218G611349:0"})RoadMapAuthoringOperations.DeleteLane(d,id);
  north.next=new[]{tail.id,left.id,right.id};
  var south=Lane(d,South);var southTail=Split(d,south,Path(south).length-55);southTail.number=2;southTail.evidence=Evidence(false);
  var southLeft=Branch(d,southTail,"reviewed:J5076-N:left",1,3.5f,Path(southTail).length,false);
  var oldLeft=southTail.next.Select(id=>Lane(d,id)).Single(l=>l.turn==-1);string east=oldLeft.next.Single();RoadMapAuthoringOperations.DeleteLane(d,oldLeft.id);
  Join(d,southLeft,east,-1,"join:reviewed:J5076-N:left",false);south.next=new[]{southTail.id,southLeft.id};
  var island=new ProjectMapObject{id="sidewalk:reviewed:J5057-NW:island",kind="sidewalk",name="북서 보행 교통섬 · 관찰/치수 추정",height=.14f,width=4,points=new[]{Point(new Vector3(497,0,473.7f)),Point(new Vector3(499.8f,0,473.7f)),Point(new Vector3(499.8f,0,481.5f))}};
  TrimSlipSidewalk(d,slip,2.35f);
  d.scene.objects=d.scene.objects.Concat(new[]{island}).ToArray();
  foreach(var l in new[]{tail,left,right,slip,southLeft})Surface(d,l,l==slip?4.2f:3.8f);
  Crossing(d,package,"reviewed:J5057-N:crosswalk","J5057_4449",new Vector3(507.75f,0,474.5f),Vector3.back,15.9f,tail.id);
  Crossing(d,package,"reviewed:J5057-N:stop","J5057_4449",new Vector3(504.9f,0,477),Vector3.back,7,tail.id,true);
  // Slip crossing follows the curve, from the existing sidewalk to the refuge edge.
  var slipPath=Path(slip);float crossAt=slipPath.length*.36f;Crossing(d,package,"reviewed:J5057-NW:slip-crosswalk","J5057_4449",slipPath.Position(crossAt),slipPath.Tangent(crossAt),6,right.id);
  d.facilities.Last().junctionId=""; // The separate slip crossing must not replace the main junction crossing station.
  Crossing(d,package,"reviewed:J5076-N:crosswalk","J5076_2580",new Vector3(507.9f,0,273),Vector3.back,13.2f,southTail.id);
  Crossing(d,package,"reviewed:J5076-N:stop","J5076_2580",new Vector3(505.1f,0,276.5f),Vector3.back,7,southTail.id,true);
  d.revision++;foreach(var issue in RoadMapValidator.Validate(package.baseline,d).Where(i=>i.error))Debug.LogError(issue+" at="+issue.position);System.IO.File.WriteAllText(System.IO.Path.Combine(root,"artifacts/junction-map-candidate.json"),d.ToJson());var result=ProjectMapPackage.Create(package.baseline,d);if(result.baselineHash!=package.baselineHash)throw new Exception("Reviewed junction changed baseline");
  ReviewedJunctionMapTests.Validate(result);ProjectMapStore.Save(root,result,token);ProjectMapBuild.Prepare();ReviewedJunctionMapTests.Run();
  Debug.Log("REVIEWED_JUNCTION_MAP_APPLIED baseline="+result.baselineHash+" content="+result.contentHash);
 }

 static List<Vector3> Clip(List<Vector3> polygon,Vector3 a,Vector3 b,bool inside){
  var output=new List<Vector3>();if(polygon.Count<3)return output;
  Func<Vector3,float> side=p=>(b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);
  for(int i=0;i<polygon.Count;i++){var p=polygon[i];var q=polygon[(i+1)%polygon.Count];float ps=side(p),qs=side(q);bool pin=inside?ps>=0:ps<=0,qin=inside?qs>=0:qs<=0;if(pin)output.Add(p);if(pin!=qin)output.Add(Vector3.Lerp(p,q,ps/(ps-qs)));}
  for(int i=output.Count-1;i>=0&&output.Count>1;i--)if(Vector3.Distance(output[i],output[(i+1)%output.Count])<.002f)output.RemoveAt(i);
  if(output.Count<3)return new List<Vector3>();float area=0;for(int i=0;i<output.Count;i++)area+=output[i].x*output[(i+1)%output.Count].z-output[(i+1)%output.Count].x*output[i].z;if(Mathf.Abs(area)<.005f)return new List<Vector3>();return output;
 }
 // Subtract the new slip pavement from imported sidewalk polygons. Retain every outside fragment.
 static void TrimSlipSidewalk(RoadMapDocument d,RoadMapLane slip,float halfWidth){
  var additions=new List<ProjectMapObject>();var path=Path(slip);float minX=slip.points.Min(p=>p.x)-halfWidth,maxX=slip.points.Max(p=>p.x)+halfWidth,minZ=slip.points.Min(p=>p.z)-halfWidth,maxZ=slip.points.Max(p=>p.z)+halfWidth;
  foreach(var o in d.scene.objects.Where(o=>o.enabled&&o.kind=="sidewalk")){
   if(o.points.Max(p=>p.x)<minX||o.points.Min(p=>p.x)>maxX||o.points.Max(p=>p.z)<minZ||o.points.Min(p=>p.z)>maxZ)continue;
   var fragments=new List<List<Vector3>>{o.points.Select(p=>p.V()).ToList()};bool changed=false;
   for(int segment=1;segment<slip.points.Length;segment++){
    var a=slip.points[segment-1].V();var b=slip.points[segment].V();var heading=(b-a).normalized;var side=TrafficSystem.Right(heading)*halfWidth;var rect=new[]{a-side-heading*.12f,b-side+heading*.12f,b+side+heading*.12f,a+side-heading*.12f};
    // Rectangle is clockwise in x/z; reverse to make the clipping interior positive.
    float orientation=0;for(int i=0;i<4;i++)orientation+=rect[i].x*rect[(i+1)%4].z-rect[(i+1)%4].x*rect[i].z;if(orientation<0)Array.Reverse(rect);
    var next=new List<List<Vector3>>();foreach(var fragment in fragments){var remainder=fragment;var outside=new List<List<Vector3>>();for(int e=0;e<4&&remainder.Count>=3;e++){var part=Clip(remainder,rect[e],rect[(e+1)%4],false);if(part.Count>=3)outside.Add(part);remainder=Clip(remainder,rect[e],rect[(e+1)%4],true);}if(remainder.Count>=3){changed=true;next.AddRange(outside);}else next.Add(fragment);}fragments=next;
   }
   if(!changed)continue;o.enabled=false;int partIndex=0;foreach(var points in fragments){var copy=JsonUtility.FromJson<ProjectMapObject>(JsonUtility.ToJson(o));copy.id=o.id+":reviewed-slip:"+partIndex++;copy.enabled=true;copy.points=points.Select(Point).ToArray();additions.Add(copy);}
  }
  d.scene.objects=d.scene.objects.Concat(additions).ToArray();Debug.Log("REVIEWED_SLIP_SIDEWALK_FRAGMENTS "+additions.Count);
 }
}

