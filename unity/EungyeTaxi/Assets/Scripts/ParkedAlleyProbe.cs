using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Alley parking, player acceptance (Eungye single, 2026-11-24). Cars stand on 은행로144번길 at one kerb, clear of
// junctions, stops and crossings; no ordinary car is routed through the alley; the taxi is moved along each
// lane with the side shift a driver would take and touches nothing and gets no centre-line or wrong-way record.
public sealed class ParkedAlleyProbe : MonoBehaviour {
 const string Flag="-alleyParkingCheck";
 DriveSession s;int checks;readonly StringBuilder report=new StringBuilder();string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Alley parking acceptance").AddComponent<ParkedAlleyProbe>();}}
 void Check(bool ok,string reason){checks++;report.AppendLine((ok?"PASS ":"FAIL ")+reason);Debug.Log("PARKED_ALLEY_PLAYER "+(ok?"PASS ":"FAIL ")+reason);if(!ok)throw new Exception(reason);}
 void Info(string line){report.AppendLine("INFO "+line);Debug.Log("PARKED_ALLEY_PLAYER INFO "+line);}
 string Money(){var l=s.business.ledger;return l.Account.Expense(l.Day,"accident")+"|"+l.Account.Expense(l.Day,"repair")+"|"+l.Account.Cash+"|"+s.law.ledger.Count+"|"+s.law.ledger.TotalScore+"|"+l.Care.Damage.Durability+"|"+s.manners.collisionEvents;}
 IEnumerator Start(){
  float limit=Time.realtimeSinceStartup+180;
  while(!(s=FindObjectsOfType<DriveSession>().FirstOrDefault(x=>!x.IsSimulationBot))||!s.StartupReady){if(!FindObjectOfType<DriveSession>()){var mode=FindObjectOfType<UnifiedModeBootstrap>();if(mode)mode.StartSingle();}if(Time.realtimeSinceStartup>limit){Finish("startup timeout");yield break;}yield return null;}
  output=ContentTuningRuntime.Argument("-alleyParkingOutput");if(string.IsNullOrEmpty(output))output=Path.GetFullPath("artifacts/alley-parking");Directory.CreateDirectory(output);
  yield return null;yield return null;string error=null;var pa=s.parkedAlley;var graph=s.jobs.nav.hd;var map=s.world.map;
  try{
   s.business.preparing=false;s.paused=false;Time.timeScale=1;s.jobs.ClearForDay();
   Check(pa,"the alley parking component is attached");
   Check(ParkedAlleyRules.Roads.Any(r=>r.name=="은행로144번길"&&r.excludeTraffic&&r.evidence.Contains("게임 연출")),"the data file names 은행로144번길 as a game staging");
   Check(pa.cars.Count>=3,"parked cars stand on the alley ("+pa.cars.Count+", skipped "+pa.skipped.Count+")");
   foreach(var line in pa.skipped.GroupBy(x=>x.Substring(x.IndexOf(' ')+1).Split('(')[0].Trim()).Select(g=>g.Key+" "+g.Count()))Info("skipped: "+line);
   foreach(var c in pa.cars)Info("car "+c.lane+"@"+c.at.ToString("0")+" "+c.position.ToString("F1")+" inner "+c.inner.ToString("0.00")+" centre line "+c.centreLine.ToString("0.00")+" shift "+ParkedAlleyRules.Shift(c.inner).ToString("0.00"));
   Check(pa.cars.All(c=>c.road=="은행로144번길"),"only on 은행로144번길");
   Check(pa.cars.All(c=>map.stops.All(st=>Vector3.Distance(c.position,st.Kerb)>=ParkedAlleyRules.ClearMetres)),"15 m clear of every passenger stop");
   Check(pa.cars.All(c=>map.targets.All(j=>Vector3.Distance(c.position,j.V())>=ParkedAlleyRules.ClearMetres+8)),"clear of every junction");
   Check(pa.cars.All(c=>map.osmCrossings==null||map.osmCrossings.All(x=>Vector3.Distance(c.position,new Vector3(x.x,0,x.z))>=ParkedAlleyRules.ClearMetres-1)),"clear of pedestrian crossings");
   Check(pa.cars.All(c=>{var r=TrafficSystem.Right(c.direction);foreach(float x in new[]{-.9f,.9f})foreach(float z in new[]{-2.2f,2.2f})if(!s.roadEnvironment.OnRoad(c.position+r*x+c.direction*z,0))return false;return true;}),"every car stands on the paved road");
   Check(pa.cars.All(c=>ParkedAlleyRules.Passable(c.inner,c.centreLine)),"the taxi can pass each on its own side of the centre line");
  }catch(Exception e){error=e.ToString();}
  if(error!=null){Finish(error);yield break;}
  // Traffic: no ordinary car's route uses the alley, now and after 20 s.
  for(int round=0;round<2;round++){
   if(round==1)yield return new WaitForSeconds(20);
   int inAlley=0,visible=0;foreach(var car in s.traffic.cars){if(!car||!car.Visible)continue;visible++;if(car.route?.graphPath!=null&&car.route.graphPath.edges.Any(e=>graph.edges[e].roadName=="은행로144번길"))inAlley++;}
   try{Check(inAlley==0,"no ordinary car is routed through the alley ("+(round==0?"start":"after 20 s")+", "+visible+" visible cars)");}catch(Exception e){error=e.ToString();}
   if(error!=null){Finish(error);yield break;}
  }
  // The taxi passes along each lane with the shift needed beside each car.
  var wait=new WaitForFixedUpdate();
  foreach(var lane in pa.cars.GroupBy(c=>c.lane)){
   int edge=Array.FindIndex(graph.edges,e=>e.id==lane.Key);var path=graph.paths[edge];var cars=lane.ToList();
   Func<float,float> shift=d=>{float best=0;foreach(var c in cars){float gap=Mathf.Abs(c.at-d);float need=ParkedAlleyRules.Shift(c.inner);float w=gap<3?1:gap<9?1-(gap-3)/6f:0;best=Mathf.Max(best,need*w);}return best;};
   float start=Mathf.Max(1,cars.Min(c=>c.at)-14),end=Mathf.Min(path.length-1,cars.Max(c=>c.at)+10);
   var t=s.taxi;var dir0=path.Tangent(start);var p0=path.Position(start);t.body.velocity=Vector3.zero;t.body.position=p0+Vector3.up*.65f;t.body.rotation=Quaternion.LookRotation(dir0);t.transform.SetPositionAndRotation(t.body.position,t.body.rotation);Physics.SyncTransforms();
   yield return new WaitForSeconds(1.5f);string before=Money();float maxShift=0;
   for(float d=start;d<end;d+=6*Time.fixedDeltaTime){var tan=path.Tangent(d);tan.y=0;tan.Normalize();float sh=shift(d);maxShift=Mathf.Max(maxShift,sh);var pos=path.Position(d)-TrafficSystem.Right(tan)*sh+Vector3.up*.65f;
    t.body.MovePosition(pos);t.body.MoveRotation(Quaternion.LookRotation(tan));var v=tan*6;v.y=t.body.velocity.y;t.body.velocity=v;yield return wait;}
   t.body.velocity=Vector3.zero;yield return new WaitForSeconds(1f);
   {var ev=s.law.ledger.Events;for(int k=0;k<ev.Length;k++)Info("law event "+ev[k].family+" / "+ev[k].action+" / "+ev[k].reason);}
   if(lane.Key==pa.cars[0].lane)ScreenCapture.CaptureScreenshot(Path.Combine(output,"alley.png"));
   try{Check(Money()==before&&!s.accident.Locked,"passing "+cars.Count+" cars on "+lane.Key+" (max shift "+maxShift.ToString("0.00")+" m): no contact, law record or score change ("+before+" -> "+Money()+")");}catch(Exception e){error=e.ToString();}
   if(error!=null){Finish(error);yield break;}
  }
  // Evening drive count: 1.5x per group.
  try{int day=pa.cars.Count;pa.Build(ParkedAlleyRules.NightMultiplier);Info("evening rebuild "+day+" -> "+pa.cars.Count);Check(pa.cars.Count>day,"more cars at night (evening drive)");pa.Build(1);Check(pa.cars.Count==day,"day count restored");}catch(Exception e){error=e.ToString();}
  Finish(error);
 }
 void Finish(string error){
  if(error!=null)Debug.LogError(error);
  if(output!=null)File.WriteAllText(Path.Combine(output,"report.txt"),report.ToString()+(error??""));
  Debug.Log("PARKED_ALLEY_PLAYER_"+(error==null?"OK":"FAIL")+" checks="+checks+" "+(error??""));Application.Quit(error==null?0:1);
 }
}
