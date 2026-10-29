using System;
using System.IO;
using UnityEngine;

// Independent score journal, linked to the live campaign identity.
[Serializable] public class DrivingScoreSnapshot {public string campaign;public int sequence;public LawEvent[] events;}
public class DrivingScoreJournal : MonoBehaviour {
 public DriveSession session; public bool Loaded {get;private set;}
 string campaign="";int savedRevision=-1;bool ready;
 string PathName {get{return session.business.SavePath+".driving";}}
 public static bool Restore(DrivingScoreSnapshot data,PrototypeIds ids,LawLedger ledger){
  if(data==null||data.events==null||data.sequence<0||data.campaign!=ids.campaign)return false;
  var checkedLedger=new LawLedger();foreach(var e in data.events)checkedLedger.Record(e);
  var existing=new System.Collections.Generic.Dictionary<string,LawEvent>();foreach(var e in ledger.Events)existing[e.id]=e;
  foreach(var e in checkedLedger.Events){LawEvent old;if(!existing.TryGetValue(e.id,out old))ledger.Record(e);else ledger.RecordIncident(e);}ids.sequence=Math.Max(ids.sequence,data.sequence);
  // Recover identity from actual evidence as well as the snapshot header. A
  // stale sequence must never make the next real incident reuse a saved ID.
  foreach(var e in ledger.Events){int split=e.id.LastIndexOf(':'),sequence;if(e.id.StartsWith(ids.campaign+":",StringComparison.Ordinal)&&split>=0&&int.TryParse(e.id.Substring(split+1),out sequence))ids.sequence=Math.Max(ids.sequence,sequence);}return true;
 }
 System.Collections.IEnumerator Start(){
  yield return null;
  if(session.business.ResumeBlocked){Loaded=true;enabled=false;yield break;}
  foreach(var arg in Environment.GetCommandLineArgs())if(arg.EndsWith("Test")){Loaded=true;enabled=false;yield break;}
  if(PracticeSession.Enabled||RoadMapRuntime.Trial){Loaded=true;enabled=false;yield break;}
  // Ensure a newly created campaign has a stable identity before journaling events.
  if(!File.Exists(session.business.SavePath)&&!session.business.SaveCheckpoint()){Loaded=true;enabled=false;yield break;}
  campaign=session.law.ids.campaign;
  foreach(var path in new[]{PathName,PathName+".bak"}){
   if(!File.Exists(path))continue;
   try{var data=JsonUtility.FromJson<DrivingScoreSnapshot>(File.ReadAllText(path));if(!Restore(data,session.law.ids,session.law.ledger))continue;break;
   }catch(Exception ex){Debug.LogWarning("Driving score journal read failed: "+ex.Message);}
  }
  ready=true;Loaded=true;Save();
 }
 void LateUpdate(){if(ready&&(campaign!=session.law.ids.campaign||savedRevision!=session.law.ledger.Revision))Save();}
 void Save(){try{
  string path=PathName;Directory.CreateDirectory(Path.GetDirectoryName(path));
  File.WriteAllText(path+".tmp",JsonUtility.ToJson(new DrivingScoreSnapshot{campaign=session.law.ids.campaign,sequence=session.law.ids.sequence,events=session.law.ledger.Events},true));
  if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
  campaign=session.law.ids.campaign;savedRevision=session.law.ledger.Revision;
 }catch(Exception ex){Debug.LogWarning("Driving score journal save failed: "+ex.Message);session.business.saveStatus="운전 점수 저장 실패 · 저장 공간을 확인하세요";}}
}
