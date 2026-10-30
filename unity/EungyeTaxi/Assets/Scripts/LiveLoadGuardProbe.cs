using System;
using System.Collections;
using System.IO;
using UnityEngine;
public sealed class LiveLoadGuardProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-liveInvalidTest")>=0)new GameObject("Corrupt live save guard").AddComponent<LiveLoadGuardProbe>();}
 IEnumerator Start(){
  DriveSession s;while(!(s=FindObjectOfType<DriveSession>())||!s.StartupReady)yield return null;yield return null;yield return null;var b=s.business;bool ok=false;
  try{
   string before=File.ReadAllText(b.SavePath),backup=File.ReadAllText(b.SavePath+".bak");
   if(!b.ResumeBlocked||b.CanAccept||!b.preparing||Time.timeScale!=0)throw new Exception("invalid live save did not block departure");
   b.Depart();
   if(!b.restartPrompt)throw new Exception("blocked departure silently ignored instead of opening recovery confirmation");
   if(!b.preparing||b.SaveCheckpoint()||File.ReadAllText(b.SavePath)!=before||File.ReadAllText(b.SavePath+".bak")!=backup)throw new Exception("failed resume overwrites existing save");
   b.ConfirmRestart(false);
   if(b.restartPrompt||!b.ResumeBlocked||File.ReadAllText(b.SavePath)!=before||File.ReadAllText(b.SavePath+".bak")!=backup)throw new Exception("cancel recovery changed preserved files");
   if(!b.RequestRestart())throw new Exception("recovery confirmation unavailable after cancel");
   using(var locked=File.Open(b.SavePath,FileMode.Open,FileAccess.Read,FileShare.None)){
    if(b.ConfirmRestart(true)||!b.ResumeBlocked)throw new Exception("recovery continued when rejected original could not be archived");
   }
   if(File.ReadAllText(b.SavePath)!=before||File.ReadAllText(b.SavePath+".bak")!=backup)throw new Exception("failed recovery archive changed originals");
   if(!b.restartPrompt&&!b.RequestRestart())throw new Exception("recovery retry confirmation unavailable");
   if(!b.ConfirmRestart(true)||b.ResumeBlocked||b.ledger.Day!=1)throw new Exception("explicit restart cannot recover blocked campaign");
   var archives=Directory.GetDirectories(Path.GetDirectoryName(b.SavePath),Path.GetFileName(b.SavePath)+".recovery-*");bool preserved=false;
   foreach(var archive in archives)if(File.Exists(Path.Combine(archive,"primary.json"))&&File.Exists(Path.Combine(archive,"backup.json"))&&File.ReadAllText(Path.Combine(archive,"primary.json"))==before&&File.ReadAllText(Path.Combine(archive,"backup.json"))==backup)preserved=true;
   if(!preserved)throw new Exception("explicit recovery did not preserve exact rejected primary and backup in separate archive");
   var data=JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(b.SavePath));if(data.version!=3||data.hasLive||data.days.active.elapsed!=0)throw new Exception("new campaign persisted stale live payload");
   ok=true;Debug.Log("LIVE_LOAD_GUARD_OK checks=9");
  }catch(Exception e){Debug.LogException(e);}
  File.WriteAllText("artifacts/live-load-guard.txt",ok?"PASS blocked recovery confirmation, cancel, primary+backup preservation, archived originals, explicit reset and clean v3 boundary":"FAIL");Application.Quit(ok?0:1);
 }
}
