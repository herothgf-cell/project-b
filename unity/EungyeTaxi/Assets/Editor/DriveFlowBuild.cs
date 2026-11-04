using System;
using UnityEngine;
public static class DriveFlowBuild {
 static int checks;
 static void Check(bool value,string text){checks++;if(!value)throw new Exception("DRIVE_FLOW "+text);}
 public static void Build(){Run();PrototypeBuild.BuildHybrid();}
 public static void Run(){
  var root=new GameObject("Signal flow fixture");try{
   var traffic=root.AddComponent<TrafficSystem>();var j=new TrafficJunction{name="test",facility=new HybridJunctionRule{crosswalk=true}};
   for(int axis=0;axis<2;axis++)foreach(int sign in new[]{1,-1}){j.lampAxes.Add(axis);j.lampSigns.Add(sign);j.leftMovements.Add(true);}
   var p=DriveFlowPolicy.Signals(traffic,j);Check(p.Cycle==72,"protected cycle incl pedestrian is72 vs80");Check(p.SecondsToHeadChange(0,1,14)==4,"head countdown includes the active left arrow");
   for(float t=0;t<p.Cycle;t+=.25f){int left=0,straightAxes=0;bool pedestrian=p.Walk(0,1,t);for(int axis=0;axis<2;axis++){
    bool a=p.Movement(axis,1,0,t)==SignalColor.Green,b=p.Movement(axis,-1,0,t)==SignalColor.Green;Check(a==b,"opposing straights paired");if(a)straightAxes++;foreach(int sign in new[]{1,-1})if(p.Movement(axis,sign,-1,t)==SignalColor.Green)left++;
   }Check(straightAxes<=1&&left<=1&&(left==0||straightAxes==0)&&(!pedestrian||left+straightAxes==0),"no conflicting vehicle or pedestrian greens");
   float remaining=p.SecondsToChange(0,1,0,t);Check(remaining>0&&p.Movement(0,1,0,t+remaining+.001f)!=p.Movement(0,1,0,t),"countdown reaches actual colour change");}
   j.leftMovements.Clear();for(int i=0;i<4;i++)j.leftMovements.Add(false);Check(DriveFlowPolicy.Signals(traffic,j).Cycle==40,"no left phases at straight-only junction");
   Check(DriveFlowPolicy.Nickname("  홍길동<>\n_기사  ")=="홍길동_기사","nickname trims and removes controls/tags");Check(DriveFlowPolicy.Nickname("<>").Length==0,"empty nickname rejected by save gate");Check(DriveFlowPolicy.Nickname(new string('가',30)).Length==12,"nickname cap");
   var room=JsonUtility.FromJson<PvpNativeRoom>(JsonUtility.ToJson(new PvpNativeRoom{names=new[]{"기사하나","기사둘","",""}}));Check(room.names[1]=="기사둘","nickname wire JSON");
   Check(DriveFlowPolicy.SteeringRate(0)==12&&DriveFlowPolicy.SteeringRate(30)==7,"prompt low/high input response");Check(DriveMath.ManualSteerAngle(60)==DriveMath.SteerAngle(60)&&DriveMath.ManualSteerAngle(0)<=52,"retain high-speed and low-speed wheel limits");
   Debug.Log("DRIVE_FLOW_OK checks="+checks);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
