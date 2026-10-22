using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
public static class RoadMapComposer {
 public static MapData Compose(MapData baseline,RoadMapDocument doc){
  if(doc.schema!=1||doc.mapId!=baseline.mapId||doc.baseHash!=RoadMapRuntime.Hash(baseline))throw new InvalidOperationException("기준 지도/스키마 불일치");
  var map=JsonUtility.FromJson<MapData>(JsonUtility.ToJson(baseline));ReviewedLaneLayout.CopyState(baseline,map);var index=new Dictionary<string,int>();for(int i=0;i<doc.lanes.Length;i++)index.Add(doc.lanes[i].id,i);
  map.hdEdges=doc.lanes.Select(l=>new HdEdge{id=l.id,sourceId=l.sourceId,fromNode=l.fromNode,toNode=l.toNode,ruleId=l.ruleId,roadName=l.roadName,status=l.status,disabled=l.disabled,number=l.number,turn=l.turn,change=l.change,sourceOffset=l.sourceOffset,points=l.points.Select((p,i)=>new MapPoint{x=p.x,z=p.z,elevation=l.heights!=null&&i<l.heights.Length?l.heights[i]:0}).ToArray(),next=l.disabled?new int[0]:l.next.Select(id=>index[id]).ToArray()}).ToArray();
  foreach(var stop in map.stops)if(stop.graphStop){string key=baseline.hdEdges[stop.laneEdge].id;if(!index.ContainsKey(key)||map.hdEdges[index[key]].disabled)throw new InvalidOperationException("승하차 접근 차로 삭제: "+key);stop.laneEdge=index[key];var path=new AssistedRoadNetwork.Path(map.hdEdges[stop.laneEdge].points.Select(p=>p.V()).ToArray());if(stop.laneAt>path.length+.1f)throw new InvalidOperationException("승하차 접근 차로 길이가 부족합니다: "+key);var p=path.Position(stop.laneAt);var dir=path.Tangent(stop.laneAt);stop.x=p.x;stop.z=p.z;stop.dx=dir.x;stop.dz=dir.z;}
  foreach(var f in doc.facilities.Where(f=>f.kind=="sign"&&f.rule)){var edge=map.hdEdges[index[f.laneId]];if(f.type=="진입 금지"){edge.disabled=true;edge.next=new int[0];foreach(var e in map.hdEdges)e.next=e.next.Where(n=>n!=index[f.laneId]).ToArray();}else if(f.type!="제한속도")edge.next=edge.next.Where(n=>map.hdEdges[n].turn!=(f.type=="좌회전 금지"?-1:1)).ToArray();}
  foreach(var program in doc.signals){var rule=map.junctionRules.First(r=>r.id==program.junctionId);rule.signalMode=2;}map.editorBaseHash=baseline.editorBaseHash;map.editorDocument=doc.Copy();map.editorRevision=doc.Version;
  var surfaces=new List<MapRoad>(map.roads);foreach(var l in doc.lanes){var old=baseline.hdEdges.FirstOrDefault(e=>e.id==l.id);if(l.disabled||old!=null&&JsonUtility.ToJson(RoadMapLane.From(old,baseline.hdEdges))==JsonUtility.ToJson(l))continue;surfaces.Add(new MapRoad{id="editor:"+l.id,name="사용자 차로 노면",width=l.width,lanes=1,oneWay=1,suppressPaint=true,points=map.hdEdges[index[l.id]].points});}map.roads=surfaces.ToArray();new HdRoadGraph(map);return map;
 }
}
