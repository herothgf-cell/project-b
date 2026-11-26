using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// 2026-11-26 play feedback: at J7973_3744 the northbound green showed 2 s, then 16 s, counted down, then 22 s.
// Logs the demand-actuated clock and the HUD countdown each half second for a few cycles.
public sealed class SignalCountdownProbe : MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-signalCountdownCheck")>=0)new GameObject("Signal countdown probe").AddComponent<SignalCountdownProbe>();}
 IEnumerator Start(){
  DriveSession s=null;float until=Time.realtimeSinceStartup+120;while((s=FindObjectOfType<DriveSession>())==null||!s.StartupReady||s.traffic==null||s.traffic.junctions.Count==0){if(Time.realtimeSinceStartup>until){Finish("no session",new StringBuilder());yield break;}yield return null;}
  var report=new StringBuilder();var t=s.traffic;string id=ContentTuningRuntime.Argument("-signalJunction");if(string.IsNullOrEmpty(id))id="J7973_3744";
  var j=t.junctions.Find(x=>x.name==id);if(j==null){Finish("junction "+id+" missing",report);yield break;}
  int axis=Mathf.Abs(Vector3.Dot(t.AxisDirection(j,0),Vector3.forward))>=Mathf.Abs(Vector3.Dot(t.AxisDirection(j,1),Vector3.forward))?0:1;int sign=t.Sign(Vector3.forward,axis,j);
  report.AppendLine("junction="+id+" axis="+axis+" sign="+sign+" adaptive="+(t.DemandState(j)!=null)+" stages="+(j.gameProgram!=null?j.gameProgram.stages.Length:0));
  if(j.gameProgram!=null)for(int i=0;i<j.gameProgram.stages.Length;i++){var st=j.gameProgram.stages[i];report.AppendLine(" stage "+i+" green="+st.green+" yellow="+st.yellow+" clear="+st.clearance+" groups="+string.Join(",",st.groups));}
  // The stage serving this straight movement: a rise only counts while a car waits there (in play the taxi does).
  int own=-1;if(j.gameProgram!=null)for(int i=0;i<j.gameProgram.stages.Length&&own<0;i++)foreach(string gid in j.gameProgram.stages[i].groups)foreach(var g in j.gameProgram.groups)if(g.id==gid&&!g.pedestrian&&g.axis==axis&&g.sign==sign&&g.turn==0)own=i;
  report.AppendLine("own stage="+own);bool queued=false;
  Time.timeScale=4;float prev=-1;int jumps=0,greenJumps=0;SignalColor prevColor=SignalColor.Red;
  for(int n=0;n<900;n++){
   var st=t.DemandState(j);var color=t.MovementSignal(j,axis,0,sign);float secs=t.SecondsToSignalChange(j,axis,sign);var next=t.PreviewLight(j,axis,sign,0);
   string line="t="+Time.time.ToString("0.0")+" color="+color+" hud="+(next.Known?next.CountdownText:"—")+" secs="+secs.ToString("0.0")+(st!=null?" stage="+st.stage+" el="+st.elapsed.ToString("0.0")+" green="+st.green.ToString("0.0")+" demand="+string.Join("/",st.demand):"");
   // A rise only counts while the watched light keeps its colour (a new colour starts a new countdown).
   if(next.Known&&prev>=0&&color==prevColor&&next.Seconds>prev+1.5f&&(color==SignalColor.Green||queued)){jumps++;if(color==SignalColor.Green)greenJumps++;line+="  <-- JUMP from "+prev.ToString("0.0");}
   prev=next.Known?next.Seconds:-1;prevColor=color;queued=st!=null&&own>=0&&st.demand[own]>0;report.AppendLine(line);
   yield return new WaitForSeconds(.5f);
  }
  report.AppendLine("upward jumps="+jumps+" during green="+greenJumps);Finish(jumps==0?null:"countdown rose "+jumps+" times ("+greenJumps+" on green)",report);
 }
 void Finish(string error,StringBuilder report){
  string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"signal-countdown.txt"),(error??"done")+"\n"+report);
  Debug.Log("SIGNAL_COUNTDOWN_DONE "+(error??"ok"));Application.Quit(error==null?0:2);
 }
}
