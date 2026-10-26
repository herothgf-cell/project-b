using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Exercise manual rule observers on the canonical reviewed lanes, rather than a schematic junction.
public static class ReviewedJunctionObserverTests {
 const string Junction="J5057_4449";
 static readonly MethodInfo LawTick=typeof(DrivingLaw).GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
 static readonly FieldInfo LawEntry=typeof(DrivingLaw).GetField("entry",BindingFlags.Instance|BindingFlags.NonPublic);
 static readonly FieldInfo MannersCrossing=typeof(DrivingManners).GetField("crossing",BindingFlags.Instance|BindingFlags.NonPublic);
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("REVIEWED_JUNCTION_OBSERVER_FAIL "+message);}
 sealed class Driver : IDisposable {
  readonly GameObject sessionObject,taxiObject;
  public readonly DriveSession session;
  public Driver(TrafficSystem traffic){
   sessionObject=new GameObject("Reviewed junction observer session");sessionObject.SetActive(false);
   session=sessionObject.AddComponent<DriveSession>();session.traffic=traffic;
   session.business=sessionObject.AddComponent<BusinessProgress>();session.business.enabledCampaign=false;session.business.ledger=new EconomyLedger(new PrototypeSettings());
   session.jobs=sessionObject.AddComponent<PassengerJobs>();
   taxiObject=new GameObject("Reviewed junction observer taxi");taxiObject.SetActive(false);
   session.taxi=taxiObject.AddComponent<TaxiController>();session.taxi.body=taxiObject.AddComponent<Rigidbody>();
   session.assisted=taxiObject.AddComponent<AssistedDriveController>();
   session.manners=taxiObject.AddComponent<DrivingManners>();session.manners.session=session;
   session.law=taxiObject.AddComponent<DrivingLaw>();session.law.session=session;
  }
  public void Move(Vector3 previous,Vector3 front,Vector3 heading,bool first){
   session.taxi.transform.position=front-heading*2;session.taxi.transform.forward=heading;
   LawTick.Invoke(session.law,null);if(!first)session.manners.CheckSignal(previous,front);
  }
  public void Dispose(){UnityEngine.Object.DestroyImmediate(taxiObject);UnityEngine.Object.DestroyImmediate(sessionObject);}
 }
 static TrafficSystem Traffic(MapData map,GameObject root){var result=root.AddComponent<TrafficSystem>();result.Build(map,false);result.manualClock=true;return result;}
 static float Phase(TrafficSystem traffic,TrafficJunction j,int axis,int sign,string lane,int turn,bool green){
  for(int t=0;t<240;t++){
   traffic.manualTime=t;
   var forward=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,0,t,lane):traffic.Signal(j,axis,sign);
   var left=j.editorProgram!=null?j.editorProgram.Movement(axis,sign,-1,t,lane):traffic.LeftSignal(j,axis,sign);var movement=turn<0?left:forward;
   if(movement==(green?SignalColor.Green:SignalColor.Red)&&forward!=SignalColor.Yellow&&left!=SignalColor.Yellow)return t;
  }
  throw new Exception("No reviewed signal phase for "+lane+" green="+green);
 }
 static void Drive(TrafficSystem traffic,HdRoadGraph.RoutePath route,string action,bool green,string name){
  using(var driver=new Driver(traffic)){
   var previous=route.path.Position(0);
   for(float d=0;d<=route.path.length;d+=.5f){var point=route.path.Position(d);driver.Move(previous,point,route.path.Tangent(d),d==0);previous=point;}
   var events=driver.session.law.ledger.Events.Where(e=>e.junction==Junction).ToArray();
   Check(events.Length==1,name+" law event count="+events.Length);
   Check(events[0].action==action,name+" action="+events[0].action);
   Check(events[0].observation.forward==(green?SignalColor.Green:SignalColor.Red),name+" law records entry signal");
   if(action=="좌회전")Check(events[0].Deduction==0&&events[0].rules.Any(r=>r.rule=="L01"&&r.verdict==LawVerdict.Deferred),name+" unedited left signal remains deferred");
   else Check((events[0].Deduction==0)==green,name+" law verdict deduction="+events[0].Deduction);
   Check(driver.session.manners.redEvents==(green?0:1),name+" manners signal count="+driver.session.manners.redEvents);
   Check(driver.session.manners.ledger.Score==(green?100:88),name+" manners score="+driver.session.manners.ledger.Score);
  }
 }
 static void Reject(TrafficSystem traffic,Vector3 crossing,Vector3 travel,Vector3 heading,string name){
  using(var driver=new Driver(traffic)){
   var before=crossing-travel*2;var after=crossing+travel;
   driver.Move(before,before,heading,true);driver.Move(before,after,heading,false);
   Check(LawEntry.GetValue(driver.session.law)==null,name+" law rejects invalid approach");
   Check(MannersCrossing.GetValue(driver.session.manners)==null,name+" manners rejects invalid approach");
   Check(driver.session.law.ledger.Events.Length==0&&driver.session.manners.redEvents==0,name+" no unrelated signal penalty");
  }
 }
 public static void Run(){
  checks=0;var package=ProjectMapStore.Load(ProjectMapBuild.Root);MapData map;string previousRegion=MapCatalog.LockedRegion;
  try{MapCatalog.LockedRegion="eungye";map=MapCatalog.Load();}finally{MapCatalog.LockedRegion=previousRegion;}
  // Keep every canonical directed edge; limit rendered furniture to the junction under observation.
  map.junctionRules=map.junctionRules.Where(j=>j.id==Junction).ToArray();map.roads=new MapRoad[0];
  map.editorDocument.facilities=map.editorDocument.facilities.Where(f=>f.junctionId==Junction&&f.kind=="stopLine").ToArray();
  var graph=new HdRoadGraph(map);Debug.Log("REVIEWED_JUNCTION_OBSERVER_CANONICAL hash="+package.contentHash+" edges="+map.hdEdges.Length);
  var root=new GameObject("Reviewed junction observer traffic");
  try{
   var traffic=Traffic(map,root);var j=traffic.junctions.Single(x=>x.name==Junction);var failures=new System.Collections.Generic.List<string>();
   string through=package.document.lanes.Single(l=>RoadMapAuthoringOperations.Origin(l)==ReviewedJunctionMap.North&&l.sourceOffset>1).id;
   string[] incoming={"reviewed:J5057-N:left",through},outgoing={"A2218G611338:0",ReviewedJunctionMap.South};
   for(int i=0;i<incoming.Length;i++){
    int start=Array.FindIndex(map.hdEdges,e=>e.id==incoming[i]),end=Array.FindIndex(map.hdEdges,e=>e.id==outgoing[i]);
    var route=graph.Route(start,graph.paths[start].length-22,end,Mathf.Min(20,graph.paths[end].length));
    Check(route!=null,"canonical route "+incoming[i]);
    var approach=graph.paths[start];float at=0;
    // The authored visible main stop line is horizontal at z=477; find its actual lane centre crossing.
    for(float d=0;d<approach.length;d+=.1f)if(approach.Position(d).z>=477&&approach.Position(d+.1f).z<=477){at=d;break;}
    Check(at>0,"actual visible stop crossing "+incoming[i]);
    var point=approach.Position(at);var heading=approach.Tangent(at);int axis=0,sign=traffic.Sign(heading,axis,j);
    Check(Vector3.Dot(point-j.center,TrafficSystem.Right(traffic.AxisDirection(j,axis)*sign))<=.15f,"regression is outside legacy strip "+incoming[i]);
    Check(traffic.UturnCondition(incoming[i])==null,"ordinary movement has no U-turn sign "+incoming[i]);
    foreach(bool green in new[]{false,true}){traffic.manualTime=Phase(traffic,j,axis,sign,incoming[i],i==0?-1:0,green);try{Drive(traffic,route,i==0?"좌회전":"직진",green,incoming[i]+(green?" green":" red"));}catch(Exception e){failures.Add(e.Message);}}
    Reject(traffic,point,heading,-heading,incoming[i]+" wrong way");
   }
   Reject(traffic,new Vector3(520,0,477),Vector3.back,Vector3.back,"distant neighbouring pose");
   // Preserve canonical lane geometry but remove its only directed junction connection.
   int leftIndex=Array.FindIndex(map.hdEdges,e=>e.id==incoming[0]);map.hdEdges[leftIndex].next=new int[0];
   var disconnectedRoot=new GameObject("Disconnected reviewed approach observer traffic");
   try{var disconnected=Traffic(map,disconnectedRoot);var path=graph.paths[leftIndex];float gap;float at=path.Project(new Vector3(506.32615f,0,477),out gap);Reject(disconnected,path.Position(at),path.Tangent(at),path.Tangent(at),"disconnected canonical left approach");}
   finally{UnityEngine.Object.DestroyImmediate(disconnectedRoot);}
   Check(failures.Count==0,string.Join(" | ",failures.ToArray()));
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Debug.Log("REVIEWED_JUNCTION_OBSERVER_TESTS_OK checks="+checks+" canonical left/straight red/green and invalid approaches");
 }
}
