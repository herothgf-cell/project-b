using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// Construction day card: one curb-side lane closes for the day.
// Site 0 is survey #48 (Y08), westbound Gyeysu-ro in front of the Hyundai Oilbank.
// 2026-11-28: six more sites across Eungye. Each was found by DailyRoadConstructionSiteFinder
// (straight outermost lane, open lane of the same direction on its left, no passenger stop,
// predecessors and successors keep another way, every stop pair stays reachable) and is
// re-checked by DailyRoadConstructionChecks. A campaign moves to the next site on each
// construction day (the condition rotation repeats every five days).
public static class DailyRoadConstruction {
 public sealed class Site {public readonly string laneId,road,place;public Site(string laneId,string road,string place){this.laneId=laneId;this.road=road;this.place=place;}}
 public static readonly Site[] Sites={
  new Site("local:11589:16123:10729:15920:part:1","계수로","현대오일뱅크"),
  new Site("local:3323:12871:2647:12412:part:1","대은로","우석타워"),
  new Site("local:4457:13181:5915:11994:part:1","은계로","은행우남아파트"),
  new Site("local:2189:7834:983:5801:part:1","은행로","벽산3차아파트"),
  new Site("local:7918:6167:7847:3996:part:1","은계로","은계브리즈힐"),
  new Site("local:14344:13354:10560:1555:part:1","마유로","진성테크"),
  new Site("local:5992:16789:7141:14981:part:1","은계중앙로","우미린레이크"),
 };
 public const string LaneId="local:11589:16123:10729:15920:part:1";
 public static int ActiveSite {get;private set;}
 public static Site Active {get{return Sites[ActiveSite];}}
 public static string RoadLabel {get{return Active.road+" 서행 · "+Active.place+" 앞";}}

 // Same campaign and day = same site; consecutive construction days use consecutive sites.
 public static int SiteFor(string seed,int conditionDay){
  uint state=2166136261;foreach(char c in seed??""){state^=c;state*=16777619;}
  int cycle=(Math.Max(1,conditionDay)-1)/5;
  return (int)((state%(uint)Sites.Length+(uint)cycle)%(uint)Sites.Length);
 }

 // {road} and {place} in condition-card and radio text. Texts saved from the single-site
 // version still name Gyeysu-ro; they are swapped for the current site.
 public static string Fill(string text){
  if(string.IsNullOrEmpty(text))return text;
  if(ActiveSite!=0){
   text=text.Replace("계수로 현대오일뱅크 앞","{road} {place} 앞").Replace("현대오일뱅크 앞","{place} 앞").Replace("계수로","{road}");
  }
  return text.Replace("{road}",Active.road).Replace("{place}",Active.place);
 }

 sealed class Segment {
  public int edge=-1;
  public Vector3[] points=Array.Empty<Vector3>();
 }
 static readonly ConditionalWeakTable<MapData,Segment[]> segments=new ConditionalWeakTable<MapData,Segment[]>();
 static Segment For(MapData map,int site){
  if(map==null||site<0||site>=Sites.Length)return null;
  return segments.GetValue(map,m=>new Segment[Sites.Length])[site]??=Find(map,site);
 }
 static Segment Find(MapData map,int site){
  var result=new Segment();
  if(map.mapId!="eungye"||!map.hybrid||map.hdEdges==null)return result;
  string id=Sites[site].laneId;
  int edge=Array.FindIndex(map.hdEdges,e=>e.id==id&&!e.disabled);
  if(edge<0)return result;
  result.edge=edge;
  result.points=Array.ConvertAll(map.hdEdges[edge].points,p=>p.V());
  return result;
 }
 public static int EdgeIndex(MapData map){return EdgeIndex(map,ActiveSite);}
 public static int EdgeIndex(MapData map,int site){return For(map,site)?.edge??-1;}
 // World-space centerline for map overlays. The returned points are shared.
 public static Vector3[] Preview(MapData map){return Preview(map,ActiveSite);}
 public static Vector3[] Preview(MapData map,int site){return For(map,site)?.points??Array.Empty<Vector3>();}
 static bool Supported(DriveSession session){
  return session&&session.world&&session.business&&!session.business.SimulationSession&&!RoadMapRuntime.Trial&&!PracticeSession.Enabled;
 }
 public static Vector3[] Preview(DriveSession session){return Supported(session)?Preview(session.world.map):Array.Empty<Vector3>();}

 // Map-only use (editor checks). Selecting another site reopens the previous one.
 public static bool Apply(MapData map,bool closed){return Apply(map,closed,ActiveSite);}
 public static bool Apply(MapData map,bool closed,int site){
  if(site<0||site>=Sites.Length)return false;
  if(site!=ActiveSite){int old=EdgeIndex(map,ActiveSite);if(old>=0)map.hdEdges[old].ClosedForConstruction=false;ActiveSite=site;}
  int index=EdgeIndex(map,site);
  if(index<0)return false;
  map.hdEdges[index].ClosedForConstruction=closed;
  return true;
 }

 public static bool Apply(DriveSession session,bool closed){return Apply(session,closed,ActiveSite);}
 public static bool Apply(DriveSession session,bool closed,int site){
  if(!Supported(session)||site<0||site>=Sites.Length)return false;
  if(site!=ActiveSite){Set(session,ActiveSite,false);ActiveSite=site;}
  return Set(session,site,closed);
 }
 static bool Set(DriveSession session,int site,bool closed){
  var map=session.world.map;
  int index=EdgeIndex(map,site);
  if(index<0)return false;
  var lane=map.hdEdges[index];
  if(lane.ClosedForConstruction!=closed){
   lane.ClosedForConstruction=closed;
   if(session.jobs&&session.jobs.nav)session.jobs.nav.LaneAvailabilityChanged(index,closed);
   var district=session.traffic?session.traffic.GetComponent<DistrictTraffic>():null;
   if(district)district.LaneAvailabilityChanged(index,closed);
  }
  if(site==ActiveSite)ConstructionZoneProps.Sync(session,closed);
  return true;
 }
}
