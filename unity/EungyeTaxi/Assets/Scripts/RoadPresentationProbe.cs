using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
// Isolated presentationTest: fixed cameras and geometry inspection, not driving/field verification.
public class RoadPresentationProbe : MonoBehaviour {
 public DriveSession session;
 readonly List<string> report=new List<string>();int checks;
 void Check(bool value,string why){checks++;report.Add((value?"PASS ":"FAIL ")+why);if(!value)throw new Exception(why);}
 IEnumerator Start(){
  while(!session||!session.StartupReady)yield return null;yield return null;yield return null;
  bool ok=true;string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../artifacts"));Directory.CreateDirectory(folder);
  try{
   Check(session.business.SavePath.StartsWith(folder+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"presentation uses isolated artifact save");
   session.business.enabled=false;session.jobs.enabled=false;session.taxi.enabled=false;session.taxi.body.isKinematic=true;session.drivingCamera.enabled=false;
   foreach(var text in session.traffic.GetComponentsInChildren<TextMesh>(true))Check(!text.text.Contains("횡단보도"),"no fabricated crosswalk text board: "+text.name);
   int approaches=0,protectedCount=0,permissiveCount=0;
   foreach(var j in session.traffic.junctions)for(int i=0;i<j.lamps.Count;i++){
    var lamps=j.lamps[i];if(lamps==null||lamps.Length==0||!lamps[0])continue;approaches++;
    var sign=lamps[0].transform.parent.Find("비보호 표지");bool expected=j.leftTurnMode==LeftTurnMode.Permissive&&(!j.scenarioSignal||j.lampAxes[i]==j.observationAxis&&j.lampSigns[i]==j.observationSign);
    Check((sign&&sign.gameObject.activeInHierarchy)==expected,"matching actual signal condition "+j.name+"/"+i);
    if(expected)permissiveCount++;else protectedCount++;
    if(sign){var back=sign.Find("회색 뒷판");Check(back&&back.localScale.x<1&&back.localScale.y<1.2f,"compact condition plate");Check(sign.GetComponentsInChildren<TextMesh>(true).All(t=>t.text=="비보호"),"condition uses arrow plus short actual legend");}
   }
   Check(approaches>0&&protectedCount>0,"actual signal approaches inspected");report.Add("approaches="+approaches+" protected="+protectedCount+" permissive="+permissiveCount);
   var junc=session.traffic.junctions.FirstOrDefault(j=>j.name=="J5750_2576"&&j.lamps.Count>0)??session.traffic.junctions.First(j=>j.lamps.Count>0);
   var forward=session.traffic.AxisDirection(junc,junc.lampAxes[0])*junc.lampSigns[0];var camera=Camera.main;
   Capture(camera,junc.center-forward*24+Vector3.up*5,junc.center+forward*7+Vector3.up*3.3f,Path.Combine(folder,"signage-junction-view.png"),1900,998);
   Capture(camera,junc.center+forward*24+Vector3.up*5,junc.center-forward*7+Vector3.up*3.3f,Path.Combine(folder,"signage-junction-opposite.png"),1900,998);
   var actual=FindObjectsOfType<Transform>().FirstOrDefault(t=>t.name=="비보호 표지"&&t.gameObject.activeInHierarchy);
   GameObject fixture=null;if(!actual){fixture=RoadSignage.CreatePermissive(session.traffic.transform,Vector3.up*60);actual=fixture.transform;report.Add("condition detail uses rendering fixture: actual map has no permissive approach");}
   var centre=actual.position;
   Capture(camera,centre-actual.forward*2,centre,Path.Combine(folder,"signage-condition-front.png"),1280,720);
   Capture(camera,centre+actual.forward*2,centre,Path.Combine(folder,"signage-condition-back.png"),1280,720);
   if(fixture)Destroy(fixture);
  }catch(Exception e){ok=false;report.Add(e.ToString());Debug.LogException(e);}
  report.Add("device="+SystemInfo.graphicsDeviceName+" mode=fixed-camera-and-geometry manualDriving=false");File.WriteAllText(Path.Combine(folder,"road-signage-player.txt"),(ok?"PASS":"FAIL")+" checks="+checks+"\n"+string.Join("\n",report));Debug.Log("ROAD_SIGNAGE_PLAYER_"+(ok?"OK":"FAILED")+" checks="+checks);Application.Quit(ok?0:1);
 }
 void Capture(Camera camera,Vector3 eye,Vector3 target,string path,int width,int height){
  camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));camera.fieldOfView=50;
  var texture=new RenderTexture(width,height,24);var active=RenderTexture.active;var prior=camera.targetTexture;camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
  var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());camera.targetTexture=prior;RenderTexture.active=active;Destroy(pixels);texture.Release();Destroy(texture);
  report.Add("CAPTURE "+Path.GetFileName(path)+" "+width+"x"+height+" eye="+eye+" target="+target);
 }
}
