using System;
using System.Globalization;
using UnityEngine;

// One compact result card: what happened, what was lost, and the next action.
public static class AccidentPresentation {
 public static Rect Bounds(float width,float height){float scale=Mathf.Min(1.25f,width/760f,height/560f);return new Rect((width-580*scale)/2,(height-420*scale)/2,580*scale,420*scale);}
 public static Rect DamageBounds(float width,float height){float scale=Mathf.Min(1.25f,width/760f,height/603f);return new Rect((width-580*scale)/2,(height-463*scale)/2,580*scale,463*scale);}
 public static string DurabilityText(AccidentState state){return !state.damageApplied?"처리 중":state.appliedDamage==0?"변동 없음":"−"+state.appliedDamage;}
 public static string FeeText(AccidentState state){return state.Fee.ToString("N0",CultureInfo.InvariantCulture)+"원";}
 public static string TimeLossText(AccidentState state){int n=(int)Math.Ceiling(state.ProcessingDuration);return "−"+n/60+":"+(n%60).ToString("00");}
 public static string ResumeAction(AccidentState state,bool disabled){return state.TowRequired?(disabled?"공업사에서 수리":"공업사에서 재개"):disabled?"견인 안내":"운행 재개";}
 public static string KindText(AccidentState state){return state.Kind==AccidentKind.Person?"보행자":state.Kind==AccidentKind.Vehicle?"차량":"시설";}
 public static string SeverityText(AccidentState state){return state.Severity==AccidentSeverity.Major?"중대 사고":state.Severity==AccidentSeverity.Moderate?"중간 사고":"경미 사고";}
 public static string FareText(AccidentState state){var text=state.outcome??"";return text.Contains("보존")?"확정 운임 유지":text.Contains("운송 중단")||text.Contains("실수령")?"취소 · 0원":"운임 없음";}
 static GUIStyle Text(int size,bool bold=false,TextAnchor alignment=TextAnchor.MiddleLeft){return new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=size,wordWrap=false,alignment=alignment,normal={textColor=CasualUI.Ink}};}
 public static bool Draw(AccidentState state,bool disabled,bool pvp){
  bool damage=state.pedestrianDamageVersion==1;float height=damage?463:420;
  var bounds=damage?DamageBounds(Screen.width,Screen.height):Bounds(Screen.width,Screen.height);float scale=bounds.width/580;var matrix=GUI.matrix;int depth=GUI.depth;var tint=GUI.color;GUI.depth=-600;
  GUI.color=new Color(0,0,0,.22f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
  GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));float x=bounds.x/scale,y=bounds.y/scale;CasualUI.Panel(new Rect(x,y,580,height));
  CasualUI.RoundedFill(new Rect(x+8,y+8,564,90),CasualUI.Ink);
  CasualUI.RoundedFill(new Rect(x+24,y+24,64,64),new Color(1,.91f,.80f));HudGlyph.Draw(new Rect(x+40,y+40,32,32),2,CasualUI.Danger);
  var title=Text(27,true);title.normal.textColor=Color.white;var subtitle=Text(16);subtitle.normal.textColor=Color.white;
  GUI.Label(new Rect(x+104,y+22,450,40),"사고 발생",title);GUI.Label(new Rect(x+104,y+62,450,28),KindText(state)+" · "+SeverityText(state),subtitle);
  string[] labels={"현재 운임","사고 처리비","영업 시간","운전 점수"};string[] values={FareText(state),FeeText(state),TimeLossText(state),state.ScorePenalty>0?"−"+state.ScorePenalty:"변동 없음"};
  if(damage){Array.Resize(ref labels,5);Array.Resize(ref values,5);labels[4]="차량 내구도";values[4]=DurabilityText(state);}
  for(int i=0;i<labels.Length;i++){float row=y+111+i*43;CasualUI.RoundedFill(new Rect(x+24,row,532,37),CasualUI.Soft);GUI.Label(new Rect(x+38,row+1,180,35),labels[i],Text(17));GUI.Label(new Rect(x+218,row+1,324,35),values[i],Text(19,true,TextAnchor.MiddleRight));}
  string status=state.phase==AccidentPhase.Stopping?(state.TowRequired&&!state.workshopTransferred&&state.outcomeApplied?"공업사 빈자리 대기":"안전 정차 중"):pvp&&state.phase==AccidentPhase.Processing?"처리 중 · "+Math.Ceiling(state.remaining)+"초":state.TowRequired?"공업사에서 운행을 이어갑니다":"사고 처리가 끝나면 운행을 이어갑니다";
  if(state.recoveryRequired)status="안전 복구 공간 대기";
  CasualUI.RoundedFill(new Rect(x+24,y+height-130,532,42),CasualUI.Warm);GUI.Label(new Rect(x+36,y+height-129,508,40),status,Text(16));bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled&&!pvp&&state.phase==AccidentPhase.Waiting;
  bool clicked=GUI.Button(new Rect(x+24,y+height-74,532,50),pvp?"처리 중":ResumeAction(state,disabled),CasualUI.Action(20));GUI.enabled=oldEnabled;GUI.matrix=matrix;GUI.depth=depth;GUI.color=tint;return clicked;
 }
}
