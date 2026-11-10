using System;
using System.Collections.Generic;
using UnityEngine;
public partial class TrafficSystem {
 readonly Dictionary<TrafficJunction,int[]> signalDemand=new Dictionary<TrafficJunction,int[]>();
 readonly Dictionary<TrafficJunction,DemandSignalClock> demandClocks=new Dictionary<TrafficJunction,DemandSignalClock>();
 DriveSession[] signalDrivers=new DriveSession[0];float nextDemandScan,nextDriverScan,lastDemandClock;bool signalClockStarted,replicaSignalClock;
 public int AdaptiveSignalCount {get{return demandClocks.Count;}}
 public bool ReplicaSignalRuntime {get{return replicaSignalClock;}}
 bool Adaptive(TrafficJunction j){return j!=null&&j.gameProgram!=null&&j.editorProgram==null&&!j.scenarioSignal&&(!manualClock||replicaSignalClock);}
 DemandSignalClock DemandClock(TrafficJunction j){DemandSignalClock value;if(!demandClocks.TryGetValue(j,out value)){value=new DemandSignalClock(j.gameProgram);demandClocks[j]=value;signalDemand[j]=new int[j.gameProgram.stages.Length];}return value;}
 float GameSignalTime(TrafficJunction j){return Adaptive(j)?DemandClock(j).ProgramTime:Clock+j.offset;}
 public float EstimatedGreenWait(TrafficJunction j,int axis,int sign,int turn){
  if(j==null||turn>0||turn==UturnMovement.Turn||j.scenarioSignal)return -1;
  if(Adaptive(j))return DemandClock(j).EstimateGreen(axis,sign,turn);
  var p=j.editorProgram??j.gameProgram;if(p==null)return -1;float now=j.editorProgram!=null?Clock:Clock+j.offset;
  return p.SecondsToGreen(axis,sign,turn,now);
 }
 void AddDemand(TrafficJunction j,int axis,int sign,int turn){
  if(!Adaptive(j))return;DemandClock(j);var counts=signalDemand[j];
  for(int i=0;i<counts.Length;i++)foreach(string id in j.gameProgram.stages[i].groups){bool found=false;foreach(var g in j.gameProgram.groups)if(g.id==id&&!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==turn){found=true;break;}if(found){counts[i]++;break;}}
 }
 // Demand does not grant a movement. Only an existing protected stage and an
 // enabled directed lane/connector can supply intent when no indicator is set.
 bool InferredLeftDemand(DriveSession driver,TrafficJunction j,int axis,int sign){
  if(j.gameProgram==null||j.facility!=null&&!j.facility.allowLeft||!HasLeftMovement(j,axis,sign))return false;
  var taxi=driver.taxi;var direction=AxisDirection(j,axis)*sign;
  float gap=Vector3.Dot(j.center-taxi.transform.position,direction)-j.StopDistance(axis,sign);
  if(gap< -TrafficJunction.StopLineEntryTolerance||gap>40)return false;
  var nav=driver.jobs?driver.jobs.nav:null;var graph=editorGraph??(nav?nav.hd:null);
  if(graph==null||roadMap==null||!ReferenceEquals(graph.edges,roadMap.hdEdges))return false;
  float at;int current=graph.Nearest(taxi.transform.position,taxi.transform.forward,out at);if(current<0)return false;
  var lane=graph.edges[current];var path=graph.paths[current];
  if(lane.disabled||lane.turn!=0||lane.change!=0||lane.next==null||path.length-at>40)return false;
  var heading=path.Tangent(path.length);
  if(Vector3.Dot(heading,direction)<.8f||Vector3.Dot(taxi.transform.forward,path.Tangent(at))<.8f)return false;
  float width=3.5f;if(roadMap.editorDocument!=null){var authored=Array.Find(roadMap.editorDocument.lanes,l=>l.id==lane.id);if(authored==null||authored.disabled)return false;width=authored.width;}
  float error;path.Project(taxi.transform.position,out error);if(error>width*.5f)return false;
  bool dedicated=true,hasLeft=false;
  foreach(int next in lane.next){if(next<0||next>=graph.edges.Length)return false;if(graph.edges[next].disabled)continue;bool left=DemandLeftConnector(graph,current,next,j);hasLeft|=left;if(!left)dedicated=false;}
  if(!hasLeft)return false;
  // A lane whose only enabled exits are left connectors supplies its own
  // movement intent, including manual driving with no navigation destination.
  if(dedicated)return true;
  if(!nav||!nav.HasDestination||nav.RetainingPreviousRoute||nav.vehicle!=taxi.transform||nav.hd==null||!ReferenceEquals(nav.hd.edges,graph.edges))return false;
  var route=nav.hdRoute;
  if(route==null||route.path==null||route.edges.Count<2||route.ends.Count!=route.edges.Count||nav.hdGoal<0||nav.hdGoal>=graph.edges.Length||graph.edges[nav.hdGoal].disabled||float.IsNaN(nav.hdGoalAt)||float.IsInfinity(nav.hdGoalAt)||nav.hdGoalAt<0||nav.hdGoalAt>graph.paths[nav.hdGoal].length)return false;
  for(int i=0;i<route.edges.Count;i++){
   int edge=route.edges[i];if(edge<0||edge>=graph.edges.Length||graph.edges[edge].disabled)return false;
   if(i>0&&(graph.edges[route.edges[i-1]].next==null||Array.IndexOf(graph.edges[route.edges[i-1]].next,edge)<0))return false;
  }
  if(route.edges[route.edges.Count-1]!=nav.hdGoal)return false;
  var goal=graph.paths[nav.hdGoal].Position(nav.hdGoalAt);
  float goalError=Vector3.Distance(goal,nav.destination),endError=Vector3.Distance(goal,route.path.Position(route.path.length));
  if(float.IsNaN(goalError)||goalError>.5f||float.IsNaN(endError)||endError>.5f)return false;
  float progress=route.path.Project(taxi.transform.position,out error);
  if(error>width*.5f||Vector3.Dot(taxi.transform.forward,route.path.Tangent(progress))<.8f)return false;
  int index=0;while(index<route.ends.Count&&progress>=route.ends[index]-.01f)index++;
  if(index>=route.edges.Count-1||route.edges[index]!=current||route.ends[index]-progress>40)return false;
  return DemandLeftConnector(graph,current,route.edges[index+1],j);
 }
 bool DemandLeftConnector(HdRoadGraph graph,int incoming,int connector,TrafficJunction j){
  if(connector<0||connector>=graph.edges.Length||Array.IndexOf(graph.edges[incoming].next,connector)<0)return false;
  var left=graph.edges[connector];if(left.disabled||left.turn!=-1||left.change!=0||left.next==null)return false;
  bool exit=false;foreach(int next in left.next)if(next>=0&&next<graph.edges.Length&&!graph.edges[next].disabled){exit=true;break;}if(!exit)return false;
  if(graph.edges[incoming].ruleId==j.name&&left.ruleId==j.name)return true;
  if(graph.edges[incoming].status!="ngii"||left.status!="ngii")return false;
  // Native NGII rule IDs belong to individual edges. Use the same bounded
  // nearest-junction association as native protected-movement discovery.
  var path=graph.paths[connector];var middle=path.Position(path.length*.5f);TrafficJunction nearest=null;float distance=32;
  foreach(var candidate in junctions){float d=Vector3.Distance(middle,candidate.center);if(d<distance){nearest=candidate;distance=d;}}
  return nearest==j;
 }
 // PvP A2 (2026-11-10 user report: almost no signal waits). With only a few
 // bots and NPCs the demand clock rested in green for whoever arrived. Recall
 // keeps both straight stages and the walk stage in every cycle, like a fixed
 // urban signal, while protected left stages still appear only on demand.
 public bool signalRecall;
 public static bool RecallStage(RoadMapSignalProgram program,int stage){foreach(string id in program.stages[stage].groups)foreach(var g in program.groups)if(g.id==id&&(g.pedestrian||g.turn==0))return true;return false;}
 void ScanSignalDemand(){
  foreach(var j in junctions)if(Adaptive(j))DemandClock(j);foreach(var pair in signalDemand)Array.Clear(pair.Value,0,pair.Value.Length);
  if(signalRecall)foreach(var pair in signalDemand)for(int i=0;i<pair.Value.Length;i++)if(RecallStage(pair.Key.gameProgram,i))pair.Value[i]++;
  foreach(var car in cars)if(car&&car.enabled&&car.Visible&&car.route!=null)foreach(var stop in car.route.stops){
   float gap=stop.distance-car.distance;if(gap<-.1f||gap>Mathf.Max(40,car.speed*7+12))continue;
   var dir=car.route.Tangent(Mathf.Max(0,stop.distance));AddDemand(stop.junction,stop.axis,Sign(dir,stop.axis,stop.junction),stop.Movement(car.route));
  }
  // One cache for all taxis, including bots and remote humans on the host.
  if(Clock>=nextDriverScan){signalDrivers=FindObjectsOfType<DriveSession>();nextDriverScan=Clock+2;}
  foreach(var driver in signalDrivers)if(driver&&driver.traffic==this&&driver.taxi){
   var taxi=driver.taxi;TrafficJunction best=null;int bestAxis=0,bestSign=1;float nearest=100;
   foreach(var j in junctions)if(Adaptive(j))for(int axis=0;axis<2;axis++){
    int sign=Sign(taxi.transform.forward,axis,j);var dir=AxisDirection(j,axis)*sign;if(Vector3.Dot(taxi.transform.forward,dir)<.8f)continue;
    var delta=j.center-taxi.transform.position;float along=Vector3.Dot(delta,dir);float lateral=Mathf.Abs(Vector3.Dot(delta,Right(dir)));
    if(along<j.StopDistance(axis,sign)-3||along>nearest||lateral>j.halfWidth[axis]+3)continue;
    best=j;bestAxis=axis;bestSign=sign;nearest=along;
   }
   if(best!=null){var indicators=taxi.GetComponent<TaxiSignals>();int turn=indicators?indicators.direction:0;if(turn==0&&InferredLeftDemand(driver,best,bestAxis,bestSign))turn=-1;AddDemand(best,bestAxis,bestSign,turn);}
  }
  if(ambientPedestrians)foreach(var actor in ambientPedestrians.actors)if(actor&&actor.route.crossing&&!actor.state.deferredReveal&&(!actor.Crossing&&actor.state.rest<=.5f||actor.Crossing)){
   var j=actor.route.junction;if(!Adaptive(j))continue;var counts=signalDemand[j];for(int i=0;i<counts.Length;i++)foreach(string id in j.gameProgram.stages[i].groups){bool found=false;foreach(var g in j.gameProgram.groups)if(g.id==id&&g.pedestrian){found=true;break;}if(found){counts[i]++;break;}}
  }
 }
 void TickSignalDemand(){
  if(manualClock||replicaSignalClock||Time.timeScale<=0)return;float now=Clock;if(!signalClockStarted){lastDemandClock=now;signalClockStarted=true;}
  if(now>=nextDemandScan){ScanSignalDemand();nextDemandScan=now+.25f;}
  float dt=Mathf.Clamp(now-lastDemandClock,0,1);lastDemandClock=now;
  foreach(var pair in demandClocks)if(Adaptive(pair.Key))pair.Value.Step(dt,signalDemand[pair.Key],ambientPedestrians&&ambientPedestrians.CrossingOccupied(pair.Key));
 }
 public DemandSignalState[] CaptureSignalRuntime(){var states=new List<DemandSignalState>();foreach(var j in junctions)if(Adaptive(j))states.Add(DemandClock(j).Capture(j.name));return states.ToArray();}
 public void RestoreSignalRuntime(DemandSignalState[] states){
  if(states==null)throw new ArgumentException("Missing authoritative signal runtime");if(states.Length>junctions.Count)throw new ArgumentException("Invalid signal count");var seen=new HashSet<string>();
  foreach(var state in states){var j=state==null?null:junctions.Find(x=>x.name==state.junction);if(j==null||j.gameProgram==null||j.editorProgram!=null||!seen.Add(j.name))throw new ArgumentException("Invalid signal junction");DemandClock(j).Restore(state);}replicaSignalClock=true;
 }
}
