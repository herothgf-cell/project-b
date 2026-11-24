using UnityEngine;

// 08 chapter 5 (2026-11-24, Eungye single): numbers and radio lines of the after-work free drive.
public static class EveningDriveRules {
 static readonly string[] lines={"은계 FM, 밤 드라이브에 어울리는 노래를 이어서 들려드립니다.","오늘 하루도 애쓰셨습니다. 창문을 조금 열어 보세요.","은계호수공원 쪽 밤길이 한산하다는 제보입니다."};
 public static bool Enabled {get{return ContentTuning.Int("home.eveningDrive.enabled",1)!=0;}}
 public static double Hour {get{return System.Math.Max(0,System.Math.Min(23.9,ContentTuning.Number("home.eveningDrive.hour",21)));}}
 public static float RadioSeconds {get{return Mathf.Max(15,(float)ContentTuning.Number("home.eveningDrive.radioSeconds",45));}}
 // One caption per interval, shown from 4 s to 12 s into it; the three lines rotate.
 public static string RadioLine(float seconds){
  if(seconds<0)return "";float period=RadioSeconds;int n=(int)(seconds/period);float into=seconds-n*period;
  if(into<4||into>12)return "";int i=n%lines.Length;return ContentTuning.Text("radio.evening."+(i+1),lines[i]);
 }
}

// The only screen element of the free drive: one line that says how to finish it.
public sealed class EveningDriveBanner : MonoBehaviour {
 public BusinessProgress business;GUIStyle style;
 public const string Text="퇴근 후 드라이브 · 미터기·호출 없음 · 숙소 칸에 세우고 F로 마치기";
 void OnGUI(){
  if(!business||!business.EveningDrive||Event.current.type!=EventType.Repaint)return;var s=business.session;
  if(s.paused||s.FullMapOpen||s.SettingsOpen||s.PracticeMenuOpen||s.law.HistoryOpen)return;
  if(style==null)style=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,alignment=TextAnchor.MiddleCenter,wordWrap=false};
  float scale=UiReadability.HudScale(Screen.width,Screen.height);style.fontSize=Mathf.RoundToInt(15*scale);style.normal.textColor=Color.white;
  var r=new Rect((Screen.width-560*scale)/2,24*scale,560*scale,34*scale);int depth=GUI.depth;GUI.depth=-170;
  var old=GUI.color;GUI.color=new Color(0,0,0,.55f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;GUI.Label(r,Text,style);GUI.depth=depth;
 }
}
