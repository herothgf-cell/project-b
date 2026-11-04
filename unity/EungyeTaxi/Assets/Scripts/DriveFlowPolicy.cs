using System;
using System.Text;
using UnityEngine;
public static class DriveFlowPolicy {
 public static float SteeringRate(float speed){return Mathf.Lerp(12,7,Mathf.Clamp01(Mathf.Abs(speed)/14));}
 public static float AdvanceSteering(float current,float target,float speed,float seconds){
  float rate=SteeringRate(speed);if(Mathf.Abs(target)<.01f)rate*=.65f;else if(current*target<0)rate*=1.4f;
  return Mathf.MoveTowards(current,target,Mathf.Max(0,seconds)*rate);
 }
 public static string Nickname(string value){var b=new StringBuilder();foreach(char c in value??""){if(b.Length>=12)break;if(char.IsLetterOrDigit(c)||c==' '||c=='_'||c=='-')b.Append(c);}return b.ToString().Trim();}
 public static string LocalName {get{if(PvpNativeLan.ProbeMode){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,"-pvpNativeLanRole");return i>=0&&i+1<a.Length?"검증_"+a[i+1]:"검증";}return Nickname(PlayerPrefs.GetString("pvp.nickname",""));}}
 public static bool SaveName(string value){string name=Nickname(value);if(name.Length==0)return false;PlayerPrefs.SetString("pvp.nickname",name);PlayerPrefs.Save();return true;}
 public static RoadMapSignalProgram Signals(TrafficSystem system,TrafficJunction j){
  var groups=new System.Collections.Generic.List<RoadMapSignalGroup>();var stages=new System.Collections.Generic.List<RoadMapStage>();
  for(int axis=0;axis<2;axis++){
   var ids=new System.Collections.Generic.List<string>();foreach(int sign in new[]{1,-1})foreach(int turn in new[]{0,1}){var g=new RoadMapSignalGroup{axis=axis,sign=sign,turn=turn};groups.Add(g);ids.Add(g.id);}
   stages.Add(new RoadMapStage{name="양방향 직진",green=10,yellow=2,clearance=2,groups=ids.ToArray()});
   foreach(int sign in new[]{1,-1})if(system.HasLeftMovement(j,axis,sign)){var g=new RoadMapSignalGroup{axis=axis,sign=sign,turn=-1};groups.Add(g);stages.Add(new RoadMapStage{name="보호 좌회전",green=4,yellow=2,clearance=2,groups=new[]{g.id}});}
  }
  if(system.HasPedestrianPhase(j)){var ids=new System.Collections.Generic.List<string>();for(int axis=0;axis<2;axis++)foreach(int sign in new[]{1,-1}){var g=new RoadMapSignalGroup{pedestrian=true,axis=axis,sign=sign};groups.Add(g);ids.Add(g.id);}stages.Add(new RoadMapStage{name="보행",green=10,yellow=0,clearance=2,groups=ids.ToArray()});}
  return new RoadMapSignalProgram{junctionId=j.name,groups=groups.ToArray(),stages=stages.ToArray()};
 }
}
