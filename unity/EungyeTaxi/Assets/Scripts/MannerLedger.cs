using System;
using System.Collections.Generic;
public class MannerLedger {
 public int Score {get;private set;}
 public int MotionDebt {get;private set;}
 public int PermanentLoss {get;private set;}
 public int StableRecovered {get;private set;}
 public MannerLedger(){Score=100;}
 readonly Dictionary<string,float> last=new Dictionary<string,float>();
 public bool Penalize(string key,int points,float now,float cooldown){float at;if(last.TryGetValue(key,out at)&&now-at<cooldown)return false;last[key]=now;int lost=Math.Min(Score,Math.Max(0,points));Score-=lost;if(key=="accel"||key=="brake")MotionDebt+=lost;else PermanentLoss+=Math.Max(0,points);return true;}
 public int RecoverStable(int points){int gain=Math.Min(Math.Max(0,points),Math.Min(6-StableRecovered,Math.Min(MotionDebt,Math.Min(100-Score,Math.Max(0,100-PermanentLoss-Score)))));Score+=gain;MotionDebt-=gain;StableRecovered+=gain;return gain;}
 public void Reset(){Score=100;MotionDebt=PermanentLoss=StableRecovered=0;last.Clear();}
 public void RestoreScore(int score){RestoreRecovery(score,0,100-score,0);}
 public void RestoreRecovery(int score,int motionDebt,int permanentLoss,int recovered){if(score<0||score>100||motionDebt<0||permanentLoss<0||recovered<0||recovered>6||motionDebt>100)throw new ArgumentException("Invalid manner recovery");Score=score;MotionDebt=motionDebt;PermanentLoss=permanentLoss;StableRecovered=recovered;last.Clear();}
 public static int Payment(int fare,int score,int required){return score>=required?fare:(int)((long)fare*7/10);}
 public static int MotionPenalty(bool comfortAccepted){return comfortAccepted?4:2;}
}
[Serializable] public sealed class StableDrivingSnapshot {public float distance,seconds;public string[] cells;public float[] cellDistances,cellSeconds;}
public sealed class StableDrivingProgress {
 const float CellSize=10f,CellDistanceLimit=14f,CellSecondsLimit=20f;
 struct Cell {public float distance,seconds;}
 readonly Dictionary<string,Cell> cells=new Dictionary<string,Cell>();
 float previousX,previousZ,distance,seconds;bool positioned;
 public void Rebase(float x,float z){previousX=x;previousZ=z;positioned=true;}
 public void Clear(){cells.Clear();distance=seconds=0;positioned=false;}
 public void BreakSegment(){distance=seconds=0;}
 public bool Observe(float x,float z,float speed,float elapsed,bool gentle){if(!positioned){Rebase(x,z);return false;}float dx=x-previousX,dz=z-previousZ;previousX=x;previousZ=z;if(!gentle){BreakSegment();return false;}if(speed<.5f||elapsed<=0||float.IsNaN(elapsed)||float.IsInfinity(elapsed))return false;float moved=(float)Math.Sqrt(dx*dx+dz*dz);if(moved<.001f||moved>Math.Max(5f,speed*elapsed*2f+2f))return false;
  string key=(int)Math.Floor(x/CellSize)+":"+(int)Math.Floor(z/CellSize);Cell cell;cells.TryGetValue(key,out cell);float credited=Math.Min(moved,Math.Max(0,CellDistanceLimit-cell.distance));float creditedSeconds=Math.Min(elapsed,Math.Max(0,CellSecondsLimit-cell.seconds));if(credited<=0||creditedSeconds<=0)return false;cell.distance+=credited;cell.seconds+=creditedSeconds;cells[key]=cell;distance+=credited;seconds+=creditedSeconds;if(distance<500f&&seconds<60f)return false;BreakSegment();return true;}
 public StableDrivingSnapshot Capture(){var s=new StableDrivingSnapshot{distance=distance,seconds=seconds,cells=new string[cells.Count],cellDistances=new float[cells.Count],cellSeconds=new float[cells.Count]};int i=0;foreach(var pair in cells){s.cells[i]=pair.Key;s.cellDistances[i]=pair.Value.distance;s.cellSeconds[i]=pair.Value.seconds;i++;}return s;}
 public void Restore(StableDrivingSnapshot s){if(s==null||s.cells==null||s.cellDistances==null||s.cellSeconds==null||s.cells.Length!=s.cellDistances.Length||s.cells.Length!=s.cellSeconds.Length||s.distance<0||s.seconds<0||float.IsNaN(s.distance)||float.IsNaN(s.seconds))throw new ArgumentException("Invalid stable driving progress");cells.Clear();for(int i=0;i<s.cells.Length;i++){if(string.IsNullOrEmpty(s.cells[i])||s.cellDistances[i]<0||s.cellDistances[i]>CellDistanceLimit||s.cellSeconds[i]<0||s.cellSeconds[i]>CellSecondsLimit||cells.ContainsKey(s.cells[i]))throw new ArgumentException("Invalid stable driving cell");cells.Add(s.cells[i],new Cell{distance=s.cellDistances[i],seconds=s.cellSeconds[i]});}distance=s.distance;seconds=s.seconds;positioned=false;}
}
