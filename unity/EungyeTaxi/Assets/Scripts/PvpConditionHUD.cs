using System;
using UnityEngine;
public sealed class PvpConditionHUD:MonoBehaviour {
 public DriveSession session;float next;string status="";GUIStyle style;
 public static Rect PanelBounds(float width,float height){float scale=UiReadability.HudScale(width,height);return new Rect(width/2-310*scale,height-334*scale,620*scale,80*scale);}
 public static string ForecastText(DriverCondition driver,double seconds){var f=PvpConditionRules.Forecast(driver,seconds);return "예상 피로 "+f.fatigue.ToString("0")+" · "+(f.PassengerMustLeave?"운송 중단 위험":f.EyesClosing?"눈감김 위험":f.RestRecommended?"휴식 권장":"졸음 위험 낮음")+" · 교통에 따라 변동";}
 // Room creation owns condition configuration; no persistent driving overlay.
}
