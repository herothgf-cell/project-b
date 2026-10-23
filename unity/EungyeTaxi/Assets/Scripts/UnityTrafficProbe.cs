using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
[Serializable] public sealed class UnityTrafficExpected {public string lane,sign,mark;}
// Controlled integration assertions, not manual driving or a physical traffic simulation.
public sealed class UnityTrafficProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-unityTrafficTest")>=0)new GameObject("Unity traffic edit validation").AddComponent<UnityTrafficProbe>();}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);bool ok=true;int count=0;Action<bool,string> check=(pass,label)=>{ok&=pass;count++;Debug.Log((pass?"PASS ":"FAIL ")+label);};try{
  var e=JsonUtility.FromJson<UnityTrafficExpected>(File.ReadAllText("artifacts/unity-traffic-player/expected.json"));var s=FindObjectOfType<DriveSession>();var map=s.world.map;var doc=map.editorDocument;check(RoadMapRuntime.Trial&&!s.business.enabledCampaign,"trial isolates business save");check(doc!=null&&doc.facilities.Any(f=>f.id==e.sign),"player loads Unity edited sign");var edge=map.hdEdges.First(l=>l.id==e.lane);check(edge.next.All(i=>map.hdEdges[i].turn!=-1||map.hdEdges[i].change!=0),"player shared graph removes forbidden left turn");check(RoadMapRules.SpeedLimit(map,e.lane,1)==17,"player driving rule uses edited speed");check(doc.facilities.Any(f=>f.id==e.mark),"player uses authored marking");check(s.world.GetComponentsInChildren<Transform>().Any(t=>t.name=="사용자 시설 "+e.sign)||FindObjectsOfType<Transform>().Any(t=>t.name=="사용자 시설 "+e.sign),"player creates actual edited sign mesh");
 }catch(Exception ex){ok=false;Debug.LogException(ex);}Directory.CreateDirectory("artifacts/unity-traffic-player");File.WriteAllText("artifacts/unity-traffic-player/player.txt","success="+ok+" checks="+count);Debug.Log("UNITY_TRAFFIC_PLAYER success="+ok+" checks="+count);Application.Quit(ok?0:1);}
}
