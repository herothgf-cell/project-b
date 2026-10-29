using System;
using System.Collections;
using UnityEngine;
namespace Pvp.Network {
public sealed class PvpNetworkProbe:MonoBehaviour {
 IEnumerator Start(){yield return null;try{var args=Environment.GetCommandLineArgs();string role=Value(args,"-pvpNetworkRole","client");int seconds=int.Parse(Value(args,"-pvpProbeSeconds",role=="host"?"18":"14"));var session=gameObject.AddComponent<PvpNetworkSession>();session.Probe=true;session.ProbeSeconds=seconds;session.ExpectedRejection=Value(args,"-pvpExpectedRejection",null);session.Begin(role=="host",Value(args,"-pvpAddress","127.0.0.1"),ushort.Parse(Value(args,"-pvpPort","7777")),Array.IndexOf(args,"-pvpBadMap")>=0);}catch(Exception e){Debug.LogException(e);Application.Quit(1);}}
 static string Value(string[] args,string key,string fallback){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
}}