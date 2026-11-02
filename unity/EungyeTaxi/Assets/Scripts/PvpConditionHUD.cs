using System;
using UnityEngine;
public sealed class PvpConditionHUD:MonoBehaviour {
 public DriveSession session;float next;string status="";GUIStyle style;
 public static string ForecastText(DriverCondition driver,double seconds){var f=PvpConditionRules.Forecast(driver,seconds);return "예상 피로 "+f.fatigue.ToString("0")+" · "+(f.PassengerMustLeave?"운송 중단 위험":f.EyesClosing?"눈감김 위험":f.RestRecommended?"휴식 권장":"졸음 위험 낮음")+" · 교통에 따라 변동";}
 void Update(){if(!session||!session.business||Time.unscaledTime<next)return;next=Time.unscaledTime+1;var d=session.business.ledger.Driver;double seconds=PvpConditionRules.SecondsToFatigue100(d);status=(d.fatigue>=100?"졸음 위험 · 안전 정차 후 휴식":"피로 100까지 약 "+Math.Ceiling(seconds/60).ToString("0")+"분 · +"+PvpConditionRules.FatiguePerMinute(d.satiety).ToString("0.#")+"/분")+(d.satiety<=25?" · 공복":"");status+="\n식사 1,000원 · 15초 / 휴식 무료 · 45초";}
 void OnGUI(){if(session&&session.jobs&&(session.requests.HasVisiblePanel||session.GetComponent<PassengerBehaviorUI>().Showing||!string.IsNullOrEmpty(session.jobs.VisibleCareDialogue)))return;if(!session||!session.StartupReady||session.paused||session.FullMapOpen||session.services.open||session.RideResultOpen||session.ChoiceOpen||!session.simulationMatch||session.simulationMatch.Finished)return;if(style==null)style=new GUIStyle(GUI.skin.box){font=KoreanText.Font,fontSize=14,wordWrap=true,alignment=TextAnchor.MiddleLeft};GUI.Box(new Rect(Screen.width/2f-235,Screen.height-76,470,66),status,style);}
}
