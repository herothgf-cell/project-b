using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Preview only. Preserve the curb footprint; three vehicle lanes occupy the
// existing pavement. Centre positions and lane-change windows are game estimates.
public static class P29MapEdit {
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);var before=ProjectMapStore.Load(root);
  string original=JsonUtility.ToJson(before);var after=Preview(before);
  after.revision=27;after.savedAt=DateTime.UtcNow.ToString("o");after.projectRevision="";
  P29MapTests.Validate(before,after);
  var candidate=ProjectMapPackage.Create(before.baseline,after);
  Check(candidate.baselineHash==before.baselineHash&&original==JsonUtility.ToJson(before),"baseline/input changed");
  ProjectMapStore.Save(root,candidate,token);var loaded=ProjectMapStore.Load(root);
  Check(loaded.document.revision==27&&loaded.contentHash==candidate.contentHash&&loaded.baselineHash==before.baselineHash,"persisted fingerprint mismatch");
  Check(loaded.document.ToJson()==candidate.document.ToJson(),"persisted candidate mismatch");
  P29MapTests.Validate(before,loaded.document);ProjectMapBuild.Prepare();
  Debug.Log("P29_MAP_APPLIED revision=27 contentHash="+loaded.contentHash+" baselineHash="+loaded.baselineHash);
 }
 public const string Prefix="reviewed:P29:",Hash="f6f057397dfbf09070db6da001b1a69c0eed3a545b5e0b8ae824683c0c2c0d88";
 public const string EInner="local:7322:2546:7735:2544",EOuter="local:7322:2502:7734:2500",WInner="local:7735:2588:7322:2590",WOuter="local:7735:2632:7323:2634";
 public static readonly string[] Sources={"37612023:A0020000:873:0","37612023:A0020000:614:0","37612023:A0020000:996:0"};
 public static string Segment(string root,string role){return "editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(root)).TrimEnd('=').Replace('+','-').Replace('/','_')+":P29-"+role;}
 public static string Terminal(string root){return Segment(root,"approach");}
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("P29_EDIT: "+why);}
 static T Copy<T>(T v){return JsonUtility.FromJson<T>(JsonUtility.ToJson(v));}
 static MapPoint Point(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static AssistedRoadNetwork.Path Path(RoadMapLane l){return new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray());}
 static RoadMapEvidence Evidence(){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,ruleConfirmed=true,checkedAt="2026-11-16",month="2026-05",url="https://map.naver.com/p?c=19,0,0,0,adh&p=zeNAWaX_hF2QnqhF1sSFcg,90,-10,80,Float",note="User confirmed question33/P29 E2/W1 throughout. East inner left-only; outer painted straight but permits straight+right. May2026 panorama and Oct2024 wPB0aAQqZhpfiR8Rlth2og comparison. Existing west permissions retained, no inferred U-turn. Surviving west lane uses midpoint of prior two lane centres; pavement/curbs retained and10→31m adjacent lane changes are game estimates."};}
 static MapPoint[] Slice(AssistedRoadNetwork.Path path,float a,float b){var points=new List<MapPoint>();for(float at=a;at<b-.001f;at+=1)points.Add(Point(path.Position(at)));points.Add(Point(path.Position(b)));return points.ToArray();}
 static RoadMapLane Split(RoadMapDocument d,RoadMapLane head,string id,float local){var path=Path(head);var tail=Copy(head);tail.id=id;tail.sourceOffset=head.sourceOffset+local;tail.fromNode=id+":node";tail.points=Slice(path,local,path.length);tail.heights=new float[tail.points.Length];head.toNode=tail.fromNode;head.points=Slice(path,0,local);head.heights=new float[head.points.Length];head.next=new[]{id};d.lanes=d.lanes.Concat(new[]{tail}).ToArray();return tail;}
 static void Anchor(RoadMapLane join,RoadMapLane from,RoadMapLane to){
  var a=from.points.Last().V();var b=to.points[0].V();var f=(a-from.points[from.points.Length-2].V()).normalized;var t=(to.points[1].V()-b).normalized;float h=Vector3.Distance(a,b)/3;join.points=new MapPoint[65];for(int i=0;i<65;i++){float v=i/64f,u=1-v;join.points[i]=Point(u*u*u*a+3*u*u*v*(a+f*h)+3*u*v*v*(b-t*h)+v*v*v*b);}join.heights=new float[65];join.fromNode=from.toNode;join.toNode=to.fromNode;join.next=new[]{to.id};join.number=to.number;join.evidence=Evidence();
 }
 public static RoadMapDocument Preview(ProjectMapPackage p){
  Check(p.document.revision==26&&p.contentHash==Hash,"exact reviewed r26 required");Check(!p.document.lanes.Any(l=>l.id.Contains(":P29-")||l.id.StartsWith(Prefix)),"already authored");
  var roots=new[]{EInner,EOuter,WInner,WOuter};Check(!p.baseline.stops.Any(s=>s.graphStop&&roots.Contains(p.baseline.hdEdges[s.laneEdge].id)),"bound stop needs explicit preservation");Check(!p.document.scene.objects.Any(o=>roots.Contains(o.laneId)),"bound service needs explicit preservation");
  var d=p.document.Copy();var lanes=d.lanes.ToDictionary(l=>l.id);foreach(string root in roots)Check(lanes.ContainsKey(root)&&!lanes[root].disabled,"expected active root "+root);
  var wi=lanes[WInner];var wo=lanes[WOuter];var outside=Path(wo);wi.points=wi.points.Select(pnt=>{float error;float at=outside.Project(pnt.V(),out error);return Point((pnt.V()+outside.Position(at))*.5f);}).ToArray();wi.heights=new float[wi.points.Length];wi.width=3.5f;wi.number=1;wi.evidence=Evidence();
  wi.next=wi.next.Concat(wo.next).Distinct().ToArray();wo.disabled=true;wo.next=new string[0];wo.evidence=Evidence();foreach(var l in d.lanes.Where(l=>!l.disabled))l.next=l.next.Select(id=>id==WOuter?WInner:id).Distinct().ToArray();
  foreach(var join in d.lanes.Where(l=>!l.disabled&&l.id.StartsWith("join:")&&(l.next.Contains(WInner)||wi.next.Contains(l.id)))){var from=d.lanes.Single(l=>!l.disabled&&l.next.Contains(join.id));Anchor(join,from,lanes[join.next.Single()]);}
  string forbidden="join:"+EInner+":0";lanes[EInner].next=lanes[EInner].next.Where(id=>id!=forbidden).ToArray();lanes[forbidden].disabled=true;lanes[forbidden].next=new string[0];lanes[forbidden].evidence=Evidence();
  foreach(string root in new[]{EInner,EOuter}){lanes[root].evidence=Evidence();var middle=Split(d,lanes[root],Segment(root,"change-window"),10);var terminal=Split(d,middle,Terminal(root),21);lanes[middle.id]=middle;lanes[terminal.id]=terminal;}
  foreach(var pair in new[]{new[]{EInner,EOuter},new[]{EOuter,EInner}}){var from=lanes[pair[0]];var to=lanes[Terminal(pair[1])];var change=Copy(from);change.id="change:"+Prefix+pair[0]+":"+pair[1];change.turn=0;change.change=to.number-from.number;Anchor(change,from,to);from.next=from.next.Concat(new[]{change.id}).ToArray();d.lanes=d.lanes.Concat(new[]{change}).ToArray();lanes[change.id]=change;}
  var terminalOuter=lanes[Terminal(EOuter)];var pos=Path(terminalOuter).Position(4);var paintEvidence=Evidence();paintEvidence.ruleConfirmed=false;paintEvidence.note+=" Explicit straight-only paint metadata does not remove either existing right-turn connection.";
  d.facilities=d.facilities.Concat(new[]{new RoadMapFacility{id=Prefix+"outer-straight-paint",kind="lanePresentation",type=LaneArrowAppearance.StraightType,laneId=terminalOuter.id,junctionId=terminalOuter.ruleId,rule=false,x=pos.x,z=pos.z,scale=1,evidence=paintEvidence}}).ToArray();
  foreach(var o in d.scene.objects){if(o.kind=="road"&&Sources.Any(id=>o.id=="road:"+id))o.lanes=3;if(o.id.StartsWith("road:polish:")){string id=o.id.Substring("road:polish:".Length);RoadMapLane lane;if(lanes.TryGetValue(id,out lane)){if(lane.disabled&&(id==WOuter||id==forbidden))o.enabled=false;else if(id==WInner||lane.evidence!=null&&lane.evidence.note.StartsWith("User confirmed question33/P29")&&id.StartsWith("join:"))o.points=lane.points.Select(q=>Point(q.V())).ToArray();}}}
  Debug.Log("P29_PREVIEW E2/W1 eastL/paintS outerNavigationS+R survivingWest="+WInner+" disabledWest="+WOuter+" sidewalks-unchanged no-save");return d;
 }
}
