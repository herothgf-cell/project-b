using System;
using System.Collections.Generic;
using UnityEngine;

// Fixed speed cameras (design 08 ch.5.6 item 4.2, decided 2026-11-24, Eungye single). The 16 camera points of the
// public dataset that lie on game lanes (Resources/eungye-speed-cameras.json). No hidden camera: within 150 m a
// badge with the camera's limit and distance shows. Passing a camera over its limit + 10 km/h costs a fine paid
// from cash at once (shortfall as unpaid, like accident fees); each camera fines at most once a day. A fine also
// cancels the hurry reward of the ride. Cameras fine in either direction (the data's direction is not mapped).
public static class SpeedCameraRules {
 [Serializable] public sealed class Camera {public string id,road,place;public int kmh;public float x,z;public Vector3 V(){return new Vector3(x,0,z);}}
 [Serializable] sealed class File {public int schema;public string mapId,source,license,note;public Camera[] cameras;}
 static Camera[] cameras;static bool loaded;public static string Source="";
 static float F(string id,double fallback){return (float)ContentTuning.Number(id,fallback);}
 static int I(string id,int fallback){return ContentTuning.Int(id,fallback);}
 public static bool Enabled {get{return I("camera.enabled",1)!=0;}}
 public static float Tolerance {get{return Mathf.Clamp(F("camera.toleranceKmh",10),0,40);}}
 public static float WarnMetres {get{return Mathf.Clamp(F("camera.warnMetres",150),30,500);}}
 public static int FineLow {get{return Mathf.Max(0,I("camera.fine.low",2000));}}
 public static int FineMid {get{return Mathf.Max(0,I("camera.fine.mid",4000));}}
 public static int FineHigh {get{return Mathf.Max(0,I("camera.fine.high",7000));}}
 public static Camera[] Cameras {get{if(!loaded){loaded=true;var a=Resources.Load<TextAsset>("eungye-speed-cameras");Configure(a?a.text:null);}return cameras??new Camera[0];}}
 public static void Configure(string json){loaded=true;cameras=null;if(string.IsNullOrEmpty(json))return;var f=JsonUtility.FromJson<File>(json);if(f!=null&&f.schema==1&&f.cameras!=null){cameras=f.cameras;Source=f.source??"";}}
 // Fine by how far over the limit: over the tolerance up to +20 -> low, up to +40 -> mid, beyond -> high. 0 = none.
 public static int Fine(float kmh,int limit){float over=kmh-limit;if(limit<=0||over<=Tolerance)return 0;return over<=20?FineLow:over<=40?FineMid:FineHigh;}
}

public sealed class SpeedCameras : MonoBehaviour {
 public DriveSession session;
 int day=-1;readonly HashSet<string> ticketed=new HashSet<string>();
 readonly Dictionary<string,float> lastAlong=new Dictionary<string,float>();
 public int Tickets,FinedWon;public string LastTicket="";public SpeedCameraRules.Camera Ahead;public float AheadMetres;
 bool Single {get{return session&&session.traffic&&session.traffic.EungyeSingle&&!session.simulationMatch&&!session.IsSimulationBot&&!PvpNativeLan.IsReplica(session)&&session.business&&session.business.enabledCampaign&&!PracticeSession.Enabled;}}
 bool Running {get{var b=session.business;return b&&b.ledger!=null&&b.ledger.Operating&&!b.preparing&&!b.EveningDrive&&!session.paused&&Time.timeScale>0&&b.ledger.Account!=null;}}
 void Update(){
  Ahead=null;if(!Single||!SpeedCameraRules.Enabled||!session.taxi)return;
  var l=session.business.ledger;if(l!=null&&l.Day!=day){day=l.Day;ticketed.Clear();}
  var t=session.taxi.transform;var fwd=t.forward;fwd.y=0;fwd.Normalize();var right=TrafficSystem.Right(fwd);float best=float.MaxValue;
  foreach(var c in SpeedCameraRules.Cameras){
   var rel=c.V()-t.position;rel.y=0;if(rel.sqrMagnitude>400*400){lastAlong.Remove(c.id);continue;}
   float along=Vector3.Dot(rel,fwd),lateral=Mathf.Abs(Vector3.Dot(rel,right));
   if(along>0&&along<=SpeedCameraRules.WarnMetres&&lateral<=15&&along<best){best=along;Ahead=c;AheadMetres=along;}
   float before;
   // Passing: the camera goes from ahead to behind while the taxi is beside it.
   if(lastAlong.TryGetValue(c.id,out before)&&before>0&&along<=0&&lateral<=12&&Running)Pass(c);
   lastAlong[c.id]=along;
  }
 }
 void Pass(SpeedCameraRules.Camera c){
  if(ticketed.Contains(c.id))return;
  float kmh=session.taxi.Speed*3.6f;int fine=SpeedCameraRules.Fine(kmh,c.kmh);if(fine<=0)return;
  // A paired camera at the same point (both lanes) fines once.
  foreach(var o in SpeedCameraRules.Cameras)if(Vector3.Distance(o.V(),c.V())<1)ticketed.Add(o.id);
  var l=session.business.ledger;string id="fine:"+c.id+":"+l.Day;
  if(!l.Account.Fine(id,fine,l.Day))return;
  Tickets++;FinedWon+=fine;LastTicket=c.id;
  var n=session.GetComponent<DriveNotifications>();if(n)n.Card("단속 카메라 · 제한 "+c.kmh+" · "+Mathf.RoundToInt(kmh)+"km/h · 과태료 "+fine.ToString("N0")+"원");
  session.business.SaveCheckpoint();
  Debug.Log("SPEED_CAMERA ticket "+c.id+" "+Mathf.RoundToInt(kmh)+"/"+c.kmh+" fine="+fine);
 }
 void OnGUI(){
  if(Ahead==null||!Single||session.paused)return;
  float s=CockpitUI.Scale;var r=new Rect(Screen.width*.5f-120*s,Screen.height-235*s,240*s,44*s);
  bool blink=Mathf.Repeat(Time.time,.8f)<.5f||AheadMetres<50;
  var old=GUI.color;GUI.color=blink?new Color(1,1,1,.95f):new Color(1,1,1,.6f);
  CasualUI.RoundedFill(r,new Color(.12f,.13f,.16f,.88f));
  var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(19*s),fontStyle=FontStyle.Bold};style.normal.textColor=new Color(1,.85f,.25f);if(KoreanText.Font)style.font=KoreanText.Font;
  GUI.Label(r,"단속 카메라 "+Ahead.kmh+" · "+Mathf.RoundToInt(AheadMetres)+"m",style);GUI.color=old;
 }
}
