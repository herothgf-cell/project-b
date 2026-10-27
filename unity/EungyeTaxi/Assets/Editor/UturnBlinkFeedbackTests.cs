using System;
using System.Linq;
using UnityEngine;
public static class UturnBlinkFeedbackTests {
 public static void RunFinal(){Run();LatestFeedbackTests.RunFinal();Debug.Log("UTURN_BLINK_FINAL_EDITOR_OK");}
 static void Check(bool ok,string why){if(!ok)throw new Exception("UTURN_BLINK_FAIL "+why);Debug.Log("UTURN_BLINK_PASS "+why);}
 public static void Run(){
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var actual=package.document.facilities.Single(f=>f.junctionId=="J8066_6364"&&f.kind=="sign"&&UturnMovement.IsCondition(f.type));
  var owner=new GameObject("real authored uturn fixture");try{
   var traffic=owner.AddComponent<TrafficSystem>();var j=new TrafficJunction{name=actual.junctionId,direction=Vector3.forward};traffic.junctions.Add(j);
   var head=new GameObject("matching signal approach");head.transform.SetParent(owner.transform);head.transform.position=new Vector3(810,0,660);var housing=WorldBuilder.Box("Signal housing",head.transform.position+Vector3.up*4.55f,new Vector3(2.9f,.85f,.35f),WorldBuilder.Mat("fixture signal",Color.black),head.transform,false);var lamp=housing.GetComponent<Renderer>();j.lamps.Add(new[]{lamp});j.lampAxes.Add(actual.axis);j.lampSigns.Add(actual.sign);
   // Deliberately add a closer opposite-approach lamp first: identity, not proximity.
   var wrong=new GameObject("wrong direction signal");wrong.transform.SetParent(owner.transform);var wrongLamp=WorldBuilder.Box("Signal housing",actual.Position+Vector3.up*4.55f,Vector3.one,WorldBuilder.Mat("wrong",Color.black),wrong.transform,false).GetComponent<Renderer>();j.lamps.Insert(0,new[]{wrongLamp});j.lampAxes.Insert(0,actual.axis);j.lampSigns.Insert(0,-actual.sign);
   var doc=new RoadMapDocument{facilities=new[]{actual}};RoadMapFacilities.Build(doc,owner.transform,traffic);var sign=owner.GetComponentsInChildren<TextMesh>().Single(t=>t.text.Contains("유턴"));
   Check(sign.transform.IsChildOf(head.transform),"authored J8066 U-turn board is attached to exact matching approach signal");Check(!sign.transform.IsChildOf(wrong.transform),"opposite approach cannot steal board");Check(Vector2.Distance(new Vector2(sign.transform.position.x,sign.transform.position.z),new Vector2(housing.transform.position.x,housing.transform.position.z))<.1f,"board is vertically above housing");Check(sign.transform.position.y>6.8f,"upper reservation keeps other sign readable");Check(!sign.transform.parent.GetComponentsInChildren<Transform>().Any(t=>t.name=="지주"),"no standalone road-centre pole remains");
   Check(Quaternion.Angle(sign.transform.parent.rotation,head.transform.rotation)<.01f,"board faces matching approach");
   Check(sign.transform.position.y-.6f*Mathf.Abs(actual.scale)>housing.transform.position.y+1.15f+.575f,"clearance above generic upper board");
   var preview=new GameObject("preview fallback");try{RoadMapFacilities.Build(doc,preview.transform,null);var previewSign=preview.GetComponentsInChildren<TextMesh>().Single();Check(previewSign.transform.parent.GetComponentsInChildren<Transform>().Any(t=>t.name=="지주"),"preview without traffic retains authored sign and pole");Check(Vector2.Distance(new Vector2(previewSign.transform.position.x,previewSign.transform.position.z),new Vector2(actual.Position.x,actual.Position.z))<.1f,"preview retains authored horizontal position");}finally{UnityEngine.Object.DestroyImmediate(preview);}
   Debug.Log("UTURN_SIGNAL_MOUNT_OK");
  }finally{UnityEngine.Object.DestroyImmediate(owner);}
 }
}
