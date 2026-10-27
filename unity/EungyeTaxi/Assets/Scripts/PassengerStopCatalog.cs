using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable] public sealed class PassengerStopCandidate {
 public string stopId,name,category,evidenceStatus,evidence,laneId,sidewalkId;
 public float laneAt,x,z,dx,dz,bayOffset,passengerX,passengerZ;
}
[Serializable] public sealed class PassengerStopCatalogData {
 public int schema;public string mapId,mapRevision,revision;public PassengerStopCandidate[] candidates;
}
public sealed class PassengerStopEntry {
 public string StopId,MapRevision,CatalogRevision,Category,EvidenceStatus,Evidence,LaneId;
 public MapStop Stop;public int Index;
}

// Explicit fictional game laybys. Geometry review never asserts real-world
// stopping permission. Canonical map inputs and the twelve scenario stops stay intact.
public sealed class PassengerStopCatalog {
 public MapStop[] Stops {get;private set;}
 public readonly List<PassengerStopEntry> Entries=new List<PassengerStopEntry>();
 public readonly List<string> Rejected=new List<string>();
 public int LegacyCount {get;private set;}
 public string Revision {get;private set;}
 readonly List<PassengerStopCandidate> admitted=new List<PassengerStopCandidate>();
 static readonly ConditionalWeakTable<MapData,PassengerStopCatalog> cache=new ConditionalWeakTable<MapData,PassengerStopCatalog>();
 public static PassengerStopCatalog For(MapData map){PassengerStopCatalog result;if(!cache.TryGetValue(map,out result)){Apply(map);cache.TryGetValue(map,out result);}return result;}
 public static void Apply(MapData map){
  PassengerStopCatalog prior;if(cache.TryGetValue(map,out prior))return;
  var legacyStops=(MapStop[])map.stops.Clone();
  // S09's third lane is 3.04 m inside the right road edge in the packaged
  // Eungye map. Its original width=3 put the taxi target at lane center and
  // the waiting passenger on asphalt. Keep the canonical map object intact.
  if(map.mapId=="eungye"&&map.hdEdges!=null&&legacyStops.Length>8){var s=legacyStops[8];if(s!=null&&s.name=="은계 승하차 9"&&s.graphStop&&s.laneEdge>=0&&s.laneEdge<map.hdEdges.Length&&map.hdEdges[s.laneEdge].id=="local:7003:14854:5837:14446:part:1"&&Mathf.Abs(s.width-3)<.01f&&Mathf.Abs(s.laneAt-15.99997f)<.02f&&Mathf.Abs(s.x-675.727905f)<.02f&&Mathf.Abs(s.z-1476.793335f)<.02f&&Mathf.Abs(s.dx+0.94404477f)<.01f&&Mathf.Abs(s.dz+0.32981756f)<.01f){
   // The packaged sidewalk:stop:8 anchor is on asphalt. Keep a separately
   // edited sidewalk anchor even when the taxi's original stop is unchanged.
   bool packagedAnchor=s.hasPassengerAnchor&&Vector3.Distance(s.passengerAnchor,new Vector3(674.7879028f,.16f,1479.4838867f))<.001f;
   legacyStops[8]=new MapStop{name=s.name,x=s.x,z=s.z,elevation=s.elevation,dx=s.dx,dz=s.dz,width=6.6f,laneEdge=s.laneEdge,laneAt=s.laneAt,graphStop=s.graphStop,hasPassengerAnchor=s.hasPassengerAnchor&&!packagedAnchor,passengerAnchor=s.passengerAnchor};}}
  var result=new PassengerStopCatalog{Stops=legacyStops,LegacyCount=legacyStops.Length,Revision="legacy"};cache.Add(map,result);
  for(int i=0;i<legacyStops.Length;i++)result.Entries.Add(new PassengerStopEntry{StopId="S"+(i+1).ToString("00"),Index=i,Stop=legacyStops[i],EvidenceStatus="legacy-scenario",Category="scenario",MapRevision=map.editorRevision});
  if(map.mapId!="eungye"||!map.HasHd||!ProjectMapScene.Active(map))return;
  var asset=Resources.Load<TextAsset>("eungye-passenger-stops");if(!asset)return;
  var data=JsonUtility.FromJson<PassengerStopCatalogData>(asset.text);
  if(data==null||data.schema!=1||data.mapId!=map.mapId||data.candidates==null)return;
  result.Revision=data.revision;var graph=new HdRoadGraph(map);var geometry=new Geometry(map);var used=new HashSet<string>();var combined=new List<MapStop>(legacyStops);
  foreach(var c in data.candidates){
   string reason;MapStop stop;
   if(c==null||string.IsNullOrEmpty(c.stopId)||!used.Add(c.stopId)){result.Rejected.Add("duplicate/missing stopId");continue;}
   if(!Validate(map,graph,geometry,c,out stop,out reason)){result.Rejected.Add(c.stopId+": "+reason);continue;}
   if(combined.Any(s=>FlatDistance(s.Kerb,stop.Kerb)<50)){result.Rejected.Add(c.stopId+": nearby stop");continue;}
   bool routes=true;foreach(var legacy in map.stops){if(graph.Route(legacy.laneEdge,legacy.laneAt,stop.laneEdge,stop.laneAt)==null||graph.Route(stop.laneEdge,stop.laneAt,legacy.laneEdge,legacy.laneAt)==null){routes=false;break;}}
   if(!routes){result.Rejected.Add(c.stopId+": disconnected directed approach/exit");continue;}
   result.Entries.Add(new PassengerStopEntry{StopId=c.stopId,Index=combined.Count,Stop=stop,LaneId=c.laneId,MapRevision=data.mapRevision,CatalogRevision=data.revision,Category=c.category,EvidenceStatus=c.evidenceStatus,Evidence=c.evidence});
   combined.Add(stop);result.admitted.Add(c);
  }
  // Validate every candidate against the unchanged scene before making cutouts.
  foreach(var c in result.admitted.ToArray())try{InstallBay(map,c);}catch(InvalidOperationException error){var entry=result.Entries.First(e=>e.StopId==c.stopId);combined.Remove(entry.Stop);result.Entries.Remove(entry);result.admitted.Remove(c);result.Rejected.Add(c.stopId+": "+error.Message);}
  for(int i=0;i<result.Entries.Count;i++)result.Entries[i].Index=i;
  result.Stops=combined.ToArray();ProjectMapScene.InvalidateAreaIndex(map);
 }
 public PassengerStopEntry Entry(int index){return index>=0&&index<Entries.Count?Entries[index]:null;}
 public int IndexOf(string id){var e=Entries.FirstOrDefault(x=>x.StopId==id);return e==null?-1:e.Index;}
 public bool IsAdditional(int index){return index>=LegacyCount&&index<Stops.Length;}
 public static float RepeatPenalty(int pickup,int drop,int previousPickup,int previousDrop){return pickup==previousPickup&&drop==previousDrop?2500:pickup==previousPickup||drop==previousDrop?180:0;}
 public static bool Reachable(HdRoadGraph graph,MapStop from,MapStop to,out float distance){distance=float.PositiveInfinity;if(graph==null||from==null||to==null)return false;var route=graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt);if(route==null)return false;distance=route.path.length;return true;}
 public static float DirectedPickup(HdRoadGraph graph,Vector3 position,Vector3 direction,MapStop stop){if(graph==null)return float.PositiveInfinity;float at;int edge=graph.Nearest(position,direction,out at);if(edge==stop.laneEdge&&Mathf.Abs(at-stop.laneAt)<.25f)return 0;var path=graph.Route(edge,at,stop.laneEdge,stop.laneAt);return path==null?float.PositiveInfinity:path.path.length;}
 static float FlatDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
 static Vector3 Pos(PassengerStopCandidate c){return new Vector3(c.x,0,c.z);}
 static Vector3 Dir(PassengerStopCandidate c){return new Vector3(c.dx,0,c.dz).normalized;}
 static bool Validate(MapData map,HdRoadGraph graph,Geometry g,PassengerStopCandidate c,out MapStop stop,out string reason){
  stop=null;reason="";if(c.evidenceStatus!="game-only-field-unverified"){reason="not a reviewed fictional game bay";return false;}
  int edge=Array.FindIndex(map.hdEdges,e=>e.id==c.laneId&&!e.disabled&&e.turn==0&&e.change==0);
  if(edge<0){reason="approach lane removed/disabled";return false;}var path=graph.paths[edge];float localAt=c.laneAt;
  // Authoring-only cuts are not new junctions. Keep the exact original
  // world anchor and full continuous approach for bay-clearance checks, while
  // navigation targets the owning downstream segment. Never nearest-snap.
  if(map.editorDocument!=null){var lineage=map.editorDocument.lanes.Where(l=>RoadMapAuthoringOperations.Origin(l)==c.laneId).OrderBy(l=>l.sourceOffset).ToArray();
   if(lineage.Length>1){var points=new List<Vector3>();bool continuous=true;float offset=lineage[0].sourceOffset;
    for(int k=0;k<lineage.Length;k++){var lane=lineage[k];int index=Array.FindIndex(graph.edges,e=>e.id==lane.id&&!e.disabled&&e.turn==0&&e.change==0);if(index<0){continuous=false;break;}var segment=graph.paths[index];if(k>0&&(Mathf.Abs(lane.sourceOffset-offset)>.02f||lineage[k-1].toNode!=lane.fromNode||!lineage[k-1].next.Contains(lane.id)||Vector3.Distance(points.Last(),segment.points[0])>.02f)){continuous=false;break;}if(k==0)points.Add(segment.points[0]);points.AddRange(segment.points.Skip(1));offset=lane.sourceOffset+segment.length;
     if(c.laneAt>=lane.sourceOffset&&c.laneAt<offset){edge=index;localAt=c.laneAt-lane.sourceOffset;}
    }
    if(!continuous){reason="split approach continuity changed";return false;}path=new AssistedRoadNetwork.Path(points.ToArray());
   }
  }
  var pos=path.Position(c.laneAt);var dir=path.Tangent(c.laneAt);var right=TrafficSystem.Right(dir);
  if(c.laneAt<15||c.laneAt>path.length-15||FlatDistance(pos,Pos(c))>.3f||Vector3.Dot(dir,Dir(c))<.995f){reason="approach geometry changed";return false;}
  var bay=pos+right*c.bayOffset;var passenger=new Vector3(c.passengerX,.14f,c.passengerZ);
  if(c.bayOffset<3.1f||c.bayOffset>3.9f||FlatDistance(passenger,bay+right*2.4f)>.1f){reason="invalid bay/sidewalk dimensions";return false;}
  if(g.FootprintBlocked(ProjectMapScene.Rectangle(bay,dir,2.8f,20))||g.FootprintBlocked(ProjectMapScene.Rectangle(passenger,dir,1.9f,28))||g.FootprintBlocked(ProjectMapScene.Rectangle(pos+right*c.bayOffset*.5f,dir,c.bayOffset+3,22))){reason="polygon obstacle overlap";return false;}
  float lo=2,hi=c.bayOffset+3.35f;foreach(float along in new[]{-13f,13})if(g.FootprintBlocked(ProjectMapScene.Rectangle(pos+dir*along+right*(lo+hi)*.5f,dir,hi-lo,2))){reason="connector polygon overlap";return false;}
  foreach(var j in map.junctionRules??new HybridJunctionRule[0])if(FlatDistance(bay,j.V())<38){reason="junction clearance";return false;}
  foreach(var crossing in map.osmCrossings??new OsmCrossing[0])if(FlatDistance(bay,new Vector3(crossing.x,0,crossing.z))<25){reason="crosswalk clearance";return false;}
  if(map.editorDocument!=null)foreach(var f in map.editorDocument.facilities)if((f.kind=="crosswalk"||f.kind=="stopLine")&&FlatDistance(bay,f.Position)<25){reason="edited crossing/stop line";return false;}
  foreach(float along in new[]{-10f,-8,-6,-4,-2,0,2,4,6,8,10}){
   if(Vector3.Dot(path.Tangent(c.laneAt+along),dir)<.995f){reason="bend";return false;}
   foreach(float across in new[]{-1.35f,0,1.35f}){var p=bay+dir*along+right*across;if(g.OnRoad(p,-.03f)||g.Blocked(p)){reason="bay obstructed";return false;}}
  }
  Func<Vector3,bool> bounds=p=>p.x>3&&p.z>3&&p.x<map.width-3&&p.z<map.depth-3;
  foreach(float along in new[]{-14f,-12,-10,-8,-4,0,4,8,10,12,14})foreach(float across in new[]{-.9f,0,.9f}){var p=passenger+dir*along+right*across;if(!bounds(p)||g.OnRoad(p,-.03f)||g.Blocked(p)){reason="sidewalk extension obstructed";return false;}}
  bool connects=false;foreach(float along in new[]{-12f,12})foreach(float across in new[]{2f,3,4,5})if(ProjectMapScene.OnArea(map,pos+dir*along+right*across,true))connects=true;
  if(!connects){reason="existing sidewalk removed";return false;}
  foreach(float along in new[]{-11f,-8,0,8,11})foreach(float across in new[]{-1.5f,0,.5f,1,1.5f,2,2.5f,c.bayOffset,c.bayOffset+1.5f}){var p=pos+dir*along+right*across;if(!bounds(p)||g.Blocked(p)){reason="entry obstruction";return false;}}
  foreach(float along in new[]{-14f,-13,-12,12,13,14})foreach(float across in new[]{2f,3,4,5,c.bayOffset+3.35f}){var p=pos+dir*along+right*across;if(!bounds(p)||g.OnRoad(p,-.03f)||g.Blocked(p)){reason="sidewalk connector obstructed";return false;}}
  stop=new MapStop{name=c.name,x=pos.x,z=pos.z,elevation=pos.y,dx=dir.x,dz=dir.z,width=3+2*c.bayOffset,laneEdge=edge,laneAt=localAt,graphStop=true,hasPassengerAnchor=true,passengerAnchor=passenger};return true;
 }
 static MapPoint P(Vector3 p){return new MapPoint{x=p.x,z=p.z,elevation=p.y};}
 static MapArea Walk(Vector3 p,Vector3 dir,float width,float length,string id){var ring=ProjectMapScene.Rectangle(p,dir,width,length);return new MapArea{kind="sidewalk",name="게임 승하차 보도",evidence=id,height=.14f,points=ring,triangles=ProjectMapScene.Triangulate(ring)};}
 static void InstallBay(MapData map,PassengerStopCandidate c){
  var pos=Pos(c);var dir=Dir(c);var right=TrafficSystem.Right(dir);var roads=new List<MapRoad>(map.roads);
  // This asphalt extension is fictional, painted and labelled in-game. The
  // outer lane remains the directed route anchor; stopping is in the pocket.
  roads.Add(new MapRoad{id="game-stop:"+c.stopId,name="게임 전용 승하차 포켓",width=c.bayOffset+3,lanes=1,oneWay=1,suppressPaint=true,points=new[]{P(pos+right*c.bayOffset*.5f-dir*7),P(pos+right*c.bayOffset*.5f+dir*7)}});
  var areas=new List<MapArea>();foreach(var area in map.areas){if(area.kind!="sidewalk"){areas.Add(area);continue;}var pieces=Subtract(area.points,pos,dir,right,-11,11,-1.6f,c.bayOffset+1.45f);if(pieces.Count==1&&ReferenceEquals(pieces[0],area.points)){areas.Add(area);continue;}foreach(var ring in pieces){if(ring.Length<3)continue;try{areas.Add(new MapArea{kind=area.kind,name=area.name,evidence=area.evidence,height=area.height,points=ring,triangles=ProjectMapScene.Triangulate(ring)});}catch(ArgumentException){throw new InvalidOperationException("Game stop sidewalk cut failed: "+c.stopId);}}}
  // 1.9m continuous pedestrian strip, with 2m end connections outside asphalt.
  areas.Add(Walk(pos+right*(c.bayOffset+2.4f),dir,1.9f,28,"game-stop:"+c.stopId));
  float lo=2,hi=c.bayOffset+3.35f;foreach(float along in new[]{-13f,13})areas.Add(Walk(pos+dir*along+right*((lo+hi)*.5f),dir,hi-lo,2,"game-stop:"+c.stopId+":connector"));
  map.roads=roads.ToArray();map.areas=areas.ToArray();
 }
 // Polygon difference from a convex rectangle, expressed as four disjoint
 // outside half-planes. Original, unaffected polygons retain their vertices.
 static List<MapPoint[]> Subtract(MapPoint[] ring,Vector3 origin,Vector3 dir,Vector3 side,float a,float b,float c,float d){
  float minA=float.MaxValue,maxA=float.MinValue,minC=float.MaxValue,maxC=float.MinValue;foreach(var p in ring){var delta=p.V()-origin;float x=Vector3.Dot(delta,dir),z=Vector3.Dot(delta,side);minA=Mathf.Min(minA,x);maxA=Mathf.Max(maxA,x);minC=Mathf.Min(minC,z);maxC=Mathf.Max(maxC,z);}if(maxA<=a||minA>=b||maxC<=c||minC>=d)return new List<MapPoint[]>{ring};
  var result=new List<MapPoint[]>();var current=ring;foreach(var plane in new[]{new Vector4(dir.x,dir.z,a,1),new Vector4(dir.x,dir.z,b,-1),new Vector4(side.x,side.z,c,1),new Vector4(side.x,side.z,d,-1)}){var outside=Clip(current,origin,plane,false);if(outside.Length>=3)result.Add(outside);current=Clip(current,origin,plane,true);if(current.Length<3)break;}return result;
 }
 static MapPoint[] Clip(MapPoint[] input,Vector3 origin,Vector4 plane,bool inside){var output=new List<MapPoint>();if(input.Length==0)return output.ToArray();Func<MapPoint,float> signed=p=>((p.x-origin.x)*plane.x+(p.z-origin.z)*plane.y-plane.z)*plane.w*(inside?1:-1);var previous=input[input.Length-1];float last=signed(previous);foreach(var next in input){float value=signed(next);if((last>=0)!=(value>=0)){float t=last/(last-value);output.Add(new MapPoint{x=Mathf.Lerp(previous.x,next.x,t),z=Mathf.Lerp(previous.z,next.z,t)});}if(value>=0)output.Add(next);previous=next;last=value;}return output.ToArray();}
 public static void Draw(MapData map,Transform parent){var catalog=For(map);if(catalog.admitted.Count==0)return;var paint=WorldBuilder.Mat("게임 승하차 포켓",new Color(.1f,.72f,.7f));foreach(var c in catalog.admitted){var dir=Dir(c);var right=TrafficSystem.Right(dir);var pos=Pos(c)+right*c.bayOffset;var root=new GameObject(c.stopId+" 게임 전용 승하차");root.transform.SetParent(parent,false);root.transform.position=pos;root.transform.rotation=Quaternion.LookRotation(dir);foreach(float side in new[]{-1.3f,1.3f})WorldBuilder.Box("게임 포켓 경계",new Vector3(side,.08f,0),new Vector3(.12f,.02f,8),paint,root.transform,false);foreach(float along in new[]{-4f,4})WorldBuilder.Box("게임 포켓 끝",new Vector3(0,.08f,along),new Vector3(2.7f,.02f,.12f),paint,root.transform,false);var label=new GameObject("게임용 표시");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(2.6f,2.8f,0);var text=label.AddComponent<TextMesh>();text.text=c.name+"\n게임 전용 · 현장 미확인";text.fontSize=40;text.characterSize=.06f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.cyan;KoreanText.Apply(text);label.AddComponent<FaceCamera>();}}

 sealed class Geometry {
  struct Road {public Vector3 a,b;public float half;}
  readonly Dictionary<Vector2Int,List<Road>> roads=new Dictionary<Vector2Int,List<Road>>();readonly Dictionary<Vector2Int,List<MapPoint[]>> blockers=new Dictionary<Vector2Int,List<MapPoint[]>>();
  static Vector2Int Key(Vector3 p){return new Vector2Int(Mathf.FloorToInt(p.x/32),Mathf.FloorToInt(p.z/32));}
  static void Add<T>(Dictionary<Vector2Int,List<T>> index,T item,IEnumerable<Vector3> points,float margin){var all=points.ToArray();for(int x=Mathf.FloorToInt((all.Min(p=>p.x)-margin)/32);x<=Mathf.FloorToInt((all.Max(p=>p.x)+margin)/32);x++)for(int z=Mathf.FloorToInt((all.Min(p=>p.z)-margin)/32);z<=Mathf.FloorToInt((all.Max(p=>p.z)+margin)/32);z++){var key=new Vector2Int(x,z);List<T> list;if(!index.TryGetValue(key,out list)){list=new List<T>();index.Add(key,list);}list.Add(item);}}
  public Geometry(MapData map){foreach(var road in map.roads)for(int i=1;i<road.points.Length;i++){var r=new Road{a=road.points[i-1].V(),b=road.points[i].V(),half=road.width*.5f};Add(roads,r,new[]{r.a,r.b},r.half+.2f);}foreach(var b in map.buildings)if(!SceneBuildingVisibility.Hidden(map,b.id))Add(blockers,b.points,b.points.Select(p=>p.V()),1);foreach(var a in map.areas)if(a.kind=="water"||a.kind=="parking"||a.kind=="restaurant"||a.kind=="rest")Add(blockers,a.points,a.points.Select(p=>p.V()),1);}
  static float Distance(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.00001f,d.sqrMagnitude)));}
  public bool OnRoad(Vector3 p,float margin){List<Road> list;if(!roads.TryGetValue(Key(p),out list))return false;return list.Any(r=>Distance(p,r.a,r.b)<=r.half+margin);}
  public bool Blocked(Vector3 p){List<MapPoint[]> list;if(!blockers.TryGetValue(Key(p),out list))return false;foreach(var ring in list){if(ProjectMapScene.Contains(ring,p))return true;for(int i=0;i<ring.Length;i++)if(Distance(p,ring[i].V(),ring[(i+1)%ring.Length].V())<.5f)return true;}return false;}
  static double Cross(MapPoint a,MapPoint b,MapPoint c){return ((double)b.x-a.x)*(c.z-a.z)-((double)b.z-a.z)*(c.x-a.x);}
  static bool Intersects(MapPoint a,MapPoint b,MapPoint c,MapPoint d){if(Mathf.Max(a.x,b.x)<Mathf.Min(c.x,d.x)||Mathf.Max(c.x,d.x)<Mathf.Min(a.x,b.x)||Mathf.Max(a.z,b.z)<Mathf.Min(c.z,d.z)||Mathf.Max(c.z,d.z)<Mathf.Min(a.z,b.z))return false;return Cross(a,b,c)*Cross(a,b,d)<=0&&Cross(c,d,a)*Cross(c,d,b)<=0;}
  static bool Overlap(MapPoint[] a,MapPoint[] b){if(ProjectMapScene.Contains(a,b[0].V())||ProjectMapScene.Contains(b,a[0].V()))return true;for(int i=0;i<a.Length;i++)for(int j=0;j<b.Length;j++)if(Intersects(a[i],a[(i+1)%a.Length],b[j],b[(j+1)%b.Length]))return true;return false;}
  public bool FootprintBlocked(MapPoint[] footprint){var checkedRings=new HashSet<MapPoint[]>();for(int x=Mathf.FloorToInt(footprint.Min(p=>p.x)/32);x<=Mathf.FloorToInt(footprint.Max(p=>p.x)/32);x++)for(int z=Mathf.FloorToInt(footprint.Min(p=>p.z)/32);z<=Mathf.FloorToInt(footprint.Max(p=>p.z)/32);z++){List<MapPoint[]> list;if(!blockers.TryGetValue(new Vector2Int(x,z),out list))continue;foreach(var ring in list)if(checkedRings.Add(ring)&&Overlap(footprint,ring))return true;}return false;}
 }
}
