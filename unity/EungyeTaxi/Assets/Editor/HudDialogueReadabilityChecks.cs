using System;
using System.Reflection;
using UnityEngine;

public static class HudDialogueReadabilityChecks {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception("HUD_DIALOGUE: "+reason);}
 public static void Run(){
  checks=0;var type=typeof(TrafficSystem).Assembly.GetType("SignalHeadPresentation");
  Check(type!=null,"signal head presentation distinguishes explicit three/four apertures");
  var count=type.GetMethod("LampCount");
  Check((int)count.Invoke(null,new object[]{"3구"})==3,"explicit three lamp head");
  Check((int)count.Invoke(null,new object[]{"4구"})==4,"explicit four lamp head");
  Check((int)count.Invoke(null,new object[]{""})==4,"legacy head retains four physical apertures");
  var resolve=type.GetMethod("Resolve");
  var j=new TrafficJunction{name="test",signalized=true,editorProgram=new RoadMapSignalProgram()};
  var f=new RoadMapFacility{id="three",kind="vehicleSignal",type="3구",junctionId="test",axis=0,sign=1,evidence=new RoadMapEvidence{appearanceConfirmed=true,url="fixture evidence"}};
  var opposite=new RoadMapFacility{id="four",kind="vehicleSignal",type="4구",junctionId="test",axis=0,sign=-1};
  var map=new MapData{editorDocument=new RoadMapDocument{facilities=new[]{f,opposite}}};
  object head=resolve.Invoke(null,new object[]{map,j,0,1});
  Check((int)head.GetType().GetField("lamps").GetValue(head)==3,"matching approach uses authored three, not opposite four");
  Check((bool)head.GetType().GetField("verified").GetValue(head),"explicit appearance evidence is distinguished");
  head=resolve.Invoke(null,new object[]{map,j,0,-1});Check((int)head.GetType().GetField("lamps").GetValue(head)==4&&!(bool)head.GetType().GetField("verified").GetValue(head),"game placement does not claim real-world confirmation");
  j.editorProgram=new RoadMapSignalProgram();head=resolve.Invoke(null,new object[]{map,j,1,1});Check((int)head.GetType().GetField("lamps").GetValue(head)==0,"missing authored direction is unknown, never invent four");
  j.signalized=false;head=resolve.Invoke(null,new object[]{map,j,0,1});Check((int)head.GetType().GetField("lamps").GetValue(head)==0,"unsignalized has no head");
  var root=new GameObject("physical head fixture");try{
   RoadMapFacilities.Build(map.editorDocument,root.transform,null);
   var three=root.transform.Find("사용자 시설 three").GetComponent<RoadMapLamp>();var four=root.transform.Find("사용자 시설 four").GetComponent<RoadMapLamp>();
   Check(three.red&&three.yellow&&three.green&&!three.leftLens&&three.leftArrow.Length==0,"three physical lenses match HUD without invented left arrow");
   Check(four.red&&four.yellow&&four.green&&four.leftLens,"four retains unlit aperture independently of left permission");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  var parts=PassengerDialogueUI.EffectParts("급가감속 감점 +1점 · 성공 종료 만족도 +6점 · 실패 종료 만족도 -4점 · 불필요한 급가감속 판정 유지");
  Check(parts[0]=="급가감속 감점 +1점 · 불필요한 급가감속 판정 유지"&&parts[1]=="성공 종료 만족도 +6점"&&parts[2]=="실패 종료 만족도 -4점","effect clauses retain original numeric meaning and guard");
  Check(PassengerDialogueUI.EffectParts("사용자 문구: 성공 시 변화 없음")[0]=="사용자 문구: 성공 시 변화 없음","custom prose is not rewritten");
  var liveParts=PassengerDialogueUI.EffectParts("급가감속 감점 +1점, 성공 종료 만족도 +6점, 실패 종료 만족도 -4점 · 불필요한 급가감속 판정 유지");
  Check(liveParts[1]=="성공 종료 만족도 +6점"&&liveParts[2]=="실패 종료 만족도 -4점","actual comma-separated ChoiceEffects output has separate outcome badges");
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080)}){
   float s=UiReadability.HudScale(size.x,size.y);var panel=PassengerDialogueUI.PanelBounds(size.x,size.y,s,320);
   Check(panel.xMin>=390&&panel.xMax<=1280,"dialogue leaves vehicle and evaluation corners clear "+size);
  }
  Debug.Log("HUD_DIALOGUE_CHECKS_OK checks="+checks);
 }
 public static void Build(){Run();BrightUIThemeTests.Run();HudRevisionBuild.Run();NavigationHudChecks.Run();ApproachSignalHeadTests.Run();NativeLeftSignalRegressionTests.Run();ChoiceEffectsRuntimeChecks.Run();PrototypeBuild.BuildHybrid();}
}
