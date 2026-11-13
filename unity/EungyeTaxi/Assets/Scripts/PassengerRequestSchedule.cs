using System;
using System.Collections.Generic;

[Serializable] public sealed class PassengerRequestSlot {
 public RequestKind kind;public float threshold;public bool presented,closed;
 public PassengerRequestSlot Copy(){return (PassengerRequestSlot)MemberwiseClone();}
}
[Serializable] public sealed class PassengerRequestScheduleSnapshot {
 public int version=1,active=-1,stopSerial,lastResponseStop=-1;
 public float movingSinceResponse,progress;public bool wasStopped,awaitingResponse;
 public PassengerRequestSlot[] slots=new PassengerRequestSlot[0];
}
// Candidate identity fixes the draw; progress and available dialogue release it.
public sealed class PassengerRequestSchedule {
 PassengerRequestScheduleSnapshot state=new PassengerRequestScheduleSnapshot();
 public static bool Route(RequestKind k){return k==RequestKind.LargeRoad||k==RequestKind.ShortRoad||k==RequestKind.RouteAdvice||k==RequestKind.SafeStop||k==RequestKind.DestinationChange||k==RequestKind.ViaStop;}
 static bool Talk(RequestKind k){return k==RequestKind.Empathy||k==RequestKind.Interview||k==RequestKind.LocalChat||k==RequestKind.Music;}
 public static bool Compatible(RequestKind a,RequestKind b){return a!=b&&!(Route(a)&&Route(b))&&!(a==RequestKind.Quiet&&Talk(b)||b==RequestKind.Quiet&&Talk(a));}
 static uint Hash(string text){uint h=2166136261;foreach(char c in text){h^=c;h*=16777619;}h^=h>>16;h*=2246822519;h^=h>>13;return h;}
 public static PassengerRequestSchedule Create(string seed,RequestKind[] eligible){
  var result=new PassengerRequestSchedule();uint roll=Hash((seed??"")+":request-count")%100;int count=roll<ContentTuning.Int("schedule.count0",20)?0:roll<ContentTuning.Int("schedule.count0",20)+ContentTuning.Int("schedule.count1",60)?1:2;
  var pool=new List<RequestKind>();foreach(var k in eligible)if(k!=RequestKind.LostItem&&Enum.IsDefined(typeof(RequestKind),k)&&!pool.Contains(k)&&ContentTuning.Flag("request."+k+".enabled",true)&&ContentTuning.Int("request."+k+".weight",1)>0)pool.Add(k);
  var selected=new List<PassengerRequestSlot>();
  for(int i=0;i<count&&pool.Count>0;i++){
   int total=0;foreach(var candidate in pool)total+=ContentTuning.Int("request."+candidate+".weight",1);int pick=(int)(Hash(seed+":request-kind:"+i)%(uint)total),at=0;while(pick>=ContentTuning.Int("request."+pool[at]+".weight",1)){pick-=ContentTuning.Int("request."+pool[at]+".weight",1);at++;}var k=pool[at];
   float threshold=i==0&&k!=RequestKind.ViaStop&&Hash(seed+":request-boarding")%2==0?0:(float)ContentTuning.Number("schedule.progressMin",.25)+(Hash(seed+":request-progress:"+i)%(uint)(Math.Round((ContentTuning.Number("schedule.progressMax",.65)-ContentTuning.Number("schedule.progressMin",.25))*1000)+1))/1000f;
   threshold=Math.Min(threshold,(float)ContentTuning.Number("schedule.progressMax",.65));if(i>0)threshold=Math.Max(threshold,selected[i-1].threshold);
   selected.Add(new PassengerRequestSlot{kind=k,threshold=threshold});pool.RemoveAll(v=>!Compatible(k,v));
  }
  result.state.slots=selected.ToArray();return result;
 }
 public bool BindRepresentative(RequestKind kind,RequestKind[] eligible,float threshold=-1){
  if(state.slots.Length==0||Array.IndexOf(eligible,kind)<0||!ContentTuning.Flag("request."+kind+".enabled",true)||ContentTuning.Int("request."+kind+".weight",1)<=0)return false;
  state.slots[0].kind=kind;if(threshold>=0)state.slots[0].threshold=Math.Min((float)ContentTuning.Number("schedule.progressMax",.65),threshold);
  for(int i=1;i<state.slots.Length;i++)if(!Compatible(kind,state.slots[i].kind)){
   bool found=false;foreach(var candidate in eligible)if(Compatible(kind,candidate)&&ContentTuning.Flag("request."+candidate+".enabled",true)&&ContentTuning.Int("request."+candidate+".weight",1)>0){state.slots[i].kind=candidate;found=true;break;}
   if(!found){Array.Resize(ref state.slots,i);break;}
  }return true;
 }
 public int Tick(float movingSeconds,float progress,bool dialogueAvailable,bool arrivalLocked,bool longSignalWait=false){
  if(float.IsNaN(movingSeconds)||float.IsInfinity(movingSeconds)||movingSeconds<0||float.IsNaN(progress)||float.IsInfinity(progress))throw new ArgumentException("Invalid request clock");
  state.movingSinceResponse+=movingSeconds;state.progress=Math.Max(state.progress,Math.Min(1,Math.Max(0,progress)));
  // Retain the legacy stop fields in snapshots for old saves and LAN payloads.
  if(dialogueAvailable&&!state.wasStopped)state.stopSerial++;state.wasStopped=dialogueAvailable;
  if(arrivalLocked||state.progress>=ContentTuning.Number("schedule.cutoff",.85)){foreach(var s in state.slots)if(!s.presented)s.closed=true;return -1;}
  if(!dialogueAvailable||state.awaitingResponse)return -1;
  for(int i=0;i<state.slots.Length;i++){
   var s=state.slots[i];if(s.presented||s.closed)continue;
   if(!longSignalWait&&(state.progress<s.threshold||s.threshold>0&&state.movingSinceResponse<ContentTuning.Number("schedule.movingSeconds",20)))return -1;
   s.presented=true;state.active=i;state.awaitingResponse=true;return i;
  }return -1;
 }
 public void Responded(){if(!state.awaitingResponse)return;state.awaitingResponse=false;state.movingSinceResponse=0;state.lastResponseStop=state.stopSerial;}
 public PassengerRequestScheduleSnapshot Capture(){var s=(PassengerRequestScheduleSnapshot)new PassengerRequestScheduleSnapshot{version=1,active=state.active,stopSerial=state.stopSerial,lastResponseStop=state.lastResponseStop,movingSinceResponse=state.movingSinceResponse,progress=state.progress,wasStopped=state.wasStopped,awaitingResponse=state.awaitingResponse};s.slots=Array.ConvertAll(state.slots,x=>x.Copy());return s;}
 public static PassengerRequestSchedule Restore(PassengerRequestScheduleSnapshot s){
  if(s==null||s.version!=1||s.slots==null||s.slots.Length>2||s.active< -1||s.active>=s.slots.Length||s.movingSinceResponse<0||float.IsNaN(s.movingSinceResponse)||float.IsInfinity(s.movingSinceResponse)||s.progress<0||s.progress>1||float.IsNaN(s.progress))throw new ArgumentException("Invalid request schedule");
  foreach(var slot in s.slots)if(slot==null||!Enum.IsDefined(typeof(RequestKind),slot.kind)||slot.threshold<0||slot.threshold>ContentTuning.Number("schedule.progressMax",.65)+.00001||float.IsNaN(slot.threshold))throw new ArgumentException("Invalid request slot");
  var result=new PassengerRequestSchedule();result.state=s;result.state=result.Capture();return result;
 }
}
