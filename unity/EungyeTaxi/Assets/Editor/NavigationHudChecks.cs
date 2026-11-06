using System;
using System.Reflection;
using UnityEngine;

// Real route costs and read-only head forecasts. Reflection keeps the RED stage compilable.
public static class NavigationHudChecks {
 static int checks;static Type preview;
 static void Check(bool value,string name){checks++;if(!value)throw new Exception("NAVIGATION_HUD "+name);}
 static T Read<T>(object value,string name){var p=value.GetType().GetProperty(name);Check(p!=null,"preview exposes "+name);return (T)p.GetValue(value);}
 static object Program(RoadMapSignalProgram p,float t){return preview.GetMethod("Program").Invoke(null,new object[]{p,0,1,t});}
 static object Adaptive(RoadMapSignalProgram p,DemandSignalState s,bool occupied){return preview.GetMethod("Adaptive").Invoke(null,new object[]{p,s,0,1,occupied});}
 static void Head(object result,float seconds,SignalColor straight,SignalColor left,string name){Check(Read<bool>(result,"Known"),name+" known");float actual=Read<float>(result,"Seconds");Check(Math.Abs(actual-seconds)<.03f,name+" seconds expected="+seconds+" actual="+actual);Check(Read<SignalColor>(result,"Straight")==straight&&Read<SignalColor>(result,"Left")==left,name+" next heads");Check(Read<string>(result,"CountdownText")==Mathf.CeilToInt(seconds)+"초",name+" numeric countdown only");}
 static RoadMapSignalProgram Signals(){return new RoadMapSignalProgram{groups=new[]{new RoadMapSignalGroup{id="s",axis=0,sign=1},new RoadMapSignalGroup{id="l",axis=0,sign=1,turn=-1},new RoadMapSignalGroup{id="b",axis=1,sign=1},new RoadMapSignalGroup{id="w",axis=0,sign=1,pedestrian=true}},stages=new[]{new RoadMapStage{groups=new[]{"s"},green=10,yellow=2,clearance=2},new RoadMapStage{groups=new[]{"l"},green=4,yellow=2,clearance=2},new RoadMapStage{groups=new[]{"b"},green=10,yellow=2,clearance=2},new RoadMapStage{groups=new[]{"w"},green=10,yellow=0,clearance=2}}};}
 static HdEdge Edge(string id,string from,string to,int[] next,params Vector2[] points){var p=new MapPoint[points.Length];for(int i=0;i<p.Length;i++)p[i]=new MapPoint{x=points[i].x,z=points[i].y};return new HdEdge{id=id,fromNode=from,toNode=to,next=next,points=p};}
 static void Routes(){
  var host=new GameObject("navigation HUD fixture");var taxi=new GameObject("navigation vehicle");
  try {
   var nav=host.AddComponent<RoadNavigation>();var conditions=host.AddComponent<RoadConditions>();nav.conditions=conditions;
   var map=new MapData{hybrid=true,roads=new MapRoad[0],hdEdges=new[]{Edge("start","a","b",new[]{1,2},new Vector2(0,0),new Vector2(0,10)),Edge("short","b","c",new[]{3},new Vector2(0,10),new Vector2(0,110)),Edge("detour","b","c",new[]{3},new Vector2(0,10),new Vector2(50,60),new Vector2(0,110)),Edge("goal","c","d",new int[0],new Vector2(0,110),new Vector2(0,120))}};
   taxi.transform.position=new Vector3(0,0,2);nav.Build(map,taxi.transform);
   conditions.AddIncident(new Vector3(0,0,60),Vector3.forward,3.2f,90);
   nav.SetHdDestination(3,8);Check(nav.PreferFastest&&nav.hdRoute!=null&&nav.hdRoute.edges.Contains(2)&&!nav.hdRoute.edges.Contains(1),"fastest takes longer actual connected detour around 90 second queue");float fastDistance=nav.remaining,fastSeconds=nav.estimatedSeconds;
   nav.TogglePreference();Check(!nav.PreferFastest&&nav.hdRoute.edges.Contains(1)&&!nav.hdRoute.edges.Contains(2),"Tab selects actual shorter path at same start and goal");Check(nav.remaining<fastDistance&&nav.estimatedSeconds>fastSeconds,"distance and live traffic time disagree on fixture");
   Check(nav.SetPassengerPreference(false,true)&&nav.hdRoute.edges.Contains(2),"passenger fast request retains actual time weighting");Check(nav.SetPassengerPreference(false,false)&&nav.hdRoute.edges.Contains(1),"passenger short request retains actual distance weighting");
   conditions.incidents.Clear();nav.TogglePreference();Check(nav.PreferFastest&&nav.hdRoute.edges.Contains(1),"same route without a queue still permits fastest preference");nav.TogglePreference();Check(!nav.PreferFastest&&nav.hdRoute.edges.Contains(1),"same route never disables distance preference");
  } finally {UnityEngine.Object.DestroyImmediate(host);UnityEngine.Object.DestroyImmediate(taxi);}
 }
 public static void Run(){
  checks=0;Routes();
  preview=typeof(TrafficSystem).Assembly.GetType("SignalPhasePreview");Check(preview!=null,"read-only next phase preview exists");
  var p=Signals();Head(Program(p,8),2,SignalColor.Yellow,SignalColor.Red,"green to yellow");Head(Program(p,11),1,SignalColor.Red,SignalColor.Red,"yellow to red");Head(Program(p,13),1,SignalColor.Red,SignalColor.Green,"red to left-only");Head(Program(p,17),1,SignalColor.Red,SignalColor.Yellow,"left yellow");Check(!Read<bool>(Program(new RoadMapSignalProgram(),0),"Known"),"empty program has no invented change");
  var clock=new DemandSignalClock(p);clock.Restore(new DemandSignalState{stage=0,elapsed=3,green=4,demand=new[]{1,1,0,0}});var state=clock.Capture("fixture");
  Head(Adaptive(p,state,false),11,SignalColor.Yellow,SignalColor.Red,"adaptive queued green extends through actual max");Check(state.elapsed==3&&state.green==4&&state.stage==0&&state.demand[0]==1&&clock.Elapsed==3,"forecast never advances authority or mutates captured counts");
  state=new DemandSignalState{stage=0,elapsed=3,green=4,demand=new[]{1,0,0,0}};var held=Adaptive(p,state,false);Check(!Read<bool>(held,"Known")&&Read<string>(held,"CountdownText")=="","indefinitely held green invents no next color or seconds");
  state=new DemandSignalState{stage=0,elapsed=14.5f,green=14,demand=new[]{1,1,0,0}};Head(Adaptive(p,state,true),1.5f,SignalColor.Red,SignalColor.Red,"occupied crossing still permits yellow to all red");
  state=new DemandSignalState{stage=3,elapsed=12,green=10,demand=new[]{1,1,0,1}};Check(!Read<bool>(Adaptive(p,state,true),"Known"),"occupied pedestrian clearance has unknown release");Head(Adaptive(p,state,false),0,SignalColor.Green,SignalColor.Red,"released pedestrian clearance");
  var next=typeof(DrivingReadability).GetProperty("NextSignalPreview");Check(next!=null&&next.GetSetMethod(true)==null,"HUD next signal preview has no setter");
  var layout=typeof(DrivingReadability).GetMethod("FollowingManeuverBounds",BindingFlags.Public|BindingFlags.Static);Check(layout!=null,"following cue layout is exposed to real rendering");var rect=(Rect)layout.Invoke(null,new object[]{29f,411f});Check(rect.xMin>=227&&rect.xMax<=337&&rect.yMin>=415&&rect.yMax<=506&&rect.width<=104,"following maneuver is small and inside main card upper right");
  var trafficObject=new GameObject("read only signal HUD");try {var traffic=trafficObject.AddComponent<TrafficSystem>();var method=typeof(TrafficSystem).GetMethod("PreviewNextSignal");Check(method!=null,"traffic exposes read-only phase query");var junction=new TrafficJunction{name="unstarted",gameProgram=p};method.Invoke(traffic,new object[]{junction,0,1});Check(traffic.AdaptiveSignalCount==0,"HUD query never creates authoritative clocks");junction.scenarioSignal=true;Check(!Read<bool>(method.Invoke(traffic,new object[]{junction,0,1}),"Known"),"scenario has no invented future phase");}finally{UnityEngine.Object.DestroyImmediate(trafficObject);}
  Debug.Log("NAVIGATION_HUD_OK checks="+checks);
 }
}
