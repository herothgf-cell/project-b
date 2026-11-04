using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ArrivalReadabilityChecks {
 static int checks;
 static void Check(bool ok,string text){checks++;if(!ok)throw new Exception("ARRIVAL_READABILITY: "+text);Debug.Log("ARRIVAL_PASS "+text);}
 static List<Vector3> Display(RoadNavigation nav){var p=typeof(RoadNavigation).GetProperty("DisplayPath");return p==null?nav.path:(List<Vector3>)p.GetValue(nav,null);}
 public static void Navigation(){
  var root=new GameObject("retained navigation regression");try{
   var car=new GameObject("fixture taxi");car.transform.SetParent(root.transform);car.transform.position=new Vector3(100,0,120);
   var nav=root.AddComponent<RoadNavigation>();var map=new MapData{width=500,depth=500,mapId="arrival-test",roads=new MapRoad[0],hdEdges=new[]{
    new HdEdge{id="a",fromNode="a0",toNode="a1",next=new int[0],points=new[]{new MapPoint{x=100,z=100},new MapPoint{x=100,z=200}}},
    new HdEdge{id="b",fromNode="b0",toNode="b1",next=new int[0],points=new[]{new MapPoint{x=130,z=100},new MapPoint{x=130,z=200}}}}};
   nav.Build(map,car.transform);nav.SetHdDestination(0,90);Check(nav.path.Count>=2&&nav.hdRoute!=null,"fixture has a valid route before deviation");
   var last=Display(nav).ToArray();car.transform.position=new Vector3(130,0,120);nav.Refresh();
   Check(Display(nav).Count==last.Length&&Display(nav)[0]==last[0],"deviation without a new connected route retains the last visible route");
   Check(nav.path.Count==0&&nav.hdRoute==null,"retained display is not an executable assistance route");
   var steering=new HdSteering{nav=nav};Check(!steering.Arm(root.AddComponent<TaxiController>()),"H cannot arm from retained display geometry");
   Check(nav.estimatedSeconds==0&&nav.remaining==0,"unavailable route does not advertise old distance or ETA");
   car.transform.position=new Vector3(100,0,125);nav.Refresh();Check(nav.path.Count>=2&&Display(nav)[0]!=last[0],"returning to connected lane replaces old display");
   car.transform.position=new Vector3(100,0,190);nav.Refresh();Check(!nav.RetainingPreviousRoute,"single-point destination arrival is not a failed reroute");
   car.transform.position=new Vector3(130,0,120);nav.Refresh();nav.SetHdDestination(0,95);Check(Display(nav).Count==0,"changing destination clears obsolete retained geometry");
   nav.Clear();Check(Display(nav).Count==0&&!nav.active,"cancel clears every route display");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 public static void Guidance(){
  var type=typeof(RoadNavigation).Assembly.GetType("StopArrivalGuidance");Check(type!=null,"stop guidance exists independently of navigation geometry");
  var stop=new MapStop{name="fixture",x=100,z=100,dx=0,dz=1,width=7};var method=type.GetMethod("Evaluate");
  Func<Vector3,Vector3,float,bool,object> hint=(p,d,v,pickup)=>method.Invoke(null,new object[]{stop,p,d,v,pickup});
  Func<object,string,string> text=(value,field)=>(string)value.GetType().GetField(field).GetValue(value);
  Func<object,bool> ready=value=>(bool)value.GetType().GetField("Ready").GetValue(value);
  var at=hint(stop.Kerb,Vector3.forward,0,true);Check(ready(at)&&text(at,"Action").Contains("F")&&text(at,"Action").Contains("승차"),"at pickup gives explicit F action without a route");
  at=hint(stop.Kerb,Vector3.forward,0,false);Check(ready(at)&&text(at,"Action").Contains("하차"),"at drop gives explicit F disembark action without a route");
  at=hint(stop.Kerb,Vector3.forward,3,true);Check(!ready(at)&&text(at,"Detail").Contains("감속"),"speed mismatch explains deceleration");
  at=hint(stop.Kerb,Vector3.back,0,true);Check(!ready(at)&&text(at,"Detail").Contains("방향"),"wrong heading explains the direction");
  at=hint(stop.Kerb+Vector3.right*3,Vector3.forward,0,true);Check(!ready(at)&&text(at,"Detail").Contains("정차 칸"),"wrong lateral position explains the target bay");
  Check(ready(hint(stop.Kerb+Vector3.forward*4.49f,Vector3.forward,.55f,true))==stop.CanStop(stop.Kerb+Vector3.forward*4.49f,Vector3.forward,.55f),"guidance shares actual stop boundaries");
 }
 public static void Sizing(){
  var type=typeof(RoadNavigation).Assembly.GetType("UiReadability");Check(type!=null,"readable UI size policy exists");
  var scale=type.GetMethod("HudScale");var font=type.GetMethod("FontFor");
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080),new Vector2(2560,1440)}){
   float s=(float)scale.Invoke(null,new object[]{size.x,size.y});int f=(int)font.Invoke(null,new object[]{14,s});Check(f*s>=15.9f,"important small text at least16px at "+size.x);
  }
  Check((float)scale.Invoke(null,new object[]{2560f,1440f})>1,"high resolution grows the complete cockpit");
  var bounds=new Rect(0,0,900,600);var inside=new Vector2(450,300);Check(MapPresentation.PlayerAnchor(bounds,inside)==inside,"player emphasis retains actual in-view position");
  Check(bounds.Contains(MapPresentation.PlayerAnchor(bounds,new Vector2(-900,1200))),"offscreen player has an in-view direction marker");
  Check(UiReadability.ScreenRect(new Rect(10,20,100,50),1.6f)==new Rect(16,32,160,80),"logical map clicks convert to physical screen coordinates");
 }
 static void Property(PassengerJobs jobs,string name,object value){typeof(PassengerJobs).GetProperty(name).GetSetMethod(true).Invoke(jobs,new[]{value});}
 public static void Preparation(){
  var root=new GameObject("arrival action state regression");try{
   var s=root.AddComponent<DriveSession>();s.taxi=root.AddComponent<TaxiController>();s.taxi.body=root.AddComponent<Rigidbody>();s.taxi.body.isKinematic=true;s.jobs=root.AddComponent<PassengerJobs>();var j=s.jobs;j.session=s;
   var stop=new MapStop{name="fixture",x=100,z=100,dx=0,dz=1,width=7};j.stops=new[]{stop};j.current=new JobOffer{pickup=0,drop=0};s.taxi.transform.position=stop.Kerb;s.taxi.body.position=stop.Kerb;
   j.trip.Accept(5000,60);var behavior=PassengerBehaviorState.Create("guidance-prep",PassengerType.Luggage);Property(j,"Behavior",behavior);
   Check(!StopArrivalGuidance.For(s).Ready&&StopArrivalGuidance.For(s).Action.Contains("준비"),"pickup luggage choice cannot falsely advertise ready-to-board");
   behavior.Choice=0;behavior.PreparationRemaining=5;Check(!StopArrivalGuidance.For(s).Ready,"pickup loading countdown cannot advertise a second F boarding");
   behavior.PreparationRemaining=0;Check(StopArrivalGuidance.For(s).Ready,"completed pickup preparation restores F action");
   j.trip.Board();typeof(PassengerJobs).GetField("passengerVia",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(j,0);var errand=PassengerErrandState.Agree("ride","via","drop");errand.Start(true);Property(j,"Errand",errand);
   Check(!StopArrivalGuidance.For(s).Ready&&StopArrivalGuidance.For(s).Action.Contains("대기"),"active20-second errand shows wait rather than another F prompt");
   var situation=new PassengerSituationState{ReSecureRemaining=5};Property(j,"Situation",situation);Check(StopArrivalGuidance.For(s).Action.Contains("준비"),"resecure work has priority over via arrival actions");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 public static void Run(){checks=0;int errors=0;foreach(Action test in new Action[]{Navigation,Guidance,Sizing,Preparation})try{test();}catch(Exception e){errors++;Debug.LogException(e);}if(errors>0)throw new Exception("ARRIVAL_READABILITY groups="+errors);Debug.Log("ARRIVAL_READABILITY_OK checks="+checks);}
 public static void Build(){Run();NavigationCueTests.Run();MapReadabilityTests.Run();PrototypeBuild.BuildHybrid();}
}
