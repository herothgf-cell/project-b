using System;
using System.Collections.Generic;
using UnityEngine;

// The initial loop survey recorded one lane per direction. The May 2026
// north-leg follow-up corrects three source groups to north2/south1. Existing
// compiled centres remain game geometry: their 9.7m separation includes the
// median, and must not be reused as the width of either driving lane.
public static class RoadviewCorridorPresentation {
 public const float GreenSurfaceWidth=13.6f,GreenLaneWidth=3.5f;
 public const string ArcEvidence="https://map.naver.com/p?c=18,0,0,0,adh&p=XmcQD7MJWTFPy9j4WjMU_A,175,-14,80,Float";
 public const string NorthEvidence="https://map.naver.com/p?c=18,0,0,0,adh&p=9XclBrRLJul3fHo6HK1_YA,178,-16,80,Float";
 static readonly HashSet<string> sources=new HashSet<string>(StringComparer.Ordinal){
  "37612023:A0020000:430:0","37612023:A0020000:641:0","37612023:A0020000:750:0","37612023:A0020000:895:0","37612023:A0020000:928:0","37612023:A0020000:1116:0","37612023:A0020000:1120:0","37612023:A0020000:1193:0","37612023:A0020000:1380:0","37612023:A0020000:1498:0","37612023:A0020000:1547:0","37612023:A0020000:2059:0","37612023:A0020000:2071:0","37612023:A0020000:2080:0"
 };
 static readonly HashSet<string> yellowSources=new HashSet<string>(StringComparer.Ordinal){"37612023:A0020000:294:0","37612023:A0020000:700:0","37612023:A0020000:1244:0"};
 static readonly HashSet<string> cyanNorthSources=new HashSet<string>(StringComparer.Ordinal){"37612023:A0020000:1116:0","37612023:A0020000:430:0","37612023:A0020000:750:0","37612023:A0020000:1193:0","37612023:A0020000:1120:0","37612023:A0020000:1547:0","37612023:A0020000:1498:0","37612023:A0020000:895:0","37612023:A0020000:641:0"};
 public static bool CyanNorthSource(string id){return id!=null&&cyanNorthSources.Contains(id);}
 public static bool CyanNorthCorridor(string ids){return !string.IsNullOrEmpty(ids)&&Array.TrueForAll(ids.Split(','),CyanNorthSource);}
 public static bool GreenSource(string id){return id!=null&&sources.Contains(id);}
 public static bool YellowSource(string id){return id!=null&&yellowSources.Contains(id);}
 public static bool GreenCorridor(string ids){
  if(string.IsNullOrEmpty(ids))return false;
  foreach(var id in ids.Split(','))if(!GreenSource(id))return false;
  return true;
 }
 public static bool YellowCorridor(string ids){if(string.IsNullOrEmpty(ids))return false;foreach(var id in ids.Split(','))if(!YellowSource(id))return false;return true;}
 public static float OuterHalf(string source,List<RoadGeometry.Lane> lanes,float fallback){
  if(lanes==null||lanes.Count==0)return fallback;
  // Bound curb paint to actual driving width after the reviewed 3-lane edit.
  // Keep the old 1+1 presentation until that explicit document edit is applied.
  if(CyanNorthCorridor(source)&&lanes.Count==3)return Mathf.Min(fallback,GreenLaneWidth*.5f);
  if(GreenCorridor(source)){
   if(lanes.Count>2||lanes.Count==2&&lanes[0].direction==lanes[1].direction)return fallback;
   return Mathf.Min(fallback,GreenLaneWidth*.5f);
  }
  // The reviewed Eungye-ro stem has three lanes per direction. Its existing
  // 5.7m centre spacing also includes extra road space: keep the original lane
  // dividers, but do not extend curb paint beyond the narrower 700 surface.
  if(YellowCorridor(source)){
   int forward=0,reverse=0;foreach(var lane in lanes){if(lane.direction>0)forward++;else reverse++;}
   if(forward<=3&&reverse<=3)return Mathf.Min(fallback,GreenLaneWidth*.5f);
  }
  return fallback;
 }
}
