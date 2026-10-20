using System;
using System.Collections.Generic;
using UnityEngine;

public static class LawZoneMapLayoutTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("LAW_ZONE_LABELS: "+message);}
 public static void Run(){
  var bounds=new Rect(4,4,272,272);var reserved=new Rect(120,120,40,40);var occupied=new List<Rect>{reserved};
  foreach(var p in new[]{new Vector2(78,233),new Vector2(68,233),new Vector2(97,233),new Vector2(68,257),new Vector2(106,257),new Vector2(109,182),new Vector2(106,138)}){
   var r=LawZoneMapLayout.Place(p,new Vector2(92,34),bounds,occupied);
   Check(r.xMin>=bounds.xMin&&r.yMin>=bounds.yMin&&r.xMax<=bounds.xMax&&r.yMax<=bounds.yMax,"name must stay inside minimap");
   foreach(var other in occupied)Check(!r.Overlaps(other),"seven clustered names must not overlap each other or player");occupied.Add(r);
  }
  var edge=LawZoneMapLayout.Place(new Vector2(280,280),new Vector2(92,34),bounds,new List<Rect>());
  Check(edge.xMax<=bounds.xMax&&edge.yMax<=bounds.yMax,"offscreen selected zone name must stay visible");
  Debug.Log("LAW_ZONE_MAP_LAYOUT_OK seven names, player exclusion, edge clamp");
 }
}
