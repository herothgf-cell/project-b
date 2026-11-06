using System;
using UnityEngine;
public sealed class PvpConditionHUD:MonoBehaviour {
 public DriveSession session;float next;string status="";GUIStyle style;
 public static Rect PanelBounds(float width,float height){float scale=UiReadability.HudScale(width,height);return new Rect(width/2-310*scale,height-334*scale,620*scale,80*scale);}
 public static string ForecastText(DriverCondition driver,double seconds){var f=PvpConditionRules.Forecast(driver,seconds);return "예상 피로 "+f.fatigue.ToString("0")+" · "+(f.PassengerMustLeave?"운송 중단 위험":f.EyesClosing?"눈감김 위험":f.RestRecommended?"휴식 권장":"졸음 위험 낮음")+" · 교통에 따라 변동";}
 void Update(){if(!session||!session.business||Time.unscaledTime<next)return;next=Time.unscaledTime+1;var d=session.business.ledger.Driver;double seconds=PvpConditionRules.SecondsToFatigue100(d);status=(d.fatigue>=100?"졸음 위험 · 안전 정차 후 휴식":"피로 100까지 약 "+Math.Ceiling(seconds/60).ToString("0")+"분 · +"+PvpConditionRules.FatiguePerMinute(d.satiety).ToString("0.#")+"/분")+(d.satiety<=25?" · 공복":"");status+="\n식사 1,000원 · 15초 / 휴식 무료 · 45초";}
 void OnGUI(){if(session&&session.jobs&&(session.requests.HasVisiblePanel||session.GetComponent<PassengerBehaviorUI>().Showing||!string.IsNullOrEmpty(session.jobs.VisibleCareDialogue)))return;if(!session||!session.StartupReady||session.paused||session.FullMapOpen||session.services.open||session.RideResultOpen||session.ChoiceOpen||!session.simulationMatch||session.simulationMatch.Finished)return;
  var matrix=GUI.matrix;float scale=CockpitUI.Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));var bounds=PanelBounds(Screen.width,Screen.height);var r=new Rect(bounds.x/scale,bounds.y/scale,bounds.width/scale,bounds.height/scale);DrivingHudArt.Glass(r);style=new GUIStyle(DrivingHudArt.Text(14,DrivingHudArt.Light)){wordWrap=true};GUI.Label(new Rect(r.x+14,r.y+8,r.width-28,r.height-16),status,style);GUI.matrix=matrix;
 }
}
