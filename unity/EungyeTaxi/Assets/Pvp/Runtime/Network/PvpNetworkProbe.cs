using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
namespace Pvp.Network {
public sealed class PvpNetworkProbe:MonoBehaviour {
 IEnumerator Start(){
  yield return null;var args=Environment.GetCommandLineArgs();
  try{string role=Value(args,"-pvpNetworkRole","client");var session=gameObject.AddComponent<PvpNetworkSession>();session.Probe=true;session.ProbeSeconds=Number(args,"-pvpProbeSeconds",role=="host"?18:14);session.ExpectedRejection=Value(args,"-pvpExpectedRejection",null);session.ProbeExternalDrop=Array.IndexOf(args,"-pvpBlackoutCheck")>=0;session.ProbeReconnect=Array.IndexOf(args,"-pvpReconnectCheck")>=0;session.ProbeTamperTicket=Array.IndexOf(args,"-pvpTamperTicket")>=0;session.ProbeLeave=Array.IndexOf(args,"-pvpLeaveCheck")>=0;session.ProbeRetryDelay=Number(args,"-pvpReconnectDelay",1);session.ReconnectGraceSeconds=Number(args,"-pvpReconnectGrace",30);session.Begin(role=="host",Value(args,"-pvpAddress","127.0.0.1"),ushort.Parse(Value(args,"-pvpPort","7777")),Array.IndexOf(args,"-pvpBadMap")>=0);}
  catch(Exception e){Debug.LogException(e);Application.Quit(1);yield break;}
  string capture=Value(args,"-pvpCapturePath",null);if(capture!=null){yield return new WaitForSecondsRealtime(10);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(capture);yield return null;yield return null;}
 }
 static float Number(string[] args,string key,float fallback){return float.Parse(Value(args,key,fallback.ToString(CultureInfo.InvariantCulture)),CultureInfo.InvariantCulture);}
 static string Value(string[] args,string key,string fallback){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
}}