using System;
using UnityEngine;
// Project-owned switch; the sample-derived policy is isolated in MitTrafficFollowing.
public sealed class MitTrafficOptions {
 public const string PreferenceKey="Traffic.MitFollowing.v1";
 public bool Enabled {get;private set;}
 public bool TestRun {get;private set;}
 public string Label {get{return "앞차 추종 보완: "+(Enabled?"ON":"OFF");}}
 public MitTrafficOptions(){
  var args=Environment.GetCommandLineArgs();
  foreach(var arg in args)if(arg.EndsWith("Test",StringComparison.OrdinalIgnoreCase))TestRun=true;
  Enabled=TestRun?false:PlayerPrefs.GetInt(PreferenceKey,1)!=0;
  if(Array.IndexOf(args,"-mitTrafficOn")>=0)Enabled=true;
  if(Array.IndexOf(args,"-mitTrafficOff")>=0)Enabled=false;
 }
 public void SetEnabled(bool value,bool persist=true){
  Enabled=value;
  if(persist&&!TestRun){PlayerPrefs.SetInt(PreferenceKey,value?1:0);PlayerPrefs.Save();}
 }
}
