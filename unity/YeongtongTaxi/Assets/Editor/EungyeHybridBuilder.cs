using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Compiler for game-provisional local lanes joined to unchanged NGII lane paths.
public static class EungyeHybridBuilder {
 static MapPoint P(Vector3 v){return new MapPoint{x=v.x,z=v.z};}
 static MapPoint[] Points(IEnumerable<Vector3> p){return p.Select(P).ToArray();}
 static string Junction(Vector3 p){return "J"+Mathf.RoundToInt(p.x*10)+"_"+Mathf.RoundToInt(p.z*10);}
 static string LaneId(AssistedRoadNetwork.Lane l){var a=l.path.Position(0);var b=l.path.Position(l.path.length);return "local:"+Mathf.RoundToInt(a.x*10)+":"+Mathf.RoundToInt(a.z*10)+":"+Mathf.RoundToInt(b.x*10)+":"+Mathf.RoundToInt(b.z*10);}
 static Vector3[] Curve(Vector3 a,Vector3 forward,Vector3 b,Vector3 exit){float handle=Vector3.Distance(a,b)*.4f;var c=a+forward*handle;var d=b-exit*handle;var result=new Vector3[33];for(int i=0;i<33;i++){float t=i/32f,u=1-t;result[i]=u*u*u*a+3*u*u*t*c+3*u*t*t*d+t*t*t*b;}return result;}
 static bool InPolygon(Vector3 p,MapPoint[] ring){bool inside=false;for(int i=0,j=ring.Length-1;i<ring.Length;j=i++){var a=ring[i];var b=ring[j];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;}
 sealed class RoadEnvelope {
  public List<Tuple<Vector3,Vector3,float>> lines=new List<Tuple<Vector3,Vector3,float>>();MapData map;
  public RoadEnvelope(MapData m){map=m;foreach(var r in m.roads)for(int i=1;i<r.points.Length;i++)lines.Add(Tuple.Create(r.points[i-1].V(),r.points[i].V(),r.width*.5f));}
  public bool Clear(Vector3[] points){foreach(var p in points){
   bool road=false;foreach(var s in lines){var d=s.Item2-s.Item1;float t=Mathf.Clamp01(Vector3.Dot(p-s.Item1,d)/Mathf.Max(.0001f,d.sqrMagnitude));if(Vector3.Distance(p,s.Item1+t*d)<s.Item3-.6f){road=true;break;}}if(!road)return false;
   foreach(var b in map.buildings)if(InPolygon(p,b.points))return false;
   if(map.areas!=null)foreach(var a in map.areas)if(a.kind=="water"&&InPolygon(p,a.points))return false;
  }return true;}
 }
 static AssistedRoadNetwork.Path PathFor(HdEdge e){return new AssistedRoadNetwork.Path(e.points.Select(p=>p.V()).ToArray());}
 static Vector3[] Slice(AssistedRoadNetwork.Path p,float a,float b){var result=new List<Vector3>{p.Position(a)};float at=0;for(int i=1;i<p.points.Length;i++){at+=Vector3.Distance(p.points[i-1],p.points[i]);if(at>a+.001f&&at<b-.001f)result.Add(p.points[i]);}result.Add(p.Position(b));return result.ToArray();}
 public static MapData Generate(){return Generate(null,true);}
 public static MapData Generate(HybridOverrides supplied,bool save){
  var map=JsonUtility.FromJson<MapData>(File.ReadAllText("Assets/Resources/eungye-map.json"));
  var hd=JsonUtility.FromJson<MapData>(File.ReadAllText("Assets/Resources/route42-map.json"));
  string config=System.IO.Path.GetFullPath("../../config/maps/eungye-traffic-overrides.json");
  var overrides=supplied??(File.Exists(config)?JsonUtility.FromJson<HybridOverrides>(File.ReadAllText(config)):new HybridOverrides());
  foreach(var r in overrides.roads){var road=Array.Find(map.roads,x=>x.id==r.roadId);if(road==null||r.lanes<2||r.lanes>8||r.lanes%2!=0)throw new Exception("Invalid road override "+r.roadId);road.lanes=r.lanes;}
  var net=new AssistedRoadNetwork();net.Build(map);var hg=new HdRoadGraph(hd);var envelope=new RoadEnvelope(map);
  var rules=new Dictionary<string,HybridJunctionRule>();
  foreach(var lane in net.lanes)foreach(int node in new[]{lane.from,lane.to}){var p=net.NodePosition(node);string id=Junction(p);if(!rules.ContainsKey(id))rules[id]=new HybridJunctionRule{id=id,x=p.x,z=p.z};}
  foreach(var rule in rules.Values){var lane=net.lanes.First(l=>Junction(net.NodePosition(l.from))==rule.id||Junction(net.NodePosition(l.to))==rule.id);var d=lane.path.Tangent(0);rule.dx=d.x;rule.dz=d.z;}
  for(int i=0;i<Mathf.Min(2,map.targets.Length);i++){var id=Junction(map.targets[i].V());HybridJunctionRule rule;if(rules.TryGetValue(id,out rule)){rule.signalMode=map.targets[i].leftTurnMode==LeftTurnMode.Permissive?1:2;var d=(map.targets[1].V()-map.targets[0].V()).normalized;rule.dx=d.x;rule.dz=d.z;}}
  foreach(var r in overrides.junctions){if(!rules.ContainsKey(r.id)||r.signalMode<0||r.signalMode>2)throw new Exception("Unknown/invalid junction rule "+r.id);var pos=rules[r.id];r.x=pos.x;r.z=pos.z;if(r.dx==0&&r.dz==0){r.dx=pos.dx;r.dz=pos.dz;}rules[r.id]=r;}
  foreach(var r in rules.Values){if(r.facilityStatus!="provisional"&&r.facilityStatus!="verified")throw new Exception("Invalid facility status "+r.id);DateTime checkedAt;
   if(r.facilityStatus=="verified"&&(string.IsNullOrWhiteSpace(r.facilityEvidence)||!DateTime.TryParseExact(r.facilityCheckedAt,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out checkedAt)))throw new Exception("Verified facility requires evidence and date "+r.id);
   foreach(var a in r.approaches)if(a.width<3||new Vector2(a.dx,a.dz).magnitude<.9f)throw new Exception("Invalid approach "+r.id);
  }
  // Only longitudinal local lanes mostly covered by actual HD paths are removed.
  // Crossing side roads remain. Coverage does not create a connection.
  var covered=new HashSet<int>();
  foreach(var l in net.lanes){int count=0,hit=0;for(float d=0;d<=l.path.length;d+=5){count++;float at;if(hg.Nearest(l.path.Position(d),l.path.Tangent(d),out at)>=0)hit++;}if(hit>=count*.6f)covered.Add(l.id);}
  var edges=new List<HdEdge>(hd.hdEdges);int originals=edges.Count;
  foreach(var e in edges){e.status="ngii";e.ruleId="NGII:"+e.sourceId;}
  var local=new Dictionary<int,int>();
  foreach(var l in net.lanes){if(covered.Contains(l.id))continue;string id=LaneId(l);local[l.id]=edges.Count;edges.Add(new HdEdge{id=id,sourceId=l.sourceIds,fromNode=id+":start",toNode=id+":end",points=Points(l.path.points),number=l.index+1,status="provisional",ruleId=Junction(net.NodePosition(l.to)),next=new int[0]});}
  var laneTurns=new Dictionary<string,HybridLaneTurnOverride>();
  foreach(var item in overrides.laneTurns??new HybridLaneTurnOverride[0]){
   if(item==null||string.IsNullOrEmpty(item.junctionId)||string.IsNullOrEmpty(item.laneId)||string.IsNullOrEmpty(item.turns)||item.turns.Any(c=>c!='L'&&c!='S'&&c!='R')||item.turns.Distinct().Count()!=item.turns.Length)throw new Exception("Invalid lane turn override");
   if(item.status!="verified"&&item.status!="provisional")throw new Exception("Invalid lane turn status "+item.laneId);
   DateTime checkedAt;
   if(item.status=="verified"&&(string.IsNullOrWhiteSpace(item.evidence)||!DateTime.TryParseExact(item.checkedAt,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out checkedAt)))throw new Exception("Verified lane turn requires evidence and date "+item.laneId);
   var found=edges.Skip(originals).FirstOrDefault(e=>e.id==item.laneId&&e.ruleId==item.junctionId);
   if(found==null||laneTurns.ContainsKey(item.laneId))throw new Exception("Unknown/duplicate lane turn override "+item.laneId);
   laneTurns.Add(item.laneId,item);
  }
  foreach(var l in net.lanes){int first;if(!local.TryGetValue(l.id,out first))continue;var rule=rules[edges[first].ruleId];
   for(int turn=-1;turn<=1;turn++){
    if(turn<0&&!rule.allowLeft||turn>0&&!rule.allowRight||turn==0&&!rule.allowStraight)continue;
    HybridLaneTurnOverride laneRule;if(laneTurns.TryGetValue(edges[first].id,out laneRule)&&laneRule.turns.IndexOf(turn<0?'L':turn>0?'R':'S')<0)continue;
    var join=net.Connection(l,turn);int target;if(join==null||!local.TryGetValue(join.target.id,out target))continue;
    if(!envelope.Clear(join.path.points))continue;
    edges.Add(new HdEdge{id="join:"+edges[first].id+":"+turn,sourceId=edges[first].sourceId,fromNode=edges[first].toNode,toNode=edges[target].fromNode,points=Points(join.path.points),number=join.target.index+1,turn=turn,status="provisional",evidence=laneRule==null?"":laneRule.evidence,updated=laneRule==null?"":laneRule.checkedAt,ruleId=rule.id,next=new int[0]});
   }
  }
  // Provisional lane changes use split nodes; the original road's interior is
  // never connected to another road merely because their lines cross.
  var splits=new Dictionary<int,SortedSet<float>>();var changes=new List<Tuple<int,int,float,float,int,Vector3[]>>();
  foreach(var l in net.lanes){int a;if(!local.TryGetValue(l.id,out a)||l.path.length<70)continue;
   foreach(int side in new[]{-1,1}){var other=net.Adjacent(l,side);int b;if(other==null||!local.TryGetValue(other.id,out b))continue;float start=10,error;float end=other.path.Project(l.path.Position(start),out error)+32;if(end>other.path.length-10)continue;
    var pts=new Vector3[33];for(int i=0;i<33;i++){float t=i/32f,blend=t*t*t*(t*(t*6-15)+10);pts[i]=Vector3.Lerp(l.path.Position(start+32*t),other.path.Position(end-32+32*t),blend);}if(!envelope.Clear(pts))continue;
    if(!splits.ContainsKey(a))splits[a]=new SortedSet<float>();if(!splits.ContainsKey(b))splits[b]=new SortedSet<float>();splits[a].Add(start);splits[b].Add(end);changes.Add(Tuple.Create(a,b,start,end,side,pts));
   }
  }
  Func<int,float,string> splitNode=(index,d)=>edges[index].id+":split:"+d.ToString("F4",System.Globalization.CultureInfo.InvariantCulture);
  foreach(var pair in splits){var e=edges[pair.Key];var path=PathFor(e);var cuts=new List<float>{0};cuts.AddRange(pair.Value);cuts.Add(path.length);string oldTo=e.toNode;
   for(int k=0;k+1<cuts.Count;k++){string from=k==0?e.fromNode:splitNode(pair.Key,cuts[k]),to=k+2==cuts.Count?oldTo:splitNode(pair.Key,cuts[k+1]);var part=new HdEdge{id=e.id+":part:"+k,sourceId=e.sourceId,fromNode=from,toNode=to,points=Points(Slice(path,cuts[k],cuts[k+1])),number=e.number,status=e.status,ruleId=e.ruleId,next=new int[0]};if(k==0){e.points=part.points;e.toNode=part.toNode;}else edges.Add(part);}
  }
  foreach(var c in changes)edges.Add(new HdEdge{id="change:"+edges[c.Item1].id+":"+edges[c.Item2].id,sourceId=edges[c.Item1].sourceId,fromNode=splitNode(c.Item1,c.Item3),toNode=splitNode(c.Item2,c.Item4),points=Points(c.Item6),number=edges[c.Item2].number,change=c.Item5,status="provisional",ruleId=edges[c.Item1].ruleId,next=new int[0]});
  // Connect only open HD capture endpoints with local endpoints, heading and
  // sampled centerline clearance. Every bridge is explicitly provisional.
  var incoming=new HashSet<int>();for(int i=0;i<originals;i++)foreach(int n in edges[i].next)incoming.Add(n);
  int localEnd=edges.Count;var paths=edges.Select(PathFor).ToArray();int portals=0;
  for(int h=0;h<originals;h++){
   if(edges[h].change!=0)continue;
   foreach(bool outgoing in new[]{true,false}){
    if(outgoing?edges[h].next.Length>0:incoming.Contains(h))continue;
    var hp=paths[h];var origin=outgoing?hp.Position(hp.length):hp.Position(0);var direction=hp.Tangent(outgoing?hp.length:0);
    int best=-1;float score=65;Vector3[] curve=null;
    for(int j=originals;j<localEnd;j++){
     if(edges[j].turn!=0||edges[j].change!=0)continue;
     var p=paths[j];var destination=outgoing?p.Position(0):p.Position(p.length);var heading=p.Tangent(outgoing?0:p.length);
     float distance=Vector3.Distance(origin,destination);if(distance<.1f||distance>=score||Vector3.Dot(direction,heading)<.85f)continue;
     var delta=outgoing?destination-origin:origin-destination;if(Vector3.Dot(delta,direction)<-2||Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(direction)))>12)continue;
     var candidate=outgoing?Curve(origin,direction,destination,heading):Curve(destination,heading,origin,direction);
     if(!envelope.Clear(candidate))continue;best=j;score=distance;curve=candidate;
    }
    if(best<0)continue;string id="portal:"+edges[h].id+":"+(outgoing?"out":"in");if(Array.IndexOf(overrides.disabledPortals,id)>=0)continue;
    edges.Add(new HdEdge{id=id,sourceId=edges[h].sourceId+" + "+edges[best].sourceId,fromNode=outgoing?edges[h].toNode:edges[best].toNode,toNode=outgoing?edges[best].fromNode:edges[h].fromNode,points=Points(curve),number=edges[best].number,status="provisional-portal",ruleId=id,next=new int[0]});portals++;
   }
  }
  var starts=new Dictionary<string,List<int>>();for(int i=0;i<edges.Count;i++){List<int> list;if(!starts.TryGetValue(edges[i].fromNode,out list)){list=new List<int>();starts[edges[i].fromNode]=list;}list.Add(i);}
  paths=edges.Select(PathFor).ToArray();
  for(int i=0;i<edges.Count;i++){List<int> options;var next=i<originals?new List<int>(edges[i].next):new List<int>();
   if(starts.TryGetValue(edges[i].toNode,out options))foreach(int n in options){if(i<originals&&n<originals||n==i||next.Contains(n))continue;if(Vector3.Distance(paths[i].Position(paths[i].length),paths[n].Position(0))<.15f&&Vector3.Dot(paths[i].Tangent(paths[i].length),paths[n].Tangent(0))>-.5f)next.Add(n);}
   edges[i].next=next.ToArray();
  }
  map.hdEdges=edges.ToArray();map.hybrid=true;map.mapId="eungye";map.hdMarks=hd.hdMarks;map.hdSignals=hd.hdSignals;map.hdTrials=null;map.junctionRules=rules.Values.OrderBy(r=>r.id).ToArray();
  map.source+=" / 국도42 정밀 차로 결합 · 나머지 차로/교차로/접속은 임시";
  // Background road surfaces remain from the district map; add actual lane
  // ribbons so the more accurate geometry always has a drivable surface.
  var coveredSources=new HashSet<string>();foreach(var lane in net.lanes)if(covered.Contains(lane.id))foreach(var id in lane.sourceIds.Split(','))coveredSources.Add(id);
  foreach(var road in map.roads)road.suppressPaint=coveredSources.Contains(road.id);foreach(var road in hd.roads)road.suppressPaint=true;
  map.roads=map.roads.Concat(hd.roads).ToArray();
  var graph=new HdRoadGraph(map);var component=LargestComponent(edges);var candidates=new List<MapStop>();
  foreach(int i in component){var e=edges[i];if(e.turn!=0||e.change!=0||e.id.StartsWith("join:")||e.status=="provisional-portal"||paths[i].length<25)continue;
   // Use rightmost generated district lanes; original multi-lane HD candidates
   // require lane 4 for this captured corridor, avoiding median boarding.
   if(i<originals&&e.number!=4)continue;
   if(i>=originals){var lane=net.lanes.Find(l=>e.sourceId==l.sourceIds&&e.number==l.index+1&&l.index==l.count-1);if(lane==null)continue;}
   float at=paths[i].length*.5f;var p=paths[i].Position(at);var d=paths[i].Tangent(at);
   candidates.Add(new MapStop{name="은계 승하차",x=p.x,z=p.z,dx=d.x,dz=d.z,width=3,graphStop=true,laneEdge=i,laneAt=at});
  }
  var chosen=new List<MapStop>();
  // Ensure a stop on the HD corridor, then distribute across the district.
  var hdStop=candidates.Find(s=>s.laneEdge<originals);if(hdStop!=null){chosen.Add(hdStop);candidates.Remove(hdStop);}
  while(chosen.Count<12&&candidates.Count>0){var next=candidates.OrderByDescending(s=>chosen.Count==0?0:chosen.Min(c=>Vector3.Distance(s.V(),c.V()))).First();if(chosen.Count>0&&chosen.Min(c=>Vector3.Distance(next.V(),c.V()))<70)break;chosen.Add(next);candidates.Remove(next);}
  for(int i=0;i<chosen.Count;i++)chosen[i].name=(chosen[i].laneEdge<originals?"국도42":"은계")+" 승하차 "+(i+1);
  map.stops=chosen.ToArray();
  if(!save)return map;
  Directory.CreateDirectory("../../data/inspection/ngii");File.WriteAllText("../../data/inspection/ngii/eungye-junction-catalog.json",JsonUtility.ToJson(new HybridOverrides{junctions=map.junctionRules},true));
  File.WriteAllText("Assets/Resources/eungye-hybrid-map.json",JsonUtility.ToJson(map));
  if(!File.Exists(config)){Directory.CreateDirectory(System.IO.Path.GetDirectoryName(config));File.WriteAllText(config,JsonUtility.ToJson(new HybridOverrides(),true));}
  File.WriteAllText("../../data/inspection/ngii/eungye-hybrid-summary.txt","localLanes="+net.lanes.Count+" covered="+covered.Count+" portals="+portals+" edges="+edges.Count+" serviceComponent="+component.Count+" stops="+chosen.Count+" hdInComponent="+component.Count(i=>i<originals));
  Debug.Log("HYBRID_COMPILED local="+net.lanes.Count+" covered="+covered.Count+" portals="+portals+" component="+component.Count+" hd="+component.Count(i=>i<originals)+" stops="+chosen.Count);
  AssetDatabase.Refresh();return map;
 }
 static HashSet<int> LargestComponent(List<HdEdge> edges){
  var visited=new HashSet<int>();var order=new List<int>();Action<int> visit=null;visit=i=>{if(!visited.Add(i))return;foreach(int n in edges[i].next)visit(n);order.Add(i);};for(int i=0;i<edges.Count;i++)visit(i);
  var reverse=new List<int>[edges.Count];for(int i=0;i<edges.Count;i++)reverse[i]=new List<int>();for(int i=0;i<edges.Count;i++)foreach(int n in edges[i].next)reverse[n].Add(i);
  visited.Clear();var best=new HashSet<int>();Action<int,HashSet<int>> collect=null;collect=(i,set)=>{if(!visited.Add(i))return;set.Add(i);foreach(int n in reverse[i])collect(n,set);};order.Reverse();foreach(int i in order){var set=new HashSet<int>();collect(i,set);if(set.Count>best.Count)best=set;}return best;
 }
}
