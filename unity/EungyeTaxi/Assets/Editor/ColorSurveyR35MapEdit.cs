using System;
using System.Linq;
using UnityEngine;

public static class ColorSurveyR35MapEdit {
 const string BaselineHash="12cb90de24c98418e2d8bb628ffd8f7eb7d51d79f7211f867b32f11cf8aa0ea5";
 const string GreenPrefix="reviewed:G05:SE:";
 const string YellowPrefix="reviewed:Y04:SW:";
 const string YellowJunction="J3516_12881";
 const int Approach=43;
 const float Taper=25f, Offset=3.2f;
 static readonly string[] GreenRoots={"local:6355:11817:7686:9830","local:6310:11767:7624:9804","local:6266:11718:7563:9778"};
 static readonly string[] GreenHeads={GreenRoots[0]+":part:2",GreenRoots[1]+":part:3",GreenRoots[2]+":part:2"};
 static readonly string[] GreenOpposite={"local:7809:9881:6444:11916","local:7747:9855:6400:11866","local:7870:9907:6489:11965"};
 static readonly string[] YellowHeads={"local:4187:13357:3637:12998","local:4154:13408:3604:13048"};
 static readonly string[] YellowLabels={"left","straight","straight-right"};

 static void Check(bool valid,string reason){if(!valid)throw new InvalidOperationException("COLOR_R35: "+reason);}
 static RoadMapEvidence GreenEvidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=NcZlmOmUnIe_TKerA0cRSQ,178,-29,64,Float",
  note="14 to 15: three through lanes with separate inner turn and outer right lanes at the stop. Checked the right arrow and left or pedestrian signal U-turn board from the second May 2026 panorama (H4oPEaaEe-cUJTC6DF_O-w). The 70m profile and turning curve are game estimates."
 };
 static RoadMapEvidence YellowEvidence()=>new RoadMapEvidence{
  state="observed",month="2026-05",checkedAt="2026-11-16",appearanceConfirmed=true,ruleConfirmed=true,
  url="https://map.naver.com/p?c=19,0,0,0,adh&p=gNajogryB_II7kPBO9wyGQ,-123,-32,80,Float",
  note="5 to 4: two upstream, three at the stop; inner permissive left and middle through in May 2026, outer through/right from the 2024 comparison (T0GZBh0-z_rn-MCngF7-Ng). The stations and offset are game estimates."
 };

 static RoadMapDocument Preview(ProjectMapPackage before){
  Check(before.document.revision==34&&before.contentHash==BaselineHash,"exact r34 required");
  var d=before.document.Copy();
  G05NorthWestMapEdit.Patch(before,d,GreenPrefix,"J7801_9629",GreenRoots,GreenHeads,GreenOpposite,GreenEvidence());
  PatchYellow(before,d);
  return d;
 }

 static void PatchYellow(ProjectMapPackage before,RoadMapDocument d){
  var lanes=d.lanes.ToDictionary(l=>l.id);
  var heads=YellowHeads.Select(id=>lanes[id]).ToArray();
  var paths=heads.Select(l=>new AssistedRoadNetwork.Path(l.points.Select(p=>p.V()).ToArray())).ToArray();
  Check(paths.All(p=>p.length>Approach),"Y04 approach changed length");
  Check(!d.facilities.Any(f=>YellowHeads.Contains(f.laneId))&&!d.signals.Any(p=>p.junctionId==YellowJunction),"Y04 already has authored controls");
  Check(!before.baseline.stops.Any(s=>s.graphStop&&YellowHeads.Contains(before.baseline.hdEdges[s.laneEdge].id)),"Y04 contains graph-bound stops");
  var evidence=YellowEvidence();
  var tails=new RoadMapLane[3];
  for(int i=0;i<tails.Length;i++){
   int source=i==2?1:0;
   var head=heads[source];var path=paths[source];
   float cut=path.length-Approach,shift=i==0?-Offset:0;
   var tail=JsonUtility.FromJson<RoadMapLane>(JsonUtility.ToJson(head));tails[i]=tail;
   tail.id=YellowPrefix+YellowLabels[i];tail.number=i+1;tail.sourceOffset=0;
   tail.fromNode=head.toNode;tail.toNode=tail.id+":end";
   Y06EastApproachEdit.Assign(tail,Enumerable.Range(0,Approach+1).Select(step=>{
    float at=cut+step;
    return path.Position(at)+TrafficSystem.Right(path.Tangent(at))*shift*Mathf.SmoothStep(0,1,Mathf.Clamp01(step/Taper));
   }),evidence);
  }
  tails[0].next=heads[0].next.Where(id=>lanes[id].turn==-1).ToArray();
  tails[1].next=heads[0].next.Where(id=>lanes[id].turn==0).ToArray();
  tails[2].next=heads[1].next;
  for(int i=0;i<tails.Length;i++)foreach(string id in tails[i].next){
   var join=lanes[id];
   if(i==0){
    Y06EastApproachEdit.Reconnect(join,tails[i],lanes[join.next.Single()],evidence);
    var surface=d.scene.objects.SingleOrDefault(o=>o.id=="road:polish:"+join.id);
    if(surface!=null)surface.points=join.points;
   }else join.fromNode=tails[i].toNode;
  }
  heads[0].next=new[]{tails[0].id,tails[1].id};heads[1].next=new[]{tails[2].id};
  for(int i=0;i<heads.Length;i++)Y06EastApproachEdit.Assign(heads[i],G05NorthWestMapEdit.OriginalHead(paths[i],paths[i].length-Approach),evidence);
  d.lanes=d.lanes.Concat(tails).ToArray();

  var entry=tails[0];var direction=RoadMapEasySelection.Heading(entry);
  var rule=before.baseline.junctionRules.Single(j=>j.id==YellowJunction);
  var roadAxis=new Vector3(rule.dx,0,rule.dz).normalized;
  int axis=Mathf.Abs(Vector3.Dot(direction,roadAxis))>.7f?0:1;
  int sign=Vector3.Dot(direction,axis==0?roadAxis:TrafficSystem.Right(roadAxis))>=0?1:-1;
  var signPosition=entry.points.Last().V()-direction*5-TrafficSystem.Right(direction)*2.2f;
  d.facilities=d.facilities.Concat(new[]{new RoadMapFacility{
   id=YellowPrefix+"permissive",kind="sign",type=RoadMapRules.PermissiveLeftSign,rule=true,
   laneId=entry.id,junctionId=YellowJunction,axis=axis,sign=sign,height=3f,width=2.5f,
   x=signPosition.x,z=signPosition.z,yaw=Quaternion.LookRotation(direction).eulerAngles.y,evidence=evidence
  }}).ToArray();
 }

 static void Validate(ProjectMapPackage before,RoadMapDocument d){
  G05NorthWestMapEdit.Validate(before,d,GreenPrefix);
  var map=RoadMapComposer.Compose(before.baseline,d);
  EungyeConnectivityRepair.Apply(map,before.baseline);
  var graph=new HdRoadGraph(map);
  var arrows=LaneDirectionPaint.Layout(map).ToLookup(a=>map.hdEdges[a.edgeIndex].id);
  var expected=new[]{LaneDirectionPaint.Left,LaneDirectionPaint.Straight,LaneDirectionPaint.Straight|LaneDirectionPaint.Right};
  for(int i=0;i<YellowLabels.Length;i++){
   var edge=graph.edges.Single(e=>e.id==YellowPrefix+YellowLabels[i]);
   Check(edge.number==i+1&&edge.next.Aggregate(0,(mask,n)=>mask|UturnMovement.PaintMask(graph.edges[n].turn))==expected[i],"Y04 movement "+edge.id);
   Check(arrows[edge.id].Any(a=>a.turns==expected[i]),"Y04 arrow "+edge.id);
   Y07MapTests.Footprint(map,edge);
   foreach(int next in edge.next)Y07MapTests.Footprint(map,graph.edges[next]);
  }
  var sign=d.facilities.Single(f=>f.id==YellowPrefix+"permissive");
  Check(RoadMapRules.PermissiveLeft(d,YellowJunction,sign.axis,sign.sign),"Y04 permissive sign");
  Check(!RoadMapRules.PermissiveLeft(d,YellowJunction,sign.axis,-sign.sign),"Y04 opposite arm retains its program");
  Debug.Log("Y04_SW_VALIDATED permissive L/S/S+R");
 }

 public static void PreviewChecks(){
  var before=ProjectMapStore.Load(ProjectMapBuild.Root);
  Validate(before,Preview(before));
 }

 public static void BuildReviewed(){
  var before=ProjectMapStore.Load(ProjectMapBuild.Root);
  var document=before.document;
  Check(document.revision==35,"saved r35 required");
  var map=RoadMapComposer.Compose(before.baseline,document);
  var sign=document.facilities.Single(f=>f.id==YellowPrefix+"permissive");
  var rule=before.baseline.junctionRules.Single(j=>j.id==YellowJunction);
  var junction=new TrafficJunction{center=rule.V(),name=YellowJunction,signalized=true,leftTurnMode=LeftTurnMode.ProtectedShared,direction=new Vector3(rule.dx,0,rule.dz).normalized};
  var host=new GameObject("Y04 test traffic");
  var blocker=new GameObject("Y04 test oncoming traffic");
  try{
   var system=host.AddComponent<TrafficSystem>();
   typeof(TrafficSystem).GetField("roadMap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(system,map);
   system.manualClock=true;
   junction.gameProgram=DriveFlowPolicy.Signals(system,junction);
   int axis=sign.axis,approach=sign.sign;
   Check(system.HasLeftMovement(junction,axis,approach)&&system.AuthoredPermissiveLeft(junction,axis,approach),"signed left remains present");
   Check(!junction.gameProgram.groups.Any(g=>!g.pedestrian&&g.axis==axis&&g.sign==approach&&g.turn==-1),"no protected stage for signed left");
   Check(junction.gameProgram.groups.Any(g=>!g.pedestrian&&g.axis==axis&&g.sign==-approach&&g.turn==-1),"opposite protected left retained");
   bool green=false,red=false;
   for(float t=0;t<100;t+=.5f){
    system.manualTime=t;
    var through=system.Signal(junction,axis,approach);
    green|=through==SignalColor.Green;red|=through==SignalColor.Red;
    Check(system.LeftEntrySignal(junction,axis,approach,sign.laneId)==through&&system.MovementSignal(junction,axis,-1,approach)==through,"signed left follows through signal");
    Check(system.LeftSignal(junction,axis,approach)==SignalColor.Red,"protected arrow stays dark");
    Check(Mathf.Abs(system.EstimatedGreenWait(junction,axis,approach,-1)-system.EstimatedGreenWait(junction,axis,approach,0))<.001f,"signed left uses through wait estimate");
   }
   Check(green&&red,"both through states observed");
   var heading=system.AxisDirection(junction,axis)*approach;
   blocker.transform.SetPositionAndRotation(junction.center+heading*4,Quaternion.LookRotation(-heading));
   blocker.layer=9;var box=blocker.AddComponent<BoxCollider>();
   Physics.SyncTransforms();
   Check(!system.YieldClear(junction,heading,-1,host),"signed left yields to opposing car");
   box.enabled=false;Physics.SyncTransforms();
   Check(system.YieldClear(junction,heading,-1,host),"signed left can enter after opposing car clears");
  }finally{
   UnityEngine.Object.DestroyImmediate(blocker);
   UnityEngine.Object.DestroyImmediate(host);
  }
  Debug.Log("Y04_PERMISSIVE_SIGNAL_OK signed through demand; protected opposite; opposing traffic yield");
  ColorSurveyPreviewTests.BuildReviewedCurrent();
 }

 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  var d=Preview(before);
  d.revision=35;d.projectRevision="";d.savedAt=DateTime.UtcNow.ToString("o");
  Validate(before,d);
  var package=ProjectMapPackage.Create(before.baseline,d);
  ProjectMapStore.Save(root,package,token);
  var loaded=ProjectMapStore.Load(root);
  Check(loaded.contentHash==package.contentHash&&loaded.baselineHash==before.baselineHash,"saved identity");
  Validate(before,loaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("COLOR_R35_APPLIED revision=35 contentHash="+loaded.contentHash);
 }
}
