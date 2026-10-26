using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class AdditionalUturnObserverTests {
 static void Check(bool value,string message){if(!value)throw new Exception("UTURN MAP OBSERVER: "+message);}
 static readonly MethodInfo LawTick=typeof(DrivingLaw).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);

 static void Drive(TrafficSystem traffic,HdRoadGraph.RoutePath route,bool allowed,string name) {
  var sessionObject=new GameObject("Uturn map observer session "+name);
  sessionObject.SetActive(false);
  var session=sessionObject.AddComponent<DriveSession>();
  session.traffic=traffic;
  session.business=sessionObject.AddComponent<BusinessProgress>();
  session.business.enabledCampaign=false;
  session.business.ledger=new EconomyLedger(new PrototypeSettings());
  session.jobs=sessionObject.AddComponent<PassengerJobs>();
  var taxiObject=new GameObject("Uturn map observer taxi "+name);
  taxiObject.SetActive(false);
  var taxi=taxiObject.AddComponent<TaxiController>();
  taxi.body=taxiObject.AddComponent<Rigidbody>();
  session.taxi=taxi;
  session.assisted=taxiObject.AddComponent<AssistedDriveController>();
  session.manners=taxiObject.AddComponent<DrivingManners>();
  session.manners.session=session;
  session.law=taxiObject.AddComponent<DrivingLaw>();
  session.law.session=session;
  Vector3 previous=Vector3.zero;
  for(float d=0;d<=route.path.length;d+=.75f){
   var point=route.path.Position(d);
   var heading=route.path.Tangent(d);
   taxi.transform.position=point-heading*2;
   taxi.transform.forward=heading;
   LawTick.Invoke(session.law,null);
   if(d>0)session.manners.CheckSignal(previous,point);
   previous=point;
  }
  var events=session.law.ledger.Events.Where(e=>e.action=="유턴").ToArray();
  Check(events.Length==1,name+" U-turn law event count="+events.Length+" actual="+string.Join(",",session.law.ledger.Events.Select(e=>e.action+":"+e.Deduction).ToArray()));
  Check((events[0].Deduction==0)==allowed,name+" U-turn law verdict");
  Check((session.manners.ledger.Score==100)==allowed,name+" U-turn manners verdict score="+session.manners.ledger.Score);
  UnityEngine.Object.DestroyImmediate(taxiObject);
  UnityEngine.Object.DestroyImmediate(sessionObject);
 }

 public static void Run(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);
  var selected=AdditionalReviewedUturnMap.Incoming.Concat(AdditionalReviewedUturnMap.Outgoing).Concat(AdditionalReviewedUturnMap.Names.Select(n=>"join:reviewed:"+n+":uturn")).ToArray();
  var selectedIds=new System.Collections.Generic.HashSet<string>(selected);
  var baseline=new MapData{mapId=package.baseline.mapId,hybrid=true,width=package.baseline.width,depth=package.baseline.depth,roads=new MapRoad[0],buildings=new MapBuilding[0],targets=package.baseline.targets.Take(2).ToArray(),stops=new MapStop[0],landmarks=new MapLandmark[0],areas=new MapArea[0],hdEdges=new HdEdge[0],hdTrials=new HdTrialCase[0],hdMarks=new HdMark[0],hdSignals=new MapPoint[0],junctionRules=package.baseline.junctionRules.Where(r=>AdditionalReviewedUturnMap.Junctions.Contains(r.id)).ToArray()};
  var document=package.document.Copy();
  document.lanes=document.lanes.Where(l=>selectedIds.Contains(l.id)).ToArray();
  foreach(var lane in document.lanes)lane.next=lane.next.Where(selectedIds.Contains).ToArray();
  document.facilities=document.facilities.Where(f=>AdditionalReviewedUturnMap.Junctions.Contains(f.junctionId)&&(UturnMovement.IsCondition(f.type)||f.kind=="stopLine")).ToArray();
  document.signals=document.signals.Where(p=>AdditionalReviewedUturnMap.Junctions.Contains(p.junctionId)).ToArray();
  document.scene=null;
  document.baseHash=RoadMapRuntime.Hash(baseline);
  var map=RoadMapComposer.Compose(baseline,document);
  var graph=new HdRoadGraph(map);
  var root=new GameObject("Reviewed Uturn map observer traffic");
  var traffic=root.AddComponent<TrafficSystem>();
  traffic.Build(map,false);
  traffic.manualClock=true;
  for(int i=0;i<AdditionalReviewedUturnMap.Incoming.Length;i++){
   string name=AdditionalReviewedUturnMap.Names[i],incoming=AdditionalReviewedUturnMap.Incoming[i],outgoing=AdditionalReviewedUturnMap.Outgoing[i];
   int start=Array.FindIndex(map.hdEdges,e=>e.id==incoming),end=Array.FindIndex(map.hdEdges,e=>e.id==outgoing);
   Check(start>=0&&end>=0,name+" map edges");
   var path=graph.Route(start,Mathf.Max(0,graph.paths[start].length-32),end,Mathf.Min(16,graph.paths[end].length));
   Check(path!=null&&path.edges.Any(e=>map.hdEdges[e].id=="join:reviewed:"+name+":uturn"),name+" authored U-turn path");
   var from=package.document.lanes.First(l=>l.id==incoming);
   var j=traffic.junctions.First(x=>x.name==from.ruleId);
   var heading=(from.points[from.points.Length-1].V()-from.points[from.points.Length-2].V()).normalized;
   int axis=Mathf.Abs(Vector3.Dot(heading,traffic.AxisDirection(j,0)))>.7f?0:1;
   int sign=traffic.Sign(heading,axis,j);
   var stop=traffic.StopPosition(j,axis,sign);
   var stopNormal=traffic.StopNormal(j,axis,sign);
   bool crossed=false;Vector3 entryPosition=Vector3.zero,entryHeading=Vector3.zero;
   for(float d=.5f;d<path.path.length;d+=.5f){
    Vector3 before=path.path.Position(d-.5f),after=path.path.Position(d);
    if(Vector3.Dot(before-stop,stopNormal)<TrafficJunction.StopLineEntryTolerance&&Vector3.Dot(after-stop,stopNormal)>=TrafficJunction.StopLineEntryTolerance){
     crossed=true;entryPosition=after;entryHeading=path.path.Tangent(d);break;
    }
   }
   Debug.Log("ADDITIONAL_ENTRY "+name+" entry="+entryPosition+" heading="+entryHeading+" stop="+stop+" lane="+traffic.EditorLaneId(entryPosition-entryHeading*2,entryHeading)+" expected="+incoming+" widths="+j.halfWidth[0]+","+j.halfWidth[1]);Check(crossed,name+" actual approach crosses stop plane");Check(traffic.SignedUturnApproach(j,axis,sign,entryPosition-entryHeading*2,entryHeading),name+" exact signed incoming lane is bound");Check(!traffic.SignedUturnApproach(j,axis,-sign,entryPosition-entryHeading*2,entryHeading),name+" opposite approach remains unbound");
   float legal=-1,illegal=-1;bool leftPhase=false,walkPhase=false;
   for(int t=0;t<240;t++){
    traffic.manualTime=t;
    if(traffic.UturnPermitted(j,axis,sign,incoming)){if(legal<0)legal=t;leftPhase|=traffic.LeftSignal(j,axis,sign)==SignalColor.Green;walkPhase|=traffic.PedestrianGreen(j,axis,sign);}
    else if(illegal<0&&traffic.Signal(j,axis,sign)!=SignalColor.Yellow&&traffic.LeftSignal(j,axis,sign)!=SignalColor.Yellow)illegal=t;
   }
   Check(legal>=0&&illegal>=0&&leftPhase&&walkPhase,name+" left / pedestrian / prohibited condition phases");
   traffic.manualTime=legal;Drive(traffic,path,true,name+" legal");
   traffic.manualTime=illegal;Drive(traffic,path,false,name+" illegal");
  }
  UnityEngine.Object.DestroyImmediate(root);
  Debug.Log("ADDITIONAL_UTURN_OBSERVER_TESTS_OK three authored paths legal illegal");
 }
}
