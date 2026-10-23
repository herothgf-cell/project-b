using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Targeted recovery of captured connectivity at J1927_5181. It does not add a
// west-approach left turn: source arrows B3218G610468/466/467 are straight only.
// Raw NGII B2 212 dashed boundaries, not geometric proximity alone, authorize
// the additional same-direction lane-change windows below.
public static class EungyeConnectivityRepair {
 public const string Prefix="repair:J1927_5181:";
 public const string Evidence="NGII 2020 A2/B2/B3 source; B2218G610332/2673/2674 type212; B3218G610468 kind5371; geometry provisional, current field rules unverified";
 sealed class Family {public string id;public List<int> edges;public AssistedRoadNetwork.Path path;}
 sealed class Change {public Family from,to;public float at,end;public int side;public MapPoint[] points;public string mark;}
 sealed class Part {public int index;public float from,to;}
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z,elevation=p.y};}
 static AssistedRoadNetwork.Path Path(HdEdge edge){return new AssistedRoadNetwork.Path(edge.points.Select(p=>p.V()).ToArray());}
 static MapPoint[] Slice(AssistedRoadNetwork.Path p,float a,float b){var points=new List<MapPoint>{P(p.Position(a))};float at=0;for(int i=1;i<p.points.Length;i++){at+=Vector3.Distance(p.points[i-1],p.points[i]);if(at>a+.001f&&at<b-.001f)points.Add(P(p.points[i]));}points.Add(P(p.Position(b)));return points.ToArray();}
 static string Number(float value){return value.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);}
 static bool SamePoints(MapPoint[] a,MapPoint[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(Vector3.Distance(a[i].V(),b[i].V())>.001f)return false;return true;}
 static bool Unedited(MapData map,MapData baseline,IEnumerable<int> indices){
  if(map.editorDocument==null||baseline==null)return true;
  foreach(int i in indices){var current=map.hdEdges[i];var original=Array.Find(baseline.hdEdges,e=>e.id==current.id);if(original==null||current.disabled!=original.disabled||current.turn!=original.turn||current.change!=original.change||!SamePoints(current.points,original.points))return false;
   var actual=current.next.Select(n=>map.hdEdges[n].id).OrderBy(x=>x);var expected=original.next.Select(n=>baseline.hdEdges[n].id).OrderBy(x=>x);if(!actual.SequenceEqual(expected))return false;
  }return true;
 }
 static Family GetFamily(MapData map,MapData baseline,string id){var edges=Enumerable.Range(0,map.hdEdges.Length).Where(i=>map.hdEdges[i].sourceId==id&&map.hdEdges[i].change==0&&map.hdEdges[i].turn==0&&!map.hdEdges[i].disabled&&map.hdEdges[i].id.StartsWith(id+":")).OrderBy(i=>map.hdEdges[i].sourceOffset).ToList();if(edges.Count==0||!Unedited(map,baseline,edges))return null;
  var points=new List<Vector3>();float length=0;for(int k=0;k<edges.Count;k++){var edge=map.hdEdges[edges[k]];if(Mathf.Abs(edge.sourceOffset-length)>.05f)return null;if(k>0&&(map.hdEdges[edges[k-1]].toNode!=edge.fromNode||!map.hdEdges[edges[k-1]].next.Contains(edges[k])||Vector3.Distance(points.Last(),edge.points[0].V())>.05f))return null;var path=Path(edge);length+=path.length;if(points.Count==0)points.Add(path.points[0]);points.AddRange(path.points.Skip(1));}
  return new Family{id=id,edges=edges,path=new AssistedRoadNetwork.Path(points.ToArray())};
 }
 static MapPoint[] Curve(Vector3 a,Vector3 forward,Vector3 b,Vector3 exit){float handle=Vector3.Distance(a,b)*.4f;var result=new MapPoint[49];for(int i=0;i<result.Length;i++){float t=i/(float)(result.Length-1),u=1-t;result[i]=P(u*u*u*a+3*u*u*t*(a+forward*handle)+3*u*t*t*(b-exit*handle)+t*t*t*b);}return result;}
 static HdEdge Extra(string id,string source,string from,string to,MapPoint[] points,int number,string evidence,int change=0){return new HdEdge{id=Prefix+id,sourceId=source,fromNode=from,toNode=to,points=points,number=number,change=change,turn=0,status="provisional-connectivity-repair",ruleId="J1927_5181",evidence=evidence,updated="2026-10-23",next=new int[0]};}
 public static void Apply(MapData map,MapData baseline=null){
  if(map==null||map.mapId!="eungye"||!map.hybrid||!map.HasHd||map.hdEdges.Any(e=>e.id.StartsWith(Prefix)))return;
  var geometry=new Clearance(map);var changes=new List<Change>();
  foreach(var pair in new[]{new[]{"A2218G611368","A2218G611366","B2218G610332"},new[]{"A2218G611366","A2218G611365","B2218G612674"},new[]{"A2218G611365","A2218G611364","B2218G612673"}}){
   var right=GetFamily(map,baseline,pair[0]);var left=GetFamily(map,baseline,pair[1]);var mark=Array.Find(map.hdMarks??new HdMark[0],m=>m.id==pair[2]&&m.type=="212"&&m.kind=="503");if(right==null||left==null||mark==null||mark.points.Length<2)continue;
   foreach(bool inward in new[]{true,false}){var from=inward?right:left;var to=inward?left:right;float ignored;float dashedStart=from.path.Project(mark.points[0].V(),out ignored),dashedEnd=from.path.Project(mark.points.Last().V(),out ignored);if(dashedStart>dashedEnd){float swap=dashedStart;dashedStart=dashedEnd;dashedEnd=swap;}
    foreach(float start in new[]{60f,100,132}){float error;float targetStart=to.path.Project(from.path.Position(start),out error),end=targetStart+32;if(start<dashedStart+4||start+32>dashedEnd-4||end>to.path.length-12||error<2.5f||error>4.5f||Vector3.Dot(from.path.Tangent(start),to.path.Tangent(targetStart))<.98f)continue;
     var points=new MapPoint[49];for(int i=0;i<points.Length;i++){float t=i/(float)(points.Length-1),blend=t*t*t*(t*(t*6-15)+10);points[i]=P(Vector3.Lerp(from.path.Position(start+32*t),to.path.Position(targetStart+32*t),blend));}
     if(!geometry.Clear(points,from.path.Tangent(start)))continue;changes.Add(new Change{from=from,to=to,at=start,end=end,side=inward?-1:1,points=points,mark=pair[2]});
    }
   }
  }
  var edges=new List<HdEdge>(map.hdEdges);var cuts=new Dictionary<int,SortedSet<float>>();
  Action<Family,float> cut=(family,global)=>{int index=family.edges.Last(i=>map.hdEdges[i].sourceOffset<global-.001f);float cutAt=global-map.hdEdges[index].sourceOffset;var length=Path(map.hdEdges[index]).length;if(cutAt<.01f||cutAt>length-.01f)return;SortedSet<float> values;if(!cuts.TryGetValue(index,out values)){values=new SortedSet<float>();cuts.Add(index,values);}values.Add(cutAt);};
  foreach(var change in changes){cut(change.from,change.at);cut(change.to,change.end);}
  int south=Array.FindIndex(map.hdEdges,e=>e.id=="A2218G611310:0"&&!e.disabled&&e.next.Length==0),local=Array.FindIndex(map.hdEdges,e=>e.id=="local:1903:4938:1912:2429"&&!e.disabled);MapPoint[] portal=null;float merge=18;
  if(south>=0&&local>=0&&Unedited(map,baseline,new[]{south,local})){var a=Path(map.hdEdges[south]);var b=Path(map.hdEdges[local]);var start=a.Position(a.length);float error;float overlap=b.Project(start,out error);merge=overlap+16;
   // Their capture endpoints overlap longitudinally. Merging at the local
   // start would drive backwards; enter the local lane farther downstream.
   if(error<4&&merge<b.length-20&&Vector3.Dot(a.Tangent(a.length),b.Tangent(merge))>.95f&&Vector3.Dot(b.Position(merge)-start,a.Tangent(a.length))>10){var candidate=Curve(start,a.Tangent(a.length),b.Position(merge),b.Tangent(merge));if(geometry.Clear(candidate,a.Tangent(a.length))){portal=candidate;SortedSet<float> values;if(!cuts.TryGetValue(local,out values)){values=new SortedSet<float>();cuts.Add(local,values);}values.Add(merge);}}
  }
  var parts=new Dictionary<int,List<Part>>();foreach(var item in cuts){var original=edges[item.Key];var path=Path(original);var all=new List<float>{0};all.AddRange(item.Value);all.Add(path.length);var list=new List<Part>();string oldTo=original.toNode;var oldNext=original.next;
   for(int k=0;k+1<all.Count;k++){int index=k==0?item.Key:edges.Count;string from=k==0?original.fromNode:Prefix+original.id+":split:"+Number(all[k]),to=k+2==all.Count?oldTo:Prefix+original.id+":split:"+Number(all[k+1]);var part=new HdEdge{id=k==0?original.id:Prefix+original.id+":part:"+Number(all[k]),sourceId=original.sourceId,fromNode=from,toNode=to,points=Slice(path,all[k],all[k+1]),number=original.number,turn=original.turn,change=original.change,status=original.status,ruleId=original.ruleId,roadName=original.roadName,evidence=original.evidence,updated=original.updated,sourceOffset=original.sourceOffset+all[k],next=k+2==all.Count?oldNext:new int[0]};if(k==0)edges[index]=part;else edges.Add(part);list.Add(new Part{index=index,from=all[k],to=all[k+1]});}
   for(int k=0;k+1<list.Count;k++)edges[list[k].index].next=new[]{list[k+1].index};parts[item.Key]=list;
   foreach(var stop in map.stops)if(stop.laneEdge==item.Key){var target=list.FirstOrDefault(p=>stop.laneAt<p.to-.001f)??list.Last();stop.laneEdge=target.index;stop.laneAt-=target.from;}
  }
  Func<Family,float,bool,int> locate=(family,at,ending)=>{int original=family.edges.Last(i=>map.hdEdges[i].sourceOffset<at+.001f);float distance=at-map.hdEdges[original].sourceOffset;List<Part> split;if(!parts.TryGetValue(original,out split))return original;return ending?(split.FirstOrDefault(p=>Mathf.Abs(p.to-distance)<.01f)??split.First(p=>distance>=p.from-.01f&&distance<=p.to+.01f)).index:(split.FirstOrDefault(p=>Mathf.Abs(p.from-distance)<.01f)??split.First(p=>distance>=p.from-.01f&&distance<=p.to+.01f)).index;};
  foreach(var change in changes){int from=locate(change.from,change.at,true),to=locate(change.to,change.end,false);var edge=Extra("change:"+change.from.id+":"+change.to.id+":"+Number(change.at),change.from.id+","+change.to.id,edges[from].toNode,edges[to].fromNode,change.points,edges[to].number,Evidence+" / "+change.mark,change.side);edge.next=new[]{to};edges[from].next=edges[from].next.Concat(new[]{edges.Count}).ToArray();edges.Add(edge);}
  if(portal!=null){var target=parts[local].First(p=>Mathf.Abs(p.from-merge)<.01f);var edge=Extra("south-portal","A2218G611310 + "+map.hdEdges[local].sourceId,edges[south].toNode,edges[target.index].fromNode,portal,1,Evidence+" / forward merge after 5.70m overlapping capture gap");edge.next=new[]{target.index};edges[south].next=new[]{edges.Count};edges.Add(edge);}
  // Captured third approach has a straight arrow but no captured connector.
  // Continue to the westbound outer exit; never synthesize a left turn south.
  int third=edges.FindIndex(e=>e.id=="A2218G611366:3"),west=edges.FindIndex(e=>e.id=="A2218G611305:0"&&!e.disabled);
  // The generated hdMarks retain B2 lines but omit B3 arrow polygons. Keep the
  // reviewed source excerpt explicit: B3218G610468 / type1 / kind5371 /
  // link A2218G611366 / first vertex (228.195167,516.652832), updated20201011.
  // Pin this inference to those exact captured endpoint locations and headings.
  bool capturedArrow=third>=0&&west>=0&&Vector3.Distance(map.hdEdges[third].points.Last().V(),new Vector3(222.11833f,0,518.43613f))<.2f&&Vector3.Distance(edges[west].points[0].V(),new Vector3(173.34722f,0,528.71651f))<.2f;
  if(third>=0&&west>=0&&capturedArrow){int originalThird=Array.FindIndex(map.hdEdges,e=>e.id=="A2218G611366:3");int originalWest=Array.FindIndex(map.hdEdges,e=>e.id=="A2218G611305:0");if(Unedited(map,baseline,new[]{originalThird,originalWest})){List<Part> split;if(parts.TryGetValue(third,out split))third=split.Last().index;var a=Path(edges[third]);var b=Path(edges[west]);if(edges[third].next.Length==0&&Vector3.Dot(a.Tangent(a.length),b.Tangent(0))>.97f&&Vector3.Distance(a.Position(a.length),b.Position(0))<60){var points=Curve(a.Position(a.length),a.Tangent(a.length),b.Position(0),b.Tangent(0));if(geometry.Clear(points,a.Tangent(a.length))){var edge=Extra("straight-third", "A2218G611366 + A2218G611305",edges[third].toNode,edges[west].fromNode,points,4,Evidence+" / missing straight connector; not a left-turn permission");edge.next=new[]{west};edges[third].next=new[]{edges.Count};edges.Add(edge);}else Debug.Log("EUNGYE_CONNECTIVITY_STRAIGHT_SKIPPED "+geometry.Reason);}}}
  map.hdEdges=edges.ToArray();new HdRoadGraph(map);Debug.Log("EUNGYE_CONNECTIVITY_REPAIR added="+map.hdEdges.Count(e=>e.id.StartsWith(Prefix)&&e.status=="provisional-connectivity-repair")+" laneChanges="+changes.Count+" southPortal="+(portal!=null)+" straightThird="+map.hdEdges.Any(e=>e.id==Prefix+"straight-third")+" / current field left-turn permission unverified; no left turn added");
 }
 public static bool FootprintClear(MapData map,HdEdge edge){var p=Path(edge);return new Clearance(map).Clear(edge.points,p.Tangent(0));}
 sealed class Clearance {
  public string Reason="";
  readonly MapData map;readonly Dictionary<Vector2Int,List<Tuple<Vector3,Vector3,float>>> roads=new Dictionary<Vector2Int,List<Tuple<Vector3,Vector3,float>>>();readonly Dictionary<Vector2Int,List<MapPoint[]>> blockers=new Dictionary<Vector2Int,List<MapPoint[]>>();
  static Vector2Int Key(Vector3 p){return new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.z/32));}
  static void Add<T>(Dictionary<Vector2Int,List<T>> cells,T item,Vector3[] points,float margin){float minX=points.Min(p=>p.x)-margin,maxX=points.Max(p=>p.x)+margin,minZ=points.Min(p=>p.z)-margin,maxZ=points.Max(p=>p.z)+margin;for(int x=Mathf.FloorToInt(minX/32);x<=Mathf.FloorToInt(maxX/32);x++)for(int z=Mathf.FloorToInt(minZ/32);z<=Mathf.FloorToInt(maxZ/32);z++){var key=new Vector2Int(x,z);List<T> values;if(!cells.TryGetValue(key,out values)){values=new List<T>();cells.Add(key,values);}values.Add(item);}}
  public Clearance(MapData map){this.map=map;foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){var a=road.points[i-1].V();var b=road.points[i].V();Add(roads,Tuple.Create(a,b,road.width*.5f),new[]{a,b},road.width*.5f);}foreach(var b in map.buildings)if(!SceneBuildingVisibility.Hidden(map,b.id))Add(blockers,b.points,b.points.Select(p=>p.V()).ToArray(),0);foreach(var a in map.areas)if(a.kind=="water")Add(blockers,a.points,a.points.Select(p=>p.V()).ToArray(),0);}
  bool Road(Vector3 p){List<Tuple<Vector3,Vector3,float>> values;if(!roads.TryGetValue(Key(p),out values))return false;return values.Any(r=>{var d=r.Item2-r.Item1;return Vector3.Distance(p,r.Item1+d*Mathf.Clamp01(Vector3.Dot(p-r.Item1,d)/Mathf.Max(.001f,d.sqrMagnitude)))<r.Item3-.05f;});}
  bool Blocked(Vector3 p){List<MapPoint[]> rings;return blockers.TryGetValue(Key(p),out rings)&&rings.Any(r=>ProjectMapScene.Contains(r,p));}
  public bool Clear(MapPoint[] points,Vector3 heading){Reason="";var path=new AssistedRoadNetwork.Path(points.Select(p=>p.V()).ToArray());for(float at=0;at<=path.length+.01f;at+=1){float d=Mathf.Min(at,path.length);var dir=path.Tangent(d);if(Vector3.Dot(dir,heading)<.8f){Reason="reverse/abrupt heading";return false;}foreach(float along in new[]{-2.25f,0,2.25f})foreach(float side in new[]{-.95f,0,.95f}){var p=path.Position(d)+dir*along+TrafficSystem.Right(dir)*side;if(!Road(p)){Reason="vehicle footprint outside roadway "+p.ToString("F2");return false;}if(ProjectMapScene.OnArea(map,p,true)){Reason="vehicle footprint on sidewalk "+p.ToString("F2");return false;}if(Blocked(p)){Reason="vehicle footprint in building/water "+p.ToString("F2");return false;}}}return true;}
 }
}
