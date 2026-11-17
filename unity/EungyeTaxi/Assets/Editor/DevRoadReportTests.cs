using System;
using UnityEngine;
public static class DevRoadReportTests {
 public static void Run(){
  var assembly=typeof(DevRoadOverview).Assembly;var type=assembly.GetType("RoadReportGesture");
  if(type==null)throw new Exception("Road point click/drag reporting is missing");
  var gesture=Activator.CreateInstance(type);var down=type.GetMethod("Down");var move=type.GetMethod("Move");var up=type.GetMethod("Up");var box=new Rect(0,0,100,100);
  down.Invoke(gesture,new object[]{new Vector2(20,20)});if(!(bool)up.Invoke(gesture,new object[]{new Vector2(21,21),box}))throw new Exception("Click should select");
  down.Invoke(gesture,new object[]{new Vector2(20,20)});move.Invoke(gesture,new object[]{new Vector2(40,20)});move.Invoke(gesture,new object[]{new Vector2(20,20)});if((bool)up.Invoke(gesture,new object[]{new Vector2(20,20),box}))throw new Exception("Returned drag must not copy");
  down.Invoke(gesture,new object[]{new Vector2(20,20)});if((bool)up.Invoke(gesture,new object[]{new Vector2(-1,20),box}))throw new Exception("Outside release must not copy");
  var clip=assembly.GetType("RoadImageClipboard");if(clip==null)throw new Exception("Image clipboard missing");
  var bytes=(byte[])clip.GetMethod("Dib").Invoke(null,new object[]{2,1,new Color32[]{new Color32(255,0,0,255),new Color32(0,0,255,255)}});
  if(BitConverter.ToInt32(bytes,0)!=40||BitConverter.ToInt32(bytes,4)!=2||BitConverter.ToInt32(bytes,8)!=1||bytes[42]!=255||bytes[44]!=255)throw new Exception("DIB dimensions or BGRA channel order");
  Debug.Log("DEV_ROAD_REPORT_MODEL_OK checks=4");
 }
 public static void Build(){Run();DevRoadOverviewTests.Run();PrototypeBuild.BuildHybrid();}
}
