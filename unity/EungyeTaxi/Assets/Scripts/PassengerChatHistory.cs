using System;
using System.Collections.Generic;

// Per-ride, visible-speech identity. No clock or satisfaction effects live here.
public sealed class PassengerChatHistory {
 readonly List<string> spoken=new List<string>();
 public static string Speech(string dialogue){
  var lines=new List<string>();foreach(string raw in (dialogue??"").Replace("\r\n","\n").Split('\n')){
   string line=raw.Trim();if(line.StartsWith("기사:",StringComparison.Ordinal)||line.StartsWith("기사 :",StringComparison.Ordinal))continue;
   if(line.StartsWith("승객:",StringComparison.Ordinal))line=line.Substring(3).Trim();else if(line.StartsWith("승객 :",StringComparison.Ordinal))line=line.Substring(4).Trim();
   if(line.Length>0)lines.Add(line);
  }return string.Join("\n",lines.ToArray());
 }
 public string Next(IEnumerable<string> candidates){if(spoken.Count>=16)return null;foreach(string line in candidates){string speech=Speech(line);if(speech.Length>0&&!spoken.Contains(speech))return line;}return null;}
 public void Remember(string line){string speech=Speech(line);if(speech.Length>0&&!spoken.Contains(speech)){if(spoken.Count>=16)throw new ArgumentException("Too many passenger chat lines");spoken.Add(speech);}}
 public string[] Capture(){return spoken.ToArray();}
 public void Restore(string[] seen,string lastReply){
  var next=new PassengerChatHistory();if(seen!=null){if(seen.Length>16)throw new ArgumentException("Too many passenger chat lines");foreach(string line in seen){if(string.IsNullOrEmpty(line)||line.Length>4000||Speech(line)!=line||next.spoken.Contains(line))throw new ArgumentException("Invalid passenger chat history");next.Remember(line);}}
  if(!string.IsNullOrEmpty(lastReply))next.Remember(lastReply);spoken.Clear();spoken.AddRange(next.spoken);
 }
}
