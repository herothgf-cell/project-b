using UnityEngine;

// Only returns a selection. Scene, save and network transitions stay in their owners.
public static class CasualMenuUI {
 static GUIStyle Text(int size,bool bold=false){var s=new GUIStyle(GUI.skin.label){font=bold?KoreanText.BoldFont:KoreanText.Font,fontSize=size,wordWrap=true};s.normal.textColor=CasualUI.Ink;return s;}
 static bool Mode(Rect r,string title,string detail,int glyph){
  bool click=GUI.Button(r,GUIContent.none,KoreanText.Button(22));
  CasualUI.RoundedFill(new Rect(r.x+18,r.y+24,54,54),CasualUI.Blue);HudGlyph.Draw(new Rect(r.x+30,r.y+36,30,30),glyph,Color.white);
  GUI.Label(new Rect(r.x+90,r.y+17,r.width-120,36),title,Text(28,true));GUI.Label(new Rect(r.x+90,r.y+58,r.width-120,44),detail,Text(17));return click;
 }
 public static int Main(){
  CasualUI.Backdrop();var matrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1200f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1200*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
  CasualUI.Panel(new Rect(45,65,485,590),CasualUI.Warm);
  GUI.Label(new Rect(76,100,420,150),"택시\n시뮬레이션",Text(58,true));GUI.Label(new Rect(78,270,410,34),"오늘도, 기분 좋은 운행",Text(23));
  CasualUI.Portrait(new Rect(150,330,270,270),1);
  CasualUI.Panel(new Rect(565,145,590,450));CasualUI.Header(new Rect(575,155,570,58),"어떻게 시작할까요?",1);
  int chosen=0;if(Mode(new Rect(587,235,546,112),"싱글","3일 영업 · 저장한 운행 이어하기",0))chosen=1;
  if(Mode(new Rect(587,363,546,112),"PVP","봇 대전 · LAN 사람 대전",1))chosen=2;
  if(GUI.Button(new Rect(587,507,546,54),"게임 종료",KoreanText.Button(20)))chosen=3;
  GUI.matrix=matrix;return chosen;
 }
}
