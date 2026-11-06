using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Runs against the old renderer too: before the shared layout helper exists, read
// its real badge rectangles from PvpPinObstacles rather than failing on an API name.
public static class MapLabelReadabilityChecks {
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
 static int checks;static readonly List<string> failures=new List<string>();
 static void Check(bool ok,string message){checks++;if(!ok){failures.Add(message);Debug.Log("MAP_LABEL_FAIL "+message);}}
 static void Set(object target,string field,object value){target.GetType().GetField(field,Hidden).SetValue(target,value);}
 static List<Rect> Layout(FullMapOverlay overlay,Rect map,GUIStyle style,List<Rect> occupied){
  var method=typeof(FullMapOverlay).GetMethod("LayoutFacilityLabels",Hidden);
  if(method!=null)return (List<Rect>)method.Invoke(overlay,new object[]{map,style,occupied});
  var old=(List<Rect>)typeof(FullMapOverlay).GetMethod("PvpPinObstacles",Hidden).Invoke(overlay,new object[]{map});
  var labels=new List<Rect>();for(int i=1;i<old.Count;i+=2)labels.Add(old[i]);return labels;
 }
 static Vector3 World(Vector2 point,Rect map){float span=Mathf.Min(map.width,map.height);return new Vector3(500+(point.x-map.center.x)*1000/span,0,500-(point.y-map.center.y)*1000/span);}
 static void Case(Vector2 screen,string caption,bool edge){
  float scale=UiReadability.MapScale(screen.x,screen.y);var map=new Rect(0,0,screen.x/scale-370,screen.y/scale-112);
  var style=new GUIStyle{font=KoreanText.Font,fontSize=UiReadability.FontFor(12,scale),wordWrap=true,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0)};
  var anchor=edge?new Vector2(map.xMax-20,22):map.center;
  var second=anchor+new Vector2(-48,40);
  var icons=new[]{new Rect(anchor.x-15,anchor.y-15,30,30),new Rect(second.x-15,second.y-15,30,30)};
  var root=new GameObject("map label regression");root.SetActive(false);
  try{
   var session=root.AddComponent<DriveSession>();session.world=root.AddComponent<WorldBuilder>();session.world.map=new MapData{width=1000,depth=1000};
   session.services=root.AddComponent<ServiceStops>();session.simulationMatch=root.AddComponent<PvpSimulationMatch>();
   var overlay=root.AddComponent<FullMapOverlay>();overlay.session=session;
   var places=new List<FullMapOverlay.Place>{new FullMapOverlay.Place{position=World(anchor,map),capabilityLabel=caption},new FullMapOverlay.Place{position=World(second,map),capabilityLabel="주유 · 세차"}};
   Set(overlay,"cachedPlaces",places);Set(overlay,"placesMap",session.world.map);Set(overlay,"nextPlaces",float.PositiveInfinity);
   // An offer pin sits directly above the facility; another drop label, a player
   // label and the map controls block the obvious alternatives.
   var occupied=new List<Rect>(icons){MapPresentation.PinHitBounds(anchor+new Vector2(0,-20),26),new Rect(anchor.x+20,anchor.y+15,54,30),new Rect(anchor.x-110,anchor.y+42,148,28),new Rect(8,8,152,48),new Rect(map.xMax-64,map.yMax-182,52,152)};
   var original=new List<Rect>(occupied);var labels=Layout(overlay,map,style,occupied);
   Check(labels.Count==2,"both facility captions retained at "+screen.x+" edge="+edge);
   for(int i=0;i<labels.Count;i++){
    var box=labels[i];string context=screen.x+" / "+places[i].capabilityLabel.Replace('\n',' ')+" edge="+edge;
    float height=style.CalcHeight(new GUIContent(places[i].capabilityLabel),Mathf.Max(1,box.width-12));
    Check(box.height+0.01f>=height+8,"measured wrapped text plus vertical padding fits: "+context+" need="+(height+8)+" got="+box.height);
    Check(box.xMin>=map.xMin&&box.yMin>=map.yMin&&box.xMax<=map.xMax+.01f&&box.yMax<=map.yMax+.01f,"badge remains inside map: "+context);
    foreach(var obstacle in original)Check(!box.Overlaps(obstacle),"badge clears facility/call/drop/player/control obstacle: "+context+" badge="+box+" obstacle="+obstacle);
    for(int j=0;j<i;j++)Check(!box.Overlaps(labels[j]),"facility badges do not overlap: "+context);
   }
   Check(style.fontSize*scale>=16,"label physical font floor at "+screen.x);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static void SingleCallPins(Vector2 screen){
  float scale=UiReadability.MapScale(screen.x,screen.y);var map=new Rect(0,0,screen.x/scale-370,screen.y/scale-112);var facility=map.center;var icon=new Rect(facility.x-16,facility.y-16,32,32);
  var root=new GameObject("single call pin facility regression");root.SetActive(false);
  try{
   var session=root.AddComponent<DriveSession>();session.world=root.AddComponent<WorldBuilder>();session.world.map=new MapData{width=1000,depth=1000};session.services=root.AddComponent<ServiceStops>();session.jobs=root.AddComponent<PassengerJobs>();
   var point=World(facility,map);session.jobs.stops=new[]{new MapStop{x=point.x,z=point.z}};session.jobs.offers=new[]{new JobOffer{pickup=0},new JobOffer{pickup=0},new JobOffer{pickup=0}};
   var overlay=root.AddComponent<FullMapOverlay>();overlay.session=session;Set(overlay,"cachedPlaces",new List<FullMapOverlay.Place>{new FullMapOverlay.Place{position=point,capabilityLabel="공업사 · 수리/견인"}});Set(overlay,"placesMap",session.world.map);Set(overlay,"nextPlaces",float.PositiveInfinity);
   var pins=new List<Rect>();for(int i=0;i<session.jobs.offers.Length;i++){
    var p=overlay.CallPinPosition(i,map);var hit=MapPresentation.PinHitBounds(p,26);
    Check(!hit.Overlaps(icon),"actual single call "+(i+1)+" avoids facility icon at "+screen.x+" pin="+hit+" icon="+icon);
    foreach(var prior in pins)Check(!hit.Overlaps(prior),"actual single call hit targets remain separate at "+screen.x);
    Check(hit.xMin>=map.xMin&&hit.yMin>=map.yMin&&hit.xMax<=map.xMax&&hit.yMax<=map.yMax,"actual single call hit target remains inside map at "+screen.x);
    Check(Vector2.Distance(p,facility)<160,"actual single displaced call remains near its authored anchor at "+screen.x);pins.Add(hit);
   }
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 public static void Run(){
  checks=0;failures.Clear();
  foreach(var screen in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)})
   foreach(var caption in new[]{"공업사 · 수리/견인","공업사\n수리/견인"})foreach(bool edge in new[]{false,true})Case(screen,caption,edge);
  foreach(var screen in new[]{new Vector2(1280,720),new Vector2(1600,900),new Vector2(1920,1080)})SingleCallPins(screen);
  if(failures.Count>0)throw new Exception("MAP_LABEL_READABILITY failures="+failures.Count+" checks="+checks+" first="+failures[0]);
  Debug.Log("MAP_LABEL_READABILITY_OK checks="+checks+" widths=1280/1600/1920 measuredWrap padding iconCallDropPlayerControl edge");
 }
}
