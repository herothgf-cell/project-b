using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// J7254 west approach only. The three upstream lanes become L/U, S, S, S, R.
public static class Y06EastApproachEdit {
 const string Prefix="reviewed:Y06:east:";
 const string RequiredHash="33db5a0c7c12a1bfc40d8c7cba19033173c74d866c92528c8b1992f91fd660bf";
 const string Junction="J7254_14780";
 const string Inner="local:5897:14274:7063:14681:part:2";
 const string Middle="local:5917:14217:7083:14624:part:3";
 const string Outer="local:5937:14159:7103:14567:part:2";
 const string FirstStraight="join:local:5897:14274:7063:14681:0";
 const string SecondStraight="join:local:5917:14217:7083:14624:0";
 const string ThirdStraight="join:local:5937:14159:7103:14567:0";
 const string DuplicateMiddle="join:reviewed:Y07:entry:reviewed:Y07:east:2:join:local:5897:14274:7063:14681:0";
 const string RightTurn="join:local:5937:14159:7103:14567:1";
 const string Uturn="join:reviewed:J7254-SW:uturn";
 const float ApproachLength=58f;
 const float TaperLength=26f;
 const float Spacing=3.04f;
 static readonly string RightTail="editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Outer)).TrimEnd('=').Replace('+','-').Replace('/','_')+":y06-right";

