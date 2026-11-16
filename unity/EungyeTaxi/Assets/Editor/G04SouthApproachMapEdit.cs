using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class G04SouthApproachMapEdit {
 const string PreviousHash="a528216cb878fa6fce9af1e2fc64c067416078a2c8e6c4874c8563792a829591";
 const string Junction="J7841_867";
 const string Prefix="reviewed:G04:NB:";
 const int Approach=46;
 const float Taper=24;
 static readonly string[] Heads={"local:7854:19:7874:684","local:7930:17:7950:682"};
 static readonly string[] Labels={"left","straight-1","straight-2","right"};
 static readonly int[] Origins={0,0,1,1};
 static readonly float[] Offsets={-3.4f,0,-3.7f,0};
 static readonly int[] Turns={-1,0,0,1};

 static void Check(bool condition,string reason){if(!condition)throw new InvalidOperationException("G04_NB: "+reason);}
 static RoadMapEvidence Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=ZRnloGTTjDnVloJGRmOe9w,12,-13,45,Float",
  note="March 2024 shows northbound L+U/S/S/R at the Eungye-ro 142beon-gil junction. May 2026 view _wAObq86L2mzgkdxPyIoGQ confirms the left or pedestrian green U-turn sign. User confirmed right-only at marker 37. Taper, lane centres and U-turn curve are game estimates."
 };

 static string Join(int lane,int turn)=>"join:"+Heads[lane]+":"+turn;

 static RoadMapDocument Preview(ProjectMapPackage package){
  Check(package.document.revision==35&&package.contentHash==PreviousHash,"exact r35 required");
  var d=package.document.Copy();
  var lanes=d.lanes.ToDictionary(l=>l.id);
  var heads=Heads.Select(id=>lanes[id]).ToArray();
  var paths=heads.Select(l=>new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray())).ToArray();
  Check(paths.All(p=>p.length>Approach),"upstream approach length");
  Check(!d.facilities.Any(f=>Heads.Contains(f.laneId))&&!d.signals.Any(p=>p.junctionId==Junction),"existing approach facility or program must be carried forward");
  Check(!package.baseline.stops.Any(s=>s.graphStop&&Heads.Contains(package.baseline.hdEdges[s.laneEdge].id)),"graph-bound stop needs explicit remap");
  var evidence=Evidence();
  var tails=new RoadMapLane[Labels.Length];
  for(int i=0;i<tails.Length;i++){
   int source=Origins[i];
   var head=heads[source];var path=paths[source];float cut=path.length-Approach,offset=Offsets[i];
   var tail=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(head));tails[i]=tail;
   tail.id=Prefix+Labels[i];tail.number=i+1;tail.sourceOffset=0;
   tail.fromNode=head.toNode;tail.toNode=tail.id+":end";
   var right=TrafficSystem.Right(path.Tangent(path.length));
   Y06EastApproachEdit.Assign(tail,Enumerable.Range(0,Approach+1).Select(step=>
    path.Position(cut+step)+right*offset*Mathf.SmoothStep(0,1,Mathf.Clamp01(step/Taper))),evidence);
   var join=lanes[Join(source,Turns[i])];
   if(offset==0)join.fromNode=tail.toNode;
   else{
    Y06EastApproachEdit.Reconnect(join,tail,lanes[join.next.Single()],evidence);
    var surface=d.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+join.id);
    if(surface!=null)surface.points=join.points;
   }
   tail.next=new[]{join.id};
  }
  for(int i=0;i<heads.Length;i++){
   Y06EastApproachEdit.Assign(heads[i],G05NorthWestMapEdit.OriginalHead(paths[i],paths[i].length-Approach),evidence);
   heads[i].next=tails.Where((_,n)=>Origins[n]==i).Select(l=>l.id).ToArray();
  }
  d.lanes=d.lanes.Concat(tails).ToArray();
  G05NorthWestMapEdit.AddUturn(package,d,tails[0],lanes,Prefix,Junction,new[]{"local:7722:689:7702:23"},evidence);
  return d;
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument d){
  var issues=RoadMapValidator.Validate(before.baseline,d).Where(i=>i.error).ToArray();
  Check(issues.Length==0,string.Join(" / ",issues.Take(8)));
  var map=RoadMapComposer.Compose(before.baseline,d);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  var masks=new[]{LaneDirectionPaint.Left|LaneDirectionPaint.Uturn,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Right};
  var straightExits=new HashSet<string>();
  for(int i=0;i<Labels.Length;i++){
   var edge=graph.edges.Single(e=>e.id==Prefix+Labels[i]);
   Check(edge.number==i+1&&edge.next.Aggregate(0,(mask,n)=>mask|UturnMovement.PaintMask(graph.edges[n].turn))==masks[i],"incorrect movement "+edge.id);
   Check(arrows[edge.id].Any(a=>a.turns==masks[i]),"missing arrow "+edge.id);
   Y07MapTests.Footprint(map,edge);
   foreach(int next in edge.next){
    var connector=graph.edges[next];
    Y07MapTests.Footprint(map,connector);
    if(connector.turn==0)straightExits.Add(graph.edges[connector.next.Single()].id);
   }
  }
  Check(straightExits.Count==2,"two distinct northbound through destinations");
  Check(UturnMovement.Condition(d,Prefix+"left")?.type==UturnMovement.LeftOrPedestrianGreen,"signed U-turn signal condition");
  var stops=PassengerStopCatalog.For(map).Stops;
  Check(stops.Length==39,"39 passenger stops");
  int pairs=0;
  foreach(var from in stops)foreach(var to in stops)if(from!=to){
   Check(graph.Route(from.laneEdge,from.laneAt,to.laneEdge,to.laneAt)!=null,"lost passenger route "+from.name+" → "+to.name);
   pairs++;
  }
  Check(pairs==1482,"all directed passenger routes");
  Debug.Log("G04_NB_VALIDATED L+U/S/S/R, conditional U-turn, 39 stops, 1482 routes");
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var d=Preview(before);
  d.revision=36;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  Validate(before,d);
  var package=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,package,token);
  var saved=ProjectMapStore.Load(root);
  Check(saved.contentHash==package.contentHash&&saved.baselineHash==before.baselineHash,"saved map identity");
  Validate(before,saved.document);
  ProjectMapBuild.Prepare();
  Debug.Log("G04_NB_APPLIED revision=36 contentHash="+saved.contentHash);
 }

 public static void BuildReviewed(){
  JunctionRouteFeedbackTests.VerifyMigration();
  FlexibleBollardTests.BuildCommercialMap();
 }
}
