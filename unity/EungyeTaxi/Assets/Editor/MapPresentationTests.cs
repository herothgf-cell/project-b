using System;
using System.Reflection;
using UnityEngine;

public static class MapPresentationTests {
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static MapPoint P(float x,float z){return new MapPoint{x=x,z=z};}
 static MapPoint[] Box(float x,float z,float w,float h){return new[]{P(x,z),P(x+w,z),P(x+w,z+h),P(x,z+h)};}
 public static void Run(){
  var type=typeof(DriveHUD).Assembly.GetType("MapPresentation");Require(type!=null,"MapPresentation renderer is not implemented");
  var map=new MapData{width=100,depth=100,roads=new[]{new MapRoad{width=12,points=new[]{P(-50,50),P(150,50)}}},areas=new[]{new MapArea{kind="water",points=Box(5,5,20,20)},new MapArea{kind="park",points=Box(65,5,20,20)},new MapArea{kind="park",points=Box(-200,90,210,40)}},buildings=new[]{new MapBuilding{points=Box(60,70,20,15)}}};
  string original=JsonUtility.ToJson(map);var texture=(Texture2D)type.GetMethod("Create",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{map,200});
  try {
   var water=texture.GetPixel(30,30);var park=texture.GetPixel(150,30);var building=texture.GetPixel(140,155);var land=texture.GetPixel(100,150);
   Require(water.b>water.r+.12f,"Water must be blue at its actual footprint");
   Require(park.g>park.r+.07f,"Park must be green at its actual footprint");
   Require(Vector3.Distance(new Vector3(building.r,building.g,building.b),new Vector3(land.r,land.g,land.b))>.07f,"Actual building footprint must differ from land");
   Require(texture.GetPixel(100,108).r>.95f,"Road width must cover its real 12m corridor");
   Require(texture.GetPixel(100,125).r<.95f,"Road must not spill beyond its corridor");
   Require(texture.GetPixel(5,195).g>texture.GetPixel(5,195).r+.07f,"Partially external polygon must clip correctly");
   Require(JsonUtility.ToJson(map)==original,"Map presentation must never mutate input geometry");
  } finally {UnityEngine.Object.DestroyImmediate(texture);}
  var empty=new MapData{width=100,depth=100};texture=(Texture2D)type.GetMethod("Create").Invoke(null,new object[]{empty,64});Require(texture.width==64&&texture.GetPixel(32,32).a==1,"Empty map must still have an opaque bounded background");UnityEngine.Object.DestroyImmediate(texture);
  var rectangular=new MapData{width=200,depth=100,roads=new[]{new MapRoad{width=12,points=new[]{P(-50,50),P(250,50)}}}};
  texture=(Texture2D)type.GetMethod("Create").Invoke(null,new object[]{rectangular,200});try{Require(texture.GetPixel(100,110).r>.95f,"Rectangular map must preserve east-west road width in world meters");}finally{UnityEngine.Object.DestroyImmediate(texture);}
  var pinBounds=type.GetMethod("PinBounds");Require(pinBounds!=null,"Pin anchor bounds must be shared with hit testing");var anchor=new Vector2(75,90);var bounds=(Rect)pinBounds.Invoke(null,new object[]{anchor,26f});Require(Mathf.Abs(bounds.yMax-anchor.y)<.01f&&Mathf.Abs(bounds.center.x-anchor.x)<.01f,"Pin tip must end at the actual geographic anchor");
  var hit=(Rect)type.GetMethod("PinHitBounds").Invoke(null,new object[]{anchor,26f});Require(hit.Contains(anchor)&&hit.Contains(bounds.min)&&hit.Contains(bounds.max),"Hit area must include anchor, entire pin and outline");
  NavigationCueTests.Run();BrightUIThemeTests.Run();Debug.Log("MAP_PRESENTATION_OK geometry / width / layers / clipping / immutable input / empty map / rectangular roads / pin anchor");
 }
 public static void Build(){Run();MapReadabilityTests.Run();LawZoneMapLayoutTests.Run();PrototypeBuild.BuildHybrid();}
}
