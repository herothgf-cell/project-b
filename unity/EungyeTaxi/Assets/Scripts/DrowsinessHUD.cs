using UnityEngine;
// Covers only the world edges; HUD and menu interaction remain accessible.
public class DrowsinessHUD : MonoBehaviour {
 public BusinessProgress business;
 void OnGUI(){
  if(!business||!business.DrowsinessRunning)return;
  var d=business.ledger.Driver.drowsiness;if(d==null||d.visionSeconds<=0)return;
  GUI.depth=10;
  if(d.Tunnel){GUI.color=Color.black;float w=Screen.width,h=Screen.height,x=w*.5f*(float)d.VisionLoss,y=h*.5f*(float)d.VisionLoss;GUI.DrawTexture(new Rect(0,0,x,h),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(w-x,0,x,h),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x,0,w-2*x,y),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(x,h-y,w-2*x,y),Texture2D.whiteTexture);GUI.color=Color.white;}
  GUI.depth=-40;float width=Mathf.Min(690,Screen.width-24);var rect=new Rect((Screen.width-width)/2,Screen.height*.63f,width,d.Blackout?155:70);KoreanText.Panel(rect);var style=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=20,wordWrap=true,alignment=TextAnchor.MiddleCenter};GUI.Label(new Rect(rect.x+10,rect.y+5,rect.width-20,d.Blackout?72:60),d.Blackout?"시야 상실 · 집으로 돌아가 휴식하겠습니까?\n정차 후 선택 · 긴급 회복 −20 (정상 수면 −40)":d.PassengerMustLeave?"졸음 위험 · 승객 중도 하차/호출 취소 · 정차 중\n휴식 전 신규 승차 불가 · 가까운 거점에서 수면":d.Warning?"졸음 예고 · 곧 시야가 좁아집니다 · 거점에서 쉬세요":"졸음 · 시야가 계속 좁아집니다 · 수면 전 회복되지 않습니다",style);
  if(d.Blackout&&GUI.Button(new Rect(rect.x+20,rect.y+85,rect.width-40,52),"귀가 휴식(임시 주차 거점) · 100초 / 같은 날 계속",new GUIStyle(GUI.skin.button){font=KoreanText.BoldFont,fontSize=19}))business.RequestEmergencyHome();
 }
}
