using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Opt-in Player lifecycle diagnostic. Requires the existing isolated-save mode:
// -singlePlayer -contentTest -persistentGreenLeftCheck
// Keeps native navigation Update, driver discovery and signal clocks running.
public sealed class PersistentGreenLeftProbe : MonoBehaviour {
 DriveSession session;bool done;float started;int checks,failures;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-persistentGreenLeftCheck")<0)return;if(!ContentTuningRuntime.TestMode){Debug.LogError("PERSISTENT_GREEN_PLAYER requires -contentTest save isolation");Application.Quit(2);return;}var root=new GameObject("Persistent green Player lifecycle diagnostic");DontDestroyOnLoad(root);root.AddComponent<PersistentGreenLeftProbe>();}
 void Awake(){started=Time.realtimeSinceStartup;Application.runInBackground=true;}
 void Update(){if(!done&&Time.realtimeSinceStartup-started>180){done=true;Debug.LogError("PERSISTENT_GREEN_PLAYER_TIMEOUT");Application.Quit(2);}}
 IEnumerator Start(){var iterator=Run();while(!done){object next;try{if(!iterator.MoveNext())yield break;next=iterator.Current;}catch(Exception e){done=true;Debug.LogError("PERSISTENT_GREEN_PLAYER_EXCEPTION "+e);Application.Quit(2);yield break;}yield return next;}}
 void Check(bool value,string reason){checks++;if(!value)failures++;Debug.Log("PERSISTENT_GREEN_PLAYER "+(value?"PASS ":"FAIL ")+reason);}
 void Pose(HdRoadGraph graph,int edge,float back){var path=graph.paths[edge];float at=path.length-back;var position=path.Position(at)+Vector3.up*.464f;var rotation=Quaternion.LookRotation(path.Tangent(at));session.taxi.body.position=position;session.taxi.body.rotation=rotation;session.taxi.transform.SetPositionAndRotation(position,rotation);Physics.SyncTransforms();session.law.ResetObservation();}
 void Trace(TrafficJunction junction,int stage,string label){
  var nav=session.jobs.nav;float at,error;int nearest=nav.hd.Nearest(session.taxi.transform.position,session.taxi.transform.forward,out at);float progress=nav.ProjectVehicle(out error);var cue=NavigationCue.FromRoute(nav.hd,nav.hdRoute,progress);var state=session.traffic.CaptureSignalRuntime().Single(x=>x.junction==junction.name);
  Debug.Log("PERSISTENT_GREEN_PLAYER_TRACE "+label+" time="+Time.time+" pose="+session.taxi.transform.position.ToString("F4")+" nearest="+(nearest<0?"none":nav.hd.edges[nearest].id)+" current="+(nav.hdRoute==null?"none":nav.hd.edges[nav.hdRoute.EdgeAt(progress)].id)+" active="+nav.active+" free="+nav.freeGuidance+" retained="+nav.RetainingPreviousRoute+" cue="+cue.Move+" distance="+cue.Distance+" remaining="+nav.remaining+" routeEdges="+(nav.hdRoute==null?0:nav.hdRoute.edges.Count)+" indicator="+session.signals.direction+" manualClock="+session.traffic.manualClock+" replica="+session.traffic.ReplicaSignalRuntime+" scenario="+junction.scenarioSignal+" stage="+state.stage+" elapsed="+state.elapsed+" counts="+string.Join(",",state.demand.Select(n=>n.ToString()).ToArray())+" left="+session.traffic.LeftSignal(junction,1,-1));
  Check(state.demand[stage]>0,"current Player driver requests left "+label);
  TraceInference(junction);
 }
 void TraceInference(TrafficJunction j){
  const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var traffic=session.traffic;var nav=session.jobs.nav;var graph=(HdRoadGraph)typeof(TrafficSystem).GetField("editorGraph",flags).GetValue(traffic)??nav.hd;var map=(MapData)typeof(TrafficSystem).GetField("roadMap",flags).GetValue(traffic);float at;int current=graph.Nearest(session.taxi.transform.position,session.taxi.transform.forward,out at);var route=nav.hdRoute;
  bool inferred=(bool)typeof(TrafficSystem).GetMethod("InferredLeftDemand",flags).Invoke(traffic,new object[]{session,j,1,-1});float laneError=999,routeError=999,routeProgress=0,goalError=999,endError=999;int index=0,badEdge=-1,badConnection=-1;
  if(current>=0)graph.paths[current].Project(session.taxi.transform.position,out laneError);
  if(route!=null&&route.path!=null){routeProgress=route.path.Project(session.taxi.transform.position,out routeError);while(index<route.ends.Count&&routeProgress>=route.ends[index]-.01f)index++;for(int i=0;i<route.edges.Count;i++){int edge=route.edges[i];if(edge<0||edge>=graph.edges.Length||graph.edges[edge].disabled){badEdge=i;break;}if(i>0&&(graph.edges[route.edges[i-1]].next==null||Array.IndexOf(graph.edges[route.edges[i-1]].next,edge)<0)){badConnection=i;break;}}if(nav.hdGoal>=0&&nav.hdGoal<graph.edges.Length){var goal=graph.paths[nav.hdGoal].Position(nav.hdGoalAt);goalError=Vector3.Distance(goal,nav.destination);endError=Vector3.Distance(goal,route.path.Position(route.path.length));}}
  Debug.Log("PERSISTENT_GREEN_INFERENCE inferred="+inferred+" sameMap="+ReferenceEquals(map,session.world.map)+" trafficEdgesCurrent="+ReferenceEquals(graph.edges,map.hdEdges)+" navEdgesTraffic="+ReferenceEquals(nav.hd.edges,graph.edges)+" sameVehicle="+(nav.vehicle==session.taxi.transform)+" laneError="+laneError+" laneRemain="+(current<0?-1:graph.paths[current].length-at)+" routeError="+routeError+" globalProgress="+routeProgress+" globalIndex="+index+" globalEdge="+(route==null||index>=route.edges.Count?-1:route.edges[index])+" nearestEdge="+current+" badEdge="+badEdge+" badConnection="+badConnection+" goal="+nav.hdGoal+" goalAt="+nav.hdGoalAt+" destination="+nav.destination.ToString("F4")+" finalEdge="+(route==null?-1:route.edges[route.edges.Count-1])+" goalError="+goalError+" endError="+endError);
 }
 IEnumerator Run(){
  while(!session||!session.StartupReady||session.business.IsRestoring){session=FindObjectsOfType<DriveSession>().FirstOrDefault(s=>!s.IsSimulationBot);yield return null;}yield return null;
  Check(session.business.SavePath.StartsWith(ContentTuningRuntime.TestDirectory,StringComparison.OrdinalIgnoreCase),"campaign save is isolated");if(failures>0){done=true;Application.Quit(2);yield break;}
  Check(!session.jobs.Active,"normal fresh empty taxi");session.business.ContinueFromMenu();session.paused=false;Time.timeScale=1;
  var nav=session.jobs.nav;var graph=nav.hd;int incoming=Array.FindIndex(graph.edges,e=>e.id=="local:7322:2546:7735:2544");var junction=session.traffic.junctions.Single(j=>j.name=="J7917_2565");int stage=Array.FindIndex(junction.gameProgram.stages,s=>s.groups.Any(id=>junction.gameProgram.groups.Any(g=>g.id==id&&!g.pedestrian&&g.axis==1&&g.sign==-1&&g.turn==-1)));
  session.taxi.body.velocity=session.taxi.body.angularVelocity=Vector3.zero;session.taxi.body.isKinematic=true;Pose(graph,incoming,35);session.signals.Clear();
  var map=session.GetComponent<FullMapOverlay>();Check(map.Open(),"actual M map opens");var parking=map.Places().Single(p=>p.name=="영업 시작·마감 주차장");Check(map.SelectPlace(parking)&&map.StartPlaceGuidance(),"actual parking map guidance starts");
  foreach(float back in new[]{35f,20f,10f,6f}){Pose(graph,incoming,back);yield return new WaitForSeconds(1.3f);Trace(junction,stage,"approach back="+back);}
  bool green=false;float deadline=Time.time+60,nextTrace=Time.time;while(Time.time<deadline){if(session.traffic.LeftSignal(junction,1,-1)==SignalColor.Green)green=true;if(Time.time>=nextTrace){Trace(junction,stage,"Qoff wait");nextTrace=Time.time+5;}yield return null;}
  Check(green,"Qoff normal parking route receives protected left within60s");session.signals.Toggle(-1);green=false;deadline=Time.time+60;nextTrace=Time.time;
  while(Time.time<deadline){if(session.traffic.LeftSignal(junction,1,-1)==SignalColor.Green)green=true;if(Time.time>=nextTrace){Trace(junction,stage,"Qon wait");nextTrace=Time.time+5;}yield return null;}
  Check(green,"Qon normal parking route receives protected left within60s");Debug.Log("PERSISTENT_GREEN_PLAYER_RESULT checks="+checks+" failures="+failures);done=true;Application.Quit(failures==0?0:1);
 }
}
