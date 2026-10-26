using System;
using System.IO;
using System.Text;
using System.Threading;

public static class CampaignCheckpointWriter {
 static readonly object gate=new object();
 // A fresh backup target avoids Windows/Mono ReplaceFile failures removing a
 // recently replaced .bak. Main is never deleted first; failed payloads remain.
 public static string Write(string path,string data){lock(gate){
  string pending=path+".pending-"+Guid.NewGuid().ToString("N"),previous=path+".previous-"+Guid.NewGuid().ToString("N");
  byte[] bytes=new UTF8Encoding(false).GetBytes(data);
  using(var stream=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
  Exception failure=null;bool committed=false;
  for(int attempt=0;attempt<5;attempt++){
   try{if(File.Exists(path))File.Replace(pending,path,previous);else File.Move(pending,path);committed=true;break;}
   catch(IOException e){failure=e;}
   catch(UnauthorizedAccessException e){failure=e;}
   // Replace can report failure after consuming the source: verify exact bytes.
   if(!File.Exists(pending)){try{committed=File.ReadAllText(path)==data;}catch(IOException){}if(committed)break;}
   if(attempt<4)Thread.Sleep(15*(1<<attempt));
  }
  if(!committed)throw new IOException("Checkpoint commit failed; previous save preserved, pending payload: "+pending,failure);
  if(!File.Exists(previous))return "";
  for(int attempt=0;attempt<3;attempt++)try{File.Copy(previous,path+".bak",true);File.Delete(previous);return "";}catch(IOException){if(attempt<2)Thread.Sleep(15);}catch(UnauthorizedAccessException){break;}
  return "이전 정상 저장 별도 보존: "+previous;
 }}
}
