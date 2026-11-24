using System;
using System.Collections.Generic;
using UnityEngine;

// Speed limits for the Eungye single game (design 08 ch.5.6, 2026-11-24).
// Confirmed values live in Resources/eungye-speed-limits.json with their evidence; estimated values
// (ordinary roads, alleys, the reach of a school zone around its confirmed point) come from the tuning catalog.
// The map document is not touched: a "제한속도" sign placed in the map editor still wins over this table.
[Serializable] public sealed class SpeedLimitRoad { public string name;public float kmh;public string evidence;public bool inactive; }
[Serializable] public sealed class SpeedLimitZone { public string id,label,evidence;public float x,z,kmh,radius; }
[Serializable] public sealed class SpeedLimitFile { public int schema;public string mapId,alleySuffix;public SpeedLimitRoad[] roads;public SpeedLimitZone[] zones; }

public static class RoadSpeedLimits {
 static SpeedLimitFile data;static Dictionary<string,float> byName;static bool loaded;
 public static bool Enabled {get{return ContentTuning.Int("law.speed.enabled",1)>0;}}
 public static float DefaultKmh {get{return (float)ContentTuning.Number("law.speed.defaultKmh",50);}}
 public static float AlleyKmh {get{return (float)ContentTuning.Number("law.speed.alleyKmh",30);}}
 public static float ZoneRadius {get{return (float)ContentTuning.Number("law.speed.zoneRadius",60);}}
 public static float Tolerance {get{return (float)ContentTuning.Number("law.speed.toleranceKmh",10);}}
 public static float Seconds {get{return (float)ContentTuning.Number("law.speed.seconds",3);}}
 public static float RepeatSeconds {get{return (float)ContentTuning.Number("law.speed.repeatSeconds",20);}}
 public static int Penalty {get{return ContentTuning.Int("law.speed.penalty",5);}}
 public static float HeavyOverKmh {get{return (float)ContentTuning.Number("law.speed.heavyOverKmh",20);}}
 public static int HeavyPenalty {get{return ContentTuning.Int("law.speed.heavyPenalty",10);}}
 public static int PassengerPenalty {get{return ContentTuning.Int("law.speed.passengerPenalty",3);}}
 // 2026-11-24 play-test round 2: a limit on every road made driving feel cramped. By default only the
 // confirmed sections (named roads in the file, school-zone points) carry a limit; 1 restores the estimate everywhere.
 public static bool EstimatedRoads {get{return ContentTuning.Int("law.speed.estimatedRoads",0)>0;}}
 // Extra driving-score points when a crash happens while over the limit, even inside the tolerance.
 public static int CrashPenalty {get{return ContentTuning.Int("law.speed.crashPenalty",5);}}
 public static float TrafficShare {get{return Mathf.Clamp((float)ContentTuning.Number("law.speed.trafficPercent",90),40,100)/100f;}}
 public static SpeedLimitFile Data {get{Load();return data;}}
 public static void Configure(string json){
  data=string.IsNullOrEmpty(json)?null:JsonUtility.FromJson<SpeedLimitFile>(json);loaded=true;byName=new Dictionary<string,float>();
  if(data==null)return;
  if(data.roads!=null)foreach(var r in data.roads)if(r!=null&&!r.inactive&&!string.IsNullOrEmpty(r.name)&&r.kmh>=5&&r.kmh<=120)byName[r.name]=r.kmh;
 }
 static void Load(){if(loaded)return;var asset=Resources.Load<TextAsset>("eungye-speed-limits");Configure(asset?asset.text:null);}
 // Road-name rule only. Named roads in the file are confirmed values; the rest is the estimate.
 public static float RoadLimit(string roadName){
  Load();float kmh;
  if(!string.IsNullOrEmpty(roadName)){
   if(byName.TryGetValue(roadName,out kmh))return kmh;
   if(!EstimatedRoads)return 0;
   string suffix=data!=null&&!string.IsNullOrEmpty(data.alleySuffix)?data.alleySuffix:"번길";
   if(roadName.EndsWith(suffix,StringComparison.Ordinal))return AlleyKmh;
  }
  return EstimatedRoads?DefaultKmh:0;
 }
 public static SpeedLimitZone ZoneAt(float x,float z){
  // A zone may carry its own shorter reach (2026-11-24 round 4: 40 m); the setting is the upper bound.
  Load();if(data==null||data.zones==null)return null;float setting=ZoneRadius;SpeedLimitZone best=null;float nearest=float.MaxValue;
  foreach(var zone in data.zones){if(zone==null||zone.kmh<5)continue;float radius=zone.radius>0?Mathf.Min(zone.radius,setting):setting;float dx=zone.x-x,dz=zone.z-z,d=dx*dx+dz*dz;if(d<=radius*radius&&d<nearest){nearest=d;best=zone;}}
  return best;
 }
 // The lower of the road rule and any school zone around the position.
 public static float Limit(string roadName,Vector3 position){
  float kmh=RoadLimit(roadName);var zone=ZoneAt(position.x,position.z);
  if(zone==null)return kmh;return kmh>0?Mathf.Min(kmh,zone.kmh):zone.kmh;
 }
 // One recorded violation: how many points, by how far over the limit the driver was.
 public static int PenaltyFor(float kmh,float limit){return kmh-limit>HeavyOverKmh?HeavyPenalty:Penalty;}
 public static bool Over(float kmh,float limit){return limit>0&&kmh>limit+Tolerance;}
 // Over the limit at all (1 km/h past it): no record by itself, but a crash in this band costs more.
 public static bool AboveLimit(float kmh,float limit){return limit>0&&kmh>=limit+1;}
}

