using System;
using System.IO;
using UnityEngine;
public static class CampaignCheckpointWriterTests {
 static void Check(bool value,string message){if(!value)throw new Exception("CHECKPOINT_TEST "+message);}
 public static void Run(){
  string dir=Path.Combine("artifacts","checkpoint-verified-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"save.json");
  for(int i=0;i<500;i++){CampaignCheckpointWriter.Write(path,i.ToString());Check(File.ReadAllText(path)==i.ToString(),"latest committed payload "+i);}
  Check(File.ReadAllText(path+".bak")=="498","last valid backup");
  var transient=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);var release=new System.Threading.Thread(()=>{System.Threading.Thread.Sleep(40);transient.Dispose();});release.Start();CampaignCheckpointWriter.Write(path,"retry-ok");release.Join();Check(File.ReadAllText(path)=="retry-ok","bounded retry survives short source lock");
  using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){bool rejected=false;try{CampaignCheckpointWriter.Write(path,"must-remain-pending");}catch(IOException){rejected=true;}Check(rejected&&File.ReadAllText(path)=="retry-ok","permanent lock cannot replace main or claim success");}
  string[] pending=Directory.GetFiles(dir,"*.pending-*");Check(pending.Length==1&&File.ReadAllText(pending[0])=="must-remain-pending","failed payload retained for recovery");
  CampaignCheckpointWriter.Write(path,"recovered");Check(File.ReadAllText(path)=="recovered","later retry succeeds");
  Debug.Log("CHECKPOINT_WRITER_OK 500writes transient-lock permanent-lock preserved-pending recovery");
 }
 public static void CompareBackup(){
  string dir=Path.Combine("artifacts","checkpoint-compare-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  foreach(bool unique in new[]{false,true}){string path=Path.Combine(dir,unique?"unique.json":"fixed.json");int errors=0;for(int i=0;i<200;i++){try{File.WriteAllText(path+".tmp",i.ToString());if(File.Exists(path))File.Replace(path+".tmp",path,unique?path+".bak."+i:path+".bak");else File.Move(path+".tmp",path);}catch(IOException){errors++;}}
   Debug.Log("CHECKPOINT_COMPARE uniqueBackup="+unique+" failures="+errors+" latest="+File.ReadAllText(path));}
 }
 public static void Diagnose(){
  string dir=Path.Combine("artifacts","checkpoint-diagnose-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string path=Path.Combine(dir,"save.json");
  int errors=0;for(int i=0;i<500;i++){try{File.WriteAllText(path+".tmp",i.ToString());if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);}catch(Exception e){errors++;Debug.Log("CHECKPOINT_REPRO i="+i+" type="+e.GetType().Name+" hresult="+e.HResult+" tmp="+File.Exists(path+".tmp")+" main="+File.Exists(path)+" backup="+File.Exists(path+".bak")+" "+e.Message);}}
  Debug.Log("CHECKPOINT_REPRO_END failures="+errors+" latest="+File.ReadAllText(path));
  using(var held=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){
   File.WriteAllText(path+".tmp","locked-write");try{File.Replace(path+".tmp",path,path+".bak");Debug.Log("CHECKPOINT_LOCK replacement permitted");}catch(Exception e){Debug.Log("CHECKPOINT_LOCK type="+e.GetType().Name+" hresult="+e.HResult+" main="+File.Exists(path)+" tmp="+File.Exists(path+".tmp")+" "+e.Message);}
  }
  if(typeof(PassengerRequest).Assembly.GetType("CampaignCheckpointWriter")==null)throw new Exception("Checkpoint writer has no bounded retry/pending-file preservation contract");
 }
}
