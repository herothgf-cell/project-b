using System;
using UnityEngine;

public static class SignalCountdownTests {
 static void Check(bool value,string message){if(!value)throw new Exception("SIGNAL_COUNTDOWN: "+message);}
 public static void Run(){
  var property=typeof(DrivingReadability).GetProperty("SignalCountdownText");Check(property!=null,"countdown text is missing");
  var root=new GameObject("Countdown display test");
  try {
   var s=root.AddComponent<DriveSession>();s.traffic=root.AddComponent<TrafficSystem>();s.traffic.manualClock=true;
   var hud=root.AddComponent<DrivingReadability>();hud.session=s;
   Check((string)property.GetValue(hud)=="","no signal has no countdown");
   var j=new TrafficJunction{signalized=true,leftTurnMode=LeftTurnMode.ProtectedShared};
   typeof(DrivingReadability).GetProperty("SignalTarget").SetValue(hud,j);
   typeof(DrivingReadability).GetProperty("SignalSign").SetValue(hud,1);
   foreach(var fixture in new[]{new[]{0f,12f},new[]{.25f,12f},new[]{1f,11f},new[]{12f,3f},new[]{15f,53f}}){s.traffic.manualTime=fixture[0];Check((string)property.GetValue(hud)=="신호 변경까지 "+fixture[1].ToString("0")+"초","actual phase countdown at "+fixture[0]);Check(s.traffic.manualTime==fixture[0],"display cannot advance traffic clock");}
   j.scenarioSignal=true;Check((string)property.GetValue(hud)=="","unavailable scenario timing must not invent seconds");
   j.scenarioSignal=false;j.signalized=false;Check((string)property.GetValue(hud)=="","unsignalized approach has no countdown");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
  Debug.Log("SIGNAL_COUNTDOWN_OK phase changes / rounding / missing timing / clock unchanged");
 }
 public static void Build(){Run();BrightUIThemeTests.Run();NavigationCueTests.Run();PrototypeBuild.BuildHybrid();}
}