// Round limit sign beside the speedometer. Signposts stand only where one was confirmed, so this is
// how the driver learns the limit of the road they are on.
public static class SpeedLimitBadge {
 static Texture2D disc;static GUIStyle text;
 static void EnsureDisc(){if(disc)return;disc=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));disc.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(31-d)));}disc.Apply(false,true);disc.wrapMode=TextureWrapMode.Clamp;}
 // A round sign face: coloured rim and a lighter centre. Used by the map discs and the limit-change notice.
 public static void DrawDisc(Rect r,Color rim,Color inner){EnsureDisc();var old=GUI.color;GUI.color=rim;GUI.DrawTexture(r,disc);float w=r.width*.14f;GUI.color=inner;GUI.DrawTexture(new Rect(r.x+w,r.y+w,r.width-w*2,r.height-w*2),disc);GUI.color=old;}
 public static void Draw(Rect r,DriveSession s){
  if(Event.current==null||Event.current.type!=EventType.Repaint||!s||!s.law)return;
  float limit=s.law.CurrentLimit;if(limit<=0)return;
  EnsureDisc();
  if(text==null){text=new GUIStyle(GUI.skin.label);text.alignment=TextAnchor.MiddleCenter;text.fontStyle=FontStyle.Bold;text.padding=new RectOffset(0,0,0,0);}
  float kmh=Mathf.Abs(s.taxi.Speed*3.6f);bool above=RoadSpeedLimits.AboveLimit(kmh,limit),over=kmh>limit+RoadSpeedLimits.Tolerance;var old=GUI.color;
  GUI.color=new Color(.86f,.13f,.13f);GUI.DrawTexture(r,disc);
  // Inside the tolerance: steady pale amber (no record, a crash costs more). Past it: red blink (record after a hold).
  float rim=r.width*.14f;GUI.color=over?(Mathf.Repeat(Time.unscaledTime,.8f)<.4f?new Color(1,.80f,.80f):Color.white):above?new Color(1,.93f,.72f):Color.white;GUI.DrawTexture(new Rect(r.x+rim,r.y+rim,r.width-rim*2,r.height-rim*2),disc);
  GUI.color=old;text.fontSize=Mathf.RoundToInt(r.height*.42f);text.normal.textColor=new Color(.08f,.09f,.12f);GUI.Label(r,limit.ToString("0"),text);
 }
}
