using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;

public static class PvpProfileReport {
 sealed class Total {public long count,bytes;public double ms,max;}
 public static void Run(){
  string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-profileInput");string file=args[at+1];
  ProfilerDriver.ClearAllFrames();ProfilerDriver.LoadProfile(file,false);
  var totals=new Dictionary<string,Total>();int frames=0;
  for(int f=ProfilerDriver.firstFrameIndex;f<=ProfilerDriver.lastFrameIndex;f++){
   using(var view=ProfilerDriver.GetRawFrameDataView(f,0)){
    if(!view.valid)continue;frames++;var parents=new Stack<KeyValuePair<int,string>>();
    for(int i=0;i<view.sampleCount;i++){
     while(parents.Count>0&&i>parents.Peek().Key)parents.Pop();
     string name=view.GetSampleName(i);Total t;if(!totals.TryGetValue(name,out t)){t=new Total();totals[name]=t;}
     double ms=view.GetSampleTimeMs(i);t.count++;t.ms+=ms;t.max=Math.Max(t.max,ms);
     if(name=="GC.Alloc"&&view.GetSampleMetadataCount(i)>0){long bytes=view.GetSampleMetadataAsLong(i,0);t.bytes+=bytes;if(parents.Count>0){string key="ALLOC:"+parents.Peek().Value;Total owner;if(!totals.TryGetValue(key,out owner)){owner=new Total();totals[key]=owner;}owner.bytes+=bytes;owner.count++;}}
     int children=view.GetSampleChildrenCountRecursive(i);if(children>0)parents.Push(new KeyValuePair<int,string>(i+children,name));
    }
   }
  }
  var report=new StringBuilder("frames="+frames+"\nname,count,totalMs,maxMs,allocatedBytes\n");
  foreach(var pair in totals.OrderByDescending(p=>p.Value.ms))report.AppendLine(pair.Key.Replace(',',';')+","+pair.Value.count+","+pair.Value.ms.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+pair.Value.max.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+pair.Value.bytes);
  File.WriteAllText(file+".csv",report.ToString());Debug.Log("PVP_PROFILE_REPORT frames="+frames+" output="+file+".csv");
 }
}
