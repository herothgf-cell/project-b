using System;
using System.Linq;
[Serializable] public class RoadMapSignalGroup {public string id=Guid.NewGuid().ToString("N"),name="진행",crosswalkId="";public bool pedestrian;public int axis,sign=1,turn;public string[] laneIds=new string[0];}
[Serializable] public class RoadMapStage {public string name="단계";public float green=12,yellow=3,clearance=2;public string[] groups=new string[0];}
[Serializable] public class RoadMapSignalProgram {
 public string junctionId;public RoadMapSignalGroup[] groups=new RoadMapSignalGroup[0];public RoadMapStage[] stages=new RoadMapStage[0];
 // These are queried for each actor at physics frequency. Keep the original
 // stage/group ordering without allocating LINQ iterators, arrays or closures.
 public bool NeedsLaneId(int axis,int sign){foreach(var g in groups)if(!g.pedestrian&&g.axis==axis&&g.sign==sign&&(g.turn==0||g.turn==1)&&g.laneIds.Length>0)return true;return false;}
 public float Cycle {get{double total=0;foreach(var s in stages)total+=s.green+s.yellow+s.clearance;return (float)total;}}
 public SignalColor Color(string id,float time){float cycle=Cycle;if(cycle<=0)return SignalColor.Red;float t=(time%cycle+cycle)%cycle;foreach(var stage in stages){float length=stage.green+stage.yellow+stage.clearance;if(t<length){if(Array.IndexOf(stage.groups,id)<0)return SignalColor.Red;RoadMapSignalGroup group=null;foreach(var g in groups)if(g.id==id){group=g;break;}return t<stage.green?SignalColor.Green:t<stage.green+stage.yellow&&(group==null||!group.pedestrian)?SignalColor.Yellow:SignalColor.Red;}t-=length;}return SignalColor.Red;}
 public SignalColor Movement(int axis,int sign,int turn,float time,string laneId=null){SignalColor result=SignalColor.Red;foreach(var g in groups){if(g.pedestrian||g.axis!=axis||g.sign!=sign||g.turn!=turn||laneId!=null&&g.laneIds.Length>0&&Array.IndexOf(g.laneIds,laneId)<0)continue;var color=Color(g.id,time);if(color==SignalColor.Green)return color;if(color==SignalColor.Yellow)result=color;}return result;}
 public bool Walk(int axis,int sign,float time){foreach(var g in groups)if(g.pedestrian&&g.axis==axis&&g.sign==sign&&Color(g.id,time)==SignalColor.Green)return true;return false;}
 public static RoadMapSignalProgram Import(TrafficSystem system,TrafficJunction j){var p=new RoadMapSignalProgram{junctionId=j.name};var groups=new System.Collections.Generic.List<RoadMapSignalGroup>();var stages=new System.Collections.Generic.List<RoadMapStage>();
  int phases=4; // Imported editing uses separated approaches; timings remain explicit game settings.
  for(int n=0;n<phases;n++){var ids=new System.Collections.Generic.List<string>();for(int axis=0;axis<2;axis++)foreach(int sign in new[]{1,-1}){if((phases==2?axis:axis*2+(sign>0?0:1))!=n)continue;foreach(int turn in new[]{-1,0,1}){if(phases==2&&turn==-1)continue;var g=new RoadMapSignalGroup{name=(axis==0?"주축":"교차축")+(sign>0?"+":"−")+(turn<0?" 좌":turn>0?" 우":" 직"),axis=axis,sign=sign,turn=turn};groups.Add(g);ids.Add(g.id);}}stages.Add(new RoadMapStage{name="차량 "+(n+1),groups=ids.ToArray()});}
  if(system.HasPedestrianPhase(j)){var ids=new System.Collections.Generic.List<string>();for(int axis=0;axis<2;axis++)foreach(int sign in new[]{1,-1}){var g=new RoadMapSignalGroup{name="보행 "+axis+"/"+sign,pedestrian=true,axis=axis,sign=sign};groups.Add(g);ids.Add(g.id);}stages.Add(new RoadMapStage{name="보행",green=10,yellow=0,clearance=2,groups=ids.ToArray()});}p.groups=groups.ToArray();p.stages=stages.ToArray();return p;
 }
}
