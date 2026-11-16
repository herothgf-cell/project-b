using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Preview only: user-confirmed map32/P07 is a pedestrian passage, not a road.
public static class PinkPassageMapEdit {
 public const string Prefix="reviewed:pink-P07:",RequiredHash="8c9a5e7f386aeeac9114b7a9c7da0c938fb8019c5456bd20bf6e6b956de388f6";
 public static readonly string[] Sources={"37612023:A0020000:123:0","37612023:A0020000:124:0","37612023:A0020000:125:0"};
 public static readonly string[] Roots={"local:8268:1673:8058:1672","local:8058:1642:8268:1643"};
 public static bool Family(string source){return source!=null&&source.Split(',').Any(Sources.Contains);}
 static void Check(bool value,string why){if(!value)throw new InvalidOperationException("PINK_PASSAGE: "+why);}
 static RoadMapEvidence Evidence(){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,ruleConfirmed=true,checkedAt="2026-11-13",month="2026-05",url="https://map.naver.com/p?c=19,0,0,0,adh&p=WfCoH8kU2zbiDF005t5wSA,270,-10,80,Float",note="User confirmed map32/P07 pedestrian-only raised pavers between internal grid and Eungye-ro bus-stop sidewalk. Also visible in Oct2024 pano F2_DjD4jF6XjsuACSPm2XQ. Vehicle graph excluded; 5.5m paver width and0.14m curb height are game estimates, not measurements."};}
 public static HashSet<string> ClosedIds(RoadMapDocument d){
  var ids=new HashSet<string>(d.lanes.Where(l=>Family(l.sourceId)).Select(l=>l.id));bool changed;
  do{changed=false;foreach(var lane in d.lanes.Where(l=>l.id.StartsWith("join:")||l.change!=0))if(lane.next.Any(ids.Contains)&&ids.Add(lane.id))changed=true;}while(changed);
  return ids;
 }
 public static bool ClosedRoad(ProjectMapObject o,HashSet<string> ids){return o.kind=="road"&&(Sources.Any(s=>o.id=="road:"+s)||ids.Any(id=>o.id=="road:polish:"+id));}
 public static RoadMapDocument Preview(ProjectMapPackage p){
  Check(p.document.revision==22&&p.contentHash==RequiredHash,"requires exact reviewed r22");Check(!p.document.scene.objects.Any(o=>o.id.StartsWith(Prefix)),"already authored");
  var closed=ClosedIds(p.document);Check(Roots.All(id=>closed.Contains(id)&&p.document.lanes.Any(l=>l.id==id&&!l.disabled)),"expected two active P07 directions");
  Check(!p.baseline.stops.Any(s=>s.graphStop&&closed.Contains(p.baseline.hdEdges[s.laneEdge].id)),"stop bound to closed passage needs explicit relocation");
  Check(!p.document.scene.objects.Any(o=>o.enabled&&closed.Contains(o.laneId)),"service/facility bound to closed passage");Check(!p.document.facilities.Any(f=>closed.Contains(f.laneId)),"traffic facility bound to closed passage");
  var d=p.document.Copy();foreach(var lane in d.lanes){if(closed.Contains(lane.id)){lane.disabled=true;lane.next=new string[0];lane.evidence=Evidence();}else lane.next=lane.next.Where(id=>!closed.Contains(id)).ToArray();}
  foreach(var o in d.scene.objects)if(ClosedRoad(o,closed))o.enabled=false;
  var a=d.lanes.Single(l=>l.id==Roots[0]);var b=d.lanes.Single(l=>l.id==Roots[1]);var west=(a.points.Last().V()+b.points[0].V())*.5f;var east=(a.points[0].V()+b.points.Last().V())*.5f;var heading=(east-west).normalized;
  Check(west.x>805&&east.x<828&&Mathf.Abs(east.z-west.z)<1,"paver endpoints remain inside confirmed P07 mouth bounds");
  var pavers=new ProjectMapObject{id=Prefix+"pavers",kind="sidewalk",name="P07 pedestrian-only raised pavers (observed; dimensions estimated)",enabled=true,height=.14f,width=5.5f,points=ProjectMapScene.Rectangle((west+east)*.5f,heading,5.5f,Vector3.Distance(west,east))};
  d.scene.objects=d.scene.objects.Concat(new[]{pavers}).ToArray();Debug.Log("PINK_PASSAGE_PREVIEW disabled="+closed.Count+" ids="+string.Join(",",closed.OrderBy(x=>x))+" pavers="+west.ToString("F2")+" -> "+east.ToString("F2")+" no-bound-stop-or-service no-save");return d;
 }
}
