using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PinkEastUnmarkedMapTests {
 static readonly string[] Ids={"local:8785:940:8785:1578","local:8737:1578:8737:940","local:8785:1734:8786:2476","local:8738:2476:8737:1734"};
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("PINK_EAST_UNMARKED: "+why);}
 static bool Interior(Vector3 p,Vector3 heading){return p.x>872&&p.x<881&&(p.z>104&&p.z<147||p.z>183&&p.z<237)&&Mathf.Abs(heading.z)>.85f;}
 static Dictionary<string,int> Counts(MapData map){var result=new Dictionary<string,int>{{"Centre line",0},{"Lane divider",0},{"Road edge",0}};RoadPaintGeometry.Paint(map,null,(n,p,d,w,l,y)=>{if(Interior(p,d)&&result.ContainsKey(n))result[n]++;});return result;}
 public static void PreviewChecks(){
  var type=typeof(PinkEastUnmarkedMapTests).Assembly.GetType("PinkEastUnmarkedMapEdit");Check(type!=null,"P12/P21 preview authoring exists");
  var preview=type.GetMethod("Preview");Check(preview!=null,"preview-only API exists");
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);string before=JsonUtility.ToJson(package);
  var doc=(RoadMapDocument)preview.Invoke(null,new object[]{package});
  Check(before==JsonUtility.ToJson(package),"input package/baseline untouched");Check(doc.ToJson()==((RoadMapDocument)preview.Invoke(null,new object[]{package})).ToJson(),"preview deterministic");
  Check(doc.lanes.Length==package.document.lanes.Length,"lane inventory unchanged");for(int i=0;i<doc.lanes.Length;i++)Check(JsonUtility.ToJson(doc.lanes[i])==JsonUtility.ToJson(package.document.lanes[i]),"geometry/permissions/source identity changed "+doc.lanes[i].id);
  Check(JsonUtility.ToJson(doc.scene)==JsonUtility.ToJson(package.document.scene),"road widths/surfaces/services unchanged");Check(doc.signals.Length==package.document.signals.Length,"signal count unchanged");for(int i=0;i<doc.signals.Length;i++)Check(JsonUtility.ToJson(doc.signals[i])==JsonUtility.ToJson(package.document.signals[i]),"signal permissions changed");
  var added=doc.facilities.Where(f=>!package.document.facilities.Any(old=>old.id==f.id)).ToArray();Check(added.Length==8,"exactly two appearance declarations for each of four direction-segments");
  foreach(var id in Ids){var lane=doc.lanes.Single(l=>l.id==id);var unmarked=added.Single(f=>f.laneId==id&&ReviewedUnmarkedRoadPaint.IsFeature(f));var arrows=added.Single(f=>f.laneId==id&&LaneDirectionPaint.IsArrowSuppression(f));Check(unmarked.startAt==lane.sourceOffset&&unmarked.endAt>unmarked.startAt&&arrows.junctionId==lane.ruleId,"full active piece with matching junction ID "+id);}
  Check(added.All(f=>Ids.Contains(f.laneId)&&!f.rule),"no school east-west road or legal rule touched");foreach(var f in package.document.facilities)Check(JsonUtility.ToJson(f)==JsonUtility.ToJson(doc.facilities.Single(a=>a.id==f.id)),"existing facility changed");
  var errors=RoadMapValidator.Validate(package.baseline,doc).Where(e=>e.error).ToArray();Check(errors.Length==0,string.Join(" / ",errors.Take(10).Select(e=>e.ToString())));
  var originalMap=package.Compose();var map=RoadMapComposer.Compose(package.baseline,doc);var originalPaint=Counts(originalMap);var paint=Counts(map);
  Check(originalPaint["Centre line"]+originalPaint["Lane divider"]>0,"fixture originally has interior synthetic markings");Check(paint["Centre line"]==0&&paint["Lane divider"]==0,"actual P12/P21 interior compiled paint is unmarked");Check(paint["Road edge"]==originalPaint["Road edge"]&&paint["Road edge"]>0,"road-edge paint unchanged");
  var arrowsAfter=LaneDirectionPaint.Layout(map);Check(!arrowsAfter.Any(a=>Ids.Contains(map.hdEdges[a.edgeIndex].id)),"all four segments have no direction arrows");
  var arrowsBefore=LaneDirectionPaint.Layout(originalMap).Where(a=>!Ids.Contains(originalMap.hdEdges[a.edgeIndex].id)).Select(a=>originalMap.hdEdges[a.edgeIndex].id+":"+a.turns).OrderBy(s=>s);var unrelated=arrowsAfter.Where(a=>!Ids.Contains(map.hdEdges[a.edgeIndex].id)).Select(a=>map.hdEdges[a.edgeIndex].id+":"+a.turns).OrderBy(s=>s);Check(arrowsBefore.SequenceEqual(unrelated),"unrelated movement arrows unchanged");
  Check(originalMap.stops.Length==map.stops.Length,"stop count unchanged");for(int i=0;i<map.stops.Length;i++)Check(JsonUtility.ToJson(originalMap.stops[i])==JsonUtility.ToJson(map.stops[i]),"stop identity/position/binding changed");
  Debug.Log("PINK_EAST_UNMARKED_PREVIEW_OK P12/P21 four directed segments, 8 appearance facilities, no save, no surface/speedbump changes");
 }
}
