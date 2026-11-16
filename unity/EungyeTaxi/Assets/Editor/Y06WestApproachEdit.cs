using System;
using System.Linq;
using UnityEngine;

// J5692 east approach. Its three through lanes retain distinct exits.
public static class Y06WestApproachEdit {
 const string Prefix="reviewed:Y06:west:";
 const string RequiredHash="0b270a36e452439324eedb558a0f01c06f54d8aa11cdae6c666b5cc3a4ce784e";
 const float ApproachLength=55f;
 const float TaperLength=25f;
 static readonly string[] Roots={"local:7043:14739:5877:14332","local:7023:14796:5857:14389","local:7003:14854:5837:14446"};
 static readonly string[] Heads={Roots[0]+":part:2",Roots[1]+":part:3",Roots[2]+":part:2"};
 static readonly string[] Names={"left","straight-1","straight-2","straight-3"};

 static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Y06_WEST: "+message);}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane)=>new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());
 static RoadMapEvidence Evidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=x43UqW58YuCBsD-bLEHtWA,250,-20,80,Float",
  note="Map 7 to 6 westbound: L/S/S/S observed in May 2026 and checked in Mar 2024. School gate and Eungye Lake Road align to J5692. May 2026 SFPQkna-ng5ZvQkMshqCyQ (x647.92 z1458.93) is upstream, with three through lanes. The 55m approach, 25m taper and lane centrelines are game estimates."
 };

 static RoadMapDocument Preview(ProjectMapPackage package){
  Check(package.document.revision==28&&package.contentHash==RequiredHash,"requires exact r28 baseline");
  var document=package.document.Copy();
  var lanes=document.lanes.ToDictionary(lane=>lane.id);
  var heads=Heads.Select(id=>lanes[id]).ToArray();
  var paths=heads.Select(Path).ToArray();
  Check(paths.All(path=>path.length>ApproachLength),"taper must lie on the terminal pieces");
  var origin=paths[0].Position(paths[0].length);
  var right=TrafficSystem.Right(paths[0].Tangent(paths[0].length));
  float span=Vector3.Dot(paths[2].Position(paths[2].length)-origin,right);
  var sources=new[]{0,0,1,2};
  var tails=new RoadMapLane[Names.Length];
  var evidence=Evidence();

  for(int i=0;i<tails.Length;i++){
   int source=sources[i];
   var path=paths[source];
   var tail=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(heads[source]));
   tail.id=Prefix+Names[i];
   tail.number=i+1;
   tail.width=3.7f;
   tail.sourceOffset=0;
   tail.fromNode=heads[source].toNode;
   tail.toNode=tail.id+":end";
   float shift=i==3?0:span*i/3-Vector3.Dot(path.Position(path.length)-origin,right);
   float start=path.length-ApproachLength;
   Y06EastApproachEdit.Assign(tail,Enumerable.Range(0,(int)ApproachLength+1).Select(step=>
    path.Position(start+step)+right*shift*Mathf.SmoothStep(0,1,Mathf.Clamp01(step/TaperLength))),evidence);
   tails[i]=tail;
  }

  var left=lanes["join:"+Roots[0]+":-1"];
  left.fromNode=tails[0].toNode;
  tails[0].next=new[]{left.id};
  for(int i=0;i<Roots.Length;i++){
   var join=lanes["join:"+Roots[i]+":0"];
   Y06EastApproachEdit.Reconnect(join,tails[i+1],lanes[join.next.Single()],evidence);
   tails[i+1].next=new[]{join.id};
   var surface=document.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+join.id);
   if(surface!=null)surface.points=join.points;
  }
  for(int i=0;i<heads.Length;i++){
   var head=heads[i];
   var path=paths[i];
   Y06EastApproachEdit.Assign(head,Y07MapEdit.Slice(path,0,path.length-ApproachLength).Select(p=>p.V()),evidence);
   head.next=tails.Where((_,j)=>sources[j]==i).Select(lane=>lane.id).ToArray();
  }
  document.lanes=document.lanes.Concat(tails).ToArray();
  return document;
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument document){
  var issues=RoadMapValidator.Validate(before.baseline,document).Where(issue=>issue.error).ToArray();
  Check(issues.Length==0,string.Join(" / ",issues.Take(8).Select(issue=>issue.ToString())));
  var map=RoadMapComposer.Compose(before.baseline,document);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  for(int i=0;i<Names.Length;i++){
   string id=Prefix+Names[i];
   var edge=graph.edges.Single(e=>e.id==id);
   var join=graph.edges[edge.next.Single()];
   var original=before.document.lanes.Single(l=>l.id==join.id);
   Check(!edge.disabled&&!join.disabled&&edge.number==i+1&&join.turn==(i==0?-1:0),"lane movements "+id);
   Check(graph.edges[join.next.Single()].id==original.next.Single(),"existing turn or through destination "+id);
   Check(Vector3.Distance(edge.points.Last().V(),join.points[0].V())<.03f,"connector gap "+id);
   Check(arrows[id].Any(a=>a.turns==(i==0?LaneDirectionPaint.Left:LaneDirectionPaint.Straight)),"missing arrow "+id);
   Y07MapTests.Footprint(map,edge);
   if(i>0)Y07MapTests.Footprint(map,join);
  }
  var catalog=PassengerStopCatalog.For(map);
  Check(catalog.Stops.Length==39,"preserved 39 stops: "+string.Join("; ",catalog.Rejected));
  int routes=0;
  foreach(var start in catalog.Stops)foreach(var end in catalog.Stops)if(start!=end){
   Check(graph.Route(start.laneEdge,start.laneAt,end.laneEdge,end.laneAt)!=null,"unreachable stop "+start.name+" → "+end.name);
   routes++;
  }
  Check(routes==1482,"all directed stop pairs");
  Debug.Log("Y06_WEST_VALIDATED L/S/S/S and 39 stops, 1482 routes");
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var document=Preview(before);
  document.revision=29;
  document.savedAt=DateTime.UtcNow.ToString("o");
  document.projectRevision="";
  Validate(before,document);
  var candidate=ProjectMapPackage.Create(before.baseline,document);
  ProjectMapStore.Save(root,candidate,token);
  var reloaded=ProjectMapStore.Load(root);
  Check(reloaded.contentHash==candidate.contentHash&&reloaded.baselineHash==before.baselineHash,"stored map identity");
  Validate(before,reloaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("Y06_WEST_APPLIED revision=29 contentHash="+reloaded.contentHash);
 }
}
