using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The authoring operation supplies the replacement strokes and the local
// footprint they replace. Both the renderer and centreline scoring use them.
public sealed class ReviewedCorridorPaint {
 public const string MaskType="검토 도색 교체 영역", StrokeType="검토 차선", MedianType="검토 중앙선", SignalPoleType="검토 신호 지주", YellowHatchType="검토 황색 빗금";
 readonly RoadMapFacility[] masks,strokes;
 readonly bool eungye;
 // These r48 strokes used separate fitted axes on either side of J866.
 // The east approach has an approved U-turn opening. Keep its continuous
 // upstream median (from 1077 and 1086 onwards), clear the two detached pairs.
 static readonly HashSet<string> detachedJ866Medians=new HashSet<string>(StringComparer.Ordinal){
  "r48:suinro:centre:0:1062","r48:suinro:centre:0:1063","r48:suinro:centre:0:1064",
  "r48:suinro:centre:0:1065","r48:suinro:centre:0:1066","r48:suinro:centre:0:1067",
  "r48:suinro:centre:0:1068","r48:suinro:centre:0:1069","r48:suinro:centre:0:1070",
  "r48:suinro:centre:0:1071","r48:suinro:centre:1:1072","r48:suinro:centre:1:1073",
  "r48:suinro:centre:1:1074","r48:suinro:centre:1:1075","r48:suinro:centre:2:1076",
  "r48:suinro:centre:2:1085"
 };
 public ReviewedCorridorPaint(MapData map){
  eungye=map.mapId=="eungye";
  var facilities=map.editorDocument?.facilities??Array.Empty<RoadMapFacility>();
  masks=facilities.Where(f=>f.kind=="lanePresentation"&&f.type==MaskType).ToArray();
  strokes=facilities.Where(IsStroke).ToArray();
 }
 public static bool IsStroke(RoadMapFacility f)=>f.kind=="line"&&(f.type==StrokeType||f.type==MedianType||f.type==YellowHatchType);
 public static bool IsLayout(RoadMapFacility f)=>f.kind=="lanePresentation"&&(f.type==MaskType||f.type==SignalPoleType);
 // Yellow source lines (centre, kerb, safety-zone outlines) inside a replacement
 // footprint are hidden whatever their heading: their tapers cross today's lanes.
 // White transverse lines (stop lines) still need the heading test. Only the
 // 2026-11-20 Suin-ro masks use this; older reviewed masks keep observed kerbs.
 public const string AnyHeadingPrefix="r48:suinro:";
 public bool Covers(Vector3 point,Vector3 heading,bool sourceYellow=false,bool safetyOutline=false){
  foreach(var mask in masks){
   var delta=point-mask.Position;
   if(delta.sqrMagnitude>mask.length*mask.length+mask.width*mask.width)continue;
   bool suinro=mask.id.StartsWith(AnyHeadingPrefix,StringComparison.Ordinal);
   if(safetyOutline&&!suinro)continue;
   var forward=Quaternion.Euler(0,mask.yaw,0)*Vector3.forward;
   if(!(sourceYellow&&suinro)&&Mathf.Abs(Vector3.Dot(heading,forward))<.85f)continue;
   if(Mathf.Abs(Vector3.Dot(delta,forward))<=mask.length*.5f&&
      Mathf.Abs(Vector3.Dot(delta,TrafficSystem.Right(forward)))<=mask.width*.5f)return true;
  }
  return false;
 }
 public void Draw(Action<string,Vector3,Vector3,float,float,bool> paint){
  foreach(var stroke in strokes){
   bool median=stroke.type==MedianType;
   if(eungye&&median&&detachedJ866Medians.Contains(stroke.id)){
    if(stroke.id=="r48:suinro:centre:0:1062"){
     // Continue the adjoining single line on surveyed road 37612022:A0020000:4430:0.
     // The start is its existing paint endpoint and the end is the last r48 west station.
     var start=new Vector3(44.15f,stroke.Position.y,530.69f);
     var end=new Vector3(63f,stroke.Position.y,529.71f);
     paint("Centre line",(start+end)*.5f,(end-start).normalized,.14f,Vector3.Distance(start,end),true);
    }
    continue;
   }
   bool hatch=stroke.type==YellowHatchType;
   paint(median?"Centre line":hatch?"Safety island hatch":"Lane divider",stroke.Position,Quaternion.Euler(0,stroke.yaw,0)*Vector3.forward,stroke.width,stroke.length,median||hatch);
  }
 }
}
