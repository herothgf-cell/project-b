using UnityEngine;

// Out-of-town call, method A (Eungye single, 2026-11-24): after the passenger of an out-of-town call is delivered
// at the edge stop, the screen fades out for a few seconds and the out-of-town leg (there and back empty) passes on
// the shift clock, fuel and fatigue in one step; the taxi then continues empty from the edge stop. Applied once,
// then saved, so a reload never passes the leg twice.
public sealed class OutOfTownTrip : MonoBehaviour {
 public DriveSession session;
 string trip="";bool aboard,emergency;public int EmergencyLegs;float blackoutAt=-1;bool applied;string line="";
 public int Legs,Skipped;public string LastOutcome="";public double LastElapsedBefore,LastElapsedAfter;public float LastFuelBefore,LastFuelAfter;public double LastFatigueBefore,LastFatigueAfter;
 public bool Blackout {get{return blackoutAt>=0;}}
 void Update(){
  if(!session||!session.jobs||!session.business||session.business.ledger==null)return;
  var j=session.jobs;
  if(!aboard&&j.trip.State==TripState.OnTrip&&j.current!=null&&(j.current.variety==CallBoardRules.OutOfTown||j.current.variety==CallBoardRules.Emergency)){aboard=true;emergency=j.current.variety==CallBoardRules.Emergency;trip=session.law?session.law.tripId:"";Debug.Log("OUT_OF_TOWN aboard trip="+trip+" emergency="+emergency);}
  if(aboard&&j.trip.State!=TripState.OnTrip){
   var result=session.business.passengers.FindLast(r=>r!=null&&r.tripId==trip);
   if(result!=null){aboard=false;if(emergency)j.CountEmergency();else j.CountOutOfTown();
    // The in-map emergency room (2026-11-26) ends the ride at its door: no blackout, no leg.
    if(result.paid>0&&emergency&&EmergencyRules.InMap(j.HospitalLegMetres))ArriveEmergency();
    else if(result.paid>0){blackoutAt=Time.unscaledTime;applied=false;}
    else{Skipped++;LastOutcome="운송 실패·지각 · 시외 구간 없음";}}
   else if(j.trip.State==TripState.Idle&&j.current==null){aboard=false;Skipped++;LastOutcome="운송 취소";}
  }
  if(Blackout){
   float t=Time.unscaledTime-blackoutAt,fade=OutOfTownRules.FadeSeconds;
   if(session.taxi){session.taxi.body.velocity=Vector3.zero;session.taxi.body.angularVelocity=Vector3.zero;}
   if(!applied&&t>=.4f)Apply();
   if(t>=fade+.8f)blackoutAt=-1;
  }
 }
 void ArriveEmergency(){
  EmergencyLegs++;LastOutcome="응급실 도착";var n=session.GetComponent<DriveNotifications>();
  if(n)n.Card("응급 손님 · "+EmergencyRules.HospitalName+"에 모셔다 드렸어요 · “기사님, 정말 고맙습니다.”");
  session.business.SaveCheckpoint();Debug.Log("OUT_OF_TOWN emergency arrived in map at "+EmergencyRules.HospitalName);
 }
 void Apply(){
  applied=true;var l=session.business.ledger;
  LastElapsedBefore=l.Elapsed;LastFuelBefore=l.Account!=null?l.Account.Fuel:0;LastFatigueBefore=l.Driver!=null?l.Driver.fatigue:0;
  float legSeconds=emergency?EmergencyRules.LegSeconds(session.jobs.HospitalLegMetres):OutOfTownRules.LegSeconds,legMetres=emergency?session.jobs.HospitalLegMetres*1.3f*2:OutOfTownRules.LegMetres;
  float half=legSeconds*.5f,metres=legMetres*.5f;
  l.AdvanceOperation(half,metres,true);l.AdvanceOperation(half,metres,false);
  LastElapsedAfter=l.Elapsed;LastFuelAfter=l.Account!=null?l.Account.Fuel:0;LastFatigueAfter=l.Driver!=null?l.Driver.fatigue:0;
  var n=session.GetComponent<DriveNotifications>();int minutes=Mathf.Max(1,Mathf.RoundToInt(legSeconds/60f));
  if(emergency){EmergencyLegs++;LastOutcome="응급실 "+minutes+"분";line=EmergencyRules.HospitalName+" · 영업 시간 약 "+minutes+"분 경과\n손님을 응급실에 모셔다 드렸습니다";if(n)n.Card("응급 손님 · "+EmergencyRules.HospitalName+"에 모셔다 드렸어요 · “기사님, 정말 고맙습니다.”");}
  else{Legs++;LastOutcome="시외 구간 "+OutOfTownRules.Minutes+"분";line="시외 구간 · 영업 시간 "+OutOfTownRules.Minutes+"분 경과\n손님을 내려 드리고 빈 차로 돌아왔습니다";if(n)n.Card("시외 구간 · 영업 시간 "+OutOfTownRules.Minutes+"분 경과 · 빈 차로 돌아왔어요");}
  session.business.SaveCheckpoint();
  Debug.Log("OUT_OF_TOWN leg elapsed "+LastElapsedBefore.ToString("0")+" -> "+LastElapsedAfter.ToString("0")+" fuel "+LastFuelBefore.ToString("0.0")+" -> "+LastFuelAfter.ToString("0.0"));
 }
 void OnGUI(){
  if(!Blackout||session.IsSimulationBot)return;
  float t=Time.unscaledTime-blackoutAt,fade=OutOfTownRules.FadeSeconds;
  float a=t<.4f?t/.4f:t<fade+.4f?1:Mathf.Clamp01(1-(t-fade-.4f)/.4f);
  GUI.depth=-1000;var old=GUI.color;GUI.color=new Color(0,0,0,a);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
  if(a>.5f&&line.Length>0){GUI.color=new Color(1,1,1,a);var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(28*CockpitUI.Scale),wordWrap=true};style.normal.textColor=Color.white;if(KoreanText.Font)style.font=KoreanText.Font;GUI.Label(new Rect(0,Screen.height*.4f,Screen.width,Screen.height*.2f),line,style);}
  GUI.color=old;
 }
}