 static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Y06_EAST: "+message);}
 static T Copy<T>(T value)=>JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
 static RoadMapEvidence Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=bt9qRzwrewHH4_xQWJRPFA,70,-7,50,Float",
  note="Map 6 to 7 eastbound: user confirmed left with U-turn, three straight, dedicated right. Supplementary sign reads 좌회전시 / 보행신호시. Upstream panorama 1zkMkPr5YlH9MdvGMjx1Ow at public map x654.17 z1443.85 places the hatched area and outer split near the final 58m. The 26m taper and 3.04m lane spacing fit the existing curbs and are game estimates."
 };

 internal static void Assign(RoadMapLane lane,IEnumerable<Vector3> points,RoadMapEvidence evidence=null){
  var positions=points.ToArray();
  lane.points=positions.Select(p=>new MapPoint{x=p.x,z=p.z}).ToArray();
  lane.heights=positions.Select(p=>p.y).ToArray();
  lane.evidence=evidence??Evidence();
 }

 static RoadMapLane Tail(RoadMapLane template,AssistedRoadNetwork.Path original,string id,int number,Vector3 right,float shift){
  var lane=Copy(template);
  lane.id=Prefix+id;
  lane.number=number;
  lane.width=Spacing;
  lane.sourceOffset=0;
  lane.toNode=lane.id+":end";
  float start=original.length-ApproachLength;
  var points=Enumerable.Range(0,(int)ApproachLength+1).Select(step=>{
   float lateral=shift*Mathf.SmoothStep(0,1,Mathf.Clamp01(step/TaperLength));
   return original.Position(start+step)+right*lateral;
  });
  Assign(lane,points);
  return lane;
 }

 internal static void Reconnect(RoadMapLane connector,RoadMapLane from,RoadMapLane to,RoadMapEvidence evidence=null){
  var a=from.points.Last().V();
  var b=to.points[0].V();
  var head=(a-from.points[from.points.Length-2].V()).normalized;
  var tail=(to.points[1].V()-b).normalized;
  float handle=Vector3.Distance(a,b)/3;
  Assign(connector,Enumerable.Range(0,65).Select(i=>{
   float t=i/64f,u=1-t;
   return u*u*u*a+3*u*u*t*(a+head*handle)+3*u*t*t*(b-tail*handle)+t*t*t*b;
  }),evidence);
  connector.fromNode=from.toNode;
  connector.toNode=to.fromNode;
  connector.number=to.number;
  connector.next=new[]{to.id};
 }

 static RoadMapDocument Preview(ProjectMapPackage package){
  Check(package.document.revision==27&&package.contentHash==RequiredHash,"requires exact r27 baseline");
  var document=package.document.Copy();
  var lanes=document.lanes.ToDictionary(lane=>lane.id);
  var heads=new[]{lanes[Inner],lanes[Middle],lanes[Outer]};
  var paths=heads.Select(Path).ToArray();
  Check(paths.All(path=>path.length>ApproachLength),"taper must begin on the terminal pieces");
  var right=TrafficSystem.Right(paths[0].Tangent(paths[0].length));
  var origin=paths[0].Position(paths[0].length);
  var sources=new[]{0,0,1,2,2};
  var labels=new[]{"left","straight-1","straight-2","straight-3","right"};
  var tails=new RoadMapLane[labels.Length];
  for(int i=0;i<tails.Length;i++){
   int source=sources[i];
   var path=paths[source];
   float shift=i==4?0:i*Spacing-Vector3.Dot(path.Position(path.length)-origin,right);
   tails[i]=Tail(heads[source],path,labels[i],i+1,right,shift);
  }
  // The unchanged kerb lane carries S09-B at a station beyond the split.
  // Its existing authored lineage keeps the bay and passenger anchor fixed.
  tails[4].id=RightTail;
  tails[4].sourceOffset=heads[2].sourceOffset+paths[2].length-ApproachLength;
  var throughTargets=new[]{Y07MapEdit.EInner,Y07MapEdit.E2,Y07MapEdit.EOuter};
  var exits=new[]{FirstStraight,SecondStraight,ThirdStraight};
  for(int i=0;i<exits.Length;i++){
   var join=lanes[exits[i]];
   Reconnect(join,tails[i+1],lanes[throughTargets[i]]);
   tails[i+1].next=new[]{join.id};
  }
  var leftTurn=lanes["join:local:5897:14274:7063:14681:-1"];
  var uturn=lanes[Uturn];
  tails[0].next=new[]{leftTurn.id,uturn.id};
  Check(Vector3.Distance(tails[0].points.Last().V(),leftTurn.points[0].V())<.01f,"left and U-turn start preserved");
  leftTurn.fromNode=uturn.fromNode=tails[0].toNode;
  var rightTurn=lanes[RightTurn];
  rightTurn.fromNode=tails[4].toNode;
  tails[4].next=new[]{rightTurn.id};
  lanes[DuplicateMiddle].disabled=true;
  lanes[DuplicateMiddle].next=Array.Empty<string>();
  for(int i=0;i<heads.Length;i++){
   var head=heads[i];
   var path=paths[i];
   var outgoing=tails.Where((_,j)=>sources[j]==i).ToArray();
   Assign(head,Y07MapEdit.Slice(path,0,path.length-ApproachLength).Select(p=>p.V()));
   head.next=outgoing.Select(lane=>lane.id).ToArray();
   foreach(var tail in outgoing)tail.fromNode=head.toNode;
  }
  foreach(var facility in document.facilities.Where(f=>f.junctionId==Junction&&f.laneId==Inner)){
   facility.laneId=tails[0].id;
   facility.evidence=Evidence();
  }
  document.lanes=document.lanes.Concat(tails).ToArray();
  foreach(var scene in document.scene.objects.Where(o=>o.kind=="road"&&o.id.Contains(DuplicateMiddle)))scene.enabled=false;
  var rebuilt=exits.Select(id=>lanes[id]).ToArray();
  foreach(var join in rebuilt){
   var surface=document.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+join.id);
   if(surface!=null)surface.points=join.points;
  }
  return document;
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument document){
  var errors=RoadMapValidator.Validate(before.baseline,document).Where(issue=>issue.error).ToArray();
  Check(errors.Length==0,string.Join(" / ",errors.Take(8).Select(issue=>issue.ToString())));
  var map=RoadMapComposer.Compose(before.baseline,document);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  var names=new[]{"left","straight-1","straight-2","straight-3","right"};
  var turns=new[]{LaneDirectionPaint.Left|LaneDirectionPaint.Uturn,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight,LaneDirectionPaint.Right};
  for(int i=0;i<names.Length;i++){
   string id=i==4?RightTail:Prefix+names[i];
   var edge=graph.edges.Single(e=>e.id==id);
   Check(edge.number==i+1&&edge.next.Aggregate(0,(mask,n)=>mask|UturnMovement.PaintMask(graph.edges[n].turn))==turns[i],"lane movements "+id);
   Check(arrows[id].Any(a=>a.turns==turns[i]),"missing arrow "+id);
   Y07MapTests.Footprint(map,edge);
   foreach(int index in edge.next){
    var next=graph.edges[index];
    Check(Vector3.Distance(edge.points.Last().V(),next.points[0].V())<.03f,"connector gap "+next.id);
    if(next.turn==0)Y07MapTests.Footprint(map,next);
    else {
     var original=before.document.lanes.Single(l=>l.id==next.id);
     Check(original.points.Length==next.points.Length&&original.points.Zip(next.points,(a,b)=>Vector3.Distance(a.V(),b.V())).All(distance=>distance<.001f),"existing turn geometry changed "+next.id);
    }
   }
  }
  Check(UturnMovement.Condition(document,Prefix+"left")?.type==UturnMovement.LeftOrPedestrianGreen,"observed U-turn signal condition");
  var catalog=PassengerStopCatalog.For(map);
  var stops=catalog.Stops;
  Check(stops.Length==39,"preserved 39 stops: "+string.Join("; ",catalog.Rejected));
  var bay=catalog.Entries.Single(entry=>entry.StopId=="S09-B").Stop;
  Check(graph.edges[bay.laneEdge].id==RightTail&&Vector3.Distance(bay.V(),new Vector3(685.75964f,0,1448.07861f))<.03f,"existing S09-B bay stays on its original kerb and station");
  int routes=0;
  foreach(var start in stops)foreach(var end in stops)if(start!=end){
   Check(graph.Route(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt)!=null,"unreachable stop "+start.name+" → "+end.name);
   routes++;
  }
  Check(routes==1482,"all directed stop pairs");
  Debug.Log("Y06_EAST_VALIDATED L+U/S/S/S/R and 39 stops, 1482 routes");
 }

 public static void PreviewChecks(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  Validate(package,Preview(package));
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var document=Preview(before);
  document.revision=28;
  document.savedAt=DateTime.UtcNow.ToString("o");
  document.projectRevision="";
  Validate(before,document);
  var candidate=ProjectMapPackage.Create(before.baseline,document);
  ProjectMapStore.Save(root,candidate,token);
  var reloaded=ProjectMapStore.Load(root);
  Check(reloaded.contentHash==candidate.contentHash&&reloaded.baselineHash==before.baselineHash,"stored map identity");
  Validate(before,reloaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("Y06_EAST_APPLIED revision=28 contentHash="+reloaded.contentHash);
 }
}
