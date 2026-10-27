using System;
using System.Reflection;
using UnityEngine;

public static class PlaytestPresentationTests {
 static void Check(bool ok,string message){Debug.Log((ok?"PRESENTATION_PASS ":"PRESENTATION_FAIL ")+message);if(!ok)throw new Exception(message);}
 public static void RunChoice(){
  var root=new GameObject("removed queue popup regression");
  try{
   var s=root.AddComponent<DriveSession>();s.law=root.AddComponent<DrivingLaw>();s.law.session=s;s.business=root.AddComponent<BusinessProgress>();s.business.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());
   var zones=root.AddComponent<ServiceLawZones>();zones.session=s;zones.scenarios.Add(new PrototypeScenario{id="PS08"});zones.scenarios.Add(new PrototypeScenario{id="PS09"});
   var choice=root.AddComponent<ScenarioChoiceSituation>();choice.session=s;
   choice.Begin(zones.scenarios[0]);Check(!choice.CanOfferSaved(),"PS08 never offers removed driving-plan popup");
   choice.RestoreLive(new ScenarioChoiceSnapshot{courseId="PS08",open=true,previousScale=1});Check(!choice.Open,"old saved PS08 popup cannot reopen");
   choice.Begin(zones.scenarios[1]);Check(choice.CanOfferSaved(),"PS09 pedestrian observation offer remains available");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=1;}
 }
 public static void RunSigns(){
  var root=new GameObject("signal sign regression");
  try{
   var s=root.AddComponent<DriveSession>();s.traffic=root.AddComponent<TrafficSystem>();s.world=root.AddComponent<WorldBuilder>();s.world.map=new MapData{width=500,depth=500};s.roadEnvironment=root.AddComponent<RoadEnvironment>();s.roadEnvironment.map=s.world.map;
   var j=new TrafficJunction{name="fixture",center=new Vector3(100,0,100),direction=Vector3.forward,leftTurnMode=LeftTurnMode.ProtectedShared};j.lampAxes.Add(0);j.lampSigns.Add(1);s.traffic.junctions.Add(j);
   var housing=WorldBuilder.Box("Signal housing",new Vector3(102,4.55f,125),new Vector3(2.2f,.85f,.35f),WorldBuilder.Mat("test housing",Color.black),s.traffic.transform,false);
   var lamps=new Renderer[3];for(int i=0;i<3;i++){var lamp=WorldBuilder.Box("test lamp",new Vector3(102+i*.5f,4.55f,124.8f),Vector3.one*.2f,WorldBuilder.Mat("test lamp",Color.red),housing.transform,false);lamps[i]=lamp.GetComponent<Renderer>();}j.lamps.Add(lamps);
   var signs=root.AddComponent<RoadSignage>();signs.session=s;typeof(RoadSignage).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(signs,null);
   j.leftTurnMode=LeftTurnMode.Permissive;j.scenarioSignal=true;j.observationAxis=0;j.observationSign=1;signs.SendMessage("LateUpdate",SendMessageOptions.DontRequireReceiver);
   var captions=root.GetComponentsInChildren<TextMesh>();bool above=false;foreach(var t in captions)if(t.text.Contains("비보호")&&t.transform.position.y>5&&Vector2.Distance(new Vector2(t.transform.position.x,t.transform.position.z),new Vector2(lamps[0].transform.position.x,lamps[0].transform.position.z))<3)above=true;
   Check(above,"permissive sign is mounted above matching vehicle signal not roadside");
  }finally{UnityEngine.Object.DestroyImmediate(root);Time.timeScale=1;}
 }
 public static void Run(){RunChoice();RunSigns();Debug.Log("PLAYTEST_PRESENTATION_OK");}
 public static void RunRedBatch(){int failures=0;foreach(Action test in new Action[]{RunChoice,RunSigns,PlaytestBalanceTests.Run})try{test();}catch(Exception e){failures++;Debug.LogException(e);}if(failures>0)throw new Exception("Feedback expected-red groups: "+failures);}
 public static void RunFocused(){int failures=0;foreach(Action test in new Action[]{Run,ServiceFacilityFeedbackTests.Run,ServiceFacilityFeedbackTests.RunPickupMap,ServiceFacilityFeedbackTests.RunPickupReturn,ServicePickupTests.Run,PlaytestBalanceTests.Run,PlaytestNavigationAudit.Run})try{test();}catch(Exception e){failures++;Debug.LogException(e);}if(failures>0)throw new Exception("Feedback focused failures: "+failures);Debug.Log("PLAYTEST_FOCUSED_OK");}
 public static void RunFinal(){RunFocused();StabilizationRegressionTests.RunFinal();StabilizationRegressionTests.RunFollowup();Debug.Log("PLAYTEST_FINAL_EDITOR_OK");}
 public static void RunReviewFindings(){int failures=0;foreach(Action test in new Action[]{ServiceFacilityFeedbackTests.RunPickupReturn,PlaytestBalanceTests.RunFinalFindings})try{test();}catch(Exception e){failures++;Debug.LogException(e);}if(failures>0)throw new Exception("Review findings groups: "+failures);Debug.Log("FEEDBACK_REVIEW_FINDINGS_OK");}
 public static void BuildFinal(){RunFinal();PrototypeBuild.BuildHybrid();}
 public static void BuildPresentation(){Run();PrototypeBuild.BuildHybrid();}
}
