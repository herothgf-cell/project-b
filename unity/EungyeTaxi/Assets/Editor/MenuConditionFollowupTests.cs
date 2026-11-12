using System;
using UnityEngine;
public static class MenuConditionFollowupTests {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception("MENU_CONDITION: "+message);}
 static void RejectRoom(string json){bool rejected=false;try{PvpNativeLan.ParseRoom(json);}catch(ArgumentException){rejected=true;}Check(rejected,"malformed or missing condition rejected before defaults");}
 public static void Run(){var previous=PvpConditionRules.Settings;try{
  var custom=new PvpConditionSettings{fatigueMultiplier=2.5,mealSeconds=21,restSeconds=77,mealCost=2200,restCost=900};
  var room=new PvpNativeRoom{slot=1,seconds=600,condition=custom,humans=new[]{true,true,false,false},looks=new[]{PvpVehicleLook.Default,new PvpVehicleLook{model="lpg",color=1,ornament=true},PvpVehicleLook.Bot("s",2),PvpVehicleLook.Bot("s",3)}};
  var restored=PvpNativeLan.ParseRoom(JsonUtility.ToJson(room));Check(restored.condition!=null&&restored.condition.Same(custom),"host wire preserves every condition field");Check(restored.looks[1].model=="lpg"&&restored.looks[1].color==1&&restored.looks[1].ornament&&restored.looks[0].model=="starter","v17 host distributes vehicle looks");var noLooks=new PvpNativeRoom{slot=1,seconds=600,condition=custom,humans=new[]{true,true,false,false}};RejectRoom(JsonUtility.ToJson(noLooks));var badLook=JsonUtility.ToJson(room).Replace("\"lpg\"","\"tank\"");RejectRoom(badLook);restored.condition.Validate();PvpConditionRules.Configure(restored.condition);Check(PvpConditionRules.RestCost==900&&PvpConditionRules.MealSeconds==21,"host snapshot applied");
  Check(!custom.Same(new PvpConditionSettings()),"changed host rules detected");RejectRoom("{\"slot\":1}");RejectRoom("{\"condition\":null}");RejectRoom("{\"condition\":{}}");RejectRoom("{\"condition\":{\"fatigueMultiplier\":1.25,\"mealSeconds\":15,\"restSeconds\":45,\"mealCost\":1000}}");RejectRoom(JsonUtility.ToJson(room).Replace("\"restCost\":900","\"restCost\":0.5"));Check(PvpNativeLan.Protocol=="native-city-lan-v18","wire version isolated");
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080),new Vector2(800,600)}){var r=DriveHUD.HelpBounds(size.x,size.y);Check(r.x>=0&&r.y>=0&&r.xMax<=size.x&&r.yMax<=size.y,"help contained");var settings=DrivingSettingsPanel.Bounds(size.x,size.y);Check(settings.x>=0&&settings.y>=0&&settings.xMax<=size.x&&settings.yMax<=size.y,"settings contained");}
  var go=new GameObject("help copy check");try{var hud=go.AddComponent<DriveHUD>();Check(hud.DrivingHelpText.Contains("숫자 1~n")&&!hud.DrivingHelpText.Contains("Tab"),"help dialogue keys without removed route shortcut");}finally{UnityEngine.Object.DestroyImmediate(go);}
  Debug.Log("MENU_CONDITION_FOLLOWUP_OK checks="+checks);
 }finally{PvpConditionRules.Configure(previous);}}
}
