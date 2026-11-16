using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class G05NorthWestMapEdit {
 const string RequiredHash="575ca884e867d4d3eecd0ee2c2488fce2d4aee7d24d370413391ddb3c84fb791";
 const string Prefix="reviewed:G05:NW:";
 const string Junction="J6199_11987";
 const float Approach=70f;
 const float Taper=24f;
 static readonly string[] Roots={"local:7747:9855:6400:11866","local:7809:9881:6444:11916","local:7870:9907:6489:11965"};
 static readonly string[] Heads={Roots[0]+":part:2",Roots[1]+":part:2",Roots[2]+":part:1"};
 static readonly string[] Labels={"left","straight-1","straight-2","straight-3","right"};
 static readonly string[] Opposite={"local:6310:11767:7624:9804","local:6266:11718:7563:9778"};
 static string Join(string root,int turn)=>"join:"+root+":"+turn;
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
 static RoadMapLane Copy(RoadMapLane lane)=>JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(lane));
 static void Check(bool ok,string reason){if(!ok)throw new InvalidOperationException("G05_NW: "+reason);}
 static RoadMapEvidence Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=Ix6rbIKQdDKEt2ehapts1A,318,5,50,Float",
  note="Map 36, 15 to 14: five approach lanes; user confirmed outer right only. Existing left turn and three public straight exits are retained. The U-turn supplementary sign reads 좌회전시 / 보행신호시. May 2026 and March 2024 compared. The 70m approach, 24m taper, lanes and U curve are game estimates."
 };

 static IEnumerable<Vector3> OriginalHead(AssistedRoadNetwork.Path path,float cut){
  yield return path.points[0];float at=0;
  for(int i=1;i<path.points.Length;i++){
   at+=Vector3.Distance(path.points[i-1],path.points[i]);
   if(at>=cut-.001f)break;
   yield return path.points[i];
  }
  yield return path.Position(cut);
 }

 static RoadMapDocument Preview(ProjectMapPackage package){
  Check(package.document.revision==33&&package.contentHash==RequiredHash,"exact r33 required");
  var d=package.document.Copy();
  var lanes=d.lanes.ToDictionary(l=>l.id);
  var heads=Heads.Select(id=>lanes[id]).ToArray();
  var paths=heads.Select(Path).ToArray();
  Check(paths.All(p=>p.length>Approach),"upstream lengths changed");
  var baselineStops=package.baseline.stops.Where(s=>s.graphStop&&Heads.Contains(package.baseline.hdEdges[s.laneEdge].id));
  foreach(var stop in baselineStops){
   int i=Array.IndexOf(Heads,package.baseline.hdEdges[stop.laneEdge].id);
   Check(stop.laneAt<paths[i].length-Approach,"existing stop would move into taper");
  }
  Check(!d.facilities.Any(f=>Heads.Contains(f.laneId))&&!d.signals.Any(p=>p.groups.Any(g=>g.laneIds.Any(Heads.Contains))),"lane-bound facility needs explicit assignment");
  var evidence=Evidence();
  var origin=paths[0].Position(paths[0].length);
  var right=TrafficSystem.Right(paths[0].Tangent(paths[0].length));
  float span=Vector3.Dot(paths[2].Position(paths[2].length)-origin,right);
  Check(span>=12f&&span<=15f,"final lane span changed");
  var sources=new[]{0,0,1,2,2};
  var tails=new RoadMapLane[5];
  for(int i=0;i<tails.Length;i++){
   int source=sources[i];
   var path=paths[source];
   float shift=span*i/4-Vector3.Dot(path.Position(path.length)-origin,right);
   var tail=Copy(heads[source]);tails[i]=tail;
   tail.id=Prefix+Labels[i];tail.number=i+1;tail.width=span/4;
   tail.sourceOffset=0;
   tail.fromNode=heads[source].toNode;
   tail.toNode=tail.id+":end";
   Y06EastApproachEdit.Assign(tail,Enumerable.Range(0,(int)Approach+1).Select(step=>
    path.Position(path.length-Approach+step)+right*shift*Mathf.SmoothStep(0,1,Mathf.Clamp01(step/Taper))),evidence);
  }
  var left=lanes[Join(Roots[0],-1)];
  var turnRight=lanes[Join(Roots[2],1)];
  Check(Vector3.Distance(tails[0].points.Last().V(),left.points[0].V())<.02f
   &&Vector3.Distance(tails[4].points.Last().V(),turnRight.points[0].V())<.02f,"existing left and right connector starts");
  left.fromNode=tails[0].toNode;turnRight.fromNode=tails[4].toNode;
  tails[0].next=new[]{left.id};tails[4].next=new[]{turnRight.id};
  for(int i=0;i<Roots.Length;i++){
   var join=lanes[Join(Roots[i],0)];
   Y06EastApproachEdit.Reconnect(join,tails[i+1],lanes[join.next.Single()],evidence);
   tails[i+1].next=new[]{join.id};
   var surface=d.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+join.id);
   if(surface!=null)surface.points=join.points;
   Y06EastApproachEdit.Assign(heads[i],OriginalHead(paths[i],paths[i].length-Approach),evidence);
   heads[i].next=tails.Where((_,j)=>sources[j]==i).Select(l=>l.id).ToArray();
  }
  d.lanes=d.lanes.Concat(tails).ToArray();
  AddUturn(package,d,tails[0],lanes,evidence);
  return d;
 }

 static void AddUturn(ProjectMapPackage package,RoadMapDocument d,RoadMapLane entry,Dictionary<string,RoadMapLane> lanes,RoadMapEvidence evidence){
  RoadMapLane target=null;Vector3[] curve=null;
  var rejected=new List<string>();
  foreach(string id in Opposite){
   try{curve=UturnMovement.Geometry(entry,lanes[id]);target=lanes[id];break;}
   catch(ArgumentException e){rejected.Add(id+": "+e.Message);}
  }
  Check(target!=null,"observed U-turn geometry cannot connect: "+string.Join(", ",rejected));
  var turn=Copy(entry);
  turn.id="join:"+Prefix+"uturn";turn.turn=UturnMovement.Turn;
  turn.fromNode=entry.toNode;turn.toNode=target.fromNode;turn.number=target.number;turn.next=new[]{target.id};
  Y06EastApproachEdit.Assign(turn,curve,evidence);
  entry.next=entry.next.Concat(new[]{turn.id}).ToArray();
  d.lanes=d.lanes.Concat(new[]{turn}).ToArray();
  var heading=(entry.points.Last().V()-entry.points[entry.points.Length-2].V()).normalized;
  var rule=package.baseline.junctionRules.Single(j=>j.id==Junction);
  var axisDirection=new Vector3(rule.dx,0,rule.dz).normalized;
  int axis=Mathf.Abs(Vector3.Dot(heading,axisDirection))>.7f?0:1;
  int sign=Vector3.Dot(heading,axis==0?axisDirection:TrafficSystem.Right(axisDirection))>=0f?1:-1;
  var pos=entry.points.Last().V()-heading*7-TrafficSystem.Right(heading)*2.3f;
  d.facilities=d.facilities.Concat(new[]{new RoadMapFacility{
   id=Prefix+"uturn-condition",kind="sign",type=UturnMovement.LeftOrPedestrianGreen,rule=true,
   laneId=entry.id,junctionId=Junction,x=pos.x,z=pos.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,
   axis=axis,sign=sign,height=3f,text="유턴 · 좌회전 또는 보행 신호 시",evidence=evidence
  }}).ToArray();
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument d){
  var issues=RoadMapValidator.Validate(before.baseline,d).Where(issue=>issue.error).ToArray();
  Check(issues.Length==0,string.Join(" / ",issues.Take(8)));
  var map=RoadMapComposer.Compose(before.baseline,d);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  var masks=new[]{LaneDirectionPaint.Left|LaneDirectionPaint.Uturn,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Right};
  var through=new HashSet<string>();
  for(int i=0;i<Labels.Length;i++){
   var edge=graph.edges.Single(e=>e.id==Prefix+Labels[i]);
   Check(edge.number==i+1&&edge.next.Aggregate(0,(mask,n)=>mask|UturnMovement.PaintMask(graph.edges[n].turn))==masks[i],"incorrect turns "+edge.id);
   Check(arrows[edge.id].Any(a=>a.turns==masks[i]),"missing painted arrow "+edge.id);
   Y07MapTests.Footprint(map,edge);
   foreach(int next in edge.next){
    var connector=graph.edges[next];
    if(connector.turn==0||connector.turn==UturnMovement.Turn)Y07MapTests.Footprint(map,connector);
    if(connector.turn==0)through.Add(graph.edges[connector.next.Single()].id);
   }
  }
  Check(through.Count==3,"three distinct straight destinations");
  Check(UturnMovement.Condition(d,Prefix+"left")?.type==UturnMovement.LeftOrPedestrianGreen,"incorrect conditional U-turn");
  var cars=PassengerStopCatalog.For(map);
  Check(cars.Stops.Length==39,"existing passenger stops: "+string.Join("; ",cars.Rejected));
  var stop=cars.Stops.Single(s=>s.name=="은계 승하차 6");
  var previous=before.baseline.stops.Single(s=>s.name==stop.name);
  Check(Vector3.Distance(stop.V(),previous.V())<.03f,"S6 boarding point moved");
  int pairs=0;
  foreach(var from in cars.Stops)foreach(var to in cars.Stops)if(from!=to){
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"unreachable stop "+from.name+" → "+to.name);pairs++;
  }
  Check(pairs==1482,"all directed passenger routes");
  Debug.Log("G05_NW_VALIDATED L+U/S/S/S/R, condition left or pedestrian green, 39 stops, 1482 routes");
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var d=Preview(before);
  d.revision=34;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  Validate(before,d);
  var package=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,package,token);
  var loaded=ProjectMapStore.Load(root);
  Check(loaded.contentHash==package.contentHash&&loaded.baselineHash==before.baselineHash,"saved identity");
  Validate(before,loaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("G05_NW_APPLIED revision=34 contentHash="+loaded.contentHash);
 }

 public static void PreviewChecks(){
  var before=ProjectMapStore.Load(ProjectMapBuild.Root);
  Validate(before,Preview(before));
 }
}
