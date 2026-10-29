using System;
using System.Collections.Generic;
using UnityEngine;
using Pvp.Map;
using Pvp.Network;
namespace Pvp.Rules {
public struct PvpMotionTrace {public int Tick,ContactKind,ContactId;public bool Applied,Boundary;public float Brake;public Vector3 Attempted;}
[Serializable] public sealed class PvpRuleEvent {public string Id,Kind,Evidence;public int Player,Tick,Revision;public long Penalty;public bool Deferred;}
// Authority observations only. No client reports, wall-clock timers, or prediction callbacks.
public sealed class PvpRuleAuthority {
 sealed class Episode {public string Id;public int Revision;public long Penalty;public bool Entered,GreenEntry,PedestrianClearEntry,Finished,RightDone;public int StopTicks,SeenPed,GraceUntil,LastPhase;}
 sealed class Continuous {public int Bad,Clear;public string Id;public int Revision;public long Penalty;}
 readonly PvpMapRuntime map;readonly string prefix;readonly Action<int,string,int,long,int> apply;readonly Episode[,] approaches=new Episode[2,2];readonly Continuous[,] continuous=new Continuous[2,2];readonly int[] lastTick={-1,-1};readonly Dictionary<string,int> warning=new Dictionary<string,int>();readonly Dictionary<string,Continuous> contacts=new Dictionary<string,Continuous>();readonly List<PvpRuleEvent> history=new List<PvpRuleEvent>();int serial;
 public PvpRuleAuthority(PvpMapRuntime arena,string matchId,Action<int,string,int,long,int> sink){map=arena;prefix=matchId;apply=sink;for(int p=0;p<2;p++)for(int k=0;k<2;k++)continuous[p,k]=new Continuous();}
 public static float SpeedLimit(int edge){return edge>=16?7:12;}
 public PvpRuleEvent[] Recent(){int count=Math.Min(8,history.Count);return history.GetRange(history.Count-count,count).ToArray();}
 string NewId(string kind,int player){return prefix+"-"+player+"-"+kind+"-"+(++serial);}
 void Record(int p,string id,string kind,int revision,long penalty,int tick,string evidence,bool deferred){PvpRuleEvent row=history.Find(e=>e.Id==id);if(row==null){row=new PvpRuleEvent{Id=id,Player=p};history.Add(row);if(history.Count>64)history.RemoveAt(0);}row.Kind=kind;row.Revision=revision;row.Penalty=penalty;row.Tick=tick;row.Evidence=evidence;row.Deferred=deferred;}
 void Raise(int p,Episode episode,string kind,long amount,int tick,string evidence){if(amount<=episode.Penalty)return;episode.Penalty=amount;episode.Revision++;apply(p,episode.Id,episode.Revision,amount,tick);Record(p,episode.Id,kind,episode.Revision,amount,tick,evidence,false);}
 void Raise(int p,Continuous episode,string kind,long amount,int tick,string evidence){if(episode.Id==null)episode.Id=NewId(kind,p);if(amount<=episode.Penalty)return;episode.Penalty=amount;episode.Revision++;apply(p,episode.Id,episode.Revision,amount,tick);Record(p,episode.Id,kind,episode.Revision,amount,tick,evidence,false);}
 void Defer(int p,string id,string kind,int tick,string evidence){var prior=history.Find(e=>e.Id==id);if(prior==null||prior.Deferred)Record(p,id,kind,0,0,tick,evidence,true);}
 void InvalidateEvidence(int player){for(int k=0;k<2;k++){continuous[player,k].Bad=0;continuous[player,k].Clear=0;var e=approaches[player,k];if(e!=null){if(e.StopTicks<10)e.StopTicks=0;e.SeenPed=0;e.GraceUntil=0;e.LastPhase=-1;}}foreach(var key in new List<string>(warning.Keys))if(key.StartsWith(player+":",StringComparison.Ordinal))warning[key]=0;foreach(var pair in contacts)if(pair.Key.StartsWith(player+":",StringComparison.Ordinal))pair.Value.Clear=0;}
 public void Observe(int player,PvpDriveSnapshot before,PvpDriveSnapshot after,PvpMotionTrace motion,Pvp.Traffic.PvpTrafficSnapshot traffic,bool eligible){int tick=motion.Tick;if(player<0||player>1||tick<=lastTick[player])return;bool contiguous=lastTick[player]+1==tick;lastTick[player]=tick;
  if(!eligible||!before.Connected||!motion.Applied){InvalidateEvidence(player);return;}
  if(!contiguous)InvalidateEvidence(player);
  if(motion.Boundary)Defer(player,prefix+"-boundary-"+player,"BoundaryBlocked",tick,"주행 금지 진입 차단 · 실제 침범/과실 미확정");
  float delta=Vector3.Distance(before.State.Position,after.State.Position);if(delta>2.1f){Defer(player,prefix+"-invalid-motion-"+player,"InvalidMotion",tick,"틱당 물리 범위를 벗어난 이동 · 복구/보정 제외");return;}
  for(int zone=0;zone<2;zone++)Signal(player,zone,before,after,motion,traffic,contiguous);
  bool junction=false;foreach(var node in map.Nodes)if(Vector3.Distance(after.State.Position,node)<6){junction=true;break;}
  float longitudinal=Vector3.Dot(after.State.Position-before.State.Position,map.Forward(new PvpRoadPosition(before.Edge,before.Fraction)))/.02f;
  bool parking=false;foreach(var stop in map.Stops)if(Vector3.Distance(after.State.Position,stop.Position)<14&&Mathf.Abs(after.State.Speed)<=6)parking=true;
  ContinuousRule(player,0,Mathf.Abs(after.State.Speed)>SpeedLimit(after.Edge)+1,Mathf.Abs(after.State.Speed)<SpeedLimit(after.Edge)-.5f,Mathf.Abs(after.State.Speed)>SpeedLimit(after.Edge)+5?7:3,tick,"Speeding","서버 제한속도/실제 속도25틱 지속");
  ContinuousRule(player,1,!parking&&!junction&&longitudinal < -1,parking||junction||longitudinal>=0,5,tick,"WrongWay","지정 도로축 반대 이동25틱 지속 · 정차점/회전 제외");
  Contacts(player,before,motion,traffic);
 }
 void ContinuousRule(int p,int kind,bool bad,bool clear,long amount,int tick,string name,string evidence){var e=continuous[p,kind];e.Bad=bad?e.Bad+1:0;e.Clear=clear?e.Clear+1:0;if(e.Clear>=25){e.Id=null;e.Revision=0;e.Penalty=0;}if(e.Bad>=25)Raise(p,e,name,amount,tick,evidence);}
 void Signal(int p,int zone,PvpDriveSnapshot before,PvpDriveSnapshot after,PvpMotionTrace motion,Pvp.Traffic.PvpTrafficSnapshot traffic,bool contiguous){int edge=zone==0?16:0;float line=zone==0?-8:-10,entry=-4;float a=zone==0?before.State.Position.z:before.State.Position.x,b=zone==0?after.State.Position.z:after.State.Position.x;var e=approaches[p,zone];
  bool approach=before.Edge==edge||after.Edge==edge;if(!approach){if(e!=null&&e.Entered)e.Finished=true;return;}
  if(e==null||e.Finished&&before.Edge==edge&&a<line-6){e=new Episode{Id=NewId("signal"+zone,p),LastPhase=traffic.Phase};approaches[p,zone]=e;}
  if(e.Finished)return;
  if(traffic.Phase==1&&e.LastPhase==0&&contiguous&&line-a>=0&&line-a<before.State.Speed*before.State.Speed/26+1)e.GraceUntil=motion.Tick+25;e.LastPhase=traffic.Phase;
  if(before.Edge==edge&&a>=line-4&&a<=line&&Mathf.Abs(before.State.Speed)<.35f)e.StopTicks++;else if(a<=line&&e.StopTicks<10)e.StopTicks=0;
  if(traffic.Pedestrians!=null&&traffic.Pedestrians.Length>0&&traffic.Pedestrians[0].Active)e.SeenPed++;else e.SeenPed=0;
  if(before.Edge==edge&&a<=line&&b>line&&traffic.Phase==1){if(motion.Tick<=e.GraceUntil&&motion.Brake>0)Defer(p,e.Id,"SignalTransition",motion.Tick,"적색 전환 시 안전 정지 거리 부족 · 회피 제동");else Raise(p,e,"RedStopLine",2,motion.Tick,"자기 접근 적색 정지선 swept 통과");}
  if(!e.Entered&&before.Edge==edge&&a<=entry&&b>entry){e.Entered=true;e.GreenEntry=traffic.Phase==0;e.PedestrianClearEntry=e.GreenEntry&&e.SeenPed==0;if(!e.GreenEntry){if(motion.Tick<=e.GraceUntil&&motion.Brake>0)Defer(p,e.Id,"SignalTransition",motion.Tick,"전환 직후 회피 제동 중 진입 · 판단 보류");else Raise(p,e,"RedEntry",7,motion.Tick,"같은 접근 적색 진입 · 정지선 사건 상향");}}
  if(zone==1&&!e.RightDone&&before.Edge==0&&after.Edge==8){e.RightDone=true;if(!e.GreenEntry&&traffic.Phase==1)Raise(p,e,"RightSignal",7,motion.Tick,"실제 우회전 연결로의 전용 금지 단계");else if(e.StopTicks<10)Raise(p,e,"RightStop",3,motion.Tick,"실제 우회전 · 의무 정차10틱 미충족");}
  if(motion.ContactKind==2&&zone==1){if(e.PedestrianClearEntry||motion.Brake>0||e.SeenPed<10)Defer(p,e.Id,"PedestrianContact",motion.Tick,"합법 진입/회피 제동/관측 부족 · 접촉만으로 과실 확정 금지");else if(before.State.Speed>1&&motion.Attempted.x>before.State.Position.x)Raise(p,e,"PedestrianObstruction",9,motion.Tick,"보호 보행 진행10틱 관측 후 구동 진입");}
  if(e.Entered&&(b>entry+8||a<line-6))e.Finished=true;
 }
 void Contacts(int p,PvpDriveSnapshot before,PvpMotionTrace motion,Pvp.Traffic.PvpTrafficSnapshot traffic){if(traffic.Npcs==null)return;foreach(var npc in traffic.Npcs){string key=p+":"+npc.Id;Continuous episode;if(!contacts.TryGetValue(key,out episode)){episode=new Continuous();contacts.Add(key,episode);}int seen;warning.TryGetValue(key,out seen);var forward=Quaternion.Euler(0,npc.Yaw,0)*Vector3.forward;var relative=npc.Position-before.State.Position;float ahead=Vector3.Dot(relative,forward);float side=(relative-forward*ahead).magnitude;bool rear=ahead>0&&side<2.5f&&Vector3.Dot(Quaternion.Euler(0,before.State.YawDegrees,0)*Vector3.forward,forward)>.7f;
   if(rear&&relative.magnitude>4.5f&&relative.magnitude<18&&before.State.Speed>npc.SpeedMm/1000f+1)seen++;else if(!rear||before.State.Speed<=npc.SpeedMm/1000f+1||relative.magnitude>18)seen=0;warning[key]=seen;
   episode.Clear=relative.magnitude>8?episode.Clear+1:0;if(episode.Clear>=25){episode.Id=null;episode.Revision=0;episode.Penalty=0;}
   if(motion.ContactKind!=1||motion.ContactId!=npc.Id)continue;
   if(rear&&seen>=10&&before.State.Speed>1&&motion.Brake<=0)Raise(p,episode,"NpcRearContact",5,motion.Tick,"후방 같은 방향 접근/상대속도/사전 관측이 확인된 접촉");
   else {if(episode.Id==null)episode.Id=NewId("npc-contact",p);Defer(p,episode.Id,"NpcContact",motion.Tick,"정차 피해/측면/관측 부족 · 책임 보류");}
  }}
}
}
