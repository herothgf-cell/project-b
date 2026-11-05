using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// Fictional workshops. Input map JSON/OSM is never rewritten. Candidate footprints
// are checked against the actual loaded geometry before becoming usable facilities.
public static class WorkshopLayout {
 public sealed class Site {
  public string id,name;public MapStop approach;public Vector3 repair,waiting,direction,building;public Vector3[] tow,entry,departure,waitingEntry,waitingToRepair;public MapRoad[] surfaces;
  public bool CanRepair(Vector3 position,Vector3 heading,float speed){var d=position-repair;return speed<.56f&&Mathf.Abs(d.y)<1.5f&&Vector3.Dot(heading.normalized,direction)>.9f&&Mathf.Abs(Vector3.Dot(d,direction))<2.8f&&Mathf.Abs(Vector3.Dot(d,TrafficSystem.Right(direction)))<1.15f;}
 }
 sealed class Layout {public readonly List<Site> sites=new List<Site>();}
 static readonly ConditionalWeakTable<MapData,Layout> cache=new ConditionalWeakTable<MapData,Layout>();
 public static IReadOnlyList<Site> For(MapData map){if(map==null)return new Site[0];return cache.GetValue(map,Build).sites;}
 public static bool Contains(MapData map,Vector3 point){
  foreach(var site in For(map))if((site.repair-point).sqrMagnitude<130*130)foreach(var surface in site.surfaces)for(int i=1;i<surface.points.Length;i++){var a=surface.points[i-1].V();var d=surface.points[i].V()-a;var flat=point;flat.y=a.y;float t=Mathf.Clamp01(Vector3.Dot(flat-a,d)/Mathf.Max(.001f,d.sqrMagnitude));if((flat-a-d*t).sqrMagnitude<=surface.width*surface.width*.25f)return true;}return false;
 }
 static MapRoad Surface(string id,float width,IList<Vector3> points){var p=new MapPoint[points.Count];for(int i=0;i<p.Length;i++)p[i]=new MapPoint{x=points[i].x,z=points[i].z,elevation=points[i].y};return new MapRoad{id=id,name="공업사 게임시설",width=width,points=p};}
 static Layout Build(MapData map){
  var result=new Layout();if(!map.HasHd||map.mapId!="eungye"&&map.mapId!="pvp-city-a")return result;
  var graph=new HdRoadGraph(map);
  // Stable edge order and positions keep paid bay IDs stable across sessions.
  for(int edge=0;edge<map.hdEdges.Length&&result.sites.Count<2;edge++){
   var e=map.hdEdges[edge];var path=graph.paths[edge];if(e.disabled||path.length<110||e.turn!=0)continue;
   for(float at=50;at<path.length-50&&result.sites.Count<2;at+=35){
    var centre=path.Position(at);var dir=path.Tangent(at);if(Vector3.Angle(dir,path.Tangent(at-35))>2||Vector3.Angle(dir,path.Tangent(at+35))>2)continue;
    bool close=false;foreach(var s in result.sites)if(Vector3.Distance(s.repair,centre)<650)close=true;if(close)continue;
    foreach(float depth in new[]{24f,32f,42f}){var site=Candidate(map,graph,edge,at,depth,result.sites.Count);
     try{Validate(map,site);}catch(InvalidOperationException){continue;}
     result.sites.Add(site);Debug.Log("WORKSHOP_SITE "+map.mapId+" "+site.id+" lane="+e.id+" at="+at+" position="+site.repair);break;
    }
   }
  }
  // No silent partial migration: old supported fuel repair remains the fallback.
  if(result.sites.Count!=2){Debug.LogWarning("WORKSHOP_LAYOUT_UNAVAILABLE "+map.mapId+" validated="+result.sites.Count+"; legacy facilities retained");result.sites.Clear();}
  return result;
 }
 static Site Candidate(MapData map,HdRoadGraph graph,int edge,float at,float depth,int index){
  var path=graph.paths[edge];var centre=path.Position(at);var direction=path.Tangent(at);var right=TrafficSystem.Right(direction);
  Func<float,float,Vector3> q=(along,across)=>centre+direction*along+right*across;
  var entry=new List<Vector3>();var departure=new List<Vector3>();
  for(int i=0;i<=32;i++){float t=i/32f,a,b;Offset(t,30,depth,out a,out b);entry.Add(q(-36+a,b));Offset(t,30,depth,out a,out b);departure.Add(q(6+a,depth-b));}
  var repair=q(0,depth);entry.Add(repair);departure.Insert(0,repair);
  var waitEntry=new List<Vector3>();var waitRepair=new List<Vector3>();for(int i=0;i<=32;i++){float a,b;Offset(i/32f,25,depth+5,out a,out b);waitEntry.Add(q(-36+a,b));Offset(i/32f,11,5,out a,out b);waitRepair.Add(q(-11+a,depth+5-b));}
  string id="workshop:"+map.mapId+":"+index;string name=(map.mapId=="eungye"?"은계":"A2")+(index==0?" 서부":" 동부")+" 공업사 (게임시설)";
  var p=path.Position(at-40);var h=path.Tangent(at-40);
  return new Site{id=id,name=name,approach=new MapStop{name=name+" 입구",x=p.x,z=p.z,elevation=p.y,dx=h.x,dz=h.z,width=3,graphStop=true,laneEdge=edge,laneAt=at-40},repair=repair,waiting=q(-11,depth+5),tow=new[]{q(0,depth+5),q(0,depth+10)},building=q(0,depth+16),direction=direction,entry=entry.ToArray(),departure=departure.ToArray(),waitingEntry=waitEntry.ToArray(),waitingToRepair=waitRepair.ToArray(),surfaces=new[]{Surface(id+":court",24,new[]{q(-14,depth+5),q(14,depth+5)}),Surface(id+":entry",5,entry),Surface(id+":exit",5,departure),Surface(id+":waiting-entry",5,waitEntry),Surface(id+":waiting-merge",5,waitRepair)}};
 }
 static void Offset(float t,float advance,float offset,out float along,out float across){float radius=(advance*advance+offset*offset)/(4*offset),angle=2*Mathf.Atan2(offset,advance);if(t<=.5f){float a=angle*t*2;along=radius*Mathf.Sin(a);across=radius*(1-Mathf.Cos(a));}else {float a=angle*(2-2*t);along=advance-radius*Mathf.Sin(a);across=offset-radius*(1-Mathf.Cos(a));}}
 public static void Validate(MapData map,Site site){
  FuelStationLayout.ValidatePlot(map,site.surfaces,site.id);
  foreach(var stop in map.stops)if(Vector3.Distance(stop.Kerb,site.repair)<42)throw new InvalidOperationException("Workshop overlaps passenger stop");
  foreach(var station in FuelStationLayout.For(map))if(Vector3.Distance(station.pump,site.repair)<65)throw new InvalidOperationException("Workshop overlaps fuel facility");
  if(ProjectMapScene.Active(map))foreach(string kind in new[]{"restaurant","rest","parking"})foreach(var obj in ProjectMapScene.Services(map,kind))if(Vector3.Distance(obj.Position,site.repair)<55)throw new InvalidOperationException("Workshop overlaps service facility");
  // The full building, waiting and tow footprint stays inside the validated court.
  var building=Surface(site.id+":building",6,new[]{site.building-site.direction*9,site.building+site.direction*9});FuelStationLayout.ValidatePlot(map,new[]{building},site.id);
 }
 public static void Draw(MapData map,Transform parent){
  if(parent.Find("자동차 공업사"))return;var root=new GameObject("자동차 공업사");root.transform.SetParent(parent,false);
  var concrete=WorldBuilder.Mat("Workshop paving",new Color(.55f,.56f,.54f));var blue=WorldBuilder.Mat("Workshop blue",new Color(.12f,.38f,.64f));var paint=WorldBuilder.Mat("Workshop yellow",new Color(1,.8f,.14f));
  foreach(var site in For(map)){
   var lot=new GameObject(site.name);lot.transform.SetParent(root.transform,false);lot.transform.position=site.repair;lot.transform.rotation=Quaternion.LookRotation(site.direction);
   foreach(var surface in site.surfaces)for(int i=1;i<surface.points.Length;i++){var a=surface.points[i-1].V();var b=surface.points[i].V();var g=WorldBuilder.Box("공업사 노면",(a+b)/2+Vector3.up*.035f,new Vector3(surface.width,.06f,Vector3.Distance(a,b)+.12f),concrete,root.transform,false);g.transform.rotation=Quaternion.LookRotation(b-a);}
   WorldBuilder.Box("정비 사무동",lot.transform.InverseTransformPoint(site.building)+Vector3.up*2,new Vector3(6,4,18),blue,lot.transform);
   var positions=new[]{site.repair,site.tow[0],site.tow[1],site.waiting};var names=new[]{"일반 수리 · F7","견인 전용 1","견인 전용 2","대기 · 결제 없음"};
   for(int i=0;i<positions.Length;i++){var local=lot.transform.InverseTransformPoint(positions[i]);foreach(float x in new[]{-1.6f,1.6f})WorldBuilder.Box(names[i],local+new Vector3(x,.08f,0),new Vector3(.12f,.02f,6.5f),paint,lot.transform,false);Label(lot.transform,names[i],local+Vector3.up*1.8f);}
   Label(lot.transform,site.name+"\n수리·견인 / 주유·세차는 주유소",new Vector3(14,5,0));
   Label(lot.transform,"입구 → 일반 정비칸",lot.transform.InverseTransformPoint(site.entry[0])+Vector3.up*2.5f);
   Label(lot.transform,"출구 · 도로 차량에 양보",lot.transform.InverseTransformPoint(site.departure[site.departure.Length-1])+Vector3.up*2.5f);
  }
 }
 static void Label(Transform parent,string text,Vector3 position){var g=new GameObject(text);g.transform.SetParent(parent,false);g.transform.localPosition=position;var label=g.AddComponent<TextMesh>();label.text=text;label.fontSize=36;label.characterSize=.07f;label.anchor=TextAnchor.MiddleCenter;label.color=Color.white;KoreanText.Apply(label);g.AddComponent<FaceCamera>();}
}
