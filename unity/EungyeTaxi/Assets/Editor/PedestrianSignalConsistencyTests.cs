using System;
using UnityEngine;

public static class PedestrianSignalConsistencyTests {
 static void Check(bool okay,string reason){if(!okay)throw new Exception("PEDESTRIAN_SIGNAL_CONSISTENCY: "+reason);}
 public static void Run(){
  var root=new GameObject("pedestrian signal consistency");
  try{
   var traffic=root.AddComponent<TrafficSystem>();traffic.manualClock=true;
   var j=new TrafficJunction{direction=Vector3.forward,scenarioSignal=true,rightTurnObservation=new RightPracticeState(6).Observe()};
   // Northbound turns east: the person crosses the east arm, whose incoming heading is west.
   j.observationAxis=0;j.observationSign=1;
   Check(traffic.PedestrianGreen(j,1,-1),"northbound right-turn person must see green on east arm");
   var map=new MapData{areas=new[]{new MapArea{kind="sidewalk",height=.2f,points=new[]{new MapPoint{x=-30,z=-30},new MapPoint{x=30,z=-30},new MapPoint{x=30,z=30},new MapPoint{x=-30,z=30}}}},hdEdges=new HdEdge[0]};
   PedestrianSignalDisplay.Create(traffic,map,j,Vector3.left,5,5,root.transform);
   var displays=root.GetComponentsInChildren<PedestrianSignalDisplay>();Check(displays.Length==2,"east crossing has both signal faces");
   foreach(var display in displays){var lamp=display.transform.Find("보행 사람").GetComponent<Renderer>();Check(lamp.sharedMaterial.color==Color.green,"both actual outgoing green lamps match crossing state");}
   Check(!traffic.PedestrianGreen(j,0,1),"incoming south arm must not inherit outgoing pedestrian observation");
   j.observationSign=-1;
   Check(traffic.PedestrianGreen(j,1,1),"southbound right-turn person must see green on west arm");
   j.observationAxis=1;j.observationSign=1;
   Check(traffic.PedestrianGreen(j,0,1),"eastbound right-turn person must see green on south arm");
   j.observationSign=-1;
   Check(traffic.PedestrianGreen(j,0,-1),"westbound right-turn person must see green on north arm");
   j.rightTurnObservation.pedestrianGreen=false;
   Check(!traffic.PedestrianGreen(j,0,-1),"explicit red observation stays red even with crossing occupancy");
   var policy=typeof(AmbientPedestrian).GetMethod("MayJaywalk");Check(policy!=null,"bounded jaywalk policy exists");
   Check(!(bool)policy.Invoke(null,new object[]{"J5076_2580:crossing",4,20f}),"four normal trips cannot enable jaywalk");
   Check(!(bool)policy.Invoke(null,new object[]{"J5076_2580:crossing",5,7.9f}),"red wait below eight seconds cannot enable jaywalk");
   Check((bool)policy.Invoke(null,new object[]{"J5076_2580:crossing",5,8f}),"designated actor may occasionally cross after five normal trips");
   Check(!(bool)policy.Invoke(null,new object[]{"J5057_4449:crossing",5,20f}),"other actors always wait for green");
   RunCrossing(root,traffic);
   Debug.Log("PEDESTRIAN_SIGNAL_CONSISTENCY_OK four outgoing arms and red observation");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static void RunCrossing(GameObject root,TrafficSystem traffic){
  // Inactive owner avoids starting a game/world; exercise the real crossing update and save paths.
  var owner=new GameObject("isolated pedestrian owner");owner.SetActive(false);owner.transform.SetParent(root.transform);
  var session=owner.AddComponent<DriveSession>();session.traffic=traffic;
  var system=owner.AddComponent<AmbientPedestrianSystem>();system.session=session;
  var actor=owner.AddComponent<AmbientPedestrian>();actor.system=system;
  var junction=new TrafficJunction{direction=Vector3.forward,scenarioSignal=true,observationAxis=0,observationSign=1,rightTurnObservation=new RightPracticeState(6).Observe()};
  actor.route=new AmbientPedestrianRoute{id="J5076_2580:crossing",crossing=true,axis=1,sign=-1,junction=junction,a=Vector3.zero,b=Vector3.right*10};
  actor.state=new AmbientPedestrianState{id=actor.route.id};system.actors.Add(actor);
  var tick=typeof(AmbientPedestrian).GetMethod("TickCrossing",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  typeof(AmbientPedestrian).GetField("speed",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(actor,1.3f);
  Action<float> advance=seconds=>tick.Invoke(actor,new object[]{seconds});
  junction.rightTurnObservation.pedestrianGreen=false;advance(20);
  Check(!actor.Crossing,"fresh actor waits through red");
  for(int i=0;i<5;i++){
   junction.rightTurnObservation.pedestrianGreen=true;advance(2);
   Check(actor.Crossing&&!actor.state.jaywalking,"ordinary departure is green");
   junction.rightTurnObservation.pedestrianGreen=false;advance(10);
   Check(!actor.Crossing&&actor.state.normalCrossings==i+1,"green-start crossing finishes on red and counts once");
   advance(0); // Observe the red phase after arriving, before the next green.
  }
  advance(7.9f);Check(!actor.Crossing,"eligible actor still waits eight seconds red");advance(.2f);
  Check(actor.Crossing&&actor.state.jaywalking&&actor.state.normalCrossings==0,"one bounded red departure resets normal-trip budget");
  var saved=JsonUtility.FromJson<AmbientPedestrianSnapshot>(JsonUtility.ToJson(system.Capture()));system.Validate(saved);actor.Restore(saved.actors[0]);
  Check(actor.Crossing&&actor.state.jaywalking,"red-start occupancy survives save restore");advance(10);advance(20);
  Check(!actor.Crossing&&actor.state.normalCrossings==0&&!actor.state.jaywalking,"no consecutive red departures");
  var legacy=JsonUtility.FromJson<AmbientPedestrianState>("{\"id\":\"J5076_2580:crossing\"}");actor.Restore(legacy);system.Validate(system.Capture());
  Check(actor.state.normalCrossings==0&&actor.state.redWait==0&&!actor.state.jaywalking,"legacy save safely starts with no jaywalk budget");
  var invalid=system.Capture();invalid.actors[0].normalCrossings=-1;bool rejected=false;try{system.Validate(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid jaywalk budget rejected");
  junction.editorProgram=new RoadMapSignalProgram{groups=new[]{new RoadMapSignalGroup{id="walk",pedestrian=true,axis=1,sign=-1}},stages=new[]{new RoadMapStage{green=10,yellow=0,clearance=2,groups=new[]{"walk"}}}};
  traffic.manualTime=11;advance(1);Check(!actor.Crossing,"authored program red blocks ordinary departure");
  traffic.manualTime=1;Check(traffic.PedestrianGreenRemaining(junction,1,-1)<0,"authored program currently exposes unknown countdown");advance(1);
  Check(actor.Crossing&&!actor.state.jaywalking,"authored green with unknown countdown permits ordinary crossing");
 }
}
